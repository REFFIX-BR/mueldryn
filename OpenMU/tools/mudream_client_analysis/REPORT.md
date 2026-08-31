# Mudream Client Analysis Report

**Purpose:** Developmental reverse-engineering notes for porting Mudream UI/systems into the owned **MuMain** client and **OpenMU** private server.  
**Scope:** Binary inventory, string/RTTI clues, config/asset mapping, protocol cross-ref. No DRM bypass, anti-cheat defeat, or exploit PoCs.  
**Date:** 2026-08-22  
**Client root:** `Mudream.online/`

---

## 1. Binary inventory

There is **no `Main.exe`**. The game client is **`MU.exe`**. The launcher is **`PlayMudream.exe`**.

| Path | Size | Role |
|------|------|------|
| `Mudream.online/Reborn/MU.exe` | **21,945,856** (~20.9 MB) | Primary game binary (richer strings/RTTI; preferred analysis target) |
| `Mudream.online/x64/MU.exe` | **16,008,704** (~15.3 MB) | Alternate/slim x64 build (same custom class names; fewer string hits) |
| `Mudream.online/PlayMudream.exe` | 4,759,552 | .NET launcher |
| `Mudream.online/unins000.exe` | ~4.9 MB | Installer uninstall stub (ignore) |

### Supporting DLLs

| Location | DLLs |
|----------|------|
| Client root | `libEGL.dll`, `libGLESv2.dll` (ANGLE/OpenGL ES), `Newtonsoft.Json.dll`, `System.Net.Http.dll` |
| `Reborn/` and `x64/` | `BugSplat64.dll`, `BugSplatRc64.dll`, `discord_game_sdk.dll`, `fmod.dll` |
| Same folders | `BsSndRpt64.exe`, `BugSplatHD64.exe` (crash reporting helpers) |

No custom gameplay plugin DLLs were found beside media/crash/Discord/FMOD. Custom systems live **inside `MU.exe`** plus **Data** configs/assets.

### Top-level folders

| Folder | Notes |
|--------|-------|
| `Data/` | ~32k files — game content |
| `Reborn/` | Full `MU.exe` + runtime DLLs |
| `x64/` | Alternate `MU.exe` + same runtime set |
| `Logs/`, `ScreenShots/` | Runtime output |

### `Data/` beyond Interface

| Area | Contents |
|------|----------|
| `Local/` | BMD tables + **plaintext XML** for custom systems (goldmine for porting) |
| `Interface/` | UI assets (encrypted `*.pdream` / `*.tdream` / `*.jdream` + some PNG/JPG/DDS/JSON) |
| `InGameShop*`, `Item`, `NPC`, `Monster`, `Player`, `Skill` | Classic MU content packs |
| `World*`, `Object*` | Maps / objects (large set incl. high map IDs 100+) |
| `Effect`, `Music`, `Sound`, `Logo`, `Launcher` | Presentation / media |

### Interface asset encryption

Live folders store Mudream-wrapped textures:

| Extension | Approx. count (Interface tree) | Notes |
|-----------|--------------------------------|-------|
| `.png` / `.jpg` | 1529 / 529 | Many plain (legacy / HUD) |
| `.tdream` | 493 | Encrypted (e.g. MainMenu panels) |
| `.pdream` | 311 | Encrypted (EventList, LegendUI, Atlas, …) |
| `.jdream` | 257 | Encrypted |
| `.dds` / `.ozt` / `.ozj` | smaller | Classic formats |
| `.json` | 14 | **Plain** UI layout atlases (very useful) |

Decrypted catalogs from prior work: `Data/Interface/_catalog/`, `Data/Interface/_decrypted/`, and `OpenMU/tools/mudream_interface_export/`.

---

## 2. Systems discovered (from RTTI + paths + configs)

MSVC RTTI in `Reborn/MU.exe` (namespace `SEASON3B`) names the custom windows/systems:

### Core custom UI (high confidence)

