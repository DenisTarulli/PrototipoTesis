using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Prototipo
{
    /// <summary>
    /// Escena de juego. Recorre los slots de la sesión: para cada uno pide el plan (tipo + tier),
    /// arma el puzzle correspondiente, muestra el tutorial la primera vez que aparece cada tipo,
    /// mide las métricas y guarda el resultado.
    ///
    /// Los tipos de puzzle que todavía no están implementados usan el simulador (botones de depuración).
    /// </summary>
    public class GameController : MonoBehaviour, IPuzzleHost
    {
        [Header("Barra superior")]
        public Text headerText;
        public Text timerText;

        [Header("Área de puzzle")]
        public Text statusText;
        public RectTransform board;
        public Sprite roundedSprite;

        [Header("Depuración (solo con showDebugInfo)")]
        public GameObject debugPanel;
        public Text debugText;
        public Button solveButton;
        public Button errorButton;
        public Button attemptButton;

        [Header("Barra inferior")]
        public Button hintButton;
        public Text hintCounterText;
        public Button abandonButton;

        [Header("Tutorial")]
        public GameObject tutorialPanel;
        public Text tutorialTitleText;
        public Text tutorialBodyText;
        public Button tutorialButton;

        [Header("Confirmación de abandono")]
        public GameObject confirmPanel;
        public Button confirmYesButton;
        public Button confirmNoButton;

        public RuntimeUI UI { get; private set; }

        private SessionManager session;
        private readonly PuzzleMetricsTracker tracker = new PuzzleMetricsTracker();
        private readonly HashSet<PuzzleType> tutorialsShown = new HashSet<PuzzleType>();
        private SlotPlan plan;
        private PuzzleBase puzzle;
        private bool slotEnding;

        private const float PauseAfterSolve = 1.0f;
        private const float PauseAfterTimeout = 1.8f;

        private void Start()
        {
            session = SessionManager.Instance;
            UI = new RuntimeUI(roundedSprite);

            // Permite apretar Play directamente en esta escena durante el desarrollo.
            if (!session.HasActiveSession)
            {
                Debug.LogWarning("[Game] No había sesión activa: se inicia una sesión de PRUEBA (debugSession = true).");
                session.BeginSession(true, true, 0, 0, debugSession: true);
            }

            confirmPanel.SetActive(false);
            tutorialPanel.SetActive(false);
            debugPanel.SetActive(session.Config.showDebugInfo);

            solveButton.onClick.AddListener(() => { if (tracker.IsRunning) ReportSolved(); });
            errorButton.onClick.AddListener(ReportError);
            attemptButton.onClick.AddListener(ReportNewAttempt);
            hintButton.onClick.AddListener(UseHint);
            abandonButton.onClick.AddListener(() => confirmPanel.SetActive(true));
            confirmNoButton.onClick.AddListener(() => confirmPanel.SetActive(false));
            confirmYesButton.onClick.AddListener(Abandon);
            tutorialButton.onClick.AddListener(CloseTutorial);

            session.SetInProgressPuzzle(() => tracker.IsRunning ? tracker.Abandon() : null);
            StartSlot();
        }

        // ------------------------------------------------------------ Slots

        private void StartSlot()
        {
            slotEnding = false;
            plan = session.GetCurrentSlotPlan();

            if (puzzle != null) Destroy(puzzle);
            puzzle = null;
            RuntimeUI.Clear(board);
            SetStatus(string.Empty);

            headerText.text = Loc.Format("game.header", plan.slotIndex + 1, session.Config.SlotCount);
            ShowTime(session.Config.puzzleTimeLimitSeconds);
            RefreshDebug();

            puzzle = CreatePuzzle(plan.type);
            if (puzzle != null)
            {
                // Semilla derivada de la sesión: el mismo participante vería los mismos tableros.
                var rng = new System.Random(session.Current.randomSeed + 7919 * (plan.slotIndex + 1));
                puzzle.Init(this, board, plan.tier, rng);
            }
            else
            {
                SetStatus(Loc.Get("puzzle." + plan.type) + "\n<size=24>" + Loc.Get("game.notImplemented") + "</size>");
            }

            if (!tutorialsShown.Contains(plan.type)) ShowTutorial(plan.type);
            else BeginPuzzle();
        }

        private PuzzleBase CreatePuzzle(PuzzleType type)
        {
            switch (type)
            {
                case PuzzleType.Sequence: return gameObject.AddComponent<SequencePuzzle>();
                case PuzzleType.MemoryPairs: return gameObject.AddComponent<MemoryPairsPuzzle>();
                default: return null; // todavía no implementado: se usa el simulador
            }
        }

        private void BeginPuzzle()
        {
            tracker.Begin(plan);
            RefreshHint();
            if (puzzle != null) puzzle.Begin();
        }

        private void ShowTutorial(PuzzleType type)
        {
            tutorialsShown.Add(type);
            tutorialTitleText.text = Loc.Get("puzzle." + type);
            tutorialBodyText.text = Loc.Get("tutorial." + type);
            tutorialPanel.SetActive(true);
            RefreshHint();
        }

        private void CloseTutorial()
        {
            tutorialPanel.SetActive(false);
            BeginPuzzle();
        }

        private void Update()
        {
            if (!tracker.IsRunning || slotEnding) return;

            float remaining = session.Config.puzzleTimeLimitSeconds - tracker.Elapsed;
            if (remaining <= 0f)
            {
                StartCoroutine(EndSlot(solved: false, timedOut: true));
                return;
            }
            ShowTime(remaining);
            RefreshHint();
        }

        private IEnumerator EndSlot(bool solved, bool timedOut)
        {
            if (slotEnding) yield break;
            slotEnding = true;

            var record = tracker.Complete(solved, timedOut); // el tiempo se corta acá, no después de la pausa
            if (puzzle != null) puzzle.Stop();
            RefreshHint();

            if (timedOut)
            {
                ShowTime(0f);
                SetStatus(Loc.Get("game.timeUp"));
            }
            else if (puzzle == null)
            {
                SetStatus(Loc.Get("game.solved"));
            }

            yield return new WaitForSeconds(timedOut ? PauseAfterTimeout : PauseAfterSolve);

            session.RecordPuzzle(record);
            if (session.AllSlotsPlayed)
            {
                session.FinishSession();
                SceneNames.Load(SceneNames.End);
            }
            else
            {
                StartSlot();
            }
        }

        // ------------------------------------------------------------ IPuzzleHost

        public void ReportError()
        {
            tracker.RegisterError();
            RefreshDebug();
        }

        public void ReportNewAttempt()
        {
            tracker.RegisterNewAttempt();
            RefreshDebug();
        }

        public void ReportSolved()
        {
            if (!tracker.IsRunning) return;
            StartCoroutine(EndSlot(solved: true, timedOut: false));
        }

        public void SetStatus(string text) => statusText.text = text;

        // ------------------------------------------------------------ Pistas y abandono

        private void UseHint()
        {
            if (!tracker.IsRunning || slotEnding) return;
            if (tracker.HintsUsed >= session.Config.maxHintsPerPuzzle) return;

            // El puzzle decide si en este momento puede dar una pista; si no, no se cuenta.
            bool given = puzzle == null || puzzle.TryHint();
            if (!given) return;

            tracker.RegisterHint();
            RefreshHint();
            RefreshDebug();
        }

        private void RefreshHint()
        {
            int max = session.Config.maxHintsPerPuzzle;
            hintButton.gameObject.SetActive(max > 0);
            hintButton.interactable = tracker.IsRunning && !slotEnding && tracker.HintsUsed < max &&
                                      (puzzle == null || puzzle.CanHint);
            if (hintCounterText != null) hintCounterText.text = $"{max - tracker.HintsUsed}/{max}";
        }

        private void Abandon()
        {
            confirmPanel.SetActive(false);
            if (puzzle != null) puzzle.Stop();
            session.AbandonSession(); // guarda el puzzle en curso como abandonado
            SceneNames.Load(SceneNames.End);
        }

        // ------------------------------------------------------------ Visual

        private void ShowTime(float seconds)
        {
            int secs = Mathf.Max(0, Mathf.CeilToInt(seconds));
            timerText.text = $"{secs / 60:00}:{secs % 60:00}";
        }

        private void RefreshDebug()
        {
            if (!session.Config.showDebugInfo || debugText == null) return;
            var puzzles = session.Current.puzzles;
            string last = puzzles.Count > 0 ? puzzles[puzzles.Count - 1].performanceScore.ToString("0.00") : "-";
            string avg = plan.windowAverage >= 0f ? plan.windowAverage.ToString("0.00") : "-";
            debugText.text =
                $"{session.Current.condition} · tier {plan.tier} ({plan.source}, {plan.decision} {plan.previousTier}→{plan.tier})\n" +
                $"último puntaje {last} · promedio ventana {avg} · errores {tracker.Errors} · pistas {tracker.HintsUsed}";
        }

        private void OnDestroy()
        {
            if (session != null) session.SetInProgressPuzzle(null);
        }
    }
}
