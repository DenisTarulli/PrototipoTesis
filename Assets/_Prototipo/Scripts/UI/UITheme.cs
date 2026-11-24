using UnityEngine;

namespace Prototipo
{
    /// <summary>Paleta suave y de bajo contraste cromático, pensada para sesiones largas.</summary>
    public static class UITheme
    {
        public static readonly Color Background    = new Color32(0xF1, 0xEE, 0xE8, 0xFF); // crema
        public static readonly Color Surface       = new Color32(0xFF, 0xFD, 0xF9, 0xFF); // tarjeta
        public static readonly Color SurfaceAlt    = new Color32(0xE6, 0xE9, 0xEC, 0xFF); // botón secundario / no seleccionado
        public static readonly Color Primary       = new Color32(0x6C, 0x8E, 0xAD, 0xFF); // azul grisáceo
        public static readonly Color Accent        = new Color32(0x8F, 0xB3, 0x96, 0xFF); // verde salvia
        public static readonly Color Warm          = new Color32(0xD6, 0xA8, 0x82, 0xFF); // durazno apagado
        public static readonly Color Danger        = new Color32(0xC4, 0x86, 0x86, 0xFF); // rosa terroso
        public static readonly Color TextPrimary   = new Color32(0x2F, 0x3A, 0x45, 0xFF);
        public static readonly Color TextSecondary = new Color32(0x6F, 0x7C, 0x89, 0xFF);
        public static readonly Color TextOnColor   = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public static readonly Color Overlay       = new Color(0.12f, 0.15f, 0.18f, 0.45f);
    }
}
