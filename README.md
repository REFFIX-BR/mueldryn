# Mu Eldryn — Season 6 Hard

Repositório do servidor **MuEldryn**: OpenMU, client MuMain, launcher e site MorpheusWeb.

## Estrutura

| Pasta | Descrição |
|-------|-----------|
| `OpenMU/` | Servidor de jogo (Connect + Game), deploy Swarm/Traefik |
| `MuMain/` | Cliente Season 6 (C++ / CMake) |
| `MUPegasusOldLauncher/` | Launcher com auto-update |
| `MorpheusWeb_SuporteS21(2)/` | Site (template `unique`) |
| `tools/` | Scripts de backup DB e deploy VPS |
| `assets/`, `skins/`, `vip-mueldryn/` | Artes e conteúdo custom |

**Não incluído** (ficam só na máquina local): `Source/` legado, dumps de banco, `vendor/` do PHP, builds (`bin/`, `out/`), templates Morpheus não usados.

## Setup rápido

### Site (local)

```bash
cd MorpheusWeb_SuporteS21(2)
cp configs/database.default.php configs/database.php
cp configs/openmu.default.php configs/openmu.php
# Edite database.php e openmu.php com suas credenciais
php -S 127.0.0.1:8090 -t public
```

O `vendor/` do PHP já vem no repositório.

### OpenMU (Docker local)

```bash
cd OpenMU/deploy/all-in-one
docker compose up -d
# Postgres exposto em localhost:5433
```

### Launcher

Ver `MUPegasusOldLauncher/README-MUELDRYN.md`.

### Deploy VPS

Ver `DEPLOY-VPS-MUELDRYN.md`.

## Configuração sensível

Estes arquivos **não vão pro Git** — copie dos `.default.php`:

- `MorpheusWeb_SuporteS21(2)/configs/database.php`
- `MorpheusWeb_SuporteS21(2)/configs/openmu.php`

## Licença

MIT — ver [LICENSE](LICENSE).
