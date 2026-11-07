using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Panel Market sederhana: beli bahan untuk craft dengan gold.
/// UI dibangun saat runtime agar tidak perlu mengubah scene secara manual.
/// </summary>
public class CityMarketUI : MonoBehaviour
{
    private static readonly string[] Materials =
    {
        "Turmeric", "Ginger", "Kencur", "Tamarind", "Palm sugar",
        "Rice", "Coconut oil", "Beeswax", "Lemongrass"
    };

    private GameObject root;
    private TMP_Text goldText;
    private TMP_Text messageText;
    private Transform rowsRoot;
    private bool built;

    public void Open()
    {
        EnsureUI();
        if (root == null) return;

        root.SetActive(true);
        root.transform.SetAsLastSibling();

        CharacterMovement player = FindFirstObjectByType<CharacterMovement>();
        if (player != null) player.StopMoving();

        LocationPopupUI popup = FindFirstObjectByType<LocationPopupUI>();
        if (popup != null) popup.ClosePopup();

        Refresh();
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
    }

    private void Update()
    {
        if (root == null || !root.activeSelf) return;

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
#else
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
#endif
    }

    private void Refresh()
    {
        GameManager gm = GameManager.Instance;

        if (goldText != null)
        {
            goldText.text = gm != null ? $"Gold: {gm.Gold:N0} G" : "Gold: -";
        }

        if (rowsRoot == null) return;

        foreach (Transform child in rowsRoot) Destroy(child.gameObject);

        if (gm == null)
        {
            AddMessageRow("Start from the planner to use the market.");
            return;
        }

        AddSectionLabel("BUY INGREDIENTS");

        foreach (string material in Materials)
        {
            BuildRow(material, gm);
        }

        BuildSellSection(gm);
        if (messageText != null) messageText.text = "Buy ingredients, or sell your crafted jamu and salep.";
    }

