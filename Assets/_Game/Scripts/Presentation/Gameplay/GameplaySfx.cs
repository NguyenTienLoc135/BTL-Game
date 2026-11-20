using System.Collections.Generic;
using BTLGame.Board;
using BTLGame.UI;
using UnityEngine;

namespace BTLGame.Presentation
{
    /// <summary>
    /// Hiệu ứng âm thanh trong trận: xúc xắc, bước đi, nhận/trả tiền, mua đất, thưởng Xuất phát, thẻ sự kiện, vào tù,
    /// dùng kỹ năng, phá sản, độc quyền, thắng trận. Chỉ NGHE các sự kiện presentation (PresentationGameplayUi, BoardView,
    /// MonopolyCelebration), không ghi trạng thái trận. Âm lượng theo "Hiệu ứng" trong Cài đặt (tắt riêng được).
    /// Chống ồn: mỗi hiệu ứng có độ ưu tiên; tiếng ưu tiên thấp bị bỏ nếu một tiếng ưu tiên cao hơn vừa phát.
    /// Các cột mốc lớn (độc quyền, thắng) hạ nhẹ nhạc nền rồi tự phục hồi.
    /// </summary>
    public sealed class GameplaySfx : MonoBehaviour
    {
        // Độ ưu tiên: càng cao càng quan trọng.
        private const int PStep = 0, PPopup = 1, PCash = 2, PBuy = 3, PBonus = 4, PMajor = 5;
        private const long StartBonus = 200000;   // thưởng Xuất phát (GAME_RULES); chỉ dùng để chọn tiếng, không tính luật

        [SerializeField] private AudioSource _source;
        [SerializeField] private PresentationGameplayUi _ui;
        [SerializeField] private BoardView _board;
        [SerializeField] private AudioClip[] _dice = new AudioClip[0];
        [SerializeField] private AudioClip[] _step = new AudioClip[0];
        [SerializeField] private AudioClip[] _gain = new AudioClip[0];
        [SerializeField] private AudioClip[] _pay = new AudioClip[0];
        [SerializeField] private AudioClip[] _popup = new AudioClip[0];
        [SerializeField] private AudioClip[] _card = new AudioClip[0];
        [SerializeField] private AudioClip[] _win = new AudioClip[0];
        [SerializeField] private MonopolyCelebration _celebration;
        [SerializeField] private AudioClip[] _monopoly = new AudioClip[0];
        [SerializeField] private AudioClip[] _buy = new AudioClip[0];
        [SerializeField] private AudioClip[] _bonus = new AudioClip[0];
        [SerializeField] private AudioClip[] _jail = new AudioClip[0];
        [SerializeField] private AudioClip[] _skill = new AudioClip[0];
        [SerializeField] private AudioClip[] _bankrupt = new AudioClip[0];

        private readonly Dictionary<int, long> _cash = new Dictionary<int, long>();
        private readonly Dictionary<int, int> _owned = new Dictionary<int, int>();
        private readonly HashSet<int> _bankruptSeen = new HashSet<int>();
        private bool _finishedPlayed;
        private bool _baseline;
        private int _lastPriority = -1;
        private float _lastAt = -10f;

        public bool IsWired => _source != null && _dice.Length > 0 && _step.Length > 0;

        private void OnEnable()
        {
            if (_ui != null)
            {
                _ui.RollClicked += OnRoll;
                _ui.DecisionShown += OnDecision;
                _ui.SnapshotRendered += OnSnapshot;
                _ui.ActionRequested += OnAction;
            }

            if (_board != null) _board.Stepped += OnStep;
            if (_celebration != null) _celebration.Started += OnMonopoly;
        }

        private void OnDisable()
        {
            if (_ui != null)
            {
                _ui.RollClicked -= OnRoll;
                _ui.DecisionShown -= OnDecision;
                _ui.SnapshotRendered -= OnSnapshot;
                _ui.ActionRequested -= OnAction;
            }

            if (_board != null) _board.Stepped -= OnStep;
            if (_celebration != null) _celebration.Started -= OnMonopoly;
        }

