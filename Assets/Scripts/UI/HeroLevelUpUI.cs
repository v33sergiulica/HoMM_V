using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HommClone.Heroes;
using HommClone.World;

namespace HommClone.UI
{
    public class HeroLevelUpUI : MonoBehaviour
    {
        public static HeroLevelUpUI Instance { get; private set; }

        private GameObject _panel;
        private Text _titleText;
        private Text _primaryStatText;
        private Transform _skillContainer;
        private HeroData _targetHero;
        private Action _onCloseCallback;

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
            _panel = new GameObject("HeroLevelUpPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(580, 420);

            Image panelImg = _panel.GetComponent<Image>();
            panelImg.color = new Color(0.12f, 0.1f, 0.08f, 0.96f);

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
            titleRect.sizeDelta = new Vector2(0, 45);

            _titleText = titleObj.GetComponent<Text>();
            _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _titleText.fontSize = 26;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.color = new Color(1f, 0.85f, 0.3f);
            _titleText.text = "LEVEL UP!";

            // Primary Stat Gained Label
            GameObject statObj = new GameObject("StatGainedText", typeof(RectTransform), typeof(Text));
            statObj.transform.SetParent(_panel.transform, false);

            RectTransform statRect = statObj.GetComponent<RectTransform>();
            statRect.anchorMin = new Vector2(0f, 1f);
            statRect.anchorMax = new Vector2(1f, 1f);
            statRect.pivot = new Vector2(0.5f, 1f);
            statRect.anchoredPosition = new Vector2(0, -65);
            statRect.sizeDelta = new Vector2(0, 35);

            _primaryStatText = statObj.GetComponent<Text>();
            _primaryStatText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _primaryStatText.fontSize = 20;
            _primaryStatText.alignment = TextAnchor.MiddleCenter;
            _primaryStatText.color = new Color(0.4f, 1f, 0.4f);
            _primaryStatText.text = "Primary Stat Gained: +1 Attack";

            // Subtitle
            GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(Text));
            subObj.transform.SetParent(_panel.transform, false);

            RectTransform subRect = subObj.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 1f);
            subRect.anchorMax = new Vector2(1f, 1f);
            subRect.pivot = new Vector2(0.5f, 1f);
            subRect.anchoredPosition = new Vector2(0, -105);
            subRect.sizeDelta = new Vector2(0, 30);

