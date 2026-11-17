using System;
using System.Collections.Generic;
using BtlGame.Core;

namespace BtlGame.AI
{
    /// <summary>
    /// Chỉ chọn trong danh sách Core đã cấp. Điểm ưu tiên và quỹ dự phòng là tuning AI,
    /// không phải luật gameplay; Core kiểm tra lại command trước khi thay đổi state.
    /// </summary>
    public sealed class HeuristicAi
    {
        public long CashReserve { get; }

        public HeuristicAi(long cashReserve = 50_000)
        {
            if (cashReserve < 0) throw new ArgumentOutOfRangeException(nameof(cashReserve));
            CashReserve = cashReserve;
        }

        public GameCommand Choose(GameSnapshot snapshot, int playerId, IReadOnlyList<LegalAction> legalActions)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (legalActions == null) throw new ArgumentNullException(nameof(legalActions));
            PlayerSnapshot player = FindPlayer(snapshot, playerId);
            if (player == null || player.Status != PlayerStatus.Active || snapshot.Status != MatchStatus.Running)
                return null;

            var candidates = new List<LegalAction>();
            foreach (LegalAction action in legalActions)
                if (action != null && action.ActorId == playerId && action.Revision == snapshot.Revision &&
                    action.DecisionId == snapshot.DecisionId && action.Kind != GameActionKind.CompletePresentation)
                    candidates.Add(action);

            ObligationSnapshot obligation = snapshot.PendingObligation;
            if (obligation != null && obligation.PayerId == playerId)
                return ChoosePayment(snapshot, player, obligation, candidates)?.ToCommand();

