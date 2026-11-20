#if UNITY_EDITOR
using BTLGame.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static BTLGame.EditorTools.UiKit;

namespace BTLGame.EditorTools
{
    /// <summary>
    /// Dựng ba màn cuối trận bằng UiKit.Popup (cùng khung với mọi popup): CHIẾN THẮNG (chỉ nút Tiếp tục),
    /// BẢNG XẾP HẠNG (Xem chi tiết / Chơi lại / Về menu) và CHI TIẾT từng người chơi. Mặc định ẩn; MatchResultScreen điều khiển.
    /// </summary>
    internal static class MatchResultScreenBuilder
    {
        private static readonly Color ConfettiA = new Color(1f, 0.45f, 0.55f);
        private static readonly Color ConfettiB = new Color(0.4f, 0.75f, 1f);
        private static readonly Color ConfettiC = new Color(1f, 0.85f, 0.3f);
        private static readonly Color ConfettiD = new Color(0.5f, 0.9f, 0.55f);

        internal static MatchResultScreen Build(Transform canvas)
        {
            var go = new GameObject("MatchResultScreen");
            go.transform.SetParent(canvas, false);
            var screen = go.AddComponent<MatchResultScreen>();
            var so = new SerializedObject(screen);

            BuildVictory(canvas, so);
            BuildBoard(canvas, so);
            BuildDetail(canvas, so);
            so.ApplyModifiedPropertiesWithoutUndo();
            return screen;
        }

        // ------------------------------------------------------------ CHIẾN THẮNG
        private static void BuildVictory(Transform canvas, SerializedObject so)
        {
            var parts = Popup(canvas, "Screen_Victory", "CHIẾN THẮNG!", new Vector2(1240f, 860f), Icon("crown_a"), false);
            var inner = parts.Inner;

            var crown = NewImage("Crown", inner, Icon("crown_a"), Gold, new Vector2(0f, 270f), new Vector2(190f, 190f));
            crown.raycastTarget = false;
            AddShadow(crown.gameObject);

            NewText("Label", inner, "NGƯỜI CHIẾN THẮNG", Bold, 34, TextDark, TextAnchor.MiddleCenter, new Vector2(0f, 150f), new Vector2(800f, 50f));
            var plate = NewImage("NamePlate", inner, BtnBlue, NavyDeep, new Vector2(0f, 62f), new Vector2(920f, 120f));
            plate.raycastTarget = false;
            var winner = NewText("Winner", plate.transform, "NGƯỜI CHƠI", Black, 60, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 4f), new Vector2(860f, 100f));
            winner.horizontalOverflow = HorizontalWrapMode.Overflow;
            winner.resizeTextForBestFit = true;
            winner.resizeTextMinSize = 28;
            winner.resizeTextMaxSize = 60;
            AddOutline(winner.gameObject, new Color(0.1f, 0.12f, 0.4f, 0.9f), 2f);

            var reason = NewText("Reason", inner, string.Empty, Bold, 30, new Color(0.55f, 0.32f, 0.02f), TextAnchor.MiddleCenter, new Vector2(0f, -22f), new Vector2(1000f, 46f));

            // Năm ô chỉ số: Tiền mặt, Tổng tài sản, Số thành phố, Bãi biển, Monopoly.
            string[] labels = { "Tiền mặt", "Tổng tài sản", "Thành phố", "Bãi biển", "Monopoly" };
            string[] icons = { "dollar", "structure_house", "structure_tower", "flag_triangle", "crown_a" };
            var stats = new Text[5];
            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * 215f;
                var tile = NewImage("Stat_" + i, inner, BtnGrey, Color.white, new Vector2(x, -170f), new Vector2(200f, 210f));
                tile.raycastTarget = false;
                var icon = NewImage("Icon", tile.transform, Icon(icons[i]), i == 4 ? Gold : NavyDeep, new Vector2(0f, 55f), new Vector2(66f, 66f));
                icon.raycastTarget = false;
                NewText("Caption", tile.transform, labels[i], Bold, 24, TextDark, TextAnchor.MiddleCenter, new Vector2(0f, -4f), new Vector2(190f, 36f));
                var value = NewText("Value", tile.transform, "0", Black, 30, new Color(0.15f, 0.15f, 0.4f), TextAnchor.MiddleCenter, new Vector2(0f, -56f), new Vector2(188f, 52f));
                value.horizontalOverflow = HorizontalWrapMode.Overflow;
                value.resizeTextForBestFit = true;
                value.resizeTextMinSize = 16;
                value.resizeTextMaxSize = 30;
                stats[i] = value;
            }

