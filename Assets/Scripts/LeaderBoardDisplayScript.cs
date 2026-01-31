using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LeaderboardDisplay : MonoBehaviour
{
	[Header("UI References")]
	[SerializeField] private TextMeshProUGUI titleText;
	[SerializeField] private TextMeshProUGUI[] rankTexts;      // "#1", "#2", etc.
	[SerializeField] private TextMeshProUGUI[] scoreTexts;     // Puntuación
	[SerializeField] private TextMeshProUGUI[] timeTexts;      // Tiempo
	[SerializeField] private TextMeshProUGUI[] dateTexts;      // Fecha (opcional)

	[Header("Settings")]
	[SerializeField] private int maxEntries = 10;
	[SerializeField] private bool autoRefresh = true;
	[SerializeField] private float refreshInterval = 5f;

	[Header("Colors")]
	[SerializeField] private Color topColor = new Color(1f, 0.84f, 0f);      // Oro
	[SerializeField] private Color secondColor = new Color(0.75f, 0.75f, 0.75f); // Plata
	[SerializeField] private Color thirdColor = new Color(0.8f, 0.5f, 0.2f);     // Bronce
	[SerializeField] private Color normalColor = Color.white;

	private float refreshTimer = 0f;

	private void Start()
	{
		UpdateLeaderboard();
	}

	private void Update()
	{
		if (autoRefresh)
		{
			refreshTimer += Time.deltaTime;

			if (refreshTimer >= refreshInterval)
			{
				UpdateLeaderboard();
				refreshTimer = 0f;
			}
		}
	}

	// Actualizar el leaderboard con los datos guardados
	public void UpdateLeaderboard()
	{
		if (SaveManager.Instance == null)
		{
			Debug.LogWarning("SaveManager not found!");
			return;
		}

		// Obtener las mejores runs
		List<RunData> topRuns = SaveManager.Instance.GetTopRuns(maxEntries);

		// Actualizar cada entrada
		for (int i = 0; i < maxEntries; i++)
		{
			if (i < topRuns.Count)
			{
				// Hay datos para esta posición
				RunData run = topRuns[i];

				// Rank
				if (rankTexts != null && i < rankTexts.Length && rankTexts[i] != null)
				{
					rankTexts[i].text = $"#{i + 1}";
					rankTexts[i].color = GetRankColor(i);
				}

				// Score
				if (scoreTexts != null && i < scoreTexts.Length && scoreTexts[i] != null)
				{
					scoreTexts[i].text = run.score.ToString();
					scoreTexts[i].color = GetRankColor(i);
				}

				// Time
				if (timeTexts != null && i < timeTexts.Length && timeTexts[i] != null)
				{
					timeTexts[i].text = run.timeElapsed;
					timeTexts[i].color = GetRankColor(i);
				}

				// Date (opcional)
				if (dateTexts != null && i < dateTexts.Length && dateTexts[i] != null)
				{
					dateTexts[i].text = run.date;
					dateTexts[i].color = GetRankColor(i);
				}
			}
			else
			{
				// No hay datos, mostrar vacío
				if (rankTexts != null && i < rankTexts.Length && rankTexts[i] != null)
				{
					rankTexts[i].text = $"#{i + 1}";
					rankTexts[i].color = normalColor * 0.3f; // Más oscuro
				}

				if (scoreTexts != null && i < scoreTexts.Length && scoreTexts[i] != null)
				{
					scoreTexts[i].text = "---";
					scoreTexts[i].color = normalColor * 0.3f;
				}

				if (timeTexts != null && i < timeTexts.Length && timeTexts[i] != null)
				{
					timeTexts[i].text = "--:--";
					timeTexts[i].color = normalColor * 0.3f;
				}

				if (dateTexts != null && i < dateTexts.Length && dateTexts[i] != null)
				{
					dateTexts[i].text = "";
					dateTexts[i].color = normalColor * 0.3f;
				}
			}
		}

		Debug.Log($"Leaderboard updated with {topRuns.Count} entries");
	}

	// Obtener el color según el rank
	private Color GetRankColor(int rank)
	{
		switch (rank)
		{
			case 0: return topColor;      // #1 - Oro
			case 1: return secondColor;   // #2 - Plata
			case 2: return thirdColor;    // #3 - Bronce
			default: return normalColor;  // Resto - Blanco
		}
	}

	// Forzar actualización (llamar desde otros scripts si es necesario)
	public void ForceRefresh()
	{
		UpdateLeaderboard();
	}

	// Debug: Actualizar desde el Inspector
	[ContextMenu("Update Leaderboard")]
	private void DebugUpdate()
	{
		UpdateLeaderboard();
	}
}
