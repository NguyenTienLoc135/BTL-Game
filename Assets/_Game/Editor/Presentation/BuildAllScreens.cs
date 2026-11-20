#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BTLGame.EditorTools
{
    /// <summary>Dựng lại tất cả màn (Gameplay rồi MainMenu) bằng một lệnh; có bản tự vào Play để test luồng.</summary>
    public static class BuildAllScreens
    {
        private const string MainMenuScene = "Assets/_Game/Scenes/MainMenu.unity";

        [MenuItem("Tools/CoTyPhu/Build All Screens")]
        public static void Run()
        {
            GameplaySceneBuilder.Build();
            MainMenuSceneBuilder.Build(); // dựng sau cùng nên MainMenu là Scene đang mở
            Debug.Log("[BuildAllScreens] Đã dựng Gameplay + MainMenu.");
        }

        /// <summary>Dùng với -batchmode: dựng lại mọi màn rồi build bản Windows để chơi thử, sau đó thoát.</summary>
        public static void RunBatchAndBuild()
        {
            Run();
            BuildPlaytest.Build();
        }

        /// <summary>Dùng khi mở Unity bằng dòng lệnh: dựng rồi vào Play ở MainMenu.</summary>
        public static void RunAndPlay()
        {
            Run();
            EditorSceneManager.OpenScene(MainMenuScene);
            EditorApplication.delayCall += () => EditorApplication.EnterPlaymode();
        }
    }
}
#endif
