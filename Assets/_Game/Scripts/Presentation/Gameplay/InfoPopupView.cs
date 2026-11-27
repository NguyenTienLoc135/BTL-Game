using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.Presentation
{
    /// <summary>
    /// Popup thông tin chỉ đọc (Tài sản, Thẻ & trạng thái) có thể kèm một nút hành động (ví dụ "Bán tài sản").
    /// Đóng bằng nút X / bấm ra ngoài (UiPopup). Không giữ trạng thái trận.
    /// </summary>
    public sealed class InfoPopupView : MonoBehaviour
    {
        [SerializeField] private UiPopup _popup;
        [SerializeField] private Text _body;
        [SerializeField] private Button _actionButton;

        /// <summary>Phát khi bấm nút hành động (nếu popup có nút).</summary>
        public event Action ActionClicked;

        private void Awake()
        {
            if (_actionButton != null)
            {
                _actionButton.onClick.AddListener(() =>
                {
                    ActionClicked?.Invoke();
                    if (_popup != null) _popup.Close();
                });
            }
        }

        public void Show(IReadOnlyList<string> lines, string emptyText, bool actionEnabled)
        {
            if (_body != null)
            {
                if (lines == null || lines.Count == 0)
                {
                    _body.text = emptyText;
                }
                else
                {
                    var sb = new StringBuilder();
                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (i > 0) sb.Append('\n');
                        sb.Append(lines[i]);
                    }
                    _body.text = sb.ToString();
                }
            }

            if (_actionButton != null)
            {
                _actionButton.interactable = actionEnabled;
            }

            if (_popup != null)
            {
                _popup.Open();
            }
        }
    }
}
