using UnityEngine;

namespace Prototipo
{
    /// <summary>
    /// Calcula el puntaje de desempeño (0 a 1) de un puzzle a partir de las cuatro métricas de la
    /// tesis: tiempo de resolución, errores, intentos y uso de ayudas. Es una suma ponderada de
    /// cuatro sub-puntajes lineales, para que cada decisión del sistema se pueda explicar a mano.
    ///
    /// Un puzzle no completado (tiempo agotado o abandono) vale 0.
    /// </summary>
    public static class PerformanceScorer
    {
        public struct Breakdown
        {
            public float time, errors, attempts, hints, total;
        }

        public static Breakdown Score(PuzzleRecord r, ExperimentConfig config)
        {
            var b = new Breakdown();
            if (r == null || !r.completed) return b; // todo en 0

            var type = ParseType(r.puzzleType);
            var tuning = config.GetTuning(type);
            var w = config.scoreWeights;

            // Tiempo: 1 si resolvió en la mitad del tiempo esperado o menos, 0 si tardó el doble o más.
            float target = Mathf.Max(1f, tuning.GetTargetSeconds(r.difficultyTier));
            b.time = Mathf.Clamp01((2f * target - r.timeSeconds) / (1.5f * target));

            // Errores: baja linealmente hasta 0 al llegar a la tolerancia del puzzle/tier.
            float tol = Mathf.Max(1f, tuning.GetErrorTolerance(r.difficultyTier));
            b.errors = 1f - Mathf.Clamp01(r.errors / tol);

            // Intentos: el primero es gratis; cada reintento resta hasta la tolerancia configurada.
            float attTol = Mathf.Max(1, config.attemptTolerance);
            b.attempts = 1f - Mathf.Clamp01((r.attempts - 1) / attTol);

            // Ayudas: proporción de pistas no usadas.
            int maxHints = config.maxHintsPerPuzzle;
            b.hints = maxHints > 0 ? 1f - Mathf.Clamp01((float)r.hintsUsed / maxHints) : 1f;

            float wSum = w.time + w.errors + w.attempts + w.hints;
            if (wSum <= 0f) wSum = 1f;
            b.total = (w.time * b.time + w.errors * b.errors + w.attempts * b.attempts + w.hints * b.hints) / wSum;
            return b;
        }

        private static PuzzleType ParseType(string s)
        {
            return System.Enum.TryParse(s, out PuzzleType t) ? t : PuzzleType.Sequence;
        }
    }
}
