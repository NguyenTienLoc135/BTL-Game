using System;
using BtlGame.Board;

namespace BtlGame.Core
{
    public sealed partial class MatchCoordinator
    {
        private MovementResult MoveByDiceAllowCommittedRoll(int playerId, DiceRoll roll) => MoveByDice(playerId, roll, true);

        private MovementResult MoveWithoutDice(int playerId, int steps, bool preserveDouble)
        {
            PlayerState player = GetActivePlayer(playerId);
            MovementResult movement = BoardRules.CalculateMovement(player.Position, steps, source: MovementSource.StrategistUltimate);
            MovementResult applied = ApplyMovement(player, movement, preserveDouble);
            State.LastDiceRoll = null;
            State.LastDicePlayerId = null;
            return applied;
        }

        private void QueuePresentation(MovementResult movement)
        {
            State.PendingMovement = movement;
            State.PhaseAfterPresentation = State.Phase;
            State.Phase = TurnPhase.Moving;
            State.ClockPaused = false;
            State.DecisionAllowsSkills = false;
        }

        /// <summary>B gọi một lần sau animation; callback cũ không được xử lý lại ô hoặc tiền.</summary>
        public ActionResult CompletePresentation(long actionId)
        {
            if (State.Status != MatchStatus.Running || State.Phase != TurnPhase.Moving ||
                State.CurrentActionId != actionId || State.PendingMovement == null)
                return Rejected("Callback di chuyển đã cũ hoặc đã được xử lý.");
            State.PendingMovement = null;
            State.Phase = State.PhaseAfterPresentation;
            BeginLandingResolution();
            AutoProgressCompletedDecision();
            State.DecisionId++;
            State.Revision++;
            RefreshDecisionPause();
            return new ActionResult(State.PendingObligation != null || State.PendingRuleDecisionId != null
                ? ActionStatus.Pending : ActionStatus.Accepted, "Đã hoàn tất presentation.", QuerySnapshot());
        }

        private void BeginLandingResolution()
        {
            if (State.Phase != TurnPhase.ResolvingLanding || State.LandingResolutionStarted) return;
            State.LandingResolutionStarted = true;
            PlayerState player = State.GetPlayer(State.CurrentPlayerId);
            TileDefinition tile = BoardCatalog.Get(player.Position);
            if (tile.Kind == TileKind.Tax) AssessTax(player.Id, ObligationKind.TaxTile);
            else if (tile.Kind == TileKind.City || tile.Kind == TileKind.Beach)
            {
                PropertyState property = State.GetProperty(tile.Id);
                if (property.OwnerId.HasValue && property.OwnerId != player.Id)
                    CreateObligation(ObligationKind.Rent, player.Id, property.OwnerId, QuoteRent(tile.Id).FinalRent, tile.Id);
            }
            else if (tile.Kind == TileKind.EventCard)
            {
                State.ResetCardParticipants();
                State.PendingCardId = _cardProvider?.PeekNextCard(QuerySnapshot());
                if (!State.PendingCardId.HasValue) State.PendingRuleDecisionId = "C_CARD_MODULE/Q02";
                else if (State.PendingCardId < 1 || State.PendingCardId > 14)
                {
                    State.PendingCardId = null;
                    State.PendingRuleDecisionId = "C_INVALID_CARD_ID";
                }
                else State.CardDrawOrdinal++;
                if (State.PendingCardId == 10) BeginOpponentGiftBatch();
            }
        }

        public int? QueryNextCard(int playerId, ICardPreviewSource source)
        {
            PlayerState player = State.GetPlayer(playerId);
            if (player.Status != PlayerStatus.Active || player.Role != RoleId.Strategist) return null;
            return source?.PeekNextCard(QuerySnapshot());
        }

        public int? QueryNextCard(int playerId) => QueryNextCard(playerId, _cardProvider);

        private MovementResult TeleportFromCurrentLanding(int playerId, int targetTileId)
        {
            EnsureRunning();
            EnsureCurrentPlayer(playerId);
            PlayerState player = GetActivePlayer(playerId);
            if (State.Phase != TurnPhase.ResolvingLanding || player.Position != 24 ||
                (State.TeleportUsedInCurrentChain && State.Decisions.TeleportOncePerChain != false) || targetTileId == 24 || player.Cash < TeleportFee)
                throw new InvalidOperationException("Không thể sử dụng dịch vụ dịch chuyển lúc này.");
            int steps = BoardRules.CountForwardPath(24, targetTileId);
            MovementResult movement = BoardRules.CalculateMovement(24, steps, source: MovementSource.Teleport);
            MovementResult applied = ApplyMovement(player, movement, preserveDouble: true, movementFee: TeleportFee);
            State.TeleportUsedInCurrentChain = true;
            return applied;
        }

        private void OrganizeFestival(int playerId, int targetTileId, bool fromCard = false)
        {
            PlayerState player = GetActivePlayer(playerId);
            PropertyState property = State.GetProperty(targetTileId);
            if (State.Phase != TurnPhase.ResolvingLanding || (!fromCard && player.Position != 16) ||
                BoardCatalog.Get(targetTileId).Kind != TileKind.City || property.OwnerId != playerId || player.Cash < 200000)
                throw new InvalidOperationException("Không đủ điều kiện tổ chức lễ hội.");
            int bonus = 5000;
            foreach (TurnEffectState effect in State.ActiveEffects)
                if (effect.Kind == EffectKind.TemporaryFestival && effect.TargetTileId == targetTileId &&
                    effect.SourcePlayerId == playerId) bonus = checked(effect.ModifierBasisPoints + 5000);
            // Một lễ hội tổ chức trên toàn bàn; đổi TP/chủ bắt đầu lại +50%, cùng TP/chủ thì cộng dồn.
            for (int i = State.ActiveEffects.Count - 1; i >= 0; i--)
                if (State.ActiveEffects[i].Kind == EffectKind.TemporaryFestival) State.RemoveEffect(State.ActiveEffects[i]);
            State.AddEffect(new TurnEffectState(_nextEffectId++, EffectKind.TemporaryFestival, playerId,
                playerId, targetTileId, bonus, null, null, player.CompletedPersonalTurns + 1));
            player.Cash -= 200000;
            State.CurrentLandingDecisionUsed = true;
        }

    }
}
