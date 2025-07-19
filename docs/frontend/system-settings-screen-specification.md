# ⚙️ Especificação da Tela de Configurações do Sistema — Smart Alarm

## 📋 **Template Base de Referência**

**🎨 Base Template**: [Horizon UI Tailwind React](https://react-themes.com/product/horizon-tailwind-react)  
**🔗 Live Preview**: [Profile Settings Demo](https://horizon-ui.com/horizon-tailwind-react/admin/profile-settings)  
**🎯 Adaptações**: Interface de configurações com seções organizadas e controles acessíveis

### **Componentes do Template a Adaptar**

- ✅ **Settings Sections**: Seções organizadas por categoria
- ✅ **Toggle Controls**: Switches para ativação/desativação
- ✅ **Select Dropdowns**: Seletores para opções múltiplas
- ✅ **Input Validation**: Validação em tempo real
- ✅ **Save Confirmation**: Confirmações de alterações
- ✅ **Reset Options**: Opções de restauração padrão

---

## 🎯 Objetivo

A tela de configurações do sistema é o centro de controle de personalização do Smart Alarm, oferecendo uma interface completa e intuitiva para que usuários configurem preferências globais, acessibilidade, notificações, temas, sincronização e diagnósticos. Deve priorizar clareza organizacional, feedback imediato das mudanças, e suporte excepcional para usuários neurodivergentes, permitindo personalização profunda sem complexidade desnecessária.

---

## 🎨 Estrutura Visual

### Layout Principal - Configurações com Sidebar (Desktop ≥1024px)

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ 🔔 Smart Alarm                    🌐 Status: Online     👤 João Silva  [⚙️] │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ ← Dashboard    ⚙️ Configurações                        [💾 Salvar] [🔄 Sync]│
│                                                                             │
│ ┌─────────────────┬─────────────────────────────────────────────────────┐   │
│ │ 📋 CATEGORIAS   │ 🎨 APARÊNCIA E TEMA                                 │   │
│ │                 │                                                     │   │
│ │ ● Aparência     │ ┌─────────────────────────────────────────────────┐ │   │
│ │ ○ Acessibilidade│ │ 🌓 Tema do Sistema                              │ │   │
│ │ ○ Notificações  │ │                                                 │ │   │
│ │ ○ Som & Vibração│ │ ● Automático (segue sistema)                   │ │   │
│ │ ○ Sincronização │ │ ○ Claro                                        │ │   │
│ │ ○ Privacidade   │ │ ○ Escuro                                       │ │   │
│ │ ○ Diagnósticos  │ │ ○ Alto Contraste                               │ │   │
│ │ ○ Sobre         │ │                                                 │ │   │
│ │                 │ │ 🎨 Esquema de Cores                             │ │   │
│ │ 💡 Dicas:       │ │ [Azul Padrão    ⬇️] [🎨 Prévia]                 │ │   │
│ │ • Mudanças são  │ │                                                 │ │   │
│ │   salvas auto   │ │ 🔤 Tamanho da Fonte                            │ │   │
│ │ • Use alto      │ │ ●─────●─────○─────○─────○                      │ │   │
│ │   contraste     │ │ Pequeno    Médio    Grande                     │ │   │
│ │ • Teste sons    │ │                                                 │ │   │
│ │                 │ │ 📏 Espaçamento                                  │ │   │
│ │ [🔄 Redefinir]  │ │ ●─────○─────○                                   │ │   │
│ │ [📤 Exportar]   │ │ Compacto  Normal  Confortável                  │ │   │
│ │ [📥 Importar]   │ │                                                 │ │   │
│ │                 │ │ ✨ Animações                                    │ │   │
│ │                 │ │ [✓] Transições suaves                          │ │   │
│ │                 │ │ [✓] Feedback visual                            │ │   │
│ │                 │ │ [✗] Animações complexas                        │ │   │
│ │                 │ │                                                 │ │   │
│ │                 │ └─────────────────────────────────────────────────┘ │   │
│ │                 │                                                     │   │
│ │                 │ 💾 Alterações salvas automaticamente               │   │
│ └─────────────────┴─────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Layout Mobile - Lista Expandível (<768px)

```text
┌─────────────────────────────────┐
│ 🔔 Smart Alarm        [☰] [👤] │
├─────────────────────────────────┤
│ ← Voltar      ⚙️ Configurações  │
│                                 │
│ 🔍 Buscar configurações...      │
│                                 │
│ ┌─────────────────────────────┐ │
│ │ 🎨 Aparência            [>] │ │
│ │ Tema: Automático            │ │
│ └─────────────────────────────┘ │
│                                 │
│ ┌─────────────────────────────┐ │
│ │ ♿ Acessibilidade        [>] │ │
│ │ 3 configurações ativas      │ │
│ └─────────────────────────────┘ │
│                                 │
│ ┌─────────────────────────────┐ │
│ │ 🔔 Notificações         [>] │ │
│ │ Som: Ativo | Push: Ativo    │ │
│ └─────────────────────────────┘ │
│                                 │
│ ┌─────────────────────────────┐ │
│ │ 🔊 Som & Vibração       [>] │ │
│ │ Volume: 80% | Vibra: On     │ │
│ └─────────────────────────────┘ │
│                                 │
│ ┌─────────────────────────────┐ │
│ │ 🔄 Sincronização        [>] │ │
│ │ ⚠️ Última sync: há 2h       │ │
│ └─────────────────────────────┘ │
│                                 │
│ ┌─────────────────────────────┐ │
│ │ 🔒 Privacidade          [>] │ │
│ │ Dados seguros               │ │
│ └─────────────────────────────┘ │
│                                 │
│ [🔄 Sincronizar Agora]          │
│                                 │
└─────────────────────────────────┘
```

---

## 📱 Estados da Tela

### 1. **Estado Inicial - Aparência (Desktop)**

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ ⚙️ Configurações - Aparência                           [💾 Auto-save: ON]   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ 🎨 TEMA E APARÊNCIA                                                 │     │
│ │                                                                     │     │
│ │ 🌓 Modo de Cor                                                      │     │
│ │ ● Automático - Segue configuração do sistema                       │     │
│ │ ○ Claro - Interface clara                                          │     │
│ │ ○ Escuro - Interface escura                                        │     │
│ │ ○ Alto Contraste - Para melhor legibilidade                       │     │
│ │                                                                     │     │
│ │ 🎨 Esquema de Cores                                                 │     │
│ │ [Azul Smart     ⬇️]  [🎨 Personalizar]                             │     │
│ │ • Azul Smart (Padrão)                                              │     │
│ │ • Verde Natureza                                                    │     │
│ │ • Roxo Criativo                                                     │     │
│ │ • Laranja Energia                                                   │     │
│ │ • Neutro Minimalista                                                │     │
│ │                                                                     │     │
│ │ 🔤 Tipografia                                                       │     │
│ │ Tamanho: ●─────○─────○─────○─────○ (Médio)                         │     │
│ │ Família: [Inter            ⬇️]                                      │     │
│ │ Altura de linha: [Padrão   ⬇️]                                      │     │
│ │                                                                     │     │
│ │ 📏 Espaçamento                                                      │     │
│ │ ●─────○─────○ (Normal)                                              │     │
│ │ Compacto | Normal | Confortável                                     │     │
│ │                                                                     │     │
│ │ ✨ Efeitos Visuais                                                  │     │
│ │ [✓] Transições suaves                                               │     │
│ │ [✓] Feedback de hover                                               │     │
│ │ [✗] Blur effects                                                    │     │
│ │ [✗] Animações complexas (Respeitando prefers-reduced-motion)       │     │
│ │                                                                     │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
│ 👁️ PREVIEW: As mudanças são aplicadas imediatamente                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2. **Estado de Acessibilidade**

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ ⚙️ Configurações - Acessibilidade                      [💾 Auto-save: ON]   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ ♿ Essas configurações melhoram a experiência para usuários com             │
│    necessidades especiais e usuários neurodivergentes                      │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ 👁️ VISIBILIDADE                                                     │     │
│ │                                                                     │     │
│ │ [✓] Alto contraste                                                  │     │
│ │ [✓] Foco visível destacado                                          │     │
│ │ [✗] Inverter cores                                                  │     │
│ │ [✓] Destacar elementos interativos                                  │     │
│ │                                                                     │     │
│ │ 🔍 Zoom                                                             │     │
│ │ ●─────●─────○─────○─────○ (125%)                                    │     │
│ │ 100%   125%   150%   175%   200%                                    │     │
│ │                                                                     │     │
│ │ 🎯 Tamanho de alvos (botões, links)                                │     │
│ │ ○ Padrão (44px mín)                                                │     │
│ │ ● Aumentado (48px mín)                                             │     │
│ │ ○ Extra Large (56px mín)                                           │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ 🧠 NEURODIVERGÊNCIA                                                 │     │
│ │                                                                     │     │
│ │ [✓] Reduzir distrações visuais                                      │     │
│ │ [✓] Simplificar animações                                           │     │
│ │ [✓] Indicadores de progresso claros                                 │     │
│ │ [✓] Confirmações extras para ações importantes                      │     │
│ │ [✗] Modo de foco intenso (oculta elementos secundários)            │     │
│ │                                                                     │     │
│ │ ⏱️ Timeouts                                                          │     │
│ │ Notificações: [30 segundos ⬇️]                                      │     │
│ │ Formulários: [Sem timeout  ⬇️]                                      │     │
│ │                                                                     │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ ⌨️ NAVEGAÇÃO                                                         │     │
│ │                                                                     │     │
│ │ [✓] Navegação por teclado melhorada                                 │     │
│ │ [✓] Skip links para conteúdo principal                             │     │
│ │ [✓] Indicador de posição atual                                     │     │
│ │ [✗] Atalhos de teclado personalizados                              │     │
│ │                                                                     │     │
│ │ [🎮 Configurar Atalhos] [🔊 Testar Screen Reader]                  │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3. **Estado de Notificações e Som**

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ ⚙️ Configurações - Notificações & Som                  [💾 Auto-save: ON]   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ 🔔 NOTIFICAÇÕES PUSH                                                │     │
│ │                                                                     │     │
│ │ Status: 🟢 Permitidas pelo navegador                               │     │
│ │                                                                     │     │
│ │ [✓] Alarmes próximos                                                │     │
│ │ [✓] Alarmes perdidos                                                │     │
│ │ [✓] Lembrete de medicamentos                                        │     │
│ │ [✗] Atualizações do sistema                                         │     │
│ │ [✗] Tips e dicas                                                    │     │
│ │                                                                     │     │
│ │ ⏰ Horário Silencioso                                               │     │
│ │ [✗] Ativar modo silencioso                                          │     │
│ │ Das: [22:00] até [06:00]                                           │     │
│ │ Exceto: [✓] Medicamentos urgentes [✓] Emergências                  │     │
│ │                                                                     │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ 🔊 ÁUDIO                                                            │     │
│ │                                                                     │     │
│ │ Volume Principal: ●─────●─────●─────○─────○ (75%)                   │     │
│ │                                                                     │     │
│ │ 🎵 Sons de Alarme                                                   │     │
│ │ • Medicamentos: [Suave Sino   ⬇️] [🔊 Testar]                      │     │
│ │ • Exercícios:   [Energético   ⬇️] [🔊 Testar]                      │     │
│ │ • Trabalho:     [Profissional ⬇️] [🔊 Testar]                      │     │
│ │ • Geral:        [Clássico     ⬇️] [🔊 Testar]                      │     │
│ │                                                                     │     │
│ │ 🎶 Configurações Avançadas                                          │     │
│ │ [✓] Fade in (aumentar volume gradualmente)                          │     │
│ │ [✓] Repetir até confirmar                                           │     │
│ │ [✗] Sons diferentes para cada dia da semana                        │     │
│ │                                                                     │     │
│ │ Duração máxima: [2 minutos ⬇️]                                      │     │
│ │                                                                     │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ 📳 VIBRAÇÃO (Dispositivos compatíveis)                              │     │
│ │                                                                     │     │
│ │ [✓] Habilitar vibração                                              │     │
│ │                                                                     │     │
│ │ Intensidade: ●─────●─────○─────○ (Média)                           │     │
│ │                                                                     │     │
│ │ Padrões por categoria:                                              │     │
│ │ • Medicamentos: [Curto-Curto-Longo ⬇️] [📳 Testar]                 │     │
│ │ • Urgente:      [Longo Contínuo     ⬇️] [📳 Testar]                 │     │
│ │ • Normal:       [Padrão             ⬇️] [📳 Testar]                 │     │
│ │                                                                     │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4. **Estado de Sincronização**

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ ⚙️ Configurações - Sincronização                       [💾 Auto-save: ON]   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ 🔄 Status: 🟢 Sincronizado | Última sync: há 5 min                         │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ 🌐 SINCRONIZAÇÃO NA NUVEM                                           │     │
│ │                                                                     │     │
│ │ Status: 🟢 Ativa                                                    │     │
│ │ Conta: joao.silva@email.com                                        │     │
│ │                                                                     │     │
│ │ [✓] Alarmes e configurações                                         │     │
│ │ [✓] Histórico de ativação                                           │     │
│ │ [✓] Preferências de acessibilidade                                  │     │
│ │ [✗] Logs de diagnóstico                                             │     │
│ │                                                                     │     │
│ │ Frequência: [Automática      ⬇️]                                    │     │
│ │ • Automática (quando houver mudanças)                              │     │
│ │ • A cada 5 minutos                                                  │     │
│ │ • A cada 15 minutos                                                 │     │
│ │ • Apenas manual                                                     │     │
│ │                                                                     │     │
│ │ [🔄 Sincronizar Agora] [🔓 Gerenciar Conta]                        │     │
│ │                                                                     │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ 📅 CALENDÁRIOS EXTERNOS                                             │     │
│ │                                                                     │     │
│ │ Google Calendar: 🟢 Conectado                    [⚙️ Configurar]   │     │
│ │ • Importar eventos como alarmes: [✓]                               │     │
│ │ • Sincronização bidirecional: [✗]                                  │     │
│ │ • Categorias a importar: Trabalho, Pessoal                         │     │
│ │                                                                     │     │
│ │ Outlook Calendar: 🔴 Desconectado               [🔗 Conectar]      │     │
│ │                                                                     │     │
│ │ Apple Calendar: 🔴 Não disponível                                   │     │
│ │ (Disponível apenas em dispositivos Apple)                          │     │
│ │                                                                     │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────┐     │
│ │ 💾 BACKUP E RESTAURAÇÃO                                             │     │
│ │                                                                     │     │
│ │ Último backup: Hoje, 15:30                                          │     │
│ │ Tamanho: 2.4 MB                                                    │     │
│ │                                                                     │     │
│ │ Backup automático: [✓] Ativado                                      │     │
│ │ Frequência: [Diário          ⬇️]                                    │     │
│ │ Manter: [30 backups         ⬇️]                                     │     │
│ │                                                                     │     │
│ │ [💾 Fazer Backup Agora] [📤 Exportar] [📥 Restaurar]               │     │
│ │                                                                     │     │
│ └─────────────────────────────────────────────────────────────────────┘     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🧩 Componentes Detalhados

### 1. **SettingsNavigation**

**Responsabilidade**: Navegação lateral entre categorias de configurações

**Props TypeScript**:

```typescript
interface SettingsNavigationProps {
  currentSection: SettingsSection;
  onSectionChange: (section: SettingsSection) => void;
  sections: SettingsSectionConfig[];
  isMobile: boolean;
  searchQuery?: string;
}

interface SettingsSectionConfig {
  id: SettingsSection;
  title: string;
  icon: string;
  description: string;
  badge?: string | number;
  isEnabled: boolean;
}

type SettingsSection = 
  | 'appearance' 
  | 'accessibility' 
  | 'notifications' 
  | 'sound' 
  | 'sync' 
  | 'privacy' 
  | 'diagnostics' 
  | 'about';
```

**Funcionalidades**:

- Navegação por teclado (Arrow keys)
- Busca por configuração
- Badges para indicar configurações pendentes
- Collapse automático em mobile
- Indicação visual da seção ativa

### 2. **ThemeCustomizer**

**Responsabilidade**: Configuração completa de tema e aparência

**Props TypeScript**:

```typescript
interface ThemeCustomizerProps {
  currentTheme: ThemeConfig;
  onChange: (theme: ThemeConfig) => void;
  previewMode: boolean;
  availableThemes: ThemePreset[];
}

interface ThemeConfig {
  mode: 'auto' | 'light' | 'dark' | 'high-contrast';
  colorScheme: 'blue' | 'green' | 'purple' | 'orange' | 'neutral' | 'custom';
  customColors?: CustomColorPalette;
  typography: {
    fontSize: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
    fontFamily: string;
    lineHeight: 'tight' | 'normal' | 'relaxed';
  };
  spacing: 'compact' | 'normal' | 'comfortable';
  animations: {
    transitions: boolean;
    hover: boolean;
    blur: boolean;
    complex: boolean;
    respectReducedMotion: boolean;
  };
}
```

**Funcionalidades**:

- Preview em tempo real
- Salvamento automático
- Presets predefinidos
- Customização de cores
- Acessibilidade automática
- Export/import de temas

### 3. **AccessibilityPanel**

**Responsabilidade**: Configurações avançadas de acessibilidade

**Props TypeScript**:

```typescript
interface AccessibilityPanelProps {
  settings: AccessibilitySettings;
  onChange: (settings: AccessibilitySettings) => void;
  capabilities: DeviceCapabilities;
}

interface AccessibilitySettings {
  visual: {
    highContrast: boolean;
    focusVisible: boolean;
    invertColors: boolean;
    highlightInteractive: boolean;
    zoom: number; // 100-200%
    targetSize: 'default' | 'increased' | 'large';
  };
  neurodivergent: {
    reduceDistractions: boolean;
    simplifyAnimations: boolean;
    progressIndicators: boolean;
    extraConfirmations: boolean;
    focusMode: boolean;
    timeouts: {
      notifications: number; // seconds
      forms: number | 'never';
    };
  };
  navigation: {
    enhancedKeyboard: boolean;
    skipLinks: boolean;
    positionIndicator: boolean;
    customShortcuts: boolean;
    shortcuts: Record<string, string>;
  };
}
```

**Funcionalidades**:

- Testes em tempo real
- Compatibilidade com screen readers
- Validação automática de contraste
- Sugestões baseadas em necessidades
- Profile de acessibilidade

### 4. **NotificationCenter**

**Responsabilidade**: Configuração completa de notificações e sons

**Props TypeScript**:

```typescript
interface NotificationCenterProps {
  settings: NotificationSettings;
  onChange: (settings: NotificationSettings) => void;
  soundLibrary: SoundOption[];
  deviceCapabilities: DeviceCapabilities;
}

interface NotificationSettings {
  push: {
    enabled: boolean;
    types: {
      upcoming: boolean;
      missed: boolean;
      medication: boolean;
      system: boolean;
      tips: boolean;
    };
    quietHours: {
      enabled: boolean;
      start: string; // HH:MM
      end: string;   // HH:MM
      exceptions: string[]; // categories
    };
  };
  audio: {
    masterVolume: number; // 0-100
    categoryVolumes: Record<AlarmCategory, number>;
    sounds: Record<AlarmCategory, string>;
    advanced: {
      fadeIn: boolean;
      repeatUntilConfirm: boolean;
      differentPerDay: boolean;
      maxDuration: number; // minutes
    };
  };
  vibration: {
    enabled: boolean;
    intensity: number; // 0-100
    patterns: Record<AlarmCategory, VibrationPattern>;
  };
}
```

### 5. **SyncManager**

**Responsabilidade**: Configuração e status de sincronização

**Props TypeScript**:

```typescript
interface SyncManagerProps {
  syncStatus: SyncStatus;
  settings: SyncSettings;
  onChange: (settings: SyncSettings) => void;
  onManualSync: () => void;
  connectedServices: ConnectedService[];
}

interface SyncSettings {
  cloud: {
    enabled: boolean;
    frequency: 'auto' | '5min' | '15min' | 'manual';
    data: {
      alarms: boolean;
      history: boolean;
      preferences: boolean;
      diagnostics: boolean;
    };
  };
  externalCalendars: {
    google: ExternalCalendarConfig | null;
    outlook: ExternalCalendarConfig | null;
    apple: ExternalCalendarConfig | null;
  };
  backup: {
    auto: boolean;
    frequency: 'daily' | 'weekly' | 'monthly';
    retention: number; // number of backups to keep
  };
}

interface ExternalCalendarConfig {
  enabled: boolean;
  importAsAlarms: boolean;
  bidirectional: boolean;
  categories: string[];
  lastSync: Date;
}
```

---

## 🎮 Fluxos de Interação

### 1. **Mudança de Tema em Tempo Real**

```typescript
const useThemePreview = () => {
  const [previewTheme, setPreviewTheme] = useState<ThemeConfig | null>(null);
  const [applyTimer, setApplyTimer] = useState<NodeJS.Timeout | null>(null);
  
  const previewChange = (changes: Partial<ThemeConfig>) => {
    // Aplica preview imediatamente
    const newTheme = { ...currentTheme, ...changes };
    setPreviewTheme(newTheme);
    document.documentElement.setAttribute('data-theme', JSON.stringify(newTheme));
    
    // Agenda aplicação definitiva após 2s
    if (applyTimer) clearTimeout(applyTimer);
    const timer = setTimeout(() => {
      applyTheme(newTheme);
      setPreviewTheme(null);
    }, 2000);
    setApplyTimer(timer);
  };
  
  const cancelPreview = () => {
    if (applyTimer) clearTimeout(applyTimer);
    document.documentElement.setAttribute('data-theme', JSON.stringify(currentTheme));
    setPreviewTheme(null);
  };
  
  return { previewChange, cancelPreview, isPrewviewing: !!previewTheme };
};
```

### 2. **Validação de Acessibilidade Automática**

```typescript
const useAccessibilityValidator = (settings: AccessibilitySettings) => {
  const [warnings, setWarnings] = useState<AccessibilityWarning[]>([]);
  const [suggestions, setSuggestions] = useState<AccessibilitySuggestion[]>([]);
  
  useEffect(() => {
    const validateSettings = () => {
      const newWarnings: AccessibilityWarning[] = [];
      const newSuggestions: AccessibilitySuggestion[] = [];
      
      // Validação de contraste
      if (!settings.visual.highContrast && settings.visual.zoom < 125) {
        newWarnings.push({
          type: 'contrast',
          message: 'Considere ativar alto contraste ou aumentar zoom',
          severity: 'medium'
        });
      }
      
      // Sugestões baseadas em combinações
      if (settings.visual.highlightInteractive && !settings.navigation.enhancedKeyboard) {
        newSuggestions.push({
          type: 'navigation',
          message: 'Ative navegação por teclado melhorada para melhor experiência',
          action: () => updateSetting('navigation.enhancedKeyboard', true)
        });
      }
      
      setWarnings(newWarnings);
      setSuggestions(newSuggestions);
    };
    
    validateSettings();
  }, [settings]);
  
  return { warnings, suggestions };
};
```

### 3. **Teste de Som Inline**

```typescript
const useSoundTester = () => {
  const [isPlaying, setIsPlaying] = useState<string | null>(null);
  const [audio, setAudio] = useState<HTMLAudioElement | null>(null);
  
  const testSound = async (soundId: string, volume: number = 75) => {
    try {
      // Para som anterior se estiver tocando
      if (audio) {
        audio.pause();
        audio.currentTime = 0;
      }
      
      setIsPlaying(soundId);
      
      const newAudio = new Audio(`/sounds/${soundId}.mp3`);
      newAudio.volume = volume / 100;
      
      newAudio.addEventListener('ended', () => {
        setIsPlaying(null);
        setAudio(null);
      });
      
      newAudio.addEventListener('error', () => {
        toast.error('Não foi possível reproduzir o som');
        setIsPlaying(null);
        setAudio(null);
      });
      
      await newAudio.play();
      setAudio(newAudio);
      
    } catch (error) {
      toast.error('Erro ao testar som. Verifique permissões de áudio.');
      setIsPlaying(null);
    }
  };
  
  const stopSound = () => {
    if (audio) {
      audio.pause();
      audio.currentTime = 0;
      setAudio(null);
    }
    setIsPlaying(null);
  };
  
  return { testSound, stopSound, isPlaying };
};
```

---

## 🔌 API Integration

### 1. **Settings Endpoints**

```typescript
// Buscar configurações do usuário
GET /api/settings
// Resposta:
{
  "theme": { /* ThemeConfig */ },
  "accessibility": { /* AccessibilitySettings */ },
  "notifications": { /* NotificationSettings */ },
  "sync": { /* SyncSettings */ }
}

// Atualizar seção específica
PUT /api/settings/{section}
{
  // configurações da seção
}

// Aplicar preset de tema
POST /api/settings/theme/apply-preset
{
  "presetId": "high-contrast-large"
}

// Testar configuração de som
POST /api/settings/test-sound
{
  "soundId": "gentle-bell",
  "volume": 75,
  "category": "medication"
}

// Backup de configurações
POST /api/settings/backup
// Resposta:
{
  "backupId": "backup_2025-07-19_15-30",
  "size": 2457600,
  "timestamp": "2025-07-19T15:30:00Z"
}

// Restaurar backup
POST /api/settings/restore
{
  "backupId": "backup_2025-07-19_15-30"
}

// Importar/Exportar configurações
GET  /api/settings/export
POST /api/settings/import
```

### 2. **Sincronização em Tempo Real**

```typescript
const useSettingsSync = () => {
  const [settings, setSettings] = useState<SettingsConfig>(initialSettings);
  const [isSyncing, setIsSyncing] = useState(false);
  const [lastSync, setLastSync] = useState<Date | null>(null);
  
  // WebSocket para sync em tempo real
  const ws = useWebSocket('/api/settings/ws', {
    onMessage: (event) => {
      const { type, data } = JSON.parse(event.data);
      
      if (type === 'SETTINGS_UPDATED') {
        setSettings(prev => ({ ...prev, ...data }));
        toast.info('Configurações sincronizadas de outro dispositivo');
      }
      
      if (type === 'SYNC_STATUS') {
        setLastSync(new Date(data.timestamp));
      }
    }
  });
  
  // Mutation para atualizar configurações
  const updateSettings = useMutation({
    mutationFn: async (updates: Partial<SettingsConfig>) => {
      setIsSyncing(true);
      const response = await api.put('/api/settings', updates);
      return response.data;
    },
    onSuccess: (updatedSettings) => {
      setSettings(updatedSettings);
      setLastSync(new Date());
      toast.success('Configurações salvas');
    },
    onError: (error) => {
      toast.error('Erro ao salvar configurações');
    },
    onSettled: () => {
      setIsSyncing(false);
    }
  });
  
  // Auto-save com debounce
  const debouncedUpdate = useMemo(
    () => debounce((updates: Partial<SettingsConfig>) => {
      updateSettings.mutate(updates);
    }, 1000),
    [updateSettings]
  );
  
  const updateSetting = (path: string, value: any) => {
    const updates = set({}, path, value);
    setSettings(prev => ({ ...prev, ...updates }));
    debouncedUpdate(updates);
  };
  
  return {
    settings,
    updateSetting,
    isSyncing,
    lastSync,
    forceSync: () => updateSettings.mutate(settings)
  };
};
```

---

## ♿ Acessibilidade

### 1. **Estrutura Semântica das Configurações**

```html
<main role="main" aria-label="Configurações do sistema">
  <h1 id="settings-title">Configurações do Smart Alarm</h1>
  
  <nav aria-label="Categorias de configurações">
    <ul role="list">
      <li>
        <button
          role="tab"
          aria-selected="true"
          aria-controls="appearance-panel"
          id="appearance-tab"
        >
          Aparência
        </button>
      </li>
    </ul>
  </nav>
  
  <div
    role="tabpanel"
    aria-labelledby="appearance-tab"
    id="appearance-panel"
  >
    <fieldset aria-labelledby="theme-legend">
      <legend id="theme-legend">Configurações de Tema</legend>
      
      <div role="radiogroup" aria-labelledby="color-mode-label">
        <span id="color-mode-label">Modo de Cor</span>
        <label>
          <input type="radio" name="colorMode" value="auto" />
          Automático
        </label>
      </div>
    </fieldset>
  </div>
</main>
```

### 2. **Anúncios para Mudanças de Configuração**

```typescript
const useSettingsAnnouncements = () => {
  const announce = (message: string, priority: 'polite' | 'assertive' = 'polite') => {
    const announcement = document.createElement('div');
    announcement.setAttribute('aria-live', priority);
    announcement.setAttribute('aria-atomic', 'true');
    announcement.className = 'sr-only';
    announcement.textContent = message;
    
    document.body.appendChild(announcement);
    
    setTimeout(() => {
      document.body.removeChild(announcement);
    }, 1000);
  };
  
  const announceSettingChange = (setting: string, newValue: string) => {
    announce(`${setting} alterado para ${newValue}`);
  };
  
  const announceSyncStatus = (status: 'success' | 'error' | 'progress') => {
    const messages = {
      success: 'Configurações sincronizadas com sucesso',
      error: 'Erro na sincronização das configurações',
      progress: 'Sincronizando configurações...'
    };
    
    announce(messages[status], status === 'error' ? 'assertive' : 'polite');
  };
  
  return { announceSettingChange, announceSyncStatus };
};
```

### 3. **Validação de Acessibilidade em Tempo Real**

```typescript
const useAccessibilityValidation = () => {
  const validateContrast = async (theme: ThemeConfig) => {
    // Valida contraste de cores
    const elements = document.querySelectorAll('[data-theme-element]');
    const violations: ContrastViolation[] = [];
    
    for (const element of elements) {
      const styles = getComputedStyle(element);
      const bgColor = styles.backgroundColor;
      const textColor = styles.color;
      
      const contrast = calculateContrast(bgColor, textColor);
      
      if (contrast < 4.5) { // WCAG AA
        violations.push({
          element: element.tagName,
          contrast,
          required: 4.5,
          suggestion: 'Aumente o contraste ou ative alto contraste'
        });
      }
    }
    
    return violations;
  };
  
  const validateFocusOrder = () => {
    // Valida ordem de foco
    const focusableElements = getFocusableElements();
    const logicalOrder = calculateLogicalOrder(focusableElements);
    const currentOrder = focusableElements.map(el => el.tabIndex);
    
    return {
      isLogical: arraysEqual(logicalOrder, currentOrder),
      suggestions: generateFocusOrderSuggestions(logicalOrder, currentOrder)
    };
  };
  
  return { validateContrast, validateFocusOrder };
};
```

---

## 🧪 Estratégia de Testes

### 1. **Testes de Configurações**

```typescript
describe('SettingsScreen', () => {
  it('renders all settings sections', () => {
    render(<SettingsScreen />);
    
    expect(screen.getByText('Aparência')).toBeInTheDocument();
    expect(screen.getByText('Acessibilidade')).toBeInTheDocument();
    expect(screen.getByText('Notificações')).toBeInTheDocument();
    expect(screen.getByText('Sincronização')).toBeInTheDocument();
  });
  
  it('applies theme changes in real time', async () => {
    const user = userEvent.setup();
    render(<SettingsScreen />);
    
    // Navigate to appearance
    await user.click(screen.getByText('Aparência'));
    
    // Change to dark theme
    await user.click(screen.getByLabelText('Escuro'));
    
    await waitFor(() => {
      expect(document.documentElement).toHaveAttribute('data-theme');
      const theme = JSON.parse(document.documentElement.getAttribute('data-theme')!);
      expect(theme.mode).toBe('dark');
    });
  });
  
  it('saves settings automatically', async () => {
    const user = userEvent.setup();
    const mockSaveSettings = jest.fn();
    
    render(<SettingsScreen onSave={mockSaveSettings} />);
    
    // Change a setting
    const volumeSlider = screen.getByLabelText('Volume Principal');
    await user.drag(volumeSlider, { delta: { x: 50, y: 0 } });
    
    // Should debounce and save after delay
    await waitFor(() => {
      expect(mockSaveSettings).toHaveBeenCalledWith(
        expect.objectContaining({
          audio: expect.objectContaining({ masterVolume: expect.any(Number) })
        })
      );
    }, { timeout: 1500 });
  });
});
```

### 2. **Testes de Acessibilidade**

```typescript
describe('Settings Accessibility', () => {
  it('has no accessibility violations', async () => {
    const { container } = render(<SettingsScreen />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
  
  it('announces setting changes to screen readers', async () => {
    const user = userEvent.setup();
    const mockAnnounce = jest.fn();
    
    render(<SettingsScreen announcer={mockAnnounce} />);
    
    await user.click(screen.getByLabelText('Alto contraste'));
    
    expect(mockAnnounce).toHaveBeenCalledWith(
      'Alto contraste alterado para ativado',
      'polite'
    );
  });
  
  it('supports keyboard navigation between sections', async () => {
    const user = userEvent.setup();
    render(<SettingsScreen />);
    
    const firstSection = screen.getByText('Aparência');
    const secondSection = screen.getByText('Acessibilidade');
    
    firstSection.focus();
    await user.keyboard('{ArrowDown}');
    
    expect(secondSection).toHaveFocus();
  });
});
```

### 3. **Testes de Sincronização**

```typescript
describe('Settings Sync', () => {
  it('syncs settings across devices', async () => {
    const mockWebSocket = createMockWebSocket();
    render(<SettingsScreen websocket={mockWebSocket} />);
    
    // Simulate external update
    mockWebSocket.simulateMessage({
      type: 'SETTINGS_UPDATED',
      data: { theme: { mode: 'dark' } }
    });
    
    await waitFor(() => {
      expect(screen.getByText('Configurações sincronizadas')).toBeInTheDocument();
    });
  });
  
  it('handles sync conflicts gracefully', async () => {
    const user = userEvent.setup();
    
    // Mock conflict scenario
    server.use(
      rest.put('/api/settings', (req, res, ctx) => {
        return res(ctx.status(409), ctx.json({
          error: 'SYNC_CONFLICT',
          serverVersion: { theme: { mode: 'light' } },
          clientVersion: { theme: { mode: 'dark' } }
        }));
      })
    );
    
    render(<SettingsScreen />);
    
    await user.click(screen.getByLabelText('Escuro'));
    
    await waitFor(() => {
      expect(screen.getByText('Conflito de sincronização')).toBeInTheDocument();
      expect(screen.getByText('Resolver Conflito')).toBeInTheDocument();
    });
  });
});
```

---

## ⚡ Performance

### 1. **Lazy Loading de Seções**

```typescript
// Lazy load das seções de configurações
const AppearanceSettings = lazy(() => import('./sections/AppearanceSettings'));
const AccessibilitySettings = lazy(() => import('./sections/AccessibilitySettings'));
const NotificationSettings = lazy(() => import('./sections/NotificationSettings'));
const SyncSettings = lazy(() => import('./sections/SyncSettings'));

const SettingsRouter = ({ currentSection }: { currentSection: SettingsSection }) => {
  const getSectionComponent = () => {
    switch (currentSection) {
      case 'appearance': return <AppearanceSettings />;
      case 'accessibility': return <AccessibilitySettings />;
      case 'notifications': return <NotificationSettings />;
      case 'sync': return <SyncSettings />;
      default: return <AppearanceSettings />;
    }
  };
  
  return (
    <Suspense fallback={<SettingsSkeleton />}>
      {getSectionComponent()}
    </Suspense>
  );
};
```

### 2. **Otimização de Re-renders**

```typescript
const useOptimizedSettings = () => {
  const [settings, setSettings] = useState<SettingsConfig>(initialSettings);
  
  // Memoize seções individuais para evitar re-renders desnecessários
  const themeSettings = useMemo(() => settings.theme, [settings.theme]);
  const accessibilitySettings = useMemo(() => settings.accessibility, [settings.accessibility]);
  const notificationSettings = useMemo(() => settings.notifications, [settings.notifications]);
  
  // Update específico por seção
  const updateSection = useCallback(<T extends keyof SettingsConfig>(
    section: T,
    updates: Partial<SettingsConfig[T]>
  ) => {
    setSettings(prev => ({
      ...prev,
      [section]: { ...prev[section], ...updates }
    }));
  }, []);
  
  return {
    themeSettings,
    accessibilitySettings,
    notificationSettings,
    updateSection
  };
};
```

### 3. **Debounce Inteligente**

```typescript
const useSmartDebounce = <T>(
  callback: (value: T) => void,
  delay: number,
  immediate: (value: T) => boolean = () => false
) => {
  const timeoutRef = useRef<NodeJS.Timeout>();
  
  return useCallback((value: T) => {
    // Aplicação imediata para mudanças críticas
    if (immediate(value)) {
      callback(value);
      return;
    }
    
    // Debounce para mudanças normais
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }
    
    timeoutRef.current = setTimeout(() => {
      callback(value);
    }, delay);
  }, [callback, delay, immediate]);
};

// Uso no componente
const debouncedSave = useSmartDebounce(
  saveSettings,
  1000,
  (setting) => setting.type === 'accessibility' // Acessibilidade é imediata
);
```

---

## 📝 Checklist de Implementação

### **🎨 Interface Base**

- [ ] Criar layout principal com sidebar de navegação
- [ ] Implementar navegação responsiva mobile
- [ ] Configurar sistema de tabs/panels acessível
- [ ] Criar componente de busca de configurações

### **🎨 Seção Aparência**

- [ ] Implementar seletor de tema com preview
- [ ] Criar customizador de esquema de cores
- [ ] Desenvolver controles de tipografia
- [ ] Implementar configurações de espaçamento e animações

### **♿ Seção Acessibilidade**

- [ ] Criar controles de visibilidade (contraste, zoom, foco)
- [ ] Implementar configurações para neurodivergência
- [ ] Desenvolver painel de navegação por teclado
- [ ] Adicionar testes de acessibilidade inline

### **🔔 Seção Notificações**

- [ ] Implementar controles de push notifications
- [ ] Criar configurador de horário silencioso
- [ ] Desenvolver painel de sons por categoria
- [ ] Implementar controles de vibração

### **🔄 Seção Sincronização**

- [ ] Criar painel de status de sync
- [ ] Implementar conexão com calendários externos
- [ ] Desenvolver sistema de backup/restauração
- [ ] Configurar resolução de conflitos

### **🔧 Funcionalidades Avançadas**

- [ ] Implementar preview em tempo real
- [ ] Configurar auto-save com debounce
- [ ] Criar sistema de export/import
- [ ] Implementar validação de configurações

### **🧪 Testes e Qualidade**

- [ ] Escrever testes unitários para componentes
- [ ] Criar testes de acessibilidade automatizados
- [ ] Implementar testes de sincronização
- [ ] Configurar testes de performance

### **⚡ Otimização**

- [ ] Implementar lazy loading de seções
- [ ] Configurar memoização apropriada
- [ ] Otimizar re-renders desnecessários
- [ ] Implementar caching de configurações

---

**📅 Estimativa Total**: ~90 minutos de desenvolvimento
**🎯 Próximo Passo**: Continuar com ETAPA 3 - Statistics/Analytics Screen

Esta especificação fornece uma base completa para implementar uma tela de configurações robusta, acessível e intuitiva para o Smart Alarm.
