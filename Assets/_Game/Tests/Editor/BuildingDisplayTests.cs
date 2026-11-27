using System.Linq;
using BTLGame.Board;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BtlGame.Tests
{
    /// <summary>Rà soát công trình trên cả 20 thành phố: cùng cấp thì cùng cấu tạo; mua cấp nào hiện đúng cấp đó, không qua cọc.</summary>
    public sealed class BuildingDisplayTests
    {
        private static readonly string[] Levels = { "Stake", "House1", "House2", "House3", "Hotel" };

        private static BoardTileView[] CityTiles()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Gameplay.unity");
            return Object.FindObjectsByType<BoardTileView>(FindObjectsInactive.Include)
                .Where(t => t.transform.Find("Building_Stake") != null).OrderBy(t => t.TileId).ToArray();
        }

        private static string Composition(Transform stage)
            => string.Join("|", stage.GetComponentsInChildren<Renderer>(true)
                .Select(r => r.name + "@" + stage.InverseTransformPoint(r.transform.position).ToString("F2") + "x" + r.transform.lossyScale.ToString("F2")
                             + ":" + r.sharedMaterial.name)
                .OrderBy(s => s));

        [Test]
        public void EveryCity_HasIdenticalBuildingPerLevel_AllHiddenByDefault()
        {
            var tiles = CityTiles();
            Assert.AreEqual(20, tiles.Length);
            foreach (string level in Levels)
            {
                string reference = Composition(tiles[0].transform.Find("Building_" + level));
                foreach (var tile in tiles)
                {
                    var stage = tile.transform.Find("Building_" + level);
                    Assert.IsFalse(stage.gameObject.activeSelf, level + " phải ẩn khi chưa mua, ô " + tile.TileId);
                    Assert.AreEqual(1, tile.transform.Cast<Transform>().Count(c => c.name == "Building_" + level), "Trùng công trình ở ô " + tile.TileId);
                    Assert.AreEqual(reference, Composition(stage), level + " khác mẫu ở ô " + tile.TileId);
                }
            }
        }

        [Test]
        public void BoughtLevel_AppearsDirectly_WithoutAnyOtherStage()
        {
            var tiles = CityTiles();
            var board = Object.FindFirstObjectByType<BoardView>();
            var players = new[] { new BoardPlayerVisualState(0, 0, 0, true) };
            for (int level = 1; level <= 5; level++)
            {
                // Trạng thái ngay sau khi mua: Core đặt thẳng cấp đã chọn, bàn cờ chỉ được bật đúng cấp đó.
                board.ApplySnapshot(new BoardPresentationSnapshot(players, tiles
                    .Select(t => new BoardPropertyVisualState(t.TileId, 0, (PropertyVisualLevel)level)).ToArray()));
                foreach (var tile in tiles)
                    for (int k = 0; k < Levels.Length; k++)
                        Assert.AreEqual(k == level - 1, tile.transform.Find("Building_" + Levels[k]).gameObject.activeSelf,
                            "Ô " + tile.TileId + " mua cấp " + level + " nhưng sai trạng thái " + Levels[k]);
            }
        }
    }
}
