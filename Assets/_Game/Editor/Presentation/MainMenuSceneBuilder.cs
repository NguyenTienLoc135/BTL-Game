#if UNITY_EDITOR
using BTLGame.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static BTLGame.EditorTools.UiKit;

namespace BTLGame.EditorTools
{
    /// <summary>
    /// Dựng Scene MainMenu: Tools ▸ CoTyPhu ▸ Build MainMenu Scene. Chạy lại được nhiều lần.
    /// Bố cục: logo + 4 nút (Bắt đầu chơi / Hướng dẫn / Về trò chơi / Thoát) + 3 icon góc phải trên;
    /// "Bắt đầu chơi" mở popup chọn chế độ Bot / Người. Chỉ dùng asset đã chọn (Kenney + Be Vietnam Pro).
    /// </summary>
    public static class MainMenuSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/MainMenu.unity";
        private const string RootName = "MainMenu_Generated";

        [MenuItem("Tools/CoTyPhu/Build MainMenu Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SceneKit.ResetScene(scene);

            var root = new GameObject(RootName);
            SetupCameraAndLight();
            BuildDiorama(root.transform);
            BuildUi(root.transform);
            SceneKit.EnsureEventSystem(root.transform);

            // Nhạc nền menu (Pizzicato jingles); tiếng click nút do UiClickSound trên Canvas phát.
            SceneKit.AddMusic(root.transform, SceneKit.MenuMusic, 0.4f);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MainMenuSceneBuilder] Đã dựng MainMenu. Hãy nhấn Play để kiểm tra trong Unity.");
        }

        // ---------------------------------------------------------------- nền 3D

