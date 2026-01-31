using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPersistence : MonoBehaviour
{
	public static PlayerPersistence Instance { get; private set; }

	[Header("Spawn Settings")]
	public string spawnPointTag = "SpawnPoint"; // Tag para identificar puntos de spawn

	private void Awake()
	{
		// Patrón Singleton: solo puede haber un Player
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject); // El player NO se destruye al cambiar escena

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
		// Deshabilitar MovementScript temporalmente
		MovementScript movement = GetComponent<MovementScript>();
		if (movement != null)
		{
			movement.enabled = false;
		}

		yield return new WaitForFixedUpdate(); // Esperar al siguiente FixedUpdate
		yield return null; // Esperar 1 frame más

		RepositionToSpawnPoint();

		// Reactivar MovementScript
		if (movement != null)
		{
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

		// 2. FORZAR actualización del HUD con el estado actual del Player
		PlayerHealth playerHealth = GetComponent<PlayerHealth>();
		if (playerHealth != null && HUDView.Instance != null)
		{
			// Forzar actualización de corazones
			HUDView.Instance.ActualizarHearts(playerHealth.currentHealth);
			Debug.Log($"HUD synced: {playerHealth.currentHealth} lives");
		}

		// 3. Actualizar otros valores del HUD desde GameManager
		if (GameManager.Instance != null && HUDView.Instance != null)
		{
			HUDView.Instance.ActualizarMonedas(GameManager.Instance.Coins);
			HUDView.Instance.ActualizarCombo(GameManager.Instance.Combo);
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
