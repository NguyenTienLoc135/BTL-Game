using BtlGame.Core;
using BTLGame.Cards;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TextCore.LowLevel;

namespace BTLGame.Editor
{
    /// <summary>Nối presentation đã bàn giao mà không dựng lại/xóa Scene hoặc prefab của nhóm.</summary>
    public static class TeamIntegrationSetup
    {
        public const string FestivalDescription = "Có thể trả 200.000đ chọn thành phố của mình. Cùng chủ/cùng thành phố cộng thêm 50% thuê gốc mỗi lần, không hết hạn theo lượt. Chọn thành phố khác thay lễ hội cũ bằng mức +50%; đổi chủ thì xóa.";
        public const string UiFontPath = "Assets/_Game/Art/Fonts/BeVietnamPro-UI SDF.asset";

        [MenuItem("BTL Game/Connect Team Gameplay")]
        public static void Prepare()
        {
            TMP_FontAsset font = EnsureUiFont();
            foreach (string prefab in new[] { "Board/BoardTile", "UI/GameHUD", "UI/DecisionPopup", "UI/MainMenuCanvas" })
                BindUiFont("Assets/_Game/Prefabs/" + prefab + ".prefab", font);
            PrepareBoardGeometry();
            var card = AssetDatabase.LoadAssetAtPath<EventCardData>("Assets/_Game/Data/Cards/EventCard_12.asset");
            var serialized = new SerializedObject(card);
            serialized.FindProperty("_description").stringValue = FestivalDescription;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(card);

            UpdateMenuGuide();
            AddScrollableOptions();
            foreach (string sceneName in new[] { "MainMenu", "Gameplay" })
            {
                Scene scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + sceneName + ".unity", OpenSceneMode.Single);
                if (Object.FindFirstObjectByType<UnityMatchHost>() == null)
                {
                    var host = new GameObject("UnityMatchHost", typeof(UnityMatchHost));
                    SceneManager.MoveGameObjectToScene(host, scene);
                }
                if (sceneName == "Gameplay") Object.FindFirstObjectByType<Camera>().orthographicSize = 9f;
                EditorSceneManager.SaveScene(scene);
            }
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/MainMenu.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("TEAM_INTEGRATION_READY: MainMenu/Gameplay đã nối UnityMatchHost; giữ nguyên GUID và visual đã bàn giao.");
        }

