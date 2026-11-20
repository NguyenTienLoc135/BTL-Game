using BTLGame.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace BtlGame.Tests
{
    /// <summary>Cài đặt âm thanh: nghe thử không ghi, Lưu mới ghi, đóng popup hoàn về giá trị đã lưu; tắt âm thanh thì nhạc/hiệu ứng = 0.</summary>
    public sealed class AudioSettingsStoreTests
    {
        private static readonly string[] Keys =
        {
            "CoTyPhu.Audio.Master", "CoTyPhu.Audio.Music", "CoTyPhu.Audio.Sfx", "CoTyPhu.Audio.MusicOn", "CoTyPhu.Audio.SfxOn", "CoTyPhu.Audio.Version"
        };

        private readonly string[] _backup = new string[6];

        [SetUp]
        public void SaveUserPrefs()
        {
            for (int i = 0; i < Keys.Length; i++)
            {
                _backup[i] = PlayerPrefs.HasKey(Keys[i]) ? PlayerPrefs.GetFloat(Keys[i]).ToString("R") + "|" + PlayerPrefs.GetInt(Keys[i]) : null;
                PlayerPrefs.DeleteKey(Keys[i]);
            }
        }

        [TearDown]
        public void RestoreUserPrefs()
        {
            for (int i = 0; i < Keys.Length; i++)
            {
                PlayerPrefs.DeleteKey(Keys[i]);
                if (_backup[i] == null) continue;
                string[] parts = _backup[i].Split('|');
                if (i >= 3) PlayerPrefs.SetInt(Keys[i], int.Parse(parts[1]));
                else PlayerPrefs.SetFloat(Keys[i], float.Parse(parts[0]));
            }

            AudioSettingsStore.Apply();
        }

        [Test]
        public void DefaultsAreAudible()
        {
            Assert.That(AudioSettingsStore.MusicOn && AudioSettingsStore.SfxOn, Is.True);
            Assert.That(AudioSettingsStore.EffectiveMusic, Is.GreaterThan(0f));
            Assert.That(AudioSettingsStore.EffectiveSfx, Is.GreaterThan(0f));
        }

        [Test]
        public void PreviewDoesNotPersistAndApplyRevertsToSaved()
        {
            AudioSettingsStore.Save(0.8f, 0.5f, 0.6f, true, true);
            AudioSettingsStore.Preview(0.2f, 0.1f, 0.0f, true, true);
            Assert.That(AudioSettingsStore.EffectiveMusic, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(AudioSettingsStore.MusicVolume, Is.EqualTo(0.5f).Within(0.0001f), "Nghe thử không được ghi.");
            Assert.That(AudioSettingsStore.DiffersFromSaved(0.2f, 0.1f, 0f, true, true), Is.True);

            AudioSettingsStore.Apply();
            Assert.That(AudioSettingsStore.EffectiveMusic, Is.EqualTo(0.5f).Within(0.0001f), "Đóng mà chưa Lưu phải hoàn về giá trị đã lưu.");
            Assert.That(AudioSettingsStore.DiffersFromSaved(0.8f, 0.5f, 0.6f, true, true), Is.False);
        }

        [Test]
        public void DisabledSoundSilencesMusicAndEffects()
        {
            AudioSettingsStore.Save(1f, 0.7f, 1f, false, false);
            Assert.That(AudioSettingsStore.EffectiveMusic, Is.EqualTo(0f));
            Assert.That(AudioSettingsStore.EffectiveSfx, Is.EqualTo(0f));

            AudioSettingsStore.Preview(1f, 0.7f, 1f, true, true);
            Assert.That(AudioSettingsStore.EffectiveMusic, Is.GreaterThan(0f), "Bật lại khi nghe thử thì phải có tiếng ngay.");
        }

        [Test]
        public void MusicAndSfxSwitchesAreIndependent()
        {
            AudioSettingsStore.Save(1f, 0.6f, 0.9f, false, true);
            Assert.That(AudioSettingsStore.EffectiveMusic, Is.EqualTo(0f));
            Assert.That(AudioSettingsStore.EffectiveSfx, Is.GreaterThan(0f));

            AudioSettingsStore.Save(1f, 0.6f, 0.9f, true, false);
            Assert.That(AudioSettingsStore.EffectiveMusic, Is.GreaterThan(0f));
            Assert.That(AudioSettingsStore.EffectiveSfx, Is.EqualTo(0f));
        }
    }
}
