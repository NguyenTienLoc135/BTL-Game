#if UNITY_EDITOR
using System.Collections.Generic;
using BTLGame.Board;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static BTLGame.EditorTools.UiKit;

namespace BTLGame.EditorTools
{
    /// <summary>
    /// Dựng bàn cờ 3D 32 ô (PRESENTATION) cho Scene Gameplay; được GameplaySceneBuilder gọi.
    /// Tên/loại/màu ô lấy từ BoardVisualCatalog (nguồn duy nhất, của nhóm B); luật và giá do Core quản lý.
    /// Mỗi ô là một BoardTileView để BoardView (Codex) đặt quân, chủ sở hữu và công trình theo trạng thái Core.
    /// Bố cục (người dùng đã chọn): ô 0 ở góc dưới-phải, đi theo chiều kim đồng hồ;
    /// góc 0/8/16/24 = Xuất phát / Nhà tù / Tổ chức sự kiện / Dịch chuyển.
    /// </summary>
    internal static class GameplayBoardBuilder
    {
        // Hình học: 4 góc 3x3, mỗi cạnh 7 ô rộng 2 x sâu 3.
        private const float Corner = 3f;
        private const float SideWidth = 2f;
        private const float Depth = 3f;
        private const float Half = 10f;
        private const float EdgeCenter = Half - Depth / 2f; // 8.5
        private const float SlabHeight = 0.4f;
        private const int TileCount = 32;
        private static readonly Color CreamColor = new Color(0.99f, 0.96f, 0.88f);
        private static readonly Color SandYellow = new Color(0.97f, 0.89f, 0.64f);

        private const string BuildingDir = "Assets/_Game/Prefabs/Buildings/";
        private const string TokenDir = "Assets/_Game/Prefabs/Characters/";

        private enum T { Start, City, Beach, Jail, Card, Festival, Teleport, Tax }

        private readonly struct Tile
        {
            public readonly T Type;
            public readonly string Name;
            public readonly Color Color;

            public Tile(T type, string name, Color color)
            {
                Type = type;
                Name = name;
                Color = color;
            }
        }

        private static T ToType(BoardTileAppearance a)
        {
            switch (a)
            {
                case BoardTileAppearance.Start: return T.Start;
                case BoardTileAppearance.City: return T.City;
                case BoardTileAppearance.Beach: return T.Beach;
                case BoardTileAppearance.Jail: return T.Jail;
                case BoardTileAppearance.Event: return T.Card;
                case BoardTileAppearance.Festival: return T.Festival;
                case BoardTileAppearance.Teleport: return T.Teleport;
                default: return T.Tax;
            }
        }

        private static Tile GetTile(int id)
        {
            var def = BoardVisualCatalog.Get(id);
            return new Tile(ToType(def.Appearance), def.DisplayName, def.Color);
        }

        internal static void Build(Transform root)
        {
            var tilesRoot = new GameObject("Tiles").transform;
            tilesRoot.SetParent(root, false);
            var tileViews = BuildTiles(tilesRoot, root);
            BuildCenter(root);
            BuildSurroundings(root);
            var tokens = BuildTokens(root);

            // BoardView (Codex) điều khiển quân/chủ ô/công trình theo snapshot của Core; vị trí lấy từ Transform thật của từng ô.
            var view = root.gameObject.AddComponent<BoardView>();
            var so = new SerializedObject(view);
            var tilesProp = so.FindProperty("_tiles");
            tilesProp.arraySize = tileViews.Length;
            for (int i = 0; i < tileViews.Length; i++) tilesProp.GetArrayElementAtIndex(i).objectReferenceValue = tileViews[i];
            var tokensProp = so.FindProperty("_tokens");
            tokensProp.arraySize = tokens.Length;
            for (int i = 0; i < tokens.Length; i++) tokensProp.GetArrayElementAtIndex(i).objectReferenceValue = tokens[i];
            so.FindProperty("_tokenLift").floatValue = SlabHeight + 0.14f;
            so.FindProperty("_tokenSpread").floatValue = 0.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>4 quân cờ từ prefab của nhóm (PlayerToken_*), phóng to cho vừa ô lớn; BoardView bật/tắt theo số người.</summary>
        private static PlayerTokenView[] BuildTokens(Transform root)
        {
            string[] names = { "Red", "Blue", "Gold", "Green" };
            var result = new PlayerTokenView[4];
            for (int i = 0; i < 4; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TokenDir + "PlayerToken_" + names[i] + ".prefab");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = "PlayerTokenSlot_" + i;
                go.transform.SetParent(root, false);
                go.transform.localScale = Vector3.one * 2.2f;
                result[i] = go.GetComponent<PlayerTokenView>();
            }

            return result;
        }

