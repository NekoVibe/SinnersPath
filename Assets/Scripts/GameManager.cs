using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	[Header("Starting Values")]
	[SerializeField] private int startingLives = 3;
	[SerializeField] private int startingCoins = 10;

	private int points;
	private int coins;
	private int lives;

	public event System.Action<int> OnPointsChanged;
	public event System.Action<int> OnCoinsChanged;
	public event System.Action<int> OnLivesChanged;

	public int Points => points;
	public int Coins => coins;
	public int Lives => lives;

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);

		points = 0;
		coins = startingCoins;
		lives = startingLives;
	}

	void Start()
	{
		OnPointsChanged?.Invoke(points);
		OnCoinsChanged?.Invoke(coins);
		OnLivesChanged?.Invoke(lives);
	}

	public void AddPoints(int amount)
	{
		if (amount < 0) return;
		points += amount;
		OnPointsChanged?.Invoke(points);
	}

	public void AddCoins(int amount)
	{
		if (amount < 0) return;
		coins += amount;
		OnCoinsChanged?.Invoke(coins);
	}

	public bool SpendCoins(int amount)
	{
		if (amount < 0 || coins < amount)
			return false;

		coins -= amount;
		OnCoinsChanged?.Invoke(coins);
		return true;
	}

	public void AddLives(int amount)
	{
		if (amount < 0) return;
		lives += amount;
		OnLivesChanged?.Invoke(lives);
	}

	public bool SpendLife()
	{
		if (lives <= 1)
			return false;

		lives -= 1;
		OnLivesChanged?.Invoke(lives);
		return true;
	}
}
