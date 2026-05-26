# Desafio Dev Container

This devcontainer provides a complete development environment for the Desafio project, including both backend (.NET 10) and frontend (React/Node.js) support.

## Features

- **Base Image**: custom `.devcontainer/Dockerfile`
  - Based on `mcr.microsoft.com/devcontainers/dotnet:1-10.0-noble`
  - Pre-installed with .NET 10 SDK
  - Ubuntu 24.04 LTS (noble)
  - Removes stale Yarn apt source to avoid feature install failures
  
- **Node.js Feature**: v20.19.0 (LTS)
  - Supports frontend React development with Vite
  - Supports AWS CDK if needed

- **Aspire CLI**
  - Installed automatically in post-create as a global .NET tool (`aspire.cli`)
  - Available as `aspire`

- **Common Utilities**
  - Provided by the base `mcr.microsoft.com/devcontainers/dotnet:1-10.0-noble` image
  - Not redeclared in `devcontainer.json` to avoid re-running the feature during build

- **VS Code Extensions**:
  - C# Dev Kit with OmniSharp
  - Prettier for code formatting
  - ESLint for TypeScript/JavaScript
  - .NET runtime support

- **Forwarded Ports**:
  - 5000: Backend HTTP
  - 5001: Backend HTTPS
  - 5173: Frontend Dev Server (Vite)
  - 5174: Frontend Preview
  - 7000: App Host
  - 7001: App Host HTTPS

## Usage

1. **Open in Dev Container**
   - In VS Code, use `Remote - Containers: Reopen in Container`
   - Or click the green `><` icon in the bottom left

2. **Post-Create Setup**
   - The container automatically runs `post-create.sh`
   - Restores all .NET projects
   - Installs frontend and CDK dependencies
  - Installs/updates Aspire CLI

3. **Running the Project**

   **Backend:**
   ```bash
   dotnet run --project Desafio.Server
   # or
   dotnet run --project Desafio.AppHost
   ```

   **Frontend:**
   ```bash
   cd frontend
   npm run dev
   ```

   **Tests:**
   ```bash
   dotnet test
   ```

   **Linting:**
   ```bash
   cd frontend
   npm run lint
   ```

## Environment Details

- **Container OS**: Ubuntu 24.04 LTS
- **.NET Version**: 10.x
- **Node Version**: 20.19.0
- **NPM**: Latest compatible version
- **All backends available**: AWS, Azure, Local

## Customization

Edit `.devcontainer/devcontainer.json` to:
- Add or remove forwarded ports
- Install additional VS Code extensions
- Modify mount points or environment variables
- Change the post-create script

Edit `.devcontainer/post-create.sh` to:
- Run additional setup commands
- Download additional tools
- Configure environment-specific settings
