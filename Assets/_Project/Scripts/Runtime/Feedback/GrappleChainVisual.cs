using Game.Hook;
using Game.Player;
using UnityEngine;

namespace Game.Feedback
{
    /// <summary>Kanca eli ile uçuşan kanca başı arasındaki zinciri gösterir.</summary>
    public class GrappleChainVisual : MonoBehaviour
    {
        [SerializeField, Tooltip("Kancanın durumunu ve etkin noktasını sağlayan bileşen. Ö")]
        GrappleHook hook;
        [SerializeField, Tooltip("Zincirin oyuncu elindeki başlangıç noktası. Ö")]
        Transform startAnchor;
        [SerializeField, Tooltip("Zincirin uçan veya saplı kanca başındaki bitiş noktası. Ö")]
        Transform endAnchor;
        [SerializeField, Tooltip("Yatay döşenecek zincir bağlantı sprite'ı. Boşsa yer tutucu üretilir. Ö")]
        Sprite linkSprite;
        [SerializeField, Tooltip("Sprite'ın piksel başına birim ölçeği. Ö")]
        float pixelsPerUnit = 16f;
        [SerializeField, Tooltip("Zincirin bir bağlantı yüksekliğindeki kalınlığı, birim. Ö")]
        float width = 6f / 16f;
        [SerializeField, Tooltip("Kanca başının çizim sırasına eklenecek değer. Ö")]
        int sortingOrderOffset;
        [SerializeField, Tooltip("Zincir sprite'ının rengi. Ö")]
        Color tint = Color.white;
        [SerializeField, Tooltip("Kullanılacak zincir SpriteRenderer. Boşsa çocuk nesne oluşturulur. Ö")]
        SpriteRenderer chainRenderer;

        Texture2D generatedTexture;
        Sprite generatedSprite;

        void Awake()
        {
            EnsureRenderer();
        }

        void OnEnable()
        {
            if (hook != null) hook.StateChanged += OnStateChanged;
            ApplyVisibility();
        }

        void OnDisable()
        {
            if (hook != null) hook.StateChanged -= OnStateChanged;
            if (chainRenderer != null) chainRenderer.enabled = false;
        }

        void OnDestroy()
        {
            DestroyGenerated(generatedSprite);
            DestroyGenerated(generatedTexture);
        }