        private static void SetupCameraAndLight()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.8f, 0.97f);
                cam.orthographic = false;
                cam.fieldOfView = 30f;
                cam.transform.position = new Vector3(0f, 3.4f, -14f);
                cam.transform.LookAt(new Vector3(0f, 2.8f, 0f));
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.8f, 0.84f, 0.92f);

            var light = Object.FindFirstObjectByType<Light>();
            if (light != null && light.type == LightType.Directional)
            {
                light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
                light.intensity = 1.1f;
            }
        }

        private static void BuildDiorama(Transform parent)
        {
            var diorama = new GameObject("Diorama").transform;
            diorama.SetParent(parent, false);

            SceneKit.Primitive(PrimitiveType.Cube, "Ground", diorama, new Vector3(0f, -0.5f, 2f), new Vector3(70f, 1f, 30f), SceneKit.Mat("MenuGround", new Color(0.5f, 0.8f, 0.42f)));

            // Hai hàng nhà/tòa nhà tạo đường chân trời thành phố.
            string[] back =
            {
                "building-skyscraper-a", "building-c", "building-skyscraper-d", "building-f", "building-skyscraper-b", "building-h",
                "building-skyscraper-e", "building-k", "building-skyscraper-c", "building-m", "building-skyscraper-a", "building-c"
            };
            float[] heights = { 8f, 3.6f, 9f, 4f, 7.5f, 3.4f, 8.5f, 3.8f, 7f, 3.6f, 8f, 3.4f };
            for (int i = 0; i < back.Length; i++)
            {
                float x = Mathf.Lerp(-13f, 13f, i / (back.Length - 1f));
                SceneKit.Spawn(SceneKit.Commercial + back[i] + ".fbx", diorama, new Vector3(x, 0f, 7f), heights[i], 0f);
            }

            string[] front = { "building-a", "building-d", "building-g", "building-j", "building-b", "building-i" };
            for (int i = 0; i < front.Length; i++)
            {
                float x = Mathf.Lerp(-10f, 10f, i / (front.Length - 1f));
                SceneKit.Spawn(SceneKit.Commercial + front[i] + ".fbx", diorama, new Vector3(x, 0f, 3.5f), 2.6f, (i % 2 == 0 ? 8f : -8f));
            }

            // Cây hai bên cho cảnh có chiều sâu.
            for (int i = 0; i < 14; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (5.5f + (i / 2) * 0.9f);
                SceneKit.Spawn(SceneKit.Suburban + (i % 3 == 0 ? "tree-small" : "tree-large") + ".fbx", diorama,
                    new Vector3(x, 0f, -1.5f + (i % 3) * 1.6f), 1.8f + (i % 4) * 0.25f, i * 37f);
            }
        }

        // ---------------------------------------------------------------- UI

        private static void BuildUi(Transform parent)
        {
            var canvasGo = SceneKit.NewCanvas(parent);
            var c = canvasGo.transform;

            // --- Trang trí góc trái dưới: xúc xắc lớn + đồng xu (chỉ là hình trang trí)
            var diceBig = NewImage("DecoDice", c, Icon("dice_3D_detailed"), Color.white, new Vector2(-640f, -330f), new Vector2(300f, 300f));
            diceBig.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 10f);
            diceBig.raycastTarget = false;
            AddShadow(diceBig.gameObject);
            for (int i = 0; i < 4; i++)
            {
                var coin = NewImage("DecoCoin" + i, c, Icon("dollar"), Gold, new Vector2(-880f + i * 62f, -430f + (i % 2) * 40f), new Vector2(90f, 90f));
                coin.raycastTarget = false;
                AddShadow(coin.gameObject);
            }

            // --- Logo
            var logo = NewRect("Logo", c, new Vector2(0f, 300f), new Vector2(1200f, 420f));
            var crown = NewImage("Crown", logo, Icon("crown_a"), Gold, new Vector2(0f, 175f), new Vector2(150f, 150f));
            crown.raycastTarget = false;
            AddShadow(crown.gameObject);
            var title = NewText("TitleMain", logo, "CỜ TỶ PHÚ", Black, 150, Gold, TextAnchor.MiddleCenter, new Vector2(0f, -5f), new Vector2(1100f, 190f));
            AddOutline(title.gameObject, GoldOutline, 6f);
            foreach (float x in new[] { -230f, 230f })
            {
                var die = NewImage("LogoDice", logo, Icon("dice_3D_detailed"), Color.white, new Vector2(x, -140f), new Vector2(90f, 90f));
                die.rectTransform.localRotation = Quaternion.Euler(0f, 0f, x < 0 ? 12f : -12f);
                die.raycastTarget = false;
                AddShadow(die.gameObject);
            }

            // --- Nút menu (xếp dọc)
            var start = PrimaryButton(c, "Btn_Start", "Bắt đầu chơi", new Vector2(0f, -20f), new Vector2(620f, 120f), 46, IconPlay);
            var howTo = SecondaryButton(c, "Btn_HowTo", "Hướng dẫn", new Vector2(0f, -150f), new Vector2(520f, 92f), 34, Icon("book_open"));
            var about = SecondaryButton(c, "Btn_About", "Về trò chơi", new Vector2(0f, -260f), new Vector2(520f, 92f), 34, IconQuestion);
            var quit = SecondaryButton(c, "Btn_Quit", "Thoát", new Vector2(0f, -370f), new Vector2(520f, 92f), 34, IconPower);

            // --- Góc phải trên: chỉ còn nút Cài đặt (bánh răng)
            var iconSettings = IconButton(c, "Icon_Settings", IconGear, new Vector2(840f, 470f), new Vector2(84f, 84f));

            // --- Popup (khung chung của UiKit)
            var modePopup = BuildModePopup(c, out var botButton, out var humanButton);
            var botSetup = MatchSetupBuilder.Build(c, MainMenuMode.VsBot);
            var humanSetup = MatchSetupBuilder.Build(c, MainMenuMode.VsHuman);
            var howToPopup = BuildGuidePopup(c);
            var aboutPopup = BuildTextPopup(c, "Popup_About", "Về trò chơi",
                "Cờ Tỷ Phú RPG\n" +
                "Board game chiến thuật theo lượt, bàn cờ 3D low poly 32 ô, tối đa 4 người (Bot hoặc nhiều người cùng một máy).\n\n" +
                "Làm bằng Unity 6 (URP).\n" +
                "Model, icon, giao diện, âm thanh: Kenney (CC0).\n" +
                "Font: Be Vietnam Pro (SIL OFL).", 30);
            var settingsPopup = SettingsPanelBuilder.Build(c);

            // --- Controller + tiếng click
            var ctrlGo = new GameObject("MainMenuController");
            ctrlGo.transform.SetParent(c.parent, false);
            var controller = ctrlGo.AddComponent<MainMenuController>();
            var so = new SerializedObject(controller);
            so.FindProperty("_startButton").objectReferenceValue = start;
            so.FindProperty("_howToButton").objectReferenceValue = howTo;
            so.FindProperty("_aboutButton").objectReferenceValue = about;
            so.FindProperty("_quitButton").objectReferenceValue = quit;
            so.FindProperty("_iconSettingsButton").objectReferenceValue = iconSettings;
            so.FindProperty("_modePopup").objectReferenceValue = modePopup;
            so.FindProperty("_howToPopup").objectReferenceValue = howToPopup;
            so.FindProperty("_aboutPopup").objectReferenceValue = aboutPopup;
            so.FindProperty("_settingsPopup").objectReferenceValue = settingsPopup;
            so.FindProperty("_botModeButton").objectReferenceValue = botButton;
            so.FindProperty("_humanModeButton").objectReferenceValue = humanButton;
            so.FindProperty("_botSetupPopup").objectReferenceValue = botSetup.Popup;
            so.FindProperty("_humanSetupPopup").objectReferenceValue = humanSetup.Popup;
            so.FindProperty("_botSetupView").objectReferenceValue = botSetup.View;
            so.FindProperty("_humanSetupView").objectReferenceValue = humanSetup.View;
            so.ApplyModifiedPropertiesWithoutUndo();

            SceneKit.AddClickSound(canvasGo);
        }

        private static UiPopup BuildModePopup(Transform canvas, out Button bot, out Button human)
        {
            var parts = Popup(canvas, "Popup_Mode", "Chọn chế độ chơi", new Vector2(1100f, 700f));
            bot = ModeCard(parts.Inner, "Btn_VsBot", "Chơi với Bot", IconRobot, new Vector2(-230f, 0f));
            human = ModeCard(parts.Inner, "Btn_VsHuman", "Chơi với người", IconPeople, new Vector2(230f, 0f));
            return parts.Popup;
        }

        /// <summary>Thẻ chọn chế độ: chỉ icon + tên (không dòng chú thích).</summary>
        private static Button ModeCard(Transform parent, string name, string title, Sprite icon, Vector2 pos)
        {
            var card = NewImage(name, parent, BtnBlue, Navy, pos, new Vector2(420f, 430f));
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            StyleButton(button);

            var iconBg = NewImage("IconBg", card.transform, BtnGrey, Cream, new Vector2(0f, 62f), new Vector2(300f, 250f));
            iconBg.raycastTarget = false;
            var i = NewImage("Icon", iconBg.transform, icon, NavyDeep, new Vector2(0f, 6f), new Vector2(190f, 190f));
            i.raycastTarget = false;
            i.preserveAspect = true;

            var t = NewText("Title", card.transform, title, Black, 44, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, -145f), new Vector2(390f, 70f));
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 26;
            t.resizeTextMaxSize = 44;
            return button;
        }

        /// <summary>Popup Hướng dẫn: nội dung dài theo docs, cuộn được bằng chuột (con lăn, kéo hoặc thanh cuộn).</summary>
        private static UiPopup BuildGuidePopup(Transform canvas)
        {
            var parts = Popup(canvas, "Popup_HowTo", "Hướng dẫn chơi", new Vector2(1300f, 920f));

            var scrollRt = NewRect("GuideScroll", parts.Inner, new Vector2(-24f, -4f), new Vector2(1140f, 780f));
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            var viewport = NewImage("Viewport", scrollRt, null, new Color(1f, 1f, 1f, 0.001f), Vector2.zero, Vector2.zero);
            Stretch(viewport.rectTransform);
            viewport.raycastTarget = true;   // để nhận kéo/con lăn
            viewport.gameObject.AddComponent<RectMask2D>();

            var body = NewText("Body", viewport.transform, GuideText.Body, Regular, 28, TextDark, TextAnchor.UpperLeft, Vector2.zero, Vector2.zero);
            var content = body.rectTransform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(-24f, 0f);
            body.supportRichText = true;
            body.lineSpacing = 1.05f;
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 45f;

            // Thanh cuộn dọc bên phải: kéo được và luôn hiện để biết còn nội dung.
            var track = NewImage("Scrollbar", parts.Inner, BtnGrey, new Color(0.82f, 0.82f, 0.9f), new Vector2(592f, -4f), new Vector2(34f, 780f));
            var area = NewRect("SlidingArea", track.transform, Vector2.zero, Vector2.zero);
            Stretch(area);
            area.offsetMin = new Vector2(3f, 3f);
            area.offsetMax = new Vector2(-3f, -3f);
            var handle = NewImage("Handle", area, BtnBlue, Navy, Vector2.zero, Vector2.zero);
            Stretch(handle.rectTransform);
            handle.raycastTarget = true;
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle.rectTransform;
            bar.targetGraphic = handle;
            bar.direction = Scrollbar.Direction.BottomToTop;
            track.raycastTarget = true;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            return parts.Popup;
        }

        private static UiPopup BuildTextPopup(Transform canvas, string name, string title, string body, int fontSize)
        {
            var parts = Popup(canvas, name, title, new Vector2(1100f, 700f));
            NewText("Body", parts.Inner, body, Regular, fontSize, TextDark, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(880f, 480f));
            return parts.Popup;
        }
    }
}
#endif
