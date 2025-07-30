# VS Code DevContainer para Smart Alarm

Este ambiente foi configurado para garantir desenvolvimento completo e confiável:

## Stack Incluída

- **.NET 8**: Backend com Clean Architecture
- **Azure CLI**: Deploy e testes em Azure/OCI  
- **Node.js LTS**: Frontend/PWA com Yarn
- **PowerShell**: Scripts de automação
- **Docker-in-Docker**: Containers e compose

## Recursos Configurados

- **Extensões essenciais**: C# DevKit, Docker, Copilot, ESLint, Prettier
- **Configurações otimizadas**: File watchers, telemetry desabilitada
- **Volumes persistentes**: node_modules e memory-bank
- **Ambiente seguro**: Git safe directory, privilégios controlados

## Como usar

1. Abra o projeto no VS Code
2. Instale a extensão "Dev Containers"
3. Reabra no container: `Ctrl+Shift+P → Dev Containers: Reopen in Container`
4. Aguarde a criação automática (pode levar alguns minutos na primeira vez)
5. Execute `dotnet restore` e `npm install --prefix frontend` se necessário

## Solução de Problemas

- **Container travando**: Rebuild sem cache (`Dev Containers: Rebuild Container`)
- **Extensões não carregam**: Reinstale extensões no container
- **Performance lenta**: Verifique recursos Docker disponíveis
- **Permissões**: Container roda como usuário `vscode` por segurança

Consulte o README principal para detalhes de arquitetura e padrões.
