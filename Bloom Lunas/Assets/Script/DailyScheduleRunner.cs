using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menjalankan rencana hari ini di CityOverWorld: karakter berjalan ke lokasi
/// setiap aktivitas, berdiam di sana selama durasi aktivitas, lalu lanjut ke
/// aktivitas berikutnya sesuai urutan jam pada buku planner.
/// </summary>
public class DailyScheduleRunner : MonoBehaviour
{
    public enum Phase
    {
        Idle,
        Walking,
        Doing,
        Missing,
        Complete,
        NoPlan,
        NoGameManager
    }

    [Header("Referensi Scene")]
    [Tooltip("Karakter yang akan dijalankan. Kosong = dicari otomatis.")]
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
    [Tooltip("Durasi nyata (detik) untuk menyelesaikan satu aktivitas. Satu slot = 3 jam di jam game.")]
    [SerializeField] private float secondsPerActivity = 7f;

    [Tooltip("Jeda sebelum melewati aktivitas yang lokasinya tidak ditemukan.")]
    [SerializeField] private float missingLocationDelay = 1.5f;

    [SerializeField] private string plannerSceneName = "MulaiGame";

    private GameManager gameManager;
    private readonly List<ActivityData> today = new List<ActivityData>();
    private readonly List<LocationPoint> locations = new List<LocationPoint>();

    private int stepIndex = -1;
    private float activityElapsed;
    private float missingElapsed;
    private bool paused;
    private Phase phase = Phase.Idle;
    private LocationPoint activeLocation;

    public Phase CurrentPhase => phase;
    public bool IsControllingMovement => !paused && (phase == Phase.Walking || phase == Phase.Doing);
    public int CurrentStep => stepIndex;
    public int TodayCount => today.Count;
    public string CurrentActivityName { get; private set; } = string.Empty;
    public string CurrentLocationName { get; private set; } = string.Empty;

    public float SecondsPerActivity
    {
        get => secondsPerActivity;
        set => secondsPerActivity = Mathf.Max(0.25f, value);
    }

    private void Start()
    {
        if (character == null)
        {
            character = FindFirstObjectByType<CharacterMovement>();
        }

        StartDay();
    }

    /// <summary>
    /// Memuat rencana untuk hari aktif saat ini dan memulai langkah pertama.
    /// Juga dipakai ulang saat pemain menekan "Next day".
    /// </summary>
    public void StartDay()
    {
        gameManager = GameManager.Instance;
        paused = false;
        activityElapsed = 0f;
        missingElapsed = 0f;
        stepIndex = -1;
        activeLocation = null;
        today.Clear();
        UpdatePauseLabel();

        if (gameManager == null)
        {
            phase = Phase.NoGameManager;
            SetHud("CITY OVERWORLD", "--:--", "No plan data",
                "Start from the planner to load your week.", "", 0f);
            SetButtons(false, false);
            return;
        }

        int day = gameManager.CurrentDay;
        string dayText = $"{GameManager.DayShortNames[day].ToUpperInvariant()}  -  DAY {day + 1}";
        today.AddRange(gameManager.GetPlansForDay(day));
        CacheLocations();

        if (today.Count == 0)
        {
            phase = Phase.NoPlan;
            SetHud(dayText, "--:--", "No activities planned",
                "Open the planner or go to the next day.", "", 0f);
            SetButtons(false, true);
            Debug.Log($"[Routine] Day {day + 1}: no activities planned.");
            return;
        }

        SetButtons(true, false);
        stepIndex = 0;
        BeginStep(dayText);
    }

    private void CacheLocations()
    {
        locations.Clear();
        locations.AddRange(FindObjectsByType<LocationPoint>(FindObjectsSortMode.None));
    }

    private void BeginStep(string dayText)
    {
        ActivityData activity = today[stepIndex];
        CurrentActivityName = activity.activityName;
        activeLocation = ResolveLocation(activity);

        if (dayLabel != null) dayLabel.text = dayText;
        if (activityLabel != null) activityLabel.text = activity.activityName;
        if (clockLabel != null) clockLabel.text = activity.timeSlot;
        if (progressFill != null) progressFill.fillAmount = 0f;

        if (nextLabel != null)
        {
            nextLabel.text = stepIndex + 1 < today.Count
                ? $"Next: {today[stepIndex + 1].activityName} at {today[stepIndex + 1].timeSlot}"
                : "Last activity of the day";
        }

        if (activeLocation == null)
        {
            phase = Phase.Missing;
            missingElapsed = 0f;
            CurrentLocationName = activity.locationName;
            if (locationLabel != null) locationLabel.text = "Location not found: " + activity.locationName;
            Debug.LogWarning($"[Routine] Location '{activity.locationName}' not found for {activity.activityName}.");
            return;
        }

        WalkTo(activeLocation);
    }

    private void WalkTo(LocationPoint location)
    {
        if (character == null)
        {
            phase = Phase.Missing;
            missingElapsed = 0f;
            if (locationLabel != null) locationLabel.text = "Character not found in scene.";
            return;
        }

        phase = Phase.Walking;
        CurrentLocationName = location.LocationName;
        if (locationLabel != null) locationLabel.text = "Walking to " + location.LocationName;
        if (progressFill != null) progressFill.fillAmount = 0f;

        Vector3 destination = location.GetDestination(character.transform.position.y);
        Debug.Log($"[Routine] Step {stepIndex + 1}/{today.Count}: walking to {location.LocationName} for {CurrentActivityName}.");
        character.MoveTo(destination, OnArrived);
    }

