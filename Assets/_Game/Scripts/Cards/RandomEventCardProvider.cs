using System;
using System.Collections.Generic;
using BtlGame.Core;

namespace BTLGame.Cards
{
    /// <summary>Q02: mỗi lần rút có xác suất đều trong 14 loại, có hoàn lại.
    /// Seed và ordinal chỉ giữ preview nhất quán; query không tiến trình tự rút của Core.</summary>
    public sealed class RandomEventCardProvider : ICardActionProvider
    {
        private readonly int _seed;

        public RandomEventCardProvider(int? seed = null) => _seed = seed ?? new Random().Next();

        public int? PeekNextCard(GameSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            long ordinal = snapshot.CardDrawOrdinal;
            int drawSeed = unchecked((_seed * 397) ^ (int)ordinal ^ (int)(ordinal >> 32));
            return new Random(drawSeed).Next(1, 15);
        }

        public IReadOnlyList<CardActionOption> QueryOptions(GameSnapshot snapshot, int actorId)
        {
            var options = new List<CardActionOption>();
            foreach (var choice in BuildChoices(snapshot, actorId)) options.Add(choice.Option);
            return options.AsReadOnly();
        }

        public CardEffectPlan Plan(GameSnapshot snapshot, int actorId, int choiceId)
        {
            foreach (var choice in BuildChoices(snapshot, actorId))
                if (choice.Option.ChoiceId == choiceId) return choice.Plan;
            throw new ArgumentException("Lựa chọn thẻ không còn hợp lệ.", nameof(choiceId));
        }

        private static string LevelName(PropertyLevel? level)
            => level == PropertyLevel.Stake ? "Cọc" : level == PropertyLevel.House1 ? "Nhà 1" : level == PropertyLevel.House2 ? "Nhà 2"
                : level == PropertyLevel.House3 ? "Nhà 3" : level == PropertyLevel.Hotel ? "Khách sạn" : "—";

        private static List<(CardActionOption Option, CardEffectPlan Plan)> BuildChoices(GameSnapshot snapshot, int actorId)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var result = new List<(CardActionOption, CardEffectPlan)>();
            if (snapshot.CurrentPlayerId != actorId || !snapshot.PendingCardId.HasValue ||
                snapshot.Status != MatchStatus.Running) return result;
            int card = snapshot.PendingCardId.Value;
            void Add(int id, string label, CardEffectKind kind, int? tile = null, int? player = null, long amount = 0)
                => result.Add((new CardActionOption(id, label, tile, player),
                    new CardEffectPlan(card, kind, tile, player, amount)));

            switch (card)
            {
                case 1: Add(1, "Nhận 300.000đ", CardEffectKind.BankCredit, amount: 300000); break;
                case 3: case 4: case 5: case 8:
                    Add(1, "Nhận thẻ", CardEffectKind.StoreCard); break;
                case 6: Add(1, "Xác nhận nghĩa vụ thuế", CardEffectKind.Tax); break;
                case 7: Add(1, "Đi tiến đến Nhà tù", CardEffectKind.SendJail); break;
                case 11: Add(1, "Đi tiến đến ô Dịch chuyển", CardEffectKind.GoTeleport); break;
                case 2: case 9: case 13:
                    CardEffectKind attack = card == 2 ? CardEffectKind.Earthquake :
                        card == 9 ? CardEffectKind.Destroy : CardEffectKind.PowerOutage;
                    foreach (PropertySnapshot property in snapshot.Properties)
                    {
                        if (BoardCatalog.Get(property.TileId).Kind != TileKind.City || !property.OwnerId.HasValue ||
                            property.OwnerId == actorId || property.Level == PropertyLevel.Hotel ||
                            (card == 2 && property.Level == PropertyLevel.Stake)) continue;
                        // Nút phải nói rõ SẼ LÀM GÌ với thành phố nào, của ai (không chỉ mỗi tên thành phố).
                        string verb = card == 2 ? "Hạ 1 cấp công trình" : card == 9 ? "Phá tài sản" : "Cúp điện";
                        string owner = string.Empty;
                        foreach (PlayerSnapshot other in snapshot.Players) if (other.Id == property.OwnerId) owner = other.DisplayName;
                        Add(property.TileId + 1, verb + " ở " + BoardCatalog.Get(property.TileId).Name + " (của " + owner + ", " + LevelName(property.Level) + ")",
                            attack, tile: property.TileId);
                    }
                    if (result.Count == 0) Add(0, "Không có mục tiêu hợp lệ — tiếp tục", CardEffectKind.None);
                    break;
                case 12:
                    long cash = 0;
                    foreach (PlayerSnapshot player in snapshot.Players) if (player.Id == actorId) cash = player.Cash;
                    if (cash >= 200000)
                        foreach (PropertySnapshot property in snapshot.Properties)
                            if (property.OwnerId == actorId && BoardCatalog.Get(property.TileId).Kind == TileKind.City)
                                Add(property.TileId + 1, "Tổ chức tại " + BoardCatalog.Get(property.TileId).Name,
                                    CardEffectKind.Festival, tile: property.TileId, amount: 200000);
                    Add(0, "Bỏ qua lễ hội", CardEffectKind.None);
                    break;
                case 14:
                    foreach (PlayerSnapshot player in snapshot.Players)
                        if (player.Id != actorId && player.Status == PlayerStatus.Active)
                            Add(player.Id + 1, "Tặng 200.000đ cho " + player.DisplayName,
                                CardEffectKind.Gift, player: player.Id, amount: 200000);
                    break;
                // Thẻ 10 do Core tự mở batch ngay khi rút, không thu tiền lần nữa từ provider.
            }
            return result;
        }
    }
}
