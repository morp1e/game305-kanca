using Game.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Başlık ekranı: oyunu başlat, çık. Klavyeyle de gezilebilsin diye ilk düğme seçili açılır.</summary>
    public sealed class TitleMenu : MonoBehaviour
    {
        [SerializeField] Button startButton;
        [SerializeField] Button quitButton;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text subtitleLabel;
        [SerializeField] TMP_Text startLabel;
        [SerializeField] TMP_Text quitLabel;
        [SerializeField] TMP_Text controlsLabel;
        [SerializeField] string titleText = "KANCA";
        [SerializeField] string subtitleText = "Konuşan bir kılıç, sol elde bir kanca, yolun sonunda bir bekçi.";
        [SerializeField] string startText = "Başla";
        [SerializeField] string quitText = "Çık";
        [SerializeField, TextArea] string controlsText = "A / D  yürü     Space  zıpla     Sağ tık  kanca     Sol tık  kılıç     Esc  duraklat";

        void Awake()
        {
            SetLabel(titleLabel, titleText);
            SetLabel(subtitleLabel, subtitleText);
            SetLabel(startLabel, startText);
            SetLabel(quitLabel, quitText);
            SetLabel(controlsLabel, controlsText);
            if (startButton != null) startButton.onClick.AddListener(StartGame);
            if (quitButton != null) quitButton.onClick.AddListener(Application.Quit);
        }

        void Start()
        {
            Time.timeScale = 1f;
            if (startButton != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        }

        void StartGame() => SceneFlow.Ensure().StartGame();

        static void SetLabel(TMP_Text label, string text)
        {
            if (label != null) label.text = text;
        }
    }
}
