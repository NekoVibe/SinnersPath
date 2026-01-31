using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
	public static PauseMenu Instance { get; private set; }

	[Header("UI References")]
	[SerializeField] private GameObject pauseMenuUI;

	[Header("Settings")]
	[SerializeField] private KeyCode pauseKey = KeyCode.Escape;
	[SerializeField] private bool canPause = true;

	private bool isPaused = false;

	private void Awake()
	{
		// Singleton
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject); // ✅ Hacer persistente entre escenas
	}

	private void Start()
	{
		// Asegurarse de que el menú esté oculto al inicio
		if (pauseMenuUI != null)
		{
			pauseMenuUI.SetActive(false);
		}

		// Asegurar que el juego no esté pausado
		ResumeGame();
	}

	private void Update()
	{
		// Detectar tecla de pausa
		if (Input.GetKeyDown(pauseKey) && canPause)
		{
			if (isPaused)
			{
				ResumeGame();
			}
			else
			{
				PauseGame();
			}
		}
	}

	// Pausar el juego
	public void PauseGame()
	{
		if (!canPause) return;

		isPaused = true;

		// Mostrar menú de pausa
		if (pauseMenuUI != null)
		{
			pauseMenuUI.SetActive(true);
		}

		// Pausar el tiempo del juego
		Time.timeScale = 0f;

		// Pausar el cronómetro si existe
		TimerManager timerManager = FindFirstObjectByType<TimerManager>();
		if (timerManager != null)
		{
			timerManager.setCronometro(false);
		}

		// Opcional: Mostrar cursor
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;

		Debug.Log("Game paused");
	}

	// Reanudar el juego
	public void ResumeGame()
	{
		isPaused = false;

		// Ocultar menú de pausa
		if (pauseMenuUI != null)
		{
			pauseMenuUI.SetActive(false);
		}

		// Reanudar el tiempo del juego
		Time.timeScale = 1f;

		// Reanudar el cronómetro si existe
		TimerManager timerManager = FindFirstObjectByType<TimerManager>();
		if (timerManager != null)
		{
			timerManager.setCronometro(true);
		}

		// Opcional: Ocultar cursor
		// Cursor.visible = false;
		// Cursor.lockState = CursorLockMode.Locked;

		Debug.Log("Game resumed");
	}

	// Reiniciar nivel actual
	public void RestartLevel()
	{
		ResumeGame(); // Importante: reanudar antes de reiniciar
		GameManager.Instance?.RestartGame();
	}

	// Volver al menú principal
	public void LoadMainMenu()
	{
		ResumeGame(); // Importante: reanudar antes de cargar menú
		GameManager.Instance?.LoadMainMenu();
	}

	// Salir del juego
	public void QuitGame()
	{
		Debug.Log("Quitting game...");

#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
	}

	// Habilitar/deshabilitar pausa (útil para cutscenes, game over, etc.)
	public void SetCanPause(bool value)
	{
		canPause = value;

		// Si se deshabilita mientras está pausado, reanudar
		if (!value && isPaused)
		{
			ResumeGame();
		}
	}

	// Getter
	public bool IsPaused()
	{
		return isPaused;
	}
}
