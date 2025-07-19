# Checklist de Deploy - Smart Alarm Serverless

## Código e Qualidade

- [x] Código compila sem erros
- [x] Atende requisitos da tarefa (handlers serverless, deploy automatizado, parametrização segura)
- [x] Padrões de código seguidos (Clean Architecture, SOLID)
- [x] Sem código inútil/comentários de debug
- [x] Commits seguem padrão convencional
- [x] Testes unitários adicionados
- [x] Todos os testes passando
- [x] Documentação atualizada (Swagger, Markdown, ADR)
- [x] Validação de dados implementada
- [x] Sem segredos hardcoded

## Integração e Ambiente

- [ ] Testes de integração executados com sucesso (`./test-integration.sh all`)
- [ ] Verificação de métricas e logs no ambiente de teste (Grafana/Jaeger)
- [ ] Validação de serviços externos (RabbitMQ, PostgreSQL, MinIO, Vault)
- [ ] Stack de observabilidade testada e configurada
- [ ] Healthchecks implementados e testados

## Segurança

- [ ] Validações de entrada implementadas em todos os endpoints
- [ ] Logging estruturado sem exposição de dados sensíveis
- [ ] Comunicação com serviços externos segura (TLS)
- [ ] Credenciais armazenadas em KeyVault
- [ ] Proteção contra injeções (SQL, NoSQL, LDAP, etc.)
- [ ] Análise de dependências realizada (vulnerabilidades)

## Governança e Documentação

- [x] Owners definidos para cada serviço/domínio
- [x] ADRs atualizados para decisões técnicas
- [x] Memory Bank atualizado a cada entrega
- [x] Documentação de endpoints e arquitetura atualizada
- [x] Checklist de PR seguido em todas as entregas

## Serverless Deploy

- [ ] Testes de carga realizados com limites adequados
- [ ] Configurações de escala validadas
- [ ] Configuração de timeout adequada para operações
- [ ] Métricas de custos implementadas e monitoradas
- [ ] Alarmes configurados para anomalias de uso/custo
- [ ] Testado em ambiente staging antes de produção
- [ ] Rollback testado e documentado
