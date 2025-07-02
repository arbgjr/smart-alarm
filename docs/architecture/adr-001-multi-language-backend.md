# ADR-001: Arquitetura Webapp de Alarmes Inteligentes para Usuários Neurodivergentes

**Status:** Aceito  
**Data:** 2025-01-02  
**Autores:** Equipe de Desenvolvimento  
**Decisores:** Product Owner, Tech Lead  

## Contexto e Problema

Estamos desenvolvendo um webapp de alarmes inteligentes especificamente projetado para usuários neurodivergentes (ADHD, TEA). O sistema deve combinar funcionalidade de alarmes com interface visual de calendário, incorporando IA para análise comportamental e sugestões contextuais.

Os principais desafios arquiteturais identificados incluem a necessidade de alta confiabilidade para alarmes críticos (medicação), interface acessível para diferentes tipos de neurodiversidade, funcionamento offline robusto, segurança OWASP-compliant para dados sensíveis de saúde mental, e compliance com LGPD para informações de neurodiversidade.

O modelo de negócio exige arquitetura open source com opção managed service, implementação de Bring Your Own Key (BYOK) para IA, e custos operacionais mínimos durante a fase de validação de mercado.

## Decisões Arquiteturais

### Frontend: React 18 + TypeScript + PWA

**Decisão:** Utilizaremos React 18 com TypeScript como base do frontend, implementando Progressive Web App (PWA) para funcionalidade offline e notificações.

**Justificativa:** React oferece o maior ecossistema de componentes acessíveis disponível, facilitando implementação de features específicas para neurodiversidade. TypeScript reduz significativamente bugs relacionados a tipos de dados, crítico quando lidamos com informações de saúde mental. PWA permite funcionalidade offline essencial para alarmes e notificações confiáveis across platforms.

O suporte nativo do React para server-side rendering e code splitting permite otimizações de performance importantes para usuários com limitações de conectividade ou dispositivos menos potentes, comum na população neurodivergente que pode ter restrições financeiras.

### Biblioteca de Calendário: React Big Calendar (MVP) → FullCalendar Premium (Escala)

**Decisão:** Começaremos com React Big Calendar para MVP, migrando para FullCalendar Premium quando revenue justificar o investimento.

**Justificativa:** React Big Calendar oferece 80% das funcionalidades necessárias gratuitamente, incluindo drag-and-drop nativo e múltiplas views (mês, semana, dia). Esta decisão alinha com nossa estratégia de custos mínimos durante validação.

FullCalendar Premium ($480/ano) será considerado quando precisarmos de Timeline views específicas para visualização de padrões temporais complexos ou quando tivermos base de usuários pagantes que justifique o investimento. A migração é relativamente simples devido às APIs similares.

### Backend: Arquitetura Híbrida Multi-Linguagem

**Decisão:** Implementaremos uma arquitetura serverless híbrida com três tecnologias especializadas por domínio de responsabilidade: Go para operações CRUD de alarmes, Python para processamento de IA e análise comportamental, e Node.js para integração com APIs de terceiros.

**Justificativa:** Esta abordagem otimiza cada componente para sua função específica, maximizando eficiência de custos e performance. Funções Go oferecem cold starts de 50-100ms e menor consumo de memória para operações CRUD frequentes, representando economia significativa considerando que essas operações constituem 70-80% do tráfego. Python aproveita ecossistema superior de machine learning (scikit-learn, pandas, TensorFlow) para análise de padrões neurodivergentes. Node.js capitaliza maturidade do ecossistema NPM para integrações complexas com calendários externos, notificações push, e APIs de terceiros.

**Orquestração entre serviços:** Comunicação será implementada via eventos assíncronos (OCI Events Service) para operações não-críticas e chamadas HTTP diretas para operações síncronas que requerem resposta imediata. API Gateway central coordenará requests externos e roteará para funções apropriadas baseado no tipo de operação.

### Cloud Provider: Oracle Cloud Infrastructure (OCI)

**Decisão:** OCI será nosso provider principal para todas as funcionalidades serverless e database.

**Justificativa:** Análise de custos demonstrou economia de 70% comparado a AWS para cargas similares. OCI oferece 10TB data egress gratuitos mensalmente versus $0.09/GB após limites mínimos em outros providers. Para aplicação global, isto representa economia significativa.

