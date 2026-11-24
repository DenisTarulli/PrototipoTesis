using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Prototipo
{
    /// <summary>
    /// Fichas deslizantes: ordenar las fichas numeradas (1, 2, 3...) deslizándolas hacia el hueco.
    ///
    /// Dificultad por tier: tamaño de la grilla y cantidad de movimientos de mezcla
    ///   (3×3·6, 3×3·12, 3×3·25, 4×4·15, 4×4·30). La mezcla parte del tablero resuelto, así que siempre tiene solución.
    /// Error: tocar una ficha que no está al lado del hueco (movimiento inválido).
    /// Intentos: «Reiniciar» vuelve al tablero mezclado inicial y cuenta como nuevo intento.
    /// Pista: resalta la próxima ficha a mover según la solución óptima (IDA*).
    /// Eficiencia: se registran los movimientos hechos y el mínimo real del tablero (no entra en el puntaje).
    /// </summary>
    public class SlidingTilesPuzzle : PuzzleBase
    {
        private struct TierParams
        {
            public int size, shuffle;
            public TierParams(int size, int shuffle) { this.size = size; this.shuffle = shuffle; }
        }

        private static readonly TierParams[] Tiers =
        {
            new TierParams(3, 6),
            new TierParams(3, 12),
            new TierParams(3, 25),
            new TierParams(4, 15),
            new TierParams(4, 30),
        };

        private const int BuildNodeBudget = 5_000_000;
        private const int HintNodeBudget = 1_500_000;
        private const float SlideSeconds = 0.08f;

        private int n, optimal, moves;
        private int[] initial, state;
        private readonly Dictionary<int, RectTransform> tileViews = new Dictionary<int, RectTransform>(); // valor → vista
        private readonly Dictionary<int, Image> tileImages = new Dictionary<int, Image>();
        private float cell, gap;
        private bool started, busy;

        protected override void Build()
        {
            var p = Tiers[Tier - 1];
            n = p.size;
            initial = SlidingSolver.Scramble(n, p.shuffle, Rng);
            state = (int[])initial.Clone();
            optimal = SlidingSolver.TrySolve(initial, n, BuildNodeBudget, out var path) ? path.Count : p.shuffle;

            gap = 10f;
            cell = n == 3 ? 170f : 132f;
            float frame = n * cell + (n - 1) * gap + 24f;

            var back = Host.UI.Image(Board, "Frame", UITheme.Background);
            back.raycastTarget = false;
            RuntimeUI.Place(back.rectTransform, Vector2.zero, new Vector2(frame, frame));

            for (int value = 1; value < n * n; value++)
            {
                int v = value;
                int goalRow = (value - 1) / n;
                // Color por fila de destino: ayuda a ver qué fichas van juntas sin depender solo del color (llevan número).
                var color = PuzzlePalette.Pieces[goalRow % PuzzlePalette.Pieces.Length];
                var button = Host.UI.Button(Board, "Tile" + value, color, value.ToString(), n == 3 ? 56 : 46,
                                            PuzzlePalette.LabelOnPiece, () => OnTile(v));
                var rt = (RectTransform)button.transform;
                rt.sizeDelta = new Vector2(cell, cell);
                tileViews[value] = rt;
                tileImages[value] = (Image)button.targetGraphic;
            }

            var restart = Host.UI.Button(Board, "Restart", UITheme.SurfaceAlt, Loc.Get("hanoi.restart"), 22, UITheme.TextPrimary, Restart);
            var rrt = (RectTransform)restart.transform;
            rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(1f, 1f);
            rrt.anchoredPosition = Vector2.zero;
            rrt.sizeDelta = new Vector2(200f, 52f);

            LayoutAll();
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
            int pos = SlidingSolver.NextMove(state, n, HintNodeBudget, out _);
            if (pos < 0) return false;
            StartCoroutine(HintRoutine(state[pos]));
            return true;
        }

        public override void Stop()
        {
            StopAllCoroutines();
            started = false;
            base.Stop();
        }

        // ------------------------------------------------------------ Lógica

        private void OnTile(int value)
        {
            if (!started || busy || Solved) return;

            int pos = System.Array.IndexOf(state, value);
            if (!SlidingSolver.TryMove(state, n, pos))
            {
                Host.ReportError();
                StartCoroutine(WrongRoutine(value));
                return;
            }

            moves++;
            Host.ReportMoves(moves, optimal);
            UpdateStatus();
            StartCoroutine(Slide(value));

            if (SlidingSolver.IsSolved(state))
            {
                Host.SetStatus(Loc.Format("tiles.done", moves, optimal));
                MarkSolved();
            }
        }

        private void Restart()
        {
            if (!started || busy || Solved || moves == 0) return;
            Host.ReportNewAttempt();
            StopAllCoroutines(); // corta deslizamientos en curso antes de reacomodar
            state = (int[])initial.Clone();
            moves = 0;
            Host.ReportMoves(moves, optimal);
            LayoutAll();
            UpdateStatus();
        }

        // ------------------------------------------------------------ Visual

        private Vector2 CellPosition(int index)
        {
            int r = index / n, c = index % n;
            float span = n * cell + (n - 1) * gap;
            return new Vector2(-span / 2f + cell / 2f + c * (cell + gap), span / 2f - cell / 2f - r * (cell + gap));
        }

        private void LayoutAll()
        {
            for (int i = 0; i < state.Length; i++)
            {
                if (state[i] == 0) continue;
                var rt = tileViews[state[i]];
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = CellPosition(i);
            }
        }

        private IEnumerator Slide(int value)
        {
            var rt = tileViews[value];
            Vector2 from = rt.anchoredPosition;
            Vector2 to = CellPosition(System.Array.IndexOf(state, value));
            for (float t = 0f; t < SlideSeconds; t += Time.deltaTime)
            {
                rt.anchoredPosition = Vector2.Lerp(from, to, t / SlideSeconds);
                yield return null;
            }
            rt.anchoredPosition = to;
        }

        private void UpdateStatus() => Host.SetStatus(Loc.Format("tiles.status", moves, optimal));

        private IEnumerator WrongRoutine(int value)
        {
            busy = true;
            var img = tileImages[value];
            var original = img.color;
            img.color = UITheme.Danger;
            Host.SetStatus(Loc.Get("tiles.invalid"));
            yield return new WaitForSeconds(0.35f);
            img.color = original;
            busy = false;
            UpdateStatus();
        }

        private IEnumerator HintRoutine(int value)
        {
            busy = true;
            Host.SetStatus(Loc.Format("tiles.hint", value));
            var rt = tileViews[value];
            for (int i = 0; i < 3; i++)
            {
                rt.localScale = Vector3.one * 1.12f;
                yield return new WaitForSeconds(0.25f);
                rt.localScale = Vector3.one;
                yield return new WaitForSeconds(0.15f);
            }
            busy = false;
        }
    }
}
