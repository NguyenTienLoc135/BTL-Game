#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BTLGame.EditorTools
{
    /// <summary>
    /// Dựng 5 prefab công trình theo CÙNG MỘT kiểu nhà; chỉ khác số tầng và màu theo người chủ (BoardTileView.SetOwner):
    /// Cọc = bệ + cọc cờ; Nhà 1 / Nhà 2 / Nhà 3 = 1 / 2 / 3 tầng giống hệt nhau xếp chồng; Khách sạn = 4 tầng cùng kiểu, thêm quả cầu vàng trên đỉnh.
    /// Tên khối quyết định cách tô: "Wall*", "Flag" = màu chủ; "Roof*", "Platform" = màu chủ sẫm; còn lại giữ nguyên.
    /// Ghi đè prefab cũ cùng đường dẫn nên GUID giữ nguyên.
    /// </summary>
    internal static class BuildingPrefabBuilder
    {
        private const string Dir = "Assets/_Game/Prefabs/Buildings/";
        private const float PlatformTop = 0.06f;
        private const float FloorHeight = 0.42f;
        private const float Width = 0.9f;

        [MenuItem("Tools/CoTyPhu/Rebuild Building Prefabs")]
        internal static void Build()
        {
            BuildStake();
            BuildHouse("House1", 1, false);
            BuildHouse("House2", 2, false);
            BuildHouse("House3", 3, false);
            BuildHouse("Hotel", 4, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BuildingPrefabBuilder] Đã dựng lại 5 prefab công trình.");
        }

        private static Material Wall => SceneKit.Mat("Bld_Wall", new Color(0.9f, 0.9f, 0.92f));
        private static Material Roof => SceneKit.Mat("Bld_Roof", new Color(0.62f, 0.64f, 0.72f));
        private static Material Window => SceneKit.Mat("Bld_Window", new Color(0.97f, 0.99f, 1f), 0.6f);
        private static Material Wood => SceneKit.Mat("Bld_Post", new Color(0.55f, 0.35f, 0.18f));

        private static void BuildStake()
        {
            var root = new GameObject("Building_Stake");
            AddPlatform(root.transform);
            SceneKit.Primitive(PrimitiveType.Cylinder, "Post", root.transform, new Vector3(0f, 0.46f, 0f), new Vector3(0.1f, 0.4f, 0.1f), Wood);
            SceneKit.Primitive(PrimitiveType.Cube, "Flag", root.transform, new Vector3(0.24f, 0.74f, 0f), new Vector3(0.44f, 0.26f, 0.05f), Wall);
            Save(root, "Stake");
        }

        private static void BuildHouse(string name, int floors, bool hotel)
        {
            var root = new GameObject("Building_" + name);
            AddPlatform(root.transform);
            float front = -Width / 2f - 0.01f;
            for (int floor = 0; floor < floors; floor++)
            {
                float y = PlatformTop + floor * FloorHeight + FloorHeight / 2f;
                SceneKit.Primitive(PrimitiveType.Cube, "Wall" + floor, root.transform, new Vector3(0f, y, 0f), new Vector3(Width, FloorHeight, Width), Wall);
                // Gờ ngăn tầng để đếm được số tầng.
                SceneKit.Primitive(PrimitiveType.Cube, "Roof_Ledge" + floor, root.transform,
                    new Vector3(0f, PlatformTop + (floor + 1) * FloorHeight - 0.025f, 0f), new Vector3(Width + 0.08f, 0.05f, Width + 0.08f), Roof);
                foreach (float x in new[] { -0.26f, 0.26f })
                    SceneKit.Primitive(PrimitiveType.Cube, "Window", root.transform, new Vector3(x, y, front), new Vector3(0.22f, 0.2f, 0.03f), Window);
            }

            SceneKit.Primitive(PrimitiveType.Cube, "Door", root.transform, new Vector3(0f, PlatformTop + 0.13f, front), new Vector3(0.16f, 0.26f, 0.03f), Wood);

            // Mái hai bậc giống nhau cho mọi cấp.
            float top = PlatformTop + floors * FloorHeight;
            SceneKit.Primitive(PrimitiveType.Cube, "Roof0", root.transform, new Vector3(0f, top + 0.05f, 0f), new Vector3(Width + 0.12f, 0.1f, Width + 0.12f), Roof);
            SceneKit.Primitive(PrimitiveType.Cube, "Roof1", root.transform, new Vector3(0f, top + 0.16f, 0f), new Vector3(Width * 0.66f, 0.12f, Width * 0.66f), Roof);
            if (hotel)
                SceneKit.Primitive(PrimitiveType.Sphere, "Gold", root.transform, new Vector3(0f, top + 0.32f, 0f), Vector3.one * 0.24f,
                    SceneKit.Mat("Bld_Gold", new Color(1f, 0.8f, 0.15f), 0.6f));
            Save(root, name);
        }

        private static void AddPlatform(Transform parent)
        {
            SceneKit.Primitive(PrimitiveType.Cylinder, "Platform", parent, new Vector3(0f, PlatformTop / 2f, 0f),
                new Vector3(1.1f, PlatformTop / 2f, 1.1f), Roof);   // gọn trong ô, không chờm viền đèn/chữ
        }

        private static void Save(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, Dir + "Building_" + name + ".prefab");
            Object.DestroyImmediate(root);
        }
    }
}
#endif
