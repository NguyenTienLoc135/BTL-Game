using System;
using System.Collections.Generic;
using UnityEngine;

namespace BTLGame.Cards
{
    [CreateAssetMenu(fileName = "EventCardCatalog", menuName = "BTL Game/Cards/Card Catalog")]
    public sealed class EventCardCatalog : ScriptableObject
    {
        [SerializeField] private EventCardData[] _cards = Array.Empty<EventCardData>();
        private Dictionary<EventCardId, EventCardData> _lookup;

        public IReadOnlyList<EventCardData> Cards => _cards;

        public bool TryGet(EventCardId id, out EventCardData card)
        {
            BuildLookup();
            return _lookup.TryGetValue(id, out card);
        }

        public bool IsComplete(out string error)
        {
            BuildLookup();
            foreach (EventCardId id in Enum.GetValues(typeof(EventCardId)))
            {
                if (_lookup.ContainsKey(id)) continue;
                error = "Thiếu định nghĩa thẻ " + (int)id + ".";
                return false;
            }
            if (_lookup.Count != 14)
            {
                error = "Catalog phải có đúng 14 loại thẻ duy nhất.";
                return false;
            }
            error = null;
            return true;
        }

        private void BuildLookup()
        {
            if (_lookup != null) return;
            _lookup = new Dictionary<EventCardId, EventCardData>();
            if (_cards == null) return;
            foreach (EventCardData card in _cards)
                if (card != null && !_lookup.ContainsKey(card.Id)) _lookup.Add(card.Id, card);
        }

        private void OnValidate() => _lookup = null;
    }
}
