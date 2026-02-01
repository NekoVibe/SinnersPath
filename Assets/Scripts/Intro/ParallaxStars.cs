using UnityEngine;
using System.Collections;

/// <summary>
/// Creates a parallax starry sky using world-space sprites.
/// Stars move slower than the camera for depth effect.
/// </summary>
public class ParallaxStars : MonoBehaviour
{
    [Header("Star Settings")]
    [SerializeField] private int starCount = 100;
    [SerializeField] private float minStarSize = 0.02f;
    [SerializeField] private float maxStarSize = 0.08f;
    [SerializeField] private Color starColor = new Color(1f, 1f, 1f, 0.8f);

    [Header("Sky Area (World Units)")]
    [SerializeField] private float skyWidth = 50f;
    [SerializeField] private float skyMinHeight = 2f;   // Stars start above this Y
    [SerializeField] private float skyMaxHeight = 15f;  // Stars end at this Y
    [SerializeField] private float skyDepth = 10f;      // Z position (behind everything)

    [Header("Parallax")]
    [SerializeField] private float parallaxFactor = 0.1f; // 0 = no movement, 1 = moves with camera
    [SerializeField] private Transform cameraTransform;

    [Header("Twinkle")]
    [SerializeField] private bool enableTwinkle = true;
    [SerializeField] private float twinkleSpeed = 0.5f;

    private Star[] stars;
    private Vector3 lastCameraPos;
    private float initialCenterX;

    private class Star
    {
        public SpriteRenderer renderer;
        public float twinkleOffset;
        public float twinkleSpeedMult;
        public float baseAlpha;
        public Vector3 initialLocalPos;
    }

    private void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;

        if (cameraTransform != null)
            lastCameraPos = cameraTransform.position;

        initialCenterX = transform.position.x;

        CreateStars();

        if (enableTwinkle)
            StartCoroutine(TwinkleRoutine());
    }

    private void CreateStars()
    {
        stars = new Star[starCount];

        // Create a simple white square sprite
        Texture2D tex = new Texture2D(4, 4);
        Color[] colors = new Color[16];
        for (int i = 0; i < 16; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        Sprite starSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);

        for (int i = 0; i < starCount; i++)
        {
            GameObject starObj = new GameObject($"Star_{i}");
            starObj.transform.SetParent(transform);

            SpriteRenderer sr = starObj.AddComponent<SpriteRenderer>();
            sr.sprite = starSprite;
            sr.sortingOrder = -100; // Behind everything

            // Random position in sky area
            float x = Random.Range(-skyWidth / 2f, skyWidth / 2f);
            float y = Random.Range(skyMinHeight, skyMaxHeight);
            Vector3 localPos = new Vector3(x, y, skyDepth);
            starObj.transform.localPosition = localPos;

            // Random size
            float size = Random.Range(minStarSize, maxStarSize);
            starObj.transform.localScale = Vector3.one * size;

            // Color with random alpha
            float alpha = Random.Range(0.3f, 0.9f);
            sr.color = new Color(starColor.r, starColor.g, starColor.b, alpha);

            stars[i] = new Star
            {
                renderer = sr,
                twinkleOffset = Random.Range(0f, Mathf.PI * 2f),
                twinkleSpeedMult = Random.Range(0.5f, 1.5f),
                baseAlpha = alpha,
                initialLocalPos = localPos
            };
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Parallax movement - stars follow camera but slower
        Vector3 cameraDelta = cameraTransform.position - lastCameraPos;
        transform.position += new Vector3(cameraDelta.x * parallaxFactor, cameraDelta.y * parallaxFactor, 0f);

        lastCameraPos = cameraTransform.position;

        // Keep stars centered around camera (infinite scrolling effect)
        WrapStars();
    }

    private void WrapStars()
    {
        if (stars == null) return;

        float cameraX = cameraTransform.position.x;
        float halfWidth = skyWidth / 2f;

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null || stars[i].renderer == null) continue;

            Transform starTransform = stars[i].renderer.transform;
            float starWorldX = starTransform.position.x;

            // Wrap stars that go too far from camera
            if (starWorldX < cameraX - halfWidth)
            {
                starTransform.position += new Vector3(skyWidth, 0f, 0f);
            }
            else if (starWorldX > cameraX + halfWidth)
            {
                starTransform.position -= new Vector3(skyWidth, 0f, 0f);
            }
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
                if (star == null || star.renderer == null) continue;

                float wave = Mathf.Sin(time * star.twinkleSpeedMult + star.twinkleOffset);
                float alpha = Mathf.Lerp(star.baseAlpha * 0.3f, star.baseAlpha, (wave + 1f) * 0.5f);

                Color c = star.renderer.color;
                c.a = alpha;
                star.renderer.color = c;
            }

            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize sky area in editor
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Vector3 center = transform.position + new Vector3(0f, (skyMinHeight + skyMaxHeight) / 2f, skyDepth);
        Vector3 size = new Vector3(skyWidth, skyMaxHeight - skyMinHeight, 0.1f);
        Gizmos.DrawCube(center, size);
        Gizmos.DrawWireCube(center, size);
    }
}
