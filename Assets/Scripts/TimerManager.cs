using UnityEngine;

public class TimerManager : MonoBehaviour
{
	public static TimerManager Instance { get; private set; }

	private float tiempoTranscurrido = 0f;
	private bool cronometroActivo = true;

	private void Awake()
	{
		// Singleton
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
		// Solo se ejecuta la primera vez
		ResetTimer();
		StartTimer();
	}

	void Update()
	{
		if (cronometroActivo)
		{
			tiempoTranscurrido += Time.deltaTime;
			string tiempoParaMostrar = FormatearTiempo(tiempoTranscurrido);
			ActualizarHUD(tiempoParaMostrar);
		}
	}

	private string FormatearTiempo(float tiempoSegundos)
	{
		int minutos = Mathf.FloorToInt(tiempoSegundos / 60);
		int segundos = Mathf.FloorToInt(tiempoSegundos % 60);
		return string.Format("{0:00}:{1:00}", minutos, segundos);
	}

	private void ActualizarHUD(string tiempo)
	{
		if (HUDManager.Instance != null)
		{
			HUDManager.Instance.ActualizarReloj(tiempo);
		}
	}

	// Resetear el cronómetro a 0
	public void ResetTimer()
	{
		tiempoTranscurrido = 0f;
		ActualizarHUD("00:00");
		Debug.Log("Timer reset to 0");
	}

	// Iniciar el cronómetro
	public void StartTimer()
	{
		cronometroActivo = true;
		Debug.Log("Timer started");
	}

	// Detener el cronómetro
	public void StopTimer()
	{
		cronometroActivo = false;
		Debug.Log("Timer stopped");
	}

	// Pausar/Reanudar el cronómetro
	public void setCronometro(bool estado)
	{
		cronometroActivo = estado;
	}

	// Obtener el tiempo actual
	public float GetCurrentTime()
	{
		return tiempoTranscurrido;
	}
}
