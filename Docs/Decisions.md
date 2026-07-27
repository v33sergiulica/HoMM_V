# Architectural & Technical Decisions

This document records the major design and technical decisions made during the development of this project, along with their context and justifications.

---

## Template

### [DEC-XXX] [Decision Title]
* **Date**: YYYY-MM-DD
* **Status**: Proposed / Accepted / Rejected / Deprecated
* **Context**: What problem are we trying to solve? What are the constraints?
* **Decision**: What did we decide to do?
* **Consequences**: What are the trade-offs, benefits, and drawbacks of this choice?

---

## Decision Log

### [DEC-001] Square Grid Implementation & Code Layout
* **Date**: 2026-07-06
* **Status**: Accepted
* **Context**: Replicating the tactical battlefield in a clean, maintainable structure while switching from a hexagonal battlefield to a square tile grid battlefield.
* **Decision**: 
  1. Set up scripts under folders segregated by interest (e.g., `Assets/Scripts/Grid/` for grid components).
  2. Implement `Tile.cs` caching `Vector2Int` positions where $X \rightarrow$ World $X$ and $Y \rightarrow$ World $Z$.
  3. Implement dynamic runtime battlefield instantiation in `GridManager.cs` to prevent manual scene setup for 120 tiles.
  4. Use standard 8-way movement capability (GetNeighbours and Chebyshev distance calculations) to reflect standard square-grid tactical game mechanics.
* **Consequences**: Easy to maintain grid parameters (width, height, prefab references) and a clean scene hierarchy with procedural instantiation under a single root transform. Ready for BFS/A* pathfinding integrations.

### [DEC-002] ScriptableObject-Driven Creature Stack Data Architecture
* **Date**: 2026-07-06
* **Status**: Accepted
* **Context**: Defining creature stats and active battlefield stacks in a modular way, avoiding a hierarchy explosion of separate script files for each unit type (e.g. Peasent.cs, Swordsman.cs).
* **Decision**: 
  1. Use `CreatureData.cs` inheriting from `ScriptableObject` to represent static blueprints for each unit type (HP, Min/Max Damage, Initiative, Speed, Ranged/Flying flags).
  2. Implement `CreatureStack.cs` as a single generic `MonoBehaviour` attached to spawned unit prefabs to manage dynamic battlefield state (current stack size, remaining HP of the top unit, current mana/ammo, grid coordinate position).
  3. Replicate the user's custom piecewise formula mapping $(D - A)$ differences from $-100$ to $+100$ for attack/defense scaling directly in the generic damage calculation method.
* **Consequences**: Adding new unit types requires zero new code—only creating new ScriptableObject assets in the editor. Easy integration of buffs/debuffs later, as they can modify the dynamic values of `CreatureStack` directly.
### [DEC-003] Time-To-Act (TTA) Tick-Based Initiative Turn Engine
* **Date**: 2026-07-06
* **Status**: Accepted
* **Context**: Avoiding complicated round-based loops and tie-breaker code (using negative ATB flags) when sorting stack actions based on Initiative.
* **Decision**: 
  1. Implement a Time-To-Act (TTA) engine in `TurnManager.cs` under a new `Assets/Scripts/Turns/` directory.
  2. Compute exact continuous tick times to determine the next stack to act: $\text{Ticks} = (100 - \text{ATB}) / \text{Initiative}$.
  3. Advance the engine by adding $\text{Initiative} \times \text{minTicks}$ to all stacks, clamping the active stack's ATB to exactly $100$.
  4. Implement Wait (reset ATB to 50) and Defend (reset ATB to 0 and apply temporary defense multiplier, which is automatically cleared at the start of its next turn).
  5. Enable a clone-based lookahead simulator (`PredictFutureTimeline`) that predicts the upcoming timeline queue for display in the UI.
* **Consequences**: Deterministic, drift-free initiative timelines. Extremely easy to implement percentage delays or speed boosts (simply add/subtract ATB). Future lookahead queue predictions can be retrieved in a single function call, keeping UI updates separated from gameplay state updates.
