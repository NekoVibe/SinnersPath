using UnityEngine;

/// <summary>
/// Place this in EVERY scene for testing. 
/// Spawns persistent objects if they don't exist (when testing scene directly).
/// Auto-destroys if persistent objects already exist (normal game flow).
/// </summary>
public class SceneBootstrap : MonoBehaviour
{
    [Header("Persistent Prefabs (for testing)")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject uiPrefab;
    [SerializeField] private GameObject managersPrefab;
    [SerializeField] private GameObject sceneFaderPrefab;

    private void Awake()
    {
        // Spawn each missing persistent object individually
        bool spawnedAnything = false;

        if (PlayerPersistence.Instance == null && playerPrefab != null)
        {
            Instantiate(playerPrefab);
            Debug.Log("[Bootstrap] Spawned Player");
            spawnedAnything = true;
        }

        if (HUDManager.Instance == null && managersPrefab != null)
        {
            Instantiate(managersPrefab);
            Debug.Log("[Bootstrap] Spawned Managers");
            spawnedAnything = true;
        }

        if (uiPrefab != null && GameObject.Find("UI") == null)
        {
            Instantiate(uiPrefab);
            Debug.Log("[Bootstrap] Spawned UI");
            spawnedAnything = true;
        }

        if (SceneFader.Instance == null && sceneFaderPrefab != null)
        {
            Instantiate(sceneFaderPrefab);
            Debug.Log("[Bootstrap] Spawned SceneFader");
            spawnedAnything = true;
        }

        if (spawnedAnything)
            Debug.Log("[Bootstrap] Testing mode - spawned missing persistent objects");

        Destroy(gameObject);
    }
}
