using System;
using UnityEngine;
using UnityEngine.UI;
using HommClone.World;

namespace HommClone.UI
{
    public class TreasureChestUI : MonoBehaviour
    {
        public static TreasureChestUI Instance { get; private set; }

        private GameObject _panel;
        private Text _titleText;
        private Text _promptText;
        private Button _goldButton;
        private Text _goldButtonText;
        private Button _xpButton;
        private Text _xpButtonText;

        private HeroData _targetHero;
        private int _goldAmount;
        private int _xpAmount;
        private Action _onChoiceMade;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildUI();
        }

        private void BuildUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            if (canvas == null) return;

            // Main Background Panel
            _panel = new GameObject("TreasureChestPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(520, 320);

            Image panelImg = _panel.GetComponent<Image>();
            panelImg.color = new Color(0.12f, 0.1f, 0.08f, 0.96f);

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
            titleRect.sizeDelta = new Vector2(0, 45);

            _titleText = titleObj.GetComponent<Text>();
            _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _titleText.fontSize = 24;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.color = new Color(1f, 0.85f, 0.3f);
            _titleText.text = "TREASURE CHEST";

            // Prompt Text
            GameObject promptObj = new GameObject("PromptText", typeof(RectTransform), typeof(Text));
            promptObj.transform.SetParent(_panel.transform, false);

            RectTransform promptRect = promptObj.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.05f, 0.45f);
            promptRect.anchorMax = new Vector2(0.95f, 0.8f);
            promptRect.sizeDelta = Vector2.zero;

            _promptText = promptObj.GetComponent<Text>();
            _promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _promptText.fontSize = 16;
            _promptText.alignment = TextAnchor.MiddleCenter;
            _promptText.color = new Color(0.9f, 0.9f, 0.9f);
            _promptText.text = "Upon opening the chest, you discover ancient riches!\nWill you keep the Gold for your kingdom or distribute it to your troops for Experience?";

            // Gold Button
            GameObject goldBtnObj = new GameObject("GoldButton", typeof(RectTransform), typeof(Image), typeof(Button));
            goldBtnObj.transform.SetParent(_panel.transform, false);

            RectTransform goldRect = goldBtnObj.GetComponent<RectTransform>();
            goldRect.anchorMin = new Vector2(0.08f, 0.1f);
            goldRect.anchorMax = new Vector2(0.46f, 0.38f);
            goldRect.sizeDelta = Vector2.zero;

            Image goldImg = goldBtnObj.GetComponent<Image>();
            goldImg.color = new Color(0.7f, 0.5f, 0.1f, 0.95f);

            _goldButton = goldBtnObj.GetComponent<Button>();
            _goldButton.onClick.AddListener(OnGoldChosen);

            GameObject goldTextObj = new GameObject("GoldText", typeof(RectTransform), typeof(Text));
            goldTextObj.transform.SetParent(goldBtnObj.transform, false);

            RectTransform gTextRect = goldTextObj.GetComponent<RectTransform>();
            gTextRect.anchorMin = Vector2.zero;
            gTextRect.anchorMax = Vector2.one;
            gTextRect.sizeDelta = Vector2.zero;

            _goldButtonText = goldTextObj.GetComponent<Text>();
            _goldButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _goldButtonText.fontSize = 17;
            _goldButtonText.fontStyle = FontStyle.Bold;
            _goldButtonText.alignment = TextAnchor.MiddleCenter;
            _goldButtonText.color = Color.white;
            _goldButtonText.text = "2,000 GOLD";

            // XP Button
            GameObject xpBtnObj = new GameObject("XPButton", typeof(RectTransform), typeof(Image), typeof(Button));
            xpBtnObj.transform.SetParent(_panel.transform, false);

            RectTransform xpRect = xpBtnObj.GetComponent<RectTransform>();
            xpRect.anchorMin = new Vector2(0.54f, 0.1f);
            xpRect.anchorMax = new Vector2(0.92f, 0.38f);
            xpRect.sizeDelta = Vector2.zero;

