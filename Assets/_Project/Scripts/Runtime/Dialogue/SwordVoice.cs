using Game.Core;
using Game.Combat;
using Game.Hook;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Dialogue
{
    /// <summary>Kılıç olaylarını repliklere ve dünya uzayı balonuna bağlar.</summary>
    public class SwordVoice : MonoBehaviour
    {
        [SerializeField] SwordLineSet lines;
        [SerializeField] GrappleHook hook;
        [SerializeField] SwordVoiceEvents swordEvents;
        [SerializeField, FormerlySerializedAs("sword")] Transform hookVisual;
        [SerializeField] GameEvent deathEvent;
        [SerializeField] GameEvent respawnEvent;
        [Tooltip("Hareketsizlik ve balon takibi için oyuncu. Boşsa bileşenin sahibi kullanılır.")]
        [SerializeField] Transform player;
        [Tooltip("Balonun kılıca göre konumu.")]
        [SerializeField] Vector3 bubbleOffset = new Vector3(0f, 0.8f, 0f);

        /// <summary>Şu an gösterilen replik; yoksa null.</summary>
        public string CurrentLine { get; private set; }

        LineSelector selector;
        HookState previousState;
        bool hasThrown;
        bool hasLaunched;
        bool idleSpoken;
        float lastLaunchTime;
        float idleElapsed;
        Vector3 lastPlayerPosition;
        GameObject bubble;
        SpriteRenderer background;
        TextMeshPro label;
        Sprite bubbleSprite;
        Texture2D bubbleTexture;
        float typeElapsed;
        float displayRemaining;

        void Awake()
        {
            selector = new LineSelector();
            if (player == null) player = transform;
            lastPlayerPosition = player.position;
            CreateBubble();
        }

        void OnEnable()
        {
            if (hook != null)
            {
                previousState = hook.State;
                hook.StateChanged += OnHookStateChanged;
            }
            if (swordEvents != null) { swordEvents.HitLanded += OnSwordHit; swordEvents.SwingMissed += OnSwordMiss; }
            if (deathEvent != null) deathEvent.Register(OnDeath);
            if (respawnEvent != null) respawnEvent.Register(OnRespawn);
        }

        void OnDisable()
        {
            if (hook != null) hook.StateChanged -= OnHookStateChanged;
            if (swordEvents != null) { swordEvents.HitLanded -= OnSwordHit; swordEvents.SwingMissed -= OnSwordMiss; }
            if (deathEvent != null) deathEvent.Unregister(OnDeath);
            if (respawnEvent != null) respawnEvent.Unregister(OnRespawn);
        }

        void Update()
        {
            UpdateIdle();
            UpdateBubble();
        }

        void OnHookStateChanged(HookState next)
        {
            if (previousState == HookState.InHand && next == HookState.Thrown && !hasThrown)
            {
                hasThrown = true;
                Speak(SwordCue.FirstThrow);
            }
            else if (previousState == HookState.Thrown && next == HookState.Stuck)
            {
                var chained = hasLaunched && lines != null && Time.time - lastLaunchTime <= lines.chainWindow;
                Speak(chained ? SwordCue.ChainLaunch : SwordCue.Stick);
            }
            else if (previousState == HookState.Thrown && next == HookState.Returning)
            {
                Speak(SwordCue.Miss);
            }
            else if (previousState == HookState.Pulling && next == HookState.Launch)
            {
                Speak(SwordCue.Launch);
                hasLaunched = true;
                lastLaunchTime = Time.time;
            }
            previousState = next;
        }

        void OnDeath() => Speak(SwordCue.Death);
        void OnRespawn() => Speak(SwordCue.Respawn);
        void OnSwordHit() => Speak(SwordCue.HitLanded);
        void OnSwordMiss() => Speak(SwordCue.SwingMissed);

        void Speak(SwordCue cue)
        {
            if (selector == null || lines == null) return;
            var selected = selector.Select(lines, cue, Time.time);
            if (selected == null) return;
            CurrentLine = selected;
            typeElapsed = 0f;
            displayRemaining = lines.displayBaseSeconds + selected.Length * lines.displaySecondsPerCharacter;
            if (label == null || label.font == null)
            {
                if (bubble != null) bubble.SetActive(false);
                return;
            }
            if (bubble != null) bubble.SetActive(true);
            label.text = string.Empty;
            FitBubble(selected);
        }

        void UpdateBubble()
        {
            if (CurrentLine == null) return;
            if (bubble != null && bubble.activeSelf)
            {
                var follow = hookVisual != null ? hookVisual : transform;
                bubble.transform.position = follow.position + bubbleOffset;
                bubble.transform.rotation = Quaternion.identity;
                if (label != null)
                {
                    typeElapsed += Time.deltaTime * (lines != null ? lines.charactersPerSecond : 1f);
                    var count = Mathf.Min(CurrentLine.Length, Mathf.FloorToInt(typeElapsed));
                    if (label.text.Length != count) label.text = CurrentLine.Substring(0, count);
                }
            }
            displayRemaining -= Time.deltaTime;
            if (displayRemaining <= 0f)
            {
                if (bubble != null) bubble.SetActive(false);
                CurrentLine = null;
            }
        }

        void UpdateIdle()
        {
            var position = player.position;
            var threshold = lines != null ? lines.movementThreshold : 0f;
            if ((position - lastPlayerPosition).sqrMagnitude > threshold * threshold)
            {
                idleElapsed = 0f;
                idleSpoken = false;
                lastPlayerPosition = position;
                return;
            }
            idleElapsed += Time.deltaTime;
            if (!idleSpoken && lines != null && idleElapsed >= lines.idleSeconds)
            {
                idleSpoken = true;
                Speak(SwordCue.Idle);
            }
        }

        void CreateBubble()
        {
            // TMP_Settings.defaultFontAsset, TMP Essential Resources kurulu değilse NullReference atar.
            // Kurulum Tools/Kanca/Projeyi Kur'dadır; burada yalnız çökmemek için önce ayar dosyasına bakılır.
            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            var defaultFont = settings != null ? TMP_Settings.defaultFontAsset : null;
            if (defaultFont == null)
            {
                Debug.LogWarning("[SwordVoice] TMP varsayılan fontu yok; replik yalnız CurrentLine'da tutulacak.", this);
                return;
            }

            bubble = new GameObject("SwordVoiceBubble");
            bubble.transform.SetParent(null);
            bubble.SetActive(false);

            bubbleTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "SwordVoiceBubbleTexture" };
            bubbleTexture.SetPixel(0, 0, new Color(0.08f, 0.07f, 0.1f, 0.9f));
            bubbleTexture.Apply();
            bubbleSprite = Sprite.Create(bubbleTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            bubbleSprite.name = "SwordVoiceBubbleSprite";

            var backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(bubble.transform, false);
            background = backgroundObject.AddComponent<SpriteRenderer>();
            background.sprite = bubbleSprite;
            background.sortingLayerName = "UI_World";
            background.sortingOrder = 0;

            var textObject = new GameObject("Text");
            textObject.transform.SetParent(bubble.transform, false);
            label = textObject.AddComponent<TextMeshPro>();
            label.font = defaultFont;
            label.fontSize = 0.32f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.sortingLayerID = SortingLayer.NameToID("UI_World");
            label.sortingOrder = 1;
        }

        void FitBubble(string text)
        {
            if (background == null || label == null || label.font == null) return;
            var size = label.GetPreferredValues(text, 5f, 2f);
            var width = Mathf.Clamp(size.x + 0.3f, 0.8f, 5f);
            var height = Mathf.Clamp(size.y + 0.2f, 0.55f, 2f);
            background.transform.localScale = new Vector3(width, height, 1f);
            label.rectTransform.sizeDelta = new Vector2(width - 0.2f, height - 0.1f);
        }

        void OnDestroy()
        {
            if (bubble != null) Destroy(bubble);
            if (bubbleSprite != null) Destroy(bubbleSprite);
            if (bubbleTexture != null) Destroy(bubbleTexture);
        }
    }
}
