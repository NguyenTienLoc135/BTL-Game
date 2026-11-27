using System.Linq;
using BTLGame.Board;
using BTLGame.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BtlGame.Tests
{
    /// <summary>Chọn ô dịch chuyển: nhận đúng ô theo vị trí bấm, chỉ các ô được phép, trả ô về chỗ cũ khi đóng.</summary>
    public sealed class TeleportPickerTests
    {
        [Test]
        public void Picker_MapsBoardPointToAllowedTile_AndRestoresPositions()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Gameplay.unity");
            var picker = Object.FindObjectsByType<TeleportPicker>(FindObjectsInactive.Include).Single();
            var tiles = Object.FindObjectsByType<BoardTileView>(FindObjectsInactive.Include).ToDictionary(t => t.TileId);
            int[] allowed = Enumerable.Range(0, 32).Where(i => i != 24).ToArray();
            var home = tiles[5].transform.position;

            picker.ShowTilePicker("chọn ô", allowed, true, false);
            Assert.That(picker.IsPicking, Is.True);
            foreach (int id in allowed)
                Assert.AreEqual(id, picker.TileAt(tiles[id].transform.position), "Bấm giữa ô phải ra đúng ô " + id);
            Assert.IsNull(picker.TileAt(tiles[24].transform.position), "Ô 24 (đang đứng) không được chọn.");
            Assert.IsNull(picker.TileAt(Vector3.zero), "Bấm giữa bàn không chọn ô nào.");

            // Không được có lớp UI trong suốt phủ cả màn đang bật (sẽ nuốt cú bấm vào ô).
            Canvas.ForceUpdateCanvases();
            var blockers = Object.FindObjectsByType<Graphic>(FindObjectsInactive.Exclude)
                .Where(g => g.raycastTarget && g.rectTransform.rect.width > 1400f && g.rectTransform.rect.height > 700f)
                .Select(g => g.name).ToArray();
            Assert.IsEmpty(blockers, "Lớp UI chặn bấm ô: " + string.Join(", ", blockers));

            picker.HideTilePicker();
            Assert.That(picker.IsPicking, Is.False);
            Assert.AreEqual(home, tiles[5].transform.position);
        }
    }
}
