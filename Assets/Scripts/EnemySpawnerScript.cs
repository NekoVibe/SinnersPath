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

		// Registrarse en el EnemyManager
		EnemyManager.Instance?.RegisterSpawner(this);

		// Si no se activa por trigger, spawnear al inicio
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

		// Notificar al EnemyManager que este spawner terminó
		EnemyManager.Instance?.NotifySpawnerCompleted(this);
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
	// ============================================
	// FUNCIONES DE DEBUG
	// ============================================

	// Forzar spawn de enemigos (sin necesidad de trigger)
	[ContextMenu("Debug: Force Spawn")]
	private void DebugForceSpawn()
	{
		if (Application.isPlaying)
		{
			StartSpawning();
		}
		else
		{
			Debug.LogWarning("This only works in Play mode!");
		}
	}

	// Matar todos los enemigos spawneados
	[ContextMenu("Debug: Kill All Spawned Enemies")]
	private void DebugKillAllEnemies()
	{
		if (!Application.isPlaying)
		{
			Debug.LogWarning("This only works in Play mode!");
			return;
		}

		// Buscar todos los enemigos en la escena
		EnemyBase[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);

		int killedCount = 0;
		foreach (EnemyBase enemy in allEnemies)
		{
			if (enemy != null && !enemy.IsDead())
			{
				// Hacer daño masivo para matarlo
				enemy.TakeDamage(999);
				killedCount++;
			}
		}

		Debug.Log($"Killed {killedCount} enemies");
	}

	// Matar UN enemigo aleatorio (para testing de drops)
	[ContextMenu("Debug: Kill Random Enemy")]
	private void DebugKillRandomEnemy()
	{
		if (!Application.isPlaying)
		{
			Debug.LogWarning("This only works in Play mode!");
			return;
		}

		// Buscar todos los enemigos vivos
		EnemyBase[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
		List<EnemyBase> aliveEnemies = new List<EnemyBase>();

		foreach (EnemyBase enemy in allEnemies)
		{
			if (enemy != null && !enemy.IsDead())
			{
				aliveEnemies.Add(enemy);
			}
		}

		if (aliveEnemies.Count > 0)
		{
			// Elegir uno aleatorio y matarlo
			int randomIndex = Random.Range(0, aliveEnemies.Count);
			EnemyBase targetEnemy = aliveEnemies[randomIndex];
			targetEnemy.TakeDamage(999);

			Debug.Log($"Killed 1 enemy at position {targetEnemy.transform.position}");
		}
		else
		{
			Debug.Log("No enemies alive to kill");
		}
	}

	// Resetear el spawner para poder usarlo de nuevo
	[ContextMenu("Debug: Reset Spawner")]
	private void DebugResetSpawner()
	{
		if (!Application.isPlaying)
		{
			Debug.LogWarning("This only works in Play mode!");
			return;
		}

		hasSpawned = false;
		isSpawning = false;
		currentWaveIndex = 0;
		activeEnemies.Clear();

		// Reactivar el collider
		Collider col = GetComponent<Collider>();
		if (col != null)
		{
			col.enabled = true;
		}

		Debug.Log("Spawner reset! You can trigger it again.");
	}

	// Spawner UN enemigo instantáneamente (para testing rápido)
	[ContextMenu("Debug: Spawn Single Enemy")]
	private void DebugSpawnSingleEnemy()
	{
		if (!Application.isPlaying)
		{
			Debug.LogWarning("This only works in Play mode!");
			return;
		}

		if (waves.Length > 0 && waves[0].enemyPrefab != null)
		{
			SpawnEnemy(waves[0].enemyPrefab);
			Debug.Log("Spawned 1 enemy");
		}
		else
		{
			Debug.LogError("No enemy prefab configured in Wave 0!");
		}
	}

	// Mostrar info del spawner
	[ContextMenu("Debug: Show Info")]
	private void DebugShowInfo()
	{
		Debug.Log("=== SPAWNER INFO ===");
		Debug.Log($"Has Spawned: {hasSpawned}");
		Debug.Log($"Is Spawning: {isSpawning}");
		Debug.Log($"Current Wave: {currentWaveIndex + 1}/{waves.Length}");
		Debug.Log($"Active Enemies: {activeEnemies.Count}");
		Debug.Log($"Total Waves: {waves.Length}");

		int totalEnemies = 0;
		for (int i = 0; i < waves.Length; i++)
		{
			totalEnemies += waves[i].enemyCount;
			Debug.Log($"Wave {i + 1}: {waves[i].waveName} - {waves[i].enemyCount} enemies");
		}

		Debug.Log($"Total Enemies to Spawn: {totalEnemies}");
	}
}
