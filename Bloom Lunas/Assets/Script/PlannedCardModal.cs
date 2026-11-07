using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Modal pop-up untuk mengonfigurasi kartu yang sudah ditaruh di kalender (Planned Card).
/// Sesuai referensi gambar:
/// - Craft: Memilih resep (Kunyit asam, Beras kencur, Turmeric salep, Lemongrass balm) & cek stok bahan.
/// - Processing: Memilih station (Home atelier vs Home drying yard).
/// - Open shop & Exploration: Memilih durasi (1 slot = 3h vs 2 slots = 6h).
/// </summary>
public class PlannedCardModal : MonoBehaviour
{
    private static PlannedCardModal instance;
    public static PlannedCardModal Instance => instance;

    [Header("UI Root")]
    [SerializeField] private GameObject modalRoot;
    [SerializeField] private RectTransform panelRect;

    // Header Texts
    private TMP_Text headerTagText;
    private TMP_Text titleText;
    private TMP_Text actionSubText;

    // Detail rows
    private TMP_Text locationValueText;
    private TMP_Text lengthValueText;
    private TMP_Text openValueText;

    // Warning / Stock box
    private GameObject warningBox;
    private TMP_Text warningText;

    // Buttons
    private Button removeButton;
    private Button doneButton;
    private Button closeXButton;

    // Dynamic containers
    private GameObject optionsGroup;
    private TMP_Text optionsLabel;
    private Transform optionsContainer;

    private DraggableCard currentCard;
    private PlannerBookUI planner;

    private void Awake()
    {
        instance = this;
        EnsureUI();
        if (modalRoot != null) modalRoot.SetActive(false);
    }

    /// <summary>
    /// Membuka modal untuk kartu tertentu.
    /// </summary>
    public void Open(DraggableCard card, PlannerBookUI owner)
    {
        if (card == null || card.Activity == null || card.CurrentSlot == null) return;

        currentCard = card;
        planner = owner;

        EnsureUI();
        RefreshContent();

        if (modalRoot != null)
        {
            modalRoot.SetActive(true);
            modalRoot.transform.SetAsLastSibling();
        }
    }

    public void Close()
    {
        SaveCard();
        if (modalRoot != null) modalRoot.SetActive(false);
        currentCard = null;
        if (planner != null)
        {
            planner.RefreshAll();
            planner.RefreshSlotVisuals();
        }
    }

    /// <summary>Menyimpan perubahan opsi kartu ke rencana mingguan.</summary>
    private void SaveCard()
    {
        if (currentCard == null || currentCard.CurrentSlot == null) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SavePlan(currentCard.CurrentSlot.dayIndex, currentCard.CurrentSlot.timeSlot, currentCard.Activity);
        }

