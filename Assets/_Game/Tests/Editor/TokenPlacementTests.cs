using System.Linq;
using BTLGame.Board;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BtlGame.Tests
{
    /// <summary>Quân đứng giữa ô, hơi tụt về phía đáy; nhiều quân cùng ô tách nhau và vẫn trong ô.</summary>
    public sealed class TokenPlacementTests
    {
        private static BoardView Board()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Gameplay.unity");
            return Object.FindFirstObjectByType<BoardView>();
        }

        private static Vector3 Position(int playerId)
            => Object.FindObjectsByType<PlayerTokenView>(FindObjectsInactive.Include).Single(t => t.PlayerId == playerId).transform.position;

        private static Vector3 Tile(int id)
            => Object.FindObjectsByType<BoardTileView>(FindObjectsInactive.Include).Single(t => t.TileId == id).transform.position;

        private static void Place(BoardView board, params int[] tiles)
            => board.ApplySnapshot(new BoardPresentationSnapshot(
                tiles.Select((tile, i) => new BoardPlayerVisualState(i, i, tile, true)).ToArray(),
                new BoardPropertyVisualState[0]));

        [Test]
        public void SingleToken_IsCentredAndSlightlyLower()
        {
            var board = Board();
            Place(board, 5, 20);
            foreach (var pair in new[] { (player: 0, tile: 5), (player: 1, tile: 20) })
            {
                Assert.AreEqual(Tile(pair.tile).x, Position(pair.player).x, 0.001f, "Giữa ô theo chiều ngang.");
                float down = Tile(pair.tile).z - Position(pair.player).z;
                Assert.That(down, Is.InRange(0.05f, 0.4f), "Hơi tụt xuống đáy nhưng không ra khỏi ô.");
            }
        }

        [Test]
        public void SharedTile_TokensSeparateButStayInside()
        {
            var board = Board();
            Place(board, 11, 11, 11, 11);   // ô cạnh bên (hẹp theo chiều dọc: rộng 3 x cao 2)
            var all = Enumerable.Range(0, 4).Select(Position).ToArray();
            for (int i = 0; i < 4; i++)
            {
                Assert.That(Mathf.Abs(all[i].x - Tile(11).x), Is.LessThanOrEqualTo(0.5f));
                Assert.That(Mathf.Abs(all[i].z - Tile(11).z), Is.LessThanOrEqualTo(0.5f));
                for (int j = i + 1; j < 4; j++) Assert.That(Vector3.Distance(all[i], all[j]), Is.GreaterThan(0.4f));
            }
        }
    }
}
