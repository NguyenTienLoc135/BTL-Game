using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using BTLGame.Cards;
using BTLGame.UI;

namespace BTLGame.Editor
{
    public static partial class PackageBuilder
    {
        private static GameObject CreateMainMenuPrefab()
        {
            GameObject canvas = CreateCanvas("MainMenuCanvas");
            CreateImage("Backdrop", canvas.transform, Vector2.zero, new Vector2(1920f, 1080f), Hex("#172D32"));
            GameObject panel = CreateImage("SetupPanel", canvas.transform, Vector2.zero, new Vector2(1080f, 980f), Hex("#1D4242"));
            CreateText("Title", panel.transform, new Vector2(0f, 420f), new Vector2(900f, 70f), "CỜ TỶ PHÚ RPG", 52, Hex("#F4E8CA"), TextAlignmentOptions.Center, FontStyles.Bold);
            CreateText("Subtitle", panel.transform, new Vector2(0f, 365f), new Vector2(900f, 42f), "CHIẾN LƯỢC • TÀI SẢN • NGHỀ NGHIỆP", 22, Hex("#D6C7A6"), TextAlignmentOptions.Center);

            OptionSelector mode = CreateSelector("ModeSelector", panel.transform, "Chế độ", new Vector2(0f, 290f), 770f);
            OptionSelector count = CreateSelector("PlayerCountSelector", panel.transform, "Số người", new Vector2(0f, 190f), 770f);
            OptionSelector time = CreateSelector("TimeLimitSelector", panel.transform, "Thời gian", new Vector2(0f, 90f), 770f);

            var roles = new OptionSelector[4];
            for (int i = 0; i < roles.Length; i++)
            {
                CreateText("PlayerLabel_" + (i + 1), panel.transform, new Vector2(-350f, -15f - i * 82f),
                    new Vector2(180f, 52f), "Người chơi " + (i + 1), 22, Hex("#F4E8CA"), TextAlignmentOptions.Left);
                roles[i] = CreateSelector("RoleSelector_" + (i + 1), panel.transform, "Nghề nghiệp",
                    new Vector2(150f, -15f - i * 82f), 540f);
            }

            Button start = CreateButton("StartButton", panel.transform, new Vector2(0f, -375f), new Vector2(390f, 76f), "TẠO TRẬN", FontSize.Large, Hex("#B58A3B"));
            Button guide = CreateButton("GuideButton", panel.transform, new Vector2(-260f, -455f), new Vector2(250f, 54f), "HƯỚNG DẪN", FontSize.Small, Hex("#326663"));
            CreateText("VolumeLabel", panel.transform, new Vector2(120f, -455f), new Vector2(150f, 48f), "Âm lượng", 18, Hex("#F4E8CA"), TextAlignmentOptions.Right);
            Slider volume = CreateSlider("VolumeSlider", panel.transform, new Vector2(355f, -455f), new Vector2(280f, 26f));
            TMP_Text status = CreateText("SetupStatus", panel.transform, new Vector2(0f, -500f), new Vector2(950f, 34f),
                "", 16, Hex("#E7DDBF"), TextAlignmentOptions.Center);

            GameObject guidePanel = CreateImage("GuidePanel", canvas.transform, Vector2.zero, new Vector2(1120f, 790f), Hex("#143433"));
            CreateText("GuideTitle", guidePanel.transform, new Vector2(0f, 320f), new Vector2(900f, 70f), "LUẬT CƠ BẢN", 42, Hex("#F4E8CA"), TextAlignmentOptions.Center, FontStyles.Bold);
            string guideCopy = "Mỗi người bắt đầu với 300.000 đồng.\n" +
                "Đi theo thứ tự 32 ô; qua ô Xuất phát nhận 100.000.\n" +
                "Mua thành phố hoặc bãi biển, trả thuê khi dừng ở tài sản đối thủ.\n" +
                "Thành phố có Cọc, Nhà 1, Nhà 2, Nhà 3 và Khách sạn.\n" +
                "Sở hữu trọn ba quốc gia bất kỳ hoặc đủ bốn bãi biển để thắng ngay.\n" +
                "Trong tù tối đa ba lượt riêng; ở lượt ba tung không đôi phải trả 200.000.\n" +
                "Chi tiết thẻ, kỹ năng, thuế, thanh lý và hết giờ được hiển thị trong trận.";
            CreateText("GuideBody", guidePanel.transform, new Vector2(0f, -5f), new Vector2(930f, 520f), guideCopy, 22, Hex("#E7DDBF"), TextAlignmentOptions.TopLeft);
            Button closeGuide = CreateButton("CloseGuideButton", guidePanel.transform, new Vector2(0f, -330f), new Vector2(260f, 58f), "ĐÓNG", FontSize.Small, Hex("#B58A3B"));
            guidePanel.SetActive(false);

            MainMenuSetupController controller = canvas.AddComponent<MainMenuSetupController>();
            controller.ConfigureReferences(mode, count, time, roles, start, volume, guide, guidePanel, status);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvas, PrefabRoot + "/UI/MainMenuCanvas.prefab");
            Object.DestroyImmediate(canvas);
            return prefab;
        }

