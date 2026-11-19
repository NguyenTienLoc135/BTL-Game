using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BtlGame.Board;
using BtlGame.Core;

namespace BtlGame.Editor
{
    public static class GameplaySceneBuilder
    {
        private const string PrefabRoot = "Assets/_Game/Prefabs";
        private const string MaterialRoot = "Assets/_Game/Materials/Board";
        private const string GameplayScenePath = "Assets/_Game/Scenes/Gameplay.unity";
        private static readonly string[] CountryMaterialNames =
        {
            "Vietnam", "Japan", "Korea", "France", "Italy", "Usa", "UnitedKingdom", "Australia"
        };
        private static readonly Color[] CountryColors =
        {
            Hex("#D95D4F"), Hex("#D47A91"), Hex("#5C86C6"), Hex("#6B9A73"),
            Hex("#AE75B4"), Hex("#4B9AA6"), Hex("#DB9E4D"), Hex("#4C9D80")
        };

        [MenuItem("BTL Game/Build Gameplay Presentation")]
        public static void Build()
        {
            EnsureFolder("Assets/_Game/Editor");
            EnsureFolder(MaterialRoot);
            EnsureFolder(PrefabRoot + "/Board");
            EnsureFolder(PrefabRoot + "/Buildings");
            EnsureFolder(PrefabRoot + "/Characters");

            Dictionary<string, Material> materials = CreateMaterials();
            GameObject[] stagePrefabs = CreatePropertyStagePrefabs(materials);
            GameObject tilePrefab = CreateTilePrefab(materials, stagePrefabs);
            GameObject[] tokenPrefabs = CreateTokenPrefabs(materials);
            BuildGameplayScene(tilePrefab, tokenPrefabs, materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Gameplay presentation generated: 32 tile mappings, four token prefabs, five property stages.");
        }

        private static Dictionary<string, Material> CreateMaterials()
        {
            var materials = new Dictionary<string, Material>();
            for (int i = 0; i < CountryMaterialNames.Length; i++)
                materials[CountryMaterialNames[i]] = CreateMaterial("Country_" + CountryMaterialNames[i], CountryColors[i]);
            materials["Start"] = CreateMaterial("Tile_Start", Hex("#E2B85E"));
            materials["Beach"] = CreateMaterial("Tile_Beach", Hex("#64B7C2"));
            materials["Jail"] = CreateMaterial("Tile_Jail", Hex("#BD7168"));
            materials["Event"] = CreateMaterial("Tile_Event", Hex("#9675B8"));
            materials["Festival"] = CreateMaterial("Tile_Festival", Hex("#DB9851"));
            materials["Teleport"] = CreateMaterial("Tile_Teleport", Hex("#5DA8B5"));
            materials["Tax"] = CreateMaterial("Tile_Tax", Hex("#87928F"));
            materials["Board"] = CreateMaterial("Board_Base", Hex("#355D56"));
            materials["Ground"] = CreateMaterial("Ground", Hex("#243B3A"));
            materials["Marker"] = CreateMaterial("Property_Marker", Hex("#F4E8C2"));
            materials["House"] = CreateMaterial("Property_House", Hex("#E6C76A"));
            materials["Hotel"] = CreateMaterial("Property_Hotel", Hex("#F4F0E6"));
            materials["Highlight"] = CreateMaterial("Tile_Highlight", Hex("#FFE578"), true);
            materials["Token"] = CreateMaterial("Token_Accent", Color.white);
            return materials;
        }

        private static Material CreateMaterial(string assetName, Color color, bool emission = false)
        {
            string path = MaterialRoot + "/" + assetName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader) { name = assetName };
                AssetDatabase.CreateAsset(material, path);
            }
            SetMaterialColor(material, color);
            if (emission && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.45f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateTilePrefab(Dictionary<string, Material> materials, GameObject[] stagePrefabs)
        {
            string path = PrefabRoot + "/Board/BoardTile.prefab";
            var root = new GameObject("BoardTile");
            BoardTileView tileView = root.AddComponent<BoardTileView>();

            GameObject body = CreateCube("TileBody", root.transform, Vector3.zero, new Vector3(1.43f, 0.22f, 1.43f), materials["Start"]);
            GameObject owner = CreateCube("OwnerMarker", root.transform, new Vector3(0f, 0.14f, -0.53f), new Vector3(0.92f, 0.12f, 0.12f), materials["Marker"]);
            GameObject label = CreateText("NameLabel", root.transform, Vector3.up * 0.125f, 0.087f, 26, Color.white);
            GameObject id = CreateText("IdLabel", root.transform, new Vector3(0f, 0.13f, 0.52f), 0.055f, 20, Hex("#EDE7D6"));
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            highlight.name = "HighlightRing";
            highlight.transform.SetParent(root.transform, false);
            highlight.transform.localPosition = new Vector3(0f, 0.135f, 0f);
            highlight.transform.localScale = new Vector3(0.98f, 0.008f, 0.98f);
            highlight.GetComponent<Renderer>().sharedMaterial = materials["Highlight"];
            Object.DestroyImmediate(highlight.GetComponent<Collider>());
            highlight.SetActive(false);

            var stages = new Transform[stagePrefabs.Length];
            for (int i = 0; i < stagePrefabs.Length; i++)
            {
                GameObject stage = (GameObject)PrefabUtility.InstantiatePrefab(stagePrefabs[i]);
                stage.transform.SetParent(root.transform, false);
                stage.transform.localPosition = new Vector3(0.37f, 0.13f, 0.30f);
                stage.SetActive(false);
                stages[i] = stage.transform;
            }
            tileView.ConfigureVisuals(body.GetComponent<Renderer>(), owner.GetComponent<Renderer>(),
                label.GetComponent<TextMesh>(), id.GetComponent<TextMesh>(), stages,
                new[] { highlight.GetComponent<Renderer>() });
            tileView.Bind(BoardCatalog.Get(0), Color.white);
            tileView.SetPropertyLevel(null);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject[] CreatePropertyStagePrefabs(Dictionary<string, Material> materials)
        {
            var prefabs = new GameObject[5];
            string[] stageNames = { "Stake", "House1", "House2", "House3", "Hotel" };
            for (int tier = 0; tier < prefabs.Length; tier++)
            {
                var stage = new GameObject("Property_" + stageNames[tier]);
                Material material = tier == 0 ? materials["Marker"] : tier == 4 ? materials["Hotel"] : materials["House"];
                int blocks = tier == 0 ? 1 : tier == 1 ? 1 : tier == 2 ? 2 : tier == 3 ? 3 : 1;
                for (int block = 0; block < blocks; block++)
                {
                    float height = tier == 4 ? 0.42f : 0.16f;
                    Vector3 position = tier == 4
                        ? new Vector3(0f, height * 0.5f, 0f)
                        : new Vector3((block % 2) * 0.16f - 0.08f, height * 0.5f + (block / 2) * 0.035f, 0f);
                    Vector3 scale = tier == 4 ? new Vector3(0.18f, height, 0.18f) : new Vector3(0.11f, height, 0.11f);
                    CreateCube("BuildingPart_" + block, stage.transform, position, scale, material);
                }
                prefabs[tier] = PrefabUtility.SaveAsPrefabAsset(stage, PrefabRoot + "/Buildings/Property_" + stageNames[tier] + ".prefab");
                Object.DestroyImmediate(stage);
            }
            return prefabs;
        }

        private static GameObject[] CreateTokenPrefabs(Dictionary<string, Material> materials)
        {
            string[] variants = { "character-male-a", "character-female-a", "character-male-c", "character-female-c" };
            var prefabs = new GameObject[4];
            for (int i = 0; i < prefabs.Length; i++)
            {
                string path = PrefabRoot + "/Characters/PlayerToken_" + (i + 1) + ".prefab";
                var root = new GameObject("PlayerToken_" + (i + 1));
                PlayerTokenView tokenView = root.AddComponent<PlayerTokenView>();
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "AccentRing";
                ring.transform.SetParent(root.transform, false);
                ring.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                ring.transform.localScale = new Vector3(0.42f, 0.035f, 0.42f);
                ring.GetComponent<Renderer>().sharedMaterial = materials["Token"];
                Object.DestroyImmediate(ring.GetComponent<Collider>());

                var visual = new GameObject("VisualRoot");
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.up * 0.075f;
                GameObject model = LoadCharacterModel(variants[i]);
                if (model != null)
                {
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    instance.name = "KenneyCharacter";
                    instance.transform.SetParent(visual.transform, false);
                    instance.transform.localScale = Vector3.one * 0.42f;
                }
                else
                {
                    GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    fallback.name = "FallbackTokenMesh";
                    fallback.transform.SetParent(visual.transform, false);
                    fallback.transform.localScale = new Vector3(0.32f, 0.42f, 0.32f);
                    Object.DestroyImmediate(fallback.GetComponent<Collider>());
                }

                tokenView.Bind(i, BoardView.GetPlayerColor(i));
                var serialized = new SerializedObject(tokenView);
                serialized.FindProperty("_visualRoot").objectReferenceValue = visual.transform;
                SerializedProperty accents = serialized.FindProperty("_accentRenderers");
                accents.arraySize = 1;
                accents.GetArrayElementAtIndex(0).objectReferenceValue = ring.GetComponent<Renderer>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                prefabs[i] = PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
            }
            return prefabs;
        }

        private static GameObject LoadCharacterModel(string modelName)
        {
            string[] guids = AssetDatabase.FindAssets(modelName + " t:Model", new[] { "Assets/ThirdParty/Kenney/MiniCharacters" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == modelName)
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return null;
        }

        private static void BuildGameplayScene(GameObject tilePrefab, GameObject[] tokenPrefabs,
            Dictionary<string, Material> materials)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Hex("#94A9A0");
            RenderSettings.ambientIntensity = 0.85f;
            RenderSettings.fog = false;

            GameObject boardRoot = new GameObject("GameplayPresentation");
            BoardView boardView = boardRoot.AddComponent<BoardView>();
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.52f, 0f);
            ground.transform.localScale = new Vector3(3.6f, 1f, 3.6f);
            ground.GetComponent<Renderer>().sharedMaterial = materials["Ground"];

            GameObject center = CreateCube("BoardCenter", boardRoot.transform, new Vector3(0f, -0.10f, 0f),
                new Vector3(10.4f, 0.22f, 10.4f), materials["Board"]);
            Object.DestroyImmediate(center.GetComponent<Collider>());
            AddBoardTitle(boardRoot.transform);
            AddKenneyCityDisplay(boardRoot.transform);

            var tiles = new BoardTileView[32];
            foreach (TileDefinition definition in BoardCatalog.Tiles)
            {
                GameObject tileObject = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab, scene);
                tileObject.name = "Tile_" + definition.Id.ToString("00") + "_" + SafeName(definition.Name);
                tileObject.transform.SetParent(boardRoot.transform, false);
                tileObject.transform.localPosition = BoardView.GetTilePosition(definition.Id) + Vector3.up * 0.16f;
                Vector3 inward = -BoardView.GetTilePosition(definition.Id);
                inward.y = 0f;
                if (inward.sqrMagnitude > 0.01f) tileObject.transform.rotation = Quaternion.LookRotation(inward.normalized, Vector3.up);

                BoardTileView tileView = tileObject.GetComponent<BoardTileView>();
                Color tileColor = GetTileColor(definition);
                tileObject.transform.Find("TileBody").GetComponent<Renderer>().sharedMaterial = materials[GetTileMaterialKey(definition)];
                tileView.Bind(definition, tileColor);
                EditorUtility.SetDirty(tileView);
                PrefabUtility.RecordPrefabInstancePropertyModifications(tileView);
                tiles[definition.Id] = tileView;
            }

            var tokens = new PlayerTokenView[4];
            for (int i = 0; i < tokenPrefabs.Length; i++)
            {
                GameObject tokenObject = (GameObject)PrefabUtility.InstantiatePrefab(tokenPrefabs[i], scene);
                tokenObject.name = "PlayerToken_" + i;
                tokenObject.transform.SetParent(boardRoot.transform, false);
                PlayerTokenView tokenView = tokenObject.GetComponent<PlayerTokenView>();
                tokenView.Bind(i, BoardView.GetPlayerColor(i));
                tokenView.SetBoardPosition(BoardView.GetPlayerPosition(i, 0));
                tokens[i] = tokenView;
            }
            boardView.Bind(tiles, tokens);
            EditorUtility.SetDirty(boardView);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.orthographic = true;
            camera.orthographicSize = 10.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("#243B3A");
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            cameraObject.transform.position = new Vector3(0f, 17f, -12f);
            cameraObject.transform.LookAt(Vector3.zero);

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Hex("#FFF1D1");
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameplayScenePath);
        }

