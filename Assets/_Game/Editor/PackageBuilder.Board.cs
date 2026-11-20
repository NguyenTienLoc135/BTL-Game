using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using BTLGame.Board;

namespace BTLGame.Editor
{
    public static partial class PackageBuilder
    {
        private static GameObject[] CreateBuildingPrefabs()
        {
            string[] labels = { "Stake", "House1", "House2", "House3", "Hotel" };
            var result = new GameObject[labels.Length];
            string[] sourceModels =
            {
                "low-detail-building-a", "low-detail-building-b", "low-detail-building-c",
                "low-detail-building-d", "building-skyscraper-a"
            };
            for (int i = 0; i < labels.Length; i++)
            {
                var root = new GameObject("Building_" + labels[i]);
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/ThirdParty/Kenney/CityCommercial/Models/FBX/" + sourceModels[i] + ".fbx");
                if (model == null) throw new System.InvalidOperationException("Thiếu FBX Kenney: " + sourceModels[i]);
                GameObject body = (GameObject)PrefabUtility.InstantiatePrefab(model);
                body.name = "Visual_" + sourceModels[i];
                body.transform.SetParent(root.transform, false);
                body.transform.localScale = i == 4 ? Vector3.one * .22f : Vector3.one * .18f;
                result[i] = PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/Buildings/Building_" + labels[i] + ".prefab");
                Object.DestroyImmediate(root);
            }
            return result;
        }

        private static GameObject CreateTilePrefab(GameObject[] stages)
        {
            var root = new GameObject("BoardTile");
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(1.38f, .18f, 1.38f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            Renderer bodyRenderer = body.GetComponent<Renderer>();
            TMP_Text name = CreateWorldText("Name", root.transform, new Vector3(0f, .13f, 0f), new Vector2(1.24f, .40f), 0.16f, TextAlignmentOptions.Center);
            TMP_Text detail = CreateWorldText("Detail", root.transform, new Vector3(0f, .135f, -.43f), new Vector2(1.20f, .40f), .105f, TextAlignmentOptions.Center);
            GameObject owner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            owner.name = "OwnerMarker";
            owner.transform.SetParent(root.transform, false);
            owner.transform.localPosition = new Vector3(0f, .12f, .61f);
            owner.transform.localScale = new Vector3(.96f, .045f, .12f);
            Object.DestroyImmediate(owner.GetComponent<Collider>());
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            highlight.name = "Highlight";
            highlight.transform.SetParent(root.transform, false);
            highlight.transform.localPosition = new Vector3(0f, .11f, 0f);
            highlight.transform.localScale = new Vector3(1.43f, .025f, 1.43f);
            highlight.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "/BoardHighlight.mat");
            Object.DestroyImmediate(highlight.GetComponent<Collider>());
            highlight.SetActive(false);
            var stageTransforms = new Transform[5];
            for (int i = 0; i < stages.Length; i++)
            {
                GameObject stage = (GameObject)PrefabUtility.InstantiatePrefab(stages[i]);
                stage.name = "Stage_" + i;
                stage.transform.SetParent(root.transform, false);
                stage.transform.localPosition = new Vector3(0f, .13f + i * .06f, -.18f);
                stageTransforms[i] = stage.transform;
                stage.SetActive(false);
            }
            var view = root.AddComponent<BoardTileView>();
            view.Configure(-1, bodyRenderer, owner.GetComponent<Renderer>(), name, detail, stageTransforms, highlight);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/Board/BoardTile.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject[] CreateTokenPrefabs()
        {
            var result = new GameObject[4];
            string[] names = { "Red", "Blue", "Gold", "Green" };
            string[] models = { "character-female-a", "character-male-a", "character-female-c", "character-male-c" };
            for (int i = 0; i < result.Length; i++)
            {
                var root = new GameObject("PlayerToken_" + names[i]);
                var visualRoot = new GameObject("VisualRoot");
                visualRoot.transform.SetParent(root.transform, false);
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/ThirdParty/Kenney/MiniCharacters/Models/FBX/" + models[i] + ".fbx");
                if (model == null) throw new System.InvalidOperationException("Thiếu FBX Kenney: " + models[i]);
                GameObject body = (GameObject)PrefabUtility.InstantiatePrefab(model);
                body.name = "Character_" + models[i];
                body.transform.SetParent(visualRoot.transform, false);
                body.transform.localScale = Vector3.one * .24f;
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "AccentRing";
                ring.transform.SetParent(root.transform, false);
                ring.transform.localPosition = new Vector3(0f, -.085f, 0f);
                ring.transform.localScale = new Vector3(.43f, .025f, .43f);
                ring.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "/BoardTokenAccent.mat");
                Object.DestroyImmediate(ring.GetComponent<Collider>());
                PlayerTokenView token = root.AddComponent<PlayerTokenView>();
                token.Bind(i, i, BoardView.GetSeatColor(i));
                root.SetActive(false);
                result[i] = PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/Characters/PlayerToken_" + names[i] + ".prefab");
                Object.DestroyImmediate(root);
            }
            return result;
        }

        private static GameObject CreateBoardPrefab(GameObject tilePrefab, GameObject[] tokenPrefabs)
        {
            var root = new GameObject("BoardRoot");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "BoardBase";
            floor.transform.SetParent(root.transform, false);
            floor.transform.localPosition = new Vector3(0f, -.20f, 0f);
            floor.transform.localScale = new Vector3(15.5f, .32f, 15.5f);
            floor.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "/BoardBase.mat");
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            var center = GameObject.CreatePrimitive(PrimitiveType.Cube);
            center.name = "CenterPlaza";
            center.transform.SetParent(root.transform, false);
            center.transform.localPosition = new Vector3(0f, -.015f, 0f);
            center.transform.localScale = new Vector3(10.8f, .08f, 10.8f);
            center.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "/BoardGround.mat");
            Object.DestroyImmediate(center.GetComponent<Collider>());
            var tileViews = new BoardTileView[32];
            for (int i = 0; i < tileViews.Length; i++)
            {
                GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab);
                tile.name = "Tile_" + i.ToString("00");
                tile.transform.SetParent(root.transform, false);
                Vector3 p = BoardView.GetTilePosition(i);
                tile.transform.localPosition = new Vector3(p.x, 0f, p.z);
                if (i == 0 || i == 8 || i == 16 || i == 24) tile.transform.localScale = Vector3.one * 1.12f;
                tileViews[i] = tile.GetComponent<BoardTileView>();
                tileViews[i].Bind(BoardVisualCatalog.Get(i));
            }
            var tokenViews = new PlayerTokenView[4];
            for (int i = 0; i < tokenViews.Length; i++)
            {
                GameObject token = (GameObject)PrefabUtility.InstantiatePrefab(tokenPrefabs[i]);
                token.name = "PlayerTokenSlot_" + i;
                token.transform.SetParent(root.transform, false);
                tokenViews[i] = token.GetComponent<PlayerTokenView>();
            }
            root.AddComponent<BoardView>().Configure(tileViews, tokenViews);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/Board/BoardRoot.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildGameplayScene(GameObject boardPrefab, GameObject uiPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject board = (GameObject)PrefabUtility.InstantiatePrefab(boardPrefab, scene);
            board.name = "BoardRoot";
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 19f, -17f);
            cameraObject.transform.rotation = Quaternion.Euler(48f, 0f, 0f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 12f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.08f, .12f, .13f);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 100f;
            var lightObject = new GameObject("Directional Light", typeof(Light));
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            var canvas = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab, scene);
            canvas.name = "GameplayCanvas";
            CreateEventSystem(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/Gameplay.unity");
        }

        private static TMP_Text CreateWorldText(string name, Transform parent, Vector3 position, Vector2 size,
            float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.color = new Color(.12f, .15f, .14f);
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }
    }
}
