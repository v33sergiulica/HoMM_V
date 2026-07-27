# Project Overview - Heroes of Might & Magic V Combat System Clone

## Objectives
The goal of this project is to recreate the rich tactical turn-based combat system inspired by **Heroes of Might & Magic V: Tribes of the East** in Unity.

---

## General Gameplay (Scope Focus)
The project focuses exclusively on **Tactical Battles**:
* When combat starts, the game switches to a separate turn-based battlefield (square grid).
* The battle is resolved when one army has no surviving creature stacks.

---

## Core Systems & Mechanics

### 1. Army Structure (Creature Stacks)
An army is composed of **creature stacks** (e.g., 56 Archers, 14 Griffins).
* Only one model represents the stack on the field, with a number showing the remaining count.
* **Stack Stats**: Current/Max Count, Creature Max HP, Injured Creature HP, Attack, Defense, Initiative, Speed, Damage Range, Special Abilities, Ammo/Mana.
* **Damage Handling**: Damage reduces individual creature HP. When HP falls below 0, creature count decreases, carrying over excess damage to the next creature in the stack.

### 2. Square Grid Battlefield
* Fixed-size square grid.
* Ground units navigate obstacles/other units using pathfinding.
* Flying units ignore most movement obstacles.
* Large units can occupy multiple squares (e.g., 2x2 squares).

### 3. Initiative Timeline & Turn Order
* Combat is fully turn-based, but stacks act *independently* based on their individual **Initiative** stat (no alternating team turns).
* The dynamic initiative timeline is always visible to the player.
* Stacks can choose to **Wait** (delay action to later in the current round) or **Defend** (end turn early for a defensive bonus).

### 4. Actions
* **Move** (up to Speed in squares).
* **Melee Attack** (against adjacent target).
* **Ranged Attack** (from a distance, penalties apply for obstacles, long range, or adjacent threats).
* **Wait** & **Defend**.
* **Special Abilities / Spells** (flying, life drain, double attack, unlimited retaliation, spellcasting, etc.).

### 5. Retaliation
* Melee units counterattack after being attacked.
* Standard limit of one retaliation per round (unless modified by abilities).

### 6. Hero Participation
* Heroes do not physically stand on the grid.
* Support from the sideline: spellcasting, active/racial skills, passive stats boosts.

### 7. Damage Resolution
* Calculated using: Attacker Attack, Defender Defense, Damage Range, Stack Count, Buffs/Debuffs, Hero Bonuses, Luck, Morale.
* Buffs (increased stats, speed, initiative, magic immunity) and Debuffs (slow, poison, blind, reduced initiative/morale) modify actions dynamically.
