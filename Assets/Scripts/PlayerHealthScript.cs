using UnityEngine;
using System.Collections;

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
	private bool godMode = false; // Invincible but still shows damage effects

	[Header("Visual Feedback")]
	[SerializeField] private float hitStopDuration = 0.05f;
	private SpriteFlash spriteFlash;

	private void Awake()
	{
		spriteFlash = GetComponent<SpriteFlash>();
	}

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
		if (currentHealth <= 0)
			return;

		if (isInvulnerable)
			return;

		// Visual feedback (always shown)
		spriteFlash?.Flash();
		CameraShake.Instance?.Shake();
		ScreenEffects.Instance?.DamageFlash();
		StartCoroutine(HitStop());

		// Activate brief invulnerability
		isInvulnerable = true;
		invulnerabilityTimer = invulnerabilityTime;

		// God mode: don't reduce health
		if (godMode)
			return;

		currentHealth -= damage;

		Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");

		// Visual feedback
		spriteFlash?.Flash();
		CameraShake.Instance?.Shake();
		ScreenEffects.Instance?.DamageFlash();
		StartCoroutine(HitStop());

		UpdateHealthHUD();

		if (currentHealth == 0)
		{
			isInvulnerable = true;
			invulnerabilityTimer = 999.0f;
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

		// Visual feedback
		ScreenEffects.Instance?.HealFlash();

		UpdateHealthHUD();
	}

	private IEnumerator HitStop()
	{
		Time.timeScale = 0f;
		yield return new WaitForSecondsRealtime(hitStopDuration);
		Time.timeScale = 1f;
	}

	// Actualizar corazones en el HUD
	private void UpdateHealthHUD()
	{
		Debug.Log($"PlayerHealth.UpdateHealthHUD: Intentando actualizar HUD con vida {currentHealth}");

		if (HUDManager.Instance == null)
		{
			Debug.LogError("PlayerHealth: HUDManager.Instance es NULL!");
			return;
		}

		HUDManager.Instance.ActualizarHearts(currentHealth);
	}

	// Muerte del jugador
	private void Die()
	{
		Debug.Log("Player died!");

		// Llamar al GameManager para Game Over
		GameManager.Instance?.GameOver();
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

	// Resetear vida al máximo (útil al reiniciar)
	public void ResetHealth()
	{
		_currentHealth = maxHealth;
		isInvulnerable = false;
		invulnerabilityTimer = 0f;
		UpdateHealthHUD();
		Debug.Log("Player health reset to maximum");
	}

	#region God Mode

	public void EnableGodMode()
	{
		godMode = true;
	}

	public void DisableGodMode()
	{
		godMode = false;
	}

	public bool IsGodMode()
	{
		return godMode;
	}

	#endregion

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
