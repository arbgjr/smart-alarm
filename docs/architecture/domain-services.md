# Domain Services – Smart Alarm

## Owners

- AlarmDomainService: @arman (Tech Lead)
- UserDomainService: @arman (Tech Lead)

## Overview

Os serviços de domínio centralizam as regras de negócio críticas do Smart Alarm, garantindo consistência, testabilidade e separação de responsabilidades conforme Clean Architecture.

### Serviços implementados

- **AlarmDomainService**: regras de criação, validação, disparo e agendamento de alarmes.
- **UserDomainService**: regras de ativação, desativação, unicidade de e-mail e vínculo com alarmes.

## Exemplos de Uso

### AlarmDomainService

```csharp
var canCreate = await alarmDomainService.CanUserCreateAlarmAsync(userId);
if (!canCreate) throw new BusinessException("Limite de alarmes atingido.");

var canTrigger = await alarmDomainService.CanTriggerAlarmAsync(alarmId);
if (canTrigger) { /* disparar alarme */ }
```

### UserDomainService

```csharp
var emailEmUso = await userDomainService.IsEmailAlreadyInUseAsync(email);
if (emailEmUso) throw new BusinessException("E-mail já cadastrado.");

var podeDesativar = await userDomainService.CanDeactivateUserAsync(userId);
if (!podeDesativar) throw new BusinessException("Usuário possui alarmes ativos.");
```

## Critérios de Pronto Atendidos

- Interfaces e implementações concretas para todos os fluxos críticos do MVP
- Testes unitários AAA cobrindo sucesso, erro e edge cases
- Documentação e exemplos de uso
- Owners definidos

## Checklist de Governança

- [x] Owners definidos para cada serviço/domínio
- [x] ADRs atualizados para decisões técnicas
- [x] Documentação de arquitetura atualizada
- [x] Checklist de PR seguido

---
> Atualize este documento sempre que houver mudanças relevantes nos serviços de domínio.
