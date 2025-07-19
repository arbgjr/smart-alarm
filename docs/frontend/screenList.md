# 🖥️ Smart Alarm - Telas do Sistema

## 📱 **Telas Principais (MVP)**

### 1. **🏠 Tela Principal / Dashboard**

- **Visão Hoje**: Lista dos alarmes do dia atual
- **Próximos Compromissos**: Visão das próximas horas/eventos
- **Resumo Rápido**: Status geral dos alarmes ativos
- **Ações Rápidas**: Botões para criar novo alarme, ver calendário
- **PWA Features**: Atalhos para medicamentos, agenda do dia

### 2. **📅 Tela do Calendário**

- **Visualização Múltipla**: 
  - Visão Mensal (grade completa)
  - Visão Semanal (detalhes da semana)  
  - Visão Diária (foco no dia)
  - **Modo Lista**: Alternativa linear para usuários com dificuldades cognitivas
- **Interatividade**:
  - Drag & Drop para mover alarmes
  - Clique em datas para criar novos alarmes
  - Navegação por teclado completa
- **Acessibilidade**: 
  - Alto contraste opcional
  - Movimento reduzido
  - Compatibilidade com leitores de tela

### 3. **⏰ Tela de Gerenciamento de Alarmes**

- **Lista de Alarmes**: Cards organizados com todos os alarmes
- **Filtros e Busca**: Por categoria, horário, status
- **Ações Rápidas**: Editar, duplicar, ativar/desativar
- **Importação CSV**: Modal para upload de alarmes em lote
- **Categorização**: Medicamentos, trabalho, exercício, geral

### 4. **🔧 Tela de Configurações de Alarme** 

- **Formulário de Criação/Edição**:
  - Nome e descrição
  - Data e horário
  - Recorrência (diário, semanal, personalizado)
  - Categoria e prioridade
- **Configurações de Notificação**:
  - Tipos: visual, sonoro, vibração
  - Volume e intensidade ajustáveis
  - Preview/teste das configurações
- **Acessibilidade**: Formulários otimizados para diferentes necessidades

### 5. **🔐 Tela de Login/Autenticação**

- **Login Tradicional**: Email/senha com validação
- **Autenticação Biométrica**: FIDO2/WebAuthn quando disponível
- **Recursos**: 
  - "Lembrar de mim"
  - Recuperação de senha
  - Link para registro
- **Segurança**: Validações em tempo real, feedback claro

## 📋 **Telas de Configuração Avançada**

### 6. **🎛️ Tela de Configurações do Sistema**

- **Preferências de Acessibilidade**:
  - Modo alto contraste
  - Fontes para dislexia
  - Redução de movimento
  - Configurações de leitor de tela
- **Configurações de Notificação**:
  - Permissões do navegador
  - Configuração de PWA
  - Testes de som e vibração
- **Preferências de Interface**:
  - Tema claro/escuro
  - Layout preferido (grade/lista)
  - Densidade de informações

### 7. **🎉 Tela de Gerenciamento de Feriados**

- **Lista de Feriados**: Cards com feriados nacionais e locais
- **CRUD Completo**: Criar, editar, excluir feriados personalizados
- **Filtros**: Por tipo (nacional, religioso, pessoal), data
- **Integração**: Como feriados afetam alarmes automático

### 8. **⚙️ Tela de Preferências de Feriados do Usuário**

- **Configuração Individual**: Como cada feriado deve afetar os alarmes
- **Ações Disponíveis**:
  - Desabilitar alarmes
  - Atrasar por tempo específico  
  - Pular completamente
- **Gestão**: Toggle para ativar/desativar regras rapidamente

### 9. **📅 Tela de Períodos de Exceção**

- **Gestão de Períodos Especiais**:
  - Férias, viagens, licenças médicas
  - Trabalho remoto, manutenção
  - Períodos personalizados
- **Configuração**: Como alarmes se comportam durante exceções
- **Calendário Visual**: Visualização dos períodos no calendário

## 🔧 **Telas de Utilitários**

### 10. **📤 Modal de Importação de Alarmes**

- **Upload de Arquivo**: Drag & drop ou seleção de CSV
- **Validação em Tempo Real**: Verificação do formato
- **Preview**: Visualização dos alarmes antes da importação
- **Resultado**: Relatório detalhado do que foi importado
- **Tratamento de Erros**: Lista clara de problemas encontrados

### 11. **📊 Tela de Estatísticas/Insights**

- **AI Insights**: Padrões de uso e recomendações
- **Estatísticas Pessoais**: Taxa de cumprimento, horários mais usados
- **Recomendações**: Sugestões para otimizar alarmes
- **Histórico**: Evolução dos hábitos ao longo do tempo

## 🌐 **Interface Responsiva**

### **💻 Desktop (≥1024px)**

- Layout em duas colunas
- Sidebar com filtros e navegação
- Modais centralizados
- Todas as funcionalidades visíveis

### **📱 Tablet (768px-1023px)**  

- Layout em coluna única
- Cards compactos
- Modais full-screen
- Navegação por abas

### **📱 Mobile (<768px)**

- Stack vertical
- Cards simplificados
- Formulários em tela cheia
- Menu hamburger
- Gestos touch otimizados

## 🎨 **Características de Acessibilidade**

### **Inclusão Visual**

- Alto contraste opcional
- Fontes especiais para dislexia
- Indicadores visuais claros
- Cores que funcionam para daltonismo

### **Navegação Alternativa**

- 100% navegável por teclado
- Suporte a leitores de tela
- Modo lista como alternativa ao calendário
- Comandos de atalho personalizáveis

### **Personalização Cognitiva**

- Movimento reduzido disponível
- Interfaces simplificadas
- Confirmações claras
- Feedback imediato em todas as ações

---

**💡 Resumo**: O Smart Alarm possui **11 telas/modais principais** projetadas com foco em acessibilidade, cada uma adaptável para diferentes necessidades dos usuários e completamente responsiva para todos os dispositivos.