        // ------------------------------------------------------------ vị trí 32 ô

        /// <summary>Tâm ô, kích thước (x,z) và hướng vào trong (đơn vị). Ô 0 ở góc dưới-phải, đi theo kim đồng hồ.</summary>
        private static void Layout(int id, out Vector2 center, out Vector2 size, out Vector2 inward)
        {
            int side = id / 8;     // 0: cạnh dưới, 1: trái, 2: trên, 3: phải
            int index = id % 8;    // 0 = góc bắt đầu của cạnh, 1..7 = ô thường

            if (index == 0)
            {
                float cx = (side == 0 || side == 3) ? EdgeCenter : -EdgeCenter;
                float cz = (side == 0 || side == 1) ? -EdgeCenter : EdgeCenter;
                center = new Vector2(cx, cz);
                size = new Vector2(Corner, Corner);
                inward = new Vector2(-Mathf.Sign(cx), -Mathf.Sign(cz));
                return;
            }

            float along = -7f + (index - 1) * SideWidth + SideWidth / 2f; // -6 … +6
            switch (side)
            {
                case 0: // dưới: từ phải sang trái
                    center = new Vector2(-along, -EdgeCenter);
                    size = new Vector2(SideWidth, Depth);
                    inward = new Vector2(0f, 1f);
                    break;
                case 1: // trái: từ dưới lên
                    center = new Vector2(-EdgeCenter, along);
                    size = new Vector2(Depth, SideWidth);
                    inward = new Vector2(1f, 0f);
                    break;
                case 2: // trên: từ trái sang phải
                    center = new Vector2(along, EdgeCenter);
                    size = new Vector2(SideWidth, Depth);
                    inward = new Vector2(0f, -1f);
                    break;
                default: // phải: từ trên xuống
                    center = new Vector2(EdgeCenter, -along);
                    size = new Vector2(Depth, SideWidth);
                    inward = new Vector2(-1f, 0f);
                    break;
            }
        }

