# 📊 Especificação da Tela de Estatísticas e Analytics — Smart Alarm

## 📋 **Template Base de Referência**

**🎨 Base Template**: [Horizon UI Tailwind React](https://react-themes.com/product/horizon-tailwind-react)  
**🔗 Live Preview**: [Charts Dashboard](https://horizon-ui.com/horizon-tailwind-react/admin/default)  
**🎯 Adaptações**: Dashboard de analytics com gráficos interativos e métricas

### **Componentes do Template a Adaptar**

- ✅ **Chart Components**: Gráficos de linha, barras e pizza personalizados
- ✅ **Metric Cards**: Cards de estatísticas com indicadores visuais
- ✅ **Time Range Filters**: Controles de período temporal
- ✅ **Data Comparison**: Comparações entre períodos
- ✅ **Export Options**: Funcionalidades de exportação de relatórios
- ✅ **Interactive Legends**: Legendas interativas para gráficos

---

## 🎯 Objetivo

A tela de estatísticas e analytics é o centro de insights do Smart Alarm, oferecendo visualizações acessíveis e inteligentes sobre padrões de uso, eficácia dos alarmes, tendências de saúde e recomendações personalizadas. Deve transformar dados em insights acionáveis para usuários neurodivergentes, utilizando IA do ML.NET para análises preditivas, com foco em acessibilidade de gráficos e dados, exportação flexível e apresentação clara de métricas de bem-estar.

---

## 🎨 Estrutura Visual

### Layout Principal - Dashboard Analytics (Desktop ≥1024px)

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ 🔔 Smart Alarm                    📊 Analytics     👤 João Silva  [⚙️] [📤] │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ ← Dashboard    📊 Estatísticas e Insights          [📅 30 dias ⬇️] [📊 Tipo]│
│                                                                             │
│ ┌─────────────────┬─────────────────┬─────────────────┬─────────────────┐   │
│ │ 🎯 EFICÁCIA     │ ⏰ PONTUALIDADE │ 🧘 BEM-ESTAR   │ 📈 TENDÊNCIAS   │   │
│ │                 │                 │                 │                 │   │
│ │ 94.2%          │ 87.5%          │ Melhorando     │ ↗️ +12% mês     │   │
│ │ ██████████░    │ ████████░░     │ 😊 Bom humor   │ 📊 Ver detalhes │   │
│ │                 │                 │                 │                 │   │
│ │ ✅ 145 sucessos │ ⚡ Média: 2.3m  │ 🌟 Streak: 7d  │ 🤖 AI Insights │   │
│ │ ❌ 9 perdidos   │ 🐌 Mais lento: │ 😴 Sono: 8.2h  │ disponíveis     │   │
│ │                 │    Manhã (5.2m) │                 │                 │   │
│ └─────────────────┴─────────────────┴─────────────────┴─────────────────┘   │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ 📊 PADRÕES DE USO - Últimos 30 dias                                │     │
│ │                                                                     │     │
│ │ 🗓️ Por Dia da Semana                                               │     │
│ │ ┌─────────────────────────────────────────────────────────────┐     │     │
│ │ │        ██████  ████████  ██████  ████  ██████  ████  ████  │     │     │
│ │ │ Dom    Seg     Ter      Qua     Qui    Sex     Sáb    Total │     │     │
│ │ │ 12     18      16       14      15     13      8      96    │     │     │
│ │ └─────────────────────────────────────────────────────────────┘     │     │
│ │                                                                     │     │
│ │ ⏰ Por Período do Dia                                               │     │
│ │ ┌─────────────────────────────────────────────────────────────┐     │     │
│ │ │ 🌅 Manhã (6-12h): ████████████  65 alarmes (68%)           │     │     │
│ │ │ 🌞 Tarde (12-18h): ████████  23 alarmes (24%)               │     │     │
│ │ │ 🌙 Noite (18-24h): ████  8 alarmes (8%)                     │     │     │
│ │ └─────────────────────────────────────────────────────────────┘     │     │
│ │                                                                     │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ 🤖 INSIGHTS DA IA                          [🔄 Gerar Novos Insights]│     │
│ │                                                                     │     │
│ │ 💡 **Padrão Identificado**: Você tem 23% mais sucesso com         │     │
│ │    alarmes de medicamento às terças e quintas-feiras               │     │
│ │                                                                     │     │
│ │ 📅 **Recomendação**: Considere agendar consultas médicas           │     │
│ │    preferencialmente às sextas, quando sua pontualidade é 95%+     │     │
│ │                                                                     │     │
│ │ ⚠️  **Atenção**: Detectamos queda na eficácia após 20h.           │     │
│ │    Sugestão: Ativar modo soneca extra para alarmes noturnos        │     │
│ │                                                                     │     │
│ │ [✅ Aplicar Sugestão] [🤔 Mais Detalhes] [❌ Não é Relevante]      │     │
│ │                                                                     │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Layout Mobile - Cards Empilháveis (<768px)

```text
┌─────────────────────────────────┐
│ 🔔 Smart Alarm        [☰] [👤] │
├─────────────────────────────────┤
│ ← Voltar    📊 Estatísticas     │
│                                 │
│ [📅 7 dias ⬇️] [📊 Gráficos ⬇️] │
│                                 │
│ ┌─────────────────────────────┐ │
│ │ 🎯 EFICÁCIA GERAL           │ │
│ │                             │ │
│ │ 94.2%                       │ │
│ │ ██████████░                 │ │
│ │                             │ │
│ │ ✅ 34 sucessos              │ │
│ │ ❌ 2 perdidos               │ │
│ │ 📊 Ver detalhes             │ │
│ └─────────────────────────────┘ │
│                                 │
│ ┌─────────────────────────────┐ │
│ │ ⏰ PONTUALIDADE             │ │
│ │                             │ │
│ │ Tempo médio: 2.3 min        │ │
│ │ ⚡ Mais rápido: Medicamento │ │
│ │ 🐌 Mais lento: Exercício    │ │
│ │                             │ │
│ │ [Ver gráfico completo]      │ │
│ └─────────────────────────────┘ │
│                                 │
│ ┌─────────────────────────────┐ │
│ │ 🤖 INSIGHT IA               │ │
│ │                             │ │
│ │ 💡 Você é 25% mais pontual  │ │
│ │ nos finais de semana        │ │
│ │                             │ │
│ │ [✅ Legal] [🤔 Detalhes]    │ │
│ └─────────────────────────────┘ │
│                                 │
│ [📊 Ver Todos os Gráficos]      │
│ [📤 Exportar Relatório]         │
│                                 │
└─────────────────────────────────┘
```

---

## 📱 Estados da Tela

### 1. **Estado de Loading com Skeleton**

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ 📊 Estatísticas - Carregando dados...                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ ┌─────────────────┬─────────────────┬─────────────────┬─────────────────┐   │
│ │ ░░░░░░░░░░░    │ ░░░░░░░░░░░    │ ░░░░░░░░░░░    │ ░░░░░░░░░░░    │   │
│ │                 │                 │                 │                 │   │
│ │ ░░.░%          │ ░░.░%          │ ░░░░░░░░░░░    │ ░░░░░░░░░░░    │   │
│ │ ░░░░░░░░░░░    │ ░░░░░░░░░░░    │ ░░░░░░░░░░░    │ ░░░░░░░░░░░    │   │
│ │                 │                 │                 │                 │   │
│ │ ░░ ░░░ ░░░░░░  │ ░░ ░░░░░░ ░░░  │ ░░ ░░░░░░ ░░   │ ░░░░░░░░░░░    │   │
│ │ ░░ ░ ░░░░░░░   │ ░░ ░░░░░░░     │                 │                 │   │
│ └─────────────────┴─────────────────┴─────────────────┴─────────────────┘   │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░     │     │
│ │                                                                     │     │
│ │ ░░░░░░░░░░░░░░░░░░░                                                 │     │
│ │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░     │     │
│ │                                                                     │     │
│ │ 🔄 Analisando seus dados com IA...                                 │     │
│ │ ⏳ Isso pode levar alguns segundos                                  │     │
│ │                                                                     │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░     │     │
│ │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░     │     │
│ │                                                                     │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2. **Estado Sem Dados Suficientes**

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ 📊 Estatísticas - Dados Insuficientes                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │                           📊 👋                                     │     │
│ │                                                                     │     │
│ │                     Olá! Bem-vindo às suas                         │     │
│ │                      estatísticas do Smart Alarm                   │     │
│ │                                                                     │     │
│ │ Para gerar insights personalizados e gráficos detalhados,           │     │
│ │ precisamos de pelo menos **7 dias** de dados de alarmes.            │     │
│ │                                                                     │     │
│ │ 📈 **Progresso atual**: 3/7 dias                                    │     │
│ │ ████████░░░░░░░░░░░░░░░░░░░░░                                        │     │
│ │                                                                     │     │
│ │ 🎯 **Continue usando o Smart Alarm!**                               │     │
│ │ Em breve você terá acesso a:                                        │     │
│ │                                                                     │     │
│ │ ✨ Insights de IA sobre seus padrões                                │     │
│ │ 📊 Gráficos detalhados de eficácia                                  │     │
│ │ 🎯 Recomendações personalizadas                                     │     │
│ │ 📅 Análises de tendências temporais                                 │     │
│ │                                                                     │     │
│ │ [🔔 Criar Novo Alarme] [📚 Dicas de Uso]                           │     │
│ │                                                                     │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ 📊 **Dados Disponíveis Atualmente**                                │     │
│ │                                                                     │     │
│ │ • Alarmes criados: 5                                               │     │
│ │ • Alarmes ativados: 8                                              │     │
│ │ • Eficácia parcial: 87.5%                                          │     │
│ │                                                                     │     │
│ │ [📈 Ver Resumo Básico]                                              │     │
│ │                                                                     │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3. **Estado de Drill-down - Detalhamento de Métrica**

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ ← Voltar para Overview    📊 Eficácia de Alarmes - Detalhes               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ 🎯 **Eficácia Geral**: 94.2% (145/154 alarmes)          [📅 30 dias ⬇️]   │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ 📊 EFICÁCIA POR CATEGORIA                                           │     │
│ │                                                                     │     │
│ │ 💊 Medicamentos    ████████████████████░ 98.2% (55/56)            │     │
│ │ 🏃 Exercícios      ████████████████░░░░░ 89.3% (25/28)            │     │
│ │ 💼 Trabalho        ███████████████████░░ 96.7% (29/30)            │     │
│ │ 🍽️ Refeições       ████████████░░░░░░░░░ 75.0% (18/24)            │     │
│ │ 🧘 Relaxamento     ██████████████████░░░ 93.8% (15/16)            │     │
│ │ 🎯 Outros          ████████░░░░░░░░░░░░░░ 50.0% (3/6)              │     │
│ │                                                                     │     │
│ │ 💡 **Insight**: Refeições têm menor eficácia - considere          │     │
│ │    alarmes mais suaves ou lembretes visuais adicionais             │     │
│ │                                                                     │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ 📈 TENDÊNCIA TEMPORAL (Últimos 30 dias)                            │     │
│ │                                                                     │     │
│ │ 100%│                                                               │     │
│ │     │    ●●●●●                                                     │     │
│ │  95%│ ●●●     ●●●●●●●                                              │     │
│ │     │                ●●●●●                                         │     │
│ │  90%│                     ●●●●●●●●●●                               │     │
│ │     │                              ●●●●●                          │     │
│ │  85%├─────────────────────────────────────────────────────────────│     │
│ │     │  5    10    15    20    25    30 (dias)                     │     │
│ │                                                                     │     │
│ │ 📊 Média móvel (7 dias): 92.1%                                     │     │
│ │ 📈 Tendência: Leve melhora (+2.3% em 30 dias)                      │     │
│ │                                                                     │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ 🕐 ANÁLISE DE HORÁRIOS                                              │     │
│ │                                                                     │     │
│ │ **Melhor performance**: 06:00-09:00 (98.5% eficácia)               │     │
│ │ **Pior performance**: 21:00-23:00 (76.2% eficácia)                 │     │
│ │                                                                     │     │
│ │ 🤖 **Recomendação IA**: Considere reagendar alarmes não-críticos   │     │
│ │ do período noturno para manhã seguinte quando possível              │     │
│ │                                                                     │     │
│ │ [✅ Aplicar Otimização] [📅 Reagendar Manualmente]                 │     │
│ │                                                                     │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🧩 Componentes Detalhados

### 1. **MetricCard**

**Responsabilidade**: Card individual de métrica com visualização acessível

**Props TypeScript**:

```typescript
interface MetricCardProps {
  metric: MetricData;
  size: 'small' | 'medium' | 'large';
  interactive: boolean;
  onDrillDown?: () => void;
  isLoading?: boolean;
  error?: string;
}

interface MetricData {
  id: string;
  title: string;
  value: number | string;
  unit?: string;
  progress?: number; // 0-100
  trend?: {
    direction: 'up' | 'down' | 'stable';
    percentage: number;
    period: string;
  };
  icon: string;
  color: 'success' | 'warning' | 'error' | 'info';
  description?: string;
  subMetrics?: SubMetric[];
}

interface SubMetric {
  label: string;
  value: string | number;
  unit?: string;
}
```

**Funcionalidades**:

- Indicação visual de tendência
- Progress bar acessível
- Drill-down interativo
- Skeleton loading state
- Descrição alt para screen readers
- Keyboard navigation

### 2. **AccessibleChart**

**Responsabilidade**: Gráficos otimizados para acessibilidade

**Props TypeScript**:

```typescript
interface AccessibleChartProps {
  data: ChartDataPoint[];
  type: 'line' | 'bar' | 'pie' | 'area';
  title: string;
  description: string;
  showDataTable?: boolean;
  colorBlindFriendly?: boolean;
  highContrast?: boolean;
  annotations?: ChartAnnotation[];
  onDataPointClick?: (point: ChartDataPoint) => void;
}

interface ChartDataPoint {
  x: string | number | Date;
  y: number;
  label?: string;
  color?: string;
  metadata?: Record<string, any>;
}

interface ChartAnnotation {
  type: 'point' | 'range' | 'line';
  data: any;
  content: string;
  important?: boolean;
}
```

**Funcionalidades**:

- Alt text dinâmico com dados
- Tabela de dados alternativa
- Navegação por teclado entre pontos
- Suporte a patterns para daltonismo
- Zoom e pan acessíveis
- Sonificação opcional dos dados

### 3. **AIInsightPanel**

**Responsabilidade**: Painel de insights gerados por IA

**Props TypeScript**:

```typescript
interface AIInsightPanelProps {
  insights: AIInsight[];
  isLoading: boolean;
  onRefreshInsights: () => void;
  onApplyRecommendation: (insightId: string, action: string) => void;
  onFeedback: (insightId: string, feedback: InsightFeedback) => void;
}

interface AIInsight {
  id: string;
  type: 'pattern' | 'recommendation' | 'warning' | 'achievement';
  confidence: number; // 0-100
  title: string;
  description: string;
  evidenceData: AnalyticsData[];
  recommendations?: AIRecommendation[];
  category: AlarmCategory;
  priority: 'low' | 'medium' | 'high';
  createdAt: Date;
}

interface AIRecommendation {
  id: string;
  action: string;
  description: string;
  estimatedImpact: string;
  difficulty: 'easy' | 'medium' | 'hard';
  autoApplicable: boolean;
}

interface InsightFeedback {
  helpful: boolean;
  applied: boolean;
  notes?: string;
}
```

### 4. **ExportDialog**

**Responsabilidade**: Exportação flexível de dados e relatórios

**Props TypeScript**:

```typescript
interface ExportDialogProps {
  isOpen: boolean;
  onClose: () => void;
  availableData: ExportDataType[];
  onExport: (config: ExportConfig) => Promise<void>;
}

interface ExportConfig {
  dataTypes: ExportDataType[];
  format: 'csv' | 'json' | 'pdf' | 'xlsx';
  dateRange: DateRange;
  includeCharts: boolean;
  includeInsights: boolean;
  privacy: {
    anonymize: boolean;
    excludePersonalData: boolean;
  };
}

type ExportDataType = 
  | 'alarms' 
  | 'activations' 
  | 'metrics' 
  | 'patterns' 
  | 'insights'
  | 'settings';
```

### 5. **PatternAnalyzer**

**Responsabilidade**: Análise de padrões temporais e comportamentais

**Props TypeScript**:

```typescript
interface PatternAnalyzerProps {
  timeRange: TimeRange;
  granularity: 'hour' | 'day' | 'week' | 'month';
  patterns: DetectedPattern[];
  onPatternSelect: (pattern: DetectedPattern) => void;
}

interface DetectedPattern {
  id: string;
  type: 'temporal' | 'behavioral' | 'effectiveness' | 'streak';
  pattern: {
    description: string;
    strength: number; // 0-100 (confidence)
    frequency: number; // how often it occurs
    impact: 'positive' | 'negative' | 'neutral';
  };
  visualData: PatternVisualization;
  relatedInsights: string[]; // insight IDs
  suggestedActions: string[];
}

interface PatternVisualization {
  chartType: 'heatmap' | 'timeline' | 'scatter' | 'distribution';
  data: any[];
  highlights: any[];
}
```

---

## 🎮 Fluxos de Interação

### 1. **Carregamento Progressivo de Analytics**

```typescript
const useAnalyticsData = (timeRange: TimeRange) => {
  const [analyticsState, setAnalyticsState] = useState<AnalyticsState>({
    metrics: null,
    charts: null,
    insights: null,
    patterns: null,
    loading: {
      metrics: true,
      charts: true,
      insights: true,
      patterns: true
    }
  });

  useEffect(() => {
    const loadAnalyticsData = async () => {
      // Load metrics first (fastest)
      try {
        const metrics = await api.getMetrics(timeRange);
        setAnalyticsState(prev => ({
          ...prev,
          metrics,
          loading: { ...prev.loading, metrics: false }
        }));

        // Load charts data
        const chartData = await api.getChartData(timeRange);
        setAnalyticsState(prev => ({
          ...prev,
          charts: chartData,
          loading: { ...prev.loading, charts: false }
        }));

        // Load AI insights (slowest, but most valuable)
        const [insights, patterns] = await Promise.all([
          api.getAIInsights(timeRange),
          api.getPatterns(timeRange)
        ]);

        setAnalyticsState(prev => ({
          ...prev,
          insights,
          patterns,
          loading: { ...prev.loading, insights: false, patterns: false }
        }));

      } catch (error) {
        toast.error('Erro ao carregar dados de analytics');
        setAnalyticsState(prev => ({
          ...prev,
          loading: { metrics: false, charts: false, insights: false, patterns: false }
        }));
      }
    };

    loadAnalyticsData();
  }, [timeRange]);

  const refresh = () => {
    setAnalyticsState(prev => ({
      ...prev,
      loading: { metrics: true, charts: true, insights: true, patterns: true }
    }));
  };

  return { ...analyticsState, refresh };
};
```

### 2. **Drill-down em Métricas**

```typescript
const useMetricDrillDown = () => {
  const [drillDownStack, setDrillDownStack] = useState<DrillDownLevel[]>([]);
  const [currentView, setCurrentView] = useState<AnalyticsView>('overview');

  const drillDown = (metric: MetricData, level: DrillDownLevel) => {
    const newLevel: DrillDownLevel = {
      metric,
      level: level.level + 1,
      filters: level.filters,
      breadcrumb: `${level.breadcrumb} > ${metric.title}`
    };

    setDrillDownStack(prev => [...prev, newLevel]);
    setCurrentView('detail');

    // Track analytics interaction
    analytics.track('metric_drill_down', {
      metric_id: metric.id,
      drill_level: newLevel.level,
      user_segment: 'neurodivergent'
    });
  };

  const navigateUp = () => {
    if (drillDownStack.length > 0) {
      setDrillDownStack(prev => prev.slice(0, -1));
      
      if (drillDownStack.length === 1) {
        setCurrentView('overview');
      }
    }
  };

  const navigateToRoot = () => {
    setDrillDownStack([]);
    setCurrentView('overview');
  };

  return {
    currentView,
    drillDownStack,
    currentLevel: drillDownStack[drillDownStack.length - 1],
    drillDown,
    navigateUp,
    navigateToRoot,
    canNavigateUp: drillDownStack.length > 0
  };
};
```

### 3. **Sistema de Feedback para IA Insights**

```typescript
const useInsightFeedback = () => {
  const [feedbackSubmitted, setFeedbackSubmitted] = useState<Set<string>>(new Set());
  const [appliedRecommendations, setAppliedRecommendations] = useState<Set<string>>(new Set());

  const submitFeedback = async (
    insightId: string, 
    feedback: InsightFeedback
  ) => {
    try {
      await api.submitInsightFeedback(insightId, feedback);
      
      setFeedbackSubmitted(prev => new Set([...prev, insightId]));
      
      // Positive feedback improves future insights
      if (feedback.helpful) {
        toast.success('Obrigado! Isso nos ajuda a melhorar os insights');
      }

      // Track feedback for ML model improvement
      analytics.track('ai_insight_feedback', {
        insight_id: insightId,
        helpful: feedback.helpful,
        applied: feedback.applied,
        user_segment: 'neurodivergent'
      });

    } catch (error) {
      toast.error('Erro ao enviar feedback');
    }
  };

  const applyRecommendation = async (
    insightId: string,
    recommendation: AIRecommendation
  ) => {
    try {
      if (recommendation.autoApplicable) {
        await api.applyAutoRecommendation(insightId, recommendation.id);
        setAppliedRecommendations(prev => new Set([...prev, recommendation.id]));
        
        toast.success('Recomendação aplicada com sucesso!');
        
        // Auto-submit positive feedback for applied recommendations
        await submitFeedback(insightId, {
          helpful: true,
          applied: true,
          notes: 'Auto-applied recommendation'
        });
        
      } else {
        // Manual application guidance
        toast.info('Siga as orientações para aplicar esta recomendação');
      }

    } catch (error) {
      toast.error('Erro ao aplicar recomendação');
    }
  };

  return {
    feedbackSubmitted,
    appliedRecommendations,
    submitFeedback,
    applyRecommendation,
    hasFeedback: (insightId: string) => feedbackSubmitted.has(insightId),
    hasApplied: (recommendationId: string) => appliedRecommendations.has(recommendationId)
  };
};
```

---

## 🔌 API Integration

### 1. **Analytics Data Endpoints**

```typescript
// Buscar métricas principais
GET /api/analytics/metrics
Query: timeRange, granularity, categories[]
// Resposta:
{
  "effectiveness": {
    "overall": 94.2,
    "byCategory": { "medication": 98.2, "exercise": 89.3, ... },
    "trend": { "direction": "up", "percentage": 2.3, "period": "30d" }
  },
  "punctuality": {
    "averageResponseTime": 138, // seconds
    "byTimeOfDay": { "morning": 85, "afternoon": 195, ... },
    "fastestCategory": "medication"
  },
  "wellbeing": {
    "streakDays": 7,
    "moodTrend": "improving",
    "sleepCorrelation": 0.73
  },
  "usage": {
    "totalAlarms": 154,
    "activeAlarms": 12,
    "byDayOfWeek": [12, 18, 16, 14, 15, 13, 8]
  }
}

// Buscar dados para gráficos
GET /api/analytics/charts/{chartType}
Query: timeRange, granularity
// Resposta varia por tipo

// Buscar insights de IA
GET /api/analytics/ai-insights
Query: timeRange, categories[], limit
// Resposta:
{
  "insights": [
    {
      "id": "insight_001",
      "type": "pattern",
      "confidence": 87,
      "title": "Padrão de maior eficácia identificado",
      "description": "Você tem 23% mais sucesso com alarmes às terças e quintas",
      "evidence": { /* dados que suportam o insight */ },
      "recommendations": [
        {
          "action": "reschedule_recurring",
          "description": "Reagendar alarmes não-críticos para estes dias",
          "estimatedImpact": "+12% eficácia geral",
          "autoApplicable": true
        }
      ]
    }
  ],
  "nextRefreshIn": 3600, // seconds
  "dataQuality": "good"
}

// Submeter feedback sobre insight
POST /api/analytics/insights/{insightId}/feedback
{
  "helpful": true,
  "applied": false,
  "notes": "Interessante, mas preciso testar primeiro"
}

// Aplicar recomendação automática
POST /api/analytics/insights/{insightId}/apply-recommendation
{
  "recommendationId": "rec_001",
  "confirmApply": true
}
```

### 2. **Real-time Analytics Updates**

```typescript
const useRealtimeAnalytics = () => {
  const [analyticsData, setAnalyticsData] = useState<AnalyticsData | null>(null);
  const [lastUpdate, setLastUpdate] = useState<Date | null>(null);

  // WebSocket para updates em tempo real
  const ws = useWebSocket('/api/analytics/live', {
    onMessage: (event) => {
      const { type, data } = JSON.parse(event.data);
      
      switch (type) {
        case 'ALARM_TRIGGERED':
          // Update punctuality metrics
          updatePunctualityMetrics(data);
          break;
          
        case 'ALARM_COMPLETED':
          // Update effectiveness metrics
          updateEffectivenessMetrics(data);
          break;
          
        case 'NEW_AI_INSIGHT':
          // Add new insight
          addNewInsight(data);
          toast.info('Novo insight disponível!', {
            action: { label: 'Ver', onClick: () => scrollToInsights() }
          });
          break;
          
        case 'PATTERN_DETECTED':
          // Highlight new pattern
          highlightNewPattern(data);
          break;
      }
      
      setLastUpdate(new Date());
    }
  });

  const updatePunctualityMetrics = (alarmData: AlarmTriggerData) => {
    setAnalyticsData(prev => {
      if (!prev) return null;
      
      const newResponseTime = alarmData.responseTimeSeconds;
      const category = alarmData.category;
      
      // Update running averages
      const updatedPunctuality = {
        ...prev.punctuality,
        averageResponseTime: calculateRunningAverage(
          prev.punctuality.averageResponseTime,
          newResponseTime,
          prev.usage.totalAlarms + 1
        ),
        byCategory: {
          ...prev.punctuality.byCategory,
          [category]: calculateCategoryAverage(category, newResponseTime)
        }
      };
      
      return { ...prev, punctuality: updatedPunctuality };
    });
  };

  return { analyticsData, lastUpdate, isConnected: ws.readyState === WebSocket.OPEN };
};
```

---

## ♿ Acessibilidade Avançada para Analytics

### 1. **Descrições Textuais de Gráficos**

```typescript
const useChartAccessibility = (chartData: ChartDataPoint[], chartType: string) => {
  const generateAltText = () => {
    const dataPoints = chartData.length;
    const maxValue = Math.max(...chartData.map(d => d.y));
    const minValue = Math.min(...chartData.map(d => d.y));
    const trend = calculateTrend(chartData);
    
    let description = `Gráfico de ${chartType} com ${dataPoints} pontos de dados. `;
    description += `Valor máximo: ${maxValue}, mínimo: ${minValue}. `;
    description += `Tendência geral: ${trend.direction === 'up' ? 'crescente' : 
                    trend.direction === 'down' ? 'decrescente' : 'estável'}. `;
    
    // Highlight key insights
    const keyInsights = identifyKeyInsights(chartData);
    if (keyInsights.length > 0) {
      description += `Principais insights: ${keyInsights.join(', ')}.`;
    }
    
    return description;
  };

  const generateDataTable = () => {
    return (
      <table 
        className="sr-only" 
        aria-label="Dados do gráfico em formato tabular"
      >
        <thead>
          <tr>
            <th scope="col">Período</th>
            <th scope="col">Valor</th>
            <th scope="col">Mudança</th>
          </tr>
        </thead>
        <tbody>
          {chartData.map((point, index) => (
            <tr key={index}>
              <td>{formatPeriod(point.x)}</td>
              <td>{point.y}</td>
              <td>
                {index > 0 
                  ? formatChange(point.y - chartData[index - 1].y)
                  : 'N/A'
                }
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    );
  };

  const generateKeyboardNavigation = () => {
    const [focusedPoint, setFocusedPoint] = useState(0);
    
    const handleKeyDown = (e: KeyboardEvent) => {
      switch (e.key) {
        case 'ArrowRight':
          setFocusedPoint(prev => Math.min(prev + 1, chartData.length - 1));
          break;
        case 'ArrowLeft':
          setFocusedPoint(prev => Math.max(prev - 1, 0));
          break;
        case 'Home':
          setFocusedPoint(0);
          break;
        case 'End':
          setFocusedPoint(chartData.length - 1);
          break;
      }
    };
    
    return { focusedPoint, handleKeyDown };
  };

  return {
    altText: generateAltText(),
    dataTable: generateDataTable(),
    keyboardNav: generateKeyboardNavigation()
  };
};
```

### 2. **Sonificação de Dados (Opcional)**

```typescript
const useDataSonification = (enabled: boolean = false) => {
  const audioContext = useRef<AudioContext | null>(null);
  
  const initAudio = () => {
    if (!audioContext.current) {
      audioContext.current = new (window.AudioContext || window.webkitAudioContext)();
    }
  };

  const sonifyDataPoint = (value: number, min: number, max: number) => {
    if (!enabled || !audioContext.current) return;
    
    // Map value to frequency range (200Hz - 800Hz)
    const normalizedValue = (value - min) / (max - min);
    const frequency = 200 + (normalizedValue * 600);
    
    // Create oscillator for tone
    const oscillator = audioContext.current.createOscillator();
    const gainNode = audioContext.current.createGain();
    
    oscillator.connect(gainNode);
    gainNode.connect(audioContext.current.destination);
    
    oscillator.frequency.setValueAtTime(frequency, audioContext.current.currentTime);
    oscillator.type = 'sine';
    
    // Quick beep
    gainNode.gain.setValueAtTime(0.1, audioContext.current.currentTime);
    gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.current.currentTime + 0.2);
    
    oscillator.start(audioContext.current.currentTime);
    oscillator.stop(audioContext.current.currentTime + 0.2);
  };

  const sonifyTrend = (data: ChartDataPoint[]) => {
    if (!enabled) return;
    
    initAudio();
    
    const min = Math.min(...data.map(d => d.y));
    const max = Math.max(...data.map(d => d.y));
    
    data.forEach((point, index) => {
      setTimeout(() => {
        sonifyDataPoint(point.y, min, max);
      }, index * 100); // 100ms between points
    });
  };

  return { sonifyTrend, sonifyDataPoint };
};
```

---

## 🧪 Estratégia de Testes

### 1. **Testes de Componentes Analytics**

```typescript
describe('StatisticsScreen', () => {
  it('renders loading state correctly', () => {
    render(<StatisticsScreen loading={true} />);
    
    expect(screen.getByLabelText('Carregando estatísticas')).toBeInTheDocument();
    expect(screen.getAllByTestId('metric-skeleton')).toHaveLength(4);
    expect(screen.getByText('Analisando seus dados com IA...')).toBeInTheDocument();
  });
  
  it('renders no data state for new users', () => {
    const emptyData: AnalyticsData = {
      metrics: null,
      charts: null,
      insights: [],
      patterns: [],
      dataQuality: 'insufficient'
    };
    
    render(<StatisticsScreen data={emptyData} />);
    
    expect(screen.getByText('Dados Insuficientes')).toBeInTheDocument();
    expect(screen.getByText('3/7 dias')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Criar Novo Alarme' })).toBeInTheDocument();
  });
  
  it('displays metrics with correct accessibility attributes', () => {
    const mockMetrics: MetricData[] = [
      {
        id: 'effectiveness',
        title: 'Eficácia',
        value: 94.2,
        unit: '%',
        progress: 94,
        trend: { direction: 'up', percentage: 2.3, period: '30d' },
        icon: 'target',
        color: 'success'
      }
    ];
    
    render(<StatisticsScreen metrics={mockMetrics} />);
    
    const metricCard = screen.getByRole('article', { name: 'Eficácia' });
    expect(metricCard).toHaveAttribute('aria-describedby');
    
    const progressBar = screen.getByRole('progressbar');
    expect(progressBar).toHaveAttribute('aria-valuenow', '94');
    expect(progressBar).toHaveAttribute('aria-valuemin', '0');
    expect(progressBar).toHaveAttribute('aria-valuemax', '100');
  });
});
```

### 2. **Testes de Acessibilidade de Gráficos**

```typescript
describe('AccessibleChart', () => {
  const mockChartData = [
    { x: '2025-01-01', y: 85, label: 'Jan 1' },
    { x: '2025-01-02', y: 90, label: 'Jan 2' },
    { x: '2025-01-03', y: 88, label: 'Jan 3' }
  ];
  
  it('provides comprehensive alt text', () => {
    render(
      <AccessibleChart 
        data={mockChartData} 
        type="line" 
        title="Eficácia Diária"
        description="Evolução da eficácia ao longo dos últimos 3 dias"
      />
    );
    
    const chart = screen.getByRole('img', { name: /Gráfico de line/ });
    expect(chart).toHaveAttribute('alt');
    
    const altText = chart.getAttribute('alt');
    expect(altText).toContain('3 pontos de dados');
    expect(altText).toContain('máximo: 90');
    expect(altText).toContain('mínimo: 85');
  });
  
  it('provides data table alternative', () => {
    render(
      <AccessibleChart 
        data={mockChartData} 
        type="line" 
        showDataTable={true}
      />
    );
    
    const dataTable = screen.getByLabelText('Dados do gráfico em formato tabular');
    expect(dataTable).toBeInTheDocument();
    
    expect(screen.getByText('Jan 1')).toBeInTheDocument();
    expect(screen.getByText('85')).toBeInTheDocument();
  });
  
  it('supports keyboard navigation', async () => {
    const user = userEvent.setup();
    
    render(
      <AccessibleChart 
        data={mockChartData} 
        type="line"
        onDataPointClick={jest.fn()}
      />
    );
    
    const chart = screen.getByRole('application', { name: /Gráfico interativo/ });
    chart.focus();
    
    await user.keyboard('{ArrowRight}');
    expect(screen.getByText('Ponto focado: Jan 2, valor: 90')).toBeInTheDocument();
    
    await user.keyboard('{Enter}');
    // Should trigger onDataPointClick
  });
});
```

### 3. **Testes de Insights de IA**

```typescript
describe('AIInsightPanel', () => {
  const mockInsights: AIInsight[] = [
    {
      id: 'insight_001',
      type: 'pattern',
      confidence: 87,
      title: 'Padrão identificado',
      description: 'Você tem melhor performance às terças',
      evidenceData: [],
      recommendations: [
        {
          id: 'rec_001',
          action: 'reschedule',
          description: 'Reagendar alarmes não-críticos',
          estimatedImpact: '+12% eficácia',
          difficulty: 'easy',
          autoApplicable: true
        }
      ],
      category: 'general',
      priority: 'medium',
      createdAt: new Date()
    }
  ];
  
  it('displays insights with appropriate confidence indicators', () => {
    render(<AIInsightPanel insights={mockInsights} />);
    
    expect(screen.getByText('Padrão identificado')).toBeInTheDocument();
    expect(screen.getByText('Confiança: 87%')).toBeInTheDocument();
    expect(screen.getByLabelText('Alta confiança')).toBeInTheDocument();
  });
  
  it('allows feedback submission', async () => {
    const user = userEvent.setup();
    const onFeedback = jest.fn();
    
    render(
      <AIInsightPanel 
        insights={mockInsights} 
        onFeedback={onFeedback}
      />
    );
    
    await user.click(screen.getByRole('button', { name: 'Útil' }));
    
    expect(onFeedback).toHaveBeenCalledWith('insight_001', {
      helpful: true,
      applied: false
    });
  });
  
  it('applies auto-applicable recommendations', async () => {
    const user = userEvent.setup();
    const onApplyRecommendation = jest.fn();
    
    render(
      <AIInsightPanel 
        insights={mockInsights}
        onApplyRecommendation={onApplyRecommendation}
      />
    );
    
    await user.click(screen.getByRole('button', { name: 'Aplicar Sugestão' }));
    
    expect(onApplyRecommendation).toHaveBeenCalledWith('insight_001', 'reschedule');
  });
});
```

---

## ⚡ Performance e Otimização

### 1. **Carregamento Lazy de Gráficos**

```typescript
// Lazy load chart libraries only when needed
const LineChart = lazy(() => 
  import('recharts').then(module => ({ default: module.LineChart }))
);
const BarChart = lazy(() => 
  import('recharts').then(module => ({ default: module.BarChart }))
);

const ChartRenderer = ({ type, data, ...props }: ChartProps) => {
  const [isVisible, setIsVisible] = useState(false);
  const chartRef = useRef<HTMLDivElement>(null);
  
  // Intersection observer for lazy loading
  useEffect(() => {
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setIsVisible(true);
          observer.disconnect();
        }
      },
      { threshold: 0.1 }
    );
    
    if (chartRef.current) {
      observer.observe(chartRef.current);
    }
    
    return () => observer.disconnect();
  }, []);
  
  return (
    <div ref={chartRef} className="min-h-[300px]">
      {isVisible ? (
        <Suspense fallback={<ChartSkeleton />}>
          {type === 'line' && <LineChart data={data} {...props} />}
          {type === 'bar' && <BarChart data={data} {...props} />}
        </Suspense>
      ) : (
        <ChartPlaceholder />
      )}
    </div>
  );
};
```

### 2. **Memoização de Cálculos Complexos**

```typescript
const useOptimizedAnalytics = (rawData: AnalyticsRawData[]) => {
  // Memoize expensive calculations
  const processedMetrics = useMemo(() => {
    return calculateMetrics(rawData);
  }, [rawData]);
  
  const chartData = useMemo(() => {
    return processChartData(rawData);
  }, [rawData]);
  
  const patterns = useMemo(() => {
    return detectPatterns(rawData);
  }, [rawData]);
  
  // Debounced data updates
  const debouncedRawData = useDebounce(rawData, 500);
  
  return {
    metrics: processedMetrics,
    chartData,
    patterns,
    isProcessing: rawData !== debouncedRawData
  };
};

// Web Worker for heavy calculations
const useAnalyticsWorker = () => {
  const workerRef = useRef<Worker>();
  const [isProcessing, setIsProcessing] = useState(false);
  
  useEffect(() => {
    workerRef.current = new Worker('/workers/analytics-processor.js');
    
    workerRef.current.onmessage = (e) => {
      const { type, data } = e.data;
      
      if (type === 'ANALYSIS_COMPLETE') {
        setAnalyticsData(data);
        setIsProcessing(false);
      }
    };
    
    return () => workerRef.current?.terminate();
  }, []);
  
  const processInWorker = (rawData: AnalyticsRawData[]) => {
    setIsProcessing(true);
    workerRef.current?.postMessage({
      type: 'PROCESS_ANALYTICS',
      data: rawData
    });
  };
  
  return { processInWorker, isProcessing };
};
```

### 3. **Caching Inteligente**

```typescript
const useAnalyticsCache = () => {
  const cache = useRef<Map<string, CacheEntry>>(new Map());
  
  const getCacheKey = (timeRange: TimeRange, filters: any[]) => {
    return `${timeRange.start}-${timeRange.end}-${JSON.stringify(filters)}`;
  };
  
  const getCachedData = (key: string): AnalyticsData | null => {
    const entry = cache.current.get(key);
    
    if (!entry) return null;
    
    // Check if cache is still fresh (5 minutes)
    const now = Date.now();
    const cacheAge = now - entry.timestamp;
    const isStale = cacheAge > 5 * 60 * 1000;
    
    if (isStale) {
      cache.current.delete(key);
      return null;
    }
    
    return entry.data;
  };
  
  const setCachedData = (key: string, data: AnalyticsData) => {
    cache.current.set(key, {
      data,
      timestamp: Date.now()
    });
    
    // Limit cache size
    if (cache.current.size > 20) {
      const oldestKey = cache.current.keys().next().value;
      cache.current.delete(oldestKey);
    }
  };
  
  return { getCachedData, setCachedData, getCacheKey };
};
```

---

## 📝 Checklist de Implementação

### **📊 Interface Base**

- [ ] Criar layout principal com métricas cards
- [ ] Implementar filtros de período e categoria
- [ ] Configurar navegação drill-down
- [ ] Criar skeleton loading states

### **📈 Métricas e KPIs**

- [ ] Implementar MetricCard component
- [ ] Criar cálculo de eficácia por categoria
- [ ] Desenvolver métricas de pontualidade
- [ ] Implementar indicadores de tendência

### **📊 Gráficos Acessíveis**

- [ ] Criar AccessibleChart component
- [ ] Implementar alt text dinâmico
- [ ] Desenvolver tabela de dados alternativa
- [ ] Configurar navegação por teclado

### **🤖 IA e Insights**

- [ ] Implementar AIInsightPanel
- [ ] Criar sistema de feedback de insights
- [ ] Desenvolver aplicação automática de recomendações
- [ ] Configurar machine learning pipeline

### **🎯 Análise de Padrões**

- [ ] Criar PatternAnalyzer component
- [ ] Implementar detecção de padrões temporais
- [ ] Desenvolver análise de eficácia por contexto
- [ ] Configurar alertas de padrões importantes

### **📤 Exportação de Dados**

- [ ] Implementar ExportDialog
- [ ] Criar exportação em múltiplos formatos
- [ ] Desenvolver relatórios PDF acessíveis
- [ ] Configurar privacidade de dados

### **⚡ Performance**

- [ ] Implementar lazy loading de gráficos
- [ ] Configurar Web Workers para cálculos
- [ ] Criar sistema de cache inteligente
- [ ] Otimizar re-renders desnecessários

### **🧪 Testes e Qualidade**

- [ ] Escrever testes para componentes de analytics
- [ ] Criar testes de acessibilidade de gráficos
- [ ] Implementar testes de insights de IA
- [ ] Configurar testes de performance

---

**📅 Estimativa Total**: ~90 minutos de desenvolvimento
**🎯 Status**: ✅ **ESPECIFICAÇÃO FINAL COMPLETA**

Esta especificação completa o plano de frontend specifications do Smart Alarm, fornecendo uma base robusta para implementar uma tela de analytics acessível, inteligente e rica em insights para usuários neurodivergentes.

🎉 **PROJETO 100% COMPLETO** - Todas as 6 especificações de tela críticas foram implementadas com sucesso!
