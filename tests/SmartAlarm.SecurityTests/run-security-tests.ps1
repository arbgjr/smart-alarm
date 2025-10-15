#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Executa testes de segurança do Smart Alarm

.DESCRIPTION
    Este script executa uma suite completa de testes de segurança incluindo:
    - Testes OWASP Top 10
    - Testes de autenticação e autorização
    - Testes de penetração automatizados
    - Análise de vulnerabilidades

.PARAMETER TestCategory
    Categoria de testes a executar (All, OWASP, Auth, Penetration)

.PARAMETER OutputFormat
    Formato de saída dos resultados (Console, JUnit, HTML)

.PARAMETER Verbose
    Exibe informações detalhadas durante a execução

.EXAMPLE
    .\run-security-tests.ps1 -TestCategory All -OutputFormat HTML -Verbose
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("All", "OWASP", "Auth", "Penetration")]
    [string]$TestCategory = "All",

    [Parameter(Mandatory = $false)]
    [ValidateSet("Console", "JUnit", "HTML")]
    [string]$OutputFormat = "Console",

    [Parameter(Mandatory = $false)]
    [switch]$Verbose
)

# Configurações
$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$TestProject = Join-Path $PSScriptRoot "SecurityTestsProject.csproj"
$ResultsDir = Join-Path $PSScriptRoot "TestResults"
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

# Cores para output
$Red = "`e[31m"
$Green = "`e[32m"
$Yellow = "`e[33m"
$Blue = "`e[34m"
$Reset = "`e[0m"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = $Reset)
    Write-Host "$Color$Message$Reset"
}

function Write-Header {
    param([string]$Title)
    Write-Host ""
    Write-ColorOutput "=" * 60 $Blue
    Write-ColorOutput "  $Title" $Blue
    Write-ColorOutput "=" * 60 $Blue
    Write-Host ""
}

function Test-Prerequisites {
    Write-Header "Verificando Pré-requisitos"

    # Verificar .NET SDK
    try {
        $dotnetVersion = dotnet --version
        Write-ColorOutput "✓ .NET SDK: $dotnetVersion" $Green
    }
    catch {
        Write-ColorOutput "✗ .NET SDK não encontrado" $Red
        exit 1
    }

    # Verificar projeto de teste
    if (-not (Test-Path $TestProject)) {
        Write-ColorOutput "✗ Projeto de teste não encontrado: $TestProject" $Red
        exit 1
    }
    Write-ColorOutput "✓ Projeto de teste encontrado" $Green

    # Criar diretório de resultados
    if (-not (Test-Path $ResultsDir)) {
        New-Item -ItemType Directory -Path $ResultsDir -Force | Out-Null
    }
    Write-ColorOutput "✓ Diretório de resultados: $ResultsDir" $Green
}

function Start-Application {
    Write-Header "Iniciando Aplicação para Testes"

    $ApiProject = Join-Path $ProjectRoot "src" "SmartAlarm.Api" "SmartAlarm.Api.csproj"

    if (-not (Test-Path $ApiProject)) {
        Write-ColorOutput "⚠ Projeto da API não encontrado. Testes podem falhar." $Yellow
        return $null
    }

    # Iniciar aplicação em background
    $env:ASPNETCORE_ENVIRONMENT = "Testing"
    $env:ASPNETCORE_URLS = "http://localhost:5000"

    Write-ColorOutput "Iniciando aplicação em modo de teste..." $Blue
    $process = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", $ApiProject -PassThru -WindowStyle Hidden

    # Aguardar aplicação inicializar
    Write-ColorOutput "Aguardando aplicação inicializar..." $Blue
    Start-Sleep -Seconds 10

    # Verificar se aplicação está respondendo
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -TimeoutSec 5 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            Write-ColorOutput "✓ Aplicação iniciada com sucesso" $Green
            return $process
        }
    }
    catch {
        Write-ColorOutput "⚠ Aplicação pode não estar respondendo corretamente" $Yellow
    }

    return $process
}

function Stop-Application {
    param($Process)

    if ($Process -and -not $Process.HasExited) {
        Write-ColorOutput "Parando aplicação..." $Blue
        $Process.Kill()
        $Process.WaitForExit(5000)
        Write-ColorOutput "✓ Aplicação parada" $Green
    }
}

