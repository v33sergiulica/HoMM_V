using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HommClone.Audio;

namespace HommClone.UI
{
    /// <summary>
    /// Top-Center Toast Notification Banner UI.
    /// Displays animated popups for world pickups (resources, spells, XP, mines, chests)
    /// with smooth slide & fade animation and sound effects.
    /// </summary>
    public class WorldNotificationUI : MonoBehaviour
    {
        public static WorldNotificationUI Instance { get; private set; }

        [Header("Audio Settings")]
        [SerializeField] private AudioClip defaultNotificationSound;

        private GameObject _canvasObj;
        private RectTransform _bannerRect;
        private CanvasGroup _canvasGroup;
        private Image _bannerBg;
        private Outline _bannerOutline;
        private Image _iconImg;
        private Outline _iconOutline;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _detailsText;

        private class NotificationItem
        {
            public string title;
            public string details;
            public Sprite icon;
            public AudioClip sound;
            public Color accentColor;

            public NotificationItem(string title, string details, Sprite icon, AudioClip sound, Color accentColor)
            {
                this.title = title;
                this.details = details;
                this.icon = icon;
                this.sound = sound;
                this.accentColor = accentColor;
            }
        }

        private Queue<NotificationItem> _notificationQueue = new Queue<NotificationItem>();
        private bool _isDisplaying = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateBannerHUD();
        }

