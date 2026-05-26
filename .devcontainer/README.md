# Dev Container do Desafio

Este devcontainer fornece um ambiente de desenvolvimento completo para o projeto Desafio, incluindo suporte tanto para backend (.NET 10) quanto para frontend (React/Node.js).

## Recursos

- **Imagem Base**: `.devcontainer/Dockerfile` customizado
  - Baseado em `mcr.microsoft.com/devcontainers/dotnet:1-10.0-noble`
  - Vem com o SDK do .NET 10 pré-instalado
  - Ubuntu 24.04 LTS (noble)
  - Remove a fonte apt antiga do Yarn para evitar falhas na instalação de features
  
- **Feature do Node.js**: v20.19.0 (LTS)
  - Suporta desenvolvimento frontend em React com Vite
  - Suporta AWS CDK, se necessário

- **Aspire CLI**
  - Instalado automaticamente no post-create como uma ferramenta global do .NET (`aspire.cli`)
  - Disponível como `aspire`

- **Utilitários Comuns**
  - Fornecidos pela imagem base `mcr.microsoft.com/devcontainers/dotnet:1-10.0-noble`
  - Não redeclarados no `devcontainer.json` para evitar reexecução da feature durante o build

- **Extensões do VS Code**:
  - C# Dev Kit com OmniSharp
  - Prettier para formatação de código
  - ESLint para TypeScript/JavaScript
  - Suporte ao runtime do .NET

- **Portas Encaminhadas**:
  - 5000: HTTP do backend
  - 5001: HTTPS do backend
  - 5173: Servidor de desenvolvimento do frontend (Vite)
  - 5174: Preview do frontend
  - 7000: App Host
  - 7001: HTTPS do App Host

## Uso

1. **Abrir no Dev Container**
   - No VS Code, use `Remote - Containers: Reopen in Container`
   - Ou clique no ícone verde `><` no canto inferior esquerdo

2. **Configuração Pós-Criação**
   - O container executa automaticamente o `post-create.sh`
   - Restaura todos os projetos .NET
   - Instala as dependências do frontend e do CDK
  - Instala/atualiza o Aspire CLI

3. **Executando o Projeto**

   **Backend:**
   ```bash
   dotnet run --project Desafio.Server
   # ou
   dotnet run --project Desafio.AppHost
   ```

   **Frontend:**
   ```bash
   cd frontend
   npm run dev
   ```

   **Testes:**
   ```bash
   dotnet test
   ```

   **Lint:**
   ```bash
   cd frontend
   npm run lint
   ```

## Detalhes do Ambiente

- **SO do container**: Ubuntu 24.04 LTS
- **Versão do .NET**: 10.x
- **Versão do Node**: 20.19.0
- **NPM**: Versão compatível mais recente
- **Todos os backends disponíveis**: AWS, Azure, Local

## Personalização

Edite `.devcontainer/devcontainer.json` para:
- Adicionar ou remover portas encaminhadas
- Instalar extensões adicionais do VS Code
- Modificar pontos de montagem ou variáveis de ambiente
- Alterar o script de post-create

Edite `.devcontainer/post-create.sh` para:
- Executar comandos adicionais de configuração
- Baixar ferramentas adicionais
- Configurar ajustes específicos de ambiente
