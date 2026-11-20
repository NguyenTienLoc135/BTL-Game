using System;
using UnityEngine;

namespace BTLGame.UI
{
    /// <summary>
    /// Lưu và áp dụng cài đặt âm thanh của người chơi (PlayerPrefs). Đây là tùy chọn của máy,
    /// KHÔNG phải trạng thái trận nên không đi qua Core. Phạm vi theo GAME_RULES: chỉ "Cài đặt âm thanh".
    /// Chỉ <see cref="Save"/> mới ghi; <see cref="Preview"/> chỉ cho nghe thử và được hoàn tác bằng <see cref="Apply"/>.
    /// Nhạc nền/hiệu ứng: AudioSource của hệ thống âm thanh đọc <see cref="EffectiveMusic"/> / <see cref="EffectiveSfx"/>.
    /// </summary>
    public static class AudioSettingsStore
    {
        private const string MasterKey = "CoTyPhu.Audio.Master";
        private const string MusicKey = "CoTyPhu.Audio.Music";
        private const string SfxKey = "CoTyPhu.Audio.Sfx";
        private const string EnabledKey = "CoTyPhu.Audio.Enabled";

        public const float DefaultMaster = 1f;
        public const float DefaultMusic = 0.7f;
        public const float DefaultSfx = 1f;
        public const bool DefaultEnabled = true;

        public static event Action Changed;

        public static float MasterVolume => PlayerPrefs.GetFloat(MasterKey, DefaultMaster);
        public static float MusicVolume => PlayerPrefs.GetFloat(MusicKey, DefaultMusic);
        public static float SfxVolume => PlayerPrefs.GetFloat(SfxKey, DefaultSfx);
        public static bool SoundEnabled => PlayerPrefs.GetInt(EnabledKey, DefaultEnabled ? 1 : 0) == 1;

        /// <summary>Âm lượng hiệu ứng thực tế (0 khi tắt âm thanh). Âm lượng chung do AudioListener xử lý.</summary>
        public static float EffectiveSfx => SoundEnabled ? SfxVolume : 0f;

        /// <summary>Âm lượng nhạc thực tế (0 khi tắt âm thanh).</summary>
        public static float EffectiveMusic => SoundEnabled ? MusicVolume : 0f;

        /// <summary>Ghi cả bốn giá trị cùng lúc (nút Lưu) rồi áp dụng.</summary>
        public static void Save(float master, float music, float sfx, bool enabled)
        {
            PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(master));
            PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(music));
            PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(sfx));
            PlayerPrefs.SetInt(EnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
            Changed?.Invoke();
        }

        /// <summary>Nghe thử âm lượng chung khi đang chỉnh, chưa ghi. Hoàn tác bằng <see cref="Apply"/>.</summary>
        public static void Preview(float master, bool enabled)
        {
            AudioListener.volume = enabled ? Mathf.Clamp01(master) : 0f;
        }

        /// <summary>Áp dụng giá trị ĐÃ LƯU lên AudioListener.</summary>
        public static void Apply()
        {
            AudioListener.volume = SoundEnabled ? MasterVolume : 0f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnStartup()
        {
            Apply();
        }
    }
}
