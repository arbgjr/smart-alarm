# Testes de Integração - Smart Alarm

## Pré-requisitos

- Docker e Docker Compose instalados
- .NET 8 SDK instalado
- Portas dos serviços (RabbitMQ, Vault, MinIO, etc.) livres

## Passos para rodar os testes de integração

1. **Suba os serviços de infraestrutura necessários:**

   ```pwsh
   docker compose up -d --build
   ```

   > Isso irá iniciar RabbitMQ, Vault, MinIO, e demais serviços necessários para os testes.

2. **Execute os testes de integração:**

   ```pwsh
   dotnet test SmartAlarm.sln --filter Category=Integration --logger "console;verbosity=detailed"
   ```

   > O filtro garante que apenas os testes de integração sejam executados.

3. **Verifique o resultado:**
   - Todos os testes devem passar.
   - Em caso de falha, verifique se os containers estão rodando corretamente:

     ```pwsh
     docker compose ps
     docker logs <nome-do-container> --tail 50
     ```

## Observações

- Os testes estão localizados em `tests/SmartAlarm.Infrastructure.Tests` e `tests/SmartAlarm.KeyVault.Tests`.
- Os testes utilizam a categoria `Integration` para facilitar o filtro.
- Para rodar todos os testes (incluindo unitários), remova o filtro `--filter Category=Integration`.
- Caso precise rodar apenas um projeto de teste específico:

  ```pwsh
  dotnet test tests/SmartAlarm.Infrastructure.Tests --filter Category=Integration --logger "console;verbosity=detailed"
  ```

## Troubleshooting

- Certifique-se de que as variáveis de ambiente dos serviços estejam corretas no `docker-compose.yml`.
- Se algum serviço não subir, verifique as portas e dependências locais.
- Para resetar o ambiente:

  ```pwsh
  docker compose down -v
  docker compose up -d --build
  ```

---

> Dúvidas ou problemas? Consulte a documentação interna ou abra uma issue.