        private static BoardTileView[] BuildTiles(Transform tilesRoot, Transform root)
        {
            var views = new BoardTileView[TileCount];

            // Một Canvas world-space chung cho nhãn + hình in, nằm phẳng trên mặt bàn (đọc được từ camera phía -z).
            var canvasGo = new GameObject("Labels", typeof(RectTransform), typeof(Canvas));
            canvasGo.transform.SetParent(root, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var crt = (RectTransform)canvasGo.transform;
            crt.sizeDelta = new Vector2(2200f, 2200f);
            crt.localScale = Vector3.one * 0.01f;
            crt.position = new Vector3(0f, SlabHeight + 0.075f, 0f);
            crt.rotation = Quaternion.Euler(90f, 0f, 0f);

            for (int id = 0; id < views.Length; id++)
            {
                var tile = GetTile(id);
                Layout(id, out var c, out var size, out var inward);
                bool corner = id % 8 == 0;
                Color accent = tile.Color; // = màu của "Body" trong catalog; BoardTileView.Bind tô lại đúng màu này lúc chạy
                string hex = ColorUtility.ToHtmlStringRGB(accent);
                Vector3 inward3 = new Vector3(inward.x, 0f, inward.y);

                var tileGo = new GameObject($"Tile_{id:00}_{tile.Name}");
                tileGo.transform.SetParent(tilesRoot, false);
                tileGo.transform.position = new Vector3(c.x, 0f, c.y);

                // "Body" = mặt ô, BoardTileView.Bind tô màu theo catalog. Mọi ô (kể cả thành phố/bãi biển) phủ KÍN màu cả ô.
                var bodyRenderer = SceneKit.Primitive(PrimitiveType.Cube, "Body", tileGo.transform, new Vector3(0f, SlabHeight / 2f, 0f),
                    new Vector3(size.x - 0.08f, SlabHeight, size.y - 0.08f), SceneKit.Mat("Tile_" + hex, accent)).GetComponent<Renderer>();

                if (corner)
                {
                    AddCornerProp(tileGo.transform, tile.Type);
                }

                // Chủ sở hữu (thành phố/bãi biển): thanh màu mỏng ở mép ngoài ô, ẩn cho tới khi có chủ (BoardTileView.SetOwner).
                Renderer owner = null;
                Transform[] stages = new Transform[5];
                if (tile.Type == T.City || tile.Type == T.Beach)
                {
                    bool horizontal = Mathf.Abs(inward.y) > 0.5f;
                    var markerSize = horizontal ? new Vector3(size.x - 0.3f, 0.1f, 0.16f) : new Vector3(0.16f, 0.1f, size.y - 0.3f);
                    var markerGo = SceneKit.Primitive(PrimitiveType.Cube, "OwnerMarker", tileGo.transform,
                        -inward3 * (Depth / 2f - 0.14f) + new Vector3(0f, SlabHeight + 0.05f, 0f), markerSize, SceneKit.Mat("OwnerMarker", Color.white));
                    owner = markerGo.GetComponent<Renderer>();
                    markerGo.SetActive(false);
                }

                // Công trình Cọc/Nhà 1–3/Khách sạn từ prefab của nhóm, ẩn cho tới khi Core báo cấp tương ứng (chỉ thành phố).
                if (tile.Type == T.City)
                {
                    string[] names = { "Stake", "House1", "House2", "House3", "Hotel" };
                    float[] heights = { 1.3f, 0.92f, 1.25f, 1.56f, 2.7f }; // = chiều cao thiết kế của prefab (BuildingPrefabBuilder), không bóp méo
                    for (int k = 0; k < 5; k++)
                    {
                        var holder = SceneKit.Spawn(BuildingDir + "Building_" + names[k] + ".prefab", tileGo.transform,
                            tileGo.transform.position + inward3 * 0.8f + new Vector3(0f, SlabHeight, 0f), heights[k], 0f);
                        if (holder != null)
                        {
                            stages[k] = holder.transform;
                            holder.SetActive(false);
                        }
                    }
                }

                var view = tileGo.AddComponent<BoardTileView>();
                var so = new SerializedObject(view);
                so.FindProperty("_tileId").intValue = id;
                so.FindProperty("_bodyRenderer").objectReferenceValue = bodyRenderer;
                so.FindProperty("_ownerMarker").objectReferenceValue = owner;
                var stagesProp = so.FindProperty("_propertyStages");
                stagesProp.arraySize = stages.Length;
                for (int k = 0; k < stages.Length; k++) stagesProp.GetArrayElementAtIndex(k).objectReferenceValue = stages[k];
                so.ApplyModifiedPropertiesWithoutUndo();
                views[id] = view;

                AddTileLabel(canvasGo.transform, tile, c, size, inward);
            }

            return views;
        }

        /// <summary>
        /// Nhãn/biểu tượng in trên mặt ô (đơn vị canvas = 100 × mét). Ô Xuất phát/Nhà tù/Tổ chức sự kiện/Dịch chuyển/Thẻ
        /// KHÔNG có chữ: Xuất phát = mũi tên chỉ chiều đi, Thẻ = vòng tròn nhiều màu, ba ô còn lại có mô hình 3D riêng.
        /// Thành phố chỉ có tên (không cờ). Ô cạnh trái/phải: chữ lệch ra ngoài, icon nằm trên dải màu phía trong.
        /// </summary>
        private static void AddTileLabel(Transform labelCanvas, Tile tile, Vector2 c, Vector2 size, Vector2 inward)
        {
            Vector2 center = c * 100f;

            switch (tile.Type)
            {
                case T.Start:
                    // Ô 0 → ô 1 đi về phía -x: mũi tên trắng quay 180°.
                    NewBadge(labelCanvas, "Arrow", Icon("arrow_right"), center, new Vector2(120f, 120f)).rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
                    return;
                case T.Jail:
                case T.Festival:
                case T.Teleport:
                    return; // mô hình 3D, không chữ
                case T.Card:
                    NewBadge(labelCanvas, "CardWheel", CardWheel, center, new Vector2(130f, 130f));
                    return;
                case T.Tax:
                    // Tờ giấy kẹp bút in phẳng lên mặt ô, không chữ.
                    NewBadge(labelCanvas, "TaxPaper", TaxPaper, center, new Vector2(150f, 150f));
                    return;
            }

            bool vertical = Mathf.Abs(inward.x) > 0.5f; // ô trái/phải
            Vector2 textPos;
            Vector2 textBox;
            if (vertical)
            {
                textPos = center - inward * 28f;
                textBox = new Vector2(140f, 90f);
            }
            else
            {
                textPos = center - inward * 30f;
                textBox = new Vector2(Mathf.Min(size.x * 100f, 190f) - 16f, 90f);
            }

            // Thành phố và bãi biển chỉ có tên (không cờ, không icon).
            NewLabel(labelCanvas, "Name", tile.Name, Bold, 25, TextDark, textPos, textBox);
        }

        // ------------------------------------------------------------ mô hình 3D ở bốn góc

        private static void AddCornerProp(Transform tile, T type)
        {
            switch (type)
            {
                case T.Jail: BuildCage(tile); break;
                case T.Festival: BuildStadium(tile); break;
                case T.Teleport: BuildAirplane(tile); break;
            }
        }

        private static Transform PropRoot(Transform tile, string name)
        {
            var root = new GameObject(name).transform;
            root.SetParent(tile, false);
            root.localPosition = new Vector3(0f, SlabHeight, 0f);
            return root;
        }

        /// <summary>
        /// Sân vận động bầu dục (ô Tổ chức sự kiện), màu đặc pastel: tường ngoài xanh trời, khán đài trong hồng phấn,
        /// sân cỏ xanh có vòng giữa sân trắng, bốn cột đèn trắng đầu vàng bơ.
        /// </summary>
        private static void BuildStadium(Transform tile)
        {
            var root = PropRoot(tile, "Stadium");
            var wall = SceneKit.Mat("Stadium_Wall", new Color(0.6f, 0.78f, 0.95f), 0.4f);
            var seat = SceneKit.Mat("Stadium_Seat", new Color(0.95f, 0.7f, 0.78f), 0.3f);
            var field = SceneKit.Mat("Stadium_Field", new Color(0.5f, 0.82f, 0.52f), 0.3f);
            var line = SceneKit.Mat("Stadium_Line", new Color(0.97f, 0.98f, 1f), 0.3f);
            var pole = SceneKit.Mat("Stadium_Pole", new Color(0.96f, 0.96f, 1f), 0.5f);
            var lamp = SceneKit.Mat("Stadium_Lamp", new Color(0.99f, 0.9f, 0.55f), 0.6f);

            SceneKit.Primitive(PrimitiveType.Cylinder, "Field", root, new Vector3(0f, 0.04f, 0f), new Vector3(1.55f, 0.04f, 1.2f), field);
            SceneKit.Primitive(PrimitiveType.Cylinder, "CenterCircle", root, new Vector3(0f, 0.065f, 0f), new Vector3(0.55f, 0.01f, 0.42f), line);
            SceneKit.Primitive(PrimitiveType.Cylinder, "CenterFill", root, new Vector3(0f, 0.07f, 0f), new Vector3(0.48f, 0.01f, 0.36f), field);
            EllipseRing(root, "Wall", 22, 1.2f, 0.96f, 0.55f, 0.14f, 0.34f, wall);
            EllipseRing(root, "Seats", 18, 0.95f, 0.74f, 0.32f, 0.22f, 0.33f, seat);

            for (int i = 0; i < 4; i++)
            {
                float a = (45f + 90f * i) * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(a) * 1.2f, 0f, Mathf.Sin(a) * 0.96f);
                SceneKit.Primitive(PrimitiveType.Cylinder, "Pole", root, p + new Vector3(0f, 0.5f, 0f), new Vector3(0.05f, 0.5f, 0.05f), pole);
                SceneKit.Primitive(PrimitiveType.Cube, "Light", root, p + new Vector3(0f, 1.03f, 0f), new Vector3(0.22f, 0.1f, 0.14f), lamp);
            }
        }

