using System.Collections.Generic;
using UnityEngine;

namespace Prototipo
{
    /// <summary>Plan de un slot: qué puzzle se juega, con qué dificultad y por qué.</summary>
    public struct SlotPlan
    {
        public int slotIndex;   // 0-based
        public PuzzleType type;
        public int tier;
        public string source;          // "calibration" | "fixed_ramp" | "adaptive"
        public int previousTier;       // 0 si es el primer slot
        public float windowAverage;    // -1 si no hubo decisión adaptativa
        public string decision;        // ver PuzzleRecord.decision

        public SlotPlan(int slotIndex, PuzzleType type, int tier, string source,
                        int previousTier = 0, float windowAverage = -1f, string decision = "none")
        {
            this.slotIndex = slotIndex;
            this.type = type;
            this.tier = tier;
            this.source = source;
            this.previousTier = previousTier;
            this.windowAverage = windowAverage;
            this.decision = decision;
        }
    }

    public interface IDifficultyProvider
    {
        /// <summary>Devuelve el tier y el origen de la decisión para el slot indicado.</summary>
        SlotPlan Plan(int slotIndex, PuzzleType type);

        /// <summary>Recibe el resultado del puzzle recién terminado (con performanceScore ya calculado).</summary>
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
            int prev = slotIndex > 0 ? config.GetFixedTier(slotIndex - 1) : 0;
            return new SlotPlan(slotIndex, type, config.GetFixedTier(slotIndex), source, prev);
        }

        public void ReportResult(PuzzleRecord result) { }
    }

    /// <summary>
    /// Condición B: dificultad adaptativa basada en desempeño.
    ///
    /// Regla (una por slot, después de la calibración):
    ///   1. Se promedian los puntajes de los últimos <c>adaptiveWindow</c> puzzles (incluye la calibración).
    ///   2. Promedio ≥ umbral superior → sube un tier.  Promedio ≤ umbral inferior → baja un tier.
    ///      Entre ambos umbrales se mantiene (banda de histéresis para evitar oscilaciones).
    ///   3. El tier nunca sale de [minTier, maxTier] ni cambia más de un nivel por paso.
    ///
    /// El primer slot adaptativo parte del tier del último slot de calibración.
    /// </summary>
    public class AdaptiveDifficultyProvider : IDifficultyProvider
    {
        private readonly ExperimentConfig config;
        private readonly List<float> scores = new List<float>();
        private int currentTier;

        public AdaptiveDifficultyProvider(ExperimentConfig config)
        {
            this.config = config;
            currentTier = config.GetFixedTier(0);
        }

        public SlotPlan Plan(int slotIndex, PuzzleType type)
        {
            int prev = slotIndex > 0 ? currentTier : 0;

            if (slotIndex < config.calibrationSlots)
            {
                currentTier = config.GetFixedTier(slotIndex);
                return new SlotPlan(slotIndex, type, currentTier, "calibration", prev);
            }

            if (scores.Count == 0)
            {
                // Sin calibración ni resultados previos: se arranca en el primer tier de la rampa.
                return new SlotPlan(slotIndex, type, currentTier, "adaptive", prev, -1f, "none");
            }

            float avg = WindowAverage();
            string decision = "keep";
            int target = currentTier;

            if (avg >= config.adaptiveUpThreshold)
            {
                target = currentTier + 1;
                decision = "up";
            }
            else if (avg <= config.adaptiveDownThreshold)
            {
                target = currentTier - 1;
                decision = "down";
            }

            int clamped = config.ClampTier(target);
            if (clamped != target) decision = decision == "up" ? "clamped_up" : "clamped_down";
            currentTier = clamped;

            return new SlotPlan(slotIndex, type, currentTier, "adaptive", prev, avg, decision);
        }

        public void ReportResult(PuzzleRecord result)
        {
            if (result == null) return;
            scores.Add(Mathf.Clamp01(result.performanceScore));
        }

        private float WindowAverage()
        {
            int n = Mathf.Min(Mathf.Max(1, config.adaptiveWindow), scores.Count);
            float sum = 0f;
            for (int i = scores.Count - n; i < scores.Count; i++) sum += scores[i];
            return sum / n;
        }
    }
}
