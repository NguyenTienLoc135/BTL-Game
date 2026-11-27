using System;
using System.Collections.Generic;
using BTLGame.Board;
using BTLGame.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BTLGame.Presentation
{
    /// <summary>
    /// Chế độ chọn ô để dịch chuyển: các ô được phép chọn bấp bênh nhẹ, ô dưới chuột nhô cao hơn, bấm vào ô thì phát TileChosen.
    /// Dải nhỏ phía trên chỉ nhắc và có nút "Bỏ qua" / "Hành động khác" — bàn cờ không bị che. Chỉ là presentation.
    /// </summary>
    public sealed class TeleportPicker : MonoBehaviour, ITilePickerUi
    {
        [SerializeField] private GameObject _banner;
        [SerializeField] private Text _message;
        [SerializeField] private Button _skipButton;
        [SerializeField] private Button _otherButton;
        [SerializeField] private float _bobHeight = 0.22f;
        [SerializeField] private float _hoverHeight = 0.5f;

        public event Action<int> TileChosen;
        public event Action SkipRequested;
        public event Action OtherActionsRequested;

        private sealed class Bobbing
        {
            public int TileId;
            public Bounds Area;
            public readonly List<Transform> Parts = new List<Transform>();
            public readonly List<Vector3> Home = new List<Vector3>();
        }

        private readonly List<Bobbing> _tiles = new List<Bobbing>();
        private float _startedAt;

        public bool IsPicking => _tiles.Count > 0;

        private void Awake()
        {
            if (_skipButton != null) _skipButton.onClick.AddListener(() => SkipRequested?.Invoke());
            if (_otherButton != null) _otherButton.onClick.AddListener(() => OtherActionsRequested?.Invoke());
            if (_banner != null) _banner.SetActive(false);
        }

        public void ShowTilePicker(string message, IReadOnlyList<int> tileIds, bool canSkip, bool hasOtherActions)
        {
            HideTilePicker();
            var wanted = new HashSet<int>(tileIds);
            var labels = GameObject.Find("Labels");
            foreach (var view in FindObjectsByType<BoardTileView>(FindObjectsInactive.Exclude))
            {
                if (!wanted.Contains(view.TileId)) continue;
                var body = view.transform.Find("Body");
                var renderer = body != null ? body.GetComponent<Renderer>() : null;
                if (renderer == null) continue;

                var item = new Bobbing { TileId = view.TileId, Area = renderer.bounds };
                item.Parts.Add(view.transform);
                // Chữ/hình in nằm trên một canvas chung: nhấc theo những thứ nằm trong phạm vi ô.
                if (labels != null)
                {
                    foreach (Transform label in labels.transform)
                        if (Inside(item.Area, label.position)) item.Parts.Add(label);
                }

                foreach (var part in item.Parts) item.Home.Add(part.position);
                _tiles.Add(item);
            }

            _startedAt = Time.unscaledTime;
            if (_message != null) _message.text = message;
            if (_skipButton != null) _skipButton.gameObject.SetActive(canSkip);
            if (_otherButton != null) _otherButton.gameObject.SetActive(hasOtherActions);
            if (_banner != null) _banner.SetActive(true);
        }

        public void HideTilePicker()
        {
            foreach (var item in _tiles)
                for (int i = 0; i < item.Parts.Count; i++)
                    if (item.Parts[i] != null) item.Parts[i].position = item.Home[i];
            _tiles.Clear();
            if (_banner != null) _banner.SetActive(false);
        }

        /// <summary>Ô đang cho chọn chứa điểm (x, z) trên mặt bàn; null nếu bấm ra ngoài.</summary>
        public int? TileAt(Vector3 boardPoint)
        {
            foreach (var item in _tiles)
                if (Inside(item.Area, boardPoint)) return item.TileId;
            return null;
        }

        private static bool Inside(Bounds area, Vector3 point)
            => point.x >= area.min.x && point.x <= area.max.x && point.z >= area.min.z && point.z <= area.max.z;

        private void Update()
        {
            if (_tiles.Count == 0) return;
            int? hovered = null;
            var mouse = Mouse.current;
            var camera = Camera.main;
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (mouse != null && camera != null && !overUi)
            {
                var surface = new Plane(Vector3.up, new Vector3(0f, _tiles[0].Area.max.y, 0f));
                var ray = camera.ScreenPointToRay(mouse.position.ReadValue());
                if (surface.Raycast(ray, out float distance)) hovered = TileAt(ray.GetPoint(distance));
            }

            float t = Time.unscaledTime - _startedAt;
            foreach (var item in _tiles)
            {
                float lift = item.TileId == hovered
                    ? _hoverHeight
                    : _bobHeight * (0.5f + 0.5f * Mathf.Sin(t * 4f - item.TileId * 0.55f));   // sóng nhẹ chạy quanh bàn
                for (int i = 0; i < item.Parts.Count; i++)
                    if (item.Parts[i] != null) item.Parts[i].position = item.Home[i] + Vector3.up * lift;
            }

            if (hovered.HasValue && mouse.leftButton.wasPressedThisFrame)
            {
                TileChosen?.Invoke(hovered.Value);
            }
        }

        private void OnDisable() => HideTilePicker();
    }
}
