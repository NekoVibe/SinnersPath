using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPersistence : MonoBehaviour
{
	public static PlayerPersistence Instance { get; private set; }

	[Header("Spawn Settings")]
	public string spawnPointTag = "SpawnPoint"; // Tag para identificar puntos de spawn

	[Header("Mask Prefabs")]
	[SerializeField] private GameObject redMaskPrefab;
	[SerializeField] private GameObject blueMaskPrefab;
	[SerializeField] private GameObject yellowMaskPrefab;

	private void Awake()
	{
		// Patrón Singleton: solo puede haber un Player
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);

		// Suscribirse al evento de carga de escena
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void Start()
	{
		// Posicionar en el spawn point al iniciar el juego (esperar un frame)
		StartCoroutine(RepositionOnStart());
	}

	private System.Collections.IEnumerator RepositionOnStart()
	{
		// Deshabilitar PlayerMovement temporalmente
		PlayerMovement movement = GetComponent<PlayerMovement>();
		if (movement != null)
		{
			movement.enabled = false;
		}

		yield return new WaitForFixedUpdate();
		yield return null;

		RepositionToSpawnPoint();

		// Reactivar PlayerMovement y reset velocity
		if (movement != null)
		{
			movement.ResetVelocity();
			movement.enabled = true;
		}
	}

	private void OnDestroy()
	{
		// Limpiar el evento cuando se destruya
		if (Instance == this)
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}
	}

	// Se llama cada vez que se carga una escena nueva
	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		Debug.Log($"Scene loaded: {scene.name}");

		// 1. Reposicionar PRIMERO
		RepositionToSpawnPoint();

		// Reset movement velocity after reposition
		PlayerMovement movement = GetComponent<PlayerMovement>();
		movement?.ResetVelocity();

		// 2. Reset player state for new level (not tutorial)
		// Skip reset for Story scene (tutorial)
		if (scene.name != "Story")
		{
			// Re-enable dash
			movement?.EnableDash();

			// Re-enable combat
			PlayerMeleeAttack meleeAttack = GetComponent<PlayerMeleeAttack>();
			meleeAttack?.EnableCombat();

			// Disable god mode
			PlayerHealth health = GetComponent<PlayerHealth>();
			health?.DisableGodMode();

			// Re-activate UI canvases that were disabled during tutorial
			ActivateTutorialHiddenUI();

			// Restore unlocked masks from PlayerPrefs
			RestoreUnlockedMasks();

			Debug.Log("[PlayerPersistence] Player state reset for new level");
		}

		// 2. FORZAR actualización del HUD con el estado actual del Player
		PlayerHealth playerHealth = GetComponent<PlayerHealth>();
		if (playerHealth != null && HUDManager.Instance != null)
		{
			// Forzar actualización de corazones
			HUDManager.Instance.ActualizarHearts(playerHealth.currentHealth);
			Debug.Log($"HUD synced: {playerHealth.currentHealth} lives");
		}

		// 3. Actualizar otros valores del HUD desde GameManager
		if (GameManager.Instance != null && HUDManager.Instance != null)
		{
			HUDManager.Instance.ActualizarMonedas(GameManager.Instance.Coins);
			HUDManager.Instance.ActualizarCombo(GameManager.Instance.Combo);
		}
	}

	// Reposicionar el player en el punto de spawn
	private void RepositionToSpawnPoint()
	{
		GameObject spawnPoint = null;

		try
		{
			spawnPoint = GameObject.FindGameObjectWithTag(spawnPointTag);
		}
		catch
		{
			// Si el tag no existe, buscar por nombre
			spawnPoint = GameObject.Find("spawnpoint");
		}

		if (spawnPoint != null)
		{
			// Resetear física ANTES de mover
			Rigidbody rb = GetComponent<Rigidbody>();
			if (rb != null)
			{
				// ✅ Solución al problema del Interpolate
				// Temporalmente desactivar interpolación para el teleport
				RigidbodyInterpolation originalInterpolation = rb.interpolation;
				rb.interpolation = RigidbodyInterpolation.None;

				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;

				// Usar Rigidbody.position para mover objetos con física
				rb.position = spawnPoint.transform.position;
				rb.rotation = spawnPoint.transform.rotation;

				// Forzar al Rigidbody a sincronizarse inmediatamente
				Physics.SyncTransforms();

				// Reactivar la interpolación original
				rb.interpolation = originalInterpolation;
			}
			else
			{
				// Si no hay Rigidbody, usar transform
				transform.position = spawnPoint.transform.position;
				transform.rotation = spawnPoint.transform.rotation;
			}

			Debug.Log($"Player repositioned to: {spawnPoint.transform.position}");
		}
		else
		{
			Debug.LogWarning($"No spawn point found with tag '{spawnPointTag}' or name 'spawnpoint'");
		}
	}

	// Re-activate UI canvases that were hidden during tutorial
	private void ActivateTutorialHiddenUI()
	{
		// Find the UI parent object (may be named "UI" in hierarchy)
		// Then activate HUDCanvas and PauseMenuCanvas children
		GameObject uiParent = GameObject.Find("UI");
		if (uiParent != null)
		{
			ActivateChildByName(uiParent.transform, "HUDCanvas");
			ActivateChildByName(uiParent.transform, "PauseMenuCanvas");
			return;
		}

		// Fallback: try HUDManager parent
		if (HUDManager.Instance != null)
		{
			Transform hudTransform = HUDManager.Instance.transform;
			ActivateChildByName(hudTransform, "HUDCanvas");
			ActivateChildByName(hudTransform, "PauseMenuCanvas");
		}
	}

	private void ActivateChildByName(Transform parent, string childName)
	{
		// First try direct child
		Transform child = parent.Find(childName);
		if (child != null)
		{
			child.gameObject.SetActive(true);
			Debug.Log($"[PlayerPersistence] {childName} activated");
			return;
		}

		// Search all children recursively (including inactive)
		foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
		{
			if (t.name == childName)
			{
				t.gameObject.SetActive(true);
				Debug.Log($"[PlayerPersistence] {childName} activated (found nested)");
				return;
			}
		}
	}

	// Restore masks that were unlocked (saved in PlayerPrefs)
	private void RestoreUnlockedMasks()
	{
		WeaponInventory inventory = GetComponent<WeaponInventory>();
		if (inventory == null)
		{
			Debug.LogWarning("[PlayerPersistence] No WeaponInventory found!");
			return;
		}

		Debug.Log($"[PlayerPersistence] RestoreUnlockedMasks - Red:{PlayerPrefs.GetInt("Mask_Red", 0)} Blue:{PlayerPrefs.GetInt("Mask_Blue", 0)} Yellow:{PlayerPrefs.GetInt("Mask_Yellow", 0)}");

		// Check each mask type in PlayerPrefs
		// MaskUnlockManager saves: PlayerPrefs.SetInt("Mask_" + maskType, 1)

		if (PlayerPrefs.GetInt("Mask_Red", 0) == 1)
		{
			if (redMaskPrefab != null)
			{
				inventory.AddWeapon(redMaskPrefab);
				Debug.Log("[PlayerPersistence] Red Mask restored from PlayerPrefs");
			}
			else
			{
				Debug.LogWarning("[PlayerPersistence] Red Mask unlocked but prefab not assigned!");
			}
		}

		if (PlayerPrefs.GetInt("Mask_Blue", 0) == 1)
		{
			if (blueMaskPrefab != null)
			{
				inventory.AddWeapon(blueMaskPrefab);
				Debug.Log("[PlayerPersistence] Blue Mask restored from PlayerPrefs");
			}
			else
			{
				Debug.LogWarning("[PlayerPersistence] Blue Mask unlocked but prefab not assigned!");
			}
		}

		if (PlayerPrefs.GetInt("Mask_Yellow", 0) == 1)
		{
			if (yellowMaskPrefab != null)
			{
				inventory.AddWeapon(yellowMaskPrefab);
				Debug.Log("[PlayerPersistence] Yellow Mask restored from PlayerPrefs");
			}
			else
			{
				Debug.LogWarning("[PlayerPersistence] Yellow Mask unlocked but prefab not assigned!");
			}
		}
	}

	// Método para cambiar de nivel (llama desde triggers, puertas, etc.)
	public void LoadNextLevel(string sceneName)
	{
		SceneManager.LoadScene(sceneName);
	}

	public void LoadNextLevel(int sceneIndex)
	{
		SceneManager.LoadScene(sceneIndex);
	}
}
