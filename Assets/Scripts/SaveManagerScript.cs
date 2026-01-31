using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

public class SaveManager : MonoBehaviour
{
	public static SaveManager Instance { get; private set; }

	private string saveFilePath;
	private const string SAVE_FILE_NAME = "leaderboard.json";

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

		// Ruta del archivo de guardado
		saveFilePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
		Debug.Log($"Save file path: {saveFilePath}");
	}

	// Guardar una nueva run en el leaderboard
	public void SaveRun(RunData newRun)
	{
		LeaderboardData leaderboard = LoadLeaderboard();

		// Añadir la nueva run
		leaderboard.runs.Add(newRun);

		// Ordenar por puntuación (mayor a menor)
		leaderboard.runs.Sort((a, b) => b.score.CompareTo(a.score));

		// Limitar a top 10 (opcional)
		if (leaderboard.runs.Count > 10)
		{
			leaderboard.runs.RemoveRange(10, leaderboard.runs.Count - 10);
		}

		// Guardar en JSON
		SaveLeaderboard(leaderboard);

		Debug.Log($"Run saved! Score: {newRun.score}, Time: {newRun.timeElapsed}");
	}

	// Cargar el leaderboard completo
	public LeaderboardData LoadLeaderboard()
	{
		if (!File.Exists(saveFilePath))
		{
			Debug.Log("No save file found. Creating new leaderboard.");
			return new LeaderboardData();
		}

		try
		{
			string json = File.ReadAllText(saveFilePath);
			LeaderboardData data = JsonUtility.FromJson<LeaderboardData>(json);
			Debug.Log($"Leaderboard loaded. Total runs: {data.runs.Count}");
			return data;
		}
		catch (Exception e)
		{
			Debug.LogError($"Error loading leaderboard: {e.Message}");
			return new LeaderboardData();
		}
	}

	// Guardar el leaderboard en el archivo JSON
	private void SaveLeaderboard(LeaderboardData leaderboard)
	{
		try
		{
			string json = JsonUtility.ToJson(leaderboard, true);
			File.WriteAllText(saveFilePath, json);
			Debug.Log("Leaderboard saved successfully!");
		}
		catch (Exception e)
		{
			Debug.LogError($"Error saving leaderboard: {e.Message}");
		}
	}

	// Obtener el top N de runs
	public List<RunData> GetTopRuns(int count = 10)
	{
		LeaderboardData leaderboard = LoadLeaderboard();

		int maxCount = Mathf.Min(count, leaderboard.runs.Count);
		return leaderboard.runs.GetRange(0, maxCount);
	}

	// Obtener la mejor puntuación
	public int GetHighScore()
	{
		LeaderboardData leaderboard = LoadLeaderboard();

		if (leaderboard.runs.Count == 0)
			return 0;

		return leaderboard.runs[0].score; // Ya está ordenado de mayor a menor
	}

	// Verificar si una puntuación entra en el top 10
	public bool IsTopScore(int score)
	{
		LeaderboardData leaderboard = LoadLeaderboard();

		// Si hay menos de 10 runs, siempre entra
		if (leaderboard.runs.Count < 10)
			return true;

		// Comparar con el puesto 10
		return score > leaderboard.runs[9].score;
	}

	// Borrar todos los datos (para testing)
	public void ClearAllData()
	{
		if (File.Exists(saveFilePath))
		{
			File.Delete(saveFilePath);
			Debug.Log("All save data deleted!");
		}
	}

	// Debug: Mostrar todas las runs en consola
	[ContextMenu("Debug: Show All Runs")]
	private void DebugShowAllRuns()
	{
		LeaderboardData leaderboard = LoadLeaderboard();

		Debug.Log($"=== LEADERBOARD ({leaderboard.runs.Count} runs) ===");

		for (int i = 0; i < leaderboard.runs.Count; i++)
		{
			RunData run = leaderboard.runs[i];
			Debug.Log($"#{i + 1} | Score: {run.score} | Time: {run.timeElapsed} | Date: {run.date}");
		}
	}

	// Debug: Añadir run de prueba
	[ContextMenu("Debug: Add Test Run")]
	private void DebugAddTestRun()
	{
		RunData testRun = new RunData
		{
			score = UnityEngine.Random.Range(100, 1000),
			timeElapsed = "05:32",
			date = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
		};

		SaveRun(testRun);
	}

	// Debug: Borrar todo
	[ContextMenu("Debug: Clear All Data")]
	private void DebugClearData()
	{
		ClearAllData();
	}
}

// ============================================
// CLASES DE DATOS
// ============================================

[System.Serializable]
public class LeaderboardData
{
	public List<RunData> runs = new List<RunData>();
}

[System.Serializable]
public class RunData
{
	public int score;           // Puntuación total
	public string timeElapsed;  // Tiempo en formato "MM:SS"
	public string date;         // Fecha de la run "dd/MM/yyyy HH:mm"

	// Opcional: Puedes añadir más datos si quieres
	// public int enemiesKilled;
	// public int coinsCollected;
	// public string playerName;
}
