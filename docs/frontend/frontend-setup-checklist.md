# Frontend Development Setup Checklist

## 📋 Pré-Requisitos Completos para Início do Desenvolvimento

Além do **Design System** e **Lista de Telas**, você precisa dos seguintes elementos:

---

## 1. 🏗️ **Configuração Inicial do Projeto**

### Criar Projeto Base
```bash
# Usando Vite (recomendado para performance)
npm create vite@latest smart-alarm-frontend -- --template react-ts
cd smart-alarm-frontend
npm install
```

### Estrutura de Pastas Necessária
```
src/
├── components/              # Componentes reutilizáveis
│   ├── atoms/              # Botões, inputs, labels
│   ├── molecules/          # Cards, formulários simples
│   ├── organisms/          # Header, sidebar, calendário
│   └── templates/          # Layouts de página
├── pages/                  # Páginas completas
├── hooks/                  # Custom hooks
│   ├── useAlarms.ts
│   ├── useAuth.ts
│   ├── useOfflineSync.ts
│   └── useNotifications.ts
├── services/               # APIs e serviços externos
│   ├── api/               # Comunicação com backend
│   ├── storage/           # IndexedDB/Dexie
│   └── notifications/     # Service Workers
├── contexts/              # Context API para estado global
├── types/                 # TypeScript types
├── utils/                 # Funções auxiliares
├── styles/               # CSS/Tailwind customizações
└── workers/              # Service Workers para PWA
```

---

## 2. 📦 **Instalação de Dependências**

### Core Dependencies
```bash
# UI e Calendar
npm install react-big-calendar @dnd-kit/core @dnd-kit/modifiers
npm install lucide-react @headlessui/react

# Formulários e Validação
npm install react-hook-form @hookform/resolvers zod

# Storage e Offline
npm install dexie workbox-webpack-plugin

# Styling
npm install tailwindcss @tailwindcss/forms @tailwindcss/typography autoprefixer postcss

# PWA
npm install workbox-webpack-plugin
```

### Development Dependencies
```bash
# Tipos TypeScript
npm install -D @types/react @types/react-dom

# Linting e Formatação
npm install -D eslint-plugin-jsx-a11y prettier eslint-config-prettier

# Testing
npm install -D vitest @testing-library/react @testing-library/jest-dom
npm install -D @testing-library/user-event jsdom
```

---

## 3. ⚙️ **Arquivos de Configuração**

### 3.1 Tailwind Config (`tailwind.config.js`)
```javascript
/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{js,jsx,ts,tsx}"],
  theme: {
    extend: {
      // Cores do Design System
      colors: {
        'smart-primary': {
          50: '#f0f9ff',
          500: '#3b82f6',
          600: '#2563eb',
          700: '#1d4ed8',
        },
        'smart-neutral': {
          50: '#f9fafb',
          100: '#f3f4f6',
          500: '#6b7280',
          900: '#111827',
        },
      },
      // Fontes para acessibilidade
      fontFamily: {
        'primary': ['Inter', 'system-ui', 'sans-serif'],
        'dyslexic': ['OpenDyslexic', 'Arial', 'sans-serif'],
      },
      // Animações reduzidas para acessibilidade
      animation: {
        'reduced-motion': 'none',
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography'),
  ],
}
```

### 3.2 Vite Config (`vite.config.ts`)
```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      workbox: {
        globPatterns: ['**/*.{js,css,html,ico,png,svg}']
      },
      manifest: {
        name: 'Smart Alarm',
        short_name: 'SmartAlarm',
        description: 'Intelligent alarm system for everyone',
        theme_color: '#3b82f6',
        icons: [
          {
            src: 'pwa-192x192.png',
            sizes: '192x192',
            type: 'image/png'
          }
        ]
      }
    })
  ],
  resolve: {
    alias: {
      '@': '/src',
      '@components': '/src/components',
      '@hooks': '/src/hooks',
      '@services': '/src/services',
      '@types': '/src/types',
    }
  }
})
```

### 3.3 TypeScript Config (`tsconfig.json`)
```json
{
  "compilerOptions": {
    "target": "ES2020",
    "useDefineForClassFields": true,
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    "noEmit": true,
    "jsx": "react-jsx",
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true,
    "baseUrl": ".",
    "paths": {
      "@/*": ["src/*"],
      "@components/*": ["src/components/*"],
      "@hooks/*": ["src/hooks/*"],
      "@services/*": ["src/services/*"],
      "@types/*": ["src/types/*"]
    }
  },
  "include": ["src"],
  "references": [{ "path": "./tsconfig.node.json" }]
}
```

### 3.4 ESLint Config (`.eslintrc.json`)
```json
{
  "extends": [
    "eslint:recommended",
    "@typescript-eslint/recommended",
    "plugin:react-hooks/recommended",
    "plugin:jsx-a11y/recommended"
  ],
  "plugins": ["jsx-a11y"],
  "rules": {
    "jsx-a11y/no-distracting-elements": "error",
    "jsx-a11y/media-has-caption": "error",
    "jsx-a11y/no-autofocus": "error",
    "jsx-a11y/aria-role": "error"
  }
}
```

---

## 4. 📱 **Configuração PWA**

### Service Worker Base (`public/sw.js`)
```javascript
// Service Worker básico para notificações
self.addEventListener('push', (event) => {
  const data = event.data?.json() ?? {}
  const options = {
    body: data.body,
    icon: '/pwa-192x192.png',
    badge: '/badge-72x72.png',
    vibrate: [200, 100, 200],
    data: {
      dateOfArrival: Date.now(),
      primaryKey: data.primaryKey
    },
    actions: [
      {
        action: 'explore',
        title: 'Ver Alarme',
        icon: '/images/checkmark.png'
      },
      {
        action: 'close',
        title: 'Fechar',
        icon: '/images/xmark.png'
      }
    ]
  }

  event.waitUntil(
    self.registration.showNotification(data.title, options)
  )
})
```

