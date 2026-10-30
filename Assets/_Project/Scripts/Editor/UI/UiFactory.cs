using System.IO;
using System.Linq;
using Game.Core;
using Game.Flow;
using Game.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.Editor.UI
{
    /// <summary>
    /// Oyun içi arayüzü, sahne akışını ve menü sahnelerini kurar. Var olan asset'lerin değerlerini ezmez;
    /// prefab'lar ve Title/End sahneleri her çalıştırmada bu koddan yeniden üretilir (elle düzenlenmez).
    /// </summary>
    public static class UiFactory
    {
        const string Root = "Assets/_Project";
        const string EventDir = Root + "/Data/Events";
        const string FlowDir = Root + "/Data/Flow";
        const string UiPrefabDir = Root + "/Prefabs/UI";
        const string ResourcesFlowDir = Root + "/Resources/Flow";
        const string SceneDir = Root + "/Scenes";
        static readonly Vector2 Reference = new Vector2(1920f, 1080f);

        [MenuItem("Tools/Kanca/Arayuzu Kur")]
        public static void BuildAll()
        {
            foreach (var folder in new[] { EventDir, FlowDir, UiPrefabDir, ResourcesFlowDir, SceneDir }) EnsureFolder(folder);
            var catalog = GetOrCreate<SceneCatalog>(FlowDir + "/SceneCatalog.asset");
            var playerHealth = GetOrCreate<HealthEvent>(EventDir + "/Event_PlayerHealth.asset");
            var bossStarted = GetOrCreate<StringEvent>(EventDir + "/Event_BossFightStarted.asset");
            var bossHealth = GetOrCreate<HealthEvent>(EventDir + "/Event_BossHealth.asset");
            var bossDefeated = GetOrCreate<GameEvent>(EventDir + "/Event_BossDefeated.asset");
            var gameWon = GetOrCreate<GameEvent>(EventDir + "/Event_GameWon.asset");
            var playerDeath = GetOrCreate<GameEvent>(EventDir + "/Event_PlayerDeath.asset");
            var playerRespawn = GetOrCreate<GameEvent>(EventDir + "/Event_PlayerRespawn.asset");

            BuildSceneFlowPrefab(catalog, gameWon, playerDeath);
            BuildEventSystemPrefab();
            BuildGameHud(playerHealth, bossStarted, bossHealth, bossDefeated, playerRespawn);
            BuildTitleScene();
            BuildEndScene();
            AddMenuScenesToBuild(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UiFactory] Arayüz, sahne akışı, Title ve End sahneleri kuruldu.");
        }

        // ---------- Prefab'lar ----------

        static void BuildSceneFlowPrefab(SceneCatalog catalog, GameEvent gameWon, GameEvent playerDeath)
        {
            var root = new GameObject("SceneFlow");
            var flow = root.AddComponent<SceneFlow>();
            var stats = root.AddComponent<RunStats>();
            var canvas = CreateCanvas("FaderCanvas", root.transform, 1000);
            var black = CreateRect("Black", canvas.transform);
            Stretch(black);
            black.gameObject.AddComponent<Image>().color = Color.black;
            var group = black.gameObject.AddComponent<CanvasGroup>();
            var fader = canvas.gameObject.AddComponent<ScreenFader>();
            Set(fader, ("group", group));
            Set(flow, ("catalog", catalog), ("fader", fader), ("gameWon", gameWon));
            Set(stats, ("playerDeath", playerDeath));
            SavePrefab(root, ResourcesFlowDir + "/SceneFlow.prefab");
        }

        static void BuildEventSystemPrefab()
        {
            var root = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SavePrefab(root, UiPrefabDir + "/EventSystem.prefab");
        }

        static void BuildGameHud(HealthEvent playerHealth, StringEvent bossStarted, HealthEvent bossHealth, GameEvent bossDefeated, GameEvent playerRespawn)
        {
            var canvas = CreateCanvas("GameHud", null, 10);
            canvas.gameObject.AddComponent<FlowBootstrap>();

            // Sol üst: can maskeleri (45 derece dönmüş kareler).
            var healthRoot = CreateRect("PlayerHealth", canvas.transform);
            Anchor(healthRoot, new Vector2(0f, 1f), new Vector2(56f, -48f), new Vector2(400f, 48f));
            var layout = healthRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var template = CreateRect("MaskTemplate", healthRoot);
            template.sizeDelta = new Vector2(30f, 30f);
            template.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var templateImage = template.gameObject.AddComponent<Image>();
            templateImage.color = UiPalette.Text;
            templateImage.raycastTarget = false;
            var hud = healthRoot.gameObject.AddComponent<HealthHud>();
            Set(hud, ("health", playerHealth), ("container", healthRoot), ("iconTemplate", templateImage));

            // Alt orta: boss çubuğu.
            var bossRoot = CreateRect("BossHealth", canvas.transform);
            Anchor(bossRoot, new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(960f, 64f));
            var bossGroup = bossRoot.gameObject.AddComponent<CanvasGroup>();
            bossGroup.blocksRaycasts = false;
            var nameLabel = CreateText("BossName", bossRoot, "Boss", 30f, UiPalette.Text, TextAlignmentOptions.Left);
            Anchor(nameLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(960f, 40f));
            nameLabel.characterSpacing = 6f;
            var frame = CreateRect("Frame", bossRoot);
            Anchor(frame, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(960f, 20f));
            frame.gameObject.AddComponent<Image>().color = UiPalette.Panel;
            var trail = CreateBar("DamageTrail", frame, UiPalette.Damage);
            var fill = CreateBar("Health", frame, UiPalette.Accent);
            var bar = bossRoot.gameObject.AddComponent<BossHealthBar>();
            Set(bar, ("fightStarted", bossStarted), ("health", bossHealth), ("defeated", bossDefeated), ("playerRespawned", playerRespawn),
                ("group", bossGroup), ("bossName", nameLabel), ("fill", fill), ("trail", trail));

            BuildPauseMenu(canvas.transform);
            SavePrefab(canvas.gameObject, UiPrefabDir + "/GameHud.prefab");
        }

        static void BuildPauseMenu(Transform canvas)
        {
            var root = CreateRect("PauseMenu", canvas);
            Stretch(root);
            var panel = CreateRect("Panel", root);
            Stretch(panel);
            panel.gameObject.AddComponent<Image>().color = UiPalette.Panel;
            var heading = CreateText("Heading", panel, "DURAKLATILDI", 64f, UiPalette.Text, TextAlignmentOptions.Center);
            Anchor(heading.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(900f, 90f));
            heading.characterSpacing = 18f;
            var resume = CreateButton("Resume", panel, "Devam", new Vector2(0f, 90f));
            var checkpoint = CreateButton("Checkpoint", panel, "Son checkpoint'e dön", new Vector2(0f, 0f));
            var title = CreateButton("Title", panel, "Başlık ekranı", new Vector2(0f, -90f));
            var quit = CreateButton("Quit", panel, "Oyundan çık", new Vector2(0f, -180f));
            var menu = root.gameObject.AddComponent<PauseMenu>();
            Set(menu, ("panel", panel.gameObject), ("headingLabel", heading),
                ("resumeButton", resume.button), ("resumeLabel", resume.label),
                ("checkpointButton", checkpoint.button), ("checkpointLabel", checkpoint.label),
                ("titleButton", title.button), ("titleLabel", title.label),
                ("quitButton", quit.button), ("quitLabel", quit.label));
            panel.gameObject.SetActive(false);
        }

        // ---------- Sahneler ----------

        static void BuildTitleScene()
        {
            var scene = NewMenuScene(out var canvas);
            var title = CreateText("Title", canvas, "KANCA", 200f, UiPalette.Text, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(1400f, 240f));
            title.characterSpacing = 40f;
            title.fontStyle = FontStyles.Bold;
            var rule = CreateRect("Rule", canvas);
            Anchor(rule, new Vector2(0.5f, 0.5f), new Vector2(0f, 88f), new Vector2(120f, 4f));
            rule.gameObject.AddComponent<Image>().color = UiPalette.Accent;
            var subtitle = CreateText("Subtitle", canvas, "", 30f, UiPalette.TextDim, TextAlignmentOptions.Center);
            Anchor(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1400f, 50f));
            var start = CreateButton("Start", canvas, "Başla", new Vector2(0f, -90f));
            var quit = CreateButton("Quit", canvas, "Çık", new Vector2(0f, -180f));
            var controls = CreateText("Controls", canvas, "", 24f, UiPalette.TextDim, TextAlignmentOptions.Center);
            Anchor(controls.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1700f, 40f));
            var menu = canvas.gameObject.AddComponent<TitleMenu>();
            Set(menu, ("titleLabel", title), ("subtitleLabel", subtitle), ("controlsLabel", controls),
                ("startButton", start.button), ("startLabel", start.label),
                ("quitButton", quit.button), ("quitLabel", quit.label));
            EditorSceneManager.SaveScene(scene, SceneDir + "/Title.unity");
        }

        static void BuildEndScene()
        {
            var scene = NewMenuScene(out var canvas);
            var heading = CreateText("Heading", canvas, "", 84f, UiPalette.Text, TextAlignmentOptions.Center);
            Anchor(heading.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1600f, 110f));
            heading.characterSpacing = 8f;
            var rule = CreateRect("Rule", canvas);
            Anchor(rule, new Vector2(0.5f, 0.5f), new Vector2(0f, 72f), new Vector2(120f, 4f));
            rule.gameObject.AddComponent<Image>().color = UiPalette.Accent;
            var summary = CreateText("Summary", canvas, "", 36f, UiPalette.TextDim, TextAlignmentOptions.Center);
            Anchor(summary.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1200f, 60f));
            var back = CreateButton("Title", canvas, "Başlık ekranına dön", new Vector2(0f, -120f));
            var end = canvas.gameObject.AddComponent<EndScreen>();
            Set(end, ("headingLabel", heading), ("summaryLabel", summary), ("titleButton", back.button), ("buttonLabel", back.label));
            EditorSceneManager.SaveScene(scene, SceneDir + "/End.unity");
        }

        static UnityEngine.SceneManagement.Scene NewMenuScene(out RectTransform canvas)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UiPalette.Ink;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabDir + "/EventSystem.prefab"));
            canvas = CreateCanvas("Canvas", null, 0);
            return scene;
        }

        static void AddMenuScenesToBuild(SceneCatalog catalog)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            void Ensure(string path, int index)
            {
                scenes.RemoveAll(s => s.path == path);
                scenes.Insert(Mathf.Clamp(index, 0, scenes.Count), new EditorBuildSettingsScene(path, true));
            }
            // Başlık ilk sahnedir (oyun buradan açılır); bitiş sona eklenir.
            Ensure(SceneDir + "/Title.unity", 0);
            Ensure(SceneDir + "/End.unity", scenes.Count);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------- Yapı taşları ----------

        static RectTransform CreateCanvas(string name, Transform parent, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.matchWidthOrHeight = 0.5f;
            return (RectTransform)go.transform;
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Tek noktaya çapalar; pivot çapayla aynıdır, böylece konum çapadan uzaklıktır.</summary>
        static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static RectTransform CreateBar(string name, RectTransform frame, Color color)
        {
            var bar = CreateRect(name, frame);
            bar.anchorMin = Vector2.zero;
            bar.anchorMax = Vector2.one;
            bar.pivot = new Vector2(0f, 0.5f);
            bar.offsetMin = new Vector2(3f, 3f);
            bar.offsetMax = new Vector2(-3f, -3f);
            var image = bar.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return bar;
        }

        static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions alignment)
        {
            var rect = CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        static (Button button, TMP_Text label) CreateButton(string name, Transform parent, string text, Vector2 position)
        {
            var rect = CreateRect(name, parent);
            Anchor(rect, new Vector2(0.5f, 0.5f), position, new Vector2(520f, 72f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = UiPalette.Button;
            colors.highlightedColor = UiPalette.ButtonHover;
            colors.selectedColor = UiPalette.ButtonHover;
            colors.pressedColor = UiPalette.Accent;
            colors.disabledColor = UiPalette.Empty;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            var label = CreateText("Label", rect, text, 32f, UiPalette.Text, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            // Düğmenin solunda ince amber çizgi: menülerin ortak işareti.
            var marker = CreateRect("Marker", rect);
            marker.anchorMin = new Vector2(0f, 0f);
            marker.anchorMax = new Vector2(0f, 1f);
            marker.pivot = new Vector2(0f, 0.5f);
            marker.sizeDelta = new Vector2(6f, 0f);
            marker.anchoredPosition = Vector2.zero;
            var markerImage = marker.gameObject.AddComponent<Image>();
            markerImage.color = UiPalette.Accent;
            markerImage.raycastTarget = false;
            return (button, label);
        }

        // ---------- Asset yardımcıları ----------

        static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void Set(Object target, params (string name, Object value)[] values)
        {
            var serialized = new SerializedObject(target);
            foreach (var (name, value) in values)
            {
                var property = serialized.FindProperty(name);
                if (property == null)
                {
                    Debug.LogWarning($"[UiFactory] {target.GetType().Name}.{name} alanı bulunamadı.");
                    continue;
                }
                property.objectReferenceValue = value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
