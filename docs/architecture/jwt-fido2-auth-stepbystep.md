# Passo a Passo: Implementação de Autenticação JWT/FIDO2 no Smart Alarm

## 1. Planejamento e Pré-requisitos

- Garanta que o backend está em .NET 8.0, Clean Architecture e com camadas separadas (Domain, Application, Infrastructure, Api).
- Dependências: `Microsoft.AspNetCore.Authentication.JwtBearer`, `Fido2`, `FluentValidation`, `Serilog`, `Application Insights`.
- Defina variáveis de ambiente para secrets (chaves JWT, FIDO2, etc). Nunca exponha segredos em código.

## 2. Estrutura de Diretórios

- `Infrastructure/Security/` para serviços de autenticação, geração/validação de tokens e FIDO2.
- `Application/DTOs/`, `Application/Handlers/`, `Application/Validators/` para fluxos de autenticação.
- `Api/Controllers/AuthController.cs` para endpoints públicos de login, registro, FIDO2 e refresh.

## 3. Implementação JWT

### a) Configuração JWT

- Configure o middleware JWT no `Program.cs` usando `AddAuthentication` e `AddJwtBearer`.
- Defina políticas de autorização (RBAC) e roles.
- Configure validação de token, expiração, audience, issuer e chaves.

### b) Serviços JWT

- Implemente `IJwtTokenService` e `JwtTokenService` para geração e validação de tokens.
- Implemente DTOs para login, registro e resposta de autenticação.
- Implemente handlers para login e registro, validando credenciais e emitindo JWT.
- Adicione logs estruturados em todos os fluxos.

### c) Testes JWT

- Crie testes unitários para geração e validação de tokens, login e registro.
- Cubra casos de sucesso, falha e borda (ex: token expirado, credenciais inválidas).

## 4. Implementação FIDO2/WebAuthn

### a) Configuração FIDO2/WebAuthn

- Adicione dependência `Fido2`.
- Configure endpoints para registro e autenticação FIDO2 no `AuthController`.
- Implemente serviços para challenge, registro e autenticação FIDO2.
- Armazene credenciais FIDO2 de forma segura (ex: tabela UserCredentials).

### b) Fluxos

- Registro: usuário inicia registro, recebe challenge, registra chave no dispositivo.
- Autenticação: usuário solicita login, challenge é validado via WebAuthn.
- Integre fallback para login tradicional (senha) e autenticação passwordless.

### c) Testes FIDO2/WebAuthn

- Teste fluxos de registro e autenticação FIDO2, incluindo falhas e edge cases.
- Valide integração com frontend (WebAuthn API).

## 5. Proteções OWASP e LGPD

- Implemente validação e sanitização de entradas em todos os endpoints.
- Proteja contra CSRF, XSS, injeção e brute force.
- Implemente RBAC e logging de tentativas de acesso negadas.
- Documente práticas de conformidade LGPD e fluxo de consentimento.

## 6. Observabilidade e Auditoria

- Adicione logs estruturados (Serilog) em todos os fluxos de autenticação.
- Implemente métricas customizadas para tentativas de login, falhas, bloqueios, etc.
- Implemente auditoria de acesso a dados sensíveis.

## 7. Documentação e Swagger

- Documente todos os endpoints de autenticação no Swagger/OpenAPI.
- Inclua exemplos de requests/responses e fluxos de erro.
- Documente requisitos de ambiente e variáveis sensíveis.

## 8. Checklist de Pull Request

- [ ] Código segue Clean Architecture e SOLID
- [ ] Testes unitários e de integração cobrindo todos os fluxos
- [ ] Cobertura mínima de 80% para código crítico
- [ ] Sem segredos hardcoded
- [ ] Documentação atualizada
- [ ] Logs estruturados e métricas implementados
- [ ] Conformidade LGPD e OWASP validada

---

> Consulte `docs/tech-debt/issues/07-implementar-seguranca.md` e `memory-bank/systemPatterns.md` para detalhes e exemplos de código.
