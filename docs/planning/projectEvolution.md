## 1. Domain Model Impact

### Entities and Value Objects

- **Alarm** (`src/SmartAlarm.Domain/Entities/Alarm.cs`)
  - Currently, alarms have schedules and can be enabled/disabled, but there is no concept of "exceptions" (dates when the alarm should not trigger).
  - You will need to add a way to associate a list of "exception dates" or "disabled periods" with each alarm or schedule.

- **Schedule** (`src/SmartAlarm.Domain/Entities/Schedule.cs`)
  - Schedules define recurrence (daily, weekly, weekdays, weekends, etc.), but do not support exclusion of specific dates or periods.
  - You will need to add logic to check for exception dates and periods before triggering.

- **New Entity/Value Object: Holiday/ExceptionPeriod**
  - You may need a new entity or value object to represent a holiday or a period (start/end date) during which alarms are disabled.

### Domain Model Documentation

- `docs/domain-model.md`
  - Update documentation to reflect new relationships and business rules.

## 2. Application Layer Impact

### Commands, Queries, and Handlers

- **Alarm Creation/Update**
  - Update commands and handlers to allow associating exception dates/periods with alarms.
  - Files: `src/SmartAlarm.Application/Commands/`, `src/SmartAlarm.Application/Handlers/UpdateAlarmHandler.cs`, `src/SmartAlarm.Application/Handlers/CreateAlarmHandler.cs`

- **Import Functionality**
  - New command/query/handler for importing holiday/vacation data from files (CSV, Excel).
  - Likely new files: `ImportHolidaysCommand.cs`, `ImportHolidaysHandler.cs`

- **Validation**
  - Update validators to check for valid date ranges, overlapping periods, etc.
  - Files: `src/SmartAlarm.Application/Validators/`

### DTOs

- **Alarm DTOs**
  - Update to include exception dates/periods.
  - Files: `src/SmartAlarm.Application/DTOs/`

## 3. API Layer Impact

### Controllers

- **AlarmController** (`src/SmartAlarm.Api/Controllers/AlarmController.cs`)
  - Add endpoints for uploading holiday/vacation files.
  - Add endpoints for managing exception dates/periods for alarms.

- **File Upload Handling**
  - Add logic to handle file uploads (CSV, Excel parsing).
  - May require new services/utilities for file parsing.

### API Documentation

- `docs/api/alarms.endpoints.md`
  - Update to document new endpoints and request/response formats.

## 4. Infrastructure Layer Impact

### Data Persistence

- **Database Models/Configurations**
  - Update EF Core configurations to persist exception dates/periods.
  - Files: `src/SmartAlarm.Infrastructure/Data/Configurations/AlarmConfiguration.cs`, `ScheduleConfiguration.cs`
  - Add new tables/entities if needed for holidays/vacation periods.

- **Migrations**
  - New migrations to add tables/columns for exception dates/periods.

### File Parsing Utilities

- New utilities for parsing CSV/Excel files and mapping to domain models.

## 5. Business Logic Impact

### Alarm Trigger Logic

- **ShouldTriggerNow/ShouldTriggerToday**
  - Update logic to check if today is a holiday or within a disabled period before triggering.
  - Files: `src/SmartAlarm.Domain/Entities/Alarm.cs`, `Schedule.cs`

### Tests

- **Unit Tests**
  - Update and add tests for new logic (e.g., alarms do not trigger on holidays/vacation).
  - Files: `tests/SmartAlarm.Tests/Domain/Entities/AlarmTests.cs`, `ScheduleTests.cs`
  - Add tests for file import and validation.

## 6. Frontend/UX Impact

- **File Upload UI**
  - Add UI for uploading holiday/vacation files and selecting which alarms to disable.
  - Not directly in this codebase, but referenced in `docs/development/frontend-development.md` and `docs/legacy/frontend-development-instructions.md`.

- **Calendar/Alarm Management UI**
  - Update to show which alarms are disabled on which dates.

## 7. Documentation

- Update all relevant documentation:
  - `docs/domain-model.md`
  - `docs/business/Roadmap MVP - Smart Alarm.md`
  - `docs/development/frontend-development.md`
  - API docs

---

## Summary Table of Impacted Areas

