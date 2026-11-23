#if UNITY_EDITOR
using BTLGame.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static BTLGame.EditorTools.UiKit;

namespace BTLGame.EditorTools
{
    /// <summary>
    /// Dựng popup "Cài đặt âm thanh" bằng UiKit.Popup (cùng khung với mọi popup khác).
    /// Phạm vi theo GAME_RULES: chỉ âm thanh — không có ngôn ngữ/đồ họa/rung/điều khiển.
    /// Dùng được cho cả MainMenu và menu tạm dừng trong Gameplay.
    /// </summary>
    internal static class SettingsPanelBuilder
    {
        private static readonly Vector4 TrackBorder = new Vector4(16f, 0f, 16f, 0f);

        internal static UiPopup Build(Transform canvas)
        {
            var trackGrey = ImportSprite(UiPack + "/Blue/slide_horizontal_grey.png", "slide_track", TrackBorder);
            var trackColor = ImportSprite(UiPack + "/Blue/slide_horizontal_color.png", "slide_fill", TrackBorder);
            var handle = ImportSprite(UiPack + "/Blue/slide_hangle.png", "slide_handle", Vector4.zero);
            var divider = ImportSprite(UiPack + "/Extra/divider.png", "divider", Vector4.zero);

            var parts = Popup(canvas, "Popup_Settings", "Cài đặt", new Vector2(1100f, 720f), IconGear);
            var inner = parts.Inner;

            // Các hàng cài đặt (toạ độ tương đối bảng kem).
            var master = Row(inner, "Âm lượng chung", 210f, trackGrey, trackColor, handle, out var masterValue);
            var music = Row(inner, "Nhạc nền", 90f, trackGrey, trackColor, handle, out var musicValue);
            var sfx = Row(inner, "Hiệu ứng âm thanh", -30f, trackGrey, trackColor, handle, out var sfxValue);
            NewText("Label_Music", inner, "Bật nhạc", Bold, 34, TextDark, TextAnchor.MiddleLeft, new Vector2(-390f, -150f), new Vector2(200f, 70f));
            var musicToggle = NewSwitch(inner, new Vector2(-190f, -150f), out var musicOn, out var musicOff);
            NewText("Label_Sfx", inner, "Bật hiệu ứng", Bold, 34, TextDark, TextAnchor.MiddleLeft, new Vector2(45f, -150f), new Vector2(260f, 70f));
            var sfxToggle = NewSwitch(inner, new Vector2(285f, -150f), out var sfxOn, out var sfxOff);

            foreach (float y in new[] { 150f, 30f, -90f })
            {
                NewImage("Divider", inner, divider, new Color(0.65f, 0.6f, 0.85f, 0.8f), new Vector2(0f, y), new Vector2(900f, 6f)).raycastTarget = false;
            }

            // Hai nút dưới cùng: Khôi phục mặc định (phụ) + Lưu (chính). Chỉ Lưu mới ghi cài đặt.
            var reset = SecondaryButton(inner, "Btn_Reset", "Khôi phục mặc định", new Vector2(-230f, -250f), new Vector2(420f, 84f), 28);
            var save = PrimaryButton(inner, "Btn_Save", "Lưu", new Vector2(230f, -250f), new Vector2(360f, 90f), 38);

            // Nhắc khi đang nghe thử mà chưa Lưu (đóng popup sẽ hoàn về giá trị đã lưu).
            var unsaved = NewText("Hint_Unsaved", inner, "● Chưa lưu", Bold, 22, new Color(0.8f, 0.4f, 0.05f), TextAnchor.MiddleCenter, new Vector2(440f, -150f), new Vector2(130f, 40f));
            unsaved.gameObject.SetActive(false);

            var panel = parts.Root.AddComponent<SettingsPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_masterSlider").objectReferenceValue = master;
            so.FindProperty("_musicSlider").objectReferenceValue = music;
            so.FindProperty("_sfxSlider").objectReferenceValue = sfx;
            so.FindProperty("_masterValue").objectReferenceValue = masterValue;
            so.FindProperty("_musicValue").objectReferenceValue = musicValue;
            so.FindProperty("_sfxValue").objectReferenceValue = sfxValue;
            so.FindProperty("_musicToggle").objectReferenceValue = musicToggle;
            so.FindProperty("_musicSwitchOn").objectReferenceValue = musicOn;
            so.FindProperty("_musicSwitchOff").objectReferenceValue = musicOff;
            so.FindProperty("_sfxToggle").objectReferenceValue = sfxToggle;
            so.FindProperty("_sfxSwitchOn").objectReferenceValue = sfxOn;
            so.FindProperty("_sfxSwitchOff").objectReferenceValue = sfxOff;
            so.FindProperty("_resetButton").objectReferenceValue = reset;
            so.FindProperty("_saveButton").objectReferenceValue = save;
            so.FindProperty("_unsavedHint").objectReferenceValue = unsaved.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();

            return parts.Popup;
        }

