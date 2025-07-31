# Documentação Técnica - Smart Alarm Frontend

## 📋 **Status do Projeto - 30/07/2025**

### **MVP Phase 3: Core User Interface - 40% Completo**

**Objetivo**: Implementar interface de usuário abrangente com funcionalidade CRUD completa para alarmes e rotinas.

---

## 🏗️ **Arquitetura Frontend**

### **Stack Tecnológico**

- **React 18**: Framework principal com hooks modernos
- **TypeScript**: Tipagem estática para maior confiabilidade
- **Vite**: Build tool e dev server (localhost:5173)
- **React Query v4**: Gerenciamento de estado e cache de API
- **React Router v6**: Roteamento client-side
- **Tailwind CSS**: Framework de estilos utility-first
- **Axios**: Cliente HTTP com interceptors

### **Estrutura de Diretórios**

```
frontend/src/
├── components/
│   ├── atoms/           # Componentes básicos (Button, Input)
│   ├── molecules/       # Componentes compostos (AlarmForm, RoutineForm)
│   │   ├── AlarmForm/
│   │   ├── RoutineForm/
│   │   └── ErrorBoundary/
│   ├── AlarmList.tsx    # Lista de alarmes
│   └── RoutineList.tsx  # Lista de rotinas
├── pages/
│   ├── Dashboard/       # Dashboard principal
│   ├── Alarms/         # Página dedicada de alarmes
│   └── Routines/       # Página dedicada de rotinas
├── hooks/
│   ├── useAuth.ts      # Hook de autenticação
│   ├── useAlarms.ts    # Hooks para alarmes
│   └── useRoutines.ts  # Hooks para rotinas
├── services/
│   ├── api.ts          # Cliente Axios configurado
│   ├── authService.ts  # Serviços de autenticação
│   ├── alarmService.ts # Serviços de alarmes
│   └── routineService.ts # Serviços de rotinas
└── types/
    ├── auth.ts         # Tipos de autenticação
    ├── alarm.ts        # Tipos de alarmes
    └── routine.ts      # Tipos de rotinas
```

---

## 🔧 **Componentes Implementados**

### **1. Componentes de Formulário**

#### **AlarmForm Component**

**Localização**: `/src/components/molecules/AlarmForm/`

**Props Interface**:

```typescript
interface AlarmFormProps {
  alarm?: AlarmDto | null;
  isOpen: boolean;
  onSuccess: () => void;
  onCancel: () => void;
}
```

**Funcionalidades**:

- Modal responsivo com z-index 50
- Datetime picker para seleção de data/hora
- Padrões de recorrência (Once, Daily, Weekly, Custom)
- Seleção de dias da semana para alarmes semanais
- Toggle de ativação/desativação
- Validação de campos obrigatórios
- Integração com hooks `useCreateAlarm` e `useUpdateAlarm`

**Estados Internos**:

- `formData`: Dados do formulário
- `isRecurring`: Controle de recorrência
- `selectedDays`: Dias selecionados para alarmes semanais

#### **RoutineForm Component**

**Localização**: `/src/components/molecules/RoutineForm/`

**Props Interface**:

```typescript
interface RoutineFormProps {
  routine?: RoutineDto | null;
  isOpen: boolean;
  onSuccess: () => void;
  onCancel: () => void;
}
```

**Funcionalidades**:

- Modal complexo para gerenciamento de rotinas
- Sistema dinâmico de steps com diferentes tipos:
  - **Notification**: Notificações push/in-app
  - **Email**: Envio de emails com templates
  - **Webhook**: Chamadas HTTP para APIs externas
  - **Delay**: Pausas temporais entre ações
  - **Condition**: Condições lógicas para execução
- Interface para adicionar/remover/editar steps
- Configuração específica por tipo de step
- Preview dos steps configurados
- Validação de rotina e steps

### **2. Páginas Principais**

#### **Dashboard Component**

**Localização**: `/src/pages/Dashboard/`

**Funcionalidades**:

- Header com informações do usuário
- Quick Stats com contadores (Active Alarms, Today's Alarms, Active Routines)
- Listas recentes de alarmes e rotinas
- Botões de ação rápida para criar alarmes/rotinas
- Links de navegação para páginas dedicadas
- Modais integrados para criação rápida

#### **AlarmsPage Component**

**Localização**: `/src/pages/Alarms/`

**Funcionalidades**:

- Página dedicada para gerenciamento completo de alarmes
- Header com navegação e botão de criação
- Lista completa de alarmes do usuário
- Integração com AlarmForm para criação/edição
- Botões de ação por item (Edit, Delete)
- Estados de loading, erro e lista vazia

#### **RoutinesPage Component**

**Localização**: `/src/pages/Routines/`

**Funcionalidades**:

- Página dedicada para gerenciamento completo de rotinas
- Design consistente com AlarmsPage
- Preview de steps configurados por rotina
- Integração com RoutineForm para criação/edição
- Controles de status visual (ativo/inativo)

---

## 🔗 **Sistema de Roteamento**

### **Configuração de Rotas**

**Arquivo**: `/src/App.tsx`

```typescript
// Rotas principais
<Route path="/" element={<LoginPage />} />
<Route path="/register" element={<RegisterPage />} />

// Rotas protegidas
<Route path="/dashboard" element={<ProtectedRoute><Dashboard /></ProtectedRoute>} />
<Route path="/alarms" element={<ProtectedRoute><AlarmsPage /></ProtectedRoute>} />
<Route path="/routines" element={<ProtectedRoute><RoutinesPage /></ProtectedRoute>} />
```

### **Proteção de Rotas**

- Component `ProtectedRoute` verifica autenticação
- Redirecionamento automático para login se não autenticado
- Persistência de estado de autenticação via React Query

---

## 🔌 **Integração com API**

### **Configuração do Cliente HTTP**

**Arquivo**: `/src/services/api.ts`

- Cliente Axios configurado com base URL
- Interceptors para inclusão automática de tokens JWT
- Refresh automático de tokens expirados
- Tratamento padronizado de erros

### **Hooks de Estado Global**

#### **useAlarms Hook**

```typescript
// Queries disponíveis
useAlarms()           // Lista todos os alarmes
useActiveAlarms()     // Alarmes ativos
useTodaysAlarms()     // Alarmes de hoje

// Mutations disponíveis
useCreateAlarm()      // Criar novo alarme
useUpdateAlarm()      // Atualizar alarme existente
useDeleteAlarm()      // Deletar alarme
```

#### **useRoutines Hook**

```typescript
// Queries disponíveis
useRoutines()         // Lista todas as rotinas
useActiveRoutines()   // Rotinas ativas

// Mutations disponíveis
useCreateRoutine()    // Criar nova rotina
useUpdateRoutine()    // Atualizar rotina existente
useDeleteRoutine()    // Deletar rotina
```

---

## 🎨 **Sistema de Design**

### **Padrões de Cores**

```typescript
// Alarmes (Tema Azul)
primary: '#2563eb'      // Blue 600
background: '#dbeafe'   // Blue 100
border: '#93c5fd'       // Blue 300

// Rotinas (Tema Verde)
primary: '#16a34a'      // Green 600
background: '#dcfce7'   // Green 100
border: '#86efac'       // Green 300

// Estados
active: '#10b981'       // Green 500
inactive: '#ef4444'     // Red 500
warning: '#f59e0b'      // Yellow 500
```

### **Componentes de UI**

- **Buttons**: Variações primary, secondary, danger
- **Modals**: Z-index 50, backdrop blur, responsivos
- **Cards**: Sombra sutil, bordas arredondadas
- **Forms**: Labels flutuantes, validação visual
- **Lists**: Hover states, ações inline

---

## 📱 **Responsividade**

### **Breakpoints Tailwind**

```typescript
sm: '640px'    // Tablet pequeno
md: '768px'    // Tablet
lg: '1024px'   // Desktop
xl: '1280px'   // Desktop grande
2xl: '1536px'  // Ultra-wide
```

### **Padrões Responsivos**

- **Mobile First**: Design começando por mobile
- **Dashboard**: 2 colunas em desktop, stack em mobile
- **Modais**: Full-screen em mobile, centered em desktop
- **Formulários**: Campos empilhados em mobile

---

## ♿ **Acessibilidade**

### **Compliance WCAG 2.1**

- **AA Level**: Contraste de cores adequado
- **Navegação por Teclado**: Tab order lógico
- **Screen Readers**: ARIA labels e descriptions
- **Focus Management**: Outline visível e lógico

### **Implementações Específicas**

```typescript
// Modais
role="dialog"
aria-modal="true"
aria-labelledby="modal-title"

// Botões
aria-label="Create new alarm"
aria-describedby="button-description"

// Formulários
htmlFor="field-id"
aria-required="true"
aria-invalid={hasError}
```

---

## 🔄 **Estados da Interface**

### **Estados de Loading**

- **Skeleton Screens**: Para listas e cards
- **Spinners**: Para operações pontuais
- **Progress Bars**: Para uploads/downloads
- **Button Loading**: Spinners em botões durante ações

### **Estados de Erro**

- **API Errors**: Mensagens contextuais
- **Network Errors**: Retry automático com feedback
- **Validation Errors**: Inline em formulários
- **404 States**: Páginas de erro customizadas

### **Estados Vazios**

- **Empty Lists**: Ilustrações e CTAs
- **No Search Results**: Sugestões de busca
- **First Use**: Onboarding e tutoriais

---

## 🧪 **Testes (Planejado)**

### **Estratégia de Testes**

- **Unit Tests**: Jest + React Testing Library
- **Integration Tests**: Cypress ou Playwright
- **E2E Tests**: Fluxos críticos de usuário
- **Visual Regression**: Chromatic ou similar

### **Coverage Targets**

- **Components**: 90%+ cobertura
- **Hooks**: 95%+ cobertura
- **Services**: 85%+ cobertura
- **Critical Paths**: 100% E2E coverage

---

## 🚀 **Performance**

### **Otimizações Implementadas**

- **Code Splitting**: React.lazy para páginas
- **React Query**: Cache inteligente de API
- **Memoization**: React.memo para componentes pesados
- **Image Optimization**: Lazy loading e formatos modernos

### **Métricas Target**

- **FCP (First Contentful Paint)**: < 1.5s
- **LCP (Largest Contentful Paint)**: < 2.5s
- **CLS (Cumulative Layout Shift)**: < 0.1
- **FID (First Input Delay)**: < 100ms

---

## 🔧 **Desenvolvimento**

### **Scripts Disponíveis**

```bash
npm run dev        # Servidor de desenvolvimento
npm run build      # Build de produção
npm run preview    # Preview do build
npm run lint       # ESLint + TypeScript check
npm run test       # Executar testes
```

### **Environment Variables**

```bash
VITE_API_BASE_URL=http://localhost:5000/api
VITE_APP_NAME=Smart Alarm
VITE_APP_VERSION=1.0.0
```

---

## 📈 **Próximos Passos - Roadmap**

### **Phase 3 Remaining (Próximas 2-3 semanas)**

1. **Edit Functionality**: Conectar botões de edição aos formulários
2. **Search & Filter**: Sistema de busca e filtros avançados
3. **Pagination**: Para listas grandes de itens
4. **Bulk Operations**: Seleção múltipla e ações em lote
5. **Toast Notifications**: Sistema de feedback visual
6. **Mobile Optimization**: Melhorias específicas para mobile

### **Phase 4: Enhancement (4-6 semanas)**

1. **Dark Mode**: Tema escuro completo
2. **Accessibility**: Compliance WCAG AAA
3. **PWA Features**: Service Workers, offline mode
4. **Advanced Analytics**: Dashboard de estatísticas
5. **Integration Tests**: Cobertura E2E completa
6. **Performance**: Otimizações avançadas

---

## 🐛 **Issues Conhecidos**

### **Limitações Atuais**

- Edit functionality não conectada (em desenvolvimento)
- Sem sistema de busca ainda
- Paginação não implementada
- Bulk operations pendentes

### **Technical Debt**

- Alguns componentes precisam de memoization
- Types podem ser mais específicos
- Error boundaries precisam ser expandidos
- Loading states podem ser mais granulares

---

## 📞 **Suporte Técnico**

### **Logs de Debug**

- **Console Browser**: Logs detalhados em desenvolvimento
- **React Query DevTools**: Estado de queries e mutations
- **Network Tab**: Monitoramento de chamadas API

### **Troubleshooting**

1. **Limpar cache**: `npm run dev -- --force`
2. **Reinstalar dependencies**: `rm -rf node_modules && npm install`
3. **Verificar Network**: Checar conectividade com API
4. **Browser DevTools**: Console para erros JavaScript

---

**📅 Última Atualização**: 30/07/2025  
**👨‍💻 Maintainer**: Equipe Smart Alarm  
**📖 Versão da Documentação**: 2.0  
**🔄 Status**: MVP Phase 3 - 40% Completo