| Layer            | Files/Areas Impacted                                                                                  | Description                                                                                   |
|------------------|------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------|
| Domain           | `Alarm.cs`, `Schedule.cs`, (new) `Holiday/ExceptionPeriod.cs`                                        | Add exception dates/periods, update trigger logic                                             |
| Application      | Commands, Handlers, Validators, DTOs                                                                 | Add import, update alarm logic, validate periods                                              |
| API              | `AlarmController.cs`, (new) endpoints, file upload handling                                          | Add endpoints for import, manage exceptions                                                   |
| Infrastructure   | EF Core configs, migrations, file parsing utilities                                                  | Persist new data, parse files                                                                 |
| Business Logic   | `ShouldTriggerNow`, `ShouldTriggerToday`                                                             | Check for holidays/periods before triggering                                                  |
| Tests            | `AlarmTests.cs`, `ScheduleTests.cs`, new tests for import/validation                                 | Ensure new logic is covered                                                                   |
| Documentation    | `domain-model.md`, `alarms.endpoints.md`, `frontend-development.md`, `Roadmap MVP - Smart Alarm.md`  | Update to reflect new features                                                                |
| Frontend (docs)  | `frontend-development.md`, `legacy/frontend-development-instructions.md`                             | UI for file upload, alarm management on holidays/vacation                                     |

---

## Key Files to Review/Modify or Extend

- `src/SmartAlarm.Domain/Entities/Alarm.cs`
- `src/SmartAlarm.Domain/Entities/Schedule.cs`
- `src/SmartAlarm.Domain/Entities/DaysOfWeek.cs`
- `src/SmartAlarm.Application/Handlers/UpdateAlarmHandler.cs`
- `src/SmartAlarm.Application/Handlers/CreateAlarmHandler.cs`
- `src/SmartAlarm.Api/Controllers/AlarmController.cs`
- `src/SmartAlarm.Infrastructure/Data/Configurations/AlarmConfiguration.cs`
- `src/SmartAlarm.Infrastructure/Data/Configurations/ScheduleConfiguration.cs`
- `tests/SmartAlarm.Tests/Domain/Entities/AlarmTests.cs`
- `tests/SmartAlarm.Tests/Domain/Entities/ScheduleTests.cs`
- Documentation in `docs/`

---

## Additional Notes

- You may need to introduce new domain concepts (e.g., `Holiday`, `ExceptionPeriod`) and their persistence.
- Consider concurrency and validation (e.g., overlapping periods, invalid dates).
- Ensure that the alarm reactivation after a period is automatic and robust.
- The import logic should handle file parsing errors gracefully and provide user feedback.
- All new business rules must be reflected in both backend logic and frontend UX (as described in documentation).

If you need a more granular breakdown for a specific layer or want to see code samples for any of these changes, let me know!Sua solicitação exige uma análise profunda de impacto arquitetural, de domínio, infraestrutura, testes e experiência do usuário. Segue uma análise detalhada, criteriosa e orientada pelas melhores práticas de engenharia de software, considerando Clean Architecture, SOLID, testabilidade, extensibilidade e padrões do projeto Smart Alarm.

---

## 1. Requisitos Funcionais e Técnicos

- Permitir upload de arquivo (CSV, Excel, tabular) com datas de feriados.
- Permitir seleção de feriados nos quais alarmes não devem tocar.
- Permitir cadastro de períodos de férias ou ausência, desativando alarmes nesse intervalo.
- Após o período, alarmes voltam a funcionar normalmente.
- Alarmes de trabalho não devem tocar em finais de semana nem feriados selecionados.

---

## 2. Impacto no Domínio

### a) Novos Conceitos/Entidades

- **Holiday (Feriado):** Representa uma data (ou intervalo) em que alarmes podem ser desativados.
- **UserHolidayPreference:** Relaciona usuário, feriado e se o alarme deve ser desativado.
- **VacationPeriod:** Período de ausência do usuário, durante o qual alarmes são desativados.

### b) Entidades Impactadas

- `Alarm`: Precisa consultar feriados e períodos de férias antes de disparar.
- `Schedule`: Lógica de disparo deve considerar feriados e férias.
- `User`: Pode ter preferências de feriados e períodos de férias.

### c) Value Objects

- Pode ser criado um VO para representar períodos (`DateRange`).

---

## 3. Impacto na Aplicação (Application Layer)

- **Serviços de Domínio:**
  - `AlarmDomainService` e possivelmente um novo `HolidayService` para lógica de feriados.
  - Métodos para verificar se um alarme deve ser disparado em determinada data, considerando feriados e férias.
- **Comandos/Queries:**
  - Upload de feriados (importação de arquivo).
  - Cadastro/edição de períodos de férias.
  - Consulta de alarmes ativos considerando regras de feriados/férias.

---

## 4. Impacto na Infraestrutura

- **Persistência:**
  - Novas tabelas para feriados, preferências de feriados e períodos de férias.
  - Mapeamento EF Core para novas entidades.
- **Importação de Arquivos:**
  - Serviço para processar arquivos CSV/Excel/tabular.
  - Validação e parsing dos arquivos.
- **Migrações de banco de dados:**
  - Scripts para criar novas tabelas e relacionamentos.

---

## 5. Impacto na API/Apresentação

- **Endpoints:**
  - Upload de feriados (arquivo).
  - CRUD de períodos de férias.
  - Configuração de preferências de feriados.
