using UnityEngine;

public class MovementScript : MonoBehaviour
{
	[Header("Movement Properties")]
	public float speed = 5f;

	[Header("Dash Properties")]
	public float dashSpeed = 20f;
	public float dashDuration = 0.2f;
	public float dashCooldown = 1f;

	private bool dash = false;
	private float dashTimer = 0f;
	private float nextDash = 0f;
	private Vector3 dashDirection;

	Rigidbody rb;

	void Start()
	{
		rb = GetComponent<Rigidbody>();
	}

	private void Update()
	{
		// No permitir movimiento si el juego está en pausa
		if (PauseMenu.Instance != null && PauseMenu.Instance.IsPaused())
			return;

		if (nextDash > 0f)
		{
			nextDash -= Time.deltaTime;
		}

		if (dash)
		{
			dashTimer -= Time.deltaTime;
			if (dashTimer <= 0f)
			{
				dash = false;
			}
		}

		if (Input.GetKeyDown(KeyCode.Space) && nextDash <= 0f)
		{
			StartDash();
		}
	}

	private void FixedUpdate()
	{
		// No aplicar física si el juego está en pausa
		if (PauseMenu.Instance != null && PauseMenu.Instance.IsPaused())
			return;

		if (dash)
		{
			rb.linearVelocity = dashDirection * dashSpeed;
		}
		else
		{
			// GetAxisRaw = valores exactos (-1, 0, 1)
			float horizontal = Input.GetAxisRaw("Horizontal");
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

	void StartDash()
	{
		Debug.Log("Dash!");
		nextDash = dashCooldown;
		dash = true;
		dashTimer = dashDuration;

		// Obtener posición del mouse en el mundo 3D
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		Plane groundPlane = new Plane(Vector3.up, transform.position);

		if (groundPlane.Raycast(ray, out float distance))
		{
			Vector3 mousePosition = ray.GetPoint(distance);
			dashDirection = (mousePosition - transform.position).normalized;
			dashDirection.y = 0f; // Mantener dash en el plano horizontal
		}
	}
}
