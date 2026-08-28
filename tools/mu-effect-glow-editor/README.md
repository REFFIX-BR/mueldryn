# MU Item Effect & Glow Editor

Editor visual para glow e efeitos de itens Mudream / OpenMU + MuMain.
Objetivo: reduzir o ciclo recompilar → testar no `Main.exe`.

> **Aviso honesto:** personagem texturizado + pose attach é o alvo **obrigatório** do preview.
> FX/glow 100% iguais ao `Main.exe` / Live Client ainda podem diferir (blend, chrome, partículas nativas).

## Como deixar o 3D igual ao client (base)

O “laranja sólido” vinha de meshes `*_R` (Bright) pintados como opacos + glow emissive forte.
Agora o editor carrega **OZJ/OZT reais**, renderiza `*_R` em **additive**, e posiciona arma/asa de forma sane.

### 1. Rode o editor (sempre após puxar Data)

```bash
cd tools/mu-effect-glow-editor
npm install
npm run dev
```

Abre em `http://localhost:5177`. **Hard refresh** (Ctrl+F5) se o viewport ainda mostrar cache antigo.

### 2. Pasta Data (obrigatória)

O Vite serve automaticamente (nesta ordem):

1. `MuMain/out/build/windows-x86/src/RelWithDebInfo/Data`
2. `Mudream.online/Data`
3. Variável `MU_DATA_PATH` (caminho absoluto da pasta `Data`)

Confirme no banner: `HTTP: …\Data` ou escolha **Data folder…** (Chrome/Edge) apontando para a pasta que contém `Player/` e `Item/`.

Endpoints úteis:

| URL | Função |
|-----|--------|
| `/mu-data/__ping` | Confirma root Data |
| `/mu-data/Item/...` | Bytes brutos (BMD/OZJ/OZT) |
| `/mu-tex/Item/...` | OZJ→JPEG / OZT→PNG cache em `.preview-cache/` |

### 3. Preset Bloody Soldier

IDs corretos (Mudream catalog):

| Slot | Group:Index |
|------|-------------|
| Set (helm…boots) | `7–11:346` (M) / `347` (F) |
| Asa | `12:348` |
| Espada | `0:388` |
| Cape (opcional) | `12:347` |

Hellfire: `7–11:350`, asa `12:352`, espada `0:397`.

### 4. O que você deve ver (base client)

- Armadura **preto/prata/vermelho** com textura (não laranja)
- Asa com **alpha** (plumas vermelhas, não bloco sólido)
- Arma na **mão**, asa nas **costas** (attach approx)
- FX como **sprites soft additive** pequenos — não quads vermelhos cobrindo o peito
- Status: `BMD ok — N peça(s), M mesh(es) texturizado(s)`

### 5. Limitações (ainda approx)

| OK agora (obrigatório) | Ainda approx / Live Client |
|------------------------|----------------------------|
| Vértices/UVs + OZJ/OZT | Animação BMD completa |
| `*_R` additive (Bright) | Chrome / blend idêntico ao Main |
| Attach arma/asa sane | Bones LinkBone 1:1 do player |
| Soft FX gizmos | Códigos Mudream nativos / partículas Main |

Validação final de cosmético: **sempre no `Main.exe`**.

## Como rodar

```bash
cd tools/mu-effect-glow-editor
npm install
npm run dev
```

| Script | Descrição |
|--------|-----------|
| `npm run dev` | Vite + `/mu-data` + `/mu-tex` |
| `npm run build` | Build de produção |
| `npm run preview` | Preview do build |
| `npm run typecheck` | TypeScript |

## Layout

- **Banner** — fidelidade vs Main.exe + status Data/BMD
- **Toolbar** — Load / Save / Export, Undo/Redo, Compare, Data folder
- **Esquerda** — Character Loadout (presets Bloody / Hellfire)
- **Centro** — viewport 3D
- **Direita** — Glow / Effects / Particles
- **Baixo** — timeline (stub procedural)

## Arquitetura

```
src/
  app/        shell (toolbar, banner, timeline)
  viewport/   CharacterScene, BmdCharacter, EffectSprites
  loadout/    presets Bloody Soldier / Hellfire
  editors/    Glow, Effects, Particles, Attachment
  state/      zustand + zundo
  schema/     item_effects.json
  mu/         BMD parser, OZJ/OZT, catálogo, Data root
```

## Formato `item_effects.json`

Documento versionado (`schemaVersion: 1`) para um **reader futuro no client** aplicar cosméticos **sem recompilar** o Main. Ver exemplos em `samples/`.
