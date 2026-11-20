using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.UI
{
    [Serializable]
    public readonly struct SelectorOption
    {
        public string Id { get; }
        public string Label { get; }
        public SelectorOption(string id, string label) { Id = id ?? string.Empty; Label = label ?? string.Empty; }
    }

    public sealed class OptionSelector : MonoBehaviour
    {
        [SerializeField] private TMP_Text _fieldLabel;
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private Button _previousButton;
        [SerializeField] private Button _nextButton;
        private readonly List<SelectorOption> _options = new List<SelectorOption>();
        private int _selectedIndex = -1;
        private bool _interactable = true;

        public event Action<string> SelectionChanged;
        public int Count => _options.Count;
        public int SelectedIndex => _selectedIndex;
        public string SelectedId => _selectedIndex >= 0 && _selectedIndex < _options.Count ? _options[_selectedIndex].Id : string.Empty;

        private void Awake()
        {
            if (_previousButton != null) _previousButton.onClick.AddListener(SelectPrevious);
            if (_nextButton != null) _nextButton.onClick.AddListener(SelectNext);
            Refresh();
        }

        private void OnDestroy()
        {
            if (_previousButton != null) _previousButton.onClick.RemoveListener(SelectPrevious);
            if (_nextButton != null) _nextButton.onClick.RemoveListener(SelectNext);
        }

        public void Configure(string fieldLabel, IReadOnlyList<SelectorOption> options, int selectedIndex = -1)
        {
            if (_fieldLabel != null) _fieldLabel.text = fieldLabel ?? string.Empty;
            _options.Clear();
            if (options != null)
                for (int i = 0; i < options.Count; i++) _options.Add(options[i]);
            _selectedIndex = selectedIndex >= 0 && selectedIndex < _options.Count ? selectedIndex : -1;
            Refresh();
        }

        public bool Select(int index)
        {
            if (index < 0 || index >= _options.Count) return false;
            _selectedIndex = index;
            Refresh();
            SelectionChanged?.Invoke(SelectedId);
            return true;
        }

        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            Refresh();
        }

        public bool HasSelection => _selectedIndex >= 0 && _selectedIndex < _options.Count;

        public void ConfigureReferences(TMP_Text fieldLabel, TMP_Text valueText, Button previous, Button next)
        {
            _fieldLabel = fieldLabel;
            _valueText = valueText;
            _previousButton = previous;
            _nextButton = next;
        }

        private void SelectPrevious()
        {
            if (_options.Count < 2) return;
            Select((_selectedIndex - 1 + _options.Count) % _options.Count);
        }

        private void SelectNext()
        {
            if (_options.Count < 2) return;
            Select((_selectedIndex + 1 + _options.Count) % _options.Count);
        }

        private void Refresh()
        {
            if (_valueText != null)
                _valueText.text = HasSelection ? _options[_selectedIndex].Label : "Chưa chọn";
            bool multiple = _options.Count > 1;
            if (_previousButton != null) _previousButton.interactable = multiple && _interactable;
            if (_nextButton != null) _nextButton.interactable = multiple && _interactable;
        }
    }
}
