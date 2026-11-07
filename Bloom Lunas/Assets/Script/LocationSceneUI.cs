using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// HUD sederhana untuk scene lokasi: status, daftar bahan, pesan,
/// tombol kembali ke kota, dan panel informasi.
/// </summary>
public class LocationSceneUI : MonoBehaviour
{
    private static readonly string[] MaterialOrder =
    {
        "Turmeric", "Ginger", "Kencur", "Tamarind", "Palm sugar",
        "Rice", "Coconut oil", "Beeswax", "Lemongrass"
    };

    private static readonly Color Paper = new Color(0.988f, 0.976f, 0.941f, 1f);
    private static readonly Color Ink = new Color(0.22f, 0.19f, 0.14f, 1f);
    private static readonly Color Muted = new Color(0.45f, 0.42f, 0.35f, 1f);
    private static readonly Color Gold = new Color(0.91f, 0.72f, 0.35f, 1f);

    private LocationSceneManager manager;
    private TMP_Text titleText;
    private TMP_Text timeText;
    private TMP_Text goldText;
    private TMP_Text productText;
    private TMP_Text hintText;
    private TMP_Text messageText;
    private GameObject messageBox;
    private Transform inventoryRows;
    private GameObject infoRoot;
    private TMP_Text infoTitle;
    private TMP_Text infoBody;

    private float messageUntil;

    private void Awake()
    {
        manager = GetComponent<LocationSceneManager>();
        if (manager == null) manager = FindFirstObjectByType<LocationSceneManager>();
        BuildUI();
        RefreshStatus();
    }

    private void Update()
    {
        if (messageBox != null && messageBox.activeSelf && Time.unscaledTime > messageUntil)
        {
            messageBox.SetActive(false);
        }
    }

    public void SetMode(string title, string hint)
    {
        if (titleText != null) titleText.text = title;
        if (hintText != null) hintText.text = hint;
    }

    public void RefreshStatus()
    {
        GameManager gm = GameManager.Instance;

        if (gm == null)
        {
            if (timeText != null) timeText.text = "No game data";
            return;
        }

        if (timeText != null)
        {
            timeText.text = $"{GameManager.DayShortNames[gm.CurrentDay]}  -  {GameManager.TimeSlots[gm.CitySlotIndex]}  -  Week {gm.CurrentWeek}";
        }
        if (goldText != null) goldText.text = $"Gold {gm.Gold:N0} G";

        if (productText != null)
        {
            int total = 0;
            foreach (string recipe in GameManager.CraftRecipes.Keys) total += gm.GetProduct(recipe);
            productText.text = $"Products ready to sell: {total}";
        }

        RefreshInventory();
    }

    public void RefreshInventory()
    {
        if (inventoryRows == null) return;
        ClearChildren(inventoryRows);

        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            AddText(inventoryRows, "No game data", 12f, Muted, FontStyles.Normal);
            return;
        }

