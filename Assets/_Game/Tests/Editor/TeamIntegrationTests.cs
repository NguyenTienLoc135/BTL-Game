using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BtlGame.AI;
using BtlGame.Board;
using BtlGame.Core;
using BTLGame.Cards;
using BTLGame.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace BtlGame.Tests
{
    public sealed class TeamIntegrationTests
    {
        [Test]
        public void PreviewIsStableDoesNotAdvanceDrawAndIncludesAllFourteenTypes()
        {
            var core = CreateAiMatch(24);
            var provider = new RandomEventCardProvider(24);
            var seen = new HashSet<int>();
            bool repeatedConsecutively = false;
            int previous = 0;
            for (int i = 0; i < 1000; i++)
            {
                core.State.CardDrawOrdinal = i;
                long revision = core.State.Revision;
                int card = provider.PeekNextCard(core.QuerySnapshot()).Value;
                Assert.That(card, Is.InRange(1, 14));
                Assert.That(provider.PeekNextCard(core.QuerySnapshot()), Is.EqualTo(card));
                Assert.That(core.State.CardDrawOrdinal, Is.EqualTo(i));
                Assert.That(core.State.Revision, Is.EqualTo(revision));
                seen.Add(card);
                repeatedConsecutively |= card == previous;
                previous = card;
            }
            Assert.That(seen.Count, Is.EqualTo(14));
            Assert.That(repeatedConsecutively, Is.True, "Q02 cho phép rút lại cùng loại ngay lần kế tiếp.");
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)]
        [TestCase(11)] [TestCase(12)] [TestCase(13)] [TestCase(14)]
        public void ProductionCardChoicesAreAcceptedByCore(int cardId)
        {
            var provider = new RandomEventCardProvider(5);
            var core = CreateAiMatch(5, provider);
            core.State.GetPlayer(core.State.CurrentPlayerId).Position = 12;
            core.State.Phase = TurnPhase.ResolvingLanding;
            core.State.LandingResolutionStarted = true;
            core.State.PendingCardId = cardId;
            core.State.CardLandingResolved = false;
            var actions = core.QueryLegalActions(core.State.CurrentPlayerId);
            Assert.That(actions.Count, Is.GreaterThan(0), "Không được kẹt khi thẻ không có mục tiêu.");
            Assert.That(core.SubmitAction(actions[0].ToCommand()).Status, Is.Not.EqualTo(ActionStatus.Rejected));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void FourAiPlayersFinishWithTheProductionCardProvider(int seed)
        {
            var core = CreateAiMatch(seed);
            var ai = core.State.Players.ToDictionary(p => p.Id, p => new AiController(core, p.Id));
            for (int step = 0; step < 4000 && core.State.Status == MatchStatus.Running; step++)
            {
                core.AdvanceClock(TimeSpan.FromSeconds(30));
                if (core.State.Status == MatchStatus.Finished) break;
                if (core.State.Phase == TurnPhase.Moving)
                    Assert.That(core.CompletePresentation(core.State.CurrentActionId).Status, Is.Not.EqualTo(ActionStatus.Rejected));
                else
                {
                    int actor = core.State.PendingObligations.Count > 0 ? core.State.PendingObligations[0].PayerId : core.State.CurrentPlayerId;
                    ActionResult result = ai[actor].Step();
                    Assert.That(result, Is.Not.Null, "Kẹt tại " + core.State.Phase + "; thẻ " + core.State.PendingCardId);
                    Assert.That(result.Status, Is.Not.EqualTo(ActionStatus.Rejected), result.Message);
                }
                Assert.That(core.State.Players.All(p => p.Cash >= 0), Is.True);
            }
            Assert.That(core.State.Status, Is.EqualTo(MatchStatus.Finished));
            Assert.That(core.State.WinnerPlayerIds.Count, Is.GreaterThan(0));
        }

        [Test]
        public void UiFontHasVietnameseGlyphsAndSdfPadding()
        {
            var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_Game/Art/Fonts/BeVietnamPro-UI SDF.asset");
            Assert.That(font, Is.Not.Null);
            Assert.That(font.atlasPadding, Is.GreaterThanOrEqualTo(8));
            Assert.That(font.material.GetFloat("_GradientScale"), Is.GreaterThanOrEqualTo(9));
            Assert.That(font.HasCharacters("CỜ TỶ PHÚ RPG — Chưa chọn, Nhà Đầu Tư, Kỹ Sư, Luật Sư, Hà Nội, Đà Nẵng, Xuất phát"), Is.True);
        }

        [Test]
        public void UiSetupUsesApprovedRulesAndDoesNotInventASecondState()
        {
            var request = new MatchSetupRequest(MatchControllerMode.VersusAI, 4, MatchTimeLimit.Unlimited,
                new[] { "Engineer", "Engineer", "Lawyer", "Strategist" });
            var core = UnityMatchHost.CreateMatch(request, 10);
            Assert.That(core.QuerySnapshot().Players.Count, Is.EqualTo(4));
            Assert.That(core.QuerySnapshot().Players.Count(p => p.Controller == ControllerKind.AI), Is.EqualTo(3));
            Assert.That(core.QuerySnapshot().Players.All(p => p.Cash == 300000 && p.Position == 0), Is.True);
            Assert.That(core.QuerySnapshot().ClockRemainingTicks, Is.EqualTo(long.MaxValue));
            Assert.That(core.QuerySnapshot().FixedFestivalCityIds.Distinct().Count(), Is.EqualTo(3));
        }

        private static Button FindActiveButton(string name) => Object
            .FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(b => b.name == name && b.gameObject.activeInHierarchy);

        /// <summary>
        /// Luồng thật qua hai Scene giao diện mới: MainMenu (Hướng dẫn → Bắt đầu chơi → Chơi với người → Bắt đầu trận)
        /// rồi Gameplay do UnityMatchHost tạo trận từ cấu hình menu, tung xúc xắc, xác nhận và chờ quân cờ di chuyển xong.
        /// </summary>
        [UnityTest]
        public IEnumerator ActualScenesStartFromMenuAndCompleteAHumanMovementWithUi()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/MainMenu.unity");
            yield return new EnterPlayMode();
            yield return null;

            FindActiveButton("Btn_HowTo").onClick.Invoke();
            var howTo = Object.FindObjectsByType<BTLGame.Presentation.UiPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(p => p.name == "Popup_HowTo");
            Assert.That(howTo.IsOpen, Is.True);
            howTo.Close();
            Assert.That(howTo.IsOpen, Is.False);

            FindActiveButton("Btn_Start").onClick.Invoke();
            FindActiveButton("Btn_VsHuman").onClick.Invoke();
            Button start = FindActiveButton("Btn_StartMatch");
            Assert.That(start.interactable, Is.True, "Màn thiết lập phải gửi được cấu hình hợp lệ cho Core.");
            start.onClick.Invoke();

            float timeout = Time.realtimeSinceStartup + 15;
            while (SceneManager.GetActiveScene().name != "Gameplay" && Time.realtimeSinceStartup < timeout) yield return null;
            yield return null;
            var host = Object.FindFirstObjectByType<UnityMatchHost>();
            Assert.That(host, Is.Not.Null);
            Assert.That(host.Match, Is.Not.Null);
            var players = host.Match.QuerySnapshot().Players;
            Assert.That(players.Count, Is.EqualTo(2));
            Assert.That(players.All(p => p.Controller == ControllerKind.Human && p.Cash == 300000 && p.Position == 0), Is.True);
            Assert.That(Object.FindObjectsByType<BTLGame.Board.BoardTileView>(FindObjectsSortMode.None).Length, Is.EqualTo(32));
            var tile = Object.FindObjectsByType<BTLGame.Board.BoardTileView>(FindObjectsSortMode.None).First(t => t.TileId == 1);
            var propertyBlock = new MaterialPropertyBlock();
            tile.transform.Find("Body").GetComponent<Renderer>().GetPropertyBlock(propertyBlock);
            Assert.That(Vector4.Distance(propertyBlock.GetColor("_BaseColor"), BTLGame.Board.BoardVisualCatalog.Get(1).Color), Is.LessThan(.00001f),
                "Màu nhóm quốc gia phải được bind khi chạy, không dựa vào PropertyBlock trong Editor.");

            long landingBefore = host.Match.QuerySnapshot().CurrentLandingId;
            Button roll = FindActiveButton("Btn_Roll");
            Assert.That(roll.interactable, Is.True);
            roll.onClick.Invoke();
            yield return null;
            Button confirm = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(b => b.name.StartsWith("OptionButton_") && b.gameObject.activeInHierarchy && b.interactable);
            confirm.onClick.Invoke();
            timeout = Time.realtimeSinceStartup + 15;
            while (host.Match.QuerySnapshot().Phase == TurnPhase.Moving && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(host.Match.QuerySnapshot().Phase, Is.Not.EqualTo(TurnPhase.Moving), "Board phải callback về Core sau animation.");
            Assert.That(host.Match.QuerySnapshot().CurrentLandingId, Is.GreaterThan(landingBefore));
            Assert.That(Object.FindFirstObjectByType<BTLGame.Board.BoardView>().IsAnimating, Is.False);
            yield return new ExitPlayMode();
        }

        private static MatchCoordinator CreateAiMatch(int seed, ICardActionProvider provider = null)
        {
            var roster = new List<PlayerSetup>();
            for (int i = 0; i < 4; i++)
                roster.Add(new PlayerSetup(i, "AI " + i, (RoleId)((i + seed) % 6 + 1), ControllerKind.AI));
            return MatchFactory.Create(roster, MatchDuration.Minutes30, random: new Random(seed),
                dice: new RandomDiceSource(new Random(seed + 100)), cards: provider ?? new RandomEventCardProvider(seed));
        }
    }
}
