# Testes End-to-End (E2E) — Smart Alarm

Este diretório contém toda a estrutura automatizada para execução dos testes E2E do projeto Smart Alarm.

## Pré-requisitos

- Docker e Docker Compose
- PowerShell (Windows) ou Bash (Linux/Mac)
- Ferramentas: `sox` (áudio), `ImageMagick` (imagem)
- Dependências do projeto instaladas
- Serviços do Smart Alarm configurados para ambiente local

## Estrutura

- `run-e2e-tests.ps1` — Executa todos os cenários E2E via PowerShell
- `run-e2e-tests.sh` — Executa todos os cenários E2E via Bash
- `helpers/` — Scripts de setup, teardown e geração de dados de teste
- `scenarios/` — Scripts para cada fluxo/cenário principal
- `data/` — Dados de teste gerados e resultados
- `mocks/` — Simulações de serviços externos

## Passo a Passo

1. **Preparar o ambiente**
   - Execute o script de setup:
     - PowerShell: `./helpers/setup-services.ps1`
     - Bash: `./helpers/setup-services.sh`

2. **Gerar dados de teste**
   - Execute o script de geração de dados:
     - PowerShell: `./helpers/generate-test-data.ps1`
     - Bash: `./helpers/generate-test-data.sh`

3. **Executar os testes E2E**
   - PowerShell: `./run-e2e-tests.ps1`
   - Bash: `./run-e2e-tests.sh`

4. **Finalizar o ambiente**
   - Execute o script de teardown:
     - PowerShell: `./helpers/teardown-services.ps1`
     - Bash: `./helpers/teardown-services.sh`

## Adicionando Novos Cenários

- Crie um novo script em `scenarios/` seguindo o padrão dos existentes.
- Utilize AAA (Arrange, Act, Assert) e cubra casos de sucesso, erro e borda.
- Gere dados de teste conforme necessário em `helpers/generate-test-data.*`

## Resultados e Logs

- Resultados dos testes e logs são salvos em `data/`.
- Consulte os arquivos `.json`, `.txt` e logs dos containers para análise detalhada.

## Observações

- Todos os scripts podem ser adaptados para CI/CD.
- Para dúvidas sobre endpoints, consulte a documentação Swagger do projeto.
- Para cenários LGPD, MFA, recuperação de senha, etc., siga o mapeamento em `docs/e2e tests/mapeamento.md`.

---

Dúvidas ou problemas? Consulte o time de desenvolvimento ou abra uma issue.
