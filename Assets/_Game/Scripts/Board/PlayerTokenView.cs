using System;
using UnityEngine;

namespace BtlGame.Board
{
    public sealed class PlayerTokenView : MonoBehaviour
    {
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Renderer[] _accentRenderers;

        [SerializeField] private int _playerId;
        public int PlayerId => _playerId;

        public void Bind(int playerId, Color color)
        {
            if (playerId < 0 || playerId > 3) throw new ArgumentOutOfRangeException(nameof(playerId));
            _playerId = playerId;
            if (_visualRoot == null) _visualRoot = transform.Find("VisualRoot");
            if (_accentRenderers == null || _accentRenderers.Length == 0)
            {
                Transform accent = transform.Find("AccentRing");
                if (accent != null) _accentRenderers = new[] { accent.GetComponent<Renderer>() };
            }
            SetColor(color);
        }

        public void SetColor(Color color)
        {
            if (_accentRenderers == null) return;
            var block = new MaterialPropertyBlock();
            foreach (Renderer target in _accentRenderers)
            {
                if (target == null) continue;
                target.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                target.SetPropertyBlock(block);
            }
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        public void SetBoardPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void SetMovingPose(float hop)
        {
            if (_visualRoot == null) return;
            _visualRoot.localPosition = Vector3.up * hop;
            _visualRoot.localRotation = Quaternion.Euler(0f, hop * 140f, 0f);
        }
    }
}
