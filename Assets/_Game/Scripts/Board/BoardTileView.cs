using UnityEngine;
using BtlGame.Core;

namespace BtlGame.Board
{
    public sealed class BoardTileView : MonoBehaviour
    {
        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private Renderer _ownerMarker;
        [SerializeField] private TextMesh _nameLabel;
        [SerializeField] private TextMesh _idLabel;
        [SerializeField] private Transform[] _propertyStages = new Transform[5];
        [SerializeField] private Renderer[] _highlightRenderers;

        private MaterialPropertyBlock _propertyBlock;
        [SerializeField] private int _tileId = -1;

        public int TileId => _tileId;

        public void Bind(TileDefinition definition, Color tileColor)
        {
            if (definition == null) throw new System.ArgumentNullException(nameof(definition));
            _tileId = definition.Id;
            if (_bodyRenderer == null) _bodyRenderer = transform.Find("TileBody")?.GetComponent<Renderer>();
            if (_ownerMarker == null) _ownerMarker = transform.Find("OwnerMarker")?.GetComponent<Renderer>();
            if (_nameLabel == null) _nameLabel = transform.Find("NameLabel")?.GetComponent<TextMesh>();
            if (_idLabel == null) _idLabel = transform.Find("IdLabel")?.GetComponent<TextMesh>();
            if (_bodyRenderer != null) SetRendererColor(_bodyRenderer, tileColor);
            if (_nameLabel != null) _nameLabel.text = definition.Name;
            if (_idLabel != null) _idLabel.text = definition.Id.ToString("00");
            if (_ownerMarker != null) _ownerMarker.gameObject.SetActive(false);
            SetPropertyLevel(null);
            SetHighlighted(false);
        }

        public void ConfigureVisuals(Renderer bodyRenderer, Renderer ownerMarker, TextMesh nameLabel,
            TextMesh idLabel, Transform[] propertyStages, Renderer[] highlightRenderers)
        {
            _bodyRenderer = bodyRenderer;
            _ownerMarker = ownerMarker;
            _nameLabel = nameLabel;
            _idLabel = idLabel;
            _propertyStages = propertyStages ?? System.Array.Empty<Transform>();
            _highlightRenderers = highlightRenderers ?? System.Array.Empty<Renderer>();
        }

        public void SetOwner(int? ownerId, Color ownerColor)
        {
            if (_ownerMarker == null) return;
            _ownerMarker.gameObject.SetActive(ownerId.HasValue);
            if (!ownerId.HasValue) return;
            SetRendererColor(_ownerMarker, ownerColor);
        }

        public void SetPropertyLevel(PropertyLevel? level)
        {
            for (int i = 0; i < _propertyStages.Length; i++)
                if (_propertyStages[i] != null)
                    _propertyStages[i].gameObject.SetActive(level.HasValue && (int)level.Value == i + 1);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_highlightRenderers == null) return;
            foreach (Renderer target in _highlightRenderers)
            {
                if (target == null) continue;
                target.gameObject.SetActive(highlighted);
            }
        }

        private void SetRendererColor(Renderer target, Color color)
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            target.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", color);
            _propertyBlock.SetColor("_Color", color);
            target.SetPropertyBlock(_propertyBlock);
        }
    }
}
