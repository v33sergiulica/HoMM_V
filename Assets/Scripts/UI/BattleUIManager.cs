using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using HommClone.Turns;
using HommClone.Creatures;
using HommClone.Spells;

namespace HommClone.UI
{
    /// <summary>
    /// Procedurally instantiates and manages the battle UI HUD (Timeline bar, active unit information, Wait and Defend action buttons)
    /// and handles instantiation of floating combat text above creature stacks.
    /// </summary>
    public class BattleUIManager : MonoBehaviour
    {
        private TurnManager _turnManager;
        
        [Header("UI Styling Colors")]
        [SerializeField] private Color player1Color = new Color(0.2f, 0.4f, 0.8f, 0.8f); // Soft Blue
        [SerializeField] private Color player2Color = new Color(0.8f, 0.2f, 0.2f, 0.8f); // Soft Red
        [SerializeField] private Color activeFrameColor = new Color(1.0f, 0.9f, 0.0f, 1.0f); // Gold highlight outline
        
        private GameObject _canvasObj;
        private RectTransform _timelinePanel;
        private Button _waitButton;
        private Button _defendButton;
        private Button _spellsButton;
        private GameObject _spellBookPanel;
        private GameObject _unitInfoPanel;
        private TextMeshProUGUI _activeUnitLabel;
        
        private List<GameObject> _timelineCards = new List<GameObject>();
        
        private RectTransform _tooltipPanel;
        private TextMeshProUGUI _tooltipText;

        private void Start()
        {
            _turnManager = FindFirstObjectByType<TurnManager>();
            if (_turnManager != null)
            {
                _turnManager.OnTurnChanged += RefreshUI;
            }

            CreateHUD();
            RefreshUI();
        }

        private void OnDestroy()
        {
            if (_turnManager != null)
            {
                _turnManager.OnTurnChanged -= RefreshUI;
            }
        }

        /// <summary>
        /// Refreshes the active unit indicator, timeline initiative bar, and action button interactability.
        /// </summary>
        public void RefreshUI()
        {
            // Block and hide Combat HUD Canvas if Setup/Deployment is active
            var setup = FindFirstObjectByType<HommClone.Turns.BattleSetupManager>();
            if (setup != null && setup.CurrentState != HommClone.Turns.BattleSetupManager.SetupState.Combat)
            {
                if (_canvasObj != null) _canvasObj.SetActive(false);
                return;
            }
            else
            {
                if (_canvasObj != null) _canvasObj.SetActive(true);
            }

            if (_turnManager == null) return;

            // 1. Update Active Participant Info and Action Controls
            Turns.ITimelineParticipant active = _turnManager.ActiveUnit;
            if (_activeUnitLabel != null)
            {
                if (active != null)
                {
                    string colorHex = (active.PlayerIndex == 1) ? "4488ff" : "ff4444";
                    string activeText = $"Active: <color=#{colorHex}><b>{active.Name}</b></color> (Player {active.PlayerIndex})";
                    
                    bool isCaster = false;

                    if (active is Heroes.Hero hero)
                    {
                        activeText = $"Active: <color=#{colorHex}><b>{hero.Name} (Hero)</b></color>";
                        isCaster = hero.Spells != null && hero.Spells.Count > 0 && hero.MaxMana > 0;
                        activeText += $" | Mana: {hero.CurrentMana}/{hero.MaxMana}";
                    }
                    else if (active is CreatureStack stack)
                    {
                        // Display ammo and blocked warnings for shooter stacks
                        if (stack.Data != null && stack.Data.IsRanged)
                        {
                            activeText += $" | Ammo: {stack.CurrentAmmo}/{stack.Data.MaxAmmo}";
                            if (stack.IsBlocked())
                            {
                                activeText += " <color=red><b>(BLOCKED)</b></color>";
                            }
                        }
                        
                        if (stack.HasAbility<CasterAbility>())
                        {
                            var casterAbility = stack.GetAbility<CasterAbility>();
                            isCaster = casterAbility != null && casterAbility.Spells != null && casterAbility.Spells.Count > 0 && stack.Data != null && stack.Data.MaxMana > 0;
                        }

                        if (isCaster)
                        {
                            activeText += $" | Mana: {stack.CurrentMana}/{stack.MaxMana}";
                        }
                    }

                    _activeUnitLabel.text = activeText;
                    
                    _waitButton.interactable = true;
                    _defendButton.interactable = true;
                    if (_spellsButton != null)
                    {
                        _spellsButton.interactable = isCaster;
                    }
                }
                else
                {
                    _activeUnitLabel.text = "Waiting for round initialization...";
                    _waitButton.interactable = false;
                    _defendButton.interactable = false;
                    if (_spellsButton != null)
                    {
                        _spellsButton.interactable = false;
                    }
                    HideSpellBook();
                }
            }

            // 2. Rebuild Timeline Cards Panel
            foreach (GameObject card in _timelineCards)
            {
                if (card != null) Destroy(card);
            }
            _timelineCards.Clear();

            // Predict the next 6 turns from the initiative bar engine
            var nextTurns = _turnManager.PredictFutureTimeline(6);
            for (int i = 0; i < nextTurns.Count; i++)
            {
                Turns.ITimelineParticipant participant = nextTurns[i];
                if (participant == null) continue;

                // Card base layout GameObject
                GameObject cardObj = new GameObject($"Card_{i}");
                cardObj.transform.SetParent(_timelinePanel, false);
                
                Image bg = cardObj.AddComponent<Image>();
                // Player team backdrop color (Player 1 = Left side, Player 2 = Right side)
                bg.color = (participant.PlayerIndex == 1) ? player1Color : player2Color;

                // Highlight the active stack slot (index 0) with an outline
                if (i == 0)
                {
                    Outline outline = cardObj.AddComponent<Outline>();
                    outline.effectColor = activeFrameColor;
                    outline.effectDistance = new Vector2(3f, 3f);
                }

                // Control scale and layout elements (square cards look better for portraits)
                LayoutElement le = cardObj.AddComponent<LayoutElement>();
                le.minWidth = 65f;
                le.minHeight = 65f;

                // Add icon if it exists
                if (participant.Icon != null)
                {
                    GameObject iconObj = new GameObject("Icon");
                    iconObj.transform.SetParent(cardObj.transform, false);
                    
                    Image iconImg = iconObj.AddComponent<Image>();
                    iconImg.sprite = participant.Icon;
                    iconImg.preserveAspect = true;

                    RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                    iconRect.anchorMin = Vector2.zero;
                    iconRect.anchorMax = Vector2.one;
                    iconRect.offsetMin = new Vector2(3f, 3f);
                    iconRect.offsetMax = new Vector2(-3f, -3f);

                    // Add count and ammo details if it is a CreatureStack
                    if (participant is CreatureStack stack)
                    {
                        GameObject countObj = new GameObject("Count");
                        countObj.transform.SetParent(cardObj.transform, false);
                        
                        TextMeshProUGUI countTxt = countObj.AddComponent<TextMeshProUGUI>();
                        countTxt.alignment = TextAlignmentOptions.BottomRight;
                        countTxt.fontSize = 11;
                        countTxt.fontStyle = FontStyles.Bold;
                        countTxt.color = Color.white;

                        var countOutline = countTxt.gameObject.AddComponent<Outline>();
                        countOutline.effectColor = Color.black;
                        countOutline.effectDistance = new Vector2(1.2f, 1.2f);
                        countTxt.text = stack.Count.ToString();

                        RectTransform countRect = countObj.GetComponent<RectTransform>();
                        countRect.anchorMin = Vector2.zero;
                        countRect.anchorMax = Vector2.one;
                        countRect.offsetMin = new Vector2(5f, 3f);
                        countRect.offsetMax = new Vector2(-5f, -3f);

                        // Add Ammo count in bottom-left corner if ranged
                        if (stack.Data != null && stack.Data.IsRanged)
                        {
                            GameObject ammoObj = new GameObject("Ammo");
                            ammoObj.transform.SetParent(cardObj.transform, false);
                            
                            TextMeshProUGUI ammoTxt = ammoObj.AddComponent<TextMeshProUGUI>();
                            ammoTxt.alignment = TextAlignmentOptions.BottomLeft;
                            ammoTxt.fontSize = 9;
                            ammoTxt.fontStyle = FontStyles.Bold;
                            ammoTxt.color = new Color(0.9f, 0.9f, 0.9f);

                            var ammoOutline = ammoTxt.gameObject.AddComponent<Outline>();
                            ammoOutline.effectColor = Color.black;
                            ammoOutline.effectDistance = new Vector2(1f, 1f);
                            ammoTxt.text = $"A:{stack.CurrentAmmo}";

                            RectTransform ammoRect = ammoObj.GetComponent<RectTransform>();
                            ammoRect.anchorMin = Vector2.zero;
                            ammoRect.anchorMax = Vector2.one;
                            ammoRect.offsetMin = new Vector2(5f, 3f);
                            ammoRect.offsetMax = new Vector2(-5f, -3f);
                        }
                    }
                }
                else
                {
                    // Fallback to text overlay if icon is missing
                    GameObject textObj = new GameObject("Text");
                    textObj.transform.SetParent(cardObj.transform, false);
                    
                    TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
                    txt.alignment = TextAlignmentOptions.Center;
                    txt.fontSize = 11;
                    txt.color = Color.white;
                    
                    string cardText = participant is CreatureStack stack 
                        ? $"<b>{participant.Name}</b>\n({stack.Count})"
                        : $"<b>{participant.Name}</b>\n<size=80%>(Hero)</size>";
                    
                    txt.text = cardText;

                    RectTransform textRect = textObj.GetComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;
                }

                _timelineCards.Add(cardObj);
            }
        }

