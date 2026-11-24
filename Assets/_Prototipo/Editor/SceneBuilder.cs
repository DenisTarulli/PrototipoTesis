using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Prototipo.EditorTools
{
    /// <summary>
    /// Menú Tools > Prototipo. Genera las escenas del prototipo desde código, crea el asset
    /// de configuración y actualiza la lista de escenas del build.
    /// </summary>
    public static class SceneBuilder
    {
        private const string Root = "Assets/_Prototipo";
        private const string ScenesFolder = Root + "/Scenes";
        private const string ResourcesFolder = Root + "/Resources";
        private const string ConfigPath = ResourcesFolder + "/ExperimentConfig.asset";

        private static string ScenePath(string sceneName) => $"{ScenesFolder}/{sceneName}.unity";

        // ------------------------------------------------------------ Menú

        [MenuItem("Tools/Prototipo/Construir escenas", priority = 0)]
        public static void BuildAll()
        {
            if (!EditorUtility.DisplayDialog("Construir escenas",
                    "Se van a generar las escenas MainMenu, Consent, Game y End en Assets/_Prototipo/Scenes.\n\n" +
                    "Si ya existen, se sobrescriben y se pierden los cambios hechos a mano en ellas.",
                    "Construir", "Cancelar"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            BuildAllWithoutDialogs();
            EditorSceneManager.OpenScene(ScenePath(SceneNames.MainMenu));
            EditorUtility.DisplayDialog("Listo",
                "Escenas generadas y agregadas al build en este orden:\nMainMenu → Consent → Game → End.\n\n" +
                "Abrí MainMenu y apretá Play para probar el flujo.", "OK");
        }

        /// <summary>Genera todas las escenas sin pedir confirmación (para automatizar).</summary>
        public static void BuildAllWithoutDialogs()
        {
            EnsureFolder("Assets", "_Prototipo");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Resources");
            EnsureConfig();

            BuildMainMenu();
            BuildConsent();
            BuildGame();
            BuildEnd();
            UpdateBuildSettings();
        }

        [MenuItem("Tools/Prototipo/Seleccionar configuración del experimento", priority = 20)]
        public static void SelectConfig()
        {
            EnsureFolder("Assets", "_Prototipo");
            EnsureFolder(Root, "Resources");
            Selection.activeObject = EnsureConfig();
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        [MenuItem("Tools/Prototipo/Abrir carpeta de datos locales", priority = 21)]
        public static void OpenDataFolder()
        {
            string path = Path.Combine(Application.persistentDataPath, "sessions");
            Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }

        // ------------------------------------------------------------ Utilidades

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static ExperimentConfig EnsureConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<ExperimentConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ExperimentConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
                AssetDatabase.SaveAssets();
            }
            return config;
        }

        private static Transform NewScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            UIFactory.CreateCamera();
            UIFactory.CreateEventSystem();
            var canvas = UIFactory.CreateCanvas();
            UIFactory.CreateBackground(canvas.transform);
            return canvas.transform;
        }

        private static T AddController<T>() where T : Component
        {
            return new GameObject("SceneController").AddComponent<T>();
        }

        private static void SaveScene(string sceneName)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath(sceneName));
        }

        private static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = SceneNames.BuildOrder
                .Select(n => new EditorBuildSettingsScene(ScenePath(n), true))
                .ToArray();
        }

        private static Text Title(Transform parent, string key, int size = 44)
            => UIFactory.CreateText(parent, "Title", key, size, UITheme.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);

        private static RectTransform Bar(Transform parent, string name, bool top, float height)
        {
            var img = UIFactory.CreateImage(parent, name, UITheme.Surface);
            float y = top ? 1f : 0f;
            UIFactory.Anchor(img.rectTransform, new Vector2(0.5f, y), new Vector2(0.5f, y), Vector2.zero, new Vector2(0, height));
            img.rectTransform.anchorMin = new Vector2(0f, y);
            img.rectTransform.anchorMax = new Vector2(1f, y);
            img.rectTransform.sizeDelta = new Vector2(0f, height);

            var hlg = img.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(48, 48, 0, 0);
            hlg.spacing = 16;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            return img.rectTransform;
        }

        // ------------------------------------------------------------ Escenas

        private static void BuildMainMenu()
        {
            var root = NewScene();
            var c = AddController<MainMenuController>();

            var card = UIFactory.CreateCard(root, "MenuCard", 640, 64, 22);
            Title(card, "app.title", 60);
            UIFactory.CreateSpacer(card, 12);
            c.playButton = UIFactory.PrimaryButton(card, "PlayButton", "menu.play");
            c.settingsButton = UIFactory.SecondaryButton(card, "SettingsButton", "menu.settings");
            c.quitButton = UIFactory.SecondaryButton(card, "QuitButton", "menu.quit");

            var version = UIFactory.CreateText(root, "Version", null, 20, UITheme.TextSecondary, TextAnchor.LowerRight, literal: "v0.1.0");
            UIFactory.Anchor(version.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 20f), new Vector2(300f, 40f));
            c.versionText = version;

            // Panel de ajustes
            var overlay = UIFactory.CreateOverlay(root, "SettingsPanel", 760, out var s);
            Title(s, "settings.title");
            UIFactory.CreateText(s, "VolumeLabel", "settings.volume", 28, UITheme.TextPrimary, TextAnchor.MiddleLeft);
            var volumeRow = UIFactory.CreateRow(s, "VolumeRow", 24);
            c.volumeSlider = UIFactory.CreateSlider(volumeRow, "VolumeSlider", 480);
            c.volumeValueText = UIFactory.CreateText(volumeRow, "VolumeValue", null, 28, UITheme.TextPrimary, TextAnchor.MiddleRight, literal: "80%");
            UIFactory.Layout(c.volumeValueText, preferredWidth: 100);

            UIFactory.CreateText(s, "LanguageLabel", "settings.language", 28, UITheme.TextPrimary, TextAnchor.MiddleLeft);
            var langRow = UIFactory.CreateRow(s, "LanguageRow", 20);
            c.langEsButton = UIFactory.SecondaryButton(langRow, "LangEs", "lang.es", 240);
            c.langEnButton = UIFactory.SecondaryButton(langRow, "LangEn", "lang.en", 240);

            UIFactory.CreateSpacer(s, 8);
            c.closeSettingsButton = UIFactory.PrimaryButton(s, "CloseButton", "settings.close");

            c.settingsPanel = overlay;
            overlay.SetActive(false);

            SaveScene(SceneNames.MainMenu);
        }

        private static void BuildConsent()
        {
            var root = NewScene();
            var c = AddController<ConsentController>();

            // 1. Consentimiento
            var consent = UIFactory.CreateCard(root, "ConsentPanel", 1180, 56, 22);
            Title(consent, "consent.title");
            UIFactory.CreateText(consent, "Body", "consent.body", 23, UITheme.TextPrimary, TextAnchor.UpperLeft);
            c.acceptToggle = UIFactory.CreateToggle(consent, "AcceptToggle", "consent.accept");
            var consentButtons = UIFactory.CreateRow(consent, "Buttons", 24);
            c.declineButton = UIFactory.SecondaryButton(consentButtons, "DeclineButton", "consent.decline", 300);
            c.consentContinueButton = UIFactory.PrimaryButton(consentButtons, "ContinueButton", "common.continue", 300);
            c.consentPanel = consent.gameObject;

            // 2. Perfil previo
            var profile = UIFactory.CreateCard(root, "ProfilePanel", 1000, 56, 22);
            Title(profile, "profile.title");
            c.adultToggle = UIFactory.CreateToggle(profile, "AdultToggle", "profile.adult");
            UIFactory.CreateSpacer(profile, 8);
            UIFactory.CreateText(profile, "GamingQuestion", "profile.gaming", 28, UITheme.TextPrimary, TextAnchor.MiddleCenter);
            c.gamingRating = UIFactory.CreateRating(profile, "GamingRating", "profile.scaleLow", "profile.scaleHigh");
            UIFactory.CreateSpacer(profile, 8);
            UIFactory.CreateText(profile, "PuzzleQuestion", "profile.puzzles", 28, UITheme.TextPrimary, TextAnchor.MiddleCenter);
            c.puzzleRating = UIFactory.CreateRating(profile, "PuzzleRating", "profile.scaleLow", "profile.scaleHigh");
            UIFactory.CreateSpacer(profile, 8);
            var profileButtons = UIFactory.CreateRow(profile, "Buttons", 24);
            c.profileBackButton = UIFactory.SecondaryButton(profileButtons, "BackButton", "common.back", 300);
            c.profileContinueButton = UIFactory.PrimaryButton(profileButtons, "ContinueButton", "common.continue", 300);
            c.profilePanel = profile.gameObject;
            profile.gameObject.SetActive(false);

            // 3. Instrucciones
            var instructions = UIFactory.CreateCard(root, "InstructionsPanel", 1000, 56, 26);
            Title(instructions, "instructions.title");
            c.instructionsBodyText = UIFactory.CreateText(instructions, "Body", null, 28, UITheme.TextPrimary, TextAnchor.UpperLeft,
                literal: Loc.Format("instructions.body", 10, "3"));
            var instructionButtons = UIFactory.CreateRow(instructions, "Buttons", 24);
            c.startButton = UIFactory.PrimaryButton(instructionButtons, "StartButton", "instructions.start", 360);
            c.instructionsPanel = instructions.gameObject;
            instructions.gameObject.SetActive(false);

            SaveScene(SceneNames.Consent);
        }

        private static void BuildGame()
        {
            var root = NewScene();
            var c = AddController<GameController>();
            c.roundedSprite = UIFactory.RoundedSprite;

            // Barra superior
            var top = Bar(root, "TopBar", top: true, height: 110);
            c.headerText = UIFactory.CreateText(top, "Header", null, 34, UITheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold, "Puzzle 1 de 10");
            UIFactory.Layout(c.headerText, flexibleWidth: 1);
            c.timerText = UIFactory.CreateText(top, "Timer", null, 34, UITheme.TextSecondary, TextAnchor.MiddleRight, FontStyle.Bold, "03:00");
            UIFactory.Layout(c.timerText, preferredWidth: 200);

            // Área central: texto de estado arriba y tablero debajo (los puzzles lo llenan en tiempo de ejecución)
            var area = UIFactory.CreateImage(root, "PuzzleArea", UITheme.Surface, true);
            UIFactory.Anchor(area.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -5f), new Vector2(1240f, 760f));
            var shadow = area.gameObject.AddComponent<Shadow>();
            shadow.effectColor = UITheme.Shadow;
            shadow.effectDistance = new Vector2(0f, -6f);

            c.statusText = UIFactory.CreateText(area.transform, "Status", null, 32, UITheme.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold, "Mirá la secuencia");
            UIFactory.Anchor(c.statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(1160f, 90f));

            var board = UIFactory.NewRect("Board", area.transform);
            UIFactory.Stretch(board, 40, 124, 40, 36);
            c.board = board;

            // Depuración: arriba a la izquierda, debajo de la barra superior
            var debug = UIFactory.NewRect("DebugPanel", root);
            UIFactory.Anchor(debug, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -116f), new Vector2(1870f, 56f));
            var debugRow = debug.gameObject.AddComponent<HorizontalLayoutGroup>();
            debugRow.spacing = 12;
            debugRow.childAlignment = TextAnchor.MiddleLeft;
            debugRow.childControlWidth = true;
            debugRow.childControlHeight = true;
            debugRow.childForceExpandWidth = false;
            debugRow.childForceExpandHeight = false;
            c.debugText = UIFactory.CreateText(debug, "DebugText", null, 18, UITheme.TextSecondary, TextAnchor.MiddleLeft, literal: "debug");
            UIFactory.Layout(c.debugText, flexibleWidth: 1);
            c.solveButton = UIFactory.CreateButton(debug, "SolveButton", "game.debugSolve", UITheme.Accent, UITheme.TextOnColor, 230, 48, 20);
            c.errorButton = UIFactory.CreateButton(debug, "ErrorButton", "game.debugError", UITheme.Danger, UITheme.TextOnColor, 200, 48, 20);
            c.attemptButton = UIFactory.CreateButton(debug, "AttemptButton", "game.debugAttempt", UITheme.SurfaceAlt, UITheme.TextPrimary, 260, 48, 20);
            c.skipButton = UIFactory.CreateButton(debug, "SkipButton", "game.debugSkip", UITheme.Warm, UITheme.TextOnColor, 200, 48, 20);
            c.debugPanel = debug.gameObject;

            // Barra inferior
            var bottom = Bar(root, "BottomBar", top: false, height: 120);
            c.hintButton = UIFactory.CreateButton(bottom, "HintButton", "game.hint", UITheme.Warm, UITheme.TextOnColor, 220, 72, 28);
            c.hintCounterText = UIFactory.CreateText(bottom, "HintCounter", null, 26, UITheme.TextSecondary, TextAnchor.MiddleLeft, literal: "2/2");
            UIFactory.Layout(c.hintCounterText, preferredWidth: 80);
            UIFactory.CreateSpacer(bottom, 0, 1);
            c.abandonButton = UIFactory.CreateButton(bottom, "AbandonButton", "game.abandon", UITheme.Danger, UITheme.TextOnColor, 260, 72, 28);

            // Tutorial (primera vez que aparece cada tipo de puzzle)
            var tutorial = UIFactory.CreateOverlay(root, "TutorialPanel", 900, out var tut);
            c.tutorialTitleText = UIFactory.CreateText(tut, "Title", null, 44, UITheme.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold, "Secuencias");
            UIFactory.CreateText(tut, "Subtitle", "tutorial.howTo", 24, UITheme.TextSecondary, TextAnchor.MiddleCenter);
            c.tutorialBodyText = UIFactory.CreateText(tut, "Body", null, 28, UITheme.TextPrimary, TextAnchor.UpperLeft, literal: Loc.Get("tutorial.Sequence"));
            UIFactory.CreateSpacer(tut, 4);
            c.tutorialButton = UIFactory.PrimaryButton(tut, "StartButton", "tutorial.start");
            c.tutorialPanel = tutorial;
            tutorial.SetActive(false);

            // Confirmación de abandono
            var overlay = UIFactory.CreateOverlay(root, "ConfirmPanel", 760, out var confirm);
            UIFactory.CreateText(confirm, "Message", "game.abandonConfirm", 30, UITheme.TextPrimary, TextAnchor.MiddleCenter);
            var confirmButtons = UIFactory.CreateRow(confirm, "Buttons", 24);
            c.confirmNoButton = UIFactory.PrimaryButton(confirmButtons, "NoButton", "game.abandonNo", 280);
            c.confirmYesButton = UIFactory.CreateButton(confirmButtons, "YesButton", "game.abandonYes", UITheme.Danger, UITheme.TextOnColor, 280);
            c.confirmPanel = overlay;
            overlay.SetActive(false);

            SaveScene(SceneNames.Game);
        }

        private static void BuildEnd()
        {
            var root = NewScene();
            var c = AddController<EndController>();

            var card = UIFactory.CreateCard(root, "EndCard", 900, 64, 24);
            c.titleText = UIFactory.CreateText(card, "Title", null, 48, UITheme.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold,
                Loc.Get("end.title"));
            UIFactory.CreateText(card, "Body", "end.body", 28, UITheme.TextSecondary, TextAnchor.MiddleCenter);
            c.codeText = UIFactory.CreateText(card, "Code", null, 30, UITheme.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold,
                Loc.Format("end.code", "XXXXXX"));
            c.statusText = UIFactory.CreateText(card, "Status", null, 24, UITheme.TextSecondary, TextAnchor.MiddleCenter, literal: "");
            var buttons = UIFactory.CreateRow(card, "Buttons", 24);
            c.menuButton = UIFactory.SecondaryButton(buttons, "MenuButton", "end.menu", 360);
            c.quitButton = UIFactory.PrimaryButton(buttons, "QuitButton", "end.quit", 240);

            SaveScene(SceneNames.End);
        }
    }
}
