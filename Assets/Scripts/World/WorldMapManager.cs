using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace HommClone.World
{
    /// <summary>
    /// Coordinates World Map interactions (raycast path selection, hero movement requests),
    /// top-bar UI rendering (Resources, MP, Day/Week), and End Turn advancement.
    /// Uses Unity's New Input System package.
    /// </summary>
    public class WorldMapManager : MonoBehaviour
    {
        private WorldGridManager _gridManager;
        private WorldHero _activeHero;
        private List<Vector2Int> _currentPath = new List<Vector2Int>();

        [Header("User UI References (Optional / Custom Canvas)")]
        [SerializeField] private Button endTurnButton;
        [SerializeField] private TextMeshProUGUI resourceText;
        [SerializeField] private TextMeshProUGUI heroInfoText;
        [SerializeField] private TextMeshProUGUI dateText;
        [SerializeField] private Image dayProgressImage; // Radial Filled Image for Day 1-7
        [SerializeField] private Image movementProgressImage; // Radial Filled Image for Hero MP
        [SerializeField] private Image heroHUDPortraitImage; // Bottom-Left Hero Portrait Image
        [SerializeField] private Button heroHUDButton; // Clickable Bottom-Left Hero Portrait Button
        [SerializeField] [Range(0f, 1f)] private float dayMinFill = 0.0f;
        [SerializeField] [Range(0f, 1f)] private float dayMaxFill = 1.0f;
        [SerializeField] private List<Image> daySegmentImages = new List<Image>(); // Optional 7 discrete segment images
        [SerializeField] private string battleSceneName = "SampleScene"; // Name of your Battle Scene in Unity
        [SerializeField] private bool autoCreateFallbackUI = false;

        private GameObject _uiCanvas;

        private void Start()
        {
            var audioManager = HommClone.Audio.AudioManager.GetOrCreateInstance();
            if (audioManager != null) audioManager.PlayWorldMapMusic();
            _gridManager = FindFirstObjectByType<WorldGridManager>();

            var manager = GameDataManager.GetOrCreateInstance();
            if (manager != null)
            {
                manager.activePlayerIndex = 1; // Always start game on Player 1's turn
            }

            UI.ResourceBarUI.GetOrCreateInstance();

            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveAllListeners();
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }

            if (heroHUDButton != null)
            {
                heroHUDButton.onClick.RemoveAllListeners();
                heroHUDButton.onClick.AddListener(OnHeroHUDClicked);
            }

            if (autoCreateFallbackUI)
            {
                CreateWorldUI();
            }

            EnsurePlayerHeroesExist();
            FocusCameraOnActiveHero();
            UpdateUI();

            CheckPostBattleCleanup();
        }

        private void EnsurePlayerHeroesExist()
        {
            var manager = GameDataManager.GetOrCreateInstance();
            WorldHero[] heroes = FindObjectsByType<WorldHero>(FindObjectsSortMode.None);

            WorldHero p1Hero = null;
            WorldHero p2Hero = null;

            if (heroes != null && heroes.Length > 0)
            {
                // Sort heroes deterministically by grid distance to (0,0) so Player 1 and Player 2 are never inverted!
                System.Array.Sort(heroes, (a, b) =>
                {
                    int distA = a.GridPosition.x + a.GridPosition.y;
                    int distB = b.GridPosition.x + b.GridPosition.y;
                    return distA.CompareTo(distB);
                });

                p1Hero = heroes[0];
                p1Hero.SetPlayerIndexAndPosition(1, p1Hero.GridPosition != Vector2Int.zero ? p1Hero.GridPosition : new Vector2Int(2, 2));

                if (heroes.Length >= 2)
                {
                    p2Hero = heroes[1];
                    p2Hero.SetPlayerIndexAndPosition(2, p2Hero.GridPosition != Vector2Int.zero ? p2Hero.GridPosition : new Vector2Int(18, 18));
                }
            }

            if (p2Hero == null)
            {
                GameObject obj = new GameObject("WorldHero_Player2");
                p2Hero = obj.AddComponent<WorldHero>();
                p2Hero.SetPlayerIndexAndPosition(2, new Vector2Int(18, 18));
            }

            if (manager != null)
            {
                if (p1Hero != null) manager.player1Hero.worldPosition = p1Hero.GridPosition;
                if (p2Hero != null) manager.player2Hero.worldPosition = p2Hero.GridPosition;
            }
        }

        private void CreateMine(ResourceType type, int income, Vector2Int pos)
        {
            GameObject obj = new GameObject($"Mine_{type}_{pos.x}_{pos.y}");
            var mine = obj.AddComponent<WorldMine>();
            mine.Initialize(type, income, pos, owner: 0);
        }

        private void CreatePickable(ResourceType type, int qty, Vector2Int pos)
        {
            GameObject obj = new GameObject($"Pickup_{type}_{pos.x}_{pos.y}");
            var p = obj.AddComponent<WorldResourcePickable>();
            p.Initialize(type, qty, pos);
        }

        private void CheckPostBattleCleanup()
        {
            var manager = GameDataManager.GetOrCreateInstance();
            if (manager != null && manager.isReturningFromBattle)
            {
                manager.isReturningFromBattle = false;
                manager.isPvPBattle = false;
                if (manager.battleWon && manager.pendingBattleMonsterPosition != new Vector2Int(-1, -1))
                {
                    Vector2Int monsterPos = manager.pendingBattleMonsterPosition;
                    WorldMonsterStack[] monsters = FindObjectsByType<WorldMonsterStack>(FindObjectsSortMode.None);
                    foreach (var m in monsters)
                    {
                        if (m != null && m.GridPosition == monsterPos)
                        {
                            Debug.Log($"[WorldMapManager] Defeated monster stack at {m.GridPosition} removed from World Map!");
                            Destroy(m.gameObject);
                            break;
                        }
                    }

                    // Auto-claim any mines or collect pickable resources adjacent to or guarded by the defeated monster!
                    WorldMine[] mines = FindObjectsByType<WorldMine>(FindObjectsSortMode.None);
                    foreach (var mine in mines)
                    {
                        if (mine != null && (mine.GridPosition == monsterPos || Vector2Int.Distance(mine.GridPosition, monsterPos) <= 1.5f))
                        {
                            mine.ClaimMine(1);
                        }
                    }

                    WorldResourcePickable[] pickables = FindObjectsByType<WorldResourcePickable>(FindObjectsSortMode.None);
                    foreach (var p in pickables)
                    {
                        if (p != null && (p.GridPosition == monsterPos || Vector2Int.Distance(p.GridPosition, monsterPos) <= 1.5f))
                        {
                            p.Collect(1);
                        }
                    }

                    // Award Experience Points for winning the battle encounter to the active hero
                    int xpReward = 1200;
                    HeroData winnerHero = manager.GetActiveHero();
                    if (winnerHero != null)
                    {
                        bool leveledUp = winnerHero.GainXP(xpReward, out LevelUpInfo lvlInfo);
                        Debug.Log($"[WorldMapManager] Hero {winnerHero.heroName} gained {xpReward} XP! Current XP: {winnerHero.currentXP}/{winnerHero.xpToNextLevel} (Level {winnerHero.level})");

                        if (leveledUp)
                        {
                            var levelUpUI = UI.HeroLevelUpUI.Instance;
                            if (levelUpUI == null)
                            {
                                GameObject uiObj = new GameObject("HeroLevelUpUI");
                                levelUpUI = uiObj.AddComponent<UI.HeroLevelUpUI>();
                            }
                            levelUpUI.ShowLevelUp(winnerHero, lvlInfo);
                        }
                    }
                }
            }
        }

        private Vector2Int _selectedTargetPos = new Vector2Int(-1, -1);
        private List<Vector2Int> _selectedPath = null;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                QuitGame();
                return;
            }

            if (_gridManager == null) _gridManager = FindFirstObjectByType<WorldGridManager>();
            _activeHero = GetActiveWorldHero();

            UpdateUI();

            if (Mouse.current == null || Camera.main == null) return;

            // Block 3D world raycasting if mouse is over UI elements!
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            // Right-Click to inspect or cancel selected path
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (Physics.Raycast(ray, out RaycastHit rHit))
                {
                    WorldHero hero = rHit.collider.GetComponentInParent<WorldHero>() ?? rHit.collider.GetComponent<WorldHero>();
                    WorldTile tile = rHit.collider.GetComponentInParent<WorldTile>() ?? rHit.collider.GetComponent<WorldTile>();

                    if (hero == null && tile != null)
                    {
                        WorldHero[] allHeroes = FindObjectsByType<WorldHero>(FindObjectsSortMode.None);
                        foreach (var h in allHeroes)
                        {
                            if (h != null && h.GridPosition == tile.GridPosition)
                            {
                                hero = h;
                                break;
                            }
                        }
                    }

                    if (hero != null)
                    {
                        var sheet = HeroCharacterSheetUI.GetOrCreateInstance();
                        if (sheet != null) sheet.ToggleWindow(hero.Data);
                        return;
                    }

                    WorldMonsterStack monster = rHit.collider.GetComponentInParent<WorldMonsterStack>() ?? rHit.collider.GetComponent<WorldMonsterStack>();
                    if (monster != null)
                    {
                        var inspection = MonsterInspectionUI.GetOrCreateInstance();
                        if (inspection != null) inspection.ShowWindow(monster);
                        return;
                    }
                }

                // Deselect current path or interrupt active hero movement on right click!
                if (_activeHero != null && _activeHero.IsMoving)
                {
                    _activeHero.StopMovement();
                }

                ClearPathHighlight();
                _selectedTargetPos = new Vector2Int(-1, -1);
                _selectedPath = null;
            }

            // Disable End Turn button while hero is moving to prevent desync
            if (endTurnButton != null && _activeHero != null)
            {
                endTurnButton.interactable = !_activeHero.IsMoving;
            }

            if (_activeHero == null || _activeHero.IsMoving || _gridManager == null) return;

            // Left-Click for 2-Step Path Selection & Confirmation (HoMM Classic Style!)
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    WorldTile tile = hit.collider.GetComponentInParent<WorldTile>() ?? hit.collider.GetComponent<WorldTile>();
                    WorldMonsterStack targetMonster = hit.collider.GetComponentInParent<WorldMonsterStack>() ?? hit.collider.GetComponent<WorldMonsterStack>();
                    WorldHero targetHero = hit.collider.GetComponentInParent<WorldHero>() ?? hit.collider.GetComponent<WorldHero>();

                    Vector2Int targetPos = new Vector2Int(-1, -1);
                    if (targetMonster != null) targetPos = targetMonster.GridPosition;
                    else if (targetHero != null && targetHero != _activeHero) targetPos = targetHero.GridPosition;
                    else if (tile != null) targetPos = tile.GridPosition;

                    if (targetPos != new Vector2Int(-1, -1) && targetPos != _activeHero.GridPosition)
                    {
                        // 2nd Click on SAME target tile -> CONFIRM & EXECUTE MOVEMENT!
                        if (targetPos == _selectedTargetPos && _selectedPath != null && _selectedPath.Count > 1)
                        {
                            List<Vector2Int> movePath = new List<Vector2Int>(_selectedPath);
                            ClearPathHighlight();
                            _selectedTargetPos = new Vector2Int(-1, -1);
                            _selectedPath = null;

                            StartCoroutine(_activeHero.MoveAlongPathCoroutine(movePath, () => {
                                UpdateUI();
                                CheckTileEncounter(_activeHero.GridPosition);
                            }));
                        }
                        // 1st Click on NEW target tile -> PROJECT PATH PREVIEW!
                        else
                        {
                            _selectedTargetPos = targetPos;
                            _selectedPath = _gridManager.FindPath(_activeHero.GridPosition, targetPos);
                            HighlightCurrentPath(_selectedPath);
                        }
                    }
                    else
                    {
                        ClearPathHighlight();
                        _selectedTargetPos = new Vector2Int(-1, -1);
                        _selectedPath = null;
                    }
                }
            }
        }

        public bool CheckGuardedInteraction(Vector2Int targetTile, out WorldMonsterStack guardingMonster)
        {
            guardingMonster = null;
            WorldMonsterStack[] monsterStacks = FindObjectsByType<WorldMonsterStack>(FindObjectsSortMode.None);
            foreach (var monster in monsterStacks)
            {
                if (monster == null || monster.Count <= 0) continue;
                int dx = Mathf.Abs(targetTile.x - monster.GridPosition.x);
                int dy = Mathf.Abs(targetTile.y - monster.GridPosition.y);
                // 8-neighbor adjacency (cardinal + diagonal <= 1)
                if (dx <= 1 && dy <= 1)
                {
                    guardingMonster = monster;
                    return true;
                }
            }
            return false;
        }

        public bool IsEncounterTile(Vector2Int pos)
        {
            var manager = GameDataManager.GetOrCreateInstance();

            // 0. Direct Enemy Hero tile
            if (manager != null)
            {
                WorldHero[] heroes = FindObjectsByType<WorldHero>(FindObjectsSortMode.None);
                foreach (var h in heroes)
                {
                    if (h != null && h.PlayerIndex != manager.activePlayerIndex && h.GridPosition == pos)
                    {
                        return true;
                    }
                }
            }

            // 1. Direct monster stack tile
            WorldMonsterStack[] monsters = FindObjectsByType<WorldMonsterStack>(FindObjectsSortMode.None);
            foreach (var m in monsters)
            {
                if (m != null && m.Count > 0 && m.GridPosition == pos)
                {
                    return true;
                }
            }

            // 2. Guarded pickable resource tile
            WorldResourcePickable[] pickables = FindObjectsByType<WorldResourcePickable>(FindObjectsSortMode.None);
            foreach (var p in pickables)
            {
                if (p != null && p.GridPosition == pos && CheckGuardedInteraction(p.GridPosition, out _))
                {
                    return true;
                }
            }

            // 3. Guarded mine / factory tile
            WorldMine[] mines = FindObjectsByType<WorldMine>(FindObjectsSortMode.None);
            foreach (var mine in mines)
            {
                if (mine != null && mine.GridPosition == pos && CheckGuardedInteraction(mine.GridPosition, out _))
                {
                    return true;
                }
            }

            // 4. Guarded treasure chest tile
            WorldTreasureChest[] chests = FindObjectsByType<WorldTreasureChest>(FindObjectsSortMode.None);
            foreach (var chest in chests)
            {
                if (chest != null && chest.GridPosition == pos && CheckGuardedInteraction(chest.GridPosition, out _))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CheckTileEncounter(Vector2Int heroPos)
        {
            var manager = GameDataManager.GetOrCreateInstance();

            // 0. Check PvP Hero vs Hero encounter
            if (manager != null)
            {
                WorldHero[] heroes = FindObjectsByType<WorldHero>(FindObjectsSortMode.None);
                foreach (var h in heroes)
                {
                    if (h != null && h.PlayerIndex != manager.activePlayerIndex && h.GridPosition == heroPos)
                    {
                        TriggerPvPBattleEncounter(h);
                        return true; // PvP Battle triggered!
                    }
                }
            }

            // 1. Check direct monster stack encounters (Must step directly onto monster stack tile!)
            WorldMonsterStack[] monsters = FindObjectsByType<WorldMonsterStack>(FindObjectsSortMode.None);
            foreach (var m in monsters)
            {
                if (m != null && m.Count > 0 && m.GridPosition == heroPos)
                {
                    TriggerBattleEncounter(m);
                    return true; // Battle triggered!
                }
            }

            // 2. Check pickable resources (Must step on resource tile)
            WorldResourcePickable[] pickables = FindObjectsByType<WorldResourcePickable>(FindObjectsSortMode.None);
            foreach (var p in pickables)
            {
                if (p != null && p.GridPosition == heroPos)
                {
                    if (CheckGuardedInteraction(p.GridPosition, out var guardMonster))
                    {
                        Debug.Log($"[WorldMapManager] Resource at {p.GridPosition} is guarded by monster at {guardMonster.GridPosition}! Triggering battle!");
                        TriggerBattleEncounter(guardMonster);
                        return true; // Battle triggered!
                    }
                    else
                    {
                        p.Collect(1);
                        return false;
                    }
                }
            }

            // 3. Check mines / factories (Must step on mine tile)
            WorldMine[] mines = FindObjectsByType<WorldMine>(FindObjectsSortMode.None);
            foreach (var mine in mines)
            {
                if (mine != null && mine.GridPosition == heroPos)
                {
                    if (CheckGuardedInteraction(mine.GridPosition, out var guardMonster))
                    {
                        Debug.Log($"[WorldMapManager] Mine at {mine.GridPosition} is guarded by monster at {guardMonster.GridPosition}! Triggering battle!");
                        TriggerBattleEncounter(guardMonster);
                        return true; // Battle triggered!
                    }
                    else
                    {
                        mine.ClaimMine(1);
                        return false;
                    }
                }
            }

            // 4. Check treasure chests (Must step on chest tile)
            WorldTreasureChest[] chests = FindObjectsByType<WorldTreasureChest>(FindObjectsSortMode.None);
            foreach (var chest in chests)
            {
                if (chest != null && chest.GridPosition == heroPos)
                {
                    if (CheckGuardedInteraction(chest.GridPosition, out var guardMonster))
                    {
                        Debug.Log($"[WorldMapManager] Treasure Chest at {chest.GridPosition} is guarded by monster at {guardMonster.GridPosition}! Triggering battle!");
                        TriggerBattleEncounter(guardMonster);
                        return true; // Battle triggered!
                    }
                    else
                    {
                        HeroData heroData = _activeHero != null ? _activeHero.Data : GameDataManager.GetOrCreateInstance().player1Hero;
                        chest.Interact(heroData);
                        return false;
                    }
                }
            }

            return false;
        }

        public void TriggerBattleEncounter(WorldMonsterStack monster)
        {
            if (monster == null) return;
            var manager = GameDataManager.GetOrCreateInstance();
            if (manager == null) return;

            manager.isPvPBattle = false;
            manager.pendingBattleEnemyArmy.Clear();
            if (monster.CreatureData != null)
            {
                manager.pendingBattleEnemyArmy.Add(new ArmySlot(monster.CreatureData, monster.Count));
            }
            manager.pendingBattleMonsterPosition = monster.GridPosition;

            Debug.Log($"[WorldMapManager] Triggering Battle Encounter vs {monster.Count}x {(monster.CreatureData != null ? monster.CreatureData.CreatureName : "Monster")} at {monster.GridPosition}!");

            string targetScene = !string.IsNullOrEmpty(battleSceneName) ? battleSceneName : "BattleScene";

            try
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
            }
            catch
            {
                if (UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings > 1)
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(1);
                }
            }
        }

        public void TriggerPvPBattleEncounter(WorldHero enemyHero)
        {
            if (enemyHero == null) return;
            var manager = GameDataManager.GetOrCreateInstance();
            if (manager == null) return;

            manager.isPvPBattle = true;
            manager.pendingBattleEnemyArmy.Clear();

            HeroData enemyHeroData = enemyHero.Data;
            if (enemyHeroData != null && enemyHeroData.army != null)
            {
                foreach (var slot in enemyHeroData.army)
                {
                    if (slot != null && slot.creatureData != null && slot.count > 0)
                    {
                        manager.pendingBattleEnemyArmy.Add(new ArmySlot(slot.creatureData, slot.count));
                    }
                }
            }

            Debug.Log($"[WorldMapManager] Triggering PvP Battle Encounter: Player {manager.activePlayerIndex} Hero vs Player {enemyHero.PlayerIndex} Hero at {enemyHero.GridPosition}!");

            string targetScene = !string.IsNullOrEmpty(battleSceneName) ? battleSceneName : "BattleScene";
            try
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
            }
            catch
            {
                if (UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings > 1)
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(1);
                }
            }
        }

        private void HighlightCurrentPath(List<Vector2Int> path)
        {
            ClearPathHighlight();
            _currentPath = path;

            HeroData data = _activeHero != null ? _activeHero.Data : null;
            float remainingMP = data != null ? data.currentMovementPoints : 15f;
            float accumulatedCost = 0f;
            bool encounterTriggeredInPath = false;

            for (int i = 1; i < path.Count; i++)
            {
                Vector2Int pos = path[i];
                WorldTile tile = _gridManager.GetTileAt(pos);
                if (tile != null)
                {
                    accumulatedCost += tile.MovementCost;
                    bool inRange = accumulatedCost <= remainingMP;

                    if (IsEncounterTile(pos))
                    {
                        encounterTriggeredInPath = true;
                    }

                    PathTileState state;
                    if (!inRange)
                    {
                        state = PathTileState.OutOfRange; // Grey
                    }
                    else if (encounterTriggeredInPath)
                    {
                        state = PathTileState.ReachableCombat; // Red
                    }
                    else
                    {
                        state = PathTileState.ReachableSafe; // Green
                    }

                    tile.HighlightAsPath(state);
                }
            }
        }

        private void ClearPathHighlight()
        {
            if (_currentPath != null)
            {
                foreach (var pos in _currentPath)
                {
                    WorldTile tile = _gridManager.GetTileAt(pos);
                    if (tile != null) tile.ResetHighlight();
                }
                _currentPath.Clear();
            }
        }

        #region UI Logic
        public void UpdateUI()
        {
            var manager = GameDataManager.GetOrCreateInstance();
            if (manager != null)
            {
                var r = manager.GetActiveResources();
                if (resourceText != null && r != null)
                {
                    resourceText.text = $"P{manager.activePlayerIndex} Gold: {r.gold} | Wood: {r.wood} | Ore: {r.ore} | Gems: {r.gems}";
                }

                var h = manager.GetActiveHero();
                if (heroInfoText != null && h != null)
                {
                    heroInfoText.text = $"P{manager.activePlayerIndex} MP: {h.currentMovementPoints:F1} / {h.maxMovementPoints:F1}";
                }

                if (dateText != null)
                {
                    dateText.text = $"Month {manager.currentMonth}, Week {manager.currentWeek}, Day {manager.currentDay}";
                }

                // Fill Radial Ring for Day 1-7 progress (1/7 to 7/7)
                if (dayProgressImage != null)
                {
                    dayProgressImage.type = Image.Type.Filled;
                    float progress = Mathf.Clamp01((float)manager.currentDay / 7.0f);
                    dayProgressImage.fillAmount = Mathf.Lerp(dayMinFill, dayMaxFill, progress);
                }

                // Optional 7 Discrete Day Segment Images (lights up Segment 1 to Segment 7 one by one!)
                if (daySegmentImages != null && daySegmentImages.Count > 0)
                {
                    for (int i = 0; i < daySegmentImages.Count; i++)
                    {
                        if (daySegmentImages[i] != null)
                        {
                            daySegmentImages[i].gameObject.SetActive(i < manager.currentDay);
                        }
                    }
                }

                // Fill Radial Ring for Hero Movement Points progress
                if (movementProgressImage != null && h != null && h.maxMovementPoints > 0)
                {
                    movementProgressImage.type = Image.Type.Filled;
                    movementProgressImage.fillAmount = Mathf.Clamp01(h.currentMovementPoints / h.maxMovementPoints);
                }

                // Update HUD Portrait Image in bottom-left corner
                if (heroHUDPortraitImage != null && h != null && h.heroPortrait != null)
                {
                    heroHUDPortraitImage.sprite = h.heroPortrait;
                }
            }
        }

        /// <summary>
        /// Called when clicking the HUD Hero Portrait icon in the bottom-left corner.
        /// Opens the Hero Character Sheet window!
        /// </summary>
        public void OnHeroHUDClicked()
        {
            Debug.Log("[WorldMapManager] Hero HUD Button Clicked!");
            var manager = GameDataManager.GetOrCreateInstance();
            var hData = manager != null ? manager.GetActiveHero() : null;
            var sheet = HeroCharacterSheetUI.GetOrCreateInstance();
            if (hData != null && sheet != null)
            {
                sheet.ShowWindow(hData);
            }
        }

        private float _lastEndTurnTime = 0f;

        /// <summary>
        /// Executes Hotseat Turn Switching between Player 1 & Player 2.
        /// </summary>
        public void OnEndTurnClicked()
        {
            if (_activeHero != null && _activeHero.IsMoving) return; // Cannot skip day while hero is moving!
            if (Time.time - _lastEndTurnTime < 0.5f) return; // Debounce to prevent double-click skips!
            _lastEndTurnTime = Time.time;

            // Clear any projected path trail on day skip/end turn
            ClearPathHighlight();
            _selectedTargetPos = new Vector2Int(-1, -1);
            _selectedPath = null;

            var manager = GameDataManager.GetOrCreateInstance();
            if (manager == null) return;

            if (manager.activePlayerIndex == 1)
            {
                // Switch to Player 2
                manager.activePlayerIndex = 2;
                manager.player2Hero.maxMovementPoints = manager.player2Hero.GetEffectiveMaxMovementPoints();
                manager.player2Hero.currentMovementPoints = manager.player2Hero.maxMovementPoints;

                FocusCameraOnActiveHero();
                UpdateUI();

                var announce = UI.TurnAnnouncementUI.Instance;
                if (announce == null)
                {
                    GameObject aObj = new GameObject("TurnAnnouncementUI");
                    announce = aObj.AddComponent<UI.TurnAnnouncementUI>();
                }
                announce.AnnounceTurn(2, manager.currentDay);

                Debug.Log($"[WorldMapManager] Hotseat Turn Switched to Player 2!");
            }
            else
            {
                // Advance Day & Process Daily Income for Both Players
                manager.ProcessDaySkip();
                manager.ProcessDailyIncome();

                // Switch to Player 1
                manager.activePlayerIndex = 1;
                manager.player1Hero.maxMovementPoints = manager.player1Hero.GetEffectiveMaxMovementPoints();
                manager.player1Hero.currentMovementPoints = manager.player1Hero.maxMovementPoints;

                FocusCameraOnActiveHero();
                UpdateUI();

                var announce = UI.TurnAnnouncementUI.Instance;
                if (announce == null)
                {
                    GameObject aObj = new GameObject("TurnAnnouncementUI");
                    announce = aObj.AddComponent<UI.TurnAnnouncementUI>();
                }
                announce.AnnounceTurn(1, manager.currentDay);

                Debug.Log($"[WorldMapManager] End Turn: Advanced to Day {manager.currentDay}! Switched turn to Player 1.");
            }
        }

        public WorldHero GetActiveWorldHero()
        {
            var manager = GameDataManager.GetOrCreateInstance();
            int activeIdx = manager != null ? manager.activePlayerIndex : 1;

            WorldHero[] heroes = FindObjectsByType<WorldHero>(FindObjectsSortMode.None);
            WorldHero target = System.Array.Find(heroes, h => h != null && h.PlayerIndex == activeIdx);

            if (target != null)
            {
                _activeHero = target;
            }
            else if (_activeHero == null && heroes.Length > 0)
            {
                _activeHero = heroes[0];
            }
            return _activeHero;
        }

        public void FocusCameraOnActiveHero()
        {
            var manager = GameDataManager.GetOrCreateInstance();
            if (manager == null) return;

            WorldHero[] heroes = FindObjectsByType<WorldHero>(FindObjectsSortMode.None);
            foreach (var h in heroes)
            {
                if (h != null && h.PlayerIndex == manager.activePlayerIndex)
                {
                    _activeHero = h;

                    WorldCameraController camController = FindFirstObjectByType<WorldCameraController>();
                    if (camController != null)
                    {
                        camController.SetTargetHero(h.transform);
                    }
                    else if (Camera.main != null)
                    {
                        Vector3 targetCam = h.transform.position;
                        targetCam.y = Camera.main.transform.position.y;
                        targetCam.z = h.transform.position.z - 6f; // Position camera view behind hero
                        Camera.main.transform.position = targetCam;
                    }
                    Debug.Log($"[WorldMapManager] Camera focused on Player {manager.activePlayerIndex} Hero '{h.name}' at position {h.transform.position} (Grid: {h.GridPosition})!");
                    break;
                }
            }
        }

        public void QuitGame()
        {
            Debug.Log("[WorldMapManager] Quitting application...");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void CreateWorldUI()
        {
            if (_uiCanvas != null) return;
            _uiCanvas = new GameObject("WorldUI_Canvas");
            Canvas canvas = _uiCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _uiCanvas.AddComponent<CanvasScaler>();
            _uiCanvas.AddComponent<GraphicRaycaster>();
        }
        #endregion
    }
}