        /// <summary>
        /// Procedurally instantiates Canvas overlay hierarchy, Panels, and Buttons at startup.
        /// </summary>
        private void CreateHUD()
        {
            // Create root Canvas GameObject
            _canvasObj = new GameObject("BattleCanvas");
            Canvas canvas = _canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvasObj.AddComponent<CanvasScaler>();
            _canvasObj.AddComponent<GraphicRaycaster>();

            // Create Timeline Card Panel
            GameObject timelineObj = new GameObject("TimelinePanel");
            timelineObj.transform.SetParent(_canvasObj.transform, false);
            _timelinePanel = timelineObj.AddComponent<RectTransform>();
            _timelinePanel.anchorMin = new Vector2(0.5f, 1f);
            _timelinePanel.anchorMax = new Vector2(0.5f, 1f);
            _timelinePanel.pivot = new Vector2(0.5f, 1f);
            _timelinePanel.anchoredPosition = new Vector2(0f, -20f);
            _timelinePanel.sizeDelta = new Vector2(620f, 65f);

            Image tlBg = timelineObj.AddComponent<Image>();
            tlBg.color = new Color(0f, 0f, 0f, 0.65f); // Translucent charcoal backdrop

            HorizontalLayoutGroup hlg = timelineObj.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.spacing = 8f;
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = true;

            // Create Active Turn Status Label
            GameObject labelObj = new GameObject("ActiveUnitLabel");
            labelObj.transform.SetParent(_canvasObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -95f);
            labelRect.sizeDelta = new Vector2(400f, 30f);

            _activeUnitLabel = labelObj.AddComponent<TextMeshProUGUI>();
            _activeUnitLabel.alignment = TextAlignmentOptions.Center;
            _activeUnitLabel.fontSize = 15;
            _activeUnitLabel.color = Color.white;
            _activeUnitLabel.text = "Setting up battle logs...";

            // Create Buttons Horizontal Panel
            GameObject actionPanelObj = new GameObject("ActionPanel");
            actionPanelObj.transform.SetParent(_canvasObj.transform, false);
            RectTransform actionPanelRect = actionPanelObj.AddComponent<RectTransform>();
            actionPanelRect.anchorMin = new Vector2(1f, 0f);
            actionPanelRect.anchorMax = new Vector2(1f, 0f);
            actionPanelRect.pivot = new Vector2(1f, 0f);
            actionPanelRect.anchoredPosition = new Vector2(-20f, 20f);
            actionPanelRect.sizeDelta = new Vector2(350f, 50f);

            HorizontalLayoutGroup actionHlg = actionPanelObj.AddComponent<HorizontalLayoutGroup>();
            actionHlg.spacing = 12f;
            actionHlg.childControlWidth = true;
            actionHlg.childControlHeight = true;

            // Instantiate Action Buttons
            _spellsButton = CreateActionButton(actionPanelObj.transform, "SpellsButton", "SPELLS", () =>
            {
                ToggleSpellBook();
            });

            _waitButton = CreateActionButton(actionPanelObj.transform, "WaitButton", "WAIT", () =>
            {
                if (_turnManager != null)
                {
                    _turnManager.ExecuteWait();
                }
            });

            _defendButton = CreateActionButton(actionPanelObj.transform, "DefendButton", "DEFEND", () =>
            {
                if (_turnManager != null)
                {
                    _turnManager.ExecuteDefend();
                }
            });

            // Create Tooltip Panel (Procedural Overlay)
            GameObject tooltipObj = new GameObject("HoverTooltipPanel");
            tooltipObj.transform.SetParent(_canvasObj.transform, false);
            _tooltipPanel = tooltipObj.AddComponent<RectTransform>();
            _tooltipPanel.anchorMin = Vector2.zero;
            _tooltipPanel.anchorMax = Vector2.zero;
            _tooltipPanel.pivot = new Vector2(0f, 1f); // pivot top-left so it stays below-right of the cursor
            _tooltipPanel.sizeDelta = new Vector2(175f, 52f);

            Image tooltipBg = tooltipObj.AddComponent<Image>();
            tooltipBg.color = new Color(0.08f, 0.08f, 0.08f, 0.9f); // Charcoal dark glass backdrop
            
            Outline tooltipBorder = tooltipObj.AddComponent<Outline>();
            tooltipBorder.effectColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            tooltipBorder.effectDistance = new Vector2(1f, 1f);

            GameObject tooltipTxtObj = new GameObject("Text");
            tooltipTxtObj.transform.SetParent(tooltipObj.transform, false);
            _tooltipText = tooltipTxtObj.AddComponent<TextMeshProUGUI>();
            _tooltipText.alignment = TextAlignmentOptions.TopLeft;
            _tooltipText.fontSize = 11;
            _tooltipText.color = Color.white;
            _tooltipText.text = "";

            RectTransform txtRect = tooltipTxtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(8f, 6f);  // padding left/bottom
            txtRect.offsetMax = new Vector2(-8f, -6f); // padding right/top

            tooltipObj.SetActive(false);
        }

