using System.Text;

namespace Prototipo
{
    /// <summary>
    /// Código anónimo de participante. No contiene la condición, para no romper el ciego:
    /// la condición se guarda en un campo aparte del registro.
    /// </summary>
    public static class ParticipantCode
    {
        // Sin caracteres ambiguos (0/O, 1/I/L).
        private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        public static string Generate(System.Random rng, int length = 6)
        {
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                sb.Append(Alphabet[rng.Next(Alphabet.Length)]);
            return sb.ToString();
        }
    }
}
