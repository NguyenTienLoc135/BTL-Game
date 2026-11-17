using System.Collections.Generic;

namespace BtlGame.Core
{
    public sealed class PlayerSnapshot
    {
        public int Id { get; }
        public int SeatIndex { get; }
        public string DisplayName { get; }
        public long Cash { get; }
        public int Position { get; }
        public PlayerStatus Status { get; }
        public bool IsJailed { get; }
        public int JailTurnsStarted { get; }
        public long CompletedPersonalTurns { get; }
        public long ActiveReadyAfterCompletedTurns { get; }
        public bool UltimateUsed { get; }
        public bool HasBankerPassive { get; }
        public RoleId Role { get; }
        public ControllerKind Controller { get; }
        public long? BankerDebtDueAtTurnOrdinal { get; }
        public long BankerDebtAmount { get; }
        public IReadOnlyCollection<int> HeldCards { get; }

        internal PlayerSnapshot(PlayerState player)
        {
            Id = player.Id;
            SeatIndex = player.SeatIndex;
            DisplayName = player.DisplayName;
            Cash = player.Cash;
            Position = player.Position;
            Status = player.Status;
            IsJailed = player.IsJailed;
            JailTurnsStarted = player.JailTurnsStarted;
            CompletedPersonalTurns = player.CompletedPersonalTurns;
            ActiveReadyAfterCompletedTurns = player.ActiveReadyAfterCompletedTurns;
            UltimateUsed = player.UltimateUsed;
            HasBankerPassive = player.HasBankerPassive;
            Role = player.Role;
            Controller = player.Controller;
            BankerDebtDueAtTurnOrdinal = player.BankerDebtDueAtTurnOrdinal;
            BankerDebtAmount = player.BankerDebtAmount;
            HeldCards = new List<int>(player.HeldCards).AsReadOnly();
        }
    }

    public sealed class PropertySnapshot
    {
        public int TileId { get; }
        public int? OwnerId { get; }
        public PropertyLevel? Level { get; }
        public IReadOnlyList<long> InvestedByLevel { get; }
        public long BeachInvestment { get; }

        internal PropertySnapshot(PropertyState property)
        {
            TileId = property.TileId;
            OwnerId = property.OwnerId;
            Level = property.Level;
            InvestedByLevel = new List<long>(property.InvestedByLevel).AsReadOnly();
            BeachInvestment = property.BeachInvestment;
        }
    }

    public sealed class ObligationSnapshot
    {
        public long Id { get; }
        public ObligationKind Kind { get; }
        public int PayerId { get; }
        public int? RecipientId { get; }
        public long AssessedAmount { get; }
        public long PayableAmount { get; }
        public bool ActiveMitigationUsed { get; }
        public int? PropertyTileId { get; }

        internal ObligationSnapshot(MandatoryObligation obligation)
        {
            Id = obligation.Id;
            Kind = obligation.Kind;
            PayerId = obligation.PayerId;
            RecipientId = obligation.RecipientId;
            AssessedAmount = obligation.AssessedAmount;
            PayableAmount = obligation.PayableAmount;
            ActiveMitigationUsed = obligation.ActiveMitigationUsed;
            PropertyTileId = obligation.PropertyTileId;
        }
    }

    public sealed class TurnEffectSnapshot
    {
        public long Id { get; }
        public EffectKind Kind { get; }
        public int SourcePlayerId { get; }
        public int? TargetPlayerId { get; }
        public int? TargetTileId { get; }
        public int ModifierBasisPoints { get; }
        public long? ExpiresAfterSourceTurnOrdinal { get; }
        public long? ExpiresBeforeSourceTurnOrdinal { get; }
        public long? ExpiresAfterRoundOrdinal { get; }

        internal TurnEffectSnapshot(TurnEffectState effect)
        {
            Id = effect.Id;
            Kind = effect.Kind;
            SourcePlayerId = effect.SourcePlayerId;
            TargetPlayerId = effect.TargetPlayerId;
            TargetTileId = effect.TargetTileId;
            ModifierBasisPoints = effect.ModifierBasisPoints;
            ExpiresAfterSourceTurnOrdinal = effect.ExpiresAfterSourceTurnOrdinal;
            ExpiresBeforeSourceTurnOrdinal = effect.ExpiresBeforeSourceTurnOrdinal;
            ExpiresAfterRoundOrdinal = effect.ExpiresAfterRoundOrdinal;
        }
    }

