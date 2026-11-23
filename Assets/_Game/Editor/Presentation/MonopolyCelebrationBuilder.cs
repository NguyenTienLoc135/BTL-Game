#if UNITY_EDITOR
using BTLGame.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static BTLGame.EditorTools.UiKit;

namespace BTLGame.EditorTools
{
    /// <summary>
    /// Dựng màn chúc mừng MONOPOLY (độc quyền một quốc gia): tiêu đề vàng + vương miện, thẻ thành phố, dải thông báo, xu, tia sáng, xúc xắc.
    /// Mặc định ẩn; MonopolyWatcher chiếu 6 giây rồi tự tắt. Không chặn thao tác (CanvasGroup không nhận raycast).
    /// </summary>
    internal static class MonopolyCelebrationBuilder
    {
        internal static MonopolyCelebration Build(Transform canvas)
        {
            var root = NewRect("MonopolyCelebration", canvas, Vector2.zero, Vector2.zero);
            Stretch(root);
            var celebration = root.gameObject.AddComponent<MonopolyCelebration>();

            var content = NewRect("Content", root, Vector2.zero, Vector2.zero);
            Stretch(content);
            var group = content.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var dim = NewImage("Dim", content, null, new Color(0.02f, 0.03f, 0.12f, 0.6f), Vector2.zero, Vector2.zero);
            Stretch(dim.rectTransform);
            dim.raycastTarget = false;

            var panel = NewRect("Panel", content, Vector2.zero, new Vector2(1500f, 900f));

            var crown = NewImage("Crown", panel, Icon("crown_a"), Gold, new Vector2(0f, 345f), new Vector2(220f, 220f));
            crown.raycastTarget = false;
            AddShadow(crown.gameObject);

            var titleText = NewText("Title", panel, "MONOPOLY", Black, 200, Gold, TextAnchor.MiddleCenter, new Vector2(0f, 195f), new Vector2(1400f, 250f));
            titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            AddOutline(titleText.gameObject, GoldOutline, 9f);

            // Thẻ thành phố (tối đa 3; quốc gia 2 thành phố chỉ dùng 2 thẻ, MonopolyCelebration tự căn giữa).
            var cards = new CardRefs[3];
            string[] icons = { "structure_tower", "structure_house", "structure_tower" };
            for (int i = 0; i < 3; i++)
            {
                cards[i] = NewCard(panel, i, new Vector2((i - 1) * 340f, -35f), icons[i]);
            }

            // Dải thông báo.
            var ribbon = NewImage("Ribbon", panel, BtnBlue, NavyDeep, new Vector2(0f, -285f), new Vector2(1200f, 180f));
            ribbon.raycastTarget = false;
            var main = NewText("Main", ribbon.transform, string.Empty, Black, 44, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 34f), new Vector2(1120f, 64f));
            Fit(main, 22, 44);
            var sub = NewText("Sub", ribbon.transform, "Tiền thuê tăng thêm 20%!", Black, 50, Gold, TextAnchor.MiddleCenter, new Vector2(0f, -38f), new Vector2(900f, 70f));
            Fit(sub, 26, 50);

            var dice = NewImage("Dice", panel, Icon("dice_3D_detailed"), Color.white, new Vector2(660f, -90f), new Vector2(190f, 190f));
            dice.raycastTarget = false;
            AddShadow(dice.gameObject);

            var coins = Decor(panel);
            Wire(celebration, content, group, panel, crown, titleText, cards, ribbon, main, sub, dice, coins.Item1, coins.Item2);

            content.gameObject.SetActive(false);
            return celebration;
        }

