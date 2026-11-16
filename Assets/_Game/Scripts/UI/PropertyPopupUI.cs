using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.UI
{
    public sealed class PropertyPopupUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _propertyNameText;
        [SerializeField] private TextMeshProUGUI _purchasePriceText;
        [SerializeField] private TextMeshProUGUI _currentRentText;
        [SerializeField] private Button _buyButton;
        [SerializeField] private Button _skipButton;

        public event Action BuyRequested;
        public event Action SkipRequested;

        private void Awake()
        {
            if (_propertyNameText == null
                || _purchasePriceText == null
                || _currentRentText == null)
            {
                Debug.LogError(
                    $"{nameof(PropertyPopupUI)} requires all property text references.",
                    this);
            }

            if (_buyButton == null || _skipButton == null)
            {
                Debug.LogError(
                    $"{nameof(PropertyPopupUI)} requires buy and skip button references.",
                    this);
                return;
            }

            _buyButton.onClick.AddListener(HandleBuyClicked);
            _skipButton.onClick.AddListener(HandleSkipClicked);
        }

        private void OnDestroy()
        {
            if (_buyButton != null)
            {
                _buyButton.onClick.RemoveListener(HandleBuyClicked);
            }

            if (_skipButton != null)
            {
                _skipButton.onClick.RemoveListener(HandleSkipClicked);
            }
        }

        public void ShowProperty(
            string propertyName,
            long purchasePrice,
            long currentRent)
        {
            if (!HasTextReferences())
            {
                return;
            }

            _propertyNameText.text = propertyName ?? string.Empty;
            _purchasePriceText.text = purchasePrice.ToString("N0");
            _currentRentText.text = currentRent.ToString("N0");
            SetButtonsInteractable(true);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            SetButtonsInteractable(false);
            gameObject.SetActive(false);
        }

        private void HandleBuyClicked()
        {
            if (!TryCompleteDecision())
            {
                return;
            }

            BuyRequested?.Invoke();
        }

        private void HandleSkipClicked()
        {
            if (!TryCompleteDecision())
            {
                return;
            }

            SkipRequested?.Invoke();
        }

        private bool TryCompleteDecision()
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            Hide();
            return true;
        }

        private bool HasTextReferences()
        {
            return _propertyNameText != null
                && _purchasePriceText != null
                && _currentRentText != null;
        }

        private void SetButtonsInteractable(bool isInteractable)
        {
            if (_buyButton != null)
            {
                _buyButton.interactable = isInteractable;
            }

            if (_skipButton != null)
            {
                _skipButton.interactable = isInteractable;
            }
        }
    }
}
