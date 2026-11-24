using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Prototipo
{
    /// <summary>
    /// Secuencias: el juego ilumina una serie de botones y el jugador la repite en el mismo orden.
    /// Se completan 3 rondas, cada una con una secuencia nueva.
    ///
    /// Dificultad por tier: cantidad de botones, largo de la secuencia y velocidad de presentación.
    /// Error: tocar un botón incorrecto. Se vuelve a mostrar la misma secuencia y cuenta como nuevo intento.
    /// Pista: vuelve a mostrar la secuencia de la ronda actual (el progreso de esa ronda se reinicia).
    /// </summary>
    public class SequencePuzzle : PuzzleBase
    {
        private struct TierParams
        {
            public int pads, length;
            public float step;
            public TierParams(int pads, int length, float step) { this.pads = pads; this.length = length; this.step = step; }
        }

        private static readonly TierParams[] Tiers =
        {
            new TierParams(4, 3, 0.80f),
            new TierParams(4, 4, 0.70f),
            new TierParams(6, 5, 0.60f),
            new TierParams(6, 6, 0.50f),
            new TierParams(9, 7, 0.45f),
        };

        public const int Rounds = 3;

        // Colores apagados y distinguibles; cada botón además lleva un número (no depender solo del color).
        private static readonly Color32[] PadColors =
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
        };

        private const float DimFactor = 0.38f;

        private TierParams p;
        private readonly List<Image> padImages = new List<Image>();
        private readonly List<Button> padButtons = new List<Button>();
        private readonly List<int> sequence = new List<int>();
        private int round;      // 0-based
        private int progress;   // cuántos acertó en la ronda actual
        private bool acceptingInput;
        private Coroutine routine;

        protected override void Build()
        {
            p = Tiers[Tier - 1];
            int cols = p.pads == 4 ? 2 : 3;
            int rows = Mathf.CeilToInt(p.pads / (float)cols);
            float size = p.pads == 9 ? 150f : 170f;
            float gap = 26f;
            float w = cols * size + (cols - 1) * gap;
            float h = rows * size + (rows - 1) * gap;

            for (int i = 0; i < p.pads; i++)
            {
                int index = i;
                int c = i % cols, r = i / cols;
                var button = Host.UI.Button(Board, "Pad" + (i + 1), Dim(i), (i + 1).ToString(), 44,
                                            new Color(1f, 1f, 1f, 0.85f), () => OnPad(index));
                var pos = new Vector2(-w / 2f + size / 2f + c * (size + gap), h / 2f - size / 2f - r * (size + gap));
                RuntimeUI.Place((RectTransform)button.transform, pos, new Vector2(size, size));
                button.interactable = false;
                padButtons.Add(button);
                padImages.Add((Image)button.targetGraphic);
            }
        }

        public override void Begin()
        {
            round = 0;
            NewSequence();
            Play(ShowSequence(0.6f));
        }

        public override bool CanHint => acceptingInput && !Solved;

        public override bool TryHint()
        {
            if (!CanHint) return false;
            Play(ShowSequence(0.3f));
            return true;
        }

        public override void Stop()
        {
            if (routine != null) StopCoroutine(routine);
            SetInput(false);
            base.Stop();
        }

        // ------------------------------------------------------------ Lógica

        private void NewSequence()
        {
            sequence.Clear();
            int last = -1;
            for (int i = 0; i < p.length; i++)
            {
                int next;
                do { next = Rng.Next(p.pads); } while (next == last); // sin repeticiones seguidas
                sequence.Add(next);
                last = next;
            }
        }

        private void OnPad(int index)
        {
            if (!acceptingInput) return;

            if (index == sequence[progress])
            {
                ResetColors(); // por si el destello anterior seguía encendido
                progress++;
                Play(Flash(index, 0.18f, afterCorrect: true));
            }
            else
            {
                Host.ReportError();
                Host.ReportNewAttempt();
                Play(WrongThenReplay(index));
            }
        }

        private void Play(IEnumerator r)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(r);
        }

        private IEnumerator ShowSequence(float delay)
        {
            SetInput(false);
            progress = 0;
            ResetColors();
            Host.SetStatus(Loc.Format("seq.watch", round + 1, Rounds));
            yield return new WaitForSeconds(delay);

            foreach (int pad in sequence)
            {
                Light(pad, true);
                yield return new WaitForSeconds(p.step * 0.7f);
                Light(pad, false);
                yield return new WaitForSeconds(p.step * 0.3f);
            }

            Host.SetStatus(Loc.Format("seq.yourTurn", 0, p.length));
            SetInput(true);
            routine = null;
        }

        private IEnumerator Flash(int pad, float seconds, bool afterCorrect)
        {
            Light(pad, true);
            if (afterCorrect) Host.SetStatus(Loc.Format("seq.yourTurn", progress, p.length));

            bool roundDone = afterCorrect && progress >= p.length;
            if (roundDone) SetInput(false);

            yield return new WaitForSeconds(seconds);
            Light(pad, false);
            routine = null;

            if (!roundDone) yield break;

            round++;
            if (round >= Rounds)
            {
                Host.SetStatus(Loc.Get("seq.done"));
                MarkSolved();
                yield break;
            }

            Host.SetStatus(Loc.Get("seq.roundOk"));
            yield return new WaitForSeconds(0.7f);
            NewSequence();
            yield return ShowSequence(0.3f);
        }

        private IEnumerator WrongThenReplay(int pad)
        {
            SetInput(false);
            Host.SetStatus(Loc.Get("seq.wrong"));
            padImages[pad].color = UITheme.Danger;
            yield return new WaitForSeconds(0.5f);
            ResetColors();
            yield return new WaitForSeconds(0.4f);
            yield return ShowSequence(0.2f);
        }

        // ------------------------------------------------------------ Visual

        private void SetInput(bool on)
        {
            acceptingInput = on;
            foreach (var b in padButtons) b.interactable = on;
        }

        private void Light(int pad, bool on)
        {
            padImages[pad].color = on ? (Color)PadColors[pad] : Dim(pad);
            padImages[pad].transform.localScale = on ? Vector3.one * 1.06f : Vector3.one;
        }

        private void ResetColors()
        {
            for (int i = 0; i < padImages.Count; i++) Light(i, false);
        }

        private static Color Dim(int pad)
        {
            Color c = PadColors[pad];
            return new Color(c.r * DimFactor, c.g * DimFactor, c.b * DimFactor, 1f);
        }
    }
}
