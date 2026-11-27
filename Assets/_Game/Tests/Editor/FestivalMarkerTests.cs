using System.Linq;
using BTLGame.Board;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BtlGame.Tests
{
    /// <summary>Đúng những thành phố lễ hội cố định mới hiện dấu; các ô khác ẩn.</summary>
    public sealed class FestivalMarkerTests
    {
        [Test]
        public void OnlyChosenCities_ShowFestivalMarker()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Gameplay.unity");
            var board = Object.FindFirstObjectByType<BoardView>();
            var tiles = Object.FindObjectsByType<BoardTileView>(FindObjectsInactive.Include);
            int[] chosen = { 1, 13, 31 };

            board.ShowFestivalTiles(chosen);

            foreach (var tile in tiles)
            {
                var marker = tile.transform.Find("FestivalMarker");
                bool visible = marker != null && marker.gameObject.activeSelf;
                Assert.AreEqual(chosen.Contains(tile.TileId), visible, "Ô " + tile.TileId);
            }
        }
    }
}
