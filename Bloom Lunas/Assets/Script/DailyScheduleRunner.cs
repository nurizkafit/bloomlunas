using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// CityOverWorld bebas: pemain bergerak sendiri. Jam berjalan per slot 3 jam.
/// Kalau pemain berada di lokasi yang sesuai jadwal saat slot berjalan, dia
/// mendapat bonus (gold + XP), dan Exploration menghasilkan bahan acak.
/// </summary>
public class DailyScheduleRunner : MonoBehaviour
{
    public enum Phase
    {
        Idle,
        Playing,
        Complete,
        NoGameManager
    }

    [Header("Referensi Scene")]
    [Tooltip("Karakter pemain. Kosong = dicari otomatis.")]
    [SerializeField] private CharacterMovement character;

    [Header("HUD")]
    [SerializeField] private TMP_Text dayLabel;
    [SerializeField] private TMP_Text clockLabel;
    [SerializeField] private TMP_Text activityLabel;
    [SerializeField] private TMP_Text locationLabel;
    [SerializeField] private TMP_Text nextLabel;
    [SerializeField] private TMP_Text pauseLabel;
    [SerializeField] private Image progressFill;
    [SerializeField] private GameObject pauseButton;
    [SerializeField] private GameObject nextDayButton;

    [Header("Pengaturan")]
    [Tooltip("Durasi nyata (detik) untuk satu slot 3 jam di jam game.")]
    [SerializeField] private float secondsPerSlot = 30f;

    [Tooltip("Jarak pemain ke lokasi agar dianggap mengikuti jadwal.")]
    [SerializeField] private float arrivalRadius = 2.4f;

    [Tooltip("Lama pesan bonus/drop tampil di HUD.")]
    [SerializeField] private float messageDuration = 4f;

    [SerializeField] private string plannerSceneName = "MulaiGame";

    private GameManager gameManager;
    private readonly List<LocationPoint> locations = new List<LocationPoint>();

    private int slotIndex;
    private float slotElapsed;
    private bool dayComplete;
    private bool transitioning;
    private string dayText = string.Empty;
    private string message = string.Empty;
    private float messageUntil;

    private Phase phase = Phase.Idle;

    public Phase CurrentPhase => phase;
    public bool IsControllingMovement => false;
    public bool IsTransitioning => transitioning;
    public int CurrentStep => slotIndex;
    public int CurrentSlotIndex => slotIndex;
    public string CurrentActivityName { get; private set; } = string.Empty;
    public string CurrentLocationName { get; private set; } = string.Empty;

    public float SecondsPerSlot
    {
        get => secondsPerSlot;
        set => secondsPerSlot = Mathf.Max(1f, value);
    }

    private void Start()
    {
        if (character == null)
        {
            character = FindFirstObjectByType<CharacterMovement>();
        }

        if (pauseButton != null) pauseButton.SetActive(false);

        StartDay();
    }

    /// <summary>Memuat hari dan slot yang tersimpan lalu menjalankan jam.</summary>
    public void StartDay()
    {
        gameManager = GameManager.Instance;
        slotElapsed = 0f;
        dayComplete = false;
        transitioning = false;
        message = string.Empty;
        messageUntil = 0f;
        locations.Clear();
        locations.AddRange(FindObjectsByType<LocationPoint>(FindObjectsInactive.Exclude));

        if (gameManager == null)
        {
            phase = Phase.NoGameManager;
            SetHud("CITY OVERWORLD", "--:--", "No plan data",
                "Start from the planner to load your week.", "", 0f);
            SetButtons(false, false);
            return;
        }

        slotIndex = gameManager.CitySlotIndex;
        int day = gameManager.CurrentDay;
        dayText = $"{GameManager.DayShortNames[day].ToUpperInvariant()}  -  DAY {day + 1}  -  WEEK {gameManager.CurrentWeek}";

        phase = Phase.Playing;
        SetButtons(false, false);
        UpdateHud();
        Debug.Log($"[FreePlay] Day {day + 1}, slot {GameManager.TimeSlots[slotIndex]}.");
    }

    private void Update()
    {
        if (gameManager == null || transitioning) return;

        if (!dayComplete)
        {
            slotElapsed += Time.deltaTime;
            TryAwardBonus();

            if (slotElapsed >= secondsPerSlot)
            {
                AdvanceSlot();
            }
        }

        UpdateHud();
    }

