using UnityEngine;
using System.Collections;

/// <summary>
/// Screen shake effect for combat feedback.
/// Attach to the main camera or use as a singleton.
/// </summary>
public class CameraShake : MonoBehaviour
{
	public static CameraShake Instance { get; private set; }

	[Header("Shake Settings")]
	[SerializeField] private float defaultIntensity = 0.15f;
	[SerializeField] private float defaultDuration = 0.15f;
	[SerializeField] private float noiseSpeed = 25f;

	private Vector3 originalPosition;
	private Coroutine shakeCoroutine;
	private Transform cameraTransform;
	private float noiseSeed;

	private void Awake()
	{
		// Singleton
		if (Instance != null && Instance != this)
		{
			Destroy(this);
			return;
		}
		Instance = this;

		// Get camera transform
		cameraTransform = Camera.main?.transform;
		if (cameraTransform == null)
		{
			cameraTransform = transform;
		}
		originalPosition = cameraTransform.localPosition;
		noiseSeed = Random.value * 100f;
	}

	/// <summary>
	/// Shake with default settings
	/// </summary>
	public void Shake()
	{
		Shake(defaultIntensity, defaultDuration);
	}

	/// <summary>
	/// Shake with custom intensity and duration
	/// </summary>
	public void Shake(float intensity, float duration)
	{
		if (shakeCoroutine != null)
		{
			StopCoroutine(shakeCoroutine);
			cameraTransform.localPosition = originalPosition;
		}
		shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
	}

	/// <summary>
	/// Light shake for small hits
	/// </summary>
	public void ShakeLight()
	{
		Shake(0.08f, 0.1f);
	}

	/// <summary>
	/// Heavy shake for big hits
	/// </summary>
	public void ShakeHeavy()
	{
		Shake(0.25f, 0.25f);
	}

	private IEnumerator ShakeCoroutine(float intensity, float duration)
	{
		float elapsed = 0f;
		float noiseTime = noiseSeed;

		while (elapsed < duration)
		{
			// Smooth falloff curve (ease out)
			float progress = elapsed / duration;
			float falloff = 1f - (progress * progress);
			float currentIntensity = intensity * falloff;

			// Perlin noise for smooth organic movement
			float offsetX = (Mathf.PerlinNoise(noiseTime, 0f) - 0.5f) * 2f * currentIntensity;
			float offsetY = (Mathf.PerlinNoise(0f, noiseTime) - 0.5f) * 2f * currentIntensity;

			cameraTransform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0f);

			noiseTime += Time.unscaledDeltaTime * noiseSpeed;
			elapsed += Time.unscaledDeltaTime;
			yield return null;
		}

		// Reset position
		cameraTransform.localPosition = originalPosition;
		shakeCoroutine = null;
	}

	/// <summary>
	/// Update original position (call after camera moves to new position)
	/// </summary>
	public void UpdateOriginalPosition()
	{
		if (shakeCoroutine == null)
		{
			originalPosition = cameraTransform.localPosition;
		}
	}

	private void LateUpdate()
	{
		// Keep original position updated when not shaking (for following cameras)
		if (shakeCoroutine == null)
		{
			originalPosition = cameraTransform.localPosition;
		}
	}
}
