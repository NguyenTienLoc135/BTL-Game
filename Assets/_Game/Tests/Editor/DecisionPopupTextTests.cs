using System.Linq;
using BTLGame.Presentation;
using BTLGame.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BtlGame.Tests
{
    /// <summary>Popup quyết định / thẻ sự kiện: chữ lựa chọn dài (hơn 80 ký tự) phải xuống dòng và nằm trọn trong nút.</summary>
    public sealed class DecisionPopupTextTests
    {

        [Test]
        public void EventCardTitles_StayOnOneLineInsideTheHeaderTab()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Gameplay.unity");
            var view = Object.FindObjectsByType<DecisionPopupView>(FindObjectsInactive.Include).Single();
            long id = 1;
            foreach (string name in new[] { "Nộp phạt thuế", "Tặng quà 200.000", "Bảo vệ công trình", "Đến ô dịch chuyển", "Ra tù" })
            {
                view.Show(new DecisionPrompt(id++, 0, 0, DecisionPromptKind.EventCard, "Thẻ sự kiện: " + name, "Mô tả",
                    new[] { new DecisionOption("ok", "Đồng ý") }));
                Canvas.ForceUpdateCanvases();

                var title = view.GetComponentsInChildren<Text>(true).Single(t => t.name == "Title");
                var header = (RectTransform)title.transform.parent;
                var generator = new TextGenerator();
                generator.Populate(title.text, title.GetGenerationSettings(title.rectTransform.rect.size));
                Assert.AreEqual(1, generator.lineCount, "Tiêu đề phải nằm một dòng: " + name);
                Assert.AreEqual(title.text.Length, generator.characterCountVisible, "Không được cắt chữ: " + name);
                Assert.LessOrEqual(title.rectTransform.rect.width, header.rect.width, name);
                Assert.LessOrEqual(header.rect.width, ((RectTransform)header.parent).rect.width, "Tab không rộng hơn khung: " + name);
            }
        }
        [Test]
        public void LongOptionText_FitsInsideItsButton_ShortTextKeepsDefaultHeight()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Gameplay.unity");
            var view = Object.FindObjectsByType<DecisionPopupView>(FindObjectsInactive.Include).Single();
            string longText = string.Join(" ", Enumerable.Repeat("Chọn thành phố đối thủ để hạ đúng một cấp công trình", 5));
            Assert.Greater(longText.Length, 80);

            view.Show(new DecisionPrompt(1, 0, 0, DecisionPromptKind.EventCard, "Thẻ sự kiện", "Mô tả",
                new[] { new DecisionOption("long", longText), new DecisionOption("short", "Bỏ qua") }));
            Canvas.ForceUpdateCanvases();

            var buttons = view.GetComponentsInChildren<Button>(true).Where(b => b.name.StartsWith("OptionButton_") && b.gameObject.activeSelf)
                .OrderBy(b => b.transform.GetSiblingIndex()).ToArray();
            Assert.AreEqual(2, buttons.Length);

            var longLabel = buttons[0].GetComponentInChildren<Text>();
            var longRect = (RectTransform)buttons[0].transform;
            Assert.Greater(longRect.rect.height, DecisionPopupView.MinOptionHeight, "Nút chữ dài phải cao thêm.");
            Assert.LessOrEqual(longLabel.preferredHeight, longLabel.rectTransform.rect.height + 1f, "Chữ không được tràn khỏi nút.");
            Assert.LessOrEqual(longLabel.rectTransform.rect.width, longRect.rect.width, "Nhãn không được rộng hơn nút.");

            Assert.AreEqual(DecisionPopupView.MinOptionHeight, ((RectTransform)buttons[1].transform).rect.height, 1f);
        }
    }
}
