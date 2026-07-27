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

        public void ShowSpellBook()
        {
            if (_turnManager == null || _turnManager.ActiveUnit == null) return;
            Turns.ITimelineParticipant active = _turnManager.ActiveUnit;
            
            List<Spell> spells = null;
            int currentMana = 0;
            int maxMana = 0;

            if (active is Heroes.Hero hero)
            {
                spells = hero.Spells;
                currentMana = hero.CurrentMana;
                maxMana = hero.MaxMana;
            }
            else if (active is CreatureStack stack)
            {
                spells = stack.Spells;
                currentMana = stack.CurrentMana;
                maxMana = stack.MaxMana;
            }

            if (spells == null || spells.Count == 0 || maxMana == 0) return;

            // Recreate or enable panel
            if (_spellBookPanel == null)
            {
                _spellBookPanel = new GameObject("SpellBookPanel");
                _spellBookPanel.transform.SetParent(_canvasObj.transform, false);
                RectTransform rect = _spellBookPanel.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(320f, 240f);

                Image bg = _spellBookPanel.AddComponent<Image>();
                bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f); // Charcoal dark glass

                Outline border = _spellBookPanel.AddComponent<Outline>();
                border.effectColor = new Color(0.6f, 0.5f, 0.3f, 0.8f); // Golden/Bronze book outline
                border.effectDistance = new Vector2(2f, 2f);
            }

            _spellBookPanel.SetActive(true);

            // Destroy old content of spell list
            foreach (Transform child in _spellBookPanel.transform)
            {
                if (child.gameObject.name != "Title" && child.gameObject.name != "CloseButton")
                {
                    Destroy(child.gameObject);
                }
            }

            // Create Title
            GameObject titleObj = _spellBookPanel.transform.Find("Title")?.gameObject;
            if (titleObj == null)
            {
                titleObj = new GameObject("Title");
                titleObj.transform.SetParent(_spellBookPanel.transform, false);
                TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
                titleTxt.alignment = TextAlignmentOptions.Center;
                titleTxt.fontSize = 15;
                titleTxt.color = new Color(0.9f, 0.8f, 0.6f, 1f); // Pale gold

                RectTransform titleRect = titleObj.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(0f, -12f);
                titleRect.sizeDelta = new Vector2(0f, 40f);
            }
            titleObj.GetComponent<TextMeshProUGUI>().text = $"<b>SPELL BOOK</b>\n<size=80%>Mana: {currentMana} / {maxMana}</size>";

            // Create Spells Grid container
            GameObject gridObj = new GameObject("SpellsGrid");
            gridObj.transform.SetParent(_spellBookPanel.transform, false);
            RectTransform gridRect = gridObj.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0f, 0f);
            gridRect.anchorMax = new Vector2(1f, 1f);
            gridRect.offsetMin = new Vector2(15f, 45f); // padding bottom
            gridRect.offsetMax = new Vector2(-15f, -60f); // padding top

            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(135f, 40f);
            grid.spacing = new Vector2(12f, 8f);
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            // Instantiate spell buttons
            foreach (var spell in spells)
            {
                if (spell == null) continue;
                
                GameObject btnObj = new GameObject($"Spell_{spell.SpellName}");
                btnObj.transform.SetParent(gridObj.transform, false);

                Image img = btnObj.AddComponent<Image>();
                img.color = new Color(0.16f, 0.16f, 0.16f, 1f);

                Button btn = btnObj.AddComponent<Button>();
                ColorBlock cb = btn.colors;
                cb.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
                cb.highlightedColor = new Color(0.3f, 0.35f, 0.4f, 1f);
                cb.pressedColor = new Color(0.12f, 0.14f, 0.16f, 1f);
                cb.disabledColor = new Color(0.08f, 0.08f, 0.08f, 0.5f);
                btn.colors = cb;

                GameObject lblObj = new GameObject("Label");
                lblObj.transform.SetParent(btnObj.transform, false);
                TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
                lbl.alignment = TextAlignmentOptions.Center;
                lbl.fontSize = 11;
                lbl.color = Color.white;
                lbl.text = $"<b>{spell.SpellName}</b>\n<size=80%>{spell.ManaCost} Mana</size>";

                RectTransform lblRect = lblObj.GetComponent<RectTransform>();
                lblRect.anchorMin = Vector2.zero;
                lblRect.anchorMax = Vector2.one;
                lblRect.sizeDelta = Vector2.zero;

                // Disable if not enough mana
                if (currentMana < spell.ManaCost)
                {
                    btn.interactable = false;
                    lbl.color = new Color(1f, 1f, 1f, 0.4f);
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

            // Create Close button at bottom
            GameObject closeObj = _spellBookPanel.transform.Find("CloseButton")?.gameObject;
            if (closeObj == null)
            {
                closeObj = new GameObject("CloseButton");
                closeObj.transform.SetParent(_spellBookPanel.transform, false);
                RectTransform closeRect = closeObj.AddComponent<RectTransform>();
                closeRect.anchorMin = new Vector2(0.5f, 0f);
                closeRect.anchorMax = new Vector2(0.5f, 0f);
                closeRect.pivot = new Vector2(0.5f, 0f);
                closeRect.anchoredPosition = new Vector2(0f, 10f);
                closeRect.sizeDelta = new Vector2(100f, 25f);

                Image closeImg = closeObj.AddComponent<Image>();
                closeImg.color = new Color(0.25f, 0.12f, 0.12f, 1f); // Dark reddish close button

                Button closeBtn = closeObj.AddComponent<Button>();
                closeBtn.onClick.AddListener(() => HideSpellBook());

                GameObject closeLblObj = new GameObject("Label");
                closeLblObj.transform.SetParent(closeObj.transform, false);
                TextMeshProUGUI closeLbl = closeLblObj.AddComponent<TextMeshProUGUI>();
                closeLbl.alignment = TextAlignmentOptions.Center;
                closeLbl.fontSize = 11;
                closeLbl.color = Color.white;
                closeLbl.text = "<b>CLOSE</b>";

                RectTransform closeLblRect = closeLblObj.GetComponent<RectTransform>();
                closeLblRect.anchorMin = Vector2.zero;
                closeLblRect.anchorMax = Vector2.one;
                closeLblRect.sizeDelta = Vector2.zero;
            }
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

            if (_unitInfoPanel == null)
            {
                // Create root panel
                _unitInfoPanel = new GameObject("UnitInfoPanel");
                _unitInfoPanel.transform.SetParent(_canvasObj.transform, false);

                RectTransform rect = _unitInfoPanel.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(400f, 320f); // Sleek medium box

                Image bg = _unitInfoPanel.AddComponent<Image>();
                bg.color = new Color(0.12f, 0.12f, 0.14f, 0.95f); // Elegant dark slate gray

                // Border/outline
                Outline outline = _unitInfoPanel.AddComponent<Outline>();
                outline.effectColor = activeFrameColor;
                outline.effectDistance = new Vector2(2f, 2f);

                // Add Title text
                GameObject titleObj = new GameObject("TitleText");
                titleObj.transform.SetParent(_unitInfoPanel.transform, false);
                TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
                titleText.alignment = TextAlignmentOptions.Top;
                titleText.fontSize = 18;
                titleText.fontStyle = FontStyles.Bold;
                titleText.color = Color.yellow;
                
                RectTransform titleRect = titleObj.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(0f, -10f);
                titleRect.sizeDelta = new Vector2(-20f, 30f);

                // Add Stats text block
                GameObject statsObj = new GameObject("StatsText");
                statsObj.transform.SetParent(_unitInfoPanel.transform, false);
                TextMeshProUGUI statsText = statsObj.AddComponent<TextMeshProUGUI>();
                statsText.alignment = TextAlignmentOptions.TopLeft;
                statsText.fontSize = 13;
                statsText.color = Color.white;

                RectTransform statsRect = statsObj.GetComponent<RectTransform>();
                statsRect.anchorMin = Vector2.zero;
                statsRect.anchorMax = Vector2.one;
                statsRect.offsetMin = new Vector2(15f, 50f); // padding left & bottom for close button
                statsRect.offsetMax = new Vector2(-15f, -45f); // padding right & top

                // Add Close button at bottom
                GameObject closeObj = new GameObject("CloseButton");
                closeObj.transform.SetParent(_unitInfoPanel.transform, false);
                RectTransform closeRect = closeObj.AddComponent<RectTransform>();
                closeRect.anchorMin = new Vector2(0.5f, 0f);
                closeRect.anchorMax = new Vector2(0.5f, 0f);
                closeRect.pivot = new Vector2(0.5f, 0f);
                closeRect.anchoredPosition = new Vector2(0f, 12f);
                closeRect.sizeDelta = new Vector2(100f, 28f);

                Image closeImg = closeObj.AddComponent<Image>();
                closeImg.color = new Color(0.3f, 0.15f, 0.15f, 1f);

                Button closeBtn = closeObj.AddComponent<Button>();
                closeBtn.onClick.AddListener(() => HideUnitInfoPanel());

                GameObject closeLblObj = new GameObject("Label");
                closeLblObj.transform.SetParent(closeObj.transform, false);
                TextMeshProUGUI closeLbl = closeLblObj.AddComponent<TextMeshProUGUI>();
                closeLbl.alignment = TextAlignmentOptions.Center;
                closeLbl.fontSize = 11;
                closeLbl.fontStyle = FontStyles.Bold;
                closeLbl.color = Color.white;
                closeLbl.text = "CLOSE";

                RectTransform closeLblRect = closeLblObj.GetComponent<RectTransform>();
                closeLblRect.anchorMin = Vector2.zero;
                closeLblRect.anchorMax = Vector2.one;
                closeLblRect.sizeDelta = Vector2.zero;
            }

            _unitInfoPanel.SetActive(true);

            // Populate Info text content
            var title = _unitInfoPanel.transform.Find("TitleText").GetComponent<TextMeshProUGUI>();
            title.text = $"<b>{stack.Name}</b> (Player {stack.PlayerIndex})";

            var stats = _unitInfoPanel.transform.Find("StatsText").GetComponent<TextMeshProUGUI>();

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

            // Piece together details
            string infoString = "";
            infoString += $"<b>Count:</b> {stack.Count}  |  <b>HP (Top Unit):</b> {stack.CurrentHealth}/{stack.Data.MaxHealth}\n";
            infoString += $"<b>Attack:</b> {stack.Attack} (Base: {stack.Data.Attack})  |  <b>Defense:</b> {stack.Defense} (Base: {stack.Data.Defense})\n";
            infoString += $"<b>Speed:</b> {stack.Speed} (Base: {stack.Data.Speed})  |  <b>Initiative:</b> {stack.Initiative:F1}\n";
            
            // Highlight positive/negative Morale & Luck color
            string moraleColor = stack.Morale > 0 ? "#44ff44" : (stack.Morale < 0 ? "#ff4444" : "#ffffff");
            string luckColor = stack.Luck > 0 ? "#44ff44" : (stack.Luck < 0 ? "#ff4444" : "#ffffff");
            
            infoString += $"<b>Morale:</b> <color={moraleColor}>{stack.Morale}</color>  |  <b>Luck:</b> <color={luckColor}>{stack.Luck}</color>\n";
            
            string rangeType = stack.Data.IsRanged ? $"Ranged (Ammo: {stack.CurrentAmmo}/{stack.Data.MaxAmmo})" : "Melee";
            infoString += $"<b>Attack Type:</b> {rangeType}  |  <b>Power:</b> {stack.TroopPower} (Single: {stack.Data.AIValue})\n\n";

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

            infoString += $"<b>Special Abilities:</b> {abilitiesList}\n";
            infoString += $"<b>Active Effects:</b> {effectsList}\n";

            stats.text = infoString;
        }
    }
}