        /// <summary>Dựng một vòng elip bằng các khối ngắn xếp theo tiếp tuyến.</summary>
        private static void EllipseRing(Transform parent, string name, int count, float a, float b, float height, float thickness, float length, Material mat)
        {
            for (int i = 0; i < count; i++)
            {
                float t = Mathf.PI * 2f * i / count;
                var tangent = new Vector3(-a * Mathf.Sin(t), 0f, b * Mathf.Cos(t)).normalized;
                var go = SceneKit.Primitive(PrimitiveType.Cube, name + i, parent, new Vector3(a * Mathf.Cos(t), height / 2f, b * Mathf.Sin(t)), new Vector3(thickness, height, length), mat);
                go.transform.localRotation = Quaternion.LookRotation(tangent, Vector3.up);
            }
        }

        /// <summary>
        /// Máy bay dân dụng trên bệ đáp (ô Dịch chuyển): thân trắng, cánh/đuôi xanh pastel, cửa sổ xanh đậm, động cơ xám nhạt,
        /// ba bánh xe. Mũi hướng về phía +x, xoay nhẹ để nhìn đẹp từ camera.
        /// </summary>
        private static void BuildAirplane(Transform tile)
        {
            var root = PropRoot(tile, "Airplane");
            var pad = SceneKit.Mat("Plane_Pad", new Color(0.82f, 0.86f, 0.93f), 0.3f);
            var padRing = SceneKit.Mat("Plane_PadRing", new Color(0.97f, 0.97f, 1f), 0.3f);
            var body = SceneKit.Mat("Plane_Body", new Color(0.98f, 0.98f, 1f), 0.55f);
            var wing = SceneKit.Mat("Plane_Wing", new Color(0.55f, 0.76f, 0.95f), 0.5f);
            var accent = SceneKit.Mat("Plane_Accent", new Color(0.98f, 0.86f, 0.5f), 0.4f);
            var glass = SceneKit.Mat("Plane_Glass", new Color(0.25f, 0.33f, 0.5f), 0.8f);
            var engine = SceneKit.Mat("Plane_Engine", new Color(0.86f, 0.88f, 0.94f), 0.5f);
            var tyre = SceneKit.Mat("Plane_Tyre", new Color(0.25f, 0.27f, 0.33f), 0.2f);

            // Bệ đáp tròn có viền sáng.
            SceneKit.Primitive(PrimitiveType.Cylinder, "PadRing", root, new Vector3(0f, 0.025f, 0f), new Vector3(2.4f, 0.025f, 2.4f), padRing);
            SceneKit.Primitive(PrimitiveType.Cylinder, "Pad", root, new Vector3(0f, 0.035f, 0f), new Vector3(2.2f, 0.03f, 2.2f), pad);

            var plane = new GameObject("Plane").transform;
            plane.SetParent(root, false);
            plane.localRotation = Quaternion.Euler(0f, -22f, 0f);

            const float y = 0.4f; // cao độ trục thân

            // Thân (viên nang nằm dọc trục x) + kính buồng lái + cửa sổ khoang.
            var fuselage = SceneKit.Primitive(PrimitiveType.Capsule, "Fuselage", plane, new Vector3(0f, y, 0f), new Vector3(0.42f, 0.95f, 0.42f), body);
            fuselage.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            SceneKit.Primitive(PrimitiveType.Cube, "Cockpit", plane, new Vector3(0.66f, y + 0.1f, 0f), new Vector3(0.14f, 0.09f, 0.3f), glass);
            for (int i = 0; i < 5; i++)
            {
                float x = -0.55f + i * 0.2f;
                SceneKit.Primitive(PrimitiveType.Sphere, "WindowL" + i, plane, new Vector3(x, y + 0.06f, -0.205f), new Vector3(0.08f, 0.09f, 0.03f), glass);
                SceneKit.Primitive(PrimitiveType.Sphere, "WindowR" + i, plane, new Vector3(x, y + 0.06f, 0.205f), new Vector3(0.08f, 0.09f, 0.03f), glass);
            }

            // Cánh chính hơi xuôi sau + động cơ dưới cánh.
            var wingL = SceneKit.Primitive(PrimitiveType.Cube, "WingL", plane, new Vector3(0.05f, y - 0.06f, -0.62f), new Vector3(0.55f, 0.05f, 1.05f), wing);
            wingL.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
            var wingR = SceneKit.Primitive(PrimitiveType.Cube, "WingR", plane, new Vector3(0.05f, y - 0.06f, 0.62f), new Vector3(0.55f, 0.05f, 1.05f), wing);
            wingR.transform.localRotation = Quaternion.Euler(0f, -18f, 0f);
            foreach (float z in new[] { -0.6f, 0.6f })
            {
                var eng = SceneKit.Primitive(PrimitiveType.Cylinder, "Engine", plane, new Vector3(0.15f, y - 0.2f, z), new Vector3(0.17f, 0.2f, 0.17f), engine);
                eng.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            // Đuôi đứng (có vạch vàng nhạt) + cánh đuôi ngang.
            var fin = SceneKit.Primitive(PrimitiveType.Cube, "TailFin", plane, new Vector3(-0.8f, y + 0.32f, 0f), new Vector3(0.42f, 0.5f, 0.06f), wing);
            fin.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            SceneKit.Primitive(PrimitiveType.Cube, "TailStripe", plane, new Vector3(-0.86f, y + 0.5f, 0f), new Vector3(0.2f, 0.12f, 0.075f), accent);
            var stabL = SceneKit.Primitive(PrimitiveType.Cube, "StabL", plane, new Vector3(-0.86f, y + 0.04f, -0.28f), new Vector3(0.3f, 0.04f, 0.46f), wing);
            stabL.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);
            var stabR = SceneKit.Primitive(PrimitiveType.Cube, "StabR", plane, new Vector3(-0.86f, y + 0.04f, 0.28f), new Vector3(0.3f, 0.04f, 0.46f), wing);
            stabR.transform.localRotation = Quaternion.Euler(0f, -15f, 0f);

            // Ba bánh xe có trụ.
            foreach (var p in new[] { new Vector3(0.5f, 0f, 0f), new Vector3(-0.25f, 0f, -0.24f), new Vector3(-0.25f, 0f, 0.24f) })
            {
                SceneKit.Primitive(PrimitiveType.Cylinder, "Strut", plane, new Vector3(p.x, 0.2f, p.z), new Vector3(0.04f, 0.1f, 0.04f), engine);
                SceneKit.Primitive(PrimitiveType.Sphere, "Wheel", plane, new Vector3(p.x, 0.11f, p.z), Vector3.one * 0.13f, tyre);
            }
        }

