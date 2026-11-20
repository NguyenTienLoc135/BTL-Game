using System;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.Presentation
{
    /// <summary>
    /// Nhóm nút chọn một trong nhiều (ví dụ 2/3/4 người, 30/45/60 phút/Vô hạn). Nút đang chọn có nền vàng.
    /// Dùng chung cho các màn thiết lập. Chỉ presentation, không chứa luật.
    /// </summary>
    public sealed class SegmentedSelector : MonoBehaviour
    {
        [Serializable]
        private sealed class Segment
        {
            public Button Button;
            public Image Background;
            public Text Label;
        }

        [SerializeField] private Segment[] _segments = new Segment[0];
        [SerializeField] private Sprite _selectedSprite;
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Color _selectedTint = Color.white;
        [SerializeField] private Color _normalTint = Color.white;

        private bool _bound;
        private int _selected;

        public event Action<int> Changed;

        public int SelectedIndex => _selected;
        public int Count => _segments.Length;

        private void Awake()
        {
            EnsureBound();
        }

        /// <summary>Chọn một mục; notify=false để đồng bộ UI mà không phát sự kiện.</summary>
        public void Select(int index, bool notify = true)
        {
            EnsureBound();
            if (_segments.Length == 0)
            {
                return;
            }

            _selected = Mathf.Clamp(index, 0, _segments.Length - 1);
            for (int i = 0; i < _segments.Length; i++)
            {
                var seg = _segments[i];
                bool on = i == _selected;
                if (seg.Background != null)
                {
                    seg.Background.sprite = on ? _selectedSprite : _normalSprite;
                    seg.Background.color = on ? _selectedTint : _normalTint;
                }

                if (seg.Label != null)
                {
                    seg.Label.color = on ? new Color(0.45f, 0.2f, 0.02f) : Color.white;
                }
            }

            if (notify)
            {
                Changed?.Invoke(_selected);
            }
        }

        private void EnsureBound()
        {
            if (_bound)
            {
                return;
            }

            _bound = true;
            for (int i = 0; i < _segments.Length; i++)
            {
                int index = i;
                if (_segments[i].Button != null)
                {
                    _segments[i].Button.onClick.AddListener(() => Select(index));
                }
            }
        }
    }
}
