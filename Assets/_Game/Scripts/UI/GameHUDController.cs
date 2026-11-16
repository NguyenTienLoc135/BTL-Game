using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.UI
{
    public sealed class GameHUDController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _playerMoneyText;
        [SerializeField] private TextMeshProUGUI _currentTurnText;
        [SerializeField] private Button _rollDiceButton;

        public event Action RollDiceRequested;

        private void Awake()
        {
            if (_playerMoneyText == null || _currentTurnText == null)
            {
                Debug.LogError(
                    $"{nameof(GameHUDController)} requires money and turn text references.",
                    this);
            }

            if (_rollDiceButton == null)
            {
                Debug.LogError(
                    $"{nameof(GameHUDController)} requires a roll dice button reference.",
                    this);
                return;
            }

            _rollDiceButton.onClick.AddListener(HandleRollDiceClicked);
        }

        private void OnDestroy()
        {
            if (_rollDiceButton != null)
            {
                _rollDiceButton.onClick.RemoveListener(HandleRollDiceClicked);
            }
        }

        public void UpdatePlayerMoney(int newMoney)
        {
            if (_playerMoneyText == null)
            {
                return;
            }

            _playerMoneyText.text = newMoney.ToString("N0");
        }

        public void UpdateCurrentTurnText(string playerName)
        {
            if (_currentTurnText == null)
            {
                return;
            }

            _currentTurnText.text = playerName ?? string.Empty;
        }

        public void SetRollDiceButtonInteractable(bool isInteractable)
        {
            if (_rollDiceButton != null)
            {
                _rollDiceButton.interactable = isInteractable;
            }
        }

        private void HandleRollDiceClicked()
        {
            RollDiceRequested?.Invoke();
        }
    }
}
