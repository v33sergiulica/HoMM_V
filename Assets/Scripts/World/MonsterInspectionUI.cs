using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HommClone.Creatures;

namespace HommClone.World
{
    /// <summary>
    /// Modal inspection window displaying monster stack data on the World Map (Creature Icon, Count, Stats)
    /// with options to ATTACK or CLOSE.
    /// </summary>
    public class MonsterInspectionUI : MonoBehaviour
    {
        public static MonsterInspectionUI Instance { get; private set; }

        private GameObject _canvasObj;
        private GameObject _panelObj;
        private Image _monsterIconImage;
        private TextMeshProUGUI _monsterTitleText;
        private TextMeshProUGUI _monsterDetailsText;
        private Button _attackButton;
        private WorldMonsterStack _inspectingMonster;

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

        public static MonsterInspectionUI GetOrCreateInstance()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<MonsterInspectionUI>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject obj = new GameObject("MonsterInspectionUI_Manager");
                    Instance = obj.AddComponent<MonsterInspectionUI>();
                }
            }
            return Instance;
        }

        public void ShowWindow(WorldMonsterStack monster)
        {
            if (_canvasObj == null) CreateUIWindow();
            if (_canvasObj != null)
            {
                _canvasObj.SetActive(true);
                RefreshData(monster);
            }
        }

        public void HideWindow()
        {
            if (_canvasObj != null)
            {
                _canvasObj.SetActive(false);
            }
        }

        public static string GetVagueDescriptor(int count)
        {
            if (count <= 4) return "Few (1-4)";
            if (count <= 9) return "Several (5-9)";
            if (count <= 19) return "Pack (10-19)";
            if (count <= 49) return "Lots (20-49)";
            if (count <= 99) return "Horde (50-99)";
            if (count <= 249) return "Throng (100-249)";
            if (count <= 499) return "Swarm (250-499)";
            if (count <= 999) return "Zounds (500-999)";
            return "Legion (1000+)";
        }

        private void RefreshData(WorldMonsterStack monster)
        {
            _inspectingMonster = monster;
            if (monster == null) return;

            string cName = monster.CreatureData != null ? monster.CreatureData.CreatureName : "Monster Stack";
            int count = monster.Count;
            var activeHero = GameDataManager.GetOrCreateInstance().GetActiveHero();
            bool hasScouting = activeHero != null && activeHero.HasScouting();

            string countDisplay = hasScouting ? $"{count}x" : GetVagueDescriptor(count);

            if (_monsterTitleText != null)
            {
                _monsterTitleText.text = $"<b>{countDisplay} {cName}</b>";
            }

            if (_monsterIconImage != null)
            {
                if (monster.CreatureData != null && monster.CreatureData.Icon != null)
                {
                    _monsterIconImage.gameObject.SetActive(true);
                    _monsterIconImage.sprite = monster.CreatureData.Icon;
                    _monsterIconImage.color = Color.white;
                }
                else
                {
                    _monsterIconImage.gameObject.SetActive(true);
                    _monsterIconImage.sprite = null;
                    _monsterIconImage.color = new Color(0.85f, 0.25f, 0.25f, 1f);
                }
            }

            if (_monsterDetailsText != null)
            {
                if (monster.CreatureData != null)
                {
                    var data = monster.CreatureData;
                    _monsterDetailsText.text = $"<color=#FF6666><b>Attack:</b></color> {data.Attack}    <color=#66AAFF><b>Defense:</b></color> {data.Defense}\n" +
                                               $"<color=#55FF55><b>Health:</b></color> {data.MaxHealth} HP    <color=#FFFF55><b>Speed:</b></color> {data.Speed}\n" +
                                               $"<color=#CC66FF><b>Damage:</b></color> {data.MinDamage}-{data.MaxDamage}    <color=#FFAA44><b>Threat:</b></color> {(count > 50 ? "Lethal" : (count > 15 ? "Strong" : "Moderate"))}";
                }
                else
                {
                    _monsterDetailsText.text = $"Stack Size: <b>{count} Units</b>\nGuarding World Map Tile";
                }
            }
        }

        private void CreateUIWindow()
        {
            if (_canvasObj != null) return;

            _canvasObj = new GameObject("MonsterInspection_Canvas");
            _canvasObj.transform.SetParent(transform, false);

            Canvas canvas = _canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            _canvasObj.AddComponent<CanvasScaler>();
            _canvasObj.AddComponent<GraphicRaycaster>();

            // Background Overlay
            GameObject overlay = new GameObject("DarkOverlay");
            overlay.transform.SetParent(_canvasObj.transform, false);
            RectTransform overRect = overlay.AddComponent<RectTransform>();
            overRect.anchorMin = Vector2.zero;
            overRect.anchorMax = Vector2.one;
            overRect.offsetMin = Vector2.zero;
            overRect.offsetMax = Vector2.zero;
            Image overImg = overlay.AddComponent<Image>();
            overImg.color = new Color(0f, 0f, 0f, 0.6f);

            Button overBtn = overlay.AddComponent<Button>();
            overBtn.onClick.AddListener(HideWindow);

            // Main Panel
            _panelObj = new GameObject("InspectionPanel");
            _panelObj.transform.SetParent(_canvasObj.transform, false);
            RectTransform pRect = _panelObj.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.anchoredPosition = Vector2.zero;
            pRect.sizeDelta = new Vector2(400f, 260f);

            Image borderImg = _panelObj.AddComponent<Image>();
            borderImg.color = new Color(0.75f, 0.65f, 0.35f, 1f); // Gold Border

            GameObject innerPanel = new GameObject("InnerPanel");
            innerPanel.transform.SetParent(_panelObj.transform, false);
            RectTransform inRect = innerPanel.AddComponent<RectTransform>();
            inRect.anchorMin = Vector2.zero;
            inRect.anchorMax = Vector2.one;
            inRect.offsetMin = new Vector2(4f, 4f);
            inRect.offsetMax = new Vector2(-4f, -4f);
            Image pImg = innerPanel.AddComponent<Image>();
            pImg.color = new Color(0.12f, 0.14f, 0.18f, 0.98f);

            // Title
            GameObject titleObj = new GameObject("MonsterTitle");
            titleObj.transform.SetParent(innerPanel.transform, false);
            _monsterTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            _monsterTitleText.fontSize = 18;
            _monsterTitleText.color = Color.gold;
            _monsterTitleText.alignment = TextAlignmentOptions.Left;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.25f, 0.75f);
            titleRect.anchorMax = new Vector2(0.88f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Monster Icon
            GameObject iconObj = new GameObject("MonsterIcon");
            iconObj.transform.SetParent(innerPanel.transform, false);
            _monsterIconImage = iconObj.AddComponent<Image>();
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.05f, 0.55f);
            iconRect.anchorMax = new Vector2(0.22f, 0.95f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            // Details Text
            GameObject detailsObj = new GameObject("DetailsText");
            detailsObj.transform.SetParent(innerPanel.transform, false);
            _monsterDetailsText = detailsObj.AddComponent<TextMeshProUGUI>();
            _monsterDetailsText.fontSize = 13;
            _monsterDetailsText.color = Color.white;
            _monsterDetailsText.alignment = TextAlignmentOptions.Left;
            RectTransform detailsRect = detailsObj.GetComponent<RectTransform>();
            detailsRect.anchorMin = new Vector2(0.05f, 0.26f);
            detailsRect.anchorMax = new Vector2(0.95f, 0.52f);
            detailsRect.offsetMin = Vector2.zero;
            detailsRect.offsetMax = Vector2.zero;

            // Close Button
            GameObject closeBtnObj = new GameObject("CloseBtn");
            closeBtnObj.transform.SetParent(innerPanel.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.3f, 0.05f);
            closeRect.anchorMax = new Vector2(0.7f, 0.22f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.color = new Color(0.3f, 0.35f, 0.45f, 1f);

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(HideWindow);

            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            var closeTxt = closeTxtObj.AddComponent<TextMeshProUGUI>();
            closeTxt.text = "<b>CLOSE</b>";
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.fontSize = 13;
            closeTxt.color = Color.white;
            RectTransform cTxtRect = closeTxtObj.GetComponent<RectTransform>();
            cTxtRect.anchorMin = Vector2.zero;
            cTxtRect.anchorMax = Vector2.one;
            cTxtRect.offsetMin = Vector2.zero;
            cTxtRect.offsetMax = Vector2.zero;
        }
    }
}