O Always Free Tier da OCI (2 milhões invocações permanentemente gratuitas) permite desenvolvimento e testing extensivo sem custos operacionais. Embora o ecossistema seja menor que AWS, funcionalidades necessárias (Functions, Autonomous Database, Object Storage) estão maduras e bem documentadas.

### Database: Oracle Autonomous Database (JSON Document Store)

**Decisão:** Utilizaremos Oracle Autonomous Database configurado como JSON Document Store para dados principais.

**Justificativa:** Esta solução oferece flexibilidade NoSQL para dados não-estruturados (configurações personalizadas de neurodiversidade) com capacidade SQL quando necessário para analytics e relatórios.

O modelo converged database elimina necessidade de múltiplos sistemas de dados, reduzindo complexidade operacional e custos. Time-to-Live (TTL) nativo permite expiração automática de alarmes antigos, importante para compliance LGPD.

### Autenticação e Segurança: FIDO2/WebAuthn + AES-256-GCM

**Decisão:** Implementaremos autenticação passwordless via FIDO2/WebAuthn com criptografia AES-256-GCM para dados em repouso.

**Justificativa:** Usuários neurodivergentes frequentemente enfrentam dificuldades com gerenciamento de senhas complexas. WebAuthn oferece autenticação mais acessível via biometrics ou hardware keys.

AES-256-GCM com chaves derivadas via PBKDF2 (100,000 iterações) garante segurança OWASP-compliant. Para dados de neurodiversidade, implementaremos técnicas de k-anonimato e privacidade diferencial para analytics que preservem privacidade individual.

### PWA e Notificações: Service Workers + Firebase Cloud Messaging (Fallback)

**Decisão:** Service Workers como mecanismo primário de notificações, com FCM como fallback para browsers limitados.

**Justificativa:** Service Workers oferecem controle total sobre notificações e funcionamento offline. iOS Safari tem limitações significativas, mas PWA instalada na home screen contorna a maioria das restrições.

FCM como fallback garante que notificações funcionem mesmo em browsers com throttling agressivo de background processing. Estratégia de redundância é crítica para alarmes relacionados a medicação onde falhas podem ter consequências sérias.

### IA e Análise Comportamental: TensorFlow.js (Local) + Federated Learning (Agregado)

**Decisão:** Processamento de IA principalmente local via TensorFlow.js, com Federated Learning opcional para melhorias coletivas.

**Justificativa:** Processamento local preserva privacidade máxima de dados sensíveis de neurodiversidade. TensorFlow.js permite análise de padrões temporais (LSTM/GRU) diretamente no browser.

Federated Learning permite que modelos melhorem coletivamente sem exposição de dados individuais, respeitando privacidade while beneficiando comunidade neurodivergente com insights agregados de padrões de uso.

### APIs Externas: Calendarific (Feriados) + APIs de Fuso Horário

**Decisão:** Calendarific para dados de feriados globais, APIs nativas de browser para detecção de fuso horário.

**Justificativa:** Calendarific oferece cobertura de 230+ países com pricing escalável, essential para feature de desativação automática de alarmes em feriados. Starts at $9/mês para uso comercial.

APIs nativas de browser (Intl.DateTimeFormat) eliminam dependências externas para fuso horário, reduzindo pontos de falha e melhorando performance offline.

### Licenciamento: Business Source License 1.1

**Decisão:** Core será lançado sob Business Source License 1.1, transitioning para GPL após 4 anos.

**Justificativa:** BSL permite código aberto para desenvolvimento e teste, mas protege contra competitors comerciais diretos por 4 anos. Após este período, transição automática para GPL garante contribuição para comunidade open source.

Arquitetura híbrida mantém core open source mientras enterprise features (BYOK, advanced analytics) permanecem proprietárias, permitindo sustentabilidade financeira.

### Sincronização Offline: Dexie.js + CRDT (Conflict-free Replicated Data Types)

**Decisão:** Dexie.js para storage local com CRDT para conflict resolution automático.

