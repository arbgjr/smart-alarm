
Write-Host "Finalizando serviços de teste..."
docker-compose -f "../../docker-compose.yml" down

Write-Host "Verificando containers..."
$containers = docker ps -a | Select-String -Pattern 'alarm|ai|integration|minio|mock-server'
if ($containers) {
    Write-Host "Containers ainda ativos:" -ForegroundColor Yellow
    $containers | ForEach-Object { Write-Host $_ }
} else {
    Write-Host "Todos os serviços de teste foram finalizados." -ForegroundColor Green
}
