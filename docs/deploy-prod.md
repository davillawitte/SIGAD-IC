# Deploy em produção (LAN) — SIGAD-IC

Acesso previsto: **http://10.9.233.98/** (nginx na porta 80; API e Postgres só na rede Docker).

## 1. Pré-requisitos na máquina

- Docker Engine + Docker Compose plugin
- Porta **80** livre
- Arquivo `.env.prod` com segredos (não versionado)

## 2. Configurar ambiente

```bash
cp .env.prod.example .env.prod
```

Edite `.env.prod`:

| Variável | Obrigatório | Nota |
|----------|-------------|------|
| `DB_PASSWORD` | sim | senha forte do Postgres |
| `JWT_KEY` | sim | ≥ 32 caracteres (`openssl rand -base64 48`) |
| `NODE_AUTH_TOKEN` | sim | PAT GitHub com `read:packages` |
| `APP_ORIGIN` | recomendado | `http://10.9.233.98` (CORS) |
| `HTTP_PORT` | opcional | padrão `80` |

Se o IP mudar, atualize `APP_ORIGIN` e, se necessário, `server_name` em `docker/nginx.conf`.

## 3. Subir

```bash
chmod +x scripts/docker-up-prod.sh scripts/docker-down-prod.sh scripts/backup-postgres.sh
./scripts/docker-up-prod.sh
```

URLs:

- App: http://10.9.233.98/
- Health: http://10.9.233.98/health
- API (mesmo host): http://10.9.233.98/api/...

Na primeira subida a API aplica migrations e seed (ambiente `Production`).

## 4. Parar

```bash
./scripts/docker-down-prod.sh
```

## 5. Backup do banco

Com o Postgres em execução:

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

| | Dev (`docker-compose.yml`) | Prod (`docker-compose.prod.yml`) |
|--|--|--|
| Frontend | :4200 | :80 |
| API | :5080 (host) | só rede interna |
| Postgres | :5433 (host) | só rede interna |
| Ambiente API | `Docker` | `Production` |
| Swagger | sim | não |

## 7. Firewall

Libere **TCP 80** para a LAN. Não é necessário publicar 5432/5080 no host.