**Justificativa:** Dexie.js oferece interface moderna sobre IndexedDB com support para versioning e migrations. Storage Persistence API garante que dados não sejam removidos por browser cleanup automático.

CRDT com Last-Write-Wins pattern oferece conflict resolution automático quando multiple devices modificam mesmos alarmes offline. Para alarmes, priority rules (habilitado > desabilitado, horário anterior > posterior) resolvem conflicts deterministicamente.

## Alternativas Consideradas

### AWS Lambda + DynamoDB
Rejeitado devido a custos 70% superiores para carga similar e lock-in vendor mais agressivo. DynamoDB exige modelagem de dados mais rígida, complicando desenvolvimento inicial.

### Azure Cosmos DB + Functions
Pricing baseado em Request Units mostrou-se mais caro para padrões de uso esperados. Vantagem de SQL queries não justifica custo adicional para MVP.

### FullCalendar Premium desde início
$480/ano representaria 10-15% do budget inicial estimado. React Big Calendar oferece funcionalidades suficientes para validação de produto.

### Autenticação tradicional com senhas
Rejected por impacto negativo em acessibilidade para usuários neurodivergentes. WebAuthn oferece melhor experiência de usuário e segurança superior.

## Impactos da Decisão

### Técnicos
Arquitetura permite desenvolvimento ágil inicial com Node.js/TypeScript, evolution path clear para components especializados (Python para IA). PWA garante funcionamento cross-platform sem development native apps.

### Financeiros
Stack escolhido mantém custos operacionais under $50/mês para primeiros 10k usuários, allowing extensive validation period antes de major investments.

### Experiência do Usuário
Interface de calendário torna alarmes mais intuitivos para usuários neurodivergentes comparado a listas tradicionais. Funcionamento offline elimina ansiedade relacionada a connectivity issues.

### Compliance e Segurança
Arquitetura atende requisitos OWASP Top 10 e LGPD para dados de saúde mental. Processamento local de IA minimiza exposure de dados sensíveis.

## Riscos e Mitigações

### Risco: Complexidade operacional de múltiplas linguagens
**Mitigação:** Implementar CI/CD padronizado com containers para todas as linguagens, usando Infrastructure as Code (OCI Resource Manager) para deployment consistente. Estabelecer padrões de logging e monitoring uniformes através de ferramentas como OpenTelemetry que funcionam across linguagens.

### Risco: Latência na comunicação entre funções
**Mitigação:** Implementar cache Redis/ElastiCache para dados frequentemente acessados, minimizar chamadas síncronas entre funções, e usar event-driven architecture para operações que podem ser assíncronas.

### Risco: Oracle descontinuando ou alterando pricing de OCI
**Mitigação:** Arquitectura baseada em containers e APIs padrão facilita migração futura. Manter abstraction layer para cloud services.

### Risco: Limitações de PWA em iOS Safari
**Mitigação:** Capacitor como fallback para wrapper nativo mantendo codebase web. Extensive testing em dispositivos iOS reais.

### Risco: Complexidade de CRDT para conflict resolution
**Mitigação:** Implementação incremental starting com Last-Write-Wins simples, evolving para CRDT apenas se conflitos se tornarem problemáticos.

### Risco: Performance de TensorFlow.js em dispositivos antigos
**Mitigação:** Fallback para processing server-side via Python functions quando device capabilities são insuficientes.

## Decisões Futuras Necessárias

Implementação específica de BYOK (Bring Your Own Key) architecture para serviços de IA externos requer research adicional sobre envelope encryption e key management.

Estratégia de monetização entre planos gratuitos/pagos needs validation com early users antes de finalizar features premium vs core.

Integration com healthcare systems (FHIR compatibility) pode ser considerada depois de market validation para enterprise adoption.

## Critérios de Sucesso

Lighthouse Score > 90 em todas categorias para performance web. Notification reliability > 95% cross-platform para alarmes críticos. WCAG 2.1 AA compliance para acessibilidade. Time to create alarm < 30 segundos para UX optimization.

Budget operacional maintenance under $100/mês para primeiros 6 meses allowing sustainable development cycle com feedback loop de usuários reais.

---

*Esta ADR será revisada após primeiros 3 meses de development ou quando major assumptions forem invalidadas por dados de uso real.*