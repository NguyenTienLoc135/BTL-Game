using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.Presentation
{
    /// <summary>
    /// Màn "KẾT QUẢ TUNG XÚC XẮC": sau mỗi lần tung, hai viên xúc xắc lớn bay ra giữa bàn rồi dừng ở mặt kết quả, dải tiêu đề bật lên,
    /// phép cộng "a + b = tổng" hiện ra và tổng được đếm lên với vòng sao nổ. Chiếu 5 giây rồi tự tắt, không chặn thao tác
    /// (không nhận raycast) và nằm DƯỚI các popup quyết định. Lần tung mới đến khi đang chiếu thì thay thế lần cũ.
    /// Chỉ là presentation: giá trị lấy từ snapshot HUD (PresentationGameplayUi.SnapshotRendered), không đụng luật hay state.
    /// </summary>
    public sealed class DiceResultOverlay : MonoBehaviour
    {
        public const float DefaultDuration = 5f;

        [SerializeField] private PresentationGameplayUi _ui;
        [SerializeField] private GameObject _content;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _banner;
        [SerializeField] private RectTransform _formula;
        [SerializeField] private Image _formulaA;
        [SerializeField] private Image _formulaB;
        [SerializeField] private RectTransform _sumBurst;
        [SerializeField] private Text _sumText;
        [SerializeField] private Image _bigA;
        [SerializeField] private Image _bigB;
        [SerializeField] private RectTransform[] _sparkles = new RectTransform[0];
        [SerializeField] private Sprite[] _faces = new Sprite[6];
        [SerializeField, Min(1f)] private float _duration = DefaultDuration;

        private readonly DiceRollTracker _tracker = new DiceRollTracker();
        private RectTransform _rootA;
        private RectTransform _rootB;
        private Vector2 _bigABase;
        private Vector2 _bigBBase;
        private Coroutine _running;

        public bool IsPlaying => _running != null;
        public float Duration => _duration;

        private void Awake()
        {
            // Mỗi xúc xắc lớn = khung chứa (nền xanh cho chấm + mặt xúc xắc); animation tác động lên khung chứa.
            if (_bigA != null) { _rootA = (RectTransform)_bigA.transform.parent; _bigABase = _rootA.anchoredPosition; }
            if (_bigB != null) { _rootB = (RectTransform)_bigB.transform.parent; _bigBBase = _rootB.anchoredPosition; }
            if (_content != null) _content.SetActive(false);
        }

        private void OnEnable()
        {
            if (_ui != null) _ui.SnapshotRendered += OnSnapshot;
        }

        private void OnDisable()
        {
            if (_ui != null) _ui.SnapshotRendered -= OnSnapshot;
        }

        private void OnSnapshot(BTLGame.UI.GameHUDSnapshot snapshot)
        {
            if (_tracker.Observe(snapshot.Die1, snapshot.Die2, snapshot.DiceRollerId))
            {
                Show(snapshot.Die1.Value, snapshot.Die2.Value);
            }
        }

        /// <summary>Chiếu kết quả hai xúc xắc (1..6); thay thế lần đang chiếu nếu có.</summary>
        public void Show(int die1, int die2)
        {
            die1 = Mathf.Clamp(die1, 1, 6);
            die2 = Mathf.Clamp(die2, 1, 6);
            if (_running != null)
            {
                StopCoroutine(_running);
            }

            _running = StartCoroutine(Play(die1, die2));
        }

        private IEnumerator Play(int die1, int die2)
        {
            if (_content != null) _content.SetActive(true);
            SetFace(_formulaA, die1);
            SetFace(_formulaB, die2);
            if (_group != null) _group.alpha = 0f;

            float t = 0f;
            while (t < _duration)
            {
                t += Time.unscaledDeltaTime;
                Animate(t, die1, die2);
                yield return null;
            }

            if (_content != null) _content.SetActive(false);
            _running = null;
        }

        private void Animate(float t, int die1, int die2)
        {
            float fadeIn = Mathf.Clamp01(t / 0.2f);
            float fadeOut = Mathf.Clamp01((_duration - t) / 0.5f);
            if (_group != null) _group.alpha = Mathf.Min(fadeIn, fadeOut);

            AnimateDie(_bigA, _rootA, _bigABase, -1f, t, die1);
            AnimateDie(_bigB, _rootB, _bigBBase, 1f, t, die2);

            // Dải tiêu đề.
            Pop(_banner, (t - 0.8f) / 0.4f, 1f);

            // Phép cộng a + b = tổng.
            Pop(_formula, (t - 1.1f) / 0.4f, 1f);

            // Tổng đếm lên rồi nổ sao.
            float reveal = (t - 1.7f) / 0.45f;
            int sum = die1 + die2;
            if (_sumText != null)
            {
                float count = Mathf.Clamp01((t - 1.6f) / 0.6f);
                _sumText.text = (count >= 1f ? sum : Mathf.FloorToInt(count * (sum + 1))).ToString();
            }

            float pulse = reveal >= 1f ? 1f + 0.06f * Mathf.Sin((t - 2.15f) * 5f) : 1f;
            Pop(_sumBurst, reveal, 1.1f * pulse);
            if (_sumBurst != null) _sumBurst.localRotation = Quaternion.Euler(0f, 0f, reveal >= 1f ? Mathf.Sin(t * 1.5f) * 5f : 0f);

            for (int i = 0; i < _sparkles.Length; i++)
            {
                var sp = _sparkles[i];
                if (sp == null) continue;
                float on = Mathf.Clamp01((t - 1.7f) / 0.3f);
                float s = on * (0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(t * 5f + i * 1.3f)));
                sp.localScale = Vector3.one * s;
                sp.localRotation = Quaternion.Euler(0f, 0f, t * 50f);
            }
        }

        /// <summary>Xúc xắc lớn bay từ dưới lên, xoay tít, nhấp nháy mặt ngẫu nhiên rồi dừng ở mặt kết quả.</summary>
        private void AnimateDie(Image die, RectTransform rect, Vector2 home, float side, float t, int value)
        {
            if (die == null) return;
            const float flight = 0.8f;
            float u = Mathf.Clamp01(t / flight);
            float ease = EaseOutCubic(u);
            Vector2 start = home + new Vector2(side * 140f, -520f);
            float arc = Mathf.Sin(u * Mathf.PI) * 160f;

            float bob = u >= 1f ? Mathf.Sin(t * 2.4f + side) * 6f : 0f;
            rect.anchoredPosition = Vector2.Lerp(start, home, ease) + new Vector2(0f, arc + bob);
            rect.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, EaseOutBack(u));
            float spin = (1f - ease) * 720f * side;
            rect.localRotation = Quaternion.Euler(0f, 0f, spin + side * 10f * ease);

            // Mặt xúc xắc đổi liên tục khi đang lăn, đúng giá trị khi đã dừng.
            if (t < flight - 0.1f)
            {
                int flicker = (int)(t * 14f + (side > 0f ? 3 : 0)) % 6 + 1;
                SetFace(die, flicker);
            }
            else
            {
                SetFace(die, value);
            }
        }

        private void SetFace(Image image, int value)
        {
            if (image != null && _faces != null && value >= 1 && value <= _faces.Length && _faces[value - 1] != null)
            {
                image.sprite = _faces[value - 1];
            }
        }

        private static void Pop(RectTransform target, float u, float finalScale)
        {
            if (target == null) return;
            u = Mathf.Clamp01(u);
            target.localScale = Vector3.one * (finalScale * EaseOutBack(u));
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
