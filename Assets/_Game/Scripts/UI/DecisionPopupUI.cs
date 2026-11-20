using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.UI
{
    public sealed class DecisionPopupUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private Transform _optionsRoot;
        [SerializeField] private Button _optionButtonPrefab;

        private readonly List<Button> _createdButtons = new List<Button>();
        private DecisionPrompt _prompt;
        private bool _submitted;

        public event Action<DecisionSubmission> Submitted;
        public event Action<long, int> DecisionOpened;
        public event Action<long, int> DecisionClosed;

        public void Configure(TMP_Text title, TMP_Text message, Transform optionsRoot, Button optionButtonPrefab)
        {
            _titleText = title;
            _messageText = message;
            _optionsRoot = optionsRoot;
            _optionButtonPrefab = optionButtonPrefab;
        }

        public void Show(DecisionPrompt prompt, string titleOverride = null, string messageOverride = null)
        {
            if (prompt == null) throw new ArgumentNullException(nameof(prompt));
            if (_titleText == null || _messageText == null || _optionsRoot == null || _optionButtonPrefab == null)
                throw new InvalidOperationException("DecisionPopupUI chưa được bind đầy đủ trong Inspector.");
            if (prompt.Options.Count == 0)
                throw new ArgumentException("DecisionPopupUI chỉ hiện quyết định có ít nhất một lựa chọn.", nameof(prompt));
            if (_prompt != null)
                throw new InvalidOperationException("Không thể mở hai popup quyết định cùng lúc.");

            ClearOptions();
            _prompt = prompt;
            _submitted = false;
            _titleText.text = titleOverride ?? prompt.Title;
            _messageText.text = messageOverride ?? prompt.Message;
            for (int i = 0; i < prompt.Options.Count; i++)
            {
                DecisionOption option = prompt.Options[i];
                Button button = Instantiate(_optionButtonPrefab, _optionsRoot);
                button.gameObject.SetActive(true);
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = option.IsLegal || string.IsNullOrEmpty(option.DisabledReason)
                        ? option.Label
                        : option.Label + " — " + option.DisabledReason;
                button.interactable = option.IsLegal;
                string optionId = option.Id;
                button.onClick.AddListener(() => Submit(optionId));
                _createdButtons.Add(button);
            }
            gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_optionsRoot);
            ScrollRect scroll = _optionsRoot.GetComponentInParent<ScrollRect>();
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
            DecisionOpened?.Invoke(prompt.DecisionId, prompt.ActorId);
        }

        public bool CompleteDecision(long decisionId)
        {
            if (_prompt == null || _prompt.DecisionId != decisionId) return false;
            int actorId = _prompt.ActorId;
            _prompt = null;
            _submitted = true;
            gameObject.SetActive(false);
            ClearOptions();
            DecisionClosed?.Invoke(decisionId, actorId);
            return true;
        }

        private void Submit(string optionId)
        {
            if (_submitted || _prompt == null) return;
            _submitted = true;
            DecisionPrompt prompt = _prompt;
            foreach (Button button in _createdButtons)
                if (button != null) button.interactable = false;
            Submitted?.Invoke(new DecisionSubmission(prompt.DecisionId, prompt.ActorId,
                prompt.ExpectedRevision, prompt.ActionId, prompt.Kind, prompt.ContextId, optionId));
        }

        private void ClearOptions()
        {
            foreach (Button button in _createdButtons)
            {
                if (button == null) continue;
                button.onClick.RemoveAllListeners();
                Destroy(button.gameObject);
            }
            _createdButtons.Clear();
        }
    }
}
