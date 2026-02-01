using UnityEngine;
using System.Collections;

public class BossEnemy : EnemyBase
{
	[Header("Boss Settings")]
	[SerializeField] private float punchRange = 2.5f;
	[SerializeField] private float punchCooldown = 1.2f;
	[SerializeField] private float punchWindup = 0.3f;
	[SerializeField] private int punchDamage = 2;
	[SerializeField] private float knockbackForce = 15f;

	[Header("Boss Visual")]
	[SerializeField] private Color punchWindupColor = new Color(1f, 0.2f, 0.2f);

	[Header("Boss Movement")]
	[SerializeField] private float chaseSpeed = 4f;
	[SerializeField] private float bossDetectionRange = 20f;

	// Estado del punch
	private float lastPunchTime = -999f;
	private bool isPunching = false;

	// Referencias
	private SpriteRenderer bossSpriteRenderer;
	private Color bossOriginalColor;
	private Rigidbody bossRb;
	private Animator bossAnimator;

	// Animation parameter hashes
	private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
	private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
	private static readonly int PunchHash = Animator.StringToHash("Punch");

	private void Awake()
	{
		bossSpriteRenderer = GetComponent<SpriteRenderer>();
		bossRb = GetComponent<Rigidbody>();
		bossAnimator = GetComponent<Animator>();

		if (bossSpriteRenderer != null)
		{
			bossOriginalColor = bossSpriteRenderer.color;
		}
	}

	// Override Update to prevent EnemyBase.Update() from running
	protected override void Update()
	{
		if (IsDead()) return;

		// Si esta atacando, no hacer nada mas
		if (isPunching)
		{
			if (bossRb != null)
			{
				bossRb.linearVelocity = new Vector3(0, bossRb.linearVelocity.y, 0);
			}
			return;
		}

		Transform player = GetPlayer();
		if (player == null) return;

		float distanceToPlayer = Vector3.Distance(transform.position, player.position);

		// Actualizar orientacion del sprite
		UpdateSpriteFlip(player);

		// Si esta en rango de punch, atacar
		if (distanceToPlayer <= punchRange && CanPunch())
		{
			StartCoroutine(PerformPunch());
		}
		// Si esta en rango de deteccion, perseguir
		else if (distanceToPlayer <= bossDetectionRange && distanceToPlayer > punchRange)
		{
			ChasePlayer(player);
		}
		else
		{
			// Idle
			if (bossRb != null)
			{
				bossRb.linearVelocity = new Vector3(0, bossRb.linearVelocity.y, 0);
			}
			SetAnimationState(false, false);
		}
	}


	private void UpdateSpriteFlip(Transform player)
	{
		if (bossSpriteRenderer == null) return;

		float directionToPlayer = player.position.x - transform.position.x;

		// Flip sprite segun direccion
		if (directionToPlayer < 0)
		{
			bossSpriteRenderer.flipX = true;
		}
		else if (directionToPlayer > 0)
		{
			bossSpriteRenderer.flipX = false;
		}
	}

	private void ChasePlayer(Transform player)
	{
		Vector3 direction = (player.position - transform.position).normalized;
		direction.y = 0f;

		if (bossRb != null)
		{
			bossRb.linearVelocity = new Vector3(direction.x * chaseSpeed, bossRb.linearVelocity.y, direction.z * chaseSpeed);
		}

		SetAnimationState(true, false);
	}

	private void SetAnimationState(bool walking, bool attacking)
	{
		if (bossAnimator == null) return;

		bossAnimator.SetBool(IsWalkingHash, walking);
		bossAnimator.SetBool(IsAttackingHash, attacking);
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

	private bool CanPunch()
	{
		return Time.time >= lastPunchTime + punchCooldown && !isPunching;
	}

	private IEnumerator PerformPunch()
	{
		isPunching = true;

		// Detener movimiento
		if (bossRb != null)
		{
			bossRb.linearVelocity = Vector3.zero;
		}

		// Animacion de ataque
		SetAnimationState(false, true);
		if (bossAnimator != null)
		{
			bossAnimator.SetTrigger(PunchHash);
		}

		// Cambiar color para indicar windup
		if (bossSpriteRenderer != null)
		{
			bossSpriteRenderer.color = punchWindupColor;
		}

		Debug.Log("Boss: Winding up punch!");

		// Esperar windup
		yield return new WaitForSeconds(punchWindup);

		// Restaurar color
		if (bossSpriteRenderer != null)
		{
			bossSpriteRenderer.color = bossOriginalColor;
		}

		// Aplicar dano
		ApplyPunchDamage();

		lastPunchTime = Time.time;

		// Esperar un poco antes de poder moverse
		yield return new WaitForSeconds(0.3f);

		isPunching = false;
		SetAnimationState(false, false);
	}

	private void ApplyPunchDamage()
	{
		Transform player = GetPlayer();
		if (player == null) return;

		float distanceToPlayer = Vector3.Distance(transform.position, player.position);

		if (distanceToPlayer <= punchRange + 0.5f)
		{
			PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
			if (playerHealth != null)
			{
				playerHealth.TakeDamage(punchDamage);
				Debug.Log($"Boss punched player for {punchDamage} damage!");

				// Knockback
				Rigidbody playerRb = player.GetComponent<Rigidbody>();
				if (playerRb != null)
				{
					Vector3 knockbackDir = (player.position - transform.position).normalized;
					knockbackDir.y = 0.3f;
					playerRb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
				}
			}
		}
	}

	public new void TakeDamage(int damageAmount)
	{
		// Interrumpir punch si esta en uno
		if (isPunching)
		{
			isPunching = false;
			StopAllCoroutines();

			SetAnimationState(false, false);

			if (bossSpriteRenderer != null)
			{
				bossSpriteRenderer.color = bossOriginalColor;
			}
		}

		base.TakeDamage(damageAmount);
	}

	private void OnDrawGizmosSelected()
	{
		// Rango de punch
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, punchRange);

		// Rango de deteccion
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, bossDetectionRange);
	}
}