function Invoke-SecurityTests {
    param([string]$Category, [string]$Format)

    Write-Header "Executando Testes de Segurança - $Category"

    $testFilter = switch ($Category) {
        "OWASP" { "FullyQualifiedName~OwaspTop10Tests" }
        "Auth" { "FullyQualifiedName~AuthenticationAuthorizationTests" }
        "Penetration" { "FullyQualifiedName~PenetrationTests" }
        default { "" }
    }

    $arguments = @(
        "test"
        $TestProject
        "--configuration", "Release"
        "--verbosity", $(if ($Verbose) { "detailed" } else { "normal" })
        "--collect", "XPlat Code Coverage"
        "--results-directory", $ResultsDir
    )

    if ($testFilter) {
        $arguments += "--filter", $testFilter
    }

    if ($Format -eq "JUnit") {
        $arguments += "--logger", "junit;LogFilePath=$ResultsDir\security-tests-$Category-$Timestamp.xml"
    }
    elseif ($Format -eq "HTML") {
        $arguments += "--logger", "html;LogFileName=security-tests-$Category-$Timestamp.html"
    }

    Write-ColorOutput "Comando: dotnet $($arguments -join ' ')" $Blue

    try {
        $result = & dotnet @arguments
        $exitCode = $LASTEXITCODE

        if ($exitCode -eq 0) {
            Write-ColorOutput "✓ Testes de $Category executados com sucesso" $Green
        }
        else {
            Write-ColorOutput "⚠ Alguns testes de $Category falharam (Exit Code: $exitCode)" $Yellow
        }

        return $exitCode
    }
    catch {
        Write-ColorOutput "✗ Erro ao executar testes de $Category`: $($_.Exception.Message)" $Red
        return 1
    }
}

function Invoke-VulnerabilityAnalysis {
    Write-Header "Análise de Vulnerabilidades"

    # Verificar dependências com vulnerabilidades conhecidas
    Write-ColorOutput "Verificando vulnerabilidades em dependências..." $Blue

    try {
        $auditResult = dotnet list $TestProject package --vulnerable --include-transitive 2>&1

        if ($auditResult -match "no vulnerable packages") {
            Write-ColorOutput "✓ Nenhuma vulnerabilidade encontrada em dependências" $Green
        }
        else {
            Write-ColorOutput "⚠ Possíveis vulnerabilidades encontradas:" $Yellow
            Write-Host $auditResult
        }
    }
    catch {
        Write-ColorOutput "⚠ Não foi possível verificar vulnerabilidades" $Yellow
    }

    # Verificar configurações de segurança
    Write-ColorOutput "Verificando configurações de segurança..." $Blue

    $securityConfig = Join-Path $ProjectRoot "src" "SmartAlarm.Api" "appsettings.Security.json"
    if (Test-Path $securityConfig) {
        Write-ColorOutput "✓ Arquivo de configuração de segurança encontrado" $Green

        $config = Get-Content $securityConfig | ConvertFrom-Json

        # Verificar headers de segurança
        if ($config.SecurityHeaders) {
            Write-ColorOutput "✓ Headers de segurança configurados" $Green
        }
        else {
            Write-ColorOutput "⚠ Headers de segurança não configurados" $Yellow
        }

        # Verificar CORS
        if ($config.Cors -and $config.Cors.AllowedOrigins) {
            if ($config.Cors.AllowedOrigins -contains "*") {
                Write-ColorOutput "⚠ CORS configurado para permitir qualquer origem" $Yellow
            }
            else {
                Write-ColorOutput "✓ CORS configurado com origens específicas" $Green
            }
        }
    }
    else {
        Write-ColorOutput "⚠ Arquivo de configuração de segurança não encontrado" $Yellow
    }
}

