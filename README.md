# ⚔️ Heroes of Might & Magic 3D - Tactical RPG & Strategy Engine

A feature-rich 3D Tactical Turn-Based RPG & Strategy Engine built in **Unity (C#)** inspired by classic *Heroes of Might and Magic (HOMM V)* mechanics.

Features a dual-layer gameplay loop: a **World Map Exploration & Economy Layer** and a **Grid-Based Tactical Combat System** powered by a custom **Minimax AI** and **Time-To-Act (ATB) Initiative Engine**.

---

## 🌟 Key Technical Highlights & Features

### ⚔️ 1. Tactical Grid Combat Engine
- **Time-To-Act (ATB) Timeline System**: Non-linear turn initiative queue (`ITimelineParticipant`) driven by creature speed and hero stats. Supports wait actions, morale rolls (Good/Bad Morale), and luck modifiers.
- **Unit Abilities & Mechanics**:
  - **Caster Ability**: Mana-based tactical spellcasting for support/offensive units.
  - **Flying Ability**: Unrestricted grid obstacle traversal.
  - **Large Creature Ability**: Multi-tile occupancy and spatial collision math.
  - **No Range Penalty Ability**: Ranged unit damage falloff overrides.
- **Hero Sideline Integration**: 3D sideline hero model rendering with custom prefab support. Direct Hero Strikes, Spellbook integration (Expert mastery scaling), and dynamic Attack/Defense stat transmission to unit stacks.

### 🧠 2. AI Decision Engine
- **Minimax Search Engine**: Heuristic-driven AI decision-making for opponent army stacks and enemy heroes (`BattleAIManager`).
- **Optimal Target Selection**: Threat evaluation, range calculations, retaliations management, and spell efficiency scoring.

### 🗺️ 3. World Map Exploration & Economy
- **A* Pathfinding System**: Smooth grid movement with terrain traversal costs and Movement Point (MP) limits.
- **Resource & Mine Economy**: Conquerable daily income mines (Gold Mine, Sawmill, Ore Pit, Crystal Cavern) and consumable resource piles.
- **Monster-Guarded Nodes**: Dynamic proximity-based node protection. Attempting to claim mines or pick up resources adjacent to wild monster stacks immediately triggers tactical combat encounters.
- **Day Skip / Turn Cycle**: Daily income generation, movement point replenishment, and calendar tracking (Day, Week, Month).

### 🎵 4. Audio Engine & Dynamic UI
- **Audio Manager System**: Singleton BGM crossfading playlists (World Map vs. Combat), spatial SFX, and win/loss music transitions.
- **Rich UI & Modal Windows**:
  - Top Resource HUD (`ResourceBarUI`) showing daily rates (+X/day).
  - Monster Inspection Modal (`MonsterInspectionUI`) triggered via Right-Click.
  - Hero Character Sheet (`HeroCharacterSheetUI`) with live stat readouts.
  - Floating combat text for damage, spell effects, and morale triggers.

---

## 🛠️ Architecture & Tech Stack

- **Engine**: Unity (C#)
- **Design Patterns**: Singleton, Observer (Action/Events), Strategy Pattern (Unit Abilities), Interface Segregation (`ITimelineParticipant`), Scriptable Objects (Data-Driven Architecture).
- **Pathfinding**: Custom A* (World Map) & BFS Reachability (Combat Grid).
- **UI**: TextMeshPro, Canvas Overlays, Responsive Dynamic Layouts.

---

## 🚀 How to Run
1. Clone this repository: `git clone https://github.com/v33sergiulica/HoMM_V.git`
2. Open the project in **Unity**.
3. Load and play from `Assets/Scenes/WorldMapScene.unity` or `BattleScene.unity`.
