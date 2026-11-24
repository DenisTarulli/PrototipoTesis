using System.Collections.Generic;

namespace Prototipo
{
    /// <summary>
    /// Todos los textos del juego. Columna 0 = español, columna 1 = inglés.
    /// Si falta la traducción, se muestra el texto en español.
    /// </summary>
    public static class Strings
    {
        public static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            // General
            { "app.title", new[] { "Puzzles Cognitivos", "Cognitive Puzzles" } },
            { "common.back", new[] { "Volver", "Back" } },
            { "common.continue", new[] { "Continuar", "Continue" } },

            // Menú principal
            { "menu.play", new[] { "Comenzar", "Start" } },
            { "menu.settings", new[] { "Ajustes", "Settings" } },
            { "menu.quit", new[] { "Salir", "Quit" } },

            // Ajustes
            { "settings.title", new[] { "Ajustes", "Settings" } },
            { "settings.volume", new[] { "Volumen", "Volume" } },
            { "settings.language", new[] { "Idioma", "Language" } },
            { "settings.close", new[] { "Cerrar", "Close" } },
            { "lang.es", new[] { "Español", "Español" } },
            { "lang.en", new[] { "English", "English" } },

            // Consentimiento (Anexo D)
            { "consent.title", new[] { "Consentimiento informado", "Informed consent" } },
            { "consent.body", new[] {
                "Usted está siendo invitado/a a participar en una investigación académica correspondiente al trabajo final de la Licenciatura en Producción de Simuladores y Videojuegos, cuyo objetivo es evaluar el impacto de un sistema de dificultad adaptativa sobre la experiencia del jugador en videojuegos basados en puzzles cognitivos.\n\n" +
                "Su participación consistirá en una sesión de juego. Durante la partida se registrarán automáticamente métricas de desempeño, como el tiempo de resolución, la cantidad de errores, el número de intentos y el uso de ayudas. Al finalizar, se le pedirá completar un breve cuestionario.\n\n" +
                "La participación es completamente voluntaria. Puede retirarse en cualquier momento, sin dar explicaciones y sin ninguna consecuencia.\n\n" +
                "Toda la información será tratada de manera anónima y confidencial, y se utilizará exclusivamente con fines académicos. En ningún caso se publicará información que permita identificarlo/a. La actividad no implica riesgos más allá de los de una sesión habitual de videojuegos.",
                "You are invited to take part in an academic study for the final project of the Bachelor's degree in Simulator and Video Game Production. Its goal is to evaluate the impact of an adaptive difficulty system on player experience in cognitive puzzle video games.\n\n" +
                "Your participation consists of one play session. During the session, performance metrics such as solving time, number of errors, number of attempts and hint usage will be recorded automatically. At the end, you will be asked to complete a short questionnaire.\n\n" +
                "Participation is completely voluntary. You may withdraw at any time, without giving any reason and without any consequence.\n\n" +
                "All information will be handled anonymously and confidentially, and used exclusively for academic purposes. No information that could identify you will ever be published. The activity involves no risks beyond those of a regular video game session." } },
            { "consent.accept", new[] { "He leído la información y acepto participar voluntariamente.", "I have read the information and voluntarily agree to participate." } },
            { "consent.decline", new[] { "No acepto", "I do not agree" } },

            // Perfil previo
            { "profile.title", new[] { "Antes de empezar", "Before you start" } },
            { "profile.adult", new[] { "Confirmo que soy mayor de 18 años.", "I confirm that I am over 18 years old." } },
            { "profile.gaming", new[] { "¿Cuánta experiencia tenés con videojuegos?", "How much experience do you have with video games?" } },
            { "profile.puzzles", new[] { "¿Cuánta experiencia tenés con juegos de puzzles o acertijos?", "How much experience do you have with puzzle games or riddles?" } },
            { "profile.scaleLow", new[] { "1 = Ninguna", "1 = None" } },
            { "profile.scaleHigh", new[] { "5 = Mucha", "5 = A lot" } },

            // Instrucciones ({0} = cantidad de puzzles, {1} = minutos por puzzle)
            { "instructions.title", new[] { "Instrucciones", "Instructions" } },
            { "instructions.body", new[] {
                "Vas a resolver {0} puzzles de distintos tipos, uno después del otro.\n\n" +
                "•  Cada puzzle tiene un tiempo máximo de {1} minutos. Si se agota, se pasa automáticamente al siguiente.\n" +
                "•  Si te trabás, podés pedir una pista con el botón «Pista».\n" +
                "•  Intentá resolver cada puzzle lo mejor que puedas.\n" +
                "•  Podés abandonar la sesión en cualquier momento con el botón «Abandonar».",
                "You will solve {0} puzzles of different types, one after another.\n\n" +
                "•  Each puzzle has a time limit of {1} minutes. When time runs out, the next puzzle starts automatically.\n" +
                "•  If you get stuck, you can ask for a hint with the \"Hint\" button.\n" +
                "•  Try to solve each puzzle as well as you can.\n" +
                "•  You can leave the session at any time with the \"Quit session\" button." } },
            { "instructions.start", new[] { "Empezar", "Begin" } },

