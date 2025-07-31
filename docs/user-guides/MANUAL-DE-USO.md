# Manual de Uso - Smart Alarm

## 📱 **Guia Completo do Usuário**

**Versão**: 1.0  
**Data**: 30/07/2025  
**Sistema**: Smart Alarm MVP Phase 3

---

## 🎯 **Visão Geral**

O Smart Alarm é um sistema inteligente de gerenciamento de alarmes e rotinas que permite criar, editar e gerenciar suas atividades diárias de forma eficiente e intuitiva.

### **Funcionalidades Principais**

- ⏰ **Gerenciamento de Alarmes**: Criar, editar, ativar/desativar alarmes únicos ou recorrentes
- 🔄 **Sistema de Rotinas**: Criar sequências automatizadas de ações (notificações, emails, webhooks)
- 📊 **Dashboard Interativo**: Visão geral das suas atividades com estatísticas em tempo real
- 🔐 **Autenticação Segura**: Sistema seguro com JWT e preparação para FIDO2

---

## 🚀 **Primeiros Passos**

### **Acesso ao Sistema**

1. **URL de Desenvolvimento**: `http://localhost:5173`
2. **Navegador Recomendado**: Chrome, Firefox, Safari (versões recentes)
3. **Requisitos**: JavaScript habilitado, conexão com internet

---

## 🔐 **1. Autenticação**

### **1.1 Login**

#### **📍 Tela: Login**

```
┌─────────────────────────────────────┐
│            Smart Alarm              │
│                                     │
│  Email:    [________________]       │
│  Password: [________________]       │
│                                     │
│  [ Login ]  [Don't have account?]   │
└─────────────────────────────────────┘
```

**Fluxo de Login**:

1. **Acessar**: Navegue para o sistema
2. **Preencher**: Digite email e senha
3. **Autenticar**: Clique em "Login"
4. **Redirecionamento**: Sistema redireciona para Dashboard

**Estados da Tela**:

- **Loading**: Spinner durante autenticação
- **Erro**: Mensagem vermelha para credenciais inválidas
- **Sucesso**: Redirecionamento automático

### **1.2 Registro**

#### **📍 Tela: Registro**

```
┌─────────────────────────────────────┐
│           Create Account            │
│                                     │
│  Name:     [________________]       │
│  Email:    [________________]       │
│  Password: [________________]       │
│                                     │
│  [ Register ]  [Already registered?]│
└─────────────────────────────────────┘
```

**Fluxo de Registro**:

1. **Acessar**: Clique em "Don't have account?"
2. **Preencher**: Nome, email e senha
3. **Registrar**: Clique em "Register"
4. **Confirmação**: Redirecionamento para Dashboard

---

## 📊 **2. Dashboard Principal**

### **2.1 Visão Geral**

#### **📍 Tela: Dashboard**