            Image xpImg = xpBtnObj.GetComponent<Image>();
            xpImg.color = new Color(0.2f, 0.55f, 0.85f, 0.95f);

            _xpButton = xpBtnObj.GetComponent<Button>();
            _xpButton.onClick.AddListener(OnXPChosen);

            GameObject xpTextObj = new GameObject("XPText", typeof(RectTransform), typeof(Text));
            xpTextObj.transform.SetParent(xpBtnObj.transform, false);

            RectTransform xTextRect = xpTextObj.GetComponent<RectTransform>();
            xTextRect.anchorMin = Vector2.zero;
            xTextRect.anchorMax = Vector2.one;
            xTextRect.sizeDelta = Vector2.zero;

            _xpButtonText = xpTextObj.GetComponent<Text>();
            _xpButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _xpButtonText.fontSize = 17;
            _xpButtonText.fontStyle = FontStyle.Bold;
            _xpButtonText.alignment = TextAnchor.MiddleCenter;
            _xpButtonText.color = Color.white;
            _xpButtonText.text = "1,000 EXP";

            _panel.SetActive(false);
        }

        public void ShowChestChoice(HeroData hero, int gold, int xp, Action onChoiceMade)
        {
            if (_panel == null) BuildUI();

            _targetHero = hero;
            _goldAmount = gold;
            _xpAmount = xp;
            _onChoiceMade = onChoiceMade;

            _goldButtonText.text = $"<color=#ffea66>+{gold:N0} GOLD</color>";
            _xpButtonText.text = $"<color=#66d9ff>+{xp:N0} EXP</color>";

            _panel.SetActive(true);
        }

        private void OnGoldChosen()
        {
            var dataManager = GameDataManager.GetOrCreateInstance();
            if (dataManager != null)
            {
                var res = dataManager.GetActiveResources();
                if (res != null)
                {
                    res.gold += _goldAmount;
                    Debug.Log($"[TreasureChestUI] Added +{_goldAmount} Gold to Player {dataManager.activePlayerIndex} Resources! Total Gold: {res.gold}");
                }
            }

            WorldNotificationUI.ShowNotification(
                "TREASURE CLAIMED",
                $"Acquired <b>+{_goldAmount:N0} Gold</b>!",
                accentColor: new Color(1f, 0.85f, 0.3f)
            );

            _panel.SetActive(false);
            _onChoiceMade?.Invoke();
        }

        private void OnXPChosen()
        {
            if (_targetHero != null)
            {
                bool leveledUp = _targetHero.GainXP(_xpAmount, out LevelUpInfo lvlInfo);
                Debug.Log($"[TreasureChestUI] Hero {_targetHero.heroName} gained +{_xpAmount} XP! Current XP: {_targetHero.currentXP}/{_targetHero.xpToNextLevel}");

                WorldNotificationUI.ShowNotification(
                    "EXPERIENCE GAINED",
                    $"<b>{_targetHero.heroName}</b> gained <b>+{_xpAmount:N0} XP</b>!",
                    accentColor: new Color(0.4f, 0.85f, 1f)
                );

                _panel.SetActive(false);

                if (leveledUp)
                {
                    var levelUpUI = HeroLevelUpUI.Instance;
                    if (levelUpUI == null)
                    {
                        GameObject uiObj = new GameObject("HeroLevelUpUI");
                        levelUpUI = uiObj.AddComponent<HeroLevelUpUI>();
                    }
                    levelUpUI.ShowLevelUp(_targetHero, lvlInfo, onClose: () =>
                    {
                        _onChoiceMade?.Invoke();
                    });
                }
                else
                {
                    _onChoiceMade?.Invoke();
                }
            }
            else
            {
                _panel.SetActive(false);
                _onChoiceMade?.Invoke();
            }
        }
    }
}