        private static void AddBoardTitle(Transform parent)
        {
            GameObject title = CreateText("BoardTitle", parent, new Vector3(0f, 0.03f, -3.9f), 0.45f, 56, Hex("#F3E8C9"));
            title.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh mesh = title.GetComponent<TextMesh>();
            mesh.text = "CO TY PHU RPG";
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.characterSize = 0.09f;
            mesh.fontStyle = FontStyle.Bold;
        }

        private static void AddKenneyCityDisplay(Transform parent)
        {
            string[] buildingNames = { "building-a", "building-c", "low-detail-building-b" };
            Vector3[] positions = { new Vector3(-2.4f, 0.02f, 0.25f), new Vector3(0f, 0.02f, 0.55f), new Vector3(2.35f, 0.02f, 0.2f) };
            for (int i = 0; i < buildingNames.Length; i++)
            {
                GameObject model = LoadModel("Assets/ThirdParty/Kenney/CityCommercial", buildingNames[i]);
                if (model == null) continue;
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                instance.name = "KenneyCityLandmark_" + i;
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = positions[i];
                instance.transform.localScale = Vector3.one * 0.62f;
            }
        }

        private static GameObject LoadModel(string root, string modelName)
        {
            string[] guids = AssetDatabase.FindAssets(modelName + " t:Model", new[] { root });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == modelName)
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return null;
        }

        private static string GetTileMaterialKey(TileDefinition definition)
        {
            switch (definition.Kind)
            {
                case TileKind.Start: return "Start";
                case TileKind.City: return CountryMaterialNames[(int)definition.Country.Value];
                case TileKind.Beach: return "Beach";
                case TileKind.Jail: return "Jail";
                case TileKind.EventCard: return "Event";
                case TileKind.Festival: return "Festival";
                case TileKind.Teleport: return "Teleport";
                default: return "Tax";
            }
        }

        private static Color GetTileColor(TileDefinition definition)
        {
            string key = GetTileMaterialKey(definition);
            for (int i = 0; i < CountryMaterialNames.Length; i++)
                if (CountryMaterialNames[i] == key) return CountryColors[i];
            switch (key)
            {
                case "Start": return Hex("#E2B85E");
                case "Beach": return Hex("#64B7C2");
                case "Jail": return Hex("#BD7168");
                case "Event": return Hex("#9675B8");
                case "Festival": return Hex("#DB9851");
                case "Teleport": return Hex("#5DA8B5");
                default: return Hex("#87928F");
            }
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static GameObject CreateText(string name, Transform parent, Vector3 position, float size,
            int fontSize, Color color)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = position;
            textObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = size;
            text.fontSize = fontSize;
            text.color = color;
            text.text = name;
            return textObject;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private static string SafeName(string value)
        {
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value.Replace(' ', '_').Replace('.', '_');
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out Color color);
            return color;
        }
    }
}