```
┌─────────────────────────────────────────────────────────────┐
│  Smart Alarm                    Welcome, [Nome do Usuário]  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  📊 QUICK STATS                                             │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐        │
│  │ Active Alarms│ │Today's Alarms│ │Active Routines│        │
│  │      [X]     │ │      [Y]     │ │      [Z]      │        │
│  └──────────────┘ └──────────────┘ └──────────────┘        │
│                                                             │
│  ⏰ RECENT ALARMS                    🔄 RECENT ROUTINES     │
│  ┌─────────────────────────────┐    ┌────────────────────┐  │
│  │ • Morning Alarm - 07:00     │    │ • Daily Routine    │  │
│  │ • Meeting Reminder - 14:30  │    │ • Evening Routine  │  │
│  │ • Workout Time - 18:00      │    │ • Weekend Tasks    │  │
│  │                             │    │                    │  │
│  │ [+ Create Alarm]            │    │ [+ Create Routine] │  │
│  │ [View all →]                │    │ [View all →]       │  │
│  └─────────────────────────────┘    └────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

**Componentes do Dashboard**:

- **Header**: Nome do sistema e informações do usuário
- **Quick Stats**: Estatísticas resumidas (Active Alarms, Today's Alarms, Active Routines)
- **Recent Alarms**: Lista dos últimos alarmes com ações rápidas
- **Recent Routines**: Lista das últimas rotinas com ações rápidas
- **Quick Actions**: Botões para criar alarmes e rotinas diretamente
- **Navigation Links**: Links para páginas dedicadas

### **2.2 Ações Disponíveis**

**Quick Actions**:

1. **[+ Create Alarm]**: Abre modal de criação de alarme
2. **[+ Create Routine]**: Abre modal de criação de rotina
3. **[View all →]**: Navega para página dedicada

**Navigation**:

- **Active Alarms → /alarms**: Página completa de gerenciamento de alarmes
- **Active Routines → /routines**: Página completa de gerenciamento de rotinas

---

## ⏰ **3. Gerenciamento de Alarmes**

### **3.1 Página de Alarmes**

#### **📍 Tela: My Alarms (/alarms)**

```
┌─────────────────────────────────────────────────────────────┐
│  ← My Alarms                              [+ Create Alarm]  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  📋 ALARM LIST                                              │
│  ┌─────────────────────────────────────────────────────────┐│
│  │ ⏰ Morning Alarm                           🟢 ACTIVE    ││
│  │    07:00 - Daily                          [Edit] [🗑️]   ││
│  │                                                         ││
│  │ 📅 Meeting Reminder                       🔴 INACTIVE  ││
│  │    14:30 - Weekdays                       [Edit] [🗑️]   ││
│  │                                                         ││
│  │ 🏃 Workout Time                           🟢 ACTIVE    ││
│  │    18:00 - Mon, Wed, Fri                  [Edit] [🗑️]   ││
│  └─────────────────────────────────────────────────────────┘│
│                                                             │
│  [← Back to Dashboard]                                      │
└─────────────────────────────────────────────────────────────┘
```

**Funcionalidades**:

- **Lista Completa**: Todos os alarmes do usuário
- **Status Visual**: Indicadores de ativo/inativo
- **Ações por Item**: Edit e Delete para cada alarme
- **Navegação**: Voltar ao Dashboard

### **3.2 Criação de Alarme**

#### **📍 Modal: Create/Edit Alarm**

```
┌─────────────────────────────────────────────────────────────┐
│                    Create New Alarm                    [✕]  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Name: [_________________________]                          │
│        Morning Workout                                      │
│                                                             │
│  Date & Time:                                               │
│  📅 [2025-07-30] ⏰ [07:00]                                 │
│                                                             │
│  Recurring Pattern:                                         │
│  ○ Once      ● Daily      ○ Weekly      ○ Custom           │
│                                                             │
│  📱 Weekly Options (if Weekly selected):                    │
│  ☑️ Mon  ☑️ Tue  ☑️ Wed  ☑️ Thu  ☑️ Fri  ☐ Sat  ☐ Sun      │
│                                                             │
│  Status:                                                    │
│  ☑️ Enable this alarm                                       │
│                                                             │
│                    [Cancel] [Save Alarm]                    │
└─────────────────────────────────────────────────────────────┘
```

**Fluxo de Criação**:

1. **Abrir Modal**: Clique em "[+ Create Alarm]" no Dashboard ou página de alarmes
2. **Preencher Nome**: Digite um nome descritivo
3. **Definir Data/Hora**: Use os seletores de data e hora
4. **Configurar Recorrência**: Escolha padrão (Once, Daily, Weekly, Custom)
5. **Configurar Dias** (se Weekly): Selecione dias da semana
6. **Definir Status**: Marque se deve estar ativo
7. **Salvar**: Clique em "Save Alarm"

**Validações**:

- **Nome**: Obrigatório, mínimo 3 caracteres
- **Data/Hora**: Deve ser futura (para alarmes únicos)
- **Recorrência**: Pelo menos um dia selecionado (se Weekly)

### **3.3 Edição de Alarme**

**Fluxo de Edição**:

1. **Acessar**: Clique em "[Edit]" na lista de alarmes
2. **Modal Pré-preenchido**: Formulário com dados atuais
3. **Modificar**: Altere os campos desejados
4. **Salvar**: Clique em "Save Alarm"
5. **Confirmação**: Modal fecha e lista atualiza

---

## 🔄 **4. Gerenciamento de Rotinas**

### **4.1 Página de Rotinas**

#### **📍 Tela: My Routines (/routines)**

```
┌─────────────────────────────────────────────────────────────┐
│  ← My Routines                            [+ Create Routine]│
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  🔄 ROUTINE LIST                                            │
│  ┌─────────────────────────────────────────────────────────┐│
│  │ 🌅 Morning Routine                     🟢 ACTIVE       ││
│  │    3 steps - Starts at 06:00          [Edit] [🗑️]      ││
│  │    • Notification • Email • Webhook                    ││
│  │                                                         ││
│  │ 🌙 Evening Routine                     🔴 INACTIVE     ││
│  │    2 steps - Starts at 21:00          [Edit] [🗑️]      ││
│  │    • Notification • Delay                              ││
│  │                                                         ││
│  │ 📋 Weekend Tasks                       🟢 ACTIVE       ││
│  │    4 steps - Starts at 10:00          [Edit] [🗑️]      ││
│  │    • Email • Condition • Notification • Webhook       ││
│  └─────────────────────────────────────────────────────────┘│
│                                                             │
│  [← Back to Dashboard]                                      │
└─────────────────────────────────────────────────────────────┘
```

**Funcionalidades**:

- **Lista Completa**: Todas as rotinas do usuário
- **Preview de Steps**: Mostra tipos de passos configurados
- **Status Visual**: Indicadores de ativo/inativo
- **Ações por Item**: Edit e Delete para cada rotina

### **4.2 Criação de Rotina**

#### **📍 Modal: Create/Edit Routine**

```
┌─────────────────────────────────────────────────────────────┐
│                   Create New Routine                   [✕]  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Name: [_________________________]                          │
│        Morning Routine                                      │
│                                                             │
│  Description: [_____________________]                       │
│               Daily morning tasks                           │
│                                                             │
│  🔄 ROUTINE STEPS:                                          │
│  ┌─────────────────────────────────────────────────────────┐│
│  │ Step 1: Notification                         [Edit] [✕] ││
│  │   Send wake-up notification                             ││
│  │                                                         ││
│  │ Step 2: Email                                [Edit] [✕] ││
│  │   Send daily agenda email                               ││
│  │                                                         ││
│  │ Step 3: Webhook                              [Edit] [✕] ││
│  │   Trigger morning lights automation                     ││
│  └─────────────────────────────────────────────────────────┘│
│                                                             │
│  [+ Add Step ▼]                                             │
│  ┌─────────────────────────────────────────────────────────┐│
│  │ Step Type:                                              ││
│  │ ○ Notification  ○ Email  ○ Webhook  ○ Delay  ○ Condition││
│  └─────────────────────────────────────────────────────────┘│
│                                                             │
│  Status: ☑️ Enable this routine                             │
│                                                             │
│                   [Cancel] [Save Routine]                   │
└─────────────────────────────────────────────────────────────┘
```

**Fluxo de Criação**:

1. **Abrir Modal**: Clique em "[+ Create Routine]"
2. **Preencher Básico**: Nome e descrição
3. **Adicionar Steps**: Clique "[+ Add Step]"
4. **Configurar Step**: Escolha tipo e configure detalhes
5. **Repetir Steps**: Adicione quantos steps necessários
6. **Definir Status**: Marque se deve estar ativa
7. **Salvar**: Clique em "Save Routine"

### **4.3 Tipos de Steps**

#### **4.3.1 Notification Step**

```
┌─────────────────────────────────────────────────────────────┐
│                Configure Notification Step                  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Title: [________________________]                          │
│         Good Morning!                                       │
│                                                             │
│  Message: [_________________________]                       │
│           Time to start your day!                           │
│                                                             │
│  Type:                                                      │
│  ● Push Notification  ○ In-App  ○ Both                      │
│                                                             │
│                          [Save Step]                        │
└─────────────────────────────────────────────────────────────┘
```

#### **4.3.2 Email Step**

```
┌─────────────────────────────────────────────────────────────┐
│                   Configure Email Step                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  To: [__________________________]                           │
│      user@example.com                                       │
│                                                             │
│  Subject: [____________________]                             │
│           Daily Agenda                                      │
│                                                             │
│  Template:                                                  │
│  ○ Simple  ● Agenda  ○ Reminder  ○ Custom                   │
│                                                             │
│  Content: [_________________________]                       │
│           Your daily tasks are ready!                       │
│                                                             │
│                          [Save Step]                        │
└─────────────────────────────────────────────────────────────┘
```

#### **4.3.3 Webhook Step**

```
┌─────────────────────────────────────────────────────────────┐
│                  Configure Webhook Step                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  URL: [____________________________]                        │
│       https://api.home.com/lights/on                        │
│                                                             │
│  Method:                                                    │
│  ● POST  ○ GET  ○ PUT  ○ DELETE                              │
│                                                             │
│  Headers: [_______________________]                          │
│           Authorization: Bearer xxx                          │
│                                                             │
│  Body: [_________________________]                          │
│        {"action": "turn_on_lights"}                         │
│                                                             │
│                          [Save Step]                        │
└─────────────────────────────────────────────────────────────┘
```

#### **4.3.4 Delay Step**

```
┌─────────────────────────────────────────────────────────────┐
│                   Configure Delay Step                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Duration:                                                  │
│  [___] ○ Seconds  ● Minutes  ○ Hours                        │
│   5                                                         │
│                                                             │
│  Description: [_____________________]                       │
│               Wait before next action                       │
│                                                             │
│                          [Save Step]                        │
└─────────────────────────────────────────────────────────────┘
```

#### **4.3.5 Condition Step**

```
┌─────────────────────────────────────────────────────────────┐
│                 Configure Condition Step                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Condition Type:                                            │
│  ● Time-based  ○ Weather  ○ API Response  ○ Custom          │
│                                                             │
│  Time Condition:                                            │
│  Execute only if current time is:                           │
│  ○ Before  ● Between  ○ After                               │
│                                                             │
│  Start Time: [07:00]  End Time: [09:00]                     │
│                                                             │
│  Action if condition fails:                                 │
│  ○ Skip step  ● Stop routine  ○ Continue                    │
│                                                             │
│                          [Save Step]                        │
└─────────────────────────────────────────────────────────────┘
```

---

## 🛠️ **5. Estados da Interface**

### **5.1 Estados de Loading**

#### **Loading Dashboard**

```
┌─────────────────────────────────────────────────────────────┐
│                Smart Alarm                                  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│                     ⟲ Loading...                           │
│                Loading dashboard...                         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

