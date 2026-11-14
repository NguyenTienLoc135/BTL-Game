using System;
using System.Collections.Generic;
using BtlGame.Core;

namespace BtlGame.Properties
{
    public readonly struct RentQuote
    {
        public RentQuote(long baseRent, long finalRent)
        {
            BaseRent = baseRent;
            FinalRent = finalRent;
        }

        public long BaseRent { get; }
        public long FinalRent { get; }
    }

    public static class EconomyCalculator
    {
        private static readonly long[] LevelPriceNumerators = { 40, 60, 80, 100, 120 };
        private static readonly long[] RentNumerators = { 15, 25, 40, 60, 80 };

        public static long ListedPrice(BoardTile tile, PropertyLevel level)
        {
            if (tile.Type == TileType.Beach)
                return tile.BeachPrice;
            if (tile.Type != TileType.City || level < PropertyLevel.Stake || level > PropertyLevel.Hotel)
                throw new ArgumentException("A listed property level is required.", nameof(level));

            return tile.House3Price * LevelPriceNumerators[(int)level] / 100;
        }

        public static long PurchasePrice(BoardTile tile, PropertyLevel level)
        {
            if (tile.Type == TileType.Beach)
            {
                if (level != PropertyLevel.Stake)
                    throw new InvalidOperationException("A beach has no building levels.");
                return tile.BeachPrice;
            }

            if (tile.Type != TileType.City || level < PropertyLevel.Stake || level > PropertyLevel.House3)
                throw new InvalidOperationException("A first purchase cannot be a hotel.");
            return ListedPrice(tile, level);
        }

        public static long UpgradePrice(BoardTile tile, PropertyLevel current, PropertyLevel target)
        {
            if (tile.Type != TileType.City || current < PropertyLevel.Stake || target <= current || target > PropertyLevel.Hotel)
                throw new InvalidOperationException("Invalid city upgrade.");
            return ListedPrice(tile, target) - ListedPrice(tile, current);
        }

        public static RentQuote CalculateRent(
            BoardTile tile,
            PropertyLevel level,
            int ownedBeaches,
            bool countryMonopoly,
            bool fixedFestival,
            bool temporaryFestival,
            int additivePercent,
            bool doubled)
        {
            if (tile.Type == TileType.Beach)
            {
                if (ownedBeaches < 1 || ownedBeaches > 3)
                    throw new ArgumentOutOfRangeException(nameof(ownedBeaches));
                long beachRent = ownedBeaches * 50000L;
                return new RentQuote(beachRent, ApplyMultiplier(beachRent, additivePercent, doubled));
            }

            if (tile.Type != TileType.City || level < PropertyLevel.Stake || level > PropertyLevel.Hotel)
                throw new InvalidOperationException("Rent requires an owned property.");

            long baseRent = tile.House3Price * RentNumerators[(int)level] / 100;
            int percent = additivePercent;
            if (countryMonopoly)
                percent += 20;
            if (fixedFestival)
                percent += 50;
            if (temporaryFestival)
                percent += 50;

            return new RentQuote(baseRent, ApplyMultiplier(baseRent, percent, doubled));
        }

        public static long TaxAtAssessment(long cash, IEnumerable<long> listedPropertyPrices)
        {
            if (cash < 0)
                throw new ArgumentOutOfRangeException(nameof(cash));

            long assetTotal = 0;
            foreach (long price in listedPropertyPrices)
            {
                if (price < 0)
                    throw new ArgumentOutOfRangeException(nameof(listedPropertyPrices));
                assetTotal = checked(assetTotal + price);
            }

            return checked((cash + assetTotal) * 20 / 100);
        }

        public static long ApplyLawyerCardTaxReduction(long assessedTax)
        {
            if (assessedTax < 0)
                throw new ArgumentOutOfRangeException(nameof(assessedTax));
            return assessedTax * 75 / 100;
        }

        public static long LiquidationValue(PropertyState property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));
            return property.Type == TileType.Beach
                ? property.BeachInvestment / 2
                : Sum(property.InvestedByTier) / 2;
        }

        public static long DamageRefund(PropertyState property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));
            return Sum(property.InvestedByTier);
        }

        public static long[] AllocateInvestment(long amount, PropertyLevel target)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (target < PropertyLevel.Stake || target > PropertyLevel.Hotel)
                throw new ArgumentOutOfRangeException(nameof(target));

            int tierCount = (int)target + 1;
            long[] result = new long[GameConstants.InvestmentTierCount];
            long denominator = 0;
            for (int i = 0; i < tierCount; i++)
                denominator += LevelPriceNumerators[i] - (i == 0 ? 0 : LevelPriceNumerators[i - 1]);

            long allocated = 0;
            long[] remainders = new long[tierCount];
            for (int i = 0; i < tierCount; i++)
            {
                long weight = LevelPriceNumerators[i] - (i == 0 ? 0 : LevelPriceNumerators[i - 1]);
                long product = checked(amount * weight);
                result[i] = product / denominator;
                remainders[i] = product % denominator;
                allocated += result[i];
            }

            while (allocated < amount)
            {
                int best = 0;
                for (int i = 1; i < tierCount; i++)
                {
                    if (remainders[i] > remainders[best])
                        best = i;
                }

                result[best]++;
                remainders[best] = -1;
                allocated++;
            }

            return result;
        }

        private static long ApplyMultiplier(long baseAmount, int additivePercent, bool doubled)
        {
            if (additivePercent < -100)
                throw new ArgumentOutOfRangeException(nameof(additivePercent));
            long adjusted = checked(baseAmount * (100L + additivePercent) / 100L);
            return doubled ? checked(adjusted * 2) : adjusted;
        }

        private static long Sum(IEnumerable<long> values)
        {
            long result = 0;
            foreach (long value in values)
                result = checked(result + value);
            return result;
        }
    }
}
