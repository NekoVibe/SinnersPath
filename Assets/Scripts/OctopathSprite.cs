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
    [SerializeField] private float tiltAngle = 8f;

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
    }

    private void ApplyRotation()
    {
        transform.rotation = Quaternion.Euler(tiltAngle, 0f, 0f);
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
