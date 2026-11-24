using System;
using UnityEngine;

namespace Prototipo
{
    /// <summary>
    /// Mide las métricas de un puzzle en curso. Los puzzles solo informan eventos
    /// (error, nuevo intento, pista) y el tracker arma el PuzzleRecord.
    /// </summary>
    public class PuzzleMetricsTracker
    {
        private PuzzleRecord record;
        private float startRealtime;

        public bool IsRunning { get; private set; }
        public int HintsUsed => record != null ? record.hintsUsed : 0;
        public int Errors => record != null ? record.errors : 0;

        public float Elapsed => IsRunning
            ? Time.realtimeSinceStartup - startRealtime
            : (record != null ? record.timeSeconds : 0f);

        public void Begin(SlotPlan plan)
        {
            record = new PuzzleRecord
            {
                slot = plan.slotIndex + 1,
                puzzleType = plan.type.ToString(),
                difficultyTier = plan.tier,
                difficultySource = plan.source,
                previousTier = plan.previousTier,
                decisionWindowAverage = plan.windowAverage,
                decision = plan.decision,
                startedAtUtc = DateTime.UtcNow.ToString("o"),
                attempts = 1
            };
            startRealtime = Time.realtimeSinceStartup;
            IsRunning = true;
        }

        public void RegisterError()
        {
            if (IsRunning) record.errors++;
        }

        public void RegisterNewAttempt()
        {
            if (IsRunning) record.attempts++;
        }

        public void RegisterHint()
        {
            if (IsRunning) record.hintsUsed++;
        }

        public void SetMoves(int moves, int optimalMoves)
        {
            if (!IsRunning) return;
            record.moves = moves;
            record.optimalMoves = optimalMoves;
        }

        public PuzzleRecord Complete(bool solved, bool timedOut)
        {
            return Close(r =>
            {
                r.completed = solved;
                r.timedOut = timedOut;
            });
        }

        public PuzzleRecord Abandon()
        {
            return Close(r => r.abandoned = true);
        }

        private PuzzleRecord Close(Action<PuzzleRecord> apply)
        {
            if (!IsRunning) return null;
            record.timeSeconds = Time.realtimeSinceStartup - startRealtime;
            apply(record);
            IsRunning = false;
            return record;
        }
    }
}
