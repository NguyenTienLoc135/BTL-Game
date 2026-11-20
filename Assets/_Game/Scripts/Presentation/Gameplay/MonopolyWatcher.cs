using System.Collections.Generic;
using BtlGame.Core;
using BTLGame.Board;
using BTLGame.UI;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace BTLGame.Presentation
{
    /// <summary>
    /// Theo dõi snapshot của Core: khi một người VỪA sở hữu trọn các thành phố của một quốc gia thì chiếu MonopolyCelebration (6 giây).
    /// Chỉ đọc snapshot (qua PresentationGameplayUi) để hiển thị, không tính luật hay ghi state.
    /// Bản Editor/Development: phím F9 chiếu thử màn này để kiểm tra giao diện.
    /// </summary>
    public sealed class MonopolyWatcher : MonoBehaviour
    {
        [SerializeField] private PresentationGameplayUi _ui;
        [SerializeField] private MonopolyCelebration _celebration;

        private Dictionary<CountryId, int> _previous;
        private GameHUDSnapshot _last;

        private void OnEnable()
        {
            if (_ui != null) _ui.SnapshotRendered += OnSnapshot;
        }

        private void OnDisable()
        {
            if (_ui != null) _ui.SnapshotRendered -= OnSnapshot;
        }

        private void OnSnapshot(GameHUDSnapshot snapshot)
        {
            _last = snapshot;
            var current = MonopolyDetector.Current(snapshot.Properties);
            if (_previous == null)
            {
                _previous = current; // lần đầu chỉ lấy mốc, không chiếu
                return;
            }

            foreach (var ev in MonopolyDetector.NewlyCompleted(_previous, current))
            {
                Celebrate(OwnerName(snapshot, ev.OwnerId), ev.TileIds);
            }

            _previous = current;
        }

        private static string OwnerName(GameHUDSnapshot snapshot, int ownerId)
        {
            foreach (var p in snapshot.Players)
            {
                if (p.Id == ownerId) return p.Name;
            }

            return "Người chơi";
        }

        private void Celebrate(string owner, IReadOnlyList<int> tiles)
        {
            if (_celebration == null)
            {
                return;
            }

            var names = new List<string>();
            var colors = new List<Color>();
            foreach (int tile in tiles)
            {
                var def = BoardVisualCatalog.Get(tile);
                names.Add(def.DisplayName);
                colors.Add(def.Color);
            }

            _celebration.Show(owner, BoardVisualCatalog.Get(tiles[0]).CountryName, names, colors);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            // Chiếu thử (chỉ Editor/Development build): F9.
            if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
            {
                string owner = _last != null && _last.Players.Count > 0 ? _last.Players[0].Name : "Bạn";
                Celebrate(owner, MonopolyDetector.CitiesOf(CountryId.Vietnam));
            }
        }
#endif
    }
}