        private static GameObject CreateDecisionPopupPrefab()
        {
            GameObject root = new GameObject("DecisionPopup", typeof(RectTransform), typeof(Image), typeof(DecisionPopupUI));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);
            root.GetComponent<Image>().color = new Color(0.02f, 0.06f, 0.07f, 0.78f);

            GameObject panel = CreateImage("PopupPanel", root.transform, Vector2.zero, new Vector2(900f, 660f), Hex("#1D4242"));
            CreateText("PopupTitle", panel.transform, new Vector2(0f, 255f), new Vector2(800f, 62f), "Quyết định", 36, Hex("#F4E8CA"), TextAlignmentOptions.Center, FontStyles.Bold);
            TMP_Text message = CreateText("PopupMessage", panel.transform, new Vector2(0f, 105f), new Vector2(780f, 210f), "", 23, Hex("#E7DDBF"), TextAlignmentOptions.Center);
            GameObject optionRoot = new GameObject("Options", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            optionRoot.transform.SetParent(panel.transform, false);
            RectTransform optionRect = optionRoot.GetComponent<RectTransform>();
            optionRect.anchorMin = optionRect.anchorMax = new Vector2(0.5f, 0.5f);
            optionRect.pivot = new Vector2(0.5f, 0.5f);
            optionRect.anchoredPosition = new Vector2(0f, -195f);
            optionRect.sizeDelta = new Vector2(620f, 190f);
            VerticalLayoutGroup layout = optionRoot.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            optionRoot.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Button optionTemplate = CreateButton("OptionButtonTemplate", optionRoot.transform, Vector2.zero, new Vector2(600f, 58f), "Lựa chọn", FontSize.Small, Hex("#326663"));
            optionTemplate.gameObject.SetActive(false);
            DecisionPopupUI popup = root.GetComponent<DecisionPopupUI>();
            popup.Configure(panel.transform.Find("PopupTitle").GetComponent<TMP_Text>(), message, optionRoot.transform, optionTemplate);
            root.SetActive(false);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/UI/DecisionPopup.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateHudPrefab()
        {
            GameObject root = new GameObject("GameplayHUD", typeof(RectTransform));
            TMP_Text name = CreateText("PlayerName", root.transform, new Vector2(-760f, 490f), new Vector2(240f, 44f), "Chưa có dữ liệu trận", 22, Hex("#F4E8CA"), TextAlignmentOptions.Left);
            TMP_Text cash = CreateText("Cash", root.transform, new Vector2(-495f, 490f), new Vector2(250f, 44f), "—", 24, Hex("#F4E8CA"), TextAlignmentOptions.Left, FontStyles.Bold);
            TMP_Text turn = CreateText("Turn", root.transform, new Vector2(-190f, 490f), new Vector2(350f, 44f), "", 20, Hex("#E7DDBF"), TextAlignmentOptions.Left);
            TMP_Text clock = CreateText("Clock", root.transform, new Vector2(680f, 490f), new Vector2(170f, 44f), "", 22, Hex("#F4E8CA"), TextAlignmentOptions.Center);
            TMP_Text dice = CreateText("Dice", root.transform, new Vector2(450f, 490f), new Vector2(200f, 44f), "", 22, Hex("#F4E8CA"), TextAlignmentOptions.Center);
            TMP_Text properties = CreateText("Properties", root.transform, new Vector2(-735f, 180f), new Vector2(380f, 330f), "Chưa có dữ liệu trận.", 16, Hex("#E7DDBF"), TextAlignmentOptions.TopLeft);
            TMP_Text status = CreateText("Status", root.transform, new Vector2(-735f, -145f), new Vector2(380f, 130f), "", 16, Hex("#E4C46C"), TextAlignmentOptions.TopLeft);
            Button roll = CreateButton("RollButton", root.transform, new Vector2(760f, -475f), new Vector2(230f, 70f), "TUNG XÚC XẮC", FontSize.Small, Hex("#B58A3B"));
            Button skill = CreateButton("ActiveSkillButton", root.transform, new Vector2(500f, -475f), new Vector2(230f, 70f), "KỸ NĂNG", FontSize.Small, Hex("#326663"));
            Button sell = CreateButton("SellButton", root.transform, new Vector2(240f, -475f), new Vector2(230f, 70f), "TÀI SẢN", FontSize.Small, Hex("#326663"));
            GameHUDController hud = root.AddComponent<GameHUDController>();
            hud.Configure(name, cash, turn, dice, clock, properties, status, roll, skill, sell);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/UI/GameHUD.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateGameplayCanvasPrefab(GameObject decisionPopupPrefab,
            GameObject hudPrefab, EventCardCatalog catalog)
        {
            GameObject canvas = CreateCanvas("GameplayCanvas");
            GameObject top = CreateImage("HudTopBar", canvas.transform, new Vector2(0f, -5f), new Vector2(1310f, 84f), new Color(0.05f, 0.11f, 0.12f, 0.90f));
            top.GetComponent<RectTransform>().anchorMin = top.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1f);
            GameObject side = CreateImage("HudSidePanel", canvas.transform, new Vector2(200f, -245f), new Vector2(410f, 540f), new Color(0.05f, 0.11f, 0.12f, 0.88f));
            side.GetComponent<RectTransform>().anchorMin = side.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 1f);
            GameObject bottom = CreateImage("HudActionPanel", canvas.transform, new Vector2(-15f, 10f), new Vector2(870f, 120f), new Color(0.05f, 0.11f, 0.12f, 0.84f));
            bottom.GetComponent<RectTransform>().anchorMin = bottom.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0f);

