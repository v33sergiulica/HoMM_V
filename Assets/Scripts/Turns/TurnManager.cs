using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using HommClone.Creatures;
using HommClone.Grid;

namespace HommClone.Turns
{
    /// <summary>
    /// Coordinates battle states, turn transitions, and calculates initiative sequence using the TTA (Time-To-Act) algorithm.
    /// Exposes predictions for the future turn timeline display. Supports both CreatureStack and Hero timeline participants.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        [Header("Battle Setup")]
        [SerializeField] private List<CreatureStack> activeStacks = new List<CreatureStack>();
        [SerializeField] private bool autoStartBattle = true;
        
        [Header("Defend Action Balance")]
        [SerializeField] private int defendBonusAmount = 5;

        private List<ITimelineParticipant> activeParticipants = new List<ITimelineParticipant>();
        private ITimelineParticipant _activeParticipant;

        public ITimelineParticipant ActiveUnit => _activeParticipant;
        
        // Backward compatibility getters
        public CreatureStack ActiveStack => _activeParticipant as CreatureStack;
        public List<CreatureStack> ActiveStacks => activeParticipants.OfType<CreatureStack>().ToList();
        public List<ITimelineParticipant> ActiveParticipants => activeParticipants;

        public event System.Action OnTurnChanged;

        private void Start()
        {
            // 1. Register inspector-assigned stacks
            foreach (var stack in activeStacks)
            {
                if (stack != null && !activeParticipants.Contains(stack))
                {
                    activeParticipants.Add(stack);
                }
            }

            // 2. Auto-gather remaining stacks from scene if empty
            if (activeParticipants.Count == 0)
            {
                var sceneStacks = FindObjectsByType<CreatureStack>(FindObjectsSortMode.None);
                foreach (var stack in sceneStacks)
                {
                    activeParticipants.Add(stack);
                }
            }

            // 3. Discover sideline heroes
            var heroes = FindObjectsByType<Heroes.Hero>(FindObjectsSortMode.None);
            foreach (var hero in heroes)
            {
                if (!activeParticipants.Contains(hero))
                {
                    activeParticipants.Add(hero);
                }
            }

            if (autoStartBattle)
            {
                StartBattle();
            }
        }

        /// <summary>
        /// Clears all registered participants (useful when setting up a custom simulation).
        /// </summary>
        public void ClearParticipants()
        {
            activeParticipants.Clear();
            _activeParticipant = null;
        }

        /// <summary>
        /// Registers any timeline participant (Hero or CreatureStack) dynamically.
        /// </summary>
        public void RegisterParticipant(ITimelineParticipant participant)
        {
            if (participant != null && !activeParticipants.Contains(participant))
            {
                activeParticipants.Add(participant);
            }
        }

        private void Update()
        {
            if (Keyboard.current != null)
            {
                // Press 'T' to simulate a standard Action -> Resets ATB to 0
                if (Keyboard.current.tKey.wasPressedThisFrame)
                {
                    if (_activeParticipant != null)
                    {
                        Debug.Log($"[Test Input] {_activeParticipant.Name} performs Action. Ending turn.");
                        ExecuteAction();
                        PrintTimelinePrediction();
                    }
                }

                // Press 'Y' to simulate a Wait Action -> Resets ATB to 50
                if (Keyboard.current.yKey.wasPressedThisFrame)
                {
                    if (_activeParticipant != null)
                    {
                        Debug.Log($"[Test Input] {_activeParticipant.Name} WAITS. Ending turn.");
                        ExecuteWait();
                        PrintTimelinePrediction();
                    }
                }

                // Press 'U' to simulate a Defend Action -> Resets ATB to 0 & adds defense
                if (Keyboard.current.uKey.wasPressedThisFrame)
                {
                    if (_activeParticipant != null)
                    {
                        Debug.Log($"[Test Input] {_activeParticipant.Name} DEFENDS. Ending turn.");
                        ExecuteDefend();
                        PrintTimelinePrediction();
                    }
                }

                // Press 'P' to highlight reachable tiles for the active stack (if it's a CreatureStack)
                if (Keyboard.current.pKey.wasPressedThisFrame)
                {
                    HighlightActiveReachableTiles();
                }
            }
        }

        private void PrintTimelinePrediction()
        {
            var nextTurns = PredictFutureTimeline(5);
            string predictionStr = string.Join(" -> ", nextTurns.Select(t => t.Name));
            Debug.Log($"[Timeline Prediction] Next 5 turns: {predictionStr}");
        }

