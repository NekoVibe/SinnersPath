using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class SlotMachine : MonoBehaviour
{
	public enum Symbol
	{
		Seven,          // 7
		Six,            // 6
		Heart,          // Corazón
		BrokenHeart,    // Corazón partido
		Coin,           // Moneda
		BrokenCoin,     // Moneda partida
		X,              // X
		Clover          // Trébol
	}

	[Header("UI References")]
	[SerializeField] private Image[] reelImages;           // 3 imágenes para los carretes
	[SerializeField] private Button spinButton;
	[SerializeField] private TextMeshProUGUI resultText;
	[SerializeField] private TextMeshProUGUI costText;

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

	[Header("Symbol Weights (mayor = más común)")]
	[SerializeField] private float weightSeven = 5f;        // 7 - Raro
	[SerializeField] private float weightSix = 5f;          // 6 - Raro
	[SerializeField] private float weightHeart = 10f;       // Corazón - Poco común
	[SerializeField] private float weightBrokenHeart = 15f; // Corazón roto - Común
	[SerializeField] private float weightCoin = 15f;        // Moneda - Común
	[SerializeField] private float weightBrokenCoin = 20f;  // Moneda rota - Muy común
	[SerializeField] private float weightX = 10f;           // X - Poco común
	[SerializeField] private float weightClover = 20f;      // Trébol - Muy común

	[Header("Audio")]
	[SerializeField] private AudioClip spinSound;
	[SerializeField] private AudioClip winSound;
	[SerializeField] private AudioClip loseSound;
	[SerializeField] private AudioClip jackpotSound;
	[SerializeField] private AudioClip breakSound;

	private Symbol[] currentSymbols = new Symbol[3];
	private bool isSpinning = false;
	private bool isBroken = false;

	private void Start()
	{
		if (spinButton != null)
		{
			spinButton.onClick.AddListener(TrySpin);
		}

		UpdateCostText();
		ShowRandomSymbols();
	}

	private void UpdateCostText()
	{
		if (costText != null)
		{
			costText.text = $"Coste: {spinCost} moneda";
		}
	}

	private void ShowRandomSymbols()
	{
		for (int i = 0; i < 3; i++)
		{
			currentSymbols[i] = GetRandomSymbol();
			UpdateReelImage(i, currentSymbols[i]);
		}
	}

	public void TrySpin()
	{
		if (isBroken)
		{
			ShowResult("¡Máquina rota!", Color.gray);
			return;
		}

		if (isSpinning)
		{
			return;
		}

		// Verificar si hay suficientes monedas
		if (GameManager.Instance == null || GameManager.Instance.Coins < spinCost)
		{
			ShowResult("¡No tienes monedas!", Color.red);
			return;
		}

		// Cobrar la tirada
		GameManager.Instance.SpendCoins(spinCost);

		// Iniciar el giro
		StartCoroutine(SpinReels());
	}

	private IEnumerator SpinReels()
	{
		isSpinning = true;
		ShowResult("...", Color.white);

		// Reproducir sonido de giro
		if (spinSound != null && AudioManager.Instance != null)
		{
			AudioManager.Instance.PlaySFX(spinSound);
		}

		// Animación de giro
		float elapsed = 0f;
		while (elapsed < spinDuration)
		{
			for (int i = 0; i < 3; i++)
			{
				Symbol randomSymbol = GetRandomSymbol();
				UpdateReelImage(i, randomSymbol);
			}

			elapsed += spinSpeed;
			yield return new WaitForSeconds(spinSpeed);
		}

		// Determinar resultado final
		for (int i = 0; i < 3; i++)
		{
			currentSymbols[i] = GetRandomSymbol();
			UpdateReelImage(i, currentSymbols[i]);
		}

		// Evaluar combinación
		EvaluateResult();

		isSpinning = false;
	}

	private Symbol GetRandomSymbol()
	{
		float totalWeight = weightSeven + weightSix + weightHeart + weightBrokenHeart +
		                    weightCoin + weightBrokenCoin + weightX + weightClover;

		float random = Random.Range(0f, totalWeight);
		float cumulative = 0f;

		// Seven
		cumulative += weightSeven;
		if (random < cumulative) return Symbol.Seven;

		// Six
		cumulative += weightSix;
		if (random < cumulative) return Symbol.Six;

		// Heart
		cumulative += weightHeart;
		if (random < cumulative) return Symbol.Heart;

		// BrokenHeart
		cumulative += weightBrokenHeart;
		if (random < cumulative) return Symbol.BrokenHeart;

		// Coin
		cumulative += weightCoin;
		if (random < cumulative) return Symbol.Coin;

		// BrokenCoin
		cumulative += weightBrokenCoin;
		if (random < cumulative) return Symbol.BrokenCoin;

		// X
		cumulative += weightX;
		if (random < cumulative) return Symbol.X;

		// Clover (default)
		return Symbol.Clover;
	}

	private void UpdateReelImage(int index, Symbol symbol)
	{
		if (reelImages == null || index >= reelImages.Length || reelImages[index] == null)
			return;

		reelImages[index].sprite = GetSpriteForSymbol(symbol);
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
		// Verificar si los 3 símbolos son iguales
		if (currentSymbols[0] == currentSymbols[1] && currentSymbols[1] == currentSymbols[2])
		{
			Symbol winningSymbol = currentSymbols[0];

			switch (winningSymbol)
			{
				case Symbol.Seven:
					// 777 -> Añade 1000 puntos, 1 vida y 5 monedas
					Win777();
					break;

				case Symbol.Six:
					// 666 -> Quita 1000 puntos, 1 vida y 5 monedas
					Lose666();
					break;

				case Symbol.Heart:
					// Corazón x3 -> Añade 1 vida
					WinHeart();
					break;

				case Symbol.BrokenHeart:
					// Corazón partido x3 -> Quita 1 vida
					LoseBrokenHeart();
					break;

				case Symbol.Coin:
					// Moneda x3 -> Añade 3 monedas
					WinCoin();
					break;

				case Symbol.BrokenCoin:
					// Moneda partida x3 -> Quita 3 monedas
					LoseBrokenCoin();
					break;

				case Symbol.X:
					// X x3 -> Se rompe la máquina
					BreakMachine();
					break;

				case Symbol.Clover:
					// Trébol x3 -> Añade 500 puntos
					WinClover();
					break;
			}
		}
		else
		{
			ShowResult("Sin premio", Color.white);
		}
	}

	#region Win/Lose Actions

	private void Win777()
	{
		GameManager.Instance?.AddPoints(1000);
		GameManager.Instance?.AddCoins(5);
		AddPlayerLife(1);

		PlaySound(jackpotSound);
		ShowResult("¡¡¡JACKPOT 777!!!\n+1000 pts, +1 vida, +5 monedas", Color.yellow);
	}

	private void Lose666()
	{
		// Quitar puntos (mínimo 0)
		if (GameManager.Instance != null)
		{
			int currentPoints = GameManager.Instance.Points;
			int pointsToRemove = Mathf.Min(currentPoints, 1000);
			GameManager.Instance.SetPoints(currentPoints - pointsToRemove);
		}

		GameManager.Instance?.SpendCoins(5);
		RemovePlayerLife(1);

		PlaySound(loseSound);
		ShowResult("¡MALDICIÓN 666!\n-1000 pts, -1 vida, -5 monedas", Color.red);
	}

	private void WinHeart()
	{
		AddPlayerLife(1);

		PlaySound(winSound);
		ShowResult("¡CORAZONES!\n+1 vida", Color.magenta);
	}

	private void LoseBrokenHeart()
	{
		RemovePlayerLife(1);

		PlaySound(loseSound);
		ShowResult("¡Corazones rotos!\n-1 vida", Color.red);
	}

	private void WinCoin()
	{
		GameManager.Instance?.AddCoins(3);

		PlaySound(winSound);
		ShowResult("¡MONEDAS!\n+3 monedas", Color.yellow);
	}

	private void LoseBrokenCoin()
	{
		GameManager.Instance?.SpendCoins(3);

		PlaySound(loseSound);
		ShowResult("¡Monedas rotas!\n-3 monedas", Color.red);
	}

	private void BreakMachine()
	{
		isBroken = true;

		if (spinButton != null)
		{
			spinButton.interactable = false;
		}

		PlaySound(breakSound);
		ShowResult("¡LA MÁQUINA SE HA ROTO!", Color.gray);
	}

	private void WinClover()
	{
		GameManager.Instance?.AddPoints(500);

		PlaySound(winSound);
		ShowResult("¡TRÉBOLES!\n+500 puntos", Color.green);
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

	/// <summary>
	/// Repara la máquina (llamar desde otro script si quieres)
	/// </summary>
	public void RepairMachine()
	{
		isBroken = false;

		if (spinButton != null)
		{
			spinButton.interactable = true;
		}

		ShowResult("¡Máquina reparada!", Color.green);
	}

	public bool IsBroken() => isBroken;
	public bool IsSpinning() => isSpinning;

	#endregion
}
