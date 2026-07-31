# Completed Tasks

A chronological archive of completed features and tasks.

## Setup & Infrastructure
- **2026-07-06**: Bootstrapped documentation directory structure (`README.md`, `Docs/`, `Tasks/`).
- **2026-07-06**: Implemented `Tile.cs` and `GridManager.cs` for square grid generation and position caching.
- **2026-07-06**: Implemented `CreatureData.cs` (ScriptableObject blueprint) and `CreatureStack.cs` (generic active unit) to manage combat stats and dynamic stack damage logic.
- **2026-07-06**: Implemented `TurnManager.cs` using the TTA (Time-To-Act) initiative tick algorithm, including Wait/Defend actions and future timeline predictions.
- **2026-07-07**: Implemented BFS pathfinding traversal (ground units cardinal-only, flying units Manhattan bypass) and visual path drawing.
- **2026-07-07**: Implemented mouse raycasting, proximity-based directional attacks (using dot-product sectors to avoid diagonal bias), and coroutine-driven asynchronous retaliation phases.
- **2026-07-07**: Implemented Main Camera Orbit Controller (panning, zoom, orbit drag) and a dynamic HUD Canvas (timeline bar, active unit statuses, action buttons, combined floating combat text).
- **2026-07-07**: Implemented Ranged Shooter mechanics (ammo tracking, adjacent-blocker detection, 50% melee penalty fallback, and no-retaliation shooting sequence).
- **2026-07-07**: Implemented interactive hover damage and casualty estimation tooltip.
- **2026-07-07**: Implemented Casters & Magic System (Spell book UI, Ice Bolt/Slow spells, SpellPower radical scaling, and turn-based debuff ticking).
- **2026-07-14**: Implemented Pre-Battle Setup Lobby & Hidden Turn-Based Deployment Phase (`BattleSetupManager.cs`) supporting Draft vs. Preset modes, PVP vs. Bot AI toggle, and dynamic grid side detection.
- **2026-07-14**: Implemented Large Creature 2x2 System (`LargeCreatureAbility.cs`, multi-tile occupancy in `CreatureStack.cs`, 2x2 BFS pathfinding in `GridManager.cs`, adjacent melee targeting and HoMM5-style 2x2 orange/yellow/green footprint highlights in `BattleInteractionManager.cs` & `TurnManager.cs`).
- **2026-07-14**: Refactored & Balanced Creature `AIValue` Power Rating formula in `CreatureData.cs` with tankiness factor `y` (based on raw attack `rawA`) and offensive factor `x`.
- **2026-07-14**: Resolved URP pipeline shader material rendering and root placeholder mesh toggling issues.
- **2026-07-21**: Implemented Iterative Deepening Minimax AI Engine (`MinimaxSearchEngine.cs`, `VirtualBattleState.cs`, `BattleAIManager.cs`) featuring Alpha-Beta Pruning, 2.0s thinking time budget, and Threat Map Kiting/Safe-Approach tile candidate evaluation.
- **2026-07-31**: Fixed Hotseat PvP Combat Army loading and deployment hero side visibility.
- **2026-07-31**: Implemented 2-Click World Map movement system (zero A* hover lag) with right-click movement cancellation and turn advance protection.
- **2026-07-31**: Added Red Danger Highlights on World Map when hovering over enemy heroes or monster encounter tiles.
- **2026-07-31**: Implemented procedurally rendered **Adventure & Navigation Skill Tree** (`AdventureSkillTree.cs`, `AdventureSkillTreeUI.cs`) based on user Paint design, featuring Logistics I-III, Pathfinding I-II, Scouting (exact vs vague HoMM count descriptors), and Stealth mechanics.
