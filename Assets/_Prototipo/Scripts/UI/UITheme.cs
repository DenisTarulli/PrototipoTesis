using UnityEngine;

namespace Prototipo
{
    /// <summary>
    /// Paleta oscura y suave, pensada para sesiones de 20-30 minutos sin cansar la vista:
    /// fondos gris azulado (no negro puro), texto gris claro (no blanco puro) y colores de acento
    /// apagados con texto oscuro encima, para mantener buen contraste sin brillos fuertes.
    /// </summary>
    public static class UITheme
    {
        public static readonly Color Background    = new Color32(0x1A, 0x1E, 0x25, 0xFF); // fondo general
        public static readonly Color Surface       = new Color32(0x24, 0x2A, 0x33, 0xFF); // tarjetas y paneles
        public static readonly Color SurfaceAlt    = new Color32(0x31, 0x39, 0x45, 0xFF); // botón secundario / no seleccionado
        public static readonly Color Primary       = new Color32(0x7F, 0xA6, 0xC9, 0xFF); // azul acero suave
        public static readonly Color Accent        = new Color32(0x8C, 0xBF, 0x9C, 0xFF); // verde salvia
        public static readonly Color Warm          = new Color32(0xD9, 0xB0, 0x87, 0xFF); // ámbar apagado
        public static readonly Color Danger        = new Color32(0xD3, 0x8E, 0x8E, 0xFF); // rosa terroso
        public static readonly Color TextPrimary   = new Color32(0xDD, 0xE2, 0xE8, 0xFF); // gris muy claro
        public static readonly Color TextSecondary = new Color32(0x98, 0xA3, 0xB0, 0xFF); // gris medio
        public static readonly Color TextOnColor   = new Color32(0x15, 0x19, 0x1F, 0xFF); // texto sobre botones de color
        public static readonly Color Overlay       = new Color(0.04f, 0.05f, 0.07f, 0.70f);
        public static readonly Color Shadow        = new Color(0f, 0f, 0f, 0.35f);
    }
}
