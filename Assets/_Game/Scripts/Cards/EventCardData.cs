using UnityEngine;

namespace BTLGame.Cards
{
    public enum EventCardEffectType
    {
        [InspectorName("Nhận tiền")]
        ReceiveMoney,
        [InspectorName("Mất tiền")]
        LoseMoney,
        [InspectorName("Di chuyển")]
        Move,
        [InspectorName("Nhận thẻ bài")]
        ReceiveCard
    }

    [CreateAssetMenu(
        fileName = "EventCard_",
        menuName = "BTL Game/Cards/Event Card")]
    public sealed class EventCardData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _title;
        [TextArea(3, 6)]
        [SerializeField] private string _description;
        [SerializeField] private EventCardEffectType _effectType;
        [SerializeField] private long _effectValue;

        public string Id => _id;
        public string Title => _title;
        public string Description => _description;
        public EventCardEffectType EffectType => _effectType;
        public long EffectValue => _effectValue;
    }
}