            // Juego
            { "game.header", new[] { "Puzzle {0} de {1}", "Puzzle {0} of {1}" } },
            { "game.hint", new[] { "Pista", "Hint" } },
            { "game.abandon", new[] { "Abandonar", "Quit session" } },
            { "game.abandonConfirm", new[] { "¿Querés abandonar la sesión?\nLo registrado hasta ahora se va a guardar.", "Do you want to quit the session?\nEverything recorded so far will be saved." } },
            { "game.abandonYes", new[] { "Sí, abandonar", "Yes, quit" } },
            { "game.abandonNo", new[] { "Seguir jugando", "Keep playing" } },
            { "game.debugSolve", new[] { "Simular resuelto", "Simulate solved" } },
            { "game.debugError", new[] { "Simular error", "Simulate error" } },
            { "game.debugAttempt", new[] { "Simular nuevo intento", "Simulate new attempt" } },
            { "game.notImplemented", new[] { "(Puzzle en desarrollo: usá los botones de simulación)", "(Puzzle in development: use the simulation buttons)" } },
            { "game.timeUp", new[] { "Se acabó el tiempo. Pasamos al siguiente puzzle.", "Time's up. Moving on to the next puzzle." } },
            { "game.solved", new[] { "¡Resuelto!", "Solved!" } },

            // Tutoriales (se muestran la primera vez que aparece cada tipo)
            { "tutorial.howTo", new[] { "Cómo se juega", "How to play" } },
            { "tutorial.start", new[] { "Entendido, empezar", "Got it, start" } },
            { "tutorial.Sequence", new[] {
                "•  Algunos botones se van a iluminar uno por uno.\n" +
                "•  Cuando termine, tocá los botones en el mismo orden.\n" +
                "•  Son 3 rondas, cada una con una secuencia nueva.\n" +
                "•  Si te equivocás, la secuencia se vuelve a mostrar y lo intentás de nuevo.\n" +
                "•  La pista vuelve a mostrar la secuencia de la ronda.",
                "•  Some buttons will light up one by one.\n" +
                "•  When it finishes, press the buttons in the same order.\n" +
                "•  There are 3 rounds, each with a new sequence.\n" +
                "•  If you make a mistake, the sequence is shown again and you try again.\n" +
                "•  The hint shows the round's sequence again." } },
            { "tutorial.MemoryPairs", new[] { "(Tutorial pendiente)", "(Tutorial pending)" } },
            { "tutorial.Hanoi", new[] { "(Tutorial pendiente)", "(Tutorial pending)" } },
            { "tutorial.SlidingTiles", new[] { "(Tutorial pendiente)", "(Tutorial pending)" } },

            // Secuencias
            { "seq.watch", new[] { "Mirá la secuencia  ·  ronda {0} de {1}", "Watch the sequence  ·  round {0} of {1}" } },
            { "seq.yourTurn", new[] { "Tu turno: {0} / {1}", "Your turn: {0} / {1}" } },
            { "seq.wrong", new[] { "Incorrecto. Mirá de nuevo.", "Wrong. Watch again." } },
            { "seq.roundOk", new[] { "¡Bien!", "Well done!" } },
            { "seq.done", new[] { "¡Resuelto!", "Solved!" } },

            // Tipos de puzzle
            { "puzzle.Sequence", new[] { "Secuencias", "Sequences" } },
            { "puzzle.MemoryPairs", new[] { "Pares de memoria", "Memory pairs" } },
            { "puzzle.Hanoi", new[] { "Torre de Hanoi", "Tower of Hanoi" } },
            { "puzzle.SlidingTiles", new[] { "Fichas deslizantes", "Sliding tiles" } },

            // Fin
            { "end.title", new[] { "¡Gracias por participar!", "Thank you for participating!" } },
            { "end.abandonedTitle", new[] { "Sesión finalizada", "Session ended" } },
            { "end.body", new[] { "Tu participación quedó registrada de forma anónima.", "Your participation was recorded anonymously." } },
            { "end.code", new[] { "Código de participante: {0}", "Participant code: {0}" } },
            { "end.status.uploading", new[] { "Enviando datos...", "Sending data..." } },
            { "end.status.uploaded", new[] { "Datos enviados correctamente.", "Data sent successfully." } },
            { "end.status.pending", new[] { "Los datos quedaron guardados en esta computadora y se enviarán más adelante.", "The data was saved on this computer and will be sent later." } },
            { "end.quit", new[] { "Salir", "Quit" } },
            { "end.menu", new[] { "Volver al menú (prueba)", "Back to menu (test)" } },
        };
    }
}
