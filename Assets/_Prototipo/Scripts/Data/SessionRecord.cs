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
        public const string CurrentSchemaVersion = "1";

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
        public float performanceScore = -1f; // puntaje usado por el sistema adaptativo (-1 = no aplica)
    }
}
