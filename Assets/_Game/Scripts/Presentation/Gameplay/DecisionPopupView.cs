using System;
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

            int count = Mathf.Min(prompt.Options.Count, _optionButtons.Length);
            if (prompt.Options.Count > _optionButtons.Length)
            {
                Debug.LogWarning("DecisionPopupView: có " + prompt.Options.Count + " lựa chọn nhưng chỉ hiển thị " + _optionButtons.Length + ".", this);
            }

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
    }
}
