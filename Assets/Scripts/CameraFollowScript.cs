using UnityEngine;

public class CameraFollow : MonoBehaviour
{
	[Header("Target Settings")]
	[SerializeField] private Transform target; // El player
	[SerializeField] private bool findPlayerAutomatically = true;

	[Header("Follow Settings")]
	[SerializeField] private bool followX = true;
	[SerializeField] private bool followY = false;
	[SerializeField] private bool followZ = false;

	[Header("Smoothing")]
	[SerializeField] private bool useSmoothing = true;
	[SerializeField] private float smoothSpeed = 5f;

	[Header("Offset")]
	[SerializeField] private Vector3 offset = new Vector3(0f, 5f, -10f);

	[Header("Limits (Optional)")]
	[SerializeField] private bool useLimits = false;
	[SerializeField] private float minX = -100f;
	[SerializeField] private float maxX = 100f;

	[Header("Update Mode")]
	[SerializeField] private bool useFixedUpdate = false; // Cambiar a true si hay jitter

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
		// Calcular posición deseada
		Vector3 desiredPosition = transform.position;

		if (followX)
		{
			desiredPosition.x = target.position.x + offset.x;

			// Aplicar límites si están activados
			if (useLimits)
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
}
