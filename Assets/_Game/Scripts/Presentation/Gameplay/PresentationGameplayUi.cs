using System;
using System.Collections.Generic;
using BTLGame.Cards;
using BTLGame.UI;
using UnityEngine;

namespace BTLGame.Presentation
{
    /// <summary>
    /// Cài đặt <see cref="IGameplayUi"/> bằng giao diện mới: HUD (GameplayHudView), popup quyết định và popup thông tin.
    /// Là cầu nối presentation: nhận GameHUDSnapshot từ UnityMatchHost, phát ý định (GameplayUiAction / DecisionSubmission) về host;
    /// KHÔNG ghi trạng thái trận và không tính luật. Gắn vào object "GameplayUi" (do GameplaySceneBuilder tạo và gán field).
    /// </summary>
    public sealed class PresentationGameplayUi : MonoBehaviour, IGameplayUi
    {
        [SerializeField] private GameplayHudView _hud;
        [SerializeField] private DecisionPopupView _decision;
        [SerializeField] private InfoPopupView _assets;
        [SerializeField] private InfoPopupView _status;
        [SerializeField] private EventCardCatalog _cardCatalog;

        public event Action<GameplayUiAction> ActionRequested;
        public event Action<DecisionSubmission> DecisionRequested;
        public event Action<EventCardIntent> CardIntentRequested;

        private GameHUDSnapshot _last;

        private void OnEnable()
        {
            if (_hud != null)
            {
                _hud.RollRequested += OnRoll;
                _hud.SkillsRequested += OnSkill;
                _hud.AssetsRequested += OnAssets;
                _hud.CardsRequested += OnStatus;
            }

            if (_decision != null) _decision.Submitted += OnDecisionSubmitted;
            if (_assets != null) _assets.ActionClicked += OnSell;
        }

        private void OnDisable()
        {
            if (_hud != null)
            {
                _hud.RollRequested -= OnRoll;
                _hud.SkillsRequested -= OnSkill;
                _hud.AssetsRequested -= OnAssets;
                _hud.CardsRequested -= OnStatus;
            }

            if (_decision != null) _decision.Submitted -= OnDecisionSubmitted;
            if (_assets != null) _assets.ActionClicked -= OnSell;
        }

        public void Render(GameHUDSnapshot snapshot)
        {
            _last = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            if (_hud != null) _hud.Apply(snapshot);
        }

        public void SetRollLabel(string label)
        {
            if (_hud != null) _hud.SetRollLabel(label);
        }

        public void ShowDecision(DecisionPrompt prompt)
        {
            if (_decision == null) throw new InvalidOperationException("DecisionPopupView chưa được gán.");
            _decision.Show(prompt);
        }

        public void ShowCard(EventCardId id, DecisionPrompt prompt)
        {
            if (prompt == null) throw new ArgumentNullException(nameof(prompt));
            if (_decision == null) throw new InvalidOperationException("DecisionPopupView chưa được gán.");

            // Thẻ sự kiện dùng cùng popup quyết định; tiêu đề/mô tả lấy từ EventCardCatalog (dữ liệu của nhóm C).
            if (_cardCatalog != null && _cardCatalog.TryGet(id, out EventCardData card))
            {
                prompt = new DecisionPrompt(prompt.DecisionId, prompt.ActorId, prompt.ExpectedRevision, prompt.Kind,
                    "Thẻ sự kiện: " + card.Title, card.Description + "\n\n" + prompt.Message, prompt.Options,
                    prompt.ActionId, prompt.ContextId);
            }

            _decision.Show(prompt);
        }

        public bool CompleteDecision(long decisionId)
        {
            return _decision != null && _decision.Complete(decisionId);
        }

        // ---------------------------------------------------------------- ý định từ HUD

        private void OnRoll() => Request(GameplayUiActionKind.Roll);
        private void OnSkill() => Request(GameplayUiActionKind.UseActiveSkill);
        private void OnSell() => Request(GameplayUiActionKind.OpenSellSelection);

        private void Request(GameplayUiActionKind kind)
        {
            if (_last == null)
            {
                return;
            }

            ActionRequested?.Invoke(new GameplayUiAction(kind, _last.ActorId, _last.Revision));
        }

        private void OnAssets()
        {
            if (_assets != null && _last != null)
            {
                _assets.Show(_last.PropertyLines, "Chưa có dữ liệu trận.", _last.CanSell);
            }
        }

        private void OnStatus()
        {
            if (_status != null && _last != null)
            {
                _status.Show(_last.StatusLines, "Chưa có dữ liệu trận.", false);
            }
        }

        // Thẻ sự kiện cũng đi qua popup quyết định chung; host nhận qua DecisionRequested như mọi lựa chọn khác.
        private void OnDecisionSubmitted(DecisionSubmission submission) => DecisionRequested?.Invoke(submission);
    }
}
