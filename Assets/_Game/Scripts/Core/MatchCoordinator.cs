using System;
using BtlGame.Board;
using BtlGame.Properties;
using BtlGame.Skills;

namespace BtlGame.Core
{
    public sealed partial class MatchCoordinator
    {
        private const long StartTileReward = 200_000;
        private const long TeleportFee = 100_000;
        private long _nextObligationId = 1;
        private long _nextEffectId = 1;
        private readonly IDiceSource _diceSource;
        private readonly ICardActionProvider _cardProvider;
        public GameState State { get; }

        public MatchCoordinator(GameState state, IDiceSource diceSource = null, ICardActionProvider cardProvider = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            _diceSource = diceSource ?? new RandomDiceSource();
            _cardProvider = cardProvider;
        }

        public GameSnapshot QuerySnapshot() => State.CreateSnapshot();
        public GameSnapshot QuerySnapshot(int playerId)
        {
            State.GetPlayer(playerId);
            return new GameSnapshot(State, playerId);
        }

        public MovementResult RollAndMove(int playerId)
        {
            EnsureCanRoll(playerId);
            return MoveByDice(playerId, _diceSource.Roll());
        }

        private MovementResult MoveByDice(int playerId, DiceRoll roll, bool alreadyCommitted = false)
        {
            EnsureRunning();
            if (State.CurrentPlayerId != playerId) throw new InvalidOperationException("Chưa đến lượt người chơi.");
            if (State.Phase != TurnPhase.AwaitingRoll) throw new InvalidOperationException("Chưa có quyền gieo xúc xắc.");
            if (State.TimeExpired && !alreadyCommitted) throw new InvalidOperationException("Đã hết giờ, không thể bắt đầu lần gieo mới.");
            PlayerState player = State.GetPlayer(playerId);
            if (player.Status != PlayerStatus.Active || player.IsJailed)
                throw new InvalidOperationException("Người chơi không thể di chuyển bằng xúc xắc lúc này.");

            MovementResult movement = BoardRules.CalculateMovement(player.Position, roll);
            MovementResult applied = ApplyMovement(player, movement);
            State.LastDiceRoll = roll;
            State.LastDicePlayerId = playerId;
            State.TeleportUsedInCurrentChain = false;
            return applied;
        }

        private MovementResult ApplyMovement(PlayerState player, MovementResult movement, bool preserveDouble = false,
            long movementFee = 0)
        {
            long startBonus = checked(movement.StartCrossings * StartTileReward);
            long resultingCash = checked(player.Cash - movementFee + startBonus);
            player.Position = movement.ToTileId;
            player.Cash = resultingCash;
            State.CurrentActionId++;
            State.CurrentLandingId++;
            State.CurrentLandingDecisionUsed = false;
            State.RentSettledAtCurrentLandingTileId = null;
            State.TaxSettledAtCurrentLanding = false;
            State.PendingDoubleRoll = preserveDouble ? State.PendingDoubleRoll : movement.IsDouble;
            State.LandingResolutionStarted = false;
            State.CardLandingResolved = false;
            State.PendingRuleDecisionId = null;
            if (movement.ToTileId == 8)
            {
                player.IsJailed = true;
                player.JailTurnsStarted = 0;
                State.PendingDoubleRoll = false;
                State.PersonalTurnMustClose = true;
                State.Phase = TurnPhase.EndTurn;
            }
            else
            {
                State.Phase = TurnPhase.ResolvingLanding;
            }
            State.Revision++;
            return movement;
        }

        /// <summary>Gọi sau khi toàn bộ ô đến và các continuation trực tiếp đã được xử lý.</summary>
        public void CompleteLanding(long actionId)
        {
            EnsureRunning();
            if (State.Phase != TurnPhase.ResolvingLanding || State.ClockPaused || actionId != State.CurrentActionId)
                throw new InvalidOperationException("Callback ô đến đã cũ hoặc đã được xử lý.");
            EnsureMandatoryLandingResolved();
            State.Phase = !State.TimeExpired && State.PendingDoubleRoll
                ? TurnPhase.AwaitingRoll
                : TurnPhase.EndTurn;
            State.Revision++;
        }

        public void SendCurrentPlayerToJail()
        {
            EnsureRunning();
            if (State.Phase != TurnPhase.ResolvingLanding)
                throw new InvalidOperationException("Chỉ xử lý Vào tù trong chuỗi ô đang dừng.");
            PlayerState player = GetActivePlayer(State.CurrentPlayerId);
            player.Position = 8;
            player.IsJailed = true;
            player.JailTurnsStarted = 0;
            State.PendingDoubleRoll = false;
            State.Phase = TurnPhase.EndTurn;
            State.PersonalTurnMustClose = true;
            State.Revision++;
        }

        public void CompletePersonalTurn()
        {
            EnsureRunning();
            if (State.Phase != TurnPhase.EndTurn) throw new InvalidOperationException("Lượt chưa đến pha kết thúc.");
            if (State.ClockPaused) throw new InvalidOperationException("Còn popup quyết định đang tạm dừng trận.");
            if (State.PendingObligation != null) throw new InvalidOperationException("Còn nghĩa vụ chưa giải quyết.");
            State.PersonalTurnMustClose = true;
            PlayerState current = State.GetPlayer(State.CurrentPlayerId);
            long endingTurnOrdinal = current.CompletedPersonalTurns + 1;
            if (current.Status == PlayerStatus.Active && current.BankerDebtDueAtTurnOrdinal == endingTurnOrdinal)
            {
                long amount = current.BankerDebtAmount;
                current.BankerDebtAmount = 0;
                current.BankerDebtDueAtTurnOrdinal = null;
                CreateObligation(ObligationKind.BankerDebt, current.Id, null, amount);
                State.Phase = TurnPhase.AwaitingObligation;
                return;
            }
            State.RoundCompletedAtLastTurnBoundary = false;
            if (current.Status == PlayerStatus.Active)
            {
                current.CompletedPersonalTurns++;
                State.MarkTurnCompletedThisRound(current.Id);
                ExpireAfterSourceTurn(current);
            }
            State.RoundCompletedAtLastTurnBoundary = State.IsRoundComplete();
            if (State.RoundCompletedAtLastTurnBoundary)
            {
                State.CompletedRounds++;
                ExpireAfterRoundEffects();
                ApplyBankerInterest();
                State.ResetRound();
            }

            PlayerState next = FindNextActivePlayer(current.SeatIndex);
            if (next == null)
            {
                if (State.TimeExpired) FinishByTime();
                else CheckLastSolventPlayer();
                return;
            }
            ExpireBeforeNextSourceTurn(next);
            if (State.TimeExpired)
            {
                FinishByTime();
                return;
            }
            State.CurrentPlayerId = next.Id;
            State.PersonalTurnMustClose = false;
            if (next.IsJailed)
            {
                next.JailTurnsStarted++;
                State.Phase = TurnPhase.AwaitingJailChoice;
            }
            else State.Phase = TurnPhase.AwaitingRoll;

            State.Revision++;
        }

