using System;
using UnityEngine;

namespace BTLGame.UI
{
    public sealed class PropertyPopupUI : MonoBehaviour
    {
        [SerializeField] private DecisionPopupUI _popup;
        public event Action<DecisionSubmission> DecisionSubmitted;

        private void OnEnable()
        {
            if (_popup != null) _popup.Submitted += HandleSubmitted;
        }

        private void OnDisable()
        {
            if (_popup != null) _popup.Submitted -= HandleSubmitted;
        }

        public void ShowProperty(DecisionPrompt prompt)
        {
            if (prompt == null) throw new ArgumentNullException(nameof(prompt));
            if (prompt.Kind != DecisionPromptKind.Property && prompt.Kind != DecisionPromptKind.Rent)
                throw new ArgumentException("PropertyPopupUI chỉ nhận quyết định tài sản/thuê.", nameof(prompt));
            if (_popup == null) throw new InvalidOperationException("Chưa gán DecisionPopupUI.");
            _popup.Show(prompt);
        }

        public bool CompleteDecision(long decisionId) => _popup != null && _popup.CompleteDecision(decisionId);

        private void HandleSubmitted(DecisionSubmission submission) => DecisionSubmitted?.Invoke(submission);
    }
}
