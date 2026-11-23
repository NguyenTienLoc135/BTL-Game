using System;
using System.Collections.Generic;
using BtlGame.Board;

namespace BtlGame.Core
{
    public enum GameActionKind
    {
        RollDice, ConfirmRoll, RerollDice, CompletePresentation, SkipLanding, EndTurn,
        BuyCity, BuyBeach, UpgradeCity, ForceBuyCity, SellProperty,
        SettleObligation, LiquidateProperty, DeclareBankruptcy,
        JailRoll, JailPay, JailCard, ActivateSkill, UseUltimate,
        Teleport, OrganizeFestival, UseTaxCard, UseRentCard, CardChoice
    }

    /// <summary>Lựa chọn do Core cấp. UI/AI không cung cấp giá, modifier hoặc kết quả xúc xắc.</summary>
    public sealed class LegalAction
    {
        public GameActionKind Kind { get; }
        public int ActorId { get; }
        public int? TargetTileId { get; }
        public PropertyLevel? TargetLevel { get; }
        public long Cost { get; }
        public long Revision { get; }
        public long DecisionId { get; }
        public string Label { get; }
        public int? ChoiceId { get; }
        public int? TargetPlayerId { get; }

        internal LegalAction(GameActionKind kind, int actorId, int? targetTileId,
            PropertyLevel? targetLevel, long cost, long revision, long decisionId, string label, int? choiceId = null, int? targetPlayerId = null)
        {
            Kind = kind; ActorId = actorId; TargetTileId = targetTileId; TargetLevel = targetLevel;
            Cost = cost; Revision = revision; DecisionId = decisionId; Label = label;
            ChoiceId = choiceId;
            TargetPlayerId = targetPlayerId;
        }

        public GameCommand ToCommand() => new GameCommand(Kind, ActorId, Revision, DecisionId, TargetTileId, TargetLevel, ChoiceId, TargetPlayerId);
    }

    public sealed class GameCommand
    {
        public GameActionKind Kind { get; }
        public int ActorId { get; }
        public long ExpectedRevision { get; }
        public long DecisionId { get; }
        public int? TargetTileId { get; }
        public PropertyLevel? TargetLevel { get; }
        public int? ChoiceId { get; }
        public int? TargetPlayerId { get; }

        public GameCommand(GameActionKind kind, int actorId, long expectedRevision, long decisionId,
            int? targetTileId = null, PropertyLevel? targetLevel = null, int? choiceId = null, int? targetPlayerId = null)
        {
            Kind = kind; ActorId = actorId; ExpectedRevision = expectedRevision; DecisionId = decisionId;
            TargetTileId = targetTileId; TargetLevel = targetLevel;
            ChoiceId = choiceId;
            TargetPlayerId = targetPlayerId;
        }
    }

    public enum ActionStatus { Accepted, Rejected, Pending }

    public sealed class ActionResult
    {
        public ActionStatus Status { get; }
        public string Message { get; }
        public GameSnapshot Snapshot { get; }
        public MovementResult Movement { get; }
        public IReadOnlyList<string> Notices { get; }
        internal ActionResult(ActionStatus status, string message, GameSnapshot snapshot,
            MovementResult movement = null, IReadOnlyList<string> notices = null)
        {
            Status = status; Message = message; Snapshot = snapshot; Movement = movement;
            Notices = new List<string>(notices ?? Array.Empty<string>()).AsReadOnly();
        }
    }

    /// <summary>Gói C cung cấp nguồn xem trước; chỉ Core cho Strategist đọc qua QueryNextCard.</summary>
    public interface ICardPreviewSource
    {
        int? PeekNextCard(GameSnapshot snapshot);
    }

    internal static class ActionLabels
    {
        internal static string Describe(GameActionKind kind, int? tileId, PropertyLevel? level)
        {
            string target = tileId.HasValue ? " — " + BoardCatalog.Get(tileId.Value).Name : string.Empty;
            string tier = level == PropertyLevel.Stake ? "Cọc" : level == PropertyLevel.House1 ? "Nhà 1" :
                level == PropertyLevel.House2 ? "Nhà 2" : level == PropertyLevel.House3 ? "Nhà 3" : "Khách sạn";
            switch (kind)
            {
                case GameActionKind.RollDice: return "Gieo xúc xắc";
                case GameActionKind.ConfirmRoll: return "Chấp nhận kết quả và di chuyển";
                case GameActionKind.RerollDice: return "Gieo lại hai xúc xắc";
                case GameActionKind.EndTurn: return "Kết thúc lượt";
                case GameActionKind.BuyCity: return "Mua " + tier + target;
                case GameActionKind.BuyBeach: return "Mua bãi biển";
                case GameActionKind.UpgradeCity: return "Nâng lên " + tier + target;
                case GameActionKind.ForceBuyCity: return "Mua cưỡng chế" + target;
                case GameActionKind.SellProperty: return "Bán cho ngân hàng" + target;
                case GameActionKind.SettleObligation: return "Thanh toán";
                case GameActionKind.LiquidateProperty: return "Thanh lý" + target;
                case GameActionKind.DeclareBankruptcy: return "Xác nhận phá sản";
                case GameActionKind.JailRoll: return "Tung thử ra tù";
                case GameActionKind.JailPay: return "Trả 200.000 để ra tù";
                case GameActionKind.JailCard: return "Dùng thẻ Ra tù";
                case GameActionKind.ActivateSkill: return "Dùng kỹ năng" + target;
                case GameActionKind.UseUltimate: return "Dùng kỹ năng tối thượng" + target;
                case GameActionKind.Teleport: return "Dịch chuyển" + target;
                case GameActionKind.OrganizeFestival: return "Tổ chức lễ hội" + target;
                case GameActionKind.UseTaxCard: return "Dùng thẻ Miễn thuế";
                case GameActionKind.UseRentCard: return "Dùng thẻ Giảm tiền thuê";
                default: return "Tiếp tục";
            }
        }
    }
}