    private ActivityData CurrentPlan()
    {
        if (gameManager == null) return null;
        return gameManager.GetPlanCoveringSlot(gameManager.CurrentDay, GameManager.TimeSlots[slotIndex]);
    }

    private void TryAwardBonus()
    {
        ActivityData plan = CurrentPlan();
        if (plan == null) return;
        if (gameManager.HasBonusClaim(gameManager.CurrentDay, plan.timeSlot)) return;

        LocationPoint target = ResolveLocation(plan);
        if (target == null) return;

        CharacterMovement player = character != null ? character : FindFirstObjectByType<CharacterMovement>();
        if (player == null) return;

        Vector3 destination = target.GetDestination(player.transform.position.y);
        if (Vector3.Distance(player.transform.position, destination) <= arrivalRadius)
        {
            ClaimBonus(plan, target.LocationName);
        }
    }

    /// <summary>
    /// Dipanggil saat pemain mengklik sebuah lokasi (mis. masuk ke rumah).
    /// Bonus hanya diberikan kalau lokasi itu sesuai rencana slot saat ini.
    /// </summary>
    public void ClaimBonusAtLocation(LocationPoint location)
    {
        if (gameManager == null || location == null) return;

        ActivityData plan = CurrentPlan();
        if (plan == null) return;
        if (gameManager.HasBonusClaim(gameManager.CurrentDay, plan.timeSlot)) return;

        string planTarget = Normalize(plan.locationName);
        string clicked = Normalize(location.LocationName);
        bool sameHome = planTarget.Contains("home") && !planTarget.Contains("drying") &&
                        clicked.Contains("home") && !clicked.Contains("drying");
        if (planTarget != clicked && !sameHome) return;

        ClaimBonus(plan, location.LocationName);
    }

    private void ClaimBonus(ActivityData plan, string locationName)
    {
        if (plan == null || gameManager == null) return;

        gameManager.AwardPlanMatchBonus(plan.activityName);
        gameManager.MarkBonusClaimed(gameManager.CurrentDay, plan.timeSlot);

        string text = $"On schedule (+15 G, +25 XP) at {locationName}.";
        string actName = (plan.activityName ?? string.Empty).ToLowerInvariant();
        if (actName.Contains("exploration"))
        {
            List<string> drops = gameManager.ExecuteExplorationDrops();
            if (drops.Count > 0) text += " Found: " + string.Join(", ", drops) + ".";
        }

        ShowMessage(text);
        Debug.Log("[FreePlay] " + text);
    }

    private void AdvanceSlot()
    {
        slotElapsed = 0f;
        slotIndex++;

        if (slotIndex >= GameManager.TimeSlots.Length)
        {
            slotIndex = GameManager.TimeSlots.Length - 1;
            dayComplete = true;
            phase = Phase.Complete;
            gameManager.CitySlotIndex = 0;
            ShowMessage("Day complete. Open the planner or start the next day.");
            SetButtons(false, true);
            return;
        }

        gameManager.CitySlotIndex = slotIndex;
        ShowMessage(string.Empty);
        SetButtons(false, false);
    }

    private void UpdateHud()
    {
        if (gameManager == null) return;

        ActivityData plan = CurrentPlan();
        LocationPoint target = plan != null ? ResolveLocation(plan) : null;
        bool claimed = plan != null && gameManager.HasBonusClaim(gameManager.CurrentDay, plan.timeSlot);

        int displaySlot = dayComplete ? GameManager.TimeSlots.Length - 1 : slotIndex;
        float progress = dayComplete ? 1f : Mathf.Clamp01(slotElapsed / secondsPerSlot);
        string clock = dayComplete ? "06:00" : FormatTime(GameManager.TimeSlots[displaySlot], progress * 180f);

        if (dayLabel != null) dayLabel.text = dayText;
        if (clockLabel != null) clockLabel.text = clock;
        if (progressFill != null) progressFill.fillAmount = progress;

        CurrentActivityName = plan != null ? plan.activityName : "Free time";
        CurrentLocationName = target != null ? target.LocationName : (plan != null ? plan.locationName : string.Empty);

        if (activityLabel != null)
        {
            activityLabel.text = dayComplete ? "Day complete" : CurrentActivityName;
        }

        if (locationLabel != null)
        {
            if (dayComplete)
            {
                locationLabel.text = "All slots are done. Well played.";
            }
            else if (plan == null)
            {
                locationLabel.text = "No schedule now. Explore freely.";
            }
            else if (target == null)
            {
                locationLabel.text = "Location not found: " + plan.locationName;
            }
            else if (claimed)
            {
                locationLabel.text = "On schedule. Bonus already earned.";
            }
            else
            {
                locationLabel.text = "Follow the plan: go to " + target.LocationName + " for a bonus.";
            }
        }

        if (nextLabel != null)
        {
            if (Time.unscaledTime < messageUntil && !string.IsNullOrEmpty(message))
            {
                nextLabel.text = message;
            }
            else
            {
                nextLabel.text = NextPlanText();
            }
        }
    }

