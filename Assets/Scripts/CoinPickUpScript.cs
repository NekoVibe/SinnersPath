using UnityEngine;

public class CoinPickup : MonoBehaviour
{
	[Header("Coin Settings")]
	[SerializeField] private int coinValue = 1;
	[SerializeField] private float pickupRadius = 1.5f;
	[SerializeField] private LayerMask playerLayer;

	[Header("Drop Animation")]
	[SerializeField] private bool enableDropAnimation = true;
	[SerializeField] private float dropArcHeight = 1.5f;
	[SerializeField] private float dropDuration = 0.5f;
	[SerializeField] private float dropSpreadRadius = 1f;

	[Header("Idle Animation")]
	[SerializeField] private bool floatAnimation = true;
	[SerializeField] private float floatSpeed = 2f;
	[SerializeField] private float floatAmplitude = 0.15f;
	[SerializeField] private bool pulseAnimation = true;
	[SerializeField] private float pulseSpeed = 3f;
	[SerializeField] private float pulseAmount = 0.1f;

	[Header("Magnetic Pickup")]
	[SerializeField] private bool magneticPickup = true;
	[SerializeField] private float magnetRadius = 3f;
	[SerializeField] private float magnetSpeed = 8f;
	[SerializeField] private float magnetAcceleration = 15f;

	[Header("Collect Animation")]
	[SerializeField] private float collectPopScale = 1.3f;
	[SerializeField] private float collectDuration = 0.15f;

	[Header("Lifetime")]
	[SerializeField] private bool autoDestroy = false;
	[SerializeField] private float lifetime = 10f;

	// State
	private Vector3 startPosition;
	private Vector3 baseScale;
	private float timeAlive = 0f;
	private bool collected = false;
	private bool isDropping = false;
	private bool isMagneting = false;
	private Transform magnetTarget;
	private float currentMagnetSpeed;

	// Drop animation state
	private Vector3 dropStartPos;
	private Vector3 dropEndPos;
	private float dropTimer = 0f;

	private void Start()
	{
		baseScale = transform.localScale;

		if (enableDropAnimation)
		{
			StartDropAnimation();
		}
		else
		{
			startPosition = transform.position;
		}
	}

	private void Update()
	{
		if (collected) return;

		// Handle drop animation
		if (isDropping)
		{
			UpdateDropAnimation();
			return; // Don't do other animations while dropping
		}

		// Handle magnetic pickup
		if (isMagneting && magnetTarget != null)
		{
			UpdateMagneticMovement();
			return;
		}

		// Idle animations
		UpdateIdleAnimations();

		// Auto-destroy
		if (autoDestroy)
		{
			timeAlive += Time.deltaTime;
			if (timeAlive >= lifetime)
			{
				Destroy(gameObject);
			}
		}

		// Check for player
		CheckPlayerPickup();
	}

	private void StartDropAnimation()
	{
		isDropping = true;
		dropTimer = 0f;
		dropStartPos = transform.position;

		// Random end position within spread radius
		Vector2 randomOffset = Random.insideUnitCircle * dropSpreadRadius;
		dropEndPos = dropStartPos + new Vector3(randomOffset.x, 0f, randomOffset.y);

		// Start small and grow during drop
		transform.localScale = baseScale * 0.3f;
	}

	private void UpdateDropAnimation()
	{
		dropTimer += Time.deltaTime;
		float t = Mathf.Clamp01(dropTimer / dropDuration);

		// Ease out curve for smooth landing
		float easeT = 1f - Mathf.Pow(1f - t, 3f);

		// Horizontal movement (linear)
		Vector3 horizontalPos = Vector3.Lerp(dropStartPos, dropEndPos, easeT);

		// Vertical arc (parabola)
		float arcProgress = t * 2f - 1f; // -1 to 1
		float arcHeight = (1f - arcProgress * arcProgress) * dropArcHeight;
		float baseY = Mathf.Lerp(dropStartPos.y, dropEndPos.y, easeT);

		transform.position = new Vector3(horizontalPos.x, baseY + arcHeight, horizontalPos.z);

		// Scale up during drop
		float scaleT = Mathf.SmoothStep(0.3f, 1f, t);
		transform.localScale = baseScale * scaleT;

		// End drop
		if (t >= 1f)
		{
			isDropping = false;
			startPosition = transform.position;
			transform.localScale = baseScale;
		}
	}

