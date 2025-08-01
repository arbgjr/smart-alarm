# Dockerfile para customização do DevContainer Smart Alarm
# Base: .NET 8 + Node.js + OCI CLI + utilitários
FROM mcr.microsoft.com/devcontainers/dotnet:8.0

# Instalação do OCI CLI
RUN apt-get update && \
    apt-get install -y python3-pip && \
    pip3 install oci-cli && \
    rm -rf /var/lib/apt/lists/*

# Instalação de utilitários adicionais
RUN apt-get update && \
    apt-get install -y git curl unzip && \
    rm -rf /var/lib/apt/lists/*

# Node.js já incluso via feature

# Usuário padrão
USER vscode
