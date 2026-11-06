using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class ActivityData
{
    public string activityId = "exploration";
    public string activityName = "Exploration";
    public string category = "Fieldwork";            // Production, Shop, Fieldwork, Guild / Organization
    public string locationName = "Gathering area";   // Terhubung dengan LocationPoint di scene CityOverWorld
    public string timeSlot = "06:00";
    public string subtext = "3h - Gathering area";
    public Color cardColor = new Color(0.96f, 0.94f, 0.88f, 1f);

    [Header("Pengaturan Kartu")]
    public int slotsCount = 1;                       // 1 slot (3 jam) atau 2 slot (6 jam)
    public string selectedRecipe = "Beras kencur";    // Untuk Craft
    public string selectedStation = "Home atelier";  // Untuk Processing: Home atelier / Home drying yard

    public ActivityData() { }

    public ActivityData(string id, string name, string cat, string loc, string sub, Color color, int slots = 1)
    {
        activityId = id;
        activityName = name;
        category = cat;
        locationName = loc;
        subtext = sub;
        cardColor = color;
        slotsCount = slots;
    }

    public ActivityData Clone()
    {
        return new ActivityData(activityId, activityName, category, locationName, subtext, cardColor, slotsCount)
        {
            timeSlot = this.timeSlot,
            selectedRecipe = this.selectedRecipe,
            selectedStation = this.selectedStation
        };
    }
}

[Serializable]
public class CraftRecipe
{
    public string recipeName;
    public int slotsRequired; // 1 slot (3h) atau 2 slot (6h)
    public Dictionary<string, int> ingredients = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    public string outputProduct;

