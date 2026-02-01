using UnityEngine;

public class RangedWeapon : WeaponBase
{
	[Header("Weapon Stats")]
	[SerializeField] private int damage = 10;
	[SerializeField] private float fireRate = 0.5f; // Tiempo entre disparos
	[SerializeField] private LayerMask hitLayers; // Capas que puede golpear

	[Header("Projectile")]
	[SerializeField] private GameObject bulletPrefab; // Prefab de la bala
	[SerializeField] private Transform firePoint; // Punto desde donde sale el disparo

	[Header("Effects")]
	[SerializeField] private GameObject muzzleFlashEffect;
	[SerializeField] private GameObject impactEffect;

	[Header("Audio")]
	[SerializeField] private AudioClip shootSound;

	// Referencias
	private AudioSource audioSource;
	private Camera mainCamera;

	// Estado
	private float lastFireTime = 0f;

	protected override void Start()
	{
		base.Start(); // IMPORTANTE: Busca el Animator del jugador

		// Obtener componentes
		audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
		}

		mainCamera = Camera.main;

		// Validar firePoint
		if (firePoint == null)
		{
			Debug.LogWarning($"{weaponName}: No FirePoint assigned! Using weapon position.");
			firePoint = transform;
		}

		// Validar bulletPrefab
		if (bulletPrefab == null)
		{
			Debug.LogError($"{weaponName}: No Bullet Prefab assigned!");
		}
	}

	// Disparar el arma
	public override void Fire()
	{
		// Verificar cooldown (fire rate)
		if (Time.time < lastFireTime + fireRate)
		{
			return;
		}

		// Verificar que existe el prefab
		if (bulletPrefab == null)
		{
			return;
		}

		// DISPARAR
		PerformShot();

		lastFireTime = Time.time;
	}

	// Realizar el disparo (proyectil)
	private void PerformShot()
	{
		// Triggerar animación de ataque
		TriggerAttackAnimation();

		// Efectos visuales
		if (muzzleFlashEffect != null)
		{
			GameObject flash = Instantiate(muzzleFlashEffect, firePoint.position, firePoint.rotation);
			Destroy(flash, 0.1f);
		}

		// Sonido de disparo
		PlaySound(shootSound);

		// Calcular dirección hacia el cursor
		Vector3 targetDirection = GetShootDirection();

		// Instanciar la bala sin rotación
		GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

		// Inicializar la bala con los parámetros del arma
		Bullet bullet = bulletObj.GetComponent<Bullet>();
		if (bullet != null)
		{
			bullet.Initialize(targetDirection, damage, hitLayers, impactEffect);
		}
	}

	// Obtener la dirección de disparo hacia donde apunta el cursor
	private Vector3 GetShootDirection()
	{
		if (mainCamera == null)
		{
			return firePoint.forward;
		}

		// Obtener posición del cursor en pantalla
		Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

		// Hacer raycast para encontrar el punto objetivo
		RaycastHit hit;
		Vector3 targetPoint;

		if (Physics.Raycast(ray, out hit, 1000f))
		{
			// Apuntar al punto donde el rayo golpea
			targetPoint = hit.point;
		}
		else
		{
			// Si no golpea nada, apuntar a un punto lejano en la dirección del rayo
			targetPoint = ray.origin + ray.direction * 1000f;
		}

		// Calcular dirección desde firePoint hacia el punto objetivo
		Vector3 direction = (targetPoint - firePoint.position).normalized;

		return direction;
	}

	// Recargar el arma (sin munición, no hace nada)
	public override void Reload()
	{
		// Sin sistema de munición
	}

	// Reproducir sonido
	private void PlaySound(AudioClip clip)
	{
		if (audioSource != null && clip != null)
		{
			audioSource.PlayOneShot(clip);
		}
	}

	// Obtener info de munición (sin límite)
	public override string GetAmmoInfo()
	{
		return "∞";
	}

	// Debug en el editor
	private void OnDrawGizmosSelected()
	{
		if (firePoint == null) return;

		// Dibujar dirección de disparo
		Gizmos.color = Color.red;
		Gizmos.DrawRay(firePoint.position, firePoint.forward * 10f);
	}
}
