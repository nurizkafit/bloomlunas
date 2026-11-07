using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Kartu aktivitas. Bisa berupa template di Card Deck (kanan buku) atau kartu
/// yang sudah ditempatkan di salah satu slot kalender.
/// </summary>
public class DraggableCard : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Mode Kartu")]
    [Tooltip("Jika dicentang, kartu ini bertindak sebagai template di Card Deck (saat di-drag akan membuat kartu baru ke kalender)")]
    public bool isDeckTemplate = false;

    [Header("Visual Tampilan Kartu")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image accentBar;
    [SerializeField] private Button deleteButton;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Data Aktivitas")]
    [SerializeField] private ActivityData activity = new ActivityData();

    [HideInInspector] public Transform parentAfterDrag;

    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private RectTransform rootCanvasRect;
    private DropSlot currentSlot;
    private PlannerBookUI planner;
    private bool isClone;
    private bool isDragging;

    public ActivityData Activity => activity;
    public DropSlot CurrentSlot => currentSlot;
    public bool IsDeckTemplate => isDeckTemplate;
    public bool IsDragging => isDragging;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        planner = GetComponentInParent<PlannerBookUI>();

        if (titleText == null) titleText = FindTextByName("Title");
        if (subText == null) subText = FindTextByName("Sub");
        if (timeText == null) timeText = FindTextByName("Time");
        if (cardBackground == null) cardBackground = GetComponent<Image>();
        if (accentBar == null) accentBar = FindImageByName("Accent");

        // Pastikan background kartu selalu aktif raycast target agar bisa di-drag dan diklik
        if (cardBackground != null)
        {
            cardBackground.raycastTarget = true;
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(DeleteCard);
        }

        RefreshVisual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Tangkap pointer down agar Unity EventSystem menjamin event drag dan click berjalan stabil
    }

    private TMP_Text FindTextByName(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private Image FindImageByName(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    /// <summary>
    /// Mengisi data aktivitas ke kartu ini.
    /// </summary>
    public void SetData(ActivityData data)
    {
        if (data == null) return;
        activity = data.Clone();
        RefreshVisual();
    }

    /// <summary>
    /// Menandai kartu sebagai template deck atau kartu jadwal.
    /// </summary>
    public void SetDeckTemplate(bool value)
    {
        isDeckTemplate = value;
        ShowDeleteButton(!value);
    }

    public void BindSlot(DropSlot slot)
    {
        currentSlot = slot;
    }

    /// <summary>
    /// Dipanggil DropSlot setelah kartu masuk ke slot: tampilkan hari + jam di kartu.
    /// </summary>
    public void SetScheduledVisual(int dayIndex, string slotTime)
    {
        if (activity != null)
        {
            activity.timeSlot = slotTime;
        }

        if (timeText != null)
        {
            string day = (dayIndex >= 0 && dayIndex < GameManager.DayShortNames.Length)
                ? GameManager.DayShortNames[dayIndex]
                : "-";
            timeText.text = $"{day} {slotTime}";
            timeText.color = new Color(0.451f, 0.416f, 0.337f, 1f);
        }

        ShowDeleteButton(true);
        RefreshVisual();
    }

    public void RefreshCardVisual()
    {
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (activity == null) return;

        if (titleText != null) titleText.text = activity.activityName;
        if (subText != null) subText.text = activity.subtext;

        if (timeText != null && currentSlot == null)
        {
            timeText.text = activity.category;
            timeText.color = new Color(0.545f, 0.510f, 0.431f, 1f);
        }

        if (cardBackground != null && activity.cardColor != Color.clear)
        {
            cardBackground.color = activity.cardColor;
        }

        if (accentBar != null)
        {
            accentBar.color = Darken(activity.cardColor, 0.35f);
        }
    }

    private static Color Darken(Color color, float amount)
    {
        return new Color(
            Mathf.Lerp(color.r, 0f, amount),
            Mathf.Lerp(color.g, 0f, amount),
            Mathf.Lerp(color.b, 0f, amount),
            Mathf.Max(0.55f, color.a));
    }

    private void ShowDeleteButton(bool visible)
    {
        if (deleteButton != null && !isDragging)
        {
            deleteButton.gameObject.SetActive(visible && !isDeckTemplate);
        }
        else if (deleteButton != null)
        {
            deleteButton.gameObject.SetActive(false);
        }
    }

    #region Drag & Drop

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Jika game sudah berjalan di CityOverWorld, jadwal tidak boleh diubah.
        if (GameManager.Instance != null && GameManager.Instance.IsGameStarted)
        {
            eventData.pointerDrag = null;
            return;
        }

        EnsureRootCanvas();
        if (rootCanvas == null) return;

        if (isDeckTemplate)
        {
            // Instansiasi tetap di parent asal (deck) agar PlannerBookUI bisa ditemukan,
            // lalu BeginDrag memindahkannya ke root canvas.
            GameObject cloneObj = Instantiate(gameObject, transform.parent, false);
            DraggableCard cloneCard = cloneObj.GetComponent<DraggableCard>();
            if (cloneCard == null)
            {
                Destroy(cloneObj);
                return;
            }

            Vector2 cardSize = rectTransform != null ? rectTransform.rect.size : new Vector2(240f, 78f);

            cloneCard.isDeckTemplate = false;
            cloneCard.isClone = true;
            cloneCard.currentSlot = null;
            cloneCard.SetData(activity);
            cloneCard.ShowDeleteButton(false);

            RectTransform cloneRect = cloneObj.GetComponent<RectTransform>();
            cloneRect.anchorMin = new Vector2(0.5f, 0.5f);
            cloneRect.anchorMax = new Vector2(0.5f, 0.5f);
            cloneRect.pivot = new Vector2(0.5f, 0.5f);
            cloneRect.sizeDelta = cardSize;
            cloneObj.transform.SetAsLastSibling();

            // Alihkan proses drag ke kartu salinan.
            eventData.pointerDrag = cloneObj;
            cloneCard.BeginDrag(eventData);
            return;
        }

        currentSlot = GetComponentInParent<DropSlot>();

        // Kartu 6 jam tidak bisa dipindah (agar slot cadangannya tidak bentrok).
        if (currentSlot != null && activity != null && activity.slotsCount > 1)
        {
            eventData.pointerDrag = null;
            if (planner != null) planner.ShowToast("This is a 6h card. Remove it to move it.");
            return;
        }

        parentAfterDrag = null;
        BeginDrag(eventData);
    }

    /// <summary>
    /// Memulai fase drag: angkat kartu ke atas canvas agar tidak terpotong panel.
    /// </summary>
    public void BeginDrag(PointerEventData eventData)
    {
        EnsureRootCanvas();
        if (rootCanvas == null) return;

        isDragging = true;
        parentAfterDrag = null;

        // Simpan ukuran kartu sebelum dilepas dari slot, lalu ubah anchor-nya ke
        // mode bebas (0.5, 0.5) supaya ukuran tidak melar mengikuti canvas.
        Vector2 cardSize = rectTransform != null ? rectTransform.rect.size : new Vector2(240f, 82f);

        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();

        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = cardSize;
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.96f;
        }

        ShowDeleteButton(false);
        FollowPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        FollowPointer(eventData);
    }

    private void FollowPointer(PointerEventData eventData)
    {
        if (rootCanvasRect != null)
        {
            Camera cam = (rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                ? null
                : (eventData.pressEventCamera ?? (rootCanvas != null ? rootCanvas.worldCamera : null));

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    rootCanvasRect, eventData.position, cam, out Vector3 worldPoint))
            {
                transform.position = worldPoint;
            }
            else
            {
                transform.position = eventData.position;
            }
        }
        else
        {
            transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        DropSlot target = parentAfterDrag != null ? parentAfterDrag.GetComponent<DropSlot>() : null;
        parentAfterDrag = null;

        if (target != null)
        {
            if (target.CurrentCard == this)
            {
                SnapToSlot();
                ShowDeleteButton(true);
                return;
            }

            if (target.IsReserved)
            {
                Reject();
                return;
            }

            if (!target.IsOccupied)
            {
                MoveTo(target);
                return;
            }

            if (currentSlot != null)
            {
                SwapWith(target);
                return;
            }
        }

        Reject();
    }

    private void MoveTo(DropSlot target)
    {
        DropSlot from = currentSlot;

        if (from != null)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RemovePlan(from.dayIndex, from.timeSlot);
            }
            from.Release();
        }

        target.Place(this);
        isClone = false;
        SnapToSlot();
        ShowDeleteButton(true);
        NotifyPlanner();
    }

    private void SwapWith(DropSlot target)
    {
        DropSlot from = currentSlot;
        DraggableCard other = target != null ? target.CurrentCard : null;

        if (from == null || other == null)
        {
            Reject();
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemovePlan(from.dayIndex, from.timeSlot);
            GameManager.Instance.RemovePlan(target.dayIndex, target.timeSlot);
        }

        from.Release();
        target.Release();

        from.Place(other);
        target.Place(this);

        SnapToSlot();
        ShowDeleteButton(true);
        NotifyPlanner();
    }

    /// <summary>
    /// Kembalikan kartu ke tempat semula, atau hapus jika ini salinan dari deck.
    /// </summary>
    private void Reject()
    {
        if (isClone)
        {
            if (planner != null) planner.ShowToast("Drop the card on a free slot.");
            Destroy(gameObject);
            return;
        }

        if (currentSlot != null)
        {
            transform.SetParent(currentSlot.transform, false);
            SnapToSlot();
            ShowDeleteButton(true);
            if (planner != null) planner.ShowToast("That slot is already taken. Drop it on a free slot.");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    /// <summary>
    /// Menyesuaikan ukuran dan posisi kartu agar pas di dalam kotak slot.
    /// </summary>
    public void SnapToSlot()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) return;

        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(3f, 3f);
        rectTransform.offsetMax = new Vector2(-3f, -3f);
        rectTransform.localScale = Vector3.one;
        rectTransform.localPosition = Vector3.zero;
        rectTransform.localRotation = Quaternion.identity;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging || planner == null) return;

        if (isDeckTemplate)
        {
            planner.OnDeckCardClicked(this);
        }
        else if (currentSlot != null)
        {
            planner.OnScheduledCardClicked(this);
        }
    }

    /// <summary>
    /// Menghapus kartu ini dari jadwal.
    /// </summary>
    public void DeleteCard()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameStarted) return;

        DropSlot slot = currentSlot;
        currentSlot = null;

        if (slot != null)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RemovePlan(slot.dayIndex, slot.timeSlot);
            }
            slot.Release();
        }

        NotifyPlanner();
        Destroy(gameObject);
    }

    private void NotifyPlanner()
    {
        if (planner != null)
        {
            planner.RefreshAll();
        }
        else if (GameManager.Instance != null)
        {
            // PlanChanged di GameManager sudah dipanggil oleh SavePlan / RemovePlan.
        }
    }

    private void EnsureRootCanvas()
    {
        if (rootCanvas != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        rootCanvas = canvas.rootCanvas;
        rootCanvasRect = rootCanvas.GetComponent<RectTransform>();
    }
}
