using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
	[Header("Target Settings")]
	[SerializeField] private Transform target; // El player
	[SerializeField] private bool findPlayerAutomatically = true;

	[Header("Initialization")]
	[SerializeField] private bool initializePositionFromPlayer = false;

	[Header("Offset")]
	[SerializeField] private Vector3 offset = new Vector3(0f, 8.9f, -17f);

	[Header("Follow Settings")]
	[SerializeField] private bool followX = false;
	[SerializeField] private bool followY = false;
	[SerializeField] private bool followZ = false;

	[Header("Smoothing")]
	[SerializeField] private bool useSmoothing = true;
	[SerializeField] private float smoothSpeed = 5f;

	[Header("Limits (Optional)")]
	[SerializeField] private bool useLimits = false;
	[SerializeField] private float minX = -100f;
	[SerializeField] private float maxX = 100f;

	[Header("Wall-Based Limits")]
	[SerializeField] private bool useWallLimits = false;
	[SerializeField] private Transform leftWall;
	[SerializeField] private Transform rightWall;
	[SerializeField] private Transform frontWall;
	[SerializeField] private Transform backWall;
	[SerializeField] private float groundPlaneY = 0f;
	[SerializeField] private float wallMargin = 0.5f;

	// Cached frustum calculations
	private Camera cam;
	private float cachedFrustumHalfWidth;
	private float cachedFrustumHalfHeight;
	private float lastAspectRatio = -1f;

	[Header("Update Mode")]
	[SerializeField] private bool useFixedUpdate = false;

	private void Start()
	{
		// Buscar el player automáticamente si no está asignado
		if (findPlayerAutomatically && target == null)
		{
			FindPlayer();
		}

		if (target == null)
		{
			Debug.LogWarning("CameraFollow: No target assigned and couldn't find player!");
			return;
		}

		if (initializePositionFromPlayer)
		{
			// Wait for player to be repositioned (PlayerPersistence waits 2 frames)
			StartCoroutine(InitializePositionDelayed());
		}
	}

	private IEnumerator InitializePositionDelayed()
	{
		// Wait 3 frames to ensure player has been repositioned
		yield return null;
		yield return null;
		yield return null;

		if (target != null)
		{
			transform.position = target.position + offset;
		}
	}

	private void LateUpdate()
	{
		if (!useFixedUpdate)
		{
			UpdateCamera();
		}
	}

	private void FixedUpdate()
	{
		if (useFixedUpdate)
		{
			UpdateCamera();
		}
	}

	private void UpdateCamera()
	{
		if (target == null)
		{
			// Intentar encontrar el player de nuevo (por si se recreó)
			if (findPlayerAutomatically)
			{
				FindPlayer();
			}
			return;
		}

		FollowTarget();
	}

	private void FollowTarget()
	{
		// Calculate frustum dimensions if using wall limits
		if (useWallLimits)
			CalculateFrustumDimensions();

		// Calcular posición deseada
		Vector3 desiredPosition = transform.position;

		if (followX)
		{
			desiredPosition.x = target.position.x + offset.x;

			// Apply wall-based limits if enabled
			if (useWallLimits && (leftWall != null || rightWall != null))
			{
				desiredPosition.x = CalculateWallClampedPosition(
					desiredPosition.x, leftWall, rightWall,
					cachedFrustumHalfWidth, isXAxis: true);
			}
			// Fallback to static limits
			else if (useLimits)
			{
				desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
			}
		}

		if (followY)
		{
			desiredPosition.y = target.position.y + offset.y;
		}

		if (followZ)
		{
			desiredPosition.z = target.position.z + offset.z;

			// Apply wall-based limits for Z axis
			if (useWallLimits && (frontWall != null || backWall != null))
			{
				desiredPosition.z = CalculateWallClampedPosition(
					desiredPosition.z, frontWall, backWall,
					cachedFrustumHalfHeight, isXAxis: false);
			}
		}

		// Aplicar suavizado o movimiento directo
		if (useSmoothing)
		{
			transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
		}
		else
		{
			transform.position = desiredPosition;
		}
	}

	// Buscar el player en la escena
	private void FindPlayer()
	{
		// Intentar por tag
		GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

		if (playerObj != null)
		{
			target = playerObj.transform;
			Debug.Log("CameraFollow: Player found by tag");
			return;
		}

		// Intentar por PlayerPersistence
		if (PlayerPersistence.Instance != null)
		{
			target = PlayerPersistence.Instance.transform;
			Debug.Log("CameraFollow: Player found by PlayerPersistence");
			return;
		}
	}

	// Llamar esto si cambias de target manualmente
	public void SetTarget(Transform newTarget)
	{
		target = newTarget;
	}

	#region Wall-Based Limits

	private void CacheCamera()
	{
		if (cam == null)
			cam = Camera.main;
	}

	/// <summary>
	/// Calculate frustum dimensions at the play area depth.
	/// Uses offset.z as the distance from camera to play area.
	/// </summary>
	private void CalculateFrustumDimensions()
	{
		CacheCamera();
		if (cam == null) return;

		// Only recalculate if aspect ratio changed
		if (Mathf.Approximately(cam.aspect, lastAspectRatio)) return;
		lastAspectRatio = cam.aspect;

		// Use the Z-offset as distance to play area (how far camera is behind player)
		float viewDistance = Mathf.Abs(offset.z);

		// Calculate frustum dimensions at that distance
		float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
		cachedFrustumHalfHeight = Mathf.Tan(fovRad / 2f) * viewDistance;
		cachedFrustumHalfWidth = cachedFrustumHalfHeight * cam.aspect;
	}

	/// <summary>
	/// Calculate clamped position so camera view doesn't extend past walls.
	/// If play area is smaller than camera view, centers camera between walls.
	/// </summary>
	private float CalculateWallClampedPosition(float desiredPos, Transform minWall, Transform maxWall, float frustumHalfSize, bool isXAxis)
	{
		float minLimit = float.MinValue;
		float maxLimit = float.MaxValue;

		if (minWall != null)
		{
			float wallPos = isXAxis ? minWall.position.x : minWall.position.z;
			minLimit = wallPos + frustumHalfSize + wallMargin;
		}

		if (maxWall != null)
		{
			float wallPos = isXAxis ? maxWall.position.x : maxWall.position.z;
			maxLimit = wallPos - frustumHalfSize - wallMargin;
		}

		// If play area is smaller than camera view (limits overlap), center the camera
		if (minWall != null && maxWall != null && minLimit > maxLimit)
		{
			float minWallPos = isXAxis ? minWall.position.x : minWall.position.z;
			float maxWallPos = isXAxis ? maxWall.position.x : maxWall.position.z;
			return (minWallPos + maxWallPos) / 2f;
		}

		return Mathf.Clamp(desiredPos, minLimit, maxLimit);
	}

	#endregion

	#region Editor Visualization

	private void OnDrawGizmosSelected()
	{
		if (!useWallLimits) return;

		CacheCamera();
		if (cam == null) return;

		// Calculate frustum dimensions using offset.z as view distance
		float viewDistance = Mathf.Abs(offset.z);
		float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
		float halfHeight = Mathf.Tan(fovRad / 2f) * viewDistance;
		float halfWidth = halfHeight * cam.aspect;

		// Draw frustum footprint at ground level (centered on camera X, at target Z)
		Gizmos.color = Color.yellow;
		Vector3 center = new Vector3(
			transform.position.x,
			groundPlaneY,
			transform.position.z + viewDistance);

		Gizmos.DrawWireCube(center, new Vector3(halfWidth * 2, 0.1f, halfHeight * 2));

		// Draw calculated limit lines
		Gizmos.color = Color.red;
		float lineLength = 100f;

		if (leftWall != null)
		{
			float limitX = leftWall.position.x + halfWidth + wallMargin;
			Gizmos.DrawLine(
				new Vector3(limitX, groundPlaneY, center.z - lineLength / 2),
				new Vector3(limitX, groundPlaneY, center.z + lineLength / 2));
		}

		if (rightWall != null)
		{
			float limitX = rightWall.position.x - halfWidth - wallMargin;
			Gizmos.DrawLine(
				new Vector3(limitX, groundPlaneY, center.z - lineLength / 2),
				new Vector3(limitX, groundPlaneY, center.z + lineLength / 2));
		}

		Gizmos.color = Color.blue;
		if (frontWall != null)
		{
			float limitZ = frontWall.position.z + halfHeight + wallMargin;
			Gizmos.DrawLine(
				new Vector3(center.x - lineLength / 2, groundPlaneY, limitZ),
				new Vector3(center.x + lineLength / 2, groundPlaneY, limitZ));
		}

		if (backWall != null)
		{
			float limitZ = backWall.position.z - halfHeight - wallMargin;
			Gizmos.DrawLine(
				new Vector3(center.x - lineLength / 2, groundPlaneY, limitZ),
				new Vector3(center.x + lineLength / 2, groundPlaneY, limitZ));
		}
	}

	#endregion
}
