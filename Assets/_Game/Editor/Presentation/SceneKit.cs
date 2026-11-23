#if UNITY_EDITOR
using System.IO;
using System.Linq;
using BTLGame.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace BTLGame.EditorTools
{
    /// <summary>Tiện ích dựng Scene dùng chung (model 3D, vật liệu, EventSystem, tiếng click) cho MainMenu và Gameplay.</summary>
    internal static class SceneKit
    {
        private const string MatDir = "Assets/_Game/Materials/Presentation";

        internal const string Commercial = "Assets/ThirdParty/Kenney/CityCommercial/Models/FBX/";
        internal const string Suburban = "Assets/ThirdParty/Kenney/CitySuburban/Models/FBX/";

        /// <summary>Đặt model FBX, chuẩn hóa chiều cao theo Renderer bounds, chân chạm y = position.y.</summary>
        internal static GameObject Spawn(string path, Transform parent, Vector3 position, float targetHeight, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("[SceneKit] Không tìm thấy model: " + path);
                return null;
            }

            var holder = new GameObject(prefab.name);
            holder.transform.SetParent(parent, false);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                if (b.size.y > 0.0001f)
                {
                    instance.transform.localScale = Vector3.one * (targetHeight / b.size.y);
                    b = renderers[0].bounds;
                    foreach (var r in renderers) b.Encapsulate(r.bounds);
                    instance.transform.localPosition = new Vector3(0f, -b.min.y, 0f);
                }
            }

            holder.transform.position = position;
            holder.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return holder;
        }

        /// <summary>
        /// Đưa Scene về trạng thái sạch: xóa mọi object gốc (kể cả nội dung Scene cũ) rồi tạo lại Main Camera + Directional Light.
        /// Builder giao diện dựng lại TOÀN BỘ Scene nên không để lẫn nội dung của bản dựng khác.
        /// </summary>
        internal static void ResetScene(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(go);
            }

            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0f, 1f, -10f);

            var lightGo = new GameObject("Directional Light", typeof(Light));
            lightGo.GetComponent<Light>().type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        /// <summary>Vật liệu URP/Lit đơn sắc, lưu dưới Assets/_Game/Materials/Presentation (tái dùng theo tên).</summary>
        internal static Material Mat(string name, Color color, float smoothness = 0.15f)
        {
            Directory.CreateDirectory(MatDir);
            string path = $"{MatDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader != null ? shader : Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        internal static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        // ---------------------------------------------------------------- âm thanh (bộ Kenney đã chọn, tham chiếu trực tiếp, không sao chép)
        private const string JingleDir = "Assets/ThirdParty/Kenney/MusicJingles/Audio/";
        private const string CasinoDir = "Assets/ThirdParty/Kenney/CasinoAudio/Audio/";
        private const string UiSoundDir = "Assets/ThirdParty/Kenney/InterfaceSounds/Audio/";

        // NHẠC NỀN = bản nhạc DÀI do nhóm bỏ vào Assets/_Game/Audio/Music/Menu và /Gameplay (xem README ở đó).
        // Bộ Kenney Music Jingles KHÔNG dùng làm nhạc nền: đo thực tế mọi đoạn chỉ dài 0,3–1,8 giây (là "stinger"), ghép lại chỉ thành chuỗi nốt rời.
        // Chưa có file thì Scene không có nhạc nền (im lặng) — chủ ý, tốt hơn nghe nốt rời rạc.
        private const string MusicDir = "Assets/_Game/Audio/Music/";

        internal static string[] MenuMusic => FolderClips(MusicDir + "Menu");

        internal static string[] GameMusic => FolderClips(MusicDir + "Gameplay");

        private static string[] FolderClips(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return new string[0];
            }

            var paths = new System.Collections.Generic.List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EnsureStreaming(path);
                paths.Add(path);
            }

            paths.Sort(System.StringComparer.Ordinal);
            return paths.ToArray();
        }

        /// <summary>Nhạc dài: Streaming + Vorbis, không nạp trước → không giữ hàng chục MB PCM trong RAM (WAV gốc ~15–21 MB/bản).</summary>
        private static void EnsureStreaming(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                return;
            }

            var settings = importer.defaultSampleSettings;
            if (settings.loadType == AudioClipLoadType.Streaming && settings.compressionFormat == AudioCompressionFormat.Vorbis
                && Mathf.Approximately(settings.quality, 0.7f) && !settings.preloadAudioData)
            {
                return;
            }

            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.SaveAndReimport();
        }

        internal static readonly string[] BuySfx = { UiSoundDir + "confirmation_001.ogg", UiSoundDir + "confirmation_002.ogg", CasinoDir + "chip-lay-1.ogg" };
        internal static readonly string[] BonusSfx = { JingleDir + "Hit jingles/jingles_HIT03.ogg", JingleDir + "Hit jingles/jingles_HIT04.ogg" };
        internal static readonly string[] JailSfx = { UiSoundDir + "bong_001.ogg", UiSoundDir + "scratch_002.ogg" };
        internal static readonly string[] SkillSfx = { UiSoundDir + "select_006.ogg", UiSoundDir + "switch_003.ogg" };
        internal static readonly string[] BankruptSfx = { UiSoundDir + "error_006.ogg", UiSoundDir + "glitch_001.ogg" };

        internal static readonly string[] DiceSfx = { CasinoDir + "dice-throw-1.ogg", CasinoDir + "dice-throw-2.ogg", CasinoDir + "dice-throw-3.ogg", CasinoDir + "dice-shake-1.ogg" };
        internal static readonly string[] StepSfx = { UiSoundDir + "tick_001.ogg", UiSoundDir + "tick_002.ogg", UiSoundDir + "tick_004.ogg" };
        internal static readonly string[] GainSfx = { CasinoDir + "chips-stack-1.ogg", CasinoDir + "chips-stack-2.ogg", CasinoDir + "chips-stack-3.ogg" };
        internal static readonly string[] PaySfx = { CasinoDir + "chips-handle-1.ogg", CasinoDir + "chips-handle-2.ogg", CasinoDir + "chips-collide-1.ogg" };
        internal static readonly string[] PopupSfx = { UiSoundDir + "open_001.ogg", UiSoundDir + "open_002.ogg" };
        internal static readonly string[] CardSfx = { CasinoDir + "card-slide-1.ogg", CasinoDir + "card-slide-2.ogg", CasinoDir + "card-place-1.ogg" };
        internal static readonly string[] WinSfx = { JingleDir + "Hit jingles/jingles_HIT15.ogg" };
        internal static readonly string[] MonopolySfx = { JingleDir + "Hit jingles/jingles_HIT11.ogg", JingleDir + "Hit jingles/jingles_HIT09.ogg" };

        internal static AudioClip[] Clips(string[] paths)
        {
            var list = new System.Collections.Generic.List<AudioClip>();
            foreach (string path in paths)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null)
                {
                    Debug.LogWarning("[SceneKit] Không tìm thấy âm thanh: " + path);
                    continue;
                }

                list.Add(clip);
            }

            return list.ToArray();
        }

        /// <summary>Tạo object "Music" (AudioSource + MusicPlayer) với danh sách nhạc nền.</summary>
        internal static MusicPlayer AddMusic(Transform parent, string[] playlist, float baseVolume)
        {
            var go = new GameObject("Music");
            go.transform.SetParent(parent, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            var player = go.AddComponent<MusicPlayer>();
            var so = new SerializedObject(player);
            so.FindProperty("_source").objectReferenceValue = source;
            so.FindProperty("_baseVolume").floatValue = baseVolume;
            var list = so.FindProperty("_playlist");
            var clips = Clips(playlist);
            list.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return player;
        }

        internal static void EnsureEventSystem(Transform parent)
        {
            var existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing != null && existing.transform.parent != parent)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.transform.SetParent(parent, false);
            var module = go.AddComponent<InputSystemUIInputModule>(); // dự án chỉ bật Input System mới
            module.AssignDefaultActions();
        }

        /// <summary>Gắn UiClickSound lên Canvas để mọi nút (kể cả popup ẩn) có cùng tiếng click.</summary>
        internal static void AddClickSound(GameObject canvasGo)
        {
            var source = canvasGo.AddComponent<AudioSource>();
            source.playOnAwake = false;
            var click = canvasGo.AddComponent<UiClickSound>();
            var so = new SerializedObject(click);
            so.FindProperty("_source").objectReferenceValue = source;
            so.FindProperty("_clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(UiKit.ClickSound);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static GameObject NewCanvas(Transform parent)
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGo.transform.SetParent(parent, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvasGo;
        }
    }
}
#endif
