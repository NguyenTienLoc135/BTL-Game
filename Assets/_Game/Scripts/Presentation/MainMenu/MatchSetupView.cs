using System;
using BtlGame.Characters;
using BtlGame.Core;
using BTLGame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.Presentation
{
    /// <summary>
    /// Màn thiết lập trận cho cả hai chế độ (Bot / Người): số người 2–4, thời gian 30/45/60/Vô hạn,
    /// chọn một trong 6 nghề cho từng người (được trùng nghề) và hiển thị Passive/Active/Ultimate/Weakness.
    /// Mô tả nghề đọc từ RoleCatalog (nguồn duy nhất của nhóm D), không lặp lại ở UI.
    /// Chỉ thu thập lựa chọn rồi phát <see cref="StartRequested"/>; không tạo trận, không ghi state.
    /// </summary>
    public sealed class MatchSetupView : MonoBehaviour
    {
        private const int MaxSeats = 4;
        private const float CardSpacing = 370f;

        [Serializable]
        private sealed class PlayerCard
        {
            public GameObject Root;
            public GameObject Focus;
            public Image SeatBar;
            public Text SeatName;
            public Image RoleIcon;
            public Text RoleName;
            public Button Prev;
            public Button Next;
            public Button Select;
        }

        [SerializeField] private MainMenuMode _mode = MainMenuMode.VsBot;
        [SerializeField] private int _defaultPlayerIndex;   // 0 → 2 người, 2 → 4 người
        [SerializeField] private int _defaultDurationIndex; // 0 → 30 phút … 3 → Vô hạn

        [Header("Lựa chọn chung")]
        [SerializeField] private SegmentedSelector _playerCountSelector;
        [SerializeField] private SegmentedSelector _durationSelector;

        [Header("Thẻ người chơi")]
        [SerializeField] private PlayerCard[] _cards = new PlayerCard[MaxSeats];
        [SerializeField] private Sprite[] _roleIcons = new Sprite[6];
        [SerializeField] private Color[] _seatColors = new Color[MaxSeats];

        [Header("Chi tiết nghề")]
        [SerializeField] private Text _detailTitle;
        [SerializeField] private Text _passiveText;
        [SerializeField] private Text _activeText;
        [SerializeField] private Text _ultimateText;
        [SerializeField] private Text _weaknessText;

        [Header("Nút")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _startButton;

        public event Action BackRequested;
        public event Action<MatchSetupRequest> StartRequested;

        private readonly int[] _roleIndex = { 0, 1, 2, 3 };
        private int _focus;
        private bool _initialized;

        private int PlayerCount => 2 + (_playerCountSelector != null ? _playerCountSelector.SelectedIndex : 0);

        private void OnEnable()
        {
            if (!_initialized)
            {
                Initialize();
            }

            Refresh();
        }

        private void Initialize()
        {
            _initialized = true;

            if (_playerCountSelector != null)
            {
                _playerCountSelector.Select(_defaultPlayerIndex, false);
                _playerCountSelector.Changed += _ => Refresh();
            }

            if (_durationSelector != null)
            {
                _durationSelector.Select(_defaultDurationIndex, false);
            }

            for (int i = 0; i < _cards.Length; i++)
            {
                int seat = i;
                var card = _cards[i];
                if (card == null) continue;
                if (card.Prev != null) card.Prev.onClick.AddListener(() => CycleRole(seat, -1));
                if (card.Next != null) card.Next.onClick.AddListener(() => CycleRole(seat, +1));
                if (card.Select != null) card.Select.onClick.AddListener(() => SetFocus(seat));
            }

            if (_backButton != null) _backButton.onClick.AddListener(() => BackRequested?.Invoke());
            if (_startButton != null) _startButton.onClick.AddListener(OnStartClicked);
        }

        private void CycleRole(int seat, int step)
        {
            int count = RoleCatalog.All.Count;
            _roleIndex[seat] = (_roleIndex[seat] + step + count) % count;
            _focus = seat;
            Refresh();
        }

        private void SetFocus(int seat)
        {
            _focus = seat;
            Refresh();
        }

        private void Refresh()
        {
            int n = PlayerCount;
            if (_focus >= n) _focus = 0;

            for (int i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                if (card == null || card.Root == null) continue;

                bool active = i < n;
                card.Root.SetActive(active);
                if (!active) continue;

                var rt = (RectTransform)card.Root.transform;
                rt.anchoredPosition = new Vector2((i - (n - 1) / 2f) * CardSpacing, rt.anchoredPosition.y);

                var role = RoleCatalog.All[_roleIndex[i]];
                card.SeatName.text = SeatName(i);
                if (card.SeatBar != null && i < _seatColors.Length) card.SeatBar.color = _seatColors[i];
                card.RoleName.text = role.DisplayName;
                if (card.RoleIcon != null && _roleIndex[i] < _roleIcons.Length) card.RoleIcon.sprite = _roleIcons[_roleIndex[i]];
                if (card.Focus != null) card.Focus.SetActive(i == _focus);
            }

            ShowDetail(RoleCatalog.All[_roleIndex[_focus]]);
        }

        private void ShowDetail(RoleDefinition role)
        {
            if (_detailTitle != null) _detailTitle.text = $"{SeatName(_focus)} — {role.DisplayName}";
            if (_passiveText != null) _passiveText.text = role.PassiveDescription;
            if (_activeText != null) _activeText.text = $"{role.ActiveDescription}\n(Hồi: {role.ActiveCooldownTurns} lượt riêng)";
            if (_ultimateText != null) _ultimateText.text = role.UltimateDescription;
            if (_weaknessText != null) _weaknessText.text = role.WeaknessDescription;
        }

        private string SeatName(int seat)
        {
            if (_mode == MainMenuMode.VsBot)
            {
                return seat == 0 ? "Bạn" : "Bot " + seat;
            }

            return "Người chơi " + (seat + 1);
        }

        private void OnStartClicked()
        {
            int n = PlayerCount;
            var roleIds = new string[n];
            for (int i = 0; i < n; i++)
            {
                roleIds[i] = ((RoleId)(_roleIndex[i] + 1)).ToString();
            }

            // Thứ tự lựa chọn trùng enum MatchTimeLimit: Minutes30, Minutes45, Minutes60, Unlimited.
            var limit = (MatchTimeLimit)(_durationSelector != null ? _durationSelector.SelectedIndex : 0);
            var mode = _mode == MainMenuMode.VsBot ? MatchControllerMode.VersusAI : MatchControllerMode.LocalHotSeat;
            StartRequested?.Invoke(new MatchSetupRequest(mode, n, limit, roleIds));
        }
    }
}
