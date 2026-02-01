using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeleeSwingWeapon : WeaponBase
{
	[Header("Weapon Stats")]
	[SerializeField] private int damage = 15;
	[SerializeField] private float attackRate = 0.8f;
	[SerializeField] private LayerMask hitLayers;

	[Header("Swing Settings")]
	[SerializeField] private float swingRange = 2.5f;
	[SerializeField] private float swingAngle = 120f;
	[SerializeField] private float swingDuration = 0.3f;

	[Header("Visual Effect")]
	[SerializeField] private GameObject swingEffectPrefab;
	[SerializeField] private float effectScale = 3f;
	[SerializeField] private float effectYOffset = 0.1f;

	[Header("Audio")]
	[SerializeField] private AudioClip swingSound;
	[SerializeField] private AudioClip hitSound;

	private AudioSource audioSource;
	private float lastAttackTime = 0f;
	private Camera mainCamera;
	private bool isSwinging = false;
	private float swingStartTime = 0f;

	protected override void Start()
	{
		base.Start();

		audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
		}

		mainCamera = Camera.main;
	}

	private void OnEnable()
	{
		// Reset swing state when enabled (fixes stuck state after scene changes)
		isSwinging = false;
	}

	// Obtener el transform del jugador dinámicamente
	private Transform GetPlayerTransform()
	{
		// Opción 1: Buscar en el padre
		Transform player = transform.root;
		if (player != null && player != transform)
		{
			return player;
		}

		// Opción 2: Buscar por tag
		GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
		if (playerObj != null)
		{
			return playerObj.transform;
		}

		// Opción 3: PlayerPersistence
		if (PlayerPersistence.Instance != null)
		{
			return PlayerPersistence.Instance.transform;
		}

		// Fallback
		return transform;
	}

	public override void Fire()
	{
		if (Time.time < lastAttackTime + attackRate)
		{
			return;
		}

		// Safety: reset stuck swing state after timeout
		if (isSwinging && Time.time > swingStartTime + swingDuration + 1f)
		{
			Debug.LogWarning("MeleeSwingWeapon: Resetting stuck isSwinging state");
			isSwinging = false;
		}

		if (isSwinging)
		{
			return;
		}

		swingStartTime = Time.time;
		StartCoroutine(PerformSwing());
		lastAttackTime = Time.time;
	}

	private IEnumerator PerformSwing()
	{
		isSwinging = true;

		// Triggerar animación
		TriggerAttackAnimation();

		// Sonido de swing
		PlaySound(swingSound);

		// Obtener referencias actualizadas
		Transform playerTransform = GetPlayerTransform();

		// Obtener dirección hacia el cursor
		Vector3 cursorDirection = GetCursorDirection(playerTransform);
		float centerAngle = Mathf.Atan2(cursorDirection.x, cursorDirection.z) * Mathf.Rad2Deg;
		float halfAngle = swingAngle / 2f;

		// Instanciar efecto visual (solo decoración)
		GameObject effectObj = null;
		if (swingEffectPrefab != null)
		{
			// Position slightly above ground
			Vector3 effectPos = playerTransform.position + Vector3.up * effectYOffset;
			effectObj = Instantiate(swingEffectPrefab, effectPos, Quaternion.identity);

			// Flip sprite based on attack direction (left/right)
			float flipX = cursorDirection.x < 0 ? -1f : 1f;
			effectObj.transform.localScale = new Vector3(effectScale * flipX, effectScale, effectScale);
			Destroy(effectObj, swingDuration + 0.1f);
		}

		// Detectar y dañar enemigos en el arco
		HashSet<GameObject> hitEnemies = new HashSet<GameObject>();
		float elapsed = 0f;

		while (elapsed < swingDuration)
		{
			// Refrescar playerTransform cada frame por si cambió
			playerTransform = GetPlayerTransform();
			Vector3 origin = playerTransform.position;

			// Detectar todos los enemigos en rango
			Collider[] colliders = Physics.OverlapSphere(origin, swingRange, hitLayers);

			foreach (Collider col in colliders)
			{
				if (hitEnemies.Contains(col.gameObject))
				{
					continue;
				}

				// Calcular dirección al enemigo
				Vector3 toEnemy = col.transform.position - origin;
				toEnemy.y = 0;

				if (toEnemy.magnitude > swingRange)
				{
					continue;
				}

				// Calcular ángulo
				float enemyAngle = Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg;
				float angleDiff = Mathf.DeltaAngle(centerAngle, enemyAngle);

				// Está dentro del arco?
				if (Mathf.Abs(angleDiff) <= halfAngle)
				{
					EnemyBase enemy = col.GetComponent<EnemyBase>();
					if (enemy != null && !enemy.IsDead())
					{
						enemy.TakeDamage(damage);
						hitEnemies.Add(col.gameObject);

						PlaySound(hitSound);
						Debug.Log($"YellowMask: Hit {enemy.name} for {damage} damage!");

						// Knockback
						Rigidbody rb = enemy.GetComponent<Rigidbody>();
						if (rb != null)
						{
							Vector3 pushDir = (enemy.transform.position - origin).normalized;
							rb.AddForce(pushDir * 8f, ForceMode.Impulse);
						}
					}
				}
			}

			// Actualizar posición del efecto visual
			if (effectObj != null)
			{
				effectObj.transform.position = playerTransform.position + Vector3.up * effectYOffset;
			}

			elapsed += Time.deltaTime;
			yield return null;
		}

		isSwinging = false;
	}

	private Vector3 GetCursorDirection(Transform playerTransform)
	{
		// Refrescar cámara si es null (puede pasar al cambiar de escena)
		if (mainCamera == null)
		{
			mainCamera = Camera.main;
		}

		if (mainCamera == null)
		{
			Debug.LogWarning("MeleeSwingWeapon: No se encontró Camera.main");
			return playerTransform.forward;
		}

		Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
		Plane groundPlane = new Plane(Vector3.up, playerTransform.position);
		float distance;

		if (groundPlane.Raycast(ray, out distance))
		{
			Vector3 hitPoint = ray.GetPoint(distance);
			Vector3 direction = (hitPoint - playerTransform.position).normalized;
			direction.y = 0;
			return direction.normalized;
		}

		return playerTransform.forward;
	}

	private void PlaySound(AudioClip clip)
	{
		if (audioSource != null && clip != null)
		{
			audioSource.PlayOneShot(clip);
		}
	}

	public override void Reload() { }

	public override string GetAmmoInfo()
	{
		return "∞";
	}

	private void OnDrawGizmosSelected()
	{
		Transform origin = transform.root != null ? transform.root : transform;
		Vector3 pos = origin.position;

		Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f);

		float halfAngle = swingAngle / 2f;
		Vector3 forward = origin.forward;
		float baseAngle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

		for (int i = 0; i <= 20; i++)
		{
			float t = (float)i / 20;
			float angle = baseAngle + Mathf.Lerp(-halfAngle, halfAngle, t);
			Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
			Gizmos.DrawLine(pos, pos + dir * swingRange);
		}
	}
}
