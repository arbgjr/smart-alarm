# 🎨 Smart Alarm Design System

## � **Template Base de Referência**

**🎨 Base Template**: [Horizon UI Tailwind React](https://react-themes.com/product/horizon-tailwind-react)  
**🔗 Live Preview**: [Demo Interativo](https://horizon-ui.com/horizon-tailwind-react/admin/default)  
**📚 Stack**: React 18 + Tailwind CSS + TypeScript  
**📜 Licença**: Open Source (Free)

### **Justificativa da Escolha**

O Horizon UI foi selecionado como template base por oferecer:

- ✅ **Design Moderno**: Estética contemporânea alinhada com tendências de UX
- ✅ **Stack Técnica**: React 18 + Tailwind CSS (nossa stack principal)
- ✅ **Componentização**: Estrutura bem organizada e reutilizável
- ✅ **Responsividade**: Otimizado para mobile e desktop
- ✅ **Performance**: Open source com otimizações incorporadas
- ✅ **Flexibilidade**: Base sólida para customizações de acessibilidade

**⚠️ Adaptações Necessárias**: O template será **extensivamente customizado** para atender aos requisitos específicos de acessibilidade WCAG 2.1 AAA e necessidades de usuários neurodivergentes.

---

## �📖 Índice

1. [Visão Geral e Princípios](#visão-geral-e-princípios)
2. [Paleta de Cores](#paleta-de-cores)
3. [Tipografia](#tipografia)
4. [Espaçamento e Layout](#espaçamento-e-layout)
5. [Componentes Base](#componentes-base)
6. [Estados e Interações](#estados-e-interações)
7. [Acessibilidade](#acessibilidade)
8. [Iconografia](#iconografia)
9. [Animações e Transições](#animações-e-transições)
10. [Temas e Personalização](#temas-e-personalização)
11. [Implementação Técnica](#implementação-técnica)

---

## 🎯 Visão Geral e Princípios

### Missão do Design System

Criar uma experiência visual e interativa **inclusiva, confiável e acessível** que atenda às necessidades diversas de todos os usuários, com foco especial em pessoas neurodivergentes que dependem do sistema para gerenciar tarefas críticas como medicamentos e compromissos importantes.

### Princípios Fundamentais

#### 1. 🧠 **Acessibilidade Cognitiva em Primeiro Lugar**

- Reduzir carga cognitiva em todas as interações
- Fornecer múltiplas formas de processar informações
- Padrões previsíveis e consistentes
- Feedback imediato e claro

#### 2. 🎯 **Confiabilidade Visual**

- Hierarquia visual clara e intuitiva
- Estados bem definidos e reconhecíveis
- Informações críticas nunca dependem apenas de cor
- Contraste adequado para todas as condições

#### 3. 🔄 **Flexibilidade e Personalização**

- Múltiplos temas (claro, escuro, alto contraste)
- Opções de tipografia (padrão, dislexia, ampliada)
- Controle de movimento e animações
- Densidade de informações ajustável

#### 4. 🛡️ **Robustez Técnica**

- Compatibilidade com tecnologias assistivas
- Performance otimizada
- Responsividade completa
- Degradação graceful

---

## 🎨 Paleta de Cores

### Cores Primárias

```css
:root {
  /* Azul Principal - Confiabilidade e Tecnologia */
  --color-primary-50: #eff6ff;
  --color-primary-100: #dbeafe;
  --color-primary-200: #bfdbfe;
  --color-primary-300: #93c5fd;
  --color-primary-400: #60a5fa;
  --color-primary-500: #3b82f6;  /* Principal */
  --color-primary-600: #2563eb;
  --color-primary-700: #1d4ed8;
  --color-primary-800: #1e40af;
  --color-primary-900: #1e3a8a;
  --color-primary-950: #172554;
}
```

### Cores Semânticas

```css
:root {
  /* Verde - Sucesso, Segurança, Conclusão */
  --color-success-50: #f0fdf4;
  --color-success-100: #dcfce7;
  --color-success-200: #bbf7d0;
  --color-success-300: #86efac;
  --color-success-400: #4ade80;
  --color-success-500: #22c55e;
  --color-success-600: #16a34a;
  --color-success-700: #15803d;
  --color-success-800: #166534;
  --color-success-900: #14532d;

  /* Amarelo - Atenção, Avisos, Pendências */
  --color-warning-50: #fffbeb;
  --color-warning-100: #fef3c7;
  --color-warning-200: #fed17d;
  --color-warning-300: #fdba2a;
  --color-warning-400: #f59e0b;
  --color-warning-500: #d97706;
  --color-warning-600: #b45309;
  --color-warning-700: #92400e;
  --color-warning-800: #78350f;
  --color-warning-900: #662510;

  /* Vermelho - Erro, Urgência, Ações Destrutivas */
  --color-error-50: #fef2f2;
  --color-error-100: #fee2e2;
  --color-error-200: #fecaca;
  --color-error-300: #fca5a5;
  --color-error-400: #f87171;
  --color-error-500: #ef4444;
  --color-error-600: #dc2626;
  --color-error-700: #b91c1c;
  --color-error-800: #991b1b;
  --color-error-900: #7f1d1d;
}
```

### Cores de Categorias de Alarmes

```css
:root {
  /* Medicamentos - Vermelho suave (crítico mas não agressivo) */
  --color-medication-primary: #f87171;
  --color-medication-background: #fef2f2;
  --color-medication-border: #fecaca;

  /* Trabalho - Azul profissional */
  --color-work-primary: #3b82f6;
  --color-work-background: #eff6ff;
  --color-work-border: #bfdbfe;

  /* Exercício - Verde energia */
  --color-exercise-primary: #10b981;
  --color-exercise-background: #ecfdf5;
  --color-exercise-border: #a7f3d0;

  /* Pessoal - Roxo criatividade */
  --color-personal-primary: #8b5cf6;
  --color-personal-background: #f5f3ff;
  --color-personal-border: #c4b5fd;

  /* Compromissos - Laranja atenção */
  --color-appointment-primary: #f59e0b;
  --color-appointment-background: #fffbeb;
  --color-appointment-border: #fed7aa;
}
```

### Cores Neutras

```css
:root {
  /* Escala de Cinzas - Interface base */
  --color-gray-50: #f9fafb;
  --color-gray-100: #f3f4f6;
  --color-gray-200: #e5e7eb;
  --color-gray-300: #d1d5db;
  --color-gray-400: #9ca3af;
  --color-gray-500: #6b7280;
  --color-gray-600: #4b5563;
  --color-gray-700: #374151;
  --color-gray-800: #1f2937;
  --color-gray-900: #111827;
  --color-gray-950: #030712;

  /* Cores de Fundo */
  --color-background-primary: #ffffff;
  --color-background-secondary: #f9fafb;
  --color-background-tertiary: #f3f4f6;
  
  /* Cores de Texto */
  --color-text-primary: #111827;
  --color-text-secondary: #4b5563;
  --color-text-tertiary: #6b7280;
  --color-text-inverse: #ffffff;
}
```

### Modo Alto Contraste

```css
/* Tema Alto Contraste para melhor acessibilidade */
:root[data-theme="high-contrast"] {
  --color-primary-500: #0000ff;
  --color-success-600: #008000;
  --color-warning-500: #ff8c00;
  --color-error-600: #ff0000;
  
  --color-background-primary: #ffffff;
  --color-text-primary: #000000;
  
  /* Bordas mais definidas */
  --border-width-default: 2px;
  --border-width-focus: 4px;
}
```

---

## 🔤 Tipografia

### Família de Fontes

```css
:root {
  /* Fonte Padrão - Inter (legibilidade otimizada) */
  --font-family-primary: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
  
  /* Fonte para Dislexia - OpenDyslexic */
  --font-family-dyslexic: 'OpenDyslexic', 'Comic Neue', cursive;
  
  /* Fonte Monospaciada - Para código e números */
  --font-family-mono: 'JetBrains Mono', 'Fira Code', 'Consolas', monospace;
}
```

### Escala Tipográfica

```css
:root {
  /* Tamanhos de Fonte - Escala harmônica */
  --font-size-xs: 0.75rem;    /* 12px */
  --font-size-sm: 0.875rem;   /* 14px */
  --font-size-base: 1rem;     /* 16px */
  --font-size-lg: 1.125rem;   /* 18px */
  --font-size-xl: 1.25rem;    /* 20px */
  --font-size-2xl: 1.5rem;    /* 24px */
  --font-size-3xl: 1.875rem;  /* 30px */
  --font-size-4xl: 2.25rem;   /* 36px */
  
  /* Alturas de Linha - Leitura confortável */
  --line-height-tight: 1.25;
  --line-height-normal: 1.5;
  --line-height-relaxed: 1.75;
  
  /* Pesos das Fontes */
  --font-weight-light: 300;
  --font-weight-normal: 400;
  --font-weight-medium: 500;
  --font-weight-semibold: 600;
  --font-weight-bold: 700;
}
```

### Classes Utilitárias de Texto

```css
/* Títulos Principais */
.text-display-large {
  font-size: var(--font-size-4xl);
  line-height: var(--line-height-tight);
  font-weight: var(--font-weight-bold);
  letter-spacing: -0.025em;
}

.text-display-medium {
  font-size: var(--font-size-3xl);
  line-height: var(--line-height-tight);
  font-weight: var(--font-weight-semibold);
}

/* Títulos de Seção */
.text-heading-large {
  font-size: var(--font-size-2xl);
  line-height: var(--line-height-normal);
  font-weight: var(--font-weight-semibold);
}

.text-heading-medium {
  font-size: var(--font-size-xl);
  line-height: var(--line-height-normal);
  font-weight: var(--font-weight-medium);
}

.text-heading-small {
  font-size: var(--font-size-lg);
  line-height: var(--line-height-normal);
  font-weight: var(--font-weight-medium);
}

/* Texto de Corpo */
.text-body-large {
  font-size: var(--font-size-lg);
  line-height: var(--line-height-relaxed);
  font-weight: var(--font-weight-normal);
}

.text-body-medium {
  font-size: var(--font-size-base);
  line-height: var(--line-height-normal);
  font-weight: var(--font-weight-normal);
}

.text-body-small {
  font-size: var(--font-size-sm);
  line-height: var(--line-height-normal);
  font-weight: var(--font-weight-normal);
}

/* Texto de Apoio */
.text-caption {
  font-size: var(--font-size-sm);
  line-height: var(--line-height-normal);
  color: var(--color-text-secondary);
}

.text-overline {
  font-size: var(--font-size-xs);
  line-height: var(--line-height-normal);
  font-weight: var(--font-weight-semibold);
  text-transform: uppercase;
  letter-spacing: 0.1em;
}
```

### Configurações Específicas para Acessibilidade

```css
/* Configurações para usuários com dislexia */
:root[data-font-preference="dyslexic"] {
  --font-family-primary: var(--font-family-dyslexic);
  --line-height-normal: 1.75; /* Maior espaçamento entre linhas */
  --letter-spacing-normal: 0.05em; /* Mais espaço entre caracteres */
}

/* Configurações para texto ampliado */
:root[data-font-size="large"] {
  --font-size-base: 1.125rem;   /* 18px */
  --font-size-sm: 1rem;         /* 16px */
  --font-size-lg: 1.25rem;      /* 20px */
}

:root[data-font-size="extra-large"] {
  --font-size-base: 1.25rem;    /* 20px */
  --font-size-sm: 1.125rem;     /* 18px */
  --font-size-lg: 1.5rem;       /* 24px */
}
```

---

## 📐 Espaçamento e Layout

### Sistema de Espaçamento

```css
:root {
  /* Escala de Espaçamento - Base 8px para consistência */
  --spacing-0: 0;
  --spacing-1: 0.25rem;  /* 4px */
  --spacing-2: 0.5rem;   /* 8px */
  --spacing-3: 0.75rem;  /* 12px */
  --spacing-4: 1rem;     /* 16px */
  --spacing-5: 1.25rem;  /* 20px */
  --spacing-6: 1.5rem;   /* 24px */
  --spacing-8: 2rem;     /* 32px */
  --spacing-10: 2.5rem;  /* 40px */
  --spacing-12: 3rem;    /* 48px */
  --spacing-16: 4rem;    /* 64px */
  --spacing-20: 5rem;    /* 80px */
  --spacing-24: 6rem;    /* 96px */
}
```

### Grid e Layout

```css
:root {
  /* Breakpoints responsivos */
  --breakpoint-sm: 640px;
  --breakpoint-md: 768px;
  --breakpoint-lg: 1024px;
  --breakpoint-xl: 1280px;
  --breakpoint-2xl: 1536px;
  
  /* Container máximos */
  --container-sm: 640px;
  --container-md: 768px;
  --container-lg: 1024px;
  --container-xl: 1280px;
  
  /* Margens dos containers */
  --container-padding: var(--spacing-4);
  --container-padding-lg: var(--spacing-8);
}
```

### Áreas de Toque e Interação

```css
:root {
  /* Tamanhos mínimos para acessibilidade */
  --touch-target-min: 44px;  /* Recomendação WCAG */
  --touch-target-comfortable: 48px;
  --touch-target-large: 56px;
  
  /* Espaçamento entre elementos interativos */
  --interactive-spacing: var(--spacing-2);
  --interactive-spacing-comfortable: var(--spacing-4);
}
```

---

## 🧩 Componentes Base

### Botões

```css
/* Base do Botão */
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: var(--touch-target-comfortable);
  padding: var(--spacing-3) var(--spacing-6);
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-medium);
  line-height: 1;
  border-radius: var(--border-radius-md);
  border: 2px solid transparent;
  cursor: pointer;
  transition: all 0.2s ease-in-out;
  text-decoration: none;
  white-space: nowrap;
  
  /* Estados de foco acessíveis */
  &:focus-visible {
    outline: 2px solid var(--color-primary-600);
    outline-offset: 2px;
  }
  
  /* Estado desabilitado */
  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
    pointer-events: none;
  }
}

/* Variações de Botões */
.btn--primary {
  background-color: var(--color-primary-600);
  color: white;
  border-color: var(--color-primary-600);
  
  &:hover:not(:disabled) {
    background-color: var(--color-primary-700);
    border-color: var(--color-primary-700);
  }
  
  &:active {
    background-color: var(--color-primary-800);
    transform: translateY(1px);
  }
}

.btn--secondary {
  background-color: white;
  color: var(--color-primary-600);
  border-color: var(--color-primary-600);
  
  &:hover:not(:disabled) {
    background-color: var(--color-primary-50);
  }
}

.btn--success {
  background-color: var(--color-success-600);
  color: white;
  border-color: var(--color-success-600);
  
  &:hover:not(:disabled) {
    background-color: var(--color-success-700);
  }
}

.btn--danger {
  background-color: var(--color-error-600);
  color: white;
  border-color: var(--color-error-600);
  
  &:hover:not(:disabled) {
    background-color: var(--color-error-700);
  }
}

/* Tamanhos de Botões */
.btn--small {
  min-height: var(--touch-target-min);
  padding: var(--spacing-2) var(--spacing-4);
  font-size: var(--font-size-sm);
}

.btn--large {
  min-height: var(--touch-target-large);
  padding: var(--spacing-4) var(--spacing-8);
  font-size: var(--font-size-lg);
}

/* Botão apenas ícone */
.btn--icon {
  width: var(--touch-target-comfortable);
  height: var(--touch-target-comfortable);
  padding: 0;
}
```

### Cards

```css
.card {
  background-color: var(--color-background-primary);
  border-radius: var(--border-radius-lg);
  border: 1px solid var(--color-gray-200);
  box-shadow: var(--shadow-sm);
  overflow: hidden;
  transition: all 0.2s ease-in-out;
  
  &:hover {
    box-shadow: var(--shadow-md);
    border-color: var(--color-gray-300);
  }
  
  &:focus-within {
    border-color: var(--color-primary-500);
    box-shadow: var(--shadow-focus);
  }
}

.card__header {
  padding: var(--spacing-6);
  border-bottom: 1px solid var(--color-gray-200);
}

.card__content {
  padding: var(--spacing-6);
}

.card__footer {
  padding: var(--spacing-6);
  border-top: 1px solid var(--color-gray-200);
  background-color: var(--color-background-secondary);
}

/* Card de Alarme com categorias */
.card--alarm {
  position: relative;
  
  &::before {
    content: '';
    position: absolute;
    left: 0;
    top: 0;
    bottom: 0;
    width: 4px;
    background-color: var(--category-color, var(--color-primary-500));
  }
  
  &.card--medication::before {
    background-color: var(--color-medication-primary);
  }
  
  &.card--work::before {
    background-color: var(--color-work-primary);
  }
  
  &.card--exercise::before {
    background-color: var(--color-exercise-primary);
  }
}
```

### Formulários

```css
.form-group {
  margin-bottom: var(--spacing-6);
}

.form-label {
  display: block;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
  margin-bottom: var(--spacing-2);
  
  /* Indicador de campo obrigatório */
  &.form-label--required::after {
    content: '*';
    color: var(--color-error-600);
    margin-left: var(--spacing-1);
  }
}

.form-input {
  width: 100%;
  min-height: var(--touch-target-comfortable);
  padding: var(--spacing-3) var(--spacing-4);
  font-size: var(--font-size-base);
  color: var(--color-text-primary);
  background-color: var(--color-background-primary);
  border: 2px solid var(--color-gray-300);
  border-radius: var(--border-radius-md);
  transition: all 0.2s ease-in-out;
  
  &::placeholder {
    color: var(--color-text-tertiary);
  }
  
  &:focus {
    outline: none;
    border-color: var(--color-primary-500);
    box-shadow: var(--shadow-focus);
  }
  
  &:invalid {
    border-color: var(--color-error-500);
  }
  
  &:disabled {
    background-color: var(--color-gray-100);
    cursor: not-allowed;
  }
}

.form-help {
  margin-top: var(--spacing-2);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.form-error {
  margin-top: var(--spacing-2);
  font-size: var(--font-size-sm);
  color: var(--color-error-600);
  display: flex;
  align-items: flex-start;
  gap: var(--spacing-2);
  
  &::before {
    content: '⚠️';
    flex-shrink: 0;
  }
}

/* Checkbox e Radio customizados */
.form-checkbox,
.form-radio {
  appearance: none;
  width: 20px;
  height: 20px;
  border: 2px solid var(--color-gray-400);
  border-radius: var(--border-radius-sm);
  position: relative;
  cursor: pointer;
  
  &:checked {
    background-color: var(--color-primary-600);
    border-color: var(--color-primary-600);
  }
  
  &:checked::after {
    content: '✓';
    position: absolute;
    color: white;
    font-size: 14px;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
  }
  
  &:focus {
    outline: 2px solid var(--color-primary-600);
    outline-offset: 2px;
  }
}

.form-radio {
  border-radius: 50%;
  
  &:checked::after {
    content: '';
    width: 8px;
    height: 8px;
    background-color: white;
    border-radius: 50%;
  }
}
```

---

## 🔄 Estados e Interações

### Estados de Loading

```css
.loading {
  opacity: 0.7;
  pointer-events: none;
  position: relative;
}

.spinner {
  display: inline-block;
  width: 20px;
  height: 20px;
  border: 2px solid var(--color-gray-300);
  border-top: 2px solid var(--color-primary-600);
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.skeleton {
  background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
  background-size: 200% 100%;
  animation: loading 1.5s infinite;
  border-radius: var(--border-radius-md);
}

@keyframes loading {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}
```

### Estados de Feedback

```css
.alert {
  padding: var(--spacing-4);
  border-radius: var(--border-radius-md);
  border: 1px solid;
  display: flex;
  align-items: flex-start;
  gap: var(--spacing-3);
  
  &.alert--success {
    background-color: var(--color-success-50);
    border-color: var(--color-success-200);
    color: var(--color-success-800);
  }
  
  &.alert--warning {
    background-color: var(--color-warning-50);
    border-color: var(--color-warning-200);
    color: var(--color-warning-800);
  }
  
  &.alert--error {
    background-color: var(--color-error-50);
    border-color: var(--color-error-200);
    color: var(--color-error-800);
  }
  
  &.alert--info {
    background-color: var(--color-primary-50);
    border-color: var(--color-primary-200);
    color: var(--color-primary-800);
  }
}

/* Toast Notifications */
.toast {
  position: fixed;
  top: var(--spacing-4);
  right: var(--spacing-4);
  max-width: 400px;
  padding: var(--spacing-4);
  background-color: white;
  border-radius: var(--border-radius-lg);
  box-shadow: var(--shadow-lg);
  border-left: 4px solid var(--toast-color);
  animation: slideInRight 0.3s ease-out;
  z-index: 1000;
}

@keyframes slideInRight {
  from {
    transform: translateX(100%);
    opacity: 0;
  }
  to {
    transform: translateX(0);
    opacity: 1;
  }
}
```

---

## ♿ Acessibilidade

### Focus States

```css
:root {
  --focus-ring-width: 2px;
  --focus-ring-offset: 2px;
  --focus-ring-color: var(--color-primary-600);
}

.focus-ring {
  &:focus-visible {
    outline: var(--focus-ring-width) solid var(--focus-ring-color);
    outline-offset: var(--focus-ring-offset);
  }
}

/* Estados de foco mais proeminentes para alto contraste */
:root[data-theme="high-contrast"] {
  --focus-ring-width: 3px;
  --focus-ring-color: #0000ff;
}
```

### Skip Links

```css
.skip-link {
  position: absolute;
  top: -40px;
  left: 6px;
  background: var(--color-primary-600);
  color: white;
  padding: 8px;
  text-decoration: none;
  border-radius: 0 0 4px 4px;
  z-index: 100;
  
  &:focus {
    top: 0;
  }
}
```

### Screen Reader Only

```css
.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

.sr-only-focusable:focus,
.sr-only-focusable:active {
  position: static;
  width: auto;
  height: auto;
  padding: inherit;
  margin: inherit;
  overflow: visible;
  clip: auto;
  white-space: inherit;
}
```

### Configurações de Movimento

```css
/* Respeitar preferência por movimento reduzido */
@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}

/* Configuração manual para usuários */
:root[data-motion="reduced"] {
  --animation-duration: 0ms;
  --transition-duration: 0ms;
}
```

---

## 🔠 Iconografia

### Sistema de Ícones

```css
.icon {
  display: inline-block;
  width: 1em;
  height: 1em;
  fill: currentColor;
  vertical-align: middle;
  flex-shrink: 0;
}

.icon--small { width: 16px; height: 16px; }
.icon--medium { width: 24px; height: 24px; }
.icon--large { width: 32px; height: 32px; }

/* Ícones por categoria */
.icon--medication { color: var(--color-medication-primary); }
.icon--work { color: var(--color-work-primary); }
.icon--exercise { color: var(--color-exercise-primary); }
.icon--personal { color: var(--color-personal-primary); }
.icon--appointment { color: var(--color-appointment-primary); }
```

### Ícones Principais

- **Alarmes**: 🔔 ⏰ ⏱️ ⏲️
- **Categorias**: 💊 💼 🏃‍♂️ 👤 📅
- **Estados**: ✅ ⚠️ ❌ 🔄 ⏸️ ▶️
- **Ações**: ✏️ 🗑️ 📋 📤 📥 ⚙️
- **Navegação**: 🏠 📅 📊 👤 ❓

---

## 🎬 Animações e Transições

### Durações Padrão

```css
:root {
  --animation-fast: 0.15s;
  --animation-normal: 0.25s;
  --animation-slow: 0.4s;
  --animation-slower: 0.6s;
  
  --easing-ease: cubic-bezier(0.25, 0.1, 0.25, 1);
  --easing-ease-in: cubic-bezier(0.42, 0, 1, 1);
  --easing-ease-out: cubic-bezier(0, 0, 0.58, 1);
  --easing-bounce: cubic-bezier(0.68, -0.55, 0.265, 1.55);
}
```

### Animações Funcionais

```css
/* Fade In/Out */
.fade-enter {
  opacity: 0;
}
.fade-enter-active {
  opacity: 1;
  transition: opacity var(--animation-normal) var(--easing-ease);
}

/* Slide In */
.slide-enter {
  transform: translateY(-10px);
  opacity: 0;
}
.slide-enter-active {
  transform: translateY(0);
  opacity: 1;
  transition: all var(--animation-normal) var(--easing-ease-out);
}

/* Pulse para notificações importantes */
@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.7; }
}

.pulse {
  animation: pulse 2s infinite;
}

/* Shake para erros */
@keyframes shake {
  0%, 100% { transform: translateX(0); }
  25% { transform: translateX(-4px); }
  75% { transform: translateX(4px); }
}

.shake {
  animation: shake 0.4s ease-in-out;
}
```

---

## 🎨 Temas e Personalização

### Tema Escuro

```css
:root[data-theme="dark"] {
  --color-background-primary: #1f2937;
  --color-background-secondary: #111827;
  --color-background-tertiary: #374151;
  
  --color-text-primary: #f9fafb;
  --color-text-secondary: #d1d5db;
  --color-text-tertiary: #9ca3af;
  
  --color-gray-200: #374151;
  --color-gray-300: #4b5563;
  --color-gray-400: #6b7280;
}
```

### Tema para Daltonismo

```css
:root[data-theme="colorblind-friendly"] {
  /* Paleta otimizada para daltonismo */
  --color-primary-600: #005cc5;    /* Azul mais saturado */
  --color-success-600: #28a745;    /* Verde padrão */
  --color-warning-600: #fd7e14;    /* Laranja em vez de amarelo */
  --color-error-600: #dc3545;      /* Vermelho padrão */
  
  /* Usar padrões além de cores */
  .status-success::before { content: '✓ '; }
  .status-warning::before { content: '⚠ '; }
  .status-error::before { content: '✗ '; }
}
```

### Personalização por Usuário

```css
/* Densidade da interface */
:root[data-density="compact"] {
  --spacing-4: 0.75rem;
  --spacing-6: 1rem;
  --touch-target-comfortable: 40px;
}

:root[data-density="comfortable"] {
  --spacing-4: 1.25rem;
  --spacing-6: 1.75rem;
  --touch-target-comfortable: 52px;
}

/* Preferências de contraste */
:root[data-contrast="high"] {
  --color-gray-300: #000000;
  --border-width-default: 2px;
  filter: contrast(1.2);
}
```

---

## 💻 Implementação Técnica

### Configuração do Tailwind CSS

```javascript
// tailwind.config.js
module.exports = {
  content: ["./src/**/*.{js,jsx,ts,tsx}"],
  darkMode: ['class', '[data-theme="dark"]'],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#eff6ff',
          500: '#3b82f6',
          600: '#2563eb',
          700: '#1d4ed8',
        },
        medication: '#f87171',
        work: '#3b82f6',
        exercise: '#10b981',
        personal: '#8b5cf6',
        appointment: '#f59e0b',
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        dyslexic: ['OpenDyslexic', 'Comic Neue', 'cursive'],
      },
      spacing: {
        'touch': '44px',
        'touch-comfortable': '48px',
        'touch-large': '56px',
      },
      animation: {
        'fade-in': 'fadeIn 0.25s ease-out',
        'slide-up': 'slideUp 0.25s ease-out',
        'pulse-gentle': 'pulseGentle 2s infinite',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        slideUp: {
          '0%': { transform: 'translateY(10px)', opacity: '0' },
          '100%': { transform: 'translateY(0)', opacity: '1' },
        },
        pulseGentle: {
          '0%, 100%': { opacity: '1' },
          '50%': { opacity: '0.8' },
        },
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography'),
  ],
}
```

### React Hook para Tema

```typescript
// hooks/useTheme.ts
import { useState, useEffect } from 'react';

interface ThemeSettings {
  theme: 'light' | 'dark' | 'high-contrast';
  fontPreference: 'default' | 'dyslexic' | 'large';
  density: 'compact' | 'comfortable' | 'spacious';
  motion: 'normal' | 'reduced';
  contrast: 'normal' | 'high';
}

export const useTheme = () => {
  const [settings, setSettings] = useState<ThemeSettings>(() => {
    const saved = localStorage.getItem('smart-alarm-theme');
    return saved ? JSON.parse(saved) : {
      theme: 'light',
      fontPreference: 'default',
      density: 'comfortable',
      motion: 'normal',
      contrast: 'normal',
    };
  });

  useEffect(() => {
    const root = document.documentElement;
    
    // Aplicar configurações ao root
    root.setAttribute('data-theme', settings.theme);
    root.setAttribute('data-font-preference', settings.fontPreference);
    root.setAttribute('data-density', settings.density);
    root.setAttribute('data-motion', settings.motion);
    root.setAttribute('data-contrast', settings.contrast);
    
    // Salvar no localStorage
    localStorage.setItem('smart-alarm-theme', JSON.stringify(settings));
  }, [settings]);

  const updateTheme = (updates: Partial<ThemeSettings>) => {
    setSettings(prev => ({ ...prev, ...updates }));
  };

  return { settings, updateTheme };
};
```

### Componente de Configuração de Tema

```typescript
// components/ThemeSelector.tsx
import React from 'react';
import { useTheme } from '../hooks/useTheme';

export const ThemeSelector: React.FC = () => {
  const { settings, updateTheme } = useTheme();

  return (
    <div className="space-y-6">
      <div className="form-group">
        <label className="form-label">Tema Visual</label>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          {[
            { value: 'light', label: 'Claro', icon: '☀️' },
            { value: 'dark', label: 'Escuro', icon: '🌙' },
            { value: 'high-contrast', label: 'Alto Contraste', icon: '🔳' }
          ].map(({ value, label, icon }) => (
            <button
              key={value}
              className={`btn btn--secondary ${
                settings.theme === value ? 'btn--primary' : ''
              }`}
              onClick={() => updateTheme({ theme: value as any })}
            >
              <span className="mr-2">{icon}</span>
              {label}
            </button>
          ))}
        </div>
      </div>

      <div className="form-group">
        <label className="form-label">Preferência de Fonte</label>
        <select
          className="form-input"
          value={settings.fontPreference}
          onChange={(e) => updateTheme({ fontPreference: e.target.value as any })}
        >
          <option value="default">Padrão</option>
          <option value="dyslexic">Otimizada para Dislexia</option>
          <option value="large">Tamanho Ampliado</option>
        </select>
      </div>

      <div className="form-group">
        <label className="form-label">Densidade da Interface</label>
        <div className="flex gap-2">
          {[
            { value: 'compact', label: 'Compacta' },
            { value: 'comfortable', label: 'Confortável' },
            { value: 'spacious', label: 'Espaçosa' }
          ].map(({ value, label }) => (
            <button
              key={value}
              className={`btn ${
                settings.density === value ? 'btn--primary' : 'btn--secondary'
              }`}
              onClick={() => updateTheme({ density: value as any })}
            >
              {label}
            </button>
          ))}
        </div>
      </div>

      <div className="form-group">
        <label className="form-label flex items-center">
          <input
            type="checkbox"
            className="form-checkbox mr-3"
            checked={settings.motion === 'reduced'}
            onChange={(e) => updateTheme({ 
              motion: e.target.checked ? 'reduced' : 'normal' 
            })}
          />
          Reduzir animações e movimento
        </label>
        <p className="form-help">
          Minimiza animações para reduzir distração e desconforto visual
        </p>
      </div>
    </div>
  );
};
```

---

## 📋 Checklist de Implementação

### ✅ Fundação

- [ ] Definir variáveis CSS customizadas para cores
- [ ] Configurar Tailwind com tokens do design system
- [ ] Implementar sistema de temas (claro/escuro/alto contraste)
- [ ] Configurar fontes (Inter, OpenDyslexic)
- [ ] Definir breakpoints responsivos

### ✅ Componentes Base

- [ ] Criar componentes de botão com todas as variações
- [ ] Implementar sistema de cards
- [ ] Desenvolver componentes de formulário acessíveis
- [ ] Criar sistema de feedback (alerts, toasts)
- [ ] Implementar estados de loading

### ✅ Acessibilidade

- [ ] Configurar focus rings consistentes
- [ ] Implementar skip links
- [ ] Criar utilitários screen-reader-only
- [ ] Testar navegação por teclado
- [ ] Validar contraste de cores (WCAG AA)
- [ ] Implementar suporte a motion preferences

### ✅ Personalização

- [ ] Criar hook useTheme
- [ ] Implementar seletor de temas
- [ ] Desenvolver configurações de densidade
- [ ] Adicionar opções de tipografia
- [ ] Testar persistência de configurações

### ✅ Testes

- [ ] Testar com leitores de tela
- [ ] Validar em diferentes navegadores
- [ ] Verificar responsividade em todos os breakpoints
- [ ] Testar performance de animações
- [ ] Validar acessibilidade com usuários reais

---

## 📚 Recursos e Referências

### Guidelines de Acessibilidade

- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [Inclusive Components by Heydon Pickering](https://inclusive-components.design/)
- [A11y Project Checklist](https://www.a11yproject.com/checklist/)

### Ferramentas de Design

- [Contrast Ratio Checker](https://webaim.org/resources/contrastchecker/)
- [Color Oracle (Colorblind Simulator)](https://colororacle.org/)
- [axe DevTools](https://www.deque.com/axe/devtools/)

### Fonts e Recursos

- [Inter Font Family](https://rsms.me/inter/)
- [OpenDyslexic Font](https://opendyslexic.org/)
- [Lucide Icons](https://lucide.dev/)

---

**🎯 Lembre-se**: Este design system foi criado com foco em **inclusão e acessibilidade**. Cada decisão de design deve considerar o impacto em usuários com diferentes necessidades, capacidades e preferências. A beleza está na funcionalidade que realmente serve a todos os usuários.
