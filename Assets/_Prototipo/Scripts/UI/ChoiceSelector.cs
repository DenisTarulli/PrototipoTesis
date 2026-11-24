using System;
using UnityEngine;
using UnityEngine.UI;

namespace Prototipo
{
    /// <summary>
    /// Grupo de botones de opción única con un valor entero por botón (por ejemplo 1-5, o -1 para
    /// "No utilicé ayudas"). Se usa en la encuesta final.
    /// </summary>
    public class ChoiceSelector : MonoBehaviour
    {
        public Button[] buttons;
        public int[] values;
        public CanvasGroup group;   // para atenuar la pregunta cuando no aplica

        public bool HasValue { get; private set; }
        public int Value { get; private set; }
        public bool Interactable { get; private set; } = true;

        public event Action<int> ValueChanged;

        private void Awake()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                int v = values != null && i < values.Length ? values[i] : i + 1;
                buttons[i].onClick.AddListener(() => Select(v));
            }
            Refresh();
        }

        public void Select(int value)
        {
            if (!Interactable) return;
            HasValue = true;
            Value = value;
            Refresh();
            ValueChanged?.Invoke(value);
        }

        public void Clear()
        {
            HasValue = false;
            Value = 0;
            Refresh();
        }

        public void SetInteractable(bool on)
        {
            Interactable = on;
            foreach (var b in buttons) b.interactable = on;
            if (group != null)
            {
                group.alpha = on ? 1f : 0.35f;
                group.interactable = on;
            }
        }

        private void Refresh()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                int v = values != null && i < values.Length ? values[i] : i + 1;
                bool selected = HasValue && v == Value;
                if (buttons[i].targetGraphic is Image img)
                    img.color = selected ? UITheme.Primary : UITheme.SurfaceAlt;
                var label = buttons[i].GetComponentInChildren<Text>();
                if (label != null)
                    label.color = selected ? UITheme.TextOnColor : UITheme.TextPrimary;
            }
        }
    }
}
