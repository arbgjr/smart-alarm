$envVars = @('Machine', 'User')
foreach ($level in $envVars) {
    [System.Environment]::GetEnvironmentVariables($level).GetEnumerator() | ForEach-Object {
        Set-Item -Path env:$($_.Name) -Value $_.Value
    }
}
#region Variáveis e Pré-Checagens
$usuario = $env:GITHUB_USERNAME
$pat = $env:GITHUB_PAT

#region Mensagem de Commit via Arquivo
$commitMsgFile = $env:GIT_COMMIT_MSG_FILE
$msg = $null

if (-not $usuario -or -not $pat) {
    Write-Host ('{"level":"Error","msg":"Variáveis de ambiente GITHUB_USERNAME e/ou GITHUB_PAT não definidas."}')
    exit 1
}

if (-not $commitMsgFile) {
    Write-Host ('{"level":"Error","msg":"Variável de ambiente GIT_COMMIT_MSG_FILE não definida."}')
    exit 1
}
if (-not (Test-Path $commitMsgFile)) {
    Write-Host ('{"level":"Error","msg":"Arquivo de mensagem de commit não encontrado: ' + $commitMsgFile + '"}')
    exit 1
}
$msg = Get-Content $commitMsgFile -Raw
if ([string]::IsNullOrWhiteSpace($msg)) {
    Write-Host ('{"level":"Error","msg":"Arquivo de mensagem de commit está vazio: ' + $commitMsgFile + '"}')
    exit 1
}

# Tentar localizar o git.exe no PATH
$gitPath = (Get-Command git -ErrorAction SilentlyContinue).Source
if (-not $gitPath) {
    # Possíveis diretórios padrão do Windows
    $possiveis = @(
        "$env:ProgramFiles\Git\cmd\git.exe",
        "$env:ProgramFiles(x86)\Git\cmd\git.exe",
        "$env:UserProfile\scoop\apps\git\current\cmd\git.exe",
        "$env:LocalAppData\Programs\Git\cmd\git.exe"
    )
    foreach ($caminho in $possiveis) {
        if (Test-Path $caminho) {
            $gitPath = $caminho
            Write-Host ("{""level"":""Warning"",""msg"":""Git não estava no PATH, mas foi encontrado em: $gitPath""}")
            break
        }
    }
}
if ($gitPath) {
    Write-Host ("{""level"":""Info"",""msg"":""Git encontrado em: $gitPath""}")
} else {
    Write-Host ('{"level":"Error","msg":"Git não encontrado no PATH nem nos diretórios padrão. Instale o Git e reinicie o VS Code."}')
    exit 1
}

# Verificar se o diretório atual é um repositório Git
if (-not (Test-Path .git)) {
    Write-Host ('{"level":"Error","msg":"Diretório atual não é um repositório Git. Navegue até um repositório válido."}')
    exit 1
}

if (-not $msg) {
    $msg = 'chore(sync): sincronizar alterações locais com remoto via task VS Code'
}

# Construir URL remota usando concatenação para evitar problemas de sintaxe
$remote = "https://" + $usuario + ":" + $pat + "@github.com/arbgjr/smart-alarm.git"
Write-Host ('{"level":"Info","msg":"Iniciando sync local-remoto..."}')
#endregion

#region Preparar Remote
& $gitPath remote set-url origin $remote
if ($LASTEXITCODE -ne 0) {
    Write-Host ('{"level":"Error","msg":"Falha ao configurar remote."}')
    & $gitPath remote set-url origin https://github.com/arbgjr/smart-alarm.git
    exit 1
}
#endregion

#region Add & Commit
try {
    # Verificar se há conflitos não resolvidos antes de tentar commitar
    $conflicts = & $gitPath ls-files --unmerged
    if ($conflicts) {
        Write-Host ('{"level":"Error","msg":"Existem arquivos em conflito não resolvidos. Resolva os conflitos, faça git add/rm e tente novamente."}')
        throw "Conflitos não resolvidos"
    }

    & $gitPath add .
    if ($LASTEXITCODE -ne 0) {
        Write-Host ('{"level":"Error","msg":"Falha ao adicionar arquivos ao stage."}')
        throw "Erro ao adicionar arquivos"
    }

    & $gitPath commit -m "$msg"
    if ($LASTEXITCODE -eq 0) {
        Write-Host ('{"level":"Info","msg":"Commit realizado com sucesso."}')
    } elseif ($LASTEXITCODE -eq 1) {
        Write-Host ('{"level":"Info","msg":"Nada para commitar."}')
    } else {
        Write-Host ('{"level":"Error","msg":"Falha ao commitar alterações. Verifique se há conflitos não resolvidos."}')
        throw "Erro ao commitar"
    }
} catch {
    & $gitPath remote set-url origin https://github.com/arbgjr/smart-alarm.git
    Write-Host ('{"level":"Warning","msg":"Remote restaurado após erro."}')
    exit 1
}
#endregion

#region Pull --rebase
try {
    & $gitPath pull $remote main --rebase
    if ($LASTEXITCODE -ne 0) {
        Write-Host ('{"level":"Error","msg":"Falha ao fazer pull --rebase."}')
        throw "Erro no pull --rebase"
    }
    Write-Host ('{"level":"Info","msg":"Rebase concluído com sucesso."}')
} catch {
    & $gitPath remote set-url origin https://github.com/arbgjr/smart-alarm.git
    Write-Host ('{"level":"Warning","msg":"Remote restaurado após erro."}')
    exit 1
}
#endregion

#region Push
try {
    & $gitPath push
    if ($LASTEXITCODE -ne 0) {
        Write-Host ('{"level":"Error","msg":"Falha ao fazer push para o remoto."}')
        throw "Erro no push"
    }
    Write-Host ('{"level":"Info","msg":"Push realizado com sucesso."}')
    # Limpar arquivo de commit após sucesso
    try {
        if ($commitMsgFile -and (Test-Path $commitMsgFile)) {
            Clear-Content $commitMsgFile
            Write-Host ('{"level":"Info","msg":"Arquivo de commit limpo com sucesso."}')
        }
    } catch {
        Write-Host ('{"level":"Warning","msg":"Falha ao limpar arquivo de commit: ' + $commitMsgFile + '"}')
    }
} catch {
    & $gitPath remote set-url origin https://github.com/arbgjr/smart-alarm.git
    Write-Host ('{"level":"Warning","msg":"Remote restaurado após erro."}')
    exit 1
}
#endregion

#region Restaurar Remote
& $gitPath remote set-url origin https://github.com/arbgjr/smart-alarm.git
if ($LASTEXITCODE -ne 0) {
    Write-Host ('{"level":"Warning","msg":"Falha ao restaurar remote padrão. Verifique manualmente."}')
} else {
    Write-Host ('{"level":"Info","msg":"Remote restaurado para padrão."}')
}
#endregion
