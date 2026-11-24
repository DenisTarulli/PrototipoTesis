using UnityEngine;
using UnityEngine.UI;

namespace Prototipo
{
    /// <summary>
    /// ETAPA 1: escena de juego con un simulador en lugar de puzzles reales.
    /// Sirve para probar el flujo completo (slots, tiempos, pistas, abandono, guardado).
    /// En la etapa 2, los puzzles reales reemplazan el panel central y reportan
    /// los mismos eventos al PuzzleMetricsTracker.
    /// </summary>
    public class GamePlaceholderController : MonoBehaviour
    {
        [Header("Barra superior")]
        public Text headerText;
        public Text timerText;

        [Header("Área de puzzle")]
        public Text infoText;
        public GameObject debugPanel;
        public Button solveButton;
        public Button errorButton;
        public Button attemptButton;

        [Header("Barra inferior")]
        public Button hintButton;
        public Text hintCounterText;
        public Button abandonButton;

        [Header("Confirmación de abandono")]
        public GameObject confirmPanel;
        public Button confirmYesButton;
        public Button confirmNoButton;

        private SessionManager session;
        private readonly PuzzleMetricsTracker tracker = new PuzzleMetricsTracker();
        private SlotPlan plan;

        private void Start()
        {
            session = SessionManager.Instance;

            // Permite apretar Play directamente en esta escena durante el desarrollo.
            if (!session.HasActiveSession)
            {
                Debug.LogWarning("[Game] No había sesión activa: se inicia una sesión de PRUEBA (debugSession = true).");
                session.BeginSession(true, true, 0, 0, debugSession: true);
            }

            confirmPanel.SetActive(false);
            debugPanel.SetActive(session.Config.showDebugInfo);

            solveButton.onClick.AddListener(() => EndSlot(solved: true, timedOut: false));
            errorButton.onClick.AddListener(tracker.RegisterError);
            attemptButton.onClick.AddListener(tracker.RegisterNewAttempt);
            hintButton.onClick.AddListener(UseHint);
            abandonButton.onClick.AddListener(() => confirmPanel.SetActive(true));
            confirmNoButton.onClick.AddListener(() => confirmPanel.SetActive(false));
            confirmYesButton.onClick.AddListener(Abandon);

            session.SetInProgressPuzzle(() => tracker.IsRunning ? tracker.Abandon() : null);
            StartSlot();
        }

        private void StartSlot()
        {
            plan = session.GetCurrentSlotPlan();
            tracker.Begin(plan);

            headerText.text = Loc.Format("game.header", plan.slotIndex + 1, session.Config.SlotCount);
            string typeName = Loc.Get("puzzle." + plan.type);
            infoText.text = session.Config.showDebugInfo
                ? $"{typeName}\n<size=24>tier {plan.tier} · {plan.source} · {session.Current.condition}</size>"
                : typeName;

            RefreshHint();
        }

        private void Update()
        {
            if (!tracker.IsRunning) return;

            float remaining = session.Config.puzzleTimeLimitSeconds - tracker.Elapsed;
            if (remaining <= 0f)
            {
                EndSlot(solved: false, timedOut: true);
                return;
            }

            int secs = Mathf.CeilToInt(remaining);
            timerText.text = $"{secs / 60:00}:{secs % 60:00}";
        }

        private void UseHint()
        {
            if (tracker.HintsUsed >= session.Config.maxHintsPerPuzzle) return;
            tracker.RegisterHint();
            // Etapa 2: acá el puzzle muestra la pista concreta.
            RefreshHint();
        }

        private void RefreshHint()
        {
            int max = session.Config.maxHintsPerPuzzle;
            hintButton.gameObject.SetActive(max > 0);
            hintButton.interactable = tracker.HintsUsed < max;
            if (hintCounterText != null) hintCounterText.text = $"{max - tracker.HintsUsed}/{max}";
        }

        private void EndSlot(bool solved, bool timedOut)
        {
            var record = tracker.Complete(solved, timedOut);
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

        private void Abandon()
        {
            confirmPanel.SetActive(false);
            session.AbandonSession(); // guarda el puzzle en curso como abandonado
            SceneNames.Load(SceneNames.End);
        }

        private void OnDestroy()
        {
            if (session != null) session.SetInProgressPuzzle(null);
        }
    }
}
