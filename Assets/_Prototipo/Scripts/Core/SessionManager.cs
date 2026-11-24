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
        private Func<PuzzleRecord> inProgressPuzzle;

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
                consentGiven = consentGiven,
                isAdult = isAdult,
                priorGamingExperience = priorGaming,
                priorPuzzleExperience = priorPuzzles,
                startedAtUtc = DateTime.UtcNow.ToString("o"),
                slotCount = Config.SlotCount
            };

            sessionStartRealtime = Time.realtimeSinceStartup;
            Upload = UploadState.None;
            Save();
            Debug.Log($"[SessionManager] Sesión {Current.participantCode} iniciada. Condición: {Current.condition}{(debugSession ? " (prueba)" : "")}");
        }

        public SlotPlan GetCurrentSlotPlan()
        {
            int i = CurrentSlotIndex;
            return difficulty.Plan(i, Config.slotTypes[i]);
        }

        /// <summary>
        /// La escena de juego registra acá cómo obtener el puzzle en curso, para poder
        /// guardarlo parcialmente si la sesión se abandona o se cierra la aplicación.
        /// </summary>
        public void SetInProgressPuzzle(Func<PuzzleRecord> getter) => inProgressPuzzle = getter;

        public void RecordPuzzle(PuzzleRecord record)
        {
            if (Current == null || record == null) return;
            Current.puzzles.Add(record);
            if (record.completed) Current.puzzlesCompleted++;
            difficulty?.ReportResult(record);
            UpdateTotalTime();
            Save();
        }

        public void FinishSession()
        {
            if (!HasActiveSession) return;
            Current.completed = true;
            Close();
        }

        public void AbandonSession()
        {
            if (!HasActiveSession) return;

            var partial = inProgressPuzzle?.Invoke();
            inProgressPuzzle = null;
            if (partial != null) RecordPuzzle(partial);

            Current.abandoned = true;
            Current.abandonedAtSlot = Mathf.Max(1, CurrentSlotIndex);
            Close();
        }

        /// <summary>Descarta la sesión terminada (solo para volver al menú en pruebas).</summary>
        public void ClearSession()
        {
            if (HasActiveSession) AbandonSession();
            Current = null;
            difficulty = null;
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

        private void Close()
        {
            inProgressPuzzle = null;
            Current.endedAtUtc = DateTime.UtcNow.ToString("o");
            UpdateTotalTime();
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
        }
    }
}
