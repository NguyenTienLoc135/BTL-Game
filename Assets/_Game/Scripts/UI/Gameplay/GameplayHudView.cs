using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.UI
{
    /// <summary>Dữ liệu một người chơi để HUD hiển thị (HUD chỉ ĐỌC, không giữ tiền thật).</summary>
    public readonly struct HudPlayer
    {
        public readonly string Name;
        public readonly long Cash;
        public readonly Color Color;

        public HudPlayer(string name, long cash, Color color)
        {
            Name = name;
            Cash = cash;
            Color = color;
        }
    }

    /// <summary>
    /// HUD màn chơi: danh sách người chơi, thời gian, vòng, nút Tài sản/Thẻ/Kỹ năng/Menu, xúc xắc.
    /// View thuần: nhận dữ liệu qua Set*, phát ý định qua event; Core mới là nơi quyết định và ghi state.
    /// Giá trị mặc định trong Scene chỉ là mẫu để xem bố cục.
    /// </summary>
    public sealed class GameplayHudView : MonoBehaviour
    {
        public const int MaxPlayers = 4;

        [Serializable]
        private sealed class PlayerRow
        {
            public GameObject Root;
            public Image Avatar;
            public Text Name;
            public Text Cash;
            public GameObject Crown;
            public GameObject Highlight;
        }

        [SerializeField] private PlayerRow[] _rows = new PlayerRow[MaxPlayers];
        [SerializeField] private Color[] _seatColors = new Color[MaxPlayers];
        [SerializeField] private Text _clockText;
        [SerializeField] private Text _roundText;
        [SerializeField] private Image _dieA;
        [SerializeField] private Image _dieB;
        [SerializeField] private Sprite[] _diceFaces = new Sprite[6];
        [SerializeField] private Button _rollButton;
        [SerializeField] private Button _assetsButton;
        [SerializeField] private Button _cardsButton;
        [SerializeField] private Button _skillsButton;
        [SerializeField] private Button _menuButton;

        public event Action RollRequested;
        public event Action AssetsRequested;
        public event Action CardsRequested;
        public event Action SkillsRequested;
        public event Action MenuRequested;

        private void Awake()
        {
            Bind(_rollButton, () => RollRequested?.Invoke());
            Bind(_assetsButton, () => AssetsRequested?.Invoke());
            Bind(_cardsButton, () => CardsRequested?.Invoke());
            Bind(_skillsButton, () => SkillsRequested?.Invoke());
            Bind(_menuButton, () => MenuRequested?.Invoke());
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        /// <summary>Màu ghế cố định (ghế 0–3), dùng chung với màn thiết lập.</summary>
        public Color SeatColor(int seat)
        {
            return _seatColors != null && seat >= 0 && seat < _seatColors.Length ? _seatColors[seat] : Color.white;
        }

        public void SetPlayers(IReadOnlyList<HudPlayer> players)
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                var row = _rows[i];
                if (row == null || row.Root == null)
                {
                    continue;
                }

                bool has = players != null && i < players.Count;
                row.Root.SetActive(has);
                if (!has)
                {
                    continue;
                }

                row.Name.text = players[i].Name;
                row.Cash.text = FormatMoney(players[i].Cash);
                row.Avatar.color = players[i].Color;
            }
        }

        public void SetCurrentPlayer(int index)
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                var row = _rows[i];
                if (row == null)
                {
                    continue;
                }

                bool current = i == index;
                if (row.Crown != null) row.Crown.SetActive(current);
                if (row.Highlight != null) row.Highlight.SetActive(current);
            }
        }

        /// <summary>Hiển thị đồng hồ trận; null = không giới hạn thời gian.</summary>
        public void SetClock(int? remainingSeconds)
        {
            if (_clockText == null)
            {
                return;
            }

            if (remainingSeconds == null)
            {
                _clockText.text = "Thời gian: Vô hạn";
                return;
            }

            int s = Mathf.Max(0, remainingSeconds.Value);
            _clockText.text = $"Thời gian: {s / 60:00}:{s % 60:00}";
        }

        public void SetRound(int round)
        {
            if (_roundText != null)
            {
                _roundText.text = "Vòng: " + round;
            }
        }

        public void SetDice(int a, int b)
        {
            SetDie(_dieA, a);
            SetDie(_dieB, b);
        }

        public void SetRollEnabled(bool enabled)
        {
            if (_rollButton != null)
            {
                _rollButton.interactable = enabled;
            }
        }

        private void SetDie(Image image, int value)
        {
            if (image != null && _diceFaces != null && value >= 1 && value <= _diceFaces.Length && _diceFaces[value - 1] != null)
            {
                image.sprite = _diceFaces[value - 1];
            }
        }

        /// <summary>Định dạng tiền kiểu 300.000đ (số nguyên, dấu chấm phân cách nghìn).</summary>
        public static string FormatMoney(long cash)
        {
            return cash.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.') + "đ";
        }
    }
}
