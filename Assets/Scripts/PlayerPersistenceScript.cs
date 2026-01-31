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
		// Reposicionar el player en el punto de spawn de la nueva escena
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
			transform.position = spawnPoint.transform.position;
			transform.rotation = spawnPoint.transform.rotation;
			Debug.Log($"Player spawned at: {spawnPoint.name}");
		}
		else
		{
			Debug.LogWarning($"No spawn point found with tag '{spawnPointTag}' or name 'spawnpoint' in scene {scene.name}");
		}

		// Resetear física si es necesario
		Rigidbody rb = GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
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