| RTTI / class clue | Likely feature | Asset / config anchors |
|-------------------|----------------|------------------------|
| `CNewUIEventListWindow` | Events schedule / timer list (“Events Timer”) | `Interface/EventList/*` |
| `CEventTimer` | Event countdown timer widget | LegendUI / EventList |
| `CActiveInvasions` | Active invasion panel | `Interface/ActiveInvasion/*` |
| `SoulSystem` | Soul point allocation UI | `Interface/Atlas/SoulSystem/`, `Local/xml/SoulSystem.xml` |
| `QuestSystem`, `QuestSystemMissionView`, `QuestSystemNpcDialog`, `CQuestSystemEx`, `CQuestSystemLog` | Custom quest system + NPC dialog + mission view + log | `Interface/Atlas/QuestSystem/`, `Interface/QuestInfo/`, `Local/xml/QuestSystem/QuestSystemText.xml` |
| `CNewUIMainMenu`, `CDelgardoMainMenuMsgBox*` | In-game main menu | `Interface/MainMenu/*`, `LegendUI/main_menu.pdream` |
| `CNewUIDeathJournalWindow` | Death / PvP journal | `Interface/DeathJournal/*` |
| `CNewUIPartySearch`, `CNewUIPartySearchSettings` | Party finder | `Interface/PartySearch/*` |
| `CNewUIGuildOracleWindow` | Guild oracle | `Interface/GuildOracle/*` |
| `GuildManager` | Guild manager / ranking / shrine | `Interface/GuildManager/*` |
| `Collections`, `CollectionsAddItem` | Item collection sets | `Interface/Collections/`, `Local/xml/Collections*.xml` |
| `Dungeon`, `DungeonDamageStatistic`, `DungeonTimer` | Custom dungeon + DPS meter + timer | `Interface/Atlas/Dungeon/`, `DamageStatistic/` |
| `CharacterOverview` | Character overview panel | `Interface/CharacterOverview/` |
| `HarmonyMix` | Harmony mix UI | `Interface/HarmonyMix/`, GFx DDS |
| `GuestAccess` | Guest / visitor access UI | `Interface/GuestAccess/` |
| `CNewUIGameMasterMenu`, `GameMasterBan` | GM menu / ban system | `Interface/GameMaster/BanSystem/` |
| `LostedWordsEvent` | “Losted Words” event | `Interface/Atlas/LostedWords/` |
| `PreviewWindow` | Item/preview window | `Interface/Atlas/PreviewWindow/` |
| `CNotifications`, `NewUINotificationInform` | Notification toast/mail | `Interface/Notification/`, `GFx/Notifications/` |
| `CNewUIInGameShop` | Cash shop | `Interface/InGameShop/`, `Data/InGameShop*` |
| Macro UI (path strings) | Macro / auto gauges | `Interface/MacroUI/*` |
| Item skinning (paths) | Item skinning UI | `Interface/Atlas/ItemSkinning/` |

Also present: RmlUi (`Rml::EventListener`) — suggests some HTML/CSS-like UI paths alongside classic NewUI.

### Config-driven systems (little/no binary RE needed)

| Config | What it defines |
|--------|-----------------|
| `Data/Local/xml/SoulSystem.xml` | Enable flag, reset price (DC + Jewel), 4 tabs × 4 elements × 3 sub-tiers, OptionIds, image paths |
| `Data/Local/xml/QuestSystem/QuestSystemText.xml` | Large quest catalog (titles + multi-line descriptions) |
| `Data/Local/xml/Collections.xml` + `CollectionsRequirements.xml` | Collection recipes, zen prices, option bonuses |
| `Data/Local/xml/BossHealthBar.xml` | Boss monster IDs → bar type/pages |
| `Data/Local/xml/NpcIconRenderer.xml` | NPC Id → floating icon asset |
| `Data/Local/xml/ChatCommandHint.xml` | Client chat command help (`/post`, `/str`, `/limit`, …) |
| `Data/Local/xml/MixExtension.xml` | Extended mix NPC recipes (socket seeds, etc.) |
| `Data/Local/CustomMonsters.xml` | Custom monster definitions |
| Atlas `*.json` | Sprite atlas coordinates for Soul/Quest/Dungeon/etc. |

