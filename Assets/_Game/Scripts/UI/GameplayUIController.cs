using System;
using BTLGame.Cards;
using UnityEngine;

namespace BTLGame.UI
{
    public sealed class GameplayUIController : MonoBehaviour
    {
        [SerializeField] private GameHUDController _hud;
        [SerializeField] private DecisionPopupUI _decisionPopup;
        [SerializeField] private EventCardManager _eventCardManager;

        public event Action<GameplayUiAction> ActionRequested;
        public event Action<DecisionSubmission> DecisionRequested;
        public event Action<EventCardIntent> CardIntentRequested;
        public event Action<long, int, bool> DecisionPauseChanged;

        public void Configure(GameHUDController hud, DecisionPopupUI decisionPopup, EventCardManager cards)
        {
            _hud = hud;
            _decisionPopup = decisionPopup;
            _eventCardManager = cards;
        }

        private void OnEnable()
        {
            if (_hud != null) _hud.ActionRequested += ForwardAction;
            if (_decisionPopup != null)
            {
                _decisionPopup.Submitted += HandleDecisionSubmitted;
                _decisionPopup.DecisionOpened += HandleDecisionOpened;
                _decisionPopup.DecisionClosed += HandleDecisionClosed;
            }
            if (_eventCardManager != null) _eventCardManager.IntentRequested += ForwardCardIntent;
        }

        private void OnDisable()
        {
            if (_hud != null) _hud.ActionRequested -= ForwardAction;
            if (_decisionPopup != null)
            {
                _decisionPopup.Submitted -= HandleDecisionSubmitted;
                _decisionPopup.DecisionOpened -= HandleDecisionOpened;
                _decisionPopup.DecisionClosed -= HandleDecisionClosed;
            }
            if (_eventCardManager != null) _eventCardManager.IntentRequested -= ForwardCardIntent;
        }

        public void Render(GameHUDSnapshot snapshot)
        {
            if (_hud == null) throw new InvalidOperationException("GameHUDController chưa được gán.");
            _hud.Render(snapshot);
        }

        public void ShowDecision(DecisionPrompt prompt)
        {
            if (_decisionPopup == null) throw new InvalidOperationException("DecisionPopupUI chưa được gán.");
            _decisionPopup.Show(prompt);
        }

        public void ShowCard(EventCardId id, DecisionPrompt prompt)
        {
            if (prompt == null) throw new ArgumentNullException(nameof(prompt));
            if (_eventCardManager == null) throw new InvalidOperationException("EventCardManager chưa được gán.");
            _eventCardManager.Present(id, prompt, prompt.ActionId);
        }

        public bool CompleteDecision(long decisionId)
        {
            return _decisionPopup != null && _decisionPopup.CompleteDecision(decisionId);
        }

        private void ForwardAction(GameplayUiAction action) => ActionRequested?.Invoke(action);

        private void HandleDecisionSubmitted(DecisionSubmission submission)
        {
            if (submission.Kind == DecisionPromptKind.EventCard || submission.Kind == DecisionPromptKind.EventCardTargetSelection)
            {
                if (!int.TryParse(submission.ContextId, out int rawCardId) ||
                    !Enum.IsDefined(typeof(EventCardId), rawCardId) || _eventCardManager == null)
                {
                    Debug.LogError("Prompt thẻ thiếu CardId hợp lệ hoặc EventCardManager.", this);
                    return;
                }
                _eventCardManager.RequestIntent((EventCardId)rawCardId, submission.ActorId,
                    submission.ActionId, submission.ExpectedRevision, submission.OptionId);
                return;
            }
            DecisionRequested?.Invoke(submission);
        }

        private void HandleDecisionOpened(long decisionId, int actorId) => DecisionPauseChanged?.Invoke(decisionId, actorId, true);
        private void HandleDecisionClosed(long decisionId, int actorId) => DecisionPauseChanged?.Invoke(decisionId, actorId, false);
        private void ForwardCardIntent(EventCardIntent intent) => CardIntentRequested?.Invoke(intent);
    }
}