        public static TMP_FontAsset EnsureUiFont()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiFontPath);
            if (font != null) return font;
            Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/ThirdParty/Fonts/BeVietnamPro/BeVietnamPro-Regular.ttf");
            if (source == null) throw new System.InvalidOperationException("Thiếu TTF Be Vietnam Pro nguồn.");
            // SDF cần khoảng đệm để shader nội suy viền chữ, tránh lấy texel từ glyph bên cạnh.
            font = TMP_FontAsset.CreateFontAsset(source, 48, 8, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, false);
            font.name = "BeVietnamPro-UI SDF";
            var chars = new List<uint>();
            chars.AddRange(Enumerable.Range(32, 95).Select(c => (uint)c));
            chars.AddRange(Enumerable.Range(160, 96).Select(c => (uint)c));
            chars.AddRange(Enumerable.Range(7840, 90).Select(c => (uint)c));
            chars.AddRange(new uint[] { 258, 259, 272, 273, 296, 297, 360, 361, 416, 417, 431, 432, 8211, 8212, 8226, 8230, 8594 });
            font.TryAddCharacters(chars.Distinct().ToArray(), out uint[] missing);
            if (!font.HasCharacters("CỜ TỶ PHÚ RPG — Chưa chọn, Nhà Đầu Tư, Kỹ Sư, Luật Sư, Hà Nội, Đà Nẵng, Xuất phát"))
                throw new System.InvalidOperationException("Font chưa chứa đủ chữ tiếng Việt cần thiết.");
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, UiFontPath);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (Texture2D atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            if (missing.Length > 0) Debug.Log("Font bỏ qua mã ký tự không có trong TTF: " + string.Join(",", missing));
            return font;
        }

        private static void BindUiFont(string path, TMP_FontAsset font)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.font = font;
                    text.fontSharedMaterial = font.material;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void PrepareBoardGeometry()
        {
            const string tilePath = "Assets/_Game/Prefabs/Board/BoardTile.prefab";
            GameObject tile = PrefabUtility.LoadPrefabContents(tilePath);
            try
            {
                tile.transform.Find("Body").GetComponent<Renderer>().sharedMaterial =
                    AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/Generated/BoardTile.mat");
                foreach (TMP_Text text in tile.GetComponentsInChildren<TMP_Text>(true))
                {
                    bool name = text.name == "Name";
                    text.enableAutoSizing = true;
                    text.fontSize = name ? 2.5f : 1.35f;
                    text.fontSizeMin = name ? 1.6f : .85f;
                    text.fontSizeMax = text.fontSize;
                    if (name) text.rectTransform.sizeDelta = new Vector2(1.28f, .55f);
                }
                PrefabUtility.SaveAsPrefabAsset(tile, tilePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(tile); }

            foreach (string color in new[] { "Red", "Blue", "Gold", "Green" })
            {
                string path = "Assets/_Game/Prefabs/Characters/PlayerToken_" + color + ".prefab";
                GameObject token = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    NormalizeVisual(token.transform.Find("VisualRoot").GetChild(0), .75f, .55f);
                    token.transform.Find("AccentRing").localPosition = Vector3.zero;
                    PrefabUtility.SaveAsPrefabAsset(token, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(token); }
            }
            foreach (string level in new[] { "Stake", "House1", "House2", "House3", "Hotel" })
            {
                string path = "Assets/_Game/Prefabs/Buildings/Building_" + level + ".prefab";
                GameObject building = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    NormalizeVisual(building.transform.GetChild(0), .8f, .7f);
                    PrefabUtility.SaveAsPrefabAsset(building, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(building); }
            }
        }

        private static void NormalizeVisual(Transform model, float height, float width)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new System.InvalidOperationException("Model không có Renderer: " + model.name);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            if (bounds.size.y <= 0) throw new System.InvalidOperationException("Model có bounds rỗng: " + model.name);
            float scale = Mathf.Min(height / bounds.size.y, width / Mathf.Max(bounds.size.x, bounds.size.z));
            model.localScale *= scale;
            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            model.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        [MenuItem("BTL Game/Build Integrated Windows Player")]
        public static void BuildWindowsPlayer()
        {
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Game/Scenes/MainMenu.unity", "Assets/_Game/Scenes/Gameplay.unity" },
                locationPathName = "Builds/TeamIntegration/BTLGame.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException("Build Windows thất bại: " + report.summary.result);
            Debug.Log("TEAM_INTEGRATION_BUILD_SUCCEEDED: " + report.summary.totalSize + " bytes");
        }

        // Chỉ gọi cho hai file Performance Testing tự sinh trong đợt xác minh này.
        // AssetDatabase quản lý cả metadata; không chỉnh/xóa GUID thủ công.
        public static void CleanVerificationArtifacts()
        {
            AssetDatabase.DeleteAsset("Assets/Resources/PerformanceTestRunInfo.json");
            AssetDatabase.DeleteAsset("Assets/Resources/PerformanceTestRunSettings.json");
            if (System.IO.Directory.Exists("Assets/Resources") &&
                System.IO.Directory.GetFileSystemEntries("Assets/Resources").Length == 0)
                AssetDatabase.DeleteAsset("Assets/Resources");
        }

        private static void UpdateMenuGuide()
        {
            const string path = "Assets/_Game/Prefabs/UI/MainMenuCanvas.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.transform.Find("GuidePanel").GetComponent<Image>().raycastTarget = true;
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                    if (text.name == "GuideBody") text.text = text.text.Replace("qua ô Xuất phát nhận 100.000", "qua hoặc dừng ô Xuất phát nhận 200.000");
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void AddScrollableOptions()
        {
            const string path = "Assets/_Game/Prefabs/UI/DecisionPopup.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform panel = root.transform.Find("PopupPanel");
                if (panel.Find("OptionsScroll") != null) return;
                RectTransform content = panel.Find("Options").GetComponent<RectTransform>();
                var scrollObject = new GameObject("OptionsScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
                scrollObject.transform.SetParent(panel, false);
                RectTransform viewport = scrollObject.GetComponent<RectTransform>();
                viewport.sizeDelta = new Vector2(800, 245);
                viewport.anchoredPosition = new Vector2(0, -177);
                scrollObject.GetComponent<Image>().color = new Color(0, 0, 0, .05f);
                content.SetParent(viewport, false);
                content.anchorMin = new Vector2(0, 1);
                content.anchorMax = new Vector2(1, 1);
                content.pivot = new Vector2(.5f, 1);
                content.sizeDelta = Vector2.zero;
                content.anchoredPosition = Vector2.zero;
                foreach (Button button in content.GetComponentsInChildren<Button>(true))
                {
                    LayoutElement sizing = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
                    sizing.minHeight = 58;
                    sizing.preferredHeight = 64;
                }
                ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
                scroll.content = content;
                scroll.viewport = viewport;
                scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 35;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
