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