        public void IssueBankerLoan(int playerId, int activeCooldownTurns)
        {
            EnsureRunning();
            PlayerState banker = GetActivePlayer(playerId);
            EnsureCurrentPlayer(playerId);
            if (!banker.HasBankerPassive) throw new InvalidOperationException("Chỉ Banker được yêu cầu khoản vay này.");
            EnsureActiveActionWindow(playerId);
            if (!banker.IsActiveReady) throw new InvalidOperationException("Kỹ năng Banker còn cooldown.");
            if (activeCooldownTurns <= 0) throw new ArgumentOutOfRangeException(nameof(activeCooldownTurns));
            if (banker.BankerDebtDueAtTurnOrdinal.HasValue)
                throw new InvalidOperationException("Đang có khoản vay Banker chưa trả.");
            long newCash = checked(banker.Cash + 100_000);
            banker.Cash = newCash;
            banker.BankerDebtAmount = 110_000;
            banker.BankerDebtDueAtTurnOrdinal = checked(banker.CompletedPersonalTurns + 6);
            CommitActiveCooldown(banker, activeCooldownTurns);
            State.Revision++;
        }

        public void CommitActiveSkillUse(int playerId, int cooldownTurns)
        {
            EnsureRunning();
            PlayerState player = GetActivePlayer(playerId);
            EnsureActiveActionWindow(playerId, allowRentReaction: true);
            if (!player.IsActiveReady) throw new InvalidOperationException("Kỹ năng còn cooldown.");
            if (cooldownTurns <= 0) throw new ArgumentOutOfRangeException(nameof(cooldownTurns));
            bool rentReaction = State.Phase == TurnPhase.AwaitingObligation &&
                State.PendingObligation != null && State.PendingObligation.Kind == ObligationKind.Rent &&
                State.PendingObligation.PayerId == playerId;
            if (rentReaction && (State.PendingObligation.PayableAmount == 0 || State.PendingObligation.ActiveMitigationUsed))
                throw new InvalidOperationException("Không tiêu thêm kỹ năng cho nghĩa vụ đã được miễn/giảm.");
            CommitActiveCooldown(player, cooldownTurns, rentReaction && State.CurrentPlayerId != playerId);
            State.Revision++;
        }

        public void CommitUltimateUse(int playerId)
        {
            EnsureRunning();
            PlayerState player = GetActivePlayer(playerId);
            EnsureActiveActionWindow(playerId);
            if (player.UltimateUsed) throw new InvalidOperationException("Ultimate đã được dùng trong trận này.");
            player.UltimateUsed = true;
            State.Revision++;
        }

        public void ActivateRentDoubleUltimate(int playerId)
        {
            EnsureRunning();
            PlayerState player = GetActivePlayer(playerId);
            EnsureCurrentPlayer(playerId);
            EnsureActiveActionWindow(playerId);
            if (player.UltimateUsed) throw new InvalidOperationException("Ultimate đã được dùng trong trận này.");
            var effect = new TurnEffectState(_nextEffectId++, EffectKind.RentDouble, playerId, null, null,
                0, null, checked(player.CompletedPersonalTurns + 2), player.CompletedPersonalTurns + 1);
            State.AddEffect(effect);
            player.UltimateUsed = true;
            State.Revision++;
        }

        public TurnEffectState AddSourceTurnDurationEffect(int sourcePlayerId, EffectKind kind, int durationOwnTurns,
            int? targetPlayerId = null, int? targetTileId = null, int modifierBasisPoints = 0)
        {
            EnsureRunning();
            PlayerState source = GetActivePlayer(sourcePlayerId);
            EnsureEffectActionWindow(sourcePlayerId);
            if (kind == EffectKind.TemporaryFestival)
                throw new ArgumentException("Lễ hội toàn bàn không có thời hạn lượt; phải qua hành động tổ chức lễ hội.", nameof(kind));
            if (kind == EffectKind.RentDouble) throw new ArgumentException("RentDouble dùng mốc Q05 riêng.", nameof(kind));
            if (durationOwnTurns <= 0) throw new ArgumentOutOfRangeException(nameof(durationOwnTurns));
            if (kind == EffectKind.PaymentImmunity && durationOwnTurns != 1)
                throw new ArgumentException("Miễn nghĩa vụ chỉ có hiệu lực trong lượt kích hoạt.", nameof(durationOwnTurns));
            if (targetPlayerId.HasValue) GetActivePlayer(targetPlayerId.Value);
            if (targetTileId.HasValue && BoardCatalog.Get(targetTileId.Value).Kind != TileKind.City)
                throw new ArgumentException("Hiệu ứng này cần ô thành phố.", nameof(targetTileId));

            long expiry = kind == EffectKind.PaymentImmunity
                ? checked(source.CompletedPersonalTurns + 1L)
                : checked(source.CompletedPersonalTurns + durationOwnTurns + 1L);
            var effect = new TurnEffectState(_nextEffectId++, kind, sourcePlayerId, targetPlayerId,
                targetTileId, modifierBasisPoints, expiry, null, source.CompletedPersonalTurns + 1);
            State.AddEffect(effect);
            State.Revision++;
            return effect;
        }

        public bool IsRentDoubleActive(int ownerPlayerId)
        {
            foreach (TurnEffectState effect in State.ActiveEffects)
                if (effect.Kind == EffectKind.RentDouble && effect.SourcePlayerId == ownerPlayerId) return true;
            return false;
        }

        public RentQuote QuoteCityRent(int tileId, int additionalModifierBasisPoints = 0)
        {
            PropertyState property = State.GetProperty(tileId);
            TileDefinition tile = BoardCatalog.Get(tileId);
            if (tile.Kind != TileKind.City || !property.OwnerId.HasValue || !property.Level.HasValue)
                throw new InvalidOperationException("Chưa có thành phố cho thuê.");

            int modifiers = additionalModifierBasisPoints + SkillRules.RentModifierBasisPoints(State.GetPlayer(property.OwnerId.Value).Role);
            foreach (int festivalCityId in State.FixedFestivalCityIds)
                if (festivalCityId == tileId) { modifiers += 5_000; break; }
            bool exclusive = OwnsAllCitiesOfCountry(property.OwnerId.Value, tile.Country.Value);
            if (exclusive) modifiers += 2_000;
            bool doubled = IsRentDoubleActive(property.OwnerId.Value);
            bool outage = false;
            foreach (TurnEffectState effect in State.ActiveEffects)
            {
                if (effect.Kind == EffectKind.RentBonus && effect.SourcePlayerId == property.OwnerId &&
                    (!effect.TargetPlayerId.HasValue || effect.TargetPlayerId == property.OwnerId) &&
                    (!effect.TargetTileId.HasValue || effect.TargetTileId == tileId))
                    modifiers = checked(modifiers + effect.ModifierBasisPoints);
                if (effect.TargetTileId != tileId) continue;
                if (effect.Kind == EffectKind.PowerOutage) outage = true;
                if (effect.Kind == EffectKind.TemporaryFestival) modifiers = checked(modifiers + effect.ModifierBasisPoints);
            }
            return EconomyRules.CityRent(tileId, property.Level.Value, modifiers, doubled,
                false, false, false, false, false, outage);
        }

