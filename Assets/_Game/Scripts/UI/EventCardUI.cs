using System;
using BTLGame.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.UI
{
    public sealed class EventCardUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private Button _confirmButton;

        public event Action CardConfirmed;

        private void Awake()
        {
            if (_confirmButton == null)
            {
                Debug.LogError(
                    $"{nameof(EventCardUI)} requires a confirm button reference.",
                    this);
                return;
            }

            _confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        private void OnDestroy()
        {
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            }
        }

        public void ShowCard(EventCardData cardData)
        {
            if (cardData == null)
            {
                Debug.LogError(
                    $"{nameof(EventCardUI)} cannot display a null card.",
                    this);
                return;
            }

            if (_titleText == null || _descriptionText == null)
            {
                Debug.LogError(
                    $"{nameof(EventCardUI)} requires title and description references.",
                    this);
                return;
            }

            _titleText.text = cardData.Title;
            _descriptionText.text = cardData.Description;
            gameObject.SetActive(true);
        }

        private void HandleConfirmClicked()
        {
            gameObject.SetActive(false);
            CardConfirmed?.Invoke();
        }
    }
}
