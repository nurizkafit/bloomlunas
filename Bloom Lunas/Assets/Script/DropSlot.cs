using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Satu kotak 3 jam di dalam buku planner (contoh: Senin 06:00).
/// Menerima kartu yang di-drag dan menyimpannya ke GameManager.
/// </summary>
public class DropSlot : MonoBehaviour, IPointerDownHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Identitas Slot")]
    [Tooltip("Index Hari: 0 = Mon, 1 = Tue, 2 = Wed, 3 = Thu, 4 = Fri, 5 = Sat, 6 = Sun")]
    public int dayIndex = 0;

    [Tooltip("Jam slot (contoh: 06:00, 09:00, 12:00, 15:00)")]
    public string timeSlot = "06:00";

    [Header("Visual")]
    [SerializeField] private Image slotImage;
    [SerializeField] private TMP_Text emptyLabel;
    [SerializeField] private Outline selectionOutline;

    [SerializeField] private Color normalColor = new Color(0.988f, 0.965f, 0.898f, 1f);
    [SerializeField] private Color highlightColor = new Color(0.898f, 0.937f, 0.855f, 1f);
    [SerializeField] private Color swapColor = new Color(0.976f, 0.925f, 0.812f, 1f);
    [SerializeField] private Color blockedColor = new Color(0.945f, 0.851f, 0.831f, 1f);

    public DraggableCard CurrentCard { get; private set; }
    public bool IsOccupied => CurrentCard != null;
    public bool IsSelected { get; private set; }
    public bool IsReserved => GameManager.Instance != null && GameManager.Instance.IsSlotReserved(dayIndex, timeSlot);
    public bool IsBlockedSlot => IsOccupied || IsReserved;
    public string TimeLabel => timeSlot;
    public PlannerBookUI Planner => planner;

    private PlannerBookUI planner;
    private string defaultEmptyLabel = "Drop here";

    private void Awake()
    {
        if (slotImage == null) slotImage = GetComponent<Image>();
        if (selectionOutline == null) selectionOutline = GetComponent<Outline>();

        if (slotImage != null)
        {
            normalColor = slotImage.color;
        }

        if (selectionOutline != null)
        {
            selectionOutline.enabled = false;
        }

        AutoDetectSlotInfo();

        if (emptyLabel != null) defaultEmptyLabel = emptyLabel.text;
    }

    /// <summary>
    /// Dipanggil oleh PlannerBookUI saat scene dimuat.
    /// </summary>
    public void Setup(PlannerBookUI owner)
    {
        planner = owner;
    }

    /// <summary>
    /// Dipakai builder / Inspector untuk menentukan warna dasar slot (misal tint malam).
    /// </summary>
    public void SetBaseColor(Color color)
    {
        normalColor = color;
        if (!IsOccupied && !IsReserved && slotImage != null)
        {
            slotImage.color = color;
        }
    }

    /// <summary>
    /// Deteksi otomatis hari dan jam dari hierarki jika belum diisi manual.
    /// </summary>
    private void AutoDetectSlotInfo()
    {
        if (transform.parent != null && transform.parent.parent != null)
        {
            Transform column = transform.parent.parent;
            if (column.name.Contains("(1)")) { /* nothing, day index is set by builder */ }
        }

        if (string.IsNullOrEmpty(timeSlot))
        {
            int siblingIdx = transform.GetSiblingIndex();
            if (siblingIdx >= 0 && siblingIdx < GameManager.TimeSlots.Length)
            {
                timeSlot = GameManager.TimeSlots[siblingIdx];
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotImage == null) return;

        DraggableCard dragging = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<DraggableCard>()
            : null;

        if (dragging == null && CurrentCard != null) dragging = CurrentCard;

        if (dragging != null && dragging.IsDragging)
        {
            // Validasi apakah slot ini diizinkan untuk aktivitas yang sedang di-drag
            bool isAllowed = GameManager.ValidateActivitySlot(dragging.Activity, dayIndex, timeSlot, out _);

            bool canUse = isAllowed && (!IsReserved || (dragging.CurrentSlot != null && dragging.CurrentSlot.timeSlot == GetReservationSourceSlot()));
            if (!canUse)
            {
                slotImage.color = blockedColor;
            }
            else if (!IsOccupied)
            {
                slotImage.color = highlightColor;
            }
            else if (dragging.CurrentSlot != null)
            {
                slotImage.color = swapColor;
            }
            else
            {
                slotImage.color = blockedColor;
            }
        }
        else if (!IsOccupied && !IsReserved)
        {
            slotImage.color = highlightColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RefreshColor();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Tangkap pointer down agar event click dan drop ditangkap stabil oleh EventSystem
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging) return;
        if (planner != null) planner.SelectSlot(this);
    }

    private string GetReservationSourceSlot()
    {
        return GameManager.Instance != null ? GameManager.Instance.GetReservationSourceSlot(dayIndex, timeSlot) : null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null) return;

        DraggableCard card = droppedObj.GetComponent<DraggableCard>();
        if (card != null)
        {
            if (IsReserved)
            {
                planner?.ShowToast("This slot is the second half of a 6h card.");
                RefreshColor();
                return;
            }

            if (IsOccupied && card.CurrentSlot == null)
            {
                planner?.ShowToast("That slot is taken. Pick another one.");
                RefreshColor();
                return;
            }

            if (!GameManager.ValidateActivitySlot(card.Activity, dayIndex, timeSlot, out string rejectReason))
            {
                planner?.ShowToast(rejectReason);
                RefreshColor();
                return;
            }

            if (!IsOccupied && GameManager.Instance != null &&
                !GameManager.Instance.CanPlaceAt(dayIndex, timeSlot, card.Activity, out rejectReason))
            {
                planner?.ShowToast(rejectReason);
                RefreshColor();
                return;
            }

            card.parentAfterDrag = transform;
        }

        RefreshColor();
    }

    /// <summary>
    /// Menempatkan kartu ke dalam slot ini, memindahkan RectTransform-nya, lalu menyimpan rencana.
    /// </summary>
    public void Place(DraggableCard card)
    {
        if (card == null) return;
        if (IsReserved && CurrentCard != card) return;

        CurrentCard = card;
        card.BindSlot(this);

        RectTransform cardRect = card.transform as RectTransform;
        if (cardRect != null)
        {
            cardRect.SetParent(transform, false);
        }

        card.SnapToSlot();
        card.SetScheduledVisual(dayIndex, timeSlot);

        if (GameManager.Instance != null)
        {
            card.Activity.timeSlot = timeSlot;
            GameManager.Instance.SavePlan(dayIndex, timeSlot, card.Activity);
        }

        RefreshVisual();
    }

    /// <summary>
    /// Mengosongkan slot (tanpa menghapus data rencana kecuali diminta).
    /// </summary>
    public void Release(bool removePlan = false)
    {
        if (removePlan && GameManager.Instance != null)
        {
            GameManager.Instance.RemovePlan(dayIndex, timeSlot);
        }

        CurrentCard = null;
        RefreshVisual();
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;

        if (selectionOutline != null)
        {
            selectionOutline.enabled = selected;
        }

        RefreshColor();
    }

    public void RefreshVisual()
    {
        if (emptyLabel != null)
        {
            emptyLabel.gameObject.SetActive(!IsOccupied);
            if (!IsOccupied)
            {
                emptyLabel.text = IsReserved ? "6h session" : defaultEmptyLabel;
            }
        }

        RefreshColor();
    }

    private void RefreshColor()
    {
        if (slotImage == null) return;

        if (IsSelected && !IsOccupied)
        {
            slotImage.color = new Color(0.976f, 0.949f, 0.851f, 1f);
        }
        else if (IsReserved)
        {
            slotImage.color = new Color(0.929f, 0.914f, 0.855f, 1f);
        }
        else
        {
            slotImage.color = normalColor;
        }
    }
}