        public RentQuote QuoteBeachRent(int tileId, int additionalModifierBasisPoints = 0)
        {
            PropertyState property = State.GetProperty(tileId);
            if (BoardCatalog.Get(tileId).Kind != TileKind.Beach || !property.OwnerId.HasValue)
                throw new InvalidOperationException("Chưa có bãi biển cho thuê.");
            int ownedBeachCount = 0;
            foreach (PropertyState candidate in State.Properties)
                if (BoardCatalog.Get(candidate.TileId).Kind == TileKind.Beach && candidate.OwnerId == property.OwnerId)
                    ownedBeachCount++;
            int modifiers = additionalModifierBasisPoints + SkillRules.RentModifierBasisPoints(State.GetPlayer(property.OwnerId.Value).Role);
            foreach (TurnEffectState effect in State.ActiveEffects)
                if (effect.SourcePlayerId == property.OwnerId && effect.Kind == EffectKind.RentBonus &&
                    (!effect.TargetPlayerId.HasValue || effect.TargetPlayerId == property.OwnerId) &&
                    (!effect.TargetTileId.HasValue || effect.TargetTileId == tileId))
                    modifiers += effect.ModifierBasisPoints;
            return EconomyRules.BeachRent(ownedBeachCount, modifiers, IsRentDoubleActive(property.OwnerId.Value));
        }

        public MandatoryObligation CreateObligation(ObligationKind kind, int payerId, int? recipientId,
            long fixedAmount, int? propertyTileId = null)
        {
            EnsureRunning();
            if (State.PendingObligation != null) throw new InvalidOperationException("Một nghĩa vụ khác đang được giải quyết.");
            if (fixedAmount < 0) throw new ArgumentOutOfRangeException(nameof(fixedAmount));
            GetActivePlayer(payerId);
            if (recipientId.HasValue) GetActivePlayer(recipientId.Value);
            if (recipientId == payerId) throw new ArgumentException("Bên trả và nhận không thể là cùng người chơi.", nameof(recipientId));
            if (propertyTileId.HasValue)
            {
                TileKind propertyKind = BoardCatalog.Get(propertyTileId.Value).Kind;
                if (propertyKind != TileKind.City && propertyKind != TileKind.Beach)
                    throw new ArgumentException("Ô liên quan không phải tài sản.", nameof(propertyTileId));
            }
            if (kind == ObligationKind.Rent)
            {
                if (State.RentSettledAtCurrentLandingTileId == propertyTileId && propertyTileId.HasValue)
                    throw new InvalidOperationException("Tiền thuê của lần dừng này đã được giải quyết.");
                if (!propertyTileId.HasValue || State.Phase != TurnPhase.ResolvingLanding ||
                    payerId != State.CurrentPlayerId || State.GetPlayer(payerId).Position != propertyTileId.Value)
                    throw new InvalidOperationException("Tiền thuê chỉ được ghi nhận tại ô đang dừng của người đi lượt.");
                PropertyState rented = State.GetProperty(propertyTileId.Value);
                if (rented.OwnerId != recipientId || recipientId == payerId)
                    throw new InvalidOperationException("Bên nhận thuê không khớp chủ tài sản.");
            }
            if (kind == ObligationKind.TaxTile)
            {
                if (payerId != State.CurrentPlayerId || State.Phase != TurnPhase.ResolvingLanding ||
                    State.GetPlayer(payerId).Position != 30 || State.TaxSettledAtCurrentLanding)
                    throw new InvalidOperationException("Thuế ô30 chỉ xử lý một lần trong lần dừng hiện tại.");
            }
            if (kind == ObligationKind.ForcedJailReleaseFee)
            {
                PlayerState jailedPayer = State.GetPlayer(payerId);
                if (payerId != State.CurrentPlayerId || State.Phase != TurnPhase.AwaitingJailChoice ||
                    !jailedPayer.IsJailed || jailedPayer.JailTurnsStarted != 3)
                    throw new InvalidOperationException("Phí tù bắt buộc chỉ phát sinh sau lần thử thất bại ở lượt tù thứ ba.");
                if (HasExistingPaymentImmunity(payerId) && State.Decisions.LawyerWaivesForcedJailFee != false)
                    throw new InvalidOperationException("P02 chưa chốt tương tác miễn phí tù bắt buộc.");
            }
            MandatoryObligation obligation = new MandatoryObligation(_nextObligationId++, kind, payerId,
                recipientId, fixedAmount, propertyTileId);
            ApplyExistingPaymentEffects(obligation);
            State.PhaseBeforeObligation = State.Phase;
            State.ClockPausedBeforeObligation = State.ClockPaused;
            State.AddObligation(obligation);
            State.PendingObligation = obligation;
            State.ClockPaused = true;
            State.Phase = TurnPhase.AwaitingObligation;
            State.Revision++;
            return obligation;
        }

        public MandatoryObligation AssessTax(int payerId, ObligationKind kind, int? recipientId = null,
            int passiveReductionBasisPoints = 0)
        {
            if (kind != ObligationKind.TaxTile && kind != ObligationKind.TaxCard)
                throw new ArgumentException("Nguồn nghĩa vụ không phải thuế.", nameof(kind));
            if (kind == ObligationKind.TaxTile && passiveReductionBasisPoints != 0)
                throw new InvalidOperationException("Passive Luật Sư không giảm thuế ô 30.");
            if (payerId != State.CurrentPlayerId || State.Phase != TurnPhase.ResolvingLanding)
                throw new InvalidOperationException("Thuế chỉ được đánh giá trong lần dừng đang xử lý.");
            if (kind == ObligationKind.TaxTile && State.GetPlayer(payerId).Position != 30)
                throw new InvalidOperationException("Thuế ô chỉ áp dụng tại ô 30.");
            PlayerState payer = State.GetPlayer(payerId);
            if (kind == ObligationKind.TaxCard)
                passiveReductionBasisPoints += SkillRules.TaxCardReductionBasisPoints(payer.Role);
            MoneyFraction assessed = EconomyRules.TaxAssessment(payer.Cash, payerId, State.Properties, passiveReductionBasisPoints);
            return CreateObligation(kind, payerId, recipientId, assessed.FloorToLong());
        }

        public void SettlePendingObligation()
        {
            EnsureRunning();
            MandatoryObligation obligation = State.PendingObligation
                ?? throw new InvalidOperationException("Không có nghĩa vụ đang chờ.");
            PlayerState payer = State.GetPlayer(obligation.PayerId);
            if (payer.Cash < obligation.PayableAmount)
                throw new InvalidOperationException("Cần thanh lý tài sản trước khi trả nghĩa vụ.");
            TransferCash(payer, obligation.RecipientId, obligation.PayableAmount);
            obligation.Status = ObligationStatus.Settled;
            State.PendingObligation = null;
            ResumeAfterObligation(obligation);
            if (obligation.Kind == ObligationKind.Rent && obligation.PropertyTileId.HasValue &&
                obligation.PayerId == State.CurrentPlayerId)
                State.RentSettledAtCurrentLandingTileId = obligation.PropertyTileId;
            if (obligation.Kind == ObligationKind.TaxTile && obligation.PayerId == State.CurrentPlayerId)
                State.TaxSettledAtCurrentLanding = true;
            State.Revision++;
            CheckLastSolventPlayer();
        }