        private void OnRoll() => Play(_dice, PCash);
        private void OnStep(int tileId) => Play(_step, PStep, 0.3f);

        private void OnMonopoly()
        {
            Play(_monopoly, PMajor);
            MusicPlayer.Duck(0.3f, 5f);
        }

        private void OnAction(GameplayUiAction action)
        {
            if (action.Kind == GameplayUiActionKind.UseActiveSkill) Play(_skill, PBuy);
        }

        private void OnDecision(DecisionPrompt prompt)
        {
            switch (prompt.Kind)
            {
                case DecisionPromptKind.EventCard:
                case DecisionPromptKind.EventCardTargetSelection:
                    Play(_card, PCash);
                    break;
                case DecisionPromptKind.Jail:
                    Play(_jail, PBuy);
                    break;
                default:
                    Play(_popup, PPopup);
                    break;
            }
        }

        private void OnSnapshot(GameHUDSnapshot snapshot)
        {
            // Chọn MỘT tiếng ưu tiên cao nhất cho lần cập nhật này (tránh nhiều tiếng chồng nhau).
            AudioClip[] bank = null;
            int priority = -1;
            void Offer(AudioClip[] candidate, int p)
            {
                if (p > priority) { bank = candidate; priority = p; }
            }

            var owned = CountOwned(snapshot);
            foreach (var p in snapshot.Players)
            {
                _cash.TryGetValue(p.Id, out long before);
                bool known = _cash.ContainsKey(p.Id);
                long delta = p.Cash - before;
                owned.TryGetValue(p.Id, out int nowOwned);
                _owned.TryGetValue(p.Id, out int wasOwned);

                if (_baseline && known)
                {
                    if (p.Bankrupt && _bankruptSeen.Add(p.Id)) Offer(_bankrupt, PBonus);
                    else if (delta == StartBonus) Offer(_bonus, PBonus);
                    else if (nowOwned > wasOwned && delta < 0) Offer(_buy, PBuy);
                    else if (delta > 0) Offer(_gain, PCash);
                    else if (delta < 0) Offer(_pay, PCash);
                }

                _cash[p.Id] = p.Cash;
                _owned[p.Id] = nowOwned;
            }

            _baseline = true;
            if (bank != null) Play(bank, priority);

            if (snapshot.MatchFinished && !_finishedPlayed)
            {
                _finishedPlayed = true;
                Play(_win, PMajor);
                MusicPlayer.Duck(0.3f, 6f);
            }
        }

        private static Dictionary<int, int> CountOwned(GameHUDSnapshot snapshot)
        {
            var counts = new Dictionary<int, int>();
            foreach (var property in snapshot.Properties)
            {
                counts.TryGetValue(property.OwnerId, out int n);
                counts[property.OwnerId] = n + 1;
            }

            return counts;
        }

        /// <summary>Phát một clip ngẫu nhiên của nhóm, trừ khi một tiếng quan trọng hơn vừa phát (chống chồng chéo, chống ồn).</summary>
        private void Play(AudioClip[] bank, int priority, float gain = 1f)
        {
            if (_source == null || bank == null || bank.Length == 0)
            {
                return;
            }

            float since = Time.unscaledTime - _lastAt;
            float holdOff = priority == PStep ? 0.3f : 0.7f;
            if (since < holdOff && priority < _lastPriority)
            {
                return;
            }

            if (since < 0.25f && priority <= _lastPriority)
            {
                return;   // cùng mức, quá sát nhau
            }

            var clip = bank[Random.Range(0, bank.Length)];
            if (clip == null)
            {
                return;   // thiếu clip: bỏ qua an toàn
            }

            if (priority > PStep || since > holdOff)
            {
                _lastPriority = priority;
                _lastAt = Time.unscaledTime;
            }

            _source.PlayOneShot(clip, AudioSettingsStore.EffectiveSfx * gain);
        }
    }
}
