using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
	[System.Serializable]
	public class Wave
	{
		public string waveName = "Wave 1";
		public GameObject enemyPrefab;
		public int enemyCount = 5;
		public float spawnDelay = 0.5f; // Tiempo entre cada enemigo
	}

	[Header("Spawn Settings")]
	[SerializeField] private Wave[] waves;
	[SerializeField] private Transform[] spawnPoints; // Puntos donde aparecen los enemigos
	[SerializeField] private float timeBetweenWaves = 3f;

	[Header("Trigger Settings")]
	[SerializeField] private bool activateOnTrigger = true;
	[SerializeField] private bool deactivateAfterSpawn = true;
	[SerializeField] private string playerTag = "Player";

	[Header("Spawn Behavior")]
	[SerializeField] private bool spawnAllAtOnce = false; // Spawn todos a la vez o con delay
	[SerializeField] private bool randomizeSpawnPoints = true;
	[SerializeField] private bool waitForWaveComplete = false; // Esperar a que mueran todos antes de la siguiente oleada

	[Header("Debug")]
	[SerializeField] private bool showGizmos = true;
	[SerializeField] private Color gizmoColor = Color.red;

	// Estado
	private bool hasSpawned = false;
	private bool isSpawning = false;
	private List<GameObject> activeEnemies = new List<GameObject>();
	private int currentWaveIndex = 0;

	private void Start()
	{
		// Si no hay spawn points definidos, usar la posición del spawner
		if (spawnPoints == null || spawnPoints.Length == 0)
		{
			spawnPoints = new Transform[] { transform };
		}

		// Si no se activa por trigger, spawnar al inicio
		if (!activateOnTrigger)
		{
			StartSpawning();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		// Activar cuando el player entre en el trigger
		if (activateOnTrigger && !hasSpawned && other.CompareTag(playerTag))
		{
			Debug.Log($"Player entered spawn trigger: {gameObject.name}");
			StartSpawning();

			// Desactivar trigger después de usarlo
			if (deactivateAfterSpawn)
			{
				Collider col = GetComponent<Collider>();
				if (col != null)
				{
					col.enabled = false;
				}
			}
		}
	}

	// Iniciar el proceso de spawn
	public void StartSpawning()
	{
		if (isSpawning || hasSpawned) return;

		hasSpawned = true;
		StartCoroutine(SpawnWaves());
	}

	// Spawnar todas las oleadas
	private IEnumerator SpawnWaves()
	{
		isSpawning = true;

		for (int i = 0; i < waves.Length; i++)
		{
			currentWaveIndex = i;
			Wave wave = waves[i];

			Debug.Log($"Spawning {wave.waveName}: {wave.enemyCount} enemies");

			if (spawnAllAtOnce)
			{
				// Spawnar todos los enemigos a la vez
				SpawnWaveInstantly(wave);
			}
			else
			{
				// Spawnar con delay entre enemigos
				yield return StartCoroutine(SpawnWaveWithDelay(wave));
			}

			// Esperar a que todos los enemigos de la oleada mueran
			if (waitForWaveComplete)
			{
				yield return StartCoroutine(WaitForWaveComplete());
			}

			// Esperar entre oleadas (si no es la última)
			if (i < waves.Length - 1)
			{
				Debug.Log($"Waiting {timeBetweenWaves}s before next wave...");
				yield return new WaitForSeconds(timeBetweenWaves);
			}
		}

		Debug.Log("All waves completed!");
		isSpawning = false;
	}

	// Spawnar una oleada instantáneamente
	private void SpawnWaveInstantly(Wave wave)
	{
		for (int i = 0; i < wave.enemyCount; i++)
		{
			SpawnEnemy(wave.enemyPrefab);
		}
	}

	// Spawnar una oleada con delay
	private IEnumerator SpawnWaveWithDelay(Wave wave)
	{
		for (int i = 0; i < wave.enemyCount; i++)
		{
			SpawnEnemy(wave.enemyPrefab);

			if (i < wave.enemyCount - 1) // No esperar después del último
			{
				yield return new WaitForSeconds(wave.spawnDelay);
			}
		}
	}

	// Spawnar un enemigo individual
	private void SpawnEnemy(GameObject enemyPrefab)
	{
		if (enemyPrefab == null)
		{
			Debug.LogError("Enemy prefab is null!");
			return;
		}

		// Elegir punto de spawn
		Transform spawnPoint = GetSpawnPoint();

		// Instanciar enemigo
		GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

		// Añadir a la lista de enemigos activos
		activeEnemies.Add(enemy);

		Debug.Log($"Enemy spawned at {spawnPoint.position}");
	}

	// Obtener punto de spawn (aleatorio o secuencial)
	private Transform GetSpawnPoint()
	{
		if (randomizeSpawnPoints)
		{
			return spawnPoints[Random.Range(0, spawnPoints.Length)];
		}
		else
		{
			// Rotar entre los spawn points
			int index = (activeEnemies.Count % spawnPoints.Length);
			return spawnPoints[index];
		}
	}

	// Esperar a que todos los enemigos de la oleada mueran
	private IEnumerator WaitForWaveComplete()
	{
		Debug.Log("Waiting for wave to complete...");

		// Limpiar referencias nulas
		activeEnemies.RemoveAll(enemy => enemy == null);

		// Esperar hasta que no queden enemigos
		while (activeEnemies.Count > 0)
		{
			activeEnemies.RemoveAll(enemy => enemy == null);
			yield return new WaitForSeconds(0.5f);
		}

		Debug.Log("Wave completed!");
	}

	// Forzar spawn desde otros scripts
	public void ForceSpawn()
	{
		if (!hasSpawned)
		{
			StartSpawning();
		}
	}

	// Reset para poder volver a spawnar
	public void ResetSpawner()
	{
		hasSpawned = false;
		isSpawning = false;
		currentWaveIndex = 0;
		activeEnemies.Clear();

		// Reactivar collider si existe
		Collider col = GetComponent<Collider>();
		if (col != null)
		{
			col.enabled = true;
		}
	}

	// Dibujar gizmos en el editor
	private void OnDrawGizmos()
	{
		if (!showGizmos) return;

		// Dibujar el área del trigger
		Collider col = GetComponent<Collider>();
		if (col != null)
		{
			Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);

			if (col is BoxCollider box)
			{
				Gizmos.matrix = transform.localToWorldMatrix;
				Gizmos.DrawCube(box.center, box.size);
			}
			else if (col is SphereCollider sphere)
			{
				Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
			}
		}

		// Dibujar spawn points
		if (spawnPoints != null)
		{
			Gizmos.color = Color.green;
			foreach (Transform point in spawnPoints)
			{
				if (point != null)
				{
					Gizmos.DrawWireSphere(point.position, 0.5f);
					Gizmos.DrawLine(point.position, point.position + Vector3.up * 2f);
				}
			}
		}
	}
}
