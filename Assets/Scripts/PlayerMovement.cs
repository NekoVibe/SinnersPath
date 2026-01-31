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

	[Header("Animation")]
	[SerializeField] private Animator animator;

	[Header("Look At Mouse")]
	[SerializeField] private Transform spriteTransform; // El transform que tiene el sprite
	[SerializeField] private Camera mainCamera;

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
		dashAction = playerInput.actions["Jump"];

		// Buscar animator si no está asignado
		if (animator == null)
		{
			animator = GetComponent<Animator>();
		}

		// ↓ AÑADIDO
		// Buscar la cámara principal
		if (mainCamera == null)
		{
			mainCamera = Camera.main;
		}

		// Si no hay spriteTransform asignado, usar el propio transform
		if (spriteTransform == null)
		{
			spriteTransform = transform;
		}
		// ↑ HASTA AQUÍ
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
				DashTrailEffect.Instance?.StopTrail();
			}
		}

		// Actualizar animación
		UpdateAnimation();

		// ↓ AÑADIDO - Mirar hacia el cursor
		LookAtMouse();
		// ↑ HASTA AQUÍ
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
			dashDirection = transform.forward;
		}

		isDashing = true;
		dashTimer = dashDuration;
		dashCooldownTimer = dashCooldown;

		DashTrailEffect.Instance?.StartTrail();
	}

	private void UpdateAnimation()
	{
		if (animator == null) return;

		bool isMoving = currentVelocity.magnitude > 0.1f;
		animator.SetBool("isWalking", isMoving);
	}

	// ↓ AÑADIDO - Método para mirar hacia el cursor
	private void LookAtMouse()
	{
		if (mainCamera == null || spriteTransform == null) return;

		// Compare in screen space (works correctly even when camera is offset from player)
		Vector3 playerScreenPos = mainCamera.WorldToScreenPoint(transform.position);
		float directionX = Input.mousePosition.x - playerScreenPos.x;

		// Flip del sprite según la dirección
		if (directionX > 0)
		{
			// Cursor a la DERECHA → Sprite normal (escala positiva)
			spriteTransform.localScale = new Vector3(Mathf.Abs(spriteTransform.localScale.x), spriteTransform.localScale.y, spriteTransform.localScale.z);
		}
		else if (directionX < 0)
		{
			// Cursor a la IZQUIERDA → Flip (escala negativa en X)
			spriteTransform.localScale = new Vector3(-Mathf.Abs(spriteTransform.localScale.x), spriteTransform.localScale.y, spriteTransform.localScale.z);
		}
	}
	// ↑ HASTA AQUÍ

	public void ResetVelocity()
	{
		currentVelocity = Vector2.zero;
		isDashing = false;
		dashTimer = 0f;
	}
}
