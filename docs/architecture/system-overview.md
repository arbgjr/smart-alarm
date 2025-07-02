# Stack Técnico Completo para WebApp de Alarmes Inteligentes Neurodivergentes

Este relatório apresenta recomendações técnicas específicas para implementar um webapp de alarmes inteligentes com foco em usuários neurodivergentes, considerando todos os aspectos arquiteturais, de segurança, IA e compliance solicitados.

## Stack tecnológico recomendado

**Frontend**: React 18 + TypeScript oferece o melhor equilíbrio entre performance, ecossistema de componentes acessíveis e suporte PWA robusto. **FullCalendar** emerge como a solução superior para interface visual, com 300+ configurações, drag-and-drop nativo e suporte completo à acessibilidade WCAG.

**Backend**: AWS Lambda + DynamoDB fornece a arquitetura serverless ideal, com custo estimado de apenas $0.00174/usuário/mês para 10k usuários. DynamoDB oferece TTL nativo para alarmes expirados e performance consistente em qualquer escala.

**PWA e Notificações**: Service Workers + FCM como fallback criam a estratégia de redundância necessária para alarmes confiáveis. A implementação de Web Locks API + Wake Lock API pode manter alarmes ativos mesmo com limitações de background processing.

## Arquitetura de segurança OWASP e LGPD

A implementação de segurança robusta exige múltiplas camadas de proteção. **AES-256-GCM** para dados em repouso, **TLS 1.3** para dados em trânsito, e **FIDO2/WebAuthn** para autenticação sem senha criam uma base sólida. Para dados de neurodiversidade, técnicas de **k-anonimato** e **privacidade diferencial** preservam privacidade enquanto permitem analytics úteis.

O **BYOK** para IA utiliza envelope encryption: dados são criptografados com chaves do cliente (DEK), que são protegidas por chaves mestras em HSMs do cliente. Isso garante que nem mesmo você acesse dados sensíveis sem autorização explícita.

Para LGPD compliance, implementar sistema de consentimento granular, direitos dos titulares automatizados, e "cryptographic erasure" via revogação de chaves atende todos os requisitos legais específicos para dados de saúde mental.

## Implementação de alarmes browser-reliable

Alarmes confiáveis em PWA requerem estratégia multicamada devido às limitações de background processing. **iOS Safari** é particularmente restritivo, exigindo que PWAs sejam adicionadas à home screen para notificações funcionarem.

A arquitetura recomendada combina: Service Workers para notificações padrão, Web Locks + Wake Lock para alarmes críticos, FCM para backup server-side, e polling fallback para browsers limitados. Heartbeat a cada 30 segundos com servidor mantém sincronização mesmo com throttling agressivo.

Para máxima confiabilidade, considere **Capacitor** como wrapper nativo, mantendo código web base mas acessando APIs nativas de alarmes quando necessário.

## Soluções de IA contextual e análise comportamental

**LSTM e GRU** são ideais para análise temporal de padrões de uso. Para neurodivergentes, modelos específicos treinados em dados de TDAH/TEA mostram 96.5% de precisão na distinção de padrões comportamentais.

**TensorFlow.js** permite processamento local completo, preservando privacidade. Para análise mais complexa, **Federated Learning** permite melhoria coletiva sem exposição de dados individuais.

O sistema contextual recomenda automaticamente: alarmes de trabalho sugerem recorrência em dias úteis, medicamentos sugerem intervalos médicos padrão, e exercícios adaptam-se a padrões históricos pessoais.

## APIs e sincronização offline-first

**Calendarific** oferece a melhor cobertura de feriados (230+ países) com pricing escalável. **Replicache** fornece sincronização offline robusta com conflict resolution automático, embora seja proprietário ($500/mês para pequenas empresas).

Para open source, **Y.js** com CRDT oferece sincronização peer-to-peer real-time. **Dexie.js** com Storage Persistence API garante dados locais persistentes mesmo com limpezas automáticas do browser.

A estratégia de **conflict resolution** usa Last-Write-Wins com priority fields: alarmes habilitados têm prioridade sobre desabilitados, horários mais cedo vencem sobre mais tarde, e device ID é tiebreaker final.

## Modelo open source com BYOK

**Business Source License 1.1** oferece o melhor equilíbrio: código aberto para desenvolvimento e teste, mas restrições comerciais por 4 anos que protegem contra competitors. Após 4 anos, transição automática para GPL.

Arquitetura híbrida recomendada: Core open source (BSL) inclui motor básico, APIs fundamentais e interface self-hosted. Enterprise features proprietárias incluem BYOK implementation, advanced analytics e multi-tenant management.

O modelo de pricing escalonado permite Community (gratuito, self-hosted), Enterprise ($50k-200k/ano) e Managed Service (base $5k/mês + usage), maximizando addressable market.

## Sincronização offline com conflict resolution

**CRDT (Conflict-free Replicated Data Types)** usando Last-Write-Wins pattern oferece conflict resolution automático e garantias de eventual consistency. Para alarmes, isso significa que modificações offline são automaticamente reconciliadas quando conectividade retorna.

**Envelope encryption** com chaves rotacionadas mensalmente e **backup incremental** client-side para S3 garantem que dados nunca sejam perdidos, mesmo com falhas simultâneas em múltiplos dispositivos.

## Bibliotecas e frameworks recomendados

**Interface**: FullCalendar + dnd-kit para calendário drag-and-drop acessível, react-i18next para internacionalização, React Aria para componentes acessíveis WCAG-compliant.

**PWA**: Workbox para Service Workers otimizados, Dexie.js para storage offline, FCM para push notifications confiáveis.

**Backend**: AWS CDK para infrastructure-as-code, DynamoDB com TTL para alarmes, Lambda para processamento serverless.

