# Cenário: Upload e Armazenamento de Arquivos
Write-Host "Cenário: Upload e Armazenamento de Arquivos"

# Upload de arquivo de áudio
Invoke-RestMethod -Uri "http://localhost:5000/api/storage/upload" -Method Post -InFile "../../data/audio-test.wav" -ContentType "audio/wav"

# Validação do armazenamento
Invoke-RestMethod -Uri "http://localhost:5000/api/storage/list" -Method Get

# Download e exclusão
Invoke-RestMethod -Uri "http://localhost:5000/api/storage/download/audio-test.wav" -Method Get
Invoke-RestMethod -Uri "http://localhost:5000/api/storage/delete/audio-test.wav" -Method Delete