            LegalAction best = null;
            long bestScore = long.MinValue;
            foreach (LegalAction action in candidates)
            {
                long score = Score(snapshot, player, action);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = action;
                }
            }
            return best?.ToCommand();
        }

        private static LegalAction ChoosePayment(GameSnapshot snapshot, PlayerSnapshot player,
            ObligationSnapshot obligation, List<LegalAction> candidates)
        {
            // Không phí thẻ/skill nếu miễn có sẵn đã đưa nghĩa vụ về 0.
            if (obligation.PayableAmount > 0)
            {
                if (player.Role == RoleId.Lawyer)
                    foreach (LegalAction action in candidates)
                        if (action.Kind == GameActionKind.ActivateSkill) return action;
                foreach (LegalAction action in candidates)
                    if (action.Kind == GameActionKind.UseTaxCard || action.Kind == GameActionKind.UseRentCard)
                        return action;
            }
            foreach (LegalAction action in candidates)
                if (action.Kind == GameActionKind.SettleObligation) return action;

            LegalAction sufficient = null;
            LegalAction largest = null;
            long sufficientRefund = long.MaxValue;
            long largestRefund = -1;
            long missing = Math.Max(0, obligation.PayableAmount - player.Cash);
            foreach (LegalAction action in candidates)
            {
                if (action.Kind != GameActionKind.LiquidateProperty || !action.TargetTileId.HasValue) continue;
                PropertySnapshot property = FindProperty(snapshot, action.TargetTileId.Value);
                if (property == null || property.OwnerId != player.Id) continue;
                long refund = Investment(property) / 2;
                // Bán tài sản nhỏ nhất đủ bù thiếu; nếu chưa có tài sản nào đủ thì tiến tới
                // thanh toán bằng khoản thu lớn nhất. Không tự đặt thứ tự bắt buộc cho Human.
                if (refund >= missing && refund < sufficientRefund)
                {
                    sufficient = action;
                    sufficientRefund = refund;
                }
                if (refund > largestRefund)
                {
                    largest = action;
                    largestRefund = refund;
                }
            }
            if (sufficient != null || largest != null) return sufficient ?? largest;
            foreach (LegalAction action in candidates)
                if (action.Kind == GameActionKind.DeclareBankruptcy) return action;
            return null;
        }

        private long Score(GameSnapshot snapshot, PlayerSnapshot player, LegalAction action)
        {
            switch (action.Kind)
            {
                case GameActionKind.BuyCity:
                case GameActionKind.BuyBeach:
                case GameActionKind.ForceBuyCity:
                    if (action.Cost > player.Cash || !action.TargetTileId.HasValue) return long.MinValue;
                    long acquisition = AcquisitionScore(snapshot, player.Id, action.TargetTileId.Value);
                    if (acquisition >= 10_000) return acquisition; // Thắng tức thời/độc quyền đáng đổi dự phòng.
                    return player.Cash - action.Cost >= CashReserve ? acquisition - action.Cost / 10_000 : -100;
                case GameActionKind.UpgradeCity:
                    return action.Cost <= player.Cash && player.Cash - action.Cost >= CashReserve
                        ? 500 + (int)(action.TargetLevel ?? PropertyLevel.Stake) * 10 : -100;
                case GameActionKind.ActivateSkill:
                    return ScoreActive(snapshot, player, action);
                case GameActionKind.UseUltimate:
                    return ScoreUltimate(snapshot, player, action);
                case GameActionKind.RerollDice:
                    if (!snapshot.PendingDiceRoll.HasValue) return long.MinValue;
                    // Khi thử ra tù, mục tiêu trước hết là có đôi để thoát. Kết quả đã
                    // ra đôi phải giữ lại, dù ô sẽ đến sau khi ra tù có rủi ro thuê.
                    if (snapshot.PendingJailTrial)
                        return snapshot.PendingDiceRoll.Value.IsDouble ? -100 : 1_200;
                    int landing = (player.Position + snapshot.PendingDiceRoll.Value.Total) % 32;
                    return LandingDanger(snapshot, player.Id, landing) > 0 ? 1_200 : -100;
                case GameActionKind.ConfirmRoll: return 200;
                case GameActionKind.JailCard: return 1_000;
                case GameActionKind.JailPay:
                    return player.Cash >= action.Cost && player.Cash - action.Cost >= CashReserve ? 400 : -100;
                case GameActionKind.JailRoll: return 300;
                case GameActionKind.Teleport:
                    if (!action.TargetTileId.HasValue || action.Cost > player.Cash) return long.MinValue;
                    int target = action.TargetTileId.Value;
                    long value = DestinationValue(snapshot, player, target);
                    return target < player.Position ? value + 220 : value - 20;
                case GameActionKind.OrganizeFestival:
                    return action.Cost <= player.Cash && player.Cash - action.Cost >= CashReserve ? 150 : -100;
                case GameActionKind.CardChoice:
                    // Core/C đã kiểm tra lựa chọn. Một nghĩa vụ thẻ có thể lớn hơn tiền mặt
                    // và dẫn tới thanh lý; AI không tự loại lựa chọn đó theo quỹ dự phòng.
                    return 250 + (action.TargetTileId.HasValue
                        ? Math.Max(0, DestinationValue(snapshot, player, action.TargetTileId.Value)) : 0);
                case GameActionKind.SellProperty:
                    return player.Cash < CashReserve && action.TargetTileId.HasValue ? 120 : -100;
                case GameActionKind.RollDice: return 100;
                case GameActionKind.SkipLanding:
                case GameActionKind.EndTurn: return 0;
                default: return long.MinValue;
            }
        }

        private long ScoreActive(GameSnapshot snapshot, PlayerSnapshot player, LegalAction action)
        {
            switch (player.Role)
            {
                case RoleId.Investor: return 900;
                case RoleId.Engineer: return 1_100 + (action.TargetTileId ?? 0);
                case RoleId.Entrepreneur: return OwnsProperty(snapshot, player.Id) ? 900 : -100;
                case RoleId.Banker:
                    long assets = 0;
                    foreach (PropertySnapshot property in snapshot.Properties)
                        if (property.OwnerId == player.Id) assets += Investment(property) / 2;
                    return player.Cash < CashReserve && assets >= 110_000 - player.Cash ? 600 : -100;
                default: return -100;
            }
        }

        private long ScoreUltimate(GameSnapshot snapshot, PlayerSnapshot player, LegalAction action)
        {
            switch (player.Role)
            {
                case RoleId.Banker: return 2_000;
                case RoleId.Investor:
                    if (!action.TargetTileId.HasValue || action.Cost > player.Cash) return long.MinValue;
                    long acquisition = AcquisitionScore(snapshot, player.Id, action.TargetTileId.Value);
                    return acquisition >= 10_000 || player.Cash - action.Cost >= CashReserve ? acquisition : -100;
                case RoleId.Engineer:
                    return action.Cost <= player.Cash && player.Cash - action.Cost >= CashReserve ? 1_200 : -100;
                case RoleId.Entrepreneur: return OwnsProperty(snapshot, player.Id) ? 850 : -100;
                case RoleId.Lawyer:
                    if (player.Cash >= CashReserve) return -100;
                    for (int steps = 2; steps <= 12; steps++)
                        if (LandingDanger(snapshot, player.Id, (player.Position + steps) % 32) > 0) return 700;
                    return -100;
                case RoleId.Strategist:
                    return action.TargetTileId.HasValue ? DestinationValue(snapshot, player, action.TargetTileId.Value) : -100;
                default: return -100;
            }
        }

        private static long DestinationValue(GameSnapshot snapshot, PlayerSnapshot player, int tileId)
        {
            TileDefinition tile = BoardCatalog.Get(tileId);
            if (tile.Kind == TileKind.Jail || tile.Kind == TileKind.Tax) return -300;
            if (tile.Kind == TileKind.Start) return 250;
            PropertySnapshot property = FindProperty(snapshot, tileId);
            if (property == null) return 40;
            if (property.OwnerId == player.Id) return 160;
            if (property.OwnerId.HasValue) return -200;
            long acquisition = AcquisitionScore(snapshot, player.Id, tileId);
            return acquisition / 5;
        }

        private static long AcquisitionScore(GameSnapshot snapshot, int playerId, int tileId)
        {
            TileDefinition tile = BoardCatalog.Get(tileId);
            if (tile.Kind == TileKind.Beach)
            {
                int beaches = 0;
                foreach (PropertySnapshot property in snapshot.Properties)
                    if (property.OwnerId == playerId && BoardCatalog.Get(property.TileId).Kind == TileKind.Beach) beaches++;
                return beaches == 3 ? 100_000 : 700 + beaches * 150;
            }
            if (tile.Kind != TileKind.City) return 0;
            int ownedInCountry = 0;
            int countrySize = 0;
            int completedCountries = 0;
            foreach (CountryId country in Enum.GetValues(typeof(CountryId)))
            {
                bool complete = true;
                bool any = false;
                foreach (TileDefinition candidate in BoardCatalog.Tiles)
                {
                    if (candidate.Country != country) continue;
                    any = true;
                    PropertySnapshot property = FindProperty(snapshot, candidate.Id);
                    bool owned = property != null && property.OwnerId == playerId;
                    if (!owned) complete = false;
                    if (country == tile.Country)
                    {
                        countrySize++;
                        if (owned) ownedInCountry++;
                    }
                }
                if (any && complete) completedCountries++;
            }
            bool completesCountry = ownedInCountry + 1 == countrySize;
            if (completesCountry && completedCountries >= 2) return 100_000;
            return completesCountry ? 10_000 : 600 + ownedInCountry * 100;
        }

        private static int LandingDanger(GameSnapshot snapshot, int playerId, int tileId)
        {
            TileKind kind = BoardCatalog.Get(tileId).Kind;
            if (kind == TileKind.Jail || kind == TileKind.Tax) return 1;
            PropertySnapshot property = FindProperty(snapshot, tileId);
            return property != null && property.OwnerId.HasValue && property.OwnerId != playerId ? 1 : 0;
        }

        private static bool OwnsProperty(GameSnapshot snapshot, int playerId)
        {
            foreach (PropertySnapshot property in snapshot.Properties)
                if (property.OwnerId == playerId) return true;
            return false;
        }

        private static long Investment(PropertySnapshot property)
        {
            long total = property.BeachInvestment;
            foreach (long amount in property.InvestedByLevel) total += amount;
            return total;
        }

        private static PlayerSnapshot FindPlayer(GameSnapshot snapshot, int playerId)
        {
            foreach (PlayerSnapshot player in snapshot.Players)
                if (player.Id == playerId) return player;
            return null;
        }

        private static PropertySnapshot FindProperty(GameSnapshot snapshot, int tileId)
        {
            foreach (PropertySnapshot property in snapshot.Properties)
                if (property.TileId == tileId) return property;
            return null;
        }
    }
}
