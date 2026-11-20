using System;
using System.Collections.Generic;
using BtlGame.Core;
using UnityEngine;

namespace BTLGame.UI
{
    /// <summary>
    /// Đổ cấu hình trận từ MainMenu (MatchSetupRequest) lên HUD: số người, tên, thời gian, vòng.
    /// Tiền/đồng hồ/người đi đầu lấy từ snapshot của Core (MatchFactory) nên không lặp lại số luật ở UI.
    /// Mở thẳng Scene Gameplay (không qua MainMenu) thì giữ dữ liệu mẫu trong Scene.
    /// TODO(A): khi UnityMatchHost/Core chính thức chạy trong Gameplay, bỏ coordinator tạm này và đọc snapshot của trận thật.
    /// </summary>
    public sealed class GameplayHudBinder : MonoBehaviour
    {
        [SerializeField] private GameplayHudView _hud;

        private void Start()
        {
            if (_hud == null || !MatchSetupHandoff.TryTake(out var request))
            {
                return;
            }

            // Coordinator chỉ để lấy snapshot khởi tạo (đúng luật Setup); không giữ, không chạy lượt.
            var snapshot = MatchFactory.Create(request.Roster, request.Duration).QuerySnapshot();

            var players = new List<HudPlayer>(snapshot.Players.Count);
            int current = -1;
            for (int i = 0; i < snapshot.Players.Count; i++)
            {
                var p = snapshot.Players[i];
                players.Add(new HudPlayer(p.DisplayName, p.Cash, _hud.SeatColor(p.SeatIndex)));
                if (p.Id == snapshot.CurrentPlayerId)
                {
                    current = i;
                }
            }

            _hud.SetPlayers(players);
            _hud.SetCurrentPlayer(current);
            _hud.SetRound((int)(snapshot.CompletedRounds + 1));

            long ticks = snapshot.ClockRemainingTicks;
            _hud.SetClock(ticks == long.MaxValue ? (int?)null : (int)(ticks / TimeSpan.TicksPerSecond));
        }
    }
}