            Text subText = subObj.GetComponent<Text>();
            subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subText.fontSize = 16;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.85f, 0.85f, 0.85f);
            subText.text = "Choose a Secondary Skill to Learn or Upgrade:";

            // Skill Options Container (Horizontal Layout)
            GameObject containerObj = new GameObject("SkillContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            containerObj.transform.SetParent(_panel.transform, false);

            RectTransform containerRect = containerObj.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.05f, 0.05f);
            containerRect.anchorMax = new Vector2(0.95f, 0.65f);
            containerRect.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup group = containerObj.GetComponent<HorizontalLayoutGroup>();
            group.spacing = 15;
            group.childControlWidth = true;
            group.childControlHeight = true;

            _skillContainer = containerObj.transform;
            _panel.SetActive(false);
        }

        public void ShowLevelUp(HeroData hero, string primaryStatGained, Action onClose = null)
        {
            ShowLevelUp(hero, new LevelUpInfo(hero.level, primaryStatGained), onClose);
        }

        public void ShowLevelUp(HeroData hero, LevelUpInfo levelUpInfo, Action onClose = null)
        {
            if (_panel == null) BuildUI();

            _targetHero = hero;
            _onCloseCallback = onClose;
            _titleText.text = $"<b>{hero.heroName.ToUpper()} REACHED LEVEL {levelUpInfo.levelReached}!</b>";
            _primaryStatText.text = $"Primary Attribute Gained: <b><color=#55ff55>{levelUpInfo.statGained}</color></b>";

            // Generate 2 or 3 random skill options
            List<SecondarySkillSlot> options = GenerateSkillOptions(hero);
            PopulateSkillOptions(options);

            _panel.SetActive(true);
        }

        private List<SecondarySkillSlot> GenerateSkillOptions(HeroData hero)
        {
            List<SecondarySkillSlot> choices = new List<SecondarySkillSlot>();
            Array allSkillTypes = Enum.GetValues(typeof(SecondarySkillType));

            List<SecondarySkillType> availableTypes = new List<SecondarySkillType>();
            foreach (SecondarySkillType t in allSkillTypes)
            {
                var existing = hero.secondarySkills.Find(s => s.type == t);
                if (existing == null || existing.rank < SkillRank.Expert)
                {
                    availableTypes.Add(t);
                }
            }

            // Shuffle available types
            for (int i = 0; i < availableTypes.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, availableTypes.Count);
                var temp = availableTypes[i];
                availableTypes[i] = availableTypes[rnd];
                availableTypes[rnd] = temp;
            }

            int optionsCount = Mathf.Min(3, availableTypes.Count);
            for (int i = 0; i < optionsCount; i++)
            {
                SecondarySkillType type = availableTypes[i];
                var existing = hero.secondarySkills.Find(s => s.type == type);
                SkillRank nextRank = existing == null ? SkillRank.Basic : (SkillRank)((int)existing.rank + 1);
                choices.Add(new SecondarySkillSlot(type, nextRank));
            }

            return choices;
        }

        private void PopulateSkillOptions(List<SecondarySkillSlot> options)
        {
            foreach (Transform child in _skillContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var opt in options)
            {
                GameObject card = new GameObject("SkillCard", typeof(RectTransform), typeof(Image), typeof(Button));
                card.transform.SetParent(_skillContainer, false);

                Image cardImg = card.GetComponent<Image>();
                cardImg.color = new Color(0.2f, 0.18f, 0.14f, 0.95f);

                Outline cardOutline = card.AddComponent<Outline>();
                cardOutline.effectColor = new Color(0.7f, 0.55f, 0.2f, 0.8f);

                // Card Layout Content
                GameObject textObj = new GameObject("CardText", typeof(RectTransform), typeof(Text));
                textObj.transform.SetParent(card.transform, false);

                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(10, 10);
                textRect.offsetMax = new Vector2(-10, -10);

                Text cardText = textObj.GetComponent<Text>();
                cardText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                cardText.fontSize = 15;
                cardText.alignment = TextAnchor.MiddleCenter;
                cardText.color = Color.white;
                cardText.text = $"<b>{opt.GetDisplayName()}</b>\n\n<color=#dddddd>{opt.GetDescription()}</color>";

                Button btn = card.GetComponent<Button>();
                var capturedOpt = opt;
                btn.onClick.AddListener(() => SelectSkill(capturedOpt));
            }
        }

        private void SelectSkill(SecondarySkillSlot skill)
        {
            if (_targetHero != null)
            {
                var existing = _targetHero.secondarySkills.Find(s => s.type == skill.type);
                if (existing != null)
                {
                    existing.rank = skill.rank;
                }
                else
                {
                    _targetHero.secondarySkills.Add(skill);
                }

                // If Logistics was upgraded, boost movement points immediately
                if (skill.type == SecondarySkillType.Logistics)
                {
                    float mpMultiplier = 1f + ((int)skill.rank * 0.10f);
                    _targetHero.maxMovementPoints = 15f * mpMultiplier;
                    _targetHero.currentMovementPoints = Mathf.Min(_targetHero.currentMovementPoints * mpMultiplier, _targetHero.maxMovementPoints);
                }

                Debug.Log($"[HeroLevelUpUI] Hero {_targetHero.heroName} learned/upgraded skill {skill.GetDisplayName()}!");
            }

            _panel.SetActive(false);

            if (_targetHero != null && _targetHero.pendingLevelUpInfos.Count > 0)
            {
                LevelUpInfo nextInfo = _targetHero.pendingLevelUpInfos[0];
                _targetHero.pendingLevelUpInfos.RemoveAt(0);
                ShowLevelUp(_targetHero, nextInfo, _onCloseCallback);
            }
            else
            {
                _onCloseCallback?.Invoke();
            }
        }
    }
}
