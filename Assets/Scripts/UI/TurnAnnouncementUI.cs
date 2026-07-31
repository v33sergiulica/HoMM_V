using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HommClone.UI
{
    public class TurnAnnouncementUI : MonoBehaviour
    {
        public static TurnAnnouncementUI Instance { get; private set; }

        private GameObject _bannerObj;
        private Text _bannerText;
        private CanvasGroup _canvasGroup;

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

            _bannerObj = new GameObject("TurnAnnouncementBanner", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            _bannerObj.transform.SetParent(canvas.transform, false);

            RectTransform bannerRect = _bannerObj.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0f, 0.7f);
            bannerRect.anchorMax = new Vector2(1f, 0.82f);
            bannerRect.sizeDelta = Vector2.zero;

            Image bannerImg = _bannerObj.GetComponent<Image>();
            bannerImg.color = new Color(0.1f, 0.12f, 0.18f, 0.92f);

            _canvasGroup = _bannerObj.GetComponent<CanvasGroup>();

            // Banner Text
            GameObject textObj = new GameObject("BannerText", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(_bannerObj.transform, false);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            _bannerText = textObj.GetComponent<Text>();
            _bannerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _bannerText.fontSize = 28;
            _bannerText.fontStyle = FontStyle.Bold;
            _bannerText.alignment = TextAnchor.MiddleCenter;
            _bannerText.color = new Color(1f, 0.85f, 0.3f);
            _bannerText.text = "PLAYER 1'S TURN - DAY 1";

            _bannerObj.SetActive(false);
        }

        public void AnnounceTurn(int playerIndex, int day, Action onComplete = null)
        {
            if (_bannerObj == null) BuildUI();

            Color playerColor = playerIndex == 1 ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.3f, 0.3f);
            string playerTitle = playerIndex == 1 ? "PLAYER 1" : "PLAYER 2";

            _bannerText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(playerColor)}><b>{playerTitle}'S TURN</b></color>  |  <color=#FFD700>DAY {day}</color>";

            StopAllCoroutines();
            StartCoroutine(BannerAnimationCoroutine(onComplete));
        }

        private IEnumerator BannerAnimationCoroutine(Action onComplete)
        {
            _bannerObj.SetActive(true);
            _canvasGroup.alpha = 0f;

            // Fade In
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / 0.3f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            _canvasGroup.alpha = 1f;

            // Hold Banner
            yield return new WaitForSeconds(1.5f);

            // Fade Out
            elapsed = 0f;
            while (elapsed < 0.3f)
            {
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.3f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            _canvasGroup.alpha = 0f;
            _bannerObj.SetActive(false);

            onComplete?.Invoke();
        }
    }
}
