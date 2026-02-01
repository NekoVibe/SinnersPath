using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Cinematic intro sequence with smooth fades and subtle glow effects.
/// No position translations - calm and professional.
/// </summary>
public class IntroSequence : MonoBehaviour
{
    [Header("Scene Navigation")]
    [SerializeField] private SceneReference nextScene;

    [Header("Logo Elements")]
    [SerializeField] private CanvasGroup logoGroup;
    [SerializeField] private Image pawIcon;
    [SerializeField] private TextMeshProUGUI studioNameText;
    [SerializeField] private Image glowEffect; // Optional glow behind logo

    [Header("Timing")]
    [SerializeField] private float initialDelay = 0.5f;
    [SerializeField] private float fadeFromBlackDuration = 1.2f;
    [SerializeField] private float pawFadeInDuration = 0.8f;
    [SerializeField] private float delayBeforeText = 0.3f;
    [SerializeField] private float textFadeInDuration = 1.0f;
    [SerializeField] private float logoHoldDuration = 2.0f;
    [SerializeField] private float fadeToBlackDuration = 0.8f;

    [Header("Glow Settings")]
    [SerializeField] private bool enableGlowPulse = true;
    [SerializeField] private float glowPulseSpeed = 1.5f;
    [SerializeField] private float glowMinAlpha = 0.3f;
    [SerializeField] private float glowMaxAlpha = 0.6f;

    [Header("Skip Settings")]
    [SerializeField] private CanvasGroup skipPromptGroup;
    [SerializeField] private float skipPromptDelay = 2.0f;
    [SerializeField] private float skipFadeDuration = 0.4f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip logoRevealSFX;

    private bool canSkip = false;
    private bool skipRequested = false;
    private bool sequenceComplete = false;
    private Coroutine glowCoroutine;

    private void Start()
    {
        InitializeElements();
        StartCoroutine(RunIntroSequence());
        StartCoroutine(EnableSkipAfterDelay());
    }

    private void InitializeElements()
    {
        // Hide everything initially
        if (logoGroup != null)
            logoGroup.alpha = 0f;

        if (pawIcon != null)
            SetImageAlpha(pawIcon, 0f);

        if (studioNameText != null)
            studioNameText.alpha = 0f;

        if (glowEffect != null)
            SetImageAlpha(glowEffect, 0f);

        if (skipPromptGroup != null)
            skipPromptGroup.alpha = 0f;

        // Ensure SceneFader starts black
        if (SceneFader.Instance != null)
            SceneFader.Instance.SetBlack();
    }

    private void Update()
    {
        if (canSkip && !skipRequested && !sequenceComplete && Input.anyKeyDown)
        {
            skipRequested = true;
            StopAllCoroutines();
            StartCoroutine(SkipToNextScene());
        }
    }

    private IEnumerator RunIntroSequence()
    {
        // Initial delay (breathing room)
        yield return new WaitForSecondsRealtime(initialDelay);

        // Show logo group container
        if (logoGroup != null)
            logoGroup.alpha = 1f;

        // Fade in from black
        if (SceneFader.Instance != null)
            yield return SceneFader.Instance.FadeInCoroutine(fadeFromBlackDuration);

        // Fade in paw icon
        if (pawIcon != null)
            yield return StartCoroutine(FadeImage(pawIcon, 0f, 1f, pawFadeInDuration));

        // Brief pause
        yield return new WaitForSecondsRealtime(delayBeforeText);

        // Fade in studio name
        if (studioNameText != null)
            yield return StartCoroutine(FadeText(studioNameText, 0f, 1f, textFadeInDuration));

        // Play reveal sound
        PlaySFX(logoRevealSFX);

        // Start glow pulse effect
        if (enableGlowPulse && glowEffect != null)
            glowCoroutine = StartCoroutine(GlowPulse());

        // Hold on logo
        yield return new WaitForSecondsRealtime(logoHoldDuration);

        // Stop glow
        if (glowCoroutine != null)
            StopCoroutine(glowCoroutine);

        // Fade glow out smoothly
        if (glowEffect != null)
            StartCoroutine(FadeImage(glowEffect, glowEffect.color.a, 0f, fadeToBlackDuration * 0.5f));

        // Transition to next scene
        sequenceComplete = true;
        yield return StartCoroutine(TransitionToNextScene());
    }

    private IEnumerator FadeImage(Image image, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseInOutSine(Mathf.Clamp01(elapsed / duration));
            SetImageAlpha(image, Mathf.Lerp(from, to, t));
            yield return null;
        }
        SetImageAlpha(image, to);
    }

    private IEnumerator FadeText(TextMeshProUGUI text, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseInOutSine(Mathf.Clamp01(elapsed / duration));
            text.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        text.alpha = to;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseInOutSine(Mathf.Clamp01(elapsed / duration));
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        group.alpha = to;
    }

    private IEnumerator GlowPulse()
    {
        // Fade glow in first
        yield return FadeImage(glowEffect, 0f, glowMinAlpha, 0.5f);

        // Continuous pulse
        float time = 0f;
        while (true)
        {
            time += Time.unscaledDeltaTime * glowPulseSpeed;
            float alpha = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, (Mathf.Sin(time) + 1f) * 0.5f);
            SetImageAlpha(glowEffect, alpha);
            yield return null;
        }
    }

    private IEnumerator EnableSkipAfterDelay()
    {
        yield return new WaitForSecondsRealtime(skipPromptDelay);

        if (sequenceComplete) yield break;

        canSkip = true;

        // Fade in skip prompt
        if (skipPromptGroup != null)
            yield return FadeCanvasGroup(skipPromptGroup, 0f, 0.6f, 0.5f);
    }

    private IEnumerator SkipToNextScene()
    {
        // Quick fade to black
        if (SceneFader.Instance != null)
            yield return SceneFader.Instance.FadeOutCoroutine(skipFadeDuration);

        LoadNextScene();
    }

    private IEnumerator TransitionToNextScene()
    {
        // Fade to black
        if (SceneFader.Instance != null)
            yield return SceneFader.Instance.FadeOutCoroutine(fadeToBlackDuration);

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (nextScene != null && nextScene.IsValid)
        {
            SceneManager.LoadScene(nextScene.SceneName);
        }
        else
        {
            Debug.LogWarning("IntroSequence: No next scene assigned! Loading scene index 1.");
            SceneManager.LoadScene(1);
        }
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null) return;
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
    }

    // Smooth sine easing for cinematic feel
    private float EaseInOutSine(float t)
    {
        return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
    }
}
