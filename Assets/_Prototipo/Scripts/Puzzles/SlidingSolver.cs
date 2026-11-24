using System;
using System.Collections.Generic;

namespace Prototipo
{
    /// <summary>
    /// Resolución del puzzle de fichas deslizantes (sin dependencias de Unity, para poder probarlo aparte).
    ///
    /// Estado: arreglo de n×n con las fichas 1..n²-1 y 0 para el hueco. Meta: 1, 2, ..., n²-1 y el hueco al final.
    /// Se usa IDA* con la distancia Manhattan (heurística admisible), así que la solución encontrada es óptima.
    /// Tiene un tope de nodos para no congelar el juego; si se supera, se devuelve false.
    /// </summary>
    public static class SlidingSolver
    {
        private const int Found = -1;
        private const int Aborted = -2;

        /// <summary>Posiciones (índices del arreglo) de las fichas que hay que mover, en orden, para resolver.</summary>
        public static bool TrySolve(int[] start, int n, int nodeBudget, out List<int> path)
        {
            var state = (int[])start.Clone();
            path = new List<int>();
            int blank = Array.IndexOf(state, 0);
            int h = Manhattan(state, n);
            if (h == 0) return true;

            int bound = h;
            int nodes = 0;
            while (true)
            {
                int t = Search(state, n, blank, -1, 0, h, bound, path, ref nodes, nodeBudget);
                if (t == Found) return true;
                if (t == Aborted || t == int.MaxValue) { path.Clear(); return false; }
                bound = t;
            }
        }

        private static int Search(int[] s, int n, int blank, int prevBlank, int g, int h, int bound,
                                  List<int> path, ref int nodes, int budget)
        {
            int f = g + h;
            if (f > bound) return f;
            if (h == 0) return Found;
            if (++nodes > budget) return Aborted;

            int min = int.MaxValue;
            int br = blank / n, bc = blank % n;
            for (int dir = 0; dir < 4; dir++)
            {
                int r = br + (dir == 0 ? -1 : dir == 1 ? 1 : 0);
                int c = bc + (dir == 2 ? -1 : dir == 3 ? 1 : 0);
                if (r < 0 || r >= n || c < 0 || c >= n) continue;
                int np = r * n + c;
                if (np == prevBlank) continue; // no deshacer el movimiento anterior

                int tile = s[np];
                int nh = h - Dist(tile, np, n) + Dist(tile, blank, n);
                s[blank] = tile; s[np] = 0;
                path.Add(np);

                int t = Search(s, n, np, blank, g + 1, nh, bound, path, ref nodes, budget);
                if (t == Found) return Found;
                path.RemoveAt(path.Count - 1);
                s[np] = tile; s[blank] = 0;

                if (t == Aborted) return Aborted;
                if (t < min) min = t;
            }
            return min;
        }

        public static int Manhattan(int[] s, int n)
        {
            int sum = 0;
            for (int i = 0; i < s.Length; i++)
                if (s[i] != 0) sum += Dist(s[i], i, n);
            return sum;
        }

        private static int Dist(int tile, int pos, int n)
        {
            int goal = tile - 1;
            return Math.Abs(goal / n - pos / n) + Math.Abs(goal % n - pos % n);
        }

        public static bool IsSolved(int[] s)
        {
            for (int i = 0; i < s.Length - 1; i++) if (s[i] != i + 1) return false;
            return s[s.Length - 1] == 0;
        }

        /// <summary>Posiciones de las fichas que pueden moverse al hueco.</summary>
        public static List<int> Movable(int[] s, int n)
        {
            var list = new List<int>(4);
            int blank = Array.IndexOf(s, 0);
            int br = blank / n, bc = blank % n;
            if (br > 0) list.Add(blank - n);
            if (br < n - 1) list.Add(blank + n);
            if (bc > 0) list.Add(blank - 1);
            if (bc < n - 1) list.Add(blank + 1);
            return list;
        }

        /// <summary>Mueve la ficha de <paramref name="pos"/> al hueco, si es adyacente. Devuelve false si no se puede.</summary>
        public static bool TryMove(int[] s, int n, int pos)
        {
            int blank = Array.IndexOf(s, 0);
            if (Math.Abs(blank / n - pos / n) + Math.Abs(blank % n - pos % n) != 1) return false;
            s[blank] = s[pos];
            s[pos] = 0;
            return true;
        }

        /// <summary>
        /// Mezcla desde el estado resuelto con movimientos al azar (siempre resoluble), sin deshacer el
        /// paso anterior. Si por azar queda resuelto, se sigue mezclando.
        /// </summary>
        public static int[] Scramble(int n, int moves, Random rng)
        {
            var s = new int[n * n];
            for (int i = 0; i < s.Length - 1; i++) s[i] = i + 1;
            s[s.Length - 1] = 0;

            int prevBlank = -1;
            int done = 0;
            while (done < moves || IsSolved(s))
            {
                int blank = Array.IndexOf(s, 0);
                var options = Movable(s, n);
                options.Remove(prevBlank);
                int pick = options[rng.Next(options.Count)];
                TryMove(s, n, pick);
                prevBlank = blank;
                done++;
            }
            return s;
        }

        /// <summary>
        /// Próxima ficha a mover según la solución óptima. Si el tope de nodos no alcanza (el jugador se alejó
        /// mucho de la solución), se elige la ficha que más reduce la distancia Manhattan.
        /// </summary>
        public static int NextMove(int[] s, int n, int nodeBudget, out bool optimal)
        {
            if (TrySolve(s, n, nodeBudget, out var path) && path.Count > 0)
            {
                optimal = true;
                return path[0];
            }

            optimal = false;
            int best = -1, bestH = int.MaxValue;
            foreach (int pos in Movable(s, n))
            {
                var copy = (int[])s.Clone();
                TryMove(copy, n, pos);
                int h = Manhattan(copy, n);
                if (h < bestH) { bestH = h; best = pos; }
            }
            return best;
        }
    }
}
