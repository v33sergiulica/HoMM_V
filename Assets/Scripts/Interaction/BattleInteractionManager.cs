using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using HommClone.Grid;
using HommClone.Creatures;
using HommClone.Turns;

namespace HommClone.Interaction
{
    /// <summary>
    /// Manages player mouse inputs, tile highlighting for movement/combat ranges,
    /// directional hover targeting on enemy units, and triggering movement/melee actions.
    /// </summary>
    public class BattleInteractionManager : MonoBehaviour
    {
        private GridManager _gridManager;
        private TurnManager _turnManager;
        private UI.BattleUIManager _uiManager;

        private bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            set => _isBusy = value;
        }

        private CreatureStack _lastActiveStack = null;
        private Vector2Int _lastActiveStackPos;
        private Dictionary<Vector2Int, List<Vector2Int>> _reachableTiles = new Dictionary<Vector2Int, List<Vector2Int>>();

        private Spells.Spell _selectedSpell = null;
        private bool _isSpellTargetingMode = false;

        private void Start()
        {
            _gridManager = FindFirstObjectByType<GridManager>();
            _turnManager = FindFirstObjectByType<TurnManager>();
            _uiManager = FindFirstObjectByType<UI.BattleUIManager>();
        }

        private void Update()
        {
            // Safeguards
            if (_turnManager == null || _gridManager == null || Camera.main == null || Mouse.current == null)
                return;

            // Block combat interaction inputs if Setup/Deployment is active
            var setup = FindFirstObjectByType<HommClone.Turns.BattleSetupManager>();
            if (setup != null && setup.CurrentState != HommClone.Turns.BattleSetupManager.SetupState.Combat)
            {
                return;
            }

            // Handle right-click detailed info sheet on CreatureStacks or Heroes
            if (!_isSpellTargetingMode && Mouse.current.rightButton.wasPressedThisFrame)
            {
                Ray infoRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(infoRay, out RaycastHit infoHit, 100f))
                {
                    CreatureStack stack = infoHit.collider.GetComponentInParent<CreatureStack>() ?? infoHit.collider.GetComponent<CreatureStack>();
                    if (stack != null && !stack.IsDead)
                    {
                        if (_uiManager != null)
                        {
                            _uiManager.ShowUnitInfoPanel(stack);
                            return;
                        }
                    }

                    Heroes.Hero hero = infoHit.collider.GetComponentInParent<Heroes.Hero>() ?? infoHit.collider.GetComponent<Heroes.Hero>();
                    if (hero != null)
                    {
                        if (_uiManager != null)
                        {
                            _uiManager.ShowHeroInfoPanel(hero);
                            return;
                        }
                    }
                }
            }

            if (_isBusy)
                return;

            // Block human player input if active unit is Player 2
            ITimelineParticipant activeUnit = _turnManager.ActiveUnit;
            if (activeUnit == null) return;

            if (activeUnit.PlayerIndex == 2)
            {
                var aiManager = FindFirstObjectByType<AI.BattleAIManager>();
                if (aiManager != null && aiManager.AIEnabled)
                {
                    ClearHighlights();
                    return;
                }
            }

