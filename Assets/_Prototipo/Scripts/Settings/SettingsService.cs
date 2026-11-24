using UnityEngine;

namespace Prototipo
{
    /// <summary>Ajustes del jugador persistidos en PlayerPrefs.</summary>
    public static class SettingsService
    {
        private const string VolumeKey = "settings.volume";
        private const string LanguageKey = "settings.language";

        public static float Volume { get; private set; } = 0.8f;

        public static void Load()
        {
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.8f));
            AudioListener.volume = Volume;
            Loc.SetLanguage(PlayerPrefs.GetString(LanguageKey, "es"));
        }

        public static void SetVolume(float value)
        {
            Volume = Mathf.Clamp01(value);
            AudioListener.volume = Volume;
            PlayerPrefs.SetFloat(VolumeKey, Volume);
            PlayerPrefs.Save();
        }

        public static void SetLanguage(string code)
        {
            Loc.SetLanguage(code);
            PlayerPrefs.SetString(LanguageKey, Loc.CurrentCode);
            PlayerPrefs.Save();
        }
    }
}
