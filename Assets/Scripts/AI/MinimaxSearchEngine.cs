using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace HommClone.AI
{
    public enum MinimaxActionType
    {
        RangedAttack,
        MeleeAttack,
        MoveOnly,
        Defend
    }

    public class MinimaxAction
    {
        public MinimaxActionType actionType;
        public int activeStackId;
        public int targetStackId;
        public Vector2Int destinationTile;
        public float score;
        public int searchDepthReached;
    }

    public class MinimaxSettings
    {
        public float maxThinkingTimeSeconds = 2.0f;
        public float directAttackBonus = 15f; // 15 points bonus on 100-scale
        public float weightKillShot = 25f;    // 25% bonus of killed stack power
        public float weightBlockShooters = 15f; // 15% power boost for blocking shooters
        public float weightRetaliationPenalty = -5f;
        public bool allowKitingWhenAttackAvailable = false;
    }

    /// <summary>
    /// Search engine implementing Iterative Deepening Minimax with Alpha-Beta pruning,
    /// time-budget controls, and tactical Danger Zone candidate tile selection.
    /// </summary>
    public static class MinimaxSearchEngine
    {
        public static MinimaxAction FindBestAction(VirtualBattleState initialState, int activeStackId, MinimaxSettings settings, Grid.GridManager gridManager)
        {
            if (settings == null) settings = new MinimaxSettings();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            long timeBudgetMs = (long)(settings.maxThinkingTimeSeconds * 1000f);
            VirtualStackState activeStack = initialState.GetStackById(activeStackId);
            if (activeStack == null || activeStack.IsDead) return null;

            MinimaxAction bestOverallAction = null;
            int maxDepthCompleted = 0;

            // Iterative Deepening: Search 1-ply, 2-ply, 3-ply... up to 6-ply within time budget
            for (int depth = 1; depth <= 6; depth++)
            {
                if (stopwatch.ElapsedMilliseconds >= timeBudgetMs) break;

                MinimaxAction depthAction = SearchDepth(initialState, activeStackId, depth, float.NegativeInfinity, float.PositiveInfinity, true, activeStack.playerIndex, stopwatch, timeBudgetMs, gridManager, settings);
                
                // If search was interrupted midway due to time limit, drop incomplete depth result
                if (stopwatch.ElapsedMilliseconds < timeBudgetMs && depthAction != null)
                {
                    bestOverallAction = depthAction;
                    maxDepthCompleted = depth;
                }
            }

            stopwatch.Stop();

            // Detailed Debug Logging of Top Candidate Actions with exact Minimax scores
            List<MinimaxAction> topCandidates = GenerateCandidateActions(initialState, activeStack, gridManager, settings);
            string debugMsg = $"<b>[Minimax AI Engine]</b> Unit: <b>{activeStack.name}</b> | Depth Reached: <b>{maxDepthCompleted}</b> ({stopwatch.ElapsedMilliseconds} ms)\n";
            debugMsg += "<b>Evaluated Candidate Actions:</b>\n";

            foreach (var cand in topCandidates)
            {
                VirtualBattleState simState = initialState.Clone();
                ApplyActionToVirtualState(simState, cand);

                // Run SearchDepth for this candidate to get its true tree score
                VirtualStackState nextUnit = FindNextParticipant(simState, activeStackId, activeStack.playerIndex, false);
                float evalScore;
                if (nextUnit != null && maxDepthCompleted > 1)
                {
                    MinimaxAction res = SearchDepth(simState, nextUnit.id, maxDepthCompleted - 1, float.NegativeInfinity, float.PositiveInfinity, false, activeStack.playerIndex, stopwatch, timeBudgetMs, gridManager, settings);
                    evalScore = res != null ? res.score : simState.Evaluate(activeStack.playerIndex, settings);
                }
                else
                {
                    evalScore = simState.Evaluate(activeStack.playerIndex, settings);
                }

                evalScore += CalculateActionBonus(initialState, cand, settings);

                string targetName = "None";
                if (cand.targetStackId != 0)
                {
                    var tStack = initialState.GetStackById(cand.targetStackId);
                    if (tStack != null) targetName = tStack.name;
                }

                string isChosen = (bestOverallAction != null && cand.actionType == bestOverallAction.actionType && cand.destinationTile == bestOverallAction.destinationTile && cand.targetStackId == bestOverallAction.targetStackId) ? " <color=yellow>★ SELECTED ★</color>" : "";

                debugMsg += $"- <b>{cand.actionType}</b> | Target: {targetName} | Tile: {cand.destinationTile} | Score: <b>{evalScore:F1}</b>{isChosen}\n";
            }

            UnityEngine.Debug.Log(debugMsg);

            if (bestOverallAction != null)
            {
                bestOverallAction.searchDepthReached = maxDepthCompleted;
            }
            else
            {
                // Fallback to simple immediate candidate action if depth 1 was cut short
                if (topCandidates.Count > 0)
                {
                    bestOverallAction = topCandidates[0];
                    bestOverallAction.searchDepthReached = 1;
                }
            }

            return bestOverallAction;
        }

        private static MinimaxAction SearchDepth(VirtualBattleState state, int activeStackId, int depth, float alpha, float beta, bool isMaximizing, int aiPlayerIndex, Stopwatch stopwatch, long timeBudgetMs, Grid.GridManager gridManager, MinimaxSettings settings)
        {
            if (stopwatch.ElapsedMilliseconds >= timeBudgetMs || depth == 0)
            {
                float eval = state.Evaluate(aiPlayerIndex, settings);
                return new MinimaxAction { score = eval };
            }

            VirtualStackState activeStack = state.GetStackById(activeStackId);
            if (activeStack == null || activeStack.IsDead)
            {
                float eval = state.Evaluate(aiPlayerIndex, settings);
                return new MinimaxAction { score = eval };
            }

            List<MinimaxAction> candidates = GenerateCandidateActions(state, activeStack, gridManager, settings);
            if (candidates.Count == 0)
            {
                // Defend fallback
                return new MinimaxAction { actionType = MinimaxActionType.Defend, activeStackId = activeStackId, score = state.Evaluate(aiPlayerIndex, settings) };
            }

            MinimaxAction bestAction = null;

            if (isMaximizing)
            {
                float maxEval = float.NegativeInfinity;
                foreach (var action in candidates)
                {
                    if (stopwatch.ElapsedMilliseconds >= timeBudgetMs) break;

                    VirtualBattleState nextState = state.Clone();
                    ApplyActionToVirtualState(nextState, action);

                    // Find next active unit (opponent turn for MIN node)
                    VirtualStackState nextUnit = FindNextParticipant(nextState, activeStackId, aiPlayerIndex, false);
                    float eval;

                    if (nextUnit != null)
                    {
                        MinimaxAction res = SearchDepth(nextState, nextUnit.id, depth - 1, alpha, beta, false, aiPlayerIndex, stopwatch, timeBudgetMs, gridManager, settings);
                        eval = res != null ? res.score : nextState.Evaluate(aiPlayerIndex, settings);
                    }
                    else
                    {
                        eval = nextState.Evaluate(aiPlayerIndex, settings);
                    }

                    eval += CalculateActionBonus(state, action, settings);

                    action.score = eval;
                    if (eval > maxEval)
                    {
                        maxEval = eval;
                        bestAction = action;
                    }

                    alpha = Mathf.Max(alpha, eval);
                    if (beta <= alpha) break; // Alpha-Beta Cutoff
                }

                if (bestAction != null) bestAction.score = maxEval;
                return bestAction;
            }
            else
            {
                float minEval = float.PositiveInfinity;
                foreach (var action in candidates)
                {
                    if (stopwatch.ElapsedMilliseconds >= timeBudgetMs) break;

                    VirtualBattleState nextState = state.Clone();
                    ApplyActionToVirtualState(nextState, action);

                    // Find next AI unit for MAX node
                    VirtualStackState nextUnit = FindNextParticipant(nextState, activeStackId, aiPlayerIndex, true);
                    float eval;

                    if (nextUnit != null)
                    {
                        MinimaxAction res = SearchDepth(nextState, nextUnit.id, depth - 1, alpha, beta, true, aiPlayerIndex, stopwatch, timeBudgetMs, gridManager, settings);
                        eval = res != null ? res.score : nextState.Evaluate(aiPlayerIndex, settings);
                    }
                    else
                    {
                        eval = nextState.Evaluate(aiPlayerIndex, settings);
                    }

                    action.score = eval;
                    if (eval < minEval)
                    {
                        minEval = eval;
                        bestAction = action;
                    }

                    beta = Mathf.Min(beta, eval);
                    if (beta <= alpha) break; // Alpha-Beta Cutoff
                }

                if (bestAction != null) bestAction.score = minEval;
                return bestAction;
            }
        }

        private static float CalculateActionBonus(VirtualBattleState state, MinimaxAction action, MinimaxSettings settings)
        {
            if (action == null || settings == null) return 0f;
            float bonus = 0f;
            if (action.actionType == MinimaxActionType.MeleeAttack || action.actionType == MinimaxActionType.RangedAttack)
            {
                bonus += settings.directAttackBonus;
                VirtualStackState targetEnemy = state.GetStackById(action.targetStackId);
                if (targetEnemy != null)
                {
                    bonus += Mathf.Min(10f, targetEnemy.attack * 0.5f);
                    if (targetEnemy.isRanged)
                    {
                        bonus += settings.weightBlockShooters;
                    }
                }
            }
            return bonus;
        }

        private static bool IsAdjacentFootprint(VirtualStackState attacker, Vector2Int attackerOrigin, VirtualStackState target)
        {
            if (attacker == null || target == null) return false;
            List<Vector2Int> attackerTiles = new List<Vector2Int> { attackerOrigin };
            if (attacker.isLarge)
            {
                attackerTiles.Add(new Vector2Int(attackerOrigin.x + 1, attackerOrigin.y));
                attackerTiles.Add(new Vector2Int(attackerOrigin.x, attackerOrigin.y + 1));
                attackerTiles.Add(new Vector2Int(attackerOrigin.x + 1, attackerOrigin.y + 1));
            }

            foreach (var posA in attackerTiles)
            {
                foreach (var posB in target.GetOccupiedTiles())
                {
                    if (Mathf.Abs(posA.x - posB.x) <= 1 && Mathf.Abs(posA.y - posB.y) <= 1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static void ApplyActionToVirtualState(VirtualBattleState state, MinimaxAction action)
        {
            VirtualStackState active = state.GetStackById(action.activeStackId);
            if (active == null || active.IsDead) return;

            if (action.actionType == MinimaxActionType.RangedAttack)
            {
                VirtualStackState target = state.GetStackById(action.targetStackId);
                if (target != null && !target.IsDead)
                {
                    state.SimulateRangedAttack(active, target);
                }
            }
            else if (action.actionType == MinimaxActionType.MeleeAttack)
            {
                VirtualStackState target = state.GetStackById(action.targetStackId);
                // Move first to destination tile if different
                active.gridPosition = action.destinationTile;
                if (target != null && !target.IsDead)
                {
                    state.SimulateMeleeAttack(active, target);
                }
            }
            else if (action.actionType == MinimaxActionType.MoveOnly)
            {
                active.gridPosition = action.destinationTile;
            }
        }

        private static VirtualStackState FindNextParticipant(VirtualBattleState state, int currentUnitId, int aiPlayerIndex, bool wantAI)
        {
            int count = state.stacks.Count;
            if (count == 0) return null;

            int currentIndex = state.stacks.FindIndex(s => s.id == currentUnitId);
            if (currentIndex < 0) currentIndex = 0;

            for (int i = 1; i <= count; i++)
            {
                int idx = (currentIndex + i) % count;
                var s = state.stacks[idx];
                if (s != null && !s.IsDead)
                {
                    if (wantAI && s.playerIndex == aiPlayerIndex) return s;
                    if (!wantAI && s.playerIndex != aiPlayerIndex) return s;
                }
            }
            return null;
        }

        private static bool IsBlocked(VirtualBattleState state, VirtualStackState shooter)
        {
            int enemyPlayerIndex = (shooter.playerIndex == 1) ? 2 : 1;
            foreach (var pos in shooter.GetOccupiedTiles())
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        Vector2Int neighbor = new Vector2Int(pos.x + dx, pos.y + dy);
                        VirtualStackState occ = state.GetStackAt(neighbor);
                        if (occ != null && !occ.IsDead && occ.playerIndex == enemyPlayerIndex)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Generates pruned candidate actions including Ranged, Melee, Defend, and Kiting / Safe Approach tiles.
        /// </summary>
        private static List<MinimaxAction> GenerateCandidateActions(VirtualBattleState state, VirtualStackState activeStack, Grid.GridManager gridManager, MinimaxSettings settings)
        {
            List<MinimaxAction> candidates = new List<MinimaxAction>();
            if (activeStack == null || activeStack.IsDead) return candidates;

            int enemyPlayerIndex = (activeStack.playerIndex == 1) ? 2 : 1;

            // Query actual grid reachability (handles obstacles, unit speed, and 2x2 large footprints)
            Dictionary<Vector2Int, List<Vector2Int>> reachableTiles = null;
            if (gridManager != null)
            {
                reachableTiles = gridManager.GetReachableTiles(activeStack.gridPosition, activeStack.speed, activeStack.isFlying, activeStack.isLarge);
            }

            // 1. Ranged Attack Options (if shooter, has ammo, and NOT blocked by adjacent enemies)
            if (activeStack.isRanged && activeStack.currentAmmo > 0 && !IsBlocked(state, activeStack))
            {
                foreach (var enemy in state.stacks)
                {
                    if (enemy != null && !enemy.IsDead && enemy.playerIndex == enemyPlayerIndex)
                    {
                        candidates.Add(new MinimaxAction
                        {
                            actionType = MinimaxActionType.RangedAttack,
                            activeStackId = activeStack.id,
                            targetStackId = enemy.id,
                            destinationTile = activeStack.gridPosition
                        });
                    }
                }
            }

            // 2. Melee Attack Options (ONLY from tiles that are actually REACHABLE this turn or current standing tile)
            int minOffset = activeStack.isLarge ? -2 : -1;
            int maxOffset = 1;

            foreach (var enemy in state.stacks)
            {
                if (enemy == null || enemy.IsDead || enemy.playerIndex != enemyPlayerIndex) continue;

                // Find adjacent attack-from positions for 1x1 or 2x2 footprints
                foreach (var tTile in enemy.GetOccupiedTiles())
                {
                    for (int dx = minOffset; dx <= maxOffset; dx++)
                    {
                        for (int dy = minOffset; dy <= maxOffset; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            Vector2Int attackFrom = new Vector2Int(tTile.x + dx, tTile.y + dy);

                            if (attackFrom.x >= 0 && attackFrom.x < state.gridWidth && attackFrom.y >= 0 && attackFrom.y < state.gridHeight)
                            {
                                // CRITICAL SAFETY CHECK: Tile MUST be reachable this turn OR be our current position
                                bool isReachable = (attackFrom == activeStack.gridPosition) || (reachableTiles != null && reachableTiles.ContainsKey(attackFrom));
                                if (!isReachable) continue;

                                bool canOccupy = state.CanOccupy(activeStack, attackFrom);
                                bool isAdjacent = IsAdjacentFootprint(activeStack, attackFrom, enemy);

                                if (canOccupy && isAdjacent)
                                {
                                    // Avoid adding duplicate actions for same target and origin
                                    if (!candidates.Exists(c => c.actionType == MinimaxActionType.MeleeAttack && c.targetStackId == enemy.id && c.destinationTile == attackFrom))
                                    {
                                        candidates.Add(new MinimaxAction
                                        {
                                            actionType = MinimaxActionType.MeleeAttack,
                                            activeStackId = activeStack.id,
                                            targetStackId = enemy.id,
                                            destinationTile = attackFrom
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 3. Safe Approach / Kiting Tiles (ONLY if no direct attack available, OR allowKitingWhenAttackAvailable is true)
            bool hasAttackAvailable = candidates.Exists(c => c.actionType == MinimaxActionType.MeleeAttack || c.actionType == MinimaxActionType.RangedAttack);
            if (!hasAttackAvailable || (settings != null && settings.allowKitingWhenAttackAvailable))
            {
                HashSet<Vector2Int> threatMap = state.CalculateEnemyThreatMap(activeStack.playerIndex);

                VirtualStackState targetEnemy = null;
                float minDist = float.MaxValue;
                foreach (var enemy in state.stacks)
                {
                    if (enemy != null && !enemy.IsDead && enemy.playerIndex == enemyPlayerIndex)
                    {
                        float dist = Vector2Int.Distance(activeStack.gridPosition, enemy.gridPosition);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            targetEnemy = enemy;
                        }
                    }
                }

                if (targetEnemy != null && reachableTiles != null)
                {
                    foreach (var candidateTile in reachableTiles.Keys)
                    {
                        bool canOccupy = state.CanOccupy(activeStack, candidateTile);
                        if (canOccupy && !threatMap.Contains(candidateTile))
                        {
                            if (Vector2Int.Distance(candidateTile, targetEnemy.gridPosition) < minDist)
                            {
                                candidates.Add(new MinimaxAction
                                {
                                    actionType = MinimaxActionType.MoveOnly,
                                    activeStackId = activeStack.id,
                                    destinationTile = candidateTile
                                });
                            }
                        }
                    }
                }
            }

            // 4. Defend Action Option
            candidates.Add(new MinimaxAction
            {
                actionType = MinimaxActionType.Defend,
                activeStackId = activeStack.id,
                destinationTile = activeStack.gridPosition
            });

            return candidates;
        }
    }
}
