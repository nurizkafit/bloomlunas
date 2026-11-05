using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Identitas Slot")]
    [Tooltip("Index Hari: 0 = Mon, 1 = Tue, 2 = Wed, 3 = Thu, 4 = Fri, 5 = Sat, 6 = Sun")]
    public int dayIndex = 0;

    [Tooltip("Jam slot (contoh: 06:00, 09:00, 12:00, 15:00)")]
    public string timeSlot = "06:00";

    [Header("Visual Feedback (Opsional)")]
    [SerializeField] private Image slotImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = new Color(0.85f, 0.95f, 0.85f, 1f);

    public DraggableCard CurrentCard { get; private set; }
    public bool IsOccupied => CurrentCard != null;

    private void Awake()
    {
        if (slotImage == null)
        {
            slotImage = GetComponent<Image>();
        }

        if (slotImage != null)
        {
            normalColor = slotImage.color;
        }

        // Otomatis deteksi hari dan jam dari hierarki jika belum disetel manual
        AutoDetectSlotInfo();
    }

    private void AutoDetectSlotInfo()
    {
        // Deteksi hari dari nama parent DayColumn
        Transform parentCol = transform.parent;
        if (parentCol != null)
        {
            string colName = parentCol.name;
            if (colName.Contains("(1)")) dayIndex = 1;      // Tue
            else if (colName.Contains("(2)")) dayIndex = 2; // Wed
            else if (colName.Contains("(3)")) dayIndex = 3; // Thu
            else if (colName.Contains("(4)")) dayIndex = 4; // Fri
            else if (colName.Contains("(5)")) dayIndex = 5; // Sat
            else if (colName.Contains("(6)")) dayIndex = 6; // Sun
            else if (colName.Contains("Mon")) dayIndex = 0; // Mon
        }

        // Deteksi jam dari urutan anak di dalam kolom (3 jam per slot)
        int siblingIdx = transform.GetSiblingIndex();
        string[] standardTimes = { "06:00", "09:00", "12:00", "15:00", "18:00", "21:00", "00:00", "03:00" };
        if (siblingIdx >= 0 && siblingIdx < standardTimes.Length)
        {
            timeSlot = standardTimes[siblingIdx];
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Beri warna sorotan jika ada kartu yang sedang di-drag melintas di atas slot ini
        if (eventData.pointerDrag != null && !IsOccupied && slotImage != null)
        {
            slotImage.color = highlightColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetSlotVisual();
    }

    public void OnDrop(PointerEventData eventData)
    {
        ResetSlotVisual();

        // Jika slot sudah terisi kartu, tolak kartu baru
        if (IsOccupied) return;

        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null) return;

        DraggableCard card = droppedObj.GetComponent<DraggableCard>();
        if (card != null)
        {
            // Beritahu kartu bahwa slot ini adalah target barunya
            card.parentAfterDrag = transform;
            AssignCard(card);
        }
    }

    /// <summary>
    /// Menempatkan kartu ke dalam slot ini dan mendaftarkannya ke GameManager.
    /// </summary>
    public void AssignCard(DraggableCard card)
    {
        CurrentCard = card;

        if (GameManager.Instance != null && card != null && card.Activity != null)
        {
            card.Activity.timeSlot = timeSlot;
            GameManager.Instance.SavePlan(dayIndex, timeSlot, card.Activity);
        }
    }

    /// <summary>
    /// Mengosongkan slot ini.
    /// </summary>
    public void ClearCard()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemovePlan(dayIndex, timeSlot);
        }
        CurrentCard = null;
    }

    private void ResetSlotVisual()
    {
        if (slotImage != null)
        {
            slotImage.color = normalColor;
        }
    }
}
