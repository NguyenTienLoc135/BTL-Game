using System;
using UnityEngine;

namespace BTLGame.UI
{
    public sealed class ObligationPopupUI : MonoBehaviour
    {
        [SerializeField] private DecisionPopupUI _popup;
        public event Action<DecisionSubmission> DecisionSubmitted;

        private void OnEnable() { if (_popup != null) _popup.Submitted += Forward; }
        private void OnDisable() { if (_popup != null) _popup.Submitted -= Forward; }

        public void Show(DecisionPrompt prompt)
        {
            if (prompt == null || (prompt.Kind != DecisionPromptKind.Rent && prompt.Kind != DecisionPromptKind.Tax && prompt.Kind != DecisionPromptKind.Liquidation))
                throw new ArgumentException("Prompt không phải nghĩa vụ/thanh lý.", nameof(prompt));
            if (_popup == null) throw new InvalidOperationException("Chưa gán DecisionPopupUI.");
            _popup.Show(prompt);
        }

        public bool CompleteDecision(long decisionId) => _popup != null && _popup.CompleteDecision(decisionId);
        private void Forward(DecisionSubmission submission) => DecisionSubmitted?.Invoke(submission);
    }
}
