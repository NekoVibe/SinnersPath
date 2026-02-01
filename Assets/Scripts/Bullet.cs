using UnityEngine;

public class Bullet : MonoBehaviour
{
	[Header("Bullet Settings")]
	[SerializeField] private float speed = 20f;
	[SerializeField] private float maxLifetime = 5f; // Tiempo máximo antes de destruirse
	[SerializeField] private int damage = 10;
	[SerializeField] private LayerMask hitLayers;

	[Header("Effects")]
	[SerializeField] private GameObject impactEffect;

	private Vector3 direction;
	private bool isInitialized = false;

	private void Start()
	{
		// Auto-destruir después de maxLifetime para evitar balas eternas
		Destroy(gameObject, maxLifetime);
	}

	/// <summary>
	/// Inicializa la bala con la dirección y configuración del arma
	/// </summary>
	public void Initialize(Vector3 targetDirection, int bulletDamage, LayerMask layers, GameObject impactPrefab = null)
	{
		// Solo movimiento en eje X: determinar si va a la derecha (+1) o izquierda (-1)
		float xDirection = targetDirection.x >= 0 ? 1f : -1f;
		direction = new Vector3(xDirection, 0f, 0f);

		damage = bulletDamage;
		hitLayers = layers;
		impactEffect = impactPrefab;
		isInitialized = true;
	}

	private void Update()
	{
		if (!isInitialized) return;

		// Mover la bala solo en el eje X
		transform.position += direction * speed * Time.deltaTime;
	}

	private void OnTriggerEnter(Collider other)
	{
		// Verificar si está en las capas que puede golpear
		if (((1 << other.gameObject.layer) & hitLayers) == 0)
		{
			return;
		}

		// Intentar aplicar daño a enemigos
		EnemyBase enemy = other.GetComponent<EnemyBase>();
		if (enemy != null)
		{
			enemy.TakeDamage(damage);
		}

		// Efecto de impacto
		if (impactEffect != null)
		{
			GameObject impact = Instantiate(impactEffect, transform.position, Quaternion.identity);
			Destroy(impact, 2f);
		}

		// Destruir la bala al impactar
		Destroy(gameObject);
	}

	private void OnCollisionEnter(Collision collision)
	{
		// Verificar si está en las capas que puede golpear
		if (((1 << collision.gameObject.layer) & hitLayers) == 0)
		{
			return;
		}

		// Intentar aplicar daño a enemigos
		EnemyBase enemy = collision.gameObject.GetComponent<EnemyBase>();
		if (enemy != null)
		{
			enemy.TakeDamage(damage);
		}

		// Efecto de impacto en el punto de contacto
		if (impactEffect != null && collision.contacts.Length > 0)
		{
			ContactPoint contact = collision.contacts[0];
			GameObject impact = Instantiate(impactEffect, contact.point, Quaternion.LookRotation(contact.normal));
			Destroy(impact, 2f);
		}

		// Destruir la bala al impactar
		Destroy(gameObject);
	}
}
