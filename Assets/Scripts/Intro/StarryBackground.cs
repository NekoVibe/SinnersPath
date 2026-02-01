using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Creates a calm starry sky background with subtle twinkling.
/// Stars only appear in the upper portion of the screen (sky area).
/// </summary>
public class StarryBackground : MonoBehaviour
{
    [Header("Star Settings")]
    [SerializeField] private int starCount = 80;
    [SerializeField] private float minStarSize = 2f;
    [SerializeField] private float maxStarSize = 6f;
    [SerializeField] private Color starColor = new Color(1f, 1f, 1f, 0.8f);

    [Header("Sky Area")]
    [SerializeField] [Range(0f, 1f)] private float skyStartHeight = 0.35f; // Stars start at 35% from bottom
    [SerializeField] [Range(0f, 1f)] private float skyEndHeight = 1f;      // Stars end at top

    [Header("Twinkle Settings")]
    [SerializeField] private bool enableTwinkle = true;
    [SerializeField] private float twinkleSpeed = 0.5f;
    [SerializeField] private float minAlpha = 0.2f;
    [SerializeField] private float maxAlpha = 0.9f;

    private Star[] stars;
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    private class Star
    {
        public Image image;
        public float twinkleOffset;
        public float twinkleSpeedMult;
        public float baseAlpha;
    }

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (rectTransform != null)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        StartCoroutine(CreateStarsDelayed());
    }

    private IEnumerator CreateStarsDelayed()
    {
        yield return new WaitForEndOfFrame();

        CreateStars();

        if (enableTwinkle)
            StartCoroutine(TwinkleRoutine());
    }

    private void CreateStars()
    {
        stars = new Star[starCount];

        float width, height;

        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            width = Screen.width;
            height = Screen.height;
        }
        else if (rectTransform != null && rectTransform.rect.width > 0)
        {
            width = rectTransform.rect.width;
            height = rectTransform.rect.height;
        }
        else
        {
            width = Screen.width;
            height = Screen.height;
        }

        // Calculate sky bounds (only upper portion)
        float skyMinY = -height / 2f + (height * skyStartHeight);
        float skyMaxY = -height / 2f + (height * skyEndHeight);

        for (int i = 0; i < starCount; i++)
        {
            GameObject starObj = new GameObject($"Star_{i}");
            starObj.transform.SetParent(transform, false);

            RectTransform rect = starObj.AddComponent<RectTransform>();
            Image img = starObj.AddComponent<Image>();

            // Random position - full width, but only in sky area
            float x = Random.Range(-width / 2f, width / 2f);
            float y = Random.Range(skyMinY, skyMaxY);
            rect.anchoredPosition = new Vector2(x, y);

            // Random size
            float size = Random.Range(minStarSize, maxStarSize);
            rect.sizeDelta = new Vector2(size, size);

            // Color with random alpha
            float alpha = Random.Range(minAlpha, maxAlpha);
            img.color = new Color(starColor.r, starColor.g, starColor.b, alpha);
            img.raycastTarget = false;

            stars[i] = new Star
            {
                image = img,
                twinkleOffset = Random.Range(0f, Mathf.PI * 2f),
                twinkleSpeedMult = Random.Range(0.5f, 1.5f),
                baseAlpha = alpha
            };
        }
    }

    private IEnumerator TwinkleRoutine()
    {
        float time = 0f;

        while (true)
        {
            time += Time.deltaTime * twinkleSpeed;

            for (int i = 0; i < stars.Length; i++)
            {
                Star star = stars[i];
                if (star == null || star.image == null) continue;

                float wave = Mathf.Sin(time * star.twinkleSpeedMult + star.twinkleOffset);
                float alpha = Mathf.Lerp(star.baseAlpha * 0.4f, star.baseAlpha, (wave + 1f) * 0.5f);

                Color c = star.image.color;
                c.a = alpha;
                star.image.color = c;
            }

            yield return null;
        }
    }
}
