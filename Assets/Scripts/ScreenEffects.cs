using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Full-screen flash effects for damage and healing feedback.
/// Attach to a Canvas with Screen Space - Overlay render mode.
/// </summary>
public class ScreenEffects : MonoBehaviour
{
    public static ScreenEffects Instance { get; private set; }

    [Header("Flash Settings")]
    [SerializeField] private Image flashOverlay;
    [SerializeField] private Color damageColor = new Color(1f, 0.1f, 0.1f, 0.45f);
    [SerializeField] private Color healColor = new Color(0.2f, 1f, 0.2f, 0.35f);
    [SerializeField] private float damageFlashDuration = 0.2f;
    [SerializeField] private float healFlashDuration = 0.25f;

    private Coroutine flashCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this); // Only destroy component, not gameObject
            return;
        }
        Instance = this;

        // Auto-find flashOverlay if not assigned
        if (flashOverlay == null)
        {
            flashOverlay = GetComponentInChildren<Image>();
        }

        if (flashOverlay != null)
        {
            flashOverlay.color = Color.clear;
        }
        else
        {
            Debug.LogWarning("ScreenEffects: No Image found for flashOverlay!");
        }
    }

    /// <summary>
    /// Flash red for damage feedback
    /// </summary>
    public void DamageFlash()
    {
        Flash(damageColor, damageFlashDuration);
    }

    /// <summary>
    /// Flash green for heal feedback
    /// </summary>
    public void HealFlash()
    {
        Flash(healColor, healFlashDuration);
    }

    /// <summary>
    /// Flash with custom color and duration
    /// </summary>
    public void Flash(Color color, float duration)
    {
        if (flashOverlay == null)
        {
            // Try to recover
            flashOverlay = GetComponentInChildren<Image>();
            if (flashOverlay == null)
            {
                Debug.LogWarning("ScreenEffects.Flash: flashOverlay is null!");
                return;
            }
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashCoroutine(color, duration));
    }

    private IEnumerator FlashCoroutine(Color color, float duration)
    {
        if (flashOverlay == null) yield break;

        float elapsed = 0f;
        flashOverlay.color = color;

        while (elapsed < duration && flashOverlay != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            // Ease-out curve for smooth fade
            float easedProgress = 1f - Mathf.Pow(1f - progress, 2f);
            float alpha = Mathf.Lerp(color.a, 0f, easedProgress);
            flashOverlay.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        if (flashOverlay != null)
        {
            flashOverlay.color = Color.clear;
        }
        flashCoroutine = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Get the flash overlay Image (for debug purposes)
    /// </summary>
    public Image GetFlashOverlay() => flashOverlay;
}
