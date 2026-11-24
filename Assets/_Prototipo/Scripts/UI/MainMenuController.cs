using UnityEngine;
using UnityEngine.UI;

namespace Prototipo
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Menú")]
        public Button playButton;
        public Button settingsButton;
        public Button quitButton;
        public Text versionText;

        [Header("Ajustes")]
        public GameObject settingsPanel;
        public Slider volumeSlider;
        public Text volumeValueText;
        public Button langEsButton;
        public Button langEnButton;
        public Button closeSettingsButton;

        private void Start()
        {
            var session = SessionManager.Instance;
            // Si volvemos al menú después de una sesión (solo en pruebas), se descarta.
            if (session.Current != null && !session.HasActiveSession) session.ClearSession();

            settingsPanel.SetActive(false);

            playButton.onClick.AddListener(() => SceneNames.Load(SceneNames.Consent));
            settingsButton.onClick.AddListener(() => settingsPanel.SetActive(true));
            closeSettingsButton.onClick.AddListener(() => settingsPanel.SetActive(false));
            quitButton.onClick.AddListener(Quit);

            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.SetValueWithoutNotify(SettingsService.Volume);
            volumeSlider.onValueChanged.AddListener(v =>
            {
                SettingsService.SetVolume(v);
                RefreshVolumeLabel();
            });
            RefreshVolumeLabel();

            langEsButton.onClick.AddListener(() => SettingsService.SetLanguage("es"));
            langEnButton.onClick.AddListener(() => SettingsService.SetLanguage("en"));
            Loc.LanguageChanged += RefreshLanguageButtons;
            RefreshLanguageButtons();

            if (versionText != null) versionText.text = "v" + session.Config.buildLabel;
        }

        private void OnDestroy()
        {
            Loc.LanguageChanged -= RefreshLanguageButtons;
        }

        private void RefreshVolumeLabel()
        {
            volumeValueText.text = Mathf.RoundToInt(SettingsService.Volume * 100f) + "%";
        }

        private void RefreshLanguageButtons()
        {
            Highlight(langEsButton, Loc.CurrentCode == "es");
            Highlight(langEnButton, Loc.CurrentCode == "en");
        }

        private static void Highlight(Button button, bool selected)
        {
            if (button.targetGraphic is Image img) img.color = selected ? UITheme.Primary : UITheme.SurfaceAlt;
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.color = selected ? UITheme.TextOnColor : UITheme.TextPrimary;
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
