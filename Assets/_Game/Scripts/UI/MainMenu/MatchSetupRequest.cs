using System.Collections.Generic;
using BtlGame.Core;

namespace BTLGame.UI
{
    /// <summary>
    /// Kết quả màn thiết lập trận: chế độ, thời gian và danh sách người chơi (ghế 0..n-1, nghề, Người/AI).
    /// Dùng thẳng kiểu của Core (PlayerSetup, MatchDuration) để A tạo trận qua MatchFactory.Create.
    /// Người đi đầu KHÔNG chọn ở đây: Core chọn ngẫu nhiên (Q03), sau đó theo thứ tự ghế cố định.
    /// </summary>
    public sealed class MatchSetupRequest
    {
        public MainMenuMode Mode { get; }
        public MatchDuration Duration { get; }
        public IReadOnlyList<PlayerSetup> Roster { get; }

        public MatchSetupRequest(MainMenuMode mode, MatchDuration duration, IReadOnlyList<PlayerSetup> roster)
        {
            Mode = mode;
            Duration = duration;
            Roster = roster;
        }
    }

    /// <summary>
    /// Chuyển MatchSetupRequest từ MainMenu sang Scene Gameplay (một ô duy nhất, lấy ra là xóa).
    /// Tạm thời cho tới khi nhóm A chốt cách truyền MatchSetup chính thức; không giữ trạng thái trận.
    /// </summary>
    public static class MatchSetupHandoff
    {
        private static MatchSetupRequest _pending;

        public static void Set(MatchSetupRequest request)
        {
            _pending = request;
        }

        public static bool TryTake(out MatchSetupRequest request)
        {
            request = _pending;
            _pending = null;
            return request != null;
        }
    }
}
