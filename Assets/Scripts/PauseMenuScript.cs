using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("UI References")]
    public GameObject pauseMenuUI;
    public GameObject optionsMenuUI;

    [Header("Audio UI (Sliders)")]
    public Slider sliderGeneral;
    public Slider sliderMusic;
    public Slider sliderEffects;

    [Header("Controls UI (Textos de botones)")]
    public TextMeshProUGUI txtHab1;
    public TextMeshProUGUI txtHab2, txtHab3, txtPrev, txtNext, txtReload, txtPickUp, txtDash;

    [Header("Settings")]
    public KeyCode pauseKey = KeyCode.Escape;
    public bool canPause = true;

    private bool isPaused = false;
    private bool isRebinding = false;

    private void Awake()
    {
        // Singleton original
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Asegurarse de que el menú esté oculto al inicio
        if (pauseMenuUI) pauseMenuUI.SetActive(false);
        if (optionsMenuUI) optionsMenuUI.SetActive(false);

        // Carga y aplica los ajustes locales (Sliders y Teclas) nada más empezar
        ApplySavedSettings();

        // Asegurar que el juego no esté pausado
        ResumeGame();
    }

    private void Update()
    {
        // Detectar tecla de pausa (bloqueado si estamos reasignando teclas)
        if (Input.GetKeyDown(pauseKey) && canPause && !isRebinding)
        {
            if (isPaused)
            {
                // Si la configuración está abierta, la cerramos primero
                if (optionsMenuUI != null && optionsMenuUI.activeSelf)
                {
                    CloseOptionsMenu();
                }
                else
                {
                    ResumeGame();
                }
            }
            else
            {
                PauseGame();
            }
        }
    }

    // ============================================
    // GESTIÓN DE AJUSTES (PERSISTENCIA)
    // ============================================

    public void ApplySavedSettings()
    {
        SettingsData data = SaveManager.Instance.LoadSettings();

        if (sliderGeneral) sliderGeneral.value = data.volGeneral;
        if (sliderMusic) sliderMusic.value = data.volMusic;
        if (sliderEffects) sliderEffects.value = data.volEffects;

        UpdateControlTexts(data);
    }

    public void SaveCurrentSettings()
    {
        if (isRebinding) return;

        SettingsData currentData = SaveManager.Instance.LoadSettings();
        
        if (sliderGeneral) currentData.volGeneral = sliderGeneral.value;
        if (sliderMusic) currentData.volMusic = sliderMusic.value;
        if (sliderEffects) currentData.volEffects = sliderEffects.value;

        SaveManager.Instance.SaveSettings(currentData);
    }

    public void OpenOptionsMenu()
    {
        ApplySavedSettings();
        optionsMenuUI?.SetActive(true);
    }

    public void CloseOptionsMenu()
    {
        if (isRebinding) return;
        SaveCurrentSettings(); 
        optionsMenuUI?.SetActive(false);
    }

    private void UpdateControlTexts(SettingsData data)
    {
        if (txtHab1) txtHab1.text = data.keyHab1;
        if (txtHab2) txtHab2.text = data.keyHab2;
        if (txtHab3) txtHab3.text = data.keyHab3;
        if (txtPrev) txtPrev.text = data.keyPrevHab;
        if (txtNext) txtNext.text = data.keyNextHab;
        if (txtReload) txtReload.text = data.keyReload;
        if (txtPickUp) txtPickUp.text = data.keyPickUp;
        if (txtDash) txtDash.text = data.keyDash;
    }

    // ============================================
    // SISTEMA DE REBINDING (TECLAS)
    // ============================================

    public void StartRebind(string actionName)
    {
        if (isRebinding) return;
        StartCoroutine(WaitForKeyPress(actionName));
    }

    private IEnumerator WaitForKeyPress(string actionName)
    {
        isRebinding = true;
        TextMeshProUGUI targetText = GetTextByActionName(actionName);
        if (targetText) targetText.text = "...";
        yield return null;

        bool keyPressed = false;
        while (!keyPressed)
        {
            if (Input.anyKeyDown)
            {
                foreach (KeyCode k in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(k) && k != KeyCode.Escape)
                    {
                        SettingsData data = SaveManager.Instance.LoadSettings();
                        AssignKey(data, actionName, k.ToString());
                        SaveManager.Instance.SaveSettings(data);
                        UpdateControlTexts(data);
                        keyPressed = true;
                    }
                }
            }
            yield return null;
        }
        isRebinding = false;
    }

    private void AssignKey(SettingsData data, string action, string keyName)
    {
        switch (action)
        {
            case "keyHab1": data.keyHab1 = keyName; break;
            case "keyHab2": data.keyHab2 = keyName; break;
            case "keyHab3": data.keyHab3 = keyName; break;
            case "keyPrevHab": data.keyPrevHab = keyName; break;
            case "keyNextHab": data.keyNextHab = keyName; break;
            case "keyReload": data.keyReload = keyName; break;
            case "keyPickUp": data.keyPickUp = keyName; break;
            case "keyDash": data.keyDash = keyName; break;
        }
    }

    private TextMeshProUGUI GetTextByActionName(string action)
    {
        return action switch {
            "keyHab1" => txtHab1, "keyHab2" => txtHab2, "keyHab3" => txtHab3,
            "keyPrevHab" => txtPrev, "keyNextHab" => txtNext, "keyReload" => txtReload,
            "keyPickUp" => txtPickUp, "keyDash" => txtDash, _ => null
        };
    }

    // ============================================
    // LÓGICA DE PAUSA (FUNCIONES ORIGINALES)
    // ============================================

    public void PauseGame()
    {
        if (!canPause) return;
        isPaused = true;
        pauseMenuUI?.SetActive(true);
        Time.timeScale = 0f;

        // Control del cronómetro original
        TimerManager timerManager = FindFirstObjectByType<TimerManager>();
        timerManager?.setCronometro(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenuUI?.SetActive(false);
        optionsMenuUI?.SetActive(false);
        Time.timeScale = 1f;

        // Control del cronómetro original
        TimerManager timerManager = FindFirstObjectByType<TimerManager>();
        timerManager?.setCronometro(true);
    }

    public void RestartLevel()
    {
        ResumeGame();
        GameManager.Instance?.RestartGame();
    }

    public void LoadMainMenu()
    {
        ResumeGame();
        GameManager.Instance?.LoadMainMenu();
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public void SetCanPause(bool value)
    {
        canPause = value;
        if (!value && isPaused)
        {
            ResumeGame();
        }
    }

    public bool IsPaused()
    {
        return isPaused;
    }
}