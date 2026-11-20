#if UNITY_EDITOR
using System.IO;
using BTLGame.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BTLGame.EditorTools
{
    /// <summary>
    /// Bộ dựng UI dùng chung cho MainMenu, Cài đặt, Gameplay HUD: một bảng màu, một nơi nhập sprite,
    /// một kiểu nút/popup. Mọi builder PHẢI đi qua lớp này để các màn đồng bộ phong cách.
    /// Sprite được COPY từ ThirdParty sang Assets/_Game/Art/Sprites/UI rồi mới đổi import settings.
    /// </summary>
    internal static class UiKit
    {
        // ---------------------------------------------------------------- đường dẫn asset
        internal const string UiPack = "Assets/ThirdParty/Kenney/UIPack/Sprites";
        internal const string Icons = "Assets/ThirdParty/Kenney/BoardGameIcons/Sprites";
        internal const string Flags = "Assets/ThirdParty/Kenney/FlagPack/Sprites";
        internal const string ClickSound = "Assets/ThirdParty/Kenney/UIPack/Audio/click-a.ogg";
        private const string SpriteDir = "Assets/_Game/Art/Sprites/UI";
        private const string FontDir = "Assets/ThirdParty/Fonts/BeVietnamPro/";

        // ---------------------------------------------------------------- bảng màu chung
        internal static readonly Color Gold = new Color(1f, 0.82f, 0.2f);
        internal static readonly Color GoldOutline = new Color(0.45f, 0.17f, 0.02f);
        internal static readonly Color Navy = new Color(0.45f, 0.55f, 0.85f);      // nhuộm sprite Blue thành xanh navy
        internal static readonly Color NavyDeep = new Color(0.32f, 0.4f, 0.75f);
        internal static readonly Color Cream = new Color(1f, 0.95f, 0.86f);
        internal static readonly Color TextDark = new Color(0.2f, 0.2f, 0.42f);
        internal static readonly Color TextOutline = new Color(0.08f, 0.1f, 0.3f, 0.85f);

        /// <summary>Màu theo ghế 0–3, dùng chung cho màn thiết lập và HUD để người chơi nhận ra đúng người.</summary>
        internal static readonly Color[] SeatColors =
        {
            new Color(0.95f, 0.65f, 0.25f), new Color(0.95f, 0.5f, 0.7f),
            new Color(0.45f, 0.75f, 0.95f), new Color(0.95f, 0.85f, 0.35f)
        };

        internal static readonly Vector4 ButtonBorder = new Vector4(36f, 44f, 36f, 36f); // L,B,R,T

        // ---------------------------------------------------------------- font
        private static Font _bold, _black, _regular;
        internal static Font Bold => _bold != null ? _bold : (_bold = LoadFont("BeVietnamPro-Bold.ttf"));
        internal static Font Black => _black != null ? _black : (_black = LoadFont("BeVietnamPro-Black.ttf"));
        internal static Font Regular => _regular != null ? _regular : (_regular = LoadFont("BeVietnamPro-Regular.ttf"));

        private static Font LoadFont(string file)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontDir + file);
            if (font == null)
            {
                Debug.LogWarning("[UiKit] Không tìm thấy font: " + FontDir + file);
            }

            return font;
        }

        // ---------------------------------------------------------------- sprite
        internal static Sprite BtnYellow => ImportSprite(UiPack + "/Yellow/button_rectangle_depth_gradient.png", "btn_yellow", ButtonBorder);
        internal static Sprite BtnBlue => ImportSprite(UiPack + "/Blue/button_rectangle_depth_gradient.png", "btn_blue", ButtonBorder);
        internal static Sprite BtnGreen => ImportSprite(UiPack + "/Green/button_rectangle_depth_gradient.png", "btn_green", ButtonBorder);
        internal static Sprite BtnRed => ImportSprite(UiPack + "/Red/button_rectangle_depth_gradient.png", "btn_red", ButtonBorder);
        internal static Sprite BtnGrey => ImportSprite(UiPack + "/Grey/button_rectangle_depth_gradient.png", "btn_grey", ButtonBorder);
        internal static Sprite RoundRed => ImportSprite(UiPack + "/Red/button_round_depth_gradient.png", "btn_round_red", Vector4.zero);
        internal static Sprite RoundGrey => ImportSprite(UiPack + "/Grey/button_round_depth_gradient.png", "btn_round_grey", Vector4.zero);
        internal static Sprite IconCross => ImportSprite(UiPack + "/Blue/icon_cross.png", "icon_cross", Vector4.zero);
        internal static Sprite IconCheck => ImportSprite(UiPack + "/Green/icon_checkmark.png", "icon_check", Vector4.zero);

        internal static Sprite Icon(string file) => ImportSprite(Icons + "/" + file + ".png", "icon_" + file, Vector4.zero);

        internal static Sprite IconPlay => ImportSprite(UiPack + "/Extra/icon_play_light.png", "icon_play", Vector4.zero);

        // Bộ icon không có bánh răng / dấu ? trong vòng tròn / nút nguồn → vẽ bằng code (trắng, nền trong suốt),
        // lưu thành sprite riêng dưới Assets/_Game/Art/Sprites/UI để dùng như icon bình thường.
        internal static Sprite IconGear => Generated("icon_gear", GearAlpha);
        internal static Sprite IconQuestion => Generated("icon_question", QuestionAlpha);
        internal static Sprite IconPower => Generated("icon_power", PowerAlpha);

        /// <summary>Đầu robot: ăng-ten, tai, hai mắt, miệng (chế độ chơi với Bot).</summary>
        internal static Sprite IconRobot => Generated("icon_robot", RobotAlpha);

        /// <summary>Hai người (bán thân) cạnh nhau (chế độ chơi với người).</summary>
        internal static Sprite IconPeople => Generated("icon_people", PeopleAlpha);

        /// <summary>Vòng tròn nhiều màu pastel in trên mặt ô Thẻ sự kiện.</summary>
        internal static Sprite CardWheel => GeneratedColor("card_wheel", CardWheelColor);

        /// <summary>Tờ giấy kẹp bút (ô Thuế), màu pastel, in phẳng lên mặt ô.</summary>
        internal static Sprite TaxPaper => GeneratedColor("tax_paper", TaxPaperColor);

        private const int GenSize = 128;

        private static Sprite Generated(string name, System.Func<float, float, float> alpha)
        {
            return GeneratedColor(name, (x, y) => new Color(1f, 1f, 1f, Mathf.Clamp01(alpha(x, y))));
        }

        /// <summary>Sprite vẽ bằng code có màu riêng (không chỉ trắng) từ hàm màu theo toạ độ chuẩn hóa [-1,1].</summary>
        private static Sprite GeneratedColor(string name, System.Func<float, float, Color> color)
        {
            Directory.CreateDirectory(SpriteDir);
            string path = SpriteDir + "/" + name + ".png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(GenSize, GenSize, TextureFormat.RGBA32, false);
                var pixels = new Color[GenSize * GenSize];
                for (int py = 0; py < GenSize; py++)
                {
                    for (int px = 0; px < GenSize; px++)
                    {
                        float x = (px + 0.5f) / GenSize * 2f - 1f;
                        float y = (py + 0.5f) / GenSize * 2f - 1f;
                        pixels[py * GenSize + px] = color(x, y);
                    }
                }

                tex.SetPixels(pixels);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private const float Aa = 0.022f; // độ mịn viền (đơn vị chuẩn hóa)

        private static float Edge(float signedDistance) => Mathf.Clamp01(0.5f - signedDistance / Aa);

        private static float SegDist(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay;
            float t = Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy));
            return Mathf.Sqrt((px - ax - t * dx) * (px - ax - t * dx) + (py - ay - t * dy) * (py - ay - t * dy));
        }

        /// <summary>Bánh răng 8 răng, lỗ tròn ở giữa.</summary>
        private static float GearAlpha(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float theta = Mathf.Atan2(y, x);
            float tooth = Mathf.Clamp01(Mathf.Sin(theta * 8f) * 2.2f + 0.5f);
            float outer = 0.64f + 0.24f * tooth;
            return Edge(r - outer) * Edge(0.27f - r);
        }

        /// <summary>Dấu ? khoét trong đĩa tròn trắng.</summary>
        private static float QuestionAlpha(float x, float y)
        {
            float disc = Edge(Mathf.Sqrt(x * x + y * y) - 0.92f);

            // Cung móc: vòng tâm (0, 0.2) bán kính 0.3, bỏ phần dưới-trái.
            float cx = x, cy = y - 0.2f;
            float rr = Mathf.Sqrt(cx * cx + cy * cy);
            float deg = Mathf.Atan2(cy, cx) * Mathf.Rad2Deg;       // -180..180
            bool inArc = deg > -45f || deg < -180f;                // giữ từ -45° qua đỉnh tới 180°
            float arc = inArc ? Edge(Mathf.Abs(rr - 0.3f) - 0.1f) : 0f;

            // Thân: từ cuối cung (≈ (0.21, -0.01)) về giữa rồi xuống.
            float stem = Edge(SegDist(x, y, 0.21f, -0.01f, 0.0f, -0.17f) - 0.1f);
            stem = Mathf.Max(stem, Edge(SegDist(x, y, 0f, -0.17f, 0f, -0.22f) - 0.1f));

            // Chấm dưới.
            float dot = Edge(Mathf.Sqrt(x * x + (y + 0.5f) * (y + 0.5f)) - 0.115f);

            float mark = Mathf.Clamp01(Mathf.Max(arc, Mathf.Max(stem, dot)));
            return disc * (1f - mark);
        }

        private static float SdRoundBox(float x, float y, float cx, float cy, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(x - cx) - (hx - r);
            float qy = Mathf.Abs(y - cy) - (hy - r);
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static float SdCircle(float x, float y, float cx, float cy, float r)
        {
            return Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;
        }

        private static float RobotAlpha(float x, float y)
        {
            float head = Edge(SdRoundBox(x, y, 0f, -0.08f, 0.6f, 0.46f, 0.16f));
            float ears = Mathf.Max(Edge(SdRoundBox(x, y, -0.72f, -0.08f, 0.1f, 0.22f, 0.05f)), Edge(SdRoundBox(x, y, 0.72f, -0.08f, 0.1f, 0.22f, 0.05f)));
            float antenna = Mathf.Max(Edge(SegDist(x, y, 0f, 0.38f, 0f, 0.66f) - 0.045f), Edge(SdCircle(x, y, 0f, 0.74f, 0.1f)));
            float body = Mathf.Max(head, Mathf.Max(ears, antenna));

            float eyes = Mathf.Max(Edge(SdCircle(x, y, -0.25f, 0.02f, 0.13f)), Edge(SdCircle(x, y, 0.25f, 0.02f, 0.13f)));
            float mouth = Edge(SegDist(x, y, -0.26f, -0.27f, 0.26f, -0.27f) - 0.045f);
            return body * (1f - Mathf.Max(eyes, mouth));
        }

        // Bán thân: đầu tròn + vai (nửa elip). s = tỉ lệ.
        private static float SdBust(float x, float y, float cx, float cy, float s)
        {
            float head = SdCircle(x, y, cx, cy + 0.36f * s, 0.27f * s);
            float a = 0.5f * s, b = 0.4f * s;
            float ex = (x - cx) / a, ey = (y - (cy - 0.36f * s)) / b;
            float shoulder = (Mathf.Sqrt(ex * ex + ey * ey) - 1f) * Mathf.Min(a, b);
            shoulder = Mathf.Max(shoulder, (cy - 0.62f * s) - y); // cắt phẳng phía dưới
            return Mathf.Min(head, shoulder);
        }

        private static float PeopleAlpha(float x, float y)
        {
            float front = Edge(SdBust(x, y, -0.2f, -0.02f, 1.0f));
            float frontDilated = Edge(SdBust(x, y, -0.2f, -0.02f, 1.0f) - 0.07f);
            float back = Edge(SdBust(x, y, 0.42f, 0.08f, 0.82f)) * (1f - frontDilated);
            return Mathf.Max(front, back);
        }

        // Trộn một lớp màu (rgb, độ phủ a) lên màu nền có alpha.
        private static Color Over(Color under, float r, float g, float b, float a)
        {
            float outA = a + under.a * (1f - a);
            if (outA <= 0.0001f)
            {
                return new Color(0f, 0f, 0f, 0f);
            }

            float w = under.a * (1f - a);
            return new Color((r * a + under.r * w) / outA, (g * a + under.g * w) / outA, (b * a + under.b * w) / outA, outA);
        }

        /// <summary>Tờ giấy (viền hồng xám) có các dòng chữ kẻ, dấu mộc tròn, bút xanh nằm vắt ngang đầu giấy, kẹp kim loại đè lên bút.</summary>
        private static Color TaxPaperColor(float x, float y)
        {
            var c = new Color(0f, 0f, 0f, 0f);

            // Tờ giấy.
            float sdPaper = SdRoundBox(x, y, 0f, -0.1f, 0.56f, 0.76f, 0.08f);
            c = Over(c, 0.78f, 0.7f, 0.76f, Edge(sdPaper));
            c = Over(c, 0.995f, 0.99f, 0.96f, Edge(sdPaper + 0.035f));

            // Các dòng chữ (đường kẻ) — dòng cuối ngắn hơn.
            for (int k = 0; k < 5; k++)
            {
                float yk = 0.4f - k * 0.2f;
                float xEnd = k == 4 ? 0.02f : 0.36f;
                c = Over(c, 0.72f, 0.76f, 0.88f, Edge(SegDist(x, y, -0.36f, yk, xEnd, yk) - 0.028f));
            }

            // Dấu mộc tròn góc dưới phải.
            c = Over(c, 0.92f, 0.6f, 0.68f, Edge(Mathf.Abs(SdCircle(x, y, 0.28f, -0.58f, 0.15f)) - 0.035f));

            // Bút: thân xanh, nắp hồng ở đầu trái, ngòi sẫm ở đầu phải.
            c = Over(c, 0.5f, 0.64f, 0.94f, Edge(SegDist(x, y, -0.42f, 0.7f, 0.5f, 0.9f) - 0.075f));
            c = Over(c, 0.92f, 0.6f, 0.7f, Edge(SegDist(x, y, -0.62f, 0.655f, -0.45f, 0.69f) - 0.08f));
            c = Over(c, 0.36f, 0.4f, 0.55f, Edge(SegDist(x, y, 0.52f, 0.905f, 0.64f, 0.93f) - 0.04f));

            // Kẹp kim loại ở đầu giấy (đè lên bút).
            c = Over(c, 0.6f, 0.64f, 0.74f, Edge(SdRoundBox(x, y, 0f, 0.76f, 0.2f, 0.1f, 0.04f)));
            c = Over(c, 0.44f, 0.5f, 0.62f, Edge(SdRoundBox(x, y, 0f, 0.77f, 0.12f, 0.04f, 0.02f)));
            return c;
        }

        private static readonly Color[] WheelColors =
        {
            new Color(0.96f, 0.62f, 0.72f), new Color(0.98f, 0.76f, 0.55f), new Color(0.98f, 0.9f, 0.55f),
            new Color(0.62f, 0.86f, 0.68f), new Color(0.58f, 0.8f, 0.95f), new Color(0.76f, 0.68f, 0.94f)
        };

        /// <summary>Đĩa 6 múi pastel, viền trắng, tâm trắng, kẻ trắng giữa các múi.</summary>
        private static Color CardWheelColor(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float disc = Edge(r - 0.94f);
            if (disc <= 0f)
            {
                return new Color(0f, 0f, 0f, 0f);
            }

            float deg = (Mathf.Atan2(y, x) * Mathf.Rad2Deg + 360f) % 360f;
            int sector = Mathf.Min(5, (int)(deg / 60f));
            float toLine = Mathf.Min(deg % 60f, 60f - deg % 60f) * Mathf.Deg2Rad * r; // khoảng cách tới đường kẻ
            Color c = WheelColors[sector];

            float white = 0f;
            if (r > 0.78f) white = 1f;                                   // viền ngoài
            else if (r < 0.2f) white = 1f;                               // tâm trắng
            white = Mathf.Max(white, Edge(toLine - 0.03f));              // đường kẻ múi
            return new Color(Mathf.Lerp(c.r, 1f, Mathf.Clamp01(white)), Mathf.Lerp(c.g, 1f, Mathf.Clamp01(white)), Mathf.Lerp(c.b, 1f, Mathf.Clamp01(white)), disc);
        }

        /// <summary>Biểu tượng nguồn/thoát: vòng hở phía trên + thanh dọc.</summary>
        private static float PowerAlpha(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float deg = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
            bool gap = Mathf.Abs(deg - 90f) < 38f;
            float ring = gap ? 0f : Edge(Mathf.Abs(r - 0.56f) - 0.1f);
            float bar = Edge(SegDist(x, y, 0f, 0.12f, 0f, 0.82f) - 0.1f);
            return Mathf.Max(ring, bar);
        }

        internal static Sprite Flag(string code) => ImportSprite(Flags + "/" + code + ".png", "flag_" + code, Vector4.zero);

        internal static Sprite ImportSprite(string source, string name, Vector4 border)
        {
            Directory.CreateDirectory(SpriteDir);
            string dest = SpriteDir + "/" + name + ".png";
            if (!File.Exists(dest))
            {
                if (!AssetDatabase.CopyAsset(source, dest))
                {
                    Debug.LogWarning("[UiKit] Không copy được sprite: " + source);
                    return null;
                }
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(dest);
            if (importer == null)
            {
                return null;
            }

            if (importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != border)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(dest);
        }

        // ---------------------------------------------------------------- phần tử cơ bản
        internal static RectTransform NewRect(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        internal static Image NewImage(string name, Transform parent, Sprite sprite, Color color, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                img.type = Image.Type.Sliced;
            }

            return img;
        }

        internal static Text NewText(string name, Transform parent, string content, Font font, int size, Color color,
            TextAnchor anchor, Vector2 pos, Vector2 box)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        internal static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        internal static void AddOutline(GameObject go, Color color, float distance)
        {
            var o = go.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(distance, -distance);
        }

        internal static void AddShadow(GameObject go)
        {
            var s = go.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.55f);
            s.effectDistance = new Vector2(2f, -3f);
        }

        // ---------------------------------------------------------------- nút
        /// <summary>Nút chính (vàng, chữ trắng viền nâu) — dùng cho hành động quan trọng nhất của màn.</summary>
        internal static Button PrimaryButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, int fontSize, Sprite icon = null)
        {
            // Chữ nâu đậm KHÔNG viền trên nền vàng: nét sắc, tương phản cao (chữ trắng + viền dày bị nhòe).
            var button = BaseButton(parent, name, BtnYellow, Color.white, icon, label, pos, size, fontSize, Black, new Color(0.42f, 0.2f, 0.02f), Color.clear, 0f, new Color(0.42f, 0.2f, 0.02f));
            return button;
        }

        /// <summary>Nút phụ (xanh navy, chữ trắng) — kiểu nút mặc định của mọi menu/popup.</summary>
        internal static Button SecondaryButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, int fontSize, Sprite icon = null)
        {
            return BaseButton(parent, name, BtnBlue, Navy, icon, label, pos, size, fontSize, Bold, Color.white, TextOutline, 2.5f);
        }

        /// <summary>Nút vuông nhỏ chỉ có icon (góc phải trên, cột nút bên phải trong trận).</summary>
        internal static Button IconButton(Transform parent, string name, Sprite icon, Vector2 pos, Vector2 size)
        {
            var img = NewImage(name, parent, BtnBlue, Navy, pos, size);
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            StyleButton(button);
            if (icon != null)
            {
                var i = NewImage("Icon", img.transform, icon, Color.white, new Vector2(0f, 3f), size * 0.55f);
                i.raycastTarget = false;
                AddShadow(i.gameObject);
            }

            return button;
        }

        private static Button BaseButton(Transform parent, string name, Sprite bg, Color tint, Sprite icon, string label,
            Vector2 pos, Vector2 size, int fontSize, Font font, Color textColor, Color outline, float outlineSize, Color? iconTint = null)
        {
            var img = NewImage(name, parent, bg, tint, pos, size);
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            StyleButton(button);

            float textLeft = 0f;
            if (icon != null)
            {
                var iconImg = NewImage("Icon", img.transform, icon, iconTint ?? Color.white, new Vector2(-size.x / 2f + size.y * 0.6f, 4f), new Vector2(size.y * 0.5f, size.y * 0.5f));
                iconImg.raycastTarget = false;
                iconImg.preserveAspect = true;
                if (iconTint == null)
                {
                    AddShadow(iconImg.gameObject);
                }
                textLeft = size.y * 0.5f;
            }

            var text = NewText("Label", img.transform, label, font, fontSize, textColor, TextAnchor.MiddleCenter,
                new Vector2(textLeft / 2f, 4f), new Vector2(size.x - textLeft - 24f, size.y - 8f));
            if (outlineSize > 0f)
            {
                AddOutline(text.gameObject, outline, outlineSize);
            }
            return button;
        }

        internal static void StyleButton(Button button)
        {
            var colors = button.colors;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
            button.colors = colors;
        }

        // ---------------------------------------------------------------- popup chung
        internal struct PopupParts
        {
            public UiPopup Popup;
            public GameObject Root;
            public RectTransform Inner;   // vùng nội dung (tâm = tâm bảng kem)
        }

        /// <summary>
        /// Popup chuẩn: nền mờ bấm để đóng, khung navy, bảng kem, tab tiêu đề nổi, nút X đỏ. Mặc định ẩn.
        /// </summary>
        internal static PopupParts Popup(Transform canvas, string name, string title, Vector2 frameSize, Sprite titleIcon = null)
        {
            var dim = NewImage(name, canvas, null, new Color(0f, 0f, 0f, 0.6f), Vector2.zero, Vector2.zero);
            Stretch(dim.rectTransform);

            // Lớp bấm-để-đóng là một object RIÊNG nằm sau khung. Nếu gắn Button lên chính nền mờ (cha của khung),
            // mọi cú bấm vào vùng trống trong khung (kể cả kéo thanh trượt) sẽ nổi bọt lên và đóng popup.
            var backdropImg = NewImage("Backdrop", dim.transform, null, new Color(0f, 0f, 0f, 0.001f), Vector2.zero, Vector2.zero);
            Stretch(backdropImg.rectTransform);
            var backdrop = backdropImg.gameObject.AddComponent<Button>();
            backdrop.targetGraphic = backdropImg;
            backdrop.transition = Selectable.Transition.None;

            var frame = NewImage("Frame", dim.transform, BtnBlue, NavyDeep, Vector2.zero, frameSize);
            var innerSize = new Vector2(frameSize.x - 80f, frameSize.y - 100f);
            var inner = NewImage("Inner", frame.transform, BtnGrey, Cream, new Vector2(0f, -35f), innerSize);

            // Tab tiêu đề tự giãn theo độ dài chữ (không tràn viền); chữ co vừa khung nếu vẫn quá dài.
            float headerW = Mathf.Clamp(title.Length * 34f + 150f + (titleIcon != null ? 80f : 0f), 480f, frameSize.x - 260f);
            var header = NewImage("Header", frame.transform, BtnBlue, new Color(0.3f, 0.34f, 0.8f), new Vector2(0f, frameSize.y / 2f - 10f), new Vector2(headerW, 110f));
            float titleX = 0f;
            float titleW = headerW - 100f;
            if (titleIcon != null)
            {
                var gear = NewImage("TitleIcon", header.transform, titleIcon, Color.white, new Vector2(-headerW / 2f + 75f, 4f), new Vector2(60f, 60f));
                AddShadow(gear.gameObject);
                titleX = 40f;
                titleW -= 80f;
            }

            var t = NewText("Title", header.transform, title, Black, 52, Color.white, TextAnchor.MiddleCenter, new Vector2(titleX, 4f), new Vector2(titleW, 90f));
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 24;
            t.resizeTextMaxSize = 52;
            AddOutline(t.gameObject, new Color(0.12f, 0.12f, 0.4f, 0.9f), 3f);

            var closeImg = NewImage("Btn_Close", frame.transform, RoundRed, Color.white, new Vector2(frameSize.x / 2f - 50f, frameSize.y / 2f - 10f), new Vector2(92f, 92f));
            var close = closeImg.gameObject.AddComponent<Button>();
            close.targetGraphic = closeImg;
            StyleButton(close);
            var cross = NewImage("Cross", closeImg.transform, IconCross, Color.white, new Vector2(0f, 4f), new Vector2(46f, 46f));
            cross.raycastTarget = false;
            AddShadow(cross.gameObject);

            var popup = dim.gameObject.AddComponent<UiPopup>();
            var so = new SerializedObject(popup);
            so.FindProperty("_closeButton").objectReferenceValue = close;
            so.FindProperty("_backdropButton").objectReferenceValue = backdrop;
            so.ApplyModifiedPropertiesWithoutUndo();

            dim.gameObject.SetActive(false);
            return new PopupParts { Popup = popup, Root = dim.gameObject, Inner = inner.rectTransform };
        }
    }
}
#endif
