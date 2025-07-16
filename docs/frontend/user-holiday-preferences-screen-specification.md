# 📝 Especificação da Tela de Gerenciamento de Preferências de Feriados do Usuário

## 🎯 Visão Geral

Esta especificação detalha como desenvolver a tela de gerenciamento de preferências de feriados no frontend React/TypeScript. A entidade UserHolidayPreference está **100% funcional** no backend com API completa, validações e testes implementados.

As preferências de feriados permitem que usuários configurem como seus alarmes devem se comportar durante feriados específicos (desabilitar, atrasar ou pular).

## 📋 Índice

1. [Estrutura da Entidade UserHolidayPreference](#estrutura-da-entidade-userholidaypreference)
2. [Endpoints da API](#endpoints-da-api)
3. [Layout da Tela](#layout-da-tela)
4. [Componentes Necessários](#componentes-necessários)
5. [Estados e Comportamentos](#estados-e-comportamentos)
6. [Validações Frontend](#validações-frontend)
7. [Fluxos de Interação](#fluxos-de-interação)
8. [Tratamento de Erros](#tratamento-de-erros)
9. [Casos de Teste](#casos-de-teste)

## 📊 Estrutura da Entidade UserHolidayPreference

### Propriedades da Entidade

```typescript
interface UserHolidayPreference {
  id: string;                    // GUID único da preferência
  userId: string;                // GUID do usuário
  holidayId: string;             // GUID do feriado
  isEnabled: boolean;            // Se a preferência está ativa
  action: HolidayPreferenceAction; // Ação a ser executada
  delayInMinutes?: number;       // Atraso em minutos (apenas para action = Delay)
  createdAt: string;             // Data de criação (ISO 8601)
  updatedAt?: string;            // Data da última atualização (ISO 8601)
  user?: UserResponseDto;        // Dados do usuário (quando incluído)
  holiday?: HolidayResponseDto;  // Dados do feriado (quando incluído)
  actionDisplayName: string;     // Nome amigável da ação (readonly)
}

enum HolidayPreferenceAction {
  Disable = 1,  // Desabilita alarmes completamente
  Delay = 2,    // Atrasa alarmes por tempo específico
  Skip = 3      // Pula alarmes (não dispara, mas mantém programação)
}
```

### Regras de Negócio Importantes

1. **Ação Disable**: Desabilita completamente os alarmes durante o feriado
2. **Ação Delay**: Atrasa os alarmes por um tempo específico (1-1440 minutos)
3. **Ação Skip**: Pula os alarmes no feriado, mas mantém programação normal no dia seguinte
4. **DelayInMinutes**: Obrigatório apenas quando action = Delay, entre 1 e 1440 minutos (24h)
5. **Unicidade**: Cada usuário pode ter apenas uma preferência por feriado
6. **IsEnabled**: Permite ativar/desativar a preferência sem excluí-la

## 🔌 Endpoints da API

### Base URL

```text
https://localhost:5001/api/v1/user-holiday-preferences
```

### Autenticação

- **Tipo**: Bearer Token (JWT)
- **Header**: `Authorization: Bearer {token}`
- **Todas as operações requerem autenticação**

### Lista de Endpoints

| Método | Endpoint | Descrição | Parâmetros |
|--------|----------|-----------|------------|
| POST | `/` | Criar nova preferência | Body: CreateUserHolidayPreferenceDto |
| GET | `/{id}` | Buscar preferência por ID | id: GUID |
| GET | `/user/{userId}` | Listar preferências do usuário | userId: GUID |
| GET | `/user/{userId}/applicable?date={date}` | Preferências aplicáveis em data específica | userId: GUID, date: DateTime |
| PUT | `/{id}` | Atualizar preferência | id: GUID, Body: UpdateUserHolidayPreferenceDto |
| DELETE | `/{id}` | Deletar preferência | id: GUID |

### DTOs para Requisições

```typescript
// Para criação
interface CreateUserHolidayPreferenceDto {
  userId: string;                    // GUID obrigatório
  holidayId: string;                 // GUID obrigatório  
  isEnabled: boolean;                // Default: true
  action: HolidayPreferenceAction;   // Obrigatório
  delayInMinutes?: number;           // 1-1440, obrigatório se action = Delay
}

// Para atualização
interface UpdateUserHolidayPreferenceDto {
  isEnabled: boolean;                // Obrigatório
  action: HolidayPreferenceAction;   // Obrigatório
  delayInMinutes?: number;           // 1-1440, obrigatório se action = Delay
}

// Resposta da API
interface UserHolidayPreferenceResponseDto {
  id: string;
  userId: string;
  holidayId: string;
  isEnabled: boolean;
  action: HolidayPreferenceAction;
  delayInMinutes?: number;
  createdAt: string;
  updatedAt?: string;
  user?: UserResponseDto;
  holiday?: HolidayResponseDto;
  actionDisplayName: string;
}
```

## 🎨 Layout da Tela

### Estrutura Visual

```text
┌─────────────────────────────────────────────────────────────┐
│ ⚙️ Preferências de Feriados                     [+ Nova]    │
├─────────────────────────────────────────────────────────────┤
│ 🔍 [Campo de Busca]                     [Filtros ▼]        │
├─────────────────────────────────────────────────────────────┤
│ 📊 Resumo:                                                 │
│ • Total: 8 preferências                                    │
│ • Ativas: 6 | Inativas: 2                                 │
│ • Desabilitar: 3 | Atrasar: 2 | Pular: 3                 │
├─────────────────────────────────────────────────────────────┤
│ 📋 Lista de Preferências                                   │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ 🎄 Natal                             [✅] [✏️] [🗑️]    │ │
│ │ 📅 25/12 • Recorrente                                  │ │
│ │ ⚙️ Atrasar 60 minutos                                   │ │
│ │ 📝 Criado: 14/07/2025 • Atualizado: 15/07/2025        │ │
│ └─────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ 🇧🇷 Independência do Brasil         [❌] [✏️] [🗑️]    │ │
│ │ 📅 07/09/2024 • Específico                             │ │
│ │ ⚙️ Desabilitar alarmes                                  │ │
│ │ 📝 Criado: 10/07/2025                                  │ │
│ └─────────────────────────────────────────────────────────┘ │
│ [Carregar mais...]                                         │
└─────────────────────────────────────────────────────────────┘
```

### Modal de Criação/Edição

```text
┌─────────────────────────────────────────────────────────────┐
│ ➕ Nova Preferência de Feriado                    [✕]      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ 📅 Feriado *                                               │
│ [Selecionar feriado...]                               [▼]  │
│                                                             │
│ ⚙️ Ação *                                                   │
│ ○ Desabilitar alarmes                                      │
│ ○ Atrasar alarmes                                          │
│ ○ Pular alarmes                                            │
│                                                             │
│ ⏰ Atraso (apenas para "Atrasar")                          │
│ [___] minutos (1-1440)                                     │
│                                                             │
│ ✅ [ ] Preferência ativa                                   │
│                                                             │
│ ℹ️ Explicação da Ação Selecionada:                         │
│ "Os alarmes serão completamente desabilitados              │
│  durante este feriado."                                    │
│                                                             │
│                           [Cancelar] [Salvar]              │
└─────────────────────────────────────────────────────────────┘
```

### Responsividade

#### Desktop (≥ 1024px)

- Layout completo com sidebar de filtros
- Cards com informações detalhadas
- Modal para criação/edição

#### Tablet (768px - 1023px)

- Layout em coluna única
- Cards compactos
- Modal redimensionado

#### Mobile (< 768px)

- Cards simplificados empilhados
- Ações em menu contextual (⋮)
- Formulário em tela completa

## 🧩 Componentes Necessários

### 1. UserHolidayPreferencesPage (Página Principal)

```typescript
interface UserHolidayPreferencesPageProps {
  userId: string; // ID do usuário atual
}

const UserHolidayPreferencesPage: React.FC<UserHolidayPreferencesPageProps> = ({ userId }) => {
  // Estados principais
  const [preferences, setPreferences] = useState<UserHolidayPreference[]>([]);
  const [holidays, setHolidays] = useState<Holiday[]>([]); // Para seleção
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Estados de filtro
  const [searchTerm, setSearchTerm] = useState('');
  const [actionFilter, setActionFilter] = useState<HolidayPreferenceAction | 'all'>('all');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');
  
  // Estados de modal
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingPreference, setEditingPreference] = useState<UserHolidayPreference | null>(null);
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);
  
  // Outros estados...
};
```

### 2. UserHolidayPreferenceCard (Item da Lista)

```typescript
interface UserHolidayPreferenceCardProps {
  preference: UserHolidayPreference;
  onEdit: (preference: UserHolidayPreference) => void;
  onDelete: (id: string) => void;
  onToggleStatus: (id: string, enabled: boolean) => Promise<void>;
  isLoading?: boolean;
}

const UserHolidayPreferenceCard: React.FC<UserHolidayPreferenceCardProps> = ({
  preference,
  onEdit,
  onDelete,
  onToggleStatus,
  isLoading = false
}) => {
  const getActionIcon = (action: HolidayPreferenceAction) => {
    switch (action) {
      case HolidayPreferenceAction.Disable: return '🚫';
      case HolidayPreferenceAction.Delay: return '⏰';
      case HolidayPreferenceAction.Skip: return '⏭️';
      default: return '❓';
    }
  };

  const getStatusIcon = (enabled: boolean) => enabled ? '✅' : '❌';

  return (
    <div className={`preference-card ${!preference.isEnabled ? 'disabled' : ''}`}>
      {/* Conteúdo do card */}
    </div>
  );
};
```

### 3. UserHolidayPreferenceForm (Formulário)

```typescript
interface UserHolidayPreferenceFormProps {
  mode: 'create' | 'edit';
  userId: string;
  initialData?: Partial<UserHolidayPreference>;
  holidays: Holiday[];
  onSubmit: (data: CreateUserHolidayPreferenceDto | UpdateUserHolidayPreferenceDto) => Promise<void>;
  onCancel: () => void;
  isLoading?: boolean;
}

const UserHolidayPreferenceForm: React.FC<UserHolidayPreferenceFormProps> = ({
  mode,
  userId,
  initialData,
  holidays,
  onSubmit,
  onCancel,
  isLoading = false
}) => {
  // Estados do formulário
  const [formData, setFormData] = useState({
    holidayId: initialData?.holidayId || '',
    action: initialData?.action || HolidayPreferenceAction.Disable,
    delayInMinutes: initialData?.delayInMinutes || undefined,
    isEnabled: initialData?.isEnabled ?? true
  });
  
  const [errors, setErrors] = useState<Record<string, string>>({});
  
  // Lógica do formulário...
};
```

### 4. UserHolidayPreferenceStats (Estatísticas)

```typescript
interface UserHolidayPreferenceStatsProps {
  preferences: UserHolidayPreference[];
}

const UserHolidayPreferenceStats: React.FC<UserHolidayPreferenceStatsProps> = ({ preferences }) => {
  const total = preferences.length;
  const active = preferences.filter(p => p.isEnabled).length;
  const inactive = total - active;
  
  const byAction = {
    disable: preferences.filter(p => p.action === HolidayPreferenceAction.Disable).length,
    delay: preferences.filter(p => p.action === HolidayPreferenceAction.Delay).length,
    skip: preferences.filter(p => p.action === HolidayPreferenceAction.Skip).length
  };
  
  return (
    <div className="preference-stats">
      <div className="stat-group">
        <div className="stat-item">
          <span className="stat-value">{total}</span>
          <span className="stat-label">Total</span>
        </div>
        <div className="stat-item">
          <span className="stat-value">{active}</span>
          <span className="stat-label">Ativas</span>
        </div>
        <div className="stat-item">
          <span className="stat-value">{inactive}</span>
          <span className="stat-label">Inativas</span>
        </div>
      </div>
      
      <div className="stat-group">
        <div className="stat-item">
          <span className="stat-icon">🚫</span>
          <span className="stat-value">{byAction.disable}</span>
          <span className="stat-label">Desabilitar</span>
        </div>
        <div className="stat-item">
          <span className="stat-icon">⏰</span>
          <span className="stat-value">{byAction.delay}</span>
          <span className="stat-label">Atrasar</span>
        </div>
        <div className="stat-item">
          <span className="stat-icon">⏭️</span>
          <span className="stat-value">{byAction.skip}</span>
          <span className="stat-label">Pular</span>
        </div>
      </div>
    </div>
  );
};
```

### 5. UserHolidayPreferenceFilters (Filtros)

```typescript
interface UserHolidayPreferenceFiltersProps {
  searchTerm: string;
  onSearchChange: (term: string) => void;
  actionFilter: HolidayPreferenceAction | 'all';
  onActionFilterChange: (filter: HolidayPreferenceAction | 'all') => void;
  statusFilter: 'all' | 'active' | 'inactive';
  onStatusFilterChange: (filter: 'all' | 'active' | 'inactive') => void;
}

const UserHolidayPreferenceFilters: React.FC<UserHolidayPreferenceFiltersProps> = ({
  searchTerm,
  onSearchChange,
  actionFilter,
  onActionFilterChange,
  statusFilter,
  onStatusFilterChange
}) => {
  return (
    <div className="preference-filters">
      <div className="search-box">
        <input
          type="text"
          placeholder="Buscar por feriado..."
          value={searchTerm}
          onChange={(e) => onSearchChange(e.target.value)}
        />
      </div>
      
      <div className="filter-group">
        <select 
          value={actionFilter} 
          onChange={(e) => onActionFilterChange(e.target.value as any)}
        >
          <option value="all">Todas as ações</option>
          <option value={HolidayPreferenceAction.Disable}>Desabilitar</option>
          <option value={HolidayPreferenceAction.Delay}>Atrasar</option>
          <option value={HolidayPreferenceAction.Skip}>Pular</option>
        </select>
        
        <select 
          value={statusFilter} 
          onChange={(e) => onStatusFilterChange(e.target.value as any)}
        >
          <option value="all">Todos os status</option>
          <option value="active">Ativas</option>
          <option value="inactive">Inativas</option>
        </select>
      </div>
    </div>
  );
};
```

## 🔄 Estados e Comportamentos

### Estados Principais

```typescript
// Estado da lista de preferências
const [preferences, setPreferences] = useState<UserHolidayPreference[]>([]);
const [holidays, setHolidays] = useState<Holiday[]>([]);

// Estados de loading
const [isLoadingList, setIsLoadingList] = useState(false);
const [isCreating, setIsCreating] = useState(false);
const [isUpdating, setIsUpdating] = useState(false);
const [isDeleting, setIsDeleting] = useState<string | null>(null);
const [isToggling, setIsToggling] = useState<string | null>(null);

// Estados de UI
const [showCreateModal, setShowCreateModal] = useState(false);
const [editingPreference, setEditingPreference] = useState<UserHolidayPreference | null>(null);
const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);

// Estados de filtro e busca
const [searchTerm, setSearchTerm] = useState('');
const [actionFilter, setActionFilter] = useState<HolidayPreferenceAction | 'all'>('all');
const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');
const [sortBy, setSortBy] = useState<'holiday' | 'action' | 'created' | 'updated'>('holiday');
const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc');

// Estados de erro
const [error, setError] = useState<string | null>(null);
const [formErrors, setFormErrors] = useState<Record<string, string>>({});
```

### Comportamentos de Loading

1. **Carregamento Inicial**: Skeleton cards para preferências e feriados
2. **Criação**: Modal com botões desabilitados e spinner
3. **Edição**: Formulário com overlay de loading
4. **Exclusão**: Card com overlay de loading
5. **Toggle Status**: Apenas o switch com loading
6. **Busca**: Debounce de 300ms antes de filtrar

### Comportamentos de Erro

1. **Erro de Rede**: Toast com botão "Tentar Novamente"
2. **Erro de Validação**: Destacar campos com erro no formulário
3. **Erro de Autorização**: Redirect para login
4. **Erro 404**: Remover item da lista local
5. **Erro 409**: Mensagem específica de conflito (preferência já existe)
6. **Erro 500**: Toast com informações de suporte

## ✅ Validações Frontend

### Campo HolidayId

```typescript
const validateHolidayId = (holidayId: string): string | null => {
  if (!holidayId.trim()) {
    return 'Feriado é obrigatório';
  }
  
  // Verificar se é um GUID válido
  const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
  if (!guidRegex.test(holidayId)) {
    return 'ID do feriado inválido';
  }
  
  return null;
};
```

### Campo Action

```typescript
const validateAction = (action: HolidayPreferenceAction): string | null => {
  const validActions = [
    HolidayPreferenceAction.Disable,
    HolidayPreferenceAction.Delay,
    HolidayPreferenceAction.Skip
  ];
  
  if (!validActions.includes(action)) {
    return 'Ação inválida';
  }
  
  return null;
};
```

### Campo DelayInMinutes

```typescript
const validateDelayInMinutes = (
  action: HolidayPreferenceAction, 
  delayInMinutes?: number
): string | null => {
  if (action === HolidayPreferenceAction.Delay) {
    if (delayInMinutes === undefined || delayInMinutes === null) {
      return 'Atraso em minutos é obrigatório quando a ação é "Atrasar"';
    }
    
    if (delayInMinutes < 1) {
      return 'Atraso deve ser pelo menos 1 minuto';
    }
    
    if (delayInMinutes > 1440) {
      return 'Atraso não pode ser maior que 1440 minutos (24 horas)';
    }
  } else {
    if (delayInMinutes !== undefined && delayInMinutes !== null && delayInMinutes !== 0) {
      return 'Atraso só deve ser especificado quando a ação é "Atrasar"';
    }
  }
  
  return null;
};
```

### Validação Completa do Formulário

```typescript
const validateForm = (data: UserHolidayPreferenceFormData): Record<string, string> => {
  const errors: Record<string, string> = {};
  
  const holidayError = validateHolidayId(data.holidayId);
  if (holidayError) errors.holidayId = holidayError;
  
  const actionError = validateAction(data.action);
  if (actionError) errors.action = actionError;
  
  const delayError = validateDelayInMinutes(data.action, data.delayInMinutes);
  if (delayError) errors.delayInMinutes = delayError;
  
  return errors;
};
```

### Validação de Duplicata

```typescript
const validateDuplicate = (
  preferences: UserHolidayPreference[], 
  holidayId: string, 
  excludeId?: string
): string | null => {
  const duplicate = preferences.find(p => 
    p.holidayId === holidayId && p.id !== excludeId
  );
  
  if (duplicate) {
    return 'Já existe uma preferência configurada para este feriado';
  }
  
  return null;
};
```

## 🎭 Fluxos de Interação

### 1. Fluxo de Criação

```
1. Usuário clica em [+ Nova Preferência]
2. Modal é aberto com formulário limpo
3. Usuário seleciona feriado no dropdown
4. Usuário escolhe ação (radio buttons):
   - Desabilitar: Nenhum campo adicional
   - Atrasar: Campo de minutos aparece
   - Pular: Nenhum campo adicional
5. Se "Atrasar": Usuário preenche minutos (1-1440)
6. Usuário marca/desmarca "Preferência ativa"
7. Sistema valida em tempo real
8. Usuário clica em "Salvar"
9. Validação final completa
10. Se válido: POST para API
11. Se sucesso: Fecha modal, atualiza lista, mostra toast
12. Se erro: Mostra erro no formulário/toast
```

### 2. Fluxo de Edição

```
1. Usuário clica em [✏️] no card da preferência
2. Modal é aberto com dados preenchidos
3. Usuário altera dados conforme necessário
4. Sistema valida em tempo real
5. Usuário clica em "Salvar"
6. Validação final
7. Se válido: PUT para API
8. Se sucesso: Fecha modal, atualiza item na lista, mostra toast
9. Se erro: Mostra erro no formulário/toast
```

### 3. Fluxo de Exclusão

```
1. Usuário clica em [🗑️] no card da preferência
2. Modal de confirmação é exibido:
   "Tem certeza que deseja excluir a preferência para '{nome do feriado}'?"
   "Esta ação não pode ser desfeita."
   [Cancelar] [Excluir]
3. Se cancelar: Fecha modal
4. Se confirmar: DELETE para API
5. Se sucesso: Remove da lista, mostra toast
6. Se erro: Mostra toast de erro
```

### 4. Fluxo de Toggle Status

```
1. Usuário clica no switch de ativação
2. Sistema mostra loading no switch
3. PUT para API com dados atualizados (apenas isEnabled)
4. Se sucesso: Atualiza status visual, mostra feedback sutil
5. Se erro: Reverte switch, mostra toast de erro
```

### 5. Fluxo de Busca e Filtros

```
1. Busca por nome de feriado:
   - Usuário digita no campo
   - Aguarda 300ms (debounce)
   - Filtra lista local por nome do feriado
   
2. Filtro por ação:
   - Usuário seleciona no dropdown
   - Filtra imediatamente por tipo de ação
   
3. Filtro por status:
   - Usuário seleciona no dropdown
   - Filtra imediatamente por ativo/inativo
   
4. Combinação de filtros aplicados simultaneamente
5. Se nenhum resultado: Mostra "Nenhuma preferência encontrada"
```

## 🚫 Tratamento de Erros

### Códigos de Status HTTP

```typescript
const handleApiError = (error: any, operation: string) => {
  switch (error.status) {
    case 400:
      return 'Dados inválidos. Verifique os campos preenchidos.';
    case 401:
      // Redirect para login
      redirectToLogin();
      return 'Sessão expirada. Redirecionando...';
    case 403:
      return 'Você não tem permissão para esta operação.';
    case 404:
      return 'Preferência não encontrada.';
    case 409:
      return 'Já existe uma preferência configurada para este feriado.';
    case 500:
      return 'Erro interno do servidor. Tente novamente.';
    default:
      return `Erro ao ${operation}. Tente novamente.`;
  }
};
```

### Tratamento de Erro de Rede

```typescript
const handleNetworkError = (error: any) => {
  if (!navigator.onLine) {
    return 'Sem conexão com a internet. Verifique sua conexão.';
  }
  
  return 'Erro de conexão. Verifique sua internet e tente novamente.';
};
```

### Estados de Erro na UI

1. **Toast para erros temporários**: Rede, servidor, operações
2. **Inline errors para validação**: Campos de formulário
3. **Empty state para lista vazia**: "Nenhuma preferência configurada"
4. **Error boundary para erros críticos**: Tela de erro geral
5. **Feedback visual**: Cards com estado de erro

## 🎯 Casos de Teste

### Cenários de Teste Obrigatórios

#### 1. Teste de Criação

```typescript
describe('Criação de Preferência de Feriado', () => {
  test('Deve criar preferência para desabilitar alarmes', async () => {
    // 1. Abrir modal de criação
    // 2. Selecionar feriado "Natal"
    // 3. Escolher ação "Desabilitar"
    // 4. Marcar como ativa
    // 5. Submeter formulário
    // 6. Verificar se aparece na lista
    // 7. Verificar toast de sucesso
  });

  test('Deve criar preferência para atrasar alarmes', async () => {
    // 1. Abrir modal
    // 2. Selecionar feriado
    // 3. Escolher ação "Atrasar"
    // 4. Preencher 60 minutos
    // 5. Submeter
    // 6. Verificar dados na lista
  });

  test('Deve mostrar erro para feriado não selecionado', async () => {
    // 1. Abrir modal
    // 2. Não selecionar feriado
    // 3. Tentar submeter
    // 4. Verificar mensagem de erro
  });

  test('Deve mostrar erro para ação "Atrasar" sem minutos', async () => {
    // 1. Selecionar ação "Atrasar"
    // 2. Não preencher minutos
    // 3. Verificar erro em tempo real
  });

  test('Deve mostrar erro para preferência duplicada', async () => {
    // 1. Tentar criar preferência para feriado já configurado
    // 2. Verificar erro de conflito
  });
});
```

#### 2. Teste de Edição

```typescript
describe('Edição de Preferência', () => {
  test('Deve editar ação de desabilitar para atrasar', async () => {
    // 1. Clicar em editar preferência existente
    // 2. Alterar de "Desabilitar" para "Atrasar"
    // 3. Preencher 30 minutos
    // 4. Salvar
    // 5. Verificar alteração na lista
  });

  test('Deve cancelar edição sem salvar', async () => {
    // 1. Iniciar edição
    // 2. Fazer alterações
    // 3. Cancelar
    // 4. Verificar que dados não foram alterados
  });

  test('Deve limpar campo de minutos ao mudar de "Atrasar" para "Pular"', async () => {
    // 1. Editar preferência com ação "Atrasar"
    // 2. Mudar para "Pular"
    // 3. Verificar que campo de minutos some
  });
});
```

#### 3. Teste de Exclusão

```typescript
describe('Exclusão de Preferência', () => {
  test('Deve excluir preferência após confirmação', async () => {
    // 1. Clicar em excluir
    // 2. Confirmar no modal
    // 3. Verificar remoção da lista
    // 4. Verificar toast de sucesso
  });

  test('Deve cancelar exclusão', async () => {
    // 1. Clicar em excluir
    // 2. Cancelar no modal
    // 3. Verificar que item permanece
  });
});
```

#### 4. Teste de Toggle Status

```typescript
describe('Ativação/Desativação', () => {
  test('Deve ativar preferência inativa', async () => {
    // 1. Clicar no switch de preferência inativa
    // 2. Verificar mudança visual
    // 3. Verificar persistência
  });

  test('Deve desativar preferência ativa', async () => {
    // 1. Clicar no switch de preferência ativa
    // 2. Verificar mudança visual
    // 3. Verificar persistência
  });

  test('Deve mostrar feedback de erro ao falhar toggle', async () => {
    // 1. Simular erro na API
    // 2. Tentar fazer toggle
    // 3. Verificar que switch volta ao estado anterior
    // 4. Verificar toast de erro
  });
});
```

#### 5. Teste de Filtros

```typescript
describe('Busca e Filtros', () => {
  test('Deve filtrar por nome de feriado', async () => {
    // 1. Criar múltiplas preferências
    // 2. Buscar por "Natal"
    // 3. Verificar apenas resultados relevantes
  });

  test('Deve filtrar por ação', async () => {
    // 1. Criar preferências com ações diferentes
    // 2. Filtrar por "Atrasar"
    // 3. Verificar apenas preferências de atraso
  });

  test('Deve filtrar por status', async () => {
    // 1. Criar preferências ativas e inativas
    // 2. Filtrar por "Ativas"
    // 3. Verificar apenas preferências ativas
  });

  test('Deve combinar múltiplos filtros', async () => {
    // 1. Aplicar filtro de ação + status
    // 2. Verificar intersecção dos resultados
  });
});
```

## 📱 Comportamentos Específicos

### Formatação de Dados

```typescript
// Para exibição da ação
const formatActionDisplay = (action: HolidayPreferenceAction, delayInMinutes?: number): string => {
  switch (action) {
    case HolidayPreferenceAction.Disable:
      return 'Desabilitar alarmes';
    case HolidayPreferenceAction.Delay:
      return `Atrasar ${delayInMinutes || 0} minutos`;
    case HolidayPreferenceAction.Skip:
      return 'Pular alarmes';
    default:
      return 'Ação desconhecida';
  }
};

// Para formatação de tempo
const formatDelayTime = (minutes: number): string => {
  if (minutes < 60) {
    return `${minutes} minutos`;
  }
  
  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;
  
  if (remainingMinutes === 0) {
    return `${hours} ${hours === 1 ? 'hora' : 'horas'}`;
  }
  
  return `${hours}h ${remainingMinutes}min`;
};
```

### Indicadores Visuais

1. **Ação Desabilitar**:
   - Ícone: 🚫
   - Cor: Vermelho (#ef4444)
   - Descrição: "Alarmes desabilitados"

2. **Ação Atrasar**:
   - Ícone: ⏰
   - Cor: Laranja (#f59e0b)
   - Descrição: "Atrasar X minutos"

3. **Ação Pular**:
   - Ícone: ⏭️
   - Cor: Azul (#3b82f6)
   - Descrição: "Pular alarmes"

4. **Status Ativo**:
   - Ícone: ✅
   - Opacidade normal
   - Switch ativo

5. **Status Inativo**:
   - Ícone: ❌
   - Opacidade reduzida (60%)
   - Switch inativo

### Ordenação Padrão

1. **Primário**: Status (ativas primeiro)
2. **Secundário**: Nome do feriado (alfabética)
3. **Terciário**: Data de criação (mais recente primeiro)

### Responsividade Detalhada

#### Mobile Específico (< 768px)

```css
.preference-card {
  padding: 12px;
  margin-bottom: 8px;
}

.preference-actions {
  display: flex;
  justify-content: space-between;
  margin-top: 12px;
}

.action-description {
  font-size: 0.875rem;
  color: #6b7280;
}
```

#### Tablet Específico (768px - 1023px)

```css
.preferences-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 16px;
}

.preference-card {
  padding: 16px;
}
```

#### Desktop Específico (≥ 1024px)

```css
.preferences-layout {
  display: grid;
  grid-template-columns: 300px 1fr;
  gap: 24px;
}

.preferences-sidebar {
  /* Filtros fixos na lateral */
}

.preferences-main {
  /* Lista principal */
}
```

## 🎨 Guia de Estilo

### Cores Principais

```css
/* Ações */
--action-disable: #ef4444;    /* Vermelho para desabilitar */
--action-delay: #f59e0b;      /* Laranja para atrasar */
--action-skip: #3b82f6;       /* Azul para pular */

/* Status */
--status-active: #10b981;     /* Verde para ativo */
--status-inactive: #6b7280;   /* Cinza para inativo */

/* Estados */
--preference-bg: #ffffff;
--preference-border: #e5e7eb;
--preference-hover: #f9fafb;
--preference-disabled: #f3f4f6;
```

### Espaçamentos

```css
/* Grid de 8px */
--space-xs: 4px;
--space-sm: 8px;
--space-md: 16px;
--space-lg: 24px;
--space-xl: 32px;
```

### Tipografia

```css
.preference-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: #111827;
}

.preference-action {
  font-size: 0.875rem;
  font-weight: 500;
}

.preference-meta {
  font-size: 0.75rem;
  color: #6b7280;
}
```

## 🔧 Implementação Técnica

### Service para API

```typescript
class UserHolidayPreferenceService {
  private baseUrl = '/api/v1/user-holiday-preferences';

  async getByUserId(userId: string): Promise<UserHolidayPreference[]> {
    const response = await fetch(`${this.baseUrl}/user/${userId}`, {
      headers: this.getAuthHeaders()
    });
    
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return response.json();
  }

  async getById(id: string): Promise<UserHolidayPreference> {
    const response = await fetch(`${this.baseUrl}/${id}`, {
      headers: this.getAuthHeaders()
    });
    
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return response.json();
  }

  async create(data: CreateUserHolidayPreferenceDto): Promise<UserHolidayPreference> {
    const response = await fetch(this.baseUrl, {
      method: 'POST',
      headers: {
        ...this.getAuthHeaders(),
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(data)
    });
    
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return response.json();
  }

  async update(id: string, data: UpdateUserHolidayPreferenceDto): Promise<UserHolidayPreference> {
    const response = await fetch(`${this.baseUrl}/${id}`, {
      method: 'PUT',
      headers: {
        ...this.getAuthHeaders(),
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(data)
    });
    
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return response.json();
  }

  async delete(id: string): Promise<void> {
    const response = await fetch(`${this.baseUrl}/${id}`, {
      method: 'DELETE',
      headers: this.getAuthHeaders()
    });
    
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
  }

  async getApplicableForDate(userId: string, date: Date): Promise<UserHolidayPreference[]> {
    const dateStr = date.toISOString().split('T')[0];
    const response = await fetch(`${this.baseUrl}/user/${userId}/applicable?date=${dateStr}`, {
      headers: this.getAuthHeaders()
    });
    
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return response.json();
  }

  private getAuthHeaders() {
    const token = localStorage.getItem('authToken');
    return token ? { Authorization: `Bearer ${token}` } : {};
  }
}
```

### Hook Customizado

```typescript
const useUserHolidayPreferences = (userId: string) => {
  const [preferences, setPreferences] = useState<UserHolidayPreference[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const service = useMemo(() => new UserHolidayPreferenceService(), []);

  const loadPreferences = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await service.getByUserId(userId);
      setPreferences(data);
    } catch (err) {
      setError('Erro ao carregar preferências');
      console.error('Error loading user holiday preferences:', err);
    } finally {
      setLoading(false);
    }
  }, [service, userId]);

  const createPreference = useCallback(async (data: CreateUserHolidayPreferenceDto) => {
    try {
      const newPreference = await service.create(data);
      setPreferences(prev => [...prev, newPreference]);
      return newPreference;
    } catch (err) {
      throw new Error('Erro ao criar preferência');
    }
  }, [service]);

  const updatePreference = useCallback(async (id: string, data: UpdateUserHolidayPreferenceDto) => {
    try {
      const updatedPreference = await service.update(id, data);
      setPreferences(prev => prev.map(p => p.id === id ? updatedPreference : p));
      return updatedPreference;
    } catch (err) {
      throw new Error('Erro ao atualizar preferência');
    }
  }, [service]);

  const deletePreference = useCallback(async (id: string) => {
    try {
      await service.delete(id);
      setPreferences(prev => prev.filter(p => p.id !== id));
    } catch (err) {
      throw new Error('Erro ao excluir preferência');
    }
  }, [service]);

  const togglePreferenceStatus = useCallback(async (id: string, enabled: boolean) => {
    const preference = preferences.find(p => p.id === id);
    if (!preference) return;

    const updateData: UpdateUserHolidayPreferenceDto = {
      isEnabled: enabled,
      action: preference.action,
      delayInMinutes: preference.delayInMinutes
    };

    return updatePreference(id, updateData);
  }, [preferences, updatePreference]);

  useEffect(() => {
    if (userId) {
      loadPreferences();
    }
  }, [loadPreferences, userId]);

  return {
    preferences,
    loading,
    error,
    createPreference,
    updatePreference,
    deletePreference,
    togglePreferenceStatus,
    refetch: loadPreferences
  };
};
```

## 📋 Checklist de Implementação

### Fase 1: Estrutura Base

- [ ] Criar página `UserHolidayPreferencesPage`
- [ ] Implementar `UserHolidayPreferenceService`
- [ ] Criar hook `useUserHolidayPreferences`
- [ ] Implementar loading states
- [ ] Configurar roteamento

### Fase 2: Listagem

- [ ] Componente `UserHolidayPreferenceCard`
- [ ] Componente `UserHolidayPreferenceStats`
- [ ] Implementar estados vazios (empty states)
- [ ] Responsividade mobile
- [ ] Indicadores visuais por ação

### Fase 3: CRUD

- [ ] Componente `UserHolidayPreferenceForm`
- [ ] Modal de criação/edição
- [ ] Validações frontend completas
- [ ] Tratamento de erros específicos
- [ ] Feedback visual (toasts)

### Fase 4: Funcionalidades Avançadas

- [ ] Toggle de status (ativar/desativar)
- [ ] Componente `UserHolidayPreferenceFilters`
- [ ] Implementar debounce na busca
- [ ] Filtros por ação e status
- [ ] Ordenação configurável

### Fase 5: Testes

- [ ] Testes unitários dos componentes
- [ ] Testes de integração da página
- [ ] Testes e2e dos fluxos principais
- [ ] Testes de acessibilidade
- [ ] Testes de responsividade

### Fase 6: Polimento

- [ ] Animações e transições suaves
- [ ] Otimizações de performance
- [ ] Internacionalização (i18n)
- [ ] Documentação dos componentes
- [ ] Review de acessibilidade (WCAG)

---

## 📞 Suporte

**Para dúvidas técnicas:**

- Consulte a documentação da API no código do controller
- Testes HTTP disponíveis para referência
- Exemplos de uso nos testes de integração

**Entidade UserHolidayPreference está 100% funcional e testada!** 🎉

**Status da API:**

- ✅ CRUD completo implementado
- ✅ Validações robustas
- ✅ Testes unitários e de integração
- ✅ Documentação Swagger
- ✅ Tratamento de erros

---

*Documento gerado em: 15/07/2025*
*Versão da API: v1*
*Backend Status: ✅ Funcional*