---

## 3. Asset ↔ system mapping

| Interface folder / atlas | Feature | MuMain / OpenMU status |
|--------------------------|---------|------------------------|
| `EventList/` | Event schedule window (icons for BC/DS/CC/IT/Moss/Gaion/BattleRoyale/…) | **Ported** as `NewUIEventScheduleWindow` + OpenMU `0xFA` |
| `ActiveInvasion/` | Live invasion HUD | **Ported** as `NewUIInvasionStatusWindow` + `FA 02/03` |
| `Atlas/SoulSystem/` + `SoulSystem.json` | Soul system layout + art | **Ported** (`NewUISoulSystemWindow` cites Mudream JSON) + OpenMU `0xFE` |
| `Atlas/QuestSystem/` + `QuestInfo/` | Custom quest UI / mission / NPC | **Partial** — `NewUIQuestPanelWindow` + OpenMU `0xFB`; Mudream has richer MissionView/NPC dialog art |
| `Atlas/Dungeon/` | Dungeon window | **Partial** — MuMain dungeon window/HUD + `FA 10–15` |
| `DamageStatistic/` | Dungeon DPS board | Mudream-only class; MuMain has related dungeon damage hooks — polish TBD |
| `LegendUI/` | Boss bar, main menu art, chat hints, reconnect, exit menu, arena timer | Split across MuMain HUD/dialogs — **asset port opportunity** |
| `LegendHUD/` | Adv dungeon info / HUD chrome | Align with MuMain dungeon HUD |
| `MainMenu/` | Full main-menu panels | MuMain has menus; Mudream art is larger custom set |
| `DeathJournal/` | Death log UI | **Not ported** (high value QoL) |
| `PartySearch/` | Party finder | **Not ported** |
| `GuildOracle/` + `GuildManager/` | Guild systems | **Not ported** (beyond stock guild) |
| `Collections/` | Collection book | **Partial** — `NewUICollectionWindow` exists |
| `MacroUI/` | Macro gauges/inputs | **Not ported** |
| `CharacterOverview/` | Overview | **Not ported** |
| `HarmonyMix/` | Harmony mix | Stock harmony exists; Mudream UI wrapper TBD |
| `GuestAccess/` | Guest access | **Not ported** |
| `GameMaster/` | GM tools | Server-admin only; low port priority for players |
| `NaviMap/` | Minimap | Classic + custom markers |
| `NpcIcons/` | Floating NPC icons | Driven by `NpcIconRenderer.xml` |
| `Notification/` + `GFx/Notifications/` | Toasts | Partial / TBD |
| `InGameShop/` | Cash shop | Stock IGS path; content differs |
| `bottom_panel/`, `new_main_frame_window/` | HUD chrome | MuMain main frame / Pegasus HUD |
| `HealthBar/`, `SkillStack/`, `PartCharge1/` | Combat HUD bits | Stock + custom |
| `Atlas/LostedWords/`, `ItemSkinning/`, `PreviewWindow/` | Extra events / cosmetics | **Not ported** |

Catalog sheet counts (from `_catalog/catalog_report.json`): EventList 23, ActiveInvasion 7, LegendUI 12, LegendHUD 34, QuestInfo 16, DeathJournal 22, MacroUI 15, GuildManager 20, GuildOracle 12, GFx 121, NaviMap 106, Atlas 97, root ~658 decrypted legacy assets.

---

## 4. Protocol / string clues

### Already implemented in MuMain + OpenMU (use as source of truth)

Documented in `MuMain/.../WSclient.cpp` and `OpenMU/.../*Packets.cs`:

