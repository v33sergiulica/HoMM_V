using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HommClone.World;

namespace HommClone.UI
{
    /// <summary>
    /// Top Overlay Resource HUD displaying current player resources (Gold, Wood, Ore, Crystals),
    /// daily income rates (+X/day), and World Day counter.
    /// </summary>
    public class ResourceBarUI : MonoBehaviour
    {
        public static ResourceBarUI Instance { get; private set; }

        private GameObject _canvasObj;
        private TextMeshProUGUI _goldText;
        private TextMeshProUGUI _woodText;
        private TextMeshProUGUI _oreText;
        private TextMeshProUGUI _gemsText;
        private TextMeshProUGUI _dayText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            CreateHUD();
        }

        private void Start()
        {
            UpdateUI();
        }

        public static ResourceBarUI GetOrCreateInstance()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<ResourceBarUI>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject obj = new GameObject("ResourceBarUI_Manager");
                    Instance = obj.AddComponent<ResourceBarUI>();
                }
            }
            return Instance;
        }

        public void UpdateUI()
        {
            if (_canvasObj == null) CreateHUD();

            var gameData = GameDataManager.GetOrCreateInstance();
            if (gameData == null) return;

            var res = gameData.player1Resources;

            // Calculate daily income rates from owned mines
            int dailyGold = 0, dailyWood = 0, dailyOre = 0, dailyGems = 0;
            var mines = FindObjectsByType<WorldMine>(FindObjectsSortMode.None);
            foreach (var m in mines)
            {
                if (m.OwnerPlayerIndex == 1)
                {
                    switch (m.MineType)
                    {
                        case ResourceType.Gold: dailyGold += m.DailyIncome; break;
                        case ResourceType.Wood: dailyWood += m.DailyIncome; break;
                        case ResourceType.Ore: dailyOre += m.DailyIncome; break;
                        case ResourceType.Gems: dailyGems += m.DailyIncome; break;
                    }
                }
            }

            if (_goldText != null) _goldText.text = $"<color=#FFDD44><b>GOLD:</b></color> {res.gold} <size=80%>(+{(dailyGold > 0 ? dailyGold.ToString() : "0")}/day)</size>";
            if (_woodText != null) _woodText.text = $"<color=#DDAA66><b>WOOD:</b></color> {res.wood} <size=80%>(+{(dailyWood > 0 ? dailyWood.ToString() : "0")}/day)</size>";
            if (_oreText != null) _oreText.text = $"<color=#AABBCC><b>ORE:</b></color> {res.ore} <size=80%>(+{(dailyOre > 0 ? dailyOre.ToString() : "0")}/day)</size>";
            if (_gemsText != null) _gemsText.text = $"<color=#66EEEE><b>GEMS:</b></color> {res.gems} <size=80%>(+{(dailyGems > 0 ? dailyGems.ToString() : "0")}/day)</size>";
            if (_dayText != null) _dayText.text = $"<color=#FFCC00><b>DAY:</b></color> {gameData.currentDay} <size=80%>(M{gameData.currentMonth} W{gameData.currentWeek})</size>";
        }

        private void CreateHUD()
        {
            if (_canvasObj != null) return;

            _canvasObj = new GameObject("ResourceBar_Canvas");
            _canvasObj.transform.SetParent(transform, false);

            Canvas canvas = _canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            _canvasObj.AddComponent<CanvasScaler>();
            _canvasObj.AddComponent<GraphicRaycaster>();

            // Top Bar Panel Container
            GameObject barObj = new GameObject("TopBarPanel");
            barObj.transform.SetParent(_canvasObj.transform, false);
            RectTransform barRect = barObj.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(0f, 36f);

            Image barBg = barObj.AddComponent<Image>();
            barBg.color = new Color(0.08f, 0.1f, 0.14f, 0.92f); // Dark Gold Slate

            // Horizontal Layout Container
            GameObject layoutObj = new GameObject("Layout");
            layoutObj.transform.SetParent(barObj.transform, false);
            RectTransform layRect = layoutObj.AddComponent<RectTransform>();
            layRect.anchorMin = Vector2.zero;
            layRect.anchorMax = Vector2.one;
            layRect.offsetMin = new Vector2(15f, 2f);
            layRect.offsetMax = new Vector2(-15f, -2f);

            HorizontalLayoutGroup horiz = layoutObj.AddComponent<HorizontalLayoutGroup>();
            horiz.spacing = 15;
            horiz.childControlWidth = true;
            horiz.childControlHeight = true;

            _goldText = CreateResourceLabel(layoutObj, "GoldText");
            _woodText = CreateResourceLabel(layoutObj, "WoodText");
            _oreText = CreateResourceLabel(layoutObj, "OreText");
            _gemsText = CreateResourceLabel(layoutObj, "GemsText");
            _dayText = CreateResourceLabel(layoutObj, "DayText");
        }

        private TextMeshProUGUI CreateResourceLabel(GameObject parent, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            var text = obj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 12;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }
    }
}
