using UnityEngine;

/// <summary>
/// Simple animated sprite effect that plays through frames and destroys itself.
/// Used for melee swing visual effects.
/// </summary>
public class AnimatedSwingEffect : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameDuration = 0.05f;
    [SerializeField] private bool loop = false;
    [SerializeField] private bool destroyOnComplete = true;

    [Header("Appearance")]
    [SerializeField] private Color tintColor = Color.white;
    [SerializeField] private bool normalizeSize = true;
    [SerializeField] private float targetSize = 10.24f; // Default: 1024px at 100 PPU

    private SpriteRenderer spriteRenderer;
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.color = tintColor;
    }

    private void Start()
    {
        // Capture base scale set by spawner (e.g., MeleeSwingWeapon)
        baseScale = transform.localScale;

        if (frames != null && frames.Length > 0)
        {
            SetFrame(0);
        }
    }

    private void SetFrame(int frameIndex)
    {
        if (frames == null || frameIndex >= frames.Length) return;

        Sprite sprite = frames[frameIndex];
        spriteRenderer.sprite = sprite;

        if (normalizeSize && sprite != null)
        {
            // Calculate scale to normalize all sprites to target size
            float spriteWorldWidth = sprite.rect.width / sprite.pixelsPerUnit;
            float spriteWorldHeight = sprite.rect.height / sprite.pixelsPerUnit;
            float maxDimension = Mathf.Max(spriteWorldWidth, spriteWorldHeight);

            if (maxDimension > 0)
            {
                float normalizeScale = targetSize / maxDimension;
                // Multiply by base scale (set by spawner like MeleeSwingWeapon)
                transform.localScale = baseScale * normalizeScale;
            }
        }
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        frameTimer += Time.deltaTime;

        if (frameTimer >= frameDuration)
        {
            frameTimer = 0f;
            currentFrame++;

            if (currentFrame >= frames.Length)
            {
                if (loop)
                {
                    currentFrame = 0;
                }
                else if (destroyOnComplete)
                {
                    Destroy(gameObject);
                    return;
                }
                else
                {
                    currentFrame = frames.Length - 1;
                }
            }

            SetFrame(currentFrame);
        }
    }

    /// <summary>
    /// Set the animation frames at runtime
    /// </summary>
    public void SetFrames(Sprite[] newFrames)
    {
        frames = newFrames;
        currentFrame = 0;
        frameTimer = 0f;
        if (spriteRenderer != null && frames != null && frames.Length > 0)
        {
            SetFrame(0);
        }
    }

    /// <summary>
    /// Set the tint color
    /// </summary>
    public void SetColor(Color color)
    {
        tintColor = color;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = tintColor;
        }
    }
}
