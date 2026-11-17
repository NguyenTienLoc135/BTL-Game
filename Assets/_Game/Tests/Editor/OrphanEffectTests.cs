using System;
using BtlGame.Board;
using BtlGame.Core;
using NUnit.Framework;

namespace BtlGame.Tests
{
    public sealed class OrphanEffectTests
    {
        [Test]
        public void OutageWithTwoTurnsRemainingSurvivesCurrentRoundAndExpiresAfterTwoNewRounds()
        {
            var core = NewCore();
            core.State.CompletedRounds = 4;
            core.State.GetPlayer(1).CompletedPersonalTurns = 2;
            TurnEffectState outage = AddEffect(core, EffectKind.PowerOutage, 4, null, 1, 1);
            core.State.CurrentPlayerId = 2;
            BankruptSource(core);
            Assert.That(outage.ExpiresAfterRoundOrdinal, Is.EqualTo(7));
            Assert.That(core.QuoteCityRent(1).FinalRent, Is.Zero);
            FinishOneRound(core);
            Assert.That(core.State.CompletedRounds, Is.EqualTo(5));
            Assert.That(core.State.ActiveEffects.Count, Is.EqualTo(1));
            FinishOneRound(core);
            Assert.That(core.State.CompletedRounds, Is.EqualTo(6));
            Assert.That(core.QuoteCityRent(1).FinalRent, Is.Zero);
            FinishOneRound(core);
            Assert.That(core.State.CompletedRounds, Is.EqualTo(7));
            Assert.That(core.State.ActiveEffects.Count, Is.Zero);
            Assert.That(core.QuoteCityRent(1).FinalRent, Is.EqualTo(30000));
        }

        [Test]
        public void DyingInActivationTurnDoesNotAddAnExtraFullRoundToTheOriginalDuration()
        {
            var core = NewCore();
            TurnEffectState outage = AddEffect(core, EffectKind.PowerOutage, 4, null, 1, 1);
            BankruptSource(core);
            Assert.That(outage.ExpiresAfterRoundOrdinal, Is.EqualTo(4), "Ba lượt tiếp theo đổi thành ba vòng mới sau vòng đang dở.");
            FinishOneRound(core);
            FinishOneRound(core);
            FinishOneRound(core);
            Assert.That(core.State.ActiveEffects.Count, Is.EqualTo(1));
            FinishOneRound(core);
            Assert.That(core.State.ActiveEffects.Count, Is.Zero);
        }

        [Test]
        public void DyingDuringALaterOwnTurnDoesNotSubtractThatTurnAsAnActivationTurn()
        {
            var core = NewCore();
            core.State.GetPlayer(1).CompletedPersonalTurns = 2;
            TurnEffectState outage = AddEffect(core, EffectKind.PowerOutage, 4, null, 1, 1);
            BankruptSource(core);
            Assert.That(outage.ExpiresAfterRoundOrdinal, Is.EqualTo(3));
        }

        [Test]
        public void ExtraDoubleRollsDoNotConsumeOrphanRoundDuration()
        {
            var core = NewCore(dice: new DoubleDice());
            core.State.GetPlayer(1).CompletedPersonalTurns = 2;
            TurnEffectState outage = AddEffect(core, EffectKind.PowerOutage, 4, null, 1, 1);
            core.State.CurrentPlayerId = 2;
            BankruptSource(core);
            for (int i = 0; i < 2; i++)
            {
                core.RollAndMove(2);
                core.CompleteLanding(core.State.CurrentActionId);
            }
            Assert.That(core.State.CompletedRounds, Is.Zero);
            Assert.That(outage.ExpiresAfterRoundOrdinal, Is.EqualTo(3));
            Assert.That(core.State.ActiveEffects.Count, Is.EqualTo(1));
            Assert.That(core.State.GetPlayer(2).CompletedPersonalTurns, Is.Zero);
        }

        [Test]
        public void NonExpiringCouponAndFestivalAreKeptWithoutInventingADeadline()
        {
            var core = NewCore();
            TurnEffectState coupon = AddEffect(core, EffectKind.PurchaseDiscount, null, null, 1, null);
            TurnEffectState festival = AddEffect(core, EffectKind.TemporaryFestival, null, null, 1, 1);
            BankruptSource(core);
            Assert.That(coupon.ExpiresAfterRoundOrdinal, Is.Null);
            Assert.That(festival.ExpiresAfterRoundOrdinal, Is.Null);
            for (int i = 0; i < 5; i++) FinishOneRound(core);
            Assert.That(core.State.ActiveEffects.Count, Is.EqualTo(2));
            Assert.That(core.QuoteCityRent(1).FinalRent, Is.EqualTo(45000));
        }