            // Block grid raycasts if mouse is over any UI element
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                ClearHighlights();
                if (_uiManager != null) _uiManager.HideHoverTooltip();
                return;
            }

            // Spell targeting mode validation (works for both Heroes and Casters!)
            if (_isSpellTargetingMode && _selectedSpell != null)
            {
                HandleSpellTargeting(activeUnit);
                return;
            }

            // Recalculate reachable tiles if the active stack changes or moves
            if (activeUnit is CreatureStack activeStack)
            {
                if (_lastActiveStack != activeStack || _lastActiveStackPos != activeStack.GridPosition)
                {
                    _reachableTiles = _gridManager.GetReachableTiles(activeStack.GridPosition, activeStack.Speed, activeStack.Data.IsFlying, activeStack.IsLarge);
                    _lastActiveStack = activeStack;
                    _lastActiveStackPos = activeStack.GridPosition;
                    DrawReachableRange();
                }

                HandleCreatureTurn(activeStack);
            }
            else if (activeUnit is Heroes.Hero hero)
            {
                if (_lastActiveStack != null)
                {
                    _reachableTiles.Clear();
                    _lastActiveStack = null;
                    DrawReachableRange();
                }
                
                HandleHeroTurn(hero);
            }
        }

        private void HandleHeroTurn(Heroes.Hero hero)
        {
            // Raycast into scene from mouse screen position
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, 100f);

            bool hoveringUnit = false;
            if (hitSomething)
            {
                CreatureStack hStack = hit.collider.GetComponentInParent<CreatureStack>();
                if (hStack != null && !hStack.IsHero && !hStack.IsDead) hoveringUnit = true;
            }

            // Draw targets (highlights all active enemy creature stacks)
            DrawReachableRange(hoveringUnit);

            if (_uiManager != null)
            {
                _uiManager.HideHoverTooltip();
            }

            if (hitSomething)
            {
                CreatureStack hitStack = hit.collider.GetComponentInParent<CreatureStack>();
                
                // Show hovered unit's movement range for tactical planning
                if (hitStack != null && !hitStack.IsDead && !hitStack.IsHero)
                {
                    HighlightUnitReachableRange(hitStack);
                }

                if (hitStack != null && hitStack.PlayerIndex != hero.PlayerIndex && !hitStack.IsDead && !hitStack.IsHero)
                {
                    Tile enemyTile = _gridManager.GetTileAt(hitStack.GridPosition);
                    if (enemyTile != null)
                    {
                        enemyTile.SetColor(new Color(0.9f, 0.2f, 0.2f, 0.9f)); // Bright red target highlight
                    }

                    // Estimate damage using Hero values
                    int rawDamage = 10 * hero.Attack;
                    int finalDamage = hitStack.CalculateRealDamage(rawDamage, hitStack.Defense);
                    int casualties = hitStack.CalculateCasualties(hitStack, finalDamage);

                    if (_uiManager != null)
                    {
                        string tooltip = $"<b>Hero Strike</b>\nDamage: {finalDamage}\nKills: {casualties}";
                        _uiManager.ShowHoverTooltip(Mouse.current.position.ReadValue(), tooltip);
                    }

                    if (Mouse.current.leftButton.wasPressedThisFrame)
                    {
                        _isBusy = true;
                        ClearHighlights();
                        if (_uiManager != null) _uiManager.HideHoverTooltip();

                        StartCoroutine(hero.DirectAttackCoroutine(hitStack, () =>
                        {
                            EndTurn();
                        }));
                    }
                }
            }
        }

        private void HandleSpellTargeting(ITimelineParticipant activeUnit)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelSpellTargeting();
                return;
            }

            if (_uiManager != null) _uiManager.HideHoverTooltip();
            ClearHighlights();

            Ray spellRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(spellRay, out RaycastHit spellHit, 100f))
            {
                CreatureStack hitStack = spellHit.collider.GetComponentInParent<CreatureStack>();
                if (hitStack != null && !hitStack.IsDead)
                {
                    bool isValid = false;
                    if (_selectedSpell.Type == Spells.SpellType.Buff)
                    {
                        isValid = (hitStack.PlayerIndex == activeUnit.PlayerIndex);
                    }
                    else
                    {
                        isValid = (hitStack.PlayerIndex != activeUnit.PlayerIndex);
                    }

                    if (isValid)
                    {
                        Color highlightColor = (_selectedSpell.Type == Spells.SpellType.Damage) 
                            ? new Color(0f, 0.8f, 1f, 0.9f)   // Cyan
                            : new Color(0.7f, 0.4f, 1f, 0.9f); // Purple
                        
                        if (_selectedSpell is Spells.FireballSpell)
                        {
                            List<Grid.Tile> neighbors = _gridManager.GetNeighbours(hitStack.GridPosition, allowDiagonals: true);
                            Grid.Tile centerTile = _gridManager.GetTileAt(hitStack.GridPosition);
                            if (centerTile != null) centerTile.SetColor(highlightColor);
                            foreach (var neighbor in neighbors)
                            {
                                if (neighbor != null) neighbor.SetColor(highlightColor);
                            }
                        }
                        else
                        {
                            Tile enemyTile = _gridManager.GetTileAt(hitStack.GridPosition);
                            if (enemyTile != null)
                            {
                                enemyTile.SetColor(highlightColor);
                            }
                        }

                        // Spell Hover Tooltip
                        if (_uiManager != null)
                        {
                            string tooltip = "";
                            if (_selectedSpell is Spells.IceBoltSpell)
                            {
                                float cst = 1.0f;
                                SpellMastery mastery = (activeUnit is Heroes.Hero) ? SpellMastery.Expert : SpellMastery.Basic;
                                if (activeUnit is CreatureStack casterStack)
                                {
                                    var casterAbility = casterStack.GetAbility<CasterAbility>();
                                    if (casterAbility != null)
                                    {
                                        mastery = casterAbility.Mastery;
                                    }
                                }
                                switch (mastery)
                                {
                                    case SpellMastery.Basic: cst = 1.0f; break;
                                    case SpellMastery.Intermediate: cst = 1.2f; break;
                                    case SpellMastery.Advanced: cst = 1.5f; break;
                                    case SpellMastery.Expert: cst = 2.0f; break;
                                }
                                int spellDamage = Mathf.RoundToInt(activeUnit.SpellPower * (cst * cst * 20f));
                                int kills = hitStack.CalculateCasualties(hitStack, spellDamage);
                                
                                tooltip = $"<b>Cast {_selectedSpell.SpellName}</b>\nDamage: {spellDamage}\nKills: {kills}";
                            }
                            else if (_selectedSpell is Spells.SlowSpell)
                            {
                                float reductionPercent = 0.20f;
                                SpellMastery mastery = (activeUnit is Heroes.Hero) ? SpellMastery.Expert : SpellMastery.Basic;
                                if (activeUnit is CreatureStack casterStack)
                                {
                                    var casterAbility = casterStack.GetAbility<CasterAbility>();
                                    if (casterAbility != null)
                                    {
                                        mastery = casterAbility.Mastery;
                                    }
                                }
                                switch (mastery)
                                {
                                    case SpellMastery.Basic: reductionPercent = 0.20f; break;
                                    case SpellMastery.Intermediate: reductionPercent = 0.30f; break;
                                    case SpellMastery.Advanced: reductionPercent = 0.45f; break;
                                    case SpellMastery.Expert: reductionPercent = 0.60f; break;
                                }
                                float initReductionRaw = hitStack.Initiative * reductionPercent;
                                float initiativeReduction = Mathf.Max(1f, Mathf.Round(initReductionRaw * 10f) / 10f);
                                tooltip = $"<b>Cast {_selectedSpell.SpellName}</b>\nEffect: -{initiativeReduction:F1} Initiative\nDuration: {activeUnit.SpellPower} rounds";
                            }
                            else if (_selectedSpell is Spells.HasteSpell)
                            {
                                float boostPercent = 0.20f;
                                SpellMastery mastery = (activeUnit is Heroes.Hero) ? SpellMastery.Expert : SpellMastery.Basic;
                                if (activeUnit is CreatureStack casterStack)
                                {
                                    var casterAbility = casterStack.GetAbility<CasterAbility>();
                                    if (casterAbility != null)
                                    {
                                        mastery = casterAbility.Mastery;
                                    }
                                }
                                switch (mastery)
                                {
                                    case SpellMastery.Basic: boostPercent = 0.20f; break;
                                    case SpellMastery.Intermediate: boostPercent = 0.30f; break;
                                    case SpellMastery.Advanced: boostPercent = 0.45f; break;
                                    case SpellMastery.Expert: boostPercent = 0.60f; break;
                                }
                                float initBoostRaw = hitStack.Initiative * boostPercent;
                                float initiativeIncrease = Mathf.Max(1f, Mathf.Round(initBoostRaw * 10f) / 10f);
                                tooltip = $"<b>Cast {_selectedSpell.SpellName}</b>\nEffect: +{initiativeIncrease:F1} Initiative\nDuration: {activeUnit.SpellPower} rounds";
                            }
                            else if (_selectedSpell is Spells.FireballSpell)
                            {
                                float multiplier = 15f;
                                SpellMastery mastery = (activeUnit is Heroes.Hero) ? SpellMastery.Expert : SpellMastery.Basic;
                                if (activeUnit is CreatureStack casterStack)
                                {
                                    var casterAbility = casterStack.GetAbility<CasterAbility>();
                                    if (casterAbility != null)
                                    {
                                        mastery = casterAbility.Mastery;
                                    }
                                }
                                switch (mastery)
                                {
                                    case SpellMastery.Basic: multiplier = 15f; break;
                                    case SpellMastery.Intermediate: multiplier = 20f; break;
                                    case SpellMastery.Advanced: multiplier = 25f; break;
                                    case SpellMastery.Expert: multiplier = 35f; break;
                                }
                                int spellDamage = Mathf.RoundToInt(activeUnit.SpellPower * multiplier);
                                tooltip = $"<b>Cast {_selectedSpell.SpellName}</b>\nAoE: 3x3 Grid\nDamage: {spellDamage}";
                            }
                            else
                            {
                                tooltip = $"<b>Cast {_selectedSpell.SpellName}</b>\nCost: {_selectedSpell.ManaCost} Mana";
                            }

                            _uiManager.ShowHoverTooltip(Mouse.current.position.ReadValue(), tooltip);
                        }

                        if (Mouse.current.leftButton.wasPressedThisFrame)
                        {
                            _isBusy = true;
                            ClearHighlights();
                            if (_uiManager != null) _uiManager.HideHoverTooltip();

                            Spells.Spell spellToCast = _selectedSpell;
                            _selectedSpell = null;
                            _isSpellTargetingMode = false;

                            if (activeUnit is Heroes.Hero hero)
                            {
                                StartCoroutine(hero.CastSpellCoroutine(spellToCast, hitStack, () =>
                                {
                                    EndTurn();
                                }));
                            }
                            else if (activeUnit is CreatureStack stack)
                            {
                                StartCoroutine(stack.CastSpellCoroutine(spellToCast, hitStack, () =>
                                {
                                    EndTurn();
                                }));
                            }
                        }
                    }
                }
            }
        }

        private void HandleCreatureTurn(CreatureStack activeStack)
        {
            // Raycast into scene from mouse screen position
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, 100f);

            bool hoveringOtherUnit = false;
            if (hitSomething)
            {
                CreatureStack hStack = hit.collider.GetComponentInParent<CreatureStack>();
                if (hStack != null && hStack != activeStack && !hStack.IsHero && !hStack.IsDead) hoveringOtherUnit = true;
            }

            // Reset current visual frames to baseline reachable range
            DrawReachableRange(hoveringOtherUnit);

            // Clear any active hover tooltips by default
            if (_uiManager != null)
            {
                _uiManager.HideHoverTooltip();
            }

            if (hitSomething)
            {
                CreatureStack hitStack = hit.collider.GetComponentInParent<CreatureStack>();
                Tile hitTile = hit.collider.GetComponentInParent<Tile>();

                // 1. Hovering an ENEMY Stack -> Ranged Shot or Melee Attack Proximity Math
                if (hitStack != null && hitStack.PlayerIndex != activeStack.PlayerIndex && !hitStack.IsDead && !hitStack.IsHero)
                {
                    // Draw enemy's movement range in soft faded slate grey for tactical calculation!
                    HighlightUnitReachableRange(hitStack);

                    bool forceMelee = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;

                    if (activeStack.CanShoot() && !forceMelee)
                    {
                        // --- RANGED SHOT MODE ---
                        foreach (Vector2Int pos in hitStack.GetOccupiedTiles())
                        {
                            Tile enemyTile = _gridManager.GetTileAt(pos);
                            if (enemyTile != null)
                            {
                                enemyTile.SetColor(new Color(0.9f, 0.2f, 0.2f, 0.9f));
                            }
                        }

                        activeStack.GetDamageEstimation(hitStack, isMelee: false, out int minDmg, out int maxDmg, out int minKills, out int maxKills);
                        if (_uiManager != null)
                        {
                            string tooltip = $"<b>Ranged Attack</b>\nDamage: {minDmg} - {maxDmg}\nKills: {minKills} - {maxKills}";
                            _uiManager.ShowHoverTooltip(Mouse.current.position.ReadValue(), tooltip);
                        }

                        if (Mouse.current.leftButton.wasPressedThisFrame)
                        {
                            _isBusy = true;
                            ClearHighlights();
                            if (_uiManager != null) _uiManager.HideHoverTooltip();

                            StartCoroutine(activeStack.RangedAttackCoroutine(hitStack, () =>
                            {
                                EndTurn();
                            }));
                        }
                    }
                    else
                    {
                        // --- MELEE ATTACK MODE ---
                        List<Tile> candidates = new List<Tile>();
                        List<Vector2Int> targetTiles = hitStack.GetOccupiedTiles();

                        List<Vector2Int> searchPositions = new List<Vector2Int>(_reachableTiles.Keys);
                        if (!searchPositions.Contains(activeStack.GridPosition))
                        {
                            searchPositions.Add(activeStack.GridPosition);
                        }

                        foreach (Vector2Int nPos in searchPositions)
                        {
                            Tile neighbor = _gridManager.GetTileAt(nPos);
                            if (neighbor == null) continue;

                            var occupant = _gridManager.GetCreatureAt(neighbor.GridPosition);
                            if (occupant == null || occupant == activeStack)
                            {
                                bool isAdjacent = false;
                                foreach (Vector2Int selfTile in activeStack.GetOccupiedTiles())
                                {
                                    Vector2Int shiftedSelfTile = nPos + (selfTile - activeStack.GridPosition);
                                    foreach (Vector2Int tTile in targetTiles)
                                    {
                                        if (Mathf.Abs(shiftedSelfTile.x - tTile.x) <= 1 && Mathf.Abs(shiftedSelfTile.y - tTile.y) <= 1)
                                        {
                                            isAdjacent = true;
                                            break;
                                        }
                                    }
                                    if (isAdjacent) break;
                                }

                                if (isAdjacent)
                                {
                                    candidates.Add(neighbor);
                                }
                            }
                        }

                        Tile bestTile = null;
                        float maxSimilarity = -2f; 

                        Vector3 enemyCenter = hitStack.transform.position;
                        Vector2 hitDirection = new Vector2(hit.point.x - enemyCenter.x, hit.point.z - enemyCenter.z);
                        
                        if (hitDirection.sqrMagnitude > 0.001f)
                        {
                            hitDirection.Normalize();
                        }
                        else
                        {
                            hitDirection = (activeStack.PlayerIndex == 1) ? Vector2.right : Vector2.left;
                        }

                        foreach (Tile neighbor in candidates)
                        {
                            Vector3 neighborCenter = neighbor.transform.position;
                            Vector2 neighborDir = new Vector2(neighborCenter.x - enemyCenter.x, neighborCenter.z - enemyCenter.z).normalized;
                            
                            float similarity = Vector2.Dot(hitDirection, neighborDir);
                            if (similarity > maxSimilarity)
                            {
                                maxSimilarity = similarity;
                                bestTile = neighbor;
                            }
                        }

                        if (bestTile != null)
                        {
                            // Highlight chosen attack-from tiles in soft red (full footprint if large)
                            if (activeStack.IsLarge)
                            {
                                for (int dx = 0; dx <= 1; dx++)
                                {
                                    for (int dy = 0; dy <= 1; dy++)
                                    {
                                        Tile atkTile = _gridManager.GetTileAt(new Vector2Int(bestTile.GridPosition.x + dx, bestTile.GridPosition.y + dy));
                                        if (atkTile != null)
                                        {
                                            atkTile.SetColor(new Color(0.9f, 0.2f, 0.2f, 0.8f));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                bestTile.SetColor(new Color(0.9f, 0.2f, 0.2f, 0.8f));
                            }

                            // Calculate and display Melee Attack Hover Tooltip
                            activeStack.GetDamageEstimation(hitStack, isMelee: true, out int minDmg, out int maxDmg, out int minKills, out int maxKills);
                            if (_uiManager != null)
                            {
                                string tooltip = $"<b>Melee Attack</b>\nDamage: {minDmg} - {maxDmg}\nKills: {minKills} - {maxKills}";
                                _uiManager.ShowHoverTooltip(Mouse.current.position.ReadValue(), tooltip);
                            }

                            // Find path to attack-from tile
                            List<Vector2Int> path = new List<Vector2Int>();
                            if (bestTile.GridPosition != activeStack.GridPosition)
                            {
                                path = _reachableTiles[bestTile.GridPosition];
                            }

                            // Highlight intermediate path coordinates in red
                            foreach (Vector2Int pos in path)
                            {
                                Tile pTile = _gridManager.GetTileAt(pos);
                                if (pTile != null)
                                {
                                    pTile.SetColor(new Color(0.9f, 0.1f, 0.1f, 0.6f));
                                }
                            }

                            // Highlight target enemy tiles in dark red
                            foreach (Vector2Int pos in hitStack.GetOccupiedTiles())
                            {
                                Tile enemyTile = _gridManager.GetTileAt(pos);
                                if (enemyTile != null)
                                {
                                    enemyTile.SetColor(new Color(0.5f, 0f, 0f, 0.9f));
                                }
                            }

                            // Left click triggers move-and-attack sequence
                            if (Mouse.current.leftButton.wasPressedThisFrame)
                            {
                                _isBusy = true;
                                ClearHighlights();
                                if (_uiManager != null) _uiManager.HideHoverTooltip();

                                if (path.Count > 0)
                                {
                                    StartCoroutine(activeStack.MoveAlongPathCoroutine(path, () =>
                                    {
                                        StartCoroutine(activeStack.MeleeAttackCoroutine(hitStack, () =>
                                        {
                                            EndTurn();
                                        }));
                                    }, hitStack));
                                }
                                else
                                {
                                    StartCoroutine(activeStack.MeleeAttackCoroutine(hitStack, () =>
                                    {
                                        EndTurn();
                                    }));
                                }
                            }
                        }
                        else
                        {
                            // Enemy is out of range for melee
                            if (_uiManager != null)
                            {
                                string tooltip = "<b>Melee Attack</b>\n<color=#ff4444>Out of Range</color>";
                                _uiManager.ShowHoverTooltip(Mouse.current.position.ReadValue(), tooltip);
                            }
                        }
                    }
                }
                // 2. Hovering an EMPTY Reachable Tile -> Movement Pathway
                else if (hitTile != null && _reachableTiles.ContainsKey(hitTile.GridPosition))
                {
                    // Ensure destination is vacant or occupied only by self (for 2x2 shifting)
                    var occupant = _gridManager.GetCreatureAt(hitTile.GridPosition);
                    if (occupant == null || occupant == activeStack)
                    {
                        List<Vector2Int> path = _reachableTiles[hitTile.GridPosition];

                        // Highlight movement path in translucent yellow
                        foreach (Vector2Int pos in path)
                        {
                            if (activeStack.IsLarge)
                            {
                                for (int dx = 0; dx <= 1; dx++)
                                {
                                    for (int dy = 0; dy <= 1; dy++)
                                    {
                                        Tile pTile = _gridManager.GetTileAt(new Vector2Int(pos.x + dx, pos.y + dy));
                                        if (pTile != null)
                                        {
                                            pTile.SetColor(new Color(1f, 0.9f, 0.1f, 0.5f));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Tile pTile = _gridManager.GetTileAt(pos);
                                if (pTile != null)
                                {
                                    pTile.SetColor(new Color(1f, 0.9f, 0.1f, 0.7f));
                                }
                            }
                        }

                        // Highlight destination in solid orange/yellow
                        if (activeStack.IsLarge)
                        {
                            for (int dx = 0; dx <= 1; dx++)
                            {
                                for (int dy = 0; dy <= 1; dy++)
                                {
                                    Tile destTile = _gridManager.GetTileAt(new Vector2Int(hitTile.GridPosition.x + dx, hitTile.GridPosition.y + dy));
                                    if (destTile != null)
                                    {
                                        destTile.SetColor(new Color(0.9f, 0.5f, 0f, 0.9f)); // Orange highlight for large body destination
                                    }
                                }
                            }
                        }
                        else
                        {
                            hitTile.SetColor(new Color(1f, 0.8f, 0f, 0.9f));
                        }

                        // Left click triggers move sequence
                        if (Mouse.current.leftButton.wasPressedThisFrame)
                        {
                            _isBusy = true;
                            ClearHighlights();
                            StartCoroutine(activeStack.MoveAlongPathCoroutine(path, () =>
                            {
                                EndTurn();
                            }));
                        }
                    }
                }
                // 3. Hovering an ALLIED Stack -> Just show their movement range
                else if (hitStack != null && hitStack.PlayerIndex == activeStack.PlayerIndex && hitStack != activeStack && !hitStack.IsDead && !hitStack.IsHero)
                {
                    HighlightUnitReachableRange(hitStack);
                }
            }
        }

        private void DrawReachableRange(bool hideForHover = false)
        {
            ClearHighlights();

            if (_turnManager == null || _turnManager.ActiveUnit == null || _gridManager == null) return;
            if (hideForHover) return; // Hide standard green tiles when tactically checking another unit

            ITimelineParticipant active = _turnManager.ActiveUnit;

            if (active is Heroes.Hero hero)
            {
                // Highlight all active enemy creature stacks in a soft red/pink tone to show they can be targeted
                foreach (var stack in _turnManager.ActiveStacks)
                {
                    if (stack != null && !stack.IsDead && stack.PlayerIndex != hero.PlayerIndex)
                    {
                        Tile tile = _gridManager.GetTileAt(stack.GridPosition);
                        if (tile != null)
                        {
                            tile.SetColor(new Color(0.9f, 0.2f, 0.2f, 0.4f)); // Translucent red
                        }
                    }
                }
                return;
            }

            if (active is CreatureStack activeStack)
            {
                // Paint all valid move positions in translucent blue
                if (activeStack.IsLarge)
                {
                    HashSet<Vector2Int> reachableBodyTiles = new HashSet<Vector2Int>();
                    foreach (var pos in _reachableTiles.Keys)
                    {
                        reachableBodyTiles.Add(pos);
                        reachableBodyTiles.Add(new Vector2Int(pos.x + 1, pos.y));
                        reachableBodyTiles.Add(new Vector2Int(pos.x, pos.y + 1));
                        reachableBodyTiles.Add(new Vector2Int(pos.x + 1, pos.y + 1));
                    }
                    foreach (var pos in reachableBodyTiles)
                    {
                        Tile tile = _gridManager.GetTileAt(pos);
                        if (tile != null)
                        {
                            tile.SetColor(new Color(0.2f, 0.5f, 0.9f, 0.4f));
                        }
                    }
                }
                else
                {
                    foreach (var pos in _reachableTiles.Keys)
                    {
                        Tile tile = _gridManager.GetTileAt(pos);
                        if (tile != null)
                        {
                            tile.SetColor(new Color(0.2f, 0.5f, 0.9f, 0.4f));
                        }
                    }
                }

                // Paint current active stack tiles in green
                if (activeStack.IsLarge)
                {
                    foreach (var pos in activeStack.GetOccupiedTiles())
                    {
                        Tile activeTile = _gridManager.GetTileAt(pos);
                        if (activeTile != null)
                        {
                            activeTile.SetColor(Color.green);
                        }
                    }
                }
                else
                {
                    Tile activeTile = _gridManager.GetTileAt(activeStack.GridPosition);
                    if (activeTile != null)
                    {
                        activeTile.SetColor(Color.green);
                    }
                }
            }
        }

        private void HighlightUnitReachableRange(CreatureStack unit)
        {
            if (unit == null || unit.IsDead || _gridManager == null) return;
            var reachable = _gridManager.GetReachableTiles(unit.GridPosition, unit.Speed, unit.Data.IsFlying, unit.IsLarge);
            Color fadedGrey = new Color(0.45f, 0.45f, 0.52f, 0.6f); // Soft slate grey
            foreach (var pos in reachable.Keys)
            {
                Tile tile = _gridManager.GetTileAt(pos);
                if (tile != null)
                {
                    tile.SetColor(fadedGrey);
                }
            }
        }

        private void ClearHighlights()
        {
            Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
            foreach (var tile in allTiles)
            {
                tile.ResetColor();
            }
        }

        private void EndTurn()
        {
            _isBusy = false;
            _turnManager.ExecuteAction();
        }
        public void StartSpellTargeting(Spells.Spell spell)
        {
            if (spell == null) return;
            _selectedSpell = spell;
            _isSpellTargetingMode = true;
            Debug.Log($"[Spell targeting] Entered spell targeting for: {spell.SpellName}");
        }

        public void CancelSpellTargeting()
        {
            _selectedSpell = null;
            _isSpellTargetingMode = false;
            ClearHighlights();
            if (_uiManager != null) _uiManager.HideHoverTooltip();
            Debug.Log("[Spell targeting] Cancelled targeting.");
        }
    }
}
