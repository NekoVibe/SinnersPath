using UnityEngine;

public class MovementScript : MonoBehaviour
{
	[Header("Movement Properties")]
	public float speed = 5f;

	Rigidbody rb;

	void Start()
	{
		rb = GetComponent<Rigidbody>();
	}

	private void FixedUpdate()
	{
			// A/D → Eje X (Horizontal)
			float horizontal = Input.GetAxisRaw("Horizontal");

			// W/S → Eje Z (Vertical se mapea a profundidad)
			float vertical = Input.GetAxisRaw("Vertical");

			// Movimiento en 3D: X y Z
			Vector3 movement = new Vector3(horizontal, 0f, vertical);

			if (movement.magnitude > 1)
			{
				movement = movement.normalized;
			}

			rb.linearVelocity = movement * speed;
	}
}
