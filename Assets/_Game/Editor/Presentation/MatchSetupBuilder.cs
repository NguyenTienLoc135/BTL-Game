#if UNITY_EDITOR
using BTLGame.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static BTLGame.EditorTools.UiKit;

namespace BTLGame.EditorTools
{
    /// <summary>
    /// Dựng màn thiết lập trận cho hai chế độ (Chơi với Bot / Chơi với người) bằng UiKit.Popup.
    /// Nội dung bám docs (GAME_RULES §1, §11, Q03): số người 2–4, thời gian 30/45/60/Vô hạn, mỗi người chọn một
    /// trong 6 nghề (được trùng), hiển thị Passive/Active/Ultimate/Weakness. KHÔNG có: độ khó Bot, tên/màu tự chọn,
    /// công tắc hướng dẫn/âm thanh (âm thanh nằm ở popup Cài đặt) vì docs không quy định.
    /// </summary>
    internal static class MatchSetupBuilder
    {
        private static readonly string[] RoleIconNames =
        {
            "dollar",          // Nhà Đầu Tư
            "structure_house", // Doanh Nhân
            "structure_tower", // Kỹ Sư
            "book_closed",     // Luật Sư
            "pouch",           // Banker
            "chess_knight"     // Nhà Chiến Lược
        };

        internal struct Result
        {
            public UiPopup Popup;
            public MatchSetupView View;
        }

        internal static Result Build(Transform canvas, MainMenuMode mode)
        {
            bool bot = mode == MainMenuMode.VsBot;
            string title = bot ? "Thiết lập trận đấu - Chơi với Bot" : "Thiết lập trận đấu - Chơi với người";
            var parts = Popup(canvas, bot ? "Popup_SetupBot" : "Popup_SetupHuman", title, new Vector2(1560f, 940f));
            var inner = parts.Inner;

            // --- Số người + thời gian
            NewText("Label_Players", inner, bot ? "Tổng số người (gồm Bạn + Bot)" : "Số người chơi", Bold, 30, TextDark, TextAnchor.MiddleLeft, new Vector2(-450f, 365f), new Vector2(520f, 50f));
            var countSel = Segmented(inner, "Selector_Players", new[] { "2 người", "3 người", "4 người" }, 200f, 215f, new Vector2(150f, 365f));

            NewText("Label_Duration", inner, "Thời gian trận đấu", Bold, 30, TextDark, TextAnchor.MiddleLeft, new Vector2(-450f, 285f), new Vector2(520f, 50f));
            var durationSel = Segmented(inner, "Selector_Duration", new[] { "30 phút", "45 phút", "60 phút", "Vô hạn" }, 170f, 185f, new Vector2(150f, 285f));

            NewText("Note_Rules", inner, "Người đi trước được chọn ngẫu nhiên, sau đó theo thứ tự ghế cố định. Được chọn trùng nghề.", Regular, 24, TextDark, TextAnchor.MiddleCenter, new Vector2(0f, 225f), new Vector2(1400f, 40f));

            // --- Thẻ người chơi (4 ghế; số thẻ hiển thị theo số người)
            var cards = new CardRefs[4];
            var seatColors = SeatColors;
            for (int i = 0; i < 4; i++)
            {
                cards[i] = PlayerCard(inner, i, seatColors[i]);
            }

            // --- Chi tiết nghề của người đang chọn
            var detail = NewImage("RoleDetail", inner, BtnBlue, NavyDeep, new Vector2(0f, -200f), new Vector2(1440f, 270f));
            var detailTitle = NewText("DetailTitle", detail.transform, string.Empty, Black, 28, Gold, TextAnchor.MiddleLeft, new Vector2(-30f, 105f), new Vector2(1340f, 44f));
            AddOutline(detailTitle.gameObject, GoldOutline, 1.5f);
            var passive = DetailColumn(detail.transform, "Passive (nội tại)", -540f);
            var active = DetailColumn(detail.transform, "Active (chủ động)", -180f);
            var ultimate = DetailColumn(detail.transform, "Ultimate (1 lần/trận)", 180f);
            var weakness = DetailColumn(detail.transform, "Weakness (điểm yếu)", 540f);

            // --- Nút dưới
            var back = SecondaryButton(inner, "Btn_Back", "Quay lại", new Vector2(-230f, -378f), new Vector2(340f, 84f), 32);
            var start = PrimaryButton(inner, "Btn_StartMatch", "Bắt đầu trận", new Vector2(230f, -378f), new Vector2(460f, 92f), 38, Icon("dice"));

            // --- Gắn field cho MatchSetupView
            var view = parts.Root.AddComponent<MatchSetupView>();
            var so = new SerializedObject(view);
            so.FindProperty("_mode").enumValueIndex = (int)mode;
            so.FindProperty("_defaultPlayerIndex").intValue = bot ? 2 : 0;
            so.FindProperty("_defaultDurationIndex").intValue = bot ? 2 : 0;
            so.FindProperty("_playerCountSelector").objectReferenceValue = countSel;
            so.FindProperty("_durationSelector").objectReferenceValue = durationSel;

            var cardsProp = so.FindProperty("_cards");
            cardsProp.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                var p = cardsProp.GetArrayElementAtIndex(i);
                p.FindPropertyRelative("Root").objectReferenceValue = cards[i].Root;
                p.FindPropertyRelative("Focus").objectReferenceValue = cards[i].Focus;
                p.FindPropertyRelative("SeatBar").objectReferenceValue = cards[i].SeatBar;
                p.FindPropertyRelative("SeatName").objectReferenceValue = cards[i].SeatName;
                p.FindPropertyRelative("RoleIcon").objectReferenceValue = cards[i].RoleIcon;
                p.FindPropertyRelative("RoleName").objectReferenceValue = cards[i].RoleName;
                p.FindPropertyRelative("Prev").objectReferenceValue = cards[i].Prev;
                p.FindPropertyRelative("Next").objectReferenceValue = cards[i].Next;
                p.FindPropertyRelative("Select").objectReferenceValue = cards[i].Select;
            }

