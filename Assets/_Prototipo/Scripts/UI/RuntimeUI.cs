using System;
using UnityEngine;
using UnityEngine.UI;

namespace Prototipo
{
    /// <summary>
    /// Creación de UI en tiempo de ejecución para los puzzles (que arman su tablero según el tier).
    /// Usa el mismo estilo que UIFactory, que solo existe en el editor. El sprite redondeado lo
    /// asigna el SceneBuilder en la escena, porque en un build no se puede cargar desde el editor.
    /// </summary>
    public class RuntimeUI
    {
        private readonly Sprite rounded;
        private readonly Font font;

        public RuntimeUI(Sprite roundedSprite)
        {
            rounded = roundedSprite;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Place(RectTransform rt, Vector2 center, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = center;
            rt.sizeDelta = size;
        }

        public Image Image(Transform parent, string name, Color color, bool roundedCorners = true)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (roundedCorners && rounded != null)
            {
                img.sprite = rounded;
                img.type = UnityEngine.UI.Image.Type.Sliced;
            }
            return img;
        }

        public Text Text(Transform parent, string name, string text, int size, Color color,
                         TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            t.text = text;
            return t;
        }

        /// <summary>Botón con fondo de color y etiqueta centrada. El color se maneja a mano (sin tinte).</summary>
        public Button Button(Transform parent, string name, Color background, string label, int fontSize, Color labelColor,
                             Action onClick)
        {
            var img = Image(parent, name, background);
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white; // los puzzles indican el estado bloqueado con su propio color
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            if (onClick != null) button.onClick.AddListener(() => onClick());

            if (!string.IsNullOrEmpty(label))
            {
                var t = Text(img.transform, "Label", label, fontSize, labelColor, TextAnchor.MiddleCenter, FontStyle.Bold);
                var lrt = t.rectTransform;
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            }
            return button;
        }

        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }
    }
}
