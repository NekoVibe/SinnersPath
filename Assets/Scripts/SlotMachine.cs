using UnityEngine;
using System.Collections;
using TMPro;

public class SlotMachine : MonoBehaviour
{
	public enum Symbol
	{
		Seven,
		Six,
		Heart,
		BrokenHeart,
		Coin,
		BrokenCoin,
		X,
		Clover
	}

	[Header("Reel Sprites")]
	[SerializeField] private SpriteRenderer[] reelRenderers;

	[Header("UI")]
	[SerializeField] private TextMeshProUGUI resultText;
	[SerializeField] private TextMeshProUGUI interactPrompt;

	[Header("Symbol Sprites")]
	[SerializeField] private Sprite spriteSeven;
	[SerializeField] private Sprite spriteSix;
	[SerializeField] private Sprite spriteHeart;
	[SerializeField] private Sprite spriteBrokenHeart;
	[SerializeField] private Sprite spriteCoin;
	[SerializeField] private Sprite spriteBrokenCoin;
	[SerializeField] private Sprite spriteX;
	[SerializeField] private Sprite spriteClover;

	[Header("Settings")]
	[SerializeField] private int spinCost = 1;
	[SerializeField] private float spinDuration = 2f;
	[SerializeField] private float spinSpeed = 0.1f;
	[SerializeField] private KeyCode interactKey = KeyCode.F;

	[Header("Symbol Weights (higher = more common)")]
	[SerializeField] private float weightSeven = 20f;
	[SerializeField] private float weightSix = 15f;
	[SerializeField] private float weightHeart = 25f;
	[SerializeField] private float weightBrokenHeart = 20f;
	[SerializeField] private float weightCoin = 30f;
	[SerializeField] private float weightBrokenCoin = 20f;
	[SerializeField] private float weightX = 15f;
	[SerializeField] private float weightClover = 35f;

	[Header("Audio")]
	[SerializeField] private AudioClip spinSound;
	[SerializeField] private AudioClip winSound;
	[SerializeField] private AudioClip loseSound;
	[SerializeField] private AudioClip jackpotSound;
	[SerializeField] private AudioClip breakSound;

	[Header("Result Display")]
	[SerializeField] private float resultDisplayTime = 10f;

	[Header("Win Probability")]
	[Tooltip("Probabilidad de forzar un combo ganador (0-1). Ej: 0.3 = 30% de probabilidad de ganar")]
	[SerializeField] [Range(0f, 1f)] private float forcedWinChance = 0.3f;

	private Symbol[] currentSymbols = new Symbol[3];
	private bool isSpinning = false;
	private bool isBroken = false;
	private bool playerInRange = false;
	private Coroutine resultCoroutine;

	private void Start()
	{
		ShowRandomSymbols();
		interactPrompt?.gameObject.SetActive(false);
		resultText?.gameObject.SetActive(false);
	}

	private void Update()
	{
		if (playerInRange && !isSpinning && Input.GetKeyDown(interactKey))
		{
			Debug.Log("[SlotMachine] Key pressed, trying to spin...");
			TrySpin();
		}
	}

