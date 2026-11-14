using System;
using BtlGame.Core;

namespace BtlGame.Properties
{
    public static class EconomyRules
    {
        private static readonly long[] PriceRatios = { 40, 60, 80, 100, 120 };
        private static readonly long[] RentMultipliersNumerator = { 3, 5, 8, 12, 16 };

        public static long GetListedPrice(BoardTile tile, CityLevel level)
        {
            if (tile == null || tile.Type != TileType.City)
            {
                throw new ArgumentException("Listed city price requires a city tile.");
            }

            return tile.ReferenceHouse3Price * PriceRatios[(int)level] / 100;
        }

        public static long GetPurchasePrice(BoardTile tile, CityLevel level)
        {
            if (tile.Type == TileType.Beach)
            {
                throw new ArgumentException("Beach has no city level.");
            }

            return GetListedPrice(tile, level);
        }

        public static long GetBaseRent(BoardTile tile, CityLevel level)
        {
            if (tile == null || tile.Type != TileType.City)
            {
                throw new ArgumentException("City rent requires a city tile.");
            }

            long numerator = tile.ReferenceHouse3Price * 5 * RentMultipliersNumerator[(int)level];
            return numerator / 200;
        }

        public static long GetBeachRent(int ownedBeachCount)
        {
            if (ownedBeachCount < 1 || ownedBeachCount > 3)
            {
                throw new ArgumentOutOfRangeException("ownedBeachCount");
            }

            return ownedBeachCount * 50000L;
        }

        public static RentQuote QuoteCityRent(
            BoardTile tile,
            CityLevel level,
            int modifierPercent,
            bool doubled)
        {
            long baseRent = GetBaseRent(tile, level);
            long adjusted = baseRent * (100L + modifierPercent) / 100L;
            if (doubled)
            {
                adjusted *= 2;
            }

            return new RentQuote(baseRent, modifierPercent, doubled, adjusted);
        }

        public static long GetTax(long cash, long listedPropertyValue)
        {
            if (cash < 0 || listedPropertyValue < 0)
            {
                throw new ArgumentOutOfRangeException("Tax inputs cannot be negative.");
            }

            return (cash + listedPropertyValue) / 5;
        }

        public static long GetSaleValue(long actualInvestment)
        {
            if (actualInvestment < 0)
            {
                throw new ArgumentOutOfRangeException("actualInvestment");
            }

            return actualInvestment / 2;
        }

        public static long GetForcedPurchasePrice(BoardTile tile, CityLevel level, int percent)
        {
            if (percent <= 0)
            {
                throw new ArgumentOutOfRangeException("percent");
            }

            return GetListedPrice(tile, level) * percent / 100L;
        }

        public static long[] AllocateInvestment(long amount, BoardTile tile, CityLevel level)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException("amount");
            }

            int tierCount = (int)level + 1;
            long[] result = new long[5];
            long totalWeight = PriceRatios[(int)level];
            long allocated = 0;
            long[] remainders = new long[tierCount];

            for (int i = 0; i < tierCount; i++)
            {
                long weight = PriceRatios[i] - (i == 0 ? 0 : PriceRatios[i - 1]);
                long numerator = amount * weight;
                result[i] = numerator / totalWeight;
                remainders[i] = numerator % totalWeight;
                allocated += result[i];
            }

            long remaining = amount - allocated;
            while (remaining > 0)
            {
                int best = 0;
                for (int i = 1; i < tierCount; i++)
                {
                    if (remainders[i] > remainders[best])
                    {
                        best = i;
                    }
                }

                result[best]++;
                remainders[best] = -1;
                remaining--;
            }

            return result;
        }
    }
}
