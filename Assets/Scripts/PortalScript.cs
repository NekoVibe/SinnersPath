using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelPortal : MonoBehaviour
{
	[Header("Level Settings")]
	[Tooltip("Nombre de la escena a cargar")]
	public string targetSceneName;

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
		if (string.IsNullOrEmpty(targetSceneName))
		{
			Debug.LogError("Target scene name is not set!");
			return;
		}

		Debug.Log($"Loading level: {targetSceneName}");
		SceneManager.LoadScene(targetSceneName);
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
