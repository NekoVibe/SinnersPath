using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
	[Header("Bullet Settings")]
	[SerializeField] private float speed = 10f;
	[SerializeField] private float maxLifetime = 5f;
	[SerializeField] private int damage = 1;

	[Header("Effects")]
	[SerializeField] private GameObject impactEffect;

	private Vector3 direction;
	private bool isInitialized = false;
	private Collider bulletCollider;
	private Collider ownerCollider;

	private void Awake()
	{
		bulletCollider = GetComponent<Collider>();
	}

	private void Start()
	{
		Destroy(gameObject, maxLifetime);
	}

	public void Initialize(Vector3 targetDirection, int bulletDamage, Collider shooterCollider)
	{
		direction = targetDirection.normalized;
		damage = bulletDamage;
		ownerCollider = shooterCollider;
		isInitialized = true;

		// Ignorar colision con el que disparo
		if (bulletCollider != null && ownerCollider != null)
		{
			Physics.IgnoreCollision(bulletCollider, ownerCollider);
		}
	}

	public void Initialize(Vector3 targetDirection, int bulletDamage = 1)
	{
		direction = targetDirection.normalized;
		damage = bulletDamage;
		isInitialized = true;
	}

	private void Update()
	{
		if (!isInitialized) return;

		transform.position += direction * speed * Time.deltaTime;
	}

	private void OnTriggerEnter(Collider other)
	{
		// Ignorar al dueno de la bala
		if (other == ownerCollider) return;

		// Ignorar otros enemigos y sus balas (layer 7 = Enemy)
		if (other.gameObject.layer == 7 || other.GetComponent<EnemyBullet>() != null)
			return;

		// Intentar aplicar dano al jugador
		PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
		if (playerHealth != null)
		{
			playerHealth.TakeDamage(damage);
			Debug.Log($"Enemy bullet hit player for {damage} damage!");
			SpawnImpactEffect();
			Destroy(gameObject);
			return;
		}

		// Si choca con algo que no es enemigo (paredes, etc), destruirse
		SpawnImpactEffect();
		Destroy(gameObject);
	}

	private void OnCollisionEnter(Collision collision)
	{
		// Ignorar al dueno de la bala
		if (collision.collider == ownerCollider) return;

		// Ignorar otros enemigos (layer 7 = Enemy)
		if (collision.gameObject.layer == 7)
			return;

		PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
		if (playerHealth != null)
		{
			playerHealth.TakeDamage(damage);
			Debug.Log($"Enemy bullet hit player for {damage} damage!");
		}

		SpawnImpactEffect();
		Destroy(gameObject);
	}

	private void SpawnImpactEffect()
	{
		if (impactEffect != null)
		{
			GameObject impact = Instantiate(impactEffect, transform.position, Quaternion.identity);
			Destroy(impact, 2f);
		}
	}
}
