# SIGAD-IC — orientações permanentes do agente

> Este arquivo é carregado automaticamente pelo Claude Code no início de cada sessão neste projeto. Mantenha-o atualizado conforme as convenções do projeto mudarem.

## Contexto do projeto

**GESTAO-IC** — template base e primeiro sistema da Polícia Científica do RN (Instituto de Criminalística).

### Stack

| Camada    | Tecnologia |
|-----------|------------|
| Frontend  | Angular 20, Node 24, `@davillawitte/pci-design-system` |
| Backend   | .NET 10, Clean Architecture |
| Banco     | PostgreSQL 17 |
| Container | Docker Compose |
| CI        | GitHub Actions |

### Estrutura do repositório

```text
gestao-ic/
├── backend/              # API .NET (Clean Architecture)
│   └── src/
│       ├── Api/              # Entry point, Swagger, Health, CORS
│       ├── Application/      # Casos de uso, FluentValidation
│       ├── Domain/           # Entidades e contratos
│       ├── Infrastructure/   # Serviços externos
│       └── Persistence/      # EF Core + PostgreSQL
├── frontend/             # SPA Angular
│   └── src/app/
│       ├── core/             # Serviços singleton, guards, models
│       ├── shared/           # Componentes reutilizáveis
│       ├── layout/           # Shell da aplicação
│       └── features/         # Módulos de funcionalidade (lazy loading)
├── docker/               # Dockerfiles
├── docker-compose.yml    # Stack local (usa .env da raiz)
├── docs/                 # ADRs e documentação
├── scripts/              # Scripts utilitários
└── .github/              # Pipelines CI
```

### Comandos

- **Backend (local):** `cd backend && dotnet restore && dotnet run --project src/Api` → API em `http://localhost:5080`, Swagger em `/swagger`, health em `/health`.
- **Frontend (local):** `./scripts/setup-frontend.sh` (instala deps + design system) depois `npm start --prefix frontend` → app em `http://localhost:4200`.
- **Stack completa via Docker:** na raiz, com `.env` preenchido, `docker compose up --build` (frontend `:4200`, API `:5080`, Postgres host `:5433` → banco `gestao_ic_dev`). Derrubar com `docker compose down`.
- **Testes backend:** `cd backend && dotnet test`.
- **Testes frontend:** `cd frontend && npm test`.
- **CI:** `.github/workflows/ci.yml` — restore, lint/build Angular, build .NET, testes. Requer secret `NODE_AUTH_TOKEN` (PAT `read:packages`) configurado no repositório, nunca via `.env` local.

### Banco de dados

- Um banco PostgreSQL por ambiente, schema único (`public`) — sem multi-tenant nem schema por setor neste estágio (evolução futura planejada, ver `ApplicationDbContext` e ADR-0001).
- Development / Docker local → `gestao_ic_dev`. Production → `gestao_ic` (via `ConnectionStrings__DefaultConnection`). Testing/CI → `gestao_ic_test`.

### Fora de escopo por ora

Não implementar a menos que explicitamente pedido: auditoria imutável / cadeia de custódia / event sourcing (só há metadados básicos em `BaseEntity`: `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`); multi-tenant / schema por setor.

### Credenciais e segredos

Segredos nunca vão para o repositório. Nunca sugerir commitar `.env`, tokens ou PATs. O `.env` local (baseado em `.env.example`) carrega `NODE_AUTH_TOKEN` para o design system via GitHub Packages; no CI, esse valor vem do secret do repositório.

## Design system first (PCI)

Sempre use `@davillawitte/pci-design-system` para UI neste projeto.

### Obrigatório

- Preferir componentes da lib: `pci-button`, `pci-input`, `pci-select`, `pci-datepicker`, `pci-checkbox`, `pci-alert`, `pci-badge`, `pci-tabs`, `pci-stepper`, `pci-toast` / `PciToastService`, `pci-feedback-modal` / `PciFeedbackModalService`, `pci-page-header`, `pci-form-page`, `pci-list-page`, `pci-stack`, ícones nos botões, etc.
- Antes de montar UI "na mão" (HTML / `mat-*` cru / CSS custom de form control), verificar se a lib já oferece o equivalente.
- Selects → `pci-select` (não `mat-select` solto com wrapper `pci-field`).
- Feedback de sucesso/erro de ação → toast ou feedback modal da lib (não `window.alert` / texto solto).
- Stepper / tabs / badges / botões com ícone → componentes da lib.

### Exceção

Só use Angular Material cru, HTML custom ou CSS próprio quando **não existir** equivalente no design system (ou o componente da lib for insuficiente de forma comprovada — explicitar o motivo no código/PR).

### Ao implementar UI

1. Consultar exports do pacote `@davillawitte/pci-design-system` antes de escrever qualquer componente novo.
2. Seguir o mesmo padrão visual das telas já alinhadas à lib.
3. Não reinventar controles que a lib já oferece.
4. Se precisar implementar algo localmente por falta de suporte no design system (ex.: um ícone
   que não existe no pacote), registrar em `docs/design-system-pendencias.md` — o quê, por quê,
   onde é usado, e a especificação pra portar pro pacote depois.

## Organização de features (frontend)

Cada feature em `frontend/src/app/features/<feature>/`:

```text
pages/
  <nome>-list/     # listagem/consulta → .ts + .html + .scss
  <nome>-form/     # cadastro/edição (wizard em etapas e cópia também são form)
  <nome>-detail/   # só visualização, quando não for list nem form
components/        # pedaços reutilizáveis (pasta por componente)
services/
models/
<feature>.routes.ts
```

Regras:
- Lista → `*-list/`. Cadastra/edita → `*-form/`.
- Sempre `templateUrl` / `styleUrl` externos — **proibido** HTML/CSS inline no `.ts`.
- Arquivos **sem** sufixo `.component` (`escala-list.ts`, não `escala-list.component.ts`).
- Regra detalhada em `.cursor/rules/frontend-feature-layout.mdc` — consultar esse arquivo antes de criar uma feature nova.

## Checklist obrigatório ao implementar uma funcionalidade

Antes de considerar a tarefa concluída, o agente deve:

1. Verificar que o projeto compila sem erros.
2. Executar os testes automatizados existentes.
3. Verificar que as APIs afetadas continuam funcionando.
4. Verificar que funcionalidades relacionadas à alteração continuam funcionando.
5. Não alterar funcionalidades existentes sem necessidade explícita da tarefa.
6. Se a alteração puder impactar outra área do sistema, fazer uma análise de regressão antes de finalizar.

### Relatório final obrigatório

Ao terminar qualquer implementação, informar ao usuário:

- **O que foi alterado** (arquivos e resumo da mudança).
- **Quais testes foram executados** (e o resultado).
- **Quais áreas foram verificadas** além do escopo direto da tarefa.
- **Se existe risco de regressão** e, se sim, qual e onde.

Nunca marcar uma tarefa como concluída sem esse relatório.