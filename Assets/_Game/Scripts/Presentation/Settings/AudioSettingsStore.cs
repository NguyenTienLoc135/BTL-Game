using System;
using UnityEngine;

namespace BTLGame.Presentation
{
    /// <summary>
    /// Lưu và áp dụng cài đặt âm thanh của người chơi (PlayerPrefs). Đây là tùy chọn của máy,
    /// KHÔNG phải trạng thái trận nên không đi qua Core. Phạm vi theo GAME_RULES: chỉ "Cài đặt âm thanh".
    /// Nhạc nền và hiệu ứng có công tắc BẬT/TẮT riêng + thanh âm lượng riêng; âm lượng chung do AudioListener xử lý.
    /// Chỉ <see cref="Save"/> mới ghi; <see cref="Preview"/> chỉ cho nghe thử và được hoàn tác bằng <see cref="Apply"/>.
    /// MusicPlayer/UiClickSound/GameplaySfx đọc <see cref="EffectiveMusic"/> / <see cref="EffectiveSfx"/>
    /// nên nghe thử và đổi cài đặt có hiệu lực ngay ở cả MainMenu lẫn Gameplay.
    /// </summary>
    public static class AudioSettingsStore
    {
        private const string MasterKey = "CoTyPhu.Audio.Master";
        private const string MusicKey = "CoTyPhu.Audio.Music";
        private const string SfxKey = "CoTyPhu.Audio.Sfx";
        private const string MusicOnKey = "CoTyPhu.Audio.MusicOn";
        private const string SfxOnKey = "CoTyPhu.Audio.SfxOn";
        private const string VersionKey = "CoTyPhu.Audio.Version";
        private const int CurrentVersion = 3;

        public const float DefaultMaster = 1f;
        public const float DefaultMusic = 0.5f;
        public const float DefaultSfx = 0.7f;
        public const bool DefaultMusicOn = true;
        public const bool DefaultSfxOn = true;

        public static event Action Changed;

        // Giá trị đang nghe thử (null = dùng giá trị đã lưu).
        private static float? _previewMusic;
        private static float? _previewSfx;
        private static bool? _previewMusicOn;
        private static bool? _previewSfxOn;

        public static float MasterVolume => PlayerPrefs.GetFloat(MasterKey, DefaultMaster);
        public static float MusicVolume => PlayerPrefs.GetFloat(MusicKey, DefaultMusic);
        public static float SfxVolume => PlayerPrefs.GetFloat(SfxKey, DefaultSfx);
        public static bool MusicOn => PlayerPrefs.GetInt(MusicOnKey, DefaultMusicOn ? 1 : 0) == 1;
        public static bool SfxOn => PlayerPrefs.GetInt(SfxOnKey, DefaultSfxOn ? 1 : 0) == 1;

        /// <summary>Âm lượng hiệu ứng thực tế (0 khi tắt hiệu ứng); tính cả phần đang nghe thử.</summary>
        public static float EffectiveSfx => (_previewSfxOn ?? SfxOn) ? (_previewSfx ?? SfxVolume) : 0f;

        /// <summary>Âm lượng nhạc thực tế (0 khi tắt nhạc); tính cả phần đang nghe thử.</summary>
        public static float EffectiveMusic => (_previewMusicOn ?? MusicOn) ? (_previewMusic ?? MusicVolume) : 0f;

        /// <summary>Ghi tất cả giá trị cùng lúc (nút Lưu) rồi áp dụng.</summary>
        public static void Save(float master, float music, float sfx, bool musicOn, bool sfxOn)
        {
            PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(master));
            PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(music));
            PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(sfx));
            PlayerPrefs.SetInt(MusicOnKey, musicOn ? 1 : 0);
            PlayerPrefs.SetInt(SfxOnKey, sfxOn ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
            Changed?.Invoke();
        }

        /// <summary>Nghe thử khi đang chỉnh, chưa ghi. Hoàn tác bằng <see cref="Apply"/>.</summary>
        public static void Preview(float master, float music, float sfx, bool musicOn, bool sfxOn)
        {
            _previewMusic = Mathf.Clamp01(music);
            _previewSfx = Mathf.Clamp01(sfx);
            _previewMusicOn = musicOn;
            _previewSfxOn = sfxOn;
            AudioListener.volume = Mathf.Clamp01(master);
        }

        /// <summary>Bỏ phần nghe thử và áp dụng giá trị ĐÃ LƯU lên AudioListener.</summary>
        public static void Apply()
        {
            _previewMusic = null;
            _previewSfx = null;
            _previewMusicOn = null;
            _previewSfxOn = null;
            AudioListener.volume = MasterVolume;
        }

        // Phiên bản cũ chỉ có MỘT công tắc "Bật âm thanh" (và từng lưu ngay khi chỉnh nên có thể kẹt giá trị 0/tắt).
        // Sang công tắc riêng + mặc định mới → đặt lại về mặc định đúng MỘT lần.
        private static void MigrateOnce()
        {
            if (PlayerPrefs.GetInt(VersionKey, 0) >= CurrentVersion)
            {
                return;
            }

            PlayerPrefs.DeleteKey(MasterKey);
            PlayerPrefs.DeleteKey(MusicKey);
            PlayerPrefs.DeleteKey(SfxKey);
            PlayerPrefs.DeleteKey(MusicOnKey);
            PlayerPrefs.DeleteKey(SfxOnKey);
            PlayerPrefs.DeleteKey("CoTyPhu.Audio.Enabled");
            PlayerPrefs.SetInt(VersionKey, CurrentVersion);
            PlayerPrefs.Save();
        }

        /// <summary>Có đang chỉnh khác giá trị đã lưu không (để hiện nhắc "chưa lưu").</summary>
        public static bool DiffersFromSaved(float master, float music, float sfx, bool musicOn, bool sfxOn)
        {
            return !Mathf.Approximately(master, MasterVolume) || !Mathf.Approximately(music, MusicVolume)
                   || !Mathf.Approximately(sfx, SfxVolume) || musicOn != MusicOn || sfxOn != SfxOn;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnStartup()
        {
            MigrateOnce();
            Apply();
        }
    }
}