    public CraftRecipe(string name, int slots, Dictionary<string, int> ing, string output)
    {
        recipeName = name;
        slotsRequired = slots;
        ingredients = ing;
        outputProduct = output;
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

    public static readonly string[] DayShortNames = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
    public static readonly string[] DayFullNames = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
    public static readonly string[] TimeSlots = { "06:00", "09:00", "12:00", "15:00", "18:00", "21:00", "00:00", "03:00" };

    [Header("Game States")]
    [Tooltip("False = Fase Planning (Bisa diedit), True = Fase Do / Di CityOverWorld (Read-only)")]
    public bool IsGameStarted = false;

    [Header("Scene Navigation")]
    [SerializeField] private string citySceneName = "CityOverWorld";

    [Header("Week & Economy (Header Planner)")]
    [SerializeField] private int currentWeek = 1;
    [Tooltip("Hari yang sedang dijalankan di CityOverWorld. 0 = Senin, 6 = Minggu")]
    [SerializeField] private int currentDay = 0;
    [SerializeField] private int gold = 240;
    [SerializeField] private int debt = 3000;
    [SerializeField] private int nextPayment = 420;
    [SerializeField] private int nextPaymentInDays = 28;
    [SerializeField] private int livingCost = 40;
    [SerializeField] private int goldAfterPlanMin = 239;
    [SerializeField] private int goldAfterPlanMax = 264;
    [SerializeField] private int energy = 100;
    [SerializeField] private int energyMax = 100;
    [SerializeField] private int xp = 0;

    public int CurrentWeek => currentWeek;
    public int CurrentDay => ((currentDay % 7) + 7) % 7;
    public string CurrentDayName => DayFullNames[CurrentDay];
    public int Gold => gold;
    public int Debt => debt;
    public int NextPayment => nextPayment;
    public int NextPaymentInDays => nextPaymentInDays;
    public int LivingCost => livingCost;
    public int GoldAfterPlanMin => goldAfterPlanMin;
    public int GoldAfterPlanMax => goldAfterPlanMax;
    public int Energy => energy;
    public int EnergyMax => Mathf.Max(1, energyMax);
    public int XP => xp;

    public int TotalPlanCount => weeklyPlan.Count;

    /// <summary>
    /// Dipanggil setiap kali rencana mingguan berubah (isi kartu ditambah / dipindah / dihapus).
    /// </summary>
    public event Action PlanChanged;

    /// <summary>
    /// Dipanggil setiap kali inventaris bahan / gold berubah.
    /// </summary>
    public event Action InventoryChanged;

    // Inventaris bahan (nama bahan -> jumlah)
    private readonly Dictionary<string, int> inventory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    // Resep Crafting bawaan
    public static readonly Dictionary<string, CraftRecipe> CraftRecipes = new Dictionary<string, CraftRecipe>(StringComparer.OrdinalIgnoreCase)
    {
        {
            "Kunyit asam", new CraftRecipe("Kunyit asam", 1, new Dictionary<string, int>
            {
                { "Turmeric", 3 },
                { "Tamarind", 1 },
                { "Palm sugar", 1 }
            }, "Fresh jamu")
        },
        {
            "Beras kencur", new CraftRecipe("Beras kencur", 1, new Dictionary<string, int>
            {
                { "Kencur", 3 },
                { "Ginger", 1 },
                { "Palm sugar", 1 },
                { "Rice", 1 }
            }, "Fresh jamu")
        },
        {
            "Turmeric salep", new CraftRecipe("Turmeric salep", 2, new Dictionary<string, int>
            {
                { "Turmeric", 3 },
                { "Coconut oil", 1 },
                { "Beeswax", 1 }
            }, "Salep")
        },
        {
            "Lemongrass balm", new CraftRecipe("Lemongrass balm", 2, new Dictionary<string, int>
            {
                { "Lemongrass", 3 },
                { "Coconut oil", 1 },
                { "Beeswax", 1 }
            }, "Salep")
        }
    };

    // Penyimpanan rencana mingguan: Key = "DayIndex_TimeSlot" (contoh: "0_06:00")
    private readonly Dictionary<string, ActivityData> weeklyPlan = new Dictionary<string, ActivityData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitStartingInventory();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitStartingInventory()
    {
        // Bahan awal sesuai prototype
        SetMaterial("Turmeric", 4);
        SetMaterial("Ginger", 2);
        SetMaterial("Kencur", 2);
        SetMaterial("Tamarind", 1);
        SetMaterial("Palm sugar", 2);
        SetMaterial("Rice", 2);
        SetMaterial("Coconut oil", 1);
        SetMaterial("Beeswax", 1);
        SetMaterial("Lemongrass", 2);
    }

    #region Inventory & Recipes

    public int GetMaterial(string materialName)
    {
        if (string.IsNullOrEmpty(materialName)) return 0;
        inventory.TryGetValue(materialName, out int count);
        return count;
    }

    public void SetMaterial(string materialName, int count)
    {
        inventory[materialName] = Mathf.Max(0, count);
        InventoryChanged?.Invoke();
    }

    public void AddMaterial(string materialName, int amount)
    {
        int current = GetMaterial(materialName);
        SetMaterial(materialName, current + amount);
    }

    public bool HasIngredients(string recipeName, out List<string> missingList)
    {
        missingList = new List<string>();
        if (!CraftRecipes.TryGetValue(recipeName, out CraftRecipe recipe)) return false;

        foreach (var item in recipe.ingredients)
        {
            int owned = GetMaterial(item.Key);
            if (owned < item.Value)
            {
                int missing = item.Value - owned;
                missingList.Add($"{missing} {item.Key}");
            }
        }

        return missingList.Count == 0;
    }

    /// <summary>
    /// Konsumsi bahan dan hasilkan produk craft jika bahan mencukupi.
    /// </summary>
    public bool ExecuteCraft(string recipeName)
    {
        if (!HasIngredients(recipeName, out _)) return false;

        CraftRecipe recipe = CraftRecipes[recipeName];
        foreach (var item in recipe.ingredients)
        {
            AddMaterial(item.Key, -item.Value);
        }

        Debug.Log($"[GameManager] Berhasil membuat {recipe.outputProduct} ({recipeName})!");
        return true;
    }

    /// <summary>
    /// Mendapatkan drop bahan acak saat aktivitas Exploration selesai.
    /// </summary>
    public List<string> ExecuteExplorationDrops()
    {
        string[] pool = { "Turmeric", "Ginger", "Kencur", "Tamarind", "Palm sugar", "Rice", "Coconut oil", "Beeswax", "Lemongrass" };
        List<string> rewards = new List<string>();

        int rollCount = UnityEngine.Random.Range(2, 4); // Dapat 2 sampai 3 jenis bahan
        for (int i = 0; i < rollCount; i++)
        {
            string chosen = pool[UnityEngine.Random.Range(0, pool.Length)];
            int qty = UnityEngine.Random.Range(1, 3);
            AddMaterial(chosen, qty);
            rewards.Add($"+{qty} {chosen}");
        }

        return rewards;
    }

    #endregion

    #region Aturan & Validasi Jadwal (Card Deck Constraints)

    /// <summary>
    /// Validasi apakah aktivitas dapat ditempatkan pada hari dan jam tersebut sesuai aturan game.
    /// </summary>
    public static bool ValidateActivitySlot(ActivityData activity, int dayIndex, string timeSlot, out string rejectReason)
    {
        rejectReason = string.Empty;
        if (activity == null || string.IsNullOrEmpty(timeSlot)) return true;

        string name = activity.activityName.ToLowerInvariant();
        string id = activity.activityId.ToLowerInvariant();

        // 1. ATURAN OPEN SHOP:
        // - Hanya bisa buka hari Senin - Sabtu (Minggu tutup)
        // - Jam buka: 08:00 - 19:00 (Slot 06:00 terlalu pagi, malam hari tutup)
        if (name.Contains("shop") || id.Contains("shop"))
        {
            if (dayIndex == 6) // Minggu
            {
                rejectReason = "Open shop tutup di hari Minggu (hanya buka Senin–Sabtu).";
                return false;
            }

            if (timeSlot == "06:00")
            {
                rejectReason = "Open shop baru buka jam 08:00 pagi (slot 06:00 terlalu pagi).";
                return false;
            }

            if (timeSlot == "18:00" || timeSlot == "21:00" || timeSlot == "00:00" || timeSlot == "03:00")
            {
                rejectReason = "Open shop hanya buka sampai jam 19:00 (malam hari tutup).";
                return false;
            }
        }

        // 2. ATURAN EXPLORATION:
        // - Bisa buka kapan saja (Senin - Minggu)
        // - Hanya bisa dari jam 06:00 pagi sampai 15:00 sore
        if (name.Contains("exploration") || id.Contains("exploration") || name.Contains("forage"))
        {
            if (timeSlot != "06:00" && timeSlot != "09:00" && timeSlot != "12:00")
            {
                rejectReason = "Exploration hanya dibuka jam 06:00 pagi sampai 15:00 sore.";
                return false;
            }
        }

        // 3. ATURAN GUILD / ORGANIZATION:
        if (name.Contains("visit"))
        {
            // Visit hanya hari Mon, Wed, Sat 8-12
            if (dayIndex != 0 && dayIndex != 2 && dayIndex != 5)
            {
                rejectReason = "Visit hanya tersedia pada hari Senin, Rabu, dan Sabtu.";
                return false;
            }
            if (timeSlot != "09:00")
            {
                rejectReason = "Visit hanya dibuka jam 08:00 – 12:00 (slot 09:00).";
                return false;
            }
        }

        if (name.Contains("commission") || name.Contains("appointment"))
        {
            // Mon-Sat 8-17
            if (dayIndex == 6)
            {
                rejectReason = "Guild tutup pada hari Minggu.";
                return false;
            }
            if (timeSlot == "06:00" || timeSlot == "18:00" || timeSlot == "21:00" || timeSlot == "00:00" || timeSlot == "03:00")
            {
                rejectReason = "Guild hanya buka jam 08:00 – 17:00.";
                return false;
            }
        }

        // Craft & Processing: Bebas ditempatkan kapan saja (Daily)
        return true;
    }

    #endregion

    #region Weekly Plan Storage

    public void SavePlan(int dayIndex, string timeSlot, ActivityData activity)
    {
        if (activity == null || string.IsNullOrEmpty(timeSlot)) return;

        string key = GetKey(dayIndex, timeSlot);
        weeklyPlan[key] = activity.Clone();
        PlanChanged?.Invoke();
    }

    public void RemovePlan(int dayIndex, string timeSlot)
    {
        string key = GetKey(dayIndex, timeSlot);
        if (weeklyPlan.Remove(key))
        {
            PlanChanged?.Invoke();
        }
    }

    public ActivityData GetPlan(int dayIndex, string timeSlot)
    {
        string key = GetKey(dayIndex, timeSlot);
        return weeklyPlan.TryGetValue(key, out ActivityData data) ? data : null;
    }

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

    public List<ActivityData> GetPlansForDay(int dayIndex)
    {
        List<ActivityData> list = new List<ActivityData>();
        string prefix = dayIndex + "_";
        foreach (string slot in TimeSlots)
        {
            string key = prefix + slot;
            if (weeklyPlan.TryGetValue(key, out ActivityData act))
            {
                list.Add(act);
            }
        }
        return list;
    }

    public int GetPlanCountForDay(int dayIndex)
    {
        int count = 0;
        string prefix = dayIndex + "_";
        foreach (var key in weeklyPlan.Keys)
        {
            if (key.StartsWith(prefix)) count++;
        }
        return count;
    }

    public int GetWorkPlanCountForDay(int dayIndex)
    {
        int count = 0;
        string prefix = dayIndex + "_";
        foreach (var kvp in weeklyPlan)
        {
            if (kvp.Key.StartsWith(prefix) && kvp.Value.category != "Rest") count++;
        }
        return count;
    }

    #endregion

    #region CityOverWorld Gameplay & Plan Match Bonus

    /// <summary>
    /// Mendapatkan bonus jika pemain mengunjungi lokasi sesuai dengan rencana hari ini.
    /// </summary>
    public void AwardPlanMatchBonus(string actName)
    {
        int bonusGold = 15;
        int bonusXP = 25;
        gold += bonusGold;
        xp += bonusXP;
        Debug.Log($"[Plan Match Bonus] +{bonusXP} XP, +{bonusGold} Gold karena mengikuti jadwal '{actName}'!");
    }

    public void AddGold(int amount)
    {
        gold = Mathf.Max(0, gold + amount);
        InventoryChanged?.Invoke();
    }

    public void AdvanceDay()
    {
        currentDay = (CurrentDay + 1) % 7;
        Debug.Log($"[GameManager] Hari berikutnya: {CurrentDayName}");
    }

    public void StartGameLoop()
    {
        IsGameStarted = true;
        SceneManager.LoadScene(citySceneName);
    }

    private string GetKey(int dayIndex, string timeSlot)
    {
        return $"{dayIndex}_{timeSlot}";
    }

    #endregion
}
