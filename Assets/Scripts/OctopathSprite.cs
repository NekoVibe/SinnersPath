using UnityEngine;

/// <summary>
/// 2.5D sprite positioning with multiple style options.
/// Supports flat (Cult of the Lamb) or upright (Octopath) sprite orientations.
/// </summary>
public class OctopathSprite : MonoBehaviour
{
    public enum SpriteStyle
    {
        Flat,       // Cult of the Lamb - sprites lie nearly flat on ground
        Upright     // Octopath Traveler - sprites tilted to appear standing
    }

    [Header("Sprite Orientation")]
    [SerializeField] private float tiltAngle = 10f;

    [Header("Flip Settings")]
    [SerializeField] private bool flipWithMovement = true;
    [SerializeField] private float flipThreshold = 0.1f;

    private SpriteRenderer spriteRenderer;
    private Vector3 lastPosition;
    private bool facingRight = true;
    private Camera mainCamera;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        lastPosition = transform.position;
    }

    private void Start()
    {
        ApplyRotation();
    }

    private void LateUpdate()
    {
        ApplyRotation();
        if (flipWithMovement && spriteRenderer != null)
            ApplyFlip();
    }

    private void ApplyRotation()
    {
        transform.rotation = Quaternion.Euler(tiltAngle, 0f, 0f);
    }

    private void ApplyFlip()
    {
        Vector3 movement = transform.position - lastPosition;
        if (Mathf.Abs(movement.x) > flipThreshold * Time.deltaTime)
        {
            bool shouldFaceRight = movement.x > 0;
            if (shouldFaceRight != facingRight)
            {
                facingRight = shouldFaceRight;
                spriteRenderer.flipX = !facingRight;
            }
        }
        lastPosition = transform.position;
    }

    /// <summary>
    /// Manually set facing direction (useful for attacks, etc.)
    /// </summary>
    public void SetFacing(bool faceRight)
    {
        facingRight = faceRight;
        if (spriteRenderer != null)
            spriteRenderer.flipX = !facingRight;
    }

    /// <summary>
    /// Get current facing direction
    /// </summary>
    public bool IsFacingRight() => facingRight;

    /// <summary>
    /// Get the tilt angle
    /// </summary>
    public float GetTiltAngle() => tiltAngle;
}
