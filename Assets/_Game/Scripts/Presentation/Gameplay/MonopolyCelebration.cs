using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.Presentation
{
    /// <summary>
    /// Màn chúc mừng "MONOPOLY" khi một người sở hữu trọn các thành phố của một quốc gia: hiện bằng animation (bảng bật lên, thẻ thành phố
    /// rơi vào, dải thông báo, xu xoay, tia sáng lấp lánh) trong 6 giây rồi tự tắt. Không chặn thao tác (không nhận raycast) và nằm DƯỚI
    /// các popup quyết định. Chỉ là presentation: nội dung do MonopolyWatcher lấy từ snapshot, không đụng luật hay state.
    /// Các lần kích hoạt đến khi đang chiếu được xếp hàng và chiếu lần lượt.
    /// </summary>
    public sealed class MonopolyCelebration : MonoBehaviour
    {
        public const float DefaultDuration = 6f;

        [Serializable]
        private sealed class CardView
        {
            public RectTransform Root;
            public Image Header;
            public Text Name;
            public Vector2 BasePosition;
            public float BaseRotation;
        }

        [SerializeField] private GameObject _content;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private RectTransform _crown;
        [SerializeField] private RectTransform _title;
        [SerializeField] private CardView[] _cards = new CardView[3];
        [SerializeField] private RectTransform _ribbon;
        [SerializeField] private Text _ribbonMain;
        [SerializeField] private Text _ribbonSub;
        [SerializeField] private RectTransform[] _coins = new RectTransform[0];
        [SerializeField] private RectTransform[] _sparkles = new RectTransform[0];
        [SerializeField] private RectTransform _dice;
        [SerializeField, Min(1f)] private float _duration = DefaultDuration;

        /// <summary>Phát khi bắt đầu chiếu một lần (cho hiệu ứng âm thanh).</summary>
        public event Action Started;

        private struct Request
        {
            public string Owner, Country;
            public IReadOnlyList<string> Cities;
            public IReadOnlyList<Color> Colors;
        }

        private readonly Queue<Request> _queue = new Queue<Request>();
        private bool _running;

        public bool IsPlaying => _running;
        public float Duration => _duration;

        public void Show(string ownerName, string countryName, IReadOnlyList<string> cityNames, IReadOnlyList<Color> cityColors)
        {
            _queue.Enqueue(new Request { Owner = ownerName, Country = countryName, Cities = cityNames, Colors = cityColors });
            if (!_running)
            {
                StartCoroutine(Run());
            }
        }

        private void Awake()
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] != null && _cards[i].Root != null)
                {
                    _cards[i].BasePosition = _cards[i].Root.anchoredPosition;
                    _cards[i].BaseRotation = _cards[i].Root.localEulerAngles.z;
                }
            }

            if (_content != null) _content.SetActive(false);
        }

        private IEnumerator Run()
        {
            _running = true;
            while (_queue.Count > 0)
            {
                yield return Play(_queue.Dequeue());
            }

            if (_content != null) _content.SetActive(false);
            _running = false;
        }

        private IEnumerator Play(Request request)
        {
            int count = Mathf.Min(request.Cities.Count, _cards.Length);
            float spacing = 340f;
            for (int i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                bool used = i < count;
                card.Root.gameObject.SetActive(used);
                if (!used) continue;

                card.Name.text = request.Cities[i];
                if (i < request.Colors.Count) card.Header.color = request.Colors[i];
                float x = (i - (count - 1) / 2f) * spacing;
                card.BasePosition = new Vector2(x, card.BasePosition.y);
                card.BaseRotation = (i - (count - 1) / 2f) * 4f;
            }

            if (_ribbonMain != null) _ribbonMain.text = request.Owner + " đã sở hữu trọn bộ " + count + " thành phố " + request.Country + "!";
            if (_ribbonSub != null) _ribbonSub.text = "Tiền thuê tăng thêm 20%!";

            if (_content != null) _content.SetActive(true);
            Started?.Invoke();

            float t = 0f;
            while (t < _duration)
            {
                t += Time.unscaledDeltaTime;
                Animate(t, count);
                yield return null;
            }

            if (_content != null) _content.SetActive(false);
        }

        private void Animate(float t, int count)
        {
            // Mờ dần vào 0.3s, mờ dần ra 0.6s cuối.
            float fadeIn = Mathf.Clamp01(t / 0.3f);
            float fadeOut = Mathf.Clamp01((_duration - t) / 0.6f);
            if (_group != null) _group.alpha = Mathf.Min(fadeIn, fadeOut);

            // Bảng chính bật lên (overshoot), cuối phim phình nhẹ khi tan.
            float pop = EaseOutBack(Mathf.Clamp01(t / 0.5f));
            float outro = t > _duration - 0.6f ? 1f + 0.05f * (1f - fadeOut) : 1f;
            if (_panel != null) _panel.localScale = Vector3.one * (Mathf.Lerp(0.55f, 1f, pop) * outro);

            if (_title != null) _title.localScale = Vector3.one * (1f + 0.035f * Mathf.Sin(t * 5f));
            if (_crown != null) _crown.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 3f) * 4f);

            for (int i = 0; i < count; i++)
            {
                var card = _cards[i];
                float u = Mathf.Clamp01((t - (0.35f + 0.18f * i)) / 0.5f);
                card.Root.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, EaseOutBack(u));
                float drop = (1f - EaseOutCubic(u)) * 280f;
                float bob = u >= 1f ? Mathf.Sin(t * 2.2f + i) * 6f : 0f;
                card.Root.anchoredPosition = card.BasePosition + new Vector2(0f, drop + bob);
                card.Root.localRotation = Quaternion.Euler(0f, 0f, card.BaseRotation * u);
            }

            if (_ribbon != null)
            {
                float u = Mathf.Clamp01((t - 1.0f) / 0.45f);
                _ribbon.localScale = new Vector3(EaseOutBack(u), 1f, 1f);
            }

            if (_ribbonSub != null) _ribbonSub.transform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(t * 6f));

            for (int i = 0; i < _coins.Length; i++)
            {
                var coin = _coins[i];
                if (coin == null) continue;
                float phase = t * 3f + i;
                coin.localScale = new Vector3(0.3f + 0.7f * Mathf.Abs(Mathf.Cos(phase)), 1f, 1f);
                coin.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.5f + i) * 20f);
            }

            for (int i = 0; i < _sparkles.Length; i++)
            {
                var sp = _sparkles[i];
                if (sp == null) continue;
                float s = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(t * 5f + i * 1.3f));
                sp.localScale = Vector3.one * s;
                sp.localRotation = Quaternion.Euler(0f, 0f, t * 50f);
            }

            if (_dice != null) _dice.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2f) * 12f);
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = x - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        private static float EaseOutCubic(float x)
        {
            float p = 1f - x;
            return 1f - p * p * p;
        }
    }
}
