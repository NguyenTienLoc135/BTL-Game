using System;
using System.Collections.Generic;
using System.Text;
using BTLGame.Board;
using BTLGame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.Presentation
{
    /// <summary>
    /// Chuỗi màn cuối trận: (1) CHIẾN THẮNG — vinh danh người thắng, chỉ có nút "Tiếp tục"; (2) BẢNG XẾP HẠNG — xếp hạng đủ người chơi với
    /// các nút "Xem chi tiết", "Chơi lại", "Về menu"; (3) CHI TIẾT — từng người: nghề, tiền, tài sản, độc quyền, danh sách bất động sản.
    /// Chỉ đọc MatchResultSummary do host dựng từ snapshot; "Chơi lại"/"Về menu" chỉ phát ý định cho host.
    /// </summary>
    public sealed class MatchResultScreen : MonoBehaviour, IMatchResultUi
    {
        [Serializable]
        private sealed class RowView
        {
            public GameObject Root;
            public Image Background;
            public Text Rank;
            public Text Name;
            public Text Cash;
            public Text Assets;
            public Text Cities;
            public Text Beaches;
            public Text Status;
            public Image Dot;
        }

        [Header("Chiến thắng")]
        [SerializeField] private UiPopup _victory;
        [SerializeField] private Text _winnerName;
        [SerializeField] private Text _reason;
        [SerializeField] private Text[] _stats = new Text[5];
        [SerializeField] private Button _continue;
        [SerializeField] private RectTransform _crown;
        [SerializeField] private RectTransform[] _confetti = new RectTransform[0];

        [Header("Bảng xếp hạng")]
        [SerializeField] private UiPopup _board;
        [SerializeField] private RowView[] _rows = new RowView[4];
        [SerializeField] private Button _detailButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;
        [SerializeField] private Text _boardNote;

        [Header("Chi tiết")]
        [SerializeField] private UiPopup _detail;
        [SerializeField] private Button[] _tabs = new Button[4];
        [SerializeField] private Text[] _tabLabels = new Text[4];
        [SerializeField] private Text _summaryText;
        [SerializeField] private Text _propertyText;

        public event Action RestartRequested;
        public event Action MenuRequested;

        private MatchResultSummary _result;
        private float _shownAt;
        private int _selectedTab;

        private static readonly Color Gold = new Color(1f, 0.82f, 0.2f);

        private void Awake()
        {
            if (_continue != null) _continue.onClick.AddListener(ShowBoard);
            if (_detailButton != null) _detailButton.onClick.AddListener(OpenDetail);
            if (_restartButton != null) _restartButton.onClick.AddListener(() => RestartRequested?.Invoke());
            if (_menuButton != null) _menuButton.onClick.AddListener(() => MenuRequested?.Invoke());
            for (int i = 0; i < _tabs.Length; i++)
            {
                int index = i;
                if (_tabs[i] != null) _tabs[i].onClick.AddListener(() => SelectTab(index));
            }
        }

        public void ShowMatchResult(MatchResultSummary summary)
        {
            _result = summary;
            FillVictory();
            FillBoard();
            if (_board != null) _board.Close();
            if (_detail != null) _detail.Close();
            _shownAt = Time.unscaledTime;
            if (_victory != null) _victory.Open();
        }


#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Chiếu thử (chỉ Editor/Development build): F10 hiện chuỗi màn cuối trận với dữ liệu mẫu để kiểm tra giao diện.
        private void LateUpdate()
        {
            if (UnityEngine.InputSystem.Keyboard.current == null || !UnityEngine.InputSystem.Keyboard.current.f10Key.wasPressedThisFrame) return;
            var props = new List<MatchResultProperty>
            {
                new MatchResultProperty(1, "TP. Hồ Chí Minh", "Nhà 3", 800000), new MatchResultProperty(2, "Đà Nẵng", "Khách sạn", 960000),
                new MatchResultProperty(4, "Bãi biển", "Bãi biển", 100000)
            };
            var players = new List<MatchResultPlayer>
            {
                new MatchResultPlayer(0, 0, "Bạn", "Nhà Đầu Tư", 1, 215000, 1345000, 7, 3, new[] { "Việt Nam" }, props, true, false),
                new MatchResultPlayer(1, 1, "Bot 1", "Luật Sư", 2, 168000, 1090000, 6, 2, new string[0], props, false, false),
                new MatchResultPlayer(2, 2, "Bot 2", "Kỹ Sư", 3, 132500, 820000, 4, 2, new string[0], props, false, false),
                new MatchResultPlayer(3, 3, "Bot 3", "Chiến Lược Gia", 4, 0, 0, 0, 0, new string[0], new List<MatchResultProperty>(), false, true)
            };
            ShowMatchResult(new MatchResultSummary("Sở hữu trọn 3 quốc gia", 18, players));
        }
#endif
        // ------------------------------------------------------------ Chiến thắng
        private void FillVictory()
        {
            var winners = new List<MatchResultPlayer>(_result.Winners);
            if (winners.Count == 0 && _result.Players.Count > 0) winners.Add(_result.Players[0]);

            var names = new StringBuilder();
            for (int i = 0; i < winners.Count; i++)
            {
                if (i > 0) names.Append("  &  ");
                names.Append(winners[i].Name);
            }

            if (_winnerName != null) _winnerName.text = names.ToString().ToUpperInvariant();
            if (_reason != null) _reason.text = _result.ReasonLabel;

            var top = winners.Count > 0 ? winners[0] : null;
            if (top == null || _stats == null) return;
            string[] values =
            {
                GameplayHudView.FormatMoney(top.Cash), GameplayHudView.FormatMoney(top.TotalAssets),
                top.Cities.ToString(), top.Beaches.ToString(), top.MonopolyCountries.Count.ToString()
            };
            for (int i = 0; i < _stats.Length && i < values.Length; i++)
            {
                if (_stats[i] != null) _stats[i].text = values[i];
            }
        }

        private void Update()
        {
            if (_victory == null || !_victory.IsOpen) return;
            float t = Time.unscaledTime - _shownAt;
            if (_crown != null)
            {
                float pop = Mathf.Clamp01(t / 0.6f);
                _crown.localScale = Vector3.one * (Mathf.Lerp(0.3f, 1f, EaseOutBack(pop)) + 0.04f * Mathf.Sin(t * 3f));
                _crown.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2f) * 5f);
            }

            // Hoa giấy rơi liên tục, mỗi mảnh một tốc độ/pha riêng.
            for (int i = 0; i < _confetti.Length; i++)
            {
                var piece = _confetti[i];
                if (piece == null) continue;
                float speed = 140f + (i % 5) * 45f;
                float period = 1300f;
                float y = 620f - ((t * speed + i * 211f) % period);
                float x = ((i * 137) % 1800) - 900f + Mathf.Sin(t * 1.5f + i) * 40f;
                piece.anchoredPosition = new Vector2(x, y);
                piece.localRotation = Quaternion.Euler(0f, 0f, t * (90f + i * 13f));
            }
        }

        // ------------------------------------------------------------ Bảng xếp hạng
        private void ShowBoard()
        {
            if (_victory != null) _victory.Close();
            if (_board != null) _board.Open();
        }

        private void FillBoard()
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                var row = _rows[i];
                if (row == null || row.Root == null) continue;
                bool used = i < _result.Players.Count;
                row.Root.SetActive(used);
                if (!used) continue;

                var p = _result.Players[i];
                row.Rank.text = p.Rank.ToString();
                row.Name.text = p.Name;
                row.Cash.text = GameplayHudView.FormatMoney(p.Cash);
                row.Assets.text = GameplayHudView.FormatMoney(p.TotalAssets);
                row.Cities.text = p.Cities.ToString();
                row.Beaches.text = p.Beaches.ToString();
                row.Status.text = p.IsWinner ? "Dẫn đầu" : p.IsBankrupt ? "Đã phá sản" : "Còn trong trận";
                row.Status.color = p.IsWinner ? new Color(0.55f, 0.32f, 0.02f) : p.IsBankrupt ? new Color(0.65f, 0.2f, 0.2f) : new Color(0.2f, 0.45f, 0.25f);
                row.Dot.color = BoardView.GetSeatColor(p.SeatIndex);
                row.Background.color = p.IsWinner ? new Color(1f, 0.9f, 0.55f) : p.IsBankrupt ? new Color(0.88f, 0.88f, 0.92f) : RowTint(i);
            }

            if (_boardNote != null)
            {
                _boardNote.text = "Xếp hạng: người thắng trước, sau đó theo tiền mặt, rồi tổng giá trị bất động sản. " + _result.ReasonLabel + " · " + _result.Rounds + " vòng.";
            }
        }

        private static Color RowTint(int index)
        {
            switch (index)
            {
                case 1: return new Color(0.9f, 0.9f, 1f);
                case 2: return new Color(1f, 0.9f, 0.85f);
                default: return new Color(0.9f, 0.96f, 1f);
            }
        }

        // ------------------------------------------------------------ Chi tiết
        private void OpenDetail()
        {
            if (_result == null || _detail == null) return;
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool used = i < _result.Players.Count;
                if (_tabs[i] != null) _tabs[i].gameObject.SetActive(used);
                if (used && _tabLabels[i] != null) _tabLabels[i].text = "#" + _result.Players[i].Rank + " " + _result.Players[i].Name;
            }

            SelectTab(0);
            _detail.Open();
        }

        private void SelectTab(int index)
        {
            if (_result == null || index < 0 || index >= _result.Players.Count) return;
            _selectedTab = index;
            var p = _result.Players[index];
            for (int i = 0; i < _tabs.Length; i++)
            {
                if (_tabs[i] == null) continue;
                var image = _tabs[i].targetGraphic as Image;
                if (image != null) image.color = i == index ? Gold : new Color(0.45f, 0.55f, 0.85f);
                if (_tabLabels[i] != null) _tabLabels[i].color = i == index ? new Color(0.42f, 0.2f, 0.02f) : Color.white;
            }

            var summary = new StringBuilder();
            summary.AppendLine(p.Name + (p.IsWinner ? "  ★ Người thắng" : string.Empty));
            summary.AppendLine("Nghề: " + p.RoleName);
            summary.AppendLine("Trạng thái: " + (p.IsBankrupt ? "Đã phá sản" : "Còn trong trận"));
            summary.AppendLine("Hạng: " + p.Rank);
            summary.AppendLine();
            summary.AppendLine("Tiền mặt: " + GameplayHudView.FormatMoney(p.Cash));
            summary.AppendLine("Giá trị bất động sản: " + GameplayHudView.FormatMoney(p.PropertyValue));
            summary.AppendLine("Tổng tài sản: " + GameplayHudView.FormatMoney(p.TotalAssets));
            summary.AppendLine();
            summary.AppendLine("Thành phố: " + p.Cities + "   Bãi biển: " + p.Beaches);
            summary.Append("Độc quyền: " + (p.MonopolyCountries.Count == 0 ? "chưa có" : string.Join(", ", p.MonopolyCountries)));
            if (_summaryText != null) _summaryText.text = summary.ToString();

            var props = new StringBuilder();
            if (p.Properties.Count == 0)
            {
                props.Append("Không còn bất động sản.");
            }
            else
            {
                foreach (var property in p.Properties)
                {
                    props.AppendLine(property.Name + " — " + property.LevelLabel + " (" + GameplayHudView.FormatMoney(property.Value) + ")");
                }
            }

            if (_propertyText != null) _propertyText.text = props.ToString();
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = x - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }
    }
}
