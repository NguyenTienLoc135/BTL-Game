using System;
using System.Collections.Generic;
using BTLGame.Board;
using BTLGame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.Presentation
{
    /// <summary>
    /// HUD màn chơi: danh sách người chơi, thời gian, vòng, nút Tài sản/Thẻ/Kỹ năng/Menu, xúc xắc.
    /// View thuần: nhận <see cref="GameHUDSnapshot"/> qua <see cref="Apply"/>, phát ý định qua event;
    /// Core (UnityMatchHost) mới là nơi quyết định và ghi state. Giá trị mặc định trong Scene chỉ là mẫu để xem bố cục.
    /// </summary>
    public sealed class GameplayHudView : MonoBehaviour
    {
        public const int MaxPlayers = 4;

        private static readonly Color TextOnGold = new Color(0.36f, 0.18f, 0.02f);
        private static readonly Color TextOnGoldSoft = new Color(0.42f, 0.22f, 0.03f);

        [Serializable]
        private sealed class PlayerRow
        {
            public GameObject Root;
            public Image Avatar;
            public Image Coin;
            public Text Name;
            public Text Role;
            public Text Cash;
            public GameObject Crown;
            public GameObject Highlight;
        }

        [SerializeField] private PlayerRow[] _rows = new PlayerRow[MaxPlayers];
        [SerializeField] private Text _clockText;
        [SerializeField] private Text _roundText;
        [SerializeField] private Image _dieA;
        [SerializeField] private Image _dieB;
        [SerializeField] private Sprite[] _diceFaces = new Sprite[6];
        [SerializeField] private Button _rollButton;
        [SerializeField] private Text _rollLabel;
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

        /// <summary>Màu ghế cố định (ghế 0–3), cùng nguồn với quân cờ trên bàn (BoardView).</summary>
        public static Color SeatColor(int seat) => seat >= 0 && seat < MaxPlayers ? BoardView.GetSeatColor(seat) : Color.white;

        /// <summary>Hiển thị toàn bộ HUD theo một bản chụp trạng thái do Core cung cấp.</summary>
        public void Apply(GameHUDSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            var infos = snapshot.Players;
            var list = new List<HudPlayer>(infos.Count);
            int current = -1;
            for (int i = 0; i < infos.Count; i++)
            {
                list.Add(new HudPlayer(infos[i].Name, infos[i].Cash, SeatColor(infos[i].Seat), infos[i].RoleName, infos[i].Bankrupt));
                if (infos[i].Id == snapshot.CurrentPlayerId)
                {
                    current = i;
                }
            }

            SetPlayers(list);
            SetCurrentPlayer(snapshot.MatchFinished ? -1 : current);
            SetClock(snapshot.ClockSeconds.HasValue ? (int?)(int)Math.Min(snapshot.ClockSeconds.Value, int.MaxValue) : null);
            SetRound((int)snapshot.Round);
            if (snapshot.Die1.HasValue && snapshot.Die2.HasValue)
            {
                SetDice(snapshot.Die1.Value, snapshot.Die2.Value);
            }

            SetActions(snapshot.CanRoll, snapshot.CanUseActiveSkill, snapshot.CanSell);
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

                var p = players[i];
                row.Name.text = p.Name;
                if (row.Role != null) row.Role.text = p.Role ?? string.Empty;
                row.Cash.text = p.Bankrupt ? "Phá sản" : FormatMoney(p.Cash);
                row.Avatar.color = p.Bankrupt ? new Color(0.6f, 0.6f, 0.65f) : p.Color;
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

                // Người tới lượt nằm trên nền vàng: chữ nâu đậm cho dễ đọc; các hàng còn lại nền xanh: chữ sáng.
                if (row.Name != null) row.Name.color = current ? TextOnGold : Color.white;
                if (row.Role != null) row.Role.color = current ? TextOnGoldSoft : new Color(0.86f, 0.9f, 1f);
                if (row.Cash != null) row.Cash.color = current ? TextOnGold : new Color(1f, 0.95f, 0.7f);
                if (row.Coin != null) row.Coin.color = current ? TextOnGold : new Color(1f, 0.82f, 0.2f);
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

        public void SetRollLabel(string label)
        {
            if (_rollLabel != null)
            {
                _rollLabel.text = label;
            }
        }

        /// <summary>Bật/tắt các nút hành động theo quyền hợp lệ do Core báo (Thẻ và Menu luôn dùng được).</summary>
        public void SetActions(bool canRoll, bool canUseSkill, bool canSell)
        {
            if (_rollButton != null) _rollButton.interactable = canRoll;
            if (_skillsButton != null) _skillsButton.interactable = canUseSkill;
            // Nút Tài sản luôn mở được (xem danh sách); việc bán nằm trong popup và dùng canSell.
            _ = canSell;
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

    /// <summary>Dữ liệu một người chơi để HUD hiển thị (HUD chỉ ĐỌC, không giữ tiền thật).</summary>
    public readonly struct HudPlayer
    {
        public readonly string Name;
        public readonly long Cash;
        public readonly Color Color;
        public readonly string Role;
        public readonly bool Bankrupt;

        public HudPlayer(string name, long cash, Color color, string role = null, bool bankrupt = false)
        {
            Name = name;
            Cash = cash;
            Color = color;
            Role = role;
            Bankrupt = bankrupt;
        }
    }
}
