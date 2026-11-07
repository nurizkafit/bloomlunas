using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI House yang dibuat saat runtime: status, tidur, craft, dan jual produk.
/// </summary>
public class HouseUI : MonoBehaviour
{
    private static readonly Color Paper = new Color(0.988f, 0.976f, 0.941f, 1f);
    private static readonly Color Ink = new Color(0.22f, 0.19f, 0.14f, 1f);
    private static readonly Color Muted = new Color(0.45f, 0.42f, 0.35f, 1f);
    private static readonly Color Gold = new Color(0.91f, 0.72f, 0.35f, 1f);
    private static readonly Color Green = new Color(0.32f, 0.52f, 0.30f, 1f);
    private static readonly Color Red = new Color(0.68f, 0.25f, 0.22f, 1f);

    private HouseManager manager;

    private TMP_Text timeText;
    private TMP_Text statsText;
    private TMP_Text energyText;
    private TMP_Text productText;
    private TMP_Text messageText;
    private GameObject messageBox;
    private float messageUntil;

    private GameObject modalRoot;
    private GameObject sleepPanel;
    private GameObject craftPanel;
    private Transform craftRows;
    private TMP_Text sleepEnergyText;

    private void Awake()
    {
        manager = GetComponent<HouseManager>();
        if (manager == null) manager = FindFirstObjectByType<HouseManager>();
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
        if (statsText != null)
        {
            statsText.text = $"Gold {gm.Gold:N0} G      Day {gm.CurrentDay + 1}";
        }
        if (energyText != null)
        {
            energyText.text = $"Energy {gm.Energy} / {gm.EnergyMax}";
        }
        if (productText != null)
        {
            int total = 0;
            foreach (string recipe in GameManager.CraftRecipes.Keys) total += gm.GetProduct(recipe);
            productText.text = $"Products ready to sell: {total}";
        }
        if (sleepEnergyText != null)
        {
            sleepEnergyText.text = $"Energy now: {gm.Energy} / {gm.EnergyMax}";
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

    public void ClosePanels()
    {
        if (sleepPanel != null) sleepPanel.SetActive(false);
        if (craftPanel != null) craftPanel.SetActive(false);
        if (modalRoot != null) modalRoot.SetActive(false);
    }

    public void OpenSleep()
    {
        ClosePanels();
        RefreshStatus();
        if (modalRoot != null) modalRoot.SetActive(true);
        if (sleepPanel != null) sleepPanel.SetActive(true);
    }

    public void OpenCraft()
    {
        ClosePanels();
        RefreshCraft();
        if (modalRoot != null) modalRoot.SetActive(true);
        if (craftPanel != null) craftPanel.SetActive(true);
    }

    public void RefreshCraft()
    {
        if (craftRows == null) return;
        ClearChildren(craftRows);

        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            AddTextRow(craftRows, "No game data.", 14f, Red);
            return;
        }

        foreach (var pair in GameManager.CraftRecipes)
        {
            string recipeName = pair.Key;
            CraftRecipe recipe = pair.Value;

            List<string> parts = new List<string>();
            foreach (var ingredient in recipe.ingredients)
            {
                parts.Add($"{ingredient.Value} {ingredient.Key} (have {gm.GetMaterial(ingredient.Key)})");
            }

            bool ready = gm.HasIngredients(recipeName, out List<string> missing);

            GameObject row = MakeRow(craftRows, 72f);
            GameObject textCol = new GameObject("TextCol", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            textCol.transform.SetParent(row.transform, false);
            textCol.GetComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup col = textCol.GetComponent<VerticalLayoutGroup>();
            col.spacing = 2f;
            col.childControlWidth = true;
            col.childControlHeight = true;

            AddText(textCol.transform, recipeName, 15f, Ink, FontStyles.Bold);
            AddText(textCol.transform, string.Join(", ", parts), 11.5f, Muted, FontStyles.Normal);
            AddText(textCol.transform, ready ? "Ready to craft" : "Missing: " + string.Join(", ", missing),
                11.5f, ready ? Green : Red, FontStyles.Normal);

            Button craft = MakeButton(row.transform, "Craft", 84f, 34f, Gold, () =>
            {
                if (manager != null) manager.Craft(recipeName);
            });
            craft.interactable = ready;
            craft.GetComponent<Image>().color = ready ? Gold : new Color(0.85f, 0.82f, 0.75f, 1f);
        }
    }

    #region UI Builder

    private void BuildUI()
    {
        EnsureEventSystem();

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HouseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        canvas.sortingOrder = 100;

        // Status panel
        GameObject status = MakePanel(canvas.transform, "StatusPanel", new Vector2(470f, 200f), Paper);
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

        AddText(status.transform, "HOME", 30f, Ink, FontStyles.Bold);
        timeText = AddText(status.transform, "MON - 09:00", 14f, Muted, FontStyles.Normal);
        statsText = AddText(status.transform, "Gold 240 G", 15f, Ink, FontStyles.Normal);
        energyText = AddText(status.transform, "Energy 100 / 100", 15f, Ink, FontStyles.Normal);
        productText = AddText(status.transform, "Products ready to sell: 0", 13f, Muted, FontStyles.Normal);

        // Leave button
        Button leave = MakeButton(canvas.transform, "Leave house", 160f, 44f, new Color(0.25f, 0.22f, 0.17f, 1f), () =>
        {
            if (manager != null) manager.LeaveHouse();
        });
        RectTransform leaveRt = leave.GetComponent<RectTransform>();
        leaveRt.anchorMin = new Vector2(1f, 1f);
        leaveRt.anchorMax = new Vector2(1f, 1f);
        leaveRt.pivot = new Vector2(1f, 1f);
        leaveRt.anchoredPosition = new Vector2(-26f, -26f);
        TMP_Text leaveText = leave.GetComponentInChildren<TMP_Text>();
        if (leaveText != null) leaveText.color = new Color(0.98f, 0.95f, 0.88f, 1f);

        // Hint bar
        GameObject hint = MakePanel(canvas.transform, "HintBar", new Vector2(900f, 42f), new Color(0.16f, 0.14f, 0.11f, 0.82f));
        RectTransform hintRt = hint.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0.5f, 0f);
        hintRt.anchorMax = new Vector2(0.5f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.anchoredPosition = new Vector2(0f, 22f);
        TMP_Text hintLabel = AddText(hint.transform, "Click the bed to sleep or the kitchen to craft. Selling happens at the market.",
            13.5f, new Color(0.97f, 0.94f, 0.87f, 1f), FontStyles.Normal);
        Stretch(hintLabel.rectTransform, 18f, 8f);

        // Message
        messageBox = MakePanel(canvas.transform, "Message", new Vector2(900f, 54f), Paper);
        RectTransform msgRt = messageBox.GetComponent<RectTransform>();
        msgRt.anchorMin = new Vector2(0.5f, 0f);
        msgRt.anchorMax = new Vector2(0.5f, 0f);
        msgRt.pivot = new Vector2(0.5f, 0f);
        msgRt.anchoredPosition = new Vector2(0f, 76f);
        messageText = AddText(messageBox.transform, "", 14.5f, Ink, FontStyles.Bold);
        Stretch(messageText.rectTransform, 18f, 8f);
        messageBox.SetActive(false);

        // Modal overlay
        modalRoot = new GameObject("ModalOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modalRoot.transform.SetParent(canvas.transform, false);
        RectTransform modalRt = modalRoot.GetComponent<RectTransform>();
        modalRt.anchorMin = Vector2.zero;
        modalRt.anchorMax = Vector2.one;
        modalRt.sizeDelta = Vector2.zero;
        modalRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

        Button dismiss = modalRoot.AddComponent<Button>();
        dismiss.onClick.AddListener(ClosePanels);
        modalRoot.SetActive(false);

        BuildSleepPanel(modalRoot.transform);
        BuildCraftPanel(modalRoot.transform);
    }

    private void BuildSleepPanel(Transform parent)
    {
        sleepPanel = MakePanel(parent, "SleepPanel", new Vector2(520f, 320f), Paper);
        VerticalLayoutGroup layout = sleepPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 24, 24);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        AddText(sleepPanel.transform, "SLEEP", 28f, Ink, FontStyles.Bold);
        AddText(sleepPanel.transform, "Rest on the bed to restore your energy.", 14f, Muted, FontStyles.Normal);
        sleepEnergyText = AddText(sleepPanel.transform, "Energy now: 100 / 100", 16f, Ink, FontStyles.Normal);

        GameObject row = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(sleepPanel.transform, false);
        row.GetComponent<LayoutElement>().preferredHeight = 48f;
        HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 14f;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;

        MakeButton(row.transform, "Rest", 180f, 44f, Gold, () =>
        {
            if (manager != null) manager.Sleep();
        });
        MakeButton(row.transform, "Cancel", 160f, 44f, new Color(0.9f, 0.88f, 0.82f, 1f), ClosePanels, Ink);
    }

    private void BuildCraftPanel(Transform parent)
    {
        craftPanel = MakePanel(parent, "CraftPanel", new Vector2(660f, 560f), Paper);
        VerticalLayoutGroup layout = craftPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(26, 26, 20, 20);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        AddText(craftPanel.transform, "KITCHEN", 28f, Ink, FontStyles.Bold);
        AddText(craftPanel.transform, "Craft a product from your ingredients. Each recipe takes 3 hours of game time.", 13f, Muted, FontStyles.Normal);

        craftRows = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement)).transform;
        craftRows.SetParent(craftPanel.transform, false);
        craftRows.GetComponent<LayoutElement>().preferredHeight = 360f;
        VerticalLayoutGroup rowsLayout = craftRows.GetComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 6f;
        rowsLayout.childControlWidth = true;
        rowsLayout.childControlHeight = false;

        MakeButton(craftPanel.transform, "Close", 140f, 42f, new Color(0.9f, 0.88f, 0.82f, 1f), ClosePanels, Ink);
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

    private GameObject MakeRow(Transform parent, float height)
    {
        GameObject row = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);

        row.GetComponent<Image>().color = new Color(0.945f, 0.925f, 0.87f, 1f);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 6, 6);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.preferredHeight = height;
        rowLayout.minHeight = height;
        return row;
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
        return AddText(parent, text, size, color, style, 1f);
    }

    private TMP_Text AddText(Transform parent, string text, float size, Color color, FontStyles style, float flexibleWidth)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);

        LayoutElement layout = obj.GetComponent<LayoutElement>();
        layout.flexibleWidth = flexibleWidth;

        TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    private void AddTextRow(Transform parent, string text, float size, Color color)
    {
        AddText(parent, text, size, color, FontStyles.Normal);
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
