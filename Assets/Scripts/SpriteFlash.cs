using UnityEngine;
using System.Collections;

/// <summary>
/// Flashes sprite white when hit.
/// Attach to any GameObject with a SpriteRenderer.
/// </summary>
public class SpriteFlash : MonoBehaviour
{
	[Header("Flash Settings")]
	[SerializeField] private Color flashColor = Color.white;
	[SerializeField] private float flashDuration = 0.1f;

	private SpriteRenderer spriteRenderer;
	private Material originalMaterial;
	private Material flashMaterial;
	private Coroutine flashCoroutine;

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();

		if (spriteRenderer != null)
		{
			originalMaterial = spriteRenderer.material;

			// Create flash material
			flashMaterial = new Material(Shader.Find("Sprites/Default"));
			flashMaterial.color = flashColor;
		}
	}

	/// <summary>
	/// Flash the sprite white
	/// </summary>
	public void Flash()
	{
		Flash(flashDuration);
	}

	/// <summary>
	/// Flash with custom duration
	/// </summary>
	public void Flash(float duration)
	{
		if (spriteRenderer == null) return;

		if (flashCoroutine != null)
		{
			StopCoroutine(flashCoroutine);
		}
		flashCoroutine = StartCoroutine(FlashCoroutine(duration));
	}

	/// <summary>
	/// Flash with custom color and duration
	/// </summary>
	public void Flash(Color color, float duration)
	{
		if (spriteRenderer == null) return;

		flashMaterial.color = color;

		if (flashCoroutine != null)
		{
			StopCoroutine(flashCoroutine);
		}
		flashCoroutine = StartCoroutine(FlashCoroutine(duration));
	}

	private IEnumerator FlashCoroutine(float duration)
	{
		// Store current color
		Color originalColor = spriteRenderer.color;

		// Flash to white
		spriteRenderer.color = flashColor;

		yield return new WaitForSecondsRealtime(duration);

		// Restore original color
		spriteRenderer.color = originalColor;
		flashCoroutine = null;
	}

	/// <summary>
	/// Blink effect (multiple flashes)
	/// </summary>
	public void Blink(int times, float interval = 0.1f)
	{
		if (flashCoroutine != null)
		{
			StopCoroutine(flashCoroutine);
		}
		flashCoroutine = StartCoroutine(BlinkCoroutine(times, interval));
	}

	private IEnumerator BlinkCoroutine(int times, float interval)
	{
		Color originalColor = spriteRenderer.color;

		for (int i = 0; i < times; i++)
		{
			spriteRenderer.color = flashColor;
			yield return new WaitForSecondsRealtime(interval);
			spriteRenderer.color = originalColor;
			yield return new WaitForSecondsRealtime(interval);
		}

		flashCoroutine = null;
	}

	private void OnDestroy()
	{
		// Clean up flash material
		if (flashMaterial != null)
		{
			Destroy(flashMaterial);
		}
	}
}