#### **Loading Lists**

```
┌─────────────────────────────────────────────────────────────┐
│  ⏰ RECENT ALARMS                                           │
│  ┌─────────────────────────────────────────────────────────┐│
│  │                                                         ││
│  │                ⟲ Loading alarms...                     ││
│  │                                                         ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

### **5.2 Estados de Erro**

#### **Erro de Conexão**

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│                      ⚠️ Error                               │
│              Failed to load alarms                          │
│                   [Try Again]                               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

#### **Erro de Validação**

```
┌─────────────────────────────────────────────────────────────┐
│  Name: [_________________________]                          │
│        ⚠️ Name is required                                  │
│                                                             │
│  Date & Time:                                               │
│  📅 [2025-07-25] ⏰ [07:00]                                 │
│      ⚠️ Date must be in the future                          │
└─────────────────────────────────────────────────────────────┘
```

### **5.3 Estados Vazios**

#### **Lista Vazia**

```
┌─────────────────────────────────────────────────────────────┐
│  ⏰ RECENT ALARMS                                           │
│  ┌─────────────────────────────────────────────────────────┐│
│  │                                                         ││
│  │                📭 No alarms yet                         ││
│  │          Create your first alarm to get started         ││
│  │                                                         ││
│  │               [+ Create Alarm]                          ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

