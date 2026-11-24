# Prototipo de dificultad adaptativa en puzzles cognitivos

Prototipo experimental desarrollado en Unity para la tesis de la **Licenciatura en Producción de Simuladores y Videojuegos** (Universidad Abierta Interamericana):

> *Diseño e implementación de un sistema de dificultad adaptativa para el equilibrio dinámico entre desafío y habilidad en videojuegos basados en puzzles cognitivos.*

El objetivo es comparar una **dificultad fija** con una **dificultad adaptativa basada en el desempeño** (Dynamic Difficulty Adjustment, DDA) y analizar sus diferencias en frustración, disfrute, dificultad percibida y equilibrio entre desafío y habilidad.

---

## Diseño experimental

- **Diseño entre grupos (between-subjects):** cada participante juega una sola versión.
  - **Versión A:** dificultad fija (rampa predefinida).
  - **Versión B:** dificultad adaptativa.
- Ambas versiones comparten la interfaz, los puzzles, las reglas y la duración. **Solo cambia cómo se elige el nivel de dificultad.**
- El participante no sabe qué versión está jugando.
- Sesión de **10 puzzles** con 4 tipos que rotan en el mismo orden en ambas condiciones. Los 2 primeros son idénticos en las dos versiones y sirven como línea base.
- Antes de jugar: consentimiento informado y perfil de experiencia previa. Al final: encuesta de 20 preguntas.

## Puzzles

Cada puzzle tiene **5 niveles de dificultad (tiers)** con parámetros explícitos.

| Puzzle | Qué varía según el tier | Error | Intento nuevo | Pista |
|---|---|---|---|---|
| **Secuencias** | Cantidad de botones, largo y velocidad de la secuencia | Botón incorrecto | Se repite la secuencia | Volver a mostrar la secuencia |
| **Pares de memoria** | Cantidad de pares (3 a 10) | Desacierto evitable con memoria | — | Mostrar todas las cartas un instante |
| **Torre de Hanoi** | Cantidad de discos (2 a 6) | Movimiento ilegal | Reiniciar | Próximo movimiento óptimo |
| **Fichas deslizantes** | Tamaño de la grilla y mezcla (3×3 y 4×4) | Ficha que no está junto al hueco | Reiniciar | Próximo movimiento óptimo (IDA*) |

## Sistema de dificultad adaptativa

La estrategia es deliberadamente **simple y transparente**, sin modelos de inteligencia artificial, para poder explicar cada decisión.

1. **Puntaje de desempeño (0 a 1) por puzzle:** suma ponderada de cuatro métricas.

   | Métrica | Peso | Criterio |
   |---|---|---|
   | Tiempo de resolución | 0,35 | Normalizado según el tiempo esperado del puzzle y el tier |
   | Errores | 0,30 | Hasta la tolerancia de ese puzzle y tier |
   | Intentos | 0,15 | |
   | Uso de ayudas | 0,20 | |

   Un puzzle no resuelto vale 0.
2. **Decisión:** se promedian los últimos 2 puntajes.
   - Si el promedio es **≥ 0,70**, sube un tier.
   - Si es **≤ 0,40**, baja un tier.
   - Entre esos dos valores se mantiene. Esta banda intermedia evita que la dificultad oscile.
3. El tier cambia como máximo **±1 por puzzle** y siempre queda entre 1 y 5.

En cada puzzle se registran el puntaje y su desglose, el tier anterior, el promedio usado y la decisión tomada, para reconstruir la evolución de la dificultad. El puntaje se calcula en las dos condiciones, aunque solo la adaptativa lo usa para decidir.

Todos los parámetros se editan desde el asset `ExperimentConfig`.

## Datos registrados

Todo se registra de forma **anónima**: cada sesión se identifica con un código aleatorio.

- **Por puzzle:** tipo, tier, tiempo, errores, intentos, ayudas, si se completó, si se agotó el tiempo, si se abandonó, puntaje y decisión adaptativa. En Hanoi y Fichas, además, los movimientos realizados y el mínimo posible.
- **Por sesión:** condición, tiempo total, puzzles completados, abandono, experiencia previa, idioma y características técnicas del equipo.
- **Encuesta final:** 20 preguntas. La mayoría son escalas del 1 al 5; también hay opciones cerradas y dos respuestas abiertas.

Cada sesión se guarda primero en disco, después de cada puzzle, y al terminar se sube a **Supabase (PostgreSQL)** con reintentos. Si no hay conexión, se reintenta al volver a abrir el juego y la pantalla final ofrece enviar el archivo a mano. La base de datos solo acepta inserciones con la clave pública. El esquema, las reglas de seguridad y las vistas para exportar a CSV están en [`Supabase/schema.sql`](Supabase/schema.sql).

## Tecnología

- Unity 6 (6000.0.77f1), Universal Render Pipeline 2D, uGUI e Input System.
- C#, Supabase (PostgreSQL + API REST).
- Interfaz en español e inglés, con tema oscuro.

## Estructura del proyecto

```
Assets/_Prototipo/
├── Scripts/
│   ├── Core/          Sesión, configuración del experimento, escenas
│   ├── Difficulty/    Puntaje de desempeño y proveedores de dificultad (fija / adaptativa)
│   ├── Puzzles/       Los 4 puzzles, base común y solucionador de fichas deslizantes
│   ├── Data/          Registro de métricas, guardado local y subida a Supabase
│   ├── UI/            Controladores de escenas, encuesta y componentes de interfaz
│   └── Localization/  Textos en español e inglés
├── Editor/            Generación de escenas, builds por condición y simulador del algoritmo
├── Resources/         ExperimentConfig (parámetros del experimento)
└── Scenes/            MainMenu → Consent → Game → Survey → End
Supabase/schema.sql    Tabla, seguridad y vistas de análisis
```

## Uso en el editor

Las herramientas están en el menú **Tools > Prototipo**:

| Opción | Qué hace |
|---|---|
| **Construir escenas** | Genera las 5 escenas desde código. |
| **Seleccionar configuración del experimento** | Abre `ExperimentConfig`: rampa fija, parámetros del algoritmo, tiempos, Supabase. |
| **Simular algoritmo adaptativo** | Juega sesiones con jugadores sintéticos de distinta habilidad y exporta un CSV con la evolución del tier. |
| **Generar builds del experimento** | Crea los ejecutables de Windows `Version_A` (fija) y `Version_B` (adaptativa), idénticos por fuera y sin información de depuración. |

Para probar, abrí `MainMenu` y apretá Play. Con `showDebugInfo` activado se muestran el tier, la decisión del algoritmo y botones para simular o saltar puzzles.

## Estado

En desarrollo. Los 4 puzzles, el sistema adaptativo, la encuesta y la subida de datos están implementados. Falta la calibración de la dificultad y la prueba piloto antes del experimento con participantes.

---

**Autor:** Denis Tarulli · Universidad Abierta Interamericana
