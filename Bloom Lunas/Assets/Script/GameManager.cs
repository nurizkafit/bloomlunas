using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class ActivityData
{
    public string activityId = "exploration";
    public string activityName = "Exploration";
    public string category = "Fieldwork";            // Production, Shop, Fieldwork, Guild
    public string locationName = "Gathering area";   // Terhubung dengan LocationPoint di scene CityOverWorld
    public string timeSlot = "06:00";
    public string subtext = "3h • Gathering area";
    public Color cardColor = new Color(0.96f, 0.94f, 0.88f, 1f);

    public ActivityData() { }

    public ActivityData(string id, string name, string cat, string loc, string sub, Color color)
    {
        activityId = id;
        activityName = name;
        category = cat;
        locationName = loc;
        subtext = sub;
        cardColor = color;
    }

    public ActivityData Clone()
    {
        return new ActivityData(activityId, activityName, category, locationName, subtext, cardColor)
        {
            timeSlot = this.timeSlot
        };
    }
}

[Serializable]
public class ScheduledPlan
{
    public int dayIndex;        // 0 = Mon, 1 = Tue, ... 6 = Sun
    public string timeSlot;     // "06:00", "09:00", dst
    public ActivityData activity;

    public ScheduledPlan(int day, string slot, ActivityData act)
    {
        dayIndex = day;
        timeSlot = slot;
        activity = act;
    }
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game States")]
    [Tooltip("False = Fase Planning (Bisa diedit), True = Fase Do / Di CityOverWorld (Read-only)")]
    public bool IsGameStarted = false;

    [Header("Scene Navigation")]
    [SerializeField] private string citySceneName = "CityOverWorld";

    // Penyimpanan rencana mingguan: Key = "DayIndex_TimeSlot" (contoh: "0_06:00")
    private Dictionary<string, ActivityData> weeklyPlan = new Dictionary<string, ActivityData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Menyimpan atau mengubah rencana pada hari dan jam tertentu.
    /// </summary>
    public void SavePlan(int dayIndex, string timeSlot, ActivityData activity)
    {
        string key = GetKey(dayIndex, timeSlot);
        weeklyPlan[key] = activity.Clone();
        Debug.Log($"[GameManager] Plan tersimpan: Hari ke-{dayIndex} ({timeSlot}) -> {activity.activityName}");
    }

    /// <summary>
    /// Menghapus rencana dari hari dan jam tertentu.
    /// </summary>
    public void RemovePlan(int dayIndex, string timeSlot)
    {
        string key = GetKey(dayIndex, timeSlot);
        if (weeklyPlan.ContainsKey(key))
        {
            weeklyPlan.Remove(key);
            Debug.Log($"[GameManager] Plan dihapus: Hari ke-{dayIndex} ({timeSlot})");
        }
    }

    /// <summary>
    /// Mendapatkan rencana pada hari dan jam tertentu.
    /// </summary>
    public ActivityData GetPlan(int dayIndex, string timeSlot)
    {
        string key = GetKey(dayIndex, timeSlot);
        if (weeklyPlan.TryGetValue(key, out ActivityData data))
        {
            return data;
        }
        return null;
    }

    /// <summary>
    /// Mengambil semua daftar rencana yang sudah dibuat.
    /// </summary>
    public List<ScheduledPlan> GetAllPlans()
    {
        List<ScheduledPlan> list = new List<ScheduledPlan>();
        foreach (var kvp in weeklyPlan)
        {
            string[] parts = kvp.Key.Split('_');
            if (parts.Length == 2 && int.TryParse(parts[0], out int day))
            {
                list.Add(new ScheduledPlan(day, parts[1], kvp.Value));
            }
        }
        return list;
    }

    /// <summary>
    /// Mengambil rencana khusus untuk hari tertentu (misal Hari Senin = 0).
    /// </summary>
    public List<ActivityData> GetPlansForDay(int dayIndex)
    {
        List<ActivityData> list = new List<ActivityData>();
        foreach (var kvp in weeklyPlan)
        {
            if (kvp.Key.StartsWith(dayIndex + "_"))
            {
                list.Add(kvp.Value);
            }
        }
        return list;
    }

    /// <summary>
    /// Memulai game (masuk ke fase Do di CityOverWorld).
    /// </summary>
    public void StartGameLoop()
    {
        IsGameStarted = true;
        Debug.Log("[GameManager] Masuk ke CityOverWorld dengan rencana yang telah dibuat!");
        SceneManager.LoadScene(citySceneName);
    }

    /// <summary>
    /// Membuka planner saat game dipause di scene CityOverWorld (mode read-only).
    /// </summary>
    public void OpenPausePlanner()
    {
        // IsGameStarted bernilai true, sehingga kartu tidak bisa di-drag / diubah
    }

    private string GetKey(int dayIndex, string timeSlot)
    {
        return $"{dayIndex}_{timeSlot}";
    }
}
