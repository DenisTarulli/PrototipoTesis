using System;
using UnityEngine;
using UnityEngine.UI;

namespace Prototipo
{
    /// <summary>Selector de escala 1-5 (o del largo que tenga el arreglo de botones).</summary>
    public class RatingSelector : MonoBehaviour
    {
        public Button[] buttons;

        public int Value { get; private set; }

        public event Action<int> ValueChanged;

        private void Awake()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                int value = i + 1;
                buttons[i].onClick.AddListener(() => Select(value));
            }
            Refresh();
        }

        public void Select(int value)
        {
            Value = value;
            Refresh();
            ValueChanged?.Invoke(value);
        }

        private void Refresh()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                bool selected = (i + 1) == Value;
                if (buttons[i].targetGraphic is Image img)
                    img.color = selected ? UITheme.Primary : UITheme.SurfaceAlt;
                var label = buttons[i].GetComponentInChildren<Text>();
                if (label != null)
                    label.color = selected ? UITheme.TextOnColor : UITheme.TextPrimary;
            }
        }
    }
}
