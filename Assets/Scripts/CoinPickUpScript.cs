using UnityEngine;

public class CoinPickup : MonoBehaviour
{
	[Header("Coin Settings")]
	[SerializeField] private int coinValue = 1; // Cuántas monedas da
	[SerializeField] private float pickupRadius = 1.5f; // Radio de recogida
	[SerializeField] private LayerMask playerLayer;

	[Header("Movement (opcional)")]
	[SerializeField] private bool floatAnimation = true;
	[SerializeField] private float floatSpeed = 2f;
	[SerializeField] private float floatAmplitude = 0.3f;

	[Header("Rotation (opcional)")]
	[SerializeField] private bool rotateAnimation = true;
	[SerializeField] private float rotationSpeed = 100f;

	[Header("Lifetime")]
	[SerializeField] private bool autoDestroy = false;
	[SerializeField] private float lifetime = 10f; // Segundos antes de desaparecer

	private Vector3 startPosition;
	private float timeAlive = 0f;
	private bool collected = false;

	private void Start()
	{
		startPosition = transform.position;
	}

	private void Update()
	{
		if (collected) return;

		// Animación de flotación
		if (floatAnimation)
		{
			float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
			transform.position = new Vector3(transform.position.x, newY, transform.position.z);
		}

		// Animación de rotación
		if (rotateAnimation)
		{
			transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
		}

		// Auto-destrucción después de un tiempo
		if (autoDestroy)
		{
			timeAlive += Time.deltaTime;
			if (timeAlive >= lifetime)
			{
				Destroy(gameObject);
			}
		}

		// Detectar player cercano
		CheckPlayerPickup();
	}

	private void CheckPlayerPickup()
	{
		Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius, playerLayer);

		foreach (Collider hit in hits)
		{
			if (hit.CompareTag("Player"))
			{
				CollectCoin(hit.gameObject);
				break;
			}
		}
	}

	private void CollectCoin(GameObject player)
	{
		if (collected) return;
		collected = true;

		// Dar monedas al GameManager
		if (GameManager.Instance != null)
		{
			GameManager.Instance.AddCoins(coinValue);
			Debug.Log($"+{coinValue} coin(s)! Total: {GameManager.Instance.Coins}");
		}

		// Aquí puedes añadir efectos de sonido/partículas
		// AudioManager.Instance?.PlaySound("CoinPickup");
		// Instantiate(coinPickupEffect, transform.position, Quaternion.identity);

		// Destruir la moneda
		Destroy(gameObject);
	}

	// Visualizar el radio de recogida en el editor
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, pickupRadius);
	}

	// Método alternativo: usar trigger collider
	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player") && !collected)
		{
			CollectCoin(other.gameObject);
		}
	}
}
