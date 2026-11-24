using UnityEngine;

namespace Prototipo
{
    /// <summary>Lo que la escena de juego le ofrece a cada puzzle.</summary>
    public interface IPuzzleHost
    {
        RuntimeUI UI { get; }

        /// <summary>Error del jugador (respuesta incorrecta, movimiento ilegal, etc.).</summary>
        void ReportError();

        /// <summary>El jugador empieza un nuevo intento del mismo puzzle (por ejemplo, después de fallar).</summary>
        void ReportNewAttempt();

        /// <summary>El puzzle quedó resuelto.</summary>
        void ReportSolved();

        /// <summary>Texto de estado o instrucción breve que se muestra arriba del tablero.</summary>
        void SetStatus(string text);
    }

    /// <summary>
    /// Base de todos los puzzles. Cada puzzle arma su tablero dentro de <see cref="Board"/> según el tier
    /// (1 a 5) y solo informa eventos al host: las métricas las mide el PuzzleMetricsTracker.
    ///
    /// Ciclo: Init (arma el tablero) → Begin (el tiempo empieza a correr) → ... → ReportSolved.
    /// </summary>
    public abstract class PuzzleBase : MonoBehaviour
    {
        protected IPuzzleHost Host { get; private set; }
        protected RectTransform Board { get; private set; }
        protected int Tier { get; private set; }
        protected System.Random Rng { get; private set; }
        protected bool Solved { get; private set; }

        public void Init(IPuzzleHost host, RectTransform board, int tier, System.Random rng)
        {
            Host = host;
            Board = board;
            Tier = Mathf.Clamp(tier, 1, 5);
            Rng = rng;
            Build();
        }

        /// <summary>Arma el tablero. Se llama antes del tutorial, así el jugador ya lo ve detrás.</summary>
        protected abstract void Build();

        /// <summary>Arranca la interacción. A partir de acá corre el tiempo.</summary>
        public abstract void Begin();

        /// <summary>Aplica una pista. Devuelve false si en este momento no hay pista posible (no se cuenta).</summary>
        public abstract bool TryHint();

        /// <summary>Si en este momento tiene sentido pedir una pista (el botón se deshabilita si no).</summary>
        public virtual bool CanHint => !Solved;

        /// <summary>Bloquea toda interacción (tiempo agotado o abandono).</summary>
        public virtual void Stop() => enabled = false;

        protected void MarkSolved()
        {
            if (Solved) return;
            Solved = true;
            Host.ReportSolved();
        }
    }
}
