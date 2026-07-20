# GESTAO-IC

Template base e primeiro sistema da **Polícia Científica do RN** — **Instituto de Criminalística**.

Stack: **Angular 20**, **.NET 10** (Clean Architecture) e **PostgreSQL**.

## Stack

| Camada    | Tecnologia              |
|-----------|-------------------------|
| Frontend  | Angular 20, Node 24, `@davillawitte/pci-design-system` |
| Backend   | .NET 10, Clean Architecture |
| Banco     | PostgreSQL 17           |
| Container | Docker Compose          |
| CI        | GitHub Actions          |

## Estrutura

```
gestao-ic/
├── backend/          # API .NET (Clean Architecture)
├── frontend/         # SPA Angular
├── docker/           # Dockerfiles e compose
├── docs/             # ADRs e documentação
├── scripts/          # Scripts utilitários
└── .github/          # Pipelines CI
```

### Backend

```
backend/src/
├── Api/              # Entry point, Swagger, Health, CORS
├── Application/      # Casos de uso, FluentValidation
├── Domain/           # Entidades e contratos
├── Infrastructure/   # Serviços externos
└── Persistence/      # EF Core + PostgreSQL
```

### Frontend

```
frontend/src/app/
├── core/             # Serviços singleton, guards, models
├── shared/           # Componentes reutilizáveis
├── layout/           # Shell da aplicação
└── features/         # Módulos de funcionalidade (lazy loading)
```

## Estratégia de banco de dados

- **Um banco PostgreSQL por ambiente** (Development, Docker/local, Production, Testing).
- **Schema único** (`public`) — sem multi-tenant nem schema por setor neste estágio.
- **Evolução futura planejada:** separação por schema/setor quando a equipe e os módulos crescerem (ver comentário em `ApplicationDbContext` e ADR-0001).

| Ambiente | Arquivo / origem | Banco |
|----------|------------------|-------|
| Development | `appsettings.Development.json` | `gestao_ic_dev` |
| Docker (local) | `appsettings.Docker.json` + compose | `gestao_ic_dev` |
| Production | `appsettings.Production.json` + env vars | `gestao_ic` (via `ConnectionStrings__DefaultConnection`) |
| Testing (CI) | `appsettings.Testing.json` | `gestao_ic_test` |

## Escopo adiado

- **Auditoria imutável / cadeia de custódia / event sourcing** — não implementado. Apenas metadados básicos em `BaseEntity` (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`).
- **Multi-tenant / schema por setor** — não implementado.

## Pré-requisitos

- [Node.js 24+](https://nodejs.org/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (opcional)
- **PAT GitHub** com escopo `read:packages` (para o design system)

## Design system (`@davillawitte/pci-design-system`)

Publicado no **GitHub Packages**. Configure autenticação:

```bash
cp frontend/.npmrc.example frontend/.npmrc
export NODE_AUTH_TOKEN=ghp_xxxxxxxx   # PAT com read:packages
./scripts/setup-frontend.sh
```

Tokens CSS globais já importados em `frontend/src/styles.scss`.

## Desenvolvimento local

### Backend

```bash
cd backend
dotnet restore
dotnet run --project src/Api
```

- API: http://localhost:5080
- Swagger: http://localhost:5080/swagger
- Health: http://localhost:5080/health

### Frontend

```bash
export NODE_AUTH_TOKEN=ghp_xxxxxxxx
./scripts/setup-frontend.sh
npm start --prefix frontend
```

App: http://localhost:4200

### Docker (stack completa)

```bash
export NODE_AUTH_TOKEN=ghp_xxxxxxxx   # necessário para build do frontend
./scripts/docker-up.sh
```

- Frontend: http://localhost:4200
- API / Swagger: http://localhost:5080/swagger
- Postgres (host): `localhost:5433` → banco `gestao_ic_dev`

```bash
./scripts/docker-down.sh
```

## Testes

```bash
cd backend && dotnet test
cd frontend && npm test
```

## CI

Configure o secret **`NODE_AUTH_TOKEN`** no repositório (PAT com `read:packages`).

O pipeline em `.github/workflows/ci.yml` executa restore, lint/build Angular, build .NET e testes.

## Próximos passos

1. Renomear `TemplateSistema` → nome definitivo do projeto (quando conveniente)
2. Adicionar entidades em `Domain/`
3. Implementar casos de uso em `Application/`
4. Criar features em `frontend/src/app/features/`
5. Consulte os ADRs em `docs/adr/`
