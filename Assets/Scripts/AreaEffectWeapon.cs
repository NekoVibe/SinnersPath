using UnityEngine;

public class AreaEffectWeapon : WeaponBase
{
	[Header("Weapon Stats")]
	[SerializeField] private int damage = 10;
	[SerializeField] private float fireRate = 1f;
	[SerializeField] private LayerMask hitLayers;

	[Header("Area Effect")]
	[SerializeField] private GameObject effectPrefab; // Prefab del cilindro/efecto
	[SerializeField] private float effectDuration = 2f;

	[Header("Effects")]
	[SerializeField] private GameObject spawnEffect;

	[Header("Audio")]
	[SerializeField] private AudioClip spawnSound;

	private AudioSource audioSource;
	private Camera mainCamera;
	private float lastFireTime = 0f;

	protected override void Start()
	{
		base.Start();

		audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
		}

		mainCamera = Camera.main;

		if (effectPrefab == null)
		{
			Debug.LogError($"{weaponName}: No Effect Prefab assigned!");
		}
	}

	public override void Fire()
	{
		if (Time.time < lastFireTime + fireRate)
		{
			return;
		}

		if (effectPrefab == null)
		{
			return;
		}

		SpawnEffect();
		lastFireTime = Time.time;
	}

	private void SpawnEffect()
	{
		TriggerAttackAnimation();

		Vector3 spawnPosition = GetMouseWorldPosition();
		spawnPosition.y = 0f; // Posición en el suelo

		// Instanciar el prefab
		GameObject effect = Instantiate(effectPrefab, spawnPosition, Quaternion.identity);
		effect.name = "BlueMask_AreaEffect";

		// Agregar el script de daño de área si no lo tiene
		AreaDamage areaDamage = effect.GetComponent<AreaDamage>();
		if (areaDamage == null)
		{
			areaDamage = effect.AddComponent<AreaDamage>();
		}
		areaDamage.Initialize(damage, hitLayers);

		// Efecto visual de spawn
		if (spawnEffect != null)
		{
			GameObject vfx = Instantiate(spawnEffect, spawnPosition, Quaternion.identity);
			Destroy(vfx, 2f);
		}

		// Sonido
		if (audioSource != null && spawnSound != null)
		{
			audioSource.PlayOneShot(spawnSound);
		}

		// Destruir después de la duración
		Destroy(effect, effectDuration);
	}

	private Vector3 GetMouseWorldPosition()
	{
		if (mainCamera == null)
		{
			return transform.position;
		}

		Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

		// Raycast al plano del suelo (Y = 0)
		Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
		float distance;

		if (groundPlane.Raycast(ray, out distance))
		{
			return ray.GetPoint(distance);
		}

		// Fallback: raycast normal
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, 1000f))
		{
			return hit.point;
		}

		return transform.position;
	}

	public override void Reload()
	{
		// Sin sistema de munición
	}

	public override string GetAmmoInfo()
	{
		return "∞";
	}
}

// Componente para manejar el daño de área del cilindro
public class AreaDamage : MonoBehaviour
{
	private int damage;
	private LayerMask hitLayers;
	private System.Collections.Generic.HashSet<GameObject> damagedEnemies = new System.Collections.Generic.HashSet<GameObject>();

	public void Initialize(int dmg, LayerMask layers)
	{
		damage = dmg;
		hitLayers = layers;
	}

	private void OnTriggerEnter(Collider other)
	{
		// Verificar si está en las capas que puede golpear
		if (((1 << other.gameObject.layer) & hitLayers) == 0)
		{
			return;
		}

		// Evitar dañar al mismo enemigo múltiples veces
		if (damagedEnemies.Contains(other.gameObject))
		{
			return;
		}

		EnemyBase enemy = other.GetComponent<EnemyBase>();
		if (enemy != null)
		{
			enemy.TakeDamage(damage);
			damagedEnemies.Add(other.gameObject);
		}
	}
}
