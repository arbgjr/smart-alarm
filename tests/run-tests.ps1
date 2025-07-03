# Test Script - Smart Alarm

Este script executa todos os testes automatizados do projeto Smart Alarm com logger detalhado, conforme padrão do projeto.

## Como usar

1. Abra o terminal na raiz do repositório.
2. Execute:

```powershell
# Executa todos os testes com logger detalhado
# (pode ser usado no PowerShell ou CI/CD)
dotnet test --logger "console;verbosity=detailed"
```

- Todos os projetos devem estar com TargetFramework net8.0.
- O comando pode ser incluído em pipelines CI/CD.
- O resultado detalhado será exibido no console.

## Observações
- Corrija eventuais avisos de compatibilidade de pacotes.
- Para rodar apenas um projeto/teste específico, adicione o caminho do .csproj.

---

Dúvidas? Consulte a documentação do projeto ou o arquivo `code-generation.instructions.md`.