    public sealed class GameSnapshot
    {
        public long Revision { get; }
        public long CompletedRounds { get; }
        public MatchStatus Status { get; }
        public WinReason? Result { get; }
        public IReadOnlyList<int> WinnerPlayerIds { get; }
        public int CurrentPlayerId { get; }
        public TurnPhase Phase { get; }
        public long CurrentLandingId { get; }
        public long CurrentActionId { get; }
        public bool PendingDoubleRoll { get; }
        public bool TaxSettledAtCurrentLanding { get; }
        public int? RentSettledAtCurrentLandingTileId { get; }
        public long DecisionId { get; }
        public BtlGame.Board.DiceRoll? PendingDiceRoll { get; }
        public BtlGame.Board.DiceRoll? LastDiceRoll { get; }
        public int? LastDicePlayerId { get; }
        public bool PendingJailTrial { get; }
        public BtlGame.Board.MovementResult PendingMovement { get; }
        public string PendingRuleDecisionId { get; }
        public IReadOnlyList<int> FixedFestivalCityIds { get; }
        public long CardDrawOrdinal { get; }
        public int? PendingCardId { get; }
        public IReadOnlyCollection<int> CardProcessedPlayerIds { get; }
        public long ClockRemainingTicks { get; }
        public bool TimeExpired { get; }
        public bool ClockPaused { get; }
        public ObligationSnapshot PendingObligation { get; }
        public IReadOnlyList<ObligationSnapshot> PendingObligations { get; }
        public bool GiftBatchActive { get; }
        public IReadOnlyList<TurnEffectSnapshot> ActiveEffects { get; }
        public IReadOnlyList<PlayerSnapshot> Players { get; }
        public IReadOnlyList<PropertySnapshot> Properties { get; }

        internal GameSnapshot(GameState state, int? viewerId = null)
        {
            Revision = state.Revision;
            CompletedRounds = state.CompletedRounds;
            Status = state.Status;
            Result = state.Result;
            WinnerPlayerIds = new List<int>(state.WinnerPlayerIds).AsReadOnly();
            CurrentPlayerId = state.CurrentPlayerId;
            Phase = state.Phase;
            CurrentLandingId = state.CurrentLandingId;
            CurrentActionId = state.CurrentActionId;
            PendingDoubleRoll = state.PendingDoubleRoll;
            TaxSettledAtCurrentLanding = state.TaxSettledAtCurrentLanding;
            RentSettledAtCurrentLandingTileId = state.RentSettledAtCurrentLandingTileId;
            DecisionId = state.DecisionId;
            PendingDiceRoll = state.PendingDiceRoll;
            LastDiceRoll = state.LastDiceRoll;
            LastDicePlayerId = state.LastDicePlayerId;
            PendingJailTrial = state.PendingJailTrial;
            PendingMovement = state.PendingMovement;
            PendingRuleDecisionId = state.PendingRuleDecisionId;
            FixedFestivalCityIds = new List<int>(state.FixedFestivalCityIds).AsReadOnly();
            CardDrawOrdinal = state.CardDrawOrdinal;
            PendingCardId = state.PendingCardId;
            CardProcessedPlayerIds = new List<int>(state.CardProcessedPlayerIds).AsReadOnly();
            ClockRemainingTicks = state.ClockRemainingTicks;
            TimeExpired = state.TimeExpired;
            ClockPaused = state.ClockPaused;
            MandatoryObligation visible = viewerId.HasValue && state.GiftBatchActive
                ? state.FindPendingForPayer(viewerId.Value) : state.PendingObligation;
            PendingObligation = visible == null ? null : new ObligationSnapshot(visible);
            GiftBatchActive = state.GiftBatchActive;
            var obligations = new List<ObligationSnapshot>();
            foreach (MandatoryObligation pending in state.PendingObligations) obligations.Add(new ObligationSnapshot(pending));
            PendingObligations = obligations.AsReadOnly();
            var players = new List<PlayerSnapshot>();
            foreach (PlayerState player in state.Players) players.Add(new PlayerSnapshot(player));
            Players = players.AsReadOnly();
            var properties = new List<PropertySnapshot>();
            foreach (PropertyState property in state.Properties) properties.Add(new PropertySnapshot(property));
            Properties = properties.AsReadOnly();
            var effects = new List<TurnEffectSnapshot>();
            foreach (TurnEffectState effect in state.ActiveEffects) effects.Add(new TurnEffectSnapshot(effect));
            ActiveEffects = effects.AsReadOnly();
        }
    }
}
