using System;
using TMPro;
using UnityEngine;

namespace BTLGame.Board
{
    public sealed class BoardTileView : MonoBehaviour
    {
        [SerializeField] private int _tileId = -1;
        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private Renderer _ownerMarker;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _detailLabel;
        [SerializeField] private Transform[] _propertyStages = new Transform[5];
        [SerializeField] private GameObject _highlightObject;

        private MaterialPropertyBlock _block;
        public int TileId => _tileId;

        public void Configure(int tileId, Renderer body, Renderer owner, TMP_Text name,
            TMP_Text detail, Transform[] stages, GameObject highlight)
        {
            _tileId = tileId;
            _bodyRenderer = body;
            _ownerMarker = owner;
            _nameLabel = name;
            _detailLabel = detail;
            _propertyStages = stages ?? Array.Empty<Transform>();
            _highlightObject = highlight;
        }

        public void Bind(BoardTileVisualDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (_tileId >= 0 && _tileId != definition.TileId)
                throw new InvalidOperationException("TileView ID và định nghĩa không khớp.");
            _tileId = definition.TileId;
            if (_bodyRenderer != null) SetColor(_bodyRenderer, definition.Color);
            if (_ownerMarker != null) _ownerMarker.gameObject.SetActive(false);
            if (_nameLabel != null) _nameLabel.text = definition.DisplayName;
            if (_detailLabel != null)
            {
                string line = definition.CountryName;
                if (!string.IsNullOrEmpty(definition.DisplayCode)) line += "  ·  " + definition.DisplayCode;
                if (!string.IsNullOrEmpty(definition.PriceLabel)) line += "\n" + definition.PriceLabel;
                _detailLabel.text = line;
            }
            SetLevel(null);
            SetHighlighted(false);
        }

        public void SetOwner(int? ownerId, Color color)
        {
            if (_ownerMarker == null) return;
            _ownerMarker.gameObject.SetActive(ownerId.HasValue);
            if (ownerId.HasValue) SetColor(_ownerMarker, color);
        }

        public void SetLevel(PropertyVisualLevel? level)
        {
            if (_propertyStages == null) return;
            for (int i = 0; i < _propertyStages.Length; i++)
                if (_propertyStages[i] != null)
                    _propertyStages[i].gameObject.SetActive(level.HasValue && (int)level.Value == i + 1);
        }

        public void SetHighlighted(bool value)
        {
            if (_highlightObject != null) _highlightObject.SetActive(value);
        }

        private void SetColor(Renderer target, Color color)
        {
            _block ??= new MaterialPropertyBlock();
            target.GetPropertyBlock(_block);
            _block.SetColor("_BaseColor", color);
            _block.SetColor("_Color", color);
            target.SetPropertyBlock(_block);
        }
    }
}
