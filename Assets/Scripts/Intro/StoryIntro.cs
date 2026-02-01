using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Story scene intro:
/// Fade in -> Wait -> Player walks in -> Dialogue -> Free to move (no dash/attack)
/// </summary>
public class StoryIntro : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform playerStartPosition;
    [SerializeField] private Transform playerEndPosition;
    [SerializeField] private float playerWalkSpeed = 2f;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 2f;
    [SerializeField] private float waitAfterFadeIn = 5f;
    [SerializeField] private float dialogueDelay = 0.5f;

    [Header("Dialogue")]
    [SerializeField] private CanvasGroup dialogueGroup;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private string[] dialogueLines;
    [SerializeField] private float timeBetweenLines = 2.5f;
    [SerializeField] private float dialogueFadeDuration = 0.5f;

    [Header("Boundaries")]
    [SerializeField] private Collider leftLimitCollider;

    private PlayerMovement playerMovement;
    private PlayerMeleeAttack playerMeleeAttack;
    private PlayerHealth playerHealth;

    private void Start()
    {
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            playerMeleeAttack = player.GetComponent<PlayerMeleeAttack>();
            playerHealth = player.GetComponent<PlayerHealth>();
        }

        StartCoroutine(RunStoryIntro());
    }

    private IEnumerator RunStoryIntro()
    {
        // Enter cinematic mode and disable all abilities
        if (playerMovement != null)
        {
            playerMovement.EnterCinematicMode();
            playerMovement.DisableDash();
        }

        if (playerMeleeAttack != null)
            playerMeleeAttack.DisableCombat();

        // Player is invincible for the entire tutorial scene
        if (playerHealth != null)
            playerHealth.EnableGodMode();

        // Disable left limit so player can walk in
        if (leftLimitCollider != null)
            leftLimitCollider.enabled = false;

        // Hide dialogue
        if (dialogueGroup != null)
            dialogueGroup.alpha = 0f;

        // Position player off-screen
        if (player != null && playerStartPosition != null)
            player.position = playerStartPosition.position;

        // Face right
        if (playerMovement != null)
            playerMovement.FaceRight();

        // Start with black screen
        if (SceneFader.Instance != null)
            SceneFader.Instance.SetBlack();

        yield return new WaitForSeconds(0.5f);

        // Fade in
        if (SceneFader.Instance != null)
            yield return SceneFader.Instance.FadeInCoroutine(fadeInDuration);

        // Wait
        yield return new WaitForSeconds(waitAfterFadeIn);

        // Player walks in
        yield return StartCoroutine(PlayerWalksIn());

        yield return new WaitForSeconds(dialogueDelay);

        // Dialogue
        if (dialogueLines != null && dialogueLines.Length > 0)
            yield return StartCoroutine(ShowDialogue());

        yield return new WaitForSeconds(0.5f);

        // Exit cinematic mode - player can move but dash/attack stay locked
        if (playerMovement != null)
            playerMovement.ExitCinematicMode();

        // Enable left limit to prevent player from going back
        if (leftLimitCollider != null)
            leftLimitCollider.enabled = true;
    }

    private IEnumerator PlayerWalksIn()
    {
        if (player == null || playerEndPosition == null)
            yield break;

        // Start walk animation
        if (playerMovement != null)
        {
            playerMovement.FaceRight();
            playerMovement.SetWalking(true);
        }

        Vector3 start = player.position;
        Vector3 end = playerEndPosition.position;
        float distance = Vector3.Distance(start, end);
        float duration = distance / playerWalkSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            player.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        // Stop walk animation
        if (playerMovement != null)
            playerMovement.SetWalking(false);
    }

    private IEnumerator ShowDialogue()
    {
        if (dialogueGroup == null || dialogueText == null)
            yield break;

        foreach (string line in dialogueLines)
        {
            dialogueText.text = line;
            yield return FadeCanvasGroup(dialogueGroup, 0f, 1f, dialogueFadeDuration);
            yield return new WaitForSeconds(timeBetweenLines);
            yield return FadeCanvasGroup(dialogueGroup, 1f, 0f, dialogueFadeDuration);
            yield return new WaitForSeconds(0.3f);
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
    }
}
