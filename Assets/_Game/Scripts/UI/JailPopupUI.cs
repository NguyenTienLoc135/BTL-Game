using System;
using UnityEngine;

namespace BTLGame.UI
{
    public sealed class JailPopupUI : MonoBehaviour
    {
        [SerializeField] private DecisionPopupUI _popup;
        public event Action<DecisionSubmission> DecisionSubmitted;
        private void OnEnable() { if (_popup != null) _popup.Submitted += Forward; }
        private void OnDisable() { if (_popup != null) _popup.Submitted -= Forward; }

        public void Show(DecisionPrompt prompt)
        {
            if (prompt == null || prompt.Kind != DecisionPromptKind.Jail)
                throw new ArgumentException("Prompt không phải quyết định trong tù.", nameof(prompt));
            if (_popup == null) throw new InvalidOperationException("Chưa gán DecisionPopupUI.");
            _popup.Show(prompt);
        }

        public bool CompleteDecision(long decisionId) => _popup != null && _popup.CompleteDecision(decisionId);
        private void Forward(DecisionSubmission submission) => DecisionSubmitted?.Invoke(submission);
    }
}
