using UnityEngine;

public class EnemyBase : MonoBehaviour
{
	[Header("Enemy Stats")]
	[SerializeField] private string enemyName = "Enemy";
	[SerializeField] private int maxHealth = 3;
	[SerializeField] private int currentHealth;
	[SerializeField] private float moveSpeed = 3f;
	[SerializeField] private int damage = 1;

	[Header("Combat Settings")]
	[SerializeField] private float attackRange = 1.5f;
	[SerializeField] private float attackCooldown = 1.5f;
	[SerializeField] private LayerMask playerLayer;

	[Header("Detection Settings")]
	[SerializeField] private float detectionRange = 8f;
	[SerializeField] private bool alwaysChasePlayer = true;

	[Header("Rewards")]
	[SerializeField] private int pointsOnDeath = 100;
	[SerializeField] private int coinsOnDeath = 5;
	[SerializeField] private GameObject dropOnDeath; // Prefab de item a dropear (opcional)

	[Header("Visual Feedback")]
	[SerializeField] private Color damageFlashColor = Color.red;
	[SerializeField] private float damageFlashDuration = 0.1f;

	[Header("Drops")]
	[SerializeField] private GameObject coinPrefab; // El prefab de la moneda
	[SerializeField] private int minCoins = 1;
	[SerializeField] private int maxCoins = 3;
	[SerializeField] private float dropForce = 2f; // Fuerza para lanzar las monedas

	// Referencias
	private Transform player;
	private Rigidbody rb;
	private SpriteRenderer spriteRenderer;
	private Color originalColor;

	// Estado
	private float lastAttackTime = 0f;
	private bool isDead = false;

	private void Start()
	{
		// Inicializar
		currentHealth = maxHealth;
		rb = GetComponent<Rigidbody>();
		spriteRenderer = GetComponent<SpriteRenderer>();

		if (spriteRenderer != null)
		{
			originalColor = spriteRenderer.color;
		}

		// Buscar al player
		FindPlayer();
	}

	private void Update()
	{
		if (isDead) return;

		// Buscar player si no está asignado
		if (player == null)
		{
			FindPlayer();
			return;
		}

		// Calcular distancia al player
		float distanceToPlayer = Vector3.Distance(transform.position, player.position);

		// Comportamiento según distancia
		if (distanceToPlayer <= attackRange)
		{
			// Atacar
			TryAttack();
		}
		else if (alwaysChasePlayer || distanceToPlayer <= detectionRange)
		{
			// Perseguir
			ChasePlayer();
		}
	}

	// Buscar al player en la escena
	private void FindPlayer()
	{
		if (PlayerPersistence.Instance != null)
		{
			player = PlayerPersistence.Instance.transform;
		}
		else
		{
			GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
			if (playerObj != null)
			{
				player = playerObj.transform;
			}
		}
	}

	// Perseguir al player
	private void ChasePlayer()
	{
		if (player == null) return;

		// Calcular dirección hacia el player
		Vector3 direction = (player.position - transform.position).normalized;
		direction.y = 0f; // Mantener en el plano horizontal

		// Mover hacia el player
		if (rb != null)
		{
			rb.linearVelocity = new Vector3(direction.x * moveSpeed, rb.linearVelocity.y, direction.z * moveSpeed);
		}
		else
		{
			transform.position += direction * moveSpeed * Time.deltaTime;
		}

		// Sprite flipping handled by OctopathSprite component
	}

	// Intentar atacar al player
	private void TryAttack()
	{
		if (Time.time >= lastAttackTime + attackCooldown)
		{
			Attack();
			lastAttackTime = Time.time;
		}

		// Detener movimiento mientras ataca
		if (rb != null)
		{
			rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
		}
	}

	// Atacar al player
	private void Attack()
	{
		if (player == null) return;

		// Verificar si el player está en rango
		Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, playerLayer);

		foreach (Collider hit in hits)
		{
			PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
			if (playerHealth != null)
			{
				playerHealth.TakeDamage(damage);
				Debug.Log($"{enemyName} attacked player for {damage} damage");
				break;
			}
		}
	}

	// Recibir daño
	public void TakeDamage(int damageAmount)
	{
		if (isDead) return;

		currentHealth -= damageAmount;
		currentHealth = Mathf.Max(currentHealth, 0);

		Debug.Log($"{enemyName} took {damageAmount} damage. Health: {currentHealth}/{maxHealth}");

		// Feedback visual
		if (spriteRenderer != null)
		{
			StartCoroutine(DamageFlash());
		}

		// Morir si la vida llega a 0
		if (currentHealth == 0)
		{
			Die();
		}
	}

	// Efecto visual al recibir daño
	private System.Collections.IEnumerator DamageFlash()
	{
		if (spriteRenderer != null)
		{
			spriteRenderer.color = damageFlashColor;
			yield return new WaitForSeconds(damageFlashDuration);
			spriteRenderer.color = originalColor;
		}
	}

	// Morir
	private void Die()
	{
		if (isDead) return;
		isDead = true;

		Debug.Log($"{enemyName} died!");

		// Desregistrar del EnemyManager
		if (EnemyManager.Instance != null)
		{
			EnemyManager.Instance.UnregisterEnemy(this);
		}

		// Dar recompensas
		GiveRewards();

		// Dropear monedas
		DropCoins();

		// Drop de item especial (si existe)
		if (dropOnDeath != null)
		{
			Instantiate(dropOnDeath, transform.position, Quaternion.identity);
		}

		// Destruir el enemigo
		Destroy(gameObject);
	}

	// Nuevo método: Dropear monedas
	private void DropCoins()
	{
		if (coinPrefab == null) return;

		// Cantidad aleatoria de monedas
		int coinCount = Random.Range(minCoins, maxCoins + 1);

		for (int i = 0; i < coinCount; i++)
		{
			// Posición ligeramente aleatoria
			Vector3 dropPosition = transform.position + new Vector3(
				Random.Range(-0.5f, 0.5f),
				0.5f,
				Random.Range(-0.5f, 0.5f)
			);

			// Instanciar la moneda
			GameObject coin = Instantiate(coinPrefab, dropPosition, Quaternion.identity);

			// Añadir fuerza aleatoria para que se disperse (opcional)
			Rigidbody rb = coin.GetComponent<Rigidbody>();
			if (rb != null)
			{
				Vector3 randomDirection = new Vector3(
					Random.Range(-1f, 1f),
					Random.Range(0.5f, 1f),
					Random.Range(-1f, 1f)
				).normalized;

				rb.AddForce(randomDirection * dropForce, ForceMode.Impulse);
			}
		}

		Debug.Log($"{enemyName} dropped {coinCount} coin(s)");
	}

	// Dar recompensas al player
	private void GiveRewards()
	{
		if (GameManager.Instance != null)
		{
			// Sumar puntos
			GameManager.Instance.AddPoints(pointsOnDeath);

			// Sumar monedas
			GameManager.Instance.AddCoins(coinsOnDeath);

			// Aumentar combo
			GameManager.Instance.AddCombo(1);
		}
	}

	// Método público para otros scripts que quieran dañar al enemigo
	public void Damage(int amount)
	{
		TakeDamage(amount);
	}

	// Getters
	public bool IsDead()
	{
		return isDead;
	}

	public int GetCurrentHealth()
	{
		return currentHealth;
	}

	// Dibujar gizmos en el editor
	private void OnDrawGizmosSelected()
	{
		// Rango de ataque (rojo)
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, attackRange);

		// Rango de detección (amarillo)
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, detectionRange);
	}
}
