using System;
using System.Collections.Generic;

namespace BtlGame.Core
{
    public sealed partial class MatchCoordinator
    {
        private void BeginOpponentGiftBatch()
        {
            if (State.GiftBatchActive || State.PendingObligation != null || State.PendingCardId != 10)
                throw new InvalidOperationException("Không thể mở trùng nghĩa vụ Quà từ đối thủ.");
            int recipientId = State.CurrentPlayerId;
            PlayerState recipient = State.GetPlayer(recipientId);
            var batch = new List<MandatoryObligation>();
            long immediateCash = recipient.Cash;
            foreach (PlayerState payer in State.Players)
            {
                if (payer.Id == recipientId || payer.Status != PlayerStatus.Active) continue;
                var obligation = new MandatoryObligation(_nextObligationId + batch.Count,
                    ObligationKind.Gift, payer.Id, recipientId, 100000, null);
                ApplyExistingPaymentEffects(obligation);
                if (payer.Cash >= obligation.PayableAmount) immediateCash = checked(immediateCash + obligation.PayableAmount);
                batch.Add(obligation);
            }
            _nextObligationId += batch.Count;
            State.PhaseBeforeObligation = TurnPhase.ResolvingLanding;
            State.ClockPausedBeforeObligation = true;
            State.GiftBatchActive = true;
            foreach (MandatoryObligation obligation in batch)
            {
                State.AddObligation(obligation);
                State.MarkCardParticipant(obligation.PayerId);
                PlayerState payer = State.GetPlayer(obligation.PayerId);
                if (payer.Cash < obligation.PayableAmount) continue;
                TransferCash(payer, recipientId, obligation.PayableAmount);
                obligation.Status = ObligationStatus.Settled;
            }
            RefreshGiftBatchAfterPayment();
            State.Revision++;
        }

        private void RefreshGiftBatchAfterPayment()
        {
            if (!State.GiftBatchActive) return;
            State.PendingObligation = null;
            foreach (MandatoryObligation obligation in State.PendingObligations)
            {
                State.PendingObligation = obligation;
                break;
            }
            if (State.PendingObligation != null)
            {
                State.Phase = TurnPhase.AwaitingObligation;
                State.ClockPaused = true;
                return;
            }
            State.GiftBatchActive = false;
            State.CardLandingResolved = true;
            State.PendingCardId = null;
            State.PendingRuleDecisionId = null;
            State.Phase = TurnPhase.ResolvingLanding;
            State.ClockPaused = true;
        }
    }
}
