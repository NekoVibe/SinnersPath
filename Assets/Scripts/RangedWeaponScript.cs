using UnityEngine;

public class RangedWeapon : WeaponBase
{
	[Header("Weapon Stats")]
	[SerializeField] private int damage = 10;
	[SerializeField] private float fireRate = 0.5f; // Tiempo entre disparos
	[SerializeField] private float range = 100f;
	[SerializeField] private LayerMask hitLayers; // Capas que puede golpear

	[Header("Ammo")]
	[SerializeField] private int maxAmmo = 30;
	[SerializeField] private int currentAmmo;
	[SerializeField] private int reserveAmmo = 90;
	[SerializeField] private float reloadTime = 2f;

	[Header("Effects")]
	[SerializeField] private GameObject muzzleFlashEffect;
	[SerializeField] private GameObject impactEffect;
	[SerializeField] private Transform firePoint; // Punto desde donde sale el disparo

	[Header("Audio")]
	[SerializeField] private AudioClip shootSound;
	[SerializeField] private AudioClip reloadSound;
	[SerializeField] private AudioClip emptySound;

	// Referencias
	private AudioSource audioSource;
	private Camera mainCamera;

	// Estado
	private float lastFireTime = 0f;
	private bool isReloading = false;

	protected override void Start()
	{
		base.Start(); // IMPORTANTE: Busca el Animator del jugador

		// Inicializar munición
		currentAmmo = maxAmmo;

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
	}

	// Disparar el arma
	public override void Fire()
	{
		// Verificar si está recargando
		if (isReloading)
		{
			return;
		}

		// Verificar cooldown (fire rate)
		if (Time.time < lastFireTime + fireRate)
		{
			return;
		}

		// Verificar munición
		if (currentAmmo <= 0)
		{
			// Sonido de arma vacía
			PlaySound(emptySound);
			return;
		}

		// DISPARAR
		PerformShot();

		// Consumir munición
		currentAmmo--;
		lastFireTime = Time.time;
	}

	// Realizar el disparo (raycast)
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

		// Raycast desde la cámara (para precisión)
		Ray ray;
		if (mainCamera != null)
		{
			// Disparar desde el centro de la pantalla
			ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
		}
		else
		{
			// Fallback: disparar desde el firePoint
			ray = new Ray(firePoint.position, firePoint.forward);
		}

		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, range, hitLayers))
		{
			// Efecto de impacto
			if (impactEffect != null)
			{
				GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
				Destroy(impact, 2f);
			}

			// Intentar aplicar daño a EnemyBase
			EnemyBase enemy = hit.collider.GetComponent<EnemyBase>();
			if (enemy != null)
			{
				enemy.TakeDamage(damage);
			}

			// Debug visual del raycast
			Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);
		}
		else
		{
			// No golpeó nada
			Debug.DrawRay(ray.origin, ray.direction * range, Color.yellow, 1f);
		}
	}

	// Recargar el arma
	public override void Reload()
	{
		// No recargar si ya está llena
		if (currentAmmo == maxAmmo)
		{
			return;
		}

		// No recargar si no hay munición de reserva
		if (reserveAmmo <= 0)
		{
			return;
		}

		// No recargar si ya está recargando
		if (isReloading)
		{
			return;
		}

		// Iniciar recarga
		StartCoroutine(ReloadRoutine());
	}

	// Corrutina de recarga
	private System.Collections.IEnumerator ReloadRoutine()
	{
		isReloading = true;

		// Sonido de recarga
		PlaySound(reloadSound);

		// Esperar tiempo de recarga
		yield return new WaitForSeconds(reloadTime);

		// Calcular cuánta munición recargar
		int ammoNeeded = maxAmmo - currentAmmo;
		int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);

		currentAmmo += ammoToReload;
		reserveAmmo -= ammoToReload;

		isReloading = false;
	}

	// Reproducir sonido
	private void PlaySound(AudioClip clip)
	{
		if (audioSource != null && clip != null)
		{
			audioSource.PlayOneShot(clip);
		}
	}

	// Obtener info de munición
	public override string GetAmmoInfo()
	{
		return $"{currentAmmo}/{reserveAmmo}";
	}

	// Añadir munición de reserva (pickup de munición)
	public void AddReserveAmmo(int amount)
	{
		reserveAmmo += amount;
	}

	// Debug en el editor
	private void OnDrawGizmosSelected()
	{
		if (firePoint == null) return;

		// Dibujar rango del arma
		Gizmos.color = Color.red;
		Gizmos.DrawRay(firePoint.position, firePoint.forward * range);
	}
}
