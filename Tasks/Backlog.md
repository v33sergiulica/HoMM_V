# Project Backlog

A list of planned features, improvements, and ideas for the Heroes of Might & Magic V combat system clone.

## Epic 1: Square Grid & Positioning
- [x] Square grid generation (configurable width/height).
- [x] Obstacle generation (impassable squares).
- [x] Large unit placement (2x2 squares occupying multiple spaces).
- [x] Pathfinding system (A* or Breadth-First Search supporting obstacles).

## Epic 2: Creature Stacks & Core Stats
- [x] CreatureData scriptable objects defining unit configurations.
- [x] CreatureStack instances managing count, active HP, and positions.
- [x] Damage calculation formulas (Attack vs Defense, luck, morale).
- [x] Visual stack representation (3D model or placeholder sprite with stack size label).

## Epic 3: Turn & Initiative Timeline
- [x] Initiative-based turn calculation.
- [x] Dynamic timeline UI displaying upcoming turns.
- [x] "Wait" command (re-inserting stack later in the initiative round).
- [x] "Defend" command (ending turn with defensive multipliers).

## Epic 4: Combat Actions & Retaliation
- [x] Melee movement & attack action flow.
- [x] Counterattack (Retaliation) mechanic (once per round restriction).
- [x] Ranged attack action flow with penalties (obstacles, distance, adjacent threats).
- [x] Flying units movement (ignoring intermediate obstacles).

## Epic 5: Buffs, Debuffs & Spellcasting
- [x] Buff/debuff manager tracking duration and effects.
- [x] Spellcasting system for creatures (mana/spell list).
- [x] Hero participation (sideline casting, active abilities, passive buffs).
- [x] Special abilities (e.g. CasterAbility, FlyingAbility, NoRangePenaltyAbility, LargeCreatureAbility).

## Epic 6: Iconic Creature Abilities (Planned)
- [ ] **Life Drain** (Vampires: heal/resurrect stack models proportional to damage dealt).
- [ ] **Double Strike** (Paladins/Wolves: execute two consecutive attacks in a single turn).
- [ ] **No Retaliation** (Rascals/Hydras: target cannot perform counterattacks).
- [ ] **Unlimited Retaliation** (Griffins: retaliate against every melee attack received per round).
- [ ] **Sweep / Breath Attack** (Hydras/Dragons: hit multiple adjacent tiles or 2-tile linear breath cone).
- [ ] **Gating / Summoning** (Demons: summon reinforcement stacks onto adjacent vacant tiles).
- [ ] **Fear / Terrify** (Dragons/Mummies: chance to force target to skip action or flee).

## Epic 7: AoE Spells & Dynamic Battlefield (Planned)
- [ ] **Area-of-Effect Spells** (Fireball, Meteor Shower, Chain Lightning with target shape overlays).
- [ ] **Morale & Luck Events** (Turn skipping on Bad Morale, double turn on High Morale, Critical Strikes on Luck).
- [ ] **Battlefield Obstacles & Hazards** (Destructible barricades, quagmires, and damage traps).

## Epic 8: Hero Customization & Artifacts (Planned)
- [ ] **Hero Inventory & Artifacts** (Equipable items adding global stats to army).
- [ ] **Command Auras** (Hero proximity buffs boosting nearby stacks' morale and defense).

## Epic 9: AI Heuristic Normalization & Tactical Scaling
- [x] **100-Based Power Ratio Normalization**: Normalize army power evaluation around 100 ($\frac{\text{AIPower}}{\text{EnemyPower} + 1} \times 100$).
- [x] **Proportional KillShot Scaling**: Scale `KillShot` bonus proportionally to the power of the wiped-out stack ($25\%$ of destroyed stack's power), prioritizing Dragon kills over Skeleton kills.
- [x] **High-Offense Target Priority**: Add target priority weighting for glass-cannon / high-attack units to eliminate big threats early.
- [ ] **Playtesting Weight Balancing**: Refine weights iteratively based on match playtesting feedback.
