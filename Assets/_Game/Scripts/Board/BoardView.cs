using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
<<<<<<< HEAD
using BtlGame.Core;

namespace BtlGame.Board
{
    public sealed class BoardView : MonoBehaviour
    {
        private static readonly Color[] PlayerColors =
        {
            new Color(0.86f, 0.20f, 0.18f),
            new Color(0.14f, 0.48f, 0.82f),
            new Color(0.95f, 0.68f, 0.12f),
            new Color(0.20f, 0.62f, 0.38f)
        };

        [SerializeField] private BoardTileView[] _tiles = new BoardTileView[32];
        [SerializeField] private PlayerTokenView[] _players = new PlayerTokenView[4];
        [SerializeField, Min(0.05f)] private float _stepDuration = 0.18f;
        [SerializeField, Min(0f)] private float _tokenHopHeight = 0.35f;

        private bool _isAnimating;
        private long _lastCompletedActionId;

        public void Bind(BoardTileView[] tiles, PlayerTokenView[] players)
        {
            if (tiles == null || tiles.Length != 32) throw new ArgumentException("BoardView cần đúng 32 ô.", nameof(tiles));
            if (players == null || players.Length > 4) throw new ArgumentException("BoardView hỗ trợ tối đa 4 quân.", nameof(players));
            _tiles = tiles;
            _players = players;
        }

        public void ApplyState(BoardPresentationState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            EnsureBindings();

            foreach (BoardTileView tile in _tiles)
            {
                tile.SetOwner(null, Color.white);
                tile.SetPropertyLevel(null);
            }
            foreach (BoardPropertyPresentation property in state.Properties)
            {
                BoardTileView tile = GetTile(property.TileId);
                tile.SetOwner(property.OwnerId, property.OwnerId.HasValue ? GetPlayerColor(property.OwnerId.Value) : Color.white);
                tile.SetPropertyLevel(property.Level);
            }

            var seen = new HashSet<int>();
            foreach (BoardPlayerPresentation player in state.Players)
            {
                if (player.PlayerId < 0 || player.PlayerId >= _players.Length || !seen.Add(player.PlayerId))
                    throw new ArgumentException("Dữ liệu quân cờ có PlayerId trùng hoặc ngoài phạm vi.", nameof(state));
                PlayerTokenView token = _players[player.PlayerId];
                GetTile(player.Position);
                token.SetVisible(player.IsVisible);
                token.SetColor(GetPlayerColor(player.PlayerId));
                token.SetBoardPosition(transform.TransformPoint(GetPlayerPosition(player.PlayerId, player.Position)));
            }
            for (int i = 0; i < _players.Length; i++)
                if (!seen.Contains(i)) _players[i].SetVisible(false);
        }

        public void PlayPath(int playerId, IReadOnlyList<int> path, long actionId, Action<long> onPresentationCompleted)
        {
            EnsureBindings();
            if (_isAnimating) throw new InvalidOperationException("BoardView đang trình bày một đường đi khác.");
            if (playerId < 0 || playerId >= _players.Length) throw new ArgumentOutOfRangeException(nameof(playerId));
            if (path == null || path.Count == 0) throw new ArgumentException("Đường đi phải có ít nhất một ô.", nameof(path));
            if (actionId <= _lastCompletedActionId) throw new InvalidOperationException("ActionId cũ hoặc đã trình bày.");
            for (int i = 0; i < path.Count; i++) GetTile(path[i]);
            StartCoroutine(AnimatePath(playerId, path, actionId, onPresentationCompleted));
        }

        public void HighlightTile(int tileId, bool highlighted)
        {
            GetTile(tileId).SetHighlighted(highlighted);
        }
=======

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
>>>>>>> develop

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

<<<<<<< HEAD
        public static Vector3 GetPlayerPosition(int playerId, int tileId)
        {
            Vector3 tilePosition = GetTilePosition(tileId);
            Vector3 inward = new Vector3(-tilePosition.x, 0f, -tilePosition.z).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, inward).normalized;
            int slot = playerId % 4;
            float sideOffset = (slot % 2 == 0 ? -1f : 1f) * 0.18f;
            float inwardOffset = slot < 2 ? -0.18f : 0.18f;
            return tilePosition + side * sideOffset + inward * inwardOffset + Vector3.up * 0.12f;
        }

        public static Color GetPlayerColor(int playerId)
        {
            if (playerId < 0 || playerId >= PlayerColors.Length) throw new ArgumentOutOfRangeException(nameof(playerId));
            return PlayerColors[playerId];
        }

        private IEnumerator AnimatePath(int playerId, IReadOnlyList<int> path, long actionId, Action<long> callback)
        {
            _isAnimating = true;
            PlayerTokenView token = _players[playerId];
            token.SetVisible(true);
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 start = token.transform.position;
                Vector3 target = transform.TransformPoint(GetPlayerPosition(playerId, path[i]));
=======
        /// <summary>
        /// Vị trí thế giới của quân ở ghế seatIndex trên ô tileId, tính từ vị trí THẬT của tile trong Scene
        /// (không giả định lưới cố định) nên dùng được cho mọi bố cục bàn cờ. Hướng "vào trong" = về tâm bàn cờ.
        /// </summary>
        private Vector3 PlayerWorldPosition(int seatIndex, int tileId)
        {
            Vector3 tile = GetTile(tileId).transform.position;
            Vector3 inward = transform.position - tile;
            inward.y = 0f;
            inward = inward.sqrMagnitude > 0.0001f ? inward.normalized : Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, inward).normalized;
            float sideOffset = (seatIndex % 2 == 0 ? -1f : 1f) * _tokenSpread;
            float inwardOffset = (seatIndex < 2 ? -1f : 1f) * _tokenSpread * 0.9f;
            return tile + side * sideOffset + inward * inwardOffset + Vector3.up * _tokenLift;
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
            for (int i = 0; i < movement.Path.Count; i++)
            {
                Vector3 from = token.transform.position;
                Vector3 to = PlayerWorldPosition(token.SeatIndex, movement.Path[i]);
>>>>>>> develop
                float elapsed = 0f;
                while (elapsed < _stepDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / _stepDuration);
<<<<<<< HEAD
                    float smooth = t * t * (3f - 2f * t);
                    token.SetBoardPosition(Vector3.LerpUnclamped(start, target, smooth));
                    token.SetMovingPose(Mathf.Sin(t * Mathf.PI) * _tokenHopHeight);
                    yield return null;
                }
                token.SetBoardPosition(target);
                token.SetMovingPose(0f);
            }
            _lastCompletedActionId = actionId;
            _isAnimating = false;
            callback?.Invoke(actionId);
=======
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
>>>>>>> develop
        }

        private BoardTileView GetTile(int tileId)
        {
            if (tileId < 0 || tileId >= _tiles.Length || _tiles[tileId] == null || _tiles[tileId].TileId != tileId)
<<<<<<< HEAD
                throw new InvalidOperationException("BoardView không có mapping hợp lệ cho tileId " + tileId + ".");
            return _tiles[tileId];
        }

        private void EnsureBindings()
        {
            if (_tiles == null || _tiles.Length != 32 || _players == null || _players.Length > 4)
                throw new InvalidOperationException("BoardView chưa được bind với 32 ô và tối đa 4 quân.");
            for (int i = 0; i < _tiles.Length; i++) GetTile(i);
            for (int i = 0; i < _players.Length; i++)
                if (_players[i] == null || _players[i].PlayerId != i)
                    throw new InvalidOperationException("BoardView cần quân có PlayerId trùng chỉ số mảng.");
=======
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
>>>>>>> develop
        }
    }
}
