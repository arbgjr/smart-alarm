# Complete Technical Stack for Intelligent Alarm WebApp

This report presents specific technical recommendations to implement an intelligent alarm webapp accessible to all users, considering all requested architectural, security, AI, and compliance aspects.

## Recommended technology stack

**Frontend**: React 18 + TypeScript, Atomic Design, robust PWA support. **FullCalendar** recomendado para interface visual, acessibilidade WCAG e drag-and-drop nativo.

**Backend**: C# (.NET 8+), Clean Architecture, Oracle Cloud Infrastructure (OCI) Functions como padrão serverless, ML.NET para IA contextual. Persistência principal em **Oracle Autonomous DB** e **PostgreSQL**; **Redis** para cache e operações rápidas.

**PWA e Notificações**: Service Workers + FCM como fallback, Web Locks API + Wake Lock API para alarmes críticos, garantindo confiabilidade mesmo com restrições de background.

**Infraestrutura**: Docker, Kubernetes, Terraform para IaC, **HashiCorp Vault** para gestão de segredos.

**Observabilidade**: Prometheus, Grafana e Loki para métricas, dashboards e logs estruturados.

## OWASP and LGPD security architecture

Implementing robust security requires multiple layers of protection. **AES-256-GCM** for data at rest, **TLS 1.3** for data in transit, and **FIDO2/WebAuthn** for passwordless authentication create a solid foundation. For neurodiversity data, **k-anonymity** and **differential privacy** techniques preserve privacy while allowing useful analytics.

**BYOK** for AI uses envelope encryption: data is encrypted with client keys (DEK), which are protected by master keys in the client's HSMs. This ensures that not even you can access sensitive data without explicit authorization.

For LGPD compliance, implementing a granular consent system, automated data subject rights, and "cryptographic erasure" via key revocation meets all specific legal requirements for mental health data.

## Browser-reliable alarm implementation

Reliable alarms in PWA require a multilayered strategy due to background processing limitations. **iOS Safari** is particularly restrictive, requiring PWAs to be added to the home screen for notifications to work.

The recommended architecture combines: Service Workers for standard notifications, Web Locks + Wake Lock for critical alarms, FCM for server-side backup, and polling fallback for limited browsers. Heartbeat every 30 seconds with the server keeps synchronization even with aggressive throttling.

For maximum reliability, consider **Capacitor** as a native wrapper, keeping the web codebase but accessing native alarm APIs when necessary.

## Contextual AI solutions and behavioral analysis

**LSTM and GRU** are ideal for temporal analysis of usage patterns. For neurodivergent users, specific models trained on ADHD/ASD data show 96.5% accuracy in distinguishing behavioral patterns.

**TensorFlow.js** allows full local processing, preserving privacy. For more complex analysis, **Federated Learning** enables collective improvement without exposing individual data.

The contextual system automatically recommends: work alarms suggest recurrence on weekdays, medications suggest standard medical intervals, and exercises adapt to personal historical patterns.

## APIs and offline-first synchronization

**Calendarific** offers the best holiday coverage (230+ countries) with scalable pricing. **Replicache** provides robust offline synchronization with automatic conflict resolution, although it is proprietary ($500/month for small businesses).

For open source, **Y.js** with CRDT offers real-time peer-to-peer synchronization. **Dexie.js** with Storage Persistence API ensures local data persists even with automatic browser cleanups.

The **conflict resolution** strategy uses Last-Write-Wins with priority fields: enabled alarms have priority over disabled ones, earlier times win over later, and device ID is the final tiebreaker.

## Open source model with BYOK

**Business Source License 1.1** offers the best balance: open source for development and testing, but commercial restrictions for 4 years that protect against competitors. After 4 years, automatic transition to GPL.

Recommended hybrid architecture: Core open source (BSL) includes basic engine, fundamental APIs, and self-hosted interface. Proprietary enterprise features include BYOK implementation, advanced analytics, and multi-tenant management.

The tiered pricing model allows Community (free, self-hosted), Enterprise ($50k-200k/year), and Managed Service (base $5k/month + usage), maximizing addressable market.

## Offline synchronization with conflict resolution

**CRDT (Conflict-free Replicated Data Types)** using Last-Write-Wins pattern offers automatic conflict resolution and eventual consistency guarantees. For alarms, this means that offline modifications are automatically reconciled when connectivity returns.

**Envelope encryption** with monthly rotated keys and **incremental client-side backup** to S3 ensure that data is never lost, even with simultaneous failures on multiple devices.

## Recommended libraries and frameworks

**Interface**: FullCalendar + dnd-kit para calendário acessível, react-i18next para internacionalização, React Aria para acessibilidade WCAG.

