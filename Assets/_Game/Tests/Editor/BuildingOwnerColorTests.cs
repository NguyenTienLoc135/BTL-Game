using System.Linq;
using BTLGame.Board;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BtlGame.Tests
{
    /// <summary>Công trình cùng một kiểu, cấp = số tầng, chỉ đổi màu theo màu ghế của chủ; hết chủ thì trả màu gốc.</summary>
    public sealed class BuildingOwnerColorTests
    {
        private static BoardTileView Tile1()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Gameplay.unity");
            return Object.FindObjectsByType<BoardTileView>(FindObjectsInactive.Include).Single(t => t.TileId == 1);
        }

        [Test]
        public void Levels_ShareOneHouseForm_DifferingOnlyByFloorCount()
        {
            var tile = Tile1();
            string[] names = { "House1", "House2", "House3", "Hotel" };
            for (int i = 0; i < names.Length; i++)
            {
                var parts = tile.transform.Find("Building_" + names[i]).GetComponentsInChildren<Renderer>(true);
                Assert.AreEqual(i + 1, parts.Count(r => r.name.StartsWith("Wall")), names[i] + " phải có đúng số tầng.");
                Assert.AreEqual((i + 1) * 2, parts.Count(r => r.name == "Window"), names[i]);
            }
        }

        [Test]
        public void Buildings_TakeOwnerSeatColor_AndResetWhenUnowned()
        {
            var tile = Tile1();
            var parts = tile.transform.Find("Building_House2").GetComponentsInChildren<Renderer>(true);
            var wall = parts.First(r => r.name.StartsWith("Wall"));
            var window = parts.First(r => r.name == "Window");
            var color = BoardView.GetSeatColor(2);

            tile.SetOwner(2, color);
            var block = new MaterialPropertyBlock();
            wall.GetPropertyBlock(block);
            Assert.That(Vector4.Distance(block.GetColor("_BaseColor"), color), Is.LessThan(.00001f), "Tường phải đúng màu chủ.");
            Assert.That(window.HasPropertyBlock(), Is.False, "Cửa sổ giữ nguyên.");

            tile.SetOwner(null, Color.white);
            Assert.That(wall.HasPropertyBlock(), Is.False);
        }
    }
}
