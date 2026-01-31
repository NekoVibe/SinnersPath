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
    }

    // ============================================
    // GESTIÓN DE CONFIGURACIÓN (TU IMAGEN)
    // ============================================

    public void SaveSettings(SettingsData newSettings)
    {
        GameData data = LoadAllData();
        data.settings = newSettings;
        SaveAllData(data);
    }

    public SettingsData LoadSettings()
    {
        return LoadAllData().settings;
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
    }

    public List<RunData> GetTopRuns(int count = 10)
    {
        GameData data = LoadAllData();
        int maxCount = Mathf.Min(count, data.leaderboard.runs.Count);
        return data.leaderboard.runs.GetRange(0, maxCount);
    }

    // ============================================
    // NÚCLEO DE PERSISTENCIA (JSON)
    // ============================================

    private GameData LoadAllData()
    {
        if (!File.Exists(saveFilePath))
            return new GameData(); // Retorna datos por defecto si no existe el archivo

        try
        {
            string json = File.ReadAllText(saveFilePath);
            return JsonUtility.FromJson<GameData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error cargando datos: {e.Message}");
            return new GameData();
        }
    }

    private void SaveAllData(GameData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(saveFilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error guardando datos: {e.Message}");
        }
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
    // Sliders de la imagen
    public float volGeneral = 1f;
    public float volMusic = 0.7f;
    public float volEffects = 0.7f;

    // Teclas de la imagen (Controls)
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