# Deploy em produção (LAN) — SIGAD-IC ao lado do gestao-SCFA

Acesso previsto nesta etapa (sem DNS / sem TLS):

| Sistema | URL | Porta no host |
|---------|-----|---------------|
| **gestao-SCFA** | http://10.9.233.98/ | **80** (intocado) |
| **SIGAD-IC** | http://10.9.233.98:8080/ | **8080** (nginx de borda) |

O SIGAD **não** publica a porta 80. O SCFA continua como está — nenhum arquivo do SCFA é alterado por este repositório.

```text
Cliente
  ├─ :80  ──► SCFA (compose do SCFA; fora deste repo)
  └─ :8080 ─► edge-nginx ─► sigad-ic-frontend ─► sigad-ic-api ─► postgres SIGAD
```

Rede Docker compartilhada `edge` (external): o frontend do SIGAD entra nela; no futuro o client do SCFA também pode entrar **sem** `docker compose down` do SIGAD remover a rede (ela é `external: true`).

## 1. Pré-requisitos na máquina

- Docker Engine + Docker Compose plugin
- Porta **8080** livre (a **80** fica com o SCFA)
- Arquivo `.env.prod` com segredos (não versionado)
- Rede `edge` (o script de up cria se não existir)

## 2. Configurar ambiente

```bash
cp .env.prod.example .env.prod
```

Edite `.env.prod`:

| Variável | Obrigatório | Nota |
|----------|-------------|------|
| `DB_PASSWORD` | sim | senha forte do Postgres do SIGAD |
| `JWT_KEY` | sim | ≥ 32 caracteres (`openssl rand -base64 48`) |
| `NODE_AUTH_TOKEN` | sim | PAT GitHub com `read:packages` |
| `APP_ORIGIN` | recomendado | `http://10.9.233.98:8080` |
| `EDGE_HTTP_PORT` | opcional | padrão `8080` |
| `ALLOWED_HOSTS` | opcional | `10.9.233.98;localhost;127.0.0.1` |

## 3. Subir

```bash
chmod +x scripts/docker-up-prod.sh scripts/docker-down-prod.sh scripts/backup-postgres.sh
./scripts/docker-up-prod.sh
```

URLs:

- App: http://10.9.233.98:8080/
- Health: http://10.9.233.98:8080/health
- API (mesmo host): http://10.9.233.98:8080/api/...

Na primeira subida a API aplica migrations e seed (ambiente `Production`).

## 4. Parar (só o SIGAD)

```bash
./scripts/docker-down-prod.sh
```

Isso **não** derruba o SCFA. A rede `edge` permanece (external).

## 5. Backup do banco (SIGAD)

Com o Postgres do SIGAD em execução:

```bash
./scripts/backup-postgres.sh          # auto (prod ou dev)
./scripts/backup-postgres.sh prod     # força produção
./scripts/backup-postgres.sh dev      # força desenvolvimento
```

Arquivos em `backups/`:

- `*.dump` — formato custom (`pg_restore`)
- `*.sql` — SQL texto

### Restaurar (produção)

```bash
# Exemplo com dump custom:
docker cp backups/gestao_ic_YYYYMMDD_HHMMSS.dump sigad-ic-postgres:/tmp/restore.dump
docker exec -e PGPASSWORD="$DB_PASSWORD" -i sigad-ic-postgres \
  pg_restore -U postgres -d gestao_ic --clean --if-exists /tmp/restore.dump
```

## 6. Diferença vs desenvolvimento

| | Dev (`docker-compose.yml`) | Prod (`docker-compose.prod.yml` + edge) |
|--|--|--|
| Frontend público | :4200 | :8080 (edge-nginx) |
| API | :5080 (host) | só rede interna |
| Postgres | :5433 (host) | só rede interna |
| Ambiente API | `Docker` | `Production` |
| Swagger | sim | não |

## 7. Firewall

Libere **TCP 8080** para a LAN (SIGAD). A **TCP 80** continua sendo do SCFA. Não publique 5432/5080 do SIGAD no host.

## 8. Memória (dois Postgres na mesma máquina)

O Postgres do SIGAD sobe com buffers conservadores (`shared_buffers=256MB`, `work_mem=8MB`, `effective_cache_size=768MB`). Expectativa aproximada em idle+carga leve: **~0,5–1 GB** para o Postgres do SIGAD, além do que o SCFA já usa. Logs Docker limitados (`json-file` 10m × 3 arquivos) nos dois stacks do SIGAD (app + edge).

## 9. Recomendações operacionais no SCFA (sem alterar código daqui)

Não fazemos mudanças no repositório do SCFA. Quando houver janela de manutenção no SCFA:

1. Publicar o Postgres do SCFA só em `127.0.0.1:5433:5432` (hoje em `0.0.0.0` expõe o banco na LAN).
2. Limitar logs Docker no compose do SCFA (`max-size` / `max-file`).
3. (Futuro) Tirar a publicação da porta 80 do SCFA, conectar o client à rede `edge` e acrescentar `listen 80` no `docker/edge-nginx.conf` apontando para o SCFA — aí o edge vira dono de 80 e 8080 (e depois 443).

## 10. Próximas etapas (quando houver DNS / TLS)

1. Acrescentar `server_name` no edge para hostnames definidos.
2. `listen 443 ssl` + redirect 301 de `:80` + HSTS.
3. O frontend do SIGAD **não precisa rebuild** (`apiUrl` relativo).
