using UnityEngine;
using UnityEngine.UI;

namespace Prototipo
{
    /// <summary>
    /// Encuesta final (Anexo A), en 5 páginas de 4 preguntas. Las preguntas 1 a 18 son obligatorias
    /// (la 15 solo si en la 14 respondió "Sí"); la 19 y la 20 son abiertas y opcionales.
    /// Al enviar, las respuestas se guardan en el registro de la sesión y recién ahí se sube.
    /// </summary>
    public class SurveyController : MonoBehaviour
    {
        public const int QuestionCount = 20;
        public static readonly int[][] Pages =
        {
            new[] { 1, 2, 3, 4 },
            new[] { 5, 6, 7, 8 },
            new[] { 9, 10, 11, 12 },
            new[] { 13, 14, 15, 16 },
            new[] { 17, 18, 19, 20 },
        };

        // Valores de las preguntas cerradas que no son escala
        public const int Yes = 1, No = 2, Third = 3; // Third = "No estoy seguro" (q14) o "Tal vez" (q17)
        public const int NoHintsUsed = -1;

        [Tooltip("Una página por elemento, en orden.")]
        public GameObject[] pagePanels;

        [Tooltip("Selector de cada pregunta cerrada; índice 0 = pregunta 1. Las abiertas (19, 20) quedan vacías.")]
        public ChoiceSelector[] selectors = new ChoiceSelector[QuestionCount];

        public InputField q19Input;
        public InputField q20Input;

        public Text progressText;
        public Text warningText;
        public Button backButton;
        public Button nextButton;
        public Text nextButtonLabel;

        private SessionManager session;
        private int page;

        private void Start()
        {
            session = SessionManager.Instance;

            if (!session.AwaitingSurvey)
            {
                if (session.Config.showDebugInfo)
                {
                    // Permite apretar Play directamente en esta escena durante el desarrollo.
                    Debug.LogWarning("[Survey] No había una sesión esperando la encuesta: se crea una de PRUEBA.");
                    session.BeginSession(true, true, 0, 0, debugSession: true);
                    session.FinishGameplay();
                }
                else
                {
                    SceneNames.Load(SceneNames.End);
                    return;
                }
            }

            backButton.onClick.AddListener(Back);
            nextButton.onClick.AddListener(Next);
            Selector(14).ValueChanged += _ => UpdateConditional();
            UpdateConditional();
            ShowPage(0);
        }

        private ChoiceSelector Selector(int question) => selectors[question - 1];

        private void UpdateConditional()
        {
            // La 15 ("¿te parecieron naturales?") solo aplica si percibió cambios.
            bool applies = Selector(14).HasValue && Selector(14).Value == Yes;
            if (!applies) Selector(15).Clear();
            Selector(15).SetInteractable(applies);
        }

        private bool IsRequired(int q)
        {
            if (q >= 19) return false;
            if (q == 15) return Selector(14).HasValue && Selector(14).Value == Yes;
            return true;
        }

        private bool PageComplete(int p)
        {
            foreach (int q in Pages[p])
                if (IsRequired(q) && !Selector(q).HasValue) return false;
            return true;
        }

        private void ShowPage(int p)
        {
            page = p;
            for (int i = 0; i < pagePanels.Length; i++) pagePanels[i].SetActive(i == p);
            progressText.text = Loc.Format("survey.page", p + 1, Pages.Length);
            warningText.text = string.Empty;
            backButton.gameObject.SetActive(p > 0);
            nextButtonLabel.text = Loc.Get(p == Pages.Length - 1 ? "survey.submit" : "survey.next");
        }

        private void Back()
        {
            if (page > 0) ShowPage(page - 1);
        }

        private void Next()
        {
            if (!PageComplete(page))
            {
                warningText.text = Loc.Get("survey.required");
                return;
            }

            if (page < Pages.Length - 1)
            {
                ShowPage(page + 1);
                return;
            }

            Submit();
        }

        private void Submit()
        {
            nextButton.interactable = false;
            var s = new SurveyRecord
            {
                q01_entertaining = Selector(1).Value,
                q02_challenging = Selector(2).Value,
                q03_matchedSkills = Selector(3).Value,
                q04_frustration = Selector(4).Value,
                q05_boredom = Selector(5).Value,
                q06_consistent = Selector(6).Value,
                q07_satisfaction = Selector(7).Value,
                q08_tooHard = Selector(8).Value,
                q09_tooEasy = Selector(9).Value,
                q10_progress = Selector(10).Value,
                q11_motivation = Selector(11).Value,
                q12_attention = Selector(12).Value,
                q13_hintsUseful = Selector(13).Value,
                q14_noticedChanges = Code(Selector(14).Value, "unsure"),
                q15_changesNatural = Selector(15).HasValue ? Selector(15).Value : -1,
                q16_rewarded = Selector(16).Value,
                q17_playAgain = Code(Selector(17).Value, "maybe"),
                q18_recommend = Selector(18).Value,
                q19_enjoyedMost = Clean(q19Input.text),
                q20_improve = Clean(q20Input.text),
            };

            session.SubmitSurvey(s);
            SceneNames.Load(SceneNames.End);
        }

        private static string Code(int value, string third)
        {
            switch (value)
            {
                case Yes: return "yes";
                case No: return "no";
                case Third: return third;
                default: return string.Empty;
            }
        }

        private static string Clean(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            text = text.Trim();
            return text.Length > 1000 ? text.Substring(0, 1000) : text;
        }
    }
}
