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
        // Dấu "thành phố lễ hội cố định" (+50% thuê), chọn ngẫu nhiên 3 ô đầu trận.
        [SerializeField] private GameObject _festivalMarker;
        // Chòi bãi biển: chỉ hiện khi bãi có chủ; mái/cờ tô đúng màu ghế của chủ (cùng màu vòng dưới chân quân cờ).
        [SerializeField] private GameObject _ownerHut;
        [SerializeField] private Renderer[] _ownerHutAccents = new Renderer[0];

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
            if (_ownerHut != null) _ownerHut.SetActive(false);
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
            if (_ownerHut != null)
            {
                _ownerHut.SetActive(ownerId.HasValue);
                if (ownerId.HasValue && _ownerHutAccents != null)
                    foreach (Renderer accent in _ownerHutAccents)
                        if (accent != null) SetColor(accent, color);
            }

            TintBuildings(ownerId.HasValue, color);
            if (_ownerMarker == null) return;
            _ownerMarker.gameObject.SetActive(ownerId.HasValue);
            if (ownerId.HasValue) SetColor(_ownerMarker, color);
        }

        /// <summary>
        /// Công trình cùng một kiểu cho mọi người, chỉ đổi màu theo màu ghế của chủ (cùng màu vòng dưới chân quân cờ):
        /// tường và cờ đúng màu, mái/gờ/bệ màu sẫm hơn; cửa, cửa sổ, cọc và quả cầu vàng giữ nguyên. Không chủ thì trả màu gốc.
        /// </summary>
        private void TintBuildings(bool owned, Color color)
        {
            if (_propertyStages == null) return;
            var dark = new Color(color.r * 0.72f, color.g * 0.72f, color.b * 0.72f, 1f);
            foreach (Transform stage in _propertyStages)
            {
                if (stage == null) continue;
                foreach (Renderer part in stage.GetComponentsInChildren<Renderer>(true))
                {
                    string name = part.name;
                    bool wall = name.StartsWith("Wall") || name == "Flag";
                    bool trim = name.StartsWith("Roof") || name == "Platform";
                    if (!wall && !trim) continue;
                    if (!owned) part.SetPropertyBlock(null);
                    else SetColor(part, wall ? color : dark);
                }
            }
        }

        public void SetLevel(PropertyVisualLevel? level)
        {
            if (_propertyStages == null) return;
            for (int i = 0; i < _propertyStages.Length; i++)
                if (_propertyStages[i] != null)
                    _propertyStages[i].gameObject.SetActive(level.HasValue && (int)level.Value == i + 1);
        }

        public void SetFestival(bool value)
        {
            if (_festivalMarker != null) _festivalMarker.SetActive(value);
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
