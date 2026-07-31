using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HommClone.Heroes;
using HommClone.World;

namespace HommClone.UI
{
    public class AdventureSkillTreeUI : MonoBehaviour
    {
        public static AdventureSkillTreeUI Instance { get; private set; }

        private GameObject _panel;
        private Text _titleText;
        private Text _tokenText;
        private Transform _nodeContainer;
        private GameObject _tooltipObj;
        private Text _tooltipText;

        private HeroData _currentHero;

        private readonly Dictionary<string, GameObject> _nodeUIObjects = new Dictionary<string, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildUI();
            Hide();
        }

        public static AdventureSkillTreeUI GetOrCreateInstance()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<AdventureSkillTreeUI>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject canvasObj = GameObject.Find("Canvas");
                    if (canvasObj == null)
                    {
                        canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                        canvasObj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    }
                    GameObject uiObj = new GameObject("AdventureSkillTreeUI");
                    uiObj.transform.SetParent(canvasObj.transform, false);
                    Instance = uiObj.AddComponent<AdventureSkillTreeUI>();
                }
            }
            return Instance;
        }

        private void BuildUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // Main Background Panel
            _panel = new GameObject("AdventureSkillTreePanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(720, 520);

            Image panelImg = _panel.GetComponent<Image>();
            panelImg.color = new Color(0.1f, 0.09f, 0.12f, 0.96f);

            // Gold Outer Border
            Outline outline = _panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.9f, 0.75f, 0.2f, 0.9f);
            outline.effectDistance = new Vector2(3, -3);

            // Title Banner
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(_panel.transform, false);

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -15);
            titleRect.sizeDelta = new Vector2(0, 40);

            _titleText = titleObj.GetComponent<Text>();
            _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _titleText.fontSize = 24;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.color = new Color(1f, 0.85f, 0.3f);
            _titleText.text = "ADVENTURE & NAVIGATION SKILL TREE";

            // Token Counter Banner
            GameObject tokenObj = new GameObject("TokenText", typeof(RectTransform), typeof(Text));
            tokenObj.transform.SetParent(_panel.transform, false);

            RectTransform tokenRect = tokenObj.GetComponent<RectTransform>();
            tokenRect.anchorMin = new Vector2(0f, 1f);
            tokenRect.anchorMax = new Vector2(1f, 1f);
            tokenRect.pivot = new Vector2(0.5f, 1f);
            tokenRect.anchoredPosition = new Vector2(0, -55);
            tokenRect.sizeDelta = new Vector2(0, 30);

            _tokenText = tokenObj.GetComponent<Text>();
            _tokenText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _tokenText.fontSize = 18;
            _tokenText.alignment = TextAnchor.MiddleCenter;
            _tokenText.color = new Color(0.4f, 1f, 0.4f);
            _tokenText.text = "Available Tokens: 0";

            // Node Tree Container
            GameObject containerObj = new GameObject("NodeContainer", typeof(RectTransform));
            containerObj.transform.SetParent(_panel.transform, false);
            _nodeContainer = containerObj.transform;

            RectTransform containerRect = containerObj.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.05f, 0.1f);
            containerRect.anchorMax = new Vector2(0.95f, 0.82f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = Vector2.zero;

            // Close Button
            GameObject closeObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObj.transform.SetParent(_panel.transform, false);

            RectTransform closeRect = closeObj.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0, 15);
            closeRect.sizeDelta = new Vector2(140, 38);

            closeObj.GetComponent<Image>().color = new Color(0.5f, 0.15f, 0.15f, 0.95f);
            closeObj.GetComponent<Button>().onClick.AddListener(Hide);

            GameObject closeTextObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            closeTextObj.transform.SetParent(closeObj.transform, false);
            RectTransform ctRect = closeTextObj.GetComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.sizeDelta = Vector2.zero;

            Text ct = closeTextObj.GetComponent<Text>();
            ct.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ct.fontSize = 16;
            ct.fontStyle = FontStyle.Bold;
            ct.alignment = TextAnchor.MiddleCenter;
            ct.color = Color.white;
            ct.text = "CLOSE";

            // Tooltip Popup
            BuildTooltipUI();
        }

        private void BuildTooltipUI()
        {
            _tooltipObj = new GameObject("SkillTooltip", typeof(RectTransform), typeof(Image));
            _tooltipObj.transform.SetParent(_panel.transform, false);

            RectTransform ttRect = _tooltipObj.GetComponent<RectTransform>();
            ttRect.sizeDelta = new Vector2(240, 90);

            Image ttImg = _tooltipObj.GetComponent<Image>();
            ttImg.color = new Color(0.05f, 0.05f, 0.07f, 0.95f);

            Outline ttOutline = _tooltipObj.AddComponent<Outline>();
            ttOutline.effectColor = new Color(0.8f, 0.7f, 0.2f, 0.9f);
            ttOutline.effectDistance = new Vector2(1, -1);

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(_tooltipObj.transform, false);
            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = new Vector2(-12, -12);

            _tooltipText = txtObj.GetComponent<Text>();
            _tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _tooltipText.fontSize = 13;
            _tooltipText.color = Color.white;
            _tooltipText.alignment = TextAnchor.UpperLeft;

            _tooltipObj.SetActive(false);
        }

        public void Show(HeroData hero = null)
        {
            if (hero == null)
            {
                var manager = GameDataManager.GetOrCreateInstance();
                hero = manager != null ? manager.GetActiveHero() : null;
            }
            if (hero == null) return;

            _currentHero = hero;
            if (_currentHero.unlockedAdventureSkills == null)
            {
                _currentHero.unlockedAdventureSkills = new List<string>();
            }

            if (_panel == null) BuildUI();
            _panel.SetActive(true);

            RefreshUI();
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_tooltipObj != null) _tooltipObj.SetActive(false);
        }

        public void RefreshUI()
        {
            if (_currentHero == null) return;

            int tokens = _currentHero.adventureSkillTokens;
            _tokenText.text = $"<b>{_currentHero.heroName}</b> — Available Skill Tokens: <color=#66ff66>{tokens}</color>";

            // Clear old node UI
            foreach (Transform child in _nodeContainer)
            {
                Destroy(child.gameObject);
            }
            _nodeUIObjects.Clear();

            var allNodes = AdventureSkillTree.GetAllNodes();

            // Grid offsets: 3 Columns (X: 0, 1, 2), 3 Rows (Y: 0, 1, 2)
            float colWidth = 210f;
            float rowHeight = 110f;
            Vector2 startPos = new Vector2(-colWidth, rowHeight);

            foreach (var node in allNodes)
            {
                GameObject nodeObj = new GameObject($"Node_{node.id}", typeof(RectTransform), typeof(Image), typeof(Button));
                nodeObj.transform.SetParent(_nodeContainer, false);
                _nodeUIObjects[node.id] = nodeObj;

                RectTransform nRect = nodeObj.GetComponent<RectTransform>();
                nRect.sizeDelta = new Vector2(180, 80);
                nRect.anchoredPosition = new Vector2(startPos.x + node.uiGridPos.x * colWidth, startPos.y - node.uiGridPos.y * rowHeight);

                bool isUnlocked = _currentHero.unlockedAdventureSkills.Contains(node.id);
                bool hasPrereq = string.IsNullOrEmpty(node.prerequisiteId) || _currentHero.unlockedAdventureSkills.Contains(node.prerequisiteId);
                bool canAfford = tokens >= node.tokenCost;
                bool isAvailable = !isUnlocked && hasPrereq && canAfford;

                Image bgImg = nodeObj.GetComponent<Image>();
                Button btn = nodeObj.GetComponent<Button>();

                // Visual Styling
                Outline border = nodeObj.AddComponent<Outline>();
                border.effectDistance = new Vector2(2, -2);

                if (isUnlocked)
                {
                    bgImg.color = new Color(0.25f, 0.2f, 0.08f, 0.95f);
                    border.effectColor = new Color(0.95f, 0.85f, 0.3f, 0.95f);
                }
                else if (isAvailable)
                {
                    bgImg.color = new Color(0.1f, 0.25f, 0.12f, 0.95f);
                    border.effectColor = new Color(0.3f, 0.9f, 0.4f, 0.95f);
                }
                else
                {
                    bgImg.color = new Color(0.12f, 0.12f, 0.14f, 0.8f);
                    border.effectColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);
                }

                // Inner Layout Text
                GameObject labelObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                labelObj.transform.SetParent(nodeObj.transform, false);
                RectTransform lRect = labelObj.GetComponent<RectTransform>();
                lRect.anchorMin = Vector2.zero;
                lRect.anchorMax = Vector2.one;
                lRect.sizeDelta = new Vector2(-12, -12);

                Text label = labelObj.GetComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 12;
                label.alignment = TextAnchor.UpperLeft;

                string statusColor = isUnlocked ? "#ffd700" : (isAvailable ? "#66ff66" : "#aaaaaa");
                label.text = $"{node.iconSymbol} <b>{node.name}</b>\n<size=10><color=#cccccc>{node.description}</color></size>";

                // Bottom-Right Token Cost Badge (matching Paint diagram!)
                GameObject badgeObj = new GameObject("CostBadge", typeof(RectTransform), typeof(Text));
                badgeObj.transform.SetParent(nodeObj.transform, false);
                RectTransform bRect = badgeObj.GetComponent<RectTransform>();
                bRect.anchorMin = new Vector2(1f, 0f);
                bRect.anchorMax = new Vector2(1f, 0f);
                bRect.pivot = new Vector2(1f, 0f);
                bRect.anchoredPosition = new Vector2(-6, 4);
                bRect.sizeDelta = new Vector2(30, 20);

                Text bText = badgeObj.GetComponent<Text>();
                bText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                bText.fontSize = 14;
                bText.fontStyle = FontStyle.Bold;
                bText.alignment = TextAnchor.LowerRight;
                bText.color = isUnlocked ? new Color(1f, 0.85f, 0.3f) : (isAvailable ? new Color(0.4f, 1f, 0.4f) : new Color(0.6f, 0.6f, 0.6f));

                string romanCost = node.tokenCost == 1 ? "I" : (node.tokenCost == 2 ? "II" : "III");
                bText.text = romanCost;

                // Click Action
                btn.onClick.RemoveAllListeners();
                if (isAvailable)
                {
                    btn.interactable = true;
                    btn.onClick.AddListener(() => UnlockNode(node));
                }
                else
                {
                    btn.interactable = false;
                }

                // Hover Event Triggers for Tooltip
                var trigger = nodeObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                
                var entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
                entryEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
                entryEnter.callback.AddListener((data) => ShowTooltip(node, nRect.anchoredPosition));
                trigger.triggers.Add(entryEnter);

                var entryExit = new UnityEngine.EventSystems.EventTrigger.Entry();
                entryExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
                entryExit.callback.AddListener((data) => HideTooltip());
                trigger.triggers.Add(entryExit);
            }
        }

        private void UnlockNode(AdventureSkillNode node)
        {
            if (_currentHero == null || node == null) return;
            if (_currentHero.adventureSkillTokens < node.tokenCost) return;

            _currentHero.adventureSkillTokens -= node.tokenCost;
            _currentHero.unlockedAdventureSkills.Add(node.id);

            // Apply immediate effect (e.g. recalculate max MP)
            _currentHero.currentMovementPoints = _currentHero.GetEffectiveMaxMovementPoints();

            Debug.Log($"[AdventureSkillTree] Hero '{_currentHero.heroName}' unlocked '{node.name}'!");
            RefreshUI();
        }

        private void ShowTooltip(AdventureSkillNode node, Vector2 nodePos)
        {
            if (_tooltipObj == null || node == null) return;

            string reqStr = string.IsNullOrEmpty(node.prerequisiteId) ? "None" : AdventureSkillTree.GetNodeById(node.prerequisiteId)?.name ?? "None";
            _tooltipText.text = $"<b>{node.iconSymbol} {node.name}</b>\n<color=#dddddd>{node.description}</color>\n<color=#aaaaaa>Req: {reqStr}</color>";

            RectTransform ttRect = _tooltipObj.GetComponent<RectTransform>();
            ttRect.anchoredPosition = nodePos + new Vector2(0, 75);
            _tooltipObj.SetActive(true);
            _tooltipObj.transform.SetAsLastSibling();
        }

        private void HideTooltip()
        {
            if (_tooltipObj != null) _tooltipObj.SetActive(false);
        }
    }
}
