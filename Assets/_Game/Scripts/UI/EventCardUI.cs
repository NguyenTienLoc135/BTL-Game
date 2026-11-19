using System;
using BTLGame.Cards;
using UnityEngine;

namespace BTLGame.UI
{
    public sealed class EventCardUI : MonoBehaviour
    {
        [SerializeField] private DecisionPopupUI _decisionPopup;

        public void ShowCard(EventCardData card, DecisionPrompt prompt)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            if (prompt == null || (prompt.Kind != DecisionPromptKind.EventCard && prompt.Kind != DecisionPromptKind.EventCardTargetSelection))
                throw new ArgumentException("Cần prompt thẻ hoặc chọn mục tiêu hợp lệ.", nameof(prompt));
            if (prompt.ContextId != ((int)card.Id).ToString())
                throw new ArgumentException("ContextId của prompt không khớp CardId.", nameof(prompt));
            if (_decisionPopup == null) throw new InvalidOperationException("Chưa gán DecisionPopupUI.");
            _decisionPopup.Show(prompt, card.Title, card.Description);
        }

        public void Configure(DecisionPopupUI decisionPopup) => _decisionPopup = decisionPopup;
    }
}
