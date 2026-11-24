using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Prototipo.EditorTools
{
    /// <summary>
    /// Crea elementos de uGUI con el estilo del prototipo. Solo se usa desde el editor
    /// (SceneBuilder), así que todo lo que crea queda serializado en las escenas y se puede
    /// retocar a mano después.
    /// </summary>
    public static class UIFactory
    {
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        private static Font font;
        private static Sprite rounded, checkmark, knob, sliderBg;

        public static Font DefaultFont => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        private static Sprite Rounded => rounded != null ? rounded : (rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"));
        private static Sprite Checkmark => checkmark != null ? checkmark : (checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"));
        private static Sprite Knob => knob != null ? knob : (knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"));
        private static Sprite SliderBackground => sliderBg != null ? sliderBg : (sliderBg = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"));

        // ------------------------------------------------------------ Básicos

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        public static LayoutElement Layout(Component c, float minHeight = -1, float preferredHeight = -1,
                                           float preferredWidth = -1, float flexibleWidth = -1)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.minHeight = minHeight;
            le.preferredHeight = preferredHeight;
            le.preferredWidth = preferredWidth;
            le.flexibleWidth = flexibleWidth;
            return le;
        }

        // ------------------------------------------------------------ Escena

        public static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0, 0, -10);
            var cam = go.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UITheme.Background;
            return cam;
        }

        public static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // Las acciones por defecto del módulo se crean en memoria y no se pueden serializar en
            // una escena. Se usa el asset del proyecto, que tiene un mapa "UI" con las mismas acciones.
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions != null)
                go.GetComponent<InputSystemUIInputModule>().actionsAsset = actions;
            else
                Debug.LogWarning($"[UIFactory] No se encontró {InputActionsPath}. El EventSystem queda sin acciones asignadas.");
        }

        public static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = LayerMask.NameToLayer("UI");
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        // ------------------------------------------------------------ Contenedores

        public static Image CreateImage(Transform parent, string name, Color color, bool roundedCorners = false)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (roundedCorners)
            {
                img.sprite = Rounded;
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        public static Image CreateBackground(Transform parent)
        {
            var img = CreateImage(parent, "Background", UITheme.Background);
            Stretch(img.rectTransform);
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Tarjeta centrada con layout vertical que se ajusta a su contenido.</summary>
        public static RectTransform CreateCard(Transform parent, string name, float width, int padding = 56, float spacing = 24)
        {
            var img = CreateImage(parent, name, UITheme.Surface, true);
            var rt = img.rectTransform;
            Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 200));

            var shadow = img.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.08f);
            shadow.effectDistance = new Vector2(0f, -6f);

            var vlg = img.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(padding, padding, padding, padding);
            vlg.spacing = spacing;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = img.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rt;
        }

        public static RectTransform CreateRow(Transform parent, string name, float spacing = 20, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rt = NewRect(name, parent);
            var hlg = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.childAlignment = align;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            return rt;
        }

        public static RectTransform CreateSpacer(Transform parent, float height = 0, float flexibleWidth = -1)
        {
            var rt = NewRect("Spacer", parent);
            Layout(rt, minHeight: height, preferredHeight: height, flexibleWidth: flexibleWidth);
            return rt;
        }

        /// <summary>Capa oscura que bloquea clics, con una tarjeta centrada. Devuelve la capa; la tarjeta es su hijo.</summary>
        public static GameObject CreateOverlay(Transform parent, string name, float cardWidth, out RectTransform card)
        {
            var overlay = CreateImage(parent, name, UITheme.Overlay);
            Stretch(overlay.rectTransform);
            card = CreateCard(overlay.transform, "Card", cardWidth);
            return overlay.gameObject;
        }

        // ------------------------------------------------------------ Controles

        public static Text CreateText(Transform parent, string name, string key, int size, Color color,
                                      TextAnchor align = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal,
                                      string literal = null)
        {
            var rt = NewRect(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = align;
            text.lineSpacing = 1.15f;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = true;

            if (!string.IsNullOrEmpty(key))
            {
                var loc = rt.gameObject.AddComponent<LocalizedText>();
                loc.Key = key; // también escribe el texto en español para previsualizar en el editor
            }
            else
            {
                text.text = literal ?? string.Empty;
            }
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string key, Color background, Color foreground,
                                          float width = -1, float height = 76, int fontSize = 30, string literal = null)
        {
            var img = CreateImage(parent, name, background, true);
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;

            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.93f, 0.93f, 0.93f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.55f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var label = CreateText(img.transform, "Label", key, fontSize, foreground, TextAnchor.MiddleCenter, FontStyle.Bold, literal);
            Stretch(label.rectTransform, 12, 0, 12, 0);

            Layout(img, minHeight: height, preferredHeight: height, preferredWidth: width);
            return button;
        }

        public static Button PrimaryButton(Transform parent, string name, string key, float width = -1)
            => CreateButton(parent, name, key, UITheme.Primary, UITheme.TextOnColor, width);

        public static Button SecondaryButton(Transform parent, string name, string key, float width = -1)
            => CreateButton(parent, name, key, UITheme.SurfaceAlt, UITheme.TextPrimary, width);

        public static Toggle CreateToggle(Transform parent, string name, string key, int fontSize = 26)
        {
            var root = NewRect(name, parent);
            var toggle = root.gameObject.AddComponent<Toggle>();
            Layout(root, minHeight: 48, preferredHeight: 48);

            var box = CreateImage(root, "Box", UITheme.SurfaceAlt, true);
            Anchor(box.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(40, 40));

            var check = CreateImage(box.transform, "Checkmark", UITheme.Primary);
            check.sprite = Checkmark;
            Stretch(check.rectTransform, 4, 4, 4, 4);

            var label = CreateText(root, "Label", key, fontSize, UITheme.TextPrimary, TextAnchor.MiddleLeft);
            Stretch(label.rectTransform, 56, 0, 0, 0);

            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = false;
            return toggle;
        }

        public static Slider CreateSlider(Transform parent, string name, float width)
        {
            var root = NewRect(name, parent);
            var slider = root.gameObject.AddComponent<Slider>();
            Layout(root, minHeight: 40, preferredHeight: 40, preferredWidth: width);

            var bg = CreateImage(root, "Background", UITheme.SurfaceAlt);
            bg.sprite = SliderBackground;
            bg.type = Image.Type.Sliced;
            bg.rectTransform.anchorMin = new Vector2(0f, 0.3f);
            bg.rectTransform.anchorMax = new Vector2(1f, 0.7f);
            bg.rectTransform.offsetMin = Vector2.zero;
            bg.rectTransform.offsetMax = Vector2.zero;

            var fillArea = NewRect("Fill Area", root);
            fillArea.anchorMin = new Vector2(0f, 0.3f);
            fillArea.anchorMax = new Vector2(1f, 0.7f);
            fillArea.offsetMin = new Vector2(20f, 0f);
            fillArea.offsetMax = new Vector2(-20f, 0f);

            var fill = CreateImage(fillArea, "Fill", UITheme.Primary);
            fill.sprite = SliderBackground;
            fill.type = Image.Type.Sliced;
            fill.rectTransform.sizeDelta = new Vector2(20f, 0f);

            var handleArea = NewRect("Handle Slide Area", root);
            Stretch(handleArea, 20, 0, 20, 0);

            var handle = CreateImage(handleArea, "Handle", UITheme.Primary);
            handle.sprite = Knob;
            handle.rectTransform.sizeDelta = new Vector2(40f, 0f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            return slider;
        }

        /// <summary>Fila con 5 botones (1-5) y etiquetas de los extremos debajo.</summary>
        public static RatingSelector CreateRating(Transform parent, string name, string lowKey, string highKey)
        {
            var group = NewRect(name, parent);
            var vlg = group.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            var row = CreateRow(group, "Buttons", 16);
            var buttons = new Button[5];
            for (int i = 0; i < 5; i++)
            {
                buttons[i] = CreateButton(row, "Option" + (i + 1), null, UITheme.SurfaceAlt, UITheme.TextPrimary,
                                          width: 96, height: 64, fontSize: 28, literal: (i + 1).ToString());
            }

            var labels = CreateRow(group, "Labels", 0, TextAnchor.MiddleCenter);
            var low = CreateText(labels, "Low", lowKey, 20, UITheme.TextSecondary, TextAnchor.MiddleLeft);
            Layout(low, preferredWidth: 280);
            CreateSpacer(labels, 0, 1);
            var high = CreateText(labels, "High", highKey, 20, UITheme.TextSecondary, TextAnchor.MiddleRight);
            Layout(high, preferredWidth: 280);
            var labelsLayout = Layout(labels, preferredWidth: 576);
            labelsLayout.flexibleWidth = 0;

            var selector = group.gameObject.AddComponent<RatingSelector>();
            selector.buttons = buttons;
            return selector;
        }
    }
}
