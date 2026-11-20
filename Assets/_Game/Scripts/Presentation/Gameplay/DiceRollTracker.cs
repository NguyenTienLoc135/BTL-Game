namespace BTLGame.Presentation
{
    /// <summary>
    /// Nhận biết một lần tung xúc xắc MỚI từ các bản chụp HUD. Cặp xúc xắc cuối cùng được giữ nguyên qua các lượt nên không thể
    /// chỉ dựa vào "có giá trị": một lần tung mới = (xúc xắc 1, xúc xắc 2, người tung) khác lần trước.
    /// Bản chụp đầu tiên chỉ lấy mốc (không chiếu lại xúc xắc cũ). Plain C#, không phụ thuộc Unity.
    /// </summary>
    public sealed class DiceRollTracker
    {
        private bool _initialized;
        private int? _die1;
        private int? _die2;
        private int? _roller;

        /// <summary>true nếu bản chụp này mang một lần tung xúc xắc mới.</summary>
        public bool Observe(int? die1, int? die2, int? roller)
        {
            if (!die1.HasValue || !die2.HasValue)
            {
                _initialized = true;
                _die1 = null;
                _die2 = null;
                _roller = null;
                return false;
            }

            bool changed = _die1 != die1 || _die2 != die2 || _roller != roller;
            bool first = !_initialized;
            _initialized = true;
            _die1 = die1;
            _die2 = die2;
            _roller = roller;
            return changed && !first;
        }
    }
}