        [Test]
        public void SourceDeathDoesNotRebaseAnotherPlayersTimedEffects()
        {
            var core = NewCore();
            TurnEffectState own = AddEffect(core, EffectKind.PowerOutage, 4, null, 1, 1);
            var other = new TurnEffectState(99, EffectKind.RentBonus, 3, 3, null, 3000, 4, null,
                activatedSourceTurnOrdinal: 1);
            core.State.AddEffect(other);
            BankruptSource(core);
            Assert.That(own.ExpiresAfterRoundOrdinal, Is.Not.Null);
            Assert.That(other.ExpiresAfterRoundOrdinal, Is.Null);
            Assert.That(other.ExpiresAfterSourceTurnOrdinal, Is.EqualTo(4));
        }

        [Test]
        public void RentDoubleRetainsOneFullNewRoundAfterItsSourceIsEliminated()
        {
            var core = NewCore();
            TurnEffectState effect = AddEffect(core, EffectKind.RentDouble, null, 2, 1, null);
            BankruptSource(core);
            Assert.That(effect.ExpiresAfterRoundOrdinal, Is.EqualTo(2));
            FinishOneRound(core);
            Assert.That(core.State.ActiveEffects.Count, Is.EqualTo(1));
            FinishOneRound(core);
            Assert.That(core.State.ActiveEffects.Count, Is.Zero);
        }

        [Test]
        public void KeepPolicyWithoutAnApprovedReplacementClockStillBlocksSourceBankruptcy()
        {
            var core = NewCore(new GameplayDecisions(removeEffectsWhenSourceBankrupt: false));
            AddEffect(core, EffectKind.PowerOutage, 4, null, 1, 1);
            core.CreateObligation(ObligationKind.Gift, 1, 2, 100000);
            Assert.Throws<InvalidOperationException>(() => core.DeclareBankruptcy(1));
            Assert.That(core.State.GetPlayer(1).Status, Is.EqualTo(PlayerStatus.Active));
            Assert.That(core.State.PendingObligation.PayableAmount, Is.EqualTo(100000));
        }

        private static MatchCoordinator NewCore(GameplayDecisions decisions = null, IDiceSource dice = null)
        {
            var state = new GameState(new[] { new PlayerState(1, 0, "Nguồn"),
                new PlayerState(2, 1, "B"), new PlayerState(3, 2, "C") }, 1, new[] { 27, 29, 31 },
                decisions: decisions ?? new GameplayDecisions(removeEffectsWhenSourceBankrupt: false,
                    removeFestivalOnOwnershipChange: true, keepOrphanEffectsThroughCurrentRound: true));
            var property = state.GetProperty(1);
            property.OwnerId = 2;
            property.Level = PropertyLevel.House3;
            state.GetPlayer(1).Cash = 0;
            return new MatchCoordinator(state, dice);
        }

        private static TurnEffectState AddEffect(MatchCoordinator core, EffectKind kind, long? after,
            long? before, long activation, int? tile)
        {
            var effect = new TurnEffectState(core.State.ActiveEffects.Count + 1, kind, 1, null, tile,
                kind == EffectKind.PurchaseDiscount ? 1500 : kind == EffectKind.TemporaryFestival ? 5000 : 0,
                after, before, activatedSourceTurnOrdinal: activation);
            core.State.AddEffect(effect);
            return effect;
        }

        private static void BankruptSource(MatchCoordinator core)
        {
            core.CreateObligation(ObligationKind.Gift, 1, 2, 100000);
            core.DeclareBankruptcy(1);
            Assert.That(core.State.GetPlayer(1).Status, Is.EqualTo(PlayerStatus.Bankrupt));
            Assert.That(core.State.Status, Is.EqualTo(MatchStatus.Running));
        }

        private static void FinishOneRound(MatchCoordinator core)
        {
            long previous = core.State.CompletedRounds;
            for (int i = 0; i < 4 && core.State.CompletedRounds == previous; i++)
            {
                core.State.Phase = TurnPhase.EndTurn;
                core.State.ClockPaused = false;
                core.CompletePersonalTurn();
            }
            Assert.That(core.State.CompletedRounds, Is.EqualTo(previous + 1));
        }

        private sealed class DoubleDice : IDiceSource
        {
            public DiceRoll Roll() => new DiceRoll(1, 1);
        }
    }
}
