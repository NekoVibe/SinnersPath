using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	[Header("Starting Values")]
	[SerializeField] private int startingCoins = 0;
	[SerializeField] private int startingCombo = 0;

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
	}

	void Start()
	{
		// Sincronizar con el HUD al iniciar
		UpdateHUD();
	}

	// Actualizar todo el HUD de una vez
	private void UpdateHUD()
	{
		HUDView.Instance?.ActualizarMonedas(coins);
		HUDView.Instance?.ActualizarCombo(combo);
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

	#endregion
}
