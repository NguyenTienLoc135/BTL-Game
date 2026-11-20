using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.UI
{
    /// <summary>
    /// Phát tiếng click cho MỌI Button dưới object này (kể cả popup đang ẩn), theo âm lượng hiệu ứng đã cài.
    /// Gắn vào Canvas gốc của từng màn để các màn có tiếng nút đồng bộ.
    /// </summary>
    public sealed class UiClickSound : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;
        [SerializeField] private AudioClip _clip;

        private void Awake()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                button.onClick.AddListener(Play);
            }
        }

        private void Play()
        {
            if (_source != null && _clip != null)
            {
                _source.PlayOneShot(_clip, AudioSettingsStore.EffectiveSfx);
            }
        }
    }
}
