using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.UI
{
    public enum MatchControllerMode { VersusAI, LocalHotSeat }
    public enum MatchTimeLimit { Minutes30, Minutes45, Minutes60, Unlimited }

    public readonly struct SetupRoleOption
    {
        public string RoleId { get; }
        public string DisplayName { get; }
        public SetupRoleOption(string roleId, string displayName)
        {
            RoleId = roleId ?? throw new ArgumentNullException(nameof(roleId));
            DisplayName = displayName ?? string.Empty;
        }
    }

    public sealed class MatchSetupRequest
    {
        public MatchControllerMode Mode { get; }
        public int PlayerCount { get; }
        public MatchTimeLimit TimeLimit { get; }
        public IReadOnlyList<string> RoleIds { get; }

        public MatchSetupRequest(MatchControllerMode mode, int playerCount,
            MatchTimeLimit timeLimit, IReadOnlyList<string> roleIds)
        {
            Mode = mode;
            PlayerCount = playerCount;
            TimeLimit = timeLimit;
            RoleIds = new ReadOnlyCollection<string>(new List<string>(roleIds));
        }
    }

    public sealed class MainMenuSetupController : MonoBehaviour
    {
        [SerializeField] private OptionSelector _modeSelector;
        [SerializeField] private OptionSelector _playerCountSelector;
        [SerializeField] private OptionSelector _timeLimitSelector;
        [SerializeField] private OptionSelector[] _roleSelectors = new OptionSelector[4];
        [SerializeField] private Button _startButton;
        [SerializeField] private Slider _volumeSlider;
        [SerializeField] private Button _guideButton;
        [SerializeField] private GameObject _guidePanel;
        [SerializeField] private TMPro.TMP_Text _statusText;

        private readonly List<SetupRoleOption> _roles = new List<SetupRoleOption>();
        private int _minimumPlayers;
        private int _maximumPlayers;
        private bool _hasSetupConstraints;
        private bool _startSubmitted;

        public event Action<MatchSetupRequest> SetupRequested;

        public void ConfigureReferences(OptionSelector mode, OptionSelector playerCount,
            OptionSelector timeLimit, OptionSelector[] roles, Button start, Slider volume,
            Button guide, GameObject guidePanel, TMPro.TMP_Text status)
        {
            _modeSelector = mode;
            _playerCountSelector = playerCount;
            _timeLimitSelector = timeLimit;
            _roleSelectors = roles ?? Array.Empty<OptionSelector>();
            _startButton = start;
            _volumeSlider = volume;
            _guideButton = guide;
            _guidePanel = guidePanel;
            _statusText = status;
        }

        private void Awake()
        {
            _modeSelector?.Configure("Chế độ", new[]
            {
                new SelectorOption("ai", "Đấu với AI"),
                new SelectorOption("local", "Chơi cùng máy")
            });
            _timeLimitSelector?.Configure("Thời gian", new[]
            {
                new SelectorOption("30", "30 phút"),
                new SelectorOption("45", "45 phút"),
                new SelectorOption("60", "60 phút"),
                new SelectorOption("unlimited", "Vô hạn")
            });
            _playerCountSelector?.Configure("Số người", Array.Empty<SelectorOption>());
            foreach (OptionSelector selector in _roleSelectors)
                selector?.Configure("Nghề nghiệp", Array.Empty<SelectorOption>());
            if (_startButton != null)
            {
                _startButton.onClick.AddListener(HandleStartClicked);
                _startButton.interactable = false;
            }
            if (_guideButton != null) _guideButton.onClick.AddListener(ToggleGuide);
            if (_volumeSlider != null)
            {
                _volumeSlider.onValueChanged.AddListener(HandleVolumeChanged);
                AudioListener.volume = Mathf.Clamp01(_volumeSlider.value);
            }
            if (_guidePanel != null) _guidePanel.SetActive(false);
            SetStatus("Chờ Core cung cấp giới hạn số người và Characters cung cấp danh sách nghề.");
        }

        private void OnDestroy()
        {
            if (_startButton != null) _startButton.onClick.RemoveListener(HandleStartClicked);
            if (_guideButton != null) _guideButton.onClick.RemoveListener(ToggleGuide);
            if (_volumeSlider != null) _volumeSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
            if (_modeSelector != null) _modeSelector.SelectionChanged -= HandleSelectionChanged;
            if (_playerCountSelector != null) _playerCountSelector.SelectionChanged -= HandleSelectionChanged;
            if (_timeLimitSelector != null) _timeLimitSelector.SelectionChanged -= HandleSelectionChanged;
            foreach (OptionSelector selector in _roleSelectors)
                if (selector != null) selector.SelectionChanged -= HandleSelectionChanged;
        }

        public void ConfigureOptions(int minimumPlayers, int maximumPlayers,
            IReadOnlyList<SetupRoleOption> roleOptions)
        {
            if (minimumPlayers < 1 || maximumPlayers > 4 || minimumPlayers > maximumPlayers)
                throw new ArgumentOutOfRangeException(nameof(minimumPlayers), "Giới hạn số người phải do Core cung cấp và không vượt quá 4.");
            if (roleOptions == null || roleOptions.Count == 0)
                throw new ArgumentException("Cần RoleCatalog từ module Characters.", nameof(roleOptions));

            _minimumPlayers = minimumPlayers;
            _maximumPlayers = maximumPlayers;
            _roles.Clear();
            _roles.AddRange(roleOptions);
            _hasSetupConstraints = true;
            _startSubmitted = false;

            var playerCounts = new List<SelectorOption>();
            for (int count = minimumPlayers; count <= maximumPlayers; count++)
                playerCounts.Add(new SelectorOption(count.ToString(), count + " người"));
            _playerCountSelector.Configure("Số người", playerCounts);

            var roleOptionsForSelector = new List<SelectorOption>();
            foreach (SetupRoleOption role in _roles)
                roleOptionsForSelector.Add(new SelectorOption(role.RoleId, role.DisplayName));
            foreach (OptionSelector selector in _roleSelectors)
                selector?.Configure("Nghề nghiệp", roleOptionsForSelector);

            SubscribeSelectors();
            UpdateRoleRows();
            ValidateSetup();
        }

        public void SetupRejected(string reason)
        {
            _startSubmitted = false;
            SetStatus(string.IsNullOrWhiteSpace(reason) ? "Core từ chối cấu hình trận." : reason);
            ValidateSetup();
        }

        public void ToggleGuide()
        {
            if (_guidePanel != null) _guidePanel.SetActive(!_guidePanel.activeSelf);
        }

        private void SubscribeSelectors()
        {
            if (_modeSelector != null) _modeSelector.SelectionChanged -= HandleSelectionChanged;
            if (_playerCountSelector != null) _playerCountSelector.SelectionChanged -= HandleSelectionChanged;
            if (_timeLimitSelector != null) _timeLimitSelector.SelectionChanged -= HandleSelectionChanged;
            foreach (OptionSelector selector in _roleSelectors)
                if (selector != null) selector.SelectionChanged -= HandleSelectionChanged;
            if (_modeSelector != null) _modeSelector.SelectionChanged += HandleSelectionChanged;
            if (_playerCountSelector != null) _playerCountSelector.SelectionChanged += HandleSelectionChanged;
            if (_timeLimitSelector != null) _timeLimitSelector.SelectionChanged += HandleSelectionChanged;
            foreach (OptionSelector selector in _roleSelectors)
                if (selector != null) selector.SelectionChanged += HandleSelectionChanged;
        }

        private void HandleSelectionChanged(string _) { _startSubmitted = false; UpdateRoleRows(); ValidateSetup(); }

        private void UpdateRoleRows()
        {
            int count = _hasSetupConstraints && _playerCountSelector != null && _playerCountSelector.HasSelection
                ? _minimumPlayers + _playerCountSelector.SelectedIndex : 0;
            for (int i = 0; i < _roleSelectors.Length; i++)
                if (_roleSelectors[i] != null) _roleSelectors[i].SetInteractable(i < count);
        }

        private void ValidateSetup()
        {
            bool valid = _hasSetupConstraints && !_startSubmitted &&
                _modeSelector != null && _modeSelector.HasSelection &&
                _playerCountSelector != null && _playerCountSelector.HasSelection &&
                _timeLimitSelector != null && _timeLimitSelector.HasSelection;
            int count = valid ? _minimumPlayers + _playerCountSelector.SelectedIndex : 0;
            if (valid && (count < _minimumPlayers || count > _maximumPlayers || count > _roleSelectors.Length)) valid = false;
            for (int i = 0; valid && i < count; i++)
                valid = _roleSelectors[i] != null && _roleSelectors[i].HasSelection;
            if (_startButton != null) _startButton.interactable = valid;
            if (valid) SetStatus("Cấu hình hợp lệ; Core vẫn kiểm tra lại trước khi tạo trận.");
        }

        private void HandleStartClicked()
        {
            if (!ValidateRequest(out MatchSetupRequest request)) return;
            if (SetupRequested == null)
            {
                SetupRejected("Chưa nối bộ điều phối Core để tạo trận.");
                return;
            }
            _startSubmitted = true;
            ValidateSetup();
            SetupRequested?.Invoke(request);
        }

        private bool ValidateRequest(out MatchSetupRequest request)
        {
            request = null;
            if (!ValidateCurrentSelection()) return false;
            int count = _minimumPlayers + _playerCountSelector.SelectedIndex;
            var roleIds = new List<string>(count);
            for (int i = 0; i < count; i++) roleIds.Add(_roleSelectors[i].SelectedId);
            MatchControllerMode mode = _modeSelector.SelectedId == "local"
                ? MatchControllerMode.LocalHotSeat : MatchControllerMode.VersusAI;
            MatchTimeLimit time = _timeLimitSelector.SelectedId switch
            {
                "30" => MatchTimeLimit.Minutes30,
                "45" => MatchTimeLimit.Minutes45,
                "60" => MatchTimeLimit.Minutes60,
                "unlimited" => MatchTimeLimit.Unlimited,
                _ => throw new InvalidOperationException("TimeLimit UI không hợp lệ.")
            };
            request = new MatchSetupRequest(mode, count, time, roleIds);
            return true;
        }

        private bool ValidateCurrentSelection()
        {
            if (!_hasSetupConstraints || _playerCountSelector == null || !_playerCountSelector.HasSelection ||
                _modeSelector == null || !_modeSelector.HasSelection || _timeLimitSelector == null || !_timeLimitSelector.HasSelection)
            {
                SetStatus("Chưa có cấu hình setup đầy đủ từ Core/Characters.");
                return false;
            }
            int count = _minimumPlayers + _playerCountSelector.SelectedIndex;
            if (count < _minimumPlayers || count > _maximumPlayers || count > _roleSelectors.Length)
            {
                SetStatus("Số người nằm ngoài giới hạn hợp lệ.");
                return false;
            }
            for (int i = 0; i < count; i++)
                if (_roleSelectors[i] == null || !_roleSelectors[i].HasSelection)
                {
                    SetStatus("Hãy chọn nghề cho từng người chơi.");
                    return false;
                }
            return true;
        }

        private void HandleVolumeChanged(float value) => AudioListener.volume = Mathf.Clamp01(value);
        private void SetStatus(string value) { if (_statusText != null) _statusText.text = value ?? string.Empty; }
    }
}
