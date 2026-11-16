using System.Collections.Generic;
using UnityEngine;

namespace BTLGame.Cards
{
    public sealed class EventCardManager : MonoBehaviour
    {
        [SerializeField] private List<EventCardData> _cards = new();

        public IReadOnlyList<EventCardData> Cards => _cards;

        public EventCardData DrawRandomCard()
        {
            int validCardCount = 0;
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null)
                {
                    validCardCount++;
                }
            }

            if (validCardCount == 0)
            {
                Debug.LogWarning(
                    $"{nameof(EventCardManager)} has no valid cards to draw.",
                    this);
                return null;
            }

            int selectedCardIndex = Random.Range(0, validCardCount);
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] == null)
                {
                    continue;
                }

                if (selectedCardIndex == 0)
                {
                    return _cards[i];
                }

                selectedCardIndex--;
            }

            Debug.LogError(
                $"{nameof(EventCardManager)} failed to select a card.",
                this);
            return null;
        }
    }
}
