using System;
using UnityEngine;

namespace BTLGame.Board
{
    public sealed class PlayerTokenView : MonoBehaviour
    {
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Renderer _accentRenderer;
        [SerializeField] private int _playerId = -1;
        [SerializeField] private int _seatIndex = -1;
        // Mô hình mặc định nhìn về +z (quay lưng về camera cố định ở phía -z) → xoay nghỉ 180° để quân nhìn về phía người chơi.
        [SerializeField] private float _restYaw = 180f;

        public int PlayerId => _playerId;
        public int SeatIndex => _seatIndex;

        public void Bind(int playerId, int seatIndex, Color color)
        {
            if (playerId < 0) throw new ArgumentOutOfRangeException(nameof(playerId));
            if (seatIndex < 0 || seatIndex > 3) throw new ArgumentOutOfRangeException(nameof(seatIndex));
            _playerId = playerId;
            _seatIndex = seatIndex;
            if (_visualRoot == null) _visualRoot = transform.Find("VisualRoot");
            if (_visualRoot != null) _visualRoot.localRotation = Quaternion.Euler(0f, _restYaw, 0f);
            if (_accentRenderer == null) _accentRenderer = transform.Find("AccentRing")?.GetComponent<Renderer>();
            SetColor(color);
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);
        public void SetPosition(Vector3 worldPosition) => transform.position = worldPosition;

        public void SetMovingPose(float hopHeight, float progress)
        {
            if (_visualRoot == null) return;
            _visualRoot.localPosition = Vector3.up * hopHeight;
            _visualRoot.localRotation = Quaternion.Euler(0f, _restYaw + progress * 360f, 0f);
        }

        private void SetColor(Color color)
        {
            if (_accentRenderer == null) return;
            var block = new MaterialPropertyBlock();
            _accentRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            _accentRenderer.SetPropertyBlock(block);
        }
    }
}
