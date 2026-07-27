using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HommClone.Grid;
using HommClone.Creatures;
using HommClone.Turns;
using HommClone.Interaction;

namespace HommClone.AI
{
    /// <summary>
    /// Evaluates and executes tactical combat decisions for Player 2's stacks
    /// using a customizable utility-based scoring model.
    /// </summary>
    public class BattleAIManager : MonoBehaviour
    {
        [Header("AI State")]
        [SerializeField] private bool aiEnabled = true;
        [SerializeField] private bool useMinimax = true;
        [SerializeField] private float maxThinkingTimeSeconds = 2.0f;
        [SerializeField] private float directAttackBonus = 15f;
        [SerializeField] private bool allowKitingWhenAttackAvailable = false;
        public bool AIEnabled => aiEnabled;

        [Header("Utility Score Balancing Weights (100-Scale)")]
        [SerializeField] private float baseAttackUtility = 100f;
        [SerializeField] private float weightDealtDamageMultiplier = 1.0f;
        [SerializeField] private float weightRetaliationPenalty = -5.0f;
        [SerializeField] private float weightKillShot = 25.0f;
        [SerializeField] private float weightBlockShooters = 15.0f;

        private TurnManager _turnManager;
        private BattleInteractionManager _interactionManager;
        private GridManager _gridManager;
        private Coroutine _aiTurnCoroutine;

        private enum AIActionType
        {
            RangedAttack,
            MeleeAttack,
            MoveOnly,
            Defend
        }

        private class AICandidateAction
        {
            public AIActionType actionType;
            public CreatureStack target;
            public Vector2Int moveTile;
            public float score;
        }

        private void Awake()
        {
            _turnManager = FindFirstObjectByType<TurnManager>();
            _interactionManager = FindFirstObjectByType<BattleInteractionManager>();
            _gridManager = FindFirstObjectByType<GridManager>();
        }

        private void OnEnable()
        {
            if (_turnManager != null)
            {
                _turnManager.OnTurnChanged += HandleTurnChanged;
            }
        }

        private void OnDisable()
        {
            if (_turnManager != null)
            {
                _turnManager.OnTurnChanged -= HandleTurnChanged;
            }
        }

        private void HandleTurnChanged()
        {
            if (!aiEnabled) return;
            if (_turnManager == null || _turnManager.ActiveUnit == null) return;

            // If it is Player 2's turn, trigger the AI decision loop after a delay
            if (_turnManager.ActiveUnit.PlayerIndex == 2 && !_turnManager.ActiveUnit.IsDead)
            {
                if (_aiTurnCoroutine != null) StopCoroutine(_aiTurnCoroutine);
                _aiTurnCoroutine = StartCoroutine(PlayAITurnCoroutine());
            }
        }

