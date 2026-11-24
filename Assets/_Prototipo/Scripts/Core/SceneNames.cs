using UnityEngine.SceneManagement;

namespace Prototipo
{
    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string Consent = "Consent";
        public const string Game = "Game";
        public const string Survey = "Survey";
        public const string End = "End";

        public static readonly string[] BuildOrder = { MainMenu, Consent, Game, Survey, End };

        public static void Load(string sceneName) => SceneManager.LoadScene(sceneName);
    }
}
