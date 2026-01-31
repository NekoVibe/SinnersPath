using UnityEngine;

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
    private Rect windowRect = new Rect(20, 20, 280, 400);
    private Vector2 scrollPos;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
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

        // === PLAYER HEALTH ===
        GUILayout.Label("<b>Player Health</b>", GetHeaderStyle());

        var player = FindFirstObjectByType<PlayerHealth>();
        if (player != null)
        {
            GUILayout.Label($"Health: {player.currentHealth} / {player.maxHealth}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Take 1 Damage", GUILayout.Height(30)))
            {
                player.TakeDamage(1);
            }
            if (GUILayout.Button("Heal 1 HP", GUILayout.Height(30)))
            {
                player.Heal(1);
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Kill Player", GUILayout.Height(25)))
            {
                player.TakeDamage(player.currentHealth);
            }
            if (GUILayout.Button("Full Heal", GUILayout.Height(25)))
            {
                player.ResetHealth();
            }
        }
        else
        {
            GUILayout.Label("Player not found", GetWarningStyle());
        }

        GUILayout.Space(10);

        // === SCREEN EFFECTS ===
        GUILayout.Label("<b>Screen Effects</b>", GetHeaderStyle());

        if (ScreenEffects.Instance != null)
        {
            // Show status
            var flashOverlay = ScreenEffects.Instance.GetFlashOverlay();
            if (flashOverlay != null)
            {
                GUILayout.Label($"Overlay: OK (alpha: {flashOverlay.color.a:F2})", GetSuccessStyle());
            }
            else
            {
                GUILayout.Label("Overlay: MISSING!", GetWarningStyle());
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Damage Flash", GUILayout.Height(30)))
            {
                Debug.Log("DebugPanel: Triggering DamageFlash");
                ScreenEffects.Instance.DamageFlash();
            }
            if (GUILayout.Button("Heal Flash", GUILayout.Height(30)))
            {
                Debug.Log("DebugPanel: Triggering HealFlash");
                ScreenEffects.Instance.HealFlash();
            }
            GUILayout.EndHorizontal();

            // Custom flash
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("White Flash", GUILayout.Height(25)))
            {
                ScreenEffects.Instance.Flash(new Color(1f, 1f, 1f, 0.6f), 0.4f);
            }
            if (GUILayout.Button("Blue Flash", GUILayout.Height(25)))
            {
                ScreenEffects.Instance.Flash(new Color(0.2f, 0.4f, 1f, 0.5f), 0.35f);
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("ScreenEffects.Instance is NULL!", GetWarningStyle());
            if (GUILayout.Button("Try Find ScreenEffects", GUILayout.Height(25)))
            {
                var found = FindFirstObjectByType<ScreenEffects>();
                if (found != null)
                {
                    Debug.Log($"Found ScreenEffects on: {found.gameObject.name}");
                }
                else
                {
                    Debug.LogWarning("No ScreenEffects component in scene!");
                }
            }
        }

        GUILayout.Space(10);

        // === CAMERA SHAKE ===
        GUILayout.Label("<b>Camera Shake</b>", GetHeaderStyle());

        if (CameraShake.Instance != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Light", GUILayout.Height(30)))
            {
                CameraShake.Instance.ShakeLight();
            }
            if (GUILayout.Button("Normal", GUILayout.Height(30)))
            {
                CameraShake.Instance.Shake();
            }
            if (GUILayout.Button("Heavy", GUILayout.Height(30)))
            {
                CameraShake.Instance.ShakeHeavy();
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("CameraShake not found!", GetWarningStyle());
        }

        GUILayout.Space(10);

        // === GAME STATE ===
        GUILayout.Label("<b>Game State</b>", GetHeaderStyle());

        if (GameManager.Instance != null)
        {
            GUILayout.Label($"Coins: {GameManager.Instance.Coins}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+10 Coins", GUILayout.Height(25)))
            {
                GameManager.Instance.AddCoins(10);
            }
            if (GUILayout.Button("-5 Coins", GUILayout.Height(25)))
            {
                GameManager.Instance.SpendCoins(5);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (GUILayout.Button("Restart Game", GUILayout.Height(30)))
            {
                GameManager.Instance.RestartGame();
            }
        }
        else
        {
            GUILayout.Label("GameManager not found", GetWarningStyle());
        }

        GUILayout.Space(10);

        // === TIME CONTROL ===
        GUILayout.Label("<b>Time Control</b>", GetHeaderStyle());
        GUILayout.Label($"TimeScale: {Time.timeScale:F2}");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("0.1x", GUILayout.Height(25)))
        {
            Time.timeScale = 0.1f;
        }
        if (GUILayout.Button("0.5x", GUILayout.Height(25)))
        {
            Time.timeScale = 0.5f;
        }
        if (GUILayout.Button("1x", GUILayout.Height(25)))
        {
            Time.timeScale = 1f;
        }
        if (GUILayout.Button("2x", GUILayout.Height(25)))
        {
            Time.timeScale = 2f;
        }
        GUILayout.EndHorizontal();

        GUILayout.EndScrollView();

        // Make window draggable
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

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