            var cont = PrimaryButton(inner, "Btn_Continue", "Tiếp tục", new Vector2(0f, -325f), new Vector2(460f, 100f), 44);

            // Hoa giấy phủ cả màn (nằm trong nền mờ, đặt sau cùng để rơi trên khung).
            var confetti = new RectTransform[28];
            Color[] palette = { ConfettiA, ConfettiB, ConfettiC, ConfettiD };
            for (int i = 0; i < confetti.Length; i++)
            {
                var piece = NewImage("Confetti" + i, parts.Root.transform, null, palette[i % palette.Length], Vector2.zero, new Vector2(16f, 26f));
                piece.raycastTarget = false;
                confetti[i] = piece.rectTransform;
            }

            so.FindProperty("_victory").objectReferenceValue = parts.Popup;
            so.FindProperty("_winnerName").objectReferenceValue = winner;
            so.FindProperty("_reason").objectReferenceValue = reason;
            var statsProp = so.FindProperty("_stats");
            statsProp.arraySize = 5;
            for (int i = 0; i < 5; i++) statsProp.GetArrayElementAtIndex(i).objectReferenceValue = stats[i];
            so.FindProperty("_continue").objectReferenceValue = cont;
            so.FindProperty("_crown").objectReferenceValue = crown.rectTransform;
            var confettiProp = so.FindProperty("_confetti");
            confettiProp.arraySize = confetti.Length;
            for (int i = 0; i < confetti.Length; i++) confettiProp.GetArrayElementAtIndex(i).objectReferenceValue = confetti[i];
        }

        // ------------------------------------------------------------ BẢNG XẾP HẠNG
        private static void BuildBoard(Transform canvas, SerializedObject so)
        {
            var parts = Popup(canvas, "Screen_Leaderboard", "BẢNG XẾP HẠNG", new Vector2(1560f, 900f), Icon("crown_a"), false);
            var inner = parts.Inner;

            // Cột: hạng, người chơi, tiền mặt, tổng tài sản, thành phố, bãi biển, trạng thái.
            float[] xs = { -650f, -430f, -140f, 110f, 300f, 420f, 580f };
            string[] heads = { "Hạng", "Người chơi", "Tiền mặt", "Tổng tài sản", "Thành phố", "Bãi biển", "Trạng thái" };
            for (int i = 0; i < heads.Length; i++)
            {
                var anchor = i == 1 ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
                float x = i == 1 ? xs[i] - 70f : xs[i];
                var head = NewText("Head_" + i, inner, heads[i], Bold, 26, TextDark, anchor, new Vector2(x, 310f), new Vector2(i == 1 ? 300f : 200f, 44f));
                head.raycastTarget = false;
            }

            var rows = new System.Collections.Generic.List<Image>();
            var rowProps = so.FindProperty("_rows");
            rowProps.arraySize = 4;
            for (int r = 0; r < 4; r++)
            {
                float y = 215f - r * 112f;
                var bg = NewImage("Row_" + r, inner, BtnGrey, Color.white, new Vector2(0f, y), new Vector2(1400f, 100f));
                bg.raycastTarget = false;
                var rank = NewText("Rank", bg.transform, "1", Black, 48, new Color(0.45f, 0.17f, 0.02f), TextAnchor.MiddleCenter, new Vector2(xs[0], 3f), new Vector2(110f, 80f));
                var dot = NewImage("Dot", bg.transform, RoundGrey, Color.white, new Vector2(xs[1] - 100f, 3f), new Vector2(54f, 54f));
                dot.raycastTarget = false;
                var name = NewText("Name", bg.transform, "Người chơi", Bold, 32, TextDark, TextAnchor.MiddleLeft, new Vector2(xs[1] + 20f, 3f), new Vector2(240f, 60f));
                var cash = NewText("Cash", bg.transform, "0", Bold, 28, TextDark, TextAnchor.MiddleCenter, new Vector2(xs[2], 3f), new Vector2(250f, 60f));
                var assets = NewText("Assets", bg.transform, "0", Bold, 28, TextDark, TextAnchor.MiddleCenter, new Vector2(xs[3], 3f), new Vector2(250f, 60f));
                var cities = NewText("Cities", bg.transform, "0", Bold, 32, TextDark, TextAnchor.MiddleCenter, new Vector2(xs[4], 3f), new Vector2(100f, 60f));
                var beaches = NewText("Beaches", bg.transform, "0", Bold, 32, TextDark, TextAnchor.MiddleCenter, new Vector2(xs[5], 3f), new Vector2(100f, 60f));
                var status = NewText("Status", bg.transform, string.Empty, Bold, 24, TextDark, TextAnchor.MiddleCenter, new Vector2(xs[6], 3f), new Vector2(210f, 60f));
                foreach (var t in new[] { rank, name, cash, assets, cities, beaches, status })
                {
                    t.raycastTarget = false;
                    t.horizontalOverflow = HorizontalWrapMode.Overflow;
                    t.resizeTextForBestFit = true;
                    t.resizeTextMinSize = 14;
                    t.resizeTextMaxSize = t.fontSize;
                }

                var row = rowProps.GetArrayElementAtIndex(r);
                row.FindPropertyRelative("Root").objectReferenceValue = bg.gameObject;
                row.FindPropertyRelative("Background").objectReferenceValue = bg;
                row.FindPropertyRelative("Rank").objectReferenceValue = rank;
                row.FindPropertyRelative("Name").objectReferenceValue = name;
                row.FindPropertyRelative("Cash").objectReferenceValue = cash;
                row.FindPropertyRelative("Assets").objectReferenceValue = assets;
                row.FindPropertyRelative("Cities").objectReferenceValue = cities;
                row.FindPropertyRelative("Beaches").objectReferenceValue = beaches;
                row.FindPropertyRelative("Status").objectReferenceValue = status;
                row.FindPropertyRelative("Dot").objectReferenceValue = dot;
            }

            var note = NewText("Note", inner, string.Empty, Regular, 22, TextDark, TextAnchor.MiddleCenter, new Vector2(0f, -250f), new Vector2(1380f, 36f));
            note.resizeTextForBestFit = true;
            note.resizeTextMinSize = 14;
            note.resizeTextMaxSize = 22;

            var detail = SecondaryButton(inner, "Btn_Detail", "Xem chi tiết", new Vector2(-450f, -320f), new Vector2(420f, 96f), 34);
            var restart = PrimaryButton(inner, "Btn_Restart", "Chơi lại", new Vector2(0f, -320f), new Vector2(420f, 96f), 38);
            var menu = SecondaryButton(inner, "Btn_Menu", "Về menu", new Vector2(450f, -320f), new Vector2(420f, 96f), 34, Icon("structure_house"));

            so.FindProperty("_board").objectReferenceValue = parts.Popup;
            so.FindProperty("_detailButton").objectReferenceValue = detail;
            so.FindProperty("_restartButton").objectReferenceValue = restart;
            so.FindProperty("_menuButton").objectReferenceValue = menu;
            so.FindProperty("_boardNote").objectReferenceValue = note;
        }

        // ------------------------------------------------------------ CHI TIẾT
        private static void BuildDetail(Transform canvas, SerializedObject so)
        {
            var parts = Popup(canvas, "Screen_ResultDetail", "Chi tiết kết quả", new Vector2(1300f, 880f));
            var inner = parts.Inner;

            var tabs = so.FindProperty("_tabs");
            var labels = so.FindProperty("_tabLabels");
            tabs.arraySize = 4;
            labels.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                var tab = SecondaryButton(inner, "Tab_" + i, "#" + (i + 1), new Vector2((i - 1.5f) * 290f, 300f), new Vector2(276f, 80f), 26);
                tabs.GetArrayElementAtIndex(i).objectReferenceValue = tab;
                labels.GetArrayElementAtIndex(i).objectReferenceValue = tab.GetComponentInChildren<Text>();
            }

            var summary = NewText("Summary", inner, string.Empty, Regular, 30, TextDark, TextAnchor.UpperLeft, new Vector2(-300f, -30f), new Vector2(520f, 560f));
            summary.horizontalOverflow = HorizontalWrapMode.Wrap;
            summary.verticalOverflow = VerticalWrapMode.Truncate;
            summary.resizeTextForBestFit = true;
            summary.resizeTextMinSize = 16;
            summary.resizeTextMaxSize = 30;

            NewText("PropHead", inner, "Bất động sản", Bold, 30, TextDark, TextAnchor.MiddleLeft, new Vector2(150f, 205f), new Vector2(520f, 44f));
            var props = NewText("Properties", inner, string.Empty, Regular, 26, TextDark, TextAnchor.UpperLeft, new Vector2(150f, -60f), new Vector2(620f, 520f));
            props.horizontalOverflow = HorizontalWrapMode.Wrap;
            props.verticalOverflow = VerticalWrapMode.Truncate;
            props.resizeTextForBestFit = true;
            props.resizeTextMinSize = 14;
            props.resizeTextMaxSize = 26;

            so.FindProperty("_detail").objectReferenceValue = parts.Popup;
            so.FindProperty("_summaryText").objectReferenceValue = summary;
            so.FindProperty("_propertyText").objectReferenceValue = props;
        }
    }
}
#endif
