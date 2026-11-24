namespace Prototipo
{
    /// <summary>Tipos de puzzle disponibles en el prototipo.</summary>
    public enum PuzzleType
    {
        Sequence,
        MemoryPairs,
        Hanoi,
        SlidingTiles
    }

    /// <summary>Condición experimental (variable independiente).</summary>
    public enum DifficultyCondition
    {
        Fixed,
        Adaptive
    }

    /// <summary>Cómo se asigna la condición a cada participante.</summary>
    public enum AssignmentMode
    {
        /// <summary>Sorteo 50/50 en cada sesión.</summary>
        Random,
        /// <summary>Siempre dificultad fija (build dedicado o pruebas).</summary>
        ForceFixed,
        /// <summary>Siempre dificultad adaptativa (build dedicado o pruebas).</summary>
        ForceAdaptive
    }
}
