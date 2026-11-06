using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Mengatur seluruh isi buku planner: tab halaman, card deck di panel kanan,
/// ringkasan jumlah slot per hari, Review Plan, dan perpindahan ke CityOverWorld.
/// </summary>
public class PlannerBookUI : MonoBehaviour
{
    public enum PlannerTab { Week, Ledger, Market, Town, Journal }

    [Serializable]
    public class DayView
    {
        public int dayIndex;
        public TMP_Text slotsText;
        public GameObject badgeRoot;
        public TMP_Text badgeText;
    }

    [Serializable]
    public class CategoryView
    {
        public string categoryName;
        public Button button;
        public TMP_Text countText;
    }

    [Serializable]
    public class BookTab
    {
        public PlannerTab tab;
        public Button button;
        public Image background;
        public TMP_Text title;
        public Color activeColor = Color.white;
        public Color idleColor = Color.white;
    }

    [Header("Halaman Buku (Root)")]
    [SerializeField] private GameObject weekPage;
    [SerializeField] private GameObject ledgerPage;
    [SerializeField] private GameObject marketPage;
    [SerializeField] private GameObject townPage;
    [SerializeField] private GameObject journalPage;
    [SerializeField] private TMP_Text ledgerBodyText;
    [SerializeField] private TMP_Text marketBodyText;
    [SerializeField] private TMP_Text townBodyText;
    [SerializeField] private TMP_Text journalBodyText;
    [SerializeField] private List<BookTab> bookTabs = new List<BookTab>();

    [Header("Header / Statistik")]
    [SerializeField] private TMP_Text weekLabelText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text debtText;
    [SerializeField] private TMP_Text nextPaymentText;
    [SerializeField] private TMP_Text nextPaymentSubText;
    [SerializeField] private TMP_Text livingCostText;
    [SerializeField] private TMP_Text livingCostSubText;
    [SerializeField] private TMP_Text goldAfterText;
    [SerializeField] private TMP_Text goldAfterSubText;
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private Image energyFill;
    [SerializeField] private TMP_Text warningChipText;
    [SerializeField] private Image warningChipBackground;
    [SerializeField] private Button openReviewButton;

    [Header("Kalender")]
    [SerializeField] private List<DayView> dayViews = new List<DayView>();

    [Header("Card Deck")]
    [SerializeField] private GameObject categoriesView;
    [SerializeField] private GameObject cardsView;
    [SerializeField] private TMP_Text categoryTitleText;
    [SerializeField] private TMP_Text deckHintText;
    [SerializeField] private TMP_Text selectedSlotText;
    [SerializeField] private Button backToCategoriesButton;
    [SerializeField] private Transform cardsContainer;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private List<CategoryView> categoryViews = new List<CategoryView>();
    [SerializeField] private PlannedCardModal plannedCardModal;

    [Header("Review Plan")]
    [SerializeField] private GameObject reviewModal;
    [SerializeField] private TMP_Text reviewSummaryText;
    [SerializeField] private TMP_Text reviewWarningsText;
    [SerializeField] private Button startAnywayButton;
    [SerializeField] private Button closeReviewButton;

    [Header("Toast")]
    [SerializeField] private GameObject toastRoot;
    [SerializeField] private TMP_Text toastText;
    [SerializeField] private CanvasGroup toastCanvasGroup;

    [Header("Daftar Aktivitas Bawaan")]
    [SerializeField] private List<ActivityData> defaultActivities = new List<ActivityData>();

    private readonly List<DropSlot> slots = new List<DropSlot>();
    private DropSlot selectedSlot;
    private Coroutine toastRoutine;

    private const string DefaultDeckHint = "Drag a card into a slot, or click a slot first and then a card.";

    #region Unity Lifecycle