    private void BuildSellSection(GameManager gm)
    {
        AddSectionLabel("SELL PRODUCTS");

        foreach (var pair in GameManager.CraftRecipes)
        {
            string product = pair.Key;
            int count = gm.GetProduct(product);
            int price = gm.GetProductPrice(product);

            GameObject row = new GameObject("SellRow_" + product, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(rowsRoot, false);

            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.spacing = 8f;

            row.GetComponent<LayoutElement>().preferredHeight = 36f;

            AddLabel(row, product, 14f, TextAlignmentOptions.MidlineLeft, 1f);
            AddLabel(row, $"x{count}", 13f, TextAlignmentOptions.Center, 0.5f);
            AddLabel(row, $"{price} G", 13f, TextAlignmentOptions.MidlineRight, 0.7f);

            GameObject sellObj = new GameObject("Sell", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            sellObj.transform.SetParent(row.transform, false);
            sellObj.GetComponent<LayoutElement>().preferredWidth = 62f;
            sellObj.GetComponent<Image>().color = count > 0
                ? new Color(0.38f, 0.58f, 0.36f, 1f)
                : new Color(0.82f, 0.80f, 0.74f, 1f);

            Button sell = sellObj.GetComponent<Button>();
            sell.interactable = count > 0;
            sell.onClick.AddListener(() =>
            {
                bool sold = gm.SellProduct(product);
                Refresh();
                if (messageText != null)
                {
                    messageText.text = sold ? $"Sold {product} for {price} G." : $"No {product} in stock.";
                }
            });

            GameObject sellLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            sellLabel.transform.SetParent(sellObj.transform, false);
            RectTransform sellRt = sellLabel.GetComponent<RectTransform>();
            sellRt.anchorMin = Vector2.zero;
            sellRt.anchorMax = Vector2.one;
            sellRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI sellText = sellLabel.GetComponent<TextMeshProUGUI>();
            sellText.text = "Sell";
            sellText.fontSize = 13f;
            sellText.fontStyle = FontStyles.Bold;
            sellText.alignment = TextAlignmentOptions.Center;
            sellText.color = count > 0 ? new Color(0.98f, 0.95f, 0.88f, 1f) : new Color(0.5f, 0.48f, 0.42f, 1f);
        }
    }

    private void AddSectionLabel(string text)
    {
        GameObject obj = new GameObject("Section", typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(rowsRoot, false);
        TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 12f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.55f, 0.47f, 0.33f, 1f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private void BuildRow(string material, GameManager gm)
    {
        GameObject row = new GameObject("Row_" + material, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(rowsRoot, false);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.spacing = 8f;

        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.preferredHeight = 36f;
        rowLayout.minHeight = 36f;

        AddLabel(row, material, 14f, TextAlignmentOptions.MidlineLeft, 1f);
        AddLabel(row, $"x{gm.GetMaterial(material)}", 13f, TextAlignmentOptions.Center, 0.5f);
        AddLabel(row, $"{gm.GetMaterialPrice(material)} G", 13f, TextAlignmentOptions.MidlineRight, 0.7f);

        GameObject buyObj = new GameObject("Buy", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buyObj.transform.SetParent(row.transform, false);
        buyObj.GetComponent<LayoutElement>().preferredWidth = 62f;
        buyObj.GetComponent<Image>().color = new Color(0.91f, 0.72f, 0.35f, 1f);

        Button buy = buyObj.GetComponent<Button>();
        buy.onClick.AddListener(() =>
        {
            bool bought = gm.BuyMaterial(material);
            string result = bought
                ? $"Bought 1 {material}."
                : (gm.Gold < gm.GetMaterialPrice(material) ? "Not enough gold." : "Cannot buy that.");

            Refresh();
            if (messageText != null) messageText.text = result;
        });

        GameObject buyLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        buyLabel.transform.SetParent(buyObj.transform, false);
        RectTransform buyRt = buyLabel.GetComponent<RectTransform>();
        buyRt.anchorMin = Vector2.zero;
        buyRt.anchorMax = Vector2.one;
        buyRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI buyText = buyLabel.GetComponent<TextMeshProUGUI>();
        buyText.text = "Buy";
        buyText.fontSize = 13f;
        buyText.fontStyle = FontStyles.Bold;
        buyText.alignment = TextAlignmentOptions.Center;
        buyText.color = new Color(0.2f, 0.15f, 0.05f, 1f);
    }

    private TMP_Text AddLabel(GameObject parent, string text, float size, TextAlignmentOptions align, float flexWidth)
    {
        GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        obj.transform.SetParent(parent.transform, false);

        LayoutElement layout = obj.GetComponent<LayoutElement>();
        layout.flexibleWidth = flexWidth;

        TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = align;
        label.color = new Color(0.25f, 0.22f, 0.18f, 1f);
        return label;
    }

    private void AddMessageRow(string text)
    {
        GameObject obj = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(rowsRoot, false);
        TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 14f;
        label.color = new Color(0.6f, 0.2f, 0.2f, 1f);
    }

    private void EnsureUI()
    {
        if (built) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(transform.root, false);
        }

        root = new GameObject("CityMarket_Root", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(canvas.transform, false);

        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;

        Image overlay = root.GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.45f);

        Button dismiss = root.AddComponent<Button>();
        dismiss.onClick.AddListener(Close);

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(root.transform, false);

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(620f, 860f);

        panel.GetComponent<Image>().color = new Color(0.988f, 0.976f, 0.941f, 1f);

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(26, 26, 20, 20);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        AddTitle(panel, "MARKET", 26f);
        AddTitle(panel, "Buy ingredients or sell your jamu and salep.", 12f);

        goldText = AddText(panel, "Gold: -", 15f, FontStyles.Bold);
        AddText(panel, "Prices are per ingredient. Exploration is still free.", 11f, FontStyles.Normal);

        GameObject rows = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        rows.transform.SetParent(panel.transform, false);
        rows.GetComponent<LayoutElement>().preferredHeight = 600f;
        VerticalLayoutGroup rowLayout = rows.GetComponent<VerticalLayoutGroup>();
        rowLayout.spacing = 4f;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowsRoot = rows.transform;

        messageText = AddText(panel, "", 12f, FontStyles.Normal);

        GameObject closeObj = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        closeObj.transform.SetParent(panel.transform, false);
        closeObj.GetComponent<LayoutElement>().preferredHeight = 40f;
        closeObj.GetComponent<Image>().color = new Color(0.91f, 0.72f, 0.35f, 1f);
        closeObj.GetComponent<Button>().onClick.AddListener(Close);

        GameObject closeLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeLabel.transform.SetParent(closeObj.transform, false);
        RectTransform closeRt = closeLabel.GetComponent<RectTransform>();
        closeRt.anchorMin = Vector2.zero;
        closeRt.anchorMax = Vector2.one;
        closeRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI closeText = closeLabel.GetComponent<TextMeshProUGUI>();
        closeText.text = "Close market";
        closeText.fontSize = 15f;
        closeText.fontStyle = FontStyles.Bold;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.color = new Color(0.2f, 0.15f, 0.05f, 1f);

        root.SetActive(false);
        built = true;
    }

    private void AddTitle(GameObject parent, string text, float size)
    {
        AddText(parent, text, size, FontStyles.Bold, new Color(0.2f, 0.17f, 0.12f, 1f));
    }

    private TMP_Text AddText(GameObject parent, string text, float size, FontStyles style,
        Color? color = null)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent.transform, false);

        TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color ?? new Color(0.42f, 0.39f, 0.32f, 1f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        return label;
    }
}
