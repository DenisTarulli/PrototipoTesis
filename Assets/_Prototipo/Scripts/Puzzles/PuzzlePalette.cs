using UnityEngine;

namespace Prototipo
{
    /// <summary>Colores compartidos por los puzzles: apagados, distinguibles entre sí y legibles sobre el tema oscuro.</summary>
    public static class PuzzlePalette
    {
        public static readonly Color32[] Pieces =
        {
            new Color32(0x7F, 0xA6, 0xC9, 0xFF), // azul
            new Color32(0x8C, 0xBF, 0x9C, 0xFF), // verde
            new Color32(0xD9, 0xB0, 0x87, 0xFF), // ámbar
            new Color32(0xC2, 0x9B, 0xD1, 0xFF), // lila
            new Color32(0xD3, 0x8E, 0x8E, 0xFF), // rosa
            new Color32(0x86, 0xC5, 0xC4, 0xFF), // turquesa
            new Color32(0xD8, 0xCF, 0x86, 0xFF), // amarillo
            new Color32(0xA9, 0xB4, 0xC2, 0xFF), // gris azulado
            new Color32(0xC9, 0xA4, 0x8C, 0xFF), // terracota
            new Color32(0xA8, 0xC6, 0x7E, 0xFF), // lima
        };

        /// <summary>Texto oscuro para poner encima de un color de la paleta.</summary>
        public static readonly Color LabelOnPiece = new Color(0.08f, 0.10f, 0.12f, 0.9f);

        public static Color Dim(Color c, float factor) => new Color(c.r * factor, c.g * factor, c.b * factor, 1f);
    }
}
