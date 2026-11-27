using System.Linq;
using BTLGame.Board;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BtlGame.Tests
{
    /// <summary>Cùng một cấp thì công trình phải cao/rộng như nhau trên mọi ô (kể cả ô lễ hội).</summary>
    public sealed class BuildingSizeTests
    {
        [Test]
        public void SameLevel_HasSameSizeOnEveryCityTile()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Gameplay.unity");
            var tiles = Object.FindObjectsByType<BoardTileView>(FindObjectsInactive.Include)
                .Where(t => t.transform.Find("Building_House1") != null).OrderBy(t => t.TileId).ToArray();
            Assert.AreEqual(20, tiles.Length);

            foreach (string level in new[] { "Stake", "House1", "House2", "House3", "Hotel" })
            {
                Vector3? first = null;
                foreach (var tile in tiles)
                {
                    var stage = tile.transform.Find("Building_" + level);
                    stage.gameObject.SetActive(true);
                    var parts = stage.GetComponentsInChildren<Renderer>();
                    var bounds = parts[0].bounds;
                    foreach (var part in parts) bounds.Encapsulate(part.bounds);
                    stage.gameObject.SetActive(false);

                    if (!first.HasValue)
                    {
                        first = bounds.size;
                        TestContext.WriteLine(level + " " + bounds.size.ToString("F2"));
                    }

                    // Ô cạnh bên không xoay nhà nên kích thước phải trùng hoàn toàn.
                    Assert.That(Vector3.Distance(bounds.size, first.Value), Is.LessThan(0.01f), level + " ở ô " + tile.TileId + ": " + bounds.size.ToString("F2"));
                }
            }
        }
    }
}
