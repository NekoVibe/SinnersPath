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

    [Header("Controls UI (Botones de rebind)")]
    public Button btnHab1;
    public Button btnHab2;
    public Button btnHab3;
    public Button btnPrev;
    public Button btnNext;
    public Button btnReload;
    public Button btnPickUp;
    public Button btnDash;

    [Header("Settings")]
    public KeyCode pauseKey = KeyCode.Escape;
    public bool canPause = true;

    private bool isPaused = false;
    private bool isRebinding = false;

    private void Awake()
    {
    }

    private void Start()
    {
        // Asegurarse de que el menú esté oculto al inicio
        if (pauseMenuUI) pauseMenuUI.SetActive(false);
        if (optionsMenuUI) optionsMenuUI.SetActive(false);

        // Conectar botones de rebind automáticamente
        btnHab1?.onClick.AddListener(() => StartRebind("keyHab1"));
        btnHab2?.onClick.AddListener(() => StartRebind("keyHab2"));
        btnHab3?.onClick.AddListener(() => StartRebind("keyHab3"));
        btnPrev?.onClick.AddListener(() => StartRebind("keyPrevHab"));
        btnNext?.onClick.AddListener(() => StartRebind("keyNextHab"));
        btnReload?.onClick.AddListener(() => StartRebind("keyReload"));
        btnPickUp?.onClick.AddListener(() => StartRebind("keyPickUp"));
        btnDash?.onClick.AddListener(() => StartRebind("keyDash"));

        // Conectar sliders de volumen para actualizar en tiempo real
        sliderGeneral?.onValueChanged.AddListener(OnVolumeChanged);
        sliderMusic?.onValueChanged.AddListener(OnVolumeChanged);
        sliderEffects?.onValueChanged.AddListener(OnVolumeChanged);

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

    // Llamado cuando cualquier slider de volumen cambia
    private void OnVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            float general = sliderGeneral ? sliderGeneral.value : 1f;
            float music = sliderMusic ? sliderMusic.value : 0.7f;
            float effects = sliderEffects ? sliderEffects.value : 0.7f;

            AudioManager.Instance.UpdateVolumes(general, music, effects);
        }
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
        SetButtonText(btnHab1, data.keyHab1);
        SetButtonText(btnHab2, data.keyHab2);
        SetButtonText(btnHab3, data.keyHab3);
        SetButtonText(btnPrev, data.keyPrevHab);
        SetButtonText(btnNext, data.keyNextHab);
        SetButtonText(btnReload, data.keyReload);
        SetButtonText(btnPickUp, data.keyPickUp);
        SetButtonText(btnDash, data.keyDash);
    }

    private void SetButtonText(Button btn, string text)
    {
        if (btn) btn.GetComponentInChildren<TextMeshProUGUI>().text = text;
    }

    // ============================================
    // SISTEMA DE REBINDING (TECLAS)
    // ============================================

	private string FormatKeyName(string keyName)
	{
		// Números del teclado principal (Alpha1 → 1)
		if (keyName.StartsWith("Alpha"))
			return keyName.Replace("Alpha", "");
		
		// Números del teclado numérico (Numpad1 → 1)
		if (keyName.StartsWith("Keypad"))
			return keyName.Replace("Keypad", "");
		
		// Otras teclas comunes
		return keyName switch
		{
			"Space" => "Espacio",
			"LeftShift" => "Shift Izq",
			"RightShift" => "Shift Der",
			"LeftControl" => "Ctrl Izq",
			"RightControl" => "Ctrl Der",
			"LeftAlt" => "Alt Izq",
			"RightAlt" => "Alt Der",
			"Mouse0" => "Click Izq",
			"Mouse1" => "Click Der",
			"Mouse2" => "Click Central",
			"Return" => "Enter",
			"Escape" => "Esc",
			"BackQuote" => "º",
			_ => keyName
		};
	}

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
						AssignKey(data, actionName, FormatKeyName(k.ToString()));
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
        Button btn = action switch
        {
            "keyHab1" => btnHab1,
            "keyHab2" => btnHab2,
            "keyHab3" => btnHab3,
            "keyPrevHab" => btnPrev,
            "keyNextHab" => btnNext,
            "keyReload" => btnReload,
            "keyPickUp" => btnPickUp,
            "keyDash" => btnDash,
            _ => null
        };
        return btn?.GetComponentInChildren<TextMeshProUGUI>();
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

		// Pausar el cronómetro
		TimerManager.Instance?.setCronometro(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenuUI?.SetActive(false);
        optionsMenuUI?.SetActive(false);
        Time.timeScale = 1f;

		// Reanudar el cronómetro
		TimerManager.Instance?.setCronometro(true);

		// Opcional: Ocultar cursor
		// Cursor.visible = false;
		// Cursor.lockState = CursorLockMode.Locked;

		Debug.Log("Game resumed");
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