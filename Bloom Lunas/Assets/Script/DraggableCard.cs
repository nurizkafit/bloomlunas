using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Mode Kartu")]
    [Tooltip("Jika dicentang, kartu ini bertindak sebagai template di Card Deck (saat di-drag akan membuat kartu baru ke kalender)")]
    public bool isDeckTemplate = false;

    [Header("Visual Tampilan Kartu")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private Image cardBackground;
    [SerializeField] private Button deleteButton;

    [Header("Data Aktivitas")]
    [SerializeField] private ActivityData activity = new ActivityData();

    [HideInInspector] public Transform parentAfterDrag;

    private Transform originalParent;
    private DropSlot originalSlot;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private RectTransform rootCanvasRect;
    private bool isClone = false;

    public ActivityData Activity => activity;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            rootCanvas = rootCanvas.rootCanvas;
            rootCanvasRect = rootCanvas.GetComponent<RectTransform>();
        }

        // Auto-find komponen teks jika belum di-assign di Inspector
        if (titleText == null)
        {
            titleText = GetComponentInChildren<TMP_Text>();
        }

        if (cardBackground == null)
        {
            cardBackground = GetComponent<Image>();
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(DeleteCard);
            // Sembunyikan tombol delete jika masih berada di deck template
            deleteButton.gameObject.SetActive(!isDeckTemplate);
        }

        RefreshVisual();
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

    private void RefreshVisual()
    {
        if (activity == null) return;

        if (titleText != null)
        {
            titleText.text = activity.activityName;
        }

        if (subText != null)
        {
            subText.text = activity.subtext;
        }

        if (timeText != null)
        {
            timeText.text = activity.timeSlot;
        }

        if (cardBackground != null && activity.cardColor != Color.clear)
        {
            cardBackground.color = activity.cardColor;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Jika game sudah berjalan / di-pause di CityOverWorld, kartu tidak boleh dipindah
        if (GameManager.Instance != null && GameManager.Instance.IsGameStarted)
        {
            eventData.pointerDrag = null;
            return;
        }

        EnsureRootCanvas();

        if (isDeckTemplate)
        {
            // Buat duplikat dari kartu ini untuk ditarik ke dalam kalender
            GameObject cloneObj = Instantiate(gameObject, rootCanvas.transform);
            DraggableCard cloneCard = cloneObj.GetComponent<DraggableCard>();
            cloneCard.isDeckTemplate = false;
            cloneCard.isClone = true;
            cloneCard.SetData(this.activity);

            // Alihkan proses drag ke kartu clone
            eventData.pointerDrag = cloneObj;
            cloneCard.StartDraggingFromClone(eventData);
            return;
        }

        StartDraggingInternal();
    }

    public void StartDraggingFromClone(PointerEventData eventData)
    {
        EnsureRootCanvas();
        originalParent = null;
        originalSlot = null;
        parentAfterDrag = null;

        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;

        if (deleteButton != null)
        {
            deleteButton.gameObject.SetActive(false);
        }

        FollowPointer(eventData);
    }

    private void StartDraggingInternal()
    {
        originalParent = transform.parent;
        originalSlot = GetComponentInParent<DropSlot>();
        parentAfterDrag = originalParent;

        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        FollowPointer(eventData);
    }

    private void FollowPointer(PointerEventData eventData)
    {
        if (rootCanvasRect != null)
        {
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rootCanvasRect, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
            {
                transform.position = worldPoint;
            }
        }
        else
        {
            transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // Cek apakah kartu dijatuhkan ke slot kalender yang valid
        if (parentAfterDrag != null && parentAfterDrag != originalParent && parentAfterDrag.GetComponent<DropSlot>() != null)
        {
            // Berhasil ditempatkan di slot baru
            if (originalSlot != null)
            {
                originalSlot.ClearCard();
            }

            DropSlot newSlot = parentAfterDrag.GetComponent<DropSlot>();
            transform.SetParent(newSlot.transform, false);
            SnapToSlot();

            newSlot.AssignCard(this);
            originalSlot = newSlot;
            originalParent = newSlot.transform;

            if (deleteButton != null)
            {
                deleteButton.gameObject.SetActive(true);
            }
        }
        else
        {
            // Gagal ditempatkan di slot
            if (isClone)
            {
                // Jika ini adalah kartu yang baru di-drag dari deck tapi gagal masuk ke slot, hapus clone
                Destroy(gameObject);
            }
            else if (originalParent != null)
            {
                // Jika kartu yang sudah ada dipindahkan tapi gagal, kembalikan ke slot asalnya
                transform.SetParent(originalParent, false);
                SnapToSlot();
            }
        }
    }

    /// <summary>
    /// Menyesuaikan ukuran dan posisi kartu agar pas di dalam kotak slot.
    /// </summary>
    public void SnapToSlot()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(3f, 3f);
        rectTransform.offsetMax = new Vector2(-3f, -3f);
        rectTransform.localScale = Vector3.one;
        rectTransform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// Menghapus kartu ini dari jadwal.
    /// </summary>
    public void DeleteCard()
    {
        if (originalSlot != null)
        {
            originalSlot.ClearCard();
        }
        Destroy(gameObject);
    }

    private void EnsureRootCanvas()
    {
        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null)
            {
                rootCanvas = rootCanvas.rootCanvas;
                rootCanvasRect = rootCanvas.GetComponent<RectTransform>();
            }
        }
    }
}
