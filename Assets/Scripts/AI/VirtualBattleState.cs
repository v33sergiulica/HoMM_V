using System.Collections.Generic;
using UnityEngine;

namespace HommClone.AI
{
    /// <summary>
    /// Represents a lightweight virtual stack snapshot for AI simulation.
    /// </summary>
    public class VirtualStackState
    {
        public int id;
        public string name;
        public int playerIndex;
        public Vector2Int gridPosition;
        public int count;
        public int currentHealth;
        public int currentAmmo;
        public int maxHealth;
        public int attack;
        public int defense;
        public int speed;
        public float initiative;
        public int minDamage;
        public int maxDamage;
        public bool isRanged;
        public bool isFlying;
        public bool isLarge;
        public bool hasNoRangePenalty;
        public int aiValuePerUnit;

        public bool IsDead => count <= 0;

        public List<Vector2Int> GetOccupiedTiles()
        {
            List<Vector2Int> tiles = new List<Vector2Int> { gridPosition };
            if (isLarge)
            {
                tiles.Add(new Vector2Int(gridPosition.x + 1, gridPosition.y));
                tiles.Add(new Vector2Int(gridPosition.x, gridPosition.y + 1));
                tiles.Add(new Vector2Int(gridPosition.x + 1, gridPosition.y + 1));
            }
            return tiles;
        }

        public bool OccupiesTile(Vector2Int pos)
        {
            if (gridPosition == pos) return true;
            if (isLarge)
            {
                if (pos.x == gridPosition.x + 1 && pos.y == gridPosition.y) return true;
                if (pos.x == gridPosition.x && pos.y == gridPosition.y + 1) return true;
                if (pos.x == gridPosition.x + 1 && pos.y == gridPosition.y + 1) return true;
            }
            return false;
        }

        public VirtualStackState Clone()
        {
            return new VirtualStackState
            {
                id = this.id,
                name = this.name,
                playerIndex = this.playerIndex,
                gridPosition = this.gridPosition,
                count = this.count,
                currentHealth = this.currentHealth,
                currentAmmo = this.currentAmmo,
                maxHealth = this.maxHealth,
                attack = this.attack,
                defense = this.defense,
                speed = this.speed,
                initiative = this.initiative,
                minDamage = this.minDamage,
                maxDamage = this.maxDamage,
                isRanged = this.isRanged,
                isFlying = this.isFlying,
                isLarge = this.isLarge,
                hasNoRangePenalty = this.hasNoRangePenalty,
                aiValuePerUnit = this.aiValuePerUnit
            };
        }

        public int CalculateRawDamage()
        {
            float avgDmg = (minDamage + maxDamage) / 2f;
            return Mathf.Max(1, Mathf.RoundToInt(avgDmg * count));
        }

        public int CalculateRealDamage(int rawDamage, int opponentDefense)
        {
            float y = (opponentDefense - attack) / 100f;
            float multiplier = 1f;

            int tempRange = (int)(y * 100);
            switch (tempRange)
            {
                case < -120: multiplier = 4.4f - y / 2f; break;
                case < -100: multiplier = 3.8f - y; break;
                case < -60: multiplier = 1.8f - 3f * y; break;
                case < -20: multiplier = 1.2f - 4f * y; break;
                case < 0: multiplier = 1f - 5f * y; break;
                case < 20: multiplier = 1f - 2f * y; break;
                case < 40: multiplier = 0.9f - 1.5f * y; break;
                case < 60: multiplier = 0.4f - y / 4f; break;
                case < 85: multiplier = 0.37f - y / 5f; break;
                default: multiplier = 0.17f / y; break;
            }

            return Mathf.Max(1, (int)(rawDamage * multiplier));
        }

        public void TakeDamage(int damageDealt)
        {
            if (damageDealt <= 0 || count <= 0) return;
            int totalHealthBefore = (count - 1) * maxHealth + currentHealth;

            if (damageDealt >= totalHealthBefore)
            {
                count = 0;
                currentHealth = 0;
                return;
            }

            int troopsTaken = damageDealt / maxHealth;
            int remainingHealthDamage = damageDealt % maxHealth;

            count -= troopsTaken;
            if (currentHealth > remainingHealthDamage)
            {
                currentHealth -= remainingHealthDamage;
            }
            else
            {
                count -= 1;
                currentHealth = currentHealth + maxHealth - remainingHealthDamage;
            }

            if (count <= 0)
            {
                count = 0;
                currentHealth = 0;
            }
        }
    }

    /// <summary>
    /// Represents a dynamic virtual battlefield state used for Minimax simulation.
    /// </summary>
    public class VirtualBattleState
    {
        public List<VirtualStackState> stacks = new List<VirtualStackState>();
        public int gridWidth = 10;
        public int gridHeight = 12;

