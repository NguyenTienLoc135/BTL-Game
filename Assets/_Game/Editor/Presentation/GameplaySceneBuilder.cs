#if UNITY_EDITOR
using System.Collections.Generic;
using BtlGame.Core;
using BTLGame.Cards;
using BTLGame.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static BTLGame.EditorTools.UiKit;

namespace BTLGame.EditorTools
{
    /// <summary>
    /// Dựng Scene Gameplay: bàn cờ 3D (GameplayBoardBuilder) + HUD + menu tạm dừng.
    /// Tools ▸ CoTyPhu ▸ Build Gameplay Scene. Chạy lại được nhiều lần.
    /// HUD chỉ là view: dữ liệu mẫu trong Scene (Bạn/Bot, 300.000đ, 60:00, Vòng 1) để xem bố cục;
    /// Core sẽ đẩy dữ liệu thật qua GameplayHudView.Set*.
    /// </summary>
    public static class GameplaySceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Gameplay.unity";
        private const string BoardRoot = "Board_Generated";
        private const string UiRoot = "GameplayUI_Generated";

        [MenuItem("Tools/CoTyPhu/Build Gameplay Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SceneKit.ResetScene(scene);

            SetupCameraAndLight();

            var board = new GameObject(BoardRoot);
            GameplayBoardBuilder.Build(board.transform);

            var ui = new GameObject(UiRoot);
            BuildHud(ui.transform);
            SceneKit.EnsureEventSystem(ui.transform);

            // Điểm nối Core: UnityMatchHost (Codex) tạo trận từ cấu hình menu và điều khiển BoardView + giao diện qua IGameplayUi.
            new GameObject("UnityMatchHost", typeof(UnityMatchHost));

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GameplaySceneBuilder] Đã dựng Gameplay (bàn cờ + HUD). Mở Scene Gameplay để kiểm tra trong Unity.");
        }

