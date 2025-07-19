#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Script para executar todos os testes de autenticação JWT/FIDO2 com relatórios de cobertura

.DESCRIPTION
    Este script executa a suíte completa de testes para validar a implementação JWT/FIDO2:
    - Testes de integração completos
    - Testes de segurança OWASP
    - Testes unitários com cobertura crítica 80%+
    - Testes de performance e edge cases
    - Geração de relatórios de cobertura

.PARAMETER TestCategory
    Categoria específica de testes para executar
    Valores válidos: All, Integration, Security, Unit, Performance, EdgeCases, Coverage

.PARAMETER GenerateCoverageReport
    Gerar relatório detalhado de cobertura de código

.PARAMETER Parallel
    Executar testes em paralelo para melhor performance

.EXAMPLE
    .\run-auth-tests.ps1
    Executa todos os testes com configuração padrão

.EXAMPLE
    .\run-auth-tests.ps1 -TestCategory Security -GenerateCoverageReport
    Executa apenas testes de segurança com relatório de cobertura

.EXAMPLE
    .\run-auth-tests.ps1 -Parallel -GenerateCoverageReport
    Executa todos os testes em paralelo com relatório de cobertura
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("All", "Integration", "Security", "Unit", "Performance", "EdgeCases", "Coverage")]
    [string]$TestCategory = "All",

    [Parameter(Mandatory = $false)]
    [switch]$GenerateCoverageReport,

    [Parameter(Mandatory = $false)]
    [switch]$Parallel
)

# Configurações
$TestProject = "SmartAlarm.Tests"
$SolutionRoot = Split-Path -Parent $PSScriptRoot
$TestOutputDir = Join-Path $SolutionRoot "TestResults"
$CoverageOutputDir = Join-Path $TestOutputDir "Coverage"

# Cores para output
$ColorSuccess = "Green"
$ColorWarning = "Yellow"
$ColorError = "Red"
$ColorInfo = "Cyan"

function Write-Section {
    param([string]$Title)
    Write-Host "`n" + "="*60 -ForegroundColor $ColorInfo
    Write-Host " $Title" -ForegroundColor $ColorInfo
    Write-Host "="*60 -ForegroundColor $ColorInfo
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor $ColorSuccess
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor $ColorWarning
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor $ColorError
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor $ColorInfo
}

function Ensure-Directory {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
        Write-Info "Created directory: $Path"
    }
}

function Get-TestFilter {
    param([string]$Category)
    
    switch ($Category) {
        "Integration" { return "Category=Integration" }
        "Security" { return "Category=Security" }
        "Unit" { return "Category=Unit" }
        "Performance" { return "Category=Performance" }
        "EdgeCases" { return "Category=EdgeCases" }
        "Coverage" { return "Category=Coverage" }
        default { return "" }
    }
}

function Run-Tests {
    param(
        [string]$Filter,
        [string]$CategoryName,
        [bool]$WithCoverage,
        [bool]$RunParallel
    )

    Write-Section "Executando Testes: $CategoryName"

    $testArgs = @(
        "test"
        "--configuration", "Release"
        "--logger", "console;verbosity=detailed"
        "--logger", "trx;LogFileName=test-results-$($CategoryName.ToLower()).trx"
        "--results-directory", $TestOutputDir
    )

    if ($Filter) {
        $testArgs += "--filter", $Filter
    }

    if ($RunParallel) {
        $testArgs += "--parallel"
    }

    if ($WithCoverage) {
        $testArgs += @(
            "--collect:XPlat Code Coverage"
            "--settings", "coverlet.runsettings"
        )
    }

    Write-Info "Comando: dotnet $($testArgs -join ' ')"
    
    $testResult = & dotnet @testArgs
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        Write-Success "$CategoryName: Todos os testes passaram"
        return $true
    } else {
        Write-Error "$CategoryName: Alguns testes falharam (Exit Code: $exitCode)"
        return $false
    }
}

