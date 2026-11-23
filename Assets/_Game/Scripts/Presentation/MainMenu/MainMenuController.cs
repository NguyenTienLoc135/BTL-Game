using System;
using BtlGame.Core;
using BTLGame.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BTLGame.Presentation
{
    /// <summary>Chế độ chơi chọn ở MainMenu. Chưa phải MatchSetup chính thức (số người/nghề/thời gian do màn kế tiếp).</summary>
    public enum MainMenuMode
    {
        VsBot,
        VsHuman
    }

    /// <summary>
    /// Điều phối MainMenu: Bắt đầu chơi → popup chọn chế độ Bot/Người; Hướng dẫn, Về trò chơi, Cài đặt, Thoát.
    /// Chỉ làm presentation; KHÔNG giữ tiền, sở hữu hay trạng thái trận. Gắn vào object "MainMenuController"
    /// (do MainMenuSceneBuilder tạo và gán field).
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Nút trên menu")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _howToButton;
        [SerializeField] private Button _aboutButton;
        [SerializeField] private Button _quitButton;

        [SerializeField] private Button _iconSettingsButton;

        [Header("Popup")]
        [SerializeField] private UiPopup _modePopup;
        [SerializeField] private UiPopup _howToPopup;
        [SerializeField] private UiPopup _aboutPopup;
        [SerializeField] private UiPopup _settingsPopup;

        [Header("Chọn chế độ")]
        [SerializeField] private Button _botModeButton;
        [SerializeField] private Button _humanModeButton;
        [Header("Thiết lập trận")]
        [SerializeField] private UiPopup _botSetupPopup;
        [SerializeField] private UiPopup _humanSetupPopup;
        [SerializeField] private MatchSetupView _botSetupView;
        [SerializeField] private MatchSetupView _humanSetupView;

        /// <summary>Phát khi người chơi chọn chế độ. Màn thiết lập tiếp theo sẽ lắng nghe sự kiện này.</summary>
        public event Action<MainMenuMode> ModeChosen;

        private const string GameplayScene = "Gameplay";

        private UiPopup[] _popups;
        private bool _isLoading;

        private void Awake()
        {
            _popups = new[] { _modePopup, _howToPopup, _aboutPopup, _settingsPopup, _botSetupPopup, _humanSetupPopup };

            foreach (var view in new[] { _botSetupView, _humanSetupView })
            {
                if (view != null)
                {
                    view.BackRequested += () => Open(_modePopup);
                    view.StartRequested += StartMatch;
                }
            }

            Bind(_startButton, () => Open(_modePopup));
            Bind(_howToButton, () => Open(_howToPopup));
            Bind(_aboutButton, () => Open(_aboutPopup));
            Bind(_iconSettingsButton, () => Open(_settingsPopup));
            Bind(_quitButton, Quit);
            Bind(_botModeButton, () => ChooseMode(MainMenuMode.VsBot));
            Bind(_humanModeButton, () => ChooseMode(MainMenuMode.VsHuman));
        }

        private void Start()
        {
            CloseAll();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseAll();
            }
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private void Open(UiPopup popup)
        {
            if (popup == null)
            {
                return;
            }

            CloseAll();
            popup.Open();
        }

        private void CloseAll()
        {
            if (_popups == null)
            {
                return;
            }

            foreach (var popup in _popups)
            {
                if (popup != null)
                {
                    popup.Close();
                }
            }
        }

        private void ChooseMode(MainMenuMode mode)
        {
            Open(mode == MainMenuMode.VsBot ? _botSetupPopup : _humanSetupPopup);
            ModeChosen?.Invoke(mode);
        }

        private void StartMatch(MatchSetupRequest request)
        {
            if (_isLoading)
            {
                return;
            }

            try
            {
                // Core kiểm tra cấu hình bằng đúng luật Setup (2–4 người, được trùng nghề, nghề hợp lệ) rồi xếp hàng cho Scene Gameplay.
                UnityMatchHost.QueueMatch(request);
            }
            catch (ArgumentException error)
            {
                Debug.LogWarning("Cấu hình trận không hợp lệ: " + error.Message, this);
                return;
            }

            _isLoading = true;
            SceneManager.LoadScene(GameplayScene);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
