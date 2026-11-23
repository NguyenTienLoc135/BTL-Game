using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.Presentation
{
    /// <summary>
    /// Popup Cài đặt âm thanh: ba thanh trượt (chung / nhạc nền / hiệu ứng), hai công tắc BẬT/TẮT độc lập
    /// (nhạc nền, hiệu ứng), nút Khôi phục mặc định và nút Lưu. Chỉnh chỉ có tác dụng nghe thử; chỉ nút Lưu mới ghi vào
    /// <see cref="AudioSettingsStore"/>. Đóng popup mà không Lưu thì hoàn về giá trị đã lưu.
    /// Không đụng tới trạng thái trận. Gắn vào object gốc của popup (do SettingsPanelBuilder tạo, gán field).
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Text _masterValue;
        [SerializeField] private Text _musicValue;
        [SerializeField] private Text _sfxValue;
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private GameObject _musicSwitchOn;
        [SerializeField] private GameObject _musicSwitchOff;
        [SerializeField] private Toggle _sfxToggle;
        [SerializeField] private GameObject _sfxSwitchOn;
        [SerializeField] private GameObject _sfxSwitchOff;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private GameObject _unsavedHint;

        // Giá trị đang chỉnh (chưa lưu).
        private float _master, _music, _sfx;
        private bool _musicOn, _sfxOn;
        private bool _bound;

        private void OnEnable()
        {
            if (!_bound)
            {
                Bind();
            }

            LoadFromStore();
            Refresh();
        }

        private void OnDisable()
        {
            // Đóng mà chưa Lưu: bỏ phần nghe thử, trả về giá trị đã lưu.
            AudioSettingsStore.Apply();
        }

        private void Bind()
        {
            _bound = true;
            if (_masterSlider != null) _masterSlider.onValueChanged.AddListener(v => { _master = v; OnEdited(); });
            if (_musicSlider != null) _musicSlider.onValueChanged.AddListener(v => { _music = v; OnEdited(); });
            if (_sfxSlider != null) _sfxSlider.onValueChanged.AddListener(v => { _sfx = v; OnEdited(); });
            if (_musicToggle != null) _musicToggle.onValueChanged.AddListener(on => { _musicOn = on; OnEdited(); });
            if (_sfxToggle != null) _sfxToggle.onValueChanged.AddListener(on => { _sfxOn = on; OnEdited(); });
            if (_resetButton != null) _resetButton.onClick.AddListener(ResetToDefault);
            if (_saveButton != null) _saveButton.onClick.AddListener(Save);
        }

        private void LoadFromStore()
        {
            _master = AudioSettingsStore.MasterVolume;
            _music = AudioSettingsStore.MusicVolume;
            _sfx = AudioSettingsStore.SfxVolume;
            _musicOn = AudioSettingsStore.MusicOn;
            _sfxOn = AudioSettingsStore.SfxOn;
        }

        private void ResetToDefault()
        {
            _master = AudioSettingsStore.DefaultMaster;
            _music = AudioSettingsStore.DefaultMusic;
            _sfx = AudioSettingsStore.DefaultSfx;
            _musicOn = AudioSettingsStore.DefaultMusicOn;
            _sfxOn = AudioSettingsStore.DefaultSfxOn;
            OnEdited();
            Refresh();
        }

        private void Save()
        {
            AudioSettingsStore.Save(_master, _music, _sfx, _musicOn, _sfxOn);
            var popup = GetComponent<UiPopup>();
            if (popup != null)
            {
                popup.Close();
            }
        }

        private void OnEdited()
        {
            AudioSettingsStore.Preview(_master, _music, _sfx, _musicOn, _sfxOn);
            UpdateLabels();
        }

        /// <summary>Đồng bộ UI theo giá trị đang chỉnh mà không kích hoạt lại listener.</summary>
        private void Refresh()
        {
            if (_masterSlider != null) _masterSlider.SetValueWithoutNotify(_master);
            if (_musicSlider != null) _musicSlider.SetValueWithoutNotify(_music);
            if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(_sfx);
            if (_musicToggle != null) _musicToggle.SetIsOnWithoutNotify(_musicOn);
            if (_sfxToggle != null) _sfxToggle.SetIsOnWithoutNotify(_sfxOn);
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            // Nhắc rõ khi đang nghe thử mà chưa Lưu: đóng popup sẽ hoàn về giá trị đã lưu.
            if (_unsavedHint != null) _unsavedHint.SetActive(AudioSettingsStore.DiffersFromSaved(_master, _music, _sfx, _musicOn, _sfxOn));

            SetPercent(_masterValue, _master);
            SetPercent(_musicValue, _music);
            SetPercent(_sfxValue, _sfx);

            SetSwitch(_musicSwitchOn, _musicSwitchOff, _musicOn);
            SetSwitch(_sfxSwitchOn, _sfxSwitchOff, _sfxOn);

            // Thanh trượt của nhóm đang tắt vẫn chỉnh được nhưng làm mờ để người chơi thấy đang tắt.
            SetDim(_musicSlider, _musicOn);
            SetDim(_sfxSlider, _sfxOn);
        }

        private static void SetSwitch(GameObject on, GameObject off, bool value)
        {
            if (on != null) on.SetActive(value);
            if (off != null) off.SetActive(!value);
        }

        private static void SetPercent(Text label, float value)
        {
            if (label != null)
            {
                label.text = Mathf.RoundToInt(value * 100f) + "%";
            }
        }

        private static void SetDim(Slider slider, bool enabled)
        {
            if (slider == null)
            {
                return;
            }

            var group = slider.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = enabled ? 1f : 0.45f;
            }
        }
    }
}