        public void ApplyActiveMitigation(long obligationId, long reducedPayable, Action commitResourceConsumption)
        {
            EnsureRunning();
            MandatoryObligation obligation = State.PendingObligation
                ?? throw new InvalidOperationException("Không có nghĩa vụ đang chờ.");
            if (obligation.Id != obligationId) throw new InvalidOperationException("Intent áp dụng cho nghĩa vụ cũ.");
            if (obligation.Kind == ObligationKind.ForcedJailReleaseFee)
                throw new InvalidOperationException("P02 chưa được quyết định cho miễn phí tù bắt buộc.");
            if (obligation.PayableAmount <= 0) throw new InvalidOperationException("Nghĩa vụ bằng 0 không tiêu hao tài nguyên.");
            if (obligation.ActiveMitigationUsed) throw new InvalidOperationException("Chỉ được một biện pháp chủ động cho mỗi nghĩa vụ.");
            if (reducedPayable < 0 || reducedPayable >= obligation.PayableAmount)
                throw new ArgumentOutOfRangeException(nameof(reducedPayable));
            if (commitResourceConsumption == null) throw new ArgumentNullException(nameof(commitResourceConsumption));

            commitResourceConsumption();
            obligation.PayableAmount = reducedPayable;
            obligation.ActiveMitigationUsed = true;
            State.Revision++;
        }

        public long LiquidatePropertyForPendingObligation(int payerId, int tileId)
        {
            EnsureRunning();
            MandatoryObligation obligation = State.PendingObligation
                ?? throw new InvalidOperationException("Không có nghĩa vụ đang chờ.");
            if (obligation.PayerId != payerId) throw new InvalidOperationException("Người chơi không phải bên trả nghĩa vụ.");
            PlayerState payer = GetActivePlayer(payerId);
            PropertyState property = State.GetProperty(tileId);
            if (property.OwnerId != payerId) throw new InvalidOperationException("Người chơi không sở hữu tài sản này.");
            EnsureOwnershipChangeAllowed(tileId);
            long refund = EconomyRules.BankSaleValue(property.TotalInvestment);
            payer.Cash = checked(payer.Cash + refund);
            OnPropertyOwnershipChanging(tileId);
            ResetProperty(property);
            RemovePowerOutageForTile(tileId);
            State.Revision++;

            if (payer.Cash >= obligation.PayableAmount)
            {
                SettlePendingObligation();
            }
            else if (!HasProperty(payerId) && CanResolveSourceBankruptcy(payerId))
            {
                DeclareBankruptcy(payerId);
            }
            return refund;
        }

        public void DeclareBankruptcy(int playerId)
        {
            EnsureRunning();
            MandatoryObligation obligation = State.PendingObligation
                ?? throw new InvalidOperationException("Chỉ được phá sản khi còn nghĩa vụ không trả được.");
            if (obligation.PayerId != playerId) throw new InvalidOperationException("Người chơi không phải bên trả nghĩa vụ.");
            PlayerState debtor = GetActivePlayer(playerId);
            if (debtor.Cash >= obligation.PayableAmount)
                throw new InvalidOperationException("Cần thanh toán nghĩa vụ nếu còn đủ tiền.");
            if (HasProperty(playerId)) throw new InvalidOperationException("Phải thanh lý toàn bộ tài sản trước khi phá sản.");
            EnsureSourceBankruptcyEffectsDecided(playerId);

            if (obligation.RecipientId.HasValue)
            {
                PlayerState recipient = State.GetPlayer(obligation.RecipientId.Value);
                recipient.Cash = checked(recipient.Cash + debtor.Cash);
            }
            debtor.Cash = 0;
            debtor.Status = PlayerStatus.Bankrupt;
            RemoveBankruptSourceEffects(playerId);
            State.RemoveFromRound(playerId);
            foreach (PropertyState property in State.Properties)
                if (property.OwnerId == playerId)
                {
                    ResetProperty(property);
                    RemovePowerOutageForTile(property.TileId);
                }
            obligation.Status = ObligationStatus.Defaulted;
            State.PendingObligation = null;
            State.ClockPaused = State.ClockPausedBeforeObligation;
            State.Phase = playerId == State.CurrentPlayerId ? TurnPhase.EndTurn : State.PhaseBeforeObligation;
            if (State.GiftBatchActive) RefreshGiftBatchAfterPayment();
            State.Revision++;
            CheckLastSolventPlayer();
        }


        public void AdvanceClock(TimeSpan elapsed)
        {
            EnsureRunning();
            if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(elapsed));
            if (!State.ClockPaused && State.Duration != MatchDuration.Unlimited && !State.TimeExpired)
            {
                State.ClockRemainingTicks = Math.Max(0, State.ClockRemainingTicks - elapsed.Ticks);
                if (State.ClockRemainingTicks == 0)
                {
                    State.TimeExpired = true;
                    if (State.Phase == TurnPhase.AwaitingRoll || State.Phase == TurnPhase.AwaitingJailChoice ||
                        State.Phase == TurnPhase.EndTurn)
                    {
                        State.Phase = TurnPhase.EndTurn;
                        CompletePersonalTurn();
                        return;
                    }
                }
                State.Revision++;
            }
        }

        public void SetDecisionPending(bool pending)
        {
            EnsureRunning();
            State.ClockPaused = pending || State.PendingObligation != null;
            State.Revision++;
        }

        public DiceRoll ChooseJailRoll(int playerId, bool lawyerUltimateCouldMitigateForcedFee)
        {
            EnsureRunning();
            PlayerState player = RequireJailTurn(playerId);
            if (State.Phase != TurnPhase.AwaitingJailChoice) throw new InvalidOperationException("Không ở lượt chọn hành động trong tù.");
            if (player.JailTurnsStarted > 3) throw new InvalidOperationException("Số lượt tù không hợp lệ.");
            DiceRoll roll = _diceSource.Roll();
            roll = new DiceRoll(roll.First, roll.Second);
            ResolveJailRoll(playerId, roll, lawyerUltimateCouldMitigateForcedFee);
            return roll;
        }

        public void PayToLeaveJail(int playerId)
        {
            PlayerState player = RequireJailTurn(playerId);
            if (State.Phase != TurnPhase.AwaitingJailChoice) throw new InvalidOperationException("Chưa đến bước chọn cách ra tù.");
            if (player.Cash < 200_000) throw new InvalidOperationException("Không đủ 200.000 để ra tù.");
            player.Cash -= 200_000;
            player.IsJailed = false;
            player.JailTurnsStarted = 0;
            State.Phase = TurnPhase.AwaitingRoll;
            State.Revision++;
        }

        public void ReleaseFromJailWithCard(int playerId)
        {
            PlayerState player = RequireJailTurn(playerId);
            if (State.Phase != TurnPhase.AwaitingJailChoice) throw new InvalidOperationException("Chưa đến bước chọn cách ra tù.");
            if (!player.HasHeldCard(8)) throw new InvalidOperationException("Người chơi không có thẻ Ra tù.");
            player.ConsumeHeldCard(8);
            player.IsJailed = false;
            player.JailTurnsStarted = 0;
            State.Phase = TurnPhase.AwaitingRoll;
            State.Revision++;
        }

