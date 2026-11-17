using System;

namespace BtlGame.Core
{
    public sealed partial class MatchCoordinator
    {
        private void RebaseEffectsAfterSourceBankruptcy(int playerId)
        {
            PlayerState source = State.GetPlayer(playerId);
            long currentRoundOffset = State.Decisions.KeepOrphanEffectsThroughCurrentRound == true ? 1L : 0L;
            foreach (TurnEffectState effect in State.ActiveEffects)
            {
                if (effect.SourcePlayerId != playerId || effect.ExpiresAfterRoundOrdinal.HasValue) continue;
                long remaining;
                if (effect.ExpiresAfterSourceTurnOrdinal.HasValue)
                {
                    remaining = Math.Max(0, effect.ExpiresAfterSourceTurnOrdinal.Value - source.CompletedPersonalTurns);
                    // Lượt kích hoạt không thuộc N lượt tiếp theo. Khi nguồn chết ngay trong
                    // lượt đó, không chuyển cả lượt kích hoạt thành một vòng thời hạn mới.
                    if (State.CurrentPlayerId == playerId &&
                        effect.ActivatedSourceTurnOrdinal == source.CompletedPersonalTurns + 1L)
                        remaining = Math.Max(0, remaining - 1L);
                }
                else if (effect.ExpiresBeforeSourceTurnOrdinal.HasValue)
                {
                    // RentDouble hết trước lượt kế tiếp: sau khi nguồn bị loại, giữ tối
                    // thiểu một vòng mới; hiệu ứng không còn sinh thuê khi nguồn mất đất.
                    remaining = Math.Max(1, effect.ExpiresBeforeSourceTurnOrdinal.Value - source.CompletedPersonalTurns - 1L);
                }
                else continue; // Coupon/lễ hội không có hạn không tự biến thành effect có hạn.
                effect.ExpiresAfterRoundOrdinal = checked(State.CompletedRounds + remaining + currentRoundOffset);
            }
        }

        private void ExpireAfterRoundEffects()
        {
            for (int i = State.ActiveEffects.Count - 1; i >= 0; i--)
            {
                TurnEffectState effect = State.ActiveEffects[i];
                if (effect.ExpiresAfterRoundOrdinal.HasValue &&
                    effect.ExpiresAfterRoundOrdinal.Value <= State.CompletedRounds)
                    State.RemoveEffect(effect);
            }
        }
    }
}