	private void UpdateIdleAnimations()
	{
		Vector3 pos = startPosition;
		Vector3 scale = baseScale;

		// Float animation
		if (floatAnimation)
		{
			float floatOffset = Mathf.Sin(Time.time * floatSpeed + GetInstanceID()) * floatAmplitude;
			pos.y += floatOffset;
		}

		// Pulse animation
		if (pulseAnimation)
		{
			float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed + GetInstanceID()) * pulseAmount;
			scale = baseScale * pulse;
		}

		transform.position = pos;
		transform.localScale = scale;
	}

	private void CheckPlayerPickup()
	{
		Collider[] hits = Physics.OverlapSphere(transform.position, magneticPickup ? magnetRadius : pickupRadius, playerLayer);

		foreach (Collider hit in hits)
		{
			if (hit.CompareTag("Player"))
			{
				float distance = Vector3.Distance(transform.position, hit.transform.position);

				// Start magnetic attraction if within magnet radius
				if (magneticPickup && distance <= magnetRadius && !isMagneting)
				{
					StartMagneticPickup(hit.transform);
				}
				// Instant pickup if within pickup radius
				else if (distance <= pickupRadius)
				{
					CollectCoin(hit.gameObject);
				}
				break;
			}
		}
	}

	private void StartMagneticPickup(Transform target)
	{
		isMagneting = true;
		magnetTarget = target;
		currentMagnetSpeed = magnetSpeed * 0.5f;
	}

	private void UpdateMagneticMovement()
	{
		if (magnetTarget == null)
		{
			isMagneting = false;
			return;
		}

		// Accelerate towards player
		currentMagnetSpeed += magnetAcceleration * Time.deltaTime;

		Vector3 direction = (magnetTarget.position - transform.position).normalized;
		transform.position += direction * currentMagnetSpeed * Time.deltaTime;

		// Shrink slightly as approaching
		float distance = Vector3.Distance(transform.position, magnetTarget.position);
		float shrinkFactor = Mathf.Lerp(0.5f, 1f, distance / magnetRadius);
		transform.localScale = baseScale * shrinkFactor;

		// Collect when close enough
		if (distance <= pickupRadius * 0.5f)
		{
			CollectCoin(magnetTarget.gameObject);
		}
	}

	private void CollectCoin(GameObject player)
	{
		if (collected) return;
		collected = true;

		// Give coins
		if (GameManager.Instance != null)
		{
			GameManager.Instance.AddCoins(coinValue);
		}

		// Pop animation then destroy
		StartCoroutine(CollectAnimation());
	}

	private System.Collections.IEnumerator CollectAnimation()
	{
		float timer = 0f;
		Vector3 originalScale = transform.localScale;

		// Pop up
		while (timer < collectDuration * 0.4f)
		{
			timer += Time.deltaTime;
			float t = timer / (collectDuration * 0.4f);
			float scale = Mathf.Lerp(1f, collectPopScale, t);
			transform.localScale = originalScale * scale;
			yield return null;
		}

		// Shrink and fade
		timer = 0f;
		SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
		Color originalColor = sr != null ? sr.color : Color.white;

		while (timer < collectDuration * 0.6f)
		{
			timer += Time.deltaTime;
			float t = timer / (collectDuration * 0.6f);
			float scale = Mathf.Lerp(collectPopScale, 0f, t);
			transform.localScale = originalScale * scale;

			if (sr != null)
			{
				Color c = originalColor;
				c.a = Mathf.Lerp(1f, 0f, t);
				sr.color = c;
			}

			yield return null;
		}

		Destroy(gameObject);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player") && !collected && !isDropping)
		{
			if (magneticPickup && !isMagneting)
			{
				StartMagneticPickup(other.transform);
			}
			else
			{
				CollectCoin(other.gameObject);
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, pickupRadius);

		if (magneticPickup)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(transform.position, magnetRadius);
		}
	}

	/// <summary>
	/// Call this when spawning a coin to trigger the drop animation
	/// </summary>
	public void TriggerDrop(Vector3 spawnPosition)
	{
		transform.position = spawnPosition;
		if (enableDropAnimation)
		{
			StartDropAnimation();
		}
	}
}