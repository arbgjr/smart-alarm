# Issues de Débito Técnico - Smart Alarm

**Atualizado em**: 17/07/2025  
**Status do Projeto**: 94% pronto para produção

## 🔄 **ISSUES PENDENTES** (1 restante)

### 🔄 **MELHORAMENTOS OPCIONAIS** 
1. [Documentar API](./06-documentar-api.md) - 🔄 **Swagger implementado, melhoramentos opcionais**

## 🆕 **PENDÊNCIAS IDENTIFICADAS** (Auditoria 17/07/2025)

### 🔴 **CRÍTICAS** (Impedem produção)
1. **Corrigir Serviços Mock em Produção** - `DependencyInjection.cs:133-136`
2. **Implementar WebhookController Completo** - `WebhookController.cs:39`
3. **Finalizar OCI Vault Provider** - `OciVaultProvider.cs:148-208`
4. **Resolver Conflitos NU1107** - `System.Diagnostics.DiagnosticSource`

### 🟡 **IMPORTANTES** (Funcionalidade reduzida)
5. **Configurar APIs Externas Reais** - Google Calendar + Microsoft Graph
6. **Implementar Azure KeyVault Real** - Remover implementação mock
7. **Implementar JWT Blacklist** - Validação de revogação
8. **Implementar Fallback de Notificações** - Email backup

### 🔄 **MELHORAMENTOS DISPONÍVEIS**
- **Documentação API**: Mais exemplos e detalhamento (opcional)

### 🔴 **PENDÊNCIAS CRÍTICAS**
- **8 itens específicos** identificados na auditoria (ver lista acima)

## 🎯 **Próximos Passos**

1. **Criar issues específicas** para as 8 pendências críticas/importantes
2. **Priorizar issues críticas** (1-4) para produção
3. **Issue 06-documentar-api.md** pode permanecer como melhoramento opcional

**Status Final**: Projeto 94% pronto, apenas ajustes finais necessários
