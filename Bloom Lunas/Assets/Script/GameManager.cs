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

    [Header("City Progress")]
    [Tooltip("Slot 3 jam yang sedang berjalan di CityOverWorld (0 = 06:00 ... 7 = 03:00).")]
    [SerializeField] private int citySlotIndex = 0;

    public int CurrentWeek => currentWeek;
    public int CitySlotIndex
    {
        get => Mathf.Clamp(citySlotIndex, 0, GameManager.TimeSlots.Length - 1);
        set => citySlotIndex = Mathf.Clamp(value, 0, GameManager.TimeSlots.Length - 1);
    }
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

    // Bonus yang sudah diklaim: key = "day_slotStart"
    private readonly HashSet<string> claimedBonuses = new HashSet<string>();

    // Produk hasil craft (nama resep -> jumlah)
    private readonly Dictionary<string, int> products = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    // Harga jual produk
    public static readonly Dictionary<string, int> ProductPrices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        { "Kunyit asam", 45 },
        { "Beras kencur", 55 },
        { "Turmeric salep", 70 },
        { "Lemongrass balm", 70 }
    };

    // Harga bahan di Market
    public static readonly Dictionary<string, int> MaterialPrices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        { "Turmeric", 8 },
        { "Ginger", 8 },
        { "Kencur", 10 },
        { "Tamarind", 6 },
        { "Palm sugar", 10 },
        { "Rice", 5 },
        { "Coconut oil", 12 },
        { "Beeswax", 15 },
        { "Lemongrass", 6 }
    };

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
            "Turmeric salep", new CraftRecipe("Turmeric salep", 1, new Dictionary<string, int>
            {
                { "Turmeric", 3 },
                { "Coconut oil", 1 },
                { "Beeswax", 1 }
            }, "Salep")
        },
        {
            "Lemongrass balm", new CraftRecipe("Lemongrass balm", 1, new Dictionary<string, int>
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

        SetProduct(recipeName, GetProduct(recipeName) + 1);
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

    public int GetProduct(string productName)
    {
        if (string.IsNullOrEmpty(productName)) return 0;
        products.TryGetValue(productName, out int count);
        return count;
    }

    public void SetProduct(string productName, int count)
    {
        products[productName] = Mathf.Max(0, count);
        InventoryChanged?.Invoke();
    }

    public int GetProductPrice(string productName)
    {
        return ProductPrices.TryGetValue(productName ?? string.Empty, out int price) ? price : 30;
    }

    public bool SellProduct(string productName)
    {
        if (GetProduct(productName) <= 0) return false;
        SetProduct(productName, GetProduct(productName) - 1);
        AddGold(GetProductPrice(productName));
        return true;
    }

    public bool BuyMaterial(string materialName)
    {
        if (!MaterialPrices.TryGetValue(materialName ?? string.Empty, out int price)) return false;
        if (gold < price) return false;
        gold -= price;
        AddMaterial(materialName, 1);
        InventoryChanged?.Invoke();
        return true;
    }

    public int GetMaterialPrice(string materialName)
    {
        return MaterialPrices.TryGetValue(materialName ?? string.Empty, out int price) ? price : 0;
    }

    public bool HasBonusClaim(int dayIndex, string planStartSlot)
    {
        return claimedBonuses.Contains($"{dayIndex}_{planStartSlot}");
    }

    public void MarkBonusClaimed(int dayIndex, string planStartSlot)
    {
        claimedBonuses.Add($"{dayIndex}_{planStartSlot}");
    }

    public void ClearBonusClaims()
    {
        claimedBonuses.Clear();
    }

    #endregion

    #region Aturan & Validasi Jadwal (Card Deck Constraints)

    /// <summary>
    /// Validasi aturan hari + jam + durasi untuk setiap kartu.
    /// </summary>
    public static bool ValidateActivitySlot(ActivityData activity, int dayIndex, string timeSlot, out string rejectReason)
    {
        rejectReason = string.Empty;
        if (activity == null || string.IsNullOrEmpty(timeSlot)) return true;

        string name = activity.activityName.ToLowerInvariant();
        string id = (activity.activityId ?? string.Empty).ToLowerInvariant();

        int start = SlotStartMinutes(timeSlot);
        if (start < 0) return true;

        int slots = Mathf.Clamp(activity.slotsCount <= 0 ? 1 : activity.slotsCount, 1, 2);
        int end = start + slots * 180;

        bool isShop = name.Contains("shop") || id.Contains("shop");
        bool isExploration = name.Contains("exploration") || id.Contains("exploration") || id.Contains("forage");
        bool isVisit = name.Contains("visit");
        bool isGuild = name.Contains("commission") || name.Contains("appointment");
        bool isCraft = name.Contains("craft");
        bool isProcessing = name.Contains("processing");

        // Open shop: Senin-Sabtu, 08:00-19:00. Durasi 3h atau 6h.
        if (isShop)
        {
            if (dayIndex == 6)
            {
                rejectReason = "Open shop is closed on Sunday (Mon-Sat only).";
                return false;
            }
            if (start < 8 * 60)
            {
                rejectReason = "Open shop starts at 08:00. Pick 09:00 or later.";
                return false;
            }
            if (end > 19 * 60)
            {
                rejectReason = "Open shop must close by 19:00. Pick an earlier or shorter session.";
                return false;
            }
        }

        // Exploration: setiap hari, 06:00-15:00. Durasi 3h atau 6h.
        if (isExploration)
        {
            if (start < 6 * 60)
            {
                rejectReason = "Exploration starts at 06:00.";
                return false;
            }
            if (end > 15 * 60)
            {
                rejectReason = "Exploration must finish by 15:00. Pick an earlier or shorter session.";
                return false;
            }
        }

        // Visit: Senin, Rabu, Sabtu, 08:00-12:00.
        if (isVisit)
        {
            if (dayIndex != 0 && dayIndex != 2 && dayIndex != 5)
            {
                rejectReason = "Visit is only available on Mon, Wed and Sat.";
                return false;
            }
            if (start < 8 * 60 || end > 12 * 60)
            {
                rejectReason = "Visit runs 08:00-12:00 only.";
                return false;
            }
            if (slots > 1)
            {
                rejectReason = "Visit is a 3-hour activity.";
                return false;
            }
        }

        // Appointment / Commission: Senin-Sabtu, 08:00-17:00.
        if (isGuild)
        {
            if (dayIndex == 6)
            {
                rejectReason = "The guild is closed on Sunday.";
                return false;
            }
            if (start < 8 * 60 || end > 17 * 60)
            {
                rejectReason = "Guild activities run 08:00-17:00.";
                return false;
            }
        }

        // Craft & Processing: kapan saja, 3 jam (1 slot).
        if ((isCraft || isProcessing) && slots > 1)
        {
            rejectReason = (isCraft ? "Craft" : "Processing") + " is a 3-hour activity.";
            return false;
        }

        return true;
    }

    public static int SlotStartMinutes(string timeSlot)
    {
        if (string.IsNullOrEmpty(timeSlot)) return -1;
        string[] parts = timeSlot.Split(':');
        if (parts.Length < 2) return -1;
        if (!int.TryParse(parts[0], out int h) || !int.TryParse(parts[1], out int m)) return -1;
        return h * 60 + m;
    }

    public static int SlotIndex(string timeSlot)
    {
        if (string.IsNullOrEmpty(timeSlot)) return -1;
        for (int i = 0; i < TimeSlots.Length; i++)
        {
            if (TimeSlots[i] == timeSlot) return i;
        }
        return -1;
    }

    /// <summary>Slot ini dipakai sebagai jam kedua dari kartu 6 jam sebelumnya.</summary>
    public bool IsSlotReserved(int dayIndex, string timeSlot)
    {
        return GetReservationSourceSlot(dayIndex, timeSlot) != null;
    }

    /// <summary>Slot start dari kartu 6 jam yang memakai slot ini, atau null.</summary>
    public string GetReservationSourceSlot(int dayIndex, string timeSlot)
    {
        int idx = SlotIndex(timeSlot);
        if (idx <= 0) return null;

        ActivityData prev = GetPlan(dayIndex, TimeSlots[idx - 1]);
        if (prev != null && prev.slotsCount > 1) return TimeSlots[idx - 1];
        return null;
    }

    public bool IsSlotBlocked(int dayIndex, string timeSlot)
    {
        return GetPlan(dayIndex, timeSlot) != null || IsSlotReserved(dayIndex, timeSlot);
    }

    /// <summary>Kartu yang mengisi slot ini (termasuk sesi 6 jam yang mulai di slot sebelumnya).</summary>
    public ActivityData GetPlanCoveringSlot(int dayIndex, string timeSlot)
    {
        ActivityData own = GetPlan(dayIndex, timeSlot);
        if (own != null) return own;

        string sourceSlot = GetReservationSourceSlot(dayIndex, timeSlot);
        return sourceSlot != null ? GetPlan(dayIndex, sourceSlot) : null;
    }

    /// <summary>
    /// Cek apakah sebuah kartu boleh menempati slot ini (durasi + slot berikutnya).
    /// </summary>
    public bool CanPlaceAt(int dayIndex, string timeSlot, ActivityData activity, out string reason)
    {
        reason = string.Empty;
        if (activity == null) return false;

        if (IsSlotBlocked(dayIndex, timeSlot))
        {
            reason = "That slot is already taken.";
            return false;
        }

        if (!ValidateActivitySlot(activity, dayIndex, timeSlot, out reason)) return false;

        if (activity.slotsCount > 1)
        {
            int idx = SlotIndex(timeSlot);
            if (idx < 0 || idx + 1 >= TimeSlots.Length)
            {
                reason = "A 6-hour session cannot start that late.";
                return false;
            }
            if (IsSlotBlocked(dayIndex, TimeSlots[idx + 1]))
            {
                reason = "The next 3-hour slot is already taken.";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Cek apakah kartu yang sudah ada boleh diubah menjadi 6 jam.
    /// </summary>
    public bool CanChangeToSlotsCount(int dayIndex, string timeSlot, ActivityData activity, int newSlots, out string reason)
    {
        reason = string.Empty;
        if (activity == null) return false;
        if (newSlots <= 1) return true;

        int idx = SlotIndex(timeSlot);
        if (idx < 0 || idx + 1 >= TimeSlots.Length)
        {
            reason = "A 6-hour session cannot start that late.";
            return false;
        }

        string nextSlot = TimeSlots[idx + 1];
        if (GetPlan(dayIndex, nextSlot) != null || IsSlotReserved(dayIndex, nextSlot))
        {
            reason = "The next slot is already taken. Pick another time.";
            return false;
        }

        ActivityData test = activity.Clone();
        test.slotsCount = newSlots;
        return ValidateActivitySlot(test, dayIndex, timeSlot, out reason);
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
            if (key.StartsWith(prefix)) count += Mathf.Max(1, weeklyPlan[key] != null ? weeklyPlan[key].slotsCount : 1);
        }
        return count;
    }

    public int GetWorkPlanCountForDay(int dayIndex)
    {
        int count = 0;
        string prefix = dayIndex + "_";
        foreach (var kvp in weeklyPlan)
        {
            if (kvp.Key.StartsWith(prefix) && kvp.Value.category != "Rest") count += Mathf.Max(1, kvp.Value.slotsCount);
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

    public void RestoreEnergy()
    {
        energy = EnergyMax;
        InventoryChanged?.Invoke();
    }

    public void AdvanceDay()
    {
        currentDay = (CurrentDay + 1) % 7;
        CitySlotIndex = 0;
        ClearBonusClaims();
        Debug.Log($"[GameManager] Hari berikutnya: {CurrentDayName}");
    }

    public void StartGameLoop()
    {
        IsGameStarted = true;
        CitySlotIndex = 0;
        ClearBonusClaims();
        SceneManager.LoadScene(citySceneName);
    }

    private string GetKey(int dayIndex, string timeSlot)
    {
        return $"{dayIndex}_{timeSlot}";
    }

    #endregion
}
