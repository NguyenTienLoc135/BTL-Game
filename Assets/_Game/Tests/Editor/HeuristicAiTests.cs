using System;
using BtlGame.AI;
using BtlGame.Board;
using BtlGame.Core;
using BtlGame.Properties;
using NUnit.Framework;

namespace BtlGame.Tests
{
    public sealed class HeuristicAiTests
    {
        [Test]
        public void EmptyActionsOrUnknownPlayerNeverCreateACommand()
        {
            var state = NewState();
            var ai = new HeuristicAi();
            Assert.That(ai.Choose(state.CreateSnapshot(), 1, Array.Empty<LegalAction>()), Is.Null);
            Assert.That(ai.Choose(state.CreateSnapshot(), 99, new[] { Option(state, GameActionKind.RollDice) }), Is.Null);
        }

        [Test]
        public void InvalidArgumentsAreReported()
        {
            var ai = new HeuristicAi();
            Assert.Throws<ArgumentNullException>(() => ai.Choose(null, 1, Array.Empty<LegalAction>()));
            Assert.Throws<ArgumentNullException>(() => ai.Choose(NewState().CreateSnapshot(), 1, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HeuristicAi(-1));
        }

        [Test]
        public void AiNeverFinishesABoardAnimation()
        {
            var state = NewState();
            Assert.That(new HeuristicAi().Choose(state.CreateSnapshot(), 1,
                new[] { Option(state, GameActionKind.CompletePresentation) }), Is.Null);
        }

        [Test]
        public void FinishedOrBankruptPlayerDoesNotAct()
        {
            var state = NewState();
            var option = Option(state, GameActionKind.RollDice);
            state.Status = MatchStatus.Finished;
            Assert.That(new HeuristicAi().Choose(state.CreateSnapshot(), 1, new[] { option }), Is.Null);
            state.Status = MatchStatus.Running;
            state.GetPlayer(1).Status = PlayerStatus.Bankrupt;
            Assert.That(new HeuristicAi().Choose(state.CreateSnapshot(), 1, new[] { option }), Is.Null);
        }

        [Test]
        public void WrongActorAndStaleRevisionOrDecisionAreNotReused()
        {
            var state = NewState();
            var options = new[]
            {
                new LegalAction(GameActionKind.RollDice, 2, null, null, 0, state.Revision, state.DecisionId, ""),
                new LegalAction(GameActionKind.RollDice, 1, null, null, 0, state.Revision - 1, state.DecisionId, ""),
                new LegalAction(GameActionKind.RollDice, 1, null, null, 0, state.Revision, state.DecisionId - 1, "")
            };
            Assert.That(new HeuristicAi().Choose(state.CreateSnapshot(), 1, options), Is.Null);
        }

        [Test]
        public void BuyingPreservesReserveAndUsesTheExactCoreOption()
        {
            var state = NewState();
            state.GetPlayer(1).Cash = 60_000;
            var buy = Option(state, GameActionKind.BuyCity, 1, PropertyLevel.Stake, 40_000);
            var skip = Option(state, GameActionKind.SkipLanding);
            var ai = new HeuristicAi(50_000);
            Assert.That(ai.Choose(state.CreateSnapshot(), 1, new[] { buy, skip }).Kind,
                Is.EqualTo(GameActionKind.SkipLanding));
            state.GetPlayer(1).Cash = 100_000;
            GameCommand chosen = ai.Choose(state.CreateSnapshot(), 1, new[] { buy, skip });
            Assert.That(chosen.Kind, Is.EqualTo(buy.Kind));
            Assert.That(chosen.ActorId, Is.EqualTo(buy.ActorId));
            Assert.That(chosen.ExpectedRevision, Is.EqualTo(buy.Revision));
            Assert.That(chosen.DecisionId, Is.EqualTo(buy.DecisionId));
            Assert.That(chosen.TargetTileId, Is.EqualTo(buy.TargetTileId));
            Assert.That(chosen.TargetLevel, Is.EqualTo(buy.TargetLevel));
        }

        [Test]
        public void FourthBeachWinTakesPriorityOverTheCashReserve()
        {
            var state = NewState();
            Own(state, 4, 100_000);
            Own(state, 14, 130_000);
            Own(state, 18, 160_000);
            state.GetPlayer(1).Cash = 190_000;
            GameCommand chosen = new HeuristicAi().Choose(state.CreateSnapshot(), 1, new[]
            {
                Option(state, GameActionKind.SkipLanding), Option(state, GameActionKind.BuyBeach, 25, cost: 190_000)
            });
            Assert.That(chosen.Kind, Is.EqualTo(GameActionKind.BuyBeach));
        }

        [Test]
        public void ThirdCountryWinTakesPriorityOverTheCashReserve()
        {
            var state = NewState();
            foreach (int tile in new[] { 1, 2, 3, 5, 6, 7, 13 })
                Own(state, tile, EconomyRules.ListedPrice(tile, PropertyLevel.Stake), PropertyLevel.Stake);
            state.GetPlayer(1).Cash = 148_000;
            GameCommand chosen = new HeuristicAi().Choose(state.CreateSnapshot(), 1, new[]
            {
                Option(state, GameActionKind.SkipLanding),
                Option(state, GameActionKind.BuyCity, 15, PropertyLevel.Stake, 148_000)
            });
            Assert.That(chosen.TargetTileId, Is.EqualTo(15));
        }

        [Test]
        public void ExistingZeroPaymentDoesNotSpendACardOrSkill()
        {
            var state = NewState(RoleId.Lawyer);
            state.PendingObligation = new MandatoryObligation(7, ObligationKind.Rent, 1, 2, 30_000, 1)
                { PayableAmount = 0 };
            GameCommand chosen = new HeuristicAi().Choose(state.CreateSnapshot(), 1, new[]
            {
                Option(state, GameActionKind.ActivateSkill), Option(state, GameActionKind.UseRentCard),
                Option(state, GameActionKind.SettleObligation)
            });
            Assert.That(chosen.Kind, Is.EqualTo(GameActionKind.SettleObligation));
        }

        [Test]
        public void LawyerReactionIsPreferredToConsumingAHeldRentCard()
        {
            var state = NewState(RoleId.Lawyer);
            state.PendingObligation = new MandatoryObligation(7, ObligationKind.Rent, 1, 2, 30_000, 1);
            GameCommand chosen = new HeuristicAi().Choose(state.CreateSnapshot(), 1, new[]
            {
                Option(state, GameActionKind.UseRentCard), Option(state, GameActionKind.SettleObligation),
                Option(state, GameActionKind.ActivateSkill)
            });
            Assert.That(chosen.Kind, Is.EqualTo(GameActionKind.ActivateSkill));
        }

        [Test]
        public void PositiveTaxUsesOnlyTheOfferedTaxCardBeforeSettlement()
        {
            var state = NewState();
            state.PendingObligation = new MandatoryObligation(7, ObligationKind.TaxTile, 1, null, 60_000, null);
            GameCommand chosen = new HeuristicAi().Choose(state.CreateSnapshot(), 1, new[]
            {
                Option(state, GameActionKind.SettleObligation), Option(state, GameActionKind.UseTaxCard)
            });
            Assert.That(chosen.Kind, Is.EqualTo(GameActionKind.UseTaxCard));
        }

        [Test]
        public void LiquidationChoosesTheSmallestSufficientRefundBeforeBankruptcy()
        {
            var state = NewState();
            state.GetPlayer(1).Cash = 10_000;
            Own(state, 1, 60_000, PropertyLevel.House1);
            Own(state, 2, 120_000);
            Own(state, 7, 200_000);
            state.PendingObligation = new MandatoryObligation(7, ObligationKind.Gift, 1, 2, 60_000, null);
            GameCommand chosen = new HeuristicAi().Choose(state.CreateSnapshot(), 1, new[]
            {
                Option(state, GameActionKind.DeclareBankruptcy),
                Option(state, GameActionKind.LiquidateProperty, 1),
                Option(state, GameActionKind.LiquidateProperty, 7),
                Option(state, GameActionKind.LiquidateProperty, 2)
            });
            Assert.That(chosen.Kind, Is.EqualTo(GameActionKind.LiquidateProperty));
            Assert.That(chosen.TargetTileId, Is.EqualTo(2));
        }

        [Test]
        public void LiquidationMakesProgressWhenNoSinglePropertyCoversDebt()
        {
            var state = NewState();
            state.GetPlayer(1).Cash = 0;
            Own(state, 1, 60_000, PropertyLevel.House1);
            Own(state, 2, 120_000);
            state.PendingObligation = new MandatoryObligation(7, ObligationKind.BankerDebt, 1, null, 110_000, null);
            GameCommand chosen = new HeuristicAi().Choose(state.CreateSnapshot(), 1, new[]
            {
                Option(state, GameActionKind.LiquidateProperty, 1),
                Option(state, GameActionKind.LiquidateProperty, 2)
            });
            Assert.That(chosen.TargetTileId, Is.EqualTo(2));
        }

        [Test]
        public void OfferedBankruptcyIsChosenOnlyWhenNoSettlementOrLiquidationRemains()
        {
            var state = NewState();
            state.GetPlayer(1).Cash = 0;
            state.PendingObligation = new MandatoryObligation(7, ObligationKind.BankerDebt, 1, null, 110_000, null);
            Assert.That(new HeuristicAi().Choose(state.CreateSnapshot(), 1,
                new[] { Option(state, GameActionKind.DeclareBankruptcy) }).Kind,
                Is.EqualTo(GameActionKind.DeclareBankruptcy));
        }

        [Test]
        public void BankerUsesItsGrantBeforeOpeningANewLoan()
        {
            var state = NewState(RoleId.Banker);
            state.GetPlayer(1).Cash = 0;
            Own(state, 2, 240_000);
            GameCommand chosen = new HeuristicAi().Choose(state.CreateSnapshot(), 1, new[]
            {
                Option(state, GameActionKind.RollDice), Option(state, GameActionKind.ActivateSkill),
                Option(state, GameActionKind.UseUltimate)
            });
            Assert.That(chosen.Kind, Is.EqualTo(GameActionKind.UseUltimate));
        }

        [Test]
        public void StrategistRerollsJailLandingButConfirmsASafeResult()
        {
            var state = NewState(RoleId.Strategist);
            state.GetPlayer(1).Position = 6;
            state.PendingDiceRoll = new DiceRoll(1, 1);
            var actions = new[] { Option(state, GameActionKind.ConfirmRoll), Option(state, GameActionKind.RerollDice) };
            Assert.That(new HeuristicAi().Choose(state.CreateSnapshot(), 1, actions).Kind,
                Is.EqualTo(GameActionKind.RerollDice));
            state.PendingDiceRoll = new DiceRoll(2, 3);
            Assert.That(new HeuristicAi().Choose(state.CreateSnapshot(), 1, actions).Kind,
                Is.EqualTo(GameActionKind.ConfirmRoll));
        }

        [Test]
        public void StrategistRerollsAFailedJailTrialEvenWhenTheDiceDestinationWouldBeSafe()
        {
            var state = NewState(RoleId.Strategist);
            state.GetPlayer(1).Position = 8;
            state.GetPlayer(1).IsJailed = true;
            state.GetPlayer(1).JailTurnsStarted = 2;
            state.Phase = TurnPhase.AwaitingRollConfirmation;
            state.PendingJailTrial = true;
            state.PendingDiceRoll = new DiceRoll(1, 2);
            var actions = new[] { Option(state, GameActionKind.ConfirmRoll), Option(state, GameActionKind.RerollDice) };

            Assert.That(new HeuristicAi().Choose(state.CreateSnapshot(), 1, actions).Kind,
                Is.EqualTo(GameActionKind.RerollDice));
            Assert.That(state.GetPlayer(1).ActiveReadyAfterCompletedTurns, Is.Zero);
            Assert.That(state.GetPlayer(1).IsJailed, Is.True);
        }

        [Test]
        public void StrategistKeepsADoubleJailTrialEvenWhenTheEscapeDestinationIsOpponentProperty()
        {
            var state = NewState(RoleId.Strategist);
            state.GetPlayer(1).Position = 8;
            state.GetPlayer(1).IsJailed = true;
            state.GetPlayer(1).JailTurnsStarted = 2;
            state.Phase = TurnPhase.AwaitingRollConfirmation;
            state.PendingJailTrial = true;
            state.PendingDiceRoll = new DiceRoll(1, 1);
            state.GetProperty(10).OwnerId = 2;
            state.GetProperty(10).Level = PropertyLevel.House3;
            var actions = new[] { Option(state, GameActionKind.RerollDice), Option(state, GameActionKind.ConfirmRoll) };

            Assert.That(new HeuristicAi().Choose(state.CreateSnapshot(), 1, actions).Kind,
                Is.EqualTo(GameActionKind.ConfirmRoll));
            Assert.That(state.GetPlayer(1).ActiveReadyAfterCompletedTurns, Is.Zero);
        }

        [Test]
        public void ChoosingDoesNotMutateOrKeepAuthorityOverTheMatch()
        {
            var state = NewState();
            state.GetPlayer(1).Cash = 100_000;
            GameSnapshot snapshot = state.CreateSnapshot();
            new HeuristicAi().Choose(snapshot, 1,
                new[] { Option(state, GameActionKind.BuyCity, 1, PropertyLevel.Stake, 40_000) });
            Assert.That(state.Revision, Is.EqualTo(snapshot.Revision));
            Assert.That(state.GetPlayer(1).Cash, Is.EqualTo(100_000));
            Assert.That(state.GetProperty(1).OwnerId, Is.Null);
            Assert.That(state.GetPlayer(1).Position, Is.Zero);
        }

        [Test]
        public void CardChoicePreservesTheProviderChoiceTokenAndCoreDecision()
        {
            var state = NewState();
            var option = new LegalAction(GameActionKind.CardChoice, 1, 15, null, 0,
                state.Revision, state.DecisionId, "Chọn mục tiêu từ gói C", choiceId: 42);
            GameCommand command = new HeuristicAi().Choose(state.CreateSnapshot(), 1, new[] { option });
            Assert.That(command, Is.Not.Null);
            Assert.That(command.Kind, Is.EqualTo(GameActionKind.CardChoice));
            Assert.That(command.ChoiceId, Is.EqualTo(42));
            Assert.That(command.TargetTileId, Is.EqualTo(15));
            Assert.That(command.ExpectedRevision, Is.EqualTo(state.Revision));
            Assert.That(command.DecisionId, Is.EqualTo(state.DecisionId));
        }

        [Test]
        public void OfferedMandatoryCardChoiceIsNotBlockedByTheCashReserve()
        {
            var state = NewState();
            state.GetPlayer(1).Cash = 0;
            var option = new LegalAction(GameActionKind.CardChoice, 1, null, null, 200_000,
                state.Revision, state.DecisionId, "Nghĩa vụ bắt buộc", choiceId: 7);
            GameCommand command = new HeuristicAi().Choose(state.CreateSnapshot(), 1, new[] { option });
            Assert.That(command.Kind, Is.EqualTo(GameActionKind.CardChoice));
            Assert.That(command.ChoiceId, Is.EqualTo(7));
            Assert.That(state.GetPlayer(1).Cash, Is.Zero);
        }

        private static GameState NewState(RoleId role = RoleId.None)
        {
            return new GameState(new[] { new PlayerState(1, 0, "AI", role), new PlayerState(2, 1, "Human") },
                1, new[] { 26, 27, 29 }) { Revision = 12, DecisionId = 9 };
        }

        private static LegalAction Option(GameState state, GameActionKind kind, int? tile = null,
            PropertyLevel? level = null, long cost = 0)
            => new LegalAction(kind, 1, tile, level, cost, state.Revision, state.DecisionId, "test");

        private static void Own(GameState state, int tileId, long investment, PropertyLevel level = PropertyLevel.House3)
        {
            PropertyState property = state.GetProperty(tileId);
            property.OwnerId = 1;
            if (BoardCatalog.Get(tileId).Kind == TileKind.Beach) property.BeachInvestment = investment;
            else
            {
                property.Level = level;
                var tiers = EconomyRules.AllocateInvestment(investment, level);
                for (int i = 0; i < tiers.Count; i++) property.MutableInvestedByLevel[i] = tiers[i];
            }
        }
    }
}
