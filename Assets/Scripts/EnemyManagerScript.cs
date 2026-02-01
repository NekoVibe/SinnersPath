using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
	public static EnemyManager Instance { get; private set; }

	private List<EnemyBase> activeEnemies = new List<EnemyBase>();
	private HashSet<EnemySpawner> registeredSpawners = new HashSet<EnemySpawner>();
	private HashSet<EnemySpawner> completedSpawners = new HashSet<EnemySpawner>();

	[Header("Debug")]
	[SerializeField] private bool showDebugLogs = true;

	private void Awake()
	{
		// Singleton por nivel (no persistente)
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
	}

	private void OnDestroy()
	{
		// Clear static reference when destroyed (scene change)
		if (Instance == this)
		{
			Instance = null;
		}
	}

	// Registrar un spawner al inicio del nivel
	public void RegisterSpawner(EnemySpawner spawner)
	{
		registeredSpawners.Add(spawner);
		if (showDebugLogs)
			Debug.Log($"[EnemyManager] Spawner registered. Total spawners: {registeredSpawners.Count}");
	}

	// Notificar que un spawner ha terminado de spawner
	public void NotifySpawnerCompleted(EnemySpawner spawner)
	{
		completedSpawners.Add(spawner);
		if (showDebugLogs)
			Debug.Log($"[EnemyManager] Spawner completed. {completedSpawners.Count}/{registeredSpawners.Count}");
	}

	// Verificar si todos los spawners han terminado de spawner
	public bool AllSpawnersCompleted()
	{
		// If no spawners registered (pre-placed enemies only), consider it complete
		if (registeredSpawners.Count == 0)
			return true;

		return completedSpawners.Count >= registeredSpawners.Count;
	}

	// Registrar un enemigo cuando se crea
	public void RegisterEnemy(EnemyBase enemy)
	{
		if (!activeEnemies.Contains(enemy))
		{
			activeEnemies.Add(enemy);
			if (showDebugLogs)
				Debug.Log($"[EnemyManager] Enemy registered. Total: {activeEnemies.Count}");
		}
	}

	// Quitar un enemigo cuando muere
	public void UnregisterEnemy(EnemyBase enemy)
	{
		if (activeEnemies.Contains(enemy))
		{
			activeEnemies.Remove(enemy);
			if (showDebugLogs)
				Debug.Log($"[EnemyManager] Enemy unregistered. Remaining: {activeEnemies.Count}");
		}
	}

	// Obtener número de enemigos vivos
	public int GetAliveEnemyCount()
	{
		// Limpiar referencias nulas
		activeEnemies.RemoveAll(e => e == null || e.IsDead());
		return activeEnemies.Count;
	}

	// Verificar si todos los enemigos están muertos Y todos los spawners terminaron
	public bool AllEnemiesDeadAndSpawnersCompleted()
	{
		bool spawnersReady = AllSpawnersCompleted();
		bool enemiesDead = GetAliveEnemyCount() == 0;

		if (showDebugLogs)
		{
			Debug.Log($"[EnemyManager] Spawners completed: {spawnersReady} | Enemies alive: {GetAliveEnemyCount()}");
		}

		return spawnersReady && enemiesDead;
	}

	// Limpiar lista
	public void ClearAll()
	{
		activeEnemies.Clear();
		registeredSpawners.Clear();
		completedSpawners.Clear();
	}
}