| Head | Sub | Direction | Feature |
|------|-----|-----------|---------|
| **0xFA** | 00 / 01 | C→S / S→C | Event schedule request / list |
| **0xFA** | 02 / 03 | C→S / S→C | Invasion status |
| **0xFA** | 04 / 05 | C→S / S→C | Player equipment preview |
| **0xFA** | 06 | S→C | Boss life bar |
| **0xFA** | 10–15 | both | Dungeon window / enter / HUD / leave |
| **0xFB** | 00,02,04,05,07… | both | Quest panel (status/claim/accept/abandon/NPC list) |
| **0xFC** | * | both | Jewel Bank |
| **0xFE** | 00/01/02/03/04 | both | Soul System (status / set / reset) |
| **0xFD** | * | — | Item post (MuMain) |
| **0xEE** | * | — | VIP shop (MuMain) |

Soul C→S (from MuMain):

- `C1 04 FE 00` — request status  
- `C1 07 FE 02 tab col value` — set allocation  
- `C1 04 FE 04` — reset  

Event schedule C→S: `C1 04 FA 00`.

### Binary string search for literal `0xFA` / `0xFB`

Opcode **literals as text** are not reliably present in `MU.exe` (compiled as bytes). Protocol certainty for Mudream’s *exact* live opcodes still needs either:

1. Matching against your already-working OpenMU handlers (if clients are protocol-compatible), or  
2. Controlled packet capture on your own server while exercising Mudream UI (authorized private-server work).

RTTI + asset paths strongly imply Mudream’s feature set matches what you already modeled under FA/FB/FE — treat your OpenMU packet docs as the porting contract unless capture shows divergence.

### Notable path strings inside `MU.exe`

Examples (decoded names; on disk often `.pdream`):

