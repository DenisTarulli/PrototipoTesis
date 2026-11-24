using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Prototipo
{
    public class EndController : MonoBehaviour
    {
        public Text titleText;
        public Text codeText;
        public Text statusText;
        public Button quitButton;
        public Button menuButton;

        [Header("Respaldo si no se pudo enviar")]
        public GameObject pendingPanel;
        public Button retryButton;
        public Button openFolderButton;

        private SessionManager session;

        private void Start()
        {
            session = SessionManager.Instance;
            var record = session.Current;

            titleText.text = Loc.Get(record != null && record.abandoned ? "end.abandonedTitle" : "end.title");
            codeText.text = record != null ? Loc.Format("end.code", record.participantCode) : string.Empty;

            quitButton.onClick.AddListener(Quit);
            menuButton.gameObject.SetActive(session.Config.showDebugInfo);
            menuButton.onClick.AddListener(() => SceneNames.Load(SceneNames.MainMenu));

            if (retryButton != null) retryButton.onClick.AddListener(session.RetryUpload);
            if (openFolderButton != null) openFolderButton.onClick.AddListener(OpenDataFile);
            if (pendingPanel != null) pendingPanel.SetActive(false);
        }

        private void Update()
        {
            switch (session.Upload)
            {
                case SessionManager.UploadState.Uploading: statusText.text = Loc.Get("end.status.uploading"); break;
                case SessionManager.UploadState.Uploaded: statusText.text = Loc.Get("end.status.uploaded"); break;
                case SessionManager.UploadState.Pending: statusText.text = Loc.Get("end.status.pending"); break;
                default: statusText.text = string.Empty; break;
            }

            if (pendingPanel != null)
                pendingPanel.SetActive(session.Upload == SessionManager.UploadState.Pending);
        }

        /// <summary>Abre el explorador con el archivo de la sesión seleccionado, para enviarlo a mano.</summary>
        private void OpenDataFile()
        {
            string file = session.CurrentSessionFile;
            if (string.IsNullOrEmpty(file)) return;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (File.Exists(file))
            {
                System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + file.Replace('/', '\\') + "\"");
                return;
            }
#endif
            Application.OpenURL("file:///" + Path.GetDirectoryName(file).Replace('\\', '/'));
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