        private void HighlightActiveReachableTiles()
        {
            var gridManager = FindFirstObjectByType<Grid.GridManager>();
            CreatureStack activeStack = ActiveStack;
            if (gridManager == null || activeStack == null) return;

            // Reset all tiles first
            Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
            foreach (var tile in allTiles)
            {
                tile.ResetColor();
            }

            // Calculate reachable tiles based on speed and flight capability
            var reachable = gridManager.GetReachableTiles(
                activeStack.GridPosition,
                activeStack.Speed,
                activeStack.Data.IsFlying,
                activeStack.IsLarge
            );

            // Highlight reachable tiles in yellow
            if (activeStack.IsLarge)
            {
                HashSet<Vector2Int> reachableBodyTiles = new HashSet<Vector2Int>();
                foreach (var pos in reachable.Keys)
                {
                    reachableBodyTiles.Add(pos);
                    reachableBodyTiles.Add(new Vector2Int(pos.x + 1, pos.y));
                    reachableBodyTiles.Add(new Vector2Int(pos.x, pos.y + 1));
                    reachableBodyTiles.Add(new Vector2Int(pos.x + 1, pos.y + 1));
                }
                foreach (var pos in reachableBodyTiles)
                {
                    var tile = gridManager.GetTileAt(pos);
                    if (tile != null)
                    {
                        tile.SetColor(new Color(1f, 0.92f, 0.016f, 1f));
                    }
                }
            }
            else
            {
                foreach (var pos in reachable.Keys)
                {
                    var tile = gridManager.GetTileAt(pos);
                    if (tile != null)
                    {
                        tile.SetColor(new Color(1f, 0.92f, 0.016f, 1f));
                    }
                }
            }

            // Highlight currently standing tiles in green
            if (activeStack.IsLarge)
            {
                foreach (var pos in activeStack.GetOccupiedTiles())
                {
                    var activeTile = gridManager.GetTileAt(pos);
                    if (activeTile != null)
                    {
                        activeTile.SetColor(Color.green);
                    }
                }
            }
            else
            {
                var activeTile = gridManager.GetTileAt(activeStack.GridPosition);
                if (activeTile != null)
                {
                    activeTile.SetColor(Color.green);
                }
            }

            Debug.Log($"[Test Highlight] Highlighting {reachable.Count} reachable tiles for {activeStack.gameObject.name} (Speed: {activeStack.Data.Speed}, Flying: {activeStack.Data.IsFlying})");
        }

        /// <summary>
        /// Registers a new stack in the turn engine (useful for dynamically spawned creatures).
        /// </summary>
        public void RegisterStack(CreatureStack stack)
        {
            if (stack != null && !activeParticipants.Contains(stack))
            {
                activeParticipants.Add(stack);
            }
        }

        /// <summary>
        /// Launches the battle, applying passive Hero boosts to troops.
        /// </summary>
        public void StartBattle()
        {
            CleanActiveParticipants();

            if (activeParticipants.Count == 0)
            {
                Debug.LogWarning("[TurnManager] No participants registered for the battle!");
                return;
            }

            // Find heroes to link to friendly troops
            List<Heroes.Hero> heroes = activeParticipants.OfType<Heroes.Hero>().Where(h => h != null).ToList();
            List<CreatureStack> troops = activeParticipants.OfType<CreatureStack>().Where(s => s != null).ToList();
            foreach (var hero in heroes)
            {
                int hOwner = hero.PlayerIndex;

                foreach (var troop in troops)
                {
                    if (troop != null && troop.PlayerIndex == hOwner)
                    {
                        troop.HeroOwner = hero;
                        Debug.Log($"[Hero Boost] Hero {hero.Name} linked to friendly stack {troop.gameObject.name} (Boosts: +{hero.Attack} Atk, +{hero.Defense} Def)");
                    }
                }
            }

            Debug.Log("[TurnManager] Starting combat simulation...");
            AdvanceTimeToNextTurn();
        }