        private IEnumerator PlayAITurnCoroutine()
        {
            // Lock interaction manager so the player cannot click during AI thinking/moving
            if (_interactionManager != null)
            {
                _interactionManager.IsBusy = true;
            }

            // Wait 1.0 second to simulate natural decision-making and show the state
            yield return new WaitForSeconds(1.0f);

            ITimelineParticipant activeUnit = _turnManager.ActiveUnit;
            if (activeUnit == null || activeUnit.IsDead || activeUnit.PlayerIndex != 2)
            {
                if (_interactionManager != null) _interactionManager.IsBusy = false;
                yield break;
            }

            if (activeUnit is Heroes.Hero hero)
            {
                Debug.Log($"[AI Hero] Thinking for active Hero: {hero.Name} (Player 2)...");
                // Select a random enemy troop stack to strike directly
                var targets = _turnManager.ActiveStacks.Where(s => s.PlayerIndex == 1 && !s.IsDead).ToList();
                if (targets.Count > 0)
                {
                    CreatureStack target = targets[Random.Range(0, targets.Count)];
                    yield return StartCoroutine(hero.DirectAttackCoroutine(target, () =>
                    {
                        if (_interactionManager != null) _interactionManager.IsBusy = false;
                        _turnManager.ExecuteAction();
                    }));
                }
                else
                {
                    if (_interactionManager != null) _interactionManager.IsBusy = false;
                    _turnManager.ExecuteAction();
                }
                yield break;
            }

            CreatureStack activeStack = activeUnit as CreatureStack;
            if (activeStack == null || activeStack.IsDead || activeStack.PlayerIndex != 2)
            {
                if (_interactionManager != null) _interactionManager.IsBusy = false;
                yield break;
            }

            Debug.Log($"[AI] Thinking for active stack: {activeStack.gameObject.name} (Player 2)...");

            // 1. Gather all active enemies (Player 1)
            List<CreatureStack> enemies = new List<CreatureStack>();
            foreach (var stack in _turnManager.ActiveStacks)
            {
                if (stack != null && !stack.IsDead && stack.PlayerIndex == 1)
                {
                    enemies.Add(stack);
                }
            }

            if (enemies.Count == 0)
            {
                Debug.Log("[AI] No active enemies found.");
                if (_interactionManager != null) _interactionManager.IsBusy = false;
                _turnManager.ExecuteAction();
                yield break;
            }

            // --- MINIMAX SEARCH ENGINE INTEGRATION (ASYNC NON-BLOCKING THREAD) ---
            if (useMinimax)
            {
                VirtualBattleState initialState = BuildVirtualBattleState();
                MinimaxSettings settings = new MinimaxSettings
                {
                    maxThinkingTimeSeconds = this.maxThinkingTimeSeconds,
                    directAttackBonus = this.directAttackBonus,
                    weightKillShot = this.weightKillShot,
                    weightBlockShooters = this.weightBlockShooters,
                    weightRetaliationPenalty = this.weightRetaliationPenalty,
                    allowKitingWhenAttackAvailable = this.allowKitingWhenAttackAvailable
                };

                MinimaxAction bestMinimaxAction = null;
                bool isMinimaxCompleted = false;
                int activeStackId = activeStack.GetInstanceID();

                // Launch Minimax calculation asynchronously on background thread to keep UI & Camera 60 FPS responsive!
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        bestMinimaxAction = MinimaxSearchEngine.FindBestAction(initialState, activeStackId, settings, null);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[AI Minimax Async Exception] {ex.Message}");
                    }
                    finally
                    {
                        isMinimaxCompleted = true;
                    }
                });

                // Yield control to main Unity loop so player can pan camera, click UI & interact smoothly
                while (!isMinimaxCompleted)
                {
                    yield return null;
                }

