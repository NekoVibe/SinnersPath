using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
	[Header("Weapon Settings")]
	public GameObject weaponPrefab; // El prefab del arma que se va a añadir al inventario

	[Header("Visual Settings (Optional)")]
	public float rotationSpeed = 50f; // Velocidad de rotación del arma en el pedestal
	public bool rotateWeapon = true;
	public float bobSpeed = 1f; // Velocidad de movimiento vertical
	public float bobHeight = 0.2f; // Altura del movimiento

	private Vector3 startPosition;

	private void Start()
	{
		startPosition = transform.position;

		// Asegurarse de que tiene un Collider con isTrigger = true
		Collider col = GetComponent<Collider>();
		if (col == null)
		{
			col = gameObject.AddComponent<BoxCollider>();
		}
		col.isTrigger = true;
	}

	private void Update()
	{
		// Efecto visual de rotación
		if (rotateWeapon)
		{
			transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
		}

		// Efecto visual de levitación
		if (bobHeight > 0)
		{
			float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
			transform.position = new Vector3(transform.position.x, newY, transform.position.z);
		}
	}
}
