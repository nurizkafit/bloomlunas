using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlannerBookUI : MonoBehaviour
{
    [Header("Book Section Tabs (Halaman Buku)")]
    [SerializeField] private GameObject panelWeek;
    [SerializeField] private GameObject panelLedger;
    [SerializeField] private GameObject panelMarket;
    [SerializeField] private GameObject panelTown;
    [SerializeField] private GameObject panelJournal;

    [Header("Deck Panel Kanan (Card Deck)")]
    [Tooltip("Panel yang berisi 4 kategori utama (Production, Shop, Fieldwork, Guild)")]
    [SerializeField] private GameObject categoriesView;

    [Tooltip("Panel yang berisi kartu-kartu setelah salah satu kategori dipilih")]
    [SerializeField] private GameObject cardsView;

    [Tooltip("Judul kategori di atas daftar kartu")]
    [SerializeField] private TMP_Text categoryTitleText;

    [Tooltip("Tombol kembali ke daftar semua kategori")]
    [SerializeField] private Button backToCategoriesButton;

    [Tooltip("Parent/Container tempat kartu-kartu dimunculkan di deck")]
    [SerializeField] private Transform cardsContainer;

    [Tooltip("Prefab kartu yang dapat di-drag (Card_Prefab)")]
    [SerializeField] private GameObject cardPrefab;

    [Header("Review Plan Modal (Pop up Ringkasan)")]
    [SerializeField] private GameObject reviewModal;
    [SerializeField] private Button openReviewButton;
    [SerializeField] private Button startAnywayButton;
    [SerializeField] private Button closeReviewButton;
    [SerializeField] private TMP_Text reviewWarningsText;

    [Header("Daftar Aktivitas Bawaan")]
    [SerializeField] private List<ActivityData> defaultActivities = new List<ActivityData>();

    private void Awake()
    {
        // Setup tombol navigasi kategori
        if (backToCategoriesButton != null)
        {
            backToCategoriesButton.onClick.AddListener(ShowCategoriesList);
        }

        // Setup tombol review modal
        if (openReviewButton != null)
        {
            openReviewButton.onClick.AddListener(OpenReviewModal);
        }

        if (startAnywayButton != null)
        {
            startAnywayButton.onClick.AddListener(OnStartAnywayClicked);
        }

        if (closeReviewButton != null)
        {
            closeReviewButton.onClick.AddListener(CloseReviewModal);
        }

        if (reviewModal != null)
        {
            reviewModal.SetActive(false);
        }

        // Siapkan aktivitas bawaan jika daftar kosong
        InitDefaultActivities();

        // Tampilkan halaman Week dan daftar kategori saat awal dibuka
        OpenWeekTab();
        ShowCategoriesList();
    }

    private void InitDefaultActivities()
    {
        if (defaultActivities.Count > 0) return;

        // Kategori Fieldwork
        defaultActivities.Add(new ActivityData(
            "exploration",
            "Exploration",
            "Fieldwork",
            "Gathering area",
            "3h • Gathering area (City zone)",
            new Color(0.92f, 0.96f, 0.90f, 1f)
        ));

        // Kategori Production
        defaultActivities.Add(new ActivityData(
            "craft",
            "Craft",
            "Production",
            "unit-house",
            "Turmeric salep - 4 Salep",
            new Color(0.98f, 0.95f, 0.88f, 1f)
        ));
        defaultActivities.Add(new ActivityData(
            "processing",
            "Processing",
            "Production",
            "unit-house",
            "3h • Home atelier",
            new Color(0.98f, 0.95f, 0.88f, 1f)
        ));

        // Kategori Shop
        defaultActivities.Add(new ActivityData(
            "open_shop",
            "Open shop",
            "Shop",
            "unit-house",
            "6h • Home shop",
            new Color(0.99f, 0.96f, 0.92f, 1f)
        ));

        // Kategori Guild / Organization
        defaultActivities.Add(new ActivityData(
            "appointment",
            "Appointment",
            "Guild / Organization",
            "unit-mill",
            "3h • Merchant Guild",
            new Color(0.93f, 0.92f, 0.98f, 1f)
        ));
        defaultActivities.Add(new ActivityData(
            "commission",
            "Commission",
            "Guild / Organization",
            "building-archery",
            "1 slot • Mon-Sat 8-17",
            new Color(0.93f, 0.92f, 0.98f, 1f)
        ));
        defaultActivities.Add(new ActivityData(
            "visit",
            "Visit",
            "Guild / Organization",
            "building-farm",
            "1 slot • Mon-Wed-Sat 8-12",
            new Color(0.93f, 0.92f, 0.98f, 1f)
        ));
    }

    #region Tab Navigation
    public void OpenWeekTab() => SwitchTab(panelWeek);
    public void OpenLedgerTab() => SwitchTab(panelLedger);
    public void OpenMarketTab() => SwitchTab(panelMarket);
    public void OpenTownTab() => SwitchTab(panelTown);
    public void OpenJournalTab() => SwitchTab(panelJournal);

    private void SwitchTab(GameObject activePanel)
    {
        if (panelWeek != null) panelWeek.SetActive(panelWeek == activePanel);
        if (panelLedger != null) panelLedger.SetActive(panelLedger == activePanel);
        if (panelMarket != null) panelMarket.SetActive(panelMarket == activePanel);
        if (panelTown != null) panelTown.SetActive(panelTown == activePanel);
        if (panelJournal != null) panelJournal.SetActive(panelJournal == activePanel);
    }
    #endregion

    #region Card Deck Category Navigation
    /// <summary>
    /// Menampilkan kembali pilihan 4 kategori (Production, Shop, Fieldwork, Guild).
    /// </summary>
    public void ShowCategoriesList()
    {
        if (categoriesView != null) categoriesView.SetActive(true);
        if (cardsView != null) cardsView.SetActive(false);
    }

    /// <summary>
    /// Dipanggil saat pemain mengklik tombol salah satu kategori (misal: "Guild / Organization").
    /// </summary>
    public void SelectCategory(string categoryName)
    {
        if (categoriesView != null) categoriesView.SetActive(false);
        if (cardsView != null) cardsView.SetActive(true);

        if (categoryTitleText != null)
        {
            categoryTitleText.text = categoryName;
        }

        PopulateCardsForCategory(categoryName);
    }

    private void PopulateCardsForCategory(string categoryName)
    {
        if (cardsContainer == null || cardPrefab == null) return;

        // Bersihkan kartu-kartu sebelumnya di deck container
        foreach (Transform child in cardsContainer)
        {
            Destroy(child.gameObject);
        }

        // Buat kartu template untuk kategori yang dipilih
        foreach (var act in defaultActivities)
        {
            if (act.category.Equals(categoryName, StringComparison.OrdinalIgnoreCase) ||
                categoryName.Contains(act.category) || act.category.Contains(categoryName))
            {
                GameObject cardObj = Instantiate(cardPrefab, cardsContainer);
                cardObj.transform.localScale = Vector3.one;

                DraggableCard card = cardObj.GetComponent<DraggableCard>();
                if (card != null)
                {
                    card.isDeckTemplate = true; // Tandai kartu ini sebagai template yang bisa ditarik
                    card.SetData(act);
                }
            }
        }
    }
    #endregion

    #region Review Plan Modal
    public void OpenReviewModal()
    {
        if (reviewModal != null)
        {
            reviewModal.SetActive(true);
        }

        // Tampilkan informasi ringkasan atau peringatan jika diperlukan
        if (reviewWarningsText != null && GameManager.Instance != null)
        {
            var allPlans = GameManager.Instance.GetAllPlans();
            if (allPlans.Count == 0)
            {
                reviewWarningsText.text = "Perhatian: Anda belum menjadwalkan aktivitas apa pun minggu ini!";
            }
            else
            {
                reviewWarningsText.text = $"Total {allPlans.Count} aktivitas siap dijalankan minggu ini.";
            }
        }
    }

    public void CloseReviewModal()
    {
        if (reviewModal != null)
        {
            reviewModal.SetActive(false);
        }
    }

    public void OnStartAnywayClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGameLoop();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("CityOverWorld");
        }
    }
    #endregion
}