        public VirtualBattleState Clone()
        {
            VirtualBattleState clone = new VirtualBattleState
            {
                gridWidth = this.gridWidth,
                gridHeight = this.gridHeight
            };
            foreach (var s in this.stacks)
            {
                clone.stacks.Add(s.Clone());
            }
            return clone;
        }

        public VirtualStackState GetStackAt(Vector2Int pos)
        {
            foreach (var s in stacks)
            {
                if (s != null && !s.IsDead && s.OccupiesTile(pos))
                {
                    return s;
                }
            }
            return null;
        }

        public VirtualStackState GetStackById(int id)
        {
            foreach (var s in stacks)
            {
                if (s != null && s.id == id) return s;
            }
            return null;
        }

        /// <summary>
        /// Validates whether a stack (1x1 or 2x2 footprint) can legally occupy an origin tile on the grid.
        /// </summary>
        public bool CanOccupy(VirtualStackState stack, Vector2Int origin)
        {
            if (stack == null) return false;
            List<Vector2Int> tiles = new List<Vector2Int> { origin };
            if (stack.isLarge)
            {
                tiles.Add(new Vector2Int(origin.x + 1, origin.y));
                tiles.Add(new Vector2Int(origin.x, origin.y + 1));
                tiles.Add(new Vector2Int(origin.x + 1, origin.y + 1));
            }

            foreach (var t in tiles)
            {
                if (t.x < 0 || t.x >= gridWidth || t.y < 0 || t.y >= gridHeight)
                    return false;

                var occupant = GetStackAt(t);
                if (occupant != null && occupant.id != stack.id)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Pure C# BFS calculating all reachable grid positions for a virtual stack.
        /// Thread-safe for async Minimax background execution.
        /// </summary>
        public Dictionary<Vector2Int, int> GetReachableTiles(VirtualStackState stack)
        {
            Dictionary<Vector2Int, int> reachable = new Dictionary<Vector2Int, int>();
            if (stack == null || stack.IsDead) return reachable;

            int maxDist = stack.speed;

            if (stack.isFlying)
            {
                // Flying unit logic: Manhattan distance
                for (int x = 0; x < gridWidth; x++)
                {
                    for (int y = 0; y < gridHeight; y++)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (pos == stack.gridPosition) continue;
                        int dist = Mathf.Abs(pos.x - stack.gridPosition.x) + Mathf.Abs(pos.y - stack.gridPosition.y);
                        if (dist <= maxDist && CanOccupy(stack, pos))
                        {
                            reachable[pos] = dist;
                        }
                    }
                }
            }
            else
            {
                // Ground unit logic: 4 cardinal directions only
                Queue<Vector2Int> queue = new Queue<Vector2Int>();
                queue.Enqueue(stack.gridPosition);
                reachable[stack.gridPosition] = 0;

                Vector2Int[] cardinalDirs = new Vector2Int[]
                {
                    new Vector2Int(1, 0),
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(0, -1)
                };

                while (queue.Count > 0)
                {
                    Vector2Int current = queue.Dequeue();
                    int currentCost = reachable[current];

                    if (currentCost >= maxDist) continue;

                    foreach (var dir in cardinalDirs)
                    {
                        Vector2Int nextPos = current + dir;

                        if (nextPos.x >= 0 && nextPos.x < gridWidth && nextPos.y >= 0 && nextPos.y < gridHeight)
                        {
                            if (CanOccupy(stack, nextPos))
                            {
                                int newCost = currentCost + 1;
                                if (!reachable.ContainsKey(nextPos) || newCost < reachable[nextPos])
                                {
                                    reachable[nextPos] = newCost;
                                    queue.Enqueue(nextPos);
                                }
                            }
                        }
                    }
                }
            }

            return reachable;
        }

        /// <summary>
        /// Heuristic evaluation function for the state:
        /// Normalized around 100 (AIPower / EnemyPower * 100) + Proportional KillShot & Shooter Block Bonuses.
        /// </summary>
        public float Evaluate(int aiPlayerIndex, MinimaxSettings settings = null)
        {
            float aiPower = 0f;
            float enemyPower = 0f;

            float killShotWeight = settings != null ? settings.weightKillShot : 25f; // % bonus of killed stack power
            float blockShootersWeight = settings != null ? settings.weightBlockShooters : 15f;

            foreach (var s in stacks)
            {
                if (s == null) continue;

                if (s.IsDead)
                {
                    // Proportional KillShot bonus: adds percentage of destroyed enemy stack's power
                    if (s.playerIndex != aiPlayerIndex)
                    {
                        aiPower += s.aiValuePerUnit * (killShotWeight * 0.01f);
                    }
                    continue;
                }

                float power = s.count * s.aiValuePerUnit;
                if (s.playerIndex == aiPlayerIndex)
                {
                    aiPower += power;

                    // Positional heuristic bonus for blocking enemy shooters vs danger zone penalty
                    if (!s.isRanged)
                    {
                        foreach (var other in stacks)
                        {
                            if (other != null && !other.IsDead && other.playerIndex != aiPlayerIndex && other.isRanged)
                            {
                                bool isAdjacent = false;
                                foreach (var posA in s.GetOccupiedTiles())
                                {
                                    foreach (var posB in other.GetOccupiedTiles())
                                    {
                                        if (Mathf.Abs(posA.x - posB.x) <= 1 && Mathf.Abs(posA.y - posB.y) <= 1)
                                        {
                                            isAdjacent = true;
                                            break;
                                        }
                                    }
                                    if (isAdjacent) break;
                                }

                                if (isAdjacent)
                                {
                                    // Bonus scaled by neutralizing the ENEMY SHOOTER's power!
                                    float shooterPower = other.count * other.aiValuePerUnit;
                                    aiPower += shooterPower * (blockShootersWeight * 0.01f);
                                }
                                else
                                {
                                    // Shooter Full-Damage Danger Zone Penalty (distance <= 5 tiles)
                                    int dist = Mathf.Max(Mathf.Abs(s.gridPosition.x - other.gridPosition.x), Mathf.Abs(s.gridPosition.y - other.gridPosition.y));
                                    if (dist <= 5)
                                    {
                                        aiPower -= power * 0.05f; // Penalty for standing in full-damage ranged line without blocking
                                    }
                                }
                            }
                        }
                    }

                    // Forward Progress Tie-Breaker Bonus: Small reward for advancing closer to enemy army
                    float minDistToEnemy = float.MaxValue;
                    foreach (var enemy in stacks)
                    {
                        if (enemy != null && !enemy.IsDead && enemy.playerIndex != aiPlayerIndex)
                        {
                            float d = Vector2Int.Distance(s.gridPosition, enemy.gridPosition);
                            if (d < minDistToEnemy) minDistToEnemy = d;
                        }
                    }
                    if (minDistToEnemy < float.MaxValue)
                    {
                        aiPower += Mathf.Max(0f, 15f - minDistToEnemy) * 2f;
                    }
                }
                else
                {
                    enemyPower += power;
                }
            }

            // 100-based Power Ratio Normalization (100 = equal power, 150 = 50% AI advantage, 75 = 25% AI disadvantage)
            float ratioScore = (aiPower / Mathf.Max(1f, enemyPower)) * 100f;
            return ratioScore;
        }

        /// <summary>
        /// Calculates all tiles threatened by opponent melee units on their next turn.
        /// </summary>
        public HashSet<Vector2Int> CalculateEnemyThreatMap(int aiPlayerIndex)
        {
            HashSet<Vector2Int> threatMap = new HashSet<Vector2Int>();
            foreach (var enemy in stacks)
            {
                if (enemy == null || enemy.IsDead || enemy.playerIndex == aiPlayerIndex) continue;

                // Expand reach around enemy based on their speed
                int reach = enemy.speed + 1;
                for (int x = enemy.gridPosition.x - reach; x <= enemy.gridPosition.x + reach; x++)
                {
                    for (int y = enemy.gridPosition.y - reach; y <= enemy.gridPosition.y + reach; y++)
                    {
                        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                        {
                            threatMap.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }
            return threatMap;
        }

        public void SimulateMeleeAttack(VirtualStackState attacker, VirtualStackState target)
        {
            if (attacker == null || target == null || attacker.IsDead || target.IsDead) return;

            int rawDamage = attacker.CalculateRawDamage();
            if (attacker.isRanged)
            {
                rawDamage = Mathf.Max(1, rawDamage / 2); // Melee penalty for shooters
            }

            int realDamage = attacker.CalculateRealDamage(rawDamage, target.defense);
            target.TakeDamage(realDamage);

            // Retaliation check
            if (!target.IsDead)
            {
                int retalRaw = target.CalculateRawDamage();
                int retalReal = target.CalculateRealDamage(retalRaw, attacker.defense);
                attacker.TakeDamage(retalReal);
            }
        }

        public void SimulateRangedAttack(VirtualStackState attacker, VirtualStackState target)
        {
            if (attacker == null || target == null || attacker.IsDead || target.IsDead || attacker.currentAmmo <= 0) return;

            attacker.currentAmmo--;

            int rawDamage = attacker.CalculateRawDamage();
            int distance = Mathf.Max(Mathf.Abs(attacker.gridPosition.x - target.gridPosition.x), Mathf.Abs(attacker.gridPosition.y - target.gridPosition.y));
            if (distance > 6 && !attacker.hasNoRangePenalty)
            {
                rawDamage = Mathf.Max(1, rawDamage / 2);
            }

            int realDamage = attacker.CalculateRealDamage(rawDamage, target.defense);
            target.TakeDamage(realDamage);
        }
    }
}