        /// <summary>Lồng sắt: sàn thép, song đứng quanh 4 cạnh, khung đỉnh và ổ khóa vàng phía trước.</summary>
        private static void BuildCage(Transform tile)
        {
            var root = PropRoot(tile, "Cage");
            var metal = SceneKit.Mat("Cage_Metal", new Color(0.38f, 0.4f, 0.48f), 0.55f);
            var gold = SceneKit.Mat("Cage_Lock", new Color(0.96f, 0.78f, 0.3f), 0.6f);
            const float half = 0.85f, barY = 0.51f, topY = 0.97f;

            SceneKit.Primitive(PrimitiveType.Cube, "Floor", root, new Vector3(0f, 0.03f, 0f), new Vector3(1.9f, 0.06f, 1.9f), metal);

            for (int k = 0; k < 7; k++)
            {
                float t = -half + k * (half * 2f / 6f);
                SceneKit.Primitive(PrimitiveType.Cylinder, "BarF", root, new Vector3(t, barY, -half), new Vector3(0.07f, 0.45f, 0.07f), metal);
                SceneKit.Primitive(PrimitiveType.Cylinder, "BarB", root, new Vector3(t, barY, half), new Vector3(0.07f, 0.45f, 0.07f), metal);
                if (k > 0 && k < 6)
                {
                    SceneKit.Primitive(PrimitiveType.Cylinder, "BarL", root, new Vector3(-half, barY, t), new Vector3(0.07f, 0.45f, 0.07f), metal);
                    SceneKit.Primitive(PrimitiveType.Cylinder, "BarR", root, new Vector3(half, barY, t), new Vector3(0.07f, 0.45f, 0.07f), metal);
                }
            }

            foreach (float y in new[] { 0.5f, topY })
            {
                SceneKit.Primitive(PrimitiveType.Cube, "BeamF", root, new Vector3(0f, y, -half), new Vector3(1.8f, 0.06f, 0.06f), metal);
                SceneKit.Primitive(PrimitiveType.Cube, "BeamB", root, new Vector3(0f, y, half), new Vector3(1.8f, 0.06f, 0.06f), metal);
                SceneKit.Primitive(PrimitiveType.Cube, "BeamL", root, new Vector3(-half, y, 0f), new Vector3(0.06f, 0.06f, 1.8f), metal);
                SceneKit.Primitive(PrimitiveType.Cube, "BeamR", root, new Vector3(half, y, 0f), new Vector3(0.06f, 0.06f, 1.8f), metal);
            }

            SceneKit.Primitive(PrimitiveType.Cube, "TopCrossX", root, new Vector3(0f, topY, 0f), new Vector3(1.8f, 0.05f, 0.05f), metal);
            SceneKit.Primitive(PrimitiveType.Cube, "TopCrossZ", root, new Vector3(0f, topY, 0f), new Vector3(0.05f, 0.05f, 1.8f), metal);
            SceneKit.Primitive(PrimitiveType.Cube, "Lock", root, new Vector3(0f, 0.42f, -half - 0.05f), new Vector3(0.2f, 0.22f, 0.08f), gold);
        }

