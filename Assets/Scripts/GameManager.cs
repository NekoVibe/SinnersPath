using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	[Header("Starting Values")]
	[SerializeField] private readonly int StartingCoins = 0;
	[SerializeField] private readonly int StartingCombo = 0;

	[Header("Scene Management")]
	[Tooltip("The starting/menu scene to load on restart or game over.")]
	[SerializeField] private SceneReference startScene;

	[Header("Audio")]
	[SerializeField] private string overworldMusicPath = "Music/Overworld";

	private int points;  // Puntuación final del juego
	private int coins;
	private int combo;

	// Eventos para notificar cambios
	public event System.Action<int> OnPointsChanged;
	public event System.Action<int> OnCoinsChanged;
	public event System.Action<int> OnComboChanged;

	// Properties públicas (solo lectura desde fuera)
	public int Points => points;
	public int Coins => coins;
	public int Combo => combo;

	void Awake()
	{
		// Singleton pattern
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);

		// Inicializar valores
		points = 0;
		coins = StartingCoins;
		combo = StartingCombo;

		// Suscribirse al evento de carga de escena
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnDestroy()
	{
		// Limpiar eventos
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	// Cada vez que se carga una escena, actualizar el HUD
	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		// Esperar un frame para que el HUD se inicialice
		StartCoroutine(UpdateHUDNextFrame());
	}

	private System.Collections.IEnumerator UpdateHUDNextFrame()
	{
		yield return null; // Esperar 1 frame
		UpdateHUD();
	}

	void Start()
	{
		// Sincronizar con el HUD al iniciar
		UpdateHUD();

		// Reproducir música de fondo (esperar a que AudioManager esté listo)
		StartCoroutine(PlayMusicWhenReady());
	}

	private System.Collections.IEnumerator PlayMusicWhenReady()
	{
		// Esperar hasta que AudioManager esté disponible
		while (AudioManager.Instance == null)
		{
			yield return null;
		}

		// Cargar música desde Resources
		AudioClip clip = Resources.Load<AudioClip>(overworldMusicPath);

		if (clip != null)
		{
			AudioManager.Instance.PlayMusic(clip);
			Debug.Log($"Playing music: {overworldMusicPath}");
		}
		else
		{
			Debug.LogWarning($"Music not found at Resources/{overworldMusicPath}. Make sure the file is in Assets/Resources/Music/Overworld.mp3");
		}
	}

	// Actualizar todo el HUD de una vez
	private void UpdateHUD()
	{
		if (HUDManager.Instance != null)
		{
			HUDManager.Instance.ActualizarMonedas(coins);
			HUDManager.Instance.ActualizarCombo(combo);
			Debug.Log("HUD updated successfully");
		}
		else
		{
			Debug.LogWarning("HUD not found! Make sure HUD_Canvas exists in the scene.");
		}
	}

	#region Coins Management

	public void AddCoins(int amount)
	{
		if (amount < 0) return;

		coins += amount;
		OnCoinsChanged?.Invoke(coins);
		HUDManager.Instance?.ActualizarMonedas(coins);

		Debug.Log($"Coins: {coins} (+{amount})");
	}

	public bool SpendCoins(int amount)
	{
		if (amount < 0 || coins < amount)
		{
			Debug.Log($"Not enough coins! Need {amount}, have {coins}");
			return false;
		}

		coins -= amount;
		OnCoinsChanged?.Invoke(coins);
		HUDManager.Instance?.ActualizarMonedas(coins);

		Debug.Log($"Coins: {coins} (-{amount})");
		return true;
	}

	#endregion

	#region Game Flow Management

	// Reiniciar la partida completamente
	// Reiniciar la partida completamente
	public void RestartGame()
	{
		Debug.Log("Restarting game...");

		// 1. Resetear el Player PRIMERO
		ResetPlayer();

		// 2. Resetear todos los valores del GameManager
		points = 0;
		coins = StartingCoins;
		combo = StartingCombo;

		// 3. Resetear el TIMER
		if (TimerManager.Instance != null)
		{
			TimerManager.Instance.ResetTimer();
			TimerManager.Instance.StartTimer();
		}

		// 4. Actualizar HUD
		if (HUDManager.Instance != null)
		{
			HUDManager.Instance.ActualizarMonedas(coins);
			HUDManager.Instance.ActualizarCombo(combo);
		}

		// 5. Notificar cambios
		OnPointsChanged?.Invoke(points);
		OnCoinsChanged?.Invoke(coins);
		OnComboChanged?.Invoke(combo);

		// 6. Cargar la escena inicial
		LoadStartScene();
	}

	private void ResetPlayer()
	{
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		if (player == null) return;

		// Resetear vida
		PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
		if (playerHealth != null)
		{
			playerHealth.ResetHealth();
			Debug.Log($"Player lives reset to {playerHealth.GetCurrentHealth()}");
		}

		// Limpiar inventario de armas
		WeaponInventory weaponInventory = player.GetComponent<WeaponInventory>();
		if (weaponInventory != null)
		{
			weaponInventory.ClearInventory();
			Debug.Log("Player inventory cleared");
		}

		// Resetear física del player
		Rigidbody rb = player.GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}
	}

	public void Victory()
	{
		Debug.Log($"Victory! Final Score: {points}");

		// Detener el cronómetro
		if (TimerManager.Instance != null)
		{
			TimerManager.Instance.StopTimer();
		}

		// Guardar la run en el leaderboard
		SaveCurrentRun();

		// Aquí mostrarías la pantalla de victoria con la puntuación
		// VictoryScreen.Instance.Show(points);
	}

	// Game Over - Muerte del jugador
	public void GameOver()
	{
		Debug.Log("Game Over!");

		// Detener el cronómetro
		if (TimerManager.Instance != null)
		{
			TimerManager.Instance.StopTimer();
		}

		// Guardar la run incluso si pierdes (opcional)
		SaveCurrentRun();

		// Reiniciar después de 3 segundos
		Invoke(nameof(RestartGame), 3f);
	}

	// Guardar la run actual
	private void SaveCurrentRun()
	{
		if (SaveManager.Instance == null)
		{
			Debug.LogError("SaveManager not found!");
			return;
		}

		// Obtener el tiempo del TimerManager
		string timeElapsed = "00:00";
		if (TimerManager.Instance != null)
		{
			float totalSeconds = TimerManager.Instance.GetCurrentTime();
			int minutes = Mathf.FloorToInt(totalSeconds / 60);
			int seconds = Mathf.FloorToInt(totalSeconds % 60);
			timeElapsed = string.Format("{0:00}:{1:00}", minutes, seconds);
		}

		// Crear la run data
		RunData newRun = new RunData
		{
			score = points,
			timeElapsed = timeElapsed,
			date = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm")
		};

		// Guardar en el leaderboard
		SaveManager.Instance.SaveRun(newRun);

		Debug.Log($"Run saved! Score: {points}, Time: {timeElapsed}");
	}

	// Volver al menú principal (primera escena)
	public void LoadMainMenu()
	{
		// Resetear valores
		points = 0;
		coins = StartingCoins;
		combo = StartingCombo;

		// Destruir el player si existe
		if (PlayerPersistence.Instance != null)
		{
			Destroy(PlayerPersistence.Instance.gameObject);
		}

		LoadStartScene();
	}

	/// <summary>
	/// Load the starting/menu scene.
	/// </summary>
	public void LoadStartScene()
	{
		if (startScene == null || !startScene.IsValid)
		{
			Debug.LogError("Start scene is not assigned in GameManager!");
			return;
		}

		SceneManager.LoadScene(startScene.SceneName);
	}

	#endregion

	#region Combo Management

	public void AddCombo(int amount = 1)
	{
		if (amount < 0) return;

		combo += amount;
		OnComboChanged?.Invoke(combo);
		HUDManager.Instance?.ActualizarCombo(combo);

		Debug.Log($"Combo: x{combo}");
	}

	public void ResetCombo()
	{
		combo = 0;
		OnComboChanged?.Invoke(combo);
		HUDManager.Instance?.ActualizarCombo(combo);

		Debug.Log("Combo reset!");
	}

	public void SetCombo(int value)
	{
		combo = Mathf.Max(0, value);
		OnComboChanged?.Invoke(combo);
		HUDManager.Instance?.ActualizarCombo(combo);
	}

	#endregion

	#region Points Management

	public void AddPoints(int amount)
	{
		if (amount < 0) return;

		points += amount;
		OnPointsChanged?.Invoke(points);

		Debug.Log($"Points: {points} (+{amount})");
	}

	public void SetPoints(int value)
	{
		points = Mathf.Max(0, value);
		OnPointsChanged?.Invoke(points);
	}

	// Obtener puntos finales (útil para pantalla de victoria)
	public int GetFinalScore()
	{
		return points;
	}

	// Resetear puntos (útil si quieres reiniciar el juego)
	public void ResetPoints()
	{
		points = 0;
		OnPointsChanged?.Invoke(points);
	}

	#endregion

	#region Debug Tools

	[ContextMenu("Test: Add 10 Coins")]
	private void TestAddCoins()
	{
		AddCoins(10);
	}

	[ContextMenu("Test: Spend 5 Coins")]
	private void TestSpendCoins()
	{
		SpendCoins(5);
	}

	[ContextMenu("Test: Add Combo")]
	private void TestAddCombo()
	{
		AddCombo(1);
	}

	[ContextMenu("Test: Reset Combo")]
	private void TestResetCombo()
	{
		ResetCombo();
	}

	[ContextMenu("Test: Add 100 Points")]
	private void TestAddPoints()
	{
		AddPoints(100);
	}

	[ContextMenu("Test: Show Final Score")]
	private void TestShowScore()
	{
		Debug.Log($"Final Score: {GetFinalScore()} points");
	}

	[ContextMenu("Test: Game Over")]
	private void TestGameOver()
	{
		GameOver();
	}

	[ContextMenu("Test: Victory")]
	private void TestVictory()
	{
		Victory();
	}

	[ContextMenu("Test: Restart Game")]
	private void TestRestartGame()
	{
		RestartGame();
	}

	#endregion
}
