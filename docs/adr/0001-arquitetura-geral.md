# ADR-0001
# Arquitetura Geral da Plataforma

## Status

Aceita

## Contexto

O Instituto necessita desenvolver diversos sistemas internos destinados a setores distintos, como gestão administrativa, escalas, patrimônio, laudos, entre outros.

Embora independentes do ponto de vista funcional, esses sistemas compartilham diversas características:

- identidade visual;
- autenticação;
- autorização;
- infraestrutura;
- pipeline;
- auditoria;
- componentes de interface;
- padrões arquiteturais;
- convenções de desenvolvimento.

Desenvolver cada sistema de forma isolada acarretaria duplicação de código, aumento do custo de manutenção e dificuldade de evolução tecnológica.

## Decisão

A solução será estruturada como uma plataforma composta por três elementos principais:

1. Template Base
2. Bibliotecas Compartilhadas
3. Sistemas Independentes

O Template Base servirá como ponto inicial para qualquer novo sistema.

As bibliotecas compartilhadas concentrarão funcionalidades reutilizáveis.

Cada sistema possuirá apenas sua regra de negócio específica.

## Estrutura

Plataforma

├── template-sistema
│
├── design-system-angular
│
├── backend-core
│
├── sistema-escalas
│
├── sistema-patrimonio
│
├── sistema-laudos
│
└── ...

## Frontend

Todos os sistemas utilizarão Angular.

Será utilizado:

- Standalone Components
- Signals
- Lazy Loading
- Biblioteca compartilhada de componentes
- Design Tokens

## Backend

Todos os sistemas utilizarão ASP.NET Core.

Será adotada Clean Architecture contendo:

- Api
- Application
- Domain
- Infrastructure
- Persistence

## Banco de Dados

Cada ambiente possuirá banco próprio.

DEV

HML

PROD

As migrações serão realizadas utilizando Entity Framework Core.

## Infraestrutura

Toda aplicação será executada em containers Docker.

O deploy ocorrerá em servidor Linux.

O Nginx atuará como Reverse Proxy.

## Compartilhamento

As funcionalidades comuns ficarão em bibliotecas próprias.

Frontend

design-system-angular

Backend

backend-core

Essas bibliotecas serão versionadas independentemente dos sistemas.

## Ambientes

Cada ambiente possuirá:

- configurações próprias;
- banco próprio;
- secrets próprios;
- pipeline própria.

## Integração Contínua

Será utilizada GitHub Actions para:

- Build
- Testes
- Publicação
- Deploy

## Benefícios

- reutilização de código;
- padronização dos sistemas;
- facilidade de manutenção;
- redução de retrabalho;
- escalabilidade;
- evolução independente dos componentes compartilhados.

## Consequências

A criação da plataforma demanda maior esforço inicial.

Entretanto, os próximos sistemas poderão ser desenvolvidos significativamente mais rápido, utilizando uma base arquitetural consolidada e padronizada.

Planejamento
        │
        ▼
ADRs
        │
        ▼
Organização GitHub
        │
        ▼
Design System
        │
        ▼
Backend Core
        │
        ▼
Template
        │
        ▼
Docker
        │
        ▼
Banco
        │
        ▼
Angular + .NET comunicando
        │
        ▼
Autenticação
        │
        ▼
GitHub Actions
        │
        ▼
Deploy
        │
        ▼
Servidor Linux
        │
        ▼
Nginx
        │
        ▼
HTTPS
        │
        ▼
Ambientes DEV/HML/PROD
        │
        ▼
Primeiro Sistema
        │
        ▼
Segundo Sistema
        │
        ▼
Terceiro Sistema

## Princípios Arquiteturais

- Reutilização antes de duplicação: funcionalidades comuns devem ser extraídas para bibliotecas compartilhadas.
- Configuração por ambiente: nenhum valor específico de ambiente deve estar no código-fonte.
- Baixo acoplamento: sistemas devem depender de contratos e bibliotecas estáveis, não de implementações de outros sistemas.
- Automação como padrão: build, testes e deploy devem ser automatizados sempre que possível.
- Segurança desde o início: autenticação, autorização, logs e auditoria fazem parte da plataforma, não são funcionalidades adicionadas depois.
- Evolução incremental: a plataforma deve permitir que novos sistemas sejam criados e que as bibliotecas evoluam sem exigir grandes refatorações.

## Escopo adiado (estágio atual)

As decisões abaixo permanecem válidas como princípios da plataforma, mas **não serão implementadas neste momento** — a equipe é reduzida e o foco é entregar valor incremental:

- **Auditoria imutável / cadeia de custódia / event sourcing:** adiado. Mantém-se apenas metadados básicos em entidades (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`).
- **Separação por schema/setor (schema-per-sector) ou multi-tenancy:** adiado. Um banco PostgreSQL por ambiente, schema único (`public`).