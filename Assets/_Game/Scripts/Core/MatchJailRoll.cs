using System;
using BtlGame.Board;

namespace BtlGame.Core
{
    public sealed partial class MatchCoordinator
    {
        private void BeginJailTrial(int playerId)
        {
            EnsureRunning();
            PlayerState player = RequireJailTurn(playerId);
            if (State.Phase != TurnPhase.AwaitingJailChoice || player.Role != RoleId.Strategist ||
                player.JailTurnsStarted > 3)
                throw new InvalidOperationException("Chưa thể mở lần thử xúc xắc trong tù của Nhà Chiến Lược.");
            DiceRoll roll = _diceSource.Roll();
            State.PendingDiceRoll = new DiceRoll(roll.First, roll.Second);
            State.PendingJailTrial = true;
            State.Phase = TurnPhase.AwaitingRollConfirmation;
        }

        private MovementResult ConfirmJailTrial(int playerId)
        {
            EnsureRunning();
            PlayerState player = RequireJailTurn(playerId);
            if (State.Phase != TurnPhase.AwaitingRollConfirmation || !State.PendingJailTrial ||
                !State.PendingDiceRoll.HasValue)
                throw new InvalidOperationException("Không có lần thử trong tù đang chờ xác nhận.");
            DiceRoll roll = State.PendingDiceRoll.Value;
            int from = player.Position;
            State.PendingDiceRoll = null;
            State.PendingJailTrial = false;
            State.Phase = TurnPhase.AwaitingJailChoice;
            ResolveJailRoll(playerId, roll, HasExistingPaymentImmunity(playerId));
            if (!roll.IsDouble) return null;
            MovementResult movement = BoardRules.CalculateMovement(from, roll);
            QueuePresentation(movement);
            return movement;
        }

        private void ResolveJailRoll(int playerId, DiceRoll roll, bool lawyerUltimateCouldMitigateForcedFee)
        {
            EnsureRunning();
            PlayerState player = RequireJailTurn(playerId);
            if (State.Phase != TurnPhase.AwaitingJailChoice || player.JailTurnsStarted > 3)
                throw new InvalidOperationException("Không ở lần thử hợp lệ trong tù.");
            State.LastDiceRoll = roll;
            State.LastDicePlayerId = playerId;
            if (roll.IsDouble)
            {
                player.IsJailed = false;
                player.JailTurnsStarted = 0;
                State.Phase = TurnPhase.AwaitingRoll;
                // Kết quả đã được gieo trước bước xác nhận; không mở lần gieo mới tại đây.
                MoveByDice(playerId, roll, alreadyCommitted: true);
                return;
            }
            State.PendingDoubleRoll = false;
            State.PersonalTurnMustClose = true;
            if (player.JailTurnsStarted < 3)
            {
                State.Phase = TurnPhase.EndTurn;
                State.Revision++;
                return;
            }
            bool hasImmunity = HasExistingPaymentImmunity(playerId);
            if (hasImmunity || (lawyerUltimateCouldMitigateForcedFee && !State.Decisions.LawyerWaivesForcedJailFee.HasValue))
            {
                if (hasImmunity && State.Decisions.LawyerWaivesForcedJailFee == true)
                {
                    player.IsJailed = false;
                    player.JailTurnsStarted = 0;
                    State.Phase = TurnPhase.EndTurn;
                    State.Revision++;
                    return;
                }
                if (!State.Decisions.LawyerWaivesForcedJailFee.HasValue)
                {
                    State.PendingRuleDecisionId = "P02";
                    State.Phase = TurnPhase.AwaitingRuleDecision;
                    State.Revision++;
                    return;
                }
            }
            State.PendingObligation = CreateObligation(ObligationKind.ForcedJailReleaseFee, playerId, null, 200000);
            player.JailFeeDue = 200000;
            State.Phase = TurnPhase.AwaitingObligation;
            State.Revision++;
        }
    }
}
