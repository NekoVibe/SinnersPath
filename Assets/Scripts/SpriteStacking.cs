using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Creates a sprite stacking effect for 2.5D depth/thickness.
/// Renders multiple copies of a sprite stacked vertically to create
/// volume illusion, similar to Cult of the Lamb's character depth effect.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteStacking : MonoBehaviour
{
	[Header("Stack Settings")]
	[SerializeField, Range(2, 15)] private int layerCount = 5;
	[SerializeField] private float layerSpacing = 0.02f;

	[Header("Visual Effects")]
	[SerializeField] private bool enableDarkening = true;
	[SerializeField, Range(0f, 1f)] private float darkenAmount = 0.4f;

	// References
	private SpriteRenderer sourceRenderer;
	private Transform cachedTransform;

	// Layer management
	private GameObject layerContainer;
	private SpriteRenderer[] layerRenderers;
	private Transform[] layerTransforms;

	// Tracking for optimization
	private Sprite cachedSprite;
	private bool cachedFlipX;
	private Vector3 cachedScale;
	private int cachedLayerCount;

	private bool IsInPrefabEditMode()
	{
#if UNITY_EDITOR
		return PrefabStageUtility.GetCurrentPrefabStage() != null;
#else
		return false;
#endif
	}

	private void OnEnable()
	{
		if (IsInPrefabEditMode()) return;

		InitializeReferences();
		CreateLayerStack();
	}

	private void Start()
	{
		if (IsInPrefabEditMode()) return;

		if (layerContainer == null)
		{
			InitializeReferences();
			CreateLayerStack();
		}

		if (Application.isPlaying && layerContainer != null)
		{
			DontDestroyOnLoad(layerContainer);
		}
	}

	private void InitializeReferences()
	{
		cachedTransform = transform;
		sourceRenderer = GetComponent<SpriteRenderer>();
	}

	private void CreateLayerStack()
	{
		DestroyLayers();

		if (sourceRenderer == null) return;

		// Create container at scene root to avoid transform inheritance
		layerContainer = new GameObject($"SpriteStack_{gameObject.name}");
		layerContainer.hideFlags = Application.isPlaying ? HideFlags.None : HideFlags.DontSave;
		layerContainer.transform.SetParent(null);

		// Pre-allocate arrays
		layerRenderers = new SpriteRenderer[layerCount];
		layerTransforms = new Transform[layerCount];

		for (int i = 0; i < layerCount; i++)
		{
			GameObject layer = new GameObject($"Layer_{i}");
			layer.hideFlags = layerContainer.hideFlags;
			layer.transform.SetParent(layerContainer.transform);

			SpriteRenderer sr = layer.AddComponent<SpriteRenderer>();
			layerRenderers[i] = sr;
			layerTransforms[i] = layer.transform;
		}

		cachedLayerCount = layerCount;
		UpdateLayers();
	}

	private void LateUpdate()
	{
		if (IsInPrefabEditMode()) return;

		// Recreate if destroyed or layer count changed
		if (layerContainer == null || cachedLayerCount != layerCount)
		{
			InitializeReferences();
			CreateLayerStack();
			return;
		}

		UpdateLayers();
	}

	private void UpdateLayers()
	{
		if (sourceRenderer == null || layerRenderers == null) return;

		Sprite currentSprite = sourceRenderer.sprite;
		if (currentSprite == null) return;

		// Get base values from source
		int baseSortingOrder = sourceRenderer.sortingOrder;
		string sortingLayerName = sourceRenderer.sortingLayerName;
		Color sourceColor = sourceRenderer.color;
		bool flipX = sourceRenderer.flipX;
		bool flipY = sourceRenderer.flipY;

		for (int i = 0; i < layerCount; i++)
		{
			if (layerRenderers[i] == null || layerTransforms[i] == null) continue;

			SpriteRenderer layerSr = layerRenderers[i];
			Transform layerTr = layerTransforms[i];

			// Copy sprite
			layerSr.sprite = currentSprite;
			layerSr.flipX = flipX;
			layerSr.flipY = flipY;

			// Calculate layer offset (Z axis for depth, always behind)
			float layerOffset = (i + 1) * layerSpacing;

			// Position: match source but offset on Z axis (into the screen)
			Vector3 position = cachedTransform.position;
			position.z += layerOffset;
			layerTr.position = position;

			// Scale: match source (including any flip via scale)
			layerTr.localScale = cachedTransform.lossyScale;

			// Darken layers further from main sprite
			if (enableDarkening && layerCount > 1)
			{
				float t = (float)i / (layerCount - 1);
				float darkness = Mathf.Lerp(1f, 1f - darkenAmount, t);
				Color layerColor = sourceColor * darkness;
				layerColor.a = sourceColor.a;
				layerSr.color = layerColor;
			}
			else
			{
				layerSr.color = sourceColor;
			}

			// Sorting order: layers always behind player
			layerSr.sortingLayerName = sortingLayerName;
			layerSr.sortingOrder = baseSortingOrder - layerCount - i;
		}

		// Cache state
		cachedSprite = currentSprite;
		cachedFlipX = flipX;
		cachedScale = cachedTransform.lossyScale;
	}

	private void OnDisable()
	{
		DestroyLayers();
	}

	private void OnDestroy()
	{
		DestroyLayers();
	}

	private void DestroyLayers()
	{
		if (layerContainer != null)
		{
			if (Application.isPlaying)
				Destroy(layerContainer);
			else
				DestroyImmediate(layerContainer);
		}

		layerRenderers = null;
		layerTransforms = null;
	}

	/// <summary>
	/// Update layer count at runtime (recreates layers)
	/// </summary>
	public void SetLayerCount(int count)
	{
		layerCount = Mathf.Clamp(count, 2, 15);
		CreateLayerStack();
	}

	/// <summary>
	/// Temporarily hide/show stack (for effects like flash)
	/// </summary>
	public void SetStackVisible(bool visible)
	{
		if (layerContainer != null)
			layerContainer.SetActive(visible);
	}

	/// <summary>
	/// Get layer renderers for external effects (e.g., SpriteFlash integration)
	/// </summary>
	public SpriteRenderer[] GetLayerRenderers()
	{
		return layerRenderers;
	}

	/// <summary>
	/// Force immediate update (call after sprite change)
	/// </summary>
	public void ForceUpdate()
	{
		UpdateLayers();
	}
}
