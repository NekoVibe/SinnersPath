using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
	[Header("Health Settings")]
	public int maxHealth = 3;

	[SerializeField] private int _currentHealth;
	public int currentHealth
	{
		get => _currentHealth;
		private set
		{
			_currentHealth = value;
			UpdateHealthHUD();
		}
	}

	[Header("Damage Settings")]
	public float invulnerabilityTime = 1.5f; // Tiempo de invulnerabilidad tras recibir daño
	private bool isInvulnerable = false;
	private float invulnerabilityTimer = 0f;

	private void Start()
	{
		// Inicializar vida y forzar actualización del HUD
		_currentHealth = maxHealth;
		UpdateHealthHUD();
	}

	private void Update()
	{
		// Manejar invulnerabilidad
		if (isInvulnerable)
		{
			invulnerabilityTimer -= Time.deltaTime;
			if (invulnerabilityTimer <= 0f)
			{
				isInvulnerable = false;
			}
		}
	}

	// Recibir daño
	public void TakeDamage(int damage)
	{
		if (isInvulnerable || currentHealth <= 0)
			return;

		currentHealth -= damage;
		currentHealth = Mathf.Max(currentHealth, 0); // No bajar de 0

		Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");

		UpdateHealthHUD();

		if (currentHealth <= 0)
		{
			Die();
		}
		else
		{
			// Activar invulnerabilidad temporal
			isInvulnerable = true;
			invulnerabilityTimer = invulnerabilityTime;
		}
	}

	// Curar vida
	public void Heal(int amount)
	{
		if (currentHealth >= maxHealth)
			return;

		currentHealth += amount;
		currentHealth = Mathf.Min(currentHealth, maxHealth); // No exceder el máximo

		Debug.Log($"Player healed {amount}. Health: {currentHealth}/{maxHealth}");
		UpdateHealthHUD();
	}

	// Actualizar corazones en el HUD
	private void UpdateHealthHUD()
	{
		// Pasar la vida actual al HUD, que mostrará tantos corazones llenos como vida tenga
		HUDView.Instance?.ActualizarHearts(currentHealth);
	}

	// Muerte del jugador
	private void Die()
	{
		Debug.Log("Player died!");

		// Detener el cronómetro
		TimerManager timerManager = FindFirstObjectByType<TimerManager>();
		timerManager?.setCronometro(false);

		// Aquí añadir lógica de muerte (reiniciar nivel, pantalla de game over, etc.)
		// Por ejemplo:
		// GameManager.Instance.GameOver();
	}

	// Getter para saber si está vivo
	public bool IsAlive()
	{
		return currentHealth > 0;
	}

	// Getter de vida actual
	public int GetCurrentHealth()
	{
		return currentHealth;
	}

	// Para debugging: cambiar vida directamente
	[ContextMenu("Test: Take 1 Damage")]
	private void TestTakeDamage()
	{
		TakeDamage(1);
	}

	[ContextMenu("Test: Heal 1 HP")]
	private void TestHeal()
	{
		Heal(1);
	}

#if UNITY_EDITOR
	// Actualizar HUD si cambias currentHealth en el Inspector durante Play mode
	private void OnValidate()
	{
		if (Application.isPlaying)
		{
			_currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);
			UpdateHealthHUD();
		}
	}
#endif
}
