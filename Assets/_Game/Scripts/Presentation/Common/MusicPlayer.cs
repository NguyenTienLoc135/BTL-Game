using UnityEngine;

namespace BTLGame.Presentation
{
    /// <summary>
    /// Nhạc nền DUY NHẤT của game (singleton sống xuyên Scene) cho các bản nhạc DÀI của nhóm (Assets/_Game/Audio/Music).
    /// Mỗi Scene đặt một object "Music" mang danh sách nhạc của Scene đó; khi đã có bộ phát toàn cục, object của Scene chỉ
    /// chuyển danh sách cho nó rồi tự hủy → không bao giờ phát chồng nhạc.
    /// - Hai AudioSource tạo MỘT lần để crossfade: bài kế vào trước khi bài hiện tại hết nên gần như không có khoảng lặng.
    /// - Danh sách xáo trộn dạng "túi": nhiều bản thì luân phiên, không lặp liền kề; chỉ một bản thì crossfade vào chính nó (không click khi lặp).
    /// - Đổi danh sách (MainMenu sang Gameplay): bài cũ mờ ra trong lúc bài mới mờ vào.
    /// - <see cref="Duck"/> hạ tạm nhạc nền khi có hiệu ứng thành tựu rồi tự phục hồi.
    /// Âm lượng theo AudioSettingsStore.EffectiveMusic (công tắc/thanh Nhạc nền, kể cả khi đang nghe thử), độc lập với hiệu ứng.
    /// </summary>
    public sealed class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;
        [SerializeField] private AudioClip[] _playlist = new AudioClip[0];
        [SerializeField, Range(0f, 1f)] private float _baseVolume = 0.5f;
        [SerializeField, Min(0.2f)] private float _crossfadeSeconds = 3f;
        [SerializeField, Min(0.2f)] private float _sceneFadeSeconds = 1.5f;

        private sealed class Voice
        {
            public AudioSource Source;
            public float Gain;
            public float Target;
            public float FadeSeconds = 3f;
        }

        private static MusicPlayer _instance;

        private Voice[] _voices;
        private int _active;
        private AudioClip[] _current = new AudioClip[0];
        private float _currentBase = 0.5f;
        private int[] _bag = new int[0];
        private int _bagPos;
        private int _lastClip = -1;
        private float _duckLevel = 1f;
        private float _duckUntil;
        private float _duckSmooth = 1f;
        private float _startAt;

        public static MusicPlayer Instance => _instance;
        public int ClipCount => _voices != null ? _current.Length : (_playlist == null ? 0 : _playlist.Length);
        public bool IsPlaying => _voices != null && _voices[_active].Source.isPlaying;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // Đã có bộ phát toàn cục: nhường danh sách của Scene này cho nó rồi biến mất.
                _instance.SetPlaylist(_playlist, _baseVolume);
                Destroy(gameObject);
                return;
            }

            _instance = this;
            transform.SetParent(null, false);
            DontDestroyOnLoad(gameObject);
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
            }

            var second = gameObject.AddComponent<AudioSource>();
            _voices = new[] { MakeVoice(_source), MakeVoice(second) };
            SetPlaylist(_playlist, _baseVolume);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private static Voice MakeVoice(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            return new Voice { Source = source };
        }

        /// <summary>Hạ nhạc nền xuống <paramref name="level"/> (0..1) trong <paramref name="seconds"/> giây rồi tự phục hồi.</summary>
        public static void Duck(float level, float seconds)
        {
            if (_instance == null)
            {
                return;
            }

            _instance._duckLevel = Mathf.Clamp01(level);
            _instance._duckUntil = Time.unscaledTime + Mathf.Max(0f, seconds);
        }

        /// <summary>Đổi danh sách nhạc. Danh sách y hệt đang phát thì giữ nguyên (không restart).</summary>
        public void SetPlaylist(AudioClip[] clips, float baseVolume)
        {
            clips = clips ?? new AudioClip[0];
            _currentBase = baseVolume;
            if (_voices == null || SameList(clips, _current))
            {
                return;
            }

            _current = clips;
            _bag = new int[0];
            _bagPos = 0;
            _lastClip = -1;
            StartNext(_sceneFadeSeconds);
        }

        private static bool SameList(AudioClip[] a, AudioClip[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void Update()
        {
            if (_voices == null)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            float duck = Time.unscaledTime < _duckUntil ? _duckLevel : 1f;
            _duckSmooth = Mathf.MoveTowards(_duckSmooth, duck, dt * (duck < _duckSmooth ? 3f : 0.5f));
            float master = AudioSettingsStore.EffectiveMusic * _currentBase * _duckSmooth;
            foreach (var voice in _voices)
            {
                voice.Gain = Mathf.MoveTowards(voice.Gain, voice.Target, dt / voice.FadeSeconds);
                voice.Source.volume = voice.Gain * master;
                if (voice.Gain <= 0f && voice.Target <= 0f && voice.Source.isPlaying)
                {
                    voice.Source.Stop();
                }
            }

            if (_current.Length == 0)
            {
                return;
            }

            // Bài đang phát sắp hết thì vào bài kế (crossfade) trước khi có khoảng lặng.
            var active = _voices[_active].Source;
            if (!active.isPlaying)
            {
                if (Time.unscaledTime - _startAt > 0.3f)
                {
                    StartNext(_crossfadeSeconds);
                }
            }
            else if (active.clip != null && active.clip.length - active.time <= Mathf.Min(_crossfadeSeconds, active.clip.length * 0.4f))
            {
                StartNext(_crossfadeSeconds);
            }
        }

        private void StartNext(float fadeSeconds)
        {
            if (_current.Length == 0)
            {
                // Không có nhạc: mờ nốt bài đang phát (nếu có) rồi im lặng.
                foreach (var voice in _voices)
                {
                    voice.Target = 0f;
                    voice.FadeSeconds = fadeSeconds;
                }

                return;
            }

            var clip = _current[NextIndex()];
            _startAt = Time.unscaledTime;
            if (clip == null)
            {
                return;   // thiếu clip: bỏ qua an toàn, lần Update sau chọn bài khác
            }

            // Bài đang phát mờ đi, bài mới (ở voice còn lại) vào dần.
            var old = _voices[_active];
            old.Target = 0f;
            old.FadeSeconds = fadeSeconds;
            _active = 1 - _active;
            var next = _voices[_active];
            next.Source.Stop();
            next.Source.clip = clip;
            next.Source.time = 0f;
            next.Gain = 0f;
            next.Target = 1f;
            next.FadeSeconds = fadeSeconds;
            next.Source.volume = 0f;
            next.Source.Play();
        }

        private int NextIndex()
        {
            if (_bagPos >= _bag.Length)
            {
                _bag = BuildBag(_current.Length, _lastClip);
                _bagPos = 0;
            }

            _lastClip = _bag[_bagPos++];
            return _lastClip;
        }

        /// <summary>Túi xáo trộn các chỉ số 0..count-1; phần tử đầu khác <paramref name="lastClip"/> để không lặp bài liền kề.</summary>
        public static int[] BuildBag(int count, int lastClip)
        {
            var bag = new int[count];
            for (int i = 0; i < count; i++) bag[i] = i;
            for (int i = count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (bag[i], bag[j]) = (bag[j], bag[i]);
            }

            if (count > 1 && bag[0] == lastClip)
            {
                (bag[0], bag[1]) = (bag[1], bag[0]);
            }

            return bag;
        }
    }
}