        // Edit modunda gecikmeli Destroy çalışmaz, testlerde sızıntı olmasın.
        static void DestroyGenerated(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        void LateUpdate()
        {
            if (chainRenderer == null) return;
            if (hook == null || endAnchor == null || !IsVisibleState(hook.State))
            {
                chainRenderer.enabled = false;
                return;
            }

            var resolvedStartAnchor = ResolveStartAnchor();
            var start = (Vector2)resolvedStartAnchor.position;
            var end = (Vector2)endAnchor.position;
            GrappleChainMath.Compute(start, end, out var center, out var angleDeg, out var length);
            if (length <= 0.0001f)
            {
                chainRenderer.enabled = false;
                return;
            }

            chainRenderer.enabled = true;
            var rotation = Quaternion.Euler(0f, 0f, angleDeg);
            chainRenderer.transform.rotation = rotation;
            chainRenderer.size = new Vector2(length, width);
            var pivotOffset = GrappleChainMath.ComputePivotOffset(
                chainRenderer.sprite.pivot,
                chainRenderer.sprite.rect.size,
                chainRenderer.size);
            chainRenderer.transform.position = center
                + (Vector2)chainRenderer.transform.right * pivotOffset.x
                + (Vector2)chainRenderer.transform.up * pivotOffset.y;
            chainRenderer.color = tint;
            ApplySortingOrder();
        }

        void OnStateChanged(HookState state)
        {
            ApplyVisibility();
        }

        /// <summary>Renderer'ı oluşturur ve testlerde açıkça başlatılabilmesini sağlar.</summary>
        public void EnsureRenderer()
        {
            if (chainRenderer == null)
            {
                var child = new GameObject("GrappleChain");
                child.transform.SetParent(transform, false);
                chainRenderer = child.AddComponent<SpriteRenderer>();
            }

            if (linkSprite == null)
            {
                CreatePlaceholderSprite();
                linkSprite = generatedSprite;
            }

            chainRenderer.sprite = linkSprite;
            chainRenderer.drawMode = SpriteDrawMode.Tiled;
            chainRenderer.tileMode = SpriteTileMode.Continuous;
            chainRenderer.color = tint;
            chainRenderer.enabled = false;
            ApplySortingOrder();
        }

        Transform ResolveStartAnchor()
        {
            if (startAnchor != null) return startAnchor;

            var armRig = hook.GetComponentInParent<PlayerArmRig>();
            if (armRig == null) armRig = hook.GetComponentInChildren<PlayerArmRig>();
            if (armRig != null && armRig.HookHandTip != null) return armRig.HookHandTip;
            return hook.transform;
        }

        void CreatePlaceholderSprite()
        {
            const int textureWidth = 8;
            const int textureHeight = 6;
            generatedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                name = "GrappleChainPlaceholder",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };

            var clear = new Color(0f, 0f, 0f, 0f);
            var dark = new Color(0.22f, 0.24f, 0.27f, 1f);
            var light = new Color(0.68f, 0.7f, 0.72f, 1f);
            var pixels = new Color[textureWidth * textureHeight];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = clear;

            // İki iç içe halka yatay döşemede bağlantı zinciri izlenimi verir.
            SetPixel(pixels, textureWidth, 1, 2, dark);
            SetPixel(pixels, textureWidth, 2, 1, dark);
            SetPixel(pixels, textureWidth, 3, 1, light);
            SetPixel(pixels, textureWidth, 4, 2, dark);
            SetPixel(pixels, textureWidth, 4, 3, dark);
            SetPixel(pixels, textureWidth, 3, 4, dark);
            SetPixel(pixels, textureWidth, 2, 4, light);
            SetPixel(pixels, textureWidth, 1, 3, dark);
            SetPixel(pixels, textureWidth, 5, 2, light);
            SetPixel(pixels, textureWidth, 6, 1, dark);
            SetPixel(pixels, textureWidth, 7, 1, dark);
            SetPixel(pixels, textureWidth, 7, 2, light);
            SetPixel(pixels, textureWidth, 7, 3, dark);
            SetPixel(pixels, textureWidth, 6, 4, dark);
            SetPixel(pixels, textureWidth, 5, 3, dark);

            generatedTexture.SetPixels(pixels);
            generatedTexture.Apply(false, false);
            generatedSprite = Sprite.Create(
                generatedTexture,
                new Rect(0f, 0f, textureWidth, textureHeight),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            generatedSprite.name = "GrappleChainPlaceholder";
        }

        static void SetPixel(Color[] pixels, int textureWidth, int x, int y, Color color)
        {
            pixels[y * textureWidth + x] = color;
        }

        void ApplySortingOrder()
        {
            if (chainRenderer == null) return;
            if (endAnchor != null && endAnchor.GetComponentInChildren<SpriteRenderer>() is SpriteRenderer headRenderer)
            {
                chainRenderer.sortingLayerID = headRenderer.sortingLayerID;
                chainRenderer.sortingOrder = headRenderer.sortingOrder + sortingOrderOffset;
            }
            else
            {
                chainRenderer.sortingOrder = sortingOrderOffset;
            }
        }

        void ApplyVisibility()
        {
            if (chainRenderer != null) chainRenderer.enabled = hook != null && endAnchor != null && IsVisibleState(hook.State);
        }

        static bool IsVisibleState(HookState state)
        {
            return state == HookState.Thrown
                || state == HookState.Stuck
                || state == HookState.Pulling
                || state == HookState.Reeling
                || state == HookState.Returning;
        }
    }
}
