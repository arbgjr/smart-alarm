# Especificação da Tela de Login — Smart Alarm

## Objetivo
A tela de login permite que o usuário acesse o sistema Smart Alarm de forma segura, utilizando autenticação tradicional (email/senha) ou passwordless (FIDO2/WebAuthn). A segurança é mandatória e todos os fluxos devem seguir as melhores práticas de UX, acessibilidade e proteção de dados.

---

## Estrutura Visual

### Layout
- Centralizar o formulário na tela, com espaçamento adequado.
- Exibir o logo do sistema no topo.
- Utilizar cores e fontes conforme o guia de estilo do projeto.
- Garantir contraste e acessibilidade (WCAG AA).

### Componentes
- **Campo Email**
  - Tipo: texto
  - Placeholder: "Digite seu email"
  - Validação: obrigatório, formato de email válido
  - Autocomplete: email
- **Campo Senha**
  - Tipo: senha
  - Placeholder: "Digite sua senha"
  - Validação: obrigatório, mínimo 6 caracteres
  - Botão para exibir/ocultar senha
- **Checkbox "Lembrar de mim"**
  - Tipo: booleano
  - Descrição: "Manter login por mais tempo"
- **Botão Entrar**
  - Estado: habilitado apenas se os campos forem válidos
  - Loading: exibir indicador durante requisição
- **Botão "Entrar com dispositivo" (FIDO2/WebAuthn)**
  - Estado: habilitado se o navegador suportar WebAuthn
  - Exibir ícone de segurança
- **Link "Esqueci minha senha"**
  - Redireciona para fluxo de recuperação
- **Link "Registrar nova conta"**
  - Redireciona para tela de registro
- **Mensagens de erro**
  - Exibir abaixo dos campos, com texto claro e acessível

---

## Regras de Negócio e Validação

- **Email**
  - Obrigatório
  - Formato válido (RFC 5322)
  - Máximo 254 caracteres
- **Senha**
  - Obrigatória
  - Mínimo 6 caracteres
  - Máximo 128 caracteres
- **Lembrar de mim**
  - Opcional
- **Tentativas de login**
  - Limitar a 5 tentativas consecutivas (bloqueio temporário)
  - Exibir mensagem clara em caso de bloqueio
- **Mensagens de erro**
  - "Email é obrigatório"
  - "Email deve ter formato válido"
  - "Senha é obrigatória"
  - "Senha deve ter pelo menos 6 caracteres"
  - "Credenciais inválidas"
  - "Conta bloqueada por excesso de tentativas"
- **Acessibilidade**
  - Todos os campos devem ter labels visíveis
  - Navegação por teclado completa
  - Mensagens de erro devem ser lidas por leitores de tela

---

## Comportamento

- **Ao submeter o formulário:**
  - Validar campos localmente
  - Enviar requisição POST para `/api/v1/auth/login` com `{ email, password, rememberMe }`
  - Exibir loading no botão
  - Em caso de sucesso, redirecionar para dashboard
  - Em caso de erro, exibir mensagem apropriada
- **Autenticação FIDO2/WebAuthn:**
  - Exibir botão se o navegador suportar
  - Ao clicar, iniciar fluxo WebAuthn (challenge via `/api/v1/auth/fido2/auth/start`)
  - Em caso de sucesso, redirecionar para dashboard
  - Em caso de erro, exibir mensagem apropriada
- **Recuperação de senha:**
  - Redirecionar para tela de recuperação
- **Registro:**
  - Redirecionar para tela de registro
- **Segurança:**
  - Nunca exibir tokens ou dados sensíveis no frontend
  - Utilizar HTTPS obrigatório
  - Sanitizar e validar todos os inputs
  - Não logar dados sensíveis
  - Implementar rate limiting e proteção contra brute force
  - Garantir conformidade LGPD

---

## API Endpoints Utilizados

- `POST /api/v1/auth/login` — Login tradicional
- `POST /api/v1/auth/fido2/auth/start` — Iniciar autenticação FIDO2
- `POST /api/v1/auth/fido2/auth/complete` — Completar autenticação FIDO2

---

## Testes e Validação

- Testar todos os fluxos (sucesso, erro, bloqueio)
- Validar acessibilidade (tab, leitor de tela)
- Testar em navegadores modernos
- Cobrir casos de borda (campos vazios, formatos inválidos, bloqueio)

---

## Observações Finais

- Seguir Atomic Design para componentes
- Documentar débitos técnicos e bugs encontrados
- Manter consistência com padrões do projeto
- Atualizar documentação em caso de mudanças

---

## Referências
- [WCAG 2.1](https://www.w3.org/WAI/standards-guidelines/wcag/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [LGPD](https://www.gov.br/lgpd)
- [WebAuthn API](https://developer.mozilla.org/en-US/docs/Web/API/Web_Authentication_API)
