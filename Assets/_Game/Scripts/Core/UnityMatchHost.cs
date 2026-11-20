using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BtlGame.AI;
using BtlGame.Characters;
using BTLGame.Board;
using BTLGame.Cards;
using BTLGame.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BtlGame.Core
{
    /// <summary>Điểm nối Unity: chỉ giữ coordinator, chuyển input thành lệnh và phản hồi animation.
    /// Không ghi trực tiếp GameState hoặc tính lại luật kinh tế trong view.</summary>
    public sealed class UnityMatchHost : MonoBehaviour
    {
        private static MatchSetupRequest _nextSetup;
        private MainMenuSetupController _menu;
        private IGameplayUi _ui;
        private BoardView _board;
        private readonly Dictionary<int, AiController> _ais = new Dictionary<int, AiController>();
        private readonly Dictionary<string, LegalAction> _offered = new Dictionary<string, LegalAction>();
        private long _popupId;
        private long _nextPopupId;
        private bool _resultShown;
        private float _nextAiAt;
        private bool _localChoicePaused;
        public MatchCoordinator Match { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSetup() => _nextSetup = null;

        private void Start()
        {
            _menu = FindFirstObjectByType<MainMenuSetupController>();
            if (_menu != null)
            {
                _menu.ConfigureOptions(SetupDecisions.Approved.MinimumPlayers, 4,
                    RoleCatalog.All.Select(r => new SetupRoleOption(r.Id.ToString(), r.DisplayName)).ToArray());
                _menu.SetupRequested += BeginMatch;
                return;
            }
            _board = FindFirstObjectByType<BoardView>();
            _ui = FindGameplayUi();
            if (_board == null || _ui == null) throw new InvalidOperationException("Gameplay thiếu BoardView hoặc IGameplayUi (GameplayUIController/PresentationGameplayUi).");
            if (_nextSetup == null)
            {
                // Play trực tiếp Gameplay vẫn đi qua màn hình cấu hình, không ngầm tạo một roster khác.
                SceneManager.LoadScene("MainMenu");
                return;
            }
            MatchSetupRequest setup = _nextSetup;
            _nextSetup = null;
            Match = CreateMatch(setup);
            foreach (PlayerSnapshot player in Match.QuerySnapshot().Players)
                if (player.Controller == ControllerKind.AI) _ais.Add(player.Id, new AiController(Match, player.Id));
            _ui.ActionRequested += OnHudAction;
            _ui.DecisionRequested += OnDecision;
            _ui.CardIntentRequested += OnCardIntent;
            _board.PresentationCompleted += OnPresentationCompleted;
            Refresh();
        }

        private static IGameplayUi FindGameplayUi()
        {
            // Giao diện nào cài IGameplayUi đều dùng được (GameplayUIController của Codex hoặc PresentationGameplayUi).
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (behaviour is IGameplayUi ui) return ui;
            return null;
        }

        /// <summary>
        /// Điểm vào cho màn thiết lập (bất kỳ giao diện nào): kiểm tra cấu hình bằng đúng luật Setup rồi xếp vào hàng đợi
        /// cho Scene Gameplay. Ném ArgumentException nếu cấu hình không hợp lệ (không chuyển Scene). Gọi LoadScene("Gameplay") sau đó.
        /// </summary>
        public static void QueueMatch(MatchSetupRequest request)
        {
            CreateMatch(request); // chỉ để kiểm tra; trận thật được tạo trong Gameplay
            _nextSetup = request;
        }

        public static MatchCoordinator CreateMatch(MatchSetupRequest request, int? seed = null)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (!Enum.IsDefined(typeof(MatchControllerMode), request.Mode) ||
                !Enum.IsDefined(typeof(MatchTimeLimit), request.TimeLimit) ||
                request.RoleIds.Count != request.PlayerCount)
                throw new ArgumentException("Cấu hình UI không hợp lệ.", nameof(request));
            var roster = new List<PlayerSetup>();
            for (int i = 0; i < request.PlayerCount; i++)
            {
                if (!Enum.TryParse(request.RoleIds[i], out RoleId role) || !Enum.IsDefined(typeof(RoleId), role) || role == RoleId.None)
                    throw new ArgumentException("Nghề không có trong RoleCatalog.", nameof(request));
                bool ai = request.Mode == MatchControllerMode.VersusAI && i > 0;
                // Chơi với Bot: ghế 0 là "Bạn", các ghế còn lại "Bot n". Chơi với người: "Người chơi n".
                string name = ai ? "Bot " + i : request.Mode == MatchControllerMode.VersusAI ? "Bạn" : "Người chơi " + (i + 1);
                roster.Add(new PlayerSetup(i, name, role, ai ? ControllerKind.AI : ControllerKind.Human));
            }
            MatchDuration duration = request.TimeLimit switch
            {
                MatchTimeLimit.Minutes30 => MatchDuration.Minutes30,
                MatchTimeLimit.Minutes45 => MatchDuration.Minutes45,
                MatchTimeLimit.Minutes60 => MatchDuration.Minutes60,
                _ => MatchDuration.Unlimited
            };
            var random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            return MatchFactory.Create(roster, duration, random: random,
                dice: new BtlGame.Board.RandomDiceSource(new System.Random(random.Next())),
                cards: new RandomEventCardProvider(random.Next()));
        }

        public void BeginMatch(MatchSetupRequest request)
        {
            // Kiểm tra trước khi rời menu; không giữ trận tạm này làm nguồn state thứ hai.
            try { CreateMatch(request); }
            catch (ArgumentException error) { _menu.SetupRejected(error.Message); return; }
            _nextSetup = request;
            SceneManager.LoadScene("Gameplay");
        }

        private void Update()
        {
            if (Match == null) return;
            GameSnapshot snapshot = Match.QuerySnapshot();
            if (snapshot.Status == MatchStatus.Finished) return;
            // Popup chọn skill/bán là presentation tạm; Core tiếp tục giữ quyền kiểm tra cửa sổ kỹ năng.
            if (!_localChoicePaused) Match.AdvanceClock(TimeSpan.FromSeconds(Time.unscaledDeltaTime));
            snapshot = Match.QuerySnapshot();
            if (snapshot.Status == MatchStatus.Finished) { Refresh(); return; }
            if (snapshot.Phase == TurnPhase.Moving || _popupId != 0) { RenderHud(snapshot); return; }
            int actor = DecisionActor(snapshot);
            if (_ais.TryGetValue(actor, out AiController ai))
            {
                if (Time.unscaledTime < _nextAiAt) { RenderHud(snapshot); return; }
                _nextAiAt = Time.unscaledTime + 0.55f;
                ActionResult result = ai.Step();
                if (result != null) HandleResult(result);
            }
            else if (snapshot.Phase == TurnPhase.ResolvingLanding || snapshot.Phase == TurnPhase.AwaitingObligation ||
                snapshot.Phase == TurnPhase.AwaitingJailChoice || snapshot.Phase == TurnPhase.AwaitingRollConfirmation)
                ShowOptions(Match.QueryLegalActions(actor));
            RenderHud(Match.QuerySnapshot());
        }

        private static int DecisionActor(GameSnapshot snapshot)
            => snapshot.PendingObligations.Count > 0 ? snapshot.PendingObligations[0].PayerId : snapshot.CurrentPlayerId;

        private void Refresh()
        {
            GameSnapshot snapshot = Match.QuerySnapshot();
            if (!_board.IsAnimating)
            {
                _board.ApplySnapshot(new BoardPresentationSnapshot(
                    snapshot.Players.Select(p => new BoardPlayerVisualState(p.Id, p.SeatIndex, p.Position,
                        p.Status == PlayerStatus.Active)).ToArray(),
                    snapshot.Properties.Select(p => new BoardPropertyVisualState(p.TileId, p.OwnerId,
                        p.Level.HasValue ? (PropertyVisualLevel?)(int)p.Level.Value : null)).ToArray()));
            }
            RenderHud(snapshot);
            if (snapshot.Status == MatchStatus.Finished && !_resultShown)
            {
                _resultShown = true;
                _popupId = ++_nextPopupId;
                _ui.ShowDecision(new DecisionPrompt(_popupId, snapshot.CurrentPlayerId, snapshot.Revision,
                    DecisionPromptKind.Result, "Kết thúc trận", "Người thắng: " + string.Join(", ",
                        snapshot.Players.Where(p => snapshot.WinnerPlayerIds.Contains(p.Id)).Select(p => p.DisplayName)) +
                        "\nKết quả: " + snapshot.Result,
                    new[] { new DecisionOption("menu", "Về Main Menu") }));
            }
        }

        private void RenderHud(GameSnapshot snapshot)
        {
            int actor = DecisionActor(snapshot);
            PlayerSnapshot player = snapshot.Players.First(p => p.Id == actor);
            bool human = player.Controller == ControllerKind.Human && snapshot.Status == MatchStatus.Running && _popupId == 0;
            IReadOnlyList<LegalAction> legal = Match.QueryLegalActions(actor);
            var lines = snapshot.Players.Select(p => p.DisplayName + " — " + RoleCatalog.Get(p.Role).DisplayName +
                ": " + Money(p.Cash) + (p.Status == PlayerStatus.Bankrupt ? " [Phá sản]" : "")).ToList();
            lines.AddRange(snapshot.Properties.Where(p => p.OwnerId == actor).Select(p =>
                BoardCatalog.Get(p.TileId).Name + " — " + (p.Level?.ToString() ?? "Bãi biển")));
            var status = new List<string> { "Ô " + player.Position + ": " + BoardCatalog.Get(player.Position).Name,
                "Hồi chiêu: " + Math.Max(0, player.ActiveReadyAfterCompletedTurns - player.CompletedPersonalTurns) +
                " lượt; Ultimate: " + (player.UltimateUsed ? "đã dùng" : "còn"),
                "Thẻ giữ: " + string.Join(", ", player.HeldCards) };
            if (player.IsJailed) status.Add("Đang ở tù — lượt " + player.JailTurnsStarted + "/3");
            int? preview = Match.QueryNextCard(actor);
            if (preview.HasValue) status.Add("Thẻ kế tiếp: " + preview.Value);
            if (snapshot.PendingRuleDecisionId != null) status.Add("Bị chặn: " + snapshot.PendingRuleDecisionId);
            var dice = snapshot.PendingDiceRoll ?? snapshot.LastDiceRoll;
            string clock = snapshot.ClockRemainingTicks == long.MaxValue ? "Vô hạn" :
                TimeSpan.FromTicks(snapshot.ClockRemainingTicks).ToString(@"mm\:ss");
            _ui.Render(new GameHUDSnapshot(actor, snapshot.Revision, player.DisplayName, player.Cash,
                "Lượt: " + snapshot.Players.First(p => p.Id == snapshot.CurrentPlayerId).DisplayName, dice.HasValue ?
                    "Xúc xắc: " + dice.Value.First + " + " + dice.Value.Second : "Xúc xắc: —", clock, lines, status,
                human && legal.Any(a => a.Kind == GameActionKind.RollDice || a.Kind == GameActionKind.EndTurn),
                human && legal.Any(IsSkill), human && legal.Any(a => a.Kind == GameActionKind.SellProperty))
                .WithDetails(snapshot.Players.Select(p => new HudPlayerInfo(p.Id, p.SeatIndex, p.DisplayName, p.Cash,
                        RoleCatalog.Get(p.Role).DisplayName, p.Status == PlayerStatus.Bankrupt)).ToArray(),
                    snapshot.CurrentPlayerId, snapshot.CompletedRounds + 1,
                    snapshot.ClockRemainingTicks == long.MaxValue ? (long?)null : snapshot.ClockRemainingTicks / TimeSpan.TicksPerSecond,
                    dice.HasValue ? (int?)dice.Value.First : null, dice.HasValue ? (int?)dice.Value.Second : null,
                    snapshot.Status == MatchStatus.Finished));
            _ui.SetRollLabel(snapshot.Phase == TurnPhase.EndTurn ? "KẾT THÚC LƯỢT" : "TUNG XÚC XẮC");
        }

        private static string Money(long amount) => amount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + "đ";
        private static bool IsSkill(LegalAction a) => a.Kind == GameActionKind.ActivateSkill ||
            a.Kind == GameActionKind.UseUltimate || a.Kind == GameActionKind.RerollDice;

        private void OnHudAction(GameplayUiAction request)
        {
            if (_popupId != 0 || request.ExpectedRevision != Match.QuerySnapshot().Revision) return;
            var actions = Match.QueryLegalActions(request.ActorId);
            if (request.Kind == GameplayUiActionKind.Roll)
            {
                LegalAction action = actions.FirstOrDefault(a => a.Kind == GameActionKind.RollDice || a.Kind == GameActionKind.EndTurn);
                if (action != null) HandleResult(Match.SubmitAction(action.ToCommand()));
            }
            else ShowOptions(actions.Where(a => request.Kind == GameplayUiActionKind.UseActiveSkill ? IsSkill(a) :
                a.Kind == GameActionKind.SellProperty).ToArray(), true);
        }

        private void ShowOptions(IReadOnlyList<LegalAction> actions, bool localSelection = false)
        {
            if (_popupId != 0 || actions.Count == 0) return;
            _offered.Clear();
            var options = new List<DecisionOption>();
            for (int i = 0; i < actions.Count; i++)
            {
                LegalAction action = actions[i];
                string key = i.ToString(CultureInfo.InvariantCulture);
                _offered.Add(key, action);
                string label = action.Label;
                if (action.Kind == GameActionKind.UseUltimate) label += " (1 lần/trận)";
                if (action.Cost > 0) label += " — " + Money(action.Cost);
                options.Add(new DecisionOption(key, label));
            }
            if (localSelection) options.Add(new DecisionOption("cancel", "Quay lại"));
            _localChoicePaused = localSelection;
            GameSnapshot snapshot = Match.QuerySnapshot();
            int actor = actions[0].ActorId;
            PlayerSnapshot player = snapshot.Players.First(p => p.Id == actor);
            _popupId = ++_nextPopupId;
            bool card = snapshot.PendingCardId.HasValue && actions.Any(a => a.Kind == GameActionKind.CardChoice);
            DecisionPromptKind kind = card ? DecisionPromptKind.EventCard : DecisionPromptKind.General;
            string message = player.DisplayName + " — " + RoleCatalog.Get(player.Role).DisplayName + "\nTiền mặt: " + Money(player.Cash);
            ObligationSnapshot obligation = snapshot.PendingObligations.FirstOrDefault(o => o.PayerId == actor);
            if (obligation != null) message += "\nPhải trả: " + Money(obligation.PayableAmount) + " (" + obligation.Kind + ")";
            if (localSelection) message += "\n" + RoleCatalog.Get(player.Role).ActiveDescription + "\n" + RoleCatalog.Get(player.Role).UltimateDescription;
            var prompt = new DecisionPrompt(_popupId, actor, snapshot.Revision, kind, "Chọn hành động", message,
                options, snapshot.CurrentActionId, snapshot.PendingCardId?.ToString());
            if (card) _ui.ShowCard((EventCardId)snapshot.PendingCardId.Value, prompt);
            else _ui.ShowDecision(prompt);
        }

        private void OnDecision(DecisionSubmission decision)
            => SubmitOption(decision.DecisionId, decision.ActorId, decision.ExpectedRevision, decision.OptionId);

        private void OnCardIntent(EventCardIntent intent)
        {
            GameSnapshot snapshot = Match.QuerySnapshot();
            if (intent.ActionId != snapshot.CurrentActionId || (int)intent.CardId != snapshot.PendingCardId) return;
            SubmitOption(_popupId, intent.ActorId, intent.ExpectedRevision, intent.SelectionId);
        }

        private void SubmitOption(long popupId, int actor, long revision, string optionId)
        {
            if (_popupId == 0 || popupId != _popupId) return;
            if (optionId == "menu" && Match.QuerySnapshot().Status == MatchStatus.Finished)
            { SceneManager.LoadScene("MainMenu"); return; }
            LegalAction selected = null;
            if (optionId != "cancel" && (!_offered.TryGetValue(optionId, out selected) || selected.ActorId != actor ||
                selected.Revision != revision)) return;
            _ui.CompleteDecision(_popupId);
            _popupId = 0;
            _localChoicePaused = false;
            _offered.Clear();
            if (selected != null) HandleResult(Match.SubmitDecision(selected.ToCommand()));
            else Refresh();
        }

        private void HandleResult(ActionResult result)
        {
            if (result.Status == ActionStatus.Rejected) Debug.LogWarning(result.Message, this);
            if (result.Movement != null)
            {
                GameSnapshot snapshot = Match.QuerySnapshot();
                _board.PlayMovement(new MovementPresentation(snapshot.CurrentPlayerId, snapshot.CurrentActionId,
                    snapshot.CurrentLandingId, result.Movement.Path));
                RenderHud(snapshot);
            }
            else Refresh();
        }

        private void OnPresentationCompleted(long actionId, long landingId)
        {
            if (landingId != Match.QuerySnapshot().CurrentLandingId) return;
            HandleResult(Match.CompletePresentation(actionId));
        }

        private void OnDestroy()
        {
            if (_menu != null) _menu.SetupRequested -= BeginMatch;
            if (_ui != null)
            {
                _ui.ActionRequested -= OnHudAction;
                _ui.DecisionRequested -= OnDecision;
                _ui.CardIntentRequested -= OnCardIntent;
            }
            if (_board != null) _board.PresentationCompleted -= OnPresentationCompleted;
        }
    }
}
