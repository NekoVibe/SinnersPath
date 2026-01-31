using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LeaderboardDisplay : MonoBehaviour
{
	[Header("Template System")]
	[SerializeField] private LeaderboardEntry entryTemplate;  // Plantilla (se oculta automáticamente)
	[SerializeField] private Transform entriesContainer;      // Contenedor donde se crean las filas

	[Header("UI References (opcional)")]
	[SerializeField] private TextMeshProUGUI titleText;

	[Header("Settings")]
	[SerializeField] private int maxEntries = 10;
	[SerializeField] private bool autoRefresh = true;
	[SerializeField] private float refreshInterval = 5f;

	[Header("Colors")]
	[SerializeField] private Color topColor = new Color(1f, 0.84f, 0f);         // Oro
	[SerializeField] private Color secondColor = new Color(0.75f, 0.75f, 0.75f); // Plata
	[SerializeField] private Color thirdColor = new Color(0.8f, 0.5f, 0.2f);     // Bronce
	[SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f); // Gris oscuro

	private List<LeaderboardEntry> spawnedEntries = new List<LeaderboardEntry>();
	private float refreshTimer = 0f;

	private void Start()
	{
		// Ocultar la plantilla
		if (entryTemplate != null)
		{
			entryTemplate.gameObject.SetActive(false);
		}

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

	public void UpdateLeaderboard()
	{
		if (SaveManager.Instance == null)
		{
			Debug.LogWarning("SaveManager not found!");
			return;
		}

		if (entryTemplate == null || entriesContainer == null)
		{
			Debug.LogWarning("Entry template or container not assigned!");
			return;
		}

		// Obtener las mejores runs
		List<RunData> topRuns = SaveManager.Instance.GetTopRuns(maxEntries);

		// Crear más filas si es necesario
		while (spawnedEntries.Count < topRuns.Count)
		{
			LeaderboardEntry newEntry = Instantiate(entryTemplate, entriesContainer);
			spawnedEntries.Add(newEntry);
		}

		// Actualizar y mostrar/ocultar filas
		for (int i = 0; i < spawnedEntries.Count; i++)
		{
			if (i < topRuns.Count)
			{
				// Mostrar y actualizar
				spawnedEntries[i].gameObject.SetActive(true);
				Color rankColor = GetRankColor(i, topRuns.Count);
				spawnedEntries[i].SetData(i + 1, topRuns[i], rankColor);
			}
			else
			{
				// Ocultar filas sobrantes
				spawnedEntries[i].gameObject.SetActive(false);
			}
		}

		Debug.Log($"Leaderboard updated with {topRuns.Count} entries");
	}

	private Color GetRankColor(int rank, int totalEntries)
	{
		if (totalEntries == 1)
		{
			return topColor;
		}

		if (totalEntries == 2)
		{
			return rank == 0 ? topColor : secondColor;
		}

		return rank switch
		{
			0 => topColor,
			1 => secondColor,
			2 => thirdColor,
			_ => normalColor,
		};
	}

	public void ForceRefresh()
	{
		UpdateLeaderboard();
	}

	[ContextMenu("Update Leaderboard")]
	private void DebugUpdate()
	{
		UpdateLeaderboard();
	}
}