---

## 🎨 **6. Design System**

### **6.1 Cores e Temas**

**Alarmes (Azul)**:

- Primary: `#2563eb` (Blue 600)
- Background: `#dbeafe` (Blue 100)
- Border: `#93c5fd` (Blue 300)

**Rotinas (Verde)**:

- Primary: `#16a34a` (Green 600)
- Background: `#dcfce7` (Green 100)
- Border: `#86efac` (Green 300)

**Estados**:

- Active: `#10b981` (Green 500)
- Inactive: `#ef4444` (Red 500)
- Warning: `#f59e0b` (Yellow 500)

### **6.2 Ícones**

- ⏰ Alarmes
- 🔄 Rotinas  
- 📊 Dashboard
- 📱 Notificação
- 📧 Email
- 🔗 Webhook
- ⏱️ Delay
- 🔍 Condição
- ✅ Ativo
- ❌ Inativo
- 🗑️ Deletar
- ✏️ Editar

---

## 📱 **7. Responsividade**

### **7.1 Desktop (≥1024px)**

- Layout em duas colunas no Dashboard
- Modais centralizados (max-width: 600px)
- Navegação horizontal no header

### **7.2 Tablet (768px - 1023px)**

- Layout em coluna única no Dashboard
- Modais adaptados (max-width: 90vw)
- Botões maiores para touch