    private string NextPlanText()
    {
        if (gameManager == null || dayComplete) return string.Empty;

        for (int i = slotIndex + 1; i < GameManager.TimeSlots.Length; i++)
        {
            ActivityData next = gameManager.GetPlan(gameManager.CurrentDay, GameManager.TimeSlots[i]);
            if (next != null)
            {
                return $"Next: {next.activityName} at {next.timeSlot}";
            }
        }

        return "No more plans today.";
    }

    private void ShowMessage(string text)
    {
        message = text;
        messageUntil = string.IsNullOrEmpty(text) ? 0f : Time.unscaledTime + messageDuration;
    }

    /// <summary>Lanjut ke hari berikutnya tanpa memuat ulang scene.</summary>
    public void NextDay()
    {
        if (gameManager == null) gameManager = GameManager.Instance;
        if (gameManager == null) return;

        gameManager.AdvanceDay();
        StartDay();
    }

    /// <summary>Kembali ke buku planner (rencana tetap tersimpan di GameManager).</summary>
    public void OpenPlanner()
    {
        if (transitioning) return;
        transitioning = true;

        if (character != null) character.StopMoving();
        if (gameManager != null) gameManager.IsGameStarted = false;

        SceneManager.LoadScene(plannerSceneName);
    }

    /// <summary>Dipanggil oleh WorldInteractionManager sebelum pindah ke House.</summary>
    public void BeginSceneTransition()
    {
        transitioning = true;
        if (character != null) character.StopMoving();
    }

    // Kompatibilitas dengan tombol Pause lama (tombolnya disembunyikan).
    public void TogglePause()
    {
    }

    private LocationPoint ResolveLocation(ActivityData activity)
    {
        if (activity == null) return null;

        string target = Normalize(activity.locationName);

        if (!string.IsNullOrEmpty(target))
        {
            foreach (LocationPoint point in locations)
            {
                if (Normalize(point.LocationName) == target) return point;
            }

            foreach (LocationPoint point in locations)
            {
                string name = Normalize(point.LocationName);
                if (name.Contains(target) || target.Contains(name)) return point;
            }
        }

        string keyword = activity.category == "Fieldwork" ? "gather"
            : activity.category == "Guild / Organization" ? "guild"
            : "home";

        foreach (LocationPoint point in locations)
        {
            if (Normalize(point.LocationName).Contains(keyword)) return point;
        }

        return null;
    }

    private static string Normalize(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        StringBuilder builder = new StringBuilder(text.Length);
        foreach (char c in text.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) builder.Append(c);
        }
        return builder.ToString();
    }

    private static string FormatTime(string slot, float addMinutes)
    {
        if (string.IsNullOrEmpty(slot)) return "--:--";

        string[] parts = slot.Split(':');
        int hours;
        int minutes;
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out hours) ||
            !int.TryParse(parts[1], out minutes))
        {
            return slot;
        }

        int total = hours * 60 + minutes + Mathf.RoundToInt(addMinutes);
        total = ((total % 1440) + 1440) % 1440;
        return $"{total / 60:00}:{total % 60:00}";
    }

    private void SetHud(string day, string clock, string activity, string location, string next, float progress)
    {
        if (dayLabel != null) dayLabel.text = day;
        if (clockLabel != null) clockLabel.text = clock;
        if (activityLabel != null) activityLabel.text = activity;
        if (locationLabel != null) locationLabel.text = location;
        if (nextLabel != null) nextLabel.text = next;
        if (progressFill != null) progressFill.fillAmount = progress;
    }

    private void SetButtons(bool canPause, bool showNextDay)
    {
        if (pauseButton != null) pauseButton.SetActive(false);
        if (nextDayButton != null) nextDayButton.SetActive(showNextDay);
    }
}
