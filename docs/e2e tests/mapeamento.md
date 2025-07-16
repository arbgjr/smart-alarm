## Mapeamento dos Fluxos Principais para Testes E2E

### Overview

Este documento mapeia os fluxos críticos do sistema Smart Alarm para testes end-to-end (E2E), garantindo cobertura dos principais cenários de negócio, integrações e requisitos de segurança/compliance.

---

### Requirements

- Cobrir todos os fluxos listados abaixo com cenários de sucesso, erro e casos de borda.
- Simular interações reais do usuário e integrações externas.
- Validar autenticação, autorização e consentimento LGPD.
- Garantir que logs estruturados e monitoramento estejam ativos durante os testes.
- Testar upload/armazenamento de arquivos e integrações com AI-Service e serviços externos.
- Assegurar que as operações de CRUD estejam protegidas por RBAC.
- Verificar consistência dos dados do usuário e integridade das rotinas/agendamentos.
- Validar o fluxo completo de recuperação de senha (solicitação, envio de token, redefinição).
- Testar autenticação multifator (MFA) via FIDO2/WebAuthn.
- Cobrir cenários de sucesso, erro (token inválido/expirado) e casos de borda (tentativas múltiplas).
- Garantir registro de logs e rastreamento das operações de segurança.

---

### Implementation Steps

1. **Criação, Edição e Exclusão de Alarmes**
   - Testar criação de alarme via API.
   - Editar propriedades do alarme (horário, recorrência, etc.).
   - Excluir alarme e validar remoção.
   - Verificar logs e rastreamento das operações.

2. **Configuração de Rotinas e Agendamentos**
   - Criar rotina vinculada a alarmes.
   - Editar/agendar rotinas.
   - Cancelar ou reprogramar rotinas.
   - Validar persistência e integridade dos dados.

3. **Upload e Armazenamento de Arquivos**

#### Preparação do Ambiente
  - Verifique se o serviço de storage (MinIO/OCI/Object Storage) está ativo e acessível.
  - Gere arquivos de áudio/imagem de exemplo para upload.
  - Utilize mocks/stubs para simular storage externo, se necessário.
  - Confirme se já existem arquivos de teste antes de gerar novos.
  - ...existing code...

4. **Autenticação (JWT/FIDO2) e Autorização (RBAC)**

#### Preparação do Ambiente
  - Certifique-se que o serviço de autenticação/autorização está ativo.
  - Crie usuários com diferentes perfis e tokens JWT/FIDO2 válidos/expirados.
  - Simule FIDO2/WebAuthn via mock/stub se não houver ambiente real.
  - Verifique se já existem usuários e tokens de teste antes de criar novos.
  - ...existing code...

5. **Integração com AI-Service**

#### Preparação do Ambiente
  - Valide que o AI-Service está ativo e integrado.
  - Gere arquivos de áudio para análise.
  - Utilize mocks/stubs para simular respostas do AI-Service em cenários de erro ou indisponibilidade.
  - Confirme se já existem arquivos de teste antes de gerar novos.
  - ...existing code...

6. **Integração com Serviços Externos via IntegrationService**

#### Preparação do Ambiente
  - Confirme que o IntegrationService está ativo.
  - Gere notificações de teste (push/email/SMS).
  - Simule APIs de terceiros com mocks/stubs.
  - Verifique se já existem notificações de teste antes de criar novas.
  - ...existing code...

7. **Consulta e Atualização de Dados do Usuário**

#### Preparação do Ambiente
  - Verifique se o serviço de usuários está ativo.
  - Crie usuários com dados variados para consulta e atualização.
  - Confirme se já existem usuários de teste antes de criar novos.
  - ...existing code...

8. **Monitoramento e Logging Estruturado**
#### Preparação do Ambiente
  - Certifique-se que Serilog e Application Insights estão configurados e ativos.
  - Utilize operações dos cenários anteriores para gerar logs.
  - ...existing code...
   - Validar geração de logs estruturados (Serilog).
   - Verificar rastreamento de operações (Application Insights).
   - Simular falhas e garantir registro adequado.

9. **Gerenciamento de Consentimento LGPD**
   - Simular fluxo de consentimento do usuário.
   - Testar revogação e atualização de consentimento.
   - Validar restrição de acesso a dados sensíveis.
   - Verificar logs de consentimento.

10. **Recuperação de Senha**
   - Simular solicitação de recuperação de senha via endpoint.
   - Validar envio de token (email/SMS/notificação).
   - Testar redefinição de senha com token válido e inválido.
   - Verificar logs de solicitação e redefinição.

11. **Autenticação Multifator (MFA)**
   - Simular login com MFA habilitado (FIDO2/WebAuthn).
   - Testar fluxo de autenticação secundária (token, biometria, dispositivo).
   - Validar tratamento de erros (token expirado, falha de autenticação).
   - Verificar logs de autenticação MFA.

---

### Testing

- Implementar cenários E2E para cada fluxo acima, cobrindo:
  - Sucesso, erro e casos de borda.
  - Validação de autenticação/autorização.
  - Simulação de integrações externas (mocks/fakes).
  - Verificação de logs e monitoramento.
  - Testes de upload/download de arquivos.
  - Testes de consentimento LGPD.
  - Fluxo que respeite LGPD e não exponha dados sensíveis.
- Utilizar AAA (Arrange, Act, Assert) em todos os testes.
- Garantir mínimo de 80% de cobertura dos fluxos críticos.
- Executar testes com `--logger "console;verbosity=detailed"`.

---
