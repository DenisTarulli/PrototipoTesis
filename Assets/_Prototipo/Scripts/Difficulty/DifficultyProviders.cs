namespace Prototipo
{
    /// <summary>Plan de un slot: qué puzzle se juega y con qué dificultad.</summary>
    public struct SlotPlan
    {
        public int slotIndex;   // 0-based
        public PuzzleType type;
        public int tier;
        public string source;

        public SlotPlan(int slotIndex, PuzzleType type, int tier, string source)
        {
            this.slotIndex = slotIndex;
            this.type = type;
            this.tier = tier;
            this.source = source;
        }
    }

    public interface IDifficultyProvider
    {
        /// <summary>Devuelve el tier y el origen de la decisión para el slot indicado.</summary>
        SlotPlan Plan(int slotIndex, PuzzleType type);

        /// <summary>Recibe el resultado del puzzle recién terminado.</summary>
        void ReportResult(PuzzleRecord result);
    }

    /// <summary>Condición A: rampa predefinida, sin cambios durante la sesión.</summary>
    public class FixedDifficultyProvider : IDifficultyProvider
    {
        private readonly ExperimentConfig config;

        public FixedDifficultyProvider(ExperimentConfig config) { this.config = config; }

        public SlotPlan Plan(int slotIndex, PuzzleType type)
        {
            string source = slotIndex < config.calibrationSlots ? "calibration" : "fixed_ramp";
            return new SlotPlan(slotIndex, type, config.GetFixedTier(slotIndex), source);
        }

        public void ReportResult(PuzzleRecord result) { }
    }

    /// <summary>
    /// Condición B: dificultad adaptativa.
    /// ETAPA 1: es un marcador. Usa la misma rampa que la condición fija y se identifica como
    /// "adaptive_stub" en los datos. El algoritmo real (puntaje de desempeño + ventana + histéresis)
    /// se implementa en la etapa siguiente.
    /// </summary>
    public class AdaptiveDifficultyProvider : IDifficultyProvider
    {
        private readonly ExperimentConfig config;

        public AdaptiveDifficultyProvider(ExperimentConfig config) { this.config = config; }

        public SlotPlan Plan(int slotIndex, PuzzleType type)
        {
            if (slotIndex < config.calibrationSlots)
                return new SlotPlan(slotIndex, type, config.GetFixedTier(slotIndex), "calibration");

            return new SlotPlan(slotIndex, type, config.GetFixedTier(slotIndex), "adaptive_stub");
        }

        public void ReportResult(PuzzleRecord result) { }
    }
}
