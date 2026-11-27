using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BTLGame.Board
{
    public sealed class BoardView : MonoBehaviour
    {
        private static readonly Color[] SeatColors =
        {
            new Color(0.95f, 0.65f, 0.25f),
            new Color(0.95f, 0.50f, 0.70f),
            new Color(0.45f, 0.75f, 0.95f),
            new Color(0.95f, 0.85f, 0.35f)
        };

        [SerializeField] private BoardTileView[] _tiles = new BoardTileView[32];
        [SerializeField] private PlayerTokenView[] _tokens = new PlayerTokenView[4];
        [SerializeField, Min(0.05f)] private float _stepDuration = 0.16f;
        [SerializeField, Min(0f)] private float _hopHeight = 0.36f;
        [SerializeField, Min(0f)] private float _tokenLift = 0.12f;      // độ cao quân so với gốc ô (mặt ô cao hơn gốc tùy mô hình)
        [SerializeField, Min(0.05f)] private float _tokenSpread = 0.22f; // khoảng lệch giữa các quân trên cùng một ô

        private readonly Dictionary<int, PlayerTokenView> _tokensByPlayerId = new Dictionary<int, PlayerTokenView>();
        private readonly Dictionary<int, int> _seatTiles = new Dictionary<int, int>();   // ghế đang hiện -> ô (để dàn quân cùng ô)
        [SerializeField, Min(0f)] private float _tokenDown = 0.22f;      // quân tụt về phía đáy so với tâm ô
        private bool _animating;
        private bool _hasCompletedAction;
        private long _lastCompletedActionId;
        private bool _visualsBound;

        public event Action<long, long> PresentationCompleted;

        /// <summary>Phát sau mỗi bước quân đi qua một ô (cho hiệu ứng âm thanh); không ảnh hưởng luật.</summary>
        public event Action<int> Stepped;
        public bool IsAnimating => _animating;

        public void Configure(BoardTileView[] tiles, PlayerTokenView[] tokens)
        {
            if (tiles == null || tiles.Length != 32) throw new ArgumentException("Cần đúng 32 tile view.", nameof(tiles));
            if (tokens == null || tokens.Length != 4) throw new ArgumentException("Cần 4 slot quân presentation.", nameof(tokens));
            _tiles = tiles;
            _tokens = tokens;
            _visualsBound = false;
        }

        public void ApplySnapshot(BoardPresentationSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (_animating) throw new InvalidOperationException("Không cập nhật snapshot giữa animation.");
            ValidateBindings();
            if (!_visualsBound)
            {
                // PropertyBlock (màu ô) không được serialize khi dựng prefab trong Editor.
                foreach (BoardTileView tile in _tiles) tile.Bind(BoardVisualCatalog.Get(tile.TileId));
                _visualsBound = true;
            }
            _tokensByPlayerId.Clear();
            var occupiedSeats = new HashSet<int>();
            foreach (PlayerTokenView token in _tokens) token.SetVisible(false);
            foreach (BoardTileView tile in _tiles)
            {
                tile.SetOwner(null, Color.white);
                tile.SetLevel(null);
            }

            _seatTiles.Clear();
            foreach (BoardPlayerVisualState player in snapshot.Players)
                if (player.IsVisible) _seatTiles[player.SeatIndex] = player.TileId;

            foreach (BoardPlayerVisualState player in snapshot.Players)
            {
                if (player.PlayerId < 0 || player.SeatIndex < 0 || player.SeatIndex >= _tokens.Length ||
                    _tokensByPlayerId.ContainsKey(player.PlayerId) || !occupiedSeats.Add(player.SeatIndex))
                    throw new ArgumentException("PlayerId/seatIndex bị trùng hoặc ngoài phạm vi.", nameof(snapshot));
                PlayerTokenView token = _tokens[player.SeatIndex];
                GetTile(player.TileId);
                token.Bind(player.PlayerId, player.SeatIndex, GetSeatColor(player.SeatIndex));
                token.SetPosition(PlayerWorldPosition(player.SeatIndex, player.TileId));
                token.SetVisible(player.IsVisible);
                _tokensByPlayerId.Add(player.PlayerId, token);
            }
            foreach (BoardPropertyVisualState property in snapshot.Properties)
            {
                BoardTileView tile = GetTile(property.TileId);
                tile.SetLevel(property.Level);
                tile.SetOwner(property.OwnerId, GetOwnerColor(property.OwnerId, snapshot.Players));
            }
        }

        public void PlayMovement(MovementPresentation movement)
        {
            if (movement == null) throw new ArgumentNullException(nameof(movement));
            ValidateBindings();
            if (_animating) throw new InvalidOperationException("Đang chạy animation của một action khác.");
            if (_hasCompletedAction && movement.ActionId <= _lastCompletedActionId)
                throw new InvalidOperationException("ActionId cũ hoặc callback presentation đã chạy.");
            if (!_tokensByPlayerId.TryGetValue(movement.PlayerId, out PlayerTokenView token))
                throw new InvalidOperationException("PlayerId chưa có trong snapshot presentation.");
            if (movement.Path == null || movement.Path.Count == 0)
                throw new ArgumentException("Path cần ít nhất một tile.", nameof(movement));
            foreach (int tileId in movement.Path) GetTile(tileId);
            StartCoroutine(Animate(token, movement));
        }

        public void HighlightTile(int tileId, bool highlighted) => GetTile(tileId).SetHighlighted(highlighted);

        /// <summary>Hiện dấu lễ hội đúng trên các ô được chọn (3 thành phố cố định của trận), ẩn ở ô khác.</summary>
        public void ShowFestivalTiles(IReadOnlyList<int> tileIds)
        {
            ValidateBindings();
            var chosen = new HashSet<int>(tileIds);
            for (int id = 0; id < _tiles.Length; id++) GetTile(id).SetFestival(chosen.Contains(id));
        }

        public static Vector3 GetTilePosition(int tileId)
        {
            if (tileId < 0 || tileId >= 32) throw new ArgumentOutOfRangeException(nameof(tileId));
            const float spacing = 1.55f;
            int x;
            int z;
            if (tileId <= 8) { x = tileId; z = 0; }
            else if (tileId <= 16) { x = 8; z = tileId - 8; }
            else if (tileId <= 24) { x = 24 - tileId; z = 8; }
            else { x = 0; z = 32 - tileId; }
            return new Vector3((x - 4) * spacing, 0f, (z - 4) * spacing);
        }

        /// <summary>
        /// Vị trí quân trên ô: giữa ô nhưng hơi tụt về phía đáy màn (-z, phía camera). Một quân đứng chính giữa theo chiều ngang;
        /// nhiều quân cùng ô thì dàn cạnh nhau (2) hoặc thành lưới 2×2 (3–4) quanh điểm đó, vẫn nằm trong ô.
        /// </summary>
        private Vector3 PlayerWorldPosition(int seatIndex, int tileId)
        {
            Vector3 tile = GetTile(tileId).transform.position;
            int count = 1;
            int order = 0;
            foreach (KeyValuePair<int, int> other in _seatTiles)
            {
                if (other.Key == seatIndex || other.Value != tileId) continue;
                count++;
                if (other.Key < seatIndex) order++;
            }

            float x = 0f;
            float z = 0f;
            if (count > 1) x = (order % 2 == 0 ? -1f : 1f) * _tokenSpread;
            if (count > 2) z = (order < 2 ? 1f : -1f) * _tokenSpread * 0.5f;
            if (count == 3 && order == 2) x = 0f;
            return tile + new Vector3(x, _tokenLift, z - _tokenDown);
        }

        public static Vector3 GetPlayerPosition(int seatIndex, int tileId)
        {
            Vector3 tile = GetTilePosition(tileId);
            Vector3 inward = new Vector3(-tile.x, 0f, -tile.z).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, inward).normalized;
            float sideOffset = (seatIndex % 2 == 0 ? -1f : 1f) * 0.20f;
            float inwardOffset = seatIndex < 2 ? -0.18f : 0.18f;
            return tile + side * sideOffset + inward * inwardOffset + Vector3.up * 0.12f;
        }

        public static Color GetSeatColor(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= SeatColors.Length) throw new ArgumentOutOfRangeException(nameof(seatIndex));
            return SeatColors[seatIndex];
        }

        private IEnumerator Animate(PlayerTokenView token, MovementPresentation movement)
        {
            _animating = true;
            if (movement.Fly)
            {
                // Dịch chuyển: một đường bay vòng cung từ ô hiện tại tới ô đích.
                int target = movement.Path[movement.Path.Count - 1];
                Vector3 start = token.transform.position;
                _seatTiles[token.SeatIndex] = target;
                Vector3 end = PlayerWorldPosition(token.SeatIndex, target);
                const float flightSeconds = 1.3f;
                float flown = 0f;
                while (flown < flightSeconds)
                {
                    flown += Time.deltaTime;
                    float t = Mathf.Clamp01(flown / flightSeconds);
                    float eased = t * t * (3f - 2f * t);
                    token.SetPosition(Vector3.LerpUnclamped(start, end, eased) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 3.5f));
                    token.SetMovingPose(0f, eased);
                    yield return null;
                }

                token.SetPosition(end);
                token.SetMovingPose(0f, 0f);
                Stepped?.Invoke(target);
            }
            else
            for (int i = 0; i < movement.Path.Count; i++)
            {
                Vector3 from = token.transform.position;
                _seatTiles[token.SeatIndex] = movement.Path[i];
                Vector3 to = PlayerWorldPosition(token.SeatIndex, movement.Path[i]);
                float elapsed = 0f;
                while (elapsed < _stepDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / _stepDuration);
                    float eased = t * t * (3f - 2f * t);
                    token.SetPosition(Vector3.LerpUnclamped(from, to, eased));
                    token.SetMovingPose(Mathf.Sin(t * Mathf.PI) * _hopHeight, i + t);
                    yield return null;
                }
                token.SetPosition(to);
                token.SetMovingPose(0f, i + 1f);
                Stepped?.Invoke(movement.Path[i]);
            }
            _lastCompletedActionId = movement.ActionId;
            _hasCompletedAction = true;
            _animating = false;
            PresentationCompleted?.Invoke(movement.ActionId, movement.LandingId);
        }

        private BoardTileView GetTile(int tileId)
        {
            if (tileId < 0 || tileId >= _tiles.Length || _tiles[tileId] == null || _tiles[tileId].TileId != tileId)
                throw new InvalidOperationException("Thiếu mapping BoardTile tileId=" + tileId + ".");
            return _tiles[tileId];
        }

        private void ValidateBindings()
        {
            if (_tiles == null || _tiles.Length != 32 || _tokens == null || _tokens.Length != 4)
                throw new InvalidOperationException("BoardView cần 32 tile và 4 slot quân.");
            for (int i = 0; i < 32; i++) GetTile(i);
            for (int i = 0; i < 4; i++)
                if (_tokens[i] == null) throw new InvalidOperationException("Thiếu PlayerTokenView seat " + i + ".");
        }

        private static Color GetOwnerColor(int? ownerId, IReadOnlyList<BoardPlayerVisualState> players)
        {
            if (!ownerId.HasValue) return Color.white;
            foreach (BoardPlayerVisualState player in players)
                if (player.PlayerId == ownerId.Value) return GetSeatColor(player.SeatIndex);
            return new Color(0.35f, 0.38f, 0.40f);
        }
    }
}
