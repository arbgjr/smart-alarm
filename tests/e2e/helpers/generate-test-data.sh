#!/bin/bash

echo "Gerando dados de teste: usuários, alarmes, arquivos..."

# Criar usuários de teste
curl -X POST "http://localhost:5000/api/users" \
  -H "Content-Type: application/json" \
  -d '{"username":"user1","email":"user1@email.com","password":"senha123"}'
curl -X POST "http://localhost:5000/api/users" \
  -H "Content-Type: application/json" \
  -d '{"username":"user2","email":"user2@email.com","password":"senha456"}'

# Criar alarmes de teste
curl -X POST "http://localhost:5000/api/alarmes" \
  -H "Content-Type: application/json" \
  -d '{"hora":"07:00","recorrencia":"diaria","usuario":"user1"}'
curl -X POST "http://localhost:5000/api/alarmes" \
  -H "Content-Type: application/json" \
  -d '{"hora":"08:30","recorrencia":"semanal","usuario":"user2"}'

# Gerar arquivo de áudio de teste
echo "Gerando arquivo de áudio de teste..."
sox -n -r 16000 -c 1 ../../data/audio-test.wav synth 1 sine 440

# Gerar arquivo de imagem de teste
echo "Gerando arquivo de imagem de teste..."
convert -size 100x100 xc:blue ../../data/image-test.png

echo "Dados de teste gerados com sucesso."
