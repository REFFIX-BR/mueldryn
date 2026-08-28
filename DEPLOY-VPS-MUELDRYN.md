# Deploy MuEldryn na VPS (Swarm + Postgres + Traefik)

Guia para subir **OpenMU + update do launcher** na VPS `200.11.121.89`, mantendo o **site Morpheus local** por enquanto, **sem perder dados** do ambiente de desenvolvimento.

---

## O que você tem hoje (local)

| Banco | Onde | Conteúdo |
|-------|------|----------|
| **Postgres `openmu`** | Docker `database`, porta **5433** | Jogo: 22 contas, 78 chars, joias, itens, vault… |
| **SQL Server `MuOnline`** | SQLEXPRESS | Site: 6 contas web, créditos, loja, configs Morpheus |

São **dois bancos diferentes**. O jogo na VPS usa só o **Postgres**. O site local continua no **SQL Server** até você subir o Morpheus depois.

**Backups já gerados** (pasta `backups/`):

- `openmu-local.dump` (~2,5 MB) — **este vai para o Postgres da VPS**
- `morpheus-muonline.bak` — guardar na VPS como backup do site (ou copiar do SQL Server se a cópia local falhou por permissão)

Para regerar:

```powershell
cd tools
.\Export-LocalDatabases.ps1
```

---

## Fase 1 — Postgres na VPS (dados do jogo)

### 1. Enviar o dump

```powershell
scp backups/openmu-local.dump root@200.11.121.89:/root/
scp tools/import-openmu-vps.sh root@200.11.121.89:/root/
```

### 2. Restaurar na VPS

```bash
ssh root@200.11.121.89
chmod +x import-openmu-vps.sh
# Ajuste senha se necessário: export PGPASSWORD=...
./import-openmu-vps.sh openmu-local.dump
```

Deve mostrar **22 accounts** e **78 characters** (mesmos números do local).

### 3. Permitir conexão do Docker → Postgres host

Se Postgres roda **no host** (não em container), libere acesso local:

```bash
# postgresql.conf
listen_addresses = 'localhost,172.17.0.1'

# pg_hba.conf — rede docker bridge (ajuste subnet se diferente)
host    openmu    postgres    172.17.0.0/16    scram-sha-256
host    openmu    postgres    172.18.0.0/16    scram-sha-256
```

Reinicie Postgres: `systemctl restart postgresql`

No stack OpenMU usamos `DB_HOST=host.docker.internal` (ver `.env`).

---

## Fase 2 — Imagem OpenMU customizada

A VPS precisa da imagem **`openmu-rare:local`** (393 MB), não só `munique/openmu`.

**No seu PC:**

```powershell
docker save openmu-rare:local -o backups/openmu-rare-local.tar
scp backups/openmu-rare-local.tar root@200.11.121.89:/root/
```

**Na VPS:**

```bash
docker load -i openmu-rare-local.tar
docker images | grep openmu-rare
```

---

## Fase 3 — Stack Swarm (sem nginx, sem Postgres no compose)

Arquivos em `OpenMU/deploy/swarm-vps/`:

```bash
mkdir -p /opt/mueldryn/stack
# copie docker-compose.stack.yml e .env
cd /opt/mueldryn/stack
cp .env.example .env
nano .env   # senha postgres, domínios Traefik
```

Rede `proxy` (mesma do Traefik):

```bash
docker network ls | grep proxy
# se não existir: docker network create --driver overlay --attachable proxy
```

Deploy:

```bash
docker stack deploy -c docker-compose.stack.yml openmu
docker service ls
docker service logs openmu_openmu -f
```

### Portas no firewall (UFW / painel cloud)

| Porta | Uso |
|-------|-----|
| **44406** | ConnectServer (client MuMain) |
| **55901–55906** | GameServers |
| 44405, 55980 | auxiliares OpenMU |

**PSGuard desligado** — client conecta direto na 44406.

### Variáveis importantes

```env
DB_HOST=host.docker.internal
DB_ADMIN_PW=<senha postgres vps>
RESOLVE_IP=200.11.121.89
Database__AssumeExternallyProvisioned=true   # já no compose
```

`RESOLVE_IP=200.11.121.89` faz o client receber o IP público ao entrar no GS (não loopback).

---

## Fase 4 — Update do launcher (Traefik)

Estrutura na VPS:

```
/opt/mueldryn/www/
  update/
    MiniUpdate/
      update.info
      Data/...
```

Publicar do PC:

```powershell
scp -r MUPegasusOldLauncher\UpdateServer\MiniUpdate root@200.11.121.89:/opt/mueldryn/www/update/
```

No `.env`: `UPDATE_ROOT=/opt/mueldryn/www`

Launcher aponta para: `http://200.11.121.89/update/` (Traefik roteia `/update` → nginx interno).

Se usar só IP sem TLS, ajuste labels Traefik para `entrypoints=web` ou regra `Host(\`200.11.121.89\`)`.

---

## Fase 5 — Site local apontando para VPS (opcional agora)

Enquanto o site fica no PC, você pode fazer o **Bridge OpenMU** ler o Postgres da VPS (joias, vault sync):

`MorpheusWeb_SuporteS21(2)/configs/openmu.php`:

```php
'dsn' => 'pgsql:host=200.11.121.89;port=5432;dbname=openmu;connect_timeout=5',
```

Requer Postgres da VPS aceitando conexão do **seu IP** (só para dev; em produção use VPN ou site na mesma rede).

O **SQL Server** (`database.php`) continua local — contas web, VIP, créditos Morpheus.

---

## Checklist rápido

- [ ] Dump restaurado na VPS (22 contas / 78 chars)
- [ ] Imagem `openmu-rare:local` carregada
- [ ] Stack `openmu` rodando, logs OK
- [ ] Firewall 44406 + 55901 abertas
- [ ] PSGuard parado na VPS
- [ ] `/update/MiniUpdate/` acessível
- [ ] Teste: `Main.exe` ou launcher → login `testgm`
- [ ] Backup `morpheus-muonline.bak` guardado na VPS

---

## Ordem recomendada de execução

1. Restaurar Postgres (dados)
2. Carregar imagem Docker
3. Subir stack OpenMU
4. Testar conexão do client
5. Publicar MiniUpdate
6. Distribuir zip do client (`pack/client`)

Quando quiser, no próximo passo fazemos juntos o SSH na VPS (senha/host) e rodamos restore + stack passo a passo.
