using System.Linq;
using BTLGame.Board;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BtlGame.Tests
{
    /// <summary>Chòi bãi biển: ẩn khi chưa có chủ; có chủ thì hiện và mái/cờ đúng màu ghế của chủ.</summary>
    public sealed class BeachHutTests
    {
        [Test]
        public void BeachHut_ShowsInOwnerSeatColor_AndHidesWhenUnowned()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Gameplay.unity");
            var tiles = Object.FindObjectsByType<BoardTileView>(FindObjectsInactive.Include);
            foreach (int id in new[] { 4, 14, 18, 25 })
            {
                var tile = tiles.Single(t => t.TileId == id);
                var hut = tile.transform.Find("BeachHut");
                Assert.That(hut != null && !hut.gameObject.activeSelf, Is.True, "Chòi có sẵn nhưng ẩn ở ô " + id);

                var color = BoardView.GetSeatColor(1);
                tile.SetOwner(1, color);
                Assert.That(hut.gameObject.activeSelf, Is.True);
                var block = new MaterialPropertyBlock();
                hut.Find("Roof0").GetComponent<Renderer>().GetPropertyBlock(block);
                Assert.That(Vector4.Distance(block.GetColor("_BaseColor"), color), Is.LessThan(.00001f));

                tile.SetOwner(null, Color.white);
                Assert.That(hut.gameObject.activeSelf, Is.False);
            }

            Assert.That(tiles.Single(t => t.TileId == 1).transform.Find("BeachHut"), Is.Null, "Thành phố không có chòi.");
        }
    }
}
