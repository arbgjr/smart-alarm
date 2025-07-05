# LGPD e Consentimento – SmartAlarm API

## Visão Geral

A API SmartAlarm implementa controles de consentimento do usuário conforme a LGPD, permitindo que cada usuário registre e consulte seu consentimento para tratamento de dados pessoais.

## Endpoints

- `POST /api/v1/consent?consentGiven=true|false`: Registra consentimento do usuário autenticado.
- `GET /api/v1/consent`: Consulta status de consentimento do usuário autenticado.

## Segurança

- Apenas usuários autenticados podem registrar ou consultar consentimento.
- Todas as ações são logadas para rastreabilidade.
- Consentimento é obrigatório para operações que envolvem dados pessoais sensíveis.

## Exemplo de Uso

```http
POST /api/v1/consent?consentGiven=true
Authorization: Bearer <token>

GET /api/v1/consent
Authorization: Bearer <token>
```

## Observações

- O serviço de consentimento pode ser estendido para persistência real e integração com logs de acesso e anonimização.
- Todas as operações são auditáveis via Serilog.
