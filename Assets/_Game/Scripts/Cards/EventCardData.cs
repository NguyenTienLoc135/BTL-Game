using UnityEngine;

namespace BTLGame.Cards
{
    public enum EventCardId
    {
        Bonus = 1,
        Earthquake = 2,
        TaxExemption = 3,
        RentReduction = 4,
        BuildingProtection = 5,
        TaxPenalty = 6,
        GoToJail = 7,
        JailRelease = 8,
        DestroyProperty = 9,
        OpponentGift = 10,
        MoveToTeleport = 11,
        OrganizeFestival = 12,
        PowerOutage = 13,
        GiftOpponent = 14
    }

    public enum EventCardActionKind
    {
        ReceiveBankPayment,
        ChooseEarthquakeTarget,
        HoldTaxExemption,
        HoldRentReduction,
        HoldBuildingProtection,
        AssessTax,
        GoToJail,
        HoldJailRelease,
        ChoosePropertyToDestroy,
        CreateOpponentPaymentBatch,
        MoveToTeleport,
        ChooseFestivalCity,
        ChoosePowerOutageTarget,
        ChooseGiftRecipient
    }

    [CreateAssetMenu(fileName = "EventCard_", menuName = "BTL Game/Cards/Event Card")]
    public sealed class EventCardData : ScriptableObject
    {
        [SerializeField] private EventCardId _id;
        [SerializeField] private string _title;
        [TextArea(2, 5), SerializeField] private string _description;
        [SerializeField] private EventCardActionKind _actionKind;
        [SerializeField] private bool _canBeHeld;

        public EventCardId Id => _id;
        public string Title => _title;
        public string Description => _description;
        public EventCardActionKind ActionKind => _actionKind;
        public bool CanBeHeld => _canBeHeld;

    }
}