        // ------------------------------------------------------------ khu trung tâm + xung quanh

        private static void BuildCenter(Transform root)
        {
            var center = new GameObject("Center").transform;
            center.SetParent(root, false);

            SceneKit.Primitive(PrimitiveType.Cube, "Lawn", center, new Vector3(0f, 0.12f, 0f), new Vector3(13.6f, 0.24f, 13.6f), SceneKit.Mat("Lawn", new Color(0.45f, 0.78f, 0.38f)));

            // Đài phun nước + quả địa cầu ở giữa.
            SceneKit.Primitive(PrimitiveType.Cylinder, "FountainBase", center, new Vector3(0f, 0.35f, 0f), new Vector3(4.4f, 0.12f, 4.4f), SceneKit.Mat("Stone", new Color(0.85f, 0.83f, 0.78f)));
            SceneKit.Primitive(PrimitiveType.Cylinder, "FountainWater", center, new Vector3(0f, 0.45f, 0f), new Vector3(3.6f, 0.06f, 3.6f), SceneKit.Mat("Water", new Color(0.3f, 0.72f, 0.95f), 0.9f));
            SceneKit.Primitive(PrimitiveType.Cylinder, "Pedestal", center, new Vector3(0f, 0.9f, 0f), new Vector3(1.0f, 0.5f, 1.0f), SceneKit.Mat("Gold", new Color(0.95f, 0.75f, 0.2f), 0.6f));
            SceneKit.Primitive(PrimitiveType.Sphere, "Globe", center, new Vector3(0f, 2.3f, 0f), new Vector3(2.2f, 2.2f, 2.2f), SceneKit.Mat("Globe", new Color(0.2f, 0.55f, 0.95f), 0.7f));

            // Vài tòa nhà nhỏ ở bốn góc bãi cỏ + cây.
            // Thấp (1.5) và đặt ở nửa gần camera: camera nhìn chéo từ phía -z nên công trình cao ở nửa xa sẽ che hàng ô phía sau.
            string[] houses = { "building-b", "building-e", "building-j", "building-l" };
            Vector2[] pos = { new Vector2(-4.8f, -3.6f), new Vector2(4.8f, -3.6f), new Vector2(-5.0f, 0.4f), new Vector2(5.0f, 0.4f) };
            for (int i = 0; i < houses.Length; i++)
            {
                SceneKit.Spawn(SceneKit.Commercial + houses[i] + ".fbx", center, new Vector3(pos[i].x, 0.24f, pos[i].y), 1.5f, i * 90f);
            }

            for (int i = 0; i < 10; i++)
            {
                float a = (i / 10f) * Mathf.PI * 2f + 0.3f;
                SceneKit.Spawn(SceneKit.Suburban + (i % 2 == 0 ? "tree-large" : "tree-small") + ".fbx", center,
                    new Vector3(Mathf.Cos(a) * 3.8f, 0.24f, Mathf.Sin(a) * 3.8f), 1.2f + (i % 3) * 0.2f, i * 31f);
            }
        }

