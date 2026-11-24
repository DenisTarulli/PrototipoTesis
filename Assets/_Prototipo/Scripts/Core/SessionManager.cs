using System;
using UnityEngine;

namespace Prototipo
{
    /// <summary>
    /// Punto central de la sesión experimental. Se crea solo antes de cargar la primera escena
    /// y persiste entre escenas. Asigna la condición, arma el plan de cada slot, guarda el
    /// registro en disco después de cada puzzle y dispara la subida al terminar.
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        public enum UploadState { None, Uploading, Uploaded, Pending }

        public static SessionManager Instance { get; private set; }

        public ExperimentConfig Config { get; private set; }
        public SessionRecord Current { get; private set; }
        public UploadState Upload { get; private set; } = UploadState.None;
        public LocalDataStore Store => store;

        public bool HasActiveSession => Current != null && !Current.completed && !Current.abandoned;
        public int CurrentSlotIndex => Current != null ? Current.puzzles.Count : 0;
        public bool AllSlotsPlayed => Current != null && CurrentSlotIndex >= Config.SlotCount;

        private IDifficultyProvider difficulty;
        private LocalDataStore store;
        private IDataUploader uploader;
        private float sessionStartRealtime;
        private float surveyStartRealtime;
        private Func<PuzzleRecord> inProgressPuzzle;
        private SlotPlan? cachedPlan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[SessionManager]");
            go.AddComponent<SessionManager>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Config = ExperimentConfig.LoadOrDefault();
            store = new LocalDataStore();
            uploader = new NullUploader();
            SettingsService.Load();
            Debug.Log($"[SessionManager] Datos locales en: {store.RootFolder}");
        }

        // ---------------------------------------------------------------- Ciclo de sesión

        public void BeginSession(bool consentGiven, bool isAdult, int priorGaming, int priorPuzzles, bool debugSession = false)
        {
            int seed = Environment.TickCount ^ Guid.NewGuid().GetHashCode();
            var rng = new System.Random(seed);
            var condition = AssignCondition(rng);

            difficulty = condition == DifficultyCondition.Fixed
                ? new FixedDifficultyProvider(Config)
                : (IDifficultyProvider)new AdaptiveDifficultyProvider(Config);

            Current = new SessionRecord
            {
                sessionId = Guid.NewGuid().ToString("N"),
                participantCode = ParticipantCode.Generate(rng),
                condition = condition == DifficultyCondition.Fixed ? "fixed" : "adaptive",
                assignmentMode = Config.assignmentMode.ToString(),
                debugSession = debugSession,
                randomSeed = seed,
                buildLabel = Config.buildLabel,
                appVersion = Application.version,
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                language = Loc.CurrentCode,
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                operatingSystem = SystemInfo.operatingSystem,
                deviceType = SystemInfo.deviceType.ToString(),
                processorType = SystemInfo.processorType,
                processorCount = SystemInfo.processorCount,
                systemMemoryMB = SystemInfo.systemMemorySize,
                graphicsDevice = SystemInfo.graphicsDeviceName,
                graphicsMemoryMB = SystemInfo.graphicsMemorySize,
                screenDpi = Screen.dpi,
                refreshRateHz = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value),
                fullScreenMode = Screen.fullScreenMode.ToString(),
                consentGiven = consentGiven,
                isAdult = isAdult,
                priorGamingExperience = priorGaming,
                priorPuzzleExperience = priorPuzzles,
                startedAtUtc = DateTime.UtcNow.ToString("o"),
                slotCount = Config.SlotCount
            };

            sessionStartRealtime = Time.realtimeSinceStartup;
            cachedPlan = null;
            Upload = UploadState.None;
            Save();
            Debug.Log($"[SessionManager] Sesión {Current.participantCode} iniciada. Condición: {Current.condition}{(debugSession ? " (prueba)" : "")}");
        }

        public SlotPlan GetCurrentSlotPlan()
        {
            // Se calcula una sola vez por slot: el proveedor adaptativo toma una decisión en cada llamada.
            int i = CurrentSlotIndex;
            if (cachedPlan == null || cachedPlan.Value.slotIndex != i)
                cachedPlan = difficulty.Plan(i, Config.slotTypes[i]);
            return cachedPlan.Value;
        }

        /// <summary>
        /// La escena de juego registra acá cómo obtener el puzzle en curso, para poder
        /// guardarlo parcialmente si la sesión se abandona o se cierra la aplicación.
        /// </summary>
        public void SetInProgressPuzzle(Func<PuzzleRecord> getter) => inProgressPuzzle = getter;

        public void RecordPuzzle(PuzzleRecord record)
        {
            if (Current == null || record == null) return;
            var score = PerformanceScorer.Score(record, Config);
            record.performanceScore = score.total;
            record.scoreTime = score.time;
            record.scoreErrors = score.errors;
            record.scoreAttempts = score.attempts;
            record.scoreHints = score.hints;

            Current.puzzles.Add(record);
            if (record.completed) Current.puzzlesCompleted++;
            difficulty?.ReportResult(record);
            UpdateTotalTime();
            Save();
        }

        /// <summary>Terminó el último puzzle y falta la encuesta final.</summary>
        public bool AwaitingSurvey => Current != null && Current.completed && !Current.surveyCompleted &&
                                      string.IsNullOrEmpty(Current.endedAtUtc);

        /// <summary>
        /// Se jugaron todos los slots. El tiempo total de sesión (Anexo B) se fija acá, sin contar la encuesta.
        /// La sesión se cierra y se sube recién al enviar la encuesta.
        /// </summary>
        public void FinishGameplay()
        {
            if (!HasActiveSession) return;
            inProgressPuzzle = null;
            Current.completed = true;
            Current.gameplayEndedAtUtc = DateTime.UtcNow.ToString("o");
            UpdateTotalTime();
            surveyStartRealtime = Time.realtimeSinceStartup;
            Save();
        }

        public void SubmitSurvey(SurveyRecord survey)
        {
            if (!AwaitingSurvey || survey == null) return;
            Current.survey = survey;
            Current.surveyCompleted = true;
            Current.surveySeconds = Time.realtimeSinceStartup - surveyStartRealtime;
            Close(updateTotalTime: false);
        }

        public void AbandonSession()
        {
            if (!HasActiveSession) return;

            var partial = inProgressPuzzle?.Invoke();
            inProgressPuzzle = null;
            if (partial != null) RecordPuzzle(partial);

            Current.abandoned = true;
            Current.abandonedAtSlot = Mathf.Max(1, CurrentSlotIndex);
            Close(updateTotalTime: true);
        }

        /// <summary>Descarta la sesión terminada (solo para volver al menú en pruebas).</summary>
        public void ClearSession()
        {
            if (HasActiveSession) AbandonSession();
            Current = null;
            difficulty = null;
            cachedPlan = null;
            Upload = UploadState.None;
        }

        // ---------------------------------------------------------------- Interno

        private DifficultyCondition AssignCondition(System.Random rng)
        {
            switch (Config.assignmentMode)
            {
                case AssignmentMode.ForceFixed: return DifficultyCondition.Fixed;
                case AssignmentMode.ForceAdaptive: return DifficultyCondition.Adaptive;
                default: return rng.Next(2) == 0 ? DifficultyCondition.Fixed : DifficultyCondition.Adaptive;
            }
        }

        private void Close(bool updateTotalTime)
        {
            inProgressPuzzle = null;
            Current.endedAtUtc = DateTime.UtcNow.ToString("o");
            if (updateTotalTime) UpdateTotalTime();
            Save();
            TryUpload();
        }

        private void UpdateTotalTime()
        {
            Current.totalSessionSeconds = Time.realtimeSinceStartup - sessionStartRealtime;
        }

        private void Save() => store.Save(Current);

        private void TryUpload()
        {
            var record = Current;
            Upload = UploadState.Uploading;
            StartCoroutine(uploader.Upload(record, ok =>
            {
                if (ok) store.MarkUploaded(record.sessionId);
                Upload = ok ? UploadState.Uploaded : UploadState.Pending;
            }));
        }

        private void OnApplicationQuit()
        {
            // Cerrar la ventana a mitad de la sesión cuenta como abandono. Queda guardado en disco.
            if (HasActiveSession) AbandonSession();

            // Cerrar durante la encuesta: el juego quedó completo pero sin encuesta. Se guarda así
            // (surveyCompleted = false) y queda pendiente de subida.
            else if (AwaitingSurvey)
            {
                Current.endedAtUtc = DateTime.UtcNow.ToString("o");
                Save();
            }
        }
    }
}
