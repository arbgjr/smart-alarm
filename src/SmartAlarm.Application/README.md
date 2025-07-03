# Fluxos Essenciais da Application Layer

## Criação de Alarme
1. Recebe `CreateAlarmCommand` com dados do DTO.
2. Valida entrada com `CreateAlarmDtoValidator`.
3. Se válido, instancia entidade `Alarm` e persiste via `IAlarmRepository`.
4. Loga, traça e incrementa métrica customizada.
5. Retorna `AlarmResponseDto`.

## Listagem de Alarmes
1. Recebe `ListAlarmsQuery` com `UserId`.
2. Consulta alarmes via `IAlarmRepository`.
3. Mapeia entidades para DTOs de resposta.
4. Loga e traça operação.
5. Retorna lista de `AlarmResponseDto`.

## Atualização de Alarme
1. Recebe `UpdateAlarmCommand` com `AlarmId` e DTO.
2. Valida entrada.
3. Busca alarme existente.
4. Se não encontrado, lança exceção.
5. Atualiza dados e persiste.
6. Loga e traça operação.
7. Retorna DTO atualizado.

## Exclusão de Alarme
1. Recebe `DeleteAlarmCommand` com `AlarmId`.
2. Busca alarme existente.
3. Se não encontrado, retorna falso/loga.
4. Exclui via repositório.
5. Loga e traça operação.
6. Retorna sucesso.

---

# Exemplos de Uso

## Criação
```csharp
var command = new CreateAlarmCommand(new CreateAlarmDto { Name = "Acordar", Time = DateTime.Today.AddHours(7), UserId = userId });
var result = await mediator.Send(command);
```

## Atualização
```csharp
var command = new UpdateAlarmCommand(alarmId, new CreateAlarmDto { Name = "Acordar cedo", Time = DateTime.Today.AddHours(6), UserId = userId });
var result = await mediator.Send(command);
```

## Exclusão
```csharp
var command = new DeleteAlarmCommand(alarmId);
bool success = await mediator.Send(command);
```

## Listagem
```csharp
var query = new ListAlarmsQuery(userId);
var alarms = await mediator.Send(query);
```
