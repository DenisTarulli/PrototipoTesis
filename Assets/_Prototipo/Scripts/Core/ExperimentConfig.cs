using System;
using UnityEngine;

namespace Prototipo
{
    /// <summary>
    /// Parámetros del experimento. El asset vive en Assets/_Prototipo/Resources/ExperimentConfig.asset
    /// y se edita desde el Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Prototipo/Experiment Config", fileName = "ExperimentConfig")]
    public class ExperimentConfig : ScriptableObject
    {
        public const string ResourcePath = "ExperimentConfig";

        [Header("Asignación de condición")]
        public AssignmentMode assignmentMode = AssignmentMode.Random;

        [Header("Estructura de la sesión")]
        [Tooltip("Tipo de puzzle de cada slot. Igual orden para ambas condiciones.")]
        public PuzzleType[] slotTypes =
        {
            PuzzleType.Sequence, PuzzleType.MemoryPairs, PuzzleType.Hanoi, PuzzleType.SlidingTiles,
            PuzzleType.Sequence, PuzzleType.MemoryPairs, PuzzleType.Hanoi, PuzzleType.SlidingTiles,
            PuzzleType.Sequence, PuzzleType.MemoryPairs
        };

        [Tooltip("Tier de dificultad de cada slot en la condición FIJA (rampa predefinida).")]
        public int[] fixedTiers = { 1, 1, 2, 2, 3, 3, 3, 4, 4, 5 };

        [Tooltip("Cantidad de slots iniciales idénticos en ambas condiciones (calibración).")]
        [Range(0, 10)] public int calibrationSlots = 2;

        [Range(1, 5)] public int minTier = 1;
        [Range(1, 5)] public int maxTier = 5;

        [Header("Reglas por puzzle")]
        [Tooltip("Tiempo máximo por puzzle en segundos. Al agotarse se registra como no completado.")]
        public float puzzleTimeLimitSeconds = 180f;

        [Tooltip("Máximo de pistas por puzzle.")]
        [Range(0, 5)] public int maxHintsPerPuzzle = 2;

        [Header("Puntaje de desempeño (ambas condiciones lo registran)")]
        [Tooltip("Peso de cada métrica en el puntaje 0-1. Se normalizan, no hace falta que sumen 1.")]
        public ScoreWeights scoreWeights = new ScoreWeights();

        [Tooltip("Reintentos (además del primero) que llevan el sub-puntaje de intentos a 0.")]
        [Min(1)] public int attemptTolerance = 3;

        [Tooltip("Tiempo esperado y tolerancia de errores por tipo de puzzle y tier. " +
                 "Valores provisorios: se calibran en el piloto.")]
        public PuzzleTuning[] puzzleTuning = PuzzleTuning.Defaults();

        [Header("Algoritmo adaptativo (condición B)")]
        [Tooltip("Cantidad de puzzles recientes que se promedian para decidir.")]
        [Range(1, 5)] public int adaptiveWindow = 2;

        [Tooltip("Si el promedio es mayor o igual, se sube un tier.")]
        [Range(0f, 1f)] public float adaptiveUpThreshold = 0.70f;

        [Tooltip("Si el promedio es menor o igual, se baja un tier. Entre ambos umbrales se mantiene (histéresis).")]
        [Range(0f, 1f)] public float adaptiveDownThreshold = 0.40f;

        [Header("Build")]
        public string buildLabel = "0.1.0";

        [Tooltip("Muestra información de depuración (tier, botones de simulación). DESACTIVAR en el build del experimento.")]
        public bool showDebugInfo = true;

        public int SlotCount => slotTypes != null ? slotTypes.Length : 0;

        public int ClampTier(int tier) => Mathf.Clamp(tier, minTier, maxTier);

        public int GetFixedTier(int slotIndex)
        {
            if (fixedTiers == null || fixedTiers.Length == 0) return minTier;
            int i = Mathf.Clamp(slotIndex, 0, fixedTiers.Length - 1);
            return ClampTier(fixedTiers[i]);
        }

        private void OnValidate()
        {
            if (maxTier < minTier) maxTier = minTier;
            if (adaptiveDownThreshold > adaptiveUpThreshold) adaptiveDownThreshold = adaptiveUpThreshold;
            if (slotTypes != null && fixedTiers != null && fixedTiers.Length != slotTypes.Length)
            {
                Debug.LogWarning($"[ExperimentConfig] fixedTiers ({fixedTiers.Length}) y slotTypes ({slotTypes.Length}) deberían tener el mismo largo.");
            }
        }

        public PuzzleTuning GetTuning(PuzzleType type)
        {
            if (puzzleTuning != null)
                foreach (var t in puzzleTuning)
                    if (t != null && t.type == type) return t;
            foreach (var t in PuzzleTuning.Defaults())
                if (t.type == type) return t;
            return new PuzzleTuning { type = type };
        }

        public static ExperimentConfig LoadOrDefault()
        {
            var config = Resources.Load<ExperimentConfig>(ResourcePath);
            if (config == null)
            {
                Debug.LogWarning("[ExperimentConfig] No se encontró Resources/ExperimentConfig. Se usan valores por defecto. " +
                                 "Ejecutá Tools > Prototipo > Construir escenas para crearlo.");
                config = CreateInstance<ExperimentConfig>();
            }
            return config;
        }
    }
}

namespace Prototipo
{
    [Serializable]
    public class ScoreWeights
    {
        [Min(0f)] public float time = 0.35f;
        [Min(0f)] public float errors = 0.30f;
        [Min(0f)] public float attempts = 0.15f;
        [Min(0f)] public float hints = 0.20f;
    }

    /// <summary>Referencias de desempeño de un tipo de puzzle, una por tier (índice 0 = tier 1).</summary>
    [Serializable]
    public class PuzzleTuning
    {
        public PuzzleType type;
        [Tooltip("Segundos que se consideran un desempeño 'normal' en cada tier.")]
        public float[] targetSeconds = { 30, 40, 55, 75, 100 };
        [Tooltip("Errores con los que el sub-puntaje de errores llega a 0 en cada tier.")]
        public float[] errorTolerance = { 3, 3, 4, 4, 5 };

        public float GetTargetSeconds(int tier) => Pick(targetSeconds, tier, 60f);
        public float GetErrorTolerance(int tier) => Pick(errorTolerance, tier, 4f);

        private static float Pick(float[] values, int tier, float fallback)
        {
            if (values == null || values.Length == 0) return fallback;
            return values[Mathf.Clamp(tier - 1, 0, values.Length - 1)];
        }

        public static PuzzleTuning[] Defaults() => new[]
        {
            new PuzzleTuning { type = PuzzleType.Sequence,     targetSeconds = new float[] { 20, 25, 30, 40, 50 },   errorTolerance = new float[] { 2, 2, 2, 3, 3 } },
            new PuzzleTuning { type = PuzzleType.MemoryPairs,  targetSeconds = new float[] { 25, 40, 55, 75, 95 },   errorTolerance = new float[] { 2, 3, 4, 5, 6 } },
            new PuzzleTuning { type = PuzzleType.Hanoi,        targetSeconds = new float[] { 12, 25, 50, 90, 140 }, errorTolerance = new float[] { 2, 3, 3, 4, 5 } },
            new PuzzleTuning { type = PuzzleType.SlidingTiles, targetSeconds = new float[] { 30, 45, 70, 100, 140 }, errorTolerance = new float[] { 5, 5, 5, 5, 5 } },
        };
    }
}