        public long BuyCity(int playerId, int tileId, PropertyLevel level, int discountBasisPoints = 0)
        {
            EnsureRunning();
            PlayerState buyer = GetActivePlayer(playerId);
            EnsureCurrentPlayer(playerId);
            EnsureStandingOn(playerId, tileId);
            PropertyState property = State.GetProperty(tileId);
            TileDefinition tile = BoardCatalog.Get(tileId);
            if (tile.Kind != TileKind.City || property.OwnerId != null)
                throw new InvalidOperationException("Thành phố không thể mua từ ngân hàng.");
            EnsureLandingDecisionAvailable();
            if (level == PropertyLevel.Hotel) throw new InvalidOperationException("Không thể mua Khách sạn lần đầu.");
            discountBasisPoints += PurchaseDiscount(buyer);
            ValidateDiscount(discountBasisPoints);
            long listedPrice = EconomyRules.ListedPrice(tileId, level);
            long cost = EconomyRules.RentToPay(listedPrice, discountBasisPoints);
            if (buyer.Cash < cost) throw new InvalidOperationException("Không đủ tiền mua.");
            System.Collections.Generic.IReadOnlyList<long> investment = EconomyRules.AllocateInvestment(cost, level, State.Decisions.InvestmentTieBreak);

            buyer.Cash -= cost;
            ConsumePurchaseCoupon(buyer.Id);
            property.OwnerId = buyer.Id;
            property.AcquiredOnLandingId = State.CurrentLandingId;
            property.Level = level;
            for (int i = 0; i < investment.Count; i++) property.MutableInvestedByLevel[i] = investment[i];
            if (level == PropertyLevel.House3) property.House3ReachedLandingId = State.CurrentLandingId;
            State.CurrentLandingDecisionUsed = true;
            State.Revision++;
            CheckImmediateWin(buyer.Id);
            return cost;
        }

        public long BuyBeach(int playerId, int tileId, int discountBasisPoints = 0)
        {
            EnsureRunning();
            PlayerState buyer = GetActivePlayer(playerId);
            EnsureCurrentPlayer(playerId);
            EnsureStandingOn(playerId, tileId);
            PropertyState property = State.GetProperty(tileId);
            TileDefinition tile = BoardCatalog.Get(tileId);
            if (tile.Kind != TileKind.Beach || property.OwnerId != null)
                throw new InvalidOperationException("Bãi biển không thể mua từ ngân hàng.");
            EnsureLandingDecisionAvailable();
            discountBasisPoints += PurchaseDiscount(buyer);
            ValidateDiscount(discountBasisPoints);
            long cost = EconomyRules.RentToPay(tile.BeachPrice, discountBasisPoints);
            if (buyer.Cash < cost) throw new InvalidOperationException("Không đủ tiền mua.");

            buyer.Cash -= cost;
            ConsumePurchaseCoupon(buyer.Id);
            property.OwnerId = buyer.Id;
            property.AcquiredOnLandingId = State.CurrentLandingId;
            property.BeachInvestment = cost;
            State.CurrentLandingDecisionUsed = true;
            State.Revision++;
            CheckImmediateWin(buyer.Id);
            return cost;
        }

        public long ForceBuyCity(int buyerId, int tileId, int purchaseBasisPoints, bool authorizedRemote = false)
        {
            EnsureRunning();
            PlayerState buyer = GetActivePlayer(buyerId);
            EnsureCurrentPlayer(buyerId);
            PropertyState property = State.GetProperty(tileId);
            TileDefinition tile = BoardCatalog.Get(tileId);
            if (tile.Kind != TileKind.City || !property.OwnerId.HasValue || property.OwnerId == buyerId || !property.Level.HasValue)
                throw new InvalidOperationException("Ô không đủ điều kiện mua lại.");
            if (property.Level == PropertyLevel.Hotel)
                throw new InvalidOperationException("Không thể mua lại Khách sạn.");
            EnsureOwnershipChangeAllowed(tileId);
            if (purchaseBasisPoints != 15_000 && purchaseBasisPoints != NormalForcedPurchaseBasisPoints(buyer))
                throw new ArgumentOutOfRangeException(nameof(purchaseBasisPoints));
            if (authorizedRemote && State.TimeExpired)
                throw new InvalidOperationException("Không thể mở kỹ năng chủ động mới sau khi hết giờ.");
            if (!authorizedRemote) EnsureLandingDecisionAvailable();
            if (purchaseBasisPoints == 15_000 && !authorizedRemote)
                throw new InvalidOperationException("Giá mua từ xa cần intent Ultimate hợp lệ.");
            if (authorizedRemote && purchaseBasisPoints != 15_000)
                throw new InvalidOperationException("Mua lại từ xa dùng mức giá 150%.");
            if (!authorizedRemote) EnsureStandingOn(buyerId, tileId);
            if (!authorizedRemote && State.RentSettledAtCurrentLandingTileId != tileId)
                throw new InvalidOperationException("Phải giải quyết tiền thuê trước khi mua lại thường.");
            if ((!authorizedRemote && State.Phase != TurnPhase.ResolvingLanding) ||
                (authorizedRemote && State.Phase != TurnPhase.AwaitingRoll && State.Phase != TurnPhase.ResolvingLanding &&
                    State.Phase != TurnPhase.EndTurn && State.Phase != TurnPhase.AwaitingJailChoice))
                throw new InvalidOperationException("Mua lại chỉ tiếp tục trong lần dừng đang xử lý.");

            long cost = new MoneyFraction(checked(EconomyRules.ListedPrice(tileId, property.Level.Value) * purchaseBasisPoints), 10_000).FloorToLong();
            if (buyer.Cash < cost) throw new InvalidOperationException("Không đủ tiền mua lại.");
            var investments = EconomyRules.AllocateInvestment(cost, property.Level.Value, State.Decisions.InvestmentTieBreak);
            PlayerState formerOwner = State.GetPlayer(property.OwnerId.Value);
            long formerOwnerCash = checked(formerOwner.Cash + cost);

            buyer.Cash -= cost;
            formerOwner.Cash = formerOwnerCash;
            OnPropertyOwnershipChanging(tileId);
            property.OwnerId = buyerId;
            property.AcquiredOnLandingId = State.CurrentLandingId;
            property.House3ReachedLandingId = null;
            Array.Clear(property.MutableInvestedByLevel, 0, property.MutableInvestedByLevel.Length);
            for (int i = 0; i < investments.Count; i++) property.MutableInvestedByLevel[i] = investments[i];
            property.BeachInvestment = 0;
            RemovePowerOutageForTile(tileId);
            State.CurrentLandingDecisionUsed = true;
            State.Revision++;
            CheckImmediateWin(buyerId);
            return cost;
        }

