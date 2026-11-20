using UnityEngine;

namespace BTLGame.Board
{
    /// <summary>
    /// Ánh xạ tileId (0–31) → Transform trong Scene Gameplay, phục vụ view quân cờ/công trình.
    /// KHÔNG chứa luật, tiền hay trạng thái trận; Core quyết định vị trí, view chỉ hiển thị.
    /// Do GameplayBoardBuilder tạo và gán sẵn mảng _tileAnchors.
    /// </summary>
    public sealed class BoardTileAnchors : MonoBehaviour
    {
        public const int TileCount = 32;
        private const float TokenSpacing = 0.55f;

        [SerializeField] private Transform[] _tileAnchors = new Transform[TileCount];

        public Transform GetTileAnchor(int tileId)
        {
            if (tileId < 0 || tileId >= TileCount || _tileAnchors == null || tileId >= _tileAnchors.Length)
            {
                Debug.LogError("[BoardTileAnchors] tileId ngoài 0–31: " + tileId, this);
                return null;
            }

            return _tileAnchors[tileId];
        }

        /// <summary>Vị trí đặt quân cờ: chia tối đa 4 chỗ (2x2) trên một ô để các quân không chồng nhau.</summary>
        public Vector3 GetTokenPosition(int tileId, int slot)
        {
            var anchor = GetTileAnchor(tileId);
            if (anchor == null)
            {
                return Vector3.zero;
            }

            float x = (slot % 2 == 0 ? -1f : 1f) * TokenSpacing * 0.5f;
            float z = (slot / 2 % 2 == 0 ? -1f : 1f) * TokenSpacing * 0.5f;
            return anchor.position + new Vector3(x, 0f, z);
        }

        private void OnValidate()
        {
            if (_tileAnchors == null || _tileAnchors.Length != TileCount)
            {
                System.Array.Resize(ref _tileAnchors, TileCount);
            }
        }
    }
}
