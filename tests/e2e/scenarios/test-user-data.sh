#!/bin/bash

echo "Cenário: Consulta e Atualização de Dados do Usuário"

# Consulta de dados cadastrais
echo "Teste: consulta de dados do usuário"
curl -s -X GET "http://localhost:5000/api/users/user1" | tee /tmp/user-consulta.json

# Atualização de informações
echo "Teste: atualização de dados do usuário"
curl -s -X PUT "http://localhost:5000/api/users/user1" \
  -H "Content-Type: application/json" \
  -d '{"nome":"Novo Nome","email":"novo@email.com"}' | tee /tmp/user-atualizacao.json

# Erro: atualização com dados inválidos
echo "Teste: erro ao atualizar dados do usuário"
curl -s -X PUT "http://localhost:5000/api/users/user1" \
  -H "Content-Type: application/json" \
  -d '{"nome":"","email":"email-invalido"}' | tee /tmp/user-erro.json

# Restrição de acesso (sem token)
echo "Teste: restrição de acesso sem autenticação"
curl -s -X GET "http://localhost:5000/api/users/user2" | tee /tmp/user-restricao.json

# Logging: Verificar logs de usuário
echo "Verificando logs de usuário..."
docker logs alarmservice --tail 20

echo "Cenário de consulta e atualização de dados do usuário finalizado."
