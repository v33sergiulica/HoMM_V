using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HommClone.World;
using HommClone.Artifacts;

namespace HommClone.UI
{
    public class HeroEquipmentUI : MonoBehaviour
    {
        public static HeroEquipmentUI Instance { get; private set; }

        private GameObject _canvasObj;
        private GameObject _panelObj;

        private TextMeshProUGUI _heroTitleText;
        private TextMeshProUGUI _attributesText;
        private TextMeshProUGUI _setBonusText;

        private Transform _paperdollContainer;
        private Transform _backpackContainer;

        private GameObject _tooltipObj;
        private TextMeshProUGUI _tooltipText;

        private HeroData _currentHero;
        private Dictionary<ArtifactSlotType, GameObject> _slotUIObjects = new Dictionary<ArtifactSlotType, GameObject>();

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

        public static HeroEquipmentUI GetOrCreateInstance()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<HeroEquipmentUI>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject obj = new GameObject("HeroEquipmentUI_Manager");
                    Instance = obj.AddComponent<HeroEquipmentUI>();
                }
            }
            return Instance;
        }

        private void BuildUI()
        {
            if (_canvasObj != null) return;

            // Canvas
            _canvasObj = new GameObject("HeroEquipmentCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = _canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 85;

            CanvasScaler scaler = _canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Modal Background Dimmer
            GameObject bgObj = new GameObject("BgDimmer", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(_canvasObj.transform, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.65f);

            // Main Window Panel
            _panelObj = new GameObject("EquipmentPanel", typeof(RectTransform), typeof(Image));
            _panelObj.transform.SetParent(_canvasObj.transform, false);
            RectTransform pRect = _panelObj.GetComponent<RectTransform>();
            pRect.sizeDelta = new Vector2(850, 720);
            pRect.anchoredPosition = Vector2.zero;

            Image pImg = _panelObj.GetComponent<Image>();
            pImg.color = new Color(0.12f, 0.1f, 0.08f, 0.98f); // Parchment Dark Leather Background

            Outline pBorder = _panelObj.AddComponent<Outline>();
            pBorder.effectColor = new Color(0.85f, 0.75f, 0.4f, 0.95f);
            pBorder.effectDistance = new Vector2(3, -3);

            // Title Bar
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(_panelObj.transform, false);
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.05f, 0.91f);
            tRect.anchorMax = new Vector2(0.80f, 0.98f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            _heroTitleText = titleObj.GetComponent<TextMeshProUGUI>();
            _heroTitleText.fontSize = 22;
            _heroTitleText.color = new Color(1f, 0.88f, 0.4f);
            _heroTitleText.alignment = TextAlignmentOptions.Left;

            // Close Button
            GameObject closeObj = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObj.transform.SetParent(_panelObj.transform, false);
            RectTransform cRect = closeObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.91f, 0.91f);
            cRect.anchorMax = new Vector2(0.97f, 0.97f);
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;

            closeObj.GetComponent<Image>().color = new Color(0.7f, 0.2f, 0.2f);
            closeObj.GetComponent<Button>().onClick.AddListener(Hide);

            GameObject xObj = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
            xObj.transform.SetParent(closeObj.transform, false);
            TextMeshProUGUI xText = xObj.GetComponent<TextMeshProUGUI>();
            xText.text = "<b>X</b>";
            xText.alignment = TextAlignmentOptions.Center;
            xText.fontSize = 16;
            xText.color = Color.white;

            // Attributes Header Box (Top Section matching HoMM 5 Screenshot)
            GameObject attrBox = new GameObject("AttributesBox", typeof(RectTransform), typeof(Image));
            attrBox.transform.SetParent(_panelObj.transform, false);
            RectTransform aRect = attrBox.GetComponent<RectTransform>();
            aRect.anchorMin = new Vector2(0.04f, 0.65f);
            aRect.anchorMax = new Vector2(0.96f, 0.90f);
            aRect.offsetMin = Vector2.zero;
            aRect.offsetMax = Vector2.zero;

            attrBox.GetComponent<Image>().color = new Color(0.18f, 0.15f, 0.11f, 0.9f);
            Outline aBorder = attrBox.AddComponent<Outline>();
            aBorder.effectColor = new Color(0.6f, 0.5f, 0.3f, 0.8f);

            GameObject attrTxtObj = new GameObject("AttributesText", typeof(RectTransform), typeof(TextMeshProUGUI));
            attrTxtObj.transform.SetParent(attrBox.transform, false);
            RectTransform atRect = attrTxtObj.GetComponent<RectTransform>();
            atRect.anchorMin = Vector2.zero;
            atRect.anchorMax = Vector2.one;
            atRect.sizeDelta = new Vector2(-20, -10);

            _attributesText = attrTxtObj.GetComponent<TextMeshProUGUI>();
            _attributesText.fontSize = 15;
            _attributesText.alignment = TextAlignmentOptions.TopLeft;

            // Paperdoll Equipment Grid Container (Middle Section)
            GameObject pdObj = new GameObject("PaperdollContainer", typeof(RectTransform));
            pdObj.transform.SetParent(_panelObj.transform, false);
            RectTransform pdRect = pdObj.GetComponent<RectTransform>();
            pdRect.anchorMin = new Vector2(0.04f, 0.25f);
            pdRect.anchorMax = new Vector2(0.96f, 0.63f);
            pdRect.offsetMin = Vector2.zero;
            pdRect.offsetMax = Vector2.zero;
            _paperdollContainer = pdObj.transform;

            // Backpack Inventory Grid Container (Bottom Section)
            GameObject bpBox = new GameObject("BackpackBox", typeof(RectTransform), typeof(Image));
            bpBox.transform.SetParent(_panelObj.transform, false);
            RectTransform bpRect = bpBox.GetComponent<RectTransform>();
            bpRect.anchorMin = new Vector2(0.04f, 0.03f);
            bpRect.anchorMax = new Vector2(0.96f, 0.23f);
            bpRect.offsetMin = Vector2.zero;
            bpRect.offsetMax = Vector2.zero;

            bpBox.GetComponent<Image>().color = new Color(0.15f, 0.13f, 0.10f, 0.9f);
            Outline bpBorder = bpBox.AddComponent<Outline>();
            bpBorder.effectColor = new Color(0.5f, 0.45f, 0.3f, 0.7f);

            GameObject bpHeader = new GameObject("BackpackHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
            bpHeader.transform.SetParent(bpBox.transform, false);
            RectTransform bphRect = bpHeader.GetComponent<RectTransform>();
            bphRect.anchorMin = new Vector2(0.02f, 0.75f);
            bphRect.anchorMax = new Vector2(0.98f, 0.98f);
            TextMeshProUGUI bphTxt = bpHeader.GetComponent<TextMeshProUGUI>();
            bphTxt.text = "<color=#ffd700><b>[BACKPACK] HERO BACKPACK</b></color> (Click item to Equip)";
            bphTxt.fontSize = 12;

            GameObject bpItemsObj = new GameObject("BackpackItemsContainer", typeof(RectTransform));
            bpItemsObj.transform.SetParent(bpBox.transform, false);
            RectTransform bpiRect = bpItemsObj.GetComponent<RectTransform>();
            bpiRect.anchorMin = new Vector2(0.02f, 0.05f);
            bpiRect.anchorMax = new Vector2(0.98f, 0.72f);
            bpiRect.offsetMin = Vector2.zero;
            bpiRect.offsetMax = Vector2.zero;
            _backpackContainer = bpItemsObj.transform;

            // Tooltip Overlay Object
            BuildTooltip();
        }

        private void BuildTooltip()
        {
            _tooltipObj = new GameObject("EquipTooltip", typeof(RectTransform), typeof(Image));
            _tooltipObj.transform.SetParent(_canvasObj.transform, false);
            RectTransform tRect = _tooltipObj.GetComponent<RectTransform>();
            tRect.sizeDelta = new Vector2(280, 150);

            _tooltipObj.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.96f);
            Outline tBorder = _tooltipObj.AddComponent<Outline>();
            tBorder.effectColor = new Color(0.9f, 0.8f, 0.3f, 0.95f);

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(_tooltipObj.transform, false);
            RectTransform txRect = txtObj.GetComponent<RectTransform>();
            txRect.anchorMin = Vector2.zero;
            txRect.anchorMax = Vector2.one;
            txRect.sizeDelta = new Vector2(-16, -16);

            _tooltipText = txtObj.GetComponent<TextMeshProUGUI>();
            _tooltipText.fontSize = 12;
            _tooltipText.color = Color.white;
            _tooltipText.alignment = TextAlignmentOptions.TopLeft;

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
            if (_currentHero.equippedArtifacts == null) _currentHero.equippedArtifacts = new List<ArtifactData>();
            if (_currentHero.backpack == null) _currentHero.backpack = new List<ArtifactData>();

            if (_canvasObj == null) BuildUI();
            _canvasObj.SetActive(true);

            RefreshUI();
        }

        public void Hide()
        {
            if (_canvasObj != null) _canvasObj.SetActive(false);
            if (_tooltipObj != null) _tooltipObj.SetActive(false);
        }

        public void RefreshUI()
        {
            if (_currentHero == null) return;

            _heroTitleText.text = $"<b>{_currentHero.heroName}</b> — Equipment & Artifact Inventory";

            // Render Attributes Header (HoMM 5 Style)
            int totalAtt = _currentHero.GetTotalAttack();
            int totalDef = _currentHero.GetTotalDefense();
            int totalSP = _currentHero.GetTotalSpellPower();
            int totalKnw = _currentHero.GetTotalKnowledge();
            int totalMorale = _currentHero.GetTotalMorale();
            int totalLuck = _currentHero.GetTotalLuck();
            int maxMana = _currentHero.GetMaxMana();

            string attColor = totalAtt > _currentHero.attack ? "#66ff66" : "#ffffff";
            string defColor = totalDef > _currentHero.defense ? "#66ff66" : "#ffffff";
            string spColor = totalSP > _currentHero.spellPower ? "#66ff66" : "#ffffff";
            string knwColor = totalKnw > _currentHero.knowledge ? "#66ff66" : "#ffffff";
            string morColor = totalMorale > 0 ? "#66ff66" : "#ffffff";
            string luckColor = totalLuck > 0 ? "#66ff66" : "#ffffff";

            string activeSetInfo = "";
            var activeSetBonuses = _currentHero.GetActiveSetBonuses();
            if (activeSetBonuses.Count > 0)
            {
                List<string> bList = new List<string>();
                foreach (var b in activeSetBonuses) bList.Add(b.bonusDescription);
                activeSetInfo = $"\n<color=#ffd700><b>ACTIVE SET BONUSES:</b></color> <color=#66ff66>{string.Join(" | ", bList)}</color>";
            }

            _attributesText.text = $"<b><color=#ffd700>Attributes</color></b>\n" +
                $"Attack: <color={attColor}><b>{totalAtt}</b></color> (Base: {_currentHero.attack})        Morale: <color={morColor}><b>{totalMorale}</b></color>\n" +
                $"Defense: <color={defColor}><b>{totalDef}</b></color> (Base: {_currentHero.defense})       Luck: <color={luckColor}><b>{totalLuck}</b></color>\n" +
                $"Spellpower: <color={spColor}><b>{totalSP}</b></color> (Base: {_currentHero.spellPower})     Mana: <color=#66aaff><b>{maxMana}/{maxMana}</b></color>\n" +
                $"Knowledge: <color={knwColor}><b>{totalKnw}</b></color> (Base: {_currentHero.knowledge}){activeSetInfo}";

            // Clear old paperdoll slots
            foreach (Transform child in _paperdollContainer) Destroy(child.gameObject);
            _slotUIObjects.Clear();

            // Paperdoll Layout Definition (3 Rows x 4 Columns matching screenshot)
            var slotPositions = new List<(ArtifactSlotType slot, string name, Vector2 pos)>
            {
                (ArtifactSlotType.RingLeft, "Left Ring", new Vector2(-280, 90)),
                (ArtifactSlotType.Head, "Head", new Vector2(-90, 90)),
                (ArtifactSlotType.Neck, "Neck", new Vector2(100, 90)),
                (ArtifactSlotType.Pocket, "Pocket / Tome", new Vector2(280, 90)),

                (ArtifactSlotType.RingRight, "Right Ring", new Vector2(-280, -10)),
                (ArtifactSlotType.Body, "Body Armor", new Vector2(-90, -10)),
                (ArtifactSlotType.LeftHand, "Left Hand (Shield)", new Vector2(100, -10)),
                (ArtifactSlotType.RightHand, "Right Hand (Weapon)", new Vector2(280, -10)),

                (ArtifactSlotType.Cape, "Cape / Mantle", new Vector2(-180, -110)),
                (ArtifactSlotType.Feet, "Feet (Boots)", new Vector2(0, -110))
            };

            foreach (var s in slotPositions)
            {
                GameObject slotObj = new GameObject($"Slot_{s.slot}", typeof(RectTransform), typeof(Image), typeof(Button));
                slotObj.transform.SetParent(_paperdollContainer, false);

                RectTransform sRect = slotObj.GetComponent<RectTransform>();
                sRect.sizeDelta = new Vector2(140, 80);
                sRect.anchoredPosition = s.pos;

                Image slotBg = slotObj.GetComponent<Image>();
                Button slotBtn = slotObj.GetComponent<Button>();
                Outline slotBorder = slotObj.AddComponent<Outline>();
                slotBorder.effectDistance = new Vector2(2, -2);

                ArtifactData equippedItem = _currentHero.GetEquippedInSlot(s.slot);

                if (equippedItem != null)
                {
                    slotBg.color = new Color(0.22f, 0.18f, 0.10f, 0.95f);
                    slotBorder.effectColor = new Color(0.95f, 0.85f, 0.35f, 0.95f);

                    GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    txtObj.transform.SetParent(slotObj.transform, false);
                    RectTransform txRect = txtObj.GetComponent<RectTransform>();
                    txRect.anchorMin = Vector2.zero;
                    txRect.anchorMax = Vector2.one;
                    txRect.sizeDelta = new Vector2(-10, -10);

                    TextMeshProUGUI label = txtObj.GetComponent<TextMeshProUGUI>();
                    label.fontSize = 11;
                    label.alignment = TextAlignmentOptions.Center;

                    string itemColor = equippedItem.rarity == ArtifactRarity.Relic ? "#ffaa33" : (equippedItem.rarity == ArtifactRarity.Major ? "#33aaff" : "#ffffff");
                    label.text = $"{equippedItem.iconSymbol} <color={itemColor}><b>{equippedItem.name}</b></color>\n<size=9><color=#aaaaaa>[{s.name}]</color></size>";

                    slotBtn.onClick.RemoveAllListeners();
                    slotBtn.onClick.AddListener(() =>
                    {
                        _currentHero.UnequipArtifact(equippedItem);
                        RefreshUI();
                    });

                    AddTooltipTrigger(slotObj, equippedItem, s.pos + new Vector2(0, 50));
                }
                else
                {
                    slotBg.color = new Color(0.1f, 0.1f, 0.12f, 0.7f);
                    slotBorder.effectColor = new Color(0.4f, 0.4f, 0.45f, 0.5f);

                    GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    txtObj.transform.SetParent(slotObj.transform, false);
                    RectTransform txRect = txtObj.GetComponent<RectTransform>();
                    txRect.anchorMin = Vector2.zero;
                    txRect.anchorMax = Vector2.one;

                    TextMeshProUGUI label = txtObj.GetComponent<TextMeshProUGUI>();
                    label.fontSize = 11;
                    label.alignment = TextAlignmentOptions.Center;
                    label.text = $"<color=#666666><i>[{s.name}]</i>\n(Empty)</color>";

                    slotBtn.interactable = false;
                }
            }

            // Render Backpack Inventory Items
            foreach (Transform child in _backpackContainer) Destroy(child.gameObject);

            float bpXStart = -340f;
            float bpXSpacing = 145f;
            int bpIndex = 0;

            foreach (var item in _currentHero.backpack)
            {
                if (item == null) continue;

                GameObject itemObj = new GameObject($"BP_{item.id}", typeof(RectTransform), typeof(Image), typeof(Button));
                itemObj.transform.SetParent(_backpackContainer, false);

                RectTransform iRect = itemObj.GetComponent<RectTransform>();
                iRect.sizeDelta = new Vector2(135, 65);
                iRect.anchoredPosition = new Vector2(bpXStart + (bpIndex % 5) * bpXSpacing, -5);

                Image itemBg = itemObj.GetComponent<Image>();
                itemBg.color = new Color(0.18f, 0.22f, 0.18f, 0.95f);

                Outline itemBorder = itemObj.AddComponent<Outline>();
                itemBorder.effectDistance = new Vector2(2, -2);
                itemBorder.effectColor = new Color(0.4f, 0.9f, 0.5f, 0.95f);

                GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(itemObj.transform, false);
                RectTransform txRect = txtObj.GetComponent<RectTransform>();
                txRect.anchorMin = Vector2.zero;
                txRect.anchorMax = Vector2.one;
                txRect.sizeDelta = new Vector2(-8, -8);

                TextMeshProUGUI label = txtObj.GetComponent<TextMeshProUGUI>();
                label.fontSize = 10;
                label.alignment = TextAlignmentOptions.Center;

                string itemColor = item.rarity == ArtifactRarity.Relic ? "#ffaa33" : (item.rarity == ArtifactRarity.Major ? "#33aaff" : "#ffffff");
                label.text = $"{item.iconSymbol} <color={itemColor}><b>{item.name}</b></color>\n<size=9><color=#88cc88>[EQUIP]</color></size>";

                var currentItem = item;
                Button itemBtn = itemObj.GetComponent<Button>();
                itemBtn.onClick.AddListener(() =>
                {
                    _currentHero.EquipArtifact(currentItem);
                    RefreshUI();
                });

                AddTooltipTrigger(itemObj, item, iRect.anchoredPosition + new Vector2(0, -180));
                bpIndex++;
            }
        }

        private void AddTooltipTrigger(GameObject obj, ArtifactData item, Vector2 uiPos)
        {
            var trigger = obj.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var enter = new UnityEngine.EventSystems.EventTrigger.Entry();
            enter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            enter.callback.AddListener((data) => ShowTooltip(item, uiPos));
            trigger.triggers.Add(enter);

            var exit = new UnityEngine.EventSystems.EventTrigger.Entry();
            exit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            exit.callback.AddListener((data) => HideTooltip());
            trigger.triggers.Add(exit);
        }

        private void ShowTooltip(ArtifactData item, Vector2 uiPos)
        {
            if (item == null || _tooltipObj == null) return;

            List<string> bonuses = new List<string>();
            if (item.attackBonus != 0) bonuses.Add($"Attack: +{item.attackBonus}");
            if (item.defenseBonus != 0) bonuses.Add($"Defense: +{item.defenseBonus}");
            if (item.spellPowerBonus != 0) bonuses.Add($"Spell Power: +{item.spellPowerBonus}");
            if (item.knowledgeBonus != 0) bonuses.Add($"Knowledge: +{item.knowledgeBonus}");
            if (item.moraleBonus != 0) bonuses.Add($"Morale: +{item.moraleBonus}");
            if (item.luckBonus != 0) bonuses.Add($"Luck: +{item.luckBonus}");
            if (item.movementPointsBonus != 0) bonuses.Add($"Movement: +{item.movementPointsBonus} MP");

            string setStatus = "";
            if (!string.IsNullOrEmpty(item.setId))
            {
                var setData = ArtifactCatalog.GetSetById(item.setId);
                if (setData != null)
                {
                    int equippedCount = 0;
                    foreach (var a in _currentHero.equippedArtifacts)
                    {
                        if (a != null && a.setId == item.setId) equippedCount++;
                    }
                    setStatus = $"\n<color=#ffd700><b>Set: {setData.setName}</b></color> ({equippedCount}/{setData.artifactIds.Count} Equipped)";
                }
            }

            string rarityColor = item.rarity == ArtifactRarity.Relic ? "#ffaa33" : (item.rarity == ArtifactRarity.Major ? "#33aaff" : "#ffffff");
            _tooltipText.text = $"{item.iconSymbol} <color={rarityColor}><b>{item.name}</b></color> ({item.slotType})\n" +
                $"<size=11><color=#cccccc>{item.description}</color></size>\n" +
                $"<color=#66ff66><b>Bonuses:</b> {string.Join(", ", bonuses)}</color>{setStatus}";

            RectTransform tRect = _tooltipObj.GetComponent<RectTransform>();
            tRect.anchoredPosition = uiPos + new Vector2(160, 40);
            _tooltipObj.SetActive(true);
        }

        private void HideTooltip()
        {
            if (_tooltipObj != null) _tooltipObj.SetActive(false);
        }
    }
}
