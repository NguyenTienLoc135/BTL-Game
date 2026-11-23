using System;
using BTLGame.Cards;

namespace BTLGame.UI
{
    /// <summary>
    /// Hợp đồng giữa UnityMatchHost (Core) và giao diện trận đấu. Có hai bản cài đặt: GameplayUIController (TMP, prefab của Codex)
    /// và PresentationGameplayUi (giao diện mới). Host chỉ biết interface này nên đổi giao diện không đụng tới Core.
    /// View chỉ hiển thị snapshot và phát ý định; không ghi trạng thái trận.
    /// </summary>
    public interface IGameplayUi
    {
        event Action<GameplayUiAction> ActionRequested;
        event Action<DecisionSubmission> DecisionRequested;
        event Action<EventCardIntent> CardIntentRequested;

        void Render(GameHUDSnapshot snapshot);
        void SetRollLabel(string label);
        void ShowDecision(DecisionPrompt prompt);
        void ShowCard(EventCardId id, DecisionPrompt prompt);

        /// <summary>Đóng popup quyết định có id này; trả về false nếu không còn popup tương ứng.</summary>
        bool CompleteDecision(long decisionId);
    }
}