        public PropertyAttackResult ApplyEarthquake(int actorId, int tileId)
        {
            EnsureRunning();
            EnsureCurrentPlayer(actorId);
            if (State.Phase != TurnPhase.ResolvingLanding) throw new InvalidOperationException("Thẻ chỉ được xử lý trong chuỗi ô hiện tại.");
            PropertyState property = State.GetProperty(tileId);
            if (BoardCatalog.Get(tileId).Kind != TileKind.City || !property.OwnerId.HasValue || !property.Level.HasValue ||
                property.OwnerId == actorId || property.Level == PropertyLevel.Stake || property.Level == PropertyLevel.Hotel)
                throw new InvalidOperationException("Chỉ Cọc–Nhà3 có thể bị hạ một cấp.");
            PlayerState owner = State.GetPlayer(property.OwnerId.Value);
            if (owner.HasHeldCard(5))
            {
                owner.ConsumeHeldCard(5);
                State.Revision++;
                return new PropertyAttackResult(true, 0);
            }
            int lostTier = (int)property.Level.Value - 1;
            long lostInvestment = property.InvestedByLevel[lostTier];
            property.MutableInvestedByLevel[lostTier] = 0;
            property.Level = (PropertyLevel)((int)property.Level.Value - 1);
            property.House3ReachedLandingId = null;
            State.Revision++;
            return new PropertyAttackResult(false, lostInvestment);
        }

        public PropertyAttackResult DestroyCityForCard(int actorId, int tileId)
        {
            EnsureRunning();
            EnsureCurrentPlayer(actorId);
            if (State.Phase != TurnPhase.ResolvingLanding) throw new InvalidOperationException("Thẻ chỉ được xử lý trong chuỗi ô hiện tại.");
            PropertyState property = State.GetProperty(tileId);
            if (BoardCatalog.Get(tileId).Kind != TileKind.City || !property.OwnerId.HasValue || !property.Level.HasValue ||
                property.OwnerId == actorId || property.Level == PropertyLevel.Hotel)
                throw new InvalidOperationException("Thẻ Phá tài sản chỉ tác động TP Cọc–Nhà3.");
            PlayerState owner = State.GetPlayer(property.OwnerId.Value);
            if (owner.HasHeldCard(5))
            {
                owner.ConsumeHeldCard(5);
                State.Revision++;
                return new PropertyAttackResult(true, 0);
            }
            long refund = property.TotalInvestment;
            EnsureOwnershipChangeAllowed(tileId);
            owner.Cash = checked(owner.Cash + refund);
            OnPropertyOwnershipChanging(tileId);
            ResetProperty(property);
            RemovePowerOutageForTile(tileId);
            State.Revision++;
            return new PropertyAttackResult(false, refund);
        }

        public bool StoreHeldCard(int playerId, int cardId)
        {
            EnsureRunning();
            EnsureCurrentPlayer(playerId);
            if (State.Phase != TurnPhase.ResolvingLanding)
                throw new InvalidOperationException("Thẻ chỉ được thêm vào tay trong chuỗi ô đang xử lý.");
            if (cardId != 3 && cardId != 4 && cardId != 5 && cardId != 8)
                throw new ArgumentOutOfRangeException(nameof(cardId), "Chỉ bốn loại thẻ quy định được giữ.");
            PlayerState player = GetActivePlayer(playerId);
            bool stored = player.AddHeldCard(cardId);
            if (stored) State.Revision++;
            return stored;
        }

        public long UpgradeCity(int playerId, int tileId, long landingId, int priceModifierBasisPoints = 0)
        {
            PropertyState property = State.GetProperty(tileId);
            if (!property.Level.HasValue) throw new InvalidOperationException("Thành phố chưa có công trình.");
            return UpgradeCity(playerId, tileId, (PropertyLevel)((int)property.Level.Value + 1),
                landingId, priceModifierBasisPoints);
        }

        public long UpgradeCity(int playerId, int tileId, PropertyLevel targetLevel,
            long landingId, int priceModifierBasisPoints = 0)
        {
            EnsureRunning();
            PlayerState owner = GetActivePlayer(playerId);
            EnsureCurrentPlayer(playerId);
            EnsureStandingOn(playerId, tileId);
            EnsureLandingDecisionAvailable();
            PropertyState property = State.GetProperty(tileId);
            if (BoardCatalog.Get(tileId).Kind != TileKind.City || property.OwnerId != playerId || !property.Level.HasValue)
                throw new InvalidOperationException("Người chơi không sở hữu thành phố hợp lệ.");
            if (landingId != State.CurrentLandingId || property.NormalUpgradeUsedOnLandingId == landingId)
                throw new InvalidOperationException("Chỉ được một lần nâng cấp thường trong mỗi lần dừng.");
            if (priceModifierBasisPoints <= -10_000 || priceModifierBasisPoints > 100_000)
                throw new ArgumentOutOfRangeException(nameof(priceModifierBasisPoints));

            PropertyLevel current = property.Level.Value;
            if (current == PropertyLevel.Hotel) throw new InvalidOperationException("Khách sạn không thể nâng thêm.");
            if (!Enum.IsDefined(typeof(PropertyLevel), targetLevel) || targetLevel <= current)
                throw new ArgumentOutOfRangeException(nameof(targetLevel));
            if (targetLevel == PropertyLevel.Hotel &&
                (current != PropertyLevel.House3 || property.House3ReachedLandingId == landingId))
                throw new InvalidOperationException("Khách sạn chỉ được xây ở lần dừng sau khi đạt Nhà 3.");
            long difference = EconomyRules.ListedPrice(tileId, targetLevel) - EconomyRules.ListedPrice(tileId, current);
            priceModifierBasisPoints -= SkillRules.NormalUpgradeDiscountBasisPoints(owner.Role);
            long cost = EconomyRules.RentToPay(difference, -priceModifierBasisPoints);
            if (owner.Cash < cost) throw new InvalidOperationException("Không đủ tiền nâng cấp.");
            var newInvestment = EconomyRules.AllocateUpgradeInvestment(cost, current, targetLevel, State.Decisions.InvestmentTieBreak);

            owner.Cash -= cost;
            for (int i = (int)current; i < (int)targetLevel; i++)
                property.MutableInvestedByLevel[i] = newInvestment[i];
            property.Level = targetLevel;
            property.NormalUpgradeUsedOnLandingId = landingId;
            State.CurrentLandingDecisionUsed = true;
            if (targetLevel == PropertyLevel.House3) property.House3ReachedLandingId = landingId;
            if (targetLevel == PropertyLevel.Hotel) RemovePowerOutageForTile(tileId);
            State.Revision++;
            return cost;
        }

        public long SellToBank(int playerId, int tileId)
        {
            EnsureRunning();
            PlayerState seller = GetActivePlayer(playerId);
            EnsureCurrentPlayer(playerId);
            if (State.TimeExpired) throw new InvalidOperationException("Không mở bán tự nguyện mới sau khi hết giờ.");
            if ((State.ClockPaused && !State.DecisionAllowsSkills) || (State.Phase != TurnPhase.AwaitingRoll && State.Phase != TurnPhase.EndTurn &&
                State.Phase != TurnPhase.AwaitingJailChoice))
                throw new InvalidOperationException("Bán tự nguyện chỉ hợp lệ ở điểm dừng trước gieo hoặc sau khi xử lý ô.");
            PropertyState property = State.GetProperty(tileId);
            if (property.OwnerId != playerId) throw new InvalidOperationException("Người chơi không sở hữu tài sản này.");
            EnsureOwnershipChangeAllowed(tileId);
            long refund = EconomyRules.BankSaleValue(property.TotalInvestment);

            seller.Cash = checked(seller.Cash + refund);
            OnPropertyOwnershipChanging(tileId);
            ResetProperty(property);
            RemovePowerOutageForTile(tileId);
            State.Revision++;
            return refund;
        }

