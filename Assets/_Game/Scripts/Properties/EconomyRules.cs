using System;
using System.Collections.Generic;
using System.Numerics;
using BtlGame.Core;

namespace BtlGame.Properties
{
    public readonly struct MoneyFraction
    {
        public long Numerator { get; }
        public long Denominator { get; }
        public MoneyFraction(long numerator, long denominator)
        {
            if (denominator <= 0) throw new ArgumentOutOfRangeException(nameof(denominator));
            Numerator = numerator;
            Denominator = denominator;
        }
        public long FloorToLong()
        {
            BigInteger value = (BigInteger)Numerator / Denominator;
            if (value < long.MinValue || value > long.MaxValue) throw new OverflowException();
            return (long)value;
        }
    }

    public sealed class RentQuote
    {
        public long BaseRent { get; }
        public int ModifierBasisPoints { get; }
        public bool Doubled { get; }
        public long FinalRent { get; }
        internal RentQuote(long baseRent, int modifierBasisPoints, bool doubled, long finalRent)
        {
            BaseRent = baseRent;
            ModifierBasisPoints = modifierBasisPoints;
            Doubled = doubled;
            FinalRent = finalRent;
        }
    }

    public static class EconomyRules
    {
        private static readonly int[] PriceNumerators = { 2, 3, 4, 5, 6 };
        private static readonly int[] PriceDenominators = { 5, 5, 5, 5, 5 };
        private static readonly int[] RentNumerators = { 3, 5, 8, 12, 16 };
        private static readonly int[] RentDenominators = { 2, 2, 2, 2, 2 };

        public static long ListedPrice(int tileId, PropertyLevel level)
        {
            TileDefinition tile = BoardCatalog.Get(tileId);
            if (tile.Kind != TileKind.City) throw new ArgumentException("Chỉ thành phố có cấp công trình.", nameof(tileId));
            int index = (int)level - 1;
            if (index < 0 || index >= PriceNumerators.Length) throw new ArgumentOutOfRangeException(nameof(level));
            return new MoneyFraction(checked(tile.ReferencePrice * PriceNumerators[index]), PriceDenominators[index]).FloorToLong();
        }

        public static long ListedPrice(PropertyState property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            TileDefinition tile = BoardCatalog.Get(property.TileId);
            if (property.OwnerId == null) return 0;
            return tile.Kind == TileKind.Beach
                ? tile.BeachPrice
                : property.Level.HasValue ? ListedPrice(property.TileId, property.Level.Value) : 0;
        }

        public static RentQuote CityRent(int tileId, PropertyLevel level, int additiveModifierBasisPoints,
            bool doubled, bool exclusiveCountry, bool fixedFestival, bool temporaryFestival,
            bool rentBoost, bool engineeringWeakness, bool powerOutage = false)
        {
            TileDefinition tile = BoardCatalog.Get(tileId);
            if (tile.Kind != TileKind.City) throw new ArgumentException("Ô không phải thành phố.", nameof(tileId));
            int index = (int)level - 1;
            if (index < 0 || index >= RentNumerators.Length) throw new ArgumentOutOfRangeException(nameof(level));

            // Base = reference * 5% * level multiplier. Percentage effects are additive on this base.
            long baseNumerator = checked(tile.ReferencePrice * RentNumerators[index] * 5);
            long baseDenominator = checked(100L * RentDenominators[index]);
            long baseRent = new MoneyFraction(baseNumerator, baseDenominator).FloorToLong();
            int modifiers = additiveModifierBasisPoints;
            if (exclusiveCountry) modifiers += 2_000;
            if (fixedFestival) modifiers += 5_000;
            if (temporaryFestival) modifiers += 5_000;
            if (rentBoost) modifiers += 3_000;
            if (engineeringWeakness) modifiers -= 1_000;
            if (modifiers < -10_000) throw new InvalidOperationException("Tổng modifier không thể làm tiền thuê âm.");
            long numerator = checked(baseNumerator * (10_000L + modifiers) * (doubled ? 2 : 1));
            long denominator = checked(baseDenominator * 10_000L);
            long final = powerOutage ? 0 : new MoneyFraction(numerator, denominator).FloorToLong();
            return new RentQuote(baseRent, modifiers, doubled, final);
        }

