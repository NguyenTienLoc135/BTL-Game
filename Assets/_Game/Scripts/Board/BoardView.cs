using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
                float elapsed = 0f;
                while (elapsed < _stepDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / _stepDuration);
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
        }

        private BoardTileView GetTile(int tileId)
        {
            if (tileId < 0 || tileId >= _tiles.Length || _tiles[tileId] == null || _tiles[tileId].TileId != tileId)
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
        }
    }
}
