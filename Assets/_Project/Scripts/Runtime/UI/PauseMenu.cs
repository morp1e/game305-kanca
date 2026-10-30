using Game.Flow;
using Game.Input;
using Game.Level;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Esc ile açılan duraklatma menüsü. Açıkken zaman ölçeği 0'dır; kapanınca, yok edilince veya sahne değişince geri gelir.
    /// Menüdeki tıklamalar oyuncunun saldırısı ya da kancası olarak tamponlanmaz.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button resumeButton;
        [SerializeField] Button checkpointButton;
        [SerializeField] Button titleButton;
        [SerializeField] Button quitButton;
        [SerializeField] TMP_Text headingLabel;
        [SerializeField] TMP_Text resumeLabel;
        [SerializeField] TMP_Text checkpointLabel;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text quitLabel;
        [SerializeField] string headingText = "DURAKLATILDI";
        [SerializeField] string resumeText = "Devam";
        [SerializeField] string checkpointText = "Son checkpoint'e dön";
        [SerializeField] string titleText = "Başlık ekranı";
        [SerializeField] string quitText = "Oyundan çık";

        float previousTimeScale = 1f;

        public bool IsPaused { get; private set; }

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
            SetLabel(headingLabel, headingText);
            SetLabel(resumeLabel, resumeText);
            SetLabel(checkpointLabel, checkpointText);
            SetLabel(titleLabel, titleText);
            SetLabel(quitLabel, quitText);
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (checkpointButton != null) checkpointButton.onClick.AddListener(ReturnToCheckpoint);
            if (titleButton != null) titleButton.onClick.AddListener(GoToTitle);
            if (quitButton != null) quitButton.onClick.AddListener(Application.Quit);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            if (SceneFlow.Instance != null && SceneFlow.Instance.IsLoading) return;
            if (IsPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (IsPaused) return;
            IsPaused = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            if (panel != null) panel.SetActive(true);
            if (resumeButton != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = previousTimeScale;
            if (panel != null) panel.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            ClearPlayerBuffers();
        }

        void ReturnToCheckpoint()
        {
            Resume();
            var respawn = FindFirstObjectByType<RespawnController>();
            if (respawn != null) respawn.Kill();
        }

        void GoToTitle()
        {
            Resume();
            SceneFlow.Ensure().LoadTitle();
        }

        static void ClearPlayerBuffers()
        {
            foreach (var reader in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) reader.ClearBuffers();
        }

        static void SetLabel(TMP_Text label, string text)
        {
            if (label != null) label.text = text;
        }

        void OnDisable()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = previousTimeScale;
        }
    }
}
