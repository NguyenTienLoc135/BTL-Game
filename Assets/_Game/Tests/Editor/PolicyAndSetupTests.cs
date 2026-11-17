using System;
using System.Collections.Generic;
using BtlGame.Board;
using BtlGame.Core;
using BtlGame.Properties;
using NUnit.Framework;

namespace BtlGame.Tests
{
    // Các policy truyền vào đây chỉ là fixture có lựa chọn tường minh, không phê duyệt luật sản phẩm.
    public sealed class PolicyAndSetupTests
    {
        [Test]
        public void ProductFactoryUsesApprovedTwoToFourPlayersAndAllowsDuplicateRoles()
        {
            var core = MatchFactory.Create(new[] { new PlayerSetup(1, "A", RoleId.Engineer),
                new PlayerSetup(2, "B", RoleId.Engineer) }, MatchDuration.Unlimited, random: new Random(1));
            Assert.That(core.State.Players.Count, Is.EqualTo(2));
            Assert.That(core.State.GetPlayer(1).Role, Is.EqualTo(core.State.GetPlayer(2).Role));
            Assert.That(core.State.CurrentPlayerId == 1 || core.State.CurrentPlayerId == 2, Is.True);
            Assert.Throws<ArgumentException>(() => MatchFactory.Create(Roster(1), MatchDuration.Unlimited));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void ExplicitSetupCreatesValidPlayersAndThreeDistinctCityFestivals(int count)
        {
            var core = MatchFactory.Create(Roster(count), MatchDuration.Minutes45,
                new SetupDecisions(2, false, true), random: new Random(123));
            Assert.That(core.State.Players.Count, Is.EqualTo(count));
            var ids = new HashSet<int>();
            foreach (PlayerState player in core.State.Players)
            {
                ids.Add(player.Id);
                Assert.That(player.Cash, Is.EqualTo(300000));
                Assert.That(player.Position, Is.Zero);
                Assert.That(player.Role, Is.Not.EqualTo(RoleId.None));
            }
            Assert.That(ids.Contains(core.State.CurrentPlayerId), Is.True);
            Assert.That(core.State.ClockRemainingTicks, Is.EqualTo(TimeSpan.FromMinutes(45).Ticks));
            var festivals = new HashSet<int>();
            foreach (int tile in core.State.FixedFestivalCityIds)
            {
                Assert.That(festivals.Add(tile), Is.True);
                Assert.That(BoardCatalog.Get(tile).Kind, Is.EqualTo(TileKind.City));
            }
            Assert.That(festivals.Count, Is.EqualTo(3));
        }

        [Test]
        public void FactoryRejectsInvalidRosterDurationAndUnselectedFirstPlayer()
        {
            var explicitSetup = new SetupDecisions(2, false, false);
            Assert.Throws<ArgumentException>(() => MatchFactory.Create(Roster(1), MatchDuration.Unlimited,
                explicitSetup, firstPlayerId: 1));
            Assert.Throws<ArgumentException>(() => MatchFactory.Create(new[] {
                new PlayerSetup(1, "A", RoleId.Banker), new PlayerSetup(1, "B", RoleId.Lawyer) },
                MatchDuration.Unlimited, explicitSetup, firstPlayerId: 1));
            Assert.Throws<ArgumentException>(() => MatchFactory.Create(new[] {
                new PlayerSetup(1, "A", RoleId.Banker), new PlayerSetup(2, "B", RoleId.Banker) },
                MatchDuration.Unlimited, explicitSetup, firstPlayerId: 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => MatchFactory.Create(Roster(2), (MatchDuration)99,
                explicitSetup, firstPlayerId: 1));
            Assert.Throws<ArgumentException>(() => MatchFactory.Create(Roster(2), MatchDuration.Unlimited, explicitSetup));
        }

        [TestCase(InvestmentTieBreak.LowestTierFirst, 1, 0)]
        [TestCase(InvestmentTieBreak.HighestTierFirst, 0, 1)]
        public void P01ExplicitTieBreakPreservesEveryDongAndNullStillBlocks(InvestmentTieBreak policy,
            long house1, long house2)
        {
            Assert.Throws<InvalidOperationException>(() => EconomyRules.AllocateInvestment(2, PropertyLevel.House2));
            IReadOnlyList<long> shares = EconomyRules.AllocateInvestment(2, PropertyLevel.House2, policy);
            CollectionAssert.AreEqual(new long[] { 1, house1, house2, 0, 0 }, shares);
            long total = 0;
            foreach (long share in shares) total += share;
            Assert.That(total, Is.EqualTo(2));
        }

        [TestCase(null)]
        [TestCase(false)]
        [TestCase(true)]
        public void Q12SameLandingHotelNeedsExplicitPermission(bool? allow)
        {
            var core = NewCore(RoleId.Engineer, new GameplayDecisions(engineerHotelOnSameLanding: allow));
            Own(core, 1, 1, PropertyLevel.House3);
            core.State.GetProperty(1).AcquiredOnLandingId = core.State.CurrentLandingId;
            core.State.GetProperty(1).House3ReachedLandingId = core.State.CurrentLandingId;
            core.State.Phase = TurnPhase.EndTurn;
            LegalAction action = Find(core, GameActionKind.ActivateSkill, 1);
            if (allow == true)
            {
                Assert.That(action, Is.Not.Null);
                Assert.That(core.SubmitAction(action.ToCommand()).Status, Is.EqualTo(ActionStatus.Accepted));
                Assert.That(core.State.GetProperty(1).Level, Is.EqualTo(PropertyLevel.Hotel));
            }
            else
            {
                Assert.That(action, Is.Null);
                Assert.That(core.SubmitAction(new GameCommand(GameActionKind.ActivateSkill, 1,
                    core.State.Revision, core.State.DecisionId, 1)).Status, Is.EqualTo(ActionStatus.Rejected));
                Assert.That(core.State.GetProperty(1).Level, Is.EqualTo(PropertyLevel.House3));
                Assert.That(core.State.GetPlayer(1).ActiveReadyAfterCompletedTurns, Is.Zero);
            }
        }

        [Test]
        public void Q16PendingCouponRetryCannotConsumeAnotherCooldownOrStackCoupons()
        {
            var core = NewCore(RoleId.Investor);
            core.SubmitAction(Find(core, GameActionKind.ActivateSkill).ToCommand());
            PlayerState actor = core.State.GetPlayer(1);
            actor.CompletedPersonalTurns = actor.ActiveReadyAfterCompletedTurns;
            long cooldown = actor.ActiveReadyAfterCompletedTurns;
            long revision = core.State.Revision;
            Assert.That(Find(core, GameActionKind.ActivateSkill), Is.Null);
            Assert.That(core.SubmitAction(new GameCommand(GameActionKind.ActivateSkill, 1,
                revision, core.State.DecisionId)).Status, Is.EqualTo(ActionStatus.Rejected));
            Assert.That(actor.ActiveReadyAfterCompletedTurns, Is.EqualTo(cooldown));
            Assert.That(core.State.Revision, Is.EqualTo(revision));
            Assert.That(core.State.ActiveEffects.Count, Is.EqualTo(1));
            Assert.That(actor.Cash, Is.EqualTo(300000));
        }

        [TestCase(null)]
        [TestCase(false)]
        [TestCase(true)]
        public void P02FailedThirdJailTrialRespectsExplicitPolicyOrWaits(bool? waive)
        {
            var core = NewCore(RoleId.Lawyer, new GameplayDecisions(lawyerWaivesForcedJailFee: waive),
                new FixedDice());
            core.AddSourceTurnDurationEffect(1, EffectKind.PaymentImmunity, 1);
            PlayerState actor = core.State.GetPlayer(1);
            actor.IsJailed = true;
            actor.Position = 8;
            actor.JailTurnsStarted = 3;
            core.State.Phase = TurnPhase.AwaitingJailChoice;
            core.ChooseJailRoll(1, false);
            if (!waive.HasValue)
            {
                Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.AwaitingRuleDecision));
                Assert.That(core.State.PendingRuleDecisionId, Is.EqualTo("P02"));
                Assert.That(actor.IsJailed, Is.True);
                Assert.That(actor.Cash, Is.EqualTo(300000));
            }
            else if (waive.Value)
            {
                Assert.That(actor.IsJailed, Is.False);
                Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.EndTurn));
                Assert.That(actor.Cash, Is.EqualTo(300000));
                Assert.That(core.State.PendingObligation, Is.Null);
            }
            else
            {
                Assert.That(core.State.PendingObligation.PayableAmount, Is.EqualTo(200000));
                core.SettlePendingObligation();
                Assert.That(actor.IsJailed, Is.False);
                Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.EndTurn));
                Assert.That(actor.Position, Is.EqualTo(8));
                Assert.That(actor.Cash, Is.EqualTo(100000));
            }
        }

        [TestCase(false, 45000)]
        [TestCase(true, 30000)]
        public void Q15OwnershipPolicyControlsFestivalEffectOnTheNewOwner(bool remove, long expectedRent)
        {
            var core = NewCore(RoleId.Investor, new GameplayDecisions(removeFestivalOnOwnershipChange: remove));
            Own(core, 2, 1, PropertyLevel.House3);
            AddOpponentFestival(core, 1);
            core.SubmitAction(Find(core, GameActionKind.UseUltimate, 1).ToCommand());
            Assert.That(core.State.GetProperty(1).OwnerId, Is.EqualTo(1));
            Assert.That(core.QuoteCityRent(1).FinalRent, Is.EqualTo(expectedRent));
            Assert.That(core.State.ActiveEffects.Count, Is.EqualTo(remove ? 0 : 1));
        }

        [Test]
        public void ProtectionBlocksDestroyWithoutRequiringTheUnrelatedFestivalOwnershipDecision()
        {
            var core = NewCore(RoleId.None, cards: new DestroyProvider());
            Own(core, 2, 1, PropertyLevel.House3);
            core.State.GetPlayer(2).AddHeldCard(5);
            AddOpponentFestival(core, 1);
            core.State.GetPlayer(1).Position = 12;
            core.State.Phase = TurnPhase.ResolvingLanding;
            core.State.LandingResolutionStarted = true;
            core.State.PendingCardId = 9;
            LegalAction choice = Find(core, GameActionKind.CardChoice, 1);
            Assert.That(choice, Is.Not.Null);
            core.SubmitAction(choice.ToCommand());
            Assert.That(core.State.GetProperty(1).OwnerId, Is.EqualTo(2));
            Assert.That(core.State.GetProperty(1).Level, Is.EqualTo(PropertyLevel.House3));
            Assert.That(core.State.GetPlayer(2).HasHeldCard(5), Is.False);
            Assert.That(core.State.ActiveEffects.Count, Is.EqualTo(1));
            Assert.That(core.State.GetPlayer(2).Cash, Is.EqualTo(300000));
        }

        [Test]
        public void Q17LastAssetSaleRemainsCommittedAndDoesNotThrowWhenBankruptcyPolicyIsPending()
        {
            var core = NewCore(RoleId.Entrepreneur, new GameplayDecisions(removeEffectsWhenSourceBankrupt: false));
            Own(core, 1, 1, PropertyLevel.Stake);
            core.AddSourceTurnDurationEffect(1, EffectKind.RentBonus, 2, modifierBasisPoints: 3000);
            core.State.GetPlayer(1).Cash = 0;
            core.CreateObligation(ObligationKind.Gift, 1, 2, 100000);
            ActionResult result = null;
            Assert.DoesNotThrow(() => result = core.SubmitAction(Find(core, GameActionKind.LiquidateProperty, 1).ToCommand()));
            Assert.That(result.Status, Is.EqualTo(ActionStatus.Pending));
            Assert.That(core.State.GetProperty(1).OwnerId, Is.Null);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(20000));
            Assert.That(core.State.GetPlayer(1).Status, Is.EqualTo(PlayerStatus.Active));
            Assert.That(core.State.PendingObligation.PayableAmount, Is.EqualTo(100000));
            Assert.That(Find(core, GameActionKind.DeclareBankruptcy), Is.Null);
        }

        [Test]
        public void ExplicitRemoveSourceEffectsPolicyAllowsBankruptcyAndRemovesThoseEffects()
        {
            var core = NewCore(RoleId.Entrepreneur, new GameplayDecisions(removeEffectsWhenSourceBankrupt: true));
            core.AddSourceTurnDurationEffect(1, EffectKind.RentBonus, 2, modifierBasisPoints: 3000);
            core.State.GetPlayer(1).Cash = 10000;
            core.CreateObligation(ObligationKind.Gift, 1, 2, 100000);
            core.SubmitAction(Find(core, GameActionKind.DeclareBankruptcy).ToCommand());
            Assert.That(core.State.GetPlayer(1).Status, Is.EqualTo(PlayerStatus.Bankrupt));
            Assert.That(core.State.ActiveEffects.Count, Is.Zero);
            Assert.That(core.State.GetPlayer(2).Cash, Is.EqualTo(310000));
            Assert.That(core.State.Status, Is.EqualTo(MatchStatus.Finished));
        }

        [Test]
        public void ExplicitRepeatTeleportPolicyHasMatchingQueryAndExecutionGuards()
        {
            var core = NewCore(RoleId.Banker, new GameplayDecisions(teleportOncePerChain: false));
            core.State.GetPlayer(1).Position = 24;
            core.State.Phase = TurnPhase.ResolvingLanding;
            core.State.LandingResolutionStarted = true;
            core.State.TeleportUsedInCurrentChain = true;
            LegalAction choice = Find(core, GameActionKind.Teleport, 25);
            Assert.That(choice, Is.Not.Null);
            ActionResult result = null;
            Assert.DoesNotThrow(() => result = core.SubmitAction(choice.ToCommand()));
            Assert.That(result.Status, Is.EqualTo(ActionStatus.Pending));
            Assert.That(core.State.GetPlayer(1).Position, Is.EqualTo(25));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(200000));
            Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.Moving));
        }

        private static IReadOnlyList<PlayerSetup> Roster(int count)
        {
            var players = new List<PlayerSetup>();
            for (int i = 0; i < count; i++)
                players.Add(new PlayerSetup(i + 1, "Người " + (i + 1), (RoleId)(i + 1),
                    i % 2 == 0 ? ControllerKind.Human : ControllerKind.AI));
            return players.AsReadOnly();
        }

        private static MatchCoordinator NewCore(RoleId role, GameplayDecisions decisions = null,
            IDiceSource dice = null, ICardActionProvider cards = null)
        {
            var state = new GameState(new[] { new PlayerState(1, 0, "A", role), new PlayerState(2, 1, "B") },
                1, new[] { 27, 29, 31 }, decisions: decisions);
            state.CurrentLandingId = 5;
            return new MatchCoordinator(state, dice, cards);
        }

        private static void Own(MatchCoordinator core, int playerId, int tile, PropertyLevel level)
        {
            PropertyState property = core.State.GetProperty(tile);
            property.OwnerId = playerId;
            property.Level = level;
            property.AcquiredOnLandingId = 0;
            if (level >= PropertyLevel.House3) property.House3ReachedLandingId = 0;
            var investment = EconomyRules.AllocateInvestment(EconomyRules.ListedPrice(tile, level), level);
            for (int i = 0; i < investment.Count; i++) property.MutableInvestedByLevel[i] = investment[i];
        }

        private static void AddOpponentFestival(MatchCoordinator core, int tile)
        {
            core.State.CurrentPlayerId = 2;
            core.State.AddEffect(new TurnEffectState(500, EffectKind.TemporaryFestival, 2, 2, tile, 5000, null, null));
            core.State.CurrentPlayerId = 1;
        }

        private static LegalAction Find(MatchCoordinator core, GameActionKind kind, int? tile = null)
        {
            int actor = core.State.PendingObligation?.PayerId ?? core.State.CurrentPlayerId;
            foreach (LegalAction action in core.QueryLegalActions(actor))
                if (action.Kind == kind && action.TargetTileId == tile) return action;
            return null;
        }

        private sealed class FixedDice : IDiceSource
        {
            public DiceRoll Roll() => new DiceRoll(1, 2);
        }

        private sealed class DestroyProvider : ICardActionProvider
        {
            public int? PeekNextCard(GameSnapshot snapshot) => 9;
            public IReadOnlyList<CardActionOption> QueryOptions(GameSnapshot snapshot, int actorId) =>
                Array.AsReadOnly(new[] { new CardActionOption(1, "Phá Hà Nội", targetTileId: 1) });
            public CardEffectPlan Plan(GameSnapshot snapshot, int actorId, int choiceId) =>
                new CardEffectPlan(9, CardEffectKind.Destroy, targetTileId: 1);
        }
    }
}
