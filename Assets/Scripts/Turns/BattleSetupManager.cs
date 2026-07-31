using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using HommClone.Creatures;
using HommClone.Turns;
using HommClone.World;

namespace HommClone.Turns
{
    public class BattleSetupManager : MonoBehaviour
    {
        public enum SetupState { Lobby, Player1Deployment, Player2Deployment, Combat }

        [Header("Prefabs & Resources")]
        [SerializeField] private GameObject troopPrefab; // Standard base prefab for creature stacks
        [SerializeField] private List<CreatureData> availableCreatures = new List<CreatureData>();

        private SetupState _currentState = SetupState.Lobby;
        private bool _isPVP = false;
        private bool _isDraftMode = true;
        private int _currentBudget = 2000;

        // Draft Pools
        private List<CreatureData> _p1DraftedTypes = new List<CreatureData>();
        private List<int> _p1DraftedCounts = new List<int>();

        private List<CreatureData> _p2DraftedTypes = new List<CreatureData>();
        private List<int> _p2DraftedCounts = new List<int>();

        // Placed Stacks
        private List<CreatureStack> _p1Placed = new List<CreatureStack>();
        private List<CreatureStack> _p2Placed = new List<CreatureStack>();

        // Selected stack index in reserve for placement
        private int _selectedReserveIndex = -1;

        // References
        private TurnManager _turnManager;
        private Grid.GridManager _gridManager;
        private UI.BattleUIManager _uiManager;

        // UI GameObjects
        private GameObject _lobbyCanvas;
        private GameObject _deploymentHUD;
        private TextMeshProUGUI _deploymentTitleText;
        private Transform _reserveTrayContainer;
        private List<GameObject> _reserveButtons = new List<GameObject>();
        private bool _p1IsAtLowIndex = true;
        private bool _isVerticalLayout = false;

        public SetupState CurrentState => _currentState;
        public bool IsPVP => _isPVP;
        public bool IsDraftMode => _isDraftMode;
        public int CurrentBudget => _currentBudget;

