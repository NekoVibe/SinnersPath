using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
	[Header("Movement")]
	[SerializeField] private float moveSpeed = 8f;
	[SerializeField] private float acceleration = 80f;
	[SerializeField] private float deceleration = 160f;

	[Header("Dash")]
	[SerializeField] private float dashSpeed = 25f;
	[SerializeField] private float dashDuration = 0.15f;
	[SerializeField] private float dashCooldown = 0.8f;

	private Rigidbody rb;
	private PlayerInput playerInput;
	private InputAction moveAction;
	private InputAction dashAction;

	private Vector2 moveInput;
	private Vector2 currentVelocity;
	private Vector3 lastMoveDirection;

	private bool isDashing;
	private float dashTimer;
	private float dashCooldownTimer;
	private Vector3 dashDirection;

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		playerInput = GetComponent<PlayerInput>();

		if (playerInput == null)
		{
			Debug.LogError("PlayerInput component missing! Add it and assign InputSystem_Actions.");
			enabled = false;
			return;
		}

		moveAction = playerInput.actions["Move"];
		dashAction = playerInput.actions["Jump"]; // Using Jump action for dash
	}

	private void OnEnable()
	{
		if (dashAction != null)
			dashAction.performed += OnDash;
	}

	private void OnDisable()
	{
		if (dashAction != null)
			dashAction.performed -= OnDash;
	}

	private void Update()
	{
		// Read input
		moveInput = moveAction.ReadValue<Vector2>();

		// Track last movement direction for dash when stationary
		if (moveInput.magnitude > 0.1f)
		{
			lastMoveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
		}

		// Dash cooldown
		if (dashCooldownTimer > 0f)
		{
			dashCooldownTimer -= Time.deltaTime;
		}

		// Dash duration
		if (isDashing)
		{
			dashTimer -= Time.deltaTime;
			if (dashTimer <= 0f)
			{
				isDashing = false;
			}
		}
	}

	private void FixedUpdate()
	{
		if (isDashing)
		{
			ApplyDash();
			return;
		}

		ApplyMovement();
	}

	private void ApplyMovement()
	{
		Vector2 targetVelocity = moveInput * moveSpeed;
		float rate = (moveInput.magnitude > 0.1f) ? acceleration : deceleration;
		currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, rate * Time.fixedDeltaTime);
		rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.y);
	}

	private void ApplyDash()
	{
		Vector2 dashVelocity = new Vector2(dashDirection.x, dashDirection.z) * dashSpeed;
		currentVelocity = dashVelocity;
		rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.y);
	}

	private void OnDash(InputAction.CallbackContext context)
	{
		if (dashCooldownTimer > 0f || isDashing)
			return;

		// Dash in movement direction, or last direction if stationary
		if (moveInput.magnitude > 0.1f)
		{
			dashDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
		}
		else if (lastMoveDirection.magnitude > 0.1f)
		{
			dashDirection = lastMoveDirection;
		}
		else
		{
			dashDirection = transform.forward; // Fallback to facing direction
		}

		isDashing = true;
		dashTimer = dashDuration;
		dashCooldownTimer = dashCooldown;
	}

	public void ResetVelocity()
	{
		currentVelocity = Vector2.zero;
		isDashing = false;
		dashTimer = 0f;
	}
}