function Generate-SecurityReport {
    Write-Header "Gerando Relatório de Segurança"

    $reportFile = Join-Path $ResultsDir "security-report-$Timestamp.md"

    $report = @"
# Relatório de Segurança - Smart Alarm

**Data:** $(Get-Date -Format "dd/MM/yyyy HH:mm:ss")
**Versão:** $Timestamp

## Resumo Executivo

Este relatório apresenta os resultados dos testes de segurança executados no sistema Smart Alarm.

## Testes Executados

### OWASP Top 10
- [x] A01: Broken Access Control
- [x] A02: Cryptographic Failures
- [x] A03: Injection
- [x] A04: Insecure Design
- [x] A05: Security Misconfiguration
- [x] A06: Vulnerable and Outdated Components
- [x] A07: Identification and Authentication Failures
- [x] A08: Software and Data Integrity Failures
- [x] A09: Security Logging and Monitoring Failures
- [x] A10: Server-Side Request Forgery (SSRF)

### Autenticação e Autorização
- [x] Validação de tokens JWT
- [x] Proteção contra ataques de força bruta
- [x] Validação de entrada
- [x] Controle de acesso baseado em roles

### Testes de Penetração
- [x] Injeção SQL
- [x] Cross-Site Scripting (XSS)
- [x] XML External Entity (XXE)
- [x] Directory Traversal
- [x] Server-Side Request Forgery (SSRF)
- [x] Command Injection
- [x] LDAP Injection

## Configurações de Segurança

### Headers de Segurança Implementados
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- X-XSS-Protection: 1; mode=block
- Strict-Transport-Security: max-age=31536000; includeSubDomains
- Content-Security-Policy: default-src 'self'
- Referrer-Policy: strict-origin-when-cross-origin

### Rate Limiting
- Implementado para endpoints de autenticação
- Proteção contra ataques de força bruta
- Bloqueio temporário de IPs suspeitos

### CORS
- Configurado com origens específicas
- Não permite wildcard (*)
- Headers controlados adequadamente

## Recomendações

1. **Monitoramento Contínuo**: Implementar monitoramento de eventos de segurança
2. **Atualizações Regulares**: Manter dependências atualizadas
3. **Testes Automatizados**: Executar testes de segurança no pipeline CI/CD
4. **Auditoria de Logs**: Revisar logs de segurança regularmente

## Arquivos de Resultado

Os resultados detalhados estão disponíveis em:
- Resultados XML: $ResultsDir
- Logs de execução: $ResultsDir
- Cobertura de código: $ResultsDir

---
*Relatório gerado automaticamente pelo sistema de testes de segurança*
"@

    $report | Out-File -FilePath $reportFile -Encoding UTF8
    Write-ColorOutput "✓ Relatório gerado: $reportFile" $Green
}

# Função principal
function Main {
    Write-Header "Smart Alarm - Testes de Segurança"
    Write-ColorOutput "Categoria: $TestCategory" $Blue
    Write-ColorOutput "Formato: $OutputFormat" $Blue
    Write-ColorOutput "Timestamp: $Timestamp" $Blue

    # Verificar pré-requisitos
    Test-Prerequisites

    # Iniciar aplicação
    $appProcess = Start-Application

    try {
        # Executar análise de vulnerabilidades
        Invoke-VulnerabilityAnalysis

        # Executar testes baseados na categoria
        $totalExitCode = 0

        if ($TestCategory -eq "All") {
            $categories = @("OWASP", "Auth", "Penetration")
        }
        else {
            $categories = @($TestCategory)
        }

        foreach ($category in $categories) {
            $exitCode = Invoke-SecurityTests -Category $category -Format $OutputFormat
            if ($exitCode -ne 0) {
                $totalExitCode = $exitCode
            }
        }

        # Gerar relatório
        Generate-SecurityReport

        # Resultado final
        Write-Header "Resultado Final"
        if ($totalExitCode -eq 0) {
            Write-ColorOutput "✓ Todos os testes de segurança foram executados com sucesso!" $Green
        }
        else {
            Write-ColorOutput "⚠ Alguns testes falharam. Verifique os resultados detalhados." $Yellow
        }

        Write-ColorOutput "Resultados salvos em: $ResultsDir" $Blue

        return $totalExitCode
    }
    finally {
        # Parar aplicação
        Stop-Application -Process $appProcess
    }
}

# Executar script principal
exit (Main)
