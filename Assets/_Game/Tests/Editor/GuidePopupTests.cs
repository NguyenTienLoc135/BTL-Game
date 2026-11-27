using System.Linq;
using BTLGame.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BtlGame.Tests
{
    /// <summary>Mục Hướng dẫn ở MainMenu: đủ các phần luật chính và cuộn được vì dài hơn khung.</summary>
    public sealed class GuidePopupTests
    {
        [Test]
        public void GuidePopup_HasRuleSections_AndScrolls()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/MainMenu.unity");
            var popup = Object.FindObjectsByType<UiPopup>(FindObjectsInactive.Include).Single(p => p.name == "Popup_HowTo");
            popup.Open();
            Canvas.ForceUpdateCanvases();

            var scroll = popup.GetComponentInChildren<ScrollRect>(true);
            Assert.IsNotNull(scroll);
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            var body = scroll.content.GetComponent<Text>();
            foreach (string section in new[] { "Cách thắng", "Thành phố", "Bãi biển", "Mua lại", "Nhà tù", "thẻ sự kiện", "nghề nghiệp" })
                StringAssert.Contains(section, body.text);
            StringAssert.DoesNotContain("tạm thời", body.text);
            StringAssert.DoesNotContain("Cuộn chuột", body.text);
            foreach (string line in body.text.Split('\n'))
                Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(line, @"^\d+\. |<b>\d+\. "), "Không đánh số mục: " + line);
            Assert.IsNotNull(scroll.verticalScrollbar, "Phải có thanh cuộn.");
            Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height, "Nội dung dài hơn khung nên phải cuộn được.");
            Assert.Less(body.text.Length, 16000, "Giới hạn đỉnh của UI Text.");
        }
    }
}
