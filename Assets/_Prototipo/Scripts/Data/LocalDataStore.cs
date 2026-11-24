using System;
using System.IO;
using UnityEngine;

namespace Prototipo
{
    /// <summary>
    /// Guarda cada sesión como JSON en disco, en
    /// %USERPROFILE%/AppData/LocalLow/&lt;Company&gt;/&lt;Product&gt;/sessions/.
    /// Las sesiones van a "pending" y se mueven a "uploaded" cuando la subida confirma.
    /// Se guarda después de cada puzzle, así un cierre inesperado no pierde datos.
    /// </summary>
    public class LocalDataStore
    {
        public string RootFolder { get; }
        public string PendingFolder { get; }
        public string UploadedFolder { get; }

        public LocalDataStore()
        {
            RootFolder = Path.Combine(Application.persistentDataPath, "sessions");
            PendingFolder = Path.Combine(RootFolder, "pending");
            UploadedFolder = Path.Combine(RootFolder, "uploaded");
            Directory.CreateDirectory(PendingFolder);
            Directory.CreateDirectory(UploadedFolder);
        }

        public bool Save(SessionRecord record)
        {
            if (record == null) return false;
            try
            {
                string path = Path.Combine(PendingFolder, record.sessionId + ".json");
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(record, true));
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LocalDataStore] No se pudo guardar la sesión: {e}");
                return false;
            }
        }

        public void MarkUploaded(string sessionId)
        {
            try
            {
                string from = Path.Combine(PendingFolder, sessionId + ".json");
                string to = Path.Combine(UploadedFolder, sessionId + ".json");
                if (!File.Exists(from)) return;
                if (File.Exists(to)) File.Delete(to);
                File.Move(from, to);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LocalDataStore] No se pudo marcar como subida: {e}");
            }
        }

        public string[] GetPendingFiles()
        {
            try { return Directory.GetFiles(PendingFolder, "*.json"); }
            catch { return Array.Empty<string>(); }
        }

        public SessionRecord Load(string path)
        {
            try { return JsonUtility.FromJson<SessionRecord>(File.ReadAllText(path)); }
            catch (Exception e)
            {
                Debug.LogError($"[LocalDataStore] No se pudo leer {path}: {e}");
                return null;
            }
        }
    }
}
