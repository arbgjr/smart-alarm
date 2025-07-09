# Memory Bank - Serverless & Deploy

## Resumo

Implementação de handlers serverless (OCI Functions) para fluxos principais do SmartAlarm, com deploy automatizado, parametrização via KeyVault e documentação completa.

## Exemplos de Uso

- Handler: `AlarmFunction` (criação de alarme)
- Script: `infrastructure/deploy-serverless.ps1`

## Critérios de Pronto Atendidos

- Código implementado, revisado e testado
- Documentação e ADR atualizados
- Cobertura mínima de 80% para código crítico
- Checklist de segurança e observabilidade atendido
- Solution compilando e testes passando

## Integração com Testes e Validação

O processo de implantação serverless agora inclui suporte para validação e testes de integração automatizados:

- **Ambiente de teste completo**: Scripts `start-dev-env.sh`, `test-integration.sh` e `stop-dev-env.sh` permitem executar testes de integração completos antes do deploy
- **Validação de serviços**: Cada serviço externo (RabbitMQ, PostgreSQL, MinIO, HashiCorp Vault) pode ser testado individualmente
- **Checklist pré-deploy**: `docs/architecture/checklist-serverless-deploy.md` atualizado com requisitos de validação
- **Stack de observabilidade**: Dashboards pré-configurados para monitorar a aplicação em produção

Para garantir a implantação segura, siga estas etapas:

1. Execute `./start-dev-env.sh all` para iniciar o ambiente completo
2. Execute `./test-integration.sh all` para validar todas as integrações
3. Verifique métricas e traces no Grafana/Jaeger
4. Realize o deploy serverless seguindo o checklist

---

> Atualize este registro a cada nova entrega relevante de serverless/deploy.
