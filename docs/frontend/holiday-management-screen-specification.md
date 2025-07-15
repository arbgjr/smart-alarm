# 📝 Especificação da Tela de Gerenciamento de Feriados

## 🎯 Visão Geral

Esta especificação detalha como desenvolver a tela de gerenciamento de feriados no frontend React/TypeScript. A entidade Holiday está **100% funcional** no backend com API completa, validações e testes implementados.

## 📋 Índice

1. [Estrutura da Entidade Holiday](#estrutura-da-entidade-holiday)
2. [Endpoints da API](#endpoints-da-api)
3. [Layout da Tela](#layout-da-tela)
4. [Componentes Necessários](#componentes-necessários)
5. [Estados e Comportamentos](#estados-e-comportamentos)
6. [Validações Frontend](#validações-frontend)
7. [Fluxos de Interação](#fluxos-de-interação)
8. [Tratamento de Erros](#tratamento-de-erros)
9. [Casos de Teste](#casos-de-teste)

## 📊 Estrutura da Entidade Holiday

### Propriedades da Entidade

```typescript
interface Holiday {
  id: string;              // GUID único do feriado
  date: string;            // Data no formato ISO 8601 (yyyy-MM-ddTHH:mm:ss.sssZ)
  description: string;     // Descrição do feriado (2-100 caracteres)
  createdAt: string;       // Data de criação (ISO 8601)
  isRecurring: boolean;    // true se é feriado recorrente (anual)
}
```

### Regras de Negócio Importantes

1. **Feriados Recorrentes**: Quando `date.year === 1`, o feriado se repete todos os anos
2. **Feriados Específicos**: Quando `date.year > 1`, o feriado é apenas para aquele ano
3. **Descrição**: Obrigatória, entre 2 e 100 caracteres, sem espaços em branco nas extremidades
4. **Data**: Sempre armazenada sem componente de hora (00:00:00)

## 🔌 Endpoints da API

### Base URL

```text
https://localhost:5001/api/v1/holidays
```

### Autenticação

- **Tipo**: Bearer Token (JWT)
- **Header**: `Authorization: Bearer {token}`
- **Roles**: Algumas operações requerem role "Admin"

### Lista de Endpoints

| Método | Endpoint | Descrição | Auth Required | Admin Only |
|--------|----------|-----------|---------------|------------|
| GET | `/holidays` | Listar todos os feriados | ✅ | ❌ |
| GET | `/holidays/{id}` | Buscar feriado por ID | ✅ | ❌ |
| GET | `/holidays/by-date?date={date}` | Buscar por data específica | ✅ | ❌ |
| POST | `/holidays` | Criar novo feriado | ✅ | ✅ |
| PUT | `/holidays/{id}` | Atualizar feriado | ✅ | ✅ |
| DELETE | `/holidays/{id}` | Deletar feriado | ✅ | ✅ |

### DTOs para Requisições

```typescript
// Para criação
interface CreateHolidayDto {
  date: string;        // ISO 8601 - Para recorrente use ano 0001
  description: string; // 2-200 caracteres
}

// Para atualização
interface UpdateHolidayDto {
  date: string;        // ISO 8601
  description: string; // 2-100 caracteres
}

// Resposta da API
interface HolidayResponseDto {
  id: string;
  date: string;
  description: string;
  createdAt: string;
  isRecurring: boolean;
}
```

## 🎨 Layout da Tela

### Estrutura Visual

```text
┌─────────────────────────────────────────────────────────────┐
│ 📅 Gerenciamento de Feriados                    [+ Novo]    │
├─────────────────────────────────────────────────────────────┤
│ 🔍 [Campo de Busca]                     [Filtros ▼]        │
├─────────────────────────────────────────────────────────────┤
│ 📊 Estatísticas:                                           │
│ • Total: 25 feriados                                       │
│ • Recorrentes: 15 | Específicos: 10                       │
├─────────────────────────────────────────────────────────────┤
│ 📋 Lista de Feriados                                       │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ 🎄 Natal                              [🔄] [✏️] [🗑️]   │ │
│ │ 📅 25/12 • Recorrente                                  │ │
│ │ 📝 Criado em: 12/07/2025                               │ │
│ └─────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ 🎊 Independência do Brasil 2024       [🔄] [✏️] [🗑️]   │ │
│ │ 📅 07/09/2024 • Específico                             │ │
│ │ 📝 Criado em: 10/07/2025                               │ │
│ └─────────────────────────────────────────────────────────┘ │
│ [Carregar mais...]                                         │
└─────────────────────────────────────────────────────────────┘
```

### Responsividade

#### Desktop (≥ 1024px)

- Layout em duas colunas: Lista principal (70%) + Painel lateral (30%)
- Formulários em modal ou painel lateral
- Tabela com todas as colunas visíveis

#### Tablet (768px - 1023px)

- Layout em coluna única
- Cards reduzidos com informações essenciais
- Formulários em modal full-screen

#### Mobile (< 768px)

- Cards compactos empilhados
- Botões de ação em menu contextual (⋮)
- Formulários em tela completa

## 🧩 Componentes Necessários

### 1. HolidayManagementPage (Página Principal)

```typescript
interface HolidayManagementPageProps {}

const HolidayManagementPage: React.FC<HolidayManagementPageProps> = () => {
  // Estado principal da página
  const [holidays, setHolidays] = useState<Holiday[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterType, setFilterType] = useState<'all' | 'recurring' | 'specific'>('all');
  
  // Outros estados...
};
```

### 2. HolidayCard (Item da Lista)

```typescript
interface HolidayCardProps {
  holiday: Holiday;
  onEdit: (holiday: Holiday) => void;
  onDelete: (id: string) => void;
  onToggleRecurring?: (holiday: Holiday) => void;
  isLoading?: boolean;
}

const HolidayCard: React.FC<HolidayCardProps> = ({
  holiday,
  onEdit,
  onDelete,
  onToggleRecurring,
  isLoading = false
}) => {
  return (
    <div className="holiday-card">
      {/* Conteúdo do card */}
    </div>
  );
};
```

### 3. HolidayForm (Formulário)

```typescript
interface HolidayFormProps {
  mode: 'create' | 'edit';
  initialData?: Partial<Holiday>;
  onSubmit: (data: CreateHolidayDto | UpdateHolidayDto) => Promise<void>;
  onCancel: () => void;
  isLoading?: boolean;
}

const HolidayForm: React.FC<HolidayFormProps> = ({
  mode,
  initialData,
  onSubmit,
  onCancel,
  isLoading = false
}) => {
  // Estados do formulário
  const [formData, setFormData] = useState({
    description: initialData?.description || '',
    date: initialData?.date || '',
    isRecurring: initialData?.isRecurring || false
  });
  
  const [errors, setErrors] = useState<Record<string, string>>({});
  
  // Lógica do formulário...
};
```

### 4. HolidayStats (Estatísticas)

```typescript
interface HolidayStatsProps {
  holidays: Holiday[];
}

const HolidayStats: React.FC<HolidayStatsProps> = ({ holidays }) => {
  const total = holidays.length;
  const recurring = holidays.filter(h => h.isRecurring).length;
  const specific = total - recurring;
  
  return (
    <div className="holiday-stats">
      <div className="stat-item">
        <span className="stat-value">{total}</span>
        <span className="stat-label">Total</span>
      </div>
      {/* Outras estatísticas... */}
    </div>
  );
};
```

### 5. HolidaySearch (Busca e Filtros)

```typescript
interface HolidaySearchProps {
  searchTerm: string;
  onSearchChange: (term: string) => void;
  filterType: 'all' | 'recurring' | 'specific';
  onFilterChange: (filter: 'all' | 'recurring' | 'specific') => void;
}
```

## 🔄 Estados e Comportamentos

### Estados Principais

```typescript
// Estado da lista de feriados
const [holidays, setHolidays] = useState<Holiday[]>([]);

// Estados de loading
const [isLoadingList, setIsLoadingList] = useState(false);
const [isCreating, setIsCreating] = useState(false);
const [isUpdating, setIsUpdating] = useState(false);
const [isDeleting, setIsDeleting] = useState<string | null>(null);

// Estados de UI
const [showCreateForm, setShowCreateForm] = useState(false);
const [editingHoliday, setEditingHoliday] = useState<Holiday | null>(null);
const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);

// Estados de filtro e busca
const [searchTerm, setSearchTerm] = useState('');
const [filterType, setFilterType] = useState<'all' | 'recurring' | 'specific'>('all');
const [sortBy, setSortBy] = useState<'date' | 'description' | 'created'>('date');
const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc');

// Estados de erro
const [error, setError] = useState<string | null>(null);
const [formErrors, setFormErrors] = useState<Record<string, string>>({});
```

### Comportamentos de Loading

1. **Carregamento Inicial**: Skeleton cards enquanto carrega a lista
2. **Criação**: Botão desabilitado com spinner
3. **Edição**: Form desabilitado com overlay de loading
4. **Exclusão**: Card com overlay de loading
5. **Busca**: Debounce de 300ms antes de filtrar

### Comportamentos de Erro

1. **Erro de Rede**: Toast com botão "Tentar Novamente"
2. **Erro de Validação**: Destacar campos com erro
3. **Erro de Autorização**: Redirect para login
4. **Erro 404**: Remover item da lista local
5. **Erro 500**: Toast com suporte para contato

## ✅ Validações Frontend

### Campo Descrição

```typescript
const validateDescription = (description: string): string | null => {
  if (!description.trim()) {
    return 'Descrição é obrigatória';
  }
  
  if (description.trim().length < 2) {
    return 'Descrição deve ter pelo menos 2 caracteres';
  }
  
  if (description.trim().length > 100) {
    return 'Descrição não pode ter mais de 100 caracteres';
  }
  
  return null;
};
```

### Campo Data

```typescript
const validateDate = (date: string, isRecurring: boolean): string | null => {
  if (!date) {
    return 'Data é obrigatória';
  }
  
  const parsedDate = new Date(date);
  if (isNaN(parsedDate.getTime())) {
    return 'Data inválida';
  }
  
  // Para feriados específicos, não permitir datas no passado
  if (!isRecurring && parsedDate < new Date()) {
    return 'Data não pode ser no passado para feriados específicos';
  }
  
  return null;
};
```

### Validação Completa do Formulário

```typescript
const validateForm = (data: HolidayFormData): Record<string, string> => {
  const errors: Record<string, string> = {};
  
  const descriptionError = validateDescription(data.description);
  if (descriptionError) errors.description = descriptionError;
  
  const dateError = validateDate(data.date, data.isRecurring);
  if (dateError) errors.date = dateError;
  
  return errors;
};
```

## 🎭 Fluxos de Interação

### 1. Fluxo de Criação

```
1. Usuário clica em [+ Novo Feriado]
2. Modal/Form é aberto
3. Usuário preenche dados:
   - Descrição (obrigatória, 2-100 chars)
   - Data (obrigatória)
   - Checkbox "Feriado Recorrente"
4. Sistema valida em tempo real
5. Usuário clica em "Salvar"
6. Validação final
7. Se válido: POST para API
8. Se sucesso: Fecha modal, atualiza lista, mostra toast de sucesso
9. Se erro: Mostra erro no form/toast
```

### 2. Fluxo de Edição

```
1. Usuário clica em [✏️] no card do feriado
2. Modal/Form é aberto com dados preenchidos
3. Usuário altera dados necessários
4. Sistema valida em tempo real
5. Usuário clica em "Salvar"
6. Validação final
7. Se válido: PUT para API
8. Se sucesso: Fecha modal, atualiza item na lista, mostra toast
9. Se erro: Mostra erro no form/toast
```

### 3. Fluxo de Exclusão

```
1. Usuário clica em [🗑️] no card do feriado
2. Modal de confirmação é exibido:
   "Tem certeza que deseja excluir o feriado '{nome}'?"
   "Esta ação não pode ser desfeita."
   [Cancelar] [Excluir]
3. Se cancelar: Fecha modal
4. Se confirmar: DELETE para API
5. Se sucesso: Remove da lista, mostra toast
6. Se erro: Mostra toast de erro
```

### 4. Fluxo de Busca

```
1. Usuário digita no campo de busca
2. Aguarda 300ms (debounce)
3. Filtra lista local por:
   - Descrição (case-insensitive)
   - Data (formato dd/MM/yyyy)
4. Atualiza lista exibida
5. Se nenhum resultado: Mostra "Nenhum feriado encontrado"
```

### 5. Fluxo de Filtros

```
1. Usuário seleciona filtro:
   - Todos
   - Recorrentes (isRecurring = true)
   - Específicos (isRecurring = false)
2. Lista é filtrada imediatamente
3. Contador atualizado
4. Busca continua funcionando no subset filtrado
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
      return 'Feriado não encontrado.';
    case 409:
      return 'Já existe um feriado com estas informações.';
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

1. **Toast para erros temporários**: Rede, servidor
2. **Inline errors para validação**: Campos de formulário
3. **Empty state para listas vazias**: "Nenhum feriado cadastrado"
4. **Error boundary para erros críticos**: Tela de erro geral

## 🎯 Casos de Teste

### Cenários de Teste Obrigatórios

#### 1. Teste de Criação

```typescript
describe('Criação de Feriado', () => {
  test('Deve criar feriado recorrente com sucesso', async () => {
    // 1. Abrir form de criação
    // 2. Preencher descrição: "Natal"
    // 3. Preencher data: "25/12"
    // 4. Marcar como recorrente
    // 5. Submeter form
    // 6. Verificar se aparece na lista
    // 7. Verificar toast de sucesso
  });

  test('Deve criar feriado específico com sucesso', async () => {
    // Similar ao anterior, mas sem marcar recorrente
  });

  test('Deve mostrar erro para descrição vazia', async () => {
    // 1. Abrir form
    // 2. Deixar descrição vazia
    // 3. Tentar submeter
    // 4. Verificar mensagem de erro
  });

  test('Deve mostrar erro para descrição muito longa', async () => {
    // 1. Abrir form
    // 2. Preencher com 101 caracteres
    // 3. Verificar erro em tempo real
  });
});
```

#### 2. Teste de Edição

```typescript
describe('Edição de Feriado', () => {
  test('Deve editar descrição com sucesso', async () => {
    // 1. Criar feriado teste
    // 2. Clicar em editar
    // 3. Alterar descrição
    // 4. Salvar
    // 5. Verificar alteração na lista
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
describe('Exclusão de Feriado', () => {
  test('Deve excluir feriado após confirmação', async () => {
    // 1. Criar feriado teste
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

#### 4. Teste de Busca e Filtros

```typescript
describe('Busca e Filtros', () => {
  test('Deve filtrar por descrição', async () => {
    // 1. Criar múltiplos feriados
    // 2. Buscar por termo específico
    // 3. Verificar resultados filtrados
  });

  test('Deve filtrar por tipo (recorrente/específico)', async () => {
    // 1. Criar feriados de ambos os tipos
    // 2. Aplicar filtro "Recorrentes"
    // 3. Verificar apenas recorrentes aparecem
  });
});
```

## 📱 Comportamentos Específicos

### Formatação de Datas

```typescript
// Para display na UI
const formatDateForDisplay = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: date.getFullYear() === 1 ? undefined : 'numeric'
  });
};

// Para feriados recorrentes, mostrar apenas dia/mês
// Para específicos, mostrar dia/mês/ano
```

### Indicadores Visuais

1. **Feriados Recorrentes**:
   - Ícone: 🔄
   - Badge: "Anual"
   - Cor: Verde

2. **Feriados Específicos**:
   - Ícone: 📅
   - Badge: "2024" (ano)
   - Cor: Azul

3. **Feriados Passados**:
   - Opacidade reduzida
   - Ícone: ⏳
   - Não podem ser editados

### Ordenação Padrão

1. **Primário**: Feriados recorrentes primeiro
2. **Secundário**: Por data (mês/dia)
3. **Terciário**: Por descrição (alfabética)

### Paginação (Futuro)

- Implementar paginação quando lista > 50 itens
- Lazy loading com scroll infinito
- Cache local dos itens já carregados

## 🎨 Guia de Estilo

### Cores (seguir design system)

```css
/* Cores principais */
--holiday-primary: #2563eb;      /* Azul principal */
--holiday-success: #10b981;      /* Verde para sucesso */
--holiday-warning: #f59e0b;      /* Amarelo para avisos */
--holiday-error: #ef4444;        /* Vermelho para erros */
--holiday-recurring: #10b981;    /* Verde para recorrentes */
--holiday-specific: #3b82f6;     /* Azul para específicos */

/* Estados */
--holiday-bg-hover: #f3f4f6;
--holiday-bg-active: #e5e7eb;
--holiday-border: #d1d5db;
```

### Espaçamentos

```css
/* Seguir grid de 8px */
--space-xs: 4px;
--space-sm: 8px;
--space-md: 16px;
--space-lg: 24px;
--space-xl: 32px;
```

### Tipografia

```css
/* Títulos */
.holiday-title: font-size: 1.5rem; font-weight: 600;
.holiday-card-title: font-size: 1.125rem; font-weight: 500;

/* Corpo */
.holiday-description: font-size: 0.875rem; line-height: 1.5;
.holiday-meta: font-size: 0.75rem; color: #6b7280;
```

## 🔧 Implementação Técnica

### Service para API

```typescript
class HolidayService {
  private baseUrl = '/api/v1/holidays';

  async getAll(): Promise<Holiday[]> {
    const response = await fetch(this.baseUrl, {
      headers: this.getAuthHeaders()
    });
    
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return response.json();
  }

  async getById(id: string): Promise<Holiday> {
    const response = await fetch(`${this.baseUrl}/${id}`, {
      headers: this.getAuthHeaders()
    });
    
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return response.json();
  }

  async create(data: CreateHolidayDto): Promise<Holiday> {
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

  async update(id: string, data: UpdateHolidayDto): Promise<Holiday> {
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
const useHolidays = () => {
  const [holidays, setHolidays] = useState<Holiday[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const holidayService = useMemo(() => new HolidayService(), []);

  const loadHolidays = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await holidayService.getAll();
      setHolidays(data);
    } catch (err) {
      setError('Erro ao carregar feriados');
      console.error('Error loading holidays:', err);
    } finally {
      setLoading(false);
    }
  }, [holidayService]);

  const createHoliday = useCallback(async (data: CreateHolidayDto) => {
    try {
      const newHoliday = await holidayService.create(data);
      setHolidays(prev => [...prev, newHoliday]);
      return newHoliday;
    } catch (err) {
      throw new Error('Erro ao criar feriado');
    }
  }, [holidayService]);

  // Outros métodos...

  useEffect(() => {
    loadHolidays();
  }, [loadHolidays]);

  return {
    holidays,
    loading,
    error,
    createHoliday,
    updateHoliday,
    deleteHoliday,
    refetch: loadHolidays
  };
};
```

## 📋 Checklist de Implementação

### Fase 1: Estrutura Base

- [ ] Criar página `HolidayManagementPage`
- [ ] Implementar `HolidayService`
- [ ] Criar hook `useHolidays`
- [ ] Implementar loading states
- [ ] Configurar roteamento

### Fase 2: Listagem

- [ ] Componente `HolidayCard`
- [ ] Componente `HolidayStats`
- [ ] Implementar paginação básica
- [ ] Estados vazios (empty states)
- [ ] Responsividade mobile

### Fase 3: CRUD

- [ ] Componente `HolidayForm`
- [ ] Modal de criação/edição
- [ ] Validações frontend
- [ ] Tratamento de erros
- [ ] Feedback visual (toasts)

### Fase 4: Busca e Filtros

- [ ] Componente `HolidaySearch`
- [ ] Implementar debounce
- [ ] Filtros por tipo
- [ ] Ordenação
- [ ] Persistir filtros no localStorage

### Fase 5: Testes

- [ ] Testes unitários dos componentes
- [ ] Testes de integração da página
- [ ] Testes e2e dos fluxos principais
- [ ] Testes de acessibilidade
- [ ] Testes de responsividade

### Fase 6: Polimento

- [ ] Animações e transições
- [ ] Otimizações de performance
- [ ] Internacionalização (i18n)
- [ ] Documentação dos componentes
- [ ] Review de acessibilidade

---

## 📞 Suporte

**Para dúvidas técnicas:**

- Consulte a documentação da API: `/docs/HOLIDAY-API-STATUS-FINAL.md`
- Testes HTTP disponíveis em: `/tests/http/holidays.http`
- Exemplos de uso nos testes de integração

**Entidade Holiday está 100% funcional e testada!** 🎉

---

*Documento gerado em: 14/07/2025*
*Versão da API: v1*
*Backend Status: ✅ Funcional*
