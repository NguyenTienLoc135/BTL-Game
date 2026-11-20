using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.UI
{
    /// <summary>
    /// Popup Cài đặt âm thanh: ba thanh trượt (chung / nhạc nền / hiệu ứng), công tắc bật âm thanh,
    /// nút Khôi phục mặc định và nút Lưu. Chỉnh chỉ có tác dụng nghe thử; chỉ nút Lưu mới ghi vào
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
        [SerializeField] private Toggle _soundToggle;
        [SerializeField] private GameObject _switchOn;
        [SerializeField] private GameObject _switchOff;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _saveButton;

        // Giá trị đang chỉnh (chưa lưu).
        private float _master, _music, _sfx;
        private bool _enabled;
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
            // Đóng mà chưa Lưu: bỏ phần nghe thử, trả AudioListener về giá trị đã lưu.
            AudioSettingsStore.Apply();
        }

        private void Bind()
        {
            _bound = true;
            if (_masterSlider != null) _masterSlider.onValueChanged.AddListener(v => { _master = v; OnEdited(); });
            if (_musicSlider != null) _musicSlider.onValueChanged.AddListener(v => { _music = v; OnEdited(); });
            if (_sfxSlider != null) _sfxSlider.onValueChanged.AddListener(v => { _sfx = v; OnEdited(); });
            if (_soundToggle != null) _soundToggle.onValueChanged.AddListener(on => { _enabled = on; OnEdited(); });
            if (_resetButton != null) _resetButton.onClick.AddListener(ResetToDefault);
            if (_saveButton != null) _saveButton.onClick.AddListener(Save);
        }

        private void LoadFromStore()
        {
            _master = AudioSettingsStore.MasterVolume;
            _music = AudioSettingsStore.MusicVolume;
            _sfx = AudioSettingsStore.SfxVolume;
            _enabled = AudioSettingsStore.SoundEnabled;
        }

        private void ResetToDefault()
        {
            _master = AudioSettingsStore.DefaultMaster;
            _music = AudioSettingsStore.DefaultMusic;
            _sfx = AudioSettingsStore.DefaultSfx;
            _enabled = AudioSettingsStore.DefaultEnabled;
            OnEdited();
            Refresh();
        }

        private void Save()
        {
            AudioSettingsStore.Save(_master, _music, _sfx, _enabled);
            var popup = GetComponent<UiPopup>();
            if (popup != null)
            {
                popup.Close();
            }
        }

        private void OnEdited()
        {
            AudioSettingsStore.Preview(_master, _enabled);
            UpdateLabels();
        }

        /// <summary>Đồng bộ UI theo giá trị đang chỉnh mà không kích hoạt lại listener.</summary>
        private void Refresh()
        {
            if (_masterSlider != null) _masterSlider.SetValueWithoutNotify(_master);
            if (_musicSlider != null) _musicSlider.SetValueWithoutNotify(_music);
            if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(_sfx);
            if (_soundToggle != null) _soundToggle.SetIsOnWithoutNotify(_enabled);
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            SetPercent(_masterValue, _master);
            SetPercent(_musicValue, _music);
            SetPercent(_sfxValue, _sfx);

            if (_switchOn != null) _switchOn.SetActive(_enabled);
            if (_switchOff != null) _switchOff.SetActive(!_enabled);

            // Khi tắt âm thanh, thanh trượt vẫn chỉnh được nhưng làm mờ để người chơi thấy đang tắt.
            SetDim(_masterSlider, _enabled);
            SetDim(_musicSlider, _enabled);
            SetDim(_sfxSlider, _enabled);
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