        /// <summary>
        /// Executes the Time-To-Act (TTA) tick calculations to select the next active participant.
        /// </summary>
        public void AdvanceTimeToNextTurn()
        {
            // Reset all tile colors on turn transitions to clean up highlight states
            var gridManager = FindFirstObjectByType<Grid.GridManager>();
            if (gridManager != null)
            {
                Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
                foreach (var tile in allTiles)
                {
                    tile.ResetColor();
                }
            }

            CleanActiveParticipants();

            // Check game over conditions: only check actual troops (ActiveStacks)
            List<CreatureStack> troops = ActiveStacks;
            bool p1Alive = troops.Any(s => s != null && !s.IsDead && s.PlayerIndex == 1);
            bool p2Alive = troops.Any(s => s != null && !s.IsDead && s.PlayerIndex == 2);

            if (!p1Alive || !p2Alive)
            {
                _activeParticipant = null;

                var manager = HommClone.World.GameDataManager.GetOrCreateInstance();
                bool isPvP = manager != null && manager.isPvPBattle;
                int activePlayerIdx = manager != null ? manager.activePlayerIndex : 1;

                string winner;
                if (isPvP)
                {
                    winner = p1Alive ? "Player 1" : (p2Alive ? "Player 2" : "No one");
                }
                else
                {
                    // In PvE combat, Side 1 (p1Alive) is the active player's hero army!
                    winner = p1Alive ? $"Player {activePlayerIdx}" : "Monsters";
                }

                Debug.Log($"[TurnManager] Battle completed! Winner: {winner}");

                if (manager != null)
                {
                    manager.isReturningFromBattle = true;
                    manager.battleWon = p1Alive;
                }

                if (HommClone.Audio.AudioManager.Instance != null)
                {
                    if (p1Alive) HommClone.Audio.AudioManager.Instance.PlayVictorySound();
                    else HommClone.Audio.AudioManager.Instance.PlayDefeatSound();
                }

                var uiManager = FindFirstObjectByType<UI.BattleUIManager>();
                if (uiManager != null)
                {
                    uiManager.ShowVictoryMessage(winner);
                }

                OnTurnChanged?.Invoke();
                return;
            }

            float minTicks = float.MaxValue;
            ITimelineParticipant nextParticipant = null;

            // Step 1: Find who will reach 100 ATB first
            foreach (var participant in activeParticipants)
            {
                if (participant == null || (participant is MonoBehaviour mb && mb == null)) continue;
                float ticksNeeded = (100f - participant.ATB) / participant.Initiative;
                
                if (ticksNeeded < minTicks)
                {
                    minTicks = ticksNeeded;
                    nextParticipant = participant;
                }
                else if (Mathf.Approximately(ticksNeeded, minTicks) && nextParticipant != null)
                {
                    // Tie-breaker 1: Higher base initiative goes first
                    if (participant.Initiative > nextParticipant.Initiative)
                    {
                        nextParticipant = participant;
                    }
                    // Tie-breaker 2: Player 1 (left) goes before Player 2 (right)
                    else if (participant.Initiative == nextParticipant.Initiative && participant.PlayerIndex < nextParticipant.PlayerIndex)
                    {
                        nextParticipant = participant;
                    }
                }
            }

            if (nextParticipant != null)
            {
                // Step 2: Advance everyone's ATB by the elapsed tick amount
                foreach (var participant in activeParticipants)
                {
                    if (participant != null && !(participant is MonoBehaviour mb && mb == null))
                    {
                        participant.ATB += participant.Initiative * minTicks;
                    }
                }

                // Step 3: Clamp the active participant to exactly 100 to prevent float precision drift
                nextParticipant.ATB = 100f;

                // --- BAD MORALE CHECK ---
                if (nextParticipant is CreatureStack activeStack && activeStack.Morale < 0)
                {
                    float badMoraleChance = Mathf.Clamp(-activeStack.Morale * 0.1f, 0f, 1f);
                    if (Random.value < badMoraleChance)
                    {
                        Debug.Log($"[Bad Morale] {activeStack.gameObject.name} got Bad Morale! Turn skipped, ATB set to 50.");
                        activeStack.ATB = 50f;

                        // Spawn UI floating text
                        var uiManager = FindFirstObjectByType<UI.BattleUIManager>();
                        if (uiManager != null)
                        {
                            uiManager.SpawnDamageText(activeStack.transform.position + Vector3.up * 2.2f, "Bad Morale!", new Color(1f, 0.2f, 0.2f));
                        }

                        // Clear defend bonus and tick effects
                        activeStack.OnTurnStart();

                        AdvanceTimeToNextTurn();
                        return;
                    }
                }

                // Set as active participant
                _activeParticipant = nextParticipant;
                Debug.Log($"[TurnManager] Turn starts for: {_activeParticipant.Name} (Player {_activeParticipant.PlayerIndex}) with Initiative {_activeParticipant.Initiative}");

                // Step 4: Tick status effects, clear defend bonuses, and reset retaliation for the stack starting its turn
                _activeParticipant.OnTurnStart();
                OnTurnChanged?.Invoke();
            }
        }

        /// <summary>
        /// Concludes a normal action (Move/Attack/Spellcast). Resets ATB to 0, or 50 if Good Morale rolls.
        /// </summary>
        public void ExecuteAction()
        {
            if (_activeParticipant == null) return;

            if (_activeParticipant is CreatureStack activeStack && activeStack.Morale > 0 && !activeStack.HasHadGoodMoraleExtraTurn)
            {
                float moraleChance = Mathf.Clamp(activeStack.Morale * 0.1f, 0f, 1f);
                if (Random.value < moraleChance)
                {
                    activeStack.HasHadGoodMoraleExtraTurn = true;
                    Debug.Log($"[Good Morale] {activeStack.gameObject.name} got Good Morale! ATB set to 50.");
                    activeStack.ATB = 50f;

                    var uiManager = FindFirstObjectByType<UI.BattleUIManager>();
                    if (uiManager != null)
                    {
                        uiManager.SpawnDamageText(activeStack.transform.position + Vector3.up * 2.2f, "Good Morale!", new Color(0.2f, 1f, 0.2f));
                    }

                    AdvanceTimeToNextTurn();
                    return;
                }
            }

            _activeParticipant.ATB = 0f;
            AdvanceTimeToNextTurn();
        }

