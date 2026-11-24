using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Prototipo.EditorTools
{
    /// <summary>
    /// Juega sesiones completas con jugadores sintéticos de distinta habilidad, usando el mismo
    /// puntaje y los mismos proveedores de dificultad que el juego. Sirve para verificar que el
    /// algoritmo adaptativo se comporta como se espera (y para ilustrarlo en la tesis) antes del piloto.
    ///
    /// Modelo de jugador (deliberadamente simple): con habilidad h y tier t, cuanto más supera t a h,
    /// más tarda, más errores comete y más pistas usa.
    /// </summary>
    public static class AdaptiveSimulator
    {
        private const string ConfigPath = "Assets/_Prototipo/Resources/ExperimentConfig.asset";

        private struct Profile
        {
            public string name;
            public float skillStart, skillEnd; // habilidad equivalente a un tier (1-5); puede mejorar durante la sesión
            public Profile(string n, float s, float e) { name = n; skillStart = s; skillEnd = e; }
        }

        private static readonly Profile[] Profiles =
        {
            new Profile("principiante", 1.3f, 1.8f),
            new Profile("intermedio",   2.8f, 3.2f),
            new Profile("experto",      4.6f, 5.0f),
            new Profile("aprende_rapido", 1.5f, 4.5f),
        };

        [MenuItem("Tools/Prototipo/Simular algoritmo adaptativo", priority = 41)]
        public static void Run()
        {
            var config = AssetDatabase.LoadAssetAtPath<ExperimentConfig>(ConfigPath);
            if (config == null) config = ScriptableObject.CreateInstance<ExperimentConfig>();

            var csv = new StringBuilder("profile,condition,run,slot,type,tier,decision,window_avg,score,time_s,errors,attempts,hints,completed\n");
            var log = new StringBuilder("[Simulador] Tier por slot (promedio de 20 corridas por perfil)\n");

            foreach (var p in Profiles)
            {
                foreach (var cond in new[] { DifficultyCondition.Fixed, DifficultyCondition.Adaptive })
                {
                    var tierSum = new float[config.SlotCount];
                    var scoreSum = new float[config.SlotCount];
                    const int runs = 20;
                    for (int run = 0; run < runs; run++)
                    {
                        var rng = new System.Random(1000 * run + p.name.GetHashCode() % 1000);
                        IDifficultyProvider provider = cond == DifficultyCondition.Fixed
                            ? new FixedDifficultyProvider(config)
                            : (IDifficultyProvider)new AdaptiveDifficultyProvider(config);

                        for (int slot = 0; slot < config.SlotCount; slot++)
                        {
                            var plan = provider.Plan(slot, config.slotTypes[slot]);
                            float skill = Mathf.Lerp(p.skillStart, p.skillEnd, slot / (float)Mathf.Max(1, config.SlotCount - 1));
                            var rec = SimulatePuzzle(config, plan, skill, rng);
                            rec.performanceScore = PerformanceScorer.Score(rec, config).total;
                            provider.ReportResult(rec);

                            tierSum[slot] += plan.tier;
                            scoreSum[slot] += rec.performanceScore;
                            csv.AppendLine(string.Join(",", p.name, cond, run, slot + 1, plan.type, plan.tier, plan.decision,
                                plan.windowAverage.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                                rec.performanceScore.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                                rec.timeSeconds.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                                rec.errors, rec.attempts, rec.hintsUsed, rec.completed));
                        }
                    }

                    log.Append($"{p.name,-15} {cond,-8} tiers: ");
                    for (int i = 0; i < tierSum.Length; i++) log.Append((tierSum[i] / runs).ToString("0.0")).Append(' ');
                    log.Append("| puntaje medio: ");
                    float total = 0f;
                    foreach (var s in scoreSum) total += s;
                    log.AppendLine((total / (runs * scoreSum.Length)).ToString("0.00"));
                }
            }

            Directory.CreateDirectory("Builds");
            string path = Path.GetFullPath("Builds/simulacion_adaptativa.csv");
            File.WriteAllText(path, csv.ToString());
            log.AppendLine($"CSV detallado: {path}");
            Debug.Log(log.ToString());
        }

        private static PuzzleRecord SimulatePuzzle(ExperimentConfig config, SlotPlan plan, float skill, System.Random rng)
        {
            var tuning = config.GetTuning(plan.type);
            float gap = plan.tier - skill; // >0: el puzzle le queda grande
            float noise = (float)(rng.NextDouble() * 0.5 - 0.25);

            float time = tuning.GetTargetSeconds(plan.tier) * Mathf.Pow(2f, 0.8f * gap + noise);
            float errRate = Mathf.Clamp01(0.35f + 0.3f * gap + noise);
            int errors = Mathf.RoundToInt(errRate * tuning.GetErrorTolerance(plan.tier) * 1.2f);
            int attempts = 1 + Mathf.Max(0, Mathf.RoundToInt(gap + noise));
            int hints = Mathf.Clamp(Mathf.RoundToInt(gap + 0.5f + noise), 0, config.maxHintsPerPuzzle);
            bool completed = time < config.puzzleTimeLimitSeconds;

            return new PuzzleRecord
            {
                slot = plan.slotIndex + 1,
                puzzleType = plan.type.ToString(),
                difficultyTier = plan.tier,
                timeSeconds = Mathf.Min(time, config.puzzleTimeLimitSeconds),
                errors = Mathf.Max(0, errors),
                attempts = attempts,
                hintsUsed = hints,
                completed = completed,
                timedOut = !completed
            };
        }
    }
}
