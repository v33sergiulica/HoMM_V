using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HommClone.Creatures;

namespace HommClone.World
{
    /// <summary>
    /// UI Window Modal displaying Hero Stats (Attack, Defense, Spell Power, Knowledge),
    /// Movement Points progress bar, and all 7 Army Slots with creature icons & counts.
    /// Opens when Right-Clicking the Hero avatar or Hero HUD icon.
    /// </summary>
    public class HeroCharacterSheetUI : MonoBehaviour
    {
        public static HeroCharacterSheetUI Instance { get; private set; }

        private GameObject _sheetCanvasObj;
        private GameObject _panelObj;

        private TextMeshProUGUI _heroNameText;
        private Image _heroPortraitImage;
        private TextMeshProUGUI _statsText;
        private TextMeshProUGUI _mpText;
        private Image _mpBarFill;

        private List<Image> _armySlotIcons = new List<Image>();
        private List<TextMeshProUGUI> _armySlotCountTexts = new List<TextMeshProUGUI>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            CreateUIWindow();
            HideWindow();
        }

        public static HeroCharacterSheetUI GetOrCreateInstance()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<HeroCharacterSheetUI>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject obj = new GameObject("HeroCharacterSheetUI_Manager");
                    Instance = obj.AddComponent<HeroCharacterSheetUI>();
                }
            }
            return Instance;
        }

        public void ShowWindow(HeroData data)
        {
            if (_sheetCanvasObj == null) CreateUIWindow();
            if (_sheetCanvasObj != null)
            {
                _sheetCanvasObj.SetActive(true);
                RefreshData(data);
                Debug.Log($"[HeroCharacterSheetUI] Character Sheet Window Opened for {data?.heroName ?? "Hero"}!");
            }
        }

        public void HideWindow()
        {
            if (_sheetCanvasObj != null)
            {
                _sheetCanvasObj.SetActive(false);
            }
        }

        public void ToggleWindow(HeroData data)
        {
            if (_sheetCanvasObj != null && _sheetCanvasObj.activeSelf)
            {
                HideWindow();
            }
            else
            {
                ShowWindow(data);
            }
        }

        private void RefreshData(HeroData data)
        {
            if (data == null) return;

            if (_heroNameText != null) _heroNameText.text = $"<b>{data.heroName}</b>";

            if (_heroPortraitImage != null)
            {
                if (data.heroPortrait != null)
                {
                    _heroPortraitImage.gameObject.SetActive(true);
                    _heroPortraitImage.sprite = data.heroPortrait;
                }
                else
                {
                    _heroPortraitImage.gameObject.SetActive(false);
                }
            }

            if (_statsText != null)
            {
                _statsText.text = $"<color=#FF6666><b>Attack:</b></color> {data.attack}    <color=#66AAFF><b>Defense:</b></color> {data.defense}\n<color=#CC66FF><b>Spell Power:</b></color> {data.spellPower}    <color=#FFCC00><b>Knowledge:</b></color> {data.knowledge}";
            }

            if (_mpText != null)
            {
                _mpText.text = $"Movement: <b>{data.currentMovementPoints:F1} / {data.maxMovementPoints:F1} MP</b>";
            }

            if (_mpBarFill != null && data.maxMovementPoints > 0)
            {
                _mpBarFill.fillAmount = Mathf.Clamp01(data.currentMovementPoints / data.maxMovementPoints);
            }

            // Ensure starter armies are initialized if army is empty
            if (data.army == null || data.army.Count == 0)
            {
                var gManager = GameDataManager.GetOrCreateInstance();
                if (gManager != null) gManager.InitializeStarterArmies();
            }

            // Populate 7 Army Slots
            for (int i = 0; i < 7; i++)
            {
                if (i < _armySlotIcons.Count)
                {
                    if (data.army != null && i < data.army.Count && data.army[i] != null && data.army[i].creatureData != null && data.army[i].count > 0)
                    {
                        var slot = data.army[i];
                        _armySlotIcons[i].gameObject.SetActive(true);

                        if (slot.creatureData.Icon != null)
                        {
                            _armySlotIcons[i].sprite = slot.creatureData.Icon;
                            _armySlotIcons[i].color = Color.white;
                        }
                        else
                        {
                            // Fallback colored icon if sprite icon not set yet
                            _armySlotIcons[i].sprite = null;
                            _armySlotIcons[i].color = new Color(0.35f, 0.55f, 0.85f, 1f);
                        }

                        if (_armySlotCountTexts[i] != null)
                        {
                            _armySlotCountTexts[i].text = $"<b>{slot.count}</b>";
                        }
                    }
                    else
                    {
                        // Empty Slot
                        _armySlotIcons[i].gameObject.SetActive(false);
                        if (_armySlotCountTexts[i] != null) _armySlotCountTexts[i].text = "";
                    }
                }
            }
        }

        private void CreateUIWindow()
        {
            if (_sheetCanvasObj != null) return;

            _sheetCanvasObj = new GameObject("HeroCharacterSheet_Canvas");
            _sheetCanvasObj.transform.SetParent(transform, false);

            Canvas canvas = _sheetCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99; // Top overlay
            _sheetCanvasObj.AddComponent<CanvasScaler>();
            _sheetCanvasObj.AddComponent<GraphicRaycaster>();

            // Dark semi-transparent background overlay
            GameObject overlay = new GameObject("DarkOverlay");
            overlay.transform.SetParent(_sheetCanvasObj.transform, false);
            RectTransform overRect = overlay.AddComponent<RectTransform>();
            overRect.anchorMin = Vector2.zero;
            overRect.anchorMax = Vector2.one;
            overRect.offsetMin = Vector2.zero;
            overRect.offsetMax = Vector2.zero;
            Image overImg = overlay.AddComponent<Image>();
            overImg.color = new Color(0f, 0f, 0f, 0.6f);

            Button overBtn = overlay.AddComponent<Button>();
            overBtn.onClick.AddListener(HideWindow);

            // Main Window Panel with Gold Outline Border
            _panelObj = new GameObject("CharacterSheetPanel");
            _panelObj.transform.SetParent(_sheetCanvasObj.transform, false);
            RectTransform pRect = _panelObj.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.anchoredPosition = Vector2.zero;
            pRect.sizeDelta = new Vector2(460f, 390f);

            // Border Outline
            Image borderImg = _panelObj.AddComponent<Image>();
            borderImg.color = new Color(0.75f, 0.65f, 0.35f, 1f); // Dark Gold Border

            GameObject innerPanel = new GameObject("InnerPanel");
            innerPanel.transform.SetParent(_panelObj.transform, false);
            RectTransform inRect = innerPanel.AddComponent<RectTransform>();
            inRect.anchorMin = Vector2.zero;
            inRect.anchorMax = Vector2.one;
            inRect.offsetMin = new Vector2(4f, 4f);
            inRect.offsetMax = new Vector2(-4f, -4f);
            Image pImg = innerPanel.AddComponent<Image>();
            pImg.color = new Color(0.1f, 0.12f, 0.16f, 0.98f); // Dark Slate Panel Background

            // Title
            GameObject titleObj = new GameObject("HeroTitle");
            titleObj.transform.SetParent(innerPanel.transform, false);
            _heroNameText = titleObj.AddComponent<TextMeshProUGUI>();
            _heroNameText.fontSize = 20;
            _heroNameText.color = Color.gold;
            _heroNameText.alignment = TextAlignmentOptions.Left;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.24f, 0.85f);
            titleRect.anchorMax = new Vector2(0.88f, 0.96f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Portrait Image Slot
            GameObject portraitObj = new GameObject("HeroPortraitImage");
            portraitObj.transform.SetParent(innerPanel.transform, false);
            _heroPortraitImage = portraitObj.AddComponent<Image>();
            RectTransform portRect = portraitObj.GetComponent<RectTransform>();
            portRect.anchorMin = new Vector2(0.04f, 0.70f);
            portRect.anchorMax = new Vector2(0.20f, 0.96f);
            portRect.offsetMin = Vector2.zero;
            portRect.offsetMax = Vector2.zero;

            // Close Button
            GameObject closeBtnObj = new GameObject("CloseBtn");
            closeBtnObj.transform.SetParent(innerPanel.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.90f, 0.88f);
            closeRect.anchorMax = new Vector2(0.97f, 0.96f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;
            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.color = new Color(0.7f, 0.2f, 0.2f);
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(HideWindow);

            GameObject closeTxt = new GameObject("X");
            closeTxt.transform.SetParent(closeBtnObj.transform, false);
            var xTxt = closeTxt.AddComponent<TextMeshProUGUI>();
            xTxt.text = "<b>X</b>";
            xTxt.alignment = TextAlignmentOptions.Center;
            xTxt.fontSize = 14;
            xTxt.color = Color.white;
            RectTransform xRect = closeTxt.GetComponent<RectTransform>();
            xRect.anchorMin = Vector2.zero;
            xRect.anchorMax = Vector2.one;
            xRect.offsetMin = Vector2.zero;
            xRect.offsetMax = Vector2.zero;

            // Stats Text
            GameObject statsObj = new GameObject("StatsText");
            statsObj.transform.SetParent(innerPanel.transform, false);
            _statsText = statsObj.AddComponent<TextMeshProUGUI>();
            _statsText.fontSize = 14;
            _statsText.color = Color.white;
            _statsText.alignment = TextAlignmentOptions.Center;
            RectTransform statsRect = statsObj.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.05f, 0.55f);
            statsRect.anchorMax = new Vector2(0.95f, 0.68f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;

            // MP Progress Bar Container
            GameObject mpBgObj = new GameObject("MP_Bar_BG");
            mpBgObj.transform.SetParent(innerPanel.transform, false);
            RectTransform mpBgRect = mpBgObj.AddComponent<RectTransform>();
            mpBgRect.anchorMin = new Vector2(0.1f, 0.44f);
            mpBgRect.anchorMax = new Vector2(0.9f, 0.52f);
            mpBgRect.offsetMin = Vector2.zero;
            mpBgRect.offsetMax = Vector2.zero;
            Image mpBgImg = mpBgObj.AddComponent<Image>();
            mpBgImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

            GameObject mpFillObj = new GameObject("MP_Bar_Fill");
            mpFillObj.transform.SetParent(mpBgObj.transform, false);
            RectTransform mpFillRect = mpFillObj.AddComponent<RectTransform>();
            mpFillRect.anchorMin = Vector2.zero;
            mpFillRect.anchorMax = Vector2.one;
            mpFillRect.offsetMin = Vector2.zero;
            mpFillRect.offsetMax = Vector2.zero;
            _mpBarFill = mpFillObj.AddComponent<Image>();
            _mpBarFill.color = new Color(0.2f, 0.8f, 0.3f, 1f);
            _mpBarFill.type = Image.Type.Filled;
            _mpBarFill.fillMethod = Image.FillMethod.Horizontal;

            GameObject mpTxtObj = new GameObject("MPText");
            mpTxtObj.transform.SetParent(mpBgObj.transform, false);
            _mpText = mpTxtObj.AddComponent<TextMeshProUGUI>();
            _mpText.fontSize = 12;
            _mpText.color = Color.white;
            _mpText.alignment = TextAlignmentOptions.Center;
            RectTransform mpTxtRect = mpTxtObj.GetComponent<RectTransform>();
            mpTxtRect.anchorMin = Vector2.zero;
            mpTxtRect.anchorMax = Vector2.one;
            mpTxtRect.offsetMin = Vector2.zero;
            mpTxtRect.offsetMax = Vector2.zero;

            // Army Header
            GameObject armyHeader = new GameObject("ArmyHeader");
            armyHeader.transform.SetParent(innerPanel.transform, false);
            var ahTxt = armyHeader.AddComponent<TextMeshProUGUI>();
            ahTxt.text = "<b>— HERO ARMY STACKS —</b>";
            ahTxt.fontSize = 13;
            ahTxt.color = Color.yellow;
            ahTxt.alignment = TextAlignmentOptions.Center;
            RectTransform ahRect = armyHeader.GetComponent<RectTransform>();
            ahRect.anchorMin = new Vector2(0.05f, 0.33f);
            ahRect.anchorMax = new Vector2(0.95f, 0.40f);
            ahRect.offsetMin = Vector2.zero;
            ahRect.offsetMax = Vector2.zero;

            // 7 Army Slots Grid Container
            GameObject gridContainer = new GameObject("ArmyGrid");
            gridContainer.transform.SetParent(innerPanel.transform, false);
            RectTransform gridRect = gridContainer.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.04f, 0.04f);
            gridRect.anchorMax = new Vector2(0.96f, 0.31f);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;

            _armySlotIcons.Clear();
            _armySlotCountTexts.Clear();

            for (int i = 0; i < 7; i++)
            {
                GameObject slotObj = new GameObject($"ArmySlot_{i}");
                slotObj.transform.SetParent(gridContainer.transform, false);
                RectTransform sRect = slotObj.AddComponent<RectTransform>();
                sRect.anchorMin = new Vector2((float)i / 7.0f, 0f);
                sRect.anchorMax = new Vector2((float)(i + 1) / 7.0f, 1f);
                sRect.offsetMin = new Vector2(3f, 3f);
                sRect.offsetMax = new Vector2(-3f, -3f);

                Image slotBg = slotObj.AddComponent<Image>();
                slotBg.color = new Color(0.18f, 0.2f, 0.26f, 1f);

                // Creature Icon
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(slotObj.transform, false);
                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.05f, 0.25f);
                iconRect.anchorMax = new Vector2(0.95f, 0.95f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                Image iconImg = iconObj.AddComponent<Image>();
                _armySlotIcons.Add(iconImg);

                // Stack Count Text
                GameObject countObj = new GameObject("CountText");
                countObj.transform.SetParent(slotObj.transform, false);
                RectTransform countRect = countObj.AddComponent<RectTransform>();
                countRect.anchorMin = new Vector2(0f, 0f);
                countRect.anchorMax = new Vector2(1f, 0.25f);
                countRect.offsetMin = Vector2.zero;
                countRect.offsetMax = Vector2.zero;
                var cTxt = countObj.AddComponent<TextMeshProUGUI>();
                cTxt.fontSize = 11;
                cTxt.color = Color.yellow;
                cTxt.alignment = TextAlignmentOptions.Center;
                _armySlotCountTexts.Add(cTxt);
            }
        }
    }
}