            GameObject hudObject = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
            hudObject.name = "GameplayHUD";
            hudObject.transform.SetParent(canvas.transform, false);
            RectTransform hudRect = hudObject.GetComponent<RectTransform>();
            Stretch(hudRect);
            GameObject popup = (GameObject)PrefabUtility.InstantiatePrefab(decisionPopupPrefab);
            popup.name = "DecisionPopup";
            popup.transform.SetParent(canvas.transform, false);

            var cardUiObject = new GameObject("EventCardUI");
            cardUiObject.transform.SetParent(canvas.transform, false);
            EventCardUI cardUi = cardUiObject.AddComponent<EventCardUI>();
            cardUi.Configure(popup.GetComponent<DecisionPopupUI>());
            var cardsObject = new GameObject("EventCardManager");
            cardsObject.transform.SetParent(canvas.transform, false);
            EventCardManager cards = cardsObject.AddComponent<EventCardManager>();
            cards.Configure(catalog, cardUi);

            GameObject routerObject = new GameObject("GameplayUIController");
            routerObject.transform.SetParent(canvas.transform, false);
            GameplayUIController router = routerObject.AddComponent<GameplayUIController>();
            router.Configure(hudObject.GetComponent<GameHUDController>(), popup.GetComponent<DecisionPopupUI>(), cards);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvas, PrefabRoot + "/UI/GameplayCanvas.prefab");
            Object.DestroyImmediate(canvas);
            return prefab;
        }

        private static void BuildMainMenuScene(GameObject menuCanvasPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvas = (GameObject)PrefabUtility.InstantiatePrefab(menuCanvasPrefab, scene);
            canvas.name = "MainMenuCanvas";
            CreateEventSystem(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/MainMenu.unity");
        }

        private static GameObject CreateCanvas(string name)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvasObject;
        }

        private static OptionSelector CreateSelector(string name, Transform parent, string label, Vector2 position, float width)
        {
            GameObject row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            RectTransform rect = row.GetComponent<RectTransform>();
            CenterRect(rect, position, new Vector2(width, 72f));
            TMP_Text labelText = CreateText("Label", row.transform, new Vector2(-width * 0.31f, 0f), new Vector2(width * 0.38f, 56f), label, 21, Hex("#F4E8CA"), TextAlignmentOptions.Left);
            TMP_Text valueText = CreateText("Value", row.transform, new Vector2(width * 0.16f, 0f), new Vector2(width * 0.48f, 56f), "Chưa chọn", 20, Hex("#F4E8CA"), TextAlignmentOptions.Center);
            Button previous = CreateButton("Previous", row.transform, new Vector2(width * 0.42f, 0f), new Vector2(50f, 50f), "‹", FontSize.Large, Hex("#326663"));
            Button next = CreateButton("Next", row.transform, new Vector2(width * 0.50f, 0f), new Vector2(50f, 50f), "›", FontSize.Large, Hex("#326663"));
            OptionSelector selector = row.AddComponent<OptionSelector>();
            selector.ConfigureReferences(labelText, valueText, previous, next);
            return selector;
        }

        private static GameObject CreateImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            CenterRect(imageObject.GetComponent<RectTransform>(), position, size);
            imageObject.GetComponent<Image>().color = color;
            imageObject.GetComponent<Image>().raycastTarget = false;
            return imageObject;
        }

        private static TMP_Text CreateText(string name, Transform parent, Vector2 position, Vector2 size,
            string value, int fontSize, Color color, TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            CenterRect(textObject.GetComponent<RectTransform>(), position, size);
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.font = Font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private enum FontSize { Small, Large }

        private static Button CreateButton(string name, Transform parent, Vector2 position,
            Vector2 size, string label, FontSize fontSize, Color color)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            CenterRect(buttonObject.GetComponent<RectTransform>(), position, size);
            Image image = buttonObject.GetComponent<Image>();
            image.color = color;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.20f);
            colors.disabledColor = new Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, 0.8f);
            button.colors = colors;
            CreateText("Label", buttonObject.transform, Vector2.zero, size - new Vector2(12f, 8f), label,
                fontSize == FontSize.Large ? 24 : 18, Hex("#FFF8E8"), TextAlignmentOptions.Center, FontStyles.Bold);
            return button;
        }

        private static Slider CreateSlider(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            CenterRect(root.GetComponent<RectTransform>(), position, size);
            GameObject background = CreateImage("Background", root.transform, Vector2.zero, size, Hex("#274F4B"));
            GameObject fill = CreateImage("Fill", root.transform, Vector2.zero, new Vector2(size.x * 0.7f, size.y), Hex("#D8A64E"));
            GameObject handle = CreateImage("Handle", root.transform, Vector2.zero, new Vector2(24f, 38f), Hex("#F4E8CA"));
            Slider slider = root.GetComponent<Slider>();
            background.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            background.GetComponent<RectTransform>().anchorMax = Vector2.one;
            background.GetComponent<RectTransform>().offsetMin = background.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            fill.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            fill.GetComponent<RectTransform>().anchorMax = Vector2.one;
            fill.GetComponent<RectTransform>().offsetMin = fill.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            handle.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.5f);
            handle.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0.5f);
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.value = 0.8f;
            return slider;
        }

        private static void CreateEventSystem(Scene scene)
        {
            var eventSystemObject = new GameObject("EventSystem");
            SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
            eventSystemObject.AddComponent<EventSystem>();
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private static void CenterRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out Color color);
            return color;
        }
    }
}
