using UnityEngine;

public class RangedEnemy : EnemyBase
{
	[Header("Ranged Attack Settings")]
	[SerializeField] private GameObject bulletPrefab;
	[SerializeField] private float shootRange = 10f;
	[SerializeField] private float shootCooldown = 1f;
	[SerializeField] private int bulletDamage = 1;
	[SerializeField] private float minShootRange = 3f;

	[Header("Bullet Spawn")]
	[SerializeField] private Vector3 bulletSpawnOffset = new Vector3(0.5f, 0f, 0f);

	[Header("Shoot Visual")]
	[SerializeField] private Color shootWindupColor = new Color(1f, 0.3f, 0.3f);
	[SerializeField] private float shootWindupTime = 0.2f;

	// Estado del disparo
	private float lastShootTime = -999f;
	private bool isShooting = false;

	// Referencias
	private SpriteRenderer rangedSpriteRenderer;
	private Color rangedOriginalColor;
	private Rigidbody rangedRb;
	private Animator rangedAnimator;

	// Animation parameter hashes
	private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
	private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

	private void Awake()
	{
		rangedSpriteRenderer = GetComponent<SpriteRenderer>();
		rangedRb = GetComponent<Rigidbody>();
		rangedAnimator = GetComponent<Animator>();

		if (rangedSpriteRenderer != null)
		{
			rangedOriginalColor = rangedSpriteRenderer.color;
		}
	}

	private void LateUpdate()
	{
		if (IsDead()) return;

		// Si esta disparando, no hacer nada
		if (isShooting) return;

		// Verificar si puede disparar
		Transform player = GetPlayer();
		if (player == null) return;

		float distanceToPlayer = Vector3.Distance(transform.position, player.position);

		if (CanShoot(distanceToPlayer))
		{
			StartCoroutine(PerformShoot());
		}
		else
		{
			// Detectar si esta caminando
			bool isMoving = rangedRb != null && rangedRb.linearVelocity.magnitude > 0.1f;
			SetAnimationState(isMoving, false);
		}
	}

	private void SetAnimationState(bool walking, bool attacking)
	{
		if (rangedAnimator == null) return;

		rangedAnimator.SetBool(IsWalkingHash, walking);
		rangedAnimator.SetBool(IsAttackingHash, attacking);
	}

	private Transform GetPlayer()
	{
		if (PlayerPersistence.Instance != null)
		{
			return PlayerPersistence.Instance.transform;
		}

		GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
		return playerObj != null ? playerObj.transform : null;
	}

	private bool CanShoot(float distance)
	{
		return distance <= shootRange &&
		       distance >= minShootRange &&
		       Time.time >= lastShootTime + shootCooldown &&
		       !isShooting;
	}

	private System.Collections.IEnumerator PerformShoot()
	{
		isShooting = true;

		// Detener movimiento
		if (rangedRb != null)
		{
			rangedRb.linearVelocity = Vector3.zero;
		}

		// Animacion de ataque
		SetAnimationState(false, true);

		// Cambiar color para indicar preparacion
		if (rangedSpriteRenderer != null)
		{
			rangedSpriteRenderer.color = shootWindupColor;
		}

		Debug.Log("RangedEnemy: Preparing to shoot!");

		// Esperar windup
		yield return new WaitForSeconds(shootWindupTime);

		// Restaurar color
		if (rangedSpriteRenderer != null)
		{
			rangedSpriteRenderer.color = rangedOriginalColor;
		}

		// Disparar
		ShootBullet();

		lastShootTime = Time.time;

		// Pequena pausa despues de disparar
		yield return new WaitForSeconds(0.1f);

		isShooting = false;
		SetAnimationState(false, false);
	}

	private void ShootBullet()
	{
		if (bulletPrefab == null)
		{
			Debug.LogWarning("RangedEnemy: No bullet prefab assigned!");
			return;
		}

		Transform player = GetPlayer();
		if (player == null) return;

		// Calcular direccion hacia el jugador (solo eje X)
		float dirX = player.position.x - transform.position.x;
		Vector3 direction = new Vector3(dirX > 0 ? 1f : -1f, 0f, 0f);

		// Ajustar offset segun la direccion - alejarlo mas del enemigo
		Vector3 spawnOffset = bulletSpawnOffset;
		if (direction.x < 0)
		{
			spawnOffset.x = -Mathf.Abs(spawnOffset.x) - 1f;
		}
		else
		{
			spawnOffset.x = Mathf.Abs(spawnOffset.x) + 1f;
		}

		Vector3 spawnPosition = transform.position + spawnOffset;

		// Crear bala
		GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
		EnemyBullet enemyBullet = bullet.GetComponent<EnemyBullet>();

		if (enemyBullet != null)
		{
			// Pasar el collider del enemigo para ignorar colisiones
			Collider myCollider = GetComponent<Collider>();
			enemyBullet.Initialize(direction, bulletDamage, myCollider);
			Debug.Log($"RangedEnemy: Shot fired towards {direction}!");
		}
		else
		{
			Debug.LogError("RangedEnemy: bulletPrefab does not have EnemyBullet component! Make sure to use EnemyBullet.prefab, not Bullet.prefab");
			Destroy(bullet);
		}
	}

	public new void TakeDamage(int damageAmount)
	{
		// Interrumpir disparo si esta en uno
		if (isShooting)
		{
			isShooting = false;
			StopAllCoroutines();

			SetAnimationState(false, false);

			if (rangedSpriteRenderer != null)
			{
				rangedSpriteRenderer.color = rangedOriginalColor;
			}
		}

		base.TakeDamage(damageAmount);
	}

	private void OnDrawGizmosSelected()
	{
		// Rango de disparo
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, shootRange);

		// Rango minimo
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position, minShootRange);

		// Punto de spawn de bala
		Gizmos.color = Color.yellow;
		Gizmos.DrawSphere(transform.position + bulletSpawnOffset, 0.2f);
	}
}
