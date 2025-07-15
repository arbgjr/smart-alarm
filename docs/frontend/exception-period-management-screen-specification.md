# 📝 Especificação da Tela de Gerenciamento de Períodos de Exceção

## 🎯 Visão Geral

Esta especificação detalha como desenvolver a tela de gerenciamento de períodos de exceção no frontend React/TypeScript. A entidade ExceptionPeriod está **100% funcional** no backend com API completa, validações e testes implementados.

## 📋 Índice

1. [Estrutura da Entidade ExceptionPeriod](#estrutura-da-entidade-exceptionperiod)
2. [Endpoints da API](#endpoints-da-api)
3. [Layout da Tela](#layout-da-tela)
4. [Componentes Necessários](#componentes-necessários)
5. [Estados e Comportamentos](#estados-e-comportamentos)
6. [Validações Frontend](#validações-frontend)
7. [Fluxos de Interação](#fluxos-de-interação)
8. [Tratamento de Erros](#tratamento-de-erros)
9. [Casos de Teste](#casos-de-teste)

## 📊 Estrutura da Entidade ExceptionPeriod

### Propriedades da Entidade

```typescript
interface ExceptionPeriod {
  id: string;              // GUID único do período de exceção
  name: string;            // Nome do período (2-100 caracteres)
  description?: string;    // Descrição opcional (até 500 caracteres)
  startDate: string;       // Data de início no formato ISO 8601
  endDate: string;         // Data de fim no formato ISO 8601
  type: ExceptionPeriodType; // Tipo do período de exceção
  isActive: boolean;       // Se o período está ativo
  userId: string;          // GUID do usuário proprietário
  createdAt: string;       // Data de criação (ISO 8601)
  updatedAt?: string;      // Data da última atualização (ISO 8601)
  durationDays: number;    // Duração em dias (calculado)
}
```

### Tipos de Período de Exceção

```typescript
enum ExceptionPeriodType {
  Vacation = 1,       // Férias
  Holiday = 2,        // Feriado
  Travel = 3,         // Viagem
  Maintenance = 4,    // Manutenção
  MedicalLeave = 5,   // Licença médica
  RemoteWork = 6,     // Trabalho remoto
  Custom = 99         // Personalizado
}
```

### Regras de Negócio Importantes

1. **Nome**: Obrigatório, entre 2 e 100 caracteres, sem espaços nas extremidades
2. **Descrição**: Opcional, máximo 500 caracteres, trimmed automaticamente
3. **Datas**: StartDate deve ser menor que EndDate, sempre sem horário (00:00:00)
4. **Sobreposição**: O sistema valida sobreposição de períodos por usuário
5. **Ativação/Desativação**: Períodos podem ser ativados/desativados sem excluir
6. **Duração**: Calculada automaticamente incluindo o dia inicial e final

## 🔌 Endpoints da API

### Base URL

```text
https://localhost:5001/api/v1/exception-periods
```

### Autenticação

- **Tipo**: Bearer Token (JWT)
- **Header**: `Authorization: Bearer {token}`
- **Roles**: Algumas operações requerem role "Admin" ou "User"

### Lista de Endpoints

| Método | Endpoint | Descrição | Auth Required | Roles |
|--------|----------|-----------|---------------|-------|
| GET | `/exception-periods` | Listar períodos do usuário com filtros | ✅ | User, Admin |
| GET | `/exception-periods/{id}` | Buscar período por ID | ✅ | User, Admin |
| GET | `/exception-periods/active-on/{date}` | Buscar períodos ativos em data específica | ✅ | User, Admin |
| POST | `/exception-periods` | Criar novo período | ✅ | User, Admin |
| PUT | `/exception-periods/{id}` | Atualizar período existente | ✅ | User, Admin |
| DELETE | `/exception-periods/{id}` | Excluir período | ✅ | User, Admin |

### Parâmetros de Query para Listagem

```typescript
interface ListExceptionPeriodsParams {
  type?: ExceptionPeriodType;  // Filtrar por tipo
  activeOnDate?: string;       // Filtrar ativos em data específica (ISO 8601)
  onlyActive?: boolean;        // Apenas períodos ativos (default: true)
}
```

### DTOs para Requisições

```typescript
// Para criação
interface CreateExceptionPeriodDto {
  name: string;                    // 2-100 caracteres
  startDate: string;               // ISO 8601
  endDate: string;                 // ISO 8601
  type: ExceptionPeriodType;       // Tipo do período
  description?: string;            // Opcional, max 500 caracteres
}

// Para atualização
interface UpdateExceptionPeriodDto {
  name: string;                    // 2-100 caracteres
  startDate: string;               // ISO 8601
  endDate: string;                 // ISO 8601
  type: ExceptionPeriodType;       // Tipo do período
  description?: string;            // Opcional, max 500 caracteres
  isActive: boolean;               // Status ativo/inativo
}

// Resposta da API
interface ExceptionPeriodDto {
  id: string;
  name: string;
  description?: string;
  startDate: string;
  endDate: string;
  type: ExceptionPeriodType;
  isActive: boolean;
  userId: string;
  createdAt: string;
  updatedAt?: string;
  durationDays: number;
}
```

## 🎨 Layout da Tela

### Estrutura Visual

```text
┌─────────────────────────────────────────────────────────────────────────────────┐
│ 📅 Gerenciamento de Períodos de Exceção                         [+ Novo Período] │
├─────────────────────────────────────────────────────────────────────────────────┤
│ 🔍 [Campo de Busca]                    [Filtros ▼] [Apenas Ativos ☑]          │
├─────────────────────────────────────────────────────────────────────────────────┤
│ 📊 Estatísticas:                                                               │
│ • Total: 12 períodos • Ativos: 8 • Inativos: 4                               │
│ • Por Tipo: Férias: 5, Viagem: 3, Manutenção: 2, Outros: 2                  │
├─────────────────────────────────────────────────────────────────────────────────┤
│ 📋 Lista de Períodos de Exceção                                               │
│ ┌─────────────────────────────────────────────────────────────────────────────┐ │
│ │ 🏖️ Férias de Verão 2025                     [🔄] [✏️] [🗑️]               │ │
│ │ 📅 01/01/2025 - 15/01/2025 • 15 dias • Ativo                             │ │
│ │ 📝 Período de férias escolares de verão                                    │ │
│ │ 🏷️ Tipo: Vacation • Criado em: 12/07/2025                                │ │
│ └─────────────────────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────────────────────┐ │
│ │ ✈️ Viagem Internacional                      [🔄] [✏️] [🗑️]               │ │
│ │ 📅 20/02/2025 - 05/03/2025 • 14 dias • Ativo                             │ │
│ │ 📝 Viagem de trabalho para conferência                                     │ │
│ │ 🏷️ Tipo: Travel • Criado em: 10/07/2025                                   │ │
│ └─────────────────────────────────────────────────────────────────────────────┘ │
│ [Carregar mais...]                                                             │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Responsividade

#### Desktop (≥ 1024px)

- Layout em duas colunas: Lista principal (70%) + Painel de filtros (30%)
- Formulários em modal centralizado
- Cards com informações completas

#### Tablet (768px - 1023px)

- Layout em coluna única
- Filtros colapsáveis no topo
- Cards reduzidos com informações essenciais

#### Mobile (< 768px)

- Cards compactos empilhados
- Botões de ação em menu contextual (⋮)
- Formulários em tela completa
- Estatísticas simplificadas

## 🧩 Componentes Necessários

### 1. ExceptionPeriodManagementPage (Página Principal)

```typescript
interface ExceptionPeriodManagementPageProps {}

const ExceptionPeriodManagementPage: React.FC<ExceptionPeriodManagementPageProps> = () => {
  // Estado principal da página
  const [periods, setPeriods] = useState<ExceptionPeriod[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterType, setFilterType] = useState<ExceptionPeriodType | 'all'>('all');
  const [onlyActive, setOnlyActive] = useState(true);
  
  // Estados do formulário
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [editingPeriod, setEditingPeriod] = useState<ExceptionPeriod | null>(null);
  
  // Outros estados...
};
```

### 2. ExceptionPeriodCard (Item da Lista)

```typescript
interface ExceptionPeriodCardProps {
  period: ExceptionPeriod;
  onEdit: (period: ExceptionPeriod) => void;
  onDelete: (id: string) => void;
  onToggleActive: (period: ExceptionPeriod) => void;
  isLoading?: boolean;
}

const ExceptionPeriodCard: React.FC<ExceptionPeriodCardProps> = ({
  period,
  onEdit,
  onDelete,
  onToggleActive,
  isLoading = false
}) => {
  const getTypeIcon = (type: ExceptionPeriodType): string => {
    const icons = {
      [ExceptionPeriodType.Vacation]: '🏖️',
      [ExceptionPeriodType.Holiday]: '🎉',
      [ExceptionPeriodType.Travel]: '✈️',
      [ExceptionPeriodType.Maintenance]: '🔧',
      [ExceptionPeriodType.MedicalLeave]: '🏥',
      [ExceptionPeriodType.RemoteWork]: '🏠',
      [ExceptionPeriodType.Custom]: '⚙️'
    };
    return icons[type] || '📅';
  };

  const formatDateRange = (start: string, end: string): string => {
    const startDate = new Date(start);
    const endDate = new Date(end);
    return `${startDate.toLocaleDateString('pt-BR')} - ${endDate.toLocaleDateString('pt-BR')}`;
  };

  return (
    <div className={`period-card ${!period.isActive ? 'inactive' : ''}`}>
      {/* Conteúdo do card */}
    </div>
  );
};
```

### 3. ExceptionPeriodForm (Formulário)

```typescript
interface ExceptionPeriodFormProps {
  mode: 'create' | 'edit';
  initialData?: Partial<ExceptionPeriod>;
  onSubmit: (data: CreateExceptionPeriodDto | UpdateExceptionPeriodDto) => Promise<void>;
  onCancel: () => void;
  isLoading?: boolean;
}

const ExceptionPeriodForm: React.FC<ExceptionPeriodFormProps> = ({
  mode,
  initialData,
  onSubmit,
  onCancel,
  isLoading = false
}) => {
  // Estados do formulário
  const [formData, setFormData] = useState({
    name: initialData?.name || '',
    description: initialData?.description || '',
    startDate: initialData?.startDate || '',
    endDate: initialData?.endDate || '',
    type: initialData?.type || ExceptionPeriodType.Custom,
    isActive: initialData?.isActive ?? true
  });
  
  const [errors, setErrors] = useState<Record<string, string>>({});
  
  // Lógica do formulário...
};
```

### 4. ExceptionPeriodStats (Estatísticas)

```typescript
interface ExceptionPeriodStatsProps {
  periods: ExceptionPeriod[];
}

const ExceptionPeriodStats: React.FC<ExceptionPeriodStatsProps> = ({ periods }) => {
  const stats = useMemo(() => {
    const total = periods.length;
    const active = periods.filter(p => p.isActive).length;
    const inactive = total - active;
    
    const byType = periods.reduce((acc, period) => {
      const typeName = getTypeName(period.type);
      acc[typeName] = (acc[typeName] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);
    
    return { total, active, inactive, byType };
  }, [periods]);
  
  return (
    <div className="period-stats">
      <div className="stat-item">
        <span className="stat-value">{stats.total}</span>
        <span className="stat-label">Total</span>
      </div>
      <div className="stat-item">
        <span className="stat-value">{stats.active}</span>
        <span className="stat-label">Ativos</span>
      </div>
      <div className="stat-item">
        <span className="stat-value">{stats.inactive}</span>
        <span className="stat-label">Inativos</span>
      </div>
      {/* Estatísticas por tipo... */}
    </div>
  );
};
```

### 5. ExceptionPeriodFilters (Filtros e Busca)

```typescript
interface ExceptionPeriodFiltersProps {
  searchTerm: string;
  onSearchChange: (term: string) => void;
  filterType: ExceptionPeriodType | 'all';
  onFilterTypeChange: (type: ExceptionPeriodType | 'all') => void;
  onlyActive: boolean;
  onOnlyActiveChange: (active: boolean) => void;
  activeOnDate?: string;
  onActiveOnDateChange: (date?: string) => void;
}

const ExceptionPeriodFilters: React.FC<ExceptionPeriodFiltersProps> = ({
  searchTerm,
  onSearchChange,
  filterType,
  onFilterTypeChange,
  onlyActive,
  onOnlyActiveChange,
  activeOnDate,
  onActiveOnDateChange
}) => {
  return (
    <div className="period-filters">
      {/* Componentes de filtro */}
    </div>
  );
};
```

## 🔄 Estados e Comportamentos

### Estados Principais

```typescript
// Estado da lista de períodos
const [periods, setPeriods] = useState<ExceptionPeriod[]>([]);

// Estados de loading
const [isLoadingList, setIsLoadingList] = useState(false);
const [isCreating, setIsCreating] = useState(false);
const [isUpdating, setIsUpdating] = useState(false);
const [isDeleting, setIsDeleting] = useState<string | null>(null);
const [isTogglingActive, setIsTogglingActive] = useState<string | null>(null);

// Estados de UI
const [showCreateForm, setShowCreateForm] = useState(false);
const [editingPeriod, setEditingPeriod] = useState<ExceptionPeriod | null>(null);
const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);

// Estados de filtro e busca
const [searchTerm, setSearchTerm] = useState('');
const [filterType, setFilterType] = useState<ExceptionPeriodType | 'all'>('all');
const [onlyActive, setOnlyActive] = useState(true);
const [activeOnDate, setActiveOnDate] = useState<string>('');
const [sortBy, setSortBy] = useState<'startDate' | 'name' | 'type' | 'created'>('startDate');
const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc');

// Estados de erro
const [error, setError] = useState<string | null>(null);
const [formErrors, setFormErrors] = useState<Record<string, string>>({});
```

### Comportamentos de Loading

1. **Carregamento Inicial**: Skeleton cards enquanto carrega a lista
2. **Criação**: Formulário desabilitado com spinner no botão
3. **Edição**: Overlay de loading no formulário
4. **Exclusão**: Card com overlay de loading
5. **Ativar/Desativar**: Spinner no botão de toggle
6. **Busca**: Debounce de 300ms antes de filtrar

### Comportamentos de Erro

1. **Erro de Rede**: Toast com botão "Tentar Novamente"
2. **Erro de Validação**: Destacar campos com erro no formulário
3. **Erro de Autorização**: Redirect para login
4. **Erro 404**: Remover item da lista local
5. **Erro 500**: Toast com informações de suporte
6. **Sobreposição de Período**: Modal explicativo com opções

## ✅ Validações Frontend

### Campo Nome

```typescript
const validateName = (name: string): string | null => {
  if (!name.trim()) {
    return 'Nome é obrigatório';
  }
  
  if (name.trim().length < 2) {
    return 'Nome deve ter pelo menos 2 caracteres';
  }
  
  if (name.trim().length > 100) {
    return 'Nome não pode ter mais de 100 caracteres';
  }
  
  return null;
};
```

### Campo Descrição

```typescript
const validateDescription = (description: string): string | null => {
  if (description && description.trim().length > 500) {
    return 'Descrição não pode ter mais de 500 caracteres';
  }
  
  return null;
};
```

### Campos de Data

```typescript
const validateDates = (startDate: string, endDate: string): Record<string, string> => {
  const errors: Record<string, string> = {};
  
  if (!startDate) {
    errors.startDate = 'Data de início é obrigatória';
  }
  
  if (!endDate) {
    errors.endDate = 'Data de fim é obrigatória';
  }
  
  if (startDate && endDate) {
    const start = new Date(startDate);
    const end = new Date(endDate);
    
    if (isNaN(start.getTime())) {
      errors.startDate = 'Data de início inválida';
    }
    
    if (isNaN(end.getTime())) {
      errors.endDate = 'Data de fim inválida';
    }
    
    if (start >= end) {
      errors.endDate = 'Data de fim deve ser posterior à data de início';
    }
    
    // Verificar se é no passado (opcional, dependendo da regra de negócio)
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    if (start < today) {
      errors.startDate = 'Data de início não pode ser no passado';
    }
  }
  
  return errors;
};
```

### Campo Tipo

```typescript
const validateType = (type: ExceptionPeriodType): string | null => {
  const validTypes = Object.values(ExceptionPeriodType).filter(
    (value) => typeof value === 'number'
  ) as ExceptionPeriodType[];
  
  if (!validTypes.includes(type)) {
    return 'Tipo de período inválido';
  }
  
  return null;
};
```

### Validação Completa do Formulário

```typescript
const validateForm = (data: ExceptionPeriodFormData): Record<string, string> => {
  const errors: Record<string, string> = {};
  
  const nameError = validateName(data.name);
  if (nameError) errors.name = nameError;
  
  const descriptionError = validateDescription(data.description);
  if (descriptionError) errors.description = descriptionError;
  
  const dateErrors = validateDates(data.startDate, data.endDate);
  Object.assign(errors, dateErrors);
  
  const typeError = validateType(data.type);
  if (typeError) errors.type = typeError;
  
  return errors;
};
```

## 🎭 Fluxos de Interação

### 1. Fluxo de Criação

```
1. Usuário clica em [+ Novo Período]
2. Modal/Form é aberto
3. Usuário preenche dados:
   - Nome (obrigatório, 2-100 chars)
   - Descrição (opcional, max 500 chars)
   - Data de início (obrigatória)
   - Data de fim (obrigatória, posterior ao início)
   - Tipo (dropdown com opções)
4. Sistema valida em tempo real
5. Usuário clica em "Salvar"
6. Validação final + verificação de sobreposição
7. Se válido: POST para API
8. Se sucesso: Fecha modal, atualiza lista, mostra toast de sucesso
9. Se erro: Mostra erro no form/toast
```

### 2. Fluxo de Edição

```
1. Usuário clica em [✏️] no card do período
2. Modal/Form é aberto com dados preenchidos
3. Usuário altera dados necessários
4. Sistema valida em tempo real
5. Usuário clica em "Salvar"
6. Validação final + verificação de sobreposição
7. Se válido: PUT para API
8. Se sucesso: Fecha modal, atualiza item na lista, mostra toast
9. Se erro: Mostra erro no form/toast
```

### 3. Fluxo de Exclusão

```
1. Usuário clica em [🗑️] no card do período
2. Modal de confirmação é exibido:
   "Tem certeza que deseja excluir o período '{nome}'?"
   "Esta ação não pode ser desfeita."
   [Cancelar] [Excluir]
3. Se cancelar: Fecha modal
4. Se confirmar: DELETE para API
5. Se sucesso: Remove da lista, mostra toast
6. Se erro: Mostra toast de erro
```

### 4. Fluxo de Ativar/Desativar

```
1. Usuário clica no botão [🔄] no card
2. Spinner aparece no botão
3. PUT para API com isActive alternado
4. Se sucesso: Atualiza status do item na lista, mostra toast
5. Se erro: Mostra toast de erro, mantém estado anterior
```

### 5. Fluxo de Busca e Filtros

```
1. Usuário digita no campo de busca
2. Aguarda 300ms (debounce)
3. Filtra lista local por:
   - Nome (case-insensitive)
   - Descrição (case-insensitive)
4. Atualiza lista exibida
5. Se nenhum resultado: Mostra "Nenhum período encontrado"

6. Usuário seleciona filtros:
   - Tipo de período (dropdown)
   - Apenas ativos (checkbox)
   - Ativos em data específica (date picker)
7. Lista é filtrada imediatamente
8. Contador de resultados atualizado
9. Busca continua funcionando no subset filtrado
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
      return 'Período de exceção não encontrado.';
    case 409:
      return 'Já existe um período com sobreposição de datas.';
    case 422:
      return 'Período sobrepõe com período existente. Ajuste as datas.';
    case 500:
      return 'Erro interno do servidor. Tente novamente.';
    default:
      return `Erro ao ${operation}. Tente novamente.`;
  }
};
```

### Tratamento de Sobreposição

```typescript
const handleOverlapError = (error: any) => {
  // Quando o backend retorna erro de sobreposição
  return {
    title: 'Conflito de Períodos',
    message: 'O período selecionado sobrepõe com um período existente.',
    suggestions: [
      'Ajuste as datas para não conflitar',
      'Verifique os períodos existentes',
      'Desative o período conflitante se necessário'
    ]
  };
};
```

### Estados de Erro na UI

1. **Toast para erros temporários**: Rede, servidor
2. **Inline errors para validação**: Campos de formulário
3. **Modal para conflitos**: Sobreposição de períodos
4. **Empty state para listas vazias**: "Nenhum período cadastrado"
5. **Error boundary para erros críticos**: Tela de erro geral

## 🎯 Casos de Teste

### Cenários de Teste Obrigatórios

#### 1. Teste de Criação

```typescript
describe('Criação de Período de Exceção', () => {
  test('Deve criar período de férias com sucesso', async () => {
    // 1. Abrir form de criação
    // 2. Preencher nome: "Férias de Verão"
    // 3. Preencher descrição: "Período de férias"
    // 4. Selecionar datas: 01/01/2025 - 15/01/2025
    // 5. Selecionar tipo: Vacation
    // 6. Submeter form
    // 7. Verificar se aparece na lista
    // 8. Verificar toast de sucesso
  });

  test('Deve criar período sem descrição', async () => {
    // Similar ao anterior, mas deixar descrição vazia
  });

  test('Deve mostrar erro para nome vazio', async () => {
    // 1. Abrir form
    // 2. Deixar nome vazio
    // 3. Tentar submeter
    // 4. Verificar mensagem de erro
  });

  test('Deve mostrar erro para datas inválidas', async () => {
    // 1. Abrir form
    // 2. Definir data fim anterior à data início
    // 3. Verificar erro em tempo real
  });

  test('Deve mostrar erro para sobreposição', async () => {
    // 1. Criar período existente
    // 2. Tentar criar com datas sobrepostas
    // 3. Verificar modal de conflito
  });
});
```

#### 2. Teste de Edição

```typescript
describe('Edição de Período de Exceção', () => {
  test('Deve editar nome com sucesso', async () => {
    // 1. Criar período teste
    // 2. Clicar em editar
    // 3. Alterar nome
    // 4. Salvar
    // 5. Verificar alteração na lista
  });

  test('Deve editar tipo de período', async () => {
    // 1. Criar período tipo Custom
    // 2. Editar para Vacation
    // 3. Verificar mudança de ícone e label
  });

  test('Deve cancelar edição sem salvar', async () => {
    // 1. Iniciar edição
    // 2. Fazer alterações
    // 3. Cancelar
    // 4. Verificar que dados não foram alterados
  });
});
```

#### 3. Teste de Exclusão

```typescript
describe('Exclusão de Período de Exceção', () => {
  test('Deve excluir período após confirmação', async () => {
    // 1. Criar período teste
    // 2. Clicar em excluir
    // 3. Confirmar exclusão
    // 4. Verificar remoção da lista
  });

  test('Deve cancelar exclusão', async () => {
    // 1. Clicar em excluir
    // 2. Cancelar no modal
    // 3. Verificar que item permanece
  });
});
```

#### 4. Teste de Ativar/Desativar

```typescript
describe('Ativar/Desativar Período', () => {
  test('Deve desativar período ativo', async () => {
    // 1. Criar período ativo
    // 2. Clicar no botão toggle
    // 3. Verificar mudança de status
    // 4. Verificar indicador visual
  });

  test('Deve ativar período inativo', async () => {
    // 1. Criar período inativo
    // 2. Clicar no botão toggle
    // 3. Verificar ativação
  });
});
```

#### 5. Teste de Busca e Filtros

```typescript
describe('Busca e Filtros', () => {
  test('Deve filtrar por nome', async () => {
    // 1. Criar múltiplos períodos
    // 2. Buscar por termo específico
    // 3. Verificar resultados filtrados
  });

  test('Deve filtrar por tipo', async () => {
    // 1. Criar períodos de tipos diferentes
    // 2. Aplicar filtro "Vacation"
    // 3. Verificar apenas períodos de férias aparecem
  });

  test('Deve filtrar apenas ativos', async () => {
    // 1. Criar períodos ativos e inativos
    // 2. Marcar "Apenas Ativos"
    // 3. Verificar apenas ativos aparecem
  });

  test('Deve filtrar por data específica', async () => {
    // 1. Criar períodos com datas diferentes
    // 2. Filtrar por data específica
    // 3. Verificar apenas períodos ativos na data
  });
});
```

## 📱 Comportamentos Específicos

### Formatação de Datas

```typescript
// Para display na UI
const formatDateRange = (startDate: string, endDate: string): string => {
  const start = new Date(startDate);
  const end = new Date(endDate);
  
  return `${start.toLocaleDateString('pt-BR')} - ${end.toLocaleDateString('pt-BR')}`;
};

// Para inputs de formulário
const formatDateForInput = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toISOString().split('T')[0]; // YYYY-MM-DD
};

// Para cálculo de duração
const calculateDuration = (startDate: string, endDate: string): number => {
  const start = new Date(startDate);
  const end = new Date(endDate);
  return Math.ceil((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24)) + 1;
};
```

### Indicadores Visuais por Tipo

```typescript
const getTypeConfig = (type: ExceptionPeriodType) => {
  const configs = {
    [ExceptionPeriodType.Vacation]: {
      icon: '🏖️',
      label: 'Férias',
      color: '#10b981', // Verde
      bgColor: '#d1fae5'
    },
    [ExceptionPeriodType.Holiday]: {
      icon: '🎉',
      label: 'Feriado',
      color: '#f59e0b', // Amarelo
      bgColor: '#fef3c7'
    },
    [ExceptionPeriodType.Travel]: {
      icon: '✈️',
      label: 'Viagem',
      color: '#3b82f6', // Azul
      bgColor: '#dbeafe'
    },
    [ExceptionPeriodType.Maintenance]: {
      icon: '🔧',
      label: 'Manutenção',
      color: '#6b7280', // Cinza
      bgColor: '#f3f4f6'
    },
    [ExceptionPeriodType.MedicalLeave]: {
      icon: '🏥',
      label: 'Licença Médica',
      color: '#ef4444', // Vermelho
      bgColor: '#fee2e2'
    },
    [ExceptionPeriodType.RemoteWork]: {
      icon: '🏠',
      label: 'Trabalho Remoto',
      color: '#8b5cf6', // Roxo
      bgColor: '#ede9fe'
    },
    [ExceptionPeriodType.Custom]: {
      icon: '⚙️',
      label: 'Personalizado',
      color: '#64748b', // Cinza escuro
      bgColor: '#f1f5f9'
    }
  };
  
  return configs[type] || configs[ExceptionPeriodType.Custom];
};
```

### Estados dos Períodos

```typescript
const getPeriodStatus = (period: ExceptionPeriod) => {
  const now = new Date();
  const startDate = new Date(period.startDate);
  const endDate = new Date(period.endDate);
  
  if (!period.isActive) {
    return {
      status: 'inactive',
      label: 'Inativo',
      color: '#6b7280',
      icon: '⏸️'
    };
  }
  
  if (now < startDate) {
    return {
      status: 'upcoming',
      label: 'Futuro',
      color: '#3b82f6',
      icon: '⏳'
    };
  }
  
  if (now >= startDate && now <= endDate) {
    return {
      status: 'active',
      label: 'Ativo Agora',
      color: '#10b981',
      icon: '✅'
    };
  }
  
  return {
    status: 'past',
    label: 'Finalizado',
    color: '#6b7280',
    icon: '✅'
  };
};
```

### Ordenação Padrão

```typescript
const sortPeriods = (periods: ExceptionPeriod[], sortBy: string, sortOrder: 'asc' | 'desc') => {
  return [...periods].sort((a, b) => {
    let compareValue = 0;
    
    switch (sortBy) {
      case 'startDate':
        compareValue = new Date(a.startDate).getTime() - new Date(b.startDate).getTime();
        break;
      case 'name':
        compareValue = a.name.localeCompare(b.name, 'pt-BR');
        break;
      case 'type':
        compareValue = a.type - b.type;
        break;
      case 'created':
        compareValue = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
        break;
      default:
        compareValue = 0;
    }
    
    return sortOrder === 'desc' ? -compareValue : compareValue;
  });
};
```

## 🎨 Guia de Estilo

### Cores (seguir design system)

```css
/* Cores principais */
--period-primary: #2563eb;       /* Azul principal */
--period-success: #10b981;       /* Verde para sucesso */
--period-warning: #f59e0b;       /* Amarelo para avisos */
--period-error: #ef4444;         /* Vermelho para erros */

/* Cores por tipo */
--period-vacation: #10b981;      /* Verde para férias */
--period-holiday: #f59e0b;       /* Amarelo para feriados */
--period-travel: #3b82f6;        /* Azul para viagens */
--period-maintenance: #6b7280;   /* Cinza para manutenção */
--period-medical: #ef4444;       /* Vermelho para licença médica */
--period-remote: #8b5cf6;        /* Roxo para trabalho remoto */
--period-custom: #64748b;        /* Cinza escuro para personalizado */

/* Estados */
--period-bg-hover: #f3f4f6;
--period-bg-active: #e5e7eb;
--period-bg-inactive: #f9fafb;
--period-border: #d1d5db;
--period-border-active: #10b981;
--period-border-inactive: #9ca3af;
```

### Espaçamentos

```css
/* Seguir grid de 8px */
--space-xs: 4px;
--space-sm: 8px;
--space-md: 16px;
--space-lg: 24px;
--space-xl: 32px;
--space-2xl: 48px;
```

### Tipografia

```css
/* Títulos */
.period-title: font-size: 1.5rem; font-weight: 600;
.period-card-title: font-size: 1.125rem; font-weight: 500;

/* Corpo */
.period-description: font-size: 0.875rem; line-height: 1.5;
.period-meta: font-size: 0.75rem; color: #6b7280;
.period-label: font-size: 0.625rem; font-weight: 500; text-transform: uppercase;
```

### Animações

```css
/* Transições suaves */
.period-card {
  transition: all 0.2s ease-in-out;
}

.period-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

/* Loading states */
.loading-pulse {
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}
```

## 🔧 Implementação Técnica

### Service para API

```typescript
class ExceptionPeriodService {
  private baseUrl = '/api/v1/exception-periods';

  async getAll(params?: ListExceptionPeriodsParams): Promise<ExceptionPeriod[]> {
    const queryString = new URLSearchParams();
    
    if (params?.type) queryString.append('type', params.type.toString());
    if (params?.activeOnDate) queryString.append('activeOnDate', params.activeOnDate);
    if (params?.onlyActive !== undefined) queryString.append('onlyActive', params.onlyActive.toString());
    
    const url = queryString.toString() ? `${this.baseUrl}?${queryString}` : this.baseUrl;
    
    const response = await fetch(url, {
      headers: this.getAuthHeaders()
    });
    
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return response.json();
  }

  async getById(id: string): Promise<ExceptionPeriod> {
    const response = await fetch(`${this.baseUrl}/${id}`, {
      headers: this.getAuthHeaders()
    });
    
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return response.json();
  }

  async getActiveOnDate(date: string): Promise<ExceptionPeriod[]> {
    const response = await fetch(`${this.baseUrl}/active-on/${date}`, {
      headers: this.getAuthHeaders()
    });
    
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return response.json();
  }

  async create(data: CreateExceptionPeriodDto): Promise<ExceptionPeriod> {
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

  async update(id: string, data: UpdateExceptionPeriodDto): Promise<ExceptionPeriod> {
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

  private getAuthHeaders() {
    const token = localStorage.getItem('authToken');
    return token ? { Authorization: `Bearer ${token}` } : {};
  }
}
```

### Hook Customizado

```typescript
const useExceptionPeriods = () => {
  const [periods, setPeriods] = useState<ExceptionPeriod[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const periodService = useMemo(() => new ExceptionPeriodService(), []);

  const loadPeriods = useCallback(async (params?: ListExceptionPeriodsParams) => {
    try {
      setLoading(true);
      setError(null);
      const data = await periodService.getAll(params);
      setPeriods(data);
    } catch (err) {
      setError('Erro ao carregar períodos de exceção');
      console.error('Error loading exception periods:', err);
    } finally {
      setLoading(false);
    }
  }, [periodService]);

  const createPeriod = useCallback(async (data: CreateExceptionPeriodDto) => {
    try {
      const newPeriod = await periodService.create(data);
      setPeriods(prev => [...prev, newPeriod]);
      return newPeriod;
    } catch (err) {
      throw new Error('Erro ao criar período de exceção');
    }
  }, [periodService]);

  const updatePeriod = useCallback(async (id: string, data: UpdateExceptionPeriodDto) => {
    try {
      const updatedPeriod = await periodService.update(id, data);
      setPeriods(prev => prev.map(p => p.id === id ? updatedPeriod : p));
      return updatedPeriod;
    } catch (err) {
      throw new Error('Erro ao atualizar período de exceção');
    }
  }, [periodService]);

  const deletePeriod = useCallback(async (id: string) => {
    try {
      await periodService.delete(id);
      setPeriods(prev => prev.filter(p => p.id !== id));
    } catch (err) {
      throw new Error('Erro ao excluir período de exceção');
    }
  }, [periodService]);

  const togglePeriodActive = useCallback(async (id: string, isActive: boolean) => {
    try {
      const period = periods.find(p => p.id === id);
      if (!period) throw new Error('Período não encontrado');
      
      const updateData: UpdateExceptionPeriodDto = {
        name: period.name,
        startDate: period.startDate,
        endDate: period.endDate,
        type: period.type,
        description: period.description,
        isActive
      };
      
      const updatedPeriod = await periodService.update(id, updateData);
      setPeriods(prev => prev.map(p => p.id === id ? updatedPeriod : p));
      return updatedPeriod;
    } catch (err) {
      throw new Error('Erro ao alterar status do período');
    }
  }, [periodService, periods]);

  useEffect(() => {
    loadPeriods();
  }, [loadPeriods]);

  return {
    periods,
    loading,
    error,
    createPeriod,
    updatePeriod,
    deletePeriod,
    togglePeriodActive,
    refetch: loadPeriods
  };
};
```

## 📋 Checklist de Implementação

### Fase 1: Estrutura Base

- [ ] Criar página `ExceptionPeriodManagementPage`
- [ ] Implementar `ExceptionPeriodService`
- [ ] Criar hook `useExceptionPeriods`
- [ ] Implementar enum `ExceptionPeriodType`
- [ ] Configurar interfaces TypeScript
- [ ] Implementar loading states
- [ ] Configurar roteamento

### Fase 2: Listagem

- [ ] Componente `ExceptionPeriodCard`
- [ ] Componente `ExceptionPeriodStats`
- [ ] Implementar paginação básica
- [ ] Estados vazios (empty states)
- [ ] Responsividade mobile
- [ ] Indicadores visuais por tipo
- [ ] Status dos períodos (ativo, futuro, finalizado)

### Fase 3: CRUD

- [ ] Componente `ExceptionPeriodForm`
- [ ] Modal de criação/edição
- [ ] Validações frontend completas
- [ ] Tratamento de sobreposição
- [ ] Feedback visual (toasts)
- [ ] Confirmação de exclusão
- [ ] Toggle ativar/desativar

### Fase 4: Busca e Filtros

- [ ] Componente `ExceptionPeriodFilters`
- [ ] Implementar debounce na busca
- [ ] Filtros por tipo
- [ ] Filtro apenas ativos
- [ ] Filtro por data específica
- [ ] Ordenação múltipla
- [ ] Persistir filtros no localStorage

### Fase 5: Testes

- [ ] Testes unitários dos componentes
- [ ] Testes de integração da página
- [ ] Testes e2e dos fluxos principais
- [ ] Testes de validação
- [ ] Testes de responsividade
- [ ] Testes de acessibilidade

### Fase 6: Polimento

- [ ] Animações e transições
- [ ] Otimizações de performance
- [ ] Internacionalização (i18n)
- [ ] Documentação dos componentes
- [ ] Review de acessibilidade
- [ ] PWA considerations

---

## 📞 Suporte

**Para dúvidas técnicas:**

- Consulte os testes de integração: `/tests/SmartAlarm.Api.Tests/Controllers/ExceptionPeriodsControllerIntegrationTests.cs`
- Testes de domínio: `/tests/SmartAlarm.Domain.Tests/Entities/ExceptionPeriodTests.cs`
- Validações: `/src/SmartAlarm.Application/Validators/ExceptionPeriod/`
- Handlers: `/src/SmartAlarm.Application/Handlers/ExceptionPeriod/`

**Entidade ExceptionPeriod está 100% funcional e testada!** 🎉

---

*Documento gerado em: 15/07/2025*  
*Versão da API: v1*  
*Backend Status: ✅ Funcional*