- **DTOs/ViewModels:**
  - Para upload, resposta de validação, cadastro de férias, etc.
- **Validação:**
  - Garantir que datas não se sobreponham, formatos corretos, etc.

---

## 6. Impacto em Testes

- **Unitários:**
  - Novos testes para lógica de feriados e férias.
  - Testes para parsing de arquivos.
  - Testes para lógica de disparo de alarme considerando feriados/férias.
- **Integração:**
  - Testes de endpoints de upload e cadastro.
  - Testes de integração com banco de dados para novas entidades.

---

## 7. Impacto em Documentação

- Atualizar Swagger/OpenAPI para novos endpoints.
- Documentar novos fluxos e regras de negócio.
- Atualizar documentação de arquitetura e padrões.

---

## 8. Impacto em Segurança e LGPD

- Garantir que dados de feriados/férias sejam tratados conforme LGPD.
- Validar uploads para evitar arquivos maliciosos.

---

## 9. Impacto em Observabilidade

- Logar operações de upload, cadastro e falhas.
- Monitorar uso de novas funcionalidades.

---

## 10. Pontos Específicos de Alteração

### a) Domínio (SmartAlarm.Domain)

- Criar entidades: `Holiday`, `UserHolidayPreference`, `VacationPeriod`.
- Alterar `Alarm` e `Schedule` para considerar feriados/férias em métodos como `ShouldTriggerNow` e `ShouldTriggerToday`.
- Adicionar serviços de domínio para lógica de feriados/férias.

### b) Infraestrutura (SmartAlarm.Infrastructure)

- Criar configurações EF Core para novas entidades.
- Implementar repositórios para feriados e férias.
- Serviço de importação de arquivos (pode ser em Application ou Infrastructure).

### c) Application Layer

- Comandos/queries para upload, cadastro e consulta.
- Handlers para processar lógica.

### d) API

- Endpoints para upload, cadastro e consulta.
- DTOs para entrada/saída.

### e) Testes (SmartAlarm.Domain.Tests, SmartAlarm.Tests)

- Testes unitários para lógica de domínio.
- Testes de integração para endpoints e persistência.

### f) Documentação

- Atualizar documentação técnica e de usuário.

---

## 11. Resumo dos Arquivos/Locais a Alterar ou Criar

- `src/SmartAlarm.Domain/Entities/Holiday.cs` (nova)
- `src/SmartAlarm.Domain/Entities/VacationPeriod.cs` (nova)
- `src/SmartAlarm.Domain/Entities/UserHolidayPreference.cs` (nova)
- Alarm.cs (métodos de verificação)
- Schedule.cs (métodos de verificação)
- `src/SmartAlarm.Domain/Services/HolidayService.cs` (nova)
- AlarmDomainService.cs (ajustes)
- `src/SmartAlarm.Infrastructure/Data/Configurations/HolidayConfiguration.cs` (nova)
- `src/SmartAlarm.Infrastructure/Data/Configurations/VacationPeriodConfiguration.cs` (nova)
- `src/SmartAlarm.Infrastructure/Data/Configurations/UserHolidayPreferenceConfiguration.cs` (nova)
- `src/SmartAlarm.Infrastructure/Repositories/HolidayRepository.cs` (nova)
- `src/SmartAlarm.Infrastructure/Repositories/VacationPeriodRepository.cs` (nova)
- `src/SmartAlarm.Infrastructure/Repositories/UserHolidayPreferenceRepository.cs` (nova)
- `src/SmartAlarm.Application/Commands/ImportHolidaysCommand.cs` (nova)
- `src/SmartAlarm.Application/Handlers/ImportHolidaysHandler.cs` (nova)
- `src/SmartAlarm.Application/Commands/CreateVacationPeriodCommand.cs` (nova)
- `src/SmartAlarm.Application/Handlers/CreateVacationPeriodHandler.cs` (nova)
- `src/SmartAlarm.Api/Controllers/HolidaysController.cs` (nova)
- `src/SmartAlarm.Api/Controllers/VacationPeriodsController.cs` (nova)
- `tests/SmartAlarm.Domain.Tests/HolidayTests.cs` (nova)
- `tests/SmartAlarm.Domain.Tests/VacationPeriodTests.cs` (nova)
- AlarmTests.cs (novos cenários)
- ScheduleTests.cs (novos cenários)
- Documentação: docs, `api/`, `architecture/`

---

## 12. Considerações Finais

- O impacto é amplo: domínio, infraestrutura, API, testes e documentação.
- Exige novas entidades, lógica de negócio, endpoints e testes.
- Atenção à consistência, validação, segurança e cobertura de testes.
- Recomenda-se planejamento incremental: primeiro domínio e testes, depois infraestrutura, depois API e integração.
