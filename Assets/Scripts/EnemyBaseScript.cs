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
	[SerializeField] private float attackDamageDelay = 0.3f;
	[SerializeField] private float attackDuration = 0.8f;
	[SerializeField] private LayerMask playerLayer;
	[Tooltip("Offset from transform for attack origin (Y should be negative to attack at ground level)")]
	[SerializeField] private Vector3 attackPointOffset = new Vector3(0f, -2f, 0f);

	[Header("Detection Settings")]
	[SerializeField] private float detectionRange = 8f;
	[SerializeField] private bool alwaysChasePlayer = true;

	[Header("Rewards")]
	[SerializeField] private int pointsOnDeath = 100;
	[SerializeField] private int coinsOnDeath = 5;
	[SerializeField] private GameObject dropOnDeath;

	[Header("Visual Feedback")]
	[SerializeField] private Color damageFlashColor = Color.red;
	[SerializeField] private float damageFlashDuration = 0.1f;

	[Header("Drops")]
	[SerializeField] private GameObject coinPrefab;
	[SerializeField] private int minCoins = 1;
	[SerializeField] private int maxCoins = 3;
	[SerializeField] private float dropForce = 2f;

	[Header("Sprite Flip")]
	[SerializeField] private bool facingRight = true;

	[Header("Animation")]
	[SerializeField] private float movementThreshold = 0.1f;

	[Header("Collider Adjustment")]
	[SerializeField] private Vector3 idleColliderCenter = new Vector3(0, 0.5f, 0);
	[SerializeField] private Vector3 idleColliderSize = new Vector3(0.8f, 1.0f, 0.8f);

	[SerializeField] private Vector3 walkColliderCenter = new Vector3(0, 0.45f, 0);
	[SerializeField] private Vector3 walkColliderSize = new Vector3(0.9f, 0.9f, 0.8f);

	[SerializeField] private Vector3 punchColliderCenter = new Vector3(0.2f, 0.4f, 0);
	[SerializeField] private Vector3 punchColliderSize = new Vector3(1.5f, 0.8f, 0.8f);

	// Referencias
	private Transform player;
	private Rigidbody rb;
	private SpriteRenderer spriteRenderer;
	private Animator animator;
	private BoxCollider boxCollider; // NUEVO
	private Color originalColor;

	// Estado
	private float lastAttackTime = 0f;
	private bool isDead = false;
	private bool isAttacking = false;

	// Control de estado de animación previo
	private bool wasWalking = false;

	// Animation parameter hashes
	private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
	private static readonly int PunchHash = Animator.StringToHash("Punch");

	private void Start()
	{
		// Inicializar
		currentHealth = maxHealth;
		rb = GetComponent<Rigidbody>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		animator = GetComponent<Animator>();
		boxCollider = GetComponent<BoxCollider>(); // NUEVO

		if (spriteRenderer != null)
		{
			originalColor = spriteRenderer.color;
		}

		if (boxCollider == null)
		{
			Debug.LogWarning($"{enemyName}: No BoxCollider found! Collider adjustment won't work.");
		}

		// Establecer collider inicial
		SetColliderToIdle();

		// Buscar al player
		FindPlayer();
	}

	protected virtual void Update()
	{
		if (isDead) return;

		// Buscar player si no está asignado
		if (player == null)
		{
			FindPlayer();
			return;
		}

		// Calcular distancia al player (horizontal only - Y positions differ due to collider offsets)
		Vector3 attackOrigin = GetAttackOrigin();
		float distanceToAttack = Vector3.Distance(attackOrigin, player.position);

		// Si está atacando, no hacer nada más
		if (isAttacking)
		{
			// Mantener detenido durante el ataque
			if (rb != null)
			{
				rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
			}
			return;
		}

		// Calcular distancia al player
		float distanceToPlayer = Vector3.Distance(transform.position, player.position);

		// Comportamiento según distancia (use horizontal distance for attack range)
		if (distanceToAttack <= attackRange)
		{
			// Atacar (también actualiza la orientación)
			UpdateSpriteFlip();
			TryAttack();
		}
		else if (alwaysChasePlayer || distanceToPlayer <= detectionRange)
		{
			// Perseguir
			ChasePlayer();
		}
		else
		{
			// Idle - no hacer nada
			UpdateAnimationIdle();
		}
	}

	// NUEVO: Métodos para ajustar el collider
	private void SetColliderToIdle()
	{
		if (boxCollider != null)
		{
			boxCollider.center = idleColliderCenter;
			boxCollider.size = idleColliderSize;
		}
	}

	private void SetColliderToWalk()
	{
		if (boxCollider != null)
		{
			boxCollider.center = walkColliderCenter;
			boxCollider.size = walkColliderSize;
		}
	}

	private void SetColliderToPunch()
	{
		if (boxCollider != null)
		{
			boxCollider.center = punchColliderCenter;
			boxCollider.size = punchColliderSize;
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

	// Actualizar el flip del sprite según la posición del jugador
	private void UpdateSpriteFlip()
	{
		if (player == null || spriteRenderer == null) return;

		// Calcular si el jugador está a la derecha o izquierda
		float directionToPlayer = player.position.x - transform.position.x;

		// Si el jugador está a la izquierda (directionToPlayer < 0)
		if (directionToPlayer < 0 && facingRight)
		{
			Flip();
		}
		// Si el jugador está a la derecha (directionToPlayer > 0)
		else if (directionToPlayer > 0 && !facingRight)
		{
			Flip();
		}
	}

	// Hacer flip del sprite
	private void Flip()
	{
		facingRight = !facingRight;
		spriteRenderer.flipX = !spriteRenderer.flipX;
	}

	// Perseguir al player
	private void ChasePlayer()
	{
		if (player == null) return;

		// Actualizar orientación del sprite
		UpdateSpriteFlip();

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

		// Actualizar animación de movimiento
		UpdateAnimationMovement();
	}

	// Intentar atacar al player
	private void TryAttack()
	{
		// Solo atacar si ha pasado el cooldown y NO está atacando
		if (Time.time >= lastAttackTime + attackCooldown && !isAttacking)
		{
			Attack();
			lastAttackTime = Time.time;
		}

		// Detener movimiento mientras espera para atacar
		if (rb != null)
		{
			rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
		}

		// NUEVO: Si no está atacando, mantener collider idle
		if (!isAttacking)
		{
			SetColliderToIdle();
		}
	}

	// Get the attack origin position (offset from transform)
	private Vector3 GetAttackOrigin()
	{
		return transform.position + attackPointOffset;
	}

	// Atacar al player
	private void Attack()
	{
		if (player == null) return;

		// Use attack origin point instead of transform.position
		Vector3 attackOrigin = GetAttackOrigin();
		Collider[] hits = Physics.OverlapSphere(attackOrigin, attackRange, playerLayer);
		isAttacking = true;

		// Desactivar IsWalking al iniciar el ataque
		if (animator != null)
		{
			if (wasWalking)
			{
				animator.SetBool(IsWalkingHash, false);
				wasWalking = false;
			}
			animator.SetTrigger(PunchHash);
		}

		// NUEVO: Cambiar collider a punch
		SetColliderToPunch();

		// Aplicar daño con delay
		Invoke(nameof(ApplyAttackDamage), attackDamageDelay);

		// Resetear flag de ataque después de la duración completa de la animación
		Invoke(nameof(ResetAttack), attackDuration);
	}

	// Aplicar el daño del ataque (se llama con delay)
	private void ApplyAttackDamage()
	{
		if (player == null || isDead) return;

		// Verificar si el player está en rango
		Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, playerLayer);

		foreach (Collider hit in hits)
		{
			Debug.Log($"Hit: {hit.name} on layer {hit.gameObject.layer}");
			PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
			if (playerHealth != null)
			{
				playerHealth.TakeDamage(damage);
				Debug.Log($"{enemyName} dealt {damage} damage to player!");
				break;
			}
		}
	}

	// Resetear flag de ataque
	private void ResetAttack()
	{
		isAttacking = false;
		// NUEVO: Volver al collider idle
		SetColliderToIdle();
	}

	// Actualizar animación de movimiento solo cuando cambie el estado
	private void UpdateAnimationMovement()
	{
		if (animator == null) return;

		// Detectar si se está moviendo
		bool isMoving = false;
		if (rb != null)
		{
			float horizontalSpeed = Mathf.Abs(rb.linearVelocity.x) + Mathf.Abs(rb.linearVelocity.z);
			isMoving = horizontalSpeed > movementThreshold;
		}

		// SOLO actualizar si el estado cambió
		if (isMoving != wasWalking)
		{
			animator.SetBool(IsWalkingHash, isMoving);
			wasWalking = isMoving;

			// NUEVO: Actualizar collider según si está caminando o no
			if (isMoving)
			{
				SetColliderToWalk();
			}
			else
			{
				SetColliderToIdle();
			}
		}
	}

	// Actualizar animación idle solo cuando cambie el estado
	private void UpdateAnimationIdle()
	{
		if (animator == null) return;

		// SOLO actualizar si estaba caminando
		if (wasWalking)
		{
			animator.SetBool(IsWalkingHash, false);
			wasWalking = false;
			// NUEVO: Cambiar a collider idle
			SetColliderToIdle();
		}

		// Detener movimiento
		if (rb != null)
		{
			rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
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
		Destroy(gameObject, 0.5f);
	}

	// Dropear monedas
	private void DropCoins()
	{
		if (coinPrefab == null) return;

		int coinCount = Random.Range(minCoins, maxCoins + 1);

		for (int i = 0; i < coinCount; i++)
		{
			Vector3 dropPosition = transform.position + new Vector3(
				Random.Range(-0.5f, 0.5f),
				0.5f,
				Random.Range(-0.5f, 0.5f)
			);

			GameObject coin = Instantiate(coinPrefab, dropPosition, Quaternion.identity);

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
			GameManager.Instance.AddPoints(pointsOnDeath);
			GameManager.Instance.AddCoins(coinsOnDeath);
			GameManager.Instance.AddCombo(1);
		}
	}

	public void Damage(int amount)
	{
		TakeDamage(amount);
	}

	public bool IsDead()
	{
		return isDead;
	}

	public int GetCurrentHealth()
	{
		return currentHealth;
	}

	public void SetHealth(int health)
	{
		maxHealth = health;
		currentHealth = health;
	}

	private void OnDrawGizmosSelected()
	{
		// Attack origin point (small red sphere)
		Vector3 attackOrigin = transform.position + attackPointOffset;
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(attackOrigin, 0.2f);

		// Rango de ataque (rojo wire sphere at attack origin)
		Gizmos.DrawWireSphere(attackOrigin, attackRange);

		// Rango de detección (amarillo, from transform position)
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, detectionRange);

		// Line from transform to attack origin (cyan)
		Gizmos.color = Color.cyan;
		Gizmos.DrawLine(transform.position, attackOrigin);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, detectionRange);

		// NUEVO: Visualizar los colliders
		if (boxCollider != null)
		{
			Gizmos.color = Color.green;
			Gizmos.matrix = transform.localToWorldMatrix;

			// Mostrar collider idle
			Gizmos.color = new Color(0, 1, 0, 0.3f);
			Gizmos.DrawWireCube(idleColliderCenter, idleColliderSize);

			// Mostrar collider walk
			Gizmos.color = new Color(0, 0, 1, 0.3f);
			Gizmos.DrawWireCube(walkColliderCenter, walkColliderSize);

			// Mostrar collider punch
			Gizmos.color = new Color(1, 0, 0, 0.3f);
			Gizmos.DrawWireCube(punchColliderCenter, punchColliderSize);
		}
	}
}