        private void Start()
        {
            var audioManager = HommClone.Audio.AudioManager.GetOrCreateInstance();
            if (audioManager != null) audioManager.PlayCombatMusic();
            _turnManager = FindFirstObjectByType<TurnManager>();
            if (_turnManager != null)
            {
                var autoStartField = typeof(TurnManager).GetField("autoStartBattle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (autoStartField != null)
                {
                    autoStartField.SetValue(_turnManager, false);
                }
            }
            HideWorldMapVisuals();
            SpawnSidelineHeroes();
            _gridManager = FindFirstObjectByType<Grid.GridManager>();
            _uiManager = FindFirstObjectByType<UI.BattleUIManager>();

            if (_gridManager != null)
            {
                _isVerticalLayout = _gridManager.Width < _gridManager.Height;

                // Auto-detect if Player 1 is on the low index or high index side based on scene stacks before destroying them
                var sceneStacksAtStart = FindObjectsByType<CreatureStack>(FindObjectsSortMode.None);
                float p1Sum = 0f;
                int p1Count = 0;
                foreach (var stack in sceneStacksAtStart)
                {
                    if (stack.PlayerIndex == 1)
                    {
                        p1Sum += _isVerticalLayout ? stack.GridPosition.y : stack.GridPosition.x;
                        p1Count++;
                    }
                }
                if (p1Count > 0)
                {
                    float midPoint = _isVerticalLayout ? (_gridManager.Height / 2f) : (_gridManager.Width / 2f);
                    _p1IsAtLowIndex = (p1Sum / p1Count) < midPoint;
                    Debug.Log($"[Setup] Auto-detected Layout: {(_isVerticalLayout ? "VERTICAL" : "HORIZONTAL")}, Player 1 Position: {(_p1IsAtLowIndex ? "LOW INDEX" : "HIGH INDEX")}");
                }
            }

            // Auto-gather available creatures from existing stacks in the scene if list is empty
            if (availableCreatures.Count == 0)
            {
                var allSceneStacks = FindObjectsByType<CreatureStack>(FindObjectsSortMode.None);
                foreach (var stack in allSceneStacks)
                {
                    if (stack.Data != null && !availableCreatures.Contains(stack.Data))
                    {
                        availableCreatures.Add(stack.Data);
                    }
                }
            }

            // Check if entering battle from a World Map encounter or with Hero Army
            var gameData = HommClone.World.GameDataManager.GetOrCreateInstance();
            if (gameData != null)
            {
                _isPVP = gameData.isPvPBattle;
                gameData.InitializeStarterArmies(availableCreatures);

                bool hasPendingBattle = gameData.pendingBattleEnemyArmy != null && gameData.pendingBattleEnemyArmy.Count > 0;
                HeroData activeHeroForBattle = gameData.GetActiveHero();
                bool hasHeroArmy = activeHeroForBattle != null && activeHeroForBattle.army != null && activeHeroForBattle.army.Count > 0;

                if (hasPendingBattle || hasHeroArmy || _isPVP)
                {
                    _p1DraftedTypes.Clear();
                    _p1DraftedCounts.Clear();
                    _p2DraftedTypes.Clear();
                    _p2DraftedCounts.Clear();

                    if (_isPVP)
                    {
                        // PvP Combat: Left side is always Player 1, Right side is always Player 2
                        foreach (var slot in gameData.player1Hero.army)
                        {
                            if (slot != null && slot.creatureData != null && slot.count > 0)
                            {
                                _p1DraftedTypes.Add(slot.creatureData);
                                _p1DraftedCounts.Add(slot.count);
                            }
                        }
                        foreach (var slot in gameData.player2Hero.army)
                        {
                            if (slot != null && slot.creatureData != null && slot.count > 0)
                            {
                                _p2DraftedTypes.Add(slot.creatureData);
                                _p2DraftedCounts.Add(slot.count);
                            }
                        }
                    }
                    else
                    {
                        // PvE Combat: Left side is Active Hero, Right side is AI Monster
                        if (hasHeroArmy)
                        {
                            foreach (var slot in activeHeroForBattle.army)
                            {
                                if (slot != null && slot.creatureData != null && slot.count > 0)
                                {
                                    _p1DraftedTypes.Add(slot.creatureData);
                                    _p1DraftedCounts.Add(slot.count);
                                }
                            }
                        }

                        if (hasPendingBattle)
                        {
                            foreach (var slot in gameData.pendingBattleEnemyArmy)
                            {
                                if (slot != null && slot.creatureData != null && slot.count > 0)
                                {
                                    _p2DraftedTypes.Add(slot.creatureData);
                                    _p2DraftedCounts.Add(slot.count);
                                }
                            }
                        }
                    }

                    Debug.Log($"[BattleSetupManager] World Map Encounter detected! (PvP: {_isPVP}) P1 Stacks: {_p1DraftedTypes.Count}, P2 Stacks: {_p2DraftedTypes.Count}. Bypassing Draft Lobby!");

                    // Start Deployment phase directly without Draft Lobby UI
                    StartDeployment();
                    return;
                }
            }

            // Fallback: Enter standard Lobby UI if no World Map army exists
            _currentState = SetupState.Lobby;
            CreateLobbyUI();
        }

        private void Update()
        {
            if (_currentState == SetupState.Lobby || _currentState == SetupState.Combat)
                return;

            HandleDeploymentInput();
        }

        #region Lobby UI Generation
        private void CreateLobbyUI()
        {
            // Create a temporary canvas for the Setup Lobby
            _lobbyCanvas = new GameObject("BattleSetupLobbyCanvas");
            Canvas canvas = _lobbyCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _lobbyCanvas.AddComponent<CanvasScaler>();
            _lobbyCanvas.AddComponent<GraphicRaycaster>();

            // Background
            GameObject bgObj = new GameObject("Bg");
            bgObj.transform.SetParent(_lobbyCanvas.transform, false);
            Image bg = bgObj.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.1f, 0.98f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(_lobbyCanvas.transform, false);
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "<b>TACTICAL BATTLE SIMULATOR LOBBY</b>";
            title.fontSize = 24;
            title.color = Color.yellow;
            title.alignment = TextAlignmentOptions.Center;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -20f);
            titleRect.sizeDelta = new Vector2(500f, 40f);

            // Controls Panel
            GameObject controlsObj = new GameObject("ControlsPanel");
            controlsObj.transform.SetParent(_lobbyCanvas.transform, false);
            RectTransform ctrlRect = controlsObj.AddComponent<RectTransform>();
            ctrlRect.anchorMin = new Vector2(0f, 0f);
            ctrlRect.anchorMax = new Vector2(1f, 1f);
            ctrlRect.offsetMin = new Vector2(50f, 100f);
            ctrlRect.offsetMax = new Vector2(-50f, -100f);

            // Add PVP vs AI Toggle Button
            CreateToggleButton(controlsObj, "ModeToggle", new Vector2(-150f, 150f), "Opponent: BOT (AI)", () => {
                _isPVP = !_isPVP;
                return _isPVP ? "Opponent: HUMAN (PVP)" : "Opponent: BOT (AI)";
            });

            // Add Preset vs Draft Toggle Button
            CreateToggleButton(controlsObj, "DraftToggle", new Vector2(150f, 150f), "Game Mode: DRAFT POOL", () => {
                _isDraftMode = !_isDraftMode;
                return _isDraftMode ? "Game Mode: DRAFT POOL" : "Game Mode: PRESET SCENE";
            });

            // Available Creatures List container
            GameObject shopObj = new GameObject("CreatureShop");
            shopObj.transform.SetParent(controlsObj.transform, false);
            RectTransform shopRect = shopObj.AddComponent<RectTransform>();
            shopRect.anchorMin = new Vector2(0f, 0.1f);
            shopRect.anchorMax = new Vector2(0.5f, 0.7f);
            shopRect.sizeDelta = Vector2.zero;

            Image shopBg = shopObj.AddComponent<Image>();
            shopBg.color = new Color(0.15f, 0.15f, 0.18f, 0.6f);

            // Add Title to shop
            GameObject shopTitle = new GameObject("ShopTitle");
            shopTitle.transform.SetParent(shopObj.transform, false);
            var sTitleText = shopTitle.AddComponent<TextMeshProUGUI>();
            sTitleText.text = "<b>Available Units (Click to Draft)</b>\n<size=75%>P1 clicks draft left, P2/Bot drafts right</size>";
            sTitleText.fontSize = 12;
            sTitleText.color = Color.white;
            sTitleText.alignment = TextAlignmentOptions.Center;
            RectTransform sTitleRect = shopTitle.GetComponent<RectTransform>();
            sTitleRect.anchorMin = new Vector2(0f, 1f);
            sTitleRect.anchorMax = new Vector2(1f, 1f);
            sTitleRect.pivot = new Vector2(0.5f, 1f);
            sTitleRect.anchoredPosition = new Vector2(0f, -5f);
            sTitleRect.sizeDelta = new Vector2(0f, 35f);

            // Grid for creatures in shop
            GameObject gridObj = new GameObject("Grid");
            gridObj.transform.SetParent(shopObj.transform, false);
            RectTransform gridRect = gridObj.AddComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = new Vector2(10f, 10f);
            gridRect.offsetMax = new Vector2(-10f, -40f);

            GridLayoutGroup glg = gridObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(110f, 50f);
            glg.spacing = new Vector2(5f, 5f);
            glg.childAlignment = TextAnchor.UpperLeft;

            // Draft Info Panel (displays current selections and spent budget)
            GameObject infoObj = new GameObject("DraftInfoPanel");
            infoObj.transform.SetParent(controlsObj.transform, false);
            RectTransform infoRect = infoObj.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.55f, 0.1f);
            infoRect.anchorMax = new Vector2(1f, 0.7f);
            infoRect.sizeDelta = Vector2.zero;

            Image infoBg = infoObj.AddComponent<Image>();
            infoBg.color = new Color(0.15f, 0.15f, 0.18f, 0.6f);

            GameObject infoTxtObj = new GameObject("Text");
            infoTxtObj.transform.SetParent(infoObj.transform, false);
            TextMeshProUGUI infoTxt = infoTxtObj.AddComponent<TextMeshProUGUI>();
            infoTxt.fontSize = 12;
            infoTxt.color = Color.white;
            infoTxt.text = "<b>Army Draft Selection:</b>\n\nPlayer 1: Empty\nPlayer 2: Empty";

            RectTransform infoTxtRect = infoTxtObj.GetComponent<RectTransform>();
            infoTxtRect.anchorMin = Vector2.zero;
            infoTxtRect.anchorMax = Vector2.one;
            infoTxtRect.offsetMin = new Vector2(15f, 15f);
            infoTxtRect.offsetMax = new Vector2(-15f, -15f);

            // Populate Shop list
            foreach (var creature in availableCreatures)
            {
                if (creature == null) continue;

                GameObject itemObj = new GameObject(creature.CreatureName);
                itemObj.transform.SetParent(gridObj.transform, false);
                Image itemBg = itemObj.AddComponent<Image>();
                itemBg.color = new Color(0.2f, 0.2f, 0.22f, 1f);

                Button itemBtn = itemObj.AddComponent<Button>();

                GameObject lblObj = new GameObject("Label");
                lblObj.transform.SetParent(itemObj.transform, false);
                TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
                lbl.fontSize = 9;
                lbl.color = Color.white;
                lbl.alignment = TextAlignmentOptions.Center;
                lbl.text = $"<b>{creature.CreatureName}</b>\nCost: {creature.AIValue}";

                RectTransform lblRect = lblObj.GetComponent<RectTransform>();
                lblRect.anchorMin = Vector2.zero;
                lblRect.anchorMax = Vector2.one;
                lblRect.sizeDelta = Vector2.zero;

                itemBtn.onClick.AddListener(() => {
                    DraftCreature(creature, infoTxt);
                });
            }

