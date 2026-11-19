using System;
using System.Collections.Generic;

namespace BtlGame.Core
{
    /// <summary>
    /// Gói C cung cấp lựa chọn cho thẻ đang xử lý. QueryOptions, Plan và PeekNextCard phải
    /// chỉ đọc: không bốc lại, tiến cursor hoặc tiêu thẻ khi Core mới hỏi/kiểm tra lệnh.
    /// Tiến trình rút và chuỗi tiếp theo phải được tích hợp riêng với trạng thái Core;
    /// interface này không chọn chính sách shuffle, rút lại hay thứ tự người trả.
    /// </summary>
    public interface ICardActionProvider : ICardPreviewSource
    {
        IReadOnlyList<CardActionOption> QueryOptions(GameSnapshot snapshot, int actorId);
        CardEffectPlan Plan(GameSnapshot snapshot, int actorId, int choiceId);
    }

    /// <summary>ID lựa chọn chỉ có nghĩa trong quyết định/revision hiện tại do Core cấp.</summary>
    public sealed class CardActionOption
    {
        public int ChoiceId { get; }
        public int? TargetTileId { get; }
        public int? TargetPlayerId { get; }
        public string Label { get; }

        public CardActionOption(int choiceId, string label, int? targetTileId = null, int? targetPlayerId = null)
        {
            if (choiceId < 0) throw new ArgumentOutOfRangeException(nameof(choiceId));
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Lựa chọn cần nhãn hiển thị.", nameof(label));
            CardIntentValidation.ValidateTargets(targetTileId, targetPlayerId);
            ChoiceId = choiceId;
            TargetTileId = targetTileId;
            TargetPlayerId = targetPlayerId;
            Label = label;
        }
    }

    public enum CardEffectKind
    {
        None, BankCredit, Tax, Earthquake, Destroy, StoreCard,
        SendJail, GoTeleport, Festival, PowerOutage, Gift
    }

    /// <summary>
    /// Ý định của module thẻ, chưa phải giao dịch đã được chấp thuận. Core phải đối chiếu
    /// SourceCardId, số tiền, actor/mục tiêu và điều kiện luật trước khi ghi state.
    /// Amount không cho UI/AI quyền tự đặt giá; thuế và các quote được Core tính lại.
    /// </summary>
    public sealed class CardEffectPlan
    {
        public int SourceCardId { get; }
        public CardEffectKind Kind { get; }
        public int? TargetTileId { get; }
        public int? TargetPlayerId { get; }
        public long Amount { get; }

        public CardEffectPlan(int sourceCardId, CardEffectKind kind, int? targetTileId = null,
            int? targetPlayerId = null, long amount = 0)
        {
            if (sourceCardId < 1 || sourceCardId > 14) throw new ArgumentOutOfRangeException(nameof(sourceCardId));
            if (!Enum.IsDefined(typeof(CardEffectKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            CardIntentValidation.ValidateTargets(targetTileId, targetPlayerId);
            SourceCardId = sourceCardId;
            Kind = kind;
            TargetTileId = targetTileId;
            TargetPlayerId = targetPlayerId;
            Amount = amount;
        }
    }

    internal static class CardIntentValidation
    {
        internal static void ValidateTargets(int? tileId, int? playerId)
        {
            if (tileId.HasValue && (tileId.Value < 0 || tileId.Value >= 32))
                throw new ArgumentOutOfRangeException(nameof(tileId));
            if (playerId.HasValue && playerId.Value < 0)
                throw new ArgumentOutOfRangeException(nameof(playerId));
        }
    }
}