        foreach (string material in MaterialOrder)
        {
            AddText(inventoryRows, $"{material}  x{gm.GetMaterial(material)}", 13f, Ink, FontStyles.Normal);
        }
    }

    public void ShowMessage(string text)
    {
        if (messageBox == null || messageText == null) return;
        messageText.text = text;
        messageBox.SetActive(true);
        messageBox.transform.SetAsLastSibling();
        messageUntil = Time.unscaledTime + 4.5f;
    }

    public void OpenInfo(string title, string body)
    {
        if (infoRoot == null) return;
        if (infoTitle != null) infoTitle.text = title;
        if (infoBody != null) infoBody.text = body;
        infoRoot.SetActive(true);
        infoRoot.transform.SetAsLastSibling();
    }

    public void CloseInfo()
    {
        if (infoRoot != null) infoRoot.SetActive(false);
    }

    #region UI Builder

    private void BuildUI()
    {
        EnsureEventSystem();

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("LocationCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        canvas.sortingOrder = 100;

        GameObject status = MakePanel(canvas.transform, "StatusPanel", new Vector2(470f, 190f), Paper);
        RectTransform statusRt = status.GetComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(0f, 1f);
        statusRt.anchorMax = new Vector2(0f, 1f);
        statusRt.pivot = new Vector2(0f, 1f);
        statusRt.anchoredPosition = new Vector2(26f, -26f);

        VerticalLayoutGroup statusLayout = status.AddComponent<VerticalLayoutGroup>();
        statusLayout.padding = new RectOffset(22, 22, 16, 16);
        statusLayout.spacing = 4f;
        statusLayout.childControlWidth = true;
        statusLayout.childControlHeight = true;

        titleText = AddText(status.transform, "LOCATION", 28f, Ink, FontStyles.Bold);
        timeText = AddText(status.transform, "MON - 09:00", 14f, Muted, FontStyles.Normal);
        goldText = AddText(status.transform, "Gold 240 G", 15f, Ink, FontStyles.Normal);
        productText = AddText(status.transform, "Products ready to sell: 0", 13f, Muted, FontStyles.Normal);

        BuildInventoryPanel(canvas.transform);
        BuildHintBar(canvas.transform);
        BuildMessageBox(canvas.transform);
        BuildBackButton(canvas.transform);
        BuildInfoPanel(canvas.transform);
    }

    private void BuildInventoryPanel(Transform parent)
    {
        GameObject panel = MakePanel(parent, "InventoryPanel", new Vector2(310f, 360f), Paper);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-26f, -26f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 14, 14);
        layout.spacing = 3f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        AddText(panel.transform, "MATERIALS", 15f, Ink, FontStyles.Bold);

        GameObject rows = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        rows.transform.SetParent(panel.transform, false);
        rows.GetComponent<LayoutElement>().preferredHeight = 280f;
        VerticalLayoutGroup rowsLayout = rows.GetComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 1f;
        rowsLayout.childControlWidth = true;
        rowsLayout.childControlHeight = true;
        inventoryRows = rows.transform;
    }

    private void BuildHintBar(Transform parent)
    {
        GameObject hint = MakePanel(parent, "HintBar", new Vector2(900f, 42f), new Color(0.16f, 0.14f, 0.11f, 0.82f));
        RectTransform rt = hint.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(26f, 26f);

        hintText = AddText(hint.transform, "Click an object to interact.", 13.5f,
            new Color(0.97f, 0.94f, 0.87f, 1f), FontStyles.Normal);
        Stretch(hintText.rectTransform, 18f, 8f);
    }

    private void BuildMessageBox(Transform parent)
    {
        messageBox = MakePanel(parent, "Message", new Vector2(820f, 52f), Paper);
        RectTransform rt = messageBox.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 82f);

        messageText = AddText(messageBox.transform, "", 14f, Ink, FontStyles.Bold);
        Stretch(messageText.rectTransform, 18f, 8f);
        messageBox.SetActive(false);
    }

    private void BuildBackButton(Transform parent)
    {
        Button back = MakeButton(parent, "Back to city", 180f, 44f, Gold, () =>
        {
            if (manager == null) manager = FindFirstObjectByType<LocationSceneManager>();
            if (manager != null) manager.BackToCity();
        });

        RectTransform rt = back.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-26f, 26f);
    }

    private void BuildInfoPanel(Transform parent)
    {
        infoRoot = new GameObject("InfoOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        infoRoot.transform.SetParent(parent, false);
        RectTransform rootRt = infoRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;
        infoRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

        Button dismiss = infoRoot.AddComponent<Button>();
        dismiss.onClick.AddListener(CloseInfo);

        GameObject panel = MakePanel(infoRoot.transform, "InfoPanel", new Vector2(640f, 460f), Paper);
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 24, 24);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        infoTitle = AddText(panel.transform, "INFO", 26f, Ink, FontStyles.Bold);
        infoBody = AddText(panel.transform, "", 14f, Muted, FontStyles.Normal);
        infoBody.textWrappingMode = TextWrappingModes.Normal;

        LayoutElement bodyLayout = infoBody.GetComponent<LayoutElement>();
        bodyLayout.preferredHeight = 260f;
        bodyLayout.flexibleHeight = 1f;

        MakeButton(panel.transform, "Close", 140f, 42f, new Color(0.9f, 0.88f, 0.82f, 1f), CloseInfo, Ink);

        infoRoot.SetActive(false);
    }

    private GameObject MakePanel(Transform parent, string name, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private Button MakeButton(Transform parent, string label, float width, float height, Color color,
        UnityEngine.Events.UnityAction onClick, Color? textColor = null)
    {
        GameObject obj = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);

        LayoutElement layout = obj.GetComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
        layout.minWidth = width;
        layout.minHeight = height;

        obj.GetComponent<Image>().color = color;
        Button button = obj.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(obj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 14f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = textColor ?? new Color(0.2f, 0.15f, 0.05f, 1f);
        return button;
    }

    private TMP_Text AddText(Transform parent, string text, float size, Color color, FontStyles style)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    private static void Stretch(RectTransform rt, float horizontal, float vertical)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(horizontal, vertical);
        rt.offsetMax = new Vector2(-horizontal, -vertical);
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        GameObject obj = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        obj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        obj.AddComponent<StandaloneInputModule>();
#endif
    }

    #endregion
}
