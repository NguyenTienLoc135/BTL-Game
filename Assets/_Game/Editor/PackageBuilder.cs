using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BTLGame.Cards;

namespace BTLGame.Editor
{
    public static partial class PackageBuilder
    {
        private const string PrefabRoot = "Assets/_Game/Prefabs";
        private const string DataRoot = "Assets/_Game/Data";
        private const string MaterialRoot = "Assets/_Game/Materials/Generated";
        private const string FontPath = TeamIntegrationSetup.UiFontPath;

        [MenuItem("BTL Game/Build Complete C and B Presentation")]
        public static void BuildAll()
        {
            EnsureFolder("Assets/_Game/Editor");
            EnsureFolder(PrefabRoot + "/Board");
            EnsureFolder(PrefabRoot + "/Buildings");
            EnsureFolder(PrefabRoot + "/Characters");
            EnsureFolder(PrefabRoot + "/UI");
            EnsureFolder(DataRoot + "/Cards");
            EnsureFolder(MaterialRoot);
            TeamIntegrationSetup.EnsureUiFont();

            CreatePresentationMaterials();
            EventCardCatalog catalog = CreateCardCatalog();
            GameObject menuCanvas = CreateMainMenuPrefab();
            GameObject decisionPopup = CreateDecisionPopupPrefab();
            GameObject hudPrefab = CreateHudPrefab();
            GameObject gameplayCanvas = CreateGameplayCanvasPrefab(decisionPopup, hudPrefab, catalog);
            GameObject[] stagePrefabs = CreateBuildingPrefabs();
            GameObject tilePrefab = CreateTilePrefab(stagePrefabs);
            GameObject[] tokenPrefabs = CreateTokenPrefabs();
            GameObject boardPrefab = CreateBoardPrefab(tilePrefab, tokenPrefabs);

            BuildMainMenuScene(menuCanvas);
            BuildGameplayScene(boardPrefab, gameplayCanvas);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            TeamIntegrationSetup.Prepare();
            Debug.Log("C/B presentation assets created and connected to Core/D.");
        }

        private static EventCardCatalog CreateCardCatalog()
        {
            CardSpec[] specs =
            {
                new CardSpec(1, "Tiền thưởng", "Nhận 300.000 từ ngân hàng.", EventCardActionKind.ReceiveBankPayment, false),
                new CardSpec(2, "Động đất", "Chọn thành phố đối thủ Nhà 1–Nhà 3 để hạ đúng một cấp.", EventCardActionKind.ChooseEarthquakeTarget, false),
                new CardSpec(3, "Miễn thuế", "Giữ lại; có thể dùng chủ động để miễn một lần chịu thuế.", EventCardActionKind.HoldTaxExemption, true),
                new CardSpec(4, "Giảm tiền thuê", "Giữ lại; có thể dùng chủ động để giảm 50% một nghĩa vụ thuê.", EventCardActionKind.HoldRentReduction, true),
                new CardSpec(5, "Bảo vệ công trình", "Giữ lại; tự chặn một tác động phá hoại hợp lệ.", EventCardActionKind.HoldBuildingProtection, true),
                new CardSpec(6, "Nộp phạt thuế", "Nộp 20% tổng tài sản tại lúc phát sinh; Luật Sư Passive giảm 25% riêng thuế thẻ.", EventCardActionKind.AssessTax, false),
                new CardSpec(7, "Vào nhà tù", "Đưa người rút vào tù.", EventCardActionKind.GoToJail, false),
                new CardSpec(8, "Ra tù", "Giữ lại; dùng để ra khỏi tù.", EventCardActionKind.HoldJailRelease, true),
                new CardSpec(9, "Phá tài sản", "Chọn thành phố đối thủ Cọc–Nhà 3; chủ cũ nhận lại 100% vốn thực còn lại.", EventCardActionKind.ChoosePropertyToDestroy, false),
                new CardSpec(10, "Quà từ đối thủ", "Mỗi đối thủ còn chơi có nghĩa vụ chuyển 100.000 cho người rút.", EventCardActionKind.CreateOpponentPaymentBatch, false),
                new CardSpec(11, "Đến ô dịch chuyển", "Di chuyển đến ô 24; nếu dùng chức năng dịch chuyển thì trả 100.000.", EventCardActionKind.MoveToTeleport, false),
                new CardSpec(12, "Tổ chức lễ hội", TeamIntegrationSetup.FestivalDescription, EventCardActionKind.ChooseFestivalCity, false),
                new CardSpec(13, "Cúp điện", "Chọn thành phố đối thủ chưa là Khách sạn; chủ không thu thuê trong ba lượt riêng tiếp theo của người dùng.", EventCardActionKind.ChoosePowerOutageTarget, false),
                new CardSpec(14, "Tặng quà 200.000", "Chọn một đối thủ để nhận nghĩa vụ chuyển 200.000.", EventCardActionKind.ChooseGiftRecipient, false)
            };
            var cards = new EventCardData[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                string path = DataRoot + "/Cards/EventCard_" + specs[i].Id.ToString("00") + ".asset";
                EventCardData card = AssetDatabase.LoadAssetAtPath<EventCardData>(path);
                if (card == null)
                {
                    card = ScriptableObject.CreateInstance<EventCardData>();
                    AssetDatabase.CreateAsset(card, path);
                }
                var serialized = new SerializedObject(card);
                serialized.FindProperty("_id").intValue = specs[i].Id;
                serialized.FindProperty("_title").stringValue = specs[i].Title;
                serialized.FindProperty("_description").stringValue = specs[i].Description;
                serialized.FindProperty("_actionKind").enumValueIndex = (int)specs[i].ActionKind;
                serialized.FindProperty("_canBeHeld").boolValue = specs[i].CanBeHeld;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(card);
                cards[i] = card;
            }

            const string catalogPath = DataRoot + "/Cards/EventCardCatalog.asset";
            EventCardCatalog catalog = AssetDatabase.LoadAssetAtPath<EventCardCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<EventCardCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }
            var catalogSerialized = new SerializedObject(catalog);
            SerializedProperty list = catalogSerialized.FindProperty("_cards");
            list.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            catalogSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void CreatePresentationMaterials()
        {
            CreateMaterial("BoardBase", Hex("#355D56"));
            CreateMaterial("BoardGround", Hex("#243B3A"));
            CreateMaterial("BoardTile", Hex("#D5C9A8"));
            CreateMaterial("BoardOwner", Color.white);
            CreateMaterial("BoardProperty", Hex("#E5C66D"));
            CreateMaterial("BoardHotel", Hex("#F3E9CF"));
            CreateMaterial("BoardHighlight", Hex("#FFE578"), true);
            CreateMaterial("BoardTokenAccent", Color.white);
            CreateMaterial("UiPanel", Hex("#173B3A"));
            CreateMaterial("UiAccent", Hex("#D8A64E"));
        }

        private static Material CreateMaterial(string name, Color color, bool emission = false)
        {
            string path = MaterialRoot + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (emission && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.45f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        private static TMPFontHolder FontHolder;

        private static TMPro.TMP_FontAsset Font
        {
            get
            {
                if (FontHolder == null) FontHolder = new TMPFontHolder();
                return FontHolder.Value;
            }
        }

        private sealed class TMPFontHolder
        {
            public TMPro.TMP_FontAsset Value => AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(FontPath);
        }

        private readonly struct CardSpec
        {
            public int Id { get; }
            public string Title { get; }
            public string Description { get; }
            public EventCardActionKind ActionKind { get; }
            public bool CanBeHeld { get; }
            public CardSpec(int id, string title, string description, EventCardActionKind actionKind, bool canBeHeld)
            { Id = id; Title = title; Description = description; ActionKind = actionKind; CanBeHeld = canBeHeld; }
        }
    }
}
