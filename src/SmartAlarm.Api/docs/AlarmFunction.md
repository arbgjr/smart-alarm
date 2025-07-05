# SmartAlarm Serverless Function - Documentação

## Descrição

Handler serverless para criação de alarmes, compatível com OCI Functions. Utiliza Clean Architecture, logging estruturado, validação e integração segura com KeyVault.

### Exemplo de Uso

```csharp
var function = new AlarmFunction(mediator, logger, configuration, keyVaultProvider);
var response = await function.HandleAsync(new CreateAlarmCommand("Alarme Teste", ...));
```

### Parâmetros de Ambiente

- `DbPassword` (KeyVault): senha do banco de dados
- `ASPNETCORE_ENVIRONMENT`: ambiente de execução (dev, staging, prod)

### Observações

- Segredos nunca devem ser hardcoded
- Logging estruturado (Serilog)
- Testes unitários e integração obrigatórios

---

## Critérios de Pronto Atendidos

- Handler serverless implementado e testado
- Deploy automatizado via script PowerShell
- Parametrização via KeyVault
- Documentação atualizada
- Checklist de PR seguido

---

Consulte o ADR correspondente e o Memory Bank para decisões técnicas.