	// 3D Collider support
	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			OnPlayerEnter();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			OnPlayerExit();
		}
	}

	// 2D Collider support
	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("Player"))
		{
			OnPlayerEnter();
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		if (other.CompareTag("Player"))
		{
			OnPlayerExit();
		}
	}

	private void OnPlayerEnter()
	{
		Debug.Log("[SlotMachine] Player entered trigger area");
		playerInRange = true;
		UpdatePromptText();
	}

	private void OnPlayerExit()
	{
		Debug.Log("[SlotMachine] Player exited trigger area");
		playerInRange = false;
		interactPrompt?.gameObject.SetActive(false);
		resultText?.gameObject.SetActive(false);
	}

	private void UpdatePromptText()
	{
		if (interactPrompt != null)
		{
			interactPrompt.gameObject.SetActive(true);
			interactPrompt.color = Color.white;
			if (isBroken)
				interactPrompt.text = "Broken machine";
			else if (isSpinning)
				interactPrompt.text = "Spinning";
			else
				interactPrompt.text = $"Press {interactKey} to spin";
		}
	}

	private void ShowRandomSymbols()
	{
		for (int i = 0; i < 3; i++)
		{
			currentSymbols[i] = GetRandomSymbol();
			UpdateReelSprite(i, currentSymbols[i]);
		}
	}

	public void TrySpin()
	{
		Debug.Log($"[SlotMachine] TrySpin called - isBroken:{isBroken}, isSpinning:{isSpinning}, GameManager:{GameManager.Instance != null}, Coins:{GameManager.Instance?.Coins}");

		if (isBroken)
		{
			ShowResult("Machine broken!", Color.gray);
			return;
		}

		if (isSpinning) return;

		if (GameManager.Instance == null || GameManager.Instance.Coins < spinCost)
		{
			ShowResult("Not enough coins!", Color.red);
			return;
		}

		Debug.Log("[SlotMachine] Starting spin!");
		GameManager.Instance.SpendCoins(spinCost);
		StartCoroutine(SpinReels());
	}

	private IEnumerator SpinReels()
	{
		isSpinning = true;
		ShowResult("...", Color.white, false);
		UpdatePromptText();

		if (spinSound != null && AudioManager.Instance != null)
		{
			AudioManager.Instance.PlaySFX(spinSound);
		}

		float elapsed = 0f;
		while (elapsed < spinDuration)
		{
			for (int i = 0; i < 3; i++)
			{
				Symbol randomSymbol = GetRandomSymbol();
				UpdateReelSprite(i, randomSymbol);
			}

			elapsed += spinSpeed;
			yield return new WaitForSeconds(spinSpeed);
		}

		// Determinar si forzamos un combo ganador
		bool forceWin = Random.value < forcedWinChance;

		if (forceWin)
		{
			// Forzar combo: elegir un símbolo y aplicarlo a los 3 reels
			Symbol winningSymbol = GetRandomSymbol();
			for (int i = 0; i < 3; i++)
			{
				currentSymbols[i] = winningSymbol;
				UpdateReelSprite(i, currentSymbols[i]);
			}
			Debug.Log($"[SlotMachine] Forced win with symbol: {winningSymbol}");
		}
		else
		{
			// Resultado aleatorio normal
			for (int i = 0; i < 3; i++)
			{
				currentSymbols[i] = GetRandomSymbol();
				UpdateReelSprite(i, currentSymbols[i]);
			}
		}

		EvaluateResult();

		isSpinning = false;
		UpdatePromptText();
	}

	private Symbol GetRandomSymbol()
	{
		float totalWeight = weightSeven + weightSix + weightHeart + weightBrokenHeart +
		                    weightCoin + weightBrokenCoin + weightX + weightClover;

		float random = Random.Range(0f, totalWeight);
		float cumulative = 0f;

		cumulative += weightSeven;
		if (random < cumulative) return Symbol.Seven;

		cumulative += weightSix;
		if (random < cumulative) return Symbol.Six;

		cumulative += weightHeart;
		if (random < cumulative) return Symbol.Heart;

		cumulative += weightBrokenHeart;
		if (random < cumulative) return Symbol.BrokenHeart;

		cumulative += weightCoin;
		if (random < cumulative) return Symbol.Coin;

		cumulative += weightBrokenCoin;
		if (random < cumulative) return Symbol.BrokenCoin;

		cumulative += weightX;
		if (random < cumulative) return Symbol.X;

		return Symbol.Clover;
	}

	private void UpdateReelSprite(int index, Symbol symbol)
	{
		if (reelRenderers == null || index >= reelRenderers.Length || reelRenderers[index] == null)
			return;

		reelRenderers[index].sprite = GetSpriteForSymbol(symbol);
	}

	private Sprite GetSpriteForSymbol(Symbol symbol)
	{
		return symbol switch
		{
			Symbol.Seven => spriteSeven,
			Symbol.Six => spriteSix,
			Symbol.Heart => spriteHeart,
			Symbol.BrokenHeart => spriteBrokenHeart,
			Symbol.Coin => spriteCoin,
			Symbol.BrokenCoin => spriteBrokenCoin,
			Symbol.X => spriteX,
			Symbol.Clover => spriteClover,
			_ => null
		};
	}

	private void EvaluateResult()
	{
		if (currentSymbols[0] == currentSymbols[1] && currentSymbols[1] == currentSymbols[2])
		{
			Symbol winningSymbol = currentSymbols[0];

			switch (winningSymbol)
			{
				case Symbol.Seven: Win777(); break;
				case Symbol.Six: Lose666(); break;
				case Symbol.Heart: WinHeart(); break;
				case Symbol.BrokenHeart: LoseBrokenHeart(); break;
				case Symbol.Coin: WinCoin(); break;
				case Symbol.BrokenCoin: LoseBrokenCoin(); break;
				case Symbol.X: BreakMachine(); break;
				case Symbol.Clover: WinClover(); break;
			}
		}
		else
		{
			ShowResult("No prize", Color.white);
		}
	}

	#region Win/Lose Actions

	private void Win777()
	{
		GameManager.Instance?.AddPoints(1000);
		GameManager.Instance?.AddCoins(5);
		AddPlayerLife(1);
		PlaySound(jackpotSound);
		ShowResult("!!!JACKPOT 777!!!\n+1000 pts, +1 life, +5 coins", Color.yellow);
	}

	private void Lose666()
	{
		if (GameManager.Instance != null)
		{
			int currentPoints = GameManager.Instance.Points;
			int pointsToRemove = Mathf.Min(currentPoints, 1000);
			GameManager.Instance.SetPoints(currentPoints - pointsToRemove);
		}
		GameManager.Instance?.SpendCoins(5);
		RemovePlayerLife(1);
		PlaySound(loseSound);
		ShowResult("CURSE 666!\n-1000 pts, -1 life, -5 coins", Color.red);
	}

	private void WinHeart()
	{
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		PlayerHealth health = player?.GetComponent<PlayerHealth>();

		// Si ya tiene vida máxima, dar puntos en lugar de vida
		if (health != null && health.currentHealth >= health.maxHealth)
		{
			GameManager.Instance?.AddPoints(500);
			PlaySound(winSound);
			ShowResult("HEARTS!\n+500 points (full health)", Color.magenta);
		}
		else
		{
			AddPlayerLife(1);
			PlaySound(winSound);
			ShowResult("HEARTS!\n+1 life", Color.magenta);
		}
	}

	private void LoseBrokenHeart()
	{
		RemovePlayerLife(1);
		PlaySound(loseSound);
		ShowResult("Broken hearts!\n-1 life", Color.red);
	}

	private void WinCoin()
	{
		GameManager.Instance?.AddCoins(3);
		PlaySound(winSound);
		ShowResult("COINS!\n+3 coins", Color.yellow);
	}

	private void LoseBrokenCoin()
	{
		GameManager.Instance?.SpendCoins(3);
		PlaySound(loseSound);
		ShowResult("Broken coins!\n-3 coins", Color.red);
	}

	private void BreakMachine()
	{
		isBroken = true;
		PlaySound(breakSound);
		ShowResult("THE MACHINE BROKE!", Color.gray);
		UpdatePromptText();
	}

	private void WinClover()
	{
		GameManager.Instance?.AddPoints(500);
		PlaySound(winSound);
		ShowResult("CLOVERS!\n+500 points", Color.green);
	}

	#endregion

	#region Helper Methods

	private void AddPlayerLife(int amount)
	{
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		if (player != null)
		{
			PlayerHealth health = player.GetComponent<PlayerHealth>();
			health?.Heal(amount);
		}
	}

	private void RemovePlayerLife(int amount)
	{
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		if (player != null)
		{
			PlayerHealth health = player.GetComponent<PlayerHealth>();
			health?.TakeDamage(amount);
		}
	}

	private void PlaySound(AudioClip clip)
	{
		if (clip != null && AudioManager.Instance != null)
		{
			AudioManager.Instance.PlaySFX(clip);
		}
	}

	private void ShowResult(string message, Color color, bool autoHide = true)
	{
		if (resultText != null)
		{
			resultText.gameObject.SetActive(true);
			resultText.text = message;
			resultText.color = color;
		}

		// Auto-ocultar resultado después de unos segundos
		if (autoHide)
		{
			if (resultCoroutine != null)
				StopCoroutine(resultCoroutine);
			resultCoroutine = StartCoroutine(HideResultAfterDelay());
		}
	}

	private IEnumerator HideResultAfterDelay()
	{
		yield return new WaitForSeconds(resultDisplayTime);
		if (resultText != null)
		{
			resultText.text = "";
		}
	}

	#endregion

	#region Public Methods

	public void RepairMachine()
	{
		isBroken = false;
		ShowResult("Machine repaired!", Color.green);
		UpdatePromptText();
	}

	public bool IsBroken() => isBroken;
	public bool IsSpinning() => isSpinning;

	#endregion
}
