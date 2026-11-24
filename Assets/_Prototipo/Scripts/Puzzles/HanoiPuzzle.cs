using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Prototipo
{
    /// <summary>
    /// Torre de Hanoi: pasar todos los discos de la torre A a la torre C, moviendo uno por vez
    /// y sin apoyar nunca un disco sobre otro más chico.
    ///
    /// Dificultad por tier: cantidad de discos (2, 3, 4, 5, 6 → mínimo 3, 7, 15, 31, 63 movimientos).
    /// Error: intentar un movimiento ilegal (disco grande sobre uno chico).
    /// Intentos: el botón «Reiniciar» vuelve los discos al inicio y cuenta como nuevo intento.
    /// Pista: resalta el próximo movimiento óptimo desde la posición actual.
    /// Eficiencia: se registran los movimientos hechos y el mínimo teórico (no entra en el puntaje).
    /// </summary>
    public class HanoiPuzzle : PuzzleBase
    {
        private static readonly int[] DiscsPerTier = { 2, 3, 4, 5, 6 };
        private static readonly string[] PegNames = { "A", "B", "C" };

        private const int Goal = 2;
        private const float PegSpacing = 380f;
        private const float ColumnWidth = 340f;
        private const float DiscHeight = 46f;
        private const float BaseY = -200f;

        private int discs, optimal, moves;
        private readonly List<int>[] pegs = { new List<int>(), new List<int>(), new List<int>() }; // índices de disco, abajo→arriba; 0 = más chico
        private readonly List<RectTransform> discViews = new List<RectTransform>();
        private readonly Image[] columns = new Image[3];
        private int selectedPeg = -1;
        private bool started, busy;

        private static readonly Color ColumnIdle = new Color(1f, 1f, 1f, 0.03f);
        private static readonly Color ColumnSelected = new Color(0.50f, 0.65f, 0.79f, 0.18f);
        private static readonly Color ColumnHint = new Color(0.85f, 0.69f, 0.53f, 0.30f);
        private static readonly Color ColumnWrong = new Color(0.83f, 0.56f, 0.56f, 0.30f);

        protected override void Build()
        {
            discs = DiscsPerTier[Tier - 1];
            optimal = (1 << discs) - 1;

            for (int p = 0; p < 3; p++)
            {
                int peg = p;
                float x = (p - 1) * PegSpacing;

                var column = Host.UI.Button(Board, "Peg" + PegNames[p], ColumnIdle, null, 0, Color.white, () => OnPeg(peg));
                RuntimeUI.Place((RectTransform)column.transform, new Vector2(x, BaseY + 210f), new Vector2(ColumnWidth, 470f));
                columns[p] = (Image)column.targetGraphic;

                var pole = Host.UI.Image(Board, "Pole" + PegNames[p], UITheme.SurfaceAlt);
                pole.raycastTarget = false;
                RuntimeUI.Place(pole.rectTransform, new Vector2(x, BaseY + 160f), new Vector2(16f, 330f));

                var label = Host.UI.Text(Board, "Label" + PegNames[p], PegNames[p], 30, UITheme.TextSecondary, TextAnchor.MiddleCenter, FontStyle.Bold);
                RuntimeUI.Place(label.rectTransform, new Vector2(x, BaseY - 44f), new Vector2(120f, 40f));
            }

            var baseBar = Host.UI.Image(Board, "Base", UITheme.SurfaceAlt);
            baseBar.raycastTarget = false;
            RuntimeUI.Place(baseBar.rectTransform, new Vector2(0f, BaseY - 10f), new Vector2(PegSpacing * 2f + ColumnWidth, 18f));

            for (int d = 0; d < discs; d++)
            {
                float t = discs == 1 ? 1f : d / (float)(discs - 1);
                float width = Mathf.Lerp(90f, 310f, t);
                var img = Host.UI.Image(Board, "Disc" + (d + 1), PuzzlePalette.Pieces[d % PuzzlePalette.Pieces.Length]);
                img.raycastTarget = false; // se hace clic en la columna, no en el disco
                img.rectTransform.sizeDelta = new Vector2(width, DiscHeight - 6f);
                discViews.Add(img.rectTransform);
            }

            var restart = Host.UI.Button(Board, "Restart", UITheme.SurfaceAlt, Loc.Get("hanoi.restart"), 22, UITheme.TextPrimary, Restart);
            var rrt = (RectTransform)restart.transform;
            rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(1f, 1f); // arriba a la derecha del tablero
            rrt.anchoredPosition = new Vector2(0f, 0f);
            rrt.sizeDelta = new Vector2(200f, 52f);

            ResetDiscs();
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
            Deselect();
            var (from, to) = NextOptimalMove();
            if (from < 0) return false;
            StartCoroutine(HintRoutine(from, to));
            return true;
        }

        public override void Stop()
        {
            StopAllCoroutines();
            started = false;
            base.Stop();
        }

        // ------------------------------------------------------------ Lógica

        private void ResetDiscs()
        {
            foreach (var p in pegs) p.Clear();
            for (int d = discs - 1; d >= 0; d--) pegs[0].Add(d);
            moves = 0;
            selectedPeg = -1;
            Layout();
        }

        private void Restart()
        {
            if (!started || busy || Solved || moves == 0) return;
            Host.ReportNewAttempt();
            ResetDiscs();
            ReportMoves();
            UpdateStatus();
        }

        private void OnPeg(int peg)
        {
            if (!started || busy || Solved) return;

            if (selectedPeg < 0)
            {
                if (pegs[peg].Count == 0) return;
                selectedPeg = peg;
                Layout();
                return;
            }

            if (peg == selectedPeg)
            {
                Deselect();
                return;
            }

            int disc = Top(selectedPeg);
            if (pegs[peg].Count > 0 && Top(peg) < disc)
            {
                Host.ReportError();
                StartCoroutine(WrongRoutine(peg));
                return;
            }

            pegs[selectedPeg].RemoveAt(pegs[selectedPeg].Count - 1);
            pegs[peg].Add(disc);
            selectedPeg = -1;
            moves++;
            ReportMoves();
            Layout();
            UpdateStatus();

            if (pegs[Goal].Count == discs)
            {
                Host.SetStatus(Loc.Format("hanoi.done", moves, optimal));
                MarkSolved();
            }
        }

        private void Deselect()
        {
            if (selectedPeg < 0) return;
            selectedPeg = -1;
            Layout();
        }

        private int Top(int peg) => pegs[peg][pegs[peg].Count - 1];

        private void ReportMoves() => Host.ReportMoves(moves, optimal);

        /// <summary>
        /// Próximo movimiento óptimo desde cualquier posición válida. Se recorre del disco más grande al más
        /// chico: si un disco no está donde debe, debe ir a ese destino y todos los más chicos tienen que
        /// juntarse en la torre restante. El último disco "mal ubicado" que se encuentra es el que se mueve.
        /// </summary>
        private (int from, int to) NextOptimalMove()
        {
            var position = new int[discs];
            for (int p = 0; p < 3; p++) foreach (int d in pegs[p]) position[d] = p;

            int target = Goal, from = -1, to = -1;
            for (int d = discs - 1; d >= 0; d--)
            {
                if (position[d] == target) continue;
                from = position[d];
                to = target;
                target = 3 - from - target;
            }
            return (from, to);
        }

        // ------------------------------------------------------------ Visual

        private void Layout()
        {
            for (int p = 0; p < 3; p++)
            {
                columns[p].color = p == selectedPeg ? ColumnSelected : ColumnIdle;
                float x = (p - 1) * PegSpacing;
                for (int i = 0; i < pegs[p].Count; i++)
                {
                    int d = pegs[p][i];
                    bool lifted = p == selectedPeg && i == pegs[p].Count - 1;
                    float y = lifted ? BaseY + 380f : BaseY + DiscHeight / 2f + i * DiscHeight;
                    var rt = discViews[d];
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(x, y);
                }
            }
        }

        private void UpdateStatus() => Host.SetStatus(Loc.Format("hanoi.status", moves, optimal));

        private IEnumerator WrongRoutine(int peg)
        {
            busy = true;
            Host.SetStatus(Loc.Get("hanoi.illegal"));
            columns[peg].color = ColumnWrong;
            yield return new WaitForSeconds(0.6f);
            busy = false;
            Deselect();
            Layout();
            UpdateStatus();
        }

        private IEnumerator HintRoutine(int from, int to)
        {
            busy = true;
            Host.SetStatus(Loc.Format("hanoi.hint", PegNames[from], PegNames[to]));
            for (int i = 0; i < 3; i++)
            {
                columns[from].color = ColumnHint;
                columns[to].color = ColumnHint;
                yield return new WaitForSeconds(0.35f);
                columns[from].color = ColumnIdle;
                columns[to].color = ColumnIdle;
                yield return new WaitForSeconds(0.2f);
            }
            busy = false;
            // El texto de la pista queda visible hasta el próximo movimiento.
        }
    }
}
