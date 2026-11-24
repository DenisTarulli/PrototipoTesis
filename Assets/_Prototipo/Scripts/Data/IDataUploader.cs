using System;
using System.Collections;

namespace Prototipo
{
    /// <summary>Destino remoto de los datos (Supabase en la etapa siguiente).</summary>
    public interface IDataUploader
    {
        /// <summary>Corrutina de subida. Debe llamar a onDone(true) solo si el servidor confirmó.</summary>
        IEnumerator Upload(SessionRecord record, Action<bool> onDone);
    }

    /// <summary>Uploader vacío: no sube nada y deja la sesión pendiente en disco.</summary>
    public class NullUploader : IDataUploader
    {
        public IEnumerator Upload(SessionRecord record, Action<bool> onDone)
        {
            onDone?.Invoke(false);
            yield break;
        }
    }
}
