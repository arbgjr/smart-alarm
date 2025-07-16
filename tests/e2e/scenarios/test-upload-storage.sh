#!/bin/bash

echo "Cenário: Upload e Armazenamento de Arquivos"

# Upload válido de áudio
echo "Teste: upload válido de áudio"
curl -s -X POST "http://localhost:5000/api/storage/upload" \
  -H "Content-Type: audio/wav" \
  --data-binary "../../data/audio-test.wav" | tee /tmp/storage-upload-success.json

# Upload inválido (imagem como áudio)
echo "Teste: upload inválido (imagem como áudio)"
curl -s -X POST "http://localhost:5000/api/storage/upload" \
  -H "Content-Type: audio/wav" \
  --data-binary "../../data/image-test.png" | tee /tmp/storage-upload-error.json

# Listar arquivos
echo "Teste: listar arquivos no storage"
curl -s -X GET "http://localhost:5000/api/storage/list" | tee /tmp/storage-list.json

# Download de arquivo
echo "Teste: download de arquivo de áudio"
curl -s -X GET "http://localhost:5000/api/storage/download/audio-test.wav" -o /tmp/audio-test-downloaded.wav

# Exclusão de arquivo
echo "Teste: exclusão de arquivo de áudio"
curl -s -X DELETE "http://localhost:5000/api/storage/delete/audio-test.wav" | tee /tmp/storage-delete.json

# Erro de storage (simulação)
echo "Teste: erro de storage (serviço parado)"
docker stop minio 2>/dev/null
curl -s -X POST "http://localhost:5000/api/storage/upload" \
  -H "Content-Type: audio/wav" \
  --data-binary "../../data/audio-test.wav" || echo "Erro de conexão com storage"
docker start minio 2>/dev/null

# Logging: Verificar logs do storage
echo "Verificando logs do storage..."
docker logs minio --tail 20

echo "Cenário de upload e armazenamento de arquivos finalizado."
