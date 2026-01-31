using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Creates afterimage trail effect during dash.
/// Spawns fading copies of the sprite at intervals.
/// </summary>
public class DashTrailEffect : MonoBehaviour
{
	public static DashTrailEffect Instance { get; private set; }

	[Header("Trail Settings")]
	[SerializeField] private float spawnInterval = 0.02f; // Time between afterimages
	[SerializeField] private float fadeDuration = 0.2f;   // How long each afterimage lasts
	[SerializeField] private int maxAfterimages = 10;     // Pool size
	[SerializeField] private float colorDarken = 0.3f;    // Darken afterimage color

	[Header("References")]
	[SerializeField] private SpriteRenderer sourceSprite; // The sprite to copy

	private List<SpriteRenderer> afterimagePool = new List<SpriteRenderer>();
	private int currentIndex = 0;
	private bool isTrailActive = false;
	private Coroutine trailCoroutine;

	private void Awake()
	{
		// Singleton
		if (Instance != null && Instance != this)
		{
			Destroy(this);
			return;
		}
		Instance = this;

		// Auto-find sprite if not assigned
		if (sourceSprite == null)
		{
			sourceSprite = GetComponent<SpriteRenderer>();
		}

		// Create afterimage pool
		CreatePool();
	}

	private void CreatePool()
	{
		for (int i = 0; i < maxAfterimages; i++)
		{
			GameObject afterimage = new GameObject($"Afterimage_{i}");
			afterimage.transform.SetParent(null); // Keep in world space
			DontDestroyOnLoad(afterimage);

			SpriteRenderer sr = afterimage.AddComponent<SpriteRenderer>();
			sr.sortingLayerName = sourceSprite != null ? sourceSprite.sortingLayerName : "Default";
			sr.sortingOrder = sourceSprite != null ? sourceSprite.sortingOrder : -1;
			sr.enabled = false;

			afterimagePool.Add(sr);
		}
	}

	/// <summary>
	/// Start spawning afterimages
	/// </summary>
	public void StartTrail()
	{
		if (sourceSprite == null) return;

		isTrailActive = true;
		if (trailCoroutine != null)
		{
			StopCoroutine(trailCoroutine);
		}
		trailCoroutine = StartCoroutine(SpawnTrailRoutine());
	}

	/// <summary>
	/// Stop spawning new afterimages (existing ones will fade out)
	/// </summary>
	public void StopTrail()
	{
		isTrailActive = false;
		if (trailCoroutine != null)
		{
			StopCoroutine(trailCoroutine);
			trailCoroutine = null;
		}
	}

	private IEnumerator SpawnTrailRoutine()
	{
		while (isTrailActive)
		{
			SpawnAfterimage();
			yield return new WaitForSeconds(spawnInterval);
		}
	}

	private void SpawnAfterimage()
	{
		if (sourceSprite == null || sourceSprite.sprite == null) return;

		SpriteRenderer afterimage = afterimagePool[currentIndex];
		currentIndex = (currentIndex + 1) % maxAfterimages;

		// Copy sprite properties
		afterimage.sprite = sourceSprite.sprite;
		afterimage.flipX = sourceSprite.flipX;
		afterimage.flipY = sourceSprite.flipY;

		// Position and rotation
		afterimage.transform.position = sourceSprite.transform.position;
		afterimage.transform.rotation = sourceSprite.transform.rotation;
		afterimage.transform.localScale = sourceSprite.transform.lossyScale;

		// Darken color for afterimage effect
		Color baseColor = sourceSprite.color;
		afterimage.color = new Color(
			baseColor.r * (1f - colorDarken),
			baseColor.g * (1f - colorDarken),
			baseColor.b * (1f - colorDarken),
			baseColor.a * 0.7f
		);

		afterimage.enabled = true;

		// Start fade coroutine
		StartCoroutine(FadeAfterimage(afterimage));
	}

	private IEnumerator FadeAfterimage(SpriteRenderer afterimage)
	{
		Color startColor = afterimage.color;
		float elapsed = 0f;

		while (elapsed < fadeDuration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / fadeDuration;

			Color c = startColor;
			c.a = Mathf.Lerp(startColor.a, 0f, t);
			afterimage.color = c;

			yield return null;
		}

		afterimage.enabled = false;
	}

	/// <summary>
	/// Spawn a single afterimage (for attacks, jumps, etc.)
	/// </summary>
	public void SpawnSingle()
	{
		SpawnAfterimage();
	}

	private void OnDestroy()
	{
		// Clean up pool
		foreach (var afterimage in afterimagePool)
		{
			if (afterimage != null)
			{
				Destroy(afterimage.gameObject);
			}
		}
	}
}