**IA**: TensorFlow.js para processamento local, OpenAI API com BYOK para análise contextual avançada, bibliotecas de privacidade diferencial para analytics seguros.

## Criptografia end-to-end e backup

**Web Crypto API** permite criptografia client-side robusta. Backup incremental com AES-256-GCM garante que apenas mudanças sejam sincronizadas, reduzindo bandwidth e melhorando performance.

Sistema de **key derivation** usando PBKDF2 com 100,000 iterações cria chaves únicas por usuário, enquanto **HSM FIPS 140-2 Level 3** protege chaves mestras em produção.

## UX especializada para neurodivergentes

Interface deve implementar **prefers-reduced-motion** para TDAH, **high contrast mode** para processamento visual, e **OpenDyslexic** como opção de fonte. **React Aria** fornece hooks especializados para acessibilidade, enquanto **Headless UI** oferece componentes unstyled acessíveis.

Configuração por **comandos naturais** ("criar alarme para medicamento às 8h todos os dias") usando **Whisper** para speech-to-text e **spaCy** para natural language understanding reduz friction cognitivo.

## Estimativas de custo e escalabilidade

Para 10k usuários: AWS (~$17/mês), Vercel (~$30/mês), APIs de IA (~$50-200/mês dependendo do uso). Total operacional estimado: **$100-300/mês** para 10k usuários, escalando linearmente.

O modelo de negócio permite break-even com ~500 clientes enterprise ou ~50k usuários freemium com conversão de 2-3% para planos pagos.

## Breakdown de Custos e Alternativas dos Serviços

### FullCalendar vs Alternativas

**FullCalendar (Pago)**:
- **Premium**: $480/ano para 1-10 desenvolvedores
- **Core gratuito**: MIT license para funcionalidades não-premium
- **Features premium**: Timeline views, drag-and-drop avançado, suporte comercial

**Alternativas Gratuitas**:
- **React Big Calendar**: Completamente gratuito, MIT license, drag-and-drop nativo
- **Simple React Calendar**: Gratuito, lightweight, view mensal básico
- **Schedule-X**: Gratuito, material design, múltiplas views

**Alternativas Pagas**:
- **DHTMLX Scheduler**: A partir de $599, 30+ componentes UI
- **Syncfusion Calendar**: $2,495-$4,995 para todos os componentes
- **Bryntum Scheduler**: Pricing por produto, trial gratuito

**Recomendação**: Para MVP, usar **React Big Calendar** (gratuito) oferece 80% das funcionalidades do FullCalendar Premium sem custo. Migrar para FullCalendar Premium apenas se precisar de Timeline views específicas.

### AWS vs Azure vs GCP vs OCI - Serverless + Database

**AWS Lambda + DynamoDB**:
- **Lambda**: $0.0000166667 por GB-segundo + $0.20 por 1M requests
- **DynamoDB**: $1.25 por million WRUs, $0.25 por million RRUs (on-demand)
- **Custo estimado para 10k usuários**: ~$15-50/mês
- **Free tier**: 1M requests Lambda + 25GB DynamoDB mensais

**Azure Functions + Cosmos DB**:
- **Functions**: Similar ao Lambda, pricing competitivo
- **Cosmos DB**: Pricing baseado em Request Units (RUs), mais caro que DynamoDB
- **Custo estimado para 10k usuários**: ~$30-80/mês (mais caro)
- **Vantagem**: SQL-like queries, múltiplos data models

**Google Cloud Functions + Firestore**:
- **Cloud Functions**: $0.40 por million invocations, 2M grátis/mês
- **Firestore**: 20k writes + 50k reads grátis/dia no Spark plan
- **Custo estimado para 10k usuários**: ~$20-60/mês
- **Vantagem**: Real-time updates nativas, easy scaling

**Oracle Cloud Functions + Database**:
- **Functions**: Pricing competitivo, Provisioned Concurrency com desconto
- **Autonomous Database**: Até 72% mais barato que AWS para workloads similares
- **Custo estimado para 10k usuários**: ~$10-35/mês (mais barato)
- **Vantagem**: 10TB data egress grátis/mês vs outros providers

### Comparação Final de Custos (10k usuários/mês)

1. **Oracle Cloud**: $10-35/mês (mais barato)
2. **AWS**: $15-50/mês (balanceado)
3. **Google Cloud**: $20-60/mês (real-time features)
4. **Azure**: $30-80/mês (mais caro, mais features)

### Recomendação de Stack por Budget

**MVP/Bootstrap (< $50/mês)**:
- **Frontend**: React + React Big Calendar (gratuito)
- **Backend**: Oracle Cloud Functions + Autonomous Database
- **OU**: AWS Lambda + DynamoDB (free tier extenso)

**Startup/Growth ($50-200/mês)**:
- **Frontend**: React + FullCalendar Premium ($40/mês)
- **Backend**: AWS Lambda + DynamoDB (proven scale)
- **OU**: Google Cloud Functions + Firestore (real-time features)

**Enterprise (> $200/mês)**:
- **Frontend**: React + FullCalendar Premium + DHTMLX components
- **Backend**: Azure Cosmos DB (SQL queries + multi-model)
- **OU**: AWS com reserved capacity (desconto até 50%)

## Conclusão

O stack mais **cost-effective** para MVP é **Oracle Cloud + React Big Calendar**, oferecendo ~70% de economia vs AWS. Para **production-ready** com features avançadas, **AWS Lambda + DynamoDB + FullCalendar Premium** oferece melhor balanceamento custo/benefício/maturidade.

A implementação faseada permite começar com alternativas gratuitas e migrar incrementalmente para soluções premium conforme revenue justifique o investimento.