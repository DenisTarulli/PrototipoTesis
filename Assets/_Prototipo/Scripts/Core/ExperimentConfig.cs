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
            if (slotTypes != null && fixedTiers != null && fixedTiers.Length != slotTypes.Length)
            {
                Debug.LogWarning($"[ExperimentConfig] fixedTiers ({fixedTiers.Length}) y slotTypes ({slotTypes.Length}) deberían tener el mismo largo.");
            }
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
