#if UNITY_EDITOR
using BTLGame.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static BTLGame.EditorTools.UiKit;

namespace BTLGame.EditorTools
{
    /// <summary>
    /// Dựng màn "KẾT QUẢ TUNG XÚC XẮC": dải tiêu đề, hai xúc xắc lớn ở giữa, phép cộng a + b = tổng với vòng sao, tia sáng.
    /// Mặc định ẩn; DiceResultOverlay chiếu 5 giây sau mỗi lần tung rồi tự tắt. Không chặn thao tác (CanvasGroup không nhận raycast).
    /// </summary>
    internal static class DiceResultOverlayBuilder
    {

        private const float BigDieSize = 150f;

        private static Image NewBigDie(string name, Transform parent, Vector2 pos)
        {
            var root = NewRect(name, parent, pos, new Vector2(BigDieSize, BigDieSize));
            var backing = NewImage("Backing", root, BtnBlue, new Color(0.18f, 0.32f, 0.85f), Vector2.zero, Vector2.one * (BigDieSize * 0.8f));
            backing.raycastTarget = false;
            var face = NewImage("Face", root, Icon("dice_3"), Color.white, Vector2.zero, new Vector2(BigDieSize, BigDieSize));
            face.raycastTarget = false;
            AddShadow(face.gameObject);
            return face;
        }
        internal static DiceResultOverlay Build(Transform canvas)
        {
            var root = NewRect("DiceResultOverlay", canvas, Vector2.zero, Vector2.zero);
            Stretch(root);
            var overlay = root.gameObject.AddComponent<DiceResultOverlay>();

            var content = NewRect("Content", root, Vector2.zero, Vector2.zero);
            Stretch(content);
            var group = content.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            // Dải tiêu đề.
            var banner = NewImage("Banner", content, BtnBlue, NavyDeep, new Vector2(0f, 300f), new Vector2(860f, 100f));
            banner.raycastTarget = false;
            var title = NewText("Title", banner.transform, "KẾT QUẢ TUNG XÚC XẮC", Black, 42, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 3f), new Vector2(700f, 70f));
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            AddOutline(title.gameObject, new Color(0.1f, 0.12f, 0.4f, 0.9f), 2f);
            foreach (float x in new[] { -390f, 390f })
            {
                var star = NewImage("Star", banner.transform, IconSparkle, Gold, new Vector2(x, 3f), new Vector2(44f, 44f));
                star.raycastTarget = false;
            }

            // Phép cộng a + b = tổng.
            var formula = NewImage("Formula", content, BtnBlue, new Color(0.38f, 0.5f, 0.9f), new Vector2(0f, 175f), new Vector2(720f, 130f));
            formula.raycastTarget = false;
            var faceA = NewImage("DieA", formula.transform, Icon("dice_3"), Color.white, new Vector2(-250f, 0f), new Vector2(86f, 86f));
            var plus = NewText("Plus", formula.transform, "+", Black, 64, Color.white, TextAnchor.MiddleCenter, new Vector2(-155f, 3f), new Vector2(70f, 80f));
            var faceB = NewImage("DieB", formula.transform, Icon("dice_4"), Color.white, new Vector2(-60f, 0f), new Vector2(86f, 86f));
            var equals = NewText("Equals", formula.transform, "=", Black, 64, Color.white, TextAnchor.MiddleCenter, new Vector2(35f, 3f), new Vector2(70f, 80f));
            faceA.raycastTarget = false;
            faceB.raycastTarget = false;
            plus.raycastTarget = false;
            equals.raycastTarget = false;
            AddShadow(faceA.gameObject);
            AddShadow(faceB.gameObject);

            var burst = NewImage("SumBurst", formula.transform, IconBurst, Gold, new Vector2(215f, 0f), new Vector2(170f, 170f));
            burst.raycastTarget = false;
            AddShadow(burst.gameObject);
            var sum = NewText("Sum", burst.transform, "7", Black, 76, new Color(0.45f, 0.17f, 0.02f), TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150f, 110f));
            sum.horizontalOverflow = HorizontalWrapMode.Overflow;
            sum.raycastTarget = false;

            // Hai xúc xắc lớn giữa bàn: khung chứa + nền xanh (để chấm hiện màu xanh thay vì lộ nền) + mặt xúc xắc.
            var bigA = NewBigDie("BigDieA", content, new Vector2(-110f, -80f));
            var bigB = NewBigDie("BigDieB", content, new Vector2(110f, -80f));

            // Tia sáng quanh phép cộng và xúc xắc.
            Vector2[] sparkPos =
            {
                new Vector2(-420f, 250f), new Vector2(430f, 250f), new Vector2(-330f, 60f), new Vector2(340f, 70f),
                new Vector2(-300f, -230f), new Vector2(300f, -220f), new Vector2(0f, 20f), new Vector2(120f, 290f)
            };
            var sparkles = new RectTransform[sparkPos.Length];
            for (int i = 0; i < sparkPos.Length; i++)
            {
                var color = i % 3 == 0 ? Color.white : new Color(1f, 0.9f, 0.45f);
                var sp = NewImage("Sparkle" + i, content, IconSparkle, color, sparkPos[i], Vector2.one * (40f + (i % 3) * 14f));
                sp.raycastTarget = false;
                sparkles[i] = sp.rectTransform;
            }

            var so = new SerializedObject(overlay);
            so.FindProperty("_content").objectReferenceValue = content.gameObject;
            so.FindProperty("_group").objectReferenceValue = group;
            so.FindProperty("_banner").objectReferenceValue = banner.rectTransform;
            so.FindProperty("_formula").objectReferenceValue = formula.rectTransform;
            so.FindProperty("_formulaA").objectReferenceValue = faceA;
            so.FindProperty("_formulaB").objectReferenceValue = faceB;
            so.FindProperty("_sumBurst").objectReferenceValue = burst.rectTransform;
            so.FindProperty("_sumText").objectReferenceValue = sum;
            so.FindProperty("_bigA").objectReferenceValue = bigA;
            so.FindProperty("_bigB").objectReferenceValue = bigB;
            var faces = so.FindProperty("_faces");
            faces.arraySize = 6;
            for (int i = 0; i < 6; i++) faces.GetArrayElementAtIndex(i).objectReferenceValue = Icon("dice_" + (i + 1));
            var sparkProp = so.FindProperty("_sparkles");
            sparkProp.arraySize = sparkles.Length;
            for (int i = 0; i < sparkles.Length; i++) sparkProp.GetArrayElementAtIndex(i).objectReferenceValue = sparkles[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            content.gameObject.SetActive(false);
            return overlay;
        }
    }
}
#endif
