# SmartAlarm Serverless Deploy

# Este script automatiza o build, publish e deploy do SmartAlarm.Api como função serverless na Oracle Cloud Infrastructure (OCI).

# ## Pré-requisitos
# - OCI CLI e Oracle Functions configurados
# - Docker instalado
# - Variáveis de ambiente e segredos configurados via KeyVault

# ## Parâmetros
# - `--env` (dev, staging, prod)

# ## Passos
# 1. Build do projeto
# 2. Publish para pasta de deploy
# 3. Deploy para OCI Functions

param(
    [string]$env = "dev"
)

Write-Host "[SmartAlarm] Build e Deploy Serverless ($env)"

# 1. Build
Write-Host "[SmartAlarm] Build..."
dotnet build ..\SmartAlarm.Api\SmartAlarm.Api.csproj -c Release || true
if ($LASTEXITCODE -ne 0) { Write-Error "Build falhou"; exit 1 }

# 2. Publish
Write-Host "[SmartAlarm] Publish..."
dotnet publish ..\SmartAlarm.Api\SmartAlarm.Api.csproj -c Release -o ..\publish\SmartAlarm.Api || true
if ($LASTEXITCODE -ne 0) { Write-Error "Publish falhou"; exit 1 }

# 3. Deploy OCI Function
Write-Host "[SmartAlarm] Deploy OCI Function..."
cd ..\publish\SmartAlarm.Api
fn deploy --app smart-alarm-backend-$env --local --no-bump || true
if ($LASTEXITCODE -ne 0) { Write-Error "Deploy OCI Function falhou"; exit 1 }

Write-Host "[SmartAlarm] Deploy concluído com sucesso."

# > Consulte a documentação oficial OCI Functions para detalhes de configuração.
