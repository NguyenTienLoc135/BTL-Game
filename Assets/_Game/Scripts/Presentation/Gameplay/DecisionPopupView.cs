using System;
using System.Collections.Generic;
using BTLGame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.Presentation
{
    /// <summary>
    /// Popup quyết định (mua/bỏ qua, thuê, thuế, tù, thẻ sự kiện, chọn mục tiêu, kết quả...). Hiển thị DecisionPrompt do Core/host tạo,
    /// phát DecisionSubmission khi bấm một lựa chọn. Không tự đóng bằng X/nền: chỉ đóng khi host gọi <see cref="Complete"/>
    /// (Q20: popup quyết định phải được hoàn tất). Danh sách lựa chọn cuộn được để chứa nhiều tài sản khi thanh lý.
    /// </summary>
    public sealed class DecisionPopupView : MonoBehaviour
    {
        [SerializeField] private UiPopup _popup;
        [SerializeField] private Text _title;
        [SerializeField] private Text _message;
        [SerializeField] private Button[] _optionButtons = new Button[0];
        [SerializeField] private Text[] _optionLabels = new Text[0];
        [SerializeField] private ScrollRect _scroll;

        public event Action<DecisionSubmission> Submitted;

        private DecisionPrompt _current;
        private readonly List<Button> _dynamicButtons = new List<Button>();
        private readonly List<Text> _dynamicLabels = new List<Text>();
        private int _serializedButtonCount = -1;
        private int _serializedLabelCount = -1;

        public long CurrentDecisionId => _current == null ? 0 : _current.DecisionId;

        public void Show(DecisionPrompt prompt)
        {
            if (prompt == null)
            {
                throw new ArgumentNullException(nameof(prompt));
            }

            _current = prompt;
            if (_title != null) _title.text = prompt.Title;
            if (_message != null) _message.text = prompt.Message;

            ClearDynamicOptions();
            EnsureOptionSlots(prompt.Options.Count);
            int count = Mathf.Min(prompt.Options.Count, _optionButtons.Length);

            for (int i = 0; i < _optionButtons.Length; i++)
            {
                var button = _optionButtons[i];
                bool used = i < count;
                button.gameObject.SetActive(used);
                if (!used)
                {
                    continue;
                }

                var option = prompt.Options[i];
                if (i < _optionLabels.Length && _optionLabels[i] != null)
                {
                    _optionLabels[i].text = option.Label;
                }

                button.interactable = option.IsLegal;
                button.onClick.RemoveAllListeners();
                string optionId = option.Id;
                button.onClick.AddListener(() => Submit(optionId));
            }

            if (_scroll != null)
            {
                _scroll.verticalNormalizedPosition = 1f;
            }

            if (_popup != null)
            {
                _popup.Open();
            }
        }

        /// <summary>Đóng popup nếu đúng id đang mở; trả về false nếu không khớp.</summary>
        public bool Complete(long decisionId)
        {
            if (_current == null || _current.DecisionId != decisionId)
            {
                return false;
            }

            _current = null;
            if (_popup != null)
            {
                _popup.Close();
            }

            return true;
        }

        private void Submit(string optionId)
        {
            if (_current == null)
            {
                return;
            }

            var p = _current;
            Submitted?.Invoke(new DecisionSubmission(p.DecisionId, p.ActorId, p.ExpectedRevision, p.ActionId, p.Kind, p.ContextId, optionId));
        }

        private void EnsureOptionSlots(int requiredCount)
        {
            if (_serializedButtonCount < 0)
            {
                _serializedButtonCount = _optionButtons?.Length ?? 0;
                _serializedLabelCount = _optionLabels?.Length ?? 0;
            }
            if (requiredCount <= _optionButtons.Length || _serializedButtonCount == 0)
            {
                return;
            }

            Button template = _serializedButtonCount > 0 ? _optionButtons[0] : null;
            Transform parent = _scroll != null && _scroll.content != null
                ? _scroll.content
                : template != null ? template.transform.parent : null;
            if (template == null || parent == null)
            {
                return;
            }

            var buttons = new List<Button>(_optionButtons);
            var labels = new List<Text>(_optionLabels);
            while (buttons.Count < requiredCount)
            {
                Button clone = Instantiate(template, parent);
                clone.onClick.RemoveAllListeners();
                clone.gameObject.SetActive(false);
                Text cloneLabel = clone.GetComponentInChildren<Text>(true);
                if (cloneLabel != null)
                {
                    cloneLabel.text = string.Empty;
                }
                buttons.Add(clone);
                labels.Add(cloneLabel);
                _dynamicButtons.Add(clone);
                _dynamicLabels.Add(cloneLabel);
            }
            _optionButtons = buttons.ToArray();
            _optionLabels = labels.ToArray();
        }

        private void ClearDynamicOptions()
        {
            if (_serializedButtonCount < 0)
            {
                return;
            }
            foreach (Button button in _dynamicButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            _dynamicButtons.Clear();
            _dynamicLabels.Clear();
            if (_optionButtons.Length > _serializedButtonCount)
            {
                var buttons = new Button[_serializedButtonCount];
                Array.Copy(_optionButtons, buttons, _serializedButtonCount);
                _optionButtons = buttons;
            }
            if (_optionLabels.Length > _serializedLabelCount)
            {
                var labels = new Text[_serializedLabelCount];
                Array.Copy(_optionLabels, labels, _serializedLabelCount);
                _optionLabels = labels;
            }
        }
    }
}