            // Start Combat Button
            GameObject startBtnObj = new GameObject("StartButton");
            startBtnObj.transform.SetParent(_lobbyCanvas.transform, false);
            RectTransform startRect = startBtnObj.AddComponent<RectTransform>();
            startRect.anchorMin = new Vector2(0.5f, 0f);
            startRect.anchorMax = new Vector2(0.5f, 0f);
            startRect.pivot = new Vector2(0.5f, 0f);
            startRect.anchoredPosition = new Vector2(0f, 30f);
            startRect.sizeDelta = new Vector2(180f, 40f);

            Image startImg = startBtnObj.AddComponent<Image>();
            startImg.color = new Color(0.12f, 0.35f, 0.12f, 1f);

            Button startBtn = startBtnObj.AddComponent<Button>();
            startBtn.onClick.AddListener(() => {
                StartDeployment();
            });

            GameObject startLblObj = new GameObject("Label");
            startLblObj.transform.SetParent(startBtnObj.transform, false);
            TextMeshProUGUI startLbl = startLblObj.AddComponent<TextMeshProUGUI>();
            startLbl.text = "<b>CONFIRM DRAFT</b>";
            startLbl.fontSize = 14;
            startLbl.color = Color.white;
            startLbl.alignment = TextAlignmentOptions.Center;

            RectTransform startLblRect = startLblObj.GetComponent<RectTransform>();
            startLblRect.anchorMin = Vector2.zero;
            startLblRect.anchorMax = Vector2.one;
            startLblRect.sizeDelta = Vector2.zero;
        }

