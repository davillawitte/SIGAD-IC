# Deploy em produção (LAN) — SIGAD-IC ao lado do gestao-SCFA

## Checklist do deploy de hoje (rotação da instância em 10.9.233.98)

A 10.9.233.98 já está no ar, provisionada antes de existir a tela `/setup` — ainda com a
credencial fictícia `vitorlopes` / `Vitor@123` (ver seção 11). O plano é: subir o código
atual (todas as correções desta fase) por cima do que já está rodando, sem perder dados, e
então trocar essa credencial fictícia por um superadministrador real. Ordem recomendada:

1. **Backup antes de tudo** (rede de segurança caso algo dê errado no passo 2) — seção 5.
2. **Redeploy do código atual** — `git pull` + `./scripts/docker-up-prod.sh` (seção 3). Isso
   reconstrói as imagens e aplica as migrations pendentes automaticamente (rodam a cada
   subida, não só na primeira — são idempotentes). `/setup` continua 410 Gone o tempo todo
   nesse passo, porque o `vitorlopes` já existe como superadministrador ativo.
3. **Rotacionar a credencial fictícia** — seção 12, subseção "Rotação da instância já
   implantada": `reset-admin-password`, corrigir o `Servidor` fictício pela tela e renomear
   o `Usuario.Login` pro CPF real.
4. **Validar** (passo 4 da mesma subseção): login com o CPF real funciona; `vitorlopes` não
   existe mais como login; `GET /api/setup/status` continua respondendo `410 Gone` (prova de
   que a tela de provisionamento não volta a aparecer).

As seções abaixo têm o detalhe de cada passo. A explicação completa do wizard `/setup` (para
quando um banco **novo**, sem nenhum superadministrador, precisar ser provisionado do zero —
próxima instalação, ambiente de homologação, etc.) está na seção 11.

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

A cada subida (não só a primeira) a API aplica migrations pendentes e o seed de catálogo
(cargos, setores, perfis) automaticamente, antes de aceitar conexões — ambiente `Production`.

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

## 11. Provisionamento do superadministrador (`/setup`)

Não existe mais credencial fixa no código. No primeiro start sem superadministrador
ativo, a API gera um token de uso único (TTL 60 min) e imprime o valor **uma vez** no
log:

```bash
docker compose -f docker-compose.prod.yml logs api | grep "SETUP:"
```

Acesse `http://<host>:8080/setup` e informe o token + os dados reais do primeiro
superadministrador (CPF, nome, matrícula, e-mail, data de nascimento, senha — mínimo de
8 caracteres, sem exigência de maiúscula/símbolo, mas bloqueando uma lista curta de senhas
muito comuns — que inclui de propósito `vitor@123`/`vitorlopes`, pra ninguém reintroduzir a
credencial fictícia como senha real). O wizard cria a cadeia completa (Servidor + Usuario +
perfil SuperAdministrador + chefia de Diretor na Direção IC) e já força troca de senha no
primeiro login (`DeveAlterarSenha = true`).

**Recomendação operacional**: rode o setup a partir da própria máquina servidora
(`http://localhost:8080/setup`) antes de divulgar o endereço na rede — enquanto for HTTP
puro, o token e a senha cruzam a rede em texto claro. Aceitável numa LAN controlada por
alguns minutos, mas é exatamente o motivo para priorizar TLS (seção 10).

Depois que existir um superadministrador ativo, todo `/api/setup/*` (inclusive
`status`) responde `410 Gone` — o wizard se autodesativa.

Se o TTL expirar antes de concluir:

```bash
docker compose -f docker-compose.prod.yml run --rm api new-setup-token
```

`allow 10.9.233.0/24;` em `docker/edge-nginx.conf` restringe `/api/setup/` à rede
institucional — ajuste a faixa se a rede real for outra.

## 12. Recuperação de senha do superadministrador

Sem SMTP configurado, se o único superadministrador perder a senha o sistema fica
inacessível por essa via. Comando interativo (pede confirmação explícita do login alvo e
registra a operação no log da API):

```bash
docker compose -f docker-compose.prod.yml run --rm api reset-admin-password
```

### Rotação da instância já implantada (10.9.233.98)

A instância em produção foi provisionada antes desta mudança e ainda usa a credencial
antiga (`vitorlopes` / `Vitor@123`, presente no histórico do Git). Passos operacionais —
não há mais nada a mudar no código:

1. Rodar `reset-admin-password` (comando acima) na instância existente.
2. Corrigir o `Servidor` semeado, que hoje tem dados fictícios (CPF `00000000000`,
   matrícula `000.001-0`, nascimento `1990-01-01`) — atualizar pela própria tela de
   edição de servidor com os dados reais da pessoa. Atenção: `Servidor.Cpf` é único, o
   CPF falso ocupa um slot real e precisa ser **atualizado**, não duplicado.
3. Alinhar o login ao CPF real (todo o resto do sistema usa CPF como login via
   `SenhaTemporaria.NormalizeLoginCpf`; `vitorlopes` era a única exceção). **Não existe
   tela nem endpoint para renomear login** — corrigir o `Servidor` (passo 2) não muda o
   `Usuario.Login`. Requer um `UPDATE` direto no banco:
   ```sql
   UPDATE "Usuario" SET "Login" = '<cpf-normalizado-11-digitos>' WHERE "Login" = 'vitorlopes';
   ```
   Faça isso com o Postgres do SIGAD parado ou em janela de manutenção, e valide o login
   novo antes de divulgar.
4. Validar o resultado:
   - Login com o CPF real (senha nova do passo 1) funciona e força troca de senha
     (`DeveAlterarSenha = true`, herdada do `reset-admin-password`).
   - `vitorlopes` não autentica mais (login não existe).
   - `/api/setup/status` continua `410 Gone` — nunca existiu (nem vai existir) uma janela em
     que a tela de provisionamento reaparece, porque o gate é "existe superadministrador
     ativo no banco", não o token:
     ```bash
     curl -i http://127.0.0.1:8080/api/setup/status
     ```
