using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Reusable fade-to-black/from-black transition system.
/// Persists across scene loads for smooth transitions.
/// </summary>
public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float defaultFadeDuration = 0.5f;
    [SerializeField] private Color fadeColor = Color.black;

    private Coroutine fadeCoroutine;
    private Canvas fadeCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Auto-setup canvas if not configured
        SetupCanvas();
    }

    private void SetupCanvas()
    {
        fadeCanvas = GetComponent<Canvas>();
        if (fadeCanvas == null)
        {
            fadeCanvas = gameObject.AddComponent<Canvas>();
        }
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999; // Always on top

        // Ensure we have a CanvasScaler
        if (GetComponent<CanvasScaler>() == null)
        {
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // Ensure we have a GraphicRaycaster (required for canvas)
        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // Auto-find or create fadeOverlay
        if (fadeOverlay == null)
        {
            fadeOverlay = GetComponentInChildren<Image>();
            if (fadeOverlay == null)
            {
                // Create fade overlay image
                GameObject overlayObj = new GameObject("FadeOverlay");
                overlayObj.transform.SetParent(transform, false);
                fadeOverlay = overlayObj.AddComponent<Image>();

                // Make it cover the entire screen
                RectTransform rect = fadeOverlay.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        fadeOverlay.color = Color.clear;
        fadeOverlay.raycastTarget = false; // Don't block input
    }

    /// <summary>
    /// Instantly set to black (no animation)
    /// </summary>
    public void SetBlack()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.color = fadeColor;
        }
    }

    /// <summary>
    /// Instantly set to clear (no animation)
    /// </summary>
    public void SetClear()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.color = Color.clear;
        }
    }

    /// <summary>
    /// Fade from black to clear
    /// </summary>
    public void FadeIn(float duration = -1f)
    {
        StartFade(true, duration < 0 ? defaultFadeDuration : duration);
    }

    /// <summary>
    /// Fade from clear to black
    /// </summary>
    public void FadeOut(float duration = -1f)
    {
        StartFade(false, duration < 0 ? defaultFadeDuration : duration);
    }

    /// <summary>
    /// Fade from black to clear (coroutine version for yielding)
    /// </summary>
    public IEnumerator FadeInCoroutine(float duration = -1f)
    {
        yield return FadeCoroutine(true, duration < 0 ? defaultFadeDuration : duration);
    }

    /// <summary>
    /// Fade from clear to black (coroutine version for yielding)
    /// </summary>
    public IEnumerator FadeOutCoroutine(float duration = -1f)
    {
        yield return FadeCoroutine(false, duration < 0 ? defaultFadeDuration : duration);
    }

    /// <summary>
    /// Fade out, load scene, fade in
    /// </summary>
    public void FadeToScene(string sceneName, float fadeDuration = -1f)
    {
        float duration = fadeDuration < 0 ? defaultFadeDuration : fadeDuration;
        StartCoroutine(FadeToSceneCoroutine(sceneName, duration));
    }

    /// <summary>
    /// Fade out, load scene, fade in (using SceneReference)
    /// </summary>
    public void FadeToScene(SceneReference sceneRef, float fadeDuration = -1f)
    {
        if (sceneRef == null || !sceneRef.IsValid)
        {
            Debug.LogWarning("SceneFader: Invalid scene reference!");
            return;
        }
        FadeToScene(sceneRef.SceneName, fadeDuration);
    }

    /// <summary>
    /// Fade to scene coroutine (for yielding)
    /// </summary>
    public IEnumerator FadeToSceneCoroutine(string sceneName, float fadeDuration = -1f)
    {
        float duration = fadeDuration < 0 ? defaultFadeDuration : fadeDuration;

        // Fade out
        yield return FadeCoroutine(false, duration);

        // Load scene
        SceneManager.LoadScene(sceneName);

        // Wait a frame for scene to load
        yield return null;

        // Fade in
        yield return FadeCoroutine(true, duration);
    }

    private void StartFade(bool fadeIn, float duration)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeCoroutine(fadeIn, duration));
    }

    private IEnumerator FadeCoroutine(bool fadeIn, float duration)
    {
        if (fadeOverlay == null) yield break;

        float elapsed = 0f;
        Color startColor = fadeIn ? fadeColor : Color.clear;
        Color endColor = fadeIn ? Color.clear : fadeColor;

        fadeOverlay.color = startColor;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            // Ease-in-out for smooth transitions
            float easedProgress = EaseInOutQuad(progress);
            fadeOverlay.color = Color.Lerp(startColor, endColor, easedProgress);
            yield return null;
        }

        fadeOverlay.color = endColor;
        fadeCoroutine = null;
    }

    private float EaseInOutQuad(float t)
    {
        return t < 0.5f
            ? 2f * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