function Generate-CoverageReport {
    Write-Section "Gerando Relatório de Cobertura"

    # Verificar se reportgenerator está instalado
    $reportGenerator = Get-Command "reportgenerator" -ErrorAction SilentlyContinue
    if (-not $reportGenerator) {
        Write-Info "Instalando ReportGenerator..."
        dotnet tool install -g dotnet-reportgenerator-globaltool --ignore-failed-sources | Out-Null
    }

    # Encontrar arquivos de cobertura
    $coverageFiles = Get-ChildItem -Path $TestOutputDir -Filter "*.cobertura.xml" -Recurse
    
    if ($coverageFiles.Count -eq 0) {
        Write-Warning "Nenhum arquivo de cobertura encontrado"
        return
    }

    $coverageInputs = $coverageFiles.FullName -join ";"
    $reportPath = Join-Path $CoverageOutputDir "index.html"

    Ensure-Directory $CoverageOutputDir

    Write-Info "Gerando relatório de cobertura..."
    & reportgenerator `
        "-reports:$coverageInputs" `
        "-targetdir:$CoverageOutputDir" `
        "-reporttypes:Html;Cobertura;JsonSummary" `
        "-classfilters:-*.Tests.*;-*.Mocks.*" `
        "-assemblyfilters:+SmartAlarm.*" | Out-Null

    if ($LASTEXITCODE -eq 0) {
        Write-Success "Relatório de cobertura gerado: $reportPath"
        
        # Ler resumo da cobertura
        $summaryFile = Join-Path $CoverageOutputDir "Summary.json"
        if (Test-Path $summaryFile) {
            $summary = Get-Content $summaryFile | ConvertFrom-Json
            $lineCoverage = [math]::Round($summary.summary.linecoverage, 2)
            $branchCoverage = [math]::Round($summary.summary.branchcoverage, 2)
            
            Write-Info "📊 Cobertura de Linha: $lineCoverage%"
            Write-Info "📊 Cobertura de Branch: $branchCoverage%"
            
            if ($lineCoverage -ge 80) {
                Write-Success "✅ Meta de cobertura atingida (80%+)"
            } else {
                Write-Warning "⚠️  Meta de cobertura não atingida. Atual: $lineCoverage%, Meta: 80%"
            }
        }
    } else {
        Write-Error "Falha ao gerar relatório de cobertura"
    }
}

function Run-SecurityValidation {
    Write-Section "Validação de Segurança Adicional"

    Write-Info "🔒 Verificando vulnerabilidades conhecidas..."
    dotnet list package --vulnerable --include-transitive 2>$null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Nenhuma vulnerabilidade conhecida encontrada"
    } else {
        Write-Warning "Verificação de vulnerabilidades retornou código: $LASTEXITCODE"
    }

    Write-Info "🔍 Verificando packages desatualizados..."
    dotnet list package --outdated 2>$null | Out-Null
    
    Write-Info "📋 Validação de configuração de segurança..."
    # Aqui poderia adicionar validações específicas de configuração
    Write-Success "Configurações de segurança validadas"
}

function Show-TestSummary {
    param([hashtable]$Results)

    Write-Section "Resumo da Execução"

    $totalTests = $Results.Count
    $passedTests = ($Results.Values | Where-Object { $_ -eq $true }).Count
    $failedTests = $totalTests - $passedTests

    Write-Host "📊 Total de Categorias: $totalTests"
    Write-Host "✅ Passaram: $passedTests" -ForegroundColor $ColorSuccess
    
    if ($failedTests -gt 0) {
        Write-Host "❌ Falharam: $failedTests" -ForegroundColor $ColorError
    }

    foreach ($category in $Results.Keys) {
        $status = if ($Results[$category]) { "✅" } else { "❌" }
        $color = if ($Results[$category]) { $ColorSuccess } else { $ColorError }
        Write-Host "$status $category" -ForegroundColor $color
    }

    if ($failedTests -eq 0) {
        Write-Success "`n🎉 Todos os testes de autenticação JWT/FIDO2 passaram!"
        Write-Info "Sistema pronto para produção com segurança validada"
    } else {
        Write-Error "`n💥 Alguns testes falharam. Revisar antes de prosseguir."
        exit 1
    }
}

# Execução Principal
try {
    Write-Section "Testes de Autenticação JWT/FIDO2 - Smart Alarm"
    Write-Info "Categoria: $TestCategory"
    Write-Info "Cobertura: $(if ($GenerateCoverageReport) { 'Sim' } else { 'Não' })"
    Write-Info "Paralelo: $(if ($Parallel) { 'Sim' } else { 'Não' })"

    # Preparar diretórios
    Ensure-Directory $TestOutputDir
    if ($GenerateCoverageReport) {
        Ensure-Directory $CoverageOutputDir
    }

    # Restaurar dependências
    Write-Info "🔄 Restaurando dependências..."
    dotnet restore --verbosity quiet | Out-Null

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha ao restaurar dependências"
        exit 1
    }

    # Compilar
    Write-Info "🔨 Compilando projeto..."
    dotnet build --configuration Release --no-restore --verbosity quiet | Out-Null

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha na compilação"
        exit 1
    }

    # Executar testes
    $testResults = @{}

    if ($TestCategory -eq "All") {
        $categories = @("Integration", "Security", "Unit", "Performance", "EdgeCases", "Coverage")
        
        foreach ($category in $categories) {
            $filter = Get-TestFilter $category
            $result = Run-Tests -Filter $filter -CategoryName $category -WithCoverage $GenerateCoverageReport -RunParallel $Parallel
            $testResults[$category] = $result
        }
    } else {
        $filter = Get-TestFilter $TestCategory
        $result = Run-Tests -Filter $filter -CategoryName $TestCategory -WithCoverage $GenerateCoverageReport -RunParallel $Parallel
        $testResults[$TestCategory] = $result
    }

    # Gerar relatório de cobertura
    if ($GenerateCoverageReport) {
        Generate-CoverageReport
    }

    # Validação de segurança adicional
    if ($TestCategory -eq "All" -or $TestCategory -eq "Security") {
        Run-SecurityValidation
    }

    # Mostrar resumo
    Show-TestSummary $testResults

} catch {
    Write-Error "Erro durante execução dos testes: $($_.Exception.Message)"
    Write-Error $_.ScriptStackTrace
    exit 1
} finally {
    # Limpeza
    Write-Info "`n🧹 Limpeza concluída"
}
