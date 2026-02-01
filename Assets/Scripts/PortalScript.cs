using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelPortal : MonoBehaviour
{
	[Header("Level Settings")]
	[Tooltip("Drag a scene here to set the target level")]
	[SerializeField] private SceneReference targetScene;
	[SerializeField] private string targetSceneName; // Alternative: set by name at runtime

	[Header("Activation Settings")]
	public bool requireInput = true; // Si necesita presionar tecla o es automático
	public KeyCode activationKey = KeyCode.E;

	[Header("Visual Feedback")]
	public string interactMessage = "Press E to enter";

	private bool playerInRange = false;

	private void Start()
	{
		// Asegurar que tiene un Collider con isTrigger
		Collider col = GetComponent<Collider>();
		if (col == null)
		{
			col = gameObject.AddComponent<BoxCollider>();
		}
		col.isTrigger = true;
	}

	private void Update()
	{
		if (playerInRange && requireInput && Input.GetKeyDown(activationKey))
		{
			LoadLevel();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		// Verificar que es el player
		if (other.CompareTag("Player") || other.GetComponent<PlayerPersistence>() != null)
		{
			playerInRange = true;

			if (requireInput)
			{
				Debug.Log(interactMessage);
			}
			else
			{
				// Si no requiere input, cargar automáticamente
				LoadLevel();
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player") || other.GetComponent<PlayerPersistence>() != null)
		{
			playerInRange = false;
		}
	}

	private void LoadLevel()
	{
		// Get scene name from SceneReference or fallback to runtime-set name
		string sceneName = targetScene.IsValid ? targetScene.SceneName : targetSceneName;

		if (string.IsNullOrEmpty(sceneName))
		{
			Debug.LogError("Target scene is not set! Drag a scene to the 'Target Scene' field or call SetTargetScene().");
			return;
		}

		Debug.Log($"Loading level: {sceneName}");

		// Use SceneFader for smooth transition if available
		if (SceneFader.Instance != null)
		{
			SceneFader.Instance.FadeToScene(sceneName, 0.5f);
		}
		else
		{
			SceneManager.LoadScene(sceneName);
		}
	}

	/// <summary>
	/// Set target scene at runtime (used by MaskUnlockManager)
	/// </summary>
	public void SetTargetScene(string sceneName)
	{
		targetSceneName = sceneName;
	}

	// Opcional: Dibujar área del portal en el editor
	private void OnDrawGizmos()
	{
		Gizmos.color = Color.cyan;
		Collider col = GetComponent<Collider>();
		if (col != null)
		{
			Gizmos.DrawWireCube(transform.position, col.bounds.size);
		}
	}
}
