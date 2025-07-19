# Cenário: Consulta e Atualização de Dados do Usuário
Write-Host "Cenário: Consulta e Atualização de Dados do Usuário"

# Consulta de dados cadastrais
Invoke-RestMethod -Uri "http://localhost:5000/api/users/user1" -Method Get

# Atualização de informações
Invoke-RestMethod -Uri "http://localhost:5000/api/users/user1" -Method Put -Body (@{ nome = "Novo Nome"; email = "novo@email.com" } | ConvertTo-Json) -ContentType "application/json"
