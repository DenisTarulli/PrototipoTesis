using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Prototipo.EditorTools
{
    /// <summary>
    /// Genera los dos ejecutables del experimento (uno por condición) con el mismo nombre,
    /// la misma versión y sin información de depuración, para mantener el ciego.
    ///
    ///   Builds/Version_A/  → dificultad FIJA        (Grupo A)
    ///   Builds/Version_B/  → dificultad ADAPTATIVA  (Grupo B)
    ///
    /// La correspondencia A/B queda solo en Builds/MAPEO_CONDICIONES.txt, fuera de las carpetas
    /// que se comparten con los participantes.
    /// </summary>
    public static class ExperimentBuilder
    {
        private const string ConfigPath = "Assets/_Prototipo/Resources/ExperimentConfig.asset";
        private const string OutputRoot = "Builds";

        [MenuItem("Tools/Prototipo/Generar builds del experimento (A fija, B adaptativa)", priority = 40)]
        public static void BuildBoth()
        {
            var config = AssetDatabase.LoadAssetAtPath<ExperimentConfig>(ConfigPath);
            if (config == null)
            {
                EditorUtility.DisplayDialog("Falta configuración", "Ejecutá primero Tools > Prototipo > Construir escenas.", "OK");
                return;
            }

            if (PlayerSettings.companyName == "DefaultCompany" &&
                !EditorUtility.DisplayDialog("Company Name sin definir",
                    "Project Settings > Player > Company Name sigue como 'DefaultCompany'. Ese nombre forma parte de la " +
                    "carpeta donde se guardan los datos en la PC del participante.\n\n¿Generar igual?",
                    "Generar igual", "Cancelar"))
                return;

            if (!EditorUtility.DisplayDialog("Generar builds",
                    $"Se generan dos ejecutables Windows en la carpeta {OutputRoot}/ del proyecto:\n\n" +
                    "• Version_A → dificultad fija\n• Version_B → dificultad adaptativa\n\n" +
                    "Ambos sin información de depuración. La configuración del Inspector se restaura al terminar.",
                    "Generar", "Cancelar"))
                return;

            var originalMode = config.assignmentMode;
            var originalDebug = config.showDebugInfo;
            string summary;
            try
            {
                var a = BuildOne(config, AssignmentMode.ForceFixed, "Version_A");
                var b = BuildOne(config, AssignmentMode.ForceAdaptive, "Version_B");
                WriteMapping(config);
                summary = $"Version_A (fija): {a}\nVersion_B (adaptativa): {b}";
            }
            finally
            {
                config.assignmentMode = originalMode;
                config.showDebugInfo = originalDebug;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            EditorUtility.DisplayDialog("Builds terminados", summary, "OK");
            EditorUtility.RevealInFinder(Path.GetFullPath(OutputRoot));
        }

        private static string BuildOne(ExperimentConfig config, AssignmentMode mode, string folder)
        {
            config.assignmentMode = mode;
            config.showDebugInfo = false;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            string exeName = PlayerSettings.productName + ".exe";
            var options = new BuildPlayerOptions
            {
                scenes = SceneNames.BuildOrder.Select(n => $"Assets/_Prototipo/Scenes/{n}.unity").ToArray(),
                locationPathName = Path.Combine(OutputRoot, folder, exeName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"Falló el build {folder}: {report.summary.result} ({report.summary.totalErrors} errores). Ver la consola.");

            return $"OK ({report.summary.totalSize / (1024 * 1024)} MB)";
        }

        private static void WriteMapping(ExperimentConfig config)
        {
            string text =
                "MAPEO DE CONDICIONES — NO compartir con participantes\n" +
                $"Generado: {DateTime.Now:yyyy-MM-dd HH:mm}\n" +
                $"Build label: {config.buildLabel}\n\n" +
                "Version_A = dificultad FIJA (Grupo A)\n" +
                "Version_B = dificultad ADAPTATIVA (Grupo B)\n\n" +
                "Repartir alternando A, B, A, B... para llegar a 5 y 5.\n" +
                "Cada sesión también guarda la condición en el campo 'condition' de sus datos.\n";
            File.WriteAllText(Path.Combine(OutputRoot, "MAPEO_CONDICIONES.txt"), text);
        }
    }
}
