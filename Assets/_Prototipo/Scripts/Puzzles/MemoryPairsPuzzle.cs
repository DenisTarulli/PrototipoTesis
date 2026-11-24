using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Prototipo
{
    /// <summary>
    /// Pares de memoria: cartas boca abajo; el jugador da vuelta dos por vez buscando las que coinciden.
    ///
    /// Dificultad por tier: cantidad de pares (3, 4, 6, 8, 10).
    /// Error: un par incorrecto que se podía evitar con memoria: la pareja de la primera carta ya se
    ///        había visto, o la segunda carta ya era conocida. Los desaciertos con cartas nunca vistas
    ///        son parte normal de explorar y no se cuentan como error.
    /// Intentos: este puzzle no tiene reintentos; queda en 1.
    /// Pista: muestra todas las cartas sin resolver durante un instante.
    /// </summary>
    public class MemoryPairsPuzzle : PuzzleBase
    {
        private static readonly int[] PairsPerTier = { 3, 4, 6, 8, 10 };
        private static readonly string[] Symbols = { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K" };

        private const float MismatchShowSeconds = 0.9f;
        private const float HintShowSeconds = 1.3f;

        private class Card
        {
            public int value;
            public bool seen, matched, faceUp;
            public Image image;
            public Text label;
            public Button button;
        }

        private readonly List<Card> cards = new List<Card>();
        private Card first;
        private bool busy;      // hay dos cartas a la vista esperando darse vuelta, o una pista en curso
        private bool started;
        private int pairs, matchedPairs;

        protected override void Build()
        {
            pairs = PairsPerTier[Tier - 1];
            int count = pairs * 2;
            int cols = count <= 6 ? 3 : count <= 8 ? 4 : count <= 16 ? 4 : 5;
            int rows = Mathf.CeilToInt(count / (float)cols);

            // Mezcla Fisher-Yates con el generador de la sesión (reproducible).
            var values = new List<int>();
            for (int i = 0; i < pairs; i++) { values.Add(i); values.Add(i); }
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = Rng.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }

            float gap = 18f;
            float maxW = Board.rect.width > 0 ? Board.rect.width : 1160f;
            float maxH = Board.rect.height > 0 ? Board.rect.height : 600f;
            float cardH = Mathf.Min(170f, (maxH - (rows - 1) * gap) / rows);
            float cardW = Mathf.Min(cardH * 0.8f, (maxW - (cols - 1) * gap) / cols);
            float w = cols * cardW + (cols - 1) * gap;
            float h = rows * cardH + (rows - 1) * gap;

            for (int i = 0; i < count; i++)
            {
                var card = new Card { value = values[i] };
                int index = i;
                card.button = Host.UI.Button(Board, "Card" + (i + 1), UITheme.SurfaceAlt, string.Empty, 0, Color.white, () => OnCard(index));
                card.image = (Image)card.button.targetGraphic;
                card.label = Host.UI.Text(card.button.transform, "Symbol", string.Empty, Mathf.RoundToInt(cardH * 0.42f),
                                          PuzzlePalette.LabelOnPiece, TextAnchor.MiddleCenter, FontStyle.Bold);
                var lrt = card.label.rectTransform;
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;

                int c = i % cols, r = i / cols;
                var pos = new Vector2(-w / 2f + cardW / 2f + c * (cardW + gap), h / 2f - cardH / 2f - r * (cardH + gap));
                RuntimeUI.Place((RectTransform)card.button.transform, pos, new Vector2(cardW, cardH));
                cards.Add(card);
                ShowFace(card, false);
            }
        }

        public override void Begin()
        {
            started = true;
            UpdateStatus();
        }

        public override bool CanHint => started && !busy && !Solved;

        public override bool TryHint()
        {
            if (!CanHint) return false;
            StartCoroutine(HintRoutine());
            return true;
        }

        public override void Stop()
        {
            StopAllCoroutines();
            started = false;
            base.Stop();
        }

        // ------------------------------------------------------------ Lógica

        private void OnCard(int index)
        {
            var card = cards[index];
            if (!started || busy || Solved || card.matched || card.faceUp) return;

            ShowFace(card, true);

            if (first == null)
            {
                first = card;
                return;
            }

            var second = card;
            if (first.value == second.value)
            {
                first.matched = second.matched = true;
                first.seen = second.seen = true;
                MarkMatched(first);
                MarkMatched(second);
                first = null;
                matchedPairs++;
                UpdateStatus();
                if (matchedPairs >= pairs)
                {
                    Host.SetStatus(Loc.Get("pairs.done"));
                    MarkSolved();
                }
            }
            else
            {
                // Error de memoria: la pareja de la primera carta ya se había visto (podía elegirla),
                // o la segunda carta ya se conocía y no era la correcta.
                bool couldKnow = AnySeenWithValue(first.value, first) || second.seen;
                if (couldKnow) Host.ReportError();
                first.seen = second.seen = true;
                StartCoroutine(FlipBack(first, second));
                first = null;
            }
        }

        private bool AnySeenWithValue(int value, Card except)
        {
            foreach (var c in cards)
                if (c != except && c.value == value && c.seen && !c.matched) return true;
            return false;
        }

        private IEnumerator FlipBack(Card a, Card b)
        {
            busy = true;
            yield return new WaitForSeconds(MismatchShowSeconds);
            ShowFace(a, false);
            ShowFace(b, false);
            busy = false;
        }

        private IEnumerator HintRoutine()
        {
            busy = true;
            Host.SetStatus(Loc.Get("pairs.hint"));
            var revealed = new List<Card>();
            foreach (var c in cards)
            {
                if (c.matched || c.faceUp) continue;
                ShowFace(c, true);
                revealed.Add(c);
            }
            yield return new WaitForSeconds(HintShowSeconds);
            foreach (var c in revealed) ShowFace(c, false);
            busy = false;
            UpdateStatus();
        }

        // ------------------------------------------------------------ Visual

        private void UpdateStatus() => Host.SetStatus(Loc.Format("pairs.status", matchedPairs, pairs));

        private void ShowFace(Card card, bool up)
        {
            card.faceUp = up;
            if (up)
            {
                card.image.color = PuzzlePalette.Pieces[card.value % PuzzlePalette.Pieces.Length];
                card.label.text = Symbols[card.value % Symbols.Length];
            }
            else
            {
                card.image.color = UITheme.SurfaceAlt;
                card.label.text = string.Empty;
            }
        }

        private static void MarkMatched(Card card)
        {
            // Las cartas resueltas quedan visibles pero atenuadas, para que se distingan de las que están en juego.
            card.image.color = PuzzlePalette.Dim(PuzzlePalette.Pieces[card.value % PuzzlePalette.Pieces.Length], 0.55f);
            card.button.interactable = false;
        }
    }
}