        private static void SetupCameraAndLight()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camGo.tag = "MainCamera";
                cam = camGo.GetComponent<Camera>();
            }

            // Camera nhìn chéo CỐ ĐỊNH (GAME_RULES §0): không có script xoay/zoom.
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.82f, 0.96f);
            cam.orthographic = false;
            cam.fieldOfView = 32f;
            cam.transform.position = new Vector3(0f, 27f, -25f);
            cam.transform.LookAt(new Vector3(0f, 0f, -1f));

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.8f, 0.84f, 0.92f);

            var light = Object.FindFirstObjectByType<Light>();
            if (light != null && light.type == LightType.Directional)
            {
                light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
                light.intensity = 1.15f;
            }
        }

        // ---------------------------------------------------------------- HUD

        private static void BuildHud(Transform parent)
        {
            var canvasGo = SceneKit.NewCanvas(parent);
            var c = canvasGo.transform;
            var hudGo = new GameObject("Hud", typeof(RectTransform));
            hudGo.transform.SetParent(c, false);
            Stretch((RectTransform)hudGo.transform);
            var hud = hudGo.AddComponent<GameplayHudView>();
            var hc = hudGo.transform;

            // --- Danh sách người chơi (trái trên)
            string[] names = { "Bạn", "Bot 1", "Bot 2", "Bot 3" };
            var rows = new List<RowRefs>();
            for (int i = 0; i < 4; i++)
            {
                rows.Add(PlayerRow(hc, names[i], SeatColors[i], new Vector2(-770f, 455f - i * 112f)));
            }

            // --- Đồng hồ + vòng (phải trên)
            var clock = InfoPill(hc, "Clock", Icon("hourglass"), "Thời gian: 60:00", new Vector2(770f, 490f));
            var round = InfoPill(hc, "Round", Icon("flag_triangle"), "Vòng: 1", new Vector2(770f, 420f));

            // --- Cột nút bên phải
            var assets = SideButton(hc, "Btn_Assets", Icon("structure_house"), "Tài sản", new Vector2(880f, 270f));
            var cards = SideButton(hc, "Btn_Cards", Icon("cards_fan"), "Thẻ", new Vector2(880f, 155f));
            var skills = SideButton(hc, "Btn_Skills", Icon("sword"), "Kỹ năng", new Vector2(880f, 40f));
            var menu = SideButton(hc, "Btn_Menu", null, "Menu", new Vector2(880f, -75f));
            AddMenuBars(menu.transform);

            // --- Xúc xắc + nút Xúc xắc (dưới giữa)
            var dicePanel = NewImage("DicePanel", hc, BtnBlue, NavyDeep, new Vector2(-190f, -450f), new Vector2(290f, 120f));
            var faces = new Sprite[6];
            for (int i = 0; i < 6; i++) faces[i] = Icon("dice_" + (i + 1));
            var dieA = NewImage("DieA", dicePanel.transform, faces[2], Color.white, new Vector2(-70f, 3f), new Vector2(86f, 86f));
            var dieB = NewImage("DieB", dicePanel.transform, faces[4], Color.white, new Vector2(70f, 3f), new Vector2(86f, 86f));
            dieA.raycastTarget = false;
            dieB.raycastTarget = false;
            var roll = PrimaryButton(hc, "Btn_Roll", "Xúc xắc", new Vector2(130f, -450f), new Vector2(290f, 112f), 44);

            // --- Popup: tạm dừng + cài đặt (cùng khung với MainMenu)
            var pause = Popup(c, "Popup_Pause", "Tạm dừng", new Vector2(900f, 620f));
            var resume = PrimaryButton(pause.Inner, "Btn_Resume", "Tiếp tục", new Vector2(0f, 120f), new Vector2(520f, 104f), 40);
            var settingsBtn = SecondaryButton(pause.Inner, "Btn_Settings", "Cài đặt", new Vector2(0f, 0f), new Vector2(520f, 92f), 34, IconGear);
            var toMenu = SecondaryButton(pause.Inner, "Btn_MainMenu", "Về menu chính", new Vector2(0f, -115f), new Vector2(520f, 92f), 34, Icon("structure_house"));
            var settings = SettingsPanelBuilder.Build(c);

            // --- Popup quyết định (không đóng bằng X/nền; chỉ đóng khi Core xử lý xong) + Tài sản + Thẻ & trạng thái
            var decisionView = BuildDecisionPopup(c);
            var assetsView = BuildInfoPopup(c, "Popup_Assets", "Tài sản", true);
            var statusView = BuildInfoPopup(c, "Popup_Status", "Thẻ & trạng thái", false);

            // --- Gắn field cho HUD
            var so = new SerializedObject(hud);
            var rowsProp = so.FindProperty("_rows");
            rowsProp.arraySize = rows.Count;
            for (int i = 0; i < rows.Count; i++)
            {
                var p = rowsProp.GetArrayElementAtIndex(i);
                p.FindPropertyRelative("Root").objectReferenceValue = rows[i].Root;
                p.FindPropertyRelative("Avatar").objectReferenceValue = rows[i].Avatar;
                p.FindPropertyRelative("Name").objectReferenceValue = rows[i].Name;
                p.FindPropertyRelative("Cash").objectReferenceValue = rows[i].Cash;
                p.FindPropertyRelative("Crown").objectReferenceValue = rows[i].Crown;
                p.FindPropertyRelative("Highlight").objectReferenceValue = rows[i].Highlight;
            }

            so.FindProperty("_clockText").objectReferenceValue = clock;
            so.FindProperty("_roundText").objectReferenceValue = round;
            so.FindProperty("_dieA").objectReferenceValue = dieA;
            so.FindProperty("_dieB").objectReferenceValue = dieB;
            var facesProp = so.FindProperty("_diceFaces");
            facesProp.arraySize = 6;
            for (int i = 0; i < 6; i++) facesProp.GetArrayElementAtIndex(i).objectReferenceValue = faces[i];
            so.FindProperty("_rollButton").objectReferenceValue = roll;
            so.FindProperty("_rollLabel").objectReferenceValue = roll.GetComponentInChildren<Text>();
            so.FindProperty("_assetsButton").objectReferenceValue = assets;
            so.FindProperty("_cardsButton").objectReferenceValue = cards;
            so.FindProperty("_skillsButton").objectReferenceValue = skills;
            so.FindProperty("_menuButton").objectReferenceValue = menu;
            so.ApplyModifiedPropertiesWithoutUndo();

            // --- Cầu nối IGameplayUi: UnityMatchHost chỉ nói chuyện với giao diện qua interface này
            var uiGo = new GameObject("GameplayUi");
            uiGo.transform.SetParent(parent, false);
            var gameplayUi = uiGo.AddComponent<PresentationGameplayUi>();
            var uso = new SerializedObject(gameplayUi);
            uso.FindProperty("_hud").objectReferenceValue = hud;
            uso.FindProperty("_decision").objectReferenceValue = decisionView;
            uso.FindProperty("_assets").objectReferenceValue = assetsView;
            uso.FindProperty("_status").objectReferenceValue = statusView;
            uso.FindProperty("_cardCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EventCardCatalog>("Assets/_Game/Data/Cards/EventCardCatalog.asset");
            uso.ApplyModifiedPropertiesWithoutUndo();

            // --- Menu tạm dừng
            var pauseGo = new GameObject("GameplayPauseMenu");
            pauseGo.transform.SetParent(parent, false);
            var pauseMenu = pauseGo.AddComponent<GameplayPauseMenu>();
            var pso = new SerializedObject(pauseMenu);
            pso.FindProperty("_hud").objectReferenceValue = hud;
            pso.FindProperty("_pausePopup").objectReferenceValue = pause.Popup;
            pso.FindProperty("_settingsPopup").objectReferenceValue = settings;
            pso.FindProperty("_resumeButton").objectReferenceValue = resume;
            pso.FindProperty("_settingsButton").objectReferenceValue = settingsBtn;
            pso.FindProperty("_mainMenuButton").objectReferenceValue = toMenu;
            pso.ApplyModifiedPropertiesWithoutUndo();

            SceneKit.AddClickSound(canvasGo);
        }

        private const int MaxDecisionOptions = 24;

        /// <summary>Popup quyết định: tiêu đề + nội dung + danh sách lựa chọn cuộn được (tối đa 24, đủ cho danh sách tài sản khi thanh lý).</summary>
        private static DecisionPopupView BuildDecisionPopup(Transform canvas)
        {
            var parts = Popup(canvas, "Popup_Decision", "Chọn hành động", new Vector2(1100f, 900f), null, false);
            var inner = parts.Inner;

            var message = NewText("Message", inner, string.Empty, Regular, 30, TextDark, TextAnchor.UpperLeft, new Vector2(0f, 270f), new Vector2(940f, 230f));
            message.resizeTextForBestFit = true;
            message.resizeTextMinSize = 18;
            message.resizeTextMaxSize = 30;
            message.verticalOverflow = VerticalWrapMode.Truncate;

            var scrollRt = NewRect("Options", inner, new Vector2(0f, -120f), new Vector2(960f, 500f));
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            var viewport = NewRect("Viewport", scrollRt, Vector2.zero, Vector2.zero);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = NewRect("Content", viewport, Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var buttons = new Button[MaxDecisionOptions];
            var labels = new Text[MaxDecisionOptions];
            for (int i = 0; i < MaxDecisionOptions; i++)
            {
                var b = SecondaryButton(content, "OptionButton_" + i, "Lựa chọn", Vector2.zero, new Vector2(900f, 72f), 28);
                var le = b.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 72f;
                le.preferredHeight = 72f;
                buttons[i] = b;
                labels[i] = b.GetComponentInChildren<Text>();
                b.gameObject.SetActive(false);
            }

            var view = parts.Root.AddComponent<DecisionPopupView>();
            var so = new SerializedObject(view);
            so.FindProperty("_popup").objectReferenceValue = parts.Popup;
            so.FindProperty("_title").objectReferenceValue = parts.TitleText;
            so.FindProperty("_message").objectReferenceValue = message;
            so.FindProperty("_scroll").objectReferenceValue = scroll;
            var bp = so.FindProperty("_optionButtons");
            var lp = so.FindProperty("_optionLabels");
            bp.arraySize = MaxDecisionOptions;
            lp.arraySize = MaxDecisionOptions;
            for (int i = 0; i < MaxDecisionOptions; i++)
            {
                bp.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
                lp.GetArrayElementAtIndex(i).objectReferenceValue = labels[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>Popup chỉ đọc (Tài sản / Thẻ & trạng thái); popup Tài sản có thêm nút "Bán tài sản".</summary>
        private static InfoPopupView BuildInfoPopup(Transform canvas, string name, string title, bool withAction)
        {
            var parts = Popup(canvas, name, title, new Vector2(1000f, 780f));
            var inner = parts.Inner;
            var body = NewText("Body", inner, string.Empty, Regular, 30, TextDark, TextAnchor.UpperLeft,
                new Vector2(0f, withAction ? 40f : 0f), new Vector2(860f, withAction ? 520f : 600f));
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 16;
            body.resizeTextMaxSize = 30;
            body.verticalOverflow = VerticalWrapMode.Truncate;

            Button action = withAction ? SecondaryButton(inner, "Btn_Sell", "Bán tài sản", new Vector2(0f, -290f), new Vector2(420f, 84f), 32) : null;

            var view = parts.Root.AddComponent<InfoPopupView>();
            var so = new SerializedObject(view);
            so.FindProperty("_popup").objectReferenceValue = parts.Popup;
            so.FindProperty("_body").objectReferenceValue = body;
            so.FindProperty("_actionButton").objectReferenceValue = action;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private sealed class RowRefs
        {
            public GameObject Root, Crown, Highlight;
            public Image Avatar;
            public Text Name, Cash;
        }

        private static RowRefs PlayerRow(Transform parent, string playerName, Color color, Vector2 pos)
        {
            var r = new RowRefs();
            var bg = NewImage("Player_" + playerName, parent, BtnBlue, Navy, pos, new Vector2(350f, 100f));
            r.Root = bg.gameObject;

            var hl = NewImage("Highlight", bg.transform, BtnYellow, Color.white, Vector2.zero, new Vector2(350f, 100f));
            hl.raycastTarget = false;
            r.Highlight = hl.gameObject;
            hl.gameObject.SetActive(playerName == "Bạn");

            r.Avatar = NewImage("Avatar", bg.transform, RoundGrey, color, new Vector2(-122f, 3f), new Vector2(76f, 76f));
            var face = NewImage("AvatarIcon", r.Avatar.transform, Icon("character"), Color.white, new Vector2(0f, 2f), new Vector2(44f, 44f));
            face.raycastTarget = false;

            r.Name = NewText("Name", bg.transform, playerName, Bold, 30, Color.white, TextAnchor.MiddleLeft, new Vector2(40f, 20f), new Vector2(210f, 44f));
            AddOutline(r.Name.gameObject, TextOutline, 2f);
            var coin = NewImage("Coin", bg.transform, Icon("dollar"), Gold, new Vector2(-45f, -22f), new Vector2(34f, 34f));
            coin.raycastTarget = false;
            r.Cash = NewText("Cash", bg.transform, GameplayHudView.FormatMoney(300000), Bold, 26, new Color(1f, 0.95f, 0.7f), TextAnchor.MiddleLeft, new Vector2(60f, -22f), new Vector2(190f, 36f));
            AddOutline(r.Cash.gameObject, TextOutline, 2f);

            var crown = NewImage("Crown", bg.transform, Icon("crown_a"), Gold, new Vector2(150f, 34f), new Vector2(46f, 46f));
            crown.raycastTarget = false;
            AddShadow(crown.gameObject);
            r.Crown = crown.gameObject;
            crown.gameObject.SetActive(playerName == "Bạn");
            return r;
        }

        private static Text InfoPill(Transform parent, string name, Sprite icon, string content, Vector2 pos)
        {
            var pill = NewImage(name, parent, BtnBlue, Navy, pos, new Vector2(330f, 62f));
            var i = NewImage("Icon", pill.transform, icon, Color.white, new Vector2(-135f, 2f), new Vector2(34f, 34f));
            i.raycastTarget = false;
            var t = NewText("Text", pill.transform, content, Bold, 26, Color.white, TextAnchor.MiddleLeft, new Vector2(25f, 2f), new Vector2(250f, 50f));
            AddOutline(t.gameObject, TextOutline, 2f);
            return t;
        }

        private static Button SideButton(Transform parent, string name, Sprite icon, string label, Vector2 pos)
        {
            var button = IconButton(parent, name, icon, pos, new Vector2(104f, 104f));
            var iconTf = button.transform.Find("Icon");
            if (iconTf != null)
            {
                var rt = (RectTransform)iconTf;
                rt.anchoredPosition = new Vector2(0f, 14f);
                rt.sizeDelta = new Vector2(46f, 46f);
            }

            var t = NewText("Label", button.transform, label, Bold, 20, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, -30f), new Vector2(100f, 28f));
            AddOutline(t.gameObject, TextOutline, 1.5f);
            return button;
        }

        /// <summary>Biểu tượng ba gạch (hamburger) vẽ bằng Image vì bộ icon không có sẵn.</summary>
        private static void AddMenuBars(Transform button)
        {
            for (int i = 0; i < 3; i++)
            {
                var bar = NewImage("Bar" + i, button, null, Color.white, new Vector2(0f, 26f - i * 13f), new Vector2(40f, 7f));
                bar.raycastTarget = false;
                AddShadow(bar.gameObject);
            }
        }
    }
}
#endif
