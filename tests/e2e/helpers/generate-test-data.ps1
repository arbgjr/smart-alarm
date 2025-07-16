
Write-Host "Gerando dados de teste: usuários, alarmes, arquivos..."

# Criar usuários de teste
Invoke-RestMethod -Uri "http://localhost:5000/api/users" -Method Post -Body (@{ username = "user1"; email = "user1@email.com"; password = "senha123" } | ConvertTo-Json) -ContentType "application/json"
Invoke-RestMethod -Uri "http://localhost:5000/api/users" -Method Post -Body (@{ username = "user2"; email = "user2@email.com"; password = "senha456" } | ConvertTo-Json) -ContentType "application/json"

# Criar alarmes de teste
Invoke-RestMethod -Uri "http://localhost:5000/api/alarmes" -Method Post -Body (@{ hora = "07:00"; recorrencia = "diaria"; usuario = "user1" } | ConvertTo-Json) -ContentType "application/json"
Invoke-RestMethod -Uri "http://localhost:5000/api/alarmes" -Method Post -Body (@{ hora = "08:30"; recorrencia = "semanal"; usuario = "user2" } | ConvertTo-Json) -ContentType "application/json"

# Gerar arquivo de áudio de teste
Write-Host "Gerando arquivo de áudio de teste..."
sox -n -r 16000 -c 1 ../../data/audio-test.wav synth 1 sine 440

# Gerar arquivo de imagem de teste
Write-Host "Gerando arquivo de imagem de teste..."
magick convert -size 100x100 xc:blue ../../data/image-test.png

Write-Host "Dados de teste gerados com sucesso."
