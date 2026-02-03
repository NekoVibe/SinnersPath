using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

public class SaveManager : MonoBehaviour
{
	public static SaveManager Instance { get; private set; }

	private string saveFilePath;
	private const string SAVE_FILE_NAME = "game_data.json";

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);

		saveFilePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
		Debug.Log($"Save file path: {saveFilePath}");
	}

	// ============================================
	// GESTIÓN DE CONFIGURACIÓN
	// ============================================

	public void SaveSettings(SettingsData newSettings)
	{
		GameData data = LoadAllData();
		data.settings = newSettings;
		SaveAllData(data);
	}

    public SettingsData LoadSettings()
    {
        SettingsData settings = LoadAllData().settings;

        // Aplicar valores por defecto si están vacíos (compatibilidad con saves antiguos)
        SettingsData defaults = new SettingsData();
        if (string.IsNullOrEmpty(settings.keyHab1)) settings.keyHab1 = defaults.keyHab1;
        if (string.IsNullOrEmpty(settings.keyHab2)) settings.keyHab2 = defaults.keyHab2;
        if (string.IsNullOrEmpty(settings.keyHab3)) settings.keyHab3 = defaults.keyHab3;
        if (string.IsNullOrEmpty(settings.keyPrevHab)) settings.keyPrevHab = defaults.keyPrevHab;
        if (string.IsNullOrEmpty(settings.keyNextHab)) settings.keyNextHab = defaults.keyNextHab;
        if (string.IsNullOrEmpty(settings.keyReload)) settings.keyReload = defaults.keyReload;
        if (string.IsNullOrEmpty(settings.keyPickUp)) settings.keyPickUp = defaults.keyPickUp;
        if (string.IsNullOrEmpty(settings.keyDash)) settings.keyDash = defaults.keyDash;

        return settings;
    }

	// ============================================
	// GESTIÓN DE LEADERBOARD (PUNTUACIONES)
	// ============================================

	public void SaveRun(RunData newRun)
	{
		GameData data = LoadAllData();
		data.leaderboard.runs.Add(newRun);

		// Ordenar de mayor a menor puntuación
		data.leaderboard.runs.Sort((a, b) => b.score.CompareTo(a.score));

		// Mantener solo el Top 10
		if (data.leaderboard.runs.Count > 10)
		{
			data.leaderboard.runs.RemoveRange(10, data.leaderboard.runs.Count - 10);
		}

		SaveAllData(data);
		Debug.Log($"Run saved! Score: {newRun.score}, Time: {newRun.timeElapsed}");
	}

	public LeaderboardData LoadLeaderboard()
	{
		GameData data = LoadAllData();
		Debug.Log($"Leaderboard loaded. Total runs: {data.leaderboard.runs.Count}");
		return data.leaderboard;
	}

	public List<RunData> GetTopRuns(int count = 10)
	{
		GameData data = LoadAllData();
		int maxCount = Mathf.Min(count, data.leaderboard.runs.Count);
		return data.leaderboard.runs.GetRange(0, maxCount);
	}

	public int GetHighScore()
	{
		GameData data = LoadAllData();

		if (data.leaderboard.runs.Count == 0)
			return 0;

		return data.leaderboard.runs[0].score; // Ya está ordenado de mayor a menor
	}

	public bool IsTopScore(int score)
	{
		GameData data = LoadAllData();

		// Si hay menos de 10 runs, siempre entra
		if (data.leaderboard.runs.Count < 10)
			return true;

		// Comparar con el puesto 10
		return score > data.leaderboard.runs[9].score;
	}

	public void ClearAllData()
	{
		if (File.Exists(saveFilePath))
		{
			File.Delete(saveFilePath);
			Debug.Log("All save data deleted!");
		}
	}

	// ============================================
	// NÚCLEO DE PERSISTENCIA (JSON)
	// ============================================

	private GameData LoadAllData()
	{
		if (!File.Exists(saveFilePath))
		{
			Debug.Log("No save file found. Creating new data.");
			return new GameData();
		}

		try
		{
			string json = File.ReadAllText(saveFilePath);
			GameData data = JsonUtility.FromJson<GameData>(json);
			return data;
		}
		catch (Exception e)
		{
			Debug.LogError($"Error loading data: {e.Message}");
			return new GameData();
		}
	}

	private void SaveAllData(GameData data)
	{
		try
		{
			string json = JsonUtility.ToJson(data, true);
			File.WriteAllText(saveFilePath, json);
			Debug.Log("Data saved successfully!");
		}
		catch (Exception e)
		{
			Debug.LogError($"Error saving data: {e.Message}");
		}
	}

	// ============================================
	// DEBUG TOOLS
	// ============================================

	[ContextMenu("Debug: Show All Runs")]
	private void DebugShowAllRuns()
	{
		GameData data = LoadAllData();

		Debug.Log($"=== LEADERBOARD ({data.leaderboard.runs.Count} runs) ===");

		for (int i = 0; i < data.leaderboard.runs.Count; i++)
		{
			RunData run = data.leaderboard.runs[i];
			Debug.Log($"#{i + 1} | Score: {run.score} | Time: {run.timeElapsed} | Date: {run.date}");
		}
	}

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

	[ContextMenu("Debug: Clear All Data")]
	private void DebugClearData()
	{
		ClearAllData();
	}
}

// ============================================
// CLASES DE DATOS (ESTRUCTURA DEL JSON)
// ============================================

[System.Serializable]
public class GameData
{
	public LeaderboardData leaderboard = new LeaderboardData();
	public SettingsData settings = new SettingsData();
}

[System.Serializable]
public class SettingsData
{
	// Sliders
	public float volGeneral = 1f;
	public float volMusic = 0.7f;
	public float volEffects = 0.7f;

	// Controls
	public string keyHab1 = "1";
	public string keyHab2 = "2";
	public string keyHab3 = "3";
	public string keyPrevHab = "Q";
	public string keyNextHab = "E";
	public string keyReload = "R";
	public string keyPickUp = "F";
	public string keyDash = "Space";
}

[System.Serializable]
public class LeaderboardData
{
	public List<RunData> runs = new List<RunData>();
}

[System.Serializable]
public class RunData
{
	public int score;
	public string timeElapsed;
	public string date;
}
