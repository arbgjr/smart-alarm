# Acesso à Documentação Swagger/OpenAPI

A documentação interativa dos endpoints da API está disponível via Swagger UI.

- **URL:** `/swagger` (raiz do serviço)
- **Formato:** OpenAPI 3.0
- **Exemplos reais de payloads e respostas**
- **Autenticação:** Suporte a JWT Bearer Token para testar endpoints protegidos

## Como acessar

1. Execute a aplicação (`dotnet run` ou via Docker Compose)
2. Acesse `https://localhost:5001/swagger` no navegador
3. Explore e teste todos os endpoints disponíveis

## Observações

- O contrato OpenAPI é gerado automaticamente a partir dos controllers e anotações
- Todos os endpoints MVP estão documentados
- Utilize o botão "Authorize" para autenticar com JWT

Consulte também os arquivos `alarms.endpoints.md` e `error-handling.md` para detalhes de contratos e padrões de erro.