        private void CreateToggleButton(GameObject parent, string name, Vector2 anchoredPos, string initialText, System.Func<string> onToggle)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent.transform, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.85f);
            rect.anchorMax = new Vector2(0.5f, 0.85f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(200f, 30f);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.28f, 1f);

            Button btn = btnObj.AddComponent<Button>();

            GameObject lblObj = new GameObject("Label");
            lblObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
            lbl.fontSize = 11;
            lbl.color = Color.white;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.text = $"<b>{initialText}</b>";

            RectTransform lblRect = lblObj.GetComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.one;
            lblRect.sizeDelta = Vector2.zero;

            btn.onClick.AddListener(() => {
                string nextText = onToggle.Invoke();
                lbl.text = $"<b>{nextText}</b>";
            });
        }

        private GameObject _activeModal = null;

        private void DraftCreature(CreatureData data, TextMeshProUGUI infoText)
        {
            if (!_isDraftMode || data == null) return;

            bool isCtrlPressed = (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.ctrlKey.isPressed);
            bool isAltPressed = (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.altKey.isPressed);
            bool draftForP2 = (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.shiftKey.isPressed);

            // Fast shortcuts: Ctrl+Click = +10, Alt+Click = MAX, standard Click = Open Quantity Modal
            if (isCtrlPressed)
            {
                DraftCreatureAmount(data, 10, draftForP2, infoText);
            }
            else if (isAltPressed)
            {
                List<CreatureData> types = draftForP2 ? _p2DraftedTypes : _p1DraftedTypes;
                List<int> counts = draftForP2 ? _p2DraftedCounts : _p1DraftedCounts;
                int totalSpent = 0;
                for (int i = 0; i < types.Count; i++) totalSpent += types[i].AIValue * counts[i];
                int maxAffordable = data.AIValue > 0 ? Mathf.Max(0, _currentBudget - totalSpent) / data.AIValue : 0;
                if (maxAffordable > 0) DraftCreatureAmount(data, maxAffordable, draftForP2, infoText);
            }
            else
            {
                OpenQuantityModal(data, infoText);
            }
        }

        private void OpenQuantityModal(CreatureData creature, TextMeshProUGUI infoText)
        {
            if (_activeModal != null) Destroy(_activeModal);

            bool draftForP2 = (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.shiftKey.isPressed);
            List<CreatureData> types = draftForP2 ? _p2DraftedTypes : _p1DraftedTypes;
            List<int> counts = draftForP2 ? _p2DraftedCounts : _p1DraftedCounts;

            int totalSpent = 0;
            for (int i = 0; i < types.Count; i++)
            {
                totalSpent += types[i].AIValue * counts[i];
            }
            int remainingBudget = Mathf.Max(0, _currentBudget - totalSpent);
            int maxAffordable = creature.AIValue > 0 ? remainingBudget / creature.AIValue : 0;

            if (maxAffordable <= 0)
            {
                Debug.LogWarning("Not enough power budget to draft this creature!");
                return;
            }

            int selectedQuantity = 0;

            _activeModal = new GameObject("QuantityModal");
            _activeModal.transform.SetParent(_lobbyCanvas.transform, false);

            RectTransform modalRect = _activeModal.AddComponent<RectTransform>();
            modalRect.anchorMin = new Vector2(0.5f, 0.5f);
            modalRect.anchorMax = new Vector2(0.5f, 0.5f);
            modalRect.pivot = new Vector2(0.5f, 0.5f);
            modalRect.sizeDelta = new Vector2(440f, 280f);

            Image bg = _activeModal.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.13f, 0.96f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(_activeModal.transform, false);
            TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.fontSize = 15;
            titleTxt.color = Color.gold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.text = $"<b>Draft {creature.CreatureName}</b>";
            titleTxt.raycastTarget = false;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.83f);
            titleRect.anchorMax = new Vector2(1f, 0.97f);
            titleRect.sizeDelta = Vector2.zero;

            // Info Label
            GameObject countObj = new GameObject("CountLabel");
            countObj.transform.SetParent(_activeModal.transform, false);
            TextMeshProUGUI countTxt = countObj.AddComponent<TextMeshProUGUI>();
            countTxt.fontSize = 13;
            countTxt.color = Color.white;
            countTxt.alignment = TextAlignmentOptions.Center;
            countTxt.raycastTarget = false;

            void UpdateModalText()
            {
                int cost = selectedQuantity * creature.AIValue;
                string playerStr = draftForP2 ? "Player 2 (Bot/P2)" : "Player 1";
                countTxt.text = $"Target: <b>{playerStr}</b>\nQuantity: <color=yellow><b>{selectedQuantity}</b></color> / {maxAffordable}\nTotal Cost: <b>{cost}</b> / {remainingBudget} Power";
            }
            UpdateModalText();

            RectTransform countRect = countObj.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.04f, 0.52f);
            countRect.anchorMax = new Vector2(0.96f, 0.82f);
            countRect.sizeDelta = Vector2.zero;

            // Preset Buttons Container
            GameObject btnRow = new GameObject("BtnRow");
            btnRow.transform.SetParent(_activeModal.transform, false);
            HorizontalLayoutGroup layout = btnRow.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            RectTransform rowRect = btnRow.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.04f, 0.28f);
            rowRect.anchorMax = new Vector2(0.96f, 0.48f);
            rowRect.sizeDelta = Vector2.zero;

            void CreateStepButton(string label, System.Action onClick)
            {
                GameObject bObj = new GameObject(label);
                bObj.transform.SetParent(btnRow.transform, false);
                Image bImg = bObj.AddComponent<Image>();
                bImg.color = new Color(0.22f, 0.24f, 0.3f, 1f);
                Button bBtn = bObj.AddComponent<Button>();
                bBtn.onClick.AddListener(() => onClick());

                GameObject tObj = new GameObject("Text");
                tObj.transform.SetParent(bObj.transform, false);
                TextMeshProUGUI tTxt = tObj.AddComponent<TextMeshProUGUI>();
                tTxt.fontSize = 12;
                tTxt.color = Color.white;
                tTxt.alignment = TextAlignmentOptions.Center;
                tTxt.text = $"<b>{label}</b>";
                tTxt.raycastTarget = false;
                RectTransform tRect = tObj.GetComponent<RectTransform>();
                tRect.anchorMin = Vector2.zero;
                tRect.anchorMax = Vector2.one;
                tRect.sizeDelta = Vector2.zero;
            }

            CreateStepButton("+1", () => { selectedQuantity = Mathf.Clamp(selectedQuantity + 1, 1, maxAffordable); UpdateModalText(); });
            CreateStepButton("+5", () => { selectedQuantity = Mathf.Clamp(selectedQuantity + 5, 1, maxAffordable); UpdateModalText(); });
            CreateStepButton("+10", () => { selectedQuantity = Mathf.Clamp(selectedQuantity + 10, 1, maxAffordable); UpdateModalText(); });
            CreateStepButton("+50", () => { selectedQuantity = Mathf.Clamp(selectedQuantity + 50, 1, maxAffordable); UpdateModalText(); });
            CreateStepButton("MAX", () => { selectedQuantity = maxAffordable; UpdateModalText(); });

            // Confirm & Cancel Buttons
            GameObject confirmBtnObj = new GameObject("ConfirmBtn");
            confirmBtnObj.transform.SetParent(_activeModal.transform, false);
            RectTransform confirmRect = confirmBtnObj.AddComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.04f, 0.05f);
            confirmRect.anchorMax = new Vector2(0.48f, 0.23f);
            confirmRect.sizeDelta = Vector2.zero;

            Image confirmImg = confirmBtnObj.AddComponent<Image>();
            confirmImg.color = new Color(0.15f, 0.48f, 0.15f, 1f);
            Button confirmBtn = confirmBtnObj.AddComponent<Button>();
            confirmBtn.onClick.AddListener(() => {
                DraftCreatureAmount(creature, selectedQuantity, draftForP2, infoText);
                Destroy(_activeModal);
            });

            GameObject cLblObj = new GameObject("Text");
            cLblObj.transform.SetParent(confirmBtnObj.transform, false);
            TextMeshProUGUI cLbl = cLblObj.AddComponent<TextMeshProUGUI>();
            cLbl.fontSize = 12;
            cLbl.color = Color.white;
            cLbl.alignment = TextAlignmentOptions.Center;
            cLbl.text = "<b>CONFIRM</b>";
            cLbl.raycastTarget = false;
            RectTransform cRect = cLblObj.GetComponent<RectTransform>();
            cRect.anchorMin = Vector2.zero;
            cRect.anchorMax = Vector2.one;
            cRect.sizeDelta = Vector2.zero;

            GameObject cancelBtnObj = new GameObject("CancelBtn");
            cancelBtnObj.transform.SetParent(_activeModal.transform, false);
            RectTransform cancelRect = cancelBtnObj.AddComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.52f, 0.05f);
            cancelRect.anchorMax = new Vector2(0.96f, 0.23f);
            cancelRect.sizeDelta = Vector2.zero;

            Image cancelImg = cancelBtnObj.AddComponent<Image>();
            cancelImg.color = new Color(0.48f, 0.15f, 0.15f, 1f);
            Button cancelBtn = cancelBtnObj.AddComponent<Button>();
            cancelBtn.onClick.AddListener(() => {
                Destroy(_activeModal);
            });

            GameObject xLblObj = new GameObject("Text");
            xLblObj.transform.SetParent(cancelBtnObj.transform, false);
            TextMeshProUGUI xLbl = xLblObj.AddComponent<TextMeshProUGUI>();
            xLbl.fontSize = 12;
            xLbl.color = Color.white;
            xLbl.alignment = TextAlignmentOptions.Center;
            xLbl.text = "<b>CANCEL</b>";
            xLbl.raycastTarget = false;
            RectTransform xRect = xLblObj.GetComponent<RectTransform>();
            xRect.anchorMin = Vector2.zero;
            xRect.anchorMax = Vector2.one;
            xRect.sizeDelta = Vector2.zero;
        }

        private void DraftCreatureAmount(CreatureData data, int amount, bool draftForP2, TextMeshProUGUI infoText)
        {
            if (!_isDraftMode || amount <= 0) return;

            List<CreatureData> types = draftForP2 ? _p2DraftedTypes : _p1DraftedTypes;
            List<int> counts = draftForP2 ? _p2DraftedCounts : _p1DraftedCounts;

            int cost = data.AIValue * amount;
            int totalSpent = 0;
            for (int i = 0; i < types.Count; i++)
            {
                totalSpent += types[i].AIValue * counts[i];
            }

            if (totalSpent + cost > _currentBudget)
            {
                Debug.LogWarning("Not enough power budget to draft this quantity!");
                return;
            }

            int index = types.IndexOf(data);
            if (index >= 0)
            {
                counts[index] += amount;
            }
            else
            {
                types.Add(data);
                counts.Add(amount);
            }

            UpdateDraftInfo(infoText);
        }

        private void UpdateDraftInfo(TextMeshProUGUI infoText)
        {
            string info = "<b>Army Draft Selection:</b>\n\n";

            int p1Spent = 0;
            info += "<b>Player 1 (Left Side):</b>\n";
            for (int i = 0; i < _p1DraftedTypes.Count; i++)
            {
                int totalCost = _p1DraftedTypes[i].AIValue * _p1DraftedCounts[i];
                info += $"- {_p1DraftedTypes[i].CreatureName} x{_p1DraftedCounts[i]} ({totalCost} Power)\n";
                p1Spent += totalCost;
            }
            info += $"Spent Budget: {p1Spent}/{_currentBudget} Power\n\n";

            int p2Spent = 0;
            info += "<b>Player 2 (Right Side / Bot):</b>\n";
            for (int i = 0; i < _p2DraftedTypes.Count; i++)
            {
                int totalCost = _p2DraftedTypes[i].AIValue * _p2DraftedCounts[i];
                info += $"- {_p2DraftedTypes[i].CreatureName} x{_p2DraftedCounts[i]} ({totalCost} Power)\n";
                p2Spent += totalCost;
            }
            info += $"Spent Budget: {p2Spent}/{_currentBudget} Power\n";
            info += "\n<i>*Hold SHIFT while clicking unit to draft for Player 2*</i>";

            infoText.text = info;
        }
        #endregion

        #region Deployment Orchestration
        private void StartDeployment()
        {
            // Close Lobby UI Canvas
            if (_lobbyCanvas != null)
            {
                Destroy(_lobbyCanvas);
            }

            // If Preset mode, we gather all active stacks in the scene and partition them.
            // Then we keep their coordinates, hide them, and allow repositioning.
            if (!_isDraftMode)
            {
                _p1DraftedTypes.Clear();
                _p1DraftedCounts.Clear();
                _p2DraftedTypes.Clear();
                _p2DraftedCounts.Clear();

                var sceneStacks = FindObjectsByType<CreatureStack>(FindObjectsSortMode.None);
                foreach (var stack in sceneStacks)
                {
                    if (stack.PlayerIndex == 1)
                    {
                        _p1DraftedTypes.Add(stack.Data);
                        _p1DraftedCounts.Add(stack.Count);
                        Destroy(stack.gameObject); // temporary wipe scene to redeploy
                    }
                    else
                    {
                        _p2DraftedTypes.Add(stack.Data);
                        _p2DraftedCounts.Add(stack.Count);
                        Destroy(stack.gameObject); // temporary wipe scene to redeploy
                    }
                }
            }

            // Initialize turn manager structures
            if (_turnManager != null)
            {
                _turnManager.ClearParticipants();
            }

            // Spawn sideline heroes at the very start of deployment
            SpawnSidelineHeroes();

            // Create deployment HUD overlays
            CreateDeploymentHUD();

            // Transition to Player 1 Deployment
            _currentState = SetupState.Player1Deployment;
            _selectedReserveIndex = -1;
            SetFactionVisibility(1, true);
            SetFactionVisibility(2, false);
            RefreshDeploymentTray();
        }

        private void CreateDeploymentHUD()
        {
            _deploymentHUD = new GameObject("DeploymentHUDCanvas");
            Canvas canvas = _deploymentHUD.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _deploymentHUD.AddComponent<CanvasScaler>();
            _deploymentHUD.AddComponent<GraphicRaycaster>();

            // Title Banner
            GameObject bannerObj = new GameObject("Banner");
            bannerObj.transform.SetParent(_deploymentHUD.transform, false);
            Image bannerImg = bannerObj.AddComponent<Image>();
            bannerImg.color = new Color(0.1f, 0.1f, 0.12f, 0.9f);
            
            RectTransform bannerRect = bannerObj.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 1f);
            bannerRect.anchorMax = new Vector2(0.5f, 1f);
            bannerRect.pivot = new Vector2(0.5f, 1f);
            bannerRect.anchoredPosition = new Vector2(0f, -10f);
            bannerRect.sizeDelta = new Vector2(400f, 40f);

            GameObject titleObj = new GameObject("Text");
            titleObj.transform.SetParent(bannerObj.transform, false);
            _deploymentTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            _deploymentTitleText.fontSize = 14;
            _deploymentTitleText.color = Color.white;
            _deploymentTitleText.alignment = TextAlignmentOptions.Center;
            _deploymentTitleText.text = "<b>PLAYER 1 DEPLOYMENT PHASE</b>";

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.sizeDelta = Vector2.zero;

            // Reserve Units Tray (Bottom container)
            GameObject trayObj = new GameObject("ReserveTray");
            trayObj.transform.SetParent(_deploymentHUD.transform, false);
            Image trayImg = trayObj.AddComponent<Image>();
            trayImg.color = new Color(0.1f, 0.1f, 0.12f, 0.9f);

            RectTransform trayRect = trayObj.GetComponent<RectTransform>();
            trayRect.anchorMin = new Vector2(0.5f, 0f);
            trayRect.anchorMax = new Vector2(0.5f, 0f);
            trayRect.pivot = new Vector2(0.5f, 0f);
            trayRect.anchoredPosition = new Vector2(0f, 10f);
            trayRect.sizeDelta = new Vector2(700f, 80f);

            _reserveTrayContainer = trayObj.transform;

            // Ready/Next Button
            GameObject readyBtnObj = new GameObject("ReadyButton");
            readyBtnObj.transform.SetParent(_deploymentHUD.transform, false);
            RectTransform readyRect = readyBtnObj.AddComponent<RectTransform>();
            readyRect.anchorMin = new Vector2(1f, 0f);
            readyRect.anchorMax = new Vector2(1f, 0f);
            readyRect.pivot = new Vector2(1f, 0f);
            readyRect.anchoredPosition = new Vector2(-20f, 20f);
            readyRect.sizeDelta = new Vector2(130f, 45f);

            Image readyImg = readyBtnObj.AddComponent<Image>();
            readyImg.color = new Color(0.12f, 0.35f, 0.12f, 1f);

            Button readyBtn = readyBtnObj.AddComponent<Button>();
            readyBtn.onClick.AddListener(() => {
                OnPlayerReady();
            });

            GameObject readyLblObj = new GameObject("Label");
            readyLblObj.transform.SetParent(readyBtnObj.transform, false);
            TextMeshProUGUI readyLbl = readyLblObj.AddComponent<TextMeshProUGUI>();
            readyLbl.text = "<b>CONFIRM PLACEMENT</b>";
            readyLbl.fontSize = 10;
            readyLbl.color = Color.white;
            readyLbl.alignment = TextAlignmentOptions.Center;

            RectTransform readyLblRect = readyLblObj.GetComponent<RectTransform>();
            readyLblRect.anchorMin = Vector2.zero;
            readyLblRect.anchorMax = Vector2.one;
            readyLblRect.sizeDelta = Vector2.zero;
        }

        private void RefreshDeploymentTray()
        {
            // Clear old buttons
            foreach (var btn in _reserveButtons)
            {
                Destroy(btn);
            }
            _reserveButtons.Clear();

            List<CreatureData> types = (_currentState == SetupState.Player1Deployment) ? _p1DraftedTypes : _p2DraftedTypes;
            List<int> counts = (_currentState == SetupState.Player1Deployment) ? _p1DraftedCounts : _p2DraftedCounts;

            for (int i = 0; i < types.Count; i++)
            {
                if (counts[i] <= 0) continue;

                int index = i;
                GameObject btnObj = new GameObject($"ReserveItem_{i}");
                btnObj.transform.SetParent(_reserveTrayContainer, false);
                _reserveButtons.Add(btnObj);

                RectTransform rect = btnObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(15f + i * 115f, 0f);
                rect.sizeDelta = new Vector2(105f, 55f);

                Image img = btnObj.AddComponent<Image>();
                img.color = (_selectedReserveIndex == index) ? new Color(0.35f, 0.35f, 0.15f, 1f) : new Color(0.2f, 0.2f, 0.22f, 1f);

                Button btn = btnObj.AddComponent<Button>();
                btn.onClick.AddListener(() => {
                    _selectedReserveIndex = index;
                    RefreshDeploymentTray();
                });

                GameObject lblObj = new GameObject("Label");
                lblObj.transform.SetParent(btnObj.transform, false);
                TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
                lbl.fontSize = 8;
                lbl.color = Color.white;
                lbl.alignment = TextAlignmentOptions.Center;
                lbl.text = $"<b>{types[i].CreatureName}</b>\nSize: {counts[i]}";

                RectTransform lblRect = lblObj.GetComponent<RectTransform>();
                lblRect.anchorMin = Vector2.zero;
                lblRect.anchorMax = Vector2.one;
                lblRect.sizeDelta = Vector2.zero;
            }
        }

        private void OnPlayerReady()
        {
            int placedStacksCount = (_currentState == SetupState.Player1Deployment) ? _p1Placed.Count : _p2Placed.Count;
            if (placedStacksCount == 0)
            {
                Debug.LogWarning("You must place at least one unit before confirming!");
                return;
            }

            if (_currentState == SetupState.Player1Deployment)
            {
                // Transition to Player 2
                _currentState = SetupState.Player2Deployment;
                _selectedReserveIndex = -1;

                // Fog of War: Hide Player 1 units, show Player 2 starting highlights
                SetFactionVisibility(1, false);
                SetFactionVisibility(2, true);

                if (!_isPVP)
                {
                    // Bot AI automatic placement
                    DeployBotAI();
                    OnPlayerReady(); // Immediately skip to combat!
                }
                else
                {
                    _deploymentTitleText.text = "<b>PLAYER 2 DEPLOYMENT PHASE</b>";
                    RefreshDeploymentTray();
                }
            }
            else if (_currentState == SetupState.Player2Deployment)
            {
                // Both are deployed! Commencing Combat
                _currentState = SetupState.Combat;

                // Close deployment UI
                if (_deploymentHUD != null)
                {
                    Destroy(_deploymentHUD);
                }

                // Make all armies visible again
                SetFactionVisibility(1, true);
                SetFactionVisibility(2, true);

                // Disable/Enable BattleAIManager based on PVP settings
                var aiManager = FindFirstObjectByType<AI.BattleAIManager>();
                if (aiManager != null)
                {
                    var aiField = typeof(AI.BattleAIManager).GetField("aiEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (aiField != null)
                    {
                        aiField.SetValue(aiManager, !_isPVP);
                    }
                }

                // Register sidelined Heroes to the timeline if they exist in scene
                var heroes = FindObjectsByType<Heroes.Hero>(FindObjectsSortMode.None);
                foreach (var hero in heroes)
                {
                    if (hero != null && _turnManager != null)
                    {
                        _turnManager.RegisterParticipant(hero);
                    }
                }

                // Register all placed stacks
                foreach (var stack in _p1Placed)
                {
                    if (stack != null && _turnManager != null)
                    {
                        _turnManager.RegisterParticipant(stack);
                    }
                }
                foreach (var stack in _p2Placed)
                {
                    if (stack != null && _turnManager != null)
                    {
                        _turnManager.RegisterParticipant(stack);
                    }
                }

                // Trigger combat start
                if (_turnManager != null)
                {
                    _turnManager.StartBattle();
                }
            }
        }

        private void DeployBotAI()
        {
            if (_gridManager == null) return;

            int gridWidth = _gridManager.Width;
            int gridHeight = _gridManager.Height;

            // Gather all available valid coordinates for Player 2 start
            List<Vector2Int> validTiles = new List<Vector2Int>();
            bool botLow = !_p1IsAtLowIndex;

            if (_isVerticalLayout)
            {
                int minY = botLow ? 0 : (gridHeight - 2);
                int maxY = botLow ? 2 : gridHeight;
                for (int y = minY; y < maxY; y++)
                {
                    for (int x = 0; x < gridWidth; x++)
                    {
                        validTiles.Add(new Vector2Int(x, y));
                    }
                }
            }
            else
            {
                int minX = botLow ? 0 : (gridWidth - 2);
                int maxX = botLow ? 2 : gridWidth;
                for (int x = minX; x < maxX; x++)
                {
                    for (int y = 0; y < gridHeight; y++)
                    {
                        validTiles.Add(new Vector2Int(x, y));
                    }
                }
            }

            // Shuffle valid start locations
            for (int i = 0; i < validTiles.Count; i++)
            {
                Vector2Int temp = validTiles[i];
                int randomIndex = Random.Range(i, validTiles.Count);
                validTiles[i] = validTiles[randomIndex];
                validTiles[randomIndex] = temp;
            }

            int tileIndex = 0;
            for (int i = 0; i < _p2DraftedTypes.Count; i++)
            {
                int stackCount = _p2DraftedCounts[i];
                if (stackCount <= 0) continue;

                if (tileIndex >= validTiles.Count)
                {
                    Debug.LogWarning("[Bot Setup] Out of valid deployment tiles for bot stacks!");
                    break;
                }

                Vector2Int spawnPos = validTiles[tileIndex++];
                CreatureData data = _p2DraftedTypes[i];

                SpawnStackAt(data, stackCount, 2, spawnPos);
                _p2DraftedCounts[i] = 0; // completely placed
            }
        }

        private void HandleDeploymentInput()
        {
            if (_gridManager == null || Camera.main == null || UnityEngine.InputSystem.Mouse.current == null)
                return;

            // Highlight valid starting columns in light blue
            HighlightValidStartingColumns();

            // Right-click picks up an already placed unit of the current player to return it to reserve
            if (UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
            {
                Ray ray = Camera.main.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    CreatureStack clickedStack = hit.collider.GetComponentInParent<CreatureStack>();
                    if (clickedStack != null)
                    {
                        int activePlayer = (_currentState == SetupState.Player1Deployment) ? 1 : 2;
                        if (clickedStack.PlayerIndex == activePlayer)
                        {
                            ReturnStackToReserve(clickedStack);
                        }
                    }
                }
            }

            // Left-click places selected reserve stack on a valid starting column tile
            if (_selectedReserveIndex >= 0 && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = Camera.main.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    Grid.Tile tile = hit.collider.GetComponentInParent<Grid.Tile>();
                    if (tile != null)
                    {
                        int activePlayer = (_currentState == SetupState.Player1Deployment) ? 1 : 2;
                        List<CreatureData> types = (activePlayer == 1) ? _p1DraftedTypes : _p2DraftedTypes;
                        List<int> counts = (activePlayer == 1) ? _p1DraftedCounts : _p2DraftedCounts;

                        CreatureData selectedData = types[_selectedReserveIndex];
                        int selectedCount = counts[_selectedReserveIndex];

                        if (selectedCount > 0)
                        {
                            List<Vector2Int> targetFootprint = new List<Vector2Int> { tile.GridPosition };
                            if (selectedData.IsLarge)
                            {
                                targetFootprint.Add(new Vector2Int(tile.GridPosition.x + 1, tile.GridPosition.y));
                                targetFootprint.Add(new Vector2Int(tile.GridPosition.x, tile.GridPosition.y + 1));
                                targetFootprint.Add(new Vector2Int(tile.GridPosition.x + 1, tile.GridPosition.y + 1));
                            }

                            bool canPlace = true;
                            foreach (Vector2Int pos in targetFootprint)
                            {
                                if (_gridManager.GetTileAt(pos) == null || !IsValidStartingTile(pos, activePlayer) || IsTileOccupiedDuringSetup(pos))
                                {
                                    canPlace = false;
                                    break;
                                }
                            }

                            if (canPlace)
                            {
                                SpawnStackAt(selectedData, selectedCount, activePlayer, tile.GridPosition);
                                counts[_selectedReserveIndex] = 0; // placed
                                _selectedReserveIndex = -1; // reset selection
                                RefreshDeploymentTray();
                            }
                        }
                    }
                }
            }
        }

        private bool IsValidStartingTile(Vector2Int pos, int playerIndex)
        {
            if (_gridManager == null) return false;
            int width = _gridManager.Width;
            int height = _gridManager.Height;

            // If Player 1 is on the left/bottom: P1 gets cols/rows 0, 1; P2 gets width-2, width-1 / height-2, height-1.
            bool checkLow = (playerIndex == 1) ? _p1IsAtLowIndex : !_p1IsAtLowIndex;

            if (_isVerticalLayout)
            {
                if (checkLow)
                {
                    return pos.y == 0 || pos.y == 1;
                }
                else
                {
                    return pos.y == height - 1 || pos.y == height - 2;
                }
            }
            else
            {
                if (checkLow)
                {
                    return pos.x == 0 || pos.x == 1;
                }
                else
                {
                    return pos.x == width - 1 || pos.x == width - 2;
                }
            }
        }

        private void HighlightValidStartingColumns()
        {
            if (_gridManager == null) return;

            int activePlayer = (_currentState == SetupState.Player1Deployment) ? 1 : 2;
            int width = _gridManager.Width;
            int height = _gridManager.Height;

            // Clear all grid colors first
            Grid.Tile[] allTiles = FindObjectsByType<Grid.Tile>(FindObjectsSortMode.None);
            foreach (var tile in allTiles)
            {
                tile.ResetColor();
            }

            // Highlight current player starting columns/rows in transparent light blue
            Color deploymentHighlightColor = new Color(0.2f, 0.6f, 1f, 0.4f);
            bool highlightLow = (activePlayer == 1) ? _p1IsAtLowIndex : !_p1IsAtLowIndex;

            if (_isVerticalLayout)
            {
                for (int x = 0; x < width; x++)
                {
                    if (highlightLow)
                    {
                        var t0 = _gridManager.GetTileAt(new Vector2Int(x, 0));
                        if (t0 != null) t0.SetColor(deploymentHighlightColor);
                        var t1 = _gridManager.GetTileAt(new Vector2Int(x, 1));
                        if (t1 != null) t1.SetColor(deploymentHighlightColor);
                    }
                    else
                    {
                        var t0 = _gridManager.GetTileAt(new Vector2Int(x, height - 1));
                        if (t0 != null) t0.SetColor(deploymentHighlightColor);
                        var t1 = _gridManager.GetTileAt(new Vector2Int(x, height - 2));
                        if (t1 != null) t1.SetColor(deploymentHighlightColor);
                    }
                }
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    if (highlightLow)
                    {
                        var t0 = _gridManager.GetTileAt(new Vector2Int(0, y));
                        if (t0 != null) t0.SetColor(deploymentHighlightColor);
                        var t1 = _gridManager.GetTileAt(new Vector2Int(1, y));
                        if (t1 != null) t1.SetColor(deploymentHighlightColor);
                    }
                    else
                    {
                        var t0 = _gridManager.GetTileAt(new Vector2Int(width - 1, y));
                        if (t0 != null) t0.SetColor(deploymentHighlightColor);
                        var t1 = _gridManager.GetTileAt(new Vector2Int(width - 2, y));
                        if (t1 != null) t1.SetColor(deploymentHighlightColor);
                    }
                }
            }
        }

        private bool IsTileOccupiedDuringSetup(Vector2Int pos)
        {
            foreach (var stack in _p1Placed)
            {
                if (stack != null && stack.OccupiesTile(pos)) return true;
            }
            foreach (var stack in _p2Placed)
            {
                if (stack != null && stack.OccupiesTile(pos)) return true;
            }
            return false;
        }

        private Vector3 GetWorldPositionOf(Vector2Int position)
        {
            if (_gridManager != null)
            {
                Grid.Tile tile = _gridManager.GetTileAt(position);
                if (tile != null)
                {
                    return tile.transform.position;
                }
            }
            return Vector3.zero;
        }

        private void SpawnStackAt(CreatureData data, int count, int playerIndex, Vector2Int position)
        {
            if (_gridManager == null) return;

            Vector3 worldPos = GetWorldPositionOf(position);
            GameObject stackGo;

            if (troopPrefab != null)
            {
                stackGo = Instantiate(troopPrefab, worldPos, Quaternion.identity);
            }
            else
            {
                // Fallback creation
                stackGo = new GameObject(data.CreatureName);
                stackGo.transform.position = worldPos;
                stackGo.AddComponent<BoxCollider>().size = new Vector3(0.8f, 0.8f, 0.8f);

                // Add text label
                GameObject txtObj = new GameObject("StackSizeText");
                txtObj.transform.SetParent(stackGo.transform, false);
                txtObj.transform.localPosition = new Vector3(0f, 1f, 0f);
                TextMeshPro tmpText = txtObj.AddComponent<TextMeshPro>();
                tmpText.fontSize = 4;
                tmpText.alignment = TextAlignmentOptions.Center;
            }

            CreatureStack stack = stackGo.GetComponent<CreatureStack>();
            if (stack == null)
            {
                stack = stackGo.AddComponent<CreatureStack>();
            }

            // Check if text size label is null, assign dynamically if so
            var textMeshField = typeof(CreatureStack).GetField("stackSizeText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (textMeshField != null && textMeshField.GetValue(stack) == null)
            {
                TextMeshPro childTmp = stackGo.GetComponentInChildren<TextMeshPro>();
                if (childTmp == null)
                {
                    GameObject txtObj = new GameObject("StackSizeText");
                    txtObj.transform.SetParent(stackGo.transform, false);
                    txtObj.transform.localPosition = new Vector3(0f, 1f, 0f);
                    childTmp = txtObj.AddComponent<TextMeshPro>();
                    childTmp.fontSize = 4;
                    childTmp.alignment = TextAlignmentOptions.Center;
                }
                textMeshField.SetValue(stack, childTmp);
            }

            stack.Initialize(data, count, playerIndex, position);
            stack.FaceDefaultDirection();

            // Register in setup list
            if (playerIndex == 1)
            {
                _p1Placed.Add(stack);
            }
            else
            {
                _p2Placed.Add(stack);
            }
        }

        private void ReturnStackToReserve(CreatureStack stack)
        {
            if (_gridManager == null) return;

            // Return to reserve counts
            List<CreatureData> types = (stack.PlayerIndex == 1) ? _p1DraftedTypes : _p2DraftedTypes;
            List<int> counts = (stack.PlayerIndex == 1) ? _p1DraftedCounts : _p2DraftedCounts;

            int index = types.IndexOf(stack.Data);
            if (index >= 0)
            {
                counts[index] += stack.Count;
            }
            else
            {
                types.Add(stack.Data);
                counts.Add(stack.Count);
            }

            // Remove from placed lists
            if (stack.PlayerIndex == 1)
            {
                _p1Placed.Remove(stack);
            }
            else
            {
                _p2Placed.Remove(stack);
            }

            // Destroy game object
            Destroy(stack.gameObject);

            // Reset tray selection and refresh
            _selectedReserveIndex = -1;
            RefreshDeploymentTray();
        }

        public void SetFactionVisibility(int playerIndex, bool visible)
        {
            List<CreatureStack> placed = (playerIndex == 1) ? _p1Placed : _p2Placed;

            // Toggle Heroes if they exist
            var existingHeroes = FindObjectsByType<Heroes.Hero>(FindObjectsSortMode.None);
            foreach (var h in existingHeroes)
            {
                if (h.PlayerIndex == playerIndex)
                {
                    // Toggle mesh renderers for hero
                    var hr = h.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in hr)
                    {
                        r.enabled = visible;
                    }
                }
            }

            foreach (var stack in placed)
            {
                if (stack == null) continue;

                // Toggle mesh renderers in children (excluding the root placeholder mesh renderer)
                var renderers = stack.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r.gameObject == stack.gameObject)
                    {
                        continue;
                    }
                    r.enabled = visible;
                }

                // Toggle colliders to prevent hovering/clicking hidden units
                var colliders = stack.GetComponentsInChildren<Collider>(true);
                foreach (var col in colliders)
                {
                    col.enabled = visible;
                }

                // Toggle text labels
                var texts = stack.GetComponentsInChildren<TextMeshPro>(true);
                foreach (var t in texts)
                {
                    t.enabled = visible;
                }
            }
        }

        private void HideWorldMapVisuals()
        {
            var worldGrid = FindFirstObjectByType<World.WorldGridManager>();
            if (worldGrid != null) worldGrid.gameObject.SetActive(false);

            var worldHero = FindFirstObjectByType<World.WorldHero>();
            if (worldHero != null) worldHero.gameObject.SetActive(false);
        }

        private void SpawnSidelineHeroes()
        {
            var existingHeroes = FindObjectsByType<Heroes.Hero>(FindObjectsSortMode.None);
            Heroes.Hero p1Hero = System.Array.Find(existingHeroes, h => h != null && h.PlayerIndex == 1 && h.transform.parent == null);
            if (p1Hero == null) p1Hero = System.Array.Find(existingHeroes, h => h != null && h.PlayerIndex == 1);

            Heroes.Hero p2Hero = System.Array.Find(existingHeroes, h => h != null && h.PlayerIndex == 2 && h.transform.parent == null);
            if (p2Hero == null) p2Hero = System.Array.Find(existingHeroes, h => h != null && h.PlayerIndex == 2);

            var gameData = World.GameDataManager.GetOrCreateInstance();

            // Spawn Player 1 Hero 3D Object at Sideline Left
            if (p1Hero == null)
            {
                GameObject p1Obj = new GameObject("Player1_Hero3D");
                p1Obj.transform.position = new Vector3(-1.8f, 0f, 5.5f);
                p1Hero = p1Obj.AddComponent<Heroes.Hero>();
                var view = p1Obj.AddComponent<Heroes.Hero3DView>();

                GameObject customPrefab = (gameData != null && gameData.player1Hero != null) ? gameData.player1Hero.heroPrefab : null;
                view.SetupHeroVisual(customPrefab, lookRight: true);
            }

            // Set P1 Hero stats from GameDataManager
            if (gameData != null && gameData.player1Hero != null)
            {
                var data = gameData.player1Hero;
                p1Hero.SetStats(data.attack, data.defense, data.spellPower, data.knowledge, data.heroName, data.heroPortrait, 1);
                Debug.Log($"[Hero Stats Applied] P1 Hero '{p1Hero.Name}' stats set from GameDataManager -> Attack: {p1Hero.Attack}, Defense: {p1Hero.Defense}, SpellPower: {p1Hero.SpellPower}, Knowledge: {p1Hero.Knowledge}");
            }

            // Spawn Player 2 Hero 3D Object at Sideline Right ONLY in PVP mode
            if (p2Hero == null && _isPVP)
            {
                GameObject p2Obj = new GameObject("Player2_Hero3D");
                p2Obj.transform.position = new Vector3(11.8f, 0f, 5.5f);
                p2Hero = p2Obj.AddComponent<Heroes.Hero>();

                var data = (gameData != null) ? gameData.player2Hero : null;
                int att = data != null ? data.attack : 5;
                int def = data != null ? data.defense : 5;
                int sp = data != null ? data.spellPower : 5;
                int kn = data != null ? data.knowledge : 5;
                string hName = data != null ? data.heroName : "Player 2 Hero";
                Sprite portrait = data != null ? data.heroPortrait : null;

                p2Hero.SetStats(att, def, sp, kn, hName, portrait, 2);

                var view = p2Obj.AddComponent<Heroes.Hero3DView>();
                GameObject customPrefab = data != null ? data.heroPrefab : null;
                view.SetupHeroVisual(customPrefab, lookRight: false);
            }

            // Assign HeroOwner to all placed & scene creature stacks
            var allSceneStacks = FindObjectsByType<CreatureStack>(FindObjectsSortMode.None);
            foreach (var stack in allSceneStacks)
            {
                if (stack == null) continue;
                if (stack.PlayerIndex == 1)
                {
                    stack.HeroOwner = p1Hero;
                }
                else if (stack.PlayerIndex == 2 && p2Hero != null)
                {
                    stack.HeroOwner = p2Hero;
                }
            }
        }
        #endregion
    }
}