    private void Awake()
    {
        SetupButtons();
        InitDefaultActivities();

        if (reviewModal != null) reviewModal.SetActive(false);
        if (toastRoot != null) toastRoot.SetActive(false);

        // Kumpulkan semua slot kalender di halaman Week.
        slots.Clear();
        if (weekPage != null)
        {
            foreach (DropSlot slot in weekPage.GetComponentsInChildren<DropSlot>(true))
            {
                slot.Setup(this);
                slots.Add(slot);
            }
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            // Scene ini adalah fase planning: jadwal harus selalu bisa diedit,
            // meskipun nilai IsGameStarted pada scene masih tersimpan true.
            GameManager.Instance.IsGameStarted = false;
            GameManager.Instance.PlanChanged += OnPlanChanged;
        }

        OpenWeekTab();
        ShowCategoriesList();

        if (deckHintText != null) deckHintText.text = DefaultDeckHint;
        RefreshAll();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlanChanged -= OnPlanChanged;
        }
    }

    private void Update()
    {
        if (reviewModal == null || !reviewModal.activeSelf) return;

#if ENABLE_INPUT_SYSTEM
        bool escape = UnityEngine.InputSystem.Keyboard.current != null &&
                      UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        bool escape = Input.GetKeyDown(KeyCode.Escape);
#endif
        if (escape) CloseReviewModal();
    }

    private void OnPlanChanged()
    {
        RefreshAll();
    }

    #endregion

    #region Setup

    private void SetupButtons()
    {
        if (backToCategoriesButton != null)
        {
            backToCategoriesButton.onClick.RemoveAllListeners();
            backToCategoriesButton.onClick.AddListener(ShowCategoriesList);
        }

        if (openReviewButton != null)
        {
            openReviewButton.onClick.RemoveAllListeners();
            openReviewButton.onClick.AddListener(OpenReviewModal);
        }

        if (startAnywayButton != null)
        {
            startAnywayButton.onClick.RemoveAllListeners();
            startAnywayButton.onClick.AddListener(OnStartAnywayClicked);
        }

        if (closeReviewButton != null)
        {
            closeReviewButton.onClick.RemoveAllListeners();
            closeReviewButton.onClick.AddListener(CloseReviewModal);
        }

        foreach (CategoryView view in categoryViews)
        {
            if (view == null || view.button == null) continue;

            CategoryView captured = view;
            view.button.onClick.RemoveAllListeners();
            view.button.onClick.AddListener(() => SelectCategory(captured.categoryName));
        }

        foreach (BookTab tab in bookTabs)
        {
            if (tab == null || tab.button == null) continue;

            PlannerTab capturedTab = tab.tab;
            tab.button.onClick.RemoveAllListeners();
            tab.button.onClick.AddListener(() => SwitchTab(capturedTab));
        }
    }

    private void InitDefaultActivities()
    {
        if (defaultActivities.Count > 0) return;

        Color production = new Color(0.976f, 0.914f, 0.804f, 1f);
        Color shop = new Color(0.898f, 0.937f, 0.867f, 1f);
        Color fieldwork = new Color(0.886f, 0.941f, 0.902f, 1f);
        Color guild = new Color(0.910f, 0.894f, 0.963f, 1f);

        // Nama lokasi harus sama dengan LocationPoint yang ada di CityOverWorld:
        // Home, Gathering area, Adventurer Guild, Home drying yard, Market.

        // Production
        defaultActivities.Add(new ActivityData("craft", "Craft", "Production", "Home",
            "3h - Home atelier", production));
        defaultActivities.Add(new ActivityData("processing", "Processing", "Production", "Home",
            "3h - Home atelier", production));

        // Shop
        defaultActivities.Add(new ActivityData("open_shop", "Open shop", "Shop", "Home",
            "6h - Home shop", shop));

        // Fieldwork
        defaultActivities.Add(new ActivityData("exploration", "Exploration", "Fieldwork", "Gathering area",
            "3h - Gathering area (City zone)", fieldwork));
        defaultActivities.Add(new ActivityData("forage", "Forage", "Fieldwork", "Gathering area",
            "2h - Gathering area", fieldwork));

        // Guild / Organization
        defaultActivities.Add(new ActivityData("appointment", "Appointment", "Guild / Organization", "Adventurer Guild",
            "3h - Adventurer Guild", guild));
        defaultActivities.Add(new ActivityData("commission", "Commission", "Guild / Organization", "Adventurer Guild",
            "1 slot - Mon-Sat 8-17", guild));
        defaultActivities.Add(new ActivityData("visit", "Visit", "Guild / Organization", "Home drying yard",
            "1 slot - Mon-Wed-Sat 8-12", guild));
    }

    #endregion

    #region Book Tabs

    public void OpenWeekTab() => SwitchTab(PlannerTab.Week);
    public void OpenLedgerTab() => SwitchTab(PlannerTab.Ledger);
    public void OpenMarketTab() => SwitchTab(PlannerTab.Market);
    public void OpenTownTab() => SwitchTab(PlannerTab.Town);
    public void OpenJournalTab() => SwitchTab(PlannerTab.Journal);

    public void SwitchTab(PlannerTab tab)
    {
        if (weekPage != null) weekPage.SetActive(tab == PlannerTab.Week);
        if (ledgerPage != null) ledgerPage.SetActive(tab == PlannerTab.Ledger);
        if (marketPage != null) marketPage.SetActive(tab == PlannerTab.Market);
        if (townPage != null) townPage.SetActive(tab == PlannerTab.Town);
        if (journalPage != null) journalPage.SetActive(tab == PlannerTab.Journal);

        foreach (BookTab bookTab in bookTabs)
        {
            if (bookTab == null) continue;

            bool active = bookTab.tab == tab;
            if (bookTab.background != null)
            {
                bookTab.background.color = active ? bookTab.activeColor : bookTab.idleColor;
            }

            if (bookTab.title != null)
            {
                bookTab.title.color = active ? new Color(0.208f, 0.176f, 0.122f, 1f)
                                             : new Color(0.365f, 0.333f, 0.259f, 1f);
            }
        }

        RefreshAll();
    }

    #endregion

    #region Day Summaries & Header

    public void RefreshAll()
    {
        RefreshHeader();
        RefreshDaySummaries();
        RefreshCategoryCounts();
        RefreshLedger();
        RefreshMarket();
        RefreshTown();
        RefreshJournal();
        RefreshSelectedSlot();
    }

    private void RefreshHeader()
    {
        GameManager gm = GameManager.Instance;

        int week = gm != null ? gm.CurrentWeek : 1;
        if (weekLabelText != null)
        {
            weekLabelText.text = $"MONTH 1  -  WEEK {week}  -  DAY 1 - 7";
        }

        if (goldText != null) goldText.text = gm != null ? $"{gm.Gold:N0} G" : "240 G";
        if (debtText != null) debtText.text = gm != null ? $"{gm.Debt:N0} G" : "3,000 G";

        if (nextPaymentText != null) nextPaymentText.text = gm != null ? $"{gm.NextPayment:N0} G" : "420 G";
        if (nextPaymentSubText != null)
        {
            nextPaymentSubText.text = gm != null
                ? $"Month end (day 28) - in {gm.NextPaymentInDays} days"
                : "Month end (day 28) - in 28 days";
        }

        if (livingCostText != null) livingCostText.text = gm != null ? $"{gm.LivingCost:N0} G" : "40 G";
        if (livingCostSubText != null) livingCostSubText.text = "paid in Check";

        if (goldAfterText != null)
        {
            goldAfterText.text = gm != null
                ? $"{gm.GoldAfterPlanMin:N0} - {gm.GoldAfterPlanMax:N0} G"
                : "239 - 264 G";
        }
        if (goldAfterSubText != null) goldAfterSubText.text = "end of week forecast";

        int energy = gm != null ? gm.Energy : 100;
        int energyMax = gm != null ? gm.EnergyMax : 100;
        if (energyText != null) energyText.text = $"{energy} / {energyMax}";

        if (energyFill != null)
        {
            RectTransform fill = energyFill.rectTransform;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(Mathf.Clamp01((float)energy / energyMax), 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }

        List<string> warnings = BuildWarnings(out int problems);

        if (warningChipText != null)
        {
            if (problems > 0)
            {
                warningChipText.text = problems == 1 ? "1 problem" : $"{problems} problems";
                warningChipText.color = new Color(0.925f, 0.737f, 0.702f, 1f);
            }
            else if (warnings.Count > 0)
            {
                warningChipText.text = warnings.Count == 1 ? "1 reminder" : $"{warnings.Count} reminders";
                warningChipText.color = new Color(0.925f, 0.847f, 0.663f, 1f);
            }
            else
            {
                warningChipText.text = "Plan looks good";
                warningChipText.color = new Color(0.718f, 0.859f, 0.710f, 1f);
            }
        }

        if (warningChipBackground != null)
        {
            warningChipBackground.color = problems > 0
                ? new Color(0.239f, 0.145f, 0.145f, 0.9f)
                : new Color(0.129f, 0.180f, 0.263f, 0.9f);
        }
    }

    private void RefreshDaySummaries()
    {
        GameManager gm = GameManager.Instance;

        foreach (DayView view in dayViews)
        {
            if (view == null) continue;

            int count = gm != null ? gm.GetPlanCountForDay(view.dayIndex) : 0;

            if (view.slotsText != null)
            {
                view.slotsText.text = count == 1 ? "1 slot of work" : $"{count} slots of work";
                view.slotsText.color = count == 0
                    ? new Color(0.651f, 0.612f, 0.525f, 1f)
                    : new Color(0.435f, 0.400f, 0.318f, 1f);
            }
        }
    }

    private List<string> BuildWarnings(out int problems)
    {
        GameManager gm = GameManager.Instance;
        List<string> warnings = new List<string>();
        problems = 0;

        if (gm == null) return warnings;

        int total = gm.TotalPlanCount;
        if (total == 0)
        {
            problems = 1;
            warnings.Add("No activities are scheduled yet.");
            return warnings;
        }

        for (int day = 0; day < 7; day++)
        {
            int count = gm.GetPlanCountForDay(day);
            string name = GameManager.DayFullNames[day];

            if (count == 0)
            {
                warnings.Add($"{name} is still empty.");
            }
            else if (gm.GetWorkPlanCountForDay(day) == 0)
            {
                warnings.Add($"{name} has no paid work planned.");
            }
        }

        return warnings;
    }

    private void RefreshCategoryCounts()
    {
        foreach (CategoryView view in categoryViews)
        {
            if (view == null || view.countText == null) continue;

            int count = 0;
            foreach (ActivityData act in defaultActivities)
            {
                if (act.category == view.categoryName) count++;
            }
            view.countText.text = count.ToString();
        }
    }

    private void RefreshSelectedSlot()
    {
        if (selectedSlotText == null) return;

        if (selectedSlot == null)
        {
            selectedSlotText.text = "No slot selected";
            return;
        }

        string day = GameManager.DayShortNames[Mathf.Clamp(selectedSlot.dayIndex, 0, 6)];
        selectedSlotText.text = $"Selected slot: {day}  -  {selectedSlot.TimeLabel}";
    }

    #endregion

    #region Card Deck

    /// <summary>
    /// Menampilkan kembali pilihan kategori (Production, Shop, Fieldwork, Guild / Organization).
    /// </summary>
    public void ShowCategoriesList()
    {
        if (categoriesView != null) categoriesView.SetActive(true);
        if (cardsView != null) cardsView.SetActive(false);
        if (backToCategoriesButton != null) backToCategoriesButton.gameObject.SetActive(false);
        if (categoryTitleText != null) categoryTitleText.text = "Categories";
    }

    /// <summary>
    /// Dipanggil saat pemain memilih salah satu kategori.
    /// </summary>
    public void SelectCategory(string categoryName)
    {
        if (string.IsNullOrEmpty(categoryName)) return;

        if (categoriesView != null) categoriesView.SetActive(false);
        if (cardsView != null) cardsView.SetActive(true);
        if (backToCategoriesButton != null) backToCategoriesButton.gameObject.SetActive(true);
        if (categoryTitleText != null) categoryTitleText.text = categoryName;

        PopulateCardsForCategory(categoryName);

        if (deckHintText != null && selectedSlot == null)
        {
            deckHintText.text = "Drag a card onto a slot, or click a slot first and then a card.";
        }
    }

    private void PopulateCardsForCategory(string categoryName)
    {
        if (cardsContainer == null || cardPrefab == null) return;

        for (int i = cardsContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = cardsContainer.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        foreach (ActivityData act in defaultActivities)
        {
            if (act.category != categoryName) continue;

            GameObject cardObj = Instantiate(cardPrefab, cardsContainer, false);
            cardObj.transform.localScale = Vector3.one;

            DraggableCard card = cardObj.GetComponent<DraggableCard>();
            if (card != null)
            {
                card.SetDeckTemplate(true);
                card.SetData(act);
            }
        }
    }

    public void OnDeckCardClicked(DraggableCard card)
    {
        if (card == null) return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameStarted) return;

        if (selectedSlot == null)
        {
            ShowToast("Click a free slot first, or drag the card onto a slot.");
            return;
        }

        if (selectedSlot.IsOccupied)
        {
            ShowToast("That slot is taken. Pick another one.");
            return;
        }

        PlaceActivity(card.Activity, selectedSlot);
    }

    public void OnScheduledCardClicked(DraggableCard card)
    {
        if (card == null || card.CurrentSlot == null) return;

        DropSlot slot = card.CurrentSlot;
        SetSelectedSlot(slot, false);

        string day = GameManager.DayShortNames[Mathf.Clamp(slot.dayIndex, 0, 6)];
        if (deckHintText != null)
        {
            deckHintText.text = $"{card.Activity.activityName}, {day} {slot.TimeLabel}. Klik untuk konfigurasi, atau drag untuk pindah.";
        }

        OpenCardModal(card);
    }

    public void OpenCardModal(DraggableCard card)
    {
        if (card == null) return;

        if (plannedCardModal == null)
        {
            plannedCardModal = FindFirstObjectByType<PlannedCardModal>();
            if (plannedCardModal == null)
            {
                GameObject obj = new GameObject("PlannedCardModal");
                obj.transform.SetParent(transform.root, false);
                plannedCardModal = obj.AddComponent<PlannedCardModal>();
            }
        }

        plannedCardModal.Open(card, this);
    }

    /// <summary>
    /// Menempatkan aktivitas baru ke slot yang dipilih.
    /// </summary>
    private void PlaceActivity(ActivityData data, DropSlot slot)
    {
        if (data == null || slot == null || slot.IsOccupied) return;
        if (cardPrefab == null) return;

        // Cek validasi aturan slot
        if (!GameManager.ValidateActivitySlot(data, slot.dayIndex, slot.timeSlot, out string rejectReason))
        {
            ShowToast(rejectReason);
            return;
        }

        GameObject cardObj = Instantiate(cardPrefab, slot.transform, false);
        DraggableCard card = cardObj.GetComponent<DraggableCard>();

        if (card == null)
        {
            Destroy(cardObj);
            return;
        }

        card.SetDeckTemplate(false);
        card.SetData(data);
        slot.Place(card);

        string day = GameManager.DayShortNames[Mathf.Clamp(slot.dayIndex, 0, 6)];
        ShowToast($"{data.activityName} ditambahkan ke {day}, {slot.TimeLabel}.");
        SetSelectedSlot(null, false);

        // Buka modal untuk kustomisasi kartu yang baru ditempatkan
        OpenCardModal(card);
    }

    public void SelectSlot(DropSlot slot)
    {
        if (slot == null) return;

        if (selectedSlot == slot)
        {
            SetSelectedSlot(null, true);
            return;
        }

        SetSelectedSlot(slot, true);
        ShowCategoriesList();

        string day = GameManager.DayShortNames[Mathf.Clamp(slot.dayIndex, 0, 6)];
        if (deckHintText != null)
        {
            deckHintText.text = $"Pick a category, then click a card for {day}, {slot.TimeLabel}.";
        }
    }

    private void SetSelectedSlot(DropSlot slot, bool updateHint)
    {
        if (selectedSlot != null && selectedSlot != slot)
        {
            selectedSlot.SetSelected(false);
        }

        selectedSlot = slot;

        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(true);
        }
        else if (updateHint && deckHintText != null)
        {
            deckHintText.text = DefaultDeckHint;
        }

        RefreshSelectedSlot();
    }

    #endregion

    #region Page Content

    private void RefreshLedger()
    {
        if (ledgerBodyText == null) return;

        GameManager gm = GameManager.Instance;
        StringBuilder sb = new StringBuilder();

        int total = gm != null ? gm.TotalPlanCount : 0;
        sb.AppendLine("WEEK AT A GLANCE");
        sb.AppendLine();

        if (gm != null)
        {
            sb.AppendLine($"Starting gold      {gm.Gold:N0} G");
            sb.AppendLine($"Debt               {gm.Debt:N0} G");
            sb.AppendLine($"Next payment       {gm.NextPayment:N0} G  (in {gm.NextPaymentInDays} days)");
            sb.AppendLine($"Weekly living cost {gm.LivingCost:N0} G");
            sb.AppendLine($"Forecast after plan {gm.GoldAfterPlanMin:N0} - {gm.GoldAfterPlanMax:N0} G");
            sb.AppendLine();
        }

        sb.AppendLine($"Scheduled activities  {total} of 56 slots");
        sb.AppendLine();

        for (int day = 0; day < 7; day++)
        {
            int count = gm != null ? gm.GetPlanCountForDay(day) : 0;
            string name = GameManager.DayFullNames[day];
            sb.AppendLine($"{name,-10} {count} slot{(count == 1 ? "" : "s")}");

            if (gm != null)
            {
                foreach (ActivityData act in gm.GetPlansForDay(day))
                {
                    sb.AppendLine($"    {act.timeSlot}  {act.activityName}  ({act.category})");
                }
            }
        }

        ledgerBodyText.text = sb.ToString();
    }

    private void RefreshMarket()
    {
        if (marketBodyText == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("PRODUCTION LINE");
        sb.AppendLine();

        foreach (ActivityData act in defaultActivities)
        {
            if (act.category != "Production" && act.category != "Shop") continue;
            sb.AppendLine($"{act.activityName,-16} {act.subtext}");
            sb.AppendLine($"    Location: {act.locationName}");
        }

        sb.AppendLine();
        sb.AppendLine("Prices and demand are recalculated when the week ends. Keep the shop open for at least one full block to catch the afternoon crowd.");

        marketBodyText.text = sb.ToString();
    }

    private void RefreshTown()
    {
        if (townBodyText == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("PLACES YOU CAN VISIT");
        sb.AppendLine();

        List<string> locations = new List<string>();
        foreach (ActivityData act in defaultActivities)
        {
            if (!locations.Contains(act.locationName)) locations.Add(act.locationName);
        }

        foreach (string location in locations)
        {
            sb.AppendLine(location.ToUpperInvariant());
            foreach (ActivityData act in defaultActivities)
            {
                if (act.locationName != location) continue;
                sb.AppendLine($"    {act.activityName,-14} {act.subtext}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("Plan a visit through the Guild / Organization category. The character walks there automatically once the week starts.");

        townBodyText.text = sb.ToString();
    }

    private void RefreshJournal()
    {
        if (journalBodyText == null) return;

        GameManager gm = GameManager.Instance;
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("WEEKLY NOTES");
        sb.AppendLine();

        if (gm == null || gm.TotalPlanCount == 0)
        {
            sb.AppendLine("Nothing written yet.");
            sb.AppendLine();
            sb.AppendLine("Tip: pick a category on the right page, then drag a card into a free slot. Click a slot first if you prefer to place cards with a single click.");
            journalBodyText.text = sb.ToString();
            return;
        }

        int total = gm.TotalPlanCount;
        sb.AppendLine($"This week holds {total} planned activit{(total == 1 ? "y" : "ies")}.");
        sb.AppendLine();

        for (int day = 0; day < 7; day++)
        {
            List<ActivityData> plans = gm.GetPlansForDay(day);
            if (plans.Count == 0) continue;

            sb.AppendLine($"Day {day + 1} - {GameManager.DayFullNames[day]}");
            foreach (ActivityData act in plans)
            {
                sb.AppendLine($"    {act.timeSlot}  {act.activityName} at {act.locationName}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("Remember to review the plan before starting the week.");

        journalBodyText.text = sb.ToString();
    }

    #endregion

    #region Review Plan Modal

    public void OpenReviewModal()
    {
        // Sembunyikan toast supaya tidak menutupi modal.
        if (toastRoutine != null)
        {
            StopCoroutine(toastRoutine);
            toastRoutine = null;
        }
        if (toastRoot != null) toastRoot.SetActive(false);

        if (reviewModal != null) reviewModal.SetActive(true);

        BuildReviewSummary();
        BuildReviewWarnings();
    }

    private void BuildReviewSummary()
    {
        if (reviewSummaryText == null) return;

        GameManager gm = GameManager.Instance;
        StringBuilder sb = new StringBuilder();

        if (gm == null || gm.TotalPlanCount == 0)
        {
            sb.AppendLine("No activities planned for this week.");
            sb.AppendLine();
            sb.AppendLine("You can still start the week and plan later, but your character will have nothing to do.");
            reviewSummaryText.text = sb.ToString();
            return;
        }

        for (int day = 0; day < 7; day++)
        {
            List<ActivityData> plans = gm.GetPlansForDay(day);
            string shortName = GameManager.DayShortNames[day].ToUpperInvariant();

            sb.Append($"<b>{shortName}</b>  <color=#8A8271>Day {day + 1} - {plans.Count} activit{(plans.Count == 1 ? "y" : "ies")}</color>");
            sb.AppendLine();

            if (plans.Count == 0)
            {
                sb.AppendLine("<color=#9A927F>    free day</color>");
            }
            else
            {
                foreach (ActivityData act in plans)
                {
                    sb.AppendLine($"    {act.timeSlot}   {act.activityName}   <color=#8A8271>{act.category}</color>");
                }
            }
            sb.AppendLine();
        }

        reviewSummaryText.text = sb.ToString();
    }

    private void BuildReviewWarnings()
    {
        if (reviewWarningsText == null) return;

        List<string> warnings = BuildWarnings(out int problems);

        if (problems == 0 && warnings.Count == 0)
        {
            reviewWarningsText.text = "No warnings. Your week is ready to run.";
            reviewWarningsText.color = new Color(0.310f, 0.451f, 0.302f, 1f);
            return;
        }

        StringBuilder sb = new StringBuilder();
        if (problems > 0)
        {
            sb.AppendLine("Start with at least one activity so the week is not wasted.");
        }

        const int maxWarnings = 3;
        for (int i = 0; i < warnings.Count && i < maxWarnings; i++)
        {
            sb.AppendLine($"- {warnings[i]}");
        }

        if (warnings.Count > maxWarnings)
        {
            sb.AppendLine($"- and {warnings.Count - maxWarnings} more empty days...");
        }

        reviewWarningsText.text = sb.ToString().TrimEnd();
        reviewWarningsText.color = new Color(0.471f, 0.333f, 0.180f, 1f);
    }

    public void CloseReviewModal()
    {
        if (reviewModal != null) reviewModal.SetActive(false);
    }

    public void OnStartAnywayClicked()
    {
        CloseReviewModal();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGameLoop();
        }
        else
        {
            SceneManager.LoadScene("CityOverWorld");
        }
    }

    #endregion

    #region Toast

    public void ShowToast(string message)
    {
        if (toastRoot == null || toastText == null) return;

        toastText.text = message;
        toastRoot.SetActive(true);

        if (toastCanvasGroup != null) toastCanvasGroup.alpha = 1f;

        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(FadeToast());
    }

    private IEnumerator FadeToast()
    {
        yield return new WaitForSecondsRealtime(2.2f);

        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (toastCanvasGroup != null)
            {
                toastCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            }
            yield return null;
        }

        if (toastRoot != null) toastRoot.SetActive(false);
        toastRoutine = null;
    }

    #endregion
}
