#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BTLGame.EditorTools
{
    /// <summary>
    /// Dựng lại 5 prefab công trình (Cọc, Nhà 1, Nhà 2, Nhà 3, Khách sạn) để mỗi cấp nhìn khác hẳn nhau từ camera cố định:
    /// - Cọc: cọc gỗ cắm trên gò đất với lá cờ đỏ (mới đặt cọc đất).
    /// - Nhà 1: một nhà ngoại ô nhỏ mái xanh trên bệ xanh lá, 1 chấm vàng.
    /// - Nhà 2: nhà ngoại ô lớn có gara + cây xanh trên bệ xanh ngọc, 2 chấm vàng.
    /// - Nhà 3: tòa nhà căn hộ cửa kính xanh trên bệ xanh dương, 3 chấm vàng.
    /// - Khách sạn: tháp cao nhất, mũ vương miện vàng + bệ vàng (không có chấm).
    /// Dùng model Kenney (CitySuburban/CityCommercial) tham chiếu trực tiếp + vài khối đơn giản; ghi đè prefab cũ cùng đường dẫn nên GUID giữ nguyên.
    /// </summary>
    internal static class BuildingPrefabBuilder
    {
        private const string Dir = "Assets/_Game/Prefabs/Buildings/";
        private const string Suburban = "Assets/ThirdParty/Kenney/CitySuburban/Models/FBX/";
        private const string Commercial = "Assets/ThirdParty/Kenney/CityCommercial/Models/FBX/";
        private const float PlatformTop = 0.06f;

        private static readonly Color PostBrown = new Color(0.55f, 0.35f, 0.18f);
        private static readonly Color FlagRed = new Color(0.92f, 0.22f, 0.25f);
        private static readonly Color Dirt = new Color(0.78f, 0.62f, 0.4f);
        private static readonly Color GoldColor = new Color(1f, 0.8f, 0.15f);

        [MenuItem("Tools/CoTyPhu/Rebuild Building Prefabs")]
        internal static void Build()
        {
            BuildStake();
            BuildHouse("House1", Suburban + "building-type-a.fbx", 0.85f, 1.0f, new Color(0.55f, 0.85f, 0.5f), 1, false);
            BuildHouse("House2", Suburban + "building-type-d.fbx", 1.15f, 1.5f, new Color(0.4f, 0.8f, 0.85f), 2, true);
            BuildTower("House3", Commercial + "building-skyscraper-a.fbx", 1.5f, new Color(0.5f, 0.58f, 0.95f), 3, false);
            BuildTower("Hotel", Commercial + "building-skyscraper-d.fbx", 2.2f, GoldColor, 0, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BuildingPrefabBuilder] Đã dựng lại 5 prefab công trình.");
        }

        // ------------------------------------------------------------ từng cấp
        private static void BuildStake()
        {
            var root = new GameObject("Building_Stake");
            SceneKit.Primitive(PrimitiveType.Sphere, "Mound", root.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.95f, 0.16f, 0.95f), SceneKit.Mat("Bld_Dirt", Dirt));
            SceneKit.Primitive(PrimitiveType.Cylinder, "Post", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.1f, 0.4f, 0.1f), SceneKit.Mat("Bld_Post", PostBrown));
            SceneKit.Primitive(PrimitiveType.Cube, "Flag", root.transform, new Vector3(0.24f, 0.78f, 0f), new Vector3(0.44f, 0.26f, 0.05f), SceneKit.Mat("Bld_Flag", FlagRed));
            Save(root, "Stake");
        }

        private static void BuildHouse(string name, string model, float height, float maxFootprint, Color platformColor, int pips, bool withTree)
        {
            var root = new GameObject("Building_" + name);
            AddPlatform(root.transform, platformColor, 1.6f);
            var house = PlaceModel(root.transform, model, height, maxFootprint, withTree ? -0.1f : 0f);
            if (withTree)
            {
                var tree = PlaceModel(root.transform, Suburban + "tree-large.fbx", height * 0.9f, 0.7f, 0f);
                if (tree != null) tree.transform.localPosition += new Vector3(0.62f, 0f, 0.1f);
                if (house != null) house.transform.localPosition += new Vector3(-0.2f, 0f, 0f);
            }

            AddPips(root.transform, pips);
            Save(root, name);
        }

        private static void BuildTower(string name, string model, float height, Color platformColor, int pips, bool crown)
        {
            var root = new GameObject("Building_" + name);
            AddPlatform(root.transform, platformColor, 1.5f);
            var tower = PlaceModel(root.transform, model, height, 1.0f, 0f);
            AddPips(root.transform, pips);
            if (crown && tower != null)
            {
                float top = GetBounds(tower).max.y - root.transform.position.y;
                AddCrown(root.transform, top);
            }

            Save(root, name);
        }

        // ------------------------------------------------------------ chi tiết
        private static void AddPlatform(Transform parent, Color color, float diameter)
        {
            SceneKit.Primitive(PrimitiveType.Cylinder, "Platform", parent, new Vector3(0f, PlatformTop / 2f, 0f),
                new Vector3(diameter, PlatformTop / 2f, diameter), SceneKit.Mat("Bld_Platform_" + ColorUtility.ToHtmlStringRGB(color), color));
        }

        /// <summary>Chấm vàng ở mép trước bệ: số chấm = số nhà (1, 2, 3) để phân biệt cấp ngay cả khi mô hình giống nhau.</summary>
        private static void AddPips(Transform parent, int count)
        {
            var mat = SceneKit.Mat("Bld_Pip", GoldColor, 0.5f);
            for (int i = 0; i < count; i++)
            {
                float x = (i - (count - 1) / 2f) * 0.24f;
                SceneKit.Primitive(PrimitiveType.Sphere, "Pip" + (i + 1), parent, new Vector3(x, PlatformTop + 0.06f, -0.66f), Vector3.one * 0.18f, mat);
            }
        }

        private static void AddCrown(Transform parent, float top)
        {
            var mat = SceneKit.Mat("Bld_Gold", GoldColor, 0.6f);
            SceneKit.Primitive(PrimitiveType.Cube, "CrownBase", parent, new Vector3(0f, top + 0.05f, 0f), new Vector3(0.78f, 0.1f, 0.78f), mat);
            foreach (var p in new[] { new Vector2(-0.3f, -0.3f), new Vector2(0.3f, -0.3f), new Vector2(-0.3f, 0.3f), new Vector2(0.3f, 0.3f) })
                SceneKit.Primitive(PrimitiveType.Cube, "CrownTooth", parent, new Vector3(p.x, top + 0.2f, p.y), new Vector3(0.14f, 0.2f, 0.14f), mat);
            SceneKit.Primitive(PrimitiveType.Sphere, "CrownJewel", parent, new Vector3(0f, top + 0.3f, 0f), Vector3.one * 0.26f, mat);
        }

        // ------------------------------------------------------------ tiện ích
        /// <summary>Đặt model Kenney (tham chiếu prefab FBX) vào khung: cao đúng <paramref name="height"/>, không rộng quá <paramref name="maxFootprint"/>, đáy trên bệ.</summary>
        private static GameObject PlaceModel(Transform parent, string path, float height, float maxFootprint, float zOffset)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning("[BuildingPrefabBuilder] Không tìm thấy model: " + path);
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            var b = GetBounds(instance);
            float scale = height / Mathf.Max(0.0001f, b.size.y);
            float footprint = Mathf.Max(b.size.x, b.size.z) * scale;
            if (footprint > maxFootprint) scale *= maxFootprint / footprint;
            instance.transform.localScale = Vector3.one * scale;
            b = GetBounds(instance);
            instance.transform.position += new Vector3(-b.center.x, PlatformTop - b.min.y, -b.center.z + zOffset);
            return instance;
        }

        private static Bounds GetBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        private static void Save(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, Dir + "Building_" + name + ".prefab");
            Object.DestroyImmediate(root);
        }
    }
}
#endif
