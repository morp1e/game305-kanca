using System.Collections;
using Game.Core;
using Game.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Flow
{
    /// <summary>
    /// Sahneler arası geçiş. Her sahne tek başına Play'e basılabilir kalır: SceneFlow ilk ihtiyaçta
    /// Resources'taki prefab'dan kurulur ve DontDestroyOnLoad olur. Bölümler tek modda yüklenir, geçişte ekran kararır.
    /// Oyun bitti kanalını dinler ve bitiş sahnesine geçer.
    /// </summary>
    public sealed class SceneFlow : MonoBehaviour
    {
        public const string PrefabResourcePath = "Flow/SceneFlow";

        [SerializeField] SceneCatalog catalog;
        [SerializeField] ScreenFader fader;
        [SerializeField] GameEvent gameWon;
        [Tooltip("Kararma ve açılma süresi, s. Ö: oynayarak ayarlanacak.")]
        [SerializeField] float fadeSeconds = 0.35f;

        public static SceneFlow Instance { get; private set; }
        public SceneCatalog Catalog => catalog;
        public RunStats Stats { get; private set; }
        public bool IsLoading { get; private set; }

        /// <summary>Varsa mevcut örneği, yoksa Resources prefab'ından (o da yoksa boş) yeni bir örnek döner.</summary>
        public static SceneFlow Ensure()
        {
            if (Instance != null) return Instance;
            var prefab = Resources.Load<SceneFlow>(PrefabResourcePath);
            var flow = prefab != null ? Instantiate(prefab) : new GameObject("SceneFlow").AddComponent<SceneFlow>();
            flow.name = "SceneFlow";
            return flow;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Stats = GetComponent<RunStats>();
            if (Stats == null) Stats = gameObject.AddComponent<RunStats>();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        void OnEnable()
        {
            // Yok edilmeyi bekleyen ikinci kopya kanala bağlanmasın.
            if (Instance == this && gameWon != null) gameWon.Register(HandleGameWon);
        }

        void OnDisable()
        {
            if (gameWon != null) gameWon.Unregister(HandleGameWon);
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (catalog != null && catalog.IsLevel(scene.name)) Stats.BeginIfIdle();
        }

        void HandleGameWon()
        {
            Stats.Stop();
            if (catalog != null) Load(catalog.endScene);
        }

        public void LoadTitle()
        {
            Stats.ResetRun();
            if (catalog != null) Load(catalog.titleScene);
        }

        public void StartGame()
        {
            Stats.Begin();
            if (catalog != null) Load(catalog.FirstLevel);
        }

        public void LoadNext()
        {
            if (catalog == null) return;
            string next = catalog.NextAfter(SceneManager.GetActiveScene().name);
            if (next == catalog.endScene) Stats.Stop();
            Load(next);
        }

        public void ReloadCurrent() => Load(SceneManager.GetActiveScene().name);

        void Load(string sceneName)
        {
            if (IsLoading || string.IsNullOrEmpty(sceneName)) return;
            StartCoroutine(Transition(sceneName));
        }

        IEnumerator Transition(string sceneName)
        {
            IsLoading = true;
            if (fader != null) yield return fader.FadeOut(fadeSeconds);
            // Duraklatma menüsünden çıkılırsa zaman ölçeği yeni sahneye taşınmasın.
            Time.timeScale = 1f;
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation != null)
                while (!operation.isDone) yield return null;
            if (fader != null) yield return fader.FadeIn(fadeSeconds);
            IsLoading = false;
        }
    }
}