        currentCard.RefreshCardVisual();
        if (planner != null) planner.RefreshSlotVisuals();
    }

    private void RefreshContent()
    {
        if (currentCard == null || currentCard.Activity == null || currentCard.CurrentSlot == null) return;

        ActivityData act = currentCard.Activity;
        DropSlot slot = currentCard.CurrentSlot;

        string dayName = GameManager.DayShortNames[Mathf.Clamp(slot.dayIndex, 0, 6)].ToUpperInvariant();
        string timeSpan = $"{slot.timeSlot}-{(act.slotsCount == 2 ? GetNextSlotTime(slot.timeSlot) : GetSlotEndTime(slot.timeSlot))}";

        if (headerTagText != null)
        {
            headerTagText.text = $"PLANNED CARD - {dayName} {timeSpan}";
        }

        if (titleText != null)
        {
            titleText.text = act.activityName;
        }

        string actName = act.activityName.ToLowerInvariant();

        // Subtitle Actions
        if (actionSubText != null)
        {
            if (actName.Contains("craft"))
                actionSubText.text = "Actions: Prepare, Brew, Concoct, Package.";
            else if (actName.Contains("shop"))
                actionSubText.text = "Actions: Serve customers, Restock shelf.";
            else if (actName.Contains("exploration") || actName.Contains("forage"))
                actionSubText.text = "Actions: Gather fresh herbs, Harvest wild plants.";
            else if (actName.Contains("processing"))
                actionSubText.text = "Actions: Dry herbs, Grind roots, Distill oil.";
            else
                actionSubText.text = "Actions: Meet, Coordinate, Trade.";
        }

        // Info Rows
        if (locationValueText != null) locationValueText.text = act.locationName;
        if (lengthValueText != null) lengthValueText.text = $"{act.slotsCount * 3} h";

        if (openValueText != null)
        {
            if (actName.Contains("shop")) openValueText.text = "Mon-Sat 8-19";
            else if (actName.Contains("exploration")) openValueText.text = "Daily 6-15";
            else if (actName.Contains("visit")) openValueText.text = "Mon, Wed, Sat 8-12";
            else openValueText.text = "Daily";
        }

        BuildCustomOptions(act);
    }

    private void BuildCustomOptions(ActivityData act)
    {
        // Bersihkan tombol opsi sebelumnya
        foreach (Transform child in optionsContainer)
        {
            Destroy(child.gameObject);
        }

        string actName = act.activityName.ToLowerInvariant();

        // 1. KASUS CRAFT: Pilihan 4 Resep
        if (actName.Contains("craft"))
        {
            if (optionsLabel != null) optionsLabel.text = "Recipe";
            if (optionsGroup != null) optionsGroup.SetActive(true);

            foreach (var kvp in GameManager.CraftRecipes)
            {
                string rName = kvp.Key;
                CraftRecipe rData = kvp.Value;
                bool isSelected = act.selectedRecipe == rName;

                string btnText = $"{rName} ({rData.slotsRequired} slot{(rData.slotsRequired > 1 ? "s" : "")})";
                Button btn = CreatePillButton(btnText, isSelected, () =>
                {
                    act.selectedRecipe = rName;
                    act.slotsCount = 1;
                    act.subtext = $"{rName} - 3h";
                    SaveCard();
                    RefreshContent();
                });
            }

            // Tampilkan warning box cek stok bahan
            UpdateCraftStockWarning(act.selectedRecipe);
        }
        // 2. KASUS PROCESSING: Pilihan Station (Home atelier vs Home drying yard)
        else if (actName.Contains("processing"))
        {
            if (optionsLabel != null) optionsLabel.text = "Station";
            if (optionsGroup != null) optionsGroup.SetActive(true);

            string[] stations = { "Home atelier", "Home drying yard" };
            foreach (string station in stations)
            {
                bool isSelected = act.selectedStation == station;
                CreatePillButton(station, isSelected, () =>
                {
                    act.selectedStation = station;
                    act.locationName = station;
                    act.subtext = $"3h - {station}";
                    SaveCard();
                    RefreshContent();
                });
            }

            if (warningBox != null) warningBox.SetActive(false);
        }
        // 3. KASUS OPEN SHOP ATAU EXPLORATION: Pilihan Durasi (1 slot vs 2 slots)
        else if (actName.Contains("shop") || actName.Contains("exploration"))
        {
            if (optionsLabel != null) optionsLabel.text = "Length";
            if (optionsGroup != null) optionsGroup.SetActive(true);

            int[] slotsChoices = { 1, 2 };
            foreach (int s in slotsChoices)
            {
                bool isSelected = act.slotsCount == s;
                string label = $"{s} slot{(s > 1 ? "s" : "")} ({s * 3}h)";
                CreatePillButton(label, isSelected, () =>
                {
                    if (s == act.slotsCount)
                    {
                        RefreshContent();
                        return;
                    }

                    if (s > 1 && currentCard != null && currentCard.CurrentSlot != null && GameManager.Instance != null &&
                        !GameManager.Instance.CanChangeToSlotsCount(currentCard.CurrentSlot.dayIndex, currentCard.CurrentSlot.timeSlot, act, s, out string reason))
                    {
                        if (planner != null) planner.ShowToast(reason);
                        RefreshContent();
                        return;
                    }

                    act.slotsCount = s;
                    act.subtext = $"{s * 3}h - {act.locationName}";
                    SaveCard();
                    RefreshContent();
                });
            }

            if (warningBox != null) warningBox.SetActive(false);
        }
        else
        {
            if (optionsGroup != null) optionsGroup.SetActive(false);
            if (warningBox != null) warningBox.SetActive(false);
        }
    }

    private void UpdateCraftStockWarning(string recipeName)
    {
        if (warningBox == null || warningText == null) return;

        if (GameManager.Instance == null)
        {
            warningBox.SetActive(false);
            return;
        }

        bool hasAll = GameManager.Instance.HasIngredients(recipeName, out List<string> missing);
        if (!hasAll && missing.Count > 0)
        {
            warningBox.SetActive(true);
            warningText.text = $"Not in stock: {string.Join(", ", missing)}.";
            warningText.color = new Color(0.686f, 0.227f, 0.208f, 1f);
            warningBox.GetComponent<Image>().color = new Color(0.976f, 0.882f, 0.871f, 1f);
        }
        else
        {
            warningBox.SetActive(true);
            warningText.text = "Ready: all ingredients are in stock.";
            warningText.color = new Color(0.208f, 0.525f, 0.239f, 1f);
            warningBox.GetComponent<Image>().color = new Color(0.898f, 0.953f, 0.898f, 1f);
        }
    }

    private Button CreatePillButton(string text, bool isSelected, Action onClick)
    {
        GameObject btnObj = new GameObject("PillBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        btnObj.transform.SetParent(optionsContainer, false);

        LayoutElement le = btnObj.GetComponent<LayoutElement>();
        le.minHeight = 32;
        le.preferredHeight = 32;

        Image img = btnObj.GetComponent<Image>();
        img.color = isSelected ? new Color(0.18f, 0.16f, 0.12f, 1f) : new Color(1f, 1f, 1f, 0.95f);

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = new Vector2(-16, 0);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 13;
        tmp.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = isSelected ? Color.white : new Color(0.25f, 0.22f, 0.18f, 1f);

        return btn;
    }

    private void RemoveCurrentCard()
    {
        if (currentCard != null)
        {
            currentCard.DeleteCard();
        }
        Close();
    }

    private string GetSlotEndTime(string startTime)
    {
        int hour = ParseHour(startTime);
        return $"{(hour + 3) % 24:00}:00";
    }

    private string GetNextSlotTime(string startTime)
    {
        int hour = ParseHour(startTime);
        return $"{(hour + 6) % 24:00}:00";
    }

    private int ParseHour(string timeStr)
    {
        if (string.IsNullOrEmpty(timeStr)) return 6;
        string[] p = timeStr.Split(':');
        if (p.Length > 0 && int.TryParse(p[0], out int h)) return h;
        return 6;
    }

    #region Dynamic UI Builder (Fallback jika belum ada di scene)

    private void EnsureUI()
    {
        if (modalRoot != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // 1. Overlay Fullscreen
        modalRoot = new GameObject("PlannedCardModal_Root", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modalRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRt = modalRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;

        Image overlayImg = modalRoot.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.45f);

        Button bgDismiss = modalRoot.AddComponent<Button>();
        bgDismiss.onClick.AddListener(Close);

        // 2. Main Card Panel
        GameObject panelObj = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
        panelObj.transform.SetParent(modalRoot.transform, false);

        panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(460, 480);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);

        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color(0.988f, 0.976f, 0.941f, 1f);

        VerticalLayoutGroup vlg = panelObj.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(24, 24, 20, 20);
        vlg.spacing = 10;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // Header
        headerTagText = AddLabel(panelObj, "PLANNED CARD - MON 09:00-12:00", 11, new Color(0.5f, 0.47f, 0.4f, 1f), FontStyles.Bold);
        titleText = AddLabel(panelObj, "Craft", 22, new Color(0.2f, 0.17f, 0.12f, 1f), FontStyles.Bold);
        actionSubText = AddLabel(panelObj, "Actions: Prepare, Brew, Concoct.", 12, new Color(0.4f, 0.37f, 0.32f, 1f), FontStyles.Normal);

        // Options Box Container
        optionsGroup = new GameObject("OptionsGroup", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(Image));
        optionsGroup.transform.SetParent(panelObj.transform, false);
        optionsGroup.GetComponent<Image>().color = new Color(0.941f, 0.918f, 0.855f, 1f);

        VerticalLayoutGroup optVlg = optionsGroup.GetComponent<VerticalLayoutGroup>();
        optVlg.padding = new RectOffset(14, 14, 12, 12);
        optVlg.spacing = 8;
        optVlg.childControlWidth = true;
        optVlg.childControlHeight = false;

        optionsLabel = AddLabel(optionsGroup, "Recipe", 12, new Color(0.35f, 0.32f, 0.27f, 1f), FontStyles.Bold);

        GameObject optContainerObj = new GameObject("Container", typeof(RectTransform), typeof(VerticalLayoutGroup));
        optContainerObj.transform.SetParent(optionsGroup.transform, false);
        VerticalLayoutGroup contVlg = optContainerObj.GetComponent<VerticalLayoutGroup>();
        contVlg.spacing = 6;
        contVlg.childControlWidth = true;
        contVlg.childControlHeight = false;
        optionsContainer = optContainerObj.transform;

        // Info Rows (Location, Length, Open)
        locationValueText = AddRow(panelObj, "Location", "Home atelier");
        lengthValueText = AddRow(panelObj, "Length", "3 h");
        openValueText = AddRow(panelObj, "Open", "Daily");

        // Warning Box
        warningBox = new GameObject("WarningBox", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        warningBox.transform.SetParent(panelObj.transform, false);
        warningBox.GetComponent<Image>().color = new Color(0.976f, 0.882f, 0.871f, 1f);

        HorizontalLayoutGroup wHlg = warningBox.GetComponent<HorizontalLayoutGroup>();
        wHlg.padding = new RectOffset(12, 12, 8, 8);
        wHlg.childControlWidth = true;
        wHlg.childControlHeight = false;
        warningText = AddLabel(warningBox, "Not in stock: ...", 11, new Color(0.686f, 0.227f, 0.208f, 1f), FontStyles.Normal);

        // Bottom Buttons Row
        GameObject btnRow = new GameObject("BtnRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        btnRow.transform.SetParent(panelObj.transform, false);
        HorizontalLayoutGroup bHlg = btnRow.GetComponent<HorizontalLayoutGroup>();
        bHlg.spacing = 16;
        bHlg.childControlWidth = true;
        bHlg.childControlHeight = false;

        // Remove Button
        removeButton = AddActionBtn(btnRow, "Remove", new Color(0.98f, 0.96f, 0.94f, 1f), new Color(0.7f, 0.25f, 0.22f, 1f), RemoveCurrentCard);
        // Done Button
        doneButton = AddActionBtn(btnRow, "Done", new Color(0.91f, 0.72f, 0.35f, 1f), new Color(0.2f, 0.15f, 0.05f, 1f), Close);
    }

    private TMP_Text AddLabel(GameObject parent, string text, float size, Color color, FontStyles style)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent.transform, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        return tmp;
    }

    private TMP_Text AddRow(GameObject parent, string title, string defaultVal)
    {
        GameObject row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent.transform, false);
        HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        TMP_Text t1 = AddLabel(row, title, 13, new Color(0.45f, 0.42f, 0.37f, 1f), FontStyles.Normal);
        t1.rectTransform.sizeDelta = new Vector2(120, 22);

        TMP_Text t2 = AddLabel(row, defaultVal, 13, new Color(0.2f, 0.17f, 0.12f, 1f), FontStyles.Bold);
        t2.rectTransform.sizeDelta = new Vector2(250, 22);
        return t2;
    }

    private Button AddActionBtn(GameObject parent, string text, Color bg, Color txtColor, Action onClick)
    {
        GameObject go = new GameObject(text + "Btn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent.transform, false);

        LayoutElement le = go.GetComponent<LayoutElement>();
        le.minHeight = 38;
        le.preferredHeight = 38;

        Image img = go.GetComponent<Image>();
        img.color = bg;

        Button b = go.GetComponent<Button>();
        b.onClick.AddListener(() => onClick?.Invoke());

        GameObject to = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        to.transform.SetParent(go.transform, false);
        RectTransform tr = to.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = to.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = txtColor;
        tmp.alignment = TextAlignmentOptions.Center;

        return b;
    }

    #endregion
}

