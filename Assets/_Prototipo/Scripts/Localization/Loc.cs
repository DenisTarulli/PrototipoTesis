using System;
using UnityEngine;

namespace Prototipo
{
    /// <summary>
    /// Localización mínima: tabla de textos en Strings.cs con una columna por idioma.
    /// Para agregar un idioma, sumar su código a Codes y una columna a cada entrada.
    /// </summary>
    public static class Loc
    {
        public static readonly string[] Codes = { "es", "en" };

        private static int index;

        public static string CurrentCode => Codes[index];

        public static event Action LanguageChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            index = 0;
            LanguageChanged = null;
        }

        public static void SetLanguage(string code)
        {
            int i = Array.IndexOf(Codes, code);
            if (i < 0) i = 0;
            if (i == index) return;
            index = i;
            LanguageChanged?.Invoke();
        }

        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (Strings.Table.TryGetValue(key, out var values) && values != null && values.Length > 0)
            {
                string s = index < values.Length ? values[index] : null;
                return string.IsNullOrEmpty(s) ? values[0] : s;
            }
            Debug.LogWarning($"[Loc] Falta la clave '{key}'.");
            return "#" + key;
        }

        public static string Format(string key, params object[] args)
        {
            try { return string.Format(Get(key), args); }
            catch (FormatException) { return Get(key); }
        }
    }
}