        private static void BuildSurroundings(Transform root)
        {
            var env = new GameObject("Surroundings").transform;
            env.SetParent(root, false);

            SceneKit.Primitive(PrimitiveType.Cube, "Ground", env, new Vector3(0f, -0.25f, 0f), new Vector3(80f, 0.5f, 60f), SceneKit.Mat("Ground", new Color(0.5f, 0.8f, 0.42f)));
            SceneKit.Primitive(PrimitiveType.Cube, "River", env, new Vector3(0f, -0.1f, 0f), new Vector3(27f, 0.2f, 27f), SceneKit.Mat("Water", new Color(0.3f, 0.72f, 0.95f), 0.9f));

            string[] props =
            {
                "tree-large", "tree-small", "tree-large", "building-type-a", "tree-small", "building-type-d",
                "tree-large", "building-type-g", "tree-small", "building-type-k", "tree-large", "building-type-n"
            };
            var rng = new System.Random(7);
            int count = 0;
            for (int attempt = 0; attempt < 500 && count < 60; attempt++)
            {
                float x = (float)(rng.NextDouble() * 60.0 - 30.0);
                float z = (float)(rng.NextDouble() * 44.0 - 18.0);
                if (Mathf.Abs(x) < 15f && z > -15f && z < 15f)
                {
                    continue; // chừa chỗ cho bàn cờ + sông
                }

                string prop = props[count % props.Length];
                bool isTree = prop.StartsWith("tree");
                SceneKit.Spawn(SceneKit.Suburban + prop + ".fbx", env, new Vector3(x, 0f, z), isTree ? 1.8f + (float)rng.NextDouble() : 2.4f, (float)rng.NextDouble() * 360f);
                count++;
            }
        }

        // ------------------------------------------------------------ helper nhãn

        private static Text NewLabel(Transform parent, string name, string content, Font font, int size, Color color, Vector2 pos, Vector2 box)
        {
            var t = NewText(name, parent, content, font, size, color, TextAnchor.MiddleCenter, Vector2.zero, box);
            var rt = (RectTransform)t.transform;
            rt.localPosition = new Vector3(pos.x, pos.y, 0f);
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 12;
            t.resizeTextMaxSize = size;
            return t;
        }

        private static Image NewBadge(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size)
        {
            var img = NewImage(name, parent, sprite, Color.white, Vector2.zero, size);
            img.rectTransform.localPosition = new Vector3(pos.x, pos.y, 0f);
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }
    }
}
#endif
