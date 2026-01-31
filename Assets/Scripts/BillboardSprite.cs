using UnityEngine;

/// <summary>
/// Makes a sprite always face the camera (billboard effect).
/// Attach to any GameObject with a SpriteRenderer for paper-like 2.5D appearance.
/// </summary>
public class BillboardSprite : MonoBehaviour
{
	[Header("Billboard Settings")]
	[SerializeField] private bool lockYAxis = true; // Only rotate on Y axis (upright sprites)
	[SerializeField] private bool flipWithMovement = true; // Flip sprite based on movement

	[Header("Flip Settings")]
	[SerializeField] private float flipThreshold = 0.1f;

	private Camera mainCamera;
	private SpriteRenderer spriteRenderer;
	private Vector3 lastPosition;
	private bool facingRight = true;

	private void Awake()
	{
		mainCamera = Camera.main;
		spriteRenderer = GetComponent<SpriteRenderer>();
		lastPosition = transform.position;
	}

	private void LateUpdate()
	{
		if (mainCamera == null)
		{
			mainCamera = Camera.main;
			if (mainCamera == null) return;
		}

		// Billboard rotation
		ApplyBillboard();

		// Flip based on movement direction
		if (flipWithMovement && spriteRenderer != null)
		{
			ApplyFlip();
		}
	}

	private void ApplyBillboard()
	{
		if (lockYAxis)
		{
			// Only rotate on Y axis - keeps sprite upright
			Vector3 lookDir = mainCamera.transform.position - transform.position;
			lookDir.y = 0;

			if (lookDir.sqrMagnitude > 0.001f)
			{
				transform.rotation = Quaternion.LookRotation(-lookDir);
			}
		}
		else
		{
			// Full billboard - face camera completely
			transform.rotation = mainCamera.transform.rotation;
		}
	}

	private void ApplyFlip()
	{
		Vector3 movement = transform.position - lastPosition;

		// Only flip if moving significantly on X axis
		if (Mathf.Abs(movement.x) > flipThreshold * Time.deltaTime)
		{
			bool shouldFaceRight = movement.x > 0;

			if (shouldFaceRight != facingRight)
			{
				facingRight = shouldFaceRight;
				spriteRenderer.flipX = !facingRight;
			}
		}

		lastPosition = transform.position;
	}

	/// <summary>
	/// Manually set facing direction (useful for attacks, etc.)
	/// </summary>
	public void SetFacing(bool faceRight)
	{
		facingRight = faceRight;
		if (spriteRenderer != null)
		{
			spriteRenderer.flipX = !facingRight;
		}
	}

	/// <summary>
	/// Get current facing direction
	/// </summary>
	public bool IsFacingRight()
	{
		return facingRight;
	}
}
