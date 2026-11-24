using System;
using System.Collections.Generic;

namespace Prototipo
{
    /// <summary>
    /// Registro completo de una sesión. Se serializa a JSON con JsonUtility,
    /// por eso todos los campos son públicos y de tipos simples.
    /// Corresponde a las variables del Anexo B de la tesis.
    /// </summary>
    [Serializable]
    public class SessionRecord
    {
        public const string CurrentSchemaVersion = "3";

        // Identificación (anónima)
        public string schemaVersion = CurrentSchemaVersion;
        public string sessionId;
        public string participantCode;
        public string condition;          // "fixed" | "adaptive"
        public string assignmentMode;
        public bool debugSession;         // true si se inició sin pasar por el consentimiento (pruebas)
        public int randomSeed;

        // Entorno
        public string buildLabel;
        public string appVersion;
        public string unityVersion;
        public string platform;
        public string language;
        public int screenWidth;
        public int screenHeight;

        // Hardware (no identifica a la persona; sirve para controlar la variable de confusión "hardware")
        public string operatingSystem;
        public string deviceType;
        public string processorType;
        public int processorCount;
        public int systemMemoryMB;
        public string graphicsDevice;
        public int graphicsMemoryMB;
        public float screenDpi;
        public int refreshRateHz;
        public string fullScreenMode;

        // Consentimiento y perfil previo
        public bool consentGiven;
        public bool isAdult;
        public int priorGamingExperience;   // 1-5
        public int priorPuzzleExperience;   // 1-5

        // Tiempos
        public string startedAtUtc;
        public string endedAtUtc;
        public float totalSessionSeconds;

        // Resultado
        public int slotCount;
        public int puzzlesCompleted;
        public bool completed;
        public bool abandoned;
        public int abandonedAtSlot = -1;   // número de slot (1-based) donde abandonó

        public List<PuzzleRecord> puzzles = new List<PuzzleRecord>();

        // Encuesta final (Anexo A). totalSessionSeconds NO incluye el tiempo de la encuesta.
        public string gameplayEndedAtUtc;
        public bool surveyCompleted;
        public float surveySeconds;
        public SurveyRecord survey = new SurveyRecord();
    }

    /// <summary>
    /// Respuestas de la encuesta del Anexo A. Las escalas van de 1 a 5 (Anexo C); 0 = sin respuesta.
    /// Las opciones cerradas se guardan como códigos en inglés, independientes del idioma del juego.
    /// </summary>
    [Serializable]
    public class SurveyRecord
    {
        public int q01_entertaining;        // ¿Qué tan entretenida te resultó la experiencia?
        public int q02_challenging;         // ¿Qué tan desafiante consideraste el nivel de dificultad?
        public int q03_matchedSkills;       // ¿La dificultad se ajustó adecuadamente a tus habilidades?
        public int q04_frustration;         // ¿Experimentaste momentos de frustración?
        public int q05_boredom;             // ¿Sentiste aburrimiento debido a una baja dificultad?
        public int q06_consistent;          // ¿El nivel de dificultad fue consistente?
        public int q07_satisfaction;        // ¿Qué tan satisfecho/a estás con la experiencia general?
        public int q08_tooHard;             // ¿Sentiste que el juego era demasiado difícil?
        public int q09_tooEasy;             // ¿Sentiste que el juego era demasiado fácil?
        public int q10_progress;            // ¿Sentiste que progresabas?
        public int q11_motivation;          // ¿Qué tan motivado/a te sentiste para continuar?
        public int q12_attention;           // ¿El juego mantuvo tu atención?
        public int q13_hintsUseful;         // 1-5; -1 = "No utilicé ayudas"
        public string q14_noticedChanges;   // "yes" | "no" | "unsure"
        public int q15_changesNatural;      // 1-5; -1 = no aplica (no percibió cambios)
        public int q16_rewarded;            // ¿El juego recompensó adecuadamente tu desempeño?
        public string q17_playAgain;        // "yes" | "no" | "maybe"
        public int q18_recommend;           // ¿Recomendarías este juego?
        public string q19_enjoyedMost;      // abierta
        public string q20_improve;          // abierta
    }

    /// <summary>Métricas de un puzzle individual.</summary>
    [Serializable]
    public class PuzzleRecord
    {
        public int slot;                  // 1-based
        public string puzzleType;
        public int difficultyTier;        // nivel de dificultad aplicado
        public string difficultySource;   // "fixed_ramp" | "calibration" | "adaptive"
        public string startedAtUtc;
        public float timeSeconds;         // tiempo de resolución
        public int errors;                // cantidad de errores
        public int attempts;              // cantidad de intentos (empieza en 1)
        public int hintsUsed;             // uso de ayudas
        public bool completed;            // puzzle completado
        public bool timedOut;             // se agotó el tiempo
        public bool abandoned;            // el participante abandonó durante este puzzle
        public bool debugSkipped;         // DESARROLLO: salteado con el botón de depuración (nunca en el experimento)

        // Eficiencia (solo en puzzles de movimientos: Hanoi, Fichas). -1 = no aplica.
        // No entra en el puntaje adaptativo (la tesis usa tiempo, errores, intentos y ayudas); queda para el análisis.
        public int moves = -1;            // movimientos válidos realizados
        public int optimalMoves = -1;     // mínimo teórico (Hanoi) o referencia del tablero (Fichas)

        // Puntaje de desempeño 0-1 (se calcula en ambas condiciones; solo la adaptativa lo usa para decidir)
        public float performanceScore = -1f;
        public float scoreTime = -1f;
        public float scoreErrors = -1f;
        public float scoreAttempts = -1f;
        public float scoreHints = -1f;

        // Decisión tomada al asignar el tier de ESTE puzzle (condición adaptativa)
        public int previousTier;            // tier del puzzle anterior (0 en el primero)
        public float decisionWindowAverage = -1f; // promedio de la ventana usado para decidir (-1 = no hubo decisión)
        public string decision;             // "up" | "down" | "keep" | "clamped_up" | "clamped_down" | "none"
    }
}