- `Interface\EventList\Background.png`, `BloodCastle.png`, `DevilSquare.png`, …  
- `Interface\ActiveInvasion\background_new.png`  
- `Interface\Atlas\SoulSystem\`, `QuestSystem\`, `Dungeon\`  
- `Interface\DeathJournal\Background.png` …  
- `Interface\LegendUI\boss_health_bar.png`, `main_menu.png`, `chat_command_hint.png`  
- `Data\Local\xml\SoulSystem.xml`, `Collections.xml`, `QuestSystem\QuestSystemText.xml`

---

## 5. Cross-reference: MuMain / OpenMU already porting

| Feature | MuMain | OpenMU |
|---------|--------|--------|
| Event schedule | `NewUIEventScheduleWindow` | `EventSchedulePackets` (`0xFA`) |
| Invasion status | `NewUIInvasionStatusWindow` | same FA family |
| Quest panel | `NewUIQuestPanelWindow` | `QuestPanelPackets` (`0xFB`) |
| Soul system | `NewUISoulSystemWindow` (layout from Mudream JSON) | `SoulSystemPackets` (`0xFE`) |
| Collections | `NewUICollectionWindow` | (logic TBD / partial) |
| Dungeon | `NewUIDungeonWindow`, dungeon HUD | FA dungeon subcodes |
| Boss life bar | `NewUIBossLifeBar` | FA 06 + `BossHealthBar.xml` concept |
| Jewel bank / VIP / item post | respective NewUI windows | `0xFC` / `0xEE` / `0xFD` |

**Gap vs Mudream:** DeathJournal, PartySearch, GuildOracle/Manager, MacroUI, CharacterOverview, LostedWords, ItemSkinning, GuestAccess, richer Quest MissionView art, EventList visual polish (event-type icons), MainMenu art pack.

---

## 6. Recommended porting priority

### P0 — Finish / harden what you already started

1. **Soul System** — align OpenMU bonuses with `SoulSystem.xml` OptionIds; keep UI on decrypted Atlas art.  
2. **Quest Panel** — import quest text/metadata from `QuestSystemText.xml`; improve MissionView using `QuestInfo/` + Atlas QuestSystem JSON.  
3. **Event schedule + Invasion** — skin with `EventList/` + `ActiveInvasion/` decrypted assets; keep FA protocol.  
4. **Boss health bar** — drive monster list from `BossHealthBar.xml`.

### P1 — High player-facing value, assets+XML rich

5. **Collections** — complete server rules from `Collections.xml` / requirements; polish `NewUICollectionWindow`.  
6. **Dungeon + DamageStatistic** — finish dungeon loop + optional DPS board UI from `DamageStatistic/`.  
7. **DeathJournal** — new MuMain window + OpenMU death-log packets (needs protocol design or capture).  
8. **PartySearch** — finder UI from assets; new opcodes or reuse party packets with extensions.

### P2 — Social / guild / QoL

9. **GuildOracle / GuildManager** — after stock guild is solid.  
10. **MainMenu / LegendUI chrome** — visual parity without new server features.  
11. **NpcIconRenderer** — floating icons from XML (mostly client).  
12. **ChatCommandHint** — client-only help overlay.

### P3 — Niche / admin / cosmetics

13. MacroUI, CharacterOverview, LostedWords, ItemSkinning, GuestAccess, GameMaster BanSystem, HarmonyMix skin, PreviewWindow.

---

## 7. What still needs deeper RE vs assets + reimplementation

### Can do **without** deep binary RE

- Soul/Collections/Quest **data models** (XML already complete).  
- UI **layout** from Atlas `*.json` + decrypted PNG catalogs.  
- Boss bar monster list, NPC icons, chat hints, mix extension recipes.  
- Visual porting of EventList / ActiveInvasion / LegendUI / MainMenu.  
- Continuing OpenMU handlers for FA/FB/FE as already designed.

### Needs **deeper RE or authorized packet capture**

- Exact Mudream opcodes for **DeathJournal, PartySearch, GuildOracle, Macro, LostedWords, ItemSkinning, GuestAccess** (no XML protocol docs found).  
- Whether Mudream’s live FA/FB/FE payloads match your OpenMU structs byte-for-byte.  
- RmlUi document sources (if any packed UI markup beyond NewUI).  
- `.pdream` format internals (you already have a decrypt/export pipeline — prefer that over re-deriving).  
- Anti-tamper / BugSplat / launcher auth — **out of scope** (do not attack).

### Practical next analysis steps (still in allowed scope)

1. Diff decrypted EventList/Soul/Quest art into MuMain `Interface/` load paths.  
2. Parse `SoulSystem.xml` / `Collections.xml` into OpenMU config initializers.  
3. On your private server: log C→S headcodes when opening Mudream EventList / DeathJournal / PartySearch (capture only your traffic).  
4. Optional: IDA/Ghidra on `Reborn/MU.exe` focused on `CNewUIEventListWindow` / `SoulSystem` vtables — for layout/protocol confirmation only.

---

## 8. Helper scripts in this folder

| File | Use |
|------|-----|
| `extract_strings.ps1` | ASCII + UTF-16LE keyword string dump from binaries |
| `extract_strings.py` | Same logic in Python (if `python` available) |
| `strings_MU.exe_Reborn.txt` | 813 keyword hits from primary client |
| `strings_MU.exe_x64.txt` | 425 hits from slim build |
| `strings_summary.json` | Scan summary |
| `inventory_client.ps1` | Re-run binary/Data inventory |
| `REPORT.md` | This document |

Re-run string extraction:

```powershell
.\extract_strings.ps1 -Binaries "..\..\..\Mudream.online\Reborn\MU.exe" -OutDir .
```

---

## 9. Bottom line

Mudream’s custom systems are **first-class C++ NewUI classes inside `Reborn/MU.exe`**, backed by **plaintext Local XML** and **Interface atlases** (encrypted on disk as `*.pdream`/`*.tdream`, already exportable). Your MuMain/OpenMU stack already covers the **highest-value protocols (FA / FB / FE)**. The best ROI is: **finish Soul/Quest/Events with Mudream assets+XML**, then port **DeathJournal / PartySearch / Guild** after designing or capturing their packets — not by attacking DRM or producing exploits.