        private void CheckImmediateWin(int playerId)
        {
            int beaches = 0;
            var countries = new System.Collections.Generic.HashSet<CountryId>();
            foreach (PropertyState property in State.Properties)
            {
                if (property.OwnerId != playerId) continue;
                TileDefinition tile = BoardCatalog.Get(property.TileId);
                if (tile.Kind == TileKind.Beach) beaches++;
            }
            if (beaches == 4) { Finish(WinReason.FourBeaches, new[] { playerId }); return; }
            foreach (CountryId country in Enum.GetValues(typeof(CountryId)))
            {
                bool hasCities = false;
                bool ownsAll = true;
                foreach (TileDefinition tile in BoardCatalog.Tiles)
                {
                    if (tile.Kind != TileKind.City || tile.Country != country) continue;
                    hasCities = true;
                    if (State.GetProperty(tile.Id).OwnerId != playerId) ownsAll = false;
                }
                if (hasCities && ownsAll) countries.Add(country);
            }
            if (countries.Count >= 3) Finish(WinReason.ThreeCountries, new[] { playerId });
        }

        private bool OwnsAllCitiesOfCountry(int playerId, CountryId country)
        {
            bool found = false;
            foreach (TileDefinition tile in BoardCatalog.Tiles)
            {
                if (tile.Kind != TileKind.City || tile.Country != country) continue;
                found = true;
                if (State.GetProperty(tile.Id).OwnerId != playerId) return false;
            }
            return found;
        }

        private void RemovePowerOutageForTile(int tileId)
        {
            for (int i = State.ActiveEffects.Count - 1; i >= 0; i--)
            {
                TurnEffectState effect = State.ActiveEffects[i];
                if (effect.Kind == EffectKind.PowerOutage && effect.TargetTileId == tileId)
                    State.RemoveEffect(effect);
            }
        }

        private void Finish(WinReason reason, System.Collections.Generic.IReadOnlyList<int> winnerIds)
        {
            State.SetWinners(winnerIds);
            State.Status = MatchStatus.Finished;
            State.Phase = TurnPhase.Finished;
            State.Result = reason;
            State.Revision++;
        }

        private PlayerState GetActivePlayer(int playerId)
        {
            PlayerState player = State.GetPlayer(playerId);
            if (player.Status != PlayerStatus.Active) throw new InvalidOperationException("Người chơi đã phá sản.");
            return player;
        }

        private static void ResetProperty(PropertyState property)
        {
            property.OwnerId = null;
            property.Level = null;
            Array.Clear(property.MutableInvestedByLevel, 0, property.MutableInvestedByLevel.Length);
            property.BeachInvestment = 0;
            property.House3ReachedLandingId = null;
            property.NormalUpgradeUsedOnLandingId = null;
            property.AcquiredOnLandingId = null;
        }

        private static void ValidateDiscount(int discountBasisPoints)
        {
            if (discountBasisPoints < -10_000 || discountBasisPoints >= 10_000)
                throw new ArgumentOutOfRangeException(nameof(discountBasisPoints));
        }

        private void EnsureCurrentPlayer(int playerId)
        {
            if (State.CurrentPlayerId != playerId) throw new InvalidOperationException("Chưa đến lượt người chơi.");
        }

        private void EnsureLandingDecisionAvailable()
        {
            if (State.Phase != TurnPhase.ResolvingLanding || State.CurrentLandingDecisionUsed)
                throw new InvalidOperationException("Không còn lựa chọn mua/nâng hợp lệ trong lần dừng này.");
        }

        private void EnsureMandatoryLandingResolved()
        {
            PlayerState actor = State.GetPlayer(State.CurrentPlayerId);
            TileDefinition tile = BoardCatalog.Get(actor.Position);
            if (tile.Kind == TileKind.Tax && !State.TaxSettledAtCurrentLanding)
                throw new InvalidOperationException("Chưa xử lý thuế bắt buộc của ô30.");
            if (tile.Kind == TileKind.City || tile.Kind == TileKind.Beach)
            {
                PropertyState property = State.GetProperty(tile.Id);
                if (property.OwnerId.HasValue && property.OwnerId != actor.Id &&
                    State.RentSettledAtCurrentLandingTileId != tile.Id)
                    throw new InvalidOperationException("Chưa giải quyết tiền thuê bắt buộc của lần dừng.");
            }
        }

        private bool HasExistingPaymentImmunity(int payerId)
        {
            if (State.CurrentPlayerId != payerId) return false;
            foreach (TurnEffectState effect in State.ActiveEffects)
                if (effect.Kind == EffectKind.PaymentImmunity && effect.SourcePlayerId == payerId &&
                    (!effect.TargetPlayerId.HasValue || effect.TargetPlayerId == payerId)) return true;
            return false;
        }

        private void ApplyExistingPaymentEffects(MandatoryObligation obligation)
        {
            bool exemptible = obligation.Kind == ObligationKind.Rent || obligation.Kind == ObligationKind.TaxTile ||
                obligation.Kind == ObligationKind.TaxCard || obligation.Kind == ObligationKind.Gift ||
                obligation.Kind == ObligationKind.OtherMandatory;
            if (exemptible && HasExistingPaymentImmunity(obligation.PayerId))
            {
                obligation.PayableAmount = 0;
                return;
            }
            if (obligation.Kind != ObligationKind.Rent || obligation.PayableAmount == 0) return;
            foreach (TurnEffectState effect in State.ActiveEffects)
                if (effect.Kind == EffectKind.CancelRent && effect.SourcePlayerId == obligation.PayerId &&
                    (!effect.TargetPlayerId.HasValue || effect.TargetPlayerId == obligation.PayerId))
                {
                    obligation.PayableAmount = 0;
                    State.RemoveEffect(effect);
                    return;
                }
        }

        private void EnsureActiveActionWindow(int playerId, bool allowRentReaction = false)
        {
            bool regularWindow = State.CurrentPlayerId == playerId &&
                (State.Phase == TurnPhase.AwaitingRoll || State.Phase == TurnPhase.EndTurn ||
                 State.Phase == TurnPhase.AwaitingJailChoice);
            bool rentReaction = allowRentReaction && State.Phase == TurnPhase.AwaitingObligation &&
                State.PendingObligation != null && State.PendingObligation.Kind == ObligationKind.Rent &&
                State.PendingObligation.PayerId == playerId;
            if (State.TimeExpired && !rentReaction)
                throw new InvalidOperationException("Không mở kỹ năng chủ động mới sau khi hết giờ.");
            if (regularWindow && State.ClockPaused && !State.DecisionAllowsSkills)
                throw new InvalidOperationException("Còn quyết định đang chờ, chưa thể mở kỹ năng mới.");
            if (!regularWindow && !rentReaction)
                throw new InvalidOperationException("Kỹ năng chưa ở cửa sổ hành động hợp lệ.");
        }

