using System;
using System.Collections.Generic;
using BtlGame.AI;
using BtlGame.Board;
using BtlGame.Core;
using NUnit.Framework;

namespace BtlGame.Tests
{
    public sealed class MatchSimulationTests
    {
        [TestCase(0)]
        [TestCase(2)]
        [TestCase(4)]
        public void FourAiPlayersCompleteAFiniteMatchThroughThePublicGateway(int roleOffset)
        {
            var policies = GameplayDecisions.Approved;
            var roster = new List<PlayerSetup>();
            for (int i = 0; i < 4; i++)
                roster.Add(new PlayerSetup(i + 1, "AI " + i, (RoleId)((i + roleOffset) % 6 + 1), ControllerKind.AI));
            var core = MatchFactory.Create(roster, MatchDuration.Minutes30,
                SetupDecisions.Approved, policies, random: new Random(100 + roleOffset),
                dice: new RandomDiceSource(new Random(200 + roleOffset)), cards: new RewardFixture());
            var controllers = new Dictionary<int, AiController>();
            foreach (PlayerSetup entry in roster) controllers.Add(entry.Id, new AiController(core, entry.Id));

            for (int step = 0; step < 2000 && core.State.Status == MatchStatus.Running; step++)
            {
                core.AdvanceClock(TimeSpan.FromSeconds(30));
                if (core.State.Status == MatchStatus.Finished) break;
                if (core.State.Phase == TurnPhase.Moving)
                {
                    // Fixture đóng vai B báo animation xong; AiController không làm việc này.
                    Assert.That(core.CompletePresentation(core.State.CurrentActionId).Status, Is.Not.EqualTo(ActionStatus.Rejected));
                }
                else
                {
                    int actor = core.State.PendingObligation?.PayerId ?? core.State.CurrentPlayerId;
                    ActionResult result = controllers[actor].Step();
                    Assert.That(result, Is.Not.Null, "Kẹt tại " + core.State.Phase + ": " + string.Join(", ", core.QueryIntegrationBlocks()));
                    Assert.That(result.Status, Is.Not.EqualTo(ActionStatus.Rejected), result.Message);
                }
                foreach (PlayerState player in core.State.Players) Assert.That(player.Cash, Is.GreaterThanOrEqualTo(0));
                foreach (PropertyState property in core.State.Properties) Assert.That(property.TotalInvestment, Is.GreaterThanOrEqualTo(0));
            }
            Assert.That(core.State.Status, Is.EqualTo(MatchStatus.Finished), "Trận không tiến được tới kết quả.");
            Assert.That(core.State.WinnerPlayerIds.Count, Is.GreaterThan(0));
        }

        private sealed class RewardFixture : ICardActionProvider
        {
            // Nguồn xác định để kiểm thử lifecycle; không là thuật toán rút thẻ Q02 của game.
            public int? PeekNextCard(GameSnapshot snapshot) => 1;
            public IReadOnlyList<CardActionOption> QueryOptions(GameSnapshot snapshot, int actorId) =>
                Array.AsReadOnly(new[] { new CardActionOption(1, "Nhận thưởng fixture") });
            public CardEffectPlan Plan(GameSnapshot snapshot, int actorId, int choiceId) =>
                new CardEffectPlan(1, CardEffectKind.BankCredit, amount: 300000);
        }
    }
}