            var iconsProp = so.FindProperty("_roleIcons");
            iconsProp.arraySize = RoleIconNames.Length;
            for (int i = 0; i < RoleIconNames.Length; i++)
            {
                iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = Icon(RoleIconNames[i]);
            }

            var colorsProp = so.FindProperty("_seatColors");
            colorsProp.arraySize = seatColors.Length;
            for (int i = 0; i < seatColors.Length; i++)
            {
                colorsProp.GetArrayElementAtIndex(i).colorValue = seatColors[i];
            }

            so.FindProperty("_detailTitle").objectReferenceValue = detailTitle;
            so.FindProperty("_passiveText").objectReferenceValue = passive;
            so.FindProperty("_activeText").objectReferenceValue = active;
            so.FindProperty("_ultimateText").objectReferenceValue = ultimate;
            so.FindProperty("_weaknessText").objectReferenceValue = weakness;
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.FindProperty("_startButton").objectReferenceValue = start;
            so.ApplyModifiedPropertiesWithoutUndo();

            return new Result { Popup = parts.Popup, View = view };
        }

        // ---------------------------------------------------------------- thành phần

        private static SegmentedSelector Segmented(Transform parent, string name, string[] labels, float segWidth, float spacing, Vector2 pos)
        {
            var root = NewRect(name, parent, pos, new Vector2(spacing * labels.Length, 64f));
            var selector = root.gameObject.AddComponent<SegmentedSelector>();

            var so = new SerializedObject(selector);
            var segs = so.FindProperty("_segments");
            segs.arraySize = labels.Length;
            for (int i = 0; i < labels.Length; i++)
            {
                float x = (i - (labels.Length - 1) / 2f) * spacing;
                var bg = NewImage("Seg_" + i, root, BtnBlue, Navy, new Vector2(x, 0f), new Vector2(segWidth, 64f));
                var button = bg.gameObject.AddComponent<Button>();
                button.targetGraphic = bg;
                StyleButton(button);
                var label = NewText("Label", bg.transform, labels[i], Bold, 26, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 3f), new Vector2(segWidth - 10f, 50f));

                var p = segs.GetArrayElementAtIndex(i);
                p.FindPropertyRelative("Button").objectReferenceValue = button;
                p.FindPropertyRelative("Background").objectReferenceValue = bg;
                p.FindPropertyRelative("Label").objectReferenceValue = label;
            }

