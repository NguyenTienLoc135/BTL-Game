using System;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.Presentation
{
    /// <summary>
    /// Popup dùng chung cho mọi màn (MainMenu, Gameplay): bật/tắt, nút X và bấm nền mờ để đóng.
    /// Gắn vào object gốc của popup (do UiKit.Popup tạo, gán sẵn field).
    /// </summary>
    public sealed class UiPopup : MonoBehaviour
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _backdropButton;

        public event Action Closed;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);
            if (_backdropButton != null) _backdropButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            gameObject.SetActive(false);
            Closed?.Invoke();
        }
    }
}