        public static RentQuote BeachRent(int ownedBeachCount, int modifierBasisPoints = 0, bool doubled = false)
        {
            if (ownedBeachCount < 1 || ownedBeachCount > 3)
                throw new ArgumentOutOfRangeException(nameof(ownedBeachCount), "Bốn bãi biển kết thúc trận trước khi tính thuê.");
            if (modifierBasisPoints < -10_000) throw new InvalidOperationException("Tổng modifier không thể làm tiền thuê âm.");
            long baseRent = ownedBeachCount * 50_000L;
            long numerator = checked(baseRent * (10_000L + modifierBasisPoints) * (doubled ? 2 : 1));
            long amount = new MoneyFraction(numerator, 10_000).FloorToLong();
            return new RentQuote(baseRent, modifierBasisPoints, doubled, amount);
        }

        public static MoneyFraction TaxAssessment(long cash, int ownerId, IEnumerable<PropertyState> properties,
            int reductionBasisPoints = 0)
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));
            if (cash < 0) throw new ArgumentOutOfRangeException(nameof(cash));
            if (reductionBasisPoints < 0 || reductionBasisPoints > 10_000)
                throw new ArgumentOutOfRangeException(nameof(reductionBasisPoints));
            long total = cash;
            foreach (PropertyState property in properties)
                if (property != null && property.OwnerId == ownerId)
                    total = checked(total + ListedPrice(property));
            return new MoneyFraction(checked(total * 20 * (10_000L - reductionBasisPoints)), 1_000_000);
        }

        public static long RentToPay(long grossRent, int reductionBasisPoints)
        {
            if (grossRent < 0) throw new ArgumentOutOfRangeException(nameof(grossRent));
            return new MoneyFraction(checked(grossRent * (10_000L - reductionBasisPoints)), 10_000).FloorToLong();
        }

        public static long BankSaleValue(long actualInvestment) => actualInvestment / 2;

        public static IReadOnlyList<long> AllocateInvestment(long actualPaid, PropertyLevel targetLevel)
        {
            if (actualPaid < 0) throw new ArgumentOutOfRangeException(nameof(actualPaid));
            int tierCount = (int)targetLevel;
            if (tierCount < 1 || tierCount > 5) throw new ArgumentOutOfRangeException(nameof(targetLevel));
            int[] weights = targetLevel == PropertyLevel.Stake ? new[] { 1 } : targetLevel == PropertyLevel.House1
                ? new[] { 2, 1 } : targetLevel == PropertyLevel.House2 ? new[] { 2, 1, 1 }
                : targetLevel == PropertyLevel.House3 ? new[] { 2, 1, 1, 1 } : new[] { 2, 1, 1, 1, 1 };
            int denominator = 0;
            foreach (int weight in weights) denominator += weight;
            var shares = new long[5];
            var remainders = new int[tierCount];
            long allocated = 0;
            for (int i = 0; i < tierCount; i++)
            {
                long numerator = checked(actualPaid * weights[i]);
                shares[i] = numerator / denominator;
                remainders[i] = (int)(numerator % denominator);
                allocated += shares[i];
            }

            int remainderCount = checked((int)(actualPaid - allocated));
            var order = new List<int>(tierCount);
            for (int i = 0; i < tierCount; i++) order.Add(i);
            order.Sort((left, right) => remainders[right].CompareTo(remainders[left]));
            if (remainderCount > 0 && remainderCount < order.Count && remainders[order[remainderCount - 1]] == remainders[order[remainderCount]])
                throw new InvalidOperationException("Q13/P01: cần quyết định thứ tự phá hòa trước khi phân bổ vốn.");
            for (int i = 0; i < remainderCount; i++) shares[order[i]]++;
            return Array.AsReadOnly(shares);
        }
    }
}
