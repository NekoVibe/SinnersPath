using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Trigger zone that starts the combat tutorial:
/// Player enters -> "..." -> Looks behind -> Enemy spawns -> Combat unlocked
/// </summary>
public class CombatTutorialTrigger : MonoBehaviour
{
    [Header("Enemy")]
    [SerializeField] private GameObject oniPrefab;
    [SerializeField] private float enemySpawnDistanceBehind = 15f;
    [SerializeField] private float enemyApproachDistance = 5f;
    [SerializeField] private float enemyApproachSpeed = 3f;

    [Header("Timing")]
    [SerializeField] private float pauseBeforeSuspicion = 0.5f;
    [SerializeField] private float lookBehindDuration = 1.5f;
    [SerializeField] private float waitAfterEnemyApproach = 0.5f;

    [Header("Dialogue")]
    [SerializeField] private CanvasGroup dialogueGroup;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private string suspicionDialogue = "...";
    [SerializeField] private float dialogueFadeDuration = 0.5f;
    [SerializeField] private float dialogueDisplayTime = 1.5f;

    private bool triggered = false;
    private GameObject spawnedEnemy;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;
            StartCoroutine(RunCombatTutorial(other.transform));
        }
    }

    private IEnumerator RunCombatTutorial(Transform player)
    {
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        PlayerMeleeAttack playerMeleeAttack = player.GetComponent<PlayerMeleeAttack>();

        // Enter cinematic mode and stop movement
        if (playerMovement != null)
        {
            playerMovement.EnterCinematicMode();
            playerMovement.ResetVelocity();
            playerMovement.SetWalking(false);
        }

        // Brief pause
        yield return new WaitForSeconds(pauseBeforeSuspicion);

        // Show "..." dialogue
        yield return StartCoroutine(ShowSuspicionDialogue());

        // Player looks behind
        if (playerMovement != null)
            playerMovement.FaceLeft();

        yield return new WaitForSeconds(lookBehindDuration);

        // Spawn and approach enemy
        yield return StartCoroutine(SpawnAndApproachEnemy(player));

        yield return new WaitForSeconds(waitAfterEnemyApproach);

        // Unlock combat!
        if (playerMeleeAttack != null)
            playerMeleeAttack.EnableCombat();

        // Start tracking for mask unlock
        FindObjectOfType<MaskUnlockManager>()?.StartTracking();

        // Exit cinematic mode
        if (playerMovement != null)
            playerMovement.ExitCinematicMode();

        // Disable this trigger
        gameObject.SetActive(false);
    }

    private IEnumerator ShowSuspicionDialogue()
    {
        if (dialogueGroup == null || dialogueText == null)
            yield break;

        dialogueText.text = suspicionDialogue;
        yield return FadeCanvasGroup(dialogueGroup, 0f, 1f, dialogueFadeDuration);
        yield return new WaitForSeconds(dialogueDisplayTime);
        yield return FadeCanvasGroup(dialogueGroup, 1f, 0f, dialogueFadeDuration);
    }

    private IEnumerator SpawnAndApproachEnemy(Transform player)
    {
        if (oniPrefab == null || player == null)
            yield break;

        // Spawn enemy to the left of the player (behind them)
        Vector3 spawnPos = player.position + Vector3.left * enemySpawnDistanceBehind;
        spawnedEnemy = Instantiate(oniPrefab, spawnPos, Quaternion.identity);

        // Set tutorial enemy health to 10
        EnemyBase enemyBase = spawnedEnemy.GetComponent<EnemyBase>();
        if (enemyBase != null)
        {
            enemyBase.SetHealth(10);
            EnemyManager.Instance?.RegisterEnemy(enemyBase);
        }

        // Target position: approach the player from behind
        Vector3 approachPos = player.position + Vector3.left * enemyApproachDistance;

        // Move enemy towards player
        float distance = Vector3.Distance(spawnPos, approachPos);
        float duration = distance / enemyApproachSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            spawnedEnemy.transform.position = Vector3.Lerp(spawnPos, approachPos, t);
            yield return null;
        }

        spawnedEnemy.transform.position = approachPos;
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

    private void OnDrawGizmos()
    {
        // Draw trigger zone in editor
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
