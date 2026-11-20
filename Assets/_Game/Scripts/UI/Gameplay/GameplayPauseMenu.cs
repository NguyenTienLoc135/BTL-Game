using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BTLGame.UI
{
    /// <summary>
    /// Menu tạm dừng trong Gameplay: Tiếp tục / Cài đặt / Về menu chính. Mở bằng nút Menu của HUD.
    /// Không ghi state trận. TODO(A): khi Core có đồng hồ/pausing thì gọi lệnh tạm dừng tại đây.
    /// </summary>
    public sealed class GameplayPauseMenu : MonoBehaviour
    {
        private const string MainMenuScene = "MainMenu";

        [SerializeField] private GameplayHudView _hud;
        [SerializeField] private UiPopup _pausePopup;
        [SerializeField] private UiPopup _settingsPopup;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _mainMenuButton;

        private void Awake()
        {
            if (_hud != null) _hud.MenuRequested += OpenPause;
            if (_resumeButton != null) _resumeButton.onClick.AddListener(() => _pausePopup.Close());
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OpenSettings);
            if (_mainMenuButton != null) _mainMenuButton.onClick.AddListener(() => SceneManager.LoadScene(MainMenuScene));
        }

        private void Start()
        {
            if (_pausePopup != null) _pausePopup.Close();
            if (_settingsPopup != null) _settingsPopup.Close();
        }

        private void OnDestroy()
        {
            if (_hud != null) _hud.MenuRequested -= OpenPause;
        }

        private void OpenPause()
        {
            if (_pausePopup != null) _pausePopup.Open();
        }

        private void OpenSettings()
        {
            _pausePopup.Close();
            _settingsPopup.Open();
        }
    }
}
