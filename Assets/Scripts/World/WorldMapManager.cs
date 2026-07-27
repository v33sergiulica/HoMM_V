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
            _activeHero = FindFirstObjectByType<WorldHero>();

            UI.ResourceBarUI.GetOrCreateInstance();

            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }

            if (heroHUDButton != null)
            {
                heroHUDButton.onClick.AddListener(OnHeroHUDClicked);
            }

            if (autoCreateFallbackUI)
            {
                CreateWorldUI();
            }

            CheckPostBattleCleanup();
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
                }
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                QuitGame();
                return;
            }

            if (_gridManager == null) _gridManager = FindFirstObjectByType<WorldGridManager>();
            if (_activeHero == null) _activeHero = FindFirstObjectByType<WorldHero>();

            UpdateUI();

            if (Mouse.current == null || Camera.main == null) return;

            // Block 3D world raycasting if mouse is over UI elements!
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                ClearPathHighlight();
                return;
            }

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            // Right-Click to open Hero Character Sheet or Monster Stack Inspection Modal
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (Physics.Raycast(ray, out RaycastHit rHit))
                {
                    WorldHero hero = rHit.collider.GetComponentInParent<WorldHero>() ?? rHit.collider.GetComponent<WorldHero>();
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
            }

            if (_activeHero == null || _activeHero.IsMoving || _gridManager == null) return;

            // Raycast mouse hovering for path preview
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                WorldTile tile = hit.collider.GetComponentInParent<WorldTile>() ?? hit.collider.GetComponent<WorldTile>();
                WorldMonsterStack targetMonster = hit.collider.GetComponentInParent<WorldMonsterStack>() ?? hit.collider.GetComponent<WorldMonsterStack>();

                Vector2Int targetPos = targetMonster != null ? targetMonster.GridPosition : (tile != null ? tile.GridPosition : new Vector2Int(-1, -1));

                if (targetPos != new Vector2Int(-1, -1))
                {
                    // Generate path preview
                    List<Vector2Int> path = _gridManager.FindPath(_activeHero.GridPosition, targetPos);
                    HighlightCurrentPath(path);

                    // Left Click to execute movement / battle encounter
                    if (Mouse.current.leftButton.wasPressedThisFrame && path != null && path.Count > 1)
                    {
                        List<Vector2Int> movePath = new List<Vector2Int>(path);
                        ClearPathHighlight();
                        StartCoroutine(_activeHero.MoveAlongPathCoroutine(movePath, () => {
                            UpdateUI();
                            CheckTileEncounter(_activeHero.GridPosition);
                        }));
                    }
                }
                else
                {
                    ClearPathHighlight();
                }
            }
            else
            {
                ClearPathHighlight();
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
            // 1. Direct monster stack tile
            WorldMonsterStack[] monsters = FindObjectsByType<WorldMonsterStack>(FindObjectsSortMode.None);
            foreach (var m in monsters)
            {
                if (m != null && m.Count > 0 && m.GridPosition == pos)
                {
                    return true;
                }
            }

            // 2. Guarded pickable resource
            WorldResourcePickable[] pickables = FindObjectsByType<WorldResourcePickable>(FindObjectsSortMode.None);
            foreach (var p in pickables)
            {
                if (p != null)
                {
                    int dx = Mathf.Abs(p.GridPosition.x - pos.x);
                    int dy = Mathf.Abs(p.GridPosition.y - pos.y);
                    if (dx <= 1 && dy <= 1 && CheckGuardedInteraction(p.GridPosition, out _))
                    {
                        return true;
                    }
                }
            }

            // 3. Guarded mine / factory
            WorldMine[] mines = FindObjectsByType<WorldMine>(FindObjectsSortMode.None);
            foreach (var mine in mines)
            {
                if (mine != null)
                {
                    int dx = Mathf.Abs(mine.GridPosition.x - pos.x);
                    int dy = Mathf.Abs(mine.GridPosition.y - pos.y);
                    if (dx <= 1 && dy <= 1 && CheckGuardedInteraction(mine.GridPosition, out _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool CheckTileEncounter(Vector2Int heroPos)
        {
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

            // 2. Check pickable resources (cardinal & diagonal 8-neighbor adjacency)
            WorldResourcePickable[] pickables = FindObjectsByType<WorldResourcePickable>(FindObjectsSortMode.None);
            foreach (var p in pickables)
            {
                if (p != null)
                {
                    int dx = Mathf.Abs(p.GridPosition.x - heroPos.x);
                    int dy = Mathf.Abs(p.GridPosition.y - heroPos.y);
                    if (dx <= 1 && dy <= 1) // On or adjacent
                    {
                        if (CheckGuardedInteraction(p.GridPosition, out var guardMonster))
                        {
                            Debug.Log($"[WorldMapManager] Resource at {p.GridPosition} is guarded by monster at {guardMonster.GridPosition}! Triggering battle!");
                            TriggerBattleEncounter(guardMonster);
                            return true; // Battle triggered!
                        }
                        else if (heroPos == p.GridPosition)
                        {
                            p.Collect(1);
                            return false;
                        }
                    }
                }
            }

            // 3. Check mines / factories (cardinal & diagonal 8-neighbor adjacency)
            WorldMine[] mines = FindObjectsByType<WorldMine>(FindObjectsSortMode.None);
            foreach (var mine in mines)
            {
                if (mine != null)
                {
                    int dx = Mathf.Abs(mine.GridPosition.x - heroPos.x);
                    int dy = Mathf.Abs(mine.GridPosition.y - heroPos.y);
                    if (dx <= 1 && dy <= 1) // On or adjacent
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
            }

            return false;
        }

        public void TriggerBattleEncounter(WorldMonsterStack monster)
        {
            if (monster == null) return;
            var manager = GameDataManager.GetOrCreateInstance();
            if (manager == null) return;

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
                else
                {
                    Debug.LogError($"[WorldMapManager] Could not load scene '{targetScene}'! Please open File > Build Settings in Unity and drag your Battle scene into 'Scenes In Build'!");
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
                var r = manager.player1Resources;
                if (resourceText != null)
                {
                    resourceText.text = $"Gold: {r.gold} | Wood: {r.wood} | Ore: {r.ore} | Gems: {r.gems}";
                }

                var h = manager.player1Hero;
                if (heroInfoText != null && h != null)
                {
                    heroInfoText.text = $"MP: {h.currentMovementPoints:F1} / {h.maxMovementPoints:F1}";
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
            if (_activeHero == null) _activeHero = FindFirstObjectByType<WorldHero>();
            var sheet = HeroCharacterSheetUI.GetOrCreateInstance();
            if (_activeHero != null && sheet != null)
            {
                sheet.ShowWindow(_activeHero.Data);
            }
        }

        /// <summary>
        /// Call this method from your custom End Turn button OnClick event in Unity Inspector!
        /// </summary>
        public void OnEndTurnClicked()
        {
            var manager = GameDataManager.GetOrCreateInstance();
            if (manager != null)
            {
                manager.ProcessDaySkip();
                UpdateUI();
                Debug.Log($"[WorldMapManager] End Turn: Advanced to Month {manager.currentMonth}, Week {manager.currentWeek}, Day {manager.currentDay}. Daily mine resources collected.");
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
