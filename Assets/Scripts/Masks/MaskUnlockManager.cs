using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MaskUnlockManager : MonoBehaviour
{
    public enum MaskType { None, Red, Blue, Yellow }

    [Header("Settings")]
    [SerializeField] private MaskType maskToUnlock;
    [SerializeField] private Sprite maskIcon;
    [SerializeField] private GameObject maskWeaponPrefab; // The actual weapon prefab to add to inventory

    [Header("Mask Obtained UI")]
    [SerializeField] private CanvasGroup obtainedUI;
    [SerializeField] private Image maskImage;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float displayTime = 2f;

    [Header("Post-Unlock Dialogue")]
    [SerializeField] private CanvasGroup dialogueGroup;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private string[] postUnlockDialogue;
    [SerializeField] private float timeBetweenLines = 2.5f;

    [Header("Exit Door")]
    [SerializeField] private GameObject exitDoorPrefab;
    [SerializeField] private Transform doorSpawnPoint;
    [SerializeField] private ParticleSystem doorSpawnEffect;
    [SerializeField] private SceneReference nextScene;

    [Header("Post-Unlock Sequence")]
    [SerializeField] private float playerWalkSpeed = 2f;
    [SerializeField] private float cameraFocusDuration = 1.5f;

    [Header("Tracking")]
    [SerializeField] private bool autoStartTracking = false; // Enable for non-tutorial levels

    private bool unlocked = false;
    private bool tracking = false;
    private GameObject spawnedDoor;
    private Transform player; // Found at runtime via PlayerPersistence singleton

    void Start()
    {
        if (autoStartTracking)
        {
            StartTracking();
        }
    }

    public void StartTracking()
    {
        tracking = true;
        Debug.Log("[MaskUnlock] StartTracking called!");
    }

    void Update()
    {
        if (!tracking || unlocked) return;

        if (EnemyManager.Instance == null)
        {
            Debug.LogWarning("[MaskUnlock] EnemyManager.Instance is null!");
            return;
        }

        // Wait for all spawners to finish spawning before checking
        if (!EnemyManager.Instance.AllSpawnersCompleted())
        {
            return;
        }

        int aliveCount = EnemyManager.Instance.GetAliveEnemyCount();
        if (aliveCount == 0)
        {
            Debug.Log("[MaskUnlock] All enemies dead! Unlocking mask...");
            UnlockMask();
        }
    }

    void UnlockMask()
    {
        unlocked = true;
        PlayerPrefs.SetInt("Mask_" + maskToUnlock, 1);
        PlayerPrefs.Save();
        Debug.Log($"[MaskUnlock] Mask {maskToUnlock} saved to PlayerPrefs!");

        // Add mask to player inventory immediately
        if (maskWeaponPrefab != null && PlayerPersistence.Instance != null)
        {
            WeaponInventory inventory = PlayerPersistence.Instance.GetComponent<WeaponInventory>();
            if (inventory != null)
            {
                inventory.AddWeapon(maskWeaponPrefab);
                Debug.Log($"[MaskUnlock] Mask {maskToUnlock} added to inventory!");
            }
        }

        StartCoroutine(ShowObtainedUI());
    }

    IEnumerator ShowObtainedUI()
    {
        Debug.Log($"[MaskUnlock] ShowObtainedUI - obtainedUI: {obtainedUI}, maskImage: {maskImage}");

        // Find player via singleton (persistent across scenes)
        if (PlayerPersistence.Instance != null)
            player = PlayerPersistence.Instance.transform;

        // Get player components
        PlayerMovement playerMovement = player?.GetComponent<PlayerMovement>();

        // Show mask obtained UI
        if (maskImage != null)
            maskImage.sprite = maskIcon;

        yield return FadeCanvasGroup(obtainedUI, 0f, 1f, fadeDuration);
        yield return new WaitForSeconds(displayTime);
        yield return FadeCanvasGroup(obtainedUI, 1f, 0f, fadeDuration);

        yield return new WaitForSeconds(0.5f);

        // 1. Lock movement + walk player to door's Z position
        playerMovement?.EnterCinematicMode();
        playerMovement?.FaceRight();
        yield return WalkPlayerToDoor(playerMovement);

        // 2. Show dialogue
        yield return ShowPostUnlockDialogue();

        // 3. Focus camera on door spawn point FIRST (so player sees the spawn)
        yield return FocusCameraOnDoorSpawnPoint();

        // 4. Spawn door + play effect (player is now watching)
        SpawnExitDoor();

        // 5. Wait for door appear animation (0.8s in DoorAppearAnimation)
        yield return new WaitForSeconds(1.2f);

        // 6. Focus camera back on player
        yield return FocusCameraOnPlayer();

        // 6. Unlock movement
        playerMovement?.ExitCinematicMode();
        Debug.Log("[MaskUnlock] Post-unlock sequence complete. Player can now move.");
    }

    IEnumerator ShowPostUnlockDialogue()
    {
        if (dialogueGroup == null || dialogueText == null || postUnlockDialogue == null)
            yield break;

        foreach (string line in postUnlockDialogue)
        {
            dialogueText.text = line;
            yield return FadeCanvasGroup(dialogueGroup, 0f, 1f, fadeDuration);
            yield return new WaitForSeconds(timeBetweenLines);
            yield return FadeCanvasGroup(dialogueGroup, 1f, 0f, fadeDuration);
            yield return new WaitForSeconds(0.3f);
        }
    }

    IEnumerator WalkPlayerToDoor(PlayerMovement pm)
    {
        if (player == null || doorSpawnPoint == null) yield break;

        pm?.SetWalking(true);

        Vector3 start = player.position;
        // Walk to door's Z position, but 10m to the left (X - 10) to have a good view
        Vector3 end = new Vector3(doorSpawnPoint.position.x - 10f, player.position.y, doorSpawnPoint.position.z);
        float distance = Vector3.Distance(start, end);
        float duration = distance / 10;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            player.position = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        pm?.SetWalking(false);
        Debug.Log("[MaskUnlock] Player walked to door position.");
    }

    void SpawnExitDoor()
    {
        if (exitDoorPrefab == null || doorSpawnPoint == null)
        {
            Debug.LogWarning("[MaskUnlock] Exit door prefab or spawn point not assigned!");
            return;
        }

        // Play spawn effect
        if (doorSpawnEffect != null)
            doorSpawnEffect.Play();

        // Spawn the door
        spawnedDoor = Instantiate(exitDoorPrefab, doorSpawnPoint.position, doorSpawnPoint.rotation);

        // Set target scene on the portal
        LevelPortal portal = spawnedDoor.GetComponent<LevelPortal>();
        if (portal != null && nextScene.IsValid)
        {
            portal.SetTargetScene(nextScene.SceneName);
        }

        Debug.Log("[MaskUnlock] Exit door spawned!");
    }

    IEnumerator FocusCameraOnDoorSpawnPoint()
    {
        CameraFollow cam = Camera.main?.GetComponent<CameraFollow>();
        if (cam == null || doorSpawnPoint == null) yield break;

        cam.SetTarget(doorSpawnPoint);
        Debug.Log("[MaskUnlock] Camera focusing on door spawn point...");
        yield return new WaitForSeconds(cameraFocusDuration);
    }

    IEnumerator FocusCameraOnPlayer()
    {
        CameraFollow cam = Camera.main?.GetComponent<CameraFollow>();
        if (cam == null || player == null) yield break;

        cam.SetTarget(player);
        Debug.Log("[MaskUnlock] Camera focusing back on player...");
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

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