### Manifest PWA (`public/manifest.json`)
```json
{
  "name": "Smart Alarm - Intelligent Alarm System",
  "short_name": "SmartAlarm",
  "description": "Sistema inteligente de alarmes para todos",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#3b82f6",
  "orientation": "portrait-primary",
  "categories": ["productivity", "health"],
  "icons": [
    {
      "src": "/pwa-64x64.png",
      "sizes": "64x64",
      "type": "image/png"
    },
    {
      "src": "/pwa-192x192.png",
      "sizes": "192x192",
      "type": "image/png"
    },
    {
      "src": "/pwa-512x512.png",
      "sizes": "512x512",
      "type": "image/png",
      "purpose": "any maskable"
    }
  ]
}
```

---

## 5. 🔧 **Tipos TypeScript Base**

### Core Types (`src/types/index.ts`)
```typescript
// Tipos base para alarmes
export interface Alarm {
  id: string
  title: string
  description?: string
  datetime: Date
  isActive: boolean
  repeatPattern?: RepeatPattern
  sound?: AlarmSound
  createdAt: Date
  updatedAt: Date
}

export interface RepeatPattern {
  type: 'daily' | 'weekly' | 'monthly' | 'custom'
  interval: number
  daysOfWeek?: number[]
  endDate?: Date
}

export interface AlarmSound {
  name: string
  url: string
  volume: number
}

// Props para componentes principais
export interface AlarmFormProps {
  alarm?: Alarm
  onSubmit: (alarm: Partial<Alarm>) => void
  onCancel: () => void
}

export interface CalendarProps {
  alarms: Alarm[]
  onAlarmClick: (alarm: Alarm) => void
  onDateSelect: (date: Date) => void
}
```

---

## 6. 🗄️ **Configuração de Storage**

### Dexie Database (`src/services/storage/db.ts`)
```typescript
import Dexie, { Table } from 'dexie'
import { Alarm } from '@types'

export class SmartAlarmDB extends Dexie {
  alarms!: Table<Alarm>

  constructor() {
    super('SmartAlarmDB')
    this.version(1).stores({
      alarms: 'id, title, datetime, isActive, createdAt'
    })
  }
}

export const db = new SmartAlarmDB()
```

---

## 7. 🎯 **Custom Hooks Essenciais**

### useAlarms Hook (`src/hooks/useAlarms.ts`)
```typescript
import { useState, useEffect } from 'react'
import { Alarm } from '@types'
import { db } from '@services/storage/db'

export const useAlarms = () => {
  const [alarms, setAlarms] = useState<Alarm[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const loadAlarms = async () => {
    setLoading(true)
    try {
      const data = await db.alarms.orderBy('datetime').toArray()
      setAlarms(data)
    } catch (err) {
      setError('Erro ao carregar alarmes')
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const createAlarm = async (alarmData: Omit<Alarm, 'id' | 'createdAt' | 'updatedAt'>) => {
    try {
      const id = await db.alarms.add({
        ...alarmData,
        id: crypto.randomUUID(),
        createdAt: new Date(),
        updatedAt: new Date()
      })
      await loadAlarms()
      return id
    } catch (err) {
      setError('Erro ao criar alarme')
      throw err
    }
  }

  useEffect(() => {
    loadAlarms()
  }, [])

  return {
    alarms,
    loading,
    error,
    createAlarm,
    loadAlarms
  }
}
```

---

## 8. 🧪 **Configuração de Testes**

### Vitest Config (`vitest.config.ts`)
```typescript
import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
  resolve: {
    alias: {
      '@': '/src',
    }
  }
})
```

### Test Setup (`src/test/setup.ts`)
```typescript
import '@testing-library/jest-dom'
import 'fake-indexeddb/auto'
```

---

## 9. 🚀 **Scripts Package.json**

### Scripts Essenciais
```json
{
  "scripts": {
    "dev": "vite",
    "build": "tsc && vite build",
    "preview": "vite preview",
    "test": "vitest",
    "test:ui": "vitest --ui",
    "lint": "eslint . --ext ts,tsx --report-unused-disable-directives --max-warnings 0",
    "lint:fix": "eslint . --ext ts,tsx --fix",
    "format": "prettier --write \"src/**/*.{ts,tsx}\"",
    "type-check": "tsc --noEmit"
  }
}
```

---

## 10. ✅ **Checklist Final**

Antes de começar a codificar, confirme que você tem:

- [ ] ✅ **Design System** completo
- [ ] ✅ **Lista de Telas** definida
- [ ] 🏗️ **Estrutura de projeto** criada
- [ ] 📦 **Dependencies** instaladas
- [ ] ⚙️ **Configurações** (Tailwind, Vite, TypeScript, ESLint)
- [ ] 📱 **PWA** configurado (manifest, service worker)
- [ ] 🔧 **Tipos TypeScript** base definidos
- [ ] 🗄️ **Storage** (Dexie) configurado
- [ ] 🎯 **Custom Hooks** base implementados
- [ ] 🧪 **Testes** configurados
- [ ] 📄 **Scripts** de desenvolvimento prontos

## 🎯 **Próximos Passos**

Com tudo configurado, você pode começar desenvolvendo:

1. **Componentes Atômicos** (Button, Input, Label)
2. **Formulário de Alarme** (molécula)
3. **Lista de Alarmes** (organismo)
4. **Tela Principal** (template/página)
5. **Funcionalidades PWA** (notificações, offline)

---

**💡 Dica**: Use o desenvolvimento híbrido - 30% vibe coding para UX/UI e arquitetura, 70% AI-assisted para implementação de funcionalidades!
