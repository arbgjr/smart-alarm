#!/bin/bash

echo "Finalizando serviços de teste..."
docker-compose -f ../../docker-compose.yml down

echo "Verificando containers..."
docker ps -a | grep -E 'alarm|ai|integration|minio|mock-server'

echo "Todos os serviços de teste foram finalizados."