        /// <summary>Xu bay quanh + tia sáng lấp lánh (trang trí).</summary>
        private static (RectTransform[], RectTransform[]) Decor(Transform panel)
        {
            Vector2[] coinPos =
            {
                new Vector2(-660f, 150f), new Vector2(-720f, -120f), new Vector2(640f, 170f), new Vector2(720f, -250f),
                new Vector2(-520f, -380f), new Vector2(520f, -400f), new Vector2(-250f, 430f)
            };
            var coins = new RectTransform[coinPos.Length];
            for (int i = 0; i < coinPos.Length; i++)
            {
                var coin = NewImage("Coin" + i, panel, Icon("dollar"), Gold, coinPos[i], new Vector2(100f, 100f));
                coin.raycastTarget = false;
                AddShadow(coin.gameObject);
                coins[i] = coin.rectTransform;
            }

            Vector2[] sparkPos =
            {
                new Vector2(-560f, 330f), new Vector2(560f, 320f), new Vector2(-690f, 20f), new Vector2(700f, 60f), new Vector2(-380f, 60f),
                new Vector2(400f, 90f), new Vector2(-120f, 420f), new Vector2(150f, 440f), new Vector2(-640f, -300f), new Vector2(640f, -330f)
            };
            var sparkles = new RectTransform[sparkPos.Length];
            for (int i = 0; i < sparkPos.Length; i++)
            {
                var color = i % 3 == 0 ? Color.white : new Color(1f, 0.9f, 0.45f);
                var sp = NewImage("Sparkle" + i, panel, IconSparkle, color, sparkPos[i], Vector2.one * (46f + (i % 4) * 14f));
                sp.raycastTarget = false;
                sparkles[i] = sp.rectTransform;
            }

            return (coins, sparkles);
        }

        private sealed class CardRefs
        {
            public RectTransform Root;
            public Image Header;
            public Text Name;
        }

        private static CardRefs NewCard(Transform parent, int index, Vector2 pos, string icon)
        {
            var r = new CardRefs();
            var card = NewImage("Card_" + index, parent, BtnGrey, Cream, pos, new Vector2(300f, 360f));
            card.raycastTarget = false;
            r.Root = card.rectTransform;

            r.Header = NewImage("Header", card.transform, BtnGrey, Color.white, new Vector2(0f, 135f), new Vector2(270f, 64f));
            r.Header.raycastTarget = false;
            var body = NewImage("Building", card.transform, Icon(icon), NavyDeep, new Vector2(0f, 8f), new Vector2(160f, 160f));
            body.raycastTarget = false;
            r.Name = NewText("Name", card.transform, "Thành phố", Black, 34, TextDark, TextAnchor.MiddleCenter, new Vector2(0f, -128f), new Vector2(270f, 56f));
            Fit(r.Name, 18, 34);
            return r;
        }

        private static void Fit(Text text, int min, int max)
        {
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = min;
            text.resizeTextMaxSize = max;
        }

        private static void Wire(MonopolyCelebration celebration, Transform content, CanvasGroup group, Transform panel, Image crown, Text title,
            CardRefs[] cards, Image ribbon, Text main, Text sub, Image dice, RectTransform[] coins, RectTransform[] sparkles)
        {
            var so = new SerializedObject(celebration);
            so.FindProperty("_content").objectReferenceValue = content.gameObject;
            so.FindProperty("_group").objectReferenceValue = group;
            so.FindProperty("_panel").objectReferenceValue = panel;
            so.FindProperty("_crown").objectReferenceValue = crown.rectTransform;
            so.FindProperty("_title").objectReferenceValue = title.rectTransform;
            var cardsProp = so.FindProperty("_cards");
            cardsProp.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                var p = cardsProp.GetArrayElementAtIndex(i);
                p.FindPropertyRelative("Root").objectReferenceValue = cards[i].Root;
                p.FindPropertyRelative("Header").objectReferenceValue = cards[i].Header;
                p.FindPropertyRelative("Name").objectReferenceValue = cards[i].Name;
            }

            so.FindProperty("_ribbon").objectReferenceValue = ribbon.rectTransform;
            so.FindProperty("_ribbonMain").objectReferenceValue = main;
            so.FindProperty("_ribbonSub").objectReferenceValue = sub;
            so.FindProperty("_dice").objectReferenceValue = dice.rectTransform;
            var coinsProp = so.FindProperty("_coins");
            coinsProp.arraySize = coins.Length;
            for (int i = 0; i < coins.Length; i++) coinsProp.GetArrayElementAtIndex(i).objectReferenceValue = coins[i];
            var sparkProp = so.FindProperty("_sparkles");
            sparkProp.arraySize = sparkles.Length;
            for (int i = 0; i < sparkles.Length; i++) sparkProp.GetArrayElementAtIndex(i).objectReferenceValue = sparkles[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