            so.FindProperty("_selectedSprite").objectReferenceValue = BtnYellow;
            so.FindProperty("_normalSprite").objectReferenceValue = BtnBlue;
            so.FindProperty("_selectedTint").colorValue = Color.white;
            so.FindProperty("_normalTint").colorValue = Navy;
            so.ApplyModifiedPropertiesWithoutUndo();
            return selector;
        }

        private sealed class CardRefs
        {
            public GameObject Root, Focus;
            public Image SeatBar, RoleIcon;
            public Text SeatName, RoleName;
            public Button Prev, Next, Select;
        }

        private static CardRefs PlayerCard(Transform parent, int seat, Color seatColor)
        {
            var r = new CardRefs();
            var bg = NewImage("Card_" + seat, parent, BtnBlue, Navy, new Vector2(0f, 70f), new Vector2(340f, 250f));
            r.Root = bg.gameObject;
            r.Select = bg.gameObject.AddComponent<Button>();
            r.Select.targetGraphic = bg;
            StyleButton(r.Select);

            // Viền vàng khi được chọn: lớp vàng nằm dưới thân kem nhỏ hơn.
            var focus = NewImage("Focus", bg.transform, BtnYellow, Color.white, Vector2.zero, new Vector2(340f, 250f));
            focus.raycastTarget = false;
            r.Focus = focus.gameObject;
            var body = NewImage("Body", bg.transform, BtnGrey, Cream, Vector2.zero, new Vector2(316f, 226f));
            body.raycastTarget = false;

            r.SeatBar = NewImage("SeatBar", body.transform, BtnGrey, seatColor, new Vector2(0f, 82f), new Vector2(296f, 52f));
            r.SeatBar.raycastTarget = false;
            r.SeatName = NewText("SeatName", r.SeatBar.transform, "Người chơi", Bold, 26, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 3f), new Vector2(280f, 44f));
            AddOutline(r.SeatName.gameObject, TextOutline, 2f);

            var circle = NewImage("IconCircle", body.transform, RoundGrey, NavyDeep, new Vector2(0f, 2f), new Vector2(100f, 100f));
            circle.raycastTarget = false;
            r.RoleIcon = NewImage("RoleIcon", circle.transform, null, Color.white, new Vector2(0f, 2f), new Vector2(58f, 58f));
            r.RoleIcon.raycastTarget = false;
            AddShadow(r.RoleIcon.gameObject);

            r.RoleName = NewText("RoleName", body.transform, "Nghề", Black, 28, TextDark, TextAnchor.MiddleCenter, new Vector2(0f, -66f), new Vector2(190f, 44f));

            r.Prev = IconButton(body.transform, "Btn_Prev", Icon("arrow_right"), new Vector2(-120f, -66f), new Vector2(54f, 54f));
            ((RectTransform)r.Prev.transform.Find("Icon")).localRotation = Quaternion.Euler(0f, 0f, 180f);
            r.Next = IconButton(body.transform, "Btn_Next", Icon("arrow_right"), new Vector2(120f, -66f), new Vector2(54f, 54f));
            return r;
        }

        private static Text DetailColumn(Transform parent, string header, float x)
        {
            var h = NewText("Header", parent, header, Black, 22, Gold, TextAnchor.MiddleLeft, new Vector2(x, 55f), new Vector2(330f, 36f));
            AddOutline(h.gameObject, GoldOutline, 1.2f);
            var body = NewText("Body", parent, string.Empty, Regular, 20, Color.white, TextAnchor.UpperLeft, new Vector2(x, -55f), new Vector2(330f, 160f));
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 14;
            body.resizeTextMaxSize = 20;
            return body;
        }
    }
}
#endif
