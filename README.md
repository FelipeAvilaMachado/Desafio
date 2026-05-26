# Desafio — Sistema de Fluxo de Caixa

> **Arquitetura de software escalável e resiliente para controle de lançamentos financeiros e consolidado diário.**

---

## Índice

1. [Arquitetura Cloud (Azure)](#11-arquitetura-cloud-azure)
2. [Arquitetura Cloud (AWS)](#12-arquitetura-cloud-aws)
3. [Arquitetura de Código](#2-arquitetura-de-código)
4. [Como abrir o DevContainer e rodar o .NET Aspire](#3-como-abrir-o-devcontainer-e-rodar-o-net-aspire)

---

## 1.1. Arquitetura Cloud (Azure)

Um comerciante precisa controlar o fluxo de caixa diário com lançamentos (débitos e créditos) e consultar um relatório de saldo diário consolidado.

A arquitetura cloud para o cenário principal em Azure é composta por:

- **Azure APIM** para autenticação, autorização e rate limit
- **Microsoft Entra ID** para autenticação e autorização baseada em identidade
- **Container Apps** para execução das APIs com escalonamento baseado em uso de recursos (CPU/RAM) e indicadores de latência (p50/p90)
- **Azure SQL** para persistência transacional e leitura de consolidado
- **Azure Cache for Redis** para cache distribuído de consolidado diário

Existem diversas maneiras de satisfazer os requisitos deste desafio, mas, por ser uma demanda baixa (picos de apenas 50 req/s), o ideal é manter a simplicidade sem perder a robustez.
Azure APIM centraliza os endpoints e a autenticação.
Container Apps é capaz de escalonar horizontal e verticalmente conforme a demanda do projeto venha a crescer.
Azure SQL e Cache for Redis são serviços gerenciados, reduzindo a manutenção e o suporte.

A demanda inicial do projeto é tão pequena que um simples serviço em uma VPS/Container com um banco SQLite seria mais do que suficiente.
Mas, levando em consideração a necessidade de ter um sistema resiliente e robusto, o ideal é a utilização de serviços gerenciados para garantir estabilidade e uptime.
Rodando o benchmark disponível no frontend em um container com 2 vCPUs e 2 GB RAM, foi possível atingir, em média, 1350 req/s no código não otimizado.
Existem diversas maneiras de otimizar ainda mais o consumo de memória e a quantidade de requisições em paralelo; uma delas seria utilizar IAsyncEnumerable<T> para consultas e retornos.
Para este demo, não foram realizados benchmarks completos nem otimizações desnecessárias para a escala atual.

Caso o projeto venha a crescer, a estrutura atual supre todas as necessidades.
Caso haja necessidade de escalonar para outras regiões/países, o ideal seria a utilização do Azure Front Door para gerenciar o balanceamento de carga para a região mais próxima do cliente.
.NET Aspire já oferece a infraestrutura como código, mas, para mais controle e padronização, o ideal seria a utilização de CI/CD para publicação.

## 1.2. Arquitetura Cloud (AWS)

A arquitetura cloud para o cenário principal em AWS é composta por:

- **Amazon API Gateway** para autenticação, autorização e rate limit
- **Amazon Cognito** para autenticação e autorização baseada em identidade
- **Amazon Fargate/ECS** para execução das APIs com escalonamento baseado em uso de recursos (CPU/RAM) e indicadores de latência (p50/p90)
- **Amazon RDS** para persistência transacional e leitura de consolidado
- **Amazon MemoryDB for Redis** para cache distribuído de consolidado diário

A lógica é a mesma do Azure, mas com os serviços adaptados para a AWS.

Obs.: para redução de custos, seria interessante a utilização de CPUs ARM, tendo em vista que o .NET compila sem necessidade de alterações no código.

### Visão geral do fluxo

```
┌─────────────────────────────────────────────────────────────────┐
│                         CLIENTES                                │
│              (Browser / Mobile / Integrações)                   │
└───────────────────────┬─────────────────────────────────────────┘
                        │ HTTPS
┌───────────────────────▼─────────────────────────────────────────┐
│                        AZURE APIM                               │
│         (Autenticação, Autorização, Rate Limiting)              │
└───────────────────────┬─────────────────────────────────────────┘
                        │
          ┌─────────────▼──────────────┐
          │    Desafio.Server          │   ← Container Apps
          │  (ASP.NET Core Minimal)    │
          │  Vertical Slices + CQRS    │
          └──────┬──────────┬──────────┘
                 │          │
    ┌────────────▼──┐   ┌───▼──────────────┐
    │   Azure SQL   │   │ Azure Cache      │
    │   (Write/Read)│   │ for Redis        │
    │ Lancamentos   │   │ (5 min TTL)      │
    │ OutboxMsgs    │   └──────────────────┘
    └────────┬──────┘
             │ Outbox Pattern (polling)
    ┌────────▼──────────────────────────────┐
    │        OutboxProcessor                │
    │   (publica em IMessageBus)            │
    └────────┬──────────────────────────────┘
             │
    ┌────────▼──────────────────────────────┐
    │      Message Bus (plugável)           │
    │   RabbitMQ | Service Bus | SQS        │
    └────────┬──────────────────────────────┘
             │ LancamentoCriadoEvent
    ┌────────▼──────────────────────────────┐
    │    Desafio.Consolidado.Worker         │
    │   (serviço separado de consolidação)  │
    └───────────────────────────────────────┘
```

> **Princípio-chave:** O serviço de lançamentos **nunca** fica indisponível por falha no Worker de consolidado. O Outbox garante entrega de eventos e consistência eventual.

### Escalabilidade

- APIs stateless executando em Container Apps
- Escalonamento horizontal baseado em CPU/RAM
- Escalonamento orientado por comportamento de latência (p50/p90)
- Redis reduz a carga de leitura no banco durante picos

### Resiliência

- Outbox Pattern evita perda de eventos no dual write
- Mensageria assíncrona desacopla API de lançamentos do consolidado
- Worker independente, com reprocessamento automático quando retorna
- Health checks para API e Worker

### Segurança

- APIM como camada de borda para rate limit e políticas de acesso
- Entra ID para autenticação/autorização
- Segredos em gerenciador seguro (Key Vault em Azure)
- Logs estruturados para trilha de auditoria

### Integração

- Eventos de domínio publicados via barramento de mensagens
- Contratos assíncronos entre serviço de lançamentos e consolidação
- Abstrações de infraestrutura permitem operação multi-cloud (Azure/AWS)
- Mesma lógica de negócio, mudando apenas adaptadores de infraestrutura

---

## 2. Arquitetura de Código

### Estrutura de projetos

```
src/
├── Desafio.AppHost/              # Orquestrador local (.NET Aspire)
├── Desafio.Server/               # API de Lançamentos (Minimal API)
├── Desafio.Consolidado.Worker/   # Worker de Consolidação (Background Service)
├── Desafio.Features/             # Lógica de negócio (Vertical Slices)
│   ├── Common/                   # Entidades, DTOs, Handlers, Auth, Outbox
│   ├── Lancamentos/              # Criar, Listar, ObterPorId
│   ├── Consolidado/              # ObterDiario, ObterPeriodo, Disparar
│   └── Benchmark/                # Teste de carga interno
├── Desafio.Infrastructure/       # EF Core, Redis, Outbox (SQL Server)
├── Desafio.Infrastructure.Azure/ # Azure Service Bus, Key Vault
├── Desafio.Infrastructure.Aws/   # AWS SQS, Secrets Manager
├── Desafio.Functions.Azure/      # Host alternativo Azure Functions
├── Desafio.Functions.Aws/        # Host alternativo AWS Lambda
├── Desafio.Tests.Unit/           # Testes unitários (xUnit + NSubstitute)
├── frontend/                     # Frontend React + Vite + TypeScript
└── infra/
    └── aws/
        ├── template.yaml         # SAM
        └── cdk/                  # CDK
```

### Vertical Slices com divisões lógicas e físicas

- Divisão lógica por funcionalidade (Lancamentos, Consolidado, Benchmark)
- Cada slice concentra endpoint, request/response e handler
- Divisão física flexível:
  - API monolítica modular para cenário principal
  - Hosts em Functions (Azure/AWS) para cenários serverless
- Mesmo núcleo de negócio reutilizado em diferentes hosts

Com a divisão lógica por funcionalidade, isso facilita a migração para microsserviços, caso haja necessidade.
Separar alguns endpoints críticos em Azure Functions/AWS Lambdas se torna prático e rápido, conforme projetos de exemplo (não são utilizados no Aspire, mas estão presentes para demonstração).

### Cache

- Cache-aside com Redis no consolidado diário
- Chave por data com TTL de 5 minutos
- Invalidação após recalcular consolidado
- Redução de latência e de carga no banco em picos

### Serviço separado para consolidação de dados

- Consolidação executa em serviço dedicado: `Desafio.Consolidado.Worker`
- Cálculo de débito/crédito/saldo desacoplado da API de escrita
- Processamento assíncrono a partir de eventos de lançamento
- API segue disponível mesmo quando o worker está indisponível

### Logs e tracing

- Logging estruturado por request e por handler
- OpenTelemetry para traces distribuídos
- Correlação entre API, banco, cache e processamento assíncrono
- Base de observabilidade para métricas de latência (p50/p90), erro e throughput

### Endpoints da API

#### Lançamentos

| Método | Endpoint | Descrição |
|---|---|---|
| `POST` | `/api/lancamentos` | Cria um novo lançamento (débito ou crédito) |
| `GET` | `/api/lancamentos` | Lista lançamentos (filtro opcional por `data`) |
| `GET` | `/api/lancamentos/{id}` | Obtém um lançamento por ID |

Exemplo — Criar lançamento:

```json
POST /api/lancamentos
{
  "tipo": "Credito",
  "valor": 1500.00,
  "descricao": "Venda de produto",
  "data": "2025-01-15"
}
```

Resposta:

```json
HTTP 201 Created
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tipo": "Credito",
  "valor": 1500.00,
  "descricao": "Venda de produto",
  "data": "2025-01-15",
  "criadoEm": "2025-01-15T10:30:00Z"
}
```

#### Consolidado

| Método | Endpoint | Descrição |
|---|---|---|
| `GET` | `/api/consolidado/diario?data=2025-01-15` | Saldo consolidado de um dia |
| `GET` | `/api/consolidado/periodo?dataInicio=2025-01-01&dataFim=2025-01-31` | Saldo consolidado de um período |
| `POST` | `/api/consolidado/disparar` | Dispara reprocessamento manual |

Exemplo — Saldo diário:

```json
GET /api/consolidado/diario?data=2025-01-15
{
  "data": "2025-01-15",
  "totalDebitos": 500.00,
  "totalCreditos": 1500.00,
  "saldo": 1000.00,
  "atualizadoEm": "2025-01-15T10:35:00Z"
}
```

---

## 3. Como abrir o DevContainer e rodar o .NET Aspire

### Pré-requisitos

| Ferramenta | Versão mínima |
|---|---|
| .NET SDK | 10.0 |
| Docker Desktop / Docker Engine | 24+ |
| Node.js | 20+ |
| .NET Aspire Workload | Incluído via SDK |

Se necessário, instalar o workload Aspire:

```bash
dotnet workload install aspire
```

### Como abrir o DevContainer

1. Abrir o projeto no VS Code
2. Executar: **Dev Containers: Reopen in Container**
3. Aguardar o post-create finalizar

O ambiente do container prepara backend e frontend para desenvolvimento.

### Subir a aplicação completa com .NET Aspire

```bash
dotnet run --project src/Desafio.AppHost/Desafio.AppHost.csproj
```

O .NET Aspire vai automaticamente:

- Provisionar containers Docker: **SQL Server**, **Redis**, **RabbitMQ**
- Iniciar a **API** (`Desafio.Server`)
- Iniciar o **Worker** (`Desafio.Consolidado.Worker`)
- Iniciar o **Frontend** (Vite React)
- Expor o **Dashboard Aspire** em `https://localhost:17048`

### Acessar os serviços

| Serviço | URL |
|---|---|
| Aspire Dashboard | https://localhost:17048 |
| API (Swagger/OpenAPI) | https://localhost:{porta}/swagger |
| Frontend | https://localhost:{porta-frontend} |
| Health Check API | https://localhost:{porta}/health |
| Health Check Worker | http://localhost:8081/health |

> As portas exatas são exibidas no terminal do Aspire e no Dashboard.

### Executar os testes

```bash
dotnet test src/Desafio.Tests.Unit/Desafio.Tests.Unit.csproj --verbosity normal
```

### Variáveis de ambiente

As configurações de desenvolvimento estão em `appsettings.Development.json` de cada projeto. Para produção, use variáveis de ambiente ou gerenciador de segredos.

| Variável | Descrição | Padrão (dev) |
|---|---|---|
| `ConnectionStrings__sql` | SQL Server connection string | Provisionado pelo Aspire |
| `ConnectionStrings__redis` | Redis connection string | Provisionado pelo Aspire |
| `ConnectionStrings__messaging` | RabbitMQ / Service Bus / SQS | Provisionado pelo Aspire |
| `Auth__Apim__SubscriptionKey` | Chave de assinatura APIM (Azure) | Desativado em dev |
| `Auth__EntraId__ValidToken` | Token válido Entra ID (Azure) | Desativado em dev |
