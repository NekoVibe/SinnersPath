using UnityEngine;

/// <summary>
/// Creates a fake shadow under sprite characters.
/// Common technique in 2.5D games like Cult of the Lamb.
/// </summary>
[ExecuteAlways]
public class SpriteShadow : MonoBehaviour
{
	[Header("Shadow Settings")]
	[SerializeField] private Vector2 shadowSize = new Vector2(3f, 1.5f);
	[SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.4f);

	[Header("Ground Detection")]
	[SerializeField] private float groundY = -0.5f;
	[SerializeField] private bool autoDetectGround = true;
	[SerializeField] private LayerMask groundLayer = ~0;
	[SerializeField] private float maxRayDistance = 20f;

	[Header("Shadow Scaling")]
	[SerializeField] private bool scaleWithHeight = true;
	[SerializeField] private float maxHeightForScaling = 10f;
	[SerializeField] private float minScale = 0.5f;

	private GameObject shadowObject;
	private SpriteRenderer shadowRenderer;
	private Transform shadowTransform;
	private Sprite cachedSprite;

	private void OnEnable()
	{
		// Create shadow if it doesn't exist
		if (shadowObject == null)
		{
			CreateShadow();
		}
	}

	private void Start()
	{
		// Ensure shadow exists
		if (shadowObject == null)
		{
			CreateShadow();
		}

		// Persist shadow across scene loads (only in play mode)
		if (Application.isPlaying && shadowObject != null)
		{
			DontDestroyOnLoad(shadowObject);
		}
	}

	private void CreateShadow()
	{
		// Clean up any existing shadow first
		if (shadowObject != null)
		{
			if (Application.isPlaying)
				Destroy(shadowObject);
			else
				DestroyImmediate(shadowObject);
		}

		// Create shadow GameObject
		shadowObject = new GameObject("Shadow_" + gameObject.name);
		shadowObject.hideFlags = Application.isPlaying ? HideFlags.None : HideFlags.DontSave;
		shadowTransform = shadowObject.transform;

		// In play mode, parent to same parent (so it doesn't rotate with character)
		// In edit mode, make it a child so it moves with the object in scene view
		if (Application.isPlaying)
		{
			shadowTransform.SetParent(transform.parent);
		}
		else
		{
			shadowTransform.SetParent(transform);
		}

		// Add SpriteRenderer
		shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
		cachedSprite = CreateEllipseSprite();
		shadowRenderer.sprite = cachedSprite;
		shadowRenderer.color = shadowColor;
		shadowRenderer.sortingOrder = -100; // Behind everything

		// Initial position
		UpdateShadow();
	}

	private Sprite CreateEllipseSprite()
	{
		// Create a simple hard-edged ellipse (stylized, matches cartoon art)
		int width = 32;
		int height = 16;
		Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
		texture.filterMode = FilterMode.Point; // Sharp pixels, no blur

		Color[] pixels = new Color[width * height];
		Vector2 center = new Vector2(width / 2f, height / 2f);
		float radiusX = width / 2f - 1f;
		float radiusY = height / 2f - 1f;

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				float dx = (x - center.x + 0.5f) / radiusX;
				float dy = (y - center.y + 0.5f) / radiusY;
				float distance = dx * dx + dy * dy;

				// Hard edge - fully opaque inside, transparent outside
				pixels[y * width + x] = distance <= 1f ? Color.white : Color.clear;
			}
		}

		texture.SetPixels(pixels);
		texture.Apply();

		// Create sprite (32 pixels per unit for chunky look)
		return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
	}

	private void LateUpdate()
	{
		UpdateShadow();
	}

	private void UpdateShadow()
	{
		// Recreate shadow if it was destroyed
		if (shadowTransform == null || shadowObject == null)
		{
			CreateShadow();
			if (shadowTransform == null) return;
		}

		// Update color if changed in inspector
		if (shadowRenderer != null && shadowRenderer.color != shadowColor)
		{
			shadowRenderer.color = shadowColor;
		}

		// Detect ground position
		float currentGroundY = groundY;
		if (autoDetectGround)
		{
			if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, maxRayDistance, groundLayer))
			{
				currentGroundY = hit.point.y;
			}
		}

		// Position shadow on ground directly below character
		shadowTransform.position = new Vector3(
			transform.position.x,
			currentGroundY + 0.01f,
			transform.position.z
		);

		// Rotate to lie flat on ground
		shadowTransform.rotation = Quaternion.Euler(90f, 0f, 0f);

		// Scale based on height
		float scale = 1f;
		if (scaleWithHeight)
		{
			float height = transform.position.y - currentGroundY;
			float heightRatio = Mathf.Clamp01(height / maxHeightForScaling);
			scale = Mathf.Lerp(1f, minScale, heightRatio);
		}

		shadowTransform.localScale = new Vector3(shadowSize.x * scale, shadowSize.y * scale, 1f);
	}

	private void OnDisable()
	{
		// Clean up shadow in editor when component is disabled
		if (!Application.isPlaying && shadowObject != null)
		{
			DestroyImmediate(shadowObject);
			shadowObject = null;
		}
	}

	private void OnDestroy()
	{
		if (shadowObject != null)
		{
			if (Application.isPlaying)
				Destroy(shadowObject);
			else
				DestroyImmediate(shadowObject);
		}

		// Clean up cached sprite
		if (cachedSprite != null)
		{
			if (Application.isPlaying)
				Destroy(cachedSprite.texture);
			else
				DestroyImmediate(cachedSprite.texture);
		}
	}

	// Public methods for customization
	public void SetShadowSize(Vector2 size)
	{
		shadowSize = size;
	}

	public void SetShadowColor(Color color)
	{
		shadowColor = color;
		if (shadowRenderer != null)
		{
			shadowRenderer.color = color;
		}
	}

	public void SetShadowVisible(bool visible)
	{
		if (shadowObject != null)
		{
			shadowObject.SetActive(visible);
		}
	}
}