        /// <summary>
        /// Concludes a Wait action. Resets ATB to 50.
        /// </summary>
        public void ExecuteWait()
        {
            if (_activeParticipant == null) return;

            _activeParticipant.ATB = 50f;
            AdvanceTimeToNextTurn();
        }

        /// <summary>
        /// Concludes a Defend action. Resets ATB to 0 (or 50 if Good Morale rolls) and adds temporary defense.
        /// </summary>
        public void ExecuteDefend()
        {
            if (_activeParticipant == null) return;

            if (_activeParticipant is CreatureStack stack)
            {
                stack.ApplyDefendBonus(defendBonusAmount);

                if (stack.Morale > 0 && !stack.HasHadGoodMoraleExtraTurn)
                {
                    float moraleChance = Mathf.Clamp(stack.Morale * 0.1f, 0f, 1f);
                    if (Random.value < moraleChance)
                    {
                        stack.HasHadGoodMoraleExtraTurn = true;
                        Debug.Log($"[Good Morale] {stack.gameObject.name} got Good Morale on Defend! ATB set to 50.");
                        stack.ATB = 50f;

                        var uiManager = FindFirstObjectByType<UI.BattleUIManager>();
                        if (uiManager != null)
                        {
                            uiManager.SpawnDamageText(stack.transform.position + Vector3.up * 2.2f, "Good Morale!", new Color(0.2f, 1f, 0.2f));
                        }

                        AdvanceTimeToNextTurn();
                        return;
                    }
                }
            }
            
            _activeParticipant.ATB = 0f;
            AdvanceTimeToNextTurn();
        }

        /// <summary>
        /// Simulates future queue sequence to predict the upcoming initiative bar.
        /// </summary>
        public List<ITimelineParticipant> PredictFutureTimeline(int steps)
        {
            List<ITimelineParticipant> prediction = new List<ITimelineParticipant>();
            
            CleanActiveParticipants();
            if (activeParticipants.Count == 0) return prediction;

            // Clone current state variables locally
            List<SimulatedParticipantState> simulated = activeParticipants.Select(p => new SimulatedParticipantState(p)).ToList();

            for (int i = 0; i < steps; i++)
            {
                float minTicks = float.MaxValue;
                SimulatedParticipantState nextState = null;

                foreach (var state in simulated)
                {
                    float ticksNeeded = (100f - state.atb) / state.initiative;
                    if (ticksNeeded < minTicks)
                    {
                        minTicks = ticksNeeded;
                        nextState = state;
                    }
                    else if (Mathf.Approximately(ticksNeeded, minTicks) && nextState != null)
                    {
                        if (state.initiative > nextState.initiative)
                        {
                            nextState = state;
                        }
                        else if (state.initiative == nextState.initiative && state.playerIndex < nextState.playerIndex)
                        {
                            nextState = state;
                        }
                    }
                }

                if (nextState != null)
                {
                    // Advance ATBs in the simulation
                    foreach (var state in simulated)
                    {
                        state.atb += state.initiative * minTicks;
                    }

                    // Log selection
                    prediction.Add(nextState.participant);
                    
                    // Reset simulator ATB for the chosen participant
                    nextState.atb = 0f;
                }
            }

            return prediction;
        }

        private void CleanActiveParticipants()
        {
            // Destroy GameObjects of dead stacks safely at the start of a turn transition
            for (int i = activeParticipants.Count - 1; i >= 0; i--)
            {
                var participant = activeParticipants[i];
                bool isUnityDestroyed = (participant is MonoBehaviour mb && mb == null);
                if (participant == null || isUnityDestroyed)
                {
                    activeParticipants.RemoveAt(i);
                    continue;
                }

                if (participant.IsDead)
                {
                    if (participant is CreatureStack stack && stack != null)
                    {
                        Debug.Log($"[TurnManager] CleanActiveParticipants: Destroying dead stack GameObject: {stack.gameObject.name}");
                        Destroy(stack.gameObject);
                    }
                    activeParticipants.RemoveAt(i);
                }
            }
        }

        private class SimulatedParticipantState
        {
            public ITimelineParticipant participant;
            public float atb;
            public float initiative;
            public int playerIndex;

            public SimulatedParticipantState(ITimelineParticipant p)
            {
                participant = p;
                atb = p.ATB;
                initiative = p.Initiative;
                playerIndex = p.PlayerIndex;
            }
        }
    }
}
