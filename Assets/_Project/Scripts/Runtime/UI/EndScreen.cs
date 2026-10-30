using Game.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Bitiş ekranı: süre ve ölüm sayısı, başlığa dönüş. Bitiş sahnesinde durur.</summary>
    public sealed class EndScreen : MonoBehaviour
    {
        [SerializeField] TMP_Text headingLabel;
        [SerializeField] TMP_Text summaryLabel;
        [SerializeField] TMP_Text buttonLabel;
        [SerializeField] Button titleButton;
        [SerializeField] string headingText = "Zincirli Bekçi düştü";
        [Tooltip("{0} süre (dk:sn), {1} ölüm sayısı.")]
        [SerializeField] string summaryFormat = "Süre  {0}     Ölüm  {1}";
        [SerializeField] string buttonText = "Başlık ekranına dön";

        void Awake()
        {
            if (headingLabel != null) headingLabel.text = headingText;
            if (buttonLabel != null) buttonLabel.text = buttonText;
            if (titleButton != null) titleButton.onClick.AddListener(() => SceneFlow.Ensure().LoadTitle());
        }

        void Start()
        {
            Time.timeScale = 1f;
            var stats = SceneFlow.Instance != null ? SceneFlow.Instance.Stats : null;
            if (summaryLabel != null)
                summaryLabel.text = string.Format(summaryFormat, stats != null ? stats.FormattedTime : "00:00", stats != null ? stats.Deaths : 0);
            if (titleButton != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(titleButton.gameObject);
        }
    }
}
