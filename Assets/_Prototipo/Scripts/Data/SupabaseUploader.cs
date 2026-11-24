using System;
using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Prototipo
{
    /// <summary>
    /// Sube una sesión a Supabase (tabla definida en Supabase/schema.sql) con un INSERT por la API REST.
    ///
    /// - Reintenta hasta 3 veces con espera creciente si falla la conexión o el servidor.
    /// - Si la sesión ya existía (409, por ejemplo porque una subida anterior llegó pero no se confirmó),
    ///   se considera subida: el session_id es único.
    /// - Usa solo la clave publishable; la tabla no permite leer ni modificar nada con ella.
    /// </summary>
    public class SupabaseUploader : IDataUploader
    {
        private static readonly float[] RetryDelays = { 2f, 6f };
        private const int TimeoutSeconds = 20;

        private readonly ExperimentConfig config;

        public SupabaseUploader(ExperimentConfig config) { this.config = config; }

        public static bool IsConfigured(ExperimentConfig c) =>
            c != null && c.uploadEnabled &&
            !string.IsNullOrWhiteSpace(c.supabaseUrl) && !string.IsNullOrWhiteSpace(c.supabasePublishableKey);

        public IEnumerator Upload(SessionRecord record, Action<bool> onDone)
        {
            if (record == null || !IsConfigured(config))
            {
                onDone?.Invoke(false);
                yield break;
            }

            string url = config.supabaseUrl.TrimEnd('/') + "/rest/v1/" + config.supabaseTable;
            byte[] body = Encoding.UTF8.GetBytes(BuildRow(record));

            for (int attempt = 0; attempt <= RetryDelays.Length; attempt++)
            {
                using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
                {
                    req.uploadHandler = new UploadHandlerRaw(body);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.timeout = TimeoutSeconds;
                    req.SetRequestHeader("Content-Type", "application/json");
                    req.SetRequestHeader("apikey", config.supabasePublishableKey);
                    req.SetRequestHeader("Prefer", "return=minimal");

                    yield return req.SendWebRequest();

                    long code = req.responseCode;
                    if (req.result == UnityWebRequest.Result.Success && code >= 200 && code < 300)
                    {
                        Debug.Log($"[Supabase] Sesión {record.participantCode} subida.");
                        onDone?.Invoke(true);
                        yield break;
                    }

                    if (code == 409)
                    {
                        Debug.Log($"[Supabase] La sesión {record.participantCode} ya estaba en la base.");
                        onDone?.Invoke(true);
                        yield break;
                    }

                    Debug.LogWarning($"[Supabase] Falló la subida (intento {attempt + 1}): HTTP {code} {req.error}\n{req.downloadHandler?.text}");

                    // Errores de datos o de permisos no se arreglan reintentando.
                    if (code == 400 || code == 401 || code == 403 || code == 404) break;
                }

                if (attempt < RetryDelays.Length)
                    yield return new WaitForSecondsRealtime(RetryDelays[attempt]);
            }

            onDone?.Invoke(false);
        }

        /// <summary>Arma la fila: columnas de filtro + el registro completo en "data" (JSON anidado, no texto).</summary>
        private static string BuildRow(SessionRecord r)
        {
            var sb = new StringBuilder(4096);
            sb.Append('{');
            Field(sb, "session_id", r.sessionId); sb.Append(',');
            Field(sb, "participant_code", r.participantCode); sb.Append(',');
            Field(sb, "condition", r.condition); sb.Append(',');
            Field(sb, "debug_session", r.debugSession); sb.Append(',');
            Field(sb, "platform", r.platform); sb.Append(',');
            Field(sb, "build_label", r.buildLabel); sb.Append(',');
            Field(sb, "schema_version", r.schemaVersion); sb.Append(',');
            Field(sb, "completed", r.completed); sb.Append(',');
            Field(sb, "abandoned", r.abandoned); sb.Append(',');
            Field(sb, "survey_completed", r.surveyCompleted); sb.Append(',');
            sb.Append("\"data\":").Append(JsonUtility.ToJson(r));
            sb.Append('}');
            return sb.ToString();
        }

        private static void Field(StringBuilder sb, string name, bool value) =>
            sb.Append('"').Append(name).Append("\":").Append(value ? "true" : "false");

        private static void Field(StringBuilder sb, string name, string value)
        {
            sb.Append('"').Append(name).Append("\":");
            if (value == null) { sb.Append("null"); return; }
            sb.Append('"');
            foreach (char ch in value)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