### **7.3 Mobile (≤767px)**

- Layout stack vertical
- Modais em tela cheia
- Navegação simplificada com hamburger menu
- Touch-friendly buttons (min 44px)

---

## ♿ **8. Acessibilidade**

### **8.1 Navegação por Teclado**

- **Tab**: Navegar entre elementos focáveis
- **Enter/Space**: Ativar botões e links
- **Escape**: Fechar modais
- **Arrow Keys**: Navegar em listas e seletores

### **8.2 Screen Readers**

- Todos os botões têm `aria-label`
- Modais têm `aria-modal="true"`
- Status são anunciados com `aria-live`
- Formulários têm labels associados

### **8.3 Contraste**

- Texto principal: Ratio 7:1 (AAA)
- Texto secundário: Ratio 4.5:1 (AA)
- Elementos interativos: Ratio 3:1 (AA)

---

## 🔧 **9. Troubleshooting**

### **9.1 Problemas Comuns**

**Modal não abre**:

- Verificar se há erros no console
- Recarregar a página
- Limpar cache do navegador

**Formulário não salva**:

- Verificar campos obrigatórios
- Verificar conexão com internet
- Verificar se dados são válidos

**Lista não carrega**:

- Verificar conexão com API
- Fazer logout/login novamente
- Verificar se serviço está funcionando

### **9.2 Suporte Técnico**

**Informações para Suporte**:

- Navegador e versão
- Sistema operacional
- Passos para reproduzir o problema
- Mensagens de erro (se houver)
- Screenshot da tela com problema

---

## 📈 **10. Próximas Funcionalidades**

### **10.1 Em Desenvolvimento**

- **Busca e Filtros**: Pesquisar alarmes e rotinas
- **Paginação**: Para listas grandes
- **Operações em Lote**: Selecionar múltiplos itens
- **Notificações Toast**: Feedback visual melhorado

### **10.2 Planejadas**

- **Temas**: Modo escuro/claro
- **Calendário**: Visualização em calendário
- **Estatísticas**: Relatórios de uso
- **Integração FIDO2**: Autenticação biométrica

---

## ✅ **11. Checklist de Uso**

### **Para Novos Usuários**

- [ ] Fazer registro no sistema
- [ ] Fazer login com credenciais
- [ ] Explorar o Dashboard
- [ ] Criar primeiro alarme
- [ ] Criar primeira rotina
- [ ] Testar funcionalidades de edição

### **Para Uso Diário**

- [ ] Verificar alarmes do dia no Dashboard
- [ ] Criar novos alarmes conforme necessário
- [ ] Ajustar rotinas existentes
- [ ] Verificar status de alarmes/rotinas
- [ ] Gerenciar alarmes inativos/ativos

---

**📞 Suporte**: Para dúvidas ou problemas, consulte a documentação técnica ou entre em contato com a equipe de desenvolvimento.

**🔄 Última Atualização**: 30/07/2025 - Versão 1.0 (MVP Phase 3)
