using UnityEngine;
using UnityEngine.UI;

namespace Prototipo
{
    /// <summary>Consentimiento informado, perfil previo e instrucciones. Al final inicia la sesión.</summary>
    public class ConsentController : MonoBehaviour
    {
        [Header("Consentimiento")]
        public GameObject consentPanel;
        public Toggle acceptToggle;
        public Button consentContinueButton;
        public Button declineButton;

        [Header("Perfil previo")]
        public GameObject profilePanel;
        public Toggle adultToggle;
        public RatingSelector gamingRating;
        public RatingSelector puzzleRating;
        public Button profileBackButton;
        public Button profileContinueButton;

        [Header("Instrucciones")]
        public GameObject instructionsPanel;
        public Text instructionsBodyText;
        public Button startButton;

        private void Start()
        {
            acceptToggle.isOn = false;
            adultToggle.isOn = false;

            acceptToggle.onValueChanged.AddListener(_ => ValidateConsent());
            declineButton.onClick.AddListener(() => SceneNames.Load(SceneNames.MainMenu));
            consentContinueButton.onClick.AddListener(() => Show(profilePanel));

            adultToggle.onValueChanged.AddListener(_ => ValidateProfile());
            gamingRating.ValueChanged += _ => ValidateProfile();
            puzzleRating.ValueChanged += _ => ValidateProfile();
            profileBackButton.onClick.AddListener(() => Show(consentPanel));
            profileContinueButton.onClick.AddListener(ShowInstructions);

            startButton.onClick.AddListener(StartSession);

            ValidateConsent();
            ValidateProfile();
            Show(consentPanel);
        }

        private void Show(GameObject panel)
        {
            consentPanel.SetActive(panel == consentPanel);
            profilePanel.SetActive(panel == profilePanel);
            instructionsPanel.SetActive(panel == instructionsPanel);
        }

        private void ValidateConsent()
        {
            consentContinueButton.interactable = acceptToggle.isOn;
        }

        private void ValidateProfile()
        {
            profileContinueButton.interactable =
                adultToggle.isOn && gamingRating.Value > 0 && puzzleRating.Value > 0;
        }

        private void ShowInstructions()
        {
            var config = SessionManager.Instance.Config;
            string minutes = (config.puzzleTimeLimitSeconds / 60f).ToString("0.#");
            instructionsBodyText.text = Loc.Format("instructions.body", config.SlotCount, minutes);
            Show(instructionsPanel);
        }

        private void StartSession()
        {
            startButton.interactable = false;
            SessionManager.Instance.BeginSession(
                consentGiven: true,
                isAdult: adultToggle.isOn,
                priorGaming: gamingRating.Value,
                priorPuzzles: puzzleRating.Value);
            SceneNames.Load(SceneNames.Game);
        }
    }
}