        private static Slider Row(Transform parent, string label, float y, Sprite track, Sprite fillSprite, Sprite handle, out Text valueText)
        {
            NewText("Label_" + label, parent, label, Bold, 34, TextDark, TextAnchor.MiddleLeft, new Vector2(-240f, y), new Vector2(420f, 70f));
            var slider = NewSlider(parent, "Slider_" + label, new Vector2(170f, y), new Vector2(420f, 36f), track, fillSprite, handle);
            valueText = NewText("Value_" + label, parent, "100%", Bold, 32, TextDark, TextAnchor.MiddleCenter, new Vector2(440f, y), new Vector2(110f, 60f));
            return slider;
        }

        private static Slider NewSlider(Transform parent, string name, Vector2 pos, Vector2 size, Sprite track, Sprite fillSprite, Sprite handleSprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var bg = NewImage("Background", rt, track, Color.white, Vector2.zero, Vector2.zero);
            Stretch(bg.rectTransform);

            var fillArea = NewRect("Fill Area", rt, Vector2.zero, Vector2.zero);
            Stretch(fillArea);
            var fill = NewImage("Fill", fillArea, fillSprite, Color.white, Vector2.zero, Vector2.zero);
            fill.rectTransform.anchorMin = new Vector2(0f, 0f);
            fill.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            var handleArea = NewRect("Handle Slide Area", rt, Vector2.zero, Vector2.zero);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(20f, 0f);
            handleArea.offsetMax = new Vector2(-20f, 0f);
            var handle = NewImage("Handle", handleArea, handleSprite, Color.white, Vector2.zero, new Vector2(36f, 48f));
            handle.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            handle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            AddShadow(handle.gameObject);

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = 1f;
            return slider;
        }

        /// <summary>
        /// Công tắc gạt. Hai trạng thái là hai nhóm riêng (chỉ một nhóm hiện tại một lúc, do SettingsPanel bật/tắt)
        /// nên chữ BẬT/TẮT và núm không bao giờ chồng lên nhau.
        /// </summary>
        private static Toggle NewSwitch(Transform parent, Vector2 pos, out GameObject onGroup, out GameObject offGroup)
        {
            var bg = NewImage("Switch", parent, BtnGrey, Color.white, pos, new Vector2(180f, 76f));
            var toggle = bg.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = bg;
            toggle.transition = Selectable.Transition.None;

            // TẮT: nền xám, núm bên trái, chữ bên phải.
            var off = NewImage("Off", bg.transform, BtnGrey, new Color(0.72f, 0.74f, 0.8f), Vector2.zero, Vector2.zero);
            Stretch(off.rectTransform);
            off.raycastTarget = false;
            NewImage("Knob", off.transform, RoundGrey, Color.white, new Vector2(-54f, 4f), new Vector2(56f, 56f)).raycastTarget = false;
            NewText("Label", off.transform, "TẮT", Bold, 26, new Color(0.3f, 0.32f, 0.42f), TextAnchor.MiddleCenter, new Vector2(24f, 4f), new Vector2(80f, 50f));

            // BẬT: nền xanh, chữ bên trái, núm bên phải.
            var on = NewImage("On", bg.transform, BtnGreen, Color.white, Vector2.zero, Vector2.zero);
            Stretch(on.rectTransform);
            on.raycastTarget = false;
            var onText = NewText("Label", on.transform, "BẬT", Bold, 26, Color.white, TextAnchor.MiddleCenter, new Vector2(-24f, 4f), new Vector2(80f, 50f));
            AddOutline(onText.gameObject, new Color(0.05f, 0.35f, 0.1f, 0.8f), 2f);
            NewImage("Knob", on.transform, RoundGrey, Color.white, new Vector2(54f, 4f), new Vector2(56f, 56f)).raycastTarget = false;

            toggle.graphic = null;
            toggle.isOn = true;
            onGroup = on.gameObject;
            offGroup = off.gameObject;
            return toggle;
        }
    }
}
#endif