        public static WorldNotificationUI GetOrCreateInstance()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<WorldNotificationUI>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject obj = new GameObject("WorldNotificationUI_Manager");
                    Instance = obj.AddComponent<WorldNotificationUI>();
                }
            }
            return Instance;
        }

        /// <summary>
        /// Displays a top-center banner notification for world map events (pickups, spells, chests, XP).
        /// </summary>
        public static void ShowNotification(string title, string details, Sprite icon = null, AudioClip sound = null, Color? accentColor = null)
        {
            var manager = GetOrCreateInstance();
            if (manager == null) return;

            Color accent = accentColor.HasValue ? accentColor.Value : new Color(1f, 0.85f, 0.3f);
            manager._notificationQueue.Enqueue(new NotificationItem(title, details, icon, sound, accent));

            if (!manager._isDisplaying)
            {
                manager.StartCoroutine(manager.ProcessNotificationQueue());
            }
        }

        private IEnumerator ProcessNotificationQueue()
        {
            _isDisplaying = true;

            while (_notificationQueue.Count > 0)
            {
                NotificationItem item = _notificationQueue.Dequeue();
                if (item == null) continue;

                if (_canvasObj == null) CreateBannerHUD();

                // Set content
                _titleText.text = $"<b><color=#{ColorUtility.ToHtmlStringRGB(item.accentColor)}>{item.title.ToUpper()}</color></b>";
                _detailsText.text = item.details;
                _bannerOutline.effectColor = item.accentColor * 0.85f;
                _iconOutline.effectColor = item.accentColor;

                if (item.icon != null)
                {
                    _iconImg.sprite = item.icon;
                    _iconImg.color = Color.white;
                }
                else
                {
                    _iconImg.sprite = null;
                    _iconImg.color = item.accentColor * 0.8f + new Color(0.2f, 0.2f, 0.2f, 0.8f);
                }

                // Play Audio SFX
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayNotificationSound(item.sound != null ? item.sound : defaultNotificationSound);
                }

                _canvasObj.SetActive(true);

                // Animation Phase 1: Slide down & Fade in (0.25s)
                float elapsed = 0f;
                float animDuration = 0.25f;
                Vector2 startPos = new Vector2(0f, 20f);
                Vector2 targetPos = new Vector2(0f, -55f);

                while (elapsed < animDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / animDuration);
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);

                    _bannerRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
                    _canvasGroup.alpha = smoothT;
                    yield return null;
                }
                _bannerRect.anchoredPosition = targetPos;
                _canvasGroup.alpha = 1f;

                // Animation Phase 2: Pause on screen (2.0s)
                yield return new WaitForSecondsRealtime(2.0f);

                // Animation Phase 3: Slide up & Fade out (0.35s)
                elapsed = 0f;
                animDuration = 0.35f;
                Vector2 fadeTargetPos = new Vector2(0f, -30f);

                while (elapsed < animDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / animDuration);
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);

                    _bannerRect.anchoredPosition = Vector2.Lerp(targetPos, fadeTargetPos, smoothT);
                    _canvasGroup.alpha = 1f - smoothT;
                    yield return null;
                }

                _canvasGroup.alpha = 0f;
                _canvasObj.SetActive(false);

                // Short delay before displaying next queued item
                if (_notificationQueue.Count > 0)
                {
                    yield return new WaitForSecondsRealtime(0.15f);
                }
            }

            _isDisplaying = false;
        }

        private void CreateBannerHUD()
        {
            if (_canvasObj != null) return;

            _canvasObj = new GameObject("WorldNotification_Canvas");
            _canvasObj.transform.SetParent(transform, false);

            Canvas canvas = _canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Higher than HUD resource bar
            _canvasObj.AddComponent<CanvasScaler>();
            _canvasObj.AddComponent<GraphicRaycaster>();

            _canvasGroup = _canvasObj.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;

            // Banner Panel Container
            GameObject panelObj = new GameObject("NotificationBanner");
            panelObj.transform.SetParent(_canvasObj.transform, false);

            _bannerRect = panelObj.AddComponent<RectTransform>();
            _bannerRect.anchorMin = new Vector2(0.5f, 1f);
            _bannerRect.anchorMax = new Vector2(0.5f, 1f);
            _bannerRect.pivot = new Vector2(0.5f, 1f);
            _bannerRect.anchoredPosition = new Vector2(0f, -55f);
            _bannerRect.sizeDelta = new Vector2(440f, 65f);

            _bannerBg = panelObj.AddComponent<Image>();
            _bannerBg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f); // Rich dark metallic obsidian

            _bannerOutline = panelObj.AddComponent<Outline>();
            _bannerOutline.effectColor = new Color(0.9f, 0.75f, 0.3f, 0.9f); // Gold outline
            _bannerOutline.effectDistance = new Vector2(2f, 2f);

            // Icon Frame (Left)
            GameObject iconFrameObj = new GameObject("IconFrame");
            iconFrameObj.transform.SetParent(panelObj.transform, false);
            RectTransform iconFrameRect = iconFrameObj.AddComponent<RectTransform>();
            iconFrameRect.anchorMin = new Vector2(0f, 0.5f);
            iconFrameRect.anchorMax = new Vector2(0f, 0.5f);
            iconFrameRect.pivot = new Vector2(0f, 0.5f);
            iconFrameRect.anchoredPosition = new Vector2(10f, 0f);
            iconFrameRect.sizeDelta = new Vector2(46f, 46f);

            Image iconFrameBg = iconFrameObj.AddComponent<Image>();
            iconFrameBg.color = new Color(0.05f, 0.05f, 0.08f, 1f);

            _iconOutline = iconFrameObj.AddComponent<Outline>();
            _iconOutline.effectColor = new Color(1f, 0.85f, 0.3f);
            _iconOutline.effectDistance = new Vector2(1.5f, 1.5f);

            // Icon Image
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(iconFrameObj.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = Vector2.zero;

            _iconImg = iconObj.AddComponent<Image>();

            // Text Container (Right side)
            GameObject textContainer = new GameObject("TextContainer");
            textContainer.transform.SetParent(panelObj.transform, false);
            RectTransform textContainerRect = textContainer.AddComponent<RectTransform>();
            textContainerRect.anchorMin = Vector2.zero;
            textContainerRect.anchorMax = Vector2.one;
            textContainerRect.offsetMin = new Vector2(66f, 4f);
            textContainerRect.offsetMax = new Vector2(-10f, -4f);

            // Title Text
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(textContainer.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = Vector2.zero;

            _titleText = titleObj.AddComponent<TextMeshProUGUI>();
            _titleText.fontSize = 13;
            _titleText.alignment = TextAlignmentOptions.Left;
            _titleText.color = new Color(1f, 0.85f, 0.3f);

            // Details Text
            GameObject detailsObj = new GameObject("DetailsText");
            detailsObj.transform.SetParent(textContainer.transform, false);
            RectTransform detailsRect = detailsObj.AddComponent<RectTransform>();
            detailsRect.anchorMin = new Vector2(0f, 0f);
            detailsRect.anchorMax = new Vector2(1f, 0.5f);
            detailsRect.pivot = new Vector2(0f, 0f);
            detailsRect.anchoredPosition = Vector2.zero;

            _detailsText = detailsObj.AddComponent<TextMeshProUGUI>();
            _detailsText.fontSize = 12;
            _detailsText.alignment = TextAlignmentOptions.Left;
            _detailsText.color = Color.white;

            _canvasObj.SetActive(false);
        }
    }
}
