using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	[Header("Starting Values")]
	[SerializeField] private int startingCoins = 0;
	[SerializeField] private int startingCombo = 0;

	[Header("Scene Management")]
	[Tooltip("The starting/menu scene to load on restart or game over.")]
	[SerializeField] private SceneReference startScene;

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
		coins = startingCoins;
		combo = startingCombo;

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
	}

	// Actualizar todo el HUD de una vez
	private void UpdateHUD()
	{
		if (HUDView.Instance != null)
		{
			HUDView.Instance.ActualizarMonedas(coins);
			HUDView.Instance.ActualizarCombo(combo);
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
		HUDView.Instance?.ActualizarMonedas(coins);

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
		HUDView.Instance?.ActualizarMonedas(coins);

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
		coins = startingCoins;
		combo = startingCombo;

		// 3. Resetear el TIMER
		if (TimerManager.Instance != null)
		{
			TimerManager.Instance.ResetTimer();
			TimerManager.Instance.StartTimer();
		}

		// 4. Actualizar HUD
		if (HUDView.Instance != null)
		{
			HUDView.Instance.ActualizarMonedas(coins);
			HUDView.Instance.ActualizarCombo(combo);
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

	// Game Over - Muerte del jugador
	public void GameOver()
	{
		Debug.Log("Game Over!");

		// Detener el cronómetro
		if (TimerManager.Instance != null)
		{
			TimerManager.Instance.StopTimer();
		}

		// Reiniciar después de 3 segundos
		Invoke(nameof(RestartGame), 3f);
	}

	// Victoria - Completar el juego
	public void Victory()
	{
		Debug.Log($"Victory! Final Score: {points} points");

		// Detener el cronómetro
		TimerManager timerManager = FindFirstObjectByType<TimerManager>();
		timerManager?.setCronometro(false);

		// Aquí puedes mostrar pantalla de victoria con puntuación final
		// Por ejemplo: VictoryUI.Instance.Show(points);
	}

	// Volver al menú principal (primera escena)
	public void LoadMainMenu()
	{
		// Resetear valores
		points = 0;
		coins = startingCoins;
		combo = startingCombo;

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
		HUDView.Instance?.ActualizarCombo(combo);

		Debug.Log($"Combo: x{combo}");
	}

	public void ResetCombo()
	{
		combo = 0;
		OnComboChanged?.Invoke(combo);
		HUDView.Instance?.ActualizarCombo(combo);

		Debug.Log("Combo reset!");
	}

	public void SetCombo(int value)
	{
		combo = Mathf.Max(0, value);
		OnComboChanged?.Invoke(combo);
		HUDView.Instance?.ActualizarCombo(combo);
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
