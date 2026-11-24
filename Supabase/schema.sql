-- =============================================================================
-- PrototipoTesis · Base de datos en Supabase
-- Pegar todo este archivo en Supabase > SQL Editor > New query, y ejecutar (Run).
-- Se puede volver a ejecutar sin problema: no borra datos existentes.
-- =============================================================================

-- 1. Tabla: una fila por sesión. "data" guarda el registro completo tal como lo genera el juego
--    (JSON con los puzzles y la encuesta). Las demás columnas son copias para filtrar fácil.
create table if not exists public.sessions (
    id                bigint generated always as identity primary key,
    received_at       timestamptz not null default now(),
    session_id        text not null unique,
    participant_code  text not null,
    condition         text not null check (condition in ('fixed', 'adaptive')),
    debug_session     boolean not null default false,
    platform          text,
    build_label       text,
    schema_version    text,
    completed         boolean not null default false,
    abandoned         boolean not null default false,
    survey_completed  boolean not null default false,
    data              jsonb not null,
    -- Límite de tamaño para que nadie pueda llenar la base con basura usando la clave pública.
    constraint data_size check (octet_length(data::text) < 200000),
    constraint session_id_format check (session_id ~ '^[0-9a-f]{32}$')
);

-- 2. Seguridad (RLS). La clave publishable del juego solo puede INSERTAR.
--    No hay políticas de lectura, modificación ni borrado: desde afuera no se puede ver ni tocar nada.
--    Vos ves y exportás los datos desde el panel de Supabase (Table Editor / SQL Editor).
alter table public.sessions enable row level security;

drop policy if exists "game can insert sessions" on public.sessions;
create policy "game can insert sessions"
    on public.sessions
    for insert
    to anon
    with check (true);

revoke all on public.sessions from anon, authenticated;
grant insert on public.sessions to anon;

-- 3. Vistas para el análisis (exportables a CSV desde el SQL Editor).
--    security_invoker = true hace que respeten la seguridad de la tabla: la clave pública no las puede leer.

-- 3a. Una fila por sesión, con la encuesta en columnas.
create or replace view public.v_sessions with (security_invoker = true) as
select
    s.received_at,
    s.participant_code,
    s.condition,
    s.debug_session,
    s.platform,
    s.build_label,
    s.completed,
    s.abandoned,
    s.survey_completed,
    (s.data->>'totalSessionSeconds')::numeric          as total_session_seconds,
    (s.data->>'puzzlesCompleted')::int                 as puzzles_completed,
    (s.data->>'abandonedAtSlot')::int                  as abandoned_at_slot,
    (s.data->>'priorGamingExperience')::int            as prior_gaming,
    (s.data->>'priorPuzzleExperience')::int            as prior_puzzles,
    s.data->>'language'                                as language,
    s.data->>'operatingSystem'                         as operating_system,
    s.data->>'processorType'                           as processor,
    (s.data->>'systemMemoryMB')::int                   as memory_mb,
    s.data->>'graphicsDevice'                          as graphics_device,
    (s.data->>'screenWidth')::int                      as screen_width,
    (s.data->>'screenHeight')::int                     as screen_height,
    (s.data->>'surveySeconds')::numeric                as survey_seconds,
    (s.data->'survey'->>'q01_entertaining')::int       as q01_entertaining,
    (s.data->'survey'->>'q02_challenging')::int        as q02_challenging,
    (s.data->'survey'->>'q03_matchedSkills')::int      as q03_matched_skills,
    (s.data->'survey'->>'q04_frustration')::int        as q04_frustration,
    (s.data->'survey'->>'q05_boredom')::int            as q05_boredom,
    (s.data->'survey'->>'q06_consistent')::int         as q06_consistent,
    (s.data->'survey'->>'q07_satisfaction')::int       as q07_satisfaction,
    (s.data->'survey'->>'q08_tooHard')::int            as q08_too_hard,
    (s.data->'survey'->>'q09_tooEasy')::int            as q09_too_easy,
    (s.data->'survey'->>'q10_progress')::int           as q10_progress,
    (s.data->'survey'->>'q11_motivation')::int         as q11_motivation,
    (s.data->'survey'->>'q12_attention')::int          as q12_attention,
    (s.data->'survey'->>'q13_hintsUseful')::int        as q13_hints_useful,      -- -1 = no utilizó ayudas
    s.data->'survey'->>'q14_noticedChanges'            as q14_noticed_changes,   -- yes / no / unsure
    (s.data->'survey'->>'q15_changesNatural')::int     as q15_changes_natural,   -- -1 = no aplica
    (s.data->'survey'->>'q16_rewarded')::int           as q16_rewarded,
    s.data->'survey'->>'q17_playAgain'                 as q17_play_again,        -- yes / no / maybe
    (s.data->'survey'->>'q18_recommend')::int          as q18_recommend,
    s.data->'survey'->>'q19_enjoyedMost'               as q19_enjoyed_most,
    s.data->'survey'->>'q20_improve'                   as q20_improve
from public.sessions s;

-- 3b. Una fila por puzzle jugado (para tiempos, errores, tiers y evolución de la dificultad).
create or replace view public.v_puzzles with (security_invoker = true) as
select
    s.participant_code,
    s.condition,
    s.debug_session,
    s.platform,
    (p->>'slot')::int                      as slot,
    p->>'puzzleType'                       as puzzle_type,
    (p->>'difficultyTier')::int            as tier,
    p->>'difficultySource'                 as tier_source,
    (p->>'previousTier')::int              as previous_tier,
    p->>'decision'                         as decision,
    (p->>'decisionWindowAverage')::numeric as decision_window_average,
    (p->>'timeSeconds')::numeric           as time_seconds,
    (p->>'errors')::int                    as errors,
    (p->>'attempts')::int                  as attempts,
    (p->>'hintsUsed')::int                 as hints_used,
    (p->>'completed')::boolean             as completed,
    (p->>'timedOut')::boolean              as timed_out,
    (p->>'abandoned')::boolean             as abandoned,
    (p->>'debugSkipped')::boolean          as debug_skipped,
    (p->>'performanceScore')::numeric      as performance_score,
    (p->>'scoreTime')::numeric             as score_time,
    (p->>'scoreErrors')::numeric           as score_errors,
    (p->>'scoreAttempts')::numeric         as score_attempts,
    (p->>'scoreHints')::numeric            as score_hints,
    (p->>'moves')::int                     as moves,
    (p->>'optimalMoves')::int              as optimal_moves
from public.sessions s
cross join lateral jsonb_array_elements(s.data->'puzzles') as p;

revoke all on public.v_sessions, public.v_puzzles from anon, authenticated;

-- =============================================================================
-- Consultas útiles (ejecutar de a una; el botón "Export" del resultado baja un CSV):
--
--   Sesiones válidas del experimento (sin pruebas del editor):
--     select * from v_sessions
--     where not debug_session and platform <> 'WindowsEditor'
--     order by received_at;
--
--   Puzzles de esas sesiones:
--     select * from v_puzzles
--     where not debug_session and platform <> 'WindowsEditor'
--     order by participant_code, slot;
--
--   Antes del experimento, borrar las sesiones de prueba (NO se puede deshacer):
--     delete from sessions where debug_session or platform = 'WindowsEditor';
-- =============================================================================
