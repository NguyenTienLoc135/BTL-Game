using System;
using BtlGame.Core;

namespace BtlGame.AI
{
    /// <summary>
    /// Thực hiện tối đa một lựa chọn mỗi lần gọi. Unity adapter quyết định nhịp gọi;
    /// controller không hoàn tất animation hoặc chạy vòng lặp hết trận.
    /// </summary>
    public sealed class AiController
    {
        private readonly MatchCoordinator _core;
        private readonly HeuristicAi _policy;
        public int PlayerId { get; }

        public AiController(MatchCoordinator core, int playerId, HeuristicAi policy = null)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
            bool found = false;
            foreach (PlayerSnapshot player in core.QuerySnapshot().Players)
                if (player.Id == playerId) { found = true; break; }
            if (!found) throw new ArgumentException("Người chơi AI không có trong trận.", nameof(playerId));
            PlayerId = playerId;
            _policy = policy ?? new HeuristicAi();
        }

        public ActionResult Step()
        {
            GameSnapshot snapshot = _core.QuerySnapshot(PlayerId);
            var legalActions = _core.QueryLegalActions(PlayerId);
            GameCommand command = _policy.Choose(snapshot, PlayerId, legalActions);
            return command == null ? null : _core.SubmitAction(command);
        }
    }
}
