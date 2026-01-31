using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelExit : MonoBehaviour
{
	[Header("Level Settings")]
	[SerializeField] private string nextLevelName;

	[Header("Activation Conditions")]
	[SerializeField] private bool requireAllEnemiesDead = true;
	[SerializeField] private bool requireKeyPress = false;
	[SerializeField] private KeyCode interactionKey = KeyCode.E;

	[Header("Visual Feedback")]
	[SerializeField] private Color lockedColor = Color.red;
	[SerializeField] private Color unlockedColor = Color.green;
	[SerializeField] private SpriteRenderer spriteRenderer;
	[SerializeField] private GameObject visualEffect; // Efecto visual cuando se desbloquea (opcional)

	[Header("UI Feedback")]
	[SerializeField] private string lockedMessage = "Eliminate all enemies first!";
	[SerializeField] private string unlockedMessage = "Press E to exit";
	[SerializeField] private float messageDuration = 2f;

	// Estado
	private bool isUnlocked = false;
	private bool playerInRange = false;
	private Color originalColor;

	private void Start()
	{
		// Guardar color original
		if (spriteRenderer != null)
		{
			originalColor = spriteRenderer.color;
		}

		// Asegurar que el collider es trigger
		Collider col = GetComponent<Collider>();
		if (col != null)
		{
			col.isTrigger = true;
		}

		// Comprobar condiciones al inicio
		CheckUnlockConditions();

		// Desactivar efecto visual hasta que se desbloquee
		if (visualEffect != null)
		{
			visualEffect.SetActive(false);
		}
	}

	[Header("Update Settings")]
	[SerializeField] private float checkInterval = 0.5f; // Verificar cada X segundos
	private float lastCheckTime = 0f;

	private void Update()
	{
		// Actualizar condiciones periódicamente
		if (!isUnlocked && requireAllEnemiesDead)
		{
			if (Time.time >= lastCheckTime + checkInterval)
			{
				lastCheckTime = Time.time;
				CheckUnlockConditions();
			}
		}

		// Si el player está en rango y se requiere tecla
		if (playerInRange && isUnlocked && requireKeyPress)
		{
			if (Input.GetKeyDown(interactionKey))
			{
				LoadNextLevel();
			}
		}
	}

	// Comprobar si se cumplen las condiciones para desbloquear
	private void CheckUnlockConditions()
	{
		bool canUnlock = true;

		// Verificar si quedan enemigos vivos
		if (requireAllEnemiesDead)
		{
			// Usar EnemyManager para verificar
			if (EnemyManager.Instance != null)
			{
				// NUEVA LÓGICA: Verificar que los spawners terminaron Y no haya enemigos
				canUnlock = EnemyManager.Instance.AllEnemiesDeadAndSpawnersCompleted();

				if (!canUnlock)
				{
					int aliveEnemies = EnemyManager.Instance.GetAliveEnemyCount();
					bool spawnersCompleted = EnemyManager.Instance.AllSpawnersCompleted();

					Debug.Log($"[LevelExit] Spawners completed: {spawnersCompleted} | Alive enemies: {aliveEnemies}");
				}
			}
			else
			{
				Debug.LogWarning("[LevelExit] No EnemyManager found! Add one to the level.");
				canUnlock = false;
			}

			if (canUnlock && !isUnlocked)
			{
				Debug.Log($"[LevelExit] All spawners completed and enemies defeated! Exit unlocked.");
			}
		}

		// Desbloquear si se cumplen las condiciones
		if (canUnlock && !isUnlocked)
		{
			UnlockExit();
		}
	}

	// Desbloquear la salida
	private void UnlockExit()
	{
		isUnlocked = true;

		// Cambiar color visual
		if (spriteRenderer != null)
		{
			spriteRenderer.color = unlockedColor;
		}

		// Activar efecto visual
		if (visualEffect != null)
		{
			visualEffect.SetActive(true);
		}

		Debug.Log("Level exit unlocked!");
	}

	private void OnTriggerEnter(Collider other)
	{
		// Detectar cuando el player entra
		if (other.CompareTag("Player"))
		{
			playerInRange = true;

			if (!isUnlocked)
			{
				// Mostrar mensaje de bloqueado
				Debug.Log(lockedMessage);
				ShowMessage(lockedMessage);
			}
			else if (requireKeyPress)
			{
				// Mostrar mensaje de interacción
				Debug.Log(unlockedMessage);
				ShowMessage(unlockedMessage);
			}
			else
			{
				// Cargar nivel automáticamente si no requiere tecla
				LoadNextLevel();
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		// Detectar cuando el player sale
		if (other.CompareTag("Player"))
		{
			playerInRange = false;
			HideMessage();
		}
	}

	// Cargar el siguiente nivel
	private void LoadNextLevel()
	{
		if (!isUnlocked)
		{
			Debug.Log("Exit is locked!");
			ShowMessage(lockedMessage);
			return;
		}

		Debug.Log($"Loading next level: {nextLevelName}");

		// Cargar por nombre o por índice
		if (!string.IsNullOrEmpty(nextLevelName))
		{
			SceneManager.LoadScene(nextLevelName);
		}
		else
		{
			Debug.LogError("No next level specified!");
		}
	}

	// Mostrar mensaje en pantalla (puedes conectar con tu sistema de UI)
	private void ShowMessage(string message)
	{
		// TODO: Conectar con sistema de UI
		// Por ahora solo log
		Debug.Log(message);
	}

	private void HideMessage()
	{
		// TODO: Ocultar mensaje de UI
	}

	// Forzar desbloqueo desde otro script (útil para debugging)
	public void ForceUnlock()
	{
		UnlockExit();
	}

	// Forzar bloqueo
	public void ForceLock()
	{
		isUnlocked = false;

		if (spriteRenderer != null)
		{
			spriteRenderer.color = lockedColor;
		}

		if (visualEffect != null)
		{
			visualEffect.SetActive(false);
		}
	}

	// Verificar si está desbloqueado
	public bool IsUnlocked()
	{
		return isUnlocked;
	}

	// Dibujar gizmos
	private void OnDrawGizmos()
	{
		Gizmos.color = isUnlocked ? Color.green : Color.red;

		Collider col = GetComponent<Collider>();
		if (col != null)
		{
			Gizmos.DrawWireCube(transform.position, col.bounds.size);
		}
		else
		{
			Gizmos.DrawWireCube(transform.position, Vector3.one);
		}

		// Dibujar flecha indicando la salida
		Gizmos.color = Color.yellow;
		Vector3 arrowStart = transform.position + Vector3.up * 2f;
		Vector3 arrowEnd = arrowStart + transform.forward * 2f;
		Gizmos.DrawLine(arrowStart, arrowEnd);
		Gizmos.DrawLine(arrowEnd, arrowEnd + Quaternion.Euler(0, 45, 0) * -transform.forward * 0.5f);
		Gizmos.DrawLine(arrowEnd, arrowEnd + Quaternion.Euler(0, -45, 0) * -transform.forward * 0.5f);
	}
}
