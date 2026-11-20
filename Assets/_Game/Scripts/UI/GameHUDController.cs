using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.UI
{
    public sealed class GameHUDController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private TMP_Text _playerMoneyText;
        [SerializeField] private TMP_Text _currentTurnText;
        [SerializeField] private TMP_Text _diceText;
        [SerializeField] private TMP_Text _clockText;
        [SerializeField] private TMP_Text _propertiesText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Button _rollDiceButton;
        [SerializeField] private Button _activeSkillButton;
        [SerializeField] private Button _sellButton;

        private GameHUDSnapshot _snapshot;
        public event Action<GameplayUiAction> ActionRequested;

        public void Configure(TMP_Text playerName, TMP_Text cash, TMP_Text turn, TMP_Text dice,
            TMP_Text clock, TMP_Text properties, TMP_Text status,
            Button roll, Button activeSkill, Button sell)
        {
            _playerNameText = playerName;
            _playerMoneyText = cash;
            _currentTurnText = turn;
            _diceText = dice;
            _clockText = clock;
            _propertiesText = properties;
            _statusText = status;
            _rollDiceButton = roll;
            _activeSkillButton = activeSkill;
            _sellButton = sell;
        }

        private void Awake()
        {
            if (_rollDiceButton != null) _rollDiceButton.onClick.AddListener(HandleRollClicked);
            if (_activeSkillButton != null) _activeSkillButton.onClick.AddListener(HandleSkillClicked);
            if (_sellButton != null) _sellButton.onClick.AddListener(HandleSellClicked);
            SetButtons(false, false, false);
        }

        private void OnDestroy()
        {
            if (_rollDiceButton != null) _rollDiceButton.onClick.RemoveListener(HandleRollClicked);
            if (_activeSkillButton != null) _activeSkillButton.onClick.RemoveListener(HandleSkillClicked);
            if (_sellButton != null) _sellButton.onClick.RemoveListener(HandleSellClicked);
        }

        public void Render(GameHUDSnapshot snapshot)
        {
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            SetText(_playerNameText, snapshot.PlayerName);
            SetText(_playerMoneyText, snapshot.Cash.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " đ");
            SetText(_currentTurnText, snapshot.TurnLabel);
            SetText(_diceText, snapshot.DiceLabel);
            SetText(_clockText, snapshot.ClockLabel);
            SetText(_propertiesText, JoinLines(snapshot.PropertyLines));
            SetText(_statusText, JoinLines(snapshot.StatusLines));
            SetButtons(snapshot.CanRoll, snapshot.CanUseActiveSkill, snapshot.CanSell);
        }

        private void HandleRollClicked() => Request(GameplayUiActionKind.Roll);

        public void SetRollLabel(string label)
        {
            TMP_Text text = _rollDiceButton == null ? null : _rollDiceButton.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = label;
        }
        private void HandleSkillClicked() => Request(GameplayUiActionKind.UseActiveSkill);
        private void HandleSellClicked() => Request(GameplayUiActionKind.OpenSellSelection);

        private void Request(GameplayUiActionKind kind)
        {
            if (_snapshot == null) return;
            SetButtons(false, false, false);
            ActionRequested?.Invoke(new GameplayUiAction(kind, _snapshot.ActorId, _snapshot.Revision));
        }

        private void SetButtons(bool canRoll, bool canUseSkill, bool canSell)
        {
            if (_rollDiceButton != null) _rollDiceButton.interactable = canRoll;
            if (_activeSkillButton != null) _activeSkillButton.interactable = canUseSkill;
            if (_sellButton != null) _sellButton.interactable = canSell;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null) target.text = value ?? string.Empty;
        }

        private static string JoinLines(System.Collections.Generic.IReadOnlyList<string> lines)
        {
            if (lines == null || lines.Count == 0) return "Chưa có dữ liệu trận.";
            var builder = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0) builder.AppendLine();
                builder.Append(lines[i]);
            }
            return builder.ToString();
        }
    }
}
