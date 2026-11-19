using System;
using BTLGame.UI;
using UnityEngine;

namespace BTLGame.Cards
{
    public sealed class EventCardManager : MonoBehaviour
    {
        [SerializeField] private EventCardCatalog _catalog;
        [SerializeField] private EventCardUI _cardUI;

        public event Action<EventCardIntent> IntentRequested;

        public void Configure(EventCardCatalog catalog, EventCardUI cardUI)
        {
            _catalog = catalog;
            _cardUI = cardUI;
        }

        public EventCardData GetDefinition(EventCardId id)
        {
            if (_catalog == null)
            {
                Debug.LogError("EventCardCatalog chưa được gán.", this);
                return null;
            }
            return _catalog.TryGet(id, out EventCardData card) ? card : null;
        }

        public bool RequestIntent(EventCardId id, int actorId, long actionId,
            long expectedRevision, string selectionId)
        {
            if (_catalog == null || !_catalog.TryGet(id, out _))
            {
                Debug.LogError("Không thể gửi intent: CardId không có trong catalog.", this);
                return false;
            }
            if (actorId < 0 || actionId <= 0 || expectedRevision < 0)
                throw new ArgumentOutOfRangeException(nameof(actorId), "Thông tin intent không hợp lệ.");

            IntentRequested?.Invoke(new EventCardIntent(id, actorId, actionId, expectedRevision, selectionId));
            return true;
        }

        public void Present(EventCardId id, DecisionPrompt prompt, long actionId)
        {
            EventCardData definition = GetDefinition(id);
            if (definition == null) return;
            if (_cardUI == null) throw new InvalidOperationException("EventCardUI chưa được gán.");
            if (prompt.ActionId != actionId)
                throw new ArgumentException("ActionId của prompt không khớp action của thẻ.", nameof(prompt));
            _cardUI.ShowCard(definition, prompt);
        }

    }
}