        private Button CreateActionButton(Transform parent, string name, string label, System.Action onClick)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.18f, 0.18f, 0.18f, 0.95f);
            cb.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 0.95f);
            cb.pressedColor = new Color(0.08f, 0.08f, 0.08f, 0.95f);
            cb.disabledColor = new Color(0.05f, 0.05f, 0.05f, 0.4f);
            btn.colors = cb;

            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = 13;
            txt.color = Color.white;
            txt.text = $"<b>{label}</b>";

            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            return btn;
        }

        /// <summary>
        /// Spawns a floating combat text label in 3D space which moves upwards and fades out.
        /// </summary>
        public void SpawnDamageText(Vector3 worldPos, string text, Color color)
        {
            // Add a small horizontal offset to prevent subsequent damage/retaliation numbers from overlapping
            Vector3 randomOffset = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(-0.05f, 0.05f), Random.Range(-0.35f, 0.35f));
            Vector3 spawnPos = worldPos + randomOffset;

            GameObject textObj = new GameObject("FloatingDamageText");
            textObj.transform.position = spawnPos;
            
            // Set initial billboard facing rotation
            if (Camera.main != null)
            {
                textObj.transform.rotation = Camera.main.transform.rotation;
            }

            TextMeshPro tm = textObj.AddComponent<TextMeshPro>();
            tm.text = text;
            tm.color = color;
            tm.alignment = TextAlignmentOptions.Center;
            tm.fontSize = 5.2f; // Slightly larger for better readability
            tm.fontStyle = FontStyles.Bold;

            StartCoroutine(FloatAndFadeCoroutine(textObj, tm));
        }

        private IEnumerator FloatAndFadeCoroutine(GameObject obj, TextMeshPro textMesh)
        {
            float duration = 1.8f; // Lasts longer for readability
            float elapsed = 0f;
            Vector3 startPos = obj.transform.position;
            Color startColor = textMesh.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Sinusoidal float ease-out: starts fast, then floats slowly at the top
                float floatOffset = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.3f;
                obj.transform.position = startPos + Vector3.up * floatOffset;
                
                // Fade color out
                textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                
                // Maintain camera billboard alignment dynamically
                if (Camera.main != null)
                {
                    obj.transform.rotation = Camera.main.transform.rotation;
                }

                yield return null;
            }

            Destroy(obj);
        }

        /// <summary>
        /// Shows the estimated damage and kills tooltip near the mouse cursor position.
        /// </summary>
        public void ShowHoverTooltip(Vector2 mousePosition, string content)
        {
            if (_tooltipPanel == null || _tooltipText == null) return;

            _tooltipText.text = content;
            
            // Position the tooltip panel slightly offset to the bottom-right of the mouse cursor
            _tooltipPanel.gameObject.SetActive(true);
            _tooltipPanel.position = mousePosition + new Vector2(18f, -15f);
        }

        /// <summary>
        /// Hides the hover tooltip immediately.
        /// </summary>
        public void HideHoverTooltip()
        {
            if (_tooltipPanel != null)
            {
                _tooltipPanel.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Instantiates a large Victory message overlay in the center of the Canvas.
        /// </summary>
        public void ShowVictoryMessage(string winner)
        {
            if (_canvasObj == null) return;

            // Prevent duplicate victory screens
            if (_canvasObj.transform.Find("VictoryPanel") != null) return;

            GameObject victoryObj = new GameObject("VictoryPanel");
            victoryObj.transform.SetParent(_canvasObj.transform, false);
            RectTransform rect = victoryObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = victoryObj.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f); // Fade out the screen back to dark translucent

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(victoryObj.transform, false);
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = 32;
            txt.color = Color.yellow;
            txt.text = $"<b>VICTORY!</b>\n<size=80%>{winner} wins the battle</size>";

            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = new Vector2(0f, 0.4f);
            txtRect.anchorMax = new Vector2(1f, 1f);
            txtRect.sizeDelta = Vector2.zero;

            // Return to World Map Button
            GameObject returnBtnObj = new GameObject("ReturnButton");
            returnBtnObj.transform.SetParent(victoryObj.transform, false);
            RectTransform retRect = returnBtnObj.AddComponent<RectTransform>();
            retRect.anchorMin = new Vector2(0.5f, 0.25f);
            retRect.anchorMax = new Vector2(0.5f, 0.25f);
            retRect.pivot = new Vector2(0.5f, 0.5f);
            retRect.sizeDelta = new Vector2(230f, 50f);

            Image retImg = returnBtnObj.AddComponent<Image>();
            retImg.color = new Color(0.15f, 0.45f, 0.15f, 1f);

            Button retBtn = returnBtnObj.AddComponent<Button>();
            retBtn.onClick.AddListener(ReturnToWorldMap);

            GameObject retLblObj = new GameObject("Label");
            retLblObj.transform.SetParent(returnBtnObj.transform, false);
            TextMeshProUGUI retLbl = retLblObj.AddComponent<TextMeshProUGUI>();
            retLbl.text = "<b>RETURN TO WORLD MAP</b>";
            retLbl.fontSize = 13;
            retLbl.color = Color.white;
            retLbl.alignment = TextAlignmentOptions.Center;
            RectTransform retLblRect = retLblObj.GetComponent<RectTransform>();
            retLblRect.anchorMin = Vector2.zero;
            retLblRect.anchorMax = Vector2.one;
            retLblRect.sizeDelta = Vector2.zero;

            // Disable actions
            if (_waitButton != null) _waitButton.interactable = false;
            if (_defendButton != null) _defendButton.interactable = false;
            if (_spellsButton != null) _spellsButton.interactable = false;
        }

        public void ReturnToWorldMap()
        {
            var audioManager = HommClone.Audio.AudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.StopSFX();
                audioManager.PlayWorldMapMusic();
            }

            var manager = HommClone.World.GameDataManager.GetOrCreateInstance();
            if (manager != null)
            {
                manager.isReturningFromBattle = true;
            }

            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            if (sceneCount > 1)
            {
                // Load World Map Scene (usually index 0 or named WorldMapScene)
                try
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("WorldMapScene");
                }
                catch
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(0);
                }
            }
            else
            {
                // Single Scene architecture: Clean up victory panel and update World Map UI
                if (_canvasObj != null)
                {
                    var victoryPanel = _canvasObj.transform.Find("VictoryPanel");
                    if (victoryPanel != null) Destroy(victoryPanel.gameObject);
                }

                var worldGrid = FindFirstObjectByType<World.WorldGridManager>(FindObjectsInactive.Include);
                if (worldGrid != null) worldGrid.gameObject.SetActive(true);

                var worldHero = FindFirstObjectByType<World.WorldHero>(FindObjectsInactive.Include);
                if (worldHero != null) worldHero.gameObject.SetActive(true);

                var worldMap = FindFirstObjectByType<World.WorldMapManager>();
                if (worldMap != null)
                {
                    worldMap.UpdateUI();
                }
                Debug.Log("[BattleUIManager] Returned to World Map view!");
            }
        }

        public void ToggleSpellBook()
        {
            if (_spellBookPanel != null && _spellBookPanel.activeSelf)
            {
                HideSpellBook();
            }
            else
            {
                ShowSpellBook();
            }
        }

        public void HideSpellBook()
        {
            if (_spellBookPanel != null)
            {
                _spellBookPanel.SetActive(false);
            }
        }

        private int _selectedSpellTab = 0; // 0 = ALL, 1 = LIGHT, 2 = DARK, 3 = DESTRUCTIVE

        public void ShowSpellBook()
        {
            if (_turnManager == null || _turnManager.ActiveUnit == null) return;
            Turns.ITimelineParticipant active = _turnManager.ActiveUnit;
            
            List<Spell> spells = null;
            int currentMana = 0;
            int maxMana = 0;
            HommClone.World.HeroData heroData = null;

            if (active is Heroes.Hero hero)
            {
                spells = hero.Spells;
                currentMana = hero.CurrentMana;
                maxMana = hero.MaxMana;

                var gdm = HommClone.World.GameDataManager.Instance;
                if (gdm != null) heroData = (hero.PlayerIndex == 1) ? gdm.player1Hero : gdm.player2Hero;
            }
            else if (active is CreatureStack stack)
            {
                spells = stack.Spells;
                currentMana = stack.CurrentMana;
                maxMana = stack.MaxMana;
            }

            if (spells == null || spells.Count == 0 || maxMana == 0) return;

            // Create or update panel
            if (_spellBookPanel == null)
            {
                _spellBookPanel = new GameObject("SpellBookPanel");
                _spellBookPanel.transform.SetParent(_canvasObj.transform, false);
                RectTransform rect = _spellBookPanel.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(520f, 420f);

                Image bg = _spellBookPanel.AddComponent<Image>();
                bg.color = new Color(0.08f, 0.08f, 0.12f, 0.96f); // Rich dark metallic obsidian

                Outline border = _spellBookPanel.AddComponent<Outline>();
                border.effectColor = new Color(0.85f, 0.7f, 0.25f, 0.9f); // Bright Gold border
                border.effectDistance = new Vector2(2.5f, 2.5f);
            }

            _spellBookPanel.SetActive(true);

            // Destroy old content
            foreach (Transform child in _spellBookPanel.transform)
            {
                Destroy(child.gameObject);
            }

            // 1. Header Title & Mana
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(_spellBookPanel.transform, false);
            RectTransform headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, -10f);
            headerRect.sizeDelta = new Vector2(0f, 45f);

            TextMeshProUGUI headerTxt = headerObj.AddComponent<TextMeshProUGUI>();
            headerTxt.alignment = TextAlignmentOptions.Center;
            headerTxt.fontSize = 17;
            headerTxt.color = new Color(1f, 0.9f, 0.5f, 1f);
            headerTxt.text = $"<b>GRIMOIRE & SPELLBOOK</b>   <size=75%>(<color=#55CCFF>Mana: {currentMana} / {maxMana}</color>)</size>";

            // 2. Magic School Tabs Container
            GameObject tabsObj = new GameObject("TabsContainer");
            tabsObj.transform.SetParent(_spellBookPanel.transform, false);
            RectTransform tabsRect = tabsObj.AddComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0f, 1f);
            tabsRect.anchorMax = new Vector2(1f, 1f);
            tabsRect.pivot = new Vector2(0.5f, 1f);
            tabsRect.anchoredPosition = new Vector2(0f, -55f);
            tabsRect.sizeDelta = new Vector2(0f, 32f);

            HorizontalLayoutGroup tabsLayout = tabsObj.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.spacing = 8f;
            tabsLayout.childAlignment = TextAnchor.MiddleCenter;
            tabsLayout.childControlWidth = true;
            tabsLayout.childControlHeight = true;

            string[] tabNames = new string[] { "ALL SPELLS", "LIGHT", "DARK", "DESTRUCTIVE" };
            Color[] tabColors = new Color[] {
                new Color(0.8f, 0.8f, 0.8f),
                new Color(1f, 0.9f, 0.4f),
                new Color(0.8f, 0.5f, 1f),
                new Color(1f, 0.35f, 0.35f)
            };

            for (int t = 0; t < tabNames.Length; t++)
            {
                int tabIdx = t;
                GameObject tBtnObj = new GameObject($"Tab_{tabNames[t]}");
                tBtnObj.transform.SetParent(tabsObj.transform, false);

                Image tImg = tBtnObj.AddComponent<Image>();
                bool isSelected = (_selectedSpellTab == tabIdx);
                tImg.color = isSelected ? tabColors[t] * 0.4f + new Color(0.2f, 0.2f, 0.2f, 0.8f) : new Color(0.12f, 0.12f, 0.15f, 0.9f);

                Outline tBorder = tBtnObj.AddComponent<Outline>();
                tBorder.effectColor = isSelected ? tabColors[t] : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                tBorder.effectDistance = new Vector2(1.5f, 1.5f);

                Button tBtn = tBtnObj.AddComponent<Button>();
                tBtn.onClick.AddListener(() =>
                {
                    _selectedSpellTab = tabIdx;
                    ShowSpellBook();
                });

                GameObject tLblObj = new GameObject("Label");
                tLblObj.transform.SetParent(tBtnObj.transform, false);
                TextMeshProUGUI tLbl = tLblObj.AddComponent<TextMeshProUGUI>();
                tLbl.alignment = TextAlignmentOptions.Center;
                tLbl.fontSize = 11;
                tLbl.color = isSelected ? tabColors[t] : new Color(0.7f, 0.7f, 0.7f);
                tLbl.text = $"<b>{tabNames[t]}</b>";

                RectTransform tLblRect = tLblObj.GetComponent<RectTransform>();
                tLblRect.anchorMin = Vector2.zero;
                tLblRect.anchorMax = Vector2.one;
                tLblRect.sizeDelta = Vector2.zero;
            }

            // 3. Spells Grid Container
            GameObject gridObj = new GameObject("SpellsGrid");
            gridObj.transform.SetParent(_spellBookPanel.transform, false);
            RectTransform gridRect = gridObj.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0f, 0f);
            gridRect.anchorMax = new Vector2(1f, 1f);
            gridRect.offsetMin = new Vector2(15f, 45f);
            gridRect.offsetMax = new Vector2(-15f, -95f);

            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(235f, 65f);
            grid.spacing = new Vector2(12f, 10f);
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            // Filter Spells by selected tab
            List<Spell> filteredSpells = new List<Spell>();
            foreach (var s in spells)
            {
                if (s == null) continue;
                if (_selectedSpellTab == 0) filteredSpells.Add(s);
                else if (_selectedSpellTab == 1 && s.School == MagicSchool.Light) filteredSpells.Add(s);
                else if (_selectedSpellTab == 2 && s.School == MagicSchool.Dark) filteredSpells.Add(s);
                else if (_selectedSpellTab == 3 && s.School == MagicSchool.Destructive) filteredSpells.Add(s);
            }

            foreach (var spell in filteredSpells)
            {
                GameObject btnObj = new GameObject($"Spell_{spell.SpellName}");
                btnObj.transform.SetParent(gridObj.transform, false);

                Image img = btnObj.AddComponent<Image>();
                img.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);

                Color schoolColor = (spell.School == MagicSchool.Light) ? new Color(1f, 0.85f, 0.3f) :
                                   (spell.School == MagicSchool.Dark) ? new Color(0.75f, 0.45f, 1f) :
                                                                         new Color(1f, 0.35f, 0.35f);

                Outline sOutline = btnObj.AddComponent<Outline>();
                sOutline.effectColor = schoolColor * 0.7f;
                sOutline.effectDistance = new Vector2(1.5f, 1.5f);

                Button btn = btnObj.AddComponent<Button>();
                ColorBlock cb = btn.colors;
                cb.normalColor = new Color(0.14f, 0.14f, 0.18f, 1f);
                cb.highlightedColor = new Color(0.24f, 0.24f, 0.32f, 1f);
                cb.pressedColor = new Color(0.09f, 0.09f, 0.12f, 1f);
                cb.disabledColor = new Color(0.08f, 0.08f, 0.08f, 0.5f);
                btn.colors = cb;

                // 1. Icon Container Frame (Left 50x50 px)
                GameObject iconFrameObj = new GameObject("IconFrame");
                iconFrameObj.transform.SetParent(btnObj.transform, false);
                RectTransform iconFrameRect = iconFrameObj.AddComponent<RectTransform>();
                iconFrameRect.anchorMin = new Vector2(0f, 0.5f);
                iconFrameRect.anchorMax = new Vector2(0f, 0.5f);
                iconFrameRect.pivot = new Vector2(0f, 0.5f);
                iconFrameRect.anchoredPosition = new Vector2(7f, 0f);
                iconFrameRect.sizeDelta = new Vector2(50f, 50f);

                Image iconFrameBg = iconFrameObj.AddComponent<Image>();
                iconFrameBg.color = new Color(0.06f, 0.06f, 0.09f, 1f);

                Outline iconBorder = iconFrameObj.AddComponent<Outline>();
                iconBorder.effectColor = schoolColor;
                iconBorder.effectDistance = new Vector2(1f, 1f);

                // Spell Icon Image
                GameObject iconImgObj = new GameObject("SpellIcon");
                iconImgObj.transform.SetParent(iconFrameObj.transform, false);
                RectTransform iconImgRect = iconImgObj.AddComponent<RectTransform>();
                iconImgRect.anchorMin = Vector2.zero;
                iconImgRect.anchorMax = Vector2.one;
                iconImgRect.sizeDelta = Vector2.zero;

                Image iconImg = iconImgObj.AddComponent<Image>();
                if (spell.Icon != null)
                {
                    iconImg.sprite = spell.Icon;
                    iconImg.color = Color.white;
                }
                else
                {
                    // Fallback visual badge with school color tint & spell initials
                    iconImg.color = schoolColor * 0.4f + new Color(0.2f, 0.2f, 0.2f, 0.8f);

                    GameObject initObj = new GameObject("Initials");
                    initObj.transform.SetParent(iconImgObj.transform, false);
                    TextMeshProUGUI initTxt = initObj.AddComponent<TextMeshProUGUI>();
                    initTxt.alignment = TextAlignmentOptions.Center;
                    initTxt.fontSize = 16;
                    initTxt.color = schoolColor;
                    string initials = spell.SpellName.Length >= 2 ? spell.SpellName.Substring(0, 2).ToUpper() : "SP";
                    initTxt.text = $"<b>{initials}</b>";

                    RectTransform initRect = initObj.GetComponent<RectTransform>();
                    initRect.anchorMin = Vector2.zero;
                    initRect.anchorMax = Vector2.one;
                    initRect.sizeDelta = Vector2.zero;
                }

                // 2. Spell Info Label (Right side)
                SpellMastery mastery = heroData != null ? heroData.GetSchoolMastery(spell.School) : SpellMastery.Basic;
                string schoolHex = ColorUtility.ToHtmlStringRGB(schoolColor);

                GameObject lblObj = new GameObject("Label");
                lblObj.transform.SetParent(btnObj.transform, false);
                TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
                lbl.alignment = TextAlignmentOptions.Left;
                lbl.fontSize = 11;
                lbl.color = Color.white;
                lbl.text = $"<b>{spell.SpellName}</b> <color=#{schoolHex}><size=80%>[{spell.School}]</size></color>\n<size=85%>Cost: <color=#55CCFF><b>{spell.ManaCost} MP</b></color> | Rank: <color=#FFCC00>{mastery}</color></size>";

                RectTransform lblRect = lblObj.GetComponent<RectTransform>();
                lblRect.anchorMin = Vector2.zero;
                lblRect.anchorMax = Vector2.one;
                lblRect.offsetMin = new Vector2(64f, 4f); // Shifted right past the 50px icon frame
                lblRect.offsetMax = new Vector2(-6f, -4f);

                if (currentMana < spell.ManaCost)
                {
                    btn.interactable = false;
                    lbl.color = new Color(1f, 1f, 1f, 0.4f);
                    if (iconImg != null) iconImg.color = new Color(iconImg.color.r, iconImg.color.g, iconImg.color.b, 0.4f);
                }
                else
                {
                    btn.onClick.AddListener(() =>
                    {
                        HideSpellBook();
                        var interactionManager = FindFirstObjectByType<Interaction.BattleInteractionManager>();
                        if (interactionManager != null)
                        {
                            interactionManager.StartSpellTargeting(spell);
                        }
                    });
                }
            }

            // 4. Close Button
            GameObject closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(_spellBookPanel.transform, false);
            RectTransform closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 10f);
            closeRect.sizeDelta = new Vector2(130f, 30f);

            Image closeImg = closeObj.AddComponent<Image>();
            closeImg.color = new Color(0.35f, 0.12f, 0.12f, 0.95f);

            Outline closeBorder = closeObj.AddComponent<Outline>();
            closeBorder.effectColor = new Color(0.8f, 0.3f, 0.3f, 0.8f);
            closeBorder.effectDistance = new Vector2(1.5f, 1.5f);

            Button closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => HideSpellBook());

            GameObject closeLblObj = new GameObject("Label");
            closeLblObj.transform.SetParent(closeObj.transform, false);
            TextMeshProUGUI closeLbl = closeLblObj.AddComponent<TextMeshProUGUI>();
            closeLbl.alignment = TextAlignmentOptions.Center;
            closeLbl.fontSize = 12;
            closeLbl.color = Color.white;
            closeLbl.text = "<b>CLOSE SPELLBOOK</b>";

            RectTransform closeLblRect = closeLblObj.GetComponent<RectTransform>();
            closeLblRect.anchorMin = Vector2.zero;
            closeLblRect.anchorMax = Vector2.one;
            closeLblRect.sizeDelta = Vector2.zero;
        }

        public void HideUnitInfoPanel()
        {
            if (_unitInfoPanel != null)
            {
                _unitInfoPanel.SetActive(false);
            }
        }

        public void ShowUnitInfoPanel(CreatureStack stack)
        {
            if (stack == null || stack.Data == null) return;

            // Close spellbook first
            HideSpellBook();

            if (_unitInfoPanel != null && _unitInfoPanel.transform.Find("InnerPanel") == null)
            {
                Destroy(_unitInfoPanel);
                _unitInfoPanel = null;
            }

            if (_unitInfoPanel == null)
            {
                CreateUnitInfoPanelUI();
            }

            _unitInfoPanel.SetActive(true);

            // Populate Creature Icon
            var iconImg = _unitInfoPanel.transform.Find("InnerPanel/IconFrame/CreatureIcon")?.GetComponent<Image>();
            if (iconImg != null)
            {
                if (stack.Data.Icon != null)
                {
                    iconImg.gameObject.SetActive(true);
                    iconImg.sprite = stack.Data.Icon;
                    iconImg.color = Color.white;
                }
                else
                {
                    iconImg.gameObject.SetActive(false);
                }
            }

            // Populate Info text content
            var title = _unitInfoPanel.transform.Find("InnerPanel/TitleText")?.GetComponent<TextMeshProUGUI>();
            if (title != null) title.text = $"<b>{stack.Name}</b> <size=80%>(Player {stack.PlayerIndex})</size>";

            var stats = _unitInfoPanel.transform.Find("InnerPanel/DetailsCard/StatsText")?.GetComponent<TextMeshProUGUI>();

            // Compile active status effects text description
            string effectsList = "";
            if (stack.ActiveEffects != null && stack.ActiveEffects.Count > 0)
            {
                List<string> effectNames = new List<string>();
                foreach (var fx in stack.ActiveEffects)
                {
                    effectNames.Add($"{fx.effectName} ({fx.duration} rounds)");
                }
                effectsList = string.Join(", ", effectNames);
            }
            else
            {
                effectsList = "<color=#888888>None</color>";
            }

            // Highlight positive/negative Morale & Luck color
            string moraleColor = stack.Morale > 0 ? "#44FF44" : (stack.Morale < 0 ? "#FF4444" : "#FFFFFF");
            string luckColor = stack.Luck > 0 ? "#44FF44" : (stack.Luck < 0 ? "#FF4444" : "#FFFFFF");

            string atkStr = stack.Attack != stack.Data.Attack ? $"{stack.Attack} <size=80%>(Base: {stack.Data.Attack})</size>" : $"{stack.Data.Attack}";
            string defStr = stack.Defense != stack.Data.Defense ? $"{stack.Defense} <size=80%>(Base: {stack.Data.Defense})</size>" : $"{stack.Data.Defense}";
            string speedStr = stack.Speed != stack.Data.Speed ? $"{stack.Speed} <size=80%>(Base: {stack.Data.Speed})</size>" : $"{stack.Data.Speed}";

            // Piece together details VERTICALLY (each attribute with distinct professional colors)
            string infoString = "";
            infoString += $"<color=#FFD700><b>Count:</b></color> <color=#FFF0A0><b>{stack.Count}</b></color>\n";
            infoString += $"<color=#55FF66><b>HP (Top Unit):</b></color> <color=#AAFFAA>{stack.CurrentHealth}/{stack.Data.MaxHealth}</color>\n";
            infoString += $"<color=#FF5555><b>Attack:</b></color> <color=#FF9999>{atkStr}</color>\n";
            infoString += $"<color=#44AAFF><b>Defense:</b></color> <color=#99CCFF>{defStr}</color>\n";
            infoString += $"<color=#FF8844><b>Speed:</b></color> <color=#FFBB99>{speedStr}</color>\n";
            infoString += $"<color=#FFCC00><b>Initiative:</b></color> <color=#FFE680>{stack.Initiative:F1}</color>\n";
            infoString += $"<color=#66FFBB><b>Morale:</b></color> <color={moraleColor}>+{stack.Morale}</color>\n";
            infoString += $"<color=#FFFF55><b>Luck:</b></color> <color={luckColor}>+{stack.Luck}</color>\n";
            
            string rangeType = stack.Data.IsRanged ? $"Ranged (Ammo: {stack.CurrentAmmo}/{stack.Data.MaxAmmo})" : "Melee";
            infoString += $"<color=#E066FF><b>Attack Type:</b></color> <color=#F0B3FF>{rangeType}</color>\n";
            infoString += $"<color=#FF44AA><b>Total Power:</b></color> <color=#FF99DD>{stack.TroopPower}</color>\n";

            // Add abilities if they exist
            string abilitiesList = "";
            if (stack.Data.Abilities != null && stack.Data.Abilities.Count > 0)
            {
                List<string> abNames = new List<string>();
                foreach (var ab in stack.Data.Abilities)
                {
                    abNames.Add(ab.GetType().Name.Replace("Ability", ""));
                }
                abilitiesList = string.Join(", ", abNames);
            }
            else
            {
                abilitiesList = "<color=#888888>None</color>";
            }

            infoString += $"<color=#FFFF88><b>Special Abilities:</b></color> <color=#FFFFDD>{abilitiesList}</color>\n";
            infoString += $"<color=#88CCFF><b>Active Effects:</b></color> <color=#CCEEFF>{effectsList}</color>";

            stats.text = infoString;
        }

        private void CreateUnitInfoPanelUI()
        {
            // Create root panel with Gold Frame & Dark Slate Background (Taller: 540x510)
            _unitInfoPanel = new GameObject("UnitInfoPanel");
            _unitInfoPanel.transform.SetParent(_canvasObj.transform, false);

            RectTransform rect = _unitInfoPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(540f, 510f);

            Image borderImg = _unitInfoPanel.AddComponent<Image>();
            borderImg.color = new Color(0.82f, 0.68f, 0.32f, 1f); // Rich Warm Gold Frame

            GameObject innerPanel = new GameObject("InnerPanel");
            innerPanel.transform.SetParent(_unitInfoPanel.transform, false);
            RectTransform inRect = innerPanel.AddComponent<RectTransform>();
            inRect.anchorMin = Vector2.zero;
            inRect.anchorMax = Vector2.one;
            inRect.offsetMin = new Vector2(5f, 5f);
            inRect.offsetMax = new Vector2(-5f, -5f);
            Image bg = innerPanel.AddComponent<Image>();
            bg.color = new Color(0.11f, 0.13f, 0.17f, 0.98f); // Deep Dark Slate Background

            // 1. TOP HEADER: Title Text (Upper Left)
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(innerPanel.transform, false);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(1f, 0.84f, 0f); // Bright Gold
            
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.04f, 0.91f);
            titleRect.anchorMax = new Vector2(0.85f, 0.97f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // 2. CLOSE BUTTON [X] (Upper Right)
            GameObject closeXObj = new GameObject("CloseXBtn");
            closeXObj.transform.SetParent(innerPanel.transform, false);
            RectTransform xBtnRect = closeXObj.AddComponent<RectTransform>();
            xBtnRect.anchorMin = new Vector2(0.88f, 0.91f);
            xBtnRect.anchorMax = new Vector2(0.96f, 0.97f);
            xBtnRect.offsetMin = Vector2.zero;
            xBtnRect.offsetMax = Vector2.zero;
            Image xImg = closeXObj.AddComponent<Image>();
            xImg.color = new Color(0.6f, 0.15f, 0.15f, 1f);
            Outline xOutline = closeXObj.AddComponent<Outline>();
            xOutline.effectColor = new Color(0.9f, 0.3f, 0.3f, 1f);
            xOutline.effectDistance = new Vector2(1.5f, 1.5f);
            Button xBtn = closeXObj.AddComponent<Button>();
            xBtn.onClick.AddListener(() => HideUnitInfoPanel());

            GameObject xTxtObj = new GameObject("XText");
            xTxtObj.transform.SetParent(closeXObj.transform, false);
            TextMeshProUGUI xTxt = xTxtObj.AddComponent<TextMeshProUGUI>();
            xTxt.alignment = TextAlignmentOptions.Center;
            xTxt.fontSize = 14;
            xTxt.fontStyle = FontStyles.Bold;
            xTxt.color = Color.white;
            xTxt.text = "<b>X</b>";
            RectTransform xTxtRect = xTxtObj.GetComponent<RectTransform>();
            xTxtRect.anchorMin = Vector2.zero;
            xTxtRect.anchorMax = Vector2.one;
            xTxtRect.offsetMin = Vector2.zero;
            xTxtRect.offsetMax = Vector2.zero;

            // 3. ICON PORTRAIT FRAME (Left Side - Tall Portrait Card)
            GameObject iconFrameObj = new GameObject("IconFrame");
            iconFrameObj.transform.SetParent(innerPanel.transform, false);
            RectTransform iconFrameRect = iconFrameObj.AddComponent<RectTransform>();
            iconFrameRect.anchorMin = new Vector2(0.04f, 0.10f);
            iconFrameRect.anchorMax = new Vector2(0.34f, 0.88f);
            iconFrameRect.offsetMin = Vector2.zero;
            iconFrameRect.offsetMax = Vector2.zero;
            Image iconFrameBg = iconFrameObj.AddComponent<Image>();
            iconFrameBg.color = new Color(0.07f, 0.08f, 0.11f, 1f);
            Outline iconOutline = iconFrameObj.AddComponent<Outline>();
            iconOutline.effectColor = new Color(0.75f, 0.62f, 0.3f, 1f);
            iconOutline.effectDistance = new Vector2(2f, 2f);

            GameObject iconObj = new GameObject("CreatureIcon");
            iconObj.transform.SetParent(iconFrameObj.transform, false);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.preserveAspect = true;
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(4f, 4f);
            iconRect.offsetMax = new Vector2(-4f, -4f);

            // 4. DETAILS CARD BLOCK (Right Side - Vertical Stats List)
            GameObject cardObj = new GameObject("DetailsCard");
            cardObj.transform.SetParent(innerPanel.transform, false);
            RectTransform cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.36f, 0.10f);
            cardRect.anchorMax = new Vector2(0.96f, 0.88f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;
            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.08f, 0.10f, 0.13f, 0.9f);
            Outline cardOutline = cardObj.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.3f, 0.35f, 0.45f, 0.8f);
            cardOutline.effectDistance = new Vector2(1f, 1f);

            GameObject statsObj = new GameObject("StatsText");
            statsObj.transform.SetParent(cardObj.transform, false);
            TextMeshProUGUI statsText = statsObj.AddComponent<TextMeshProUGUI>();
            statsText.alignment = TextAlignmentOptions.TopLeft;
            statsText.fontSize = 13;
            statsText.color = Color.white;
            statsText.lineSpacing = 4f;

            RectTransform statsRect = statsObj.GetComponent<RectTransform>();
            statsRect.anchorMin = Vector2.zero;
            statsRect.anchorMax = Vector2.one;
            statsRect.offsetMin = new Vector2(12f, 8f);
            statsRect.offsetMax = new Vector2(-12f, -8f);

            // 5. BOTTOM CLOSE BUTTON
            GameObject closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(innerPanel.transform, false);
            RectTransform closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.38f, 0.02f);
            closeRect.anchorMax = new Vector2(0.62f, 0.09f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;

            Image closeImg = closeObj.AddComponent<Image>();
            closeImg.color = new Color(0.45f, 0.15f, 0.15f, 1f);
            Outline closeOutline = closeObj.AddComponent<Outline>();
            closeOutline.effectColor = new Color(0.75f, 0.3f, 0.3f, 1f);
            closeOutline.effectDistance = new Vector2(1.5f, 1.5f);

            Button closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => HideUnitInfoPanel());

            GameObject closeLblObj = new GameObject("Label");
            closeLblObj.transform.SetParent(closeObj.transform, false);
            TextMeshProUGUI closeLbl = closeLblObj.AddComponent<TextMeshProUGUI>();
            closeLbl.alignment = TextAlignmentOptions.Center;
            closeLbl.fontSize = 12;
            closeLbl.fontStyle = FontStyles.Bold;
            closeLbl.color = Color.white;
            closeLbl.text = "<b>CLOSE</b>";

            RectTransform closeLblRect = closeLblObj.GetComponent<RectTransform>();
            closeLblRect.anchorMin = Vector2.zero;
            closeLblRect.anchorMax = Vector2.one;
            closeLblRect.offsetMin = Vector2.zero;
            closeLblRect.offsetMax = Vector2.zero;
        }

        public void ShowHeroInfoPanel(Heroes.Hero hero)
        {
            if (hero == null) return;
            HideSpellBook();

            if (_unitInfoPanel != null && _unitInfoPanel.transform.Find("InnerPanel") == null)
            {
                Destroy(_unitInfoPanel);
                _unitInfoPanel = null;
            }

            if (_unitInfoPanel == null)
            {
                CreateUnitInfoPanelUI();
            }

            _unitInfoPanel.SetActive(true);

            // Get hero data from GameDataManager to format artifact bonuses & portrait
            var gameData = HommClone.World.GameDataManager.GetOrCreateInstance();
            HommClone.World.HeroData heroData = null;
            if (gameData != null)
            {
                if (gameData.isPvPBattle)
                {
                    heroData = (hero.PlayerIndex == 1) ? gameData.player1Hero : gameData.player2Hero;
                }
                else
                {
                    heroData = (hero.PlayerIndex == 1) ? gameData.GetActiveHero() : null;
                }
            }

            // Populate Hero Icon
            var iconImg = _unitInfoPanel.transform.Find("InnerPanel/IconFrame/CreatureIcon")?.GetComponent<Image>();
            Sprite heroSprite = (heroData != null && heroData.heroPortrait != null) ? heroData.heroPortrait : hero.Icon;
            if (iconImg != null)
            {
                if (heroSprite != null)
                {
                    iconImg.gameObject.SetActive(true);
                    iconImg.sprite = heroSprite;
                    iconImg.color = Color.white;
                }
                else
                {
                    iconImg.gameObject.SetActive(false);
                }
            }

            var title = _unitInfoPanel.transform.Find("InnerPanel/TitleText")?.GetComponent<TextMeshProUGUI>();
            if (title != null) title.text = $"<color=#FFD700><b>[HERO] {hero.Name}</b></color> <size=80%>(Player {hero.PlayerIndex})</size>";

            var stats = _unitInfoPanel.transform.Find("InnerPanel/DetailsCard/StatsText")?.GetComponent<TextMeshProUGUI>();

            // Format Hero stats VERTICALLY with distinct professional colors
            string infoString = "";
            infoString += $"<color=#FF5555><b>Attack:</b></color> <color=#FF9999>{hero.Attack}</color>";
            if (heroData != null) infoString += $" <size=80%>(Base: {heroData.attack})</size>";
            infoString += "\n";

            infoString += $"<color=#44AAFF><b>Defense:</b></color> <color=#99CCFF>{hero.Defense}</color>";
            if (heroData != null) infoString += $" <size=80%>(Base: {heroData.defense})</size>";
            infoString += "\n";

            infoString += $"<color=#CC66FF><b>Spell Power:</b></color> <color=#E6B3FF>{hero.SpellPower}</color>";
            if (heroData != null) infoString += $" <size=80%>(Base: {heroData.spellPower})</size>";
            infoString += "\n";

            infoString += $"<color=#FFCC00><b>Knowledge:</b></color> <color=#FFE680>{hero.Knowledge}</color>";
            if (heroData != null) infoString += $" <size=80%>(Base: {heroData.knowledge})</size>";
            infoString += "\n";

            infoString += $"<color=#33CCFF><b>Mana Pool:</b></color> <color=#99E6FF>{hero.CurrentMana} / {hero.MaxMana}</color>\n";
            infoString += $"<color=#FF8844><b>Initiative:</b></color> <color=#FFBB99>{hero.Initiative:F1}</color>\n";

            int morale = heroData != null ? heroData.GetTotalMorale() : hero.Morale;
            int luck = heroData != null ? heroData.GetTotalLuck() : hero.Luck;
            string moraleColor = morale > 0 ? "#44FF44" : (morale < 0 ? "#FF4444" : "#FFFFFF");
            string luckColor = luck > 0 ? "#FFFF44" : (luck < 0 ? "#FF4444" : "#FFFFFF");
            infoString += $"<color=#66FFBB><b>Morale Boost:</b></color> <color={moraleColor}>+{morale}</color>\n";
            infoString += $"<color=#FFFF55><b>Luck Boost:</b></color> <color={luckColor}>+{luck}</color>\n\n";

            string artifactsFormatted = "";
            if (heroData != null && heroData.equippedArtifacts != null && heroData.equippedArtifacts.Count > 0)
            {
                List<string> artNames = new List<string>();
                foreach (var art in heroData.equippedArtifacts)
                {
                    if (art != null) artNames.Add(art.name);
                }
                artifactsFormatted = string.Join(", ", artNames);
            }
            else
            {
                artifactsFormatted = "<color=#888888>None</color>";
            }

            infoString += $"<color=#FFFF88><b>Equipped Artifacts:</b></color> <color=#FFFFDD>{artifactsFormatted}</color>";

            stats.text = infoString;
        }
    }
}
