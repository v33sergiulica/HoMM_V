# Current Sprint

Tasks targeted for completion in the current sprint.

## Active Focus: Grid Pathfinding & Combat Actions
- [x] Create project documentation structure (Docs/ and Tasks/ directories).
- [x] Align with User on the initial implementation task (Square Grid).
- [x] Implement the Tile and GridManager components for 10x12 battlefield generation.
- [x] Implement CreatureData ScriptableObject and CreatureStack dynamic entity scripts.
- [x] Implement Time-To-Act (TTA) Turn Engine and Initiative Timeline predictions.
- [x] Implement grid pathfinding (A* or BFS) and movement action state.
- [x] Implement melee attack and retaliation action loops.
- [x] Implement premium battle HUD UI with dynamic timeline and action buttons.
- [x] Implement floating combat numbers with ease-out sinusoidal animation.
- [x] Implement Shooter / Ranged combat mechanics (ammo tracking, blocker detection, 50% melee penalty).
- [x] Implement interactive hover damage and casualty estimation tooltip.
- [x] Implement Casters & Magic System (Spell book UI, Ice Bolt/Slow spells, SpellPower radical scaling, and turn-based debuff ticking).
- [x] Implement Pre-Battle Draft & Deployment Lobby System (`BattleSetupManager.cs`) with PVP vs. Bot toggle.
- [x] Implement Large Creature (2x2) System with 2x2 BFS pathfinding, HoMM5 footprint highlights, and setup bounds.
- [x] Refactor and balance Creature `AIValue` formula in `CreatureData.cs` (tankiness multiplier `y`, offensive `x`).
- [x] Fix URP magenta shader material assignments and root placeholder mesh toggles.
- [x] Implement Iterative Deepening Minimax AI Engine (`MinimaxSearchEngine.cs`, `VirtualBattleState.cs`) with Alpha-Beta Pruning, 2s time budget, and Threat Map Kiting.