        private void EnsureEffectActionWindow(int playerId)
        {
            EnsureCurrentPlayer(playerId);
            bool allowedPhase = State.Phase == TurnPhase.AwaitingRoll || State.Phase == TurnPhase.ResolvingLanding ||
                State.Phase == TurnPhase.EndTurn || State.Phase == TurnPhase.AwaitingJailChoice;
            if (!allowedPhase || (State.TimeExpired && State.Phase != TurnPhase.ResolvingLanding))
                throw new InvalidOperationException("Hiệu ứng không được kích hoạt ngoài cửa sổ hợp lệ.");
        }

        private static void CommitActiveCooldown(PlayerState player, int cooldownTurns, bool outsideOwnTurn = false)
        {
            player.ActiveReadyAfterCompletedTurns = checked(player.CompletedPersonalTurns + cooldownTurns + (outsideOwnTurn ? 0L : 1L));
        }

        private void EnsureStandingOn(int playerId, int tileId)
        {
            if (State.GetPlayer(playerId).Position != tileId)
                throw new InvalidOperationException("Hành động này chỉ hợp lệ khi dừng tại ô tài sản.");
        }

        private void EnsureRunning()
        {
            if (State.Status != MatchStatus.Running) throw new InvalidOperationException("Trận đã kết thúc.");
        }

        private void EnsureCanRoll(int playerId)
        {
            EnsureRunning();
            EnsureCurrentPlayer(playerId);
            if (State.Phase != TurnPhase.AwaitingRoll)
                throw new InvalidOperationException("Chưa có quyền gieo xúc xắc.");
            if (State.TimeExpired)
                throw new InvalidOperationException("Đã hết giờ, không thể bắt đầu lần gieo mới.");
            PlayerState player = GetActivePlayer(playerId);
            if (player.IsJailed) throw new InvalidOperationException("Người chơi đang ở tù.");
        }

        private PlayerState RequireJailTurn(int playerId)
        {
            EnsureRunning();
            EnsureCurrentPlayer(playerId);
            PlayerState player = GetActivePlayer(playerId);
            if (!player.IsJailed) throw new InvalidOperationException("Người chơi không ở tù.");
            return player;
        }

        private void ResumeAfterObligation(MandatoryObligation obligation)
        {
            if (State.GiftBatchActive)
            {
                RefreshGiftBatchAfterPayment();
                return;
            }
            State.ClockPaused = State.ClockPausedBeforeObligation;
            if (obligation.Kind == ObligationKind.ForcedJailReleaseFee)
            {
                PlayerState player = State.GetPlayer(obligation.PayerId);
                player.IsJailed = false;
                player.JailTurnsStarted = 0;
                player.JailFeeDue = 0;
                State.Phase = TurnPhase.EndTurn;
            }
            else State.Phase = State.PhaseBeforeObligation;
        }

        private void TransferCash(PlayerState payer, int? recipientId, long amount)
        {
            if (!recipientId.HasValue)
            {
                payer.Cash -= amount;
                return;
            }
            if (recipientId.Value == payer.Id)
            {
                return;
            }
            PlayerState recipient = State.GetPlayer(recipientId.Value);
            long newRecipientCash = checked(recipient.Cash + amount);
            payer.Cash -= amount;
            recipient.Cash = newRecipientCash;
        }

        private void CheckLastSolventPlayer()
        {
            if (State.InitialPlayerCount < 2) return;
            PlayerState survivor = null;
            foreach (PlayerState player in State.Players)
            {
                if (player.Status != PlayerStatus.Active) continue;
                if (survivor != null) return;
                survivor = player;
            }
            if (survivor != null) Finish(WinReason.LastSolventPlayer, new[] { survivor.Id });
        }

        private PlayerState FindNextActivePlayer(int afterSeatIndex)
        {
            PlayerState next = null;
            int shortestDistance = int.MaxValue;
            foreach (PlayerState player in State.Players)
            {
                if (player.Status != PlayerStatus.Active || player.SeatIndex == afterSeatIndex) continue;
                int distance = (player.SeatIndex - afterSeatIndex + 4) % 4;
                if (distance == 0) distance = 4;
                if (distance < shortestDistance) { next = player; shortestDistance = distance; }
            }
            return next;
        }

        private bool HasProperty(int playerId)
        {
            foreach (PropertyState property in State.Properties)
                if (property.OwnerId == playerId) return true;
            return false;
        }

        private void FinishByTime()
        {
            long bestCash = long.MinValue;
            long bestPropertyValue = long.MinValue;
            int cashLeaders = 0;
            var winners = new System.Collections.Generic.List<int>();
            foreach (PlayerState player in State.Players)
            {
                if (player.Status != PlayerStatus.Active) continue;
                long value = GetListedPropertyValue(player.Id);
                if (player.Cash > bestCash)
                {
                    bestCash = player.Cash;
                    bestPropertyValue = value;
                    cashLeaders = 1;
                    winners.Clear();
                    winners.Add(player.Id);
                }
                else if (player.Cash == bestCash)
                {
                    cashLeaders++;
                    if (value > bestPropertyValue) { bestPropertyValue = value; winners.Clear(); winners.Add(player.Id); }
                    else if (value == bestPropertyValue) winners.Add(player.Id);
                }
            }
            if (winners.Count == 0) return;
            Finish(winners.Count > 1 ? WinReason.TimeShared : cashLeaders > 1 ? WinReason.TimePropertyValue : WinReason.TimeCash, winners);
        }

        private long GetListedPropertyValue(int playerId)
        {
            long total = 0;
            foreach (PropertyState property in State.Properties)
                if (property.OwnerId == playerId) total = checked(total + EconomyRules.ListedPrice(property));
            return total;
        }

        private void ApplyBankerInterest()
        {
            var payments = new System.Collections.Generic.List<(PlayerState player, long resultingCash)>();
            foreach (PlayerState player in State.Players)
            {
                if (player.Status != PlayerStatus.Active || !player.HasBankerPassive || player.Cash <= 0) continue;
                long interest = new MoneyFraction(checked(player.Cash), 100).FloorToLong();
                payments.Add((player, checked(player.Cash + interest)));
            }
            foreach (var payment in payments) payment.player.Cash = payment.resultingCash;
        }

        private void ExpireAfterSourceTurn(PlayerState source)
        {
            var expired = new System.Collections.Generic.List<TurnEffectState>();
            foreach (TurnEffectState effect in State.ActiveEffects)
                if (effect.SourcePlayerId == source.Id && effect.ExpiresAfterSourceTurnOrdinal.HasValue &&
                    source.CompletedPersonalTurns >= effect.ExpiresAfterSourceTurnOrdinal.Value)
                    expired.Add(effect);
            foreach (TurnEffectState effect in expired) State.RemoveEffect(effect);
        }

        private void ExpireBeforeNextSourceTurn(PlayerState nextPlayer)
        {
            long nextOwnTurnOrdinal = nextPlayer.CompletedPersonalTurns + 1;
            var expired = new System.Collections.Generic.List<TurnEffectState>();
            foreach (TurnEffectState effect in State.ActiveEffects)
                if (effect.SourcePlayerId == nextPlayer.Id && effect.ExpiresBeforeSourceTurnOrdinal.HasValue &&
                    nextOwnTurnOrdinal >= effect.ExpiresBeforeSourceTurnOrdinal.Value)
                    expired.Add(effect);
            foreach (TurnEffectState effect in expired) State.RemoveEffect(effect);
        }
    }
}
