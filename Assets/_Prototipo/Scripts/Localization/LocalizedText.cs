using UnityEngine;
using UnityEngine.UI;

namespace Prototipo
{
    /// <summary>Actualiza un Text con la clave indicada cada vez que cambia el idioma.</summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        private Text text;

        public string Key
        {
            get => key;
            set { key = value; Refresh(); }
        }

        private void OnEnable()
        {
            Loc.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            Loc.LanguageChanged -= Refresh;
        }

        public void Refresh()
        {
            if (text == null) text = GetComponent<Text>();
            if (text != null && !string.IsNullOrEmpty(key)) text.text = Loc.Get(key);
        }
    }
}
