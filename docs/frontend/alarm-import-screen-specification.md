# 📝 Especificação da Tela de Upload/Importação de Alarmes

## 🎯 Visão Geral

Esta especificação detalha como desenvolver a tela de upload e importação de alarmes no frontend React/TypeScript. A funcionalidade de importação está **100% funcional** no backend com API completa, FileParser implementado, validações e testes implementados.

## 📋 Índice

1. [Estrutura da Funcionalidade de Import](#estrutura-da-funcionalidade-de-import)
2. [Endpoints da API](#endpoints-da-api)
3. [Layout da Tela](#layout-da-tela)
4. [Componentes Necessários](#componentes-necessários)
5. [Estados e Comportamentos](#estados-e-comportamentos)
6. [Validações Frontend](#validações-frontend)
7. [Fluxos de Interação](#fluxos-de-interação)
8. [Tratamento de Erros](#tratamento-de-erros)
9. [Casos de Teste](#casos-de-teste)

## 📊 Estrutura da Funcionalidade de Import

### Entidades e DTOs Implementados

```typescript
interface ImportAlarmsCommand {
  fileStream: File;           // Arquivo CSV a ser importado
  fileName: string;           // Nome do arquivo para validação
  userId: string;             // GUID do usuário (automático pela autenticação)
  overwriteExisting: boolean; // Se deve sobrescrever alarmes existentes
}

interface ImportAlarmsResponseDto {
  totalRecords: number;       // Total de registros processados
  successfulImports: number;  // Número de alarmes importados com sucesso
  failedImports: number;      // Número de alarmes que falharam
  updatedImports: number;     // Número de alarmes atualizados
  errors: ImportErrorDto[];   // Lista de erros encontrados
  importedAlarms: AlarmResponseDto[]; // Lista de alarmes importados
  isSuccess: boolean;         // Indica se foi totalmente bem-sucedida
  summary: string;            // Mensagem resumo da importação
}

interface ImportErrorDto {
  lineNumber?: number;        // Número da linha com erro (se aplicável)
  errorMessage: string;       // Mensagem de erro
  errorType: string;          // Tipo do erro: Validation, Parsing, Business, Processing, System
  errorCode: string;          // Código do erro
}

interface AlarmCsvRecord {
  name: string;               // Nome do alarme (obrigatório, 2-100 chars)
  time: string;              // Horário no formato HH:MM (obrigatório)
  daysOfWeek: string;        // Dias da semana separados por vírgula
  description?: string;       // Descrição opcional (ignorada no import)
  isActive: string;          // "true" ou "false" (opcional, padrão: true)
}
```

### Regras de Negócio Importantes

1. **Formato Suportado**: Apenas arquivos CSV (.csv)
2. **Tamanho Máximo**: 5MB por arquivo
3. **Estrutura CSV**: Cabeçalho obrigatório: `Name,Time,DaysOfWeek,Description,IsActive`
4. **Dias da Semana**: Suporte a português e inglês (segunda/monday, ter/tue, etc.)
5. **Duplicatas**: Por padrão gera erro, mas pode sobrescrever se `overwriteExisting=true`
6. **Validação**: Nome obrigatório, horário válido, dias da semana válidos

## 🔌 Endpoints da API

### Base URL

```text
https://localhost:5001/api/v1/alarms
```

### Autenticação

- **Tipo**: Bearer Token (JWT)
- **Header**: `Authorization: Bearer {token}`
- **Roles**: "Admin" ou "User"

### Endpoint de Import

| Método | Endpoint | Descrição | Auth Required | Roles |
|--------|----------|-----------|---------------|--------|
| POST | `/alarms/import` | Importar alarmes de arquivo CSV | ✅ | Admin, User |

### Parâmetros da Requisição

```typescript
// Multipart/form-data
interface ImportRequest {
  file: File;                    // Arquivo CSV (obrigatório)
  overwriteExisting: boolean;    // Sobrescrever existentes (opcional, padrão: false)
}
```

### Exemplo de Arquivo CSV

```csv
Name,Time,DaysOfWeek,Description,IsActive
Acordar,07:00,"segunda,terça,quarta,quinta,sexta",Alarme para acordar,true
Reunião,14:30,terça,Reunião importante,false
Exercício,18:00,"monday,tuesday,wednesday,thursday,friday",Hora do exercício,true
Medicamento,09:00,"seg,qua,sex",Tomar medicamento,true
```

### Códigos de Resposta HTTP

| Código | Descrição | Quando Ocorre |
|--------|-----------|---------------|
| 200 | Sucesso | Importação processada (mesmo com erros parciais) |
| 400 | Bad Request | Arquivo inválido, dados malformados |
| 401 | Unauthorized | Token inválido ou expirado |
| 413 | Payload Too Large | Arquivo maior que 5MB |
| 415 | Unsupported Media Type | Arquivo não é CSV |
| 500 | Internal Server Error | Erro interno do servidor |

## 🎨 Layout da Tela

### Estrutura Visual

```text
┌─────────────────────────────────────────────────────────────┐
│ 📤 Importação de Alarmes                      [❌ Fechar]  │
├─────────────────────────────────────────────────────────────┤
│ 📋 Instruções                                              │
│ • Formato: Apenas arquivos CSV (.csv)                      │
│ • Tamanho máximo: 5MB                                      │
│ • Estrutura: Name,Time,DaysOfWeek,Description,IsActive     │
│ • Exemplo: Acordar,07:00,"segunda,terça",Descrição,true    │
│                                                             │
│ 📁 Seleção de Arquivo                                      │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │     [📁] Clique para selecionar arquivo CSV             │ │
│ │          ou arraste e solte aqui                        │ │
│ │                                                         │ │
│ │     Formatos aceitos: .csv                              │ │
│ │     Tamanho máximo: 5MB                                 │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                             │
│ ⚙️ Opções de Importação                                     │
│ ☐ Sobrescrever alarmes existentes com mesmo nome           │
│                                                             │
│ 📤 Ações                                                    │
│ [📤 Importar] [📋 Baixar Exemplo] [❌ Cancelar]            │
├─────────────────────────────────────────────────────────────┤
│ 📊 Progresso (durante importação)                          │
│ ████████████████████░░░░ 80%                               │
│ Processando: linha 15 de 20...                             │
├─────────────────────────────────────────────────────────────┤
│ 📈 Resultado da Importação                                  │
│ ✅ Importação concluída!                                   │
│ • Total processado: 20 alarmes                             │
│ • Importados: 15 novos alarmes                             │
│ • Atualizados: 3 alarmes                                   │
│ • Falharam: 2 alarmes                                      │
│                                                             │
│ ❌ Erros Encontrados (2)                                   │
│ • Linha 8: Horário inválido '25:70'                        │
│ • Linha 12: Alarme 'Teste' já existe                       │
│                                                             │
│ [📋 Ver Detalhes] [✅ Concluir]                            │
└─────────────────────────────────────────────────────────────┘
```

### Responsividade

#### Desktop (≥ 1024px)

- Modal centralizado com largura fixa (600px)
- Área de drop bem definida
- Todos os elementos visíveis simultaneamente

#### Tablet (768px - 1023px)

- Modal ocupa 90% da largura da tela
- Elementos empilhados verticalmente
- Área de drop reduzida mas funcional

#### Mobile (< 768px)

- Modal em tela cheia
- Navegação por etapas (seleção → opções → resultado)
- Botões de ação em stack vertical

## 🧩 Componentes Necessários

### 1. AlarmImportModal (Modal Principal)

```typescript
interface AlarmImportModalProps {
  isOpen: boolean;
  onClose: () => void;
  onImportComplete?: (result: ImportAlarmsResponseDto) => void;
}

const AlarmImportModal: React.FC<AlarmImportModalProps> = ({
  isOpen,
  onClose,
  onImportComplete
}) => {
  // Estados principais
  const [currentStep, setCurrentStep] = useState<'upload' | 'processing' | 'result'>('upload');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [overwriteExisting, setOverwriteExisting] = useState(false);
  const [importResult, setImportResult] = useState<ImportAlarmsResponseDto | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);

  // Lógica do componente...
};
```

### 2. FileDropZone (Área de Upload)

```typescript
interface FileDropZoneProps {
  onFileSelect: (file: File) => void;
  acceptedFormats: string[];
  maxSizeInMB: number;
  selectedFile?: File | null;
  disabled?: boolean;
}

const FileDropZone: React.FC<FileDropZoneProps> = ({
  onFileSelect,
  acceptedFormats,
  maxSizeInMB,
  selectedFile,
  disabled = false
}) => {
  const [isDragOver, setIsDragOver] = useState(false);
  const [dragError, setDragError] = useState<string | null>(null);
  
  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
    
    const files = Array.from(e.dataTransfer.files);
    if (files.length > 1) {
      setDragError('Selecione apenas um arquivo');
      return;
    }
    
    const file = files[0];
    if (validateFile(file)) {
      onFileSelect(file);
      setDragError(null);
    }
  }, [onFileSelect]);

  // Mais lógica...
};
```

### 3. ImportOptions (Opções de Importação)

```typescript
interface ImportOptionsProps {
  overwriteExisting: boolean;
  onOverwriteChange: (value: boolean) => void;
  disabled?: boolean;
}

const ImportOptions: React.FC<ImportOptionsProps> = ({
  overwriteExisting,
  onOverwriteChange,
  disabled = false
}) => {
  return (
    <div className="import-options">
      <h3>⚙️ Opções de Importação</h3>
      <label className="checkbox-option">
        <input
          type="checkbox"
          checked={overwriteExisting}
          onChange={(e) => onOverwriteChange(e.target.checked)}
          disabled={disabled}
        />
        <span>Sobrescrever alarmes existentes com mesmo nome</span>
        <small>
          Se marcado, alarmes com nomes duplicados serão atualizados.
          Caso contrário, serão considerados erros.
        </small>
      </label>
    </div>
  );
};
```

### 4. ImportProgress (Barra de Progresso)

```typescript
interface ImportProgressProps {
  progress: number; // 0-100
  currentLine?: number;
  totalLines?: number;
  isVisible: boolean;
}

const ImportProgress: React.FC<ImportProgressProps> = ({
  progress,
  currentLine,
  totalLines,
  isVisible
}) => {
  if (!isVisible) return null;

  return (
    <div className="import-progress">
      <h3>📊 Progresso da Importação</h3>
      <div className="progress-bar">
        <div 
          className="progress-fill" 
          style={{ width: `${progress}%` }}
        />
      </div>
      <div className="progress-text">
        {progress}% - {currentLine && totalLines 
          ? `Processando linha ${currentLine} de ${totalLines}...`
          : 'Processando arquivo...'
        }
      </div>
    </div>
  );
};
```

### 5. ImportResult (Resultado da Importação)

```typescript
interface ImportResultProps {
  result: ImportAlarmsResponseDto;
  onShowDetails?: () => void;
  onComplete: () => void;
}

const ImportResult: React.FC<ImportResultProps> = ({
  result,
  onShowDetails,
  onComplete
}) => {
  const isSuccess = result.isSuccess;
  const hasErrors = result.errors.length > 0;

  return (
    <div className="import-result">
      <div className={`result-header ${isSuccess ? 'success' : 'warning'}`}>
        <span className="result-icon">
          {isSuccess ? '✅' : '⚠️'}
        </span>
        <h3>
          {isSuccess 
            ? 'Importação concluída com sucesso!' 
            : 'Importação concluída com avisos'
          }
        </h3>
      </div>

      <div className="result-summary">
        <div className="summary-stats">
          <div className="stat-item">
            <span className="stat-value">{result.totalRecords}</span>
            <span className="stat-label">Total processado</span>
          </div>
          <div className="stat-item success">
            <span className="stat-value">{result.successfulImports}</span>
            <span className="stat-label">Novos alarmes</span>
          </div>
          <div className="stat-item info">
            <span className="stat-value">{result.updatedImports}</span>
            <span className="stat-label">Atualizados</span>
          </div>
          <div className="stat-item error">
            <span className="stat-value">{result.failedImports}</span>
            <span className="stat-label">Falharam</span>
          </div>
        </div>
      </div>

      {hasErrors && (
        <ImportErrorsList 
          errors={result.errors}
          onShowDetails={onShowDetails}
        />
      )}

      <div className="result-actions">
        {onShowDetails && (
          <button 
            type="button" 
            className="btn-secondary"
            onClick={onShowDetails}
          >
            📋 Ver Detalhes
          </button>
        )}
        <button 
          type="button" 
          className="btn-primary"
          onClick={onComplete}
        >
          ✅ Concluir
        </button>
      </div>
    </div>
  );
};
```

### 6. ImportErrorsList (Lista de Erros)

```typescript
interface ImportErrorsListProps {
  errors: ImportErrorDto[];
  maxVisible?: number;
  onShowDetails?: () => void;
}

const ImportErrorsList: React.FC<ImportErrorsListProps> = ({
  errors,
  maxVisible = 5,
  onShowDetails
}) => {
  const visibleErrors = errors.slice(0, maxVisible);
  const hasMoreErrors = errors.length > maxVisible;

  return (
    <div className="import-errors">
      <h4>❌ Erros Encontrados ({errors.length})</h4>
      <ul className="error-list">
        {visibleErrors.map((error, index) => (
          <li key={index} className={`error-item ${error.errorType.toLowerCase()}`}>
            <span className="error-line">
              {error.lineNumber ? `Linha ${error.lineNumber}: ` : ''}
            </span>
            <span className="error-message">{error.errorMessage}</span>
            <span className="error-type">({error.errorType})</span>
          </li>
        ))}
      </ul>
      
      {hasMoreErrors && (
        <button 
          type="button" 
          className="show-more-errors"
          onClick={onShowDetails}
        >
          Ver todos os {errors.length} erros...
        </button>
      )}
    </div>
  );
};
```

### 7. CsvTemplateDownload (Download de Exemplo)

```typescript
interface CsvTemplateDownloadProps {
  onDownload: () => void;
}

const CsvTemplateDownload: React.FC<CsvTemplateDownloadProps> = ({
  onDownload
}) => {
  const generateTemplate = () => {
    const csvContent = `Name,Time,DaysOfWeek,Description,IsActive
Acordar,07:00,"segunda,terça,quarta,quinta,sexta",Alarme para acordar,true
Reunião Diária,09:00,"monday,tuesday,wednesday,thursday,friday",Reunião de equipe,true
Almoço,12:00,"seg,ter,qua,qui,sex",Hora do almoço,false
Exercício,18:00,"segunda,quarta,sexta",Treino na academia,true
Medicamento,21:00,"domingo,segunda,terça,quarta,quinta,sexta,sábado",Tomar medicação,true`;

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', 'exemplo-alarmes.csv');
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    onDownload();
  };

  return (
    <button 
      type="button" 
      className="btn-outline"
      onClick={generateTemplate}
    >
      📋 Baixar Exemplo CSV
    </button>
  );
};
```

## 🔄 Estados e Comportamentos

### Estados Principais

```typescript
// Estado do modal
const [isModalOpen, setIsModalOpen] = useState(false);

// Estados do fluxo de importação
const [currentStep, setCurrentStep] = useState<'upload' | 'processing' | 'result'>('upload');

// Estados do arquivo
const [selectedFile, setSelectedFile] = useState<File | null>(null);
const [fileError, setFileError] = useState<string | null>(null);

// Estados das opções
const [overwriteExisting, setOverwriteExisting] = useState(false);

// Estados do upload/processamento
const [isUploading, setIsUploading] = useState(false);
const [uploadProgress, setUploadProgress] = useState(0);
const [uploadError, setUploadError] = useState<string | null>(null);

// Estados do resultado
const [importResult, setImportResult] = useState<ImportAlarmsResponseDto | null>(null);
const [showErrorDetails, setShowErrorDetails] = useState(false);
```

### Comportamentos de Loading

1. **Seleção de Arquivo**: Loading spinner durante validação
2. **Upload**: Progress bar com percentual e linha atual
3. **Processamento**: Indicador de processamento do servidor
4. **Resultado**: Transição suave para tela de resultado

### Comportamentos de Drag & Drop

```typescript
const handleDragEnter = (e: React.DragEvent) => {
  e.preventDefault();
  setIsDragOver(true);
};

const handleDragLeave = (e: React.DragEvent) => {
  e.preventDefault();
  // Verificar se realmente saiu da área
  if (!e.currentTarget.contains(e.relatedTarget as Node)) {
    setIsDragOver(false);
  }
};

const handleDrop = (e: React.DragEvent) => {
  e.preventDefault();
  setIsDragOver(false);
  
  const files = Array.from(e.dataTransfer.files);
  if (files.length === 1) {
    handleFileSelection(files[0]);
  } else {
    setFileError('Selecione apenas um arquivo');
  }
};
```

## ✅ Validações Frontend

### Validação de Arquivo

```typescript
const validateFile = (file: File): { isValid: boolean; error?: string } => {
  // Verificar extensão
  const allowedExtensions = ['.csv'];
  const fileExtension = file.name.toLowerCase().substring(file.name.lastIndexOf('.'));
  
  if (!allowedExtensions.includes(fileExtension)) {
    return { 
      isValid: false, 
      error: `Formato não suportado. Use: ${allowedExtensions.join(', ')}` 
    };
  }
  
  // Verificar tamanho (5MB)
  const maxSizeInBytes = 5 * 1024 * 1024;
  if (file.size > maxSizeInBytes) {
    return { 
      isValid: false, 
      error: `Arquivo muito grande. Máximo: 5MB. Atual: ${(file.size / 1024 / 1024).toFixed(2)}MB` 
    };
  }
  
  // Verificar se o arquivo não está vazio
  if (file.size === 0) {
    return { 
      isValid: false, 
      error: 'Arquivo está vazio' 
    };
  }
  
  return { isValid: true };
};
```

### Validação Prévia do Conteúdo (Opcional)

```typescript
const previewCsvContent = async (file: File): Promise<{ 
  isValid: boolean; 
  preview?: string[]; 
  error?: string 
}> => {
  try {
    const text = await file.text();
    const lines = text.split('\n').filter(line => line.trim());
    
    if (lines.length < 2) {
      return { 
        isValid: false, 
        error: 'Arquivo deve ter pelo menos um cabeçalho e uma linha de dados' 
      };
    }
    
    const header = lines[0];
    const expectedHeaders = ['Name', 'Time', 'DaysOfWeek', 'Description', 'IsActive'];
    const actualHeaders = header.split(',').map(h => h.trim().replace(/"/g, ''));
    
    const missingHeaders = expectedHeaders.filter(h => !actualHeaders.includes(h));
    if (missingHeaders.length > 0) {
      return { 
        isValid: false, 
        error: `Cabeçalhos ausentes: ${missingHeaders.join(', ')}` 
      };
    }
    
    return { 
      isValid: true, 
      preview: lines.slice(0, 6) // Mostrar até 5 linhas de dados + cabeçalho
    };
  } catch (error) {
    return { 
      isValid: false, 
      error: 'Erro ao ler arquivo' 
    };
  }
};
```

## 🎭 Fluxos de Interação

### 1. Fluxo Principal de Importação

```
1. Usuário clica em [📤 Importar Alarmes]
2. Modal de importação é aberto
3. Usuário seleciona arquivo CSV:
   a. Via clique no botão de seleção
   b. Via drag & drop na área designada
4. Sistema valida arquivo:
   - Formato (.csv)
   - Tamanho (≤ 5MB)
   - Estrutura básica (opcional)
5. Se válido: Mostra preview e opções
6. Se inválido: Mostra erro e permite nova seleção
7. Usuário configura opções:
   - ☐ Sobrescrever alarmes existentes
8. Usuário clica em [📤 Importar]
9. Sistema inicia upload com progress bar
10. Servidor processa arquivo e retorna resultado
11. Sistema mostra resultado da importação:
    - Estatísticas de sucesso/erro
    - Lista de erros (se houver)
    - Lista de alarmes importados
12. Usuário pode:
    - Ver detalhes dos erros
    - Baixar relatório
    - Concluir e fechar modal
```

### 2. Fluxo de Download de Exemplo

```
1. Usuário clica em [📋 Baixar Exemplo CSV]
2. Sistema gera arquivo CSV de exemplo
3. Browser inicia download automaticamente
4. Arquivo salvo como "exemplo-alarmes.csv"
5. Usuário pode usar como template
```

### 3. Fluxo de Tratamento de Erros

```
1. Erro de validação de arquivo:
   - Mostra mensagem específica
   - Mantém área de upload ativa
   - Permite nova seleção

2. Erro de upload/rede:
   - Mostra toast de erro
   - Oferece botão "Tentar Novamente"
   - Mantém dados do formulário

3. Erro de processamento:
   - Mostra erros linha por linha
   - Permite download de relatório
   - Oferece opção de nova tentativa

4. Erro de autorização:
   - Redirect para login
   - Preserva estado para retorno
```

## 🚫 Tratamento de Erros

### Tipos de Erro e Respostas

```typescript
const handleApiError = (error: any): string => {
  switch (error.status) {
    case 400:
      return 'Arquivo inválido ou dados malformados. Verifique o formato e tente novamente.';
    case 401:
      // Redirect para login
      redirectToLogin();
      return 'Sessão expirada. Redirecionando para login...';
    case 413:
      return 'Arquivo muito grande. O tamanho máximo permitido é 5MB.';
    case 415:
      return 'Formato de arquivo não suportado. Use apenas arquivos CSV (.csv).';
    case 500:
      return 'Erro interno do servidor. Tente novamente em alguns instantes.';
    default:
      return 'Erro inesperado durante a importação. Verifique sua conexão e tente novamente.';
  }
};
```

### Categorização de Erros da API

```typescript
const getErrorSeverity = (errorType: string): 'error' | 'warning' | 'info' => {
  switch (errorType) {
    case 'System':
    case 'Parsing':
      return 'error';
    case 'Business':
    case 'Validation':
      return 'warning';
    case 'Processing':
    default:
      return 'info';
  }
};

const getErrorIcon = (errorType: string): string => {
  switch (errorType) {
    case 'System': return '🔥';
    case 'Parsing': return '📄';
    case 'Business': return '⚠️';
    case 'Validation': return '✏️';
    case 'Processing': return 'ℹ️';
    default: return '❌';
  }
};
```

### Estados de Erro na UI

1. **Erro de Seleção**: Mensagem inline na área de drop
2. **Erro de Upload**: Toast com opção de retry
3. **Erro de Processamento**: Lista detalhada no resultado
4. **Erro de Rede**: Banner com opção de reconectar

## 🎯 Casos de Teste

### Cenários de Teste Obrigatórios

#### 1. Testes de Upload de Arquivo

```typescript
describe('Upload de Arquivo', () => {
  test('Deve aceitar arquivo CSV válido', async () => {
    // 1. Abrir modal de importação
    // 2. Selecionar arquivo .csv válido
    // 3. Verificar se arquivo foi aceito
    // 4. Verificar se botão importar está habilitado
  });

  test('Deve rejeitar arquivo com extensão inválida', async () => {
    // 1. Tentar selecionar arquivo .txt
    // 2. Verificar mensagem de erro
    // 3. Verificar se botão importar está desabilitado
  });

  test('Deve rejeitar arquivo muito grande', async () => {
    // 1. Simular arquivo > 5MB
    // 2. Verificar mensagem de erro de tamanho
    // 3. Verificar se upload foi impedido
  });

  test('Deve funcionar com drag and drop', async () => {
    // 1. Simular arrastar arquivo para área de drop
    // 2. Verificar se arquivo foi aceito
    // 3. Verificar feedback visual
  });
});
```

#### 2. Testes de Processamento

```typescript
describe('Processamento de Importação', () => {
  test('Deve mostrar progresso durante upload', async () => {
    // 1. Iniciar importação
    // 2. Verificar se progress bar aparece
    // 3. Verificar se percentual é atualizado
    // 4. Verificar se mensagem de status é exibida
  });

  test('Deve processar importação com sucesso total', async () => {
    // 1. Importar arquivo válido sem duplicatas
    // 2. Verificar estatísticas finais
    // 3. Verificar que não há erros
    // 4. Verificar lista de alarmes importados
  });

  test('Deve processar importação com erros parciais', async () => {
    // 1. Importar arquivo com alguns erros
    // 2. Verificar estatísticas mistas
    // 3. Verificar lista de erros
    // 4. Verificar que sucessos foram importados
  });

  test('Deve tratar opção sobrescrever existentes', async () => {
    // 1. Importar arquivo com alarmes duplicados
    // 2. Testar com opção desmarcada (deve dar erro)
    // 3. Testar com opção marcada (deve atualizar)
  });
});
```

#### 3. Testes de Tratamento de Erros

```typescript
describe('Tratamento de Erros', () => {
  test('Deve mostrar erro de rede adequadamente', async () => {
    // 1. Simular falha de rede durante upload
    // 2. Verificar mensagem de erro
    // 3. Verificar botão de retry
    // 4. Testar funcionalidade de retry
  });

  test('Deve mostrar erros de validação linha por linha', async () => {
    // 1. Importar arquivo com erros de validação
    // 2. Verificar lista de erros detalhada
    // 3. Verificar números de linha
    // 4. Verificar tipos de erro
  });

  test('Deve permitir visualizar detalhes de erros', async () => {
    // 1. Importar arquivo com muitos erros
    // 2. Verificar resumo de erros
    // 3. Clicar em "Ver detalhes"
    // 4. Verificar modal/painel de detalhes
  });
});
```

#### 4. Testes de UX e Acessibilidade

```typescript
describe('UX e Acessibilidade', () => {
  test('Deve ser navegável por teclado', async () => {
    // 1. Navegar por todos os elementos usando Tab
    // 2. Ativar botões usando Enter/Space
    // 3. Verificar foco visual
    // 4. Testar navegação com screen reader
  });

  test('Deve ser responsivo em diferentes tamanhos', async () => {
    // 1. Testar em desktop (1024px+)
    // 2. Testar em tablet (768px-1023px)
    // 3. Testar em mobile (<768px)
    // 4. Verificar usabilidade em cada tamanho
  });

  test('Deve fornecer feedback adequado', async () => {
    // 1. Verificar loading states
    // 2. Verificar mensagens de sucesso
    // 3. Verificar mensagens de erro
    // 4. Verificar estados vazios
  });
});
```

## 📱 Comportamentos Específicos

### Feedback Visual Durante Upload

```typescript
const getUploadStatusIcon = (status: 'idle' | 'uploading' | 'success' | 'error') => {
  switch (status) {
    case 'idle': return '📁';
    case 'uploading': return '⏳';
    case 'success': return '✅';
    case 'error': return '❌';
  }
};

const getUploadStatusText = (status: 'idle' | 'uploading' | 'success' | 'error', progress?: number) => {
  switch (status) {
    case 'idle': return 'Clique para selecionar arquivo CSV';
    case 'uploading': return `Enviando... ${progress || 0}%`;
    case 'success': return 'Arquivo carregado com sucesso';
    case 'error': return 'Erro no upload. Tente novamente';
  }
};
```

### Indicadores de Progresso

1. **Seleção de Arquivo**:
   - Ícone: 📁 → ⏳ → ✅/❌
   - Texto: Descritivo do status atual

2. **Upload**:
   - Progress bar: 0% → 100%
   - Linha atual: "Processando linha X de Y"
   - ETA: Estimativa de tempo restante

3. **Processamento**:
   - Spinner: Durante processamento no servidor
   - Estatísticas: Atualizadas em tempo real

### Persistência de Estado

```typescript
// Salvar progresso em caso de erro
const saveImportState = (state: ImportState) => {
  sessionStorage.setItem('import-state', JSON.stringify(state));
};

// Recuperar estado após erro/refresh
const recoverImportState = (): ImportState | null => {
  const saved = sessionStorage.getItem('import-state');
  return saved ? JSON.parse(saved) : null;
};

// Limpar estado após sucesso
const clearImportState = () => {
  sessionStorage.removeItem('import-state');
};
```

## 🎨 Guia de Estilo

### Cores (seguir design system)

```css
/* Cores principais */
--import-primary: #3b82f6;      /* Azul para ações primárias */
--import-success: #10b981;      /* Verde para sucessos */
--import-warning: #f59e0b;      /* Amarelo para avisos */
--import-error: #ef4444;        /* Vermelho para erros */
--import-info: #6366f1;         /* Roxo para informações */

/* Estados de upload */
--upload-idle: #f3f4f6;         /* Cinza claro para área inativa */
--upload-hover: #e5e7eb;        /* Cinza médio para hover */
--upload-active: #dbeafe;       /* Azul claro para drag over */
--upload-error: #fef2f2;        /* Rosa claro para erro */

/* Progress bar */
--progress-bg: #f3f4f6;
--progress-fill: #3b82f6;
--progress-success: #10b981;
--progress-error: #ef4444;
```

### Animações

```css
/* Drag & drop feedback */
.drop-zone {
  transition: all 0.2s ease-in-out;
}

.drop-zone.drag-over {
  transform: scale(1.02);
  box-shadow: 0 8px 25px rgba(59, 130, 246, 0.15);
}

/* Progress bar animation */
.progress-fill {
  transition: width 0.3s ease-out;
}

/* Result fade-in */
.import-result {
  animation: fadeInUp 0.5s ease-out;
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
```

### Ícones e Símbolos

```typescript
const ImportIcons = {
  upload: '📤',
  file: '📁',
  csv: '📊',
  success: '✅',
  error: '❌',
  warning: '⚠️',
  info: 'ℹ️',
  processing: '⏳',
  download: '📋',
  close: '❌',
  settings: '⚙️'
} as const;
```

## 🔧 Implementação Técnica

### Service para API

```typescript
class AlarmImportService {
  private baseUrl = '/api/v1/alarms';

  async importAlarms(
    file: File, 
    overwriteExisting: boolean = false,
    onProgress?: (progress: number) => void
  ): Promise<ImportAlarmsResponseDto> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('overwriteExisting', overwriteExisting.toString());

    const xhr = new XMLHttpRequest();
    
    return new Promise((resolve, reject) => {
      xhr.upload.addEventListener('progress', (e) => {
        if (e.lengthComputable && onProgress) {
          const progress = Math.round((e.loaded / e.total) * 100);
          onProgress(progress);
        }
      });

      xhr.addEventListener('load', () => {
        if (xhr.status >= 200 && xhr.status < 300) {
          try {
            const result = JSON.parse(xhr.responseText);
            resolve(result);
          } catch (error) {
            reject(new Error('Erro ao processar resposta do servidor'));
          }
        } else {
          reject(new Error(`HTTP ${xhr.status}: ${xhr.statusText}`));
        }
      });

      xhr.addEventListener('error', () => {
        reject(new Error('Erro de rede durante o upload'));
      });

      xhr.open('POST', `${this.baseUrl}/import`);
      xhr.setRequestHeader('Authorization', `Bearer ${this.getAuthToken()}`);
      xhr.send(formData);
    });
  }

  private getAuthToken(): string {
    return localStorage.getItem('authToken') || '';
  }

  downloadTemplate(): void {
    const csvContent = `Name,Time,DaysOfWeek,Description,IsActive
Acordar,07:00,"segunda,terça,quarta,quinta,sexta",Alarme para acordar,true
Reunião,14:30,terça,Reunião importante,false
Exercício,18:00,"monday,tuesday,wednesday,thursday,friday",Hora do exercício,true
Medicamento,09:00,"seg,qua,sex",Tomar medicamento,true`;

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'exemplo-alarmes.csv';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }
}
```

### Hook Customizado

```typescript
const useAlarmImport = () => {
  const [isImporting, setIsImporting] = useState(false);
  const [progress, setProgress] = useState(0);
  const [result, setResult] = useState<ImportAlarmsResponseDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  const importService = useMemo(() => new AlarmImportService(), []);

  const importAlarms = useCallback(async (
    file: File, 
    overwriteExisting: boolean = false
  ) => {
    try {
      setIsImporting(true);
      setProgress(0);
      setError(null);
      setResult(null);

      const result = await importService.importAlarms(
        file, 
        overwriteExisting,
        setProgress
      );

      setResult(result);
      return result;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Erro desconhecido';
      setError(errorMessage);
      throw err;
    } finally {
      setIsImporting(false);
    }
  }, [importService]);

  const downloadTemplate = useCallback(() => {
    importService.downloadTemplate();
  }, [importService]);

  const reset = useCallback(() => {
    setProgress(0);
    setResult(null);
    setError(null);
  }, []);

  return {
    importAlarms,
    downloadTemplate,
    reset,
    isImporting,
    progress,
    result,
    error
  };
};
```

### Exemplo de Uso

```typescript
const AlarmManagementPage: React.FC = () => {
  const [showImportModal, setShowImportModal] = useState(false);
  const { refreshAlarms } = useAlarms(); // Hook existente para listar alarmes

  const handleImportComplete = (result: ImportAlarmsResponseDto) => {
    // Mostrar toast de sucesso
    if (result.isSuccess) {
      toast.success(`${result.successfulImports} alarmes importados com sucesso!`);
    } else {
      toast.warning(`Importação concluída com ${result.failedImports} erros.`);
    }

    // Atualizar lista de alarmes
    refreshAlarms();
    
    // Fechar modal
    setShowImportModal(false);
  };

  return (
    <div className="alarm-management">
      {/* Outros componentes da página */}
      
      <div className="page-actions">
        <button 
          className="btn-primary"
          onClick={() => setShowImportModal(true)}
        >
          📤 Importar Alarmes
        </button>
      </div>

      <AlarmImportModal
        isOpen={showImportModal}
        onClose={() => setShowImportModal(false)}
        onImportComplete={handleImportComplete}
      />
    </div>
  );
};
```

## 📋 Checklist de Implementação

### Fase 1: Estrutura Base

- [ ] Criar componente `AlarmImportModal`
- [ ] Implementar `AlarmImportService`
- [ ] Criar hook `useAlarmImport`
- [ ] Configurar tipos TypeScript
- [ ] Implementar layout responsivo

### Fase 2: Upload de Arquivos

- [ ] Componente `FileDropZone`
- [ ] Validação de arquivos
- [ ] Drag & drop funcional
- [ ] Preview de arquivo selecionado
- [ ] Estados de loading

### Fase 3: Processamento

- [ ] Componente `ImportProgress`
- [ ] Progress bar com upload real
- [ ] Estados de processamento
- [ ] Tratamento de cancelamento
- [ ] Feedback visual adequado

### Fase 4: Resultado e Erros

- [ ] Componente `ImportResult`
- [ ] Componente `ImportErrorsList`
- [ ] Categorização de erros
- [ ] Detalhamento de erros
- [ ] Ações pós-importação

### Fase 5: Funcionalidades Extras

- [ ] Download de template CSV
- [ ] Persistência de estado
- [ ] Retry automático
- [ ] Relatórios de importação
- [ ] Histórico de importações

### Fase 6: Testes e Polimento

- [ ] Testes unitários dos componentes
- [ ] Testes de integração da API
- [ ] Testes e2e do fluxo completo
- [ ] Testes de acessibilidade
- [ ] Otimizações de performance

---

## 📞 Suporte

**Para dúvidas técnicas:**

- Consulte o código do `ImportAlarmsHandler` e `CsvFileParser`
- Testes HTTP disponíveis no endpoint `/alarms/import`
- Exemplos de uso nos testes de integração

**Funcionalidade de Importação está 100% funcional e testada!** 🎉

**Status da API:**

- ✅ CRUD completo implementado
- ✅ FileParser para CSV funcional
- ✅ Validações robustas
- ✅ Testes unitários e de integração
- ✅ Tratamento de erros detalhado
- ✅ Support a múltiplos idiomas para dias da semana

---

*Documento gerado em: 16/07/2025*
*Versão da API: v1*
*Backend Status: ✅ Funcional*
