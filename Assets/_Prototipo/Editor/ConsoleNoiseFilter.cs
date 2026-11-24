using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Prototipo.EditorTools
{
    /// <summary>
    /// Oculta de la consola el aviso repetido del paquete Unity AI Assistant
    /// ("Error reason is 'NoSubscription'..."), que aparece porque la cuenta no tiene suscripción a los
    /// generadores de IA de Unity. El paquete no se puede quitar porque es el que conecta Unity con Claude,
    /// y no ofrece una opción para silenciarlo.
    ///
    /// Solo afecta al editor y solo a ese mensaje; todo lo demás pasa igual.
    /// Se activa o desactiva desde Tools > Prototipo > Ocultar aviso 'NoSubscription' de Unity AI.
    /// </summary>
    [InitializeOnLoad]
    public static class ConsoleNoiseFilter
    {
        private const string PrefKey = "Prototipo.HideNoSubscriptionLog";
        private const string MenuPath = "Tools/Prototipo/Ocultar aviso 'NoSubscription' de Unity AI";

        static ConsoleNoiseFilter()
        {
            if (Enabled) Install();
        }

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        [MenuItem(MenuPath, priority = 60)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) Install(); else Uninstall();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static void Install()
        {
            if (Debug.unityLogger.logHandler is FilteringHandler) return;
            Debug.unityLogger.logHandler = new FilteringHandler(Debug.unityLogger.logHandler);
        }

        private static void Uninstall()
        {
            if (Debug.unityLogger.logHandler is FilteringHandler f)
                Debug.unityLogger.logHandler = f.Inner;
        }

        private sealed class FilteringHandler : ILogHandler
        {
            public readonly ILogHandler Inner;

            public FilteringHandler(ILogHandler inner) { Inner = inner; }

            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                if (IsNoise(format, args)) return;
                Inner.LogFormat(logType, context, format, args);
            }

            public void LogException(Exception exception, Object context) => Inner.LogException(exception, context);

            private static bool IsNoise(string format, object[] args)
            {
                string text = args != null && args.Length > 0 && args[0] is string s ? s : format;
                return text != null &&
                       text.StartsWith("Error reason is 'NoSubscription'", StringComparison.Ordinal) &&
                       text.Contains("generators.ai.unity.com");
            }
        }
    }
}