**PWA**: Workbox para Service Workers, Dexie.js para armazenamento offline, FCM para push notifications.

**Backend**: Oracle Cloud Infrastructure SDK, Oracle.ManagedDataAccess, Npgsql (PostgreSQL), StackExchange.Redis, Polly, HttpClientFactory, JWT/FIDO2, FluentValidation.

**AI**: ML.NET para IA contextual no backend, TensorFlow.js para processamento local, OpenAI API com BYOK para análises avançadas, bibliotecas de privacidade diferencial para analytics seguro.

## End-to-end encryption and backup

**Web Crypto API** enables robust client-side encryption. Incremental backup with AES-256-GCM ensures that only changes are synchronized, reducing bandwidth and improving performance.

**Key derivation** system using PBKDF2 with 100,000 iterations creates unique keys per user, while **HSM FIPS 140-2 Level 3** protects master keys in production.

## Specialized UX for all users

Interface should implement **prefers-reduced-motion** for users sensitive to motion, **high contrast mode** for better visual processing, and **OpenDyslexic** as a font option to improve readability. **React Aria** provides specialized hooks for accessibility, while **Headless UI** offers unstyled accessible components.

Configuration by **natural commands** ("create alarm for 8am every day") using **Whisper** for speech-to-text and **spaCy** for natural language understanding reduces usage complexity.

## Cost and scalability estimates

For 10k users: AWS (~$17/month), Vercel (~$30/month), AI APIs (~$50-200/month depending on usage). Total estimated operational: **$100-300/month** for 10k users, scaling linearly.

The business model allows break-even with ~500 enterprise clients or ~50k freemium users with 2-3% conversion to paid plans.

## Service Cost Breakdown and Alternatives

### FullCalendar vs Alternatives

**FullCalendar (Paid)**:

- **Premium**: $480/year for 1-10 developers
- **Free core**: MIT license for non-premium features
- **Premium features**: Timeline views, advanced drag-and-drop, commercial support

**Free Alternatives**:

- **React Big Calendar**: Completely free, MIT license, native drag-and-drop
- **Simple React Calendar**: Free, lightweight, basic monthly view
- **Schedule-X**: Free, material design, multiple views

**Paid Alternatives**:

- **DHTMLX Scheduler**: From $599, 30+ UI components
- **Syncfusion Calendar**: $2,495-$4,995 for all components
- **Bryntum Scheduler**: Pricing per product, free trial

**Recommendation**: For MVP, using **React Big Calendar** (free) offers 80% of FullCalendar Premium's features at no cost. Migrate to FullCalendar Premium only if specific Timeline views are needed.

### AWS vs Azure vs GCP vs OCI - Serverless + Database

**Oracle Cloud Functions + Autonomous Database/PostgreSQL/Redis**:

- **Functions**: Serverless padrão, escalabilidade automática, baixo custo.
- **Autonomous Database**: Banco de dados gerenciado, alta disponibilidade, compliance e performance.
- **PostgreSQL**: Alternativa open source para persistência relacional.
- **Redis**: Cache e operações rápidas.
- **Estimated cost for 10k users**: ~$10-35/month (cheaper)
- **Advantage**: 10TB data egress free/month, integração nativa com OCI Vault, Observability (Prometheus, Grafana, Loki).

### Final Cost Comparison (10k users/month)

1. **Oracle Cloud**: $10-35/month (cheaper)
2. **AWS**: $15-50/month (balanced)
3. **Google Cloud**: $20-60/month (real-time features)
4. **Azure**: $30-80/month (more expensive, more features)

### Recommended Stack by Budget

**MVP/Bootstrap (< $50/month)**:

- **Frontend**: React + React Big Calendar (free)
- **Backend**: Oracle Cloud Functions + Autonomous Database
- **OR**: AWS Lambda + DynamoDB (extensive free tier)

**Startup/Growth ($50-200/month)**:

- **Frontend**: React + FullCalendar Premium ($40/month)
- **Backend**: AWS Lambda + DynamoDB (proven scale)
- **OR**: Google Cloud Functions + Firestore (real-time features)

**Enterprise (> $200/month)**:

- **Frontend**: React + FullCalendar Premium + DHTMLX components
- **Backend**: Azure Cosmos DB (SQL queries + multi-model)
- **OR**: AWS with reserved capacity (up to 50% discount)

## Conclusion

The most **cost-effective** stack for MVP is **Oracle Cloud + React Big Calendar**, offering ~70% savings vs AWS. For **production-ready** with advanced features, **AWS Lambda + DynamoDB + FullCalendar Premium** offers the best cost/benefit/maturity balance.

Phased implementation allows starting with free alternatives and migrating incrementally to premium solutions as revenue justifies the investment.
