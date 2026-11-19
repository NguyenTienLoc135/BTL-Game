using System;
using System.Collections.Generic;
using BtlGame.Core;

namespace BtlGame.Board
{
    public enum MovementSource { Dice, Teleport, Card, StrategistUltimate }
    public readonly struct DiceRoll
    {
        public int First { get; }
        public int Second { get; }
        public int Total => First + Second;
        public bool IsDouble => First == Second;

        public DiceRoll(int first, int second)
        {
            if (first < 1 || first > 6) throw new ArgumentOutOfRangeException(nameof(first));
            if (second < 1 || second > 6) throw new ArgumentOutOfRangeException(nameof(second));
            First = first;
            Second = second;
        }
    }

    public interface IDiceSource
    {
        DiceRoll Roll();
    }

    public sealed class RandomDiceSource : IDiceSource
    {
        private readonly Random _random;
        public RandomDiceSource(Random random = null) => _random = random ?? new Random();
        public DiceRoll Roll() => new DiceRoll(_random.Next(1, 7), _random.Next(1, 7));
    }

    public sealed class MovementResult
    {
        public int FromTileId { get; }
        public int ToTileId { get; }
        public int Steps { get; }
        public int StartCrossings { get; }
        public bool IsDouble { get; }
        public MovementSource Source { get; }
        public IReadOnlyList<int> Path { get; }

        internal MovementResult(int from, int to, int steps, int crossings, bool isDouble, List<int> path,
            MovementSource source = MovementSource.Dice)
        {
            FromTileId = from;
            ToTileId = to;
            Steps = steps;
            StartCrossings = crossings;
            IsDouble = isDouble;
            Source = source;
            Path = path.AsReadOnly();
        }
    }

    public static class BoardRules
    {
        public static MovementResult CalculateMovement(int fromTileId, DiceRoll roll)
            => CalculateMovement(fromTileId, roll.Total, roll.IsDouble);

        public static MovementResult CalculateMovement(int fromTileId, int steps, bool isDouble = false,
            MovementSource source = MovementSource.Dice)
        {
            BoardCatalog.Get(fromTileId);
            if (steps < 1 || steps > 31) throw new ArgumentOutOfRangeException(nameof(steps));
            var path = new List<int>(steps);
            int crossings = 0;
            int position = fromTileId;
            for (int i = 0; i < steps; i++)
            {
                position++;
                if (position == 32)
                {
                    position = 0;
                    crossings++;
                }
                path.Add(position);
            }

            return new MovementResult(fromTileId, position, steps, crossings, isDouble, path, source);
        }

        public static int CountForwardPath(int fromTileId, int toTileId)
        {
            BoardCatalog.Get(fromTileId);
            BoardCatalog.Get(toTileId);
            return (toTileId - fromTileId + 32) % 32;
        }
    }
}