                if (bestMinimaxAction != null)
                {
                    yield return StartCoroutine(ExecuteMinimaxActionCoroutine(activeStack, bestMinimaxAction));
                    yield break;
                }
            }

            // 2. Evaluate all candidate actions
            List<AICandidateAction> candidates = new List<AICandidateAction>();

            // Option A: Ranged Attack (if can shoot)
            if (activeStack.CanShoot())
            {
                foreach (var enemy in enemies)
                {
                    float score = EvaluateRangedScore(activeStack, enemy);
                    candidates.Add(new AICandidateAction
                    {
                        actionType = AIActionType.RangedAttack,
                        target = enemy,
                        score = score
                    });
                }
            }

            // Option B: Melee Attack (from reachable adjacent tiles)
            var reachable = _gridManager.GetReachableTiles(
                activeStack.GridPosition, 
                activeStack.Speed, 
                activeStack.Data.IsFlying,
                activeStack.IsLarge
            );
            
            // Include current position in reachable tiles
            if (!reachable.ContainsKey(activeStack.GridPosition))
            {
                reachable[activeStack.GridPosition] = new List<Vector2Int>();
            }

            foreach (var enemy in enemies)
            {
                List<Tile> neighbours = _gridManager.GetNeighbours(enemy.GridPosition);
                foreach (var neighbour in neighbours)
                {
                    Vector2Int nPos = neighbour.GridPosition;
                    
                    if (reachable.ContainsKey(nPos))
                    {
                        // Ensure tile is vacant (or we are already standing on it)
                        CreatureStack occupancy = _gridManager.GetCreatureAt(nPos);
                        if (occupancy != null && occupancy != activeStack)
                        {
                            continue; // blocked by another stack
                        }

                        float score = EvaluateMeleeScore(activeStack, enemy, nPos);
                        candidates.Add(new AICandidateAction
                        {
                            actionType = AIActionType.MeleeAttack,
                            target = enemy,
                            moveTile = nPos,
                            score = score
                        });
                    }
                }
            }

            // Option C: Pure Move (if we need to advance)
            foreach (var rPos in reachable.Keys)
            {
                CreatureStack occupancy = _gridManager.GetCreatureAt(rPos);
                if (occupancy != null && occupancy != activeStack)
                {
                    continue;
                }

                float score = EvaluateMoveOnlyScore(activeStack, rPos, enemies);
                candidates.Add(new AICandidateAction
                {
                    actionType = AIActionType.MoveOnly,
                    moveTile = rPos,
                    score = score
                });
            }

            // Option D: Defend (always a backup)
            candidates.Add(new AICandidateAction
            {
                actionType = AIActionType.Defend,
                score = 15f // Baseline utility
            });

            // 3. Select action with highest utility score
            AICandidateAction bestAction = null;
            float maxScore = float.MinValue;
            foreach (var cand in candidates)
            {
                if (cand.score > maxScore)
                {
                    maxScore = cand.score;
                    bestAction = cand;
                }
            }

            // 4. Execute Chosen Action
            if (bestAction != null)
            {
                Debug.Log($"[AI] Chosen action: {bestAction.actionType} (Utility Score: {bestAction.score:F1}) against {(bestAction.target != null ? bestAction.target.gameObject.name : "None")}");
                
                switch (bestAction.actionType)
                {
                    case AIActionType.RangedAttack:
                        yield return StartCoroutine(activeStack.RangedAttackCoroutine(bestAction.target, () =>
                        {
                            if (_interactionManager != null) _interactionManager.IsBusy = false;
                            _turnManager.ExecuteAction();
                        }));
                        break;

                    case AIActionType.MeleeAttack:
                        List<Vector2Int> path = reachable[bestAction.moveTile];
                        if (path.Count > 0)
                        {
                            yield return StartCoroutine(activeStack.MoveAlongPathCoroutine(path, () =>
                            {
                                StartCoroutine(activeStack.MeleeAttackCoroutine(bestAction.target, () =>
                                {
                                    if (_interactionManager != null) _interactionManager.IsBusy = false;
                                    _turnManager.ExecuteAction();
                                }));
                            }, bestAction.target));
                        }
                        else
                        {
                            yield return StartCoroutine(activeStack.MeleeAttackCoroutine(bestAction.target, () =>
                            {
                                if (_interactionManager != null) _interactionManager.IsBusy = false;
                                _turnManager.ExecuteAction();
                            }));
                        }
                        break;

                    case AIActionType.MoveOnly:
                        List<Vector2Int> movePath = reachable[bestAction.moveTile];
                        if (movePath.Count > 0)
                        {
                            yield return StartCoroutine(activeStack.MoveAlongPathCoroutine(movePath, () =>
                            {
                                if (_interactionManager != null) _interactionManager.IsBusy = false;
                                _turnManager.ExecuteAction();
                            }));
                        }
                        else
                        {
                            if (_interactionManager != null) _interactionManager.IsBusy = false;
                            _turnManager.ExecuteDefend();
                        }
                        break;

                    case AIActionType.Defend:
                    default:
                        if (_interactionManager != null) _interactionManager.IsBusy = false;
                        _turnManager.ExecuteDefend();
                        break;
                }
            }
            else
            {
                Debug.LogWarning("[AI] Failed to determine best action, defaulting to Defend.");
                if (_interactionManager != null) _interactionManager.IsBusy = false;
                _turnManager.ExecuteDefend();
            }
        }

        private float EvaluateRangedScore(CreatureStack active, CreatureStack enemy)
        {
            active.GetDamageEstimation(enemy, isMelee: false, out int minDmg, out int maxDmg, out int minKills, out int maxKills);
            float avgDmg = (minDmg + maxDmg) / 2f;

            // Large base boost for attacking, and damage scaled by target's design power value
            float score = baseAttackUtility + avgDmg * weightDealtDamageMultiplier * (enemy.Data.AIValue / 100f);

            // Add killshot bonus
            if (minKills >= enemy.Count)
            {
                score += weightKillShot;
            }

            return score;
        }

        private float EvaluateMeleeScore(CreatureStack active, CreatureStack enemy, Vector2Int standPosition)
        {
            active.GetDamageEstimation(enemy, isMelee: true, out int minDmg, out int maxDmg, out int minKills, out int maxKills);
            float avgDmg = (minDmg + maxDmg) / 2f;

            // Ranged units suffer melee penalty
            if (active.Data.IsRanged)
            {
                avgDmg /= 2f;
            }

            // Large base boost for attacking, and damage scaled by target's design power value
            float score = baseAttackUtility + avgDmg * weightDealtDamageMultiplier * (enemy.Data.AIValue / 100f);

            if (minKills >= enemy.Count)
            {
                score += weightKillShot;
            }

            // Retaliation penalty
            if (!enemy.HasRetaliatedThisRound)
            {
                enemy.GetDamageEstimation(active, isMelee: true, out int retMin, out int retMax, out int retKills, out int retMaxKills);
                float avgRetDmg = (retMin + retMax) / 2f;
                
                score += avgRetDmg * weightRetaliationPenalty * (active.Data.AIValue / 100f);

                if (retKills >= active.Count)
                {
                    score -= 200f; // Extremely high penalty if retaliation wipes us
                }
            }

            // Blocking shooters bonus
            if (enemy.Data.IsRanged)
            {
                score += weightBlockShooters;
            }

            return score;
        }

        private float EvaluateMoveOnlyScore(CreatureStack active, Vector2Int tilePos, List<CreatureStack> enemies)
        {
            int minDistance = int.MaxValue;
            CreatureStack targetEnemy = null;

            foreach (var enemy in enemies)
            {
                int dist = GetGridDistance(tilePos, enemy.GridPosition);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    targetEnemy = enemy;
                }
            }

            if (targetEnemy == null) return 0f;

            // Base score for moving starts high (e.g. 80) and decreases slightly with distance,
            // ensuring that moving closer is ALWAYS preferred over Defend (15) even at maximum distance (12).
            float score = 80f - minDistance * 3f;

            // Tiny weight for targeting higher value units
            score += (targetEnemy.Data.AIValue / 500f);

            // Penalty for choosing to stand still if not already in range, encouraging progress
            if (tilePos == active.GridPosition)
            {
                score -= 15f;
            }

            return score;
        }

        private int GetGridDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private VirtualBattleState BuildVirtualBattleState()
        {
            VirtualBattleState vState = new VirtualBattleState();
            if (_gridManager != null)
            {
                vState.gridWidth = _gridManager.Width;
                vState.gridHeight = _gridManager.Height;
            }

            foreach (var stack in _turnManager.ActiveStacks)
            {
                if (stack != null && !stack.IsDead)
                {
                    bool noRangePen = stack.HasAbility<NoRangePenaltyAbility>();
                    vState.stacks.Add(new VirtualStackState
                    {
                        id = stack.GetInstanceID(),
                        name = stack.Name,
                        playerIndex = stack.PlayerIndex,
                        gridPosition = stack.GridPosition,
                        count = stack.Count,
                        currentHealth = stack.CurrentHealth,
                        currentAmmo = stack.CurrentAmmo,
                        maxHealth = stack.Data.MaxHealth,
                        attack = stack.Attack,
                        defense = stack.Defense,
                        speed = stack.Speed,
                        initiative = stack.Initiative,
                        minDamage = stack.Data.MinDamage,
                        maxDamage = stack.Data.MaxDamage,
                        isRanged = stack.Data.IsRanged,
                        isFlying = stack.Data.IsFlying,
                        isLarge = stack.IsLarge,
                        hasNoRangePenalty = noRangePen,
                        aiValuePerUnit = stack.Data.AIValue
                    });
                }
            }
            return vState;
        }

        private IEnumerator ExecuteMinimaxActionCoroutine(CreatureStack activeStack, MinimaxAction action)
        {
            CreatureStack targetStack = null;
            if (action.targetStackId != 0)
            {
                foreach (var s in _turnManager.ActiveStacks)
                {
                    if (s != null && s.GetInstanceID() == action.targetStackId)
                    {
                        targetStack = s;
                        break;
                    }
                }
            }

            if (action.actionType == MinimaxActionType.RangedAttack && targetStack != null && !targetStack.IsDead)
            {
                yield return StartCoroutine(activeStack.RangedAttackCoroutine(targetStack, () =>
                {
                    if (_interactionManager != null) _interactionManager.IsBusy = false;
                    _turnManager.ExecuteAction();
                }));
            }
            else if (action.actionType == MinimaxActionType.MeleeAttack && targetStack != null && !targetStack.IsDead)
            {
                List<Vector2Int> path = new List<Vector2Int>();
                var reachable = _gridManager.GetReachableTiles(activeStack.GridPosition, activeStack.Speed, activeStack.Data.IsFlying, activeStack.IsLarge);
                if (reachable.ContainsKey(action.destinationTile))
                {
                    path = reachable[action.destinationTile];
                }

                if (path.Count > 0)
                {
                    yield return StartCoroutine(activeStack.MoveAlongPathCoroutine(path, () =>
                    {
                        StartCoroutine(activeStack.MeleeAttackCoroutine(targetStack, () =>
                        {
                            if (_interactionManager != null) _interactionManager.IsBusy = false;
                            _turnManager.ExecuteAction();
                        }));
                    }, targetStack));
                }
                else if (action.destinationTile == activeStack.GridPosition || IsAdjacent(activeStack, targetStack))
                {
                    yield return StartCoroutine(activeStack.MeleeAttackCoroutine(targetStack, () =>
                    {
                        if (_interactionManager != null) _interactionManager.IsBusy = false;
                        _turnManager.ExecuteAction();
                    }));
                }
                else
                {
                    Debug.LogWarning($"[AI Safety] Target {targetStack.Name} is not reachable by {activeStack.Name}! Executing Defend fallback.");
                    activeStack.ApplyDefendBonus(3);
                    if (_interactionManager != null) _interactionManager.IsBusy = false;
                    _turnManager.ExecuteAction();
                }
            }
            else if (action.actionType == MinimaxActionType.MoveOnly)
            {
                List<Vector2Int> path = new List<Vector2Int>();
                var reachable = _gridManager.GetReachableTiles(activeStack.GridPosition, activeStack.Speed, activeStack.Data.IsFlying, activeStack.IsLarge);
                if (reachable.ContainsKey(action.destinationTile))
                {
                    path = reachable[action.destinationTile];
                }

                if (path.Count > 0)
                {
                    yield return StartCoroutine(activeStack.MoveAlongPathCoroutine(path, () =>
                    {
                        if (_interactionManager != null) _interactionManager.IsBusy = false;
                        _turnManager.ExecuteAction();
                    }));
                }
                else
                {
                    if (_interactionManager != null) _interactionManager.IsBusy = false;
                    _turnManager.ExecuteAction();
                }
            }
            else // Defend
            {
                activeStack.ApplyDefendBonus(3);
                if (_interactionManager != null) _interactionManager.IsBusy = false;
                _turnManager.ExecuteAction();
            }
        }

        private bool IsAdjacent(CreatureStack a, CreatureStack b)
        {
            if (a == null || b == null) return false;
            foreach (var posA in a.GetOccupiedTiles())
            {
                foreach (var posB in b.GetOccupiedTiles())
                {
                    if (Mathf.Abs(posA.x - posB.x) <= 1 && Mathf.Abs(posA.y - posB.y) <= 1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
