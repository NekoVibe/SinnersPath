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

	[Header("UI Panel")]
	[SerializeField] private GameObject uiPanel;
	[SerializeField] private TextMeshProUGUI resultText;
	[SerializeField] private TextMeshProUGUI costText;
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
	[SerializeField] private KeyCode interactKey = KeyCode.E;

	[Header("Symbol Weights (higher = more common)")]
	[SerializeField] private float weightSeven = 5f;
	[SerializeField] private float weightSix = 5f;
	[SerializeField] private float weightHeart = 10f;
	[SerializeField] private float weightBrokenHeart = 15f;
	[SerializeField] private float weightCoin = 15f;
	[SerializeField] private float weightBrokenCoin = 20f;
	[SerializeField] private float weightX = 10f;
	[SerializeField] private float weightClover = 20f;

	[Header("Audio")]
	[SerializeField] private AudioClip spinSound;
	[SerializeField] private AudioClip winSound;
	[SerializeField] private AudioClip loseSound;
	[SerializeField] private AudioClip jackpotSound;
	[SerializeField] private AudioClip breakSound;

	private Symbol[] currentSymbols = new Symbol[3];
	private bool isSpinning = false;
	private bool isBroken = false;
	private bool playerInRange = false;
	private bool isActive = false;

	private void Start()
	{
		UpdateCostText();
		ShowRandomSymbols();

		uiPanel?.SetActive(false);
		interactPrompt?.gameObject.SetActive(false);
	}

	private void Update()
	{
		if (playerInRange && Input.GetKeyDown(interactKey))
		{
			if (!isActive)
			{
				OpenMachine();
			}
			else if (!isSpinning)
			{
				TrySpin();
			}
		}

		if (isActive && Input.GetKeyDown(KeyCode.Escape))
		{
			CloseMachine();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			playerInRange = true;
			if (interactPrompt != null)
			{
				interactPrompt.gameObject.SetActive(true);
				interactPrompt.text = isBroken ? "Broken machine" : $"Press {interactKey} to play";
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			playerInRange = false;
			interactPrompt?.gameObject.SetActive(false);
			CloseMachine();
		}
	}

	private void OpenMachine()
	{
		if (isBroken) return;

		isActive = true;
		uiPanel?.SetActive(true);
		interactPrompt?.text = $"Press {interactKey} to spin";
		ShowResult($"Cost: {spinCost} coin", Color.white);
	}

	private void CloseMachine()
	{
		isActive = false;
		uiPanel?.SetActive(false);
		if (interactPrompt != null && playerInRange)
		{
			interactPrompt.text = isBroken ? "Broken machine" : $"Press {interactKey} to play";
		}
	}

	private void UpdateCostText()
	{
		costText?.text = $"Cost: {spinCost} coin";
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

		GameManager.Instance.SpendCoins(spinCost);
		StartCoroutine(SpinReels());
	}

	private IEnumerator SpinReels()
	{
		isSpinning = true;
		ShowResult("...", Color.white);
		interactPrompt?.text = "Spinning...";

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

		for (int i = 0; i < 3; i++)
		{
			currentSymbols[i] = GetRandomSymbol();
			UpdateReelSprite(i, currentSymbols[i]);
		}

		EvaluateResult();

		isSpinning = false;
		if (interactPrompt != null && !isBroken)
			interactPrompt.text = $"Press {interactKey} to spin";
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
		AddPlayerLife(1);
		PlaySound(winSound);
		ShowResult("HEARTS!\n+1 life", Color.magenta);
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
		interactPrompt?.text = "Broken machine";

		Invoke(nameof(CloseMachine), 2f);
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

	private void ShowResult(string message, Color color)
	{
		if (resultText != null)
		{
			resultText.text = message;
			resultText.color = color;
		}
	}

	#endregion

	#region Public Methods

	public void RepairMachine()
	{
		isBroken = false;
		ShowResult("Machine repaired!", Color.green);
		interactPrompt?.text = $"Press {interactKey} to play";
	}

	public bool IsBroken() => isBroken;
	public bool IsSpinning() => isSpinning;

	#endregion
}
