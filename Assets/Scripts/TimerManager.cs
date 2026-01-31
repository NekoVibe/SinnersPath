using UnityEngine;

public class TimerManager : MonoBehaviour
{
    [SerializeField] private HUDView hud;
    private float tiempoTranscurrido = 0f;
    private bool cronometroActivo = true;

    void Update()
    {
        if (cronometroActivo)
        {
            // Calcular el tiempo real
            tiempoTranscurrido += Time.deltaTime;

            // Convertir a formato MM:SS
            string tiempoParaMostrar = FormatearTiempo(tiempoTranscurrido);

            // Enviar al HUD
            hud.ActualizarReloj(tiempoParaMostrar);
        }
    }

    private string FormatearTiempo(float tiempoSegundos)
    {
        int minutos = Mathf.FloorToInt(tiempoSegundos / 60);
        int segundos = Mathf.FloorToInt(tiempoSegundos % 60);
        
        // Retorna el texto con formato de dos dígitos (00:00)
        return string.Format("{0:00}:{1:00}", minutos, segundos);
    }
    
    // Función extra por si se quiere pausar el tiempo al morir o ganar
    public void setCronometro(bool estado) => cronometroActivo = estado;
}