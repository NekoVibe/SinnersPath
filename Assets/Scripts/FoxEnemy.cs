using UnityEngine;
using System.Collections;

public class FoxEnemy : EnemyBase
{
	[Header("Pounce Attack Settings")]
	[SerializeField] private float pounceRange = 8f;
	[SerializeField] private float pounceCooldown = 2.5f;
	[SerializeField] private float pounceWindup = 0.4f;
	[SerializeField] private float pounceDuration = 0.5f;
	[SerializeField] private float pounceHeight = 2f;
	[SerializeField] private float minPounceRange = 4f;

	[Header("Pounce Visual")]
	[SerializeField] private Color pounceWindupColor = new Color(1f, 0.5f, 0f);

	// Estado del pounce
	private float lastPounceTime = -999f;
	private bool isPouncing = false;
	private bool isWindingUp = false;
	private bool hasHitPlayerThisPounce = false;
	private Vector3 pounceTarget;
	private Vector3 pounceStartPos;
	private float pounceTimer = 0f;
	private float originalY;

	// Referencias
	private SpriteRenderer foxSpriteRenderer;
	private Color foxOriginalColor;
	private Rigidbody foxRb;
	private Animator foxAnimator;

	// Animation parameter hashes
	private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
	private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

	private void Awake()
	{
		foxSpriteRenderer = GetComponent<SpriteRenderer>();
		foxRb = GetComponent<Rigidbody>();
		foxAnimator = GetComponent<Animator>();

		if (foxSpriteRenderer != null)
		{
			foxOriginalColor = foxSpriteRenderer.color;
		}
	}

	private void LateUpdate()
	{
		if (IsDead()) return;

		// Si está en medio del abalanzamiento, manejar el pounce
		if (isPouncing)
		{
			UpdatePounce();
			SetAnimationState(false, true); // No walking, attacking
			return;
		}

		// Si está preparando el salto
		if (isWindingUp)
		{
			if (foxRb != null)
			{
				foxRb.linearVelocity = Vector3.zero;
			}
			SetAnimationState(false, true); // No walking, attacking (windup)
			return;
		}

		// Verificar si puede abalanzarse
		Transform player = GetPlayer();
		if (player == null) return;

		float distanceToPlayer = Vector3.Distance(transform.position, player.position);

		if (CanPounce(distanceToPlayer))
		{
			StartCoroutine(PerformPounce());
		}
		else
		{
			// Detectar si está caminando (el EnemyBase lo hace en Update)
			bool isMoving = foxRb != null && foxRb.linearVelocity.magnitude > 0.1f;
			SetAnimationState(isMoving, false);
		}
	}

	private void SetAnimationState(bool walking, bool attacking)
	{
		if (foxAnimator == null) return;

		foxAnimator.SetBool(IsWalkingHash, walking);
		foxAnimator.SetBool(IsAttackingHash, attacking);
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

	private bool CanPounce(float distance)
	{
		return distance <= pounceRange &&
		       distance >= minPounceRange &&
		       Time.time >= lastPounceTime + pounceCooldown &&
		       !isPouncing &&
		       !isWindingUp;
	}

	private IEnumerator PerformPounce()
	{
		isWindingUp = true;
		hasHitPlayerThisPounce = false;

		// Detener movimiento y hacer kinematic
		if (foxRb != null)
		{
			foxRb.linearVelocity = Vector3.zero;
			foxRb.isKinematic = true;
		}

		// Animación de ataque
		SetAnimationState(false, true);

		// Cambiar color para indicar preparación
		if (foxSpriteRenderer != null)
		{
			foxSpriteRenderer.color = pounceWindupColor;
		}

		// Guardar posición objetivo
		Transform player = GetPlayer();
		if (player != null)
		{
			pounceTarget = player.position;
		}
		pounceStartPos = transform.position;
		originalY = pounceStartPos.y;

		Debug.Log("Fox: Preparing to pounce!");

		// Esperar windup
		yield return new WaitForSeconds(pounceWindup);

		// Restaurar color
		if (foxSpriteRenderer != null)
		{
			foxSpriteRenderer.color = foxOriginalColor;
		}

		// Iniciar el salto
		isWindingUp = false;
		isPouncing = true;
		pounceTimer = 0f;
		lastPounceTime = Time.time;

		Debug.Log("Fox: Pouncing!");
	}

	private void UpdatePounce()
	{
		pounceTimer += Time.deltaTime;
		float progress = pounceTimer / pounceDuration;

		if (progress >= 1f)
		{
			EndPounce();
			return;
		}

		// Calcular posición horizontal
		Vector3 flatStart = new Vector3(pounceStartPos.x, originalY, pounceStartPos.z);
		Vector3 flatTarget = new Vector3(pounceTarget.x, originalY, pounceTarget.z);
		Vector3 newPos = Vector3.Lerp(flatStart, flatTarget, progress);

		// Altura parabólica
		float height = pounceHeight * 4f * progress * (1f - progress);
		newPos.y = originalY + height;

		// Aplicar posición
		transform.position = newPos;

		// Detectar colisión con el jugador
		if (!hasHitPlayerThisPounce)
		{
			CheckPounceHit();
		}
	}

	private void CheckPounceHit()
	{
		int playerLayerMask = 1 << 6;
		Collider[] hits = Physics.OverlapSphere(transform.position, 2f, playerLayerMask);

		foreach (Collider hit in hits)
		{
			PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
			if (playerHealth != null)
			{
				hasHitPlayerThisPounce = true;
				playerHealth.TakeDamage(1);
				Debug.Log("Fox pounced on player for 1 damage!");

				Rigidbody playerRb = hit.GetComponent<Rigidbody>();
				if (playerRb != null)
				{
					Vector3 knockbackDir = (hit.transform.position - transform.position).normalized;
					playerRb.AddForce(knockbackDir * 12f, ForceMode.Impulse);
				}

				break;
			}
		}
	}

	private void EndPounce()
	{
		isPouncing = false;
		hasHitPlayerThisPounce = false;

		// Terminar animación de ataque
		SetAnimationState(false, false);

		// Restaurar la posición Y original (antes del salto)
		Vector3 pos = transform.position;
		pos.y = originalY;
		transform.position = pos;

		// Rehabilitar física
		if (foxRb != null)
		{
			foxRb.isKinematic = false;
			foxRb.linearVelocity = Vector3.zero;
		}

		Debug.Log("Fox: Pounce ended");
	}

	public new void TakeDamage(int damageAmount)
	{
		// Interrumpir pounce si está en uno
		if (isPouncing || isWindingUp)
		{
			isPouncing = false;
			isWindingUp = false;
			hasHitPlayerThisPounce = false;
			StopAllCoroutines();

			SetAnimationState(false, false);

			if (foxSpriteRenderer != null)
			{
				foxSpriteRenderer.color = foxOriginalColor;
			}

			if (foxRb != null)
			{
				foxRb.isKinematic = false;
			}
		}

		base.TakeDamage(damageAmount);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, pounceRange);

		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position, minPounceRange);
	}
}
