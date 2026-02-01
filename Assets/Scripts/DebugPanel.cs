using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// In-game debug panel for testing effects and game systems.
/// Toggle with F1 key. Only works in Editor and Development builds.
/// </summary>
public class DebugPanel : MonoBehaviour
{
    public static DebugPanel Instance { get; private set; }

    [Header("Toggle Key")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    private bool isVisible = false;
    private Rect windowRect = new Rect(20, 20, 360, 700);
    private Vector2 scrollPos;

    // Cached references
    private PlayerHealth cachedPlayerHealth;
    private PlayerMovement cachedPlayerMovement;
    private WeaponInventory cachedInventory;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
        }

        // Refresh references while panel is visible
        if (isVisible)
        {
            RefreshCachedReferences();
        }
    }

    private void RefreshCachedReferences()
    {
        if (PlayerPersistence.Instance != null)
        {
            cachedPlayerHealth = PlayerPersistence.Instance.GetComponent<PlayerHealth>();
            cachedPlayerMovement = PlayerPersistence.Instance.GetComponent<PlayerMovement>();
            cachedInventory = PlayerPersistence.Instance.GetComponent<WeaponInventory>();
        }
        else
        {
            cachedPlayerHealth = FindFirstObjectByType<PlayerHealth>();
            cachedPlayerMovement = FindFirstObjectByType<PlayerMovement>();
            cachedInventory = FindFirstObjectByType<WeaponInventory>();
        }
    }

    private void OnGUI()
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!isVisible) return;

        // Apply dark style
        GUI.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        windowRect = GUI.Window(0, windowRect, DrawWindow, "Debug Panel (F1)");
        #endif
    }

    private void DrawWindow(int windowID)
    {
        GUILayout.Space(5);
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        DrawPlayerSection();
        DrawMasksSection();
        DrawInventorySection();
        DrawGameStateSection();
        DrawSceneSkipSection();
        DrawScreenEffectsSection();
        DrawCameraShakeSection();
        DrawTimeControlSection();
        DrawDebugInfoSection();

        GUILayout.EndScrollView();

        // Make window draggable
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    // ============================================
    // PLAYER SECTION
    // ============================================
    private void DrawPlayerSection()
    {
        GUILayout.Label("<b>Player</b>", GetHeaderStyle());

        if (cachedPlayerHealth != null)
        {
            // Health display
            GUILayout.Label($"Health: {cachedPlayerHealth.currentHealth} / {cachedPlayerHealth.maxHealth}");

            // God Mode toggle
            bool godMode = cachedPlayerHealth.IsGodMode();
            GUI.color = godMode ? Color.green : Color.white;
            if (GUILayout.Button($"God Mode: {(godMode ? "ON" : "OFF")}", GUILayout.Height(25)))
            {
                if (godMode) cachedPlayerHealth.DisableGodMode();
                else cachedPlayerHealth.EnableGodMode();
            }
            GUI.color = Color.white;

            // Health controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-1 HP", GUILayout.Height(25)))
                cachedPlayerHealth.TakeDamage(1);
            if (GUILayout.Button("+1 HP", GUILayout.Height(25)))
                cachedPlayerHealth.Heal(1);
            if (GUILayout.Button("Full", GUILayout.Height(25)))
                cachedPlayerHealth.ResetHealth();
            if (GUILayout.Button("Kill", GUILayout.Height(25)))
                cachedPlayerHealth.TakeDamage(cachedPlayerHealth.currentHealth);
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("PlayerHealth not found", GetWarningStyle());
        }

        // Dash toggle
        if (cachedPlayerMovement != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable Dash", GUILayout.Height(25)))
                cachedPlayerMovement.EnableDash();
            if (GUILayout.Button("Disable Dash", GUILayout.Height(25)))
                cachedPlayerMovement.DisableDash();
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
    }

    // ============================================
    // MASKS SECTION
    // ============================================
    private void DrawMasksSection()
    {
        GUILayout.Label("<b>Masks (PlayerPrefs)</b>", GetHeaderStyle());

        bool r = PlayerPrefs.GetInt("Mask_Red", 0) == 1;
        bool b = PlayerPrefs.GetInt("Mask_Blue", 0) == 1;
        bool y = PlayerPrefs.GetInt("Mask_Yellow", 0) == 1;

        // Status display with colors
        GUILayout.BeginHorizontal();
        GUI.color = r ? Color.red : Color.gray;
        GUILayout.Label("Red:" + (r ? "Y" : "N"), GUILayout.Width(60));
        GUI.color = b ? Color.cyan : Color.gray;
        GUILayout.Label("Blue:" + (b ? "Y" : "N"), GUILayout.Width(60));
        GUI.color = y ? Color.yellow : Color.gray;
        GUILayout.Label("Yellow:" + (y ? "Y" : "N"), GUILayout.Width(70));
        GUI.color = Color.white;
        GUILayout.EndHorizontal();

        // Toggle buttons
        GUILayout.BeginHorizontal();
        GUI.color = r ? Color.red : Color.white;
        if (GUILayout.Button("Red", GUILayout.Height(25)))
        {
            PlayerPrefs.SetInt("Mask_Red", r ? 0 : 1);
            PlayerPrefs.Save();
        }
        GUI.color = b ? Color.cyan : Color.white;
        if (GUILayout.Button("Blue", GUILayout.Height(25)))
        {
            PlayerPrefs.SetInt("Mask_Blue", b ? 0 : 1);
            PlayerPrefs.Save();
        }
        GUI.color = y ? Color.yellow : Color.white;
        if (GUILayout.Button("Yellow", GUILayout.Height(25)))
        {
            PlayerPrefs.SetInt("Mask_Yellow", y ? 0 : 1);
            PlayerPrefs.Save();
        }
        GUI.color = Color.white;
        GUILayout.EndHorizontal();

        // Unlock All / Clear All
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Unlock All", GUILayout.Height(25)))
        {
            PlayerPrefs.SetInt("Mask_Red", 1);
            PlayerPrefs.SetInt("Mask_Blue", 1);
            PlayerPrefs.SetInt("Mask_Yellow", 1);
            PlayerPrefs.Save();
            Debug.Log("[DebugPanel] All masks unlocked in PlayerPrefs");
        }
        if (GUILayout.Button("Clear All", GUILayout.Height(25)))
        {
            PlayerPrefs.SetInt("Mask_Red", 0);
            PlayerPrefs.SetInt("Mask_Blue", 0);
            PlayerPrefs.SetInt("Mask_Yellow", 0);
            PlayerPrefs.Save();
            Debug.Log("[DebugPanel] All masks cleared from PlayerPrefs");
        }
        GUILayout.EndHorizontal();

        // Force apply masks to inventory now
        if (GUILayout.Button("Apply Masks to Inventory NOW", GUILayout.Height(25)))
        {
            ForceApplyMasksToInventory();
        }

        GUILayout.Space(10);
    }

    private void ForceApplyMasksToInventory()
    {
        if (PlayerPersistence.Instance == null)
        {
            Debug.LogWarning("[DebugPanel] PlayerPersistence not found!");
            return;
        }

        // Call the public method to restore masks
        PlayerPersistence.Instance.RestoreUnlockedMasks();
        Debug.Log("[DebugPanel] Force applied masks to inventory");
    }

    // ============================================
    // INVENTORY SECTION
    // ============================================
    private void DrawInventorySection()
    {
        GUILayout.Label("<b>Inventory</b>", GetHeaderStyle());

        if (cachedInventory != null)
        {
            var weapon = cachedInventory.GetCurrentWeapon();
            string weaponName = weapon != null ? weapon.weaponName : "None";
            string ammoInfo = cachedInventory.GetCurrentAmmoInfo();

            GUILayout.Label($"Weapon: {weaponName} | Ammo: {ammoInfo}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Prev", GUILayout.Height(25)))
                cachedInventory.PreviousWeapon();
            if (GUILayout.Button("Next", GUILayout.Height(25)))
                cachedInventory.NextWeapon();
            if (GUILayout.Button("Clear All", GUILayout.Height(25)))
                cachedInventory.ClearInventory();
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("WeaponInventory not found", GetWarningStyle());
        }

        GUILayout.Space(10);
    }

    // ============================================
    // GAME STATE SECTION
    // ============================================
    private void DrawGameStateSection()
    {
        GUILayout.Label("<b>Game State</b>", GetHeaderStyle());

        if (GameManager.Instance != null)
        {
            GUILayout.Label($"Coins: {GameManager.Instance.Coins} | Points: {GameManager.Instance.GetFinalScore()} | Combo: x{GameManager.Instance.Combo}");

            // Coins
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+10$", GUILayout.Height(25)))
                GameManager.Instance.AddCoins(10);
            if (GUILayout.Button("-10$", GUILayout.Height(25)))
                GameManager.Instance.SpendCoins(10);
            if (GUILayout.Button("+100 Pts", GUILayout.Height(25)))
                GameManager.Instance.AddPoints(100);
            if (GUILayout.Button("Reset Combo", GUILayout.Height(25)))
                GameManager.Instance.ResetCombo();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Game flow controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Restart", GUILayout.Height(25)))
                GameManager.Instance.RestartGame();
            if (GUILayout.Button("Game Over", GUILayout.Height(25)))
                GameManager.Instance.GameOver();
            if (GUILayout.Button("Victory", GUILayout.Height(25)))
                GameManager.Instance.Victory();
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("GameManager not found", GetWarningStyle());
        }

        GUILayout.Space(10);
    }

    // ============================================
    // SCENE SKIP SECTION
    // ============================================
    private void DrawSceneSkipSection()
    {
        GUILayout.Label("<b>Scene Skip</b>", GetHeaderStyle());

        string[] scenes = { "Intro", "Story", "Tutorial", "Level1", "Rest", "Outro" };

        // Row 1
        GUILayout.BeginHorizontal();
        for (int i = 0; i < 3 && i < scenes.Length; i++)
        {
            if (GUILayout.Button(scenes[i], GUILayout.Height(25)))
                LoadScene(scenes[i]);
        }
        GUILayout.EndHorizontal();

        // Row 2
        GUILayout.BeginHorizontal();
        for (int i = 3; i < scenes.Length; i++)
        {
            if (GUILayout.Button(scenes[i], GUILayout.Height(25)))
                LoadScene(scenes[i]);
        }
        GUILayout.EndHorizontal();

        // Reload current scene button
        if (GUILayout.Button("Reload Current Scene", GUILayout.Height(25)))
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }

        GUILayout.Space(10);
    }

    private void LoadScene(string sceneName)
    {
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeToScene(sceneName, 0.3f);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    // ============================================
    // SCREEN EFFECTS SECTION
    // ============================================
    private void DrawScreenEffectsSection()
    {
        GUILayout.Label("<b>Screen Effects</b>", GetHeaderStyle());

        if (ScreenEffects.Instance != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Damage Flash", GUILayout.Height(25)))
                ScreenEffects.Instance.DamageFlash();
            if (GUILayout.Button("Heal Flash", GUILayout.Height(25)))
                ScreenEffects.Instance.HealFlash();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("White", GUILayout.Height(25)))
                ScreenEffects.Instance.Flash(new Color(1f, 1f, 1f, 0.6f), 0.4f);
            if (GUILayout.Button("Blue", GUILayout.Height(25)))
                ScreenEffects.Instance.Flash(new Color(0.2f, 0.4f, 1f, 0.5f), 0.35f);
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("ScreenEffects not found", GetWarningStyle());
        }

        GUILayout.Space(10);
    }

    // ============================================
    // CAMERA SHAKE SECTION
    // ============================================
    private void DrawCameraShakeSection()
    {
        GUILayout.Label("<b>Camera Shake</b>", GetHeaderStyle());

        if (CameraShake.Instance != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Light", GUILayout.Height(25)))
                CameraShake.Instance.ShakeLight();
            if (GUILayout.Button("Normal", GUILayout.Height(25)))
                CameraShake.Instance.Shake();
            if (GUILayout.Button("Heavy", GUILayout.Height(25)))
                CameraShake.Instance.ShakeHeavy();
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("CameraShake not found", GetWarningStyle());
        }

        GUILayout.Space(10);
    }

    // ============================================
    // TIME CONTROL SECTION
    // ============================================
    private void DrawTimeControlSection()
    {
        GUILayout.Label("<b>Time Control</b>", GetHeaderStyle());
        GUILayout.Label($"TimeScale: {Time.timeScale:F2}x");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("0x", GUILayout.Height(25)))
            Time.timeScale = 0f;
        if (GUILayout.Button("0.1x", GUILayout.Height(25)))
            Time.timeScale = 0.1f;
        if (GUILayout.Button("0.5x", GUILayout.Height(25)))
            Time.timeScale = 0.5f;
        if (GUILayout.Button("1x", GUILayout.Height(25)))
            Time.timeScale = 1f;
        if (GUILayout.Button("2x", GUILayout.Height(25)))
            Time.timeScale = 2f;
        if (GUILayout.Button("4x", GUILayout.Height(25)))
            Time.timeScale = 4f;
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
    }

    // ============================================
    // DEBUG INFO SECTION
    // ============================================
    private void DrawDebugInfoSection()
    {
        GUILayout.Label("<b>Debug Info</b>", GetHeaderStyle());

        // FPS
        float fps = 1f / Time.unscaledDeltaTime;
        GUI.color = fps >= 60 ? Color.green : (fps >= 30 ? Color.yellow : Color.red);
        GUILayout.Label($"FPS: {fps:F0}");
        GUI.color = Color.white;

        // Scene
        GUILayout.Label($"Scene: {SceneManager.GetActiveScene().name}");

        // Singleton status
        GUILayout.BeginHorizontal();
        DrawStatusDot("Player", PlayerPersistence.Instance != null);
        DrawStatusDot("Game", GameManager.Instance != null);
        DrawStatusDot("HUD", HUDManager.Instance != null);
        DrawStatusDot("Fader", SceneFader.Instance != null);
        GUILayout.EndHorizontal();

        // Force rebind HUD UI
        if (HUDManager.Instance != null)
        {
            if (GUILayout.Button("Force Rebind HUD UI", GUILayout.Height(25)))
            {
                HUDManager.Instance.ForceRebindUI();
                Debug.Log("[DebugPanel] Force rebind HUD UI triggered");
            }
        }

        GUILayout.Space(10);
    }

    private void DrawStatusDot(string label, bool isActive)
    {
        GUI.color = isActive ? Color.green : Color.red;
        GUILayout.Label($"{label}:{(isActive ? "OK" : "X")}", GUILayout.Width(70));
        GUI.color = Color.white;
    }

    // ============================================
    // STYLES
    // ============================================
    private GUIStyle GetHeaderStyle()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.richText = true;
        style.fontSize = 14;
        return style;
    }

    private GUIStyle GetWarningStyle()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.yellow;
        style.fontStyle = FontStyle.Italic;
        return style;
    }

    private GUIStyle GetSuccessStyle()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.green;
        return style;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