    private void OnArrived()
    {
        if (phase != Phase.Walking || paused) return;

        phase = Phase.Doing;
        activityElapsed = 0f;
        if (locationLabel != null && activeLocation != null)
        {
            locationLabel.text = "At " + activeLocation.LocationName;
        }

        Debug.Log($"[Routine] Doing {CurrentActivityName} at {CurrentLocationName}.");
    }

    private void Update()
    {
        if (paused) return;

        if (phase == Phase.Doing)
        {
            activityElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(activityElapsed / secondsPerActivity);

            if (progressFill != null) progressFill.fillAmount = progress;
            UpdateClock(progress);

            if (progress >= 1f) CompleteStep();
        }
        else if (phase == Phase.Missing)
        {
            missingElapsed += Time.deltaTime;
            if (missingElapsed >= missingLocationDelay) CompleteStep();
        }
    }

    private void UpdateClock(float progress)
    {
        if (clockLabel == null || stepIndex < 0 || stepIndex >= today.Count) return;
        clockLabel.text = FormatTime(today[stepIndex].timeSlot, progress * 180f);
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

    private void CompleteStep()
    {
        Debug.Log($"[Routine] Finished {CurrentActivityName}.");

        // Eksekusi aktivitas dan berikan Plan Match Bonus
        if (stepIndex >= 0 && stepIndex < today.Count && GameManager.Instance != null)
        {
            ActivityData act = today[stepIndex];
            string actName = act.activityName.ToLowerInvariant();

            // Bonus mengikuti jadwal
            GameManager.Instance.AwardPlanMatchBonus(act.activityName);

            if (actName.Contains("exploration") || actName.Contains("forage"))
            {
                var rewards = GameManager.Instance.ExecuteExplorationDrops();
                Debug.Log($"[Exploration] Drop bahan didapat: {string.Join(", ", rewards)}");
            }
            else if (actName.Contains("craft"))
            {
                bool success = GameManager.Instance.ExecuteCraft(act.selectedRecipe);
                if (success)
                    Debug.Log($"[Craft] Berhasil membuat {act.selectedRecipe}!");
                else
                    Debug.Log($"[Craft] Gagal membuat {act.selectedRecipe} karena bahan belum mencukupi.");
            }
            else if (actName.Contains("shop"))
            {
                int earnings = UnityEngine.Random.Range(25, 45) * act.slotsCount;
                GameManager.Instance.AddGold(earnings);
                Debug.Log($"[Shop] Toko dibuka! Keuntungan penjualan: +{earnings} Gold.");
            }
        }

        stepIndex++;

        if (stepIndex >= today.Count)
        {
            FinishDay();
        }
        else
        {
            BeginStep(dayLabel != null ? dayLabel.text : "");
        }
    }

    private void FinishDay()
    {
        phase = Phase.Complete;
        if (activityLabel != null) activityLabel.text = "Day complete";
        if (locationLabel != null) locationLabel.text = "All planned activities are done.";
        if (nextLabel != null) nextLabel.text = string.Empty;
        if (progressFill != null) progressFill.fillAmount = 1f;
        SetButtons(false, true);
        Debug.Log("[Routine] Day complete.");
    }

    /// <summary>
    /// Menjeda / melanjutkan rutinitas. Saat dijeda pemain bisa mengklik lokasi
    /// secara manual seperti biasa.
    /// </summary>
    public void TogglePause()
    {
        if (phase != Phase.Walking && phase != Phase.Doing) return;

        paused = !paused;
        UpdatePauseLabel();

        if (paused)
        {
            if (character != null) character.StopMoving();
            if (locationLabel != null)
            {
                locationLabel.text = phase == Phase.Doing
                    ? "Paused at " + CurrentLocationName
                    : "Paused - heading to " + CurrentLocationName;
            }
        }
        else if (phase == Phase.Walking)
        {
            WalkTo(activeLocation);
        }
        else if (activeLocation != null && character != null &&
                 Vector3.Distance(character.transform.position,
                     activeLocation.GetDestination(character.transform.position.y)) > 0.5f)
        {
            // Pemain sempat berjalan manual saat dijeda: kembali ke lokasi aktivitas.
            WalkTo(activeLocation);
        }
        else if (locationLabel != null)
        {
            locationLabel.text = "At " + CurrentLocationName;
        }
    }

    /// <summary>
    /// Lanjut ke hari berikutnya tanpa memuat ulang scene.
    /// </summary>
    public void NextDay()
    {
        if (gameManager == null) gameManager = GameManager.Instance;
        if (gameManager == null) return;

        gameManager.AdvanceDay();
        StartDay();
    }

    /// <summary>
    /// Kembali ke buku planner (rencana tetap tersimpan di GameManager).
    /// </summary>
    public void OpenPlanner()
    {
        if (character != null) character.StopMoving();
        paused = true;

        if (gameManager != null) gameManager.IsGameStarted = false;

        SceneManager.LoadScene(plannerSceneName);
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

        // Fallback berdasarkan kategori untuk data lama yang nama lokasinya
        // belum cocok dengan LocationPoint di scene.
        string keyword = activity.category == "Fieldwork" ? "gather"
            : activity.category == "Guild / Organization" ? "guild"
            : "home";

        foreach (LocationPoint point in locations)
        {
            if (Normalize(point.LocationName).Contains(keyword)) return point;
        }

        return locations.Count > 0 ? locations[0] : null;
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
        if (pauseButton != null) pauseButton.SetActive(canPause);
        if (nextDayButton != null) nextDayButton.SetActive(showNextDay);
    }

    private void UpdatePauseLabel()
    {
        if (pauseLabel != null) pauseLabel.text = paused ? "Resume" : "Pause";
    }
}
