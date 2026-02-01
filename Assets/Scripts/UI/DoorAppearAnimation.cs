using UnityEngine;
using System.Collections;

/// <summary>
/// Animates the door appearing with a scale-up effect and optional glow.
/// Attach to the ExitDoor prefab.
/// </summary>
public class DoorAppearAnimation : MonoBehaviour
{
    [Header("Scale Animation")]
    [SerializeField] private float appearDuration = 0.8f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool animateOnStart = true;

    [Header("Glow Effect")]
    [SerializeField] private float glowIntensity = 2f;
    [SerializeField] private Color glowColor = new Color(1f, 0.5f, 0f, 1f); // Orange glow
    [SerializeField] private float glowPulseDuration = 1.5f;

    private Vector3 targetScale;
    private Renderer doorRenderer;
    private MaterialPropertyBlock propBlock;
    private Coroutine glowCoroutine;

    private void Awake()
    {
        targetScale = transform.localScale;
        doorRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();

        if (animateOnStart)
        {
            // Start hidden
            transform.localScale = Vector3.zero;
        }
    }

    private void Start()
    {
        if (animateOnStart)
        {
            PlayAppearAnimation();
        }
    }

    public void PlayAppearAnimation()
    {
        StartCoroutine(AppearCoroutine());
    }

    private IEnumerator AppearCoroutine()
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;

        // Scale up animation
        while (elapsed < appearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / appearDuration;
            float curveValue = scaleCurve.Evaluate(t);

            transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, curveValue);
            yield return null;
        }

        transform.localScale = targetScale;

        // Start glow pulse after appearing
        if (doorRenderer != null)
        {
            glowCoroutine = StartCoroutine(GlowPulseCoroutine());
        }
    }

    private IEnumerator GlowPulseCoroutine()
    {
        while (true)
        {
            float elapsed = 0f;
            while (elapsed < glowPulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / glowPulseDuration;

                // Pulse intensity using sine wave
                float intensity = glowIntensity * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f));

                // Apply emission color
                if (doorRenderer != null)
                {
                    doorRenderer.GetPropertyBlock(propBlock);
                    propBlock.SetColor("_EmissionColor", glowColor * intensity);
                    doorRenderer.SetPropertyBlock(propBlock);
                }

                yield return null;
            }
        }
    }

    private void OnDestroy()
    {
        if (glowCoroutine != null)
        {
            StopCoroutine(glowCoroutine);
        }
    }
}
