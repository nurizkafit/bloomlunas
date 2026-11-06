#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Membangun ulang seluruh UI buku planner di scene MulaiGame, membuat Card_Prefab,
/// dan menyambungkan semua referensi ke PlannerBookUI.
/// Menu: Bloom Lunas > Rebuild Planner Book UI
/// Batchmode: -executeMethod PlannerBookBuilder.RebuildPlannerBook
/// </summary>
public static class PlannerBookBuilder
{
    private const string ScenePath = "Assets/Scenes/MulaiGame.unity";
    private const string CardPrefabPath = "Assets/Card_Prefab.prefab";
    private const string RoundedSpritePath = "Assets/Image/UI_Rounded.png";
    private const string SlotSpritePath = "Assets/Image/UI_SlotRounded.png";

    // ---------- Palette ----------
    static readonly Color Navy = new Color(0.043f, 0.070f, 0.113f, 1f);
    static readonly Color HeaderPanel = new Color(0.075f, 0.110f, 0.180f, 1f);
    static readonly Color StatPanel = new Color(0.106f, 0.145f, 0.220f, 1f);
    static readonly Color Gold = new Color(0.910f, 0.784f, 0.475f, 1f);
    static readonly Color GoldButton = new Color(0.925f, 0.816f, 0.545f, 1f);
    static readonly Color GoldButtonIdle = new Color(0.898f, 0.780f, 0.514f, 1f);
    static readonly Color CreamText = new Color(0.965f, 0.945f, 0.890f, 1f);
    static readonly Color MutedText = new Color(0.612f, 0.647f, 0.710f, 1f);
    static readonly Color Leather = new Color(0.192f, 0.118f, 0.063f, 1f);
    static readonly Color LeatherDark = new Color(0.145f, 0.086f, 0.043f, 1f);
    static readonly Color Trim = new Color(0.780f, 0.635f, 0.376f, 0.55f);
    static readonly Color PageCream = new Color(0.988f, 0.965f, 0.898f, 1f);
    static readonly Color PageCreamSoft = new Color(0.973f, 0.941f, 0.855f, 1f);
    static readonly Color DeckCream = new Color(1f, 0.988f, 0.953f, 1f);
    static readonly Color Ink = new Color(0.180f, 0.165f, 0.133f, 1f);
    static readonly Color InkSoft = new Color(0.373f, 0.345f, 0.278f, 1f);
    static readonly Color InkMuted = new Color(0.553f, 0.518f, 0.443f, 1f);
    static readonly Color LineSoft = new Color(0.886f, 0.835f, 0.729f, 0.85f);
    static readonly Color SlotDay = new Color(0.973f, 0.937f, 0.827f, 1f);
    static readonly Color SlotNight = new Color(0.933f, 0.910f, 0.957f, 1f);
    static readonly Color SlotMidnight = new Color(0.957f, 0.906f, 0.878f, 1f);
    static readonly Color Amber = new Color(0.925f, 0.796f, 0.478f, 1f);
    static readonly Color Good = new Color(0.718f, 0.859f, 0.710f, 1f);
    static readonly Color Danger = new Color(0.925f, 0.737f, 0.702f, 1f);

    static readonly string[] CategoryNames = { "Production", "Shop", "Fieldwork", "Guild / Organization" };
    static readonly Color[] CategoryAccents =
    {
        new Color(0.753f, 0.573f, 0.318f, 1f),
        new Color(0.463f, 0.612f, 0.435f, 1f),
        new Color(0.396f, 0.580f, 0.549f, 1f),
        new Color(0.518f, 0.451f, 0.686f, 1f)
    };

    static readonly string[] TimeLabels = { "06:00", "09:00", "12:00", "15:00", "18:00", "21:00", "00:00", "03:00" };

    static TMP_FontAsset uiFont;
    static TMP_FontAsset titleFont;
    static Sprite roundedSprite;
    static Sprite slotSprite;

    #region Entry Point

    [MenuItem("Bloom Lunas/Rebuild Planner Book UI")]
    public static void RebuildPlannerBook()
    {
        try
        {
            LoadAssets();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[PlannerBookBuilder] Canvas tidak ditemukan di scene MulaiGame.");
                EditorApplication.Exit(1);
                return;
            }

            GameObject oldBook = GameObject.Find("BookPanel");
            if (oldBook != null)
            {
                Debug.Log("[PlannerBookBuilder] Menghapus BookPanel lama.");
                UnityEngine.Object.DestroyImmediate(oldBook);
            }

            GameObject oldPlanner = GameObject.Find("PlannerBookUI");
            if (oldPlanner != null)
            {
                UnityEngine.Object.DestroyImmediate(oldPlanner);
            }

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.localScale = Vector3.one;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.anchoredPosition = Vector2.zero;
            canvasRect.sizeDelta = Vector2.zero;
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.zero;
            canvasRect.pivot = new Vector2(0f, 0f);

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            GameObject cardPrefab = CreateCardPrefab();
            UiRefs refs = BuildPlannerUI(canvasRect, cardPrefab);
            ApplyWiring(refs);

            // Scene MulaiGame adalah fase planning, jadi jangan pernah tersimpan sebagai read-only.
            GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.IsGameStarted = false;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[PlannerBookBuilder] Selesai: buku planner berhasil dibangun dan disimpan.");
        }
        catch (Exception e)
        {
            Debug.LogError("[PlannerBookBuilder] GAGAL: " + e);
            EditorApplication.Exit(1);
        }
    }

    #endregion

    #region Assets

    static void LoadAssets()
    {
        uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/Xmind/Xmind-VariableFont_wght SDF.asset");
        if (uiFont == null) uiFont = TMP_Settings.defaultFontAsset;

        titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/Redaction/Redaction-Regular SDF.asset");
        if (titleFont == null) titleFont = uiFont;

        roundedSprite = EnsureRoundedSprite(RoundedSpritePath, 22f, 24f);
        slotSprite = EnsureRoundedSprite(SlotSpritePath, 7f, 8f);
    }

    /// <summary>
    /// Membuat sprite kotak membulat (9-slice) bila belum ada.
    /// </summary>
    static Sprite EnsureRoundedSprite(string path, float radius, float border)
    {
        if (!File.Exists(path))
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets/Image");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.spriteBorder = new Vector4(border, border, border, border);
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogWarning("[PlannerBookBuilder] Sprite tidak ditemukan: " + path);
        }
        return sprite;
    }

    #endregion

    #region Helpers

    static RectTransform MakeRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(100f, 40f);
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    static RectTransform Stretch(string name, Transform parent, float left, float bottom, float right, float top)
    {
        RectTransform rt = MakeRect(name, parent);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }

    static RectTransform TopLeft(string name, Transform parent, float x, float y, float width, float height)
    {
        RectTransform rt = MakeRect(name, parent);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(width, height);
        return rt;
    }

    static RectTransform TopRight(string name, Transform parent, float x, float y, float width, float height)
    {
        RectTransform rt = MakeRect(name, parent);
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-x, -y);
        rt.sizeDelta = new Vector2(width, height);
        return rt;
    }

    static RectTransform TopBar(string name, Transform parent, float left, float right, float top, float height)
    {
        RectTransform rt = MakeRect(name, parent);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(left, -top - height);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }

    static RectTransform RightMiddle(string name, Transform parent, float x, float width, float height)
    {
        RectTransform rt = MakeRect(name, parent);
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-x, 0f);
        rt.sizeDelta = new Vector2(width, height);
        return rt;
    }

    static Image AddImage(RectTransform rt, Color color, bool rounded = true, bool raycast = false, Sprite sprite = null)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        Sprite use = sprite != null ? sprite : (rounded ? roundedSprite : null);

        if (use != null)
        {
            img.sprite = use;
            img.type = Image.Type.Sliced;
        }

        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    static TextMeshProUGUI AddText(RectTransform rt, string content, float size, Color color,
        TextAlignmentOptions align, TMP_FontAsset font = null, bool wrap = true, float lineSpacing = 0f)
    {
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset useFont = font != null ? font : uiFont;

        tmp.font = useFont;
        if (useFont != null) tmp.fontSharedMaterial = useFont.material;

        tmp.text = content;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.richText = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        tmp.lineSpacing = lineSpacing;

        return tmp;
    }

    static Button AddButton(RectTransform rt, Color color, bool rounded, out Image background)
    {
        background = AddImage(rt, color, rounded, true);

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
        colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        return button;
    }

    static LayoutElement AddLayout(RectTransform rt, float preferredWidth = -1f, float preferredHeight = -1f,
        float flexibleWidth = -1f, float flexibleHeight = -1f, float minWidth = -1f, float minHeight = -1f)
    {
        LayoutElement element = rt.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = preferredWidth;
        element.preferredHeight = preferredHeight;
        element.flexibleWidth = flexibleWidth;
        element.flexibleHeight = flexibleHeight;
        element.minWidth = minWidth;
        element.minHeight = minHeight;
        return element;
    }

    static Outline AddOutline(Graphic graphic, Color color, Vector2 distance)
    {
        Outline outline = graphic.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = false;
        outline.enabled = false;
        return outline;
    }

    static void AddDivider(RectTransform parent, float top, float left, float right, Color color)
    {
        RectTransform line = TopBar("Divider", parent, left, right, top, 1f);
        AddImage(line, color, false, false);
    }

    static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    static TextMeshProUGUI AddLabel(RectTransform parent, string name, float x, float y, float width, float height,
        string content, float size, Color color, TextAlignmentOptions align, TMP_FontAsset font = null)
    {
        RectTransform rt = TopLeft(name, parent, x, y, width, height);
        return AddText(rt, content, size, color, align, font);
    }

    #endregion

    #region Card Prefab

    static GameObject CreateCardPrefab()
    {
        GameObject root = new GameObject("Card_Prefab", typeof(RectTransform));
        root.layer = LayerMask.NameToLayer("UI");

        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(240f, 82f);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);

        Image background = AddImage(rt, new Color(1f, 0.992f, 0.969f, 1f), true, true);
        Outline cardEdge = background.gameObject.AddComponent<Outline>();
        cardEdge.effectColor = new Color(0.839f, 0.780f, 0.643f, 0.55f);
        cardEdge.effectDistance = new Vector2(1f, -1f);
        cardEdge.useGraphicAlpha = false;

        CanvasGroup group = root.AddComponent<CanvasGroup>();
        DraggableCard card = root.AddComponent<DraggableCard>();
        AddLayout(rt, -1f, 82f, 1f, -1f, -1f, 70f);

        // Accent bar kiri
        RectTransform accentRt = MakeRect("Accent", rt);
        accentRt.anchorMin = new Vector2(0f, 0f);
        accentRt.anchorMax = new Vector2(0f, 1f);
        accentRt.pivot = new Vector2(0f, 0.5f);
        accentRt.offsetMin = new Vector2(0f, 7f);
        accentRt.offsetMax = new Vector2(4f, -7f);
        Image accent = AddImage(accentRt, new Color(0.6f, 0.5f, 0.35f, 1f), true, false, slotSprite);

        // Title
        RectTransform titleRt = MakeRect("Title", rt);
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.offsetMin = new Vector2(14f, -28f);
        titleRt.offsetMax = new Vector2(-28f, -7f);
        TextMeshProUGUI title = AddText(titleRt, "Craft", 13f, Ink, TextAlignmentOptions.MidlineLeft, null, false);

        // Subtext
        RectTransform subRt = MakeRect("Sub", rt);
        subRt.anchorMin = new Vector2(0f, 1f);
        subRt.anchorMax = new Vector2(1f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.offsetMin = new Vector2(14f, -61f);
        subRt.offsetMax = new Vector2(-10f, -29f);
        TextMeshProUGUI sub = AddText(subRt, "3h - Home atelier", 8.8f,
            new Color(0.451f, 0.420f, 0.349f, 1f), TextAlignmentOptions.TopLeft);

        // Time / category
        RectTransform timeRt = TopLeft("Time", rt, 14f, 64f, 160f, 13f);
        TextMeshProUGUI time = AddText(timeRt, "Production", 7.8f, InkMuted, TextAlignmentOptions.MidlineLeft, null, false);

        // Delete button
        RectTransform delRt = TopRight("Delete", rt, 6f, 6f, 18f, 18f);
        Image delBg = AddImage(delRt, new Color(0f, 0f, 0f, 0.04f), true, true);
        Button delBtn = delRt.gameObject.AddComponent<Button>();
        delBtn.targetGraphic = delBg;

        RectTransform delLabelRt = Stretch("X", delRt, 0f, 0f, 0f, 0f);
        AddText(delLabelRt, "x", 10f, InkMuted, TextAlignmentOptions.Center, null, false);

        SerializedObject so = new SerializedObject(card);
        SetProperty(so, "titleText", title);
        SetProperty(so, "subText", sub);
        SetProperty(so, "timeText", time);
        SetProperty(so, "cardBackground", background);
        SetProperty(so, "accentBar", accent);
        SetProperty(so, "deleteButton", delBtn);
        SetProperty(so, "canvasGroup", group);
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        if (prefab == null)
        {
            Debug.LogError("[PlannerBookBuilder] Gagal membuat Card_Prefab.");
        }
        else
        {
            Debug.Log("[PlannerBookBuilder] Card_Prefab dibuat ulang.");
        }

        return prefab;
    }

    #endregion

    #region Main UI

    class DayRef
    {
        public int index;
        public TextMeshProUGUI slots;
        public GameObject badgeRoot;
        public TextMeshProUGUI badge;
    }

    class CategoryRef
    {
        public string name;
        public Button button;
        public TextMeshProUGUI count;
    }

    class TabRef
    {
        public PlannerBookUI.PlannerTab tab;
        public Button button;
        public Image background;
        public TextMeshProUGUI title;
        public Color active;
        public Color idle;
    }

    class UiRefs
    {
        public PlannerBookUI planner;
        public GameObject weekPage, ledgerPage, marketPage, townPage, journalPage;
        public TextMeshProUGUI weekLabel, gold, debt, nextPayment, nextPaymentSub, livingCost, livingCostSub;
        public TextMeshProUGUI goldAfter, goldAfterSub, energy, warningChip;
        public Image energyFill, warningChipBackground;
        public Button reviewButton, backButton, startAnyway, closeReview;
        public GameObject categoriesView, cardsView, reviewModal, toastRoot;
        public TextMeshProUGUI categoryTitle, deckHint, selectedSlotText, ledgerBody, marketBody, townBody, journalBody;
        public TextMeshProUGUI reviewSummary, reviewWarnings, toastText;
        public CanvasGroup toastGroup;
        public Transform cardsContainer;
        public readonly List<DayRef> days = new List<DayRef>();
        public readonly List<CategoryRef> categories = new List<CategoryRef>();
        public readonly List<TabRef> tabs = new List<TabRef>();
    }

    static UiRefs BuildPlannerUI(RectTransform canvas, GameObject cardPrefab)
    {
        UiRefs r = new UiRefs();

        RectTransform plannerRoot = Stretch("PlannerBookUI", canvas, 0f, 0f, 0f, 0f);
        r.planner = plannerRoot.gameObject.AddComponent<PlannerBookUI>();

        // Background
        RectTransform bg = Stretch("Background", plannerRoot, 0f, 0f, 0f, 0f);
        AddImage(bg, Navy, false, false);

        BuildHeader(plannerRoot, r);

        RectTransform bookArea = Stretch("BookArea", plannerRoot, 24f, 16f, 24f, 104f);

        RectTransform pagesHost = Stretch("PagesHost", bookArea, 0f, 0f, 176f, 0f);
        BuildTabRail(bookArea, r);

        // Halaman Week
        RectTransform weekPage = Stretch("WeekPage", pagesHost, 0f, 0f, 0f, 0f);
        r.weekPage = weekPage.gameObject;
        BuildWeekPage(weekPage, r, cardPrefab);

        // Halaman lain
        r.ledgerPage = BuildAltPage(pagesHost, "LedgerPage", "Ledger",
            "Assets, budget and the week at a glance", out r.ledgerBody);
        r.marketPage = BuildAltPage(pagesHost, "MarketPage", "Market",
            "Production line and shop forecast", out r.marketBody);
        r.townPage = BuildAltPage(pagesHost, "TownPage", "Town",
            "Places, opening hours and visits", out r.townBody);
        r.journalPage = BuildAltPage(pagesHost, "JournalPage", "Journal",
            "Notes and recap of the week", out r.journalBody);

        r.ledgerPage.SetActive(false);
        r.marketPage.SetActive(false);
        r.townPage.SetActive(false);
        r.journalPage.SetActive(false);

        BuildReviewModal(plannerRoot, r);
        BuildToast(plannerRoot, r);

        return r;
    }

    #endregion

    #region Header

    static void BuildHeader(RectTransform plannerRoot, UiRefs r)
    {
        RectTransform header = TopBar("Header", plannerRoot, 24f, 24f, 16f, 76f);
        AddImage(header, HeaderPanel, true, false);

        // Judul game
        RectTransform titleRt = TopLeft("Title", header, 18f, 10f, 220f, 34f);
        AddText(titleRt, "BloomLunas", 25f, Gold, TextAlignmentOptions.MidlineLeft, titleFont);

        RectTransform subRt = TopLeft("WeekLabel", header, 19f, 43f, 230f, 14f);
        r.weekLabel = AddText(subRt, "MONTH 1  -  WEEK 1  -  DAY 1 - 7", 7.5f, MutedText,
            TextAlignmentOptions.MidlineLeft, null, false);
        r.weekLabel.characterSpacing = 1.2f;

        // Statistik
        StatWidgets gold = BuildStat(header, 244f, 6f, 104f, 64f, "GOLD", "240 G", null);
        r.gold = gold.value;

        StatWidgets debt = BuildStat(header, 356f, 6f, 112f, 64f, "DEBT", "3,000 G", null);
        r.debt = debt.value;

        StatWidgets next = BuildStat(header, 476f, 6f, 196f, 64f, "NEXT PAYMENT", "420 G",
            "Month end (day 28) - in 28 days");
        r.nextPayment = next.value;
        r.nextPaymentSub = next.sub;

        StatWidgets living = BuildStat(header, 680f, 6f, 188f, 64f, "WEEKLY LIVING COST", "40 G", "paid in Check");
        r.livingCost = living.value;
        r.livingCostSub = living.sub;

        StatWidgets after = BuildStat(header, 876f, 6f, 196f, 64f, "GOLD AFTER PLAN", "239 - 264 G",
            "end of week forecast");
        r.goldAfter = after.value;
        r.goldAfterSub = after.sub;

        // Energy
        RectTransform energyRt = TopLeft("Stat_Energy", header, 1080f, 6f, 196f, 64f);
        AddImage(energyRt, StatPanel, true, false);

        TextMeshProUGUI energyLabel = AddLabel(energyRt, "Label", 11f, 7f, 110f, 12f, "ENERGY", 7f, MutedText,
            TextAlignmentOptions.MidlineLeft);
        energyLabel.characterSpacing = 1.2f;

        RectTransform energyValueRt = TopRight("Value", energyRt, 11f, 5f, 100f, 16f);
        r.energy = AddText(energyValueRt, "100 / 100", 9.5f, CreamText, TextAlignmentOptions.MidlineRight, null, false);

        RectTransform barBg = TopLeft("BarBg", energyRt, 11f, 28f, 174f, 9f);
        AddImage(barBg, new Color(0.18f, 0.23f, 0.32f, 1f), true, false, slotSprite);

        RectTransform barFill = Stretch("BarFill", barBg, 1f, 1f, 1f, 1f);
        r.energyFill = AddImage(barFill, new Color(0.541f, 0.784f, 0.573f, 1f), true, false, slotSprite);

        RectTransform energyHintRt = TopLeft("Hint", energyRt, 11f, 42f, 174f, 14f);
        AddText(energyHintRt, "rest available today", 7.2f, MutedText, TextAlignmentOptions.MidlineLeft, null, false);

        // Warning chip
        RectTransform chipRt = RightMiddle("WarningChip", header, 180f, 176f, 30f);
        r.warningChipBackground = AddImage(chipRt, new Color(0.129f, 0.180f, 0.263f, 1f), true, false);

        RectTransform dotRt = TopLeft("Dot", chipRt, 10f, 12f, 6f, 6f);
        AddImage(dotRt, Amber, true, false, slotSprite);

        RectTransform chipLabelRt = TopLeft("Text", chipRt, 22f, 0f, 146f, 30f);
        r.warningChip = AddText(chipLabelRt, "Plan looks good", 9f, Amber, TextAlignmentOptions.MidlineLeft, null, false);

        // Review button
        RectTransform reviewRt = RightMiddle("ReviewButton", header, 16f, 150f, 42f);
        Image reviewBg;
        r.reviewButton = AddButton(reviewRt, GoldButton, true, out reviewBg);

        RectTransform reviewLabelRt = Stretch("Label", reviewRt, 8f, 0f, 8f, 0f);
        AddText(reviewLabelRt, "Review plan", 11f, new Color(0.212f, 0.161f, 0.078f, 1f), TextAlignmentOptions.Center);
    }

    struct StatWidgets
    {
        public TextMeshProUGUI label;
        public TextMeshProUGUI value;
        public TextMeshProUGUI sub;
    }

    static StatWidgets BuildStat(Transform parent, float x, float y, float width, float height,
        string label, string value, string sub)
    {
        RectTransform rt = TopLeft("Stat_" + label, parent, x, y, width, height);
        AddImage(rt, StatPanel, true, false);

        TextMeshProUGUI labelTmp = AddLabel(rt, "Label", 11f, 7f, width - 22f, 12f, label, 7f, MutedText,
            TextAlignmentOptions.MidlineLeft);
        labelTmp.characterSpacing = 1.2f;

        TextMeshProUGUI valueTmp = AddLabel(rt, "Value", 11f, 19f, width - 22f, 24f, value, 16.5f, Gold,
            TextAlignmentOptions.MidlineLeft);

        TextMeshProUGUI subTmp = null;
        if (!string.IsNullOrEmpty(sub))
        {
            subTmp = AddLabel(rt, "Sub", 11f, 44f, width - 22f, 13f, sub, 6.9f, MutedText,
                TextAlignmentOptions.MidlineLeft);
        }

        return new StatWidgets { label = labelTmp, value = valueTmp, sub = subTmp };
    }

    #endregion

    #region Tab Rail

    static void BuildTabRail(RectTransform bookArea, UiRefs r)
    {
        RectTransform rail = MakeRect("TabRail", bookArea);
        rail.anchorMin = new Vector2(1f, 0f);
        rail.anchorMax = new Vector2(1f, 1f);
        rail.pivot = new Vector2(1f, 0.5f);
        rail.sizeDelta = new Vector2(168f, 0f);
        rail.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = rail.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(0, 0, 4, 0);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        AddTab(rail, r, PlannerBookUI.PlannerTab.Week, "Week", "Plan days 1 - 7",
            new Color(0.957f, 0.914f, 0.749f, 1f), new Color(0.910f, 0.855f, 0.686f, 1f));
        AddTab(rail, r, PlannerBookUI.PlannerTab.Ledger, "Ledger", "Assets - budget",
            new Color(0.812f, 0.886f, 0.788f, 1f), new Color(0.886f, 0.933f, 0.859f, 1f));
        AddTab(rail, r, PlannerBookUI.PlannerTab.Market, "Market", "Forecast",
            new Color(0.796f, 0.863f, 0.925f, 1f), new Color(0.878f, 0.914f, 0.949f, 1f));
        AddTab(rail, r, PlannerBookUI.PlannerTab.Town, "Town", "Schedule - hours",
            new Color(0.894f, 0.824f, 0.745f, 1f), new Color(0.933f, 0.886f, 0.827f, 1f));
        AddTab(rail, r, PlannerBookUI.PlannerTab.Journal, "Journal", "Notes - recap",
            new Color(0.847f, 0.827f, 0.925f, 1f), new Color(0.902f, 0.894f, 0.945f, 1f));
    }

    static void AddTab(Transform rail, UiRefs r, PlannerBookUI.PlannerTab tab, string title, string subtitle,
        Color active, Color idle)
    {
        RectTransform rt = MakeRect("Tab_" + title, rail);
        AddLayout(rt, -1f, 92f, 1f, -1f);

        Image bg;
        Button button = AddButton(rt, idle, true, out bg);
        Outline outline = AddOutline(bg, new Color(0.502f, 0.392f, 0.235f, 0.55f), new Vector2(1f, -1f));

        RectTransform titleRt = TopLeft("Title", rt, 14f, 12f, 140f, 24f);
        TextMeshProUGUI titleTmp = AddText(titleRt, title, 14f, InkSoft, TextAlignmentOptions.MidlineLeft, titleFont, false);

        RectTransform subtitleRt = TopLeft("Subtitle", rt, 14f, 38f, 140f, 30f);
        AddText(subtitleRt, subtitle, 8.2f, InkMuted, TextAlignmentOptions.TopLeft);

        r.tabs.Add(new TabRef
        {
            tab = tab,
            button = button,
            background = bg,
            title = titleTmp,
            active = active,
            idle = idle
        });

        outline.enabled = false;
    }

    #endregion

    #region Week Page

    static void BuildWeekPage(RectTransform weekPage, UiRefs r, GameObject cardPrefab)
    {
        // Shadow buku
        RectTransform shadow = Stretch("BookShadow", weekPage, -8f, -20f, 8f, 8f);
        AddImage(shadow, new Color(0f, 0f, 0f, 0.28f), true, false);

        // Cover kulit
        RectTransform cover = Stretch("BookCover", weekPage, 0f, 0f, 0f, 0f);
        AddImage(cover, LeatherDark, true, false);

        RectTransform trim = Stretch("Trim", cover, 7f, 7f, 7f, 7f);
        AddImage(trim, Trim, true, false);

        RectTransform coverInner = Stretch("CoverInner", cover, 8f, 8f, 8f, 8f);
        AddImage(coverInner, Leather, true, false);

        RectTransform pages = Stretch("Pages", cover, 16f, 16f, 16f, 16f);

        // Halaman kiri: Mon - Thu
        RectTransform leftPage = MakeRect("LeftPage", pages);
        leftPage.anchorMin = new Vector2(0f, 0f);
        leftPage.anchorMax = new Vector2(0.5f, 1f);
        leftPage.pivot = new Vector2(0.5f, 0.5f);
        leftPage.offsetMin = new Vector2(0f, 0f);
        leftPage.offsetMax = new Vector2(-6f, 0f);
        AddImage(leftPage, PageCream, true, false);

        // Halaman kanan: Fri - Sun + deck
        RectTransform rightPage = MakeRect("RightPage", pages);
        rightPage.anchorMin = new Vector2(0.5f, 0f);
        rightPage.anchorMax = new Vector2(1f, 1f);
        rightPage.pivot = new Vector2(0.5f, 0.5f);
        rightPage.offsetMin = new Vector2(6f, 0f);
        rightPage.offsetMax = new Vector2(0f, 0f);
        AddImage(rightPage, PageCream, true, false);

        // Bayangan tulang buku
        RectTransform spine = MakeRect("Spine", pages);
        spine.anchorMin = new Vector2(0.5f, 0f);
        spine.anchorMax = new Vector2(0.5f, 1f);
        spine.pivot = new Vector2(0.5f, 0.5f);
        spine.sizeDelta = new Vector2(18f, 0f);
        spine.anchoredPosition = Vector2.zero;
        AddImage(spine, new Color(0f, 0f, 0f, 0.07f), false, false);

        BuildPageHeader(leftPage, "This week - Mon - Thu", "3-HOUR SLOTS  -  DRAG A CARD OR CLICK A SLOT");
        BuildPageHeader(rightPage, "Fri - Sun",
            "06:00-18:00 DAY   /   18:00-00:00 NIGHT   /   00:00-03:00 MIDNIGHT");

        BuildCalendar(leftPage, new[] { 0, 1, 2, 3 }, r, null);
        BuildCalendar(rightPage, new[] { 4, 5, 6 }, r, cardPrefab);

        // Footer halaman kanan
        RectTransform footerRt = TopRight("Footer", rightPage, 22f, 0f, 220f, 16f);
        footerRt.anchorMin = new Vector2(1f, 0f);
        footerRt.anchorMax = new Vector2(1f, 0f);
        footerRt.pivot = new Vector2(1f, 0f);
        footerRt.anchoredPosition = new Vector2(-22f, 8f);
        AddText(footerRt, "Week 1", 8.5f, InkMuted, TextAlignmentOptions.MidlineRight, titleFont, false);
    }

    static void BuildPageHeader(RectTransform page, string title, string meta)
    {
        RectTransform titleRt = TopLeft("PageTitle", page, 20f, 16f, 470f, 30f);
        AddText(titleRt, title, 18f, Ink, TextAlignmentOptions.MidlineLeft, titleFont);

        RectTransform metaRt = TopBar("PageMeta", page, 200f, 20f, 24f, 16f);
        TextMeshProUGUI metaTmp = AddText(metaRt, meta, 7.1f, new Color(0.478f, 0.443f, 0.369f, 1f),
            TextAlignmentOptions.MidlineRight, null, false);
        metaTmp.characterSpacing = 0.8f;
    }

    static void BuildCalendar(RectTransform page, int[] dayIndexes, UiRefs r, GameObject cardPrefab)
    {
        RectTransform row = Stretch("Calendar", page, 18f, 16f, 18f, 64f);

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        // Gutter jam
        RectTransform gutter = MakeRect("TimeGutter", row);
        AddLayout(gutter, 46f, -1f, 0f, 1f, 46f);

        RectTransform labels = Stretch("Labels", gutter, 0f, 0f, 0f, 54f);
        for (int i = 0; i < 8; i++)
        {
            RectTransform labelRt = MakeRect("Time_" + TimeLabels[i], labels);
            labelRt.anchorMin = new Vector2(0f, 1f - (i + 1) / 8f);
            labelRt.anchorMax = new Vector2(1f, 1f - i / 8f);
            labelRt.pivot = new Vector2(0.5f, 0.5f);
            labelRt.offsetMin = new Vector2(0f, 6f);
            labelRt.offsetMax = new Vector2(-4f, -6f);

            AddText(labelRt, TimeLabels[i], 7.8f, InkMuted, TextAlignmentOptions.MidlineRight, null, false);
        }

        foreach (int day in dayIndexes)
        {
            BuildDayColumn(row, day, r);
        }

        if (cardPrefab != null)
        {
            BuildDeck(row, r, cardPrefab);
        }
    }

    static void BuildDayColumn(Transform row, int dayIndex, UiRefs r)
    {
        string dayName = GameManager.DayShortNames[dayIndex];

        RectTransform col = MakeRect("DayColumn_" + dayName, row);
        AddLayout(col, -1f, -1f, 1f, 1f, 110f);

        // Header hari
        RectTransform head = TopBar("Header", col, 0f, 0f, 0f, 54f);

        RectTransform nameRt = TopLeft("DayName", head, 2f, 1f, 120f, 22f);
        bool closed = dayIndex == 6;
        AddText(nameRt, dayName, 16f, closed ? InkMuted : Ink, TextAlignmentOptions.MidlineLeft, titleFont);

        RectTransform dayNoRt = TopRight("DayNumber", head, 2f, 4f, 60f, 14f);
        AddText(dayNoRt, "Day " + (dayIndex + 1), 7.8f, InkMuted, TextAlignmentOptions.MidlineRight, null, false);

        // Badge Market / Closed
        bool market = dayIndex == 0 || dayIndex == 2 || dayIndex == 5;
        RectTransform badgeRt = TopLeft("Badge", head, 2f, 24f, 50f, 15f);
        Color badgeColor = closed ? new Color(0.941f, 0.863f, 0.847f, 1f)
                                  : new Color(0.875f, 0.918f, 0.800f, 1f);
        AddImage(badgeRt, badgeColor, true, false, slotSprite);

        RectTransform badgeTextRt = Stretch("Text", badgeRt, 0f, 0f, 0f, 0f);
        TextMeshProUGUI badgeText = AddText(badgeTextRt, closed ? "CLOSED" : "MARKET", 6.4f,
            closed ? new Color(0.541f, 0.357f, 0.318f, 1f) : new Color(0.310f, 0.396f, 0.251f, 1f),
            TextAlignmentOptions.Center, null, false);
        badgeText.characterSpacing = 0.8f;

        // Jumlah slot
        RectTransform slotsRt = TopLeft("SlotSummary", head, 2f, 39f, 170f, 14f);
        TextMeshProUGUI slotsText = AddText(slotsRt, "0 slots of work", 8.2f, InkMuted,
            TextAlignmentOptions.MidlineLeft, null, false);

        r.days.Add(new DayRef
        {
            index = dayIndex,
            slots = slotsText,
            badgeRoot = badgeRt.gameObject,
            badge = badgeText
        });

        // Area slot dimulai di bawah header hari, supaya header tidak tertutup slot pertama.
        RectTransform slotsArea = Stretch("Slots", col, 0f, 0f, 0f, 54f);

        // 8 slot waktu
        for (int i = 0; i < 8; i++)
        {
            Color tint = SlotDay;
            string label = string.Empty; // jam sudah ada di gutter; slot biasa dibiarkan bersih

            if (i == 4 || i == 5)
            {
                tint = SlotNight;
                label = "Night";
            }
            else if (i >= 6)
            {
                tint = SlotMidnight;
                label = "Midnight";
            }

            RectTransform slotRt = MakeRect("Slot_" + TimeLabels[i], slotsArea);
            slotRt.anchorMin = new Vector2(0f, 1f - (i + 1) / 8f);
            slotRt.anchorMax = new Vector2(1f, 1f - i / 8f);
            slotRt.pivot = new Vector2(0.5f, 0.5f);
            slotRt.offsetMin = new Vector2(0f, 1.5f);
            slotRt.offsetMax = new Vector2(0f, -1.5f);

            Image slotImage = AddImage(slotRt, tint, true, true, slotSprite);

            RectTransform emptyRt = TopLeft("Empty", slotRt, 7f, 5f, 130f, 14f);
            TextMeshProUGUI emptyLabel = AddText(emptyRt, label, 8f, new Color(0.510f, 0.475f, 0.396f, 1f),
                TextAlignmentOptions.MidlineLeft, null, false);

            Outline selection = AddOutline(slotImage, new Color(0.804f, 0.647f, 0.337f, 1f), new Vector2(1.4f, -1.4f));

            DropSlot drop = slotRt.gameObject.AddComponent<DropSlot>();
            drop.dayIndex = dayIndex;
            drop.timeSlot = TimeLabels[i];

            SerializedObject so = new SerializedObject(drop);
            SetProperty(so, "slotImage", slotImage);
            SetProperty(so, "emptyLabel", emptyLabel);
            SetProperty(so, "selectionOutline", selection);
            SetProperty(so, "normalColor", tint);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    #endregion

    #region Deck

    static void BuildDeck(Transform row, UiRefs r, GameObject cardPrefab)
    {
        RectTransform deck = MakeRect("Deck", row);
        AddLayout(deck, 240f, -1f, 0f, 1f, 220f);
        AddImage(deck, DeckCream, true, false, roundedSprite);

        VerticalLayoutGroup layout = deck.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 7f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Top row: back + title
        RectTransform topRow = MakeRect("TopRow", deck);
        AddLayout(topRow, -1f, 22f);

        RectTransform backRt = TopLeft("BackButton", topRow, 0f, 0f, 20f, 22f);
        Image backBg;
        r.backButton = AddButton(backRt, new Color(0f, 0f, 0f, 0.05f), true, out backBg);
        RectTransform backLabelRt = Stretch("Label", backRt, 0f, 0f, 0f, 0f);
        AddText(backLabelRt, "<", 12f, InkSoft, TextAlignmentOptions.Center, null, false);

        RectTransform deckTitleRt = Stretch("Title", topRow, 24f, 0f, 0f, 0f);
        r.categoryTitle = AddText(deckTitleRt, "Categories", 13f, Ink, TextAlignmentOptions.MidlineLeft, null, false);

        // Hint
        RectTransform hintRt = MakeRect("Hint", deck);
        AddLayout(hintRt, -1f, 38f);
        r.deckHint = AddText(hintRt, "Drag a card into a slot, or click a slot first and then a card.",
            8.4f, new Color(0.424f, 0.392f, 0.322f, 1f), TextAlignmentOptions.TopLeft, null, true, 4f);

        RectTransform dividerRt = MakeRect("Divider", deck);
        AddLayout(dividerRt, -1f, 1f);
        AddImage(dividerRt, LineSoft, false, false);

        // Categories view
        RectTransform catView = MakeRect("CategoriesView", deck);
        AddLayout(catView, -1f, -1f, 1f, 1f);

        VerticalLayoutGroup catLayout = catView.gameObject.AddComponent<VerticalLayoutGroup>();
        catLayout.spacing = 6f;
        catLayout.childControlWidth = true;
        catLayout.childControlHeight = true;
        catLayout.childForceExpandWidth = true;
        catLayout.childForceExpandHeight = false;

        for (int i = 0; i < CategoryNames.Length; i++)
        {
            AddCategoryButton(catView, r, CategoryNames[i], CategoryAccents[i], i + 1);
        }

        RectTransform spacer = MakeRect("Spacer", catView);
        AddLayout(spacer, -1f, -1f, -1f, 1f);

        RectTransform tipRt = MakeRect("Tip", catView);
        AddLayout(tipRt, -1f, 52f);
        AddText(tipRt, "Tip: every card is one 3-hour slot. Some activities only fit morning or afternoon hours.",
            8f, InkMuted, TextAlignmentOptions.TopLeft, null, true, 4f);

        r.categoriesView = catView.gameObject;

        // Cards view
        RectTransform cardsView = MakeRect("CardsView", deck);
        AddLayout(cardsView, -1f, -1f, 1f, 1f);

        RectTransform viewport = Stretch("Viewport", cardsView, 0f, 0f, 0f, 0f);
        AddImage(viewport, new Color(1f, 1f, 1f, 0f), false, true);
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = MakeRect("CardsContainer", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 6f;
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = cardsView.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;

        r.cardsContainer = content;
        r.cardsView = cardsView.gameObject;
        cardsView.gameObject.SetActive(false);

        // Selected slot footer
        RectTransform selectedRt = MakeRect("SelectedSlot", deck);
        AddLayout(selectedRt, -1f, 26f);
        r.selectedSlotText = AddText(selectedRt, "No slot selected", 8f, InkMuted,
            TextAlignmentOptions.MidlineLeft, null, false);
    }

    static void AddCategoryButton(Transform parent, UiRefs r, string name, Color accent, int count)
    {
        RectTransform rt = MakeRect("Category_" + name.Replace("/", "").Replace(" ", ""), parent);
        AddLayout(rt, -1f, 52f);

        Image bg;
        Button button = AddButton(rt, new Color(0.973f, 0.949f, 0.886f, 1f), true, out bg);
        Outline outline = AddOutline(bg, accent, new Vector2(1f, -1f));
        outline.enabled = false;

        RectTransform stripe = MakeRect("Accent", rt);
        stripe.anchorMin = new Vector2(0f, 0f);
        stripe.anchorMax = new Vector2(0f, 1f);
        stripe.pivot = new Vector2(0f, 0.5f);
        stripe.offsetMin = new Vector2(6f, 9f);
        stripe.offsetMax = new Vector2(10f, -9f);
        AddImage(stripe, accent, true, false, slotSprite);

        RectTransform nameRt = Stretch("Name", rt, 18f, 8f, 34f, 8f);
        AddText(nameRt, name, 11.5f, Ink, TextAlignmentOptions.MidlineLeft, null, true, 2f);

        RectTransform countRt = RightMiddle("Count", rt, 8f, 24f, 24f);
        AddText(countRt, count.ToString(), 9f, InkMuted, TextAlignmentOptions.Center, null, false);

        r.categories.Add(new CategoryRef { name = name, button = button, count = countRt.GetComponent<TextMeshProUGUI>() });
    }

    #endregion

    #region Alt Pages

    static GameObject BuildAltPage(Transform parent, string name, string title, string subtitle, out TextMeshProUGUI body)
    {
        RectTransform page = Stretch(name, parent, 0f, 0f, 0f, 0f);
        AddImage(page, LeatherDark, true, false);

        RectTransform inner = Stretch("Page", page, 16f, 16f, 16f, 16f);
        AddImage(inner, PageCream, true, false);

        RectTransform titleRt = TopLeft("Title", inner, 56f, 44f, 700f, 44f);
        AddText(titleRt, title, 30f, Ink, TextAlignmentOptions.MidlineLeft, titleFont);

        RectTransform subtitleRt = TopLeft("Subtitle", inner, 58f, 92f, 760f, 20f);
        AddText(subtitleRt, subtitle, 9.5f, InkMuted, TextAlignmentOptions.MidlineLeft);

        AddDivider(inner, 124f, 56f, 56f, LineSoft);

        RectTransform bodyRt = Stretch("Body", inner, 58f, 64f, 58f, 154f);
        body = AddText(bodyRt, "", 12.5f, InkSoft, TextAlignmentOptions.TopLeft, null, true, 10f);

        RectTransform footerRt = TopRight("Footer", inner, 40f, 0f, 260f, 18f);
        footerRt.anchorMin = new Vector2(1f, 0f);
        footerRt.anchorMax = new Vector2(1f, 0f);
        footerRt.pivot = new Vector2(1f, 0f);
        footerRt.anchoredPosition = new Vector2(-40f, 22f);
        AddText(footerRt, "BloomLunas  -  Week 1", 8f, InkMuted, TextAlignmentOptions.MidlineRight, titleFont, false);

        return page.gameObject;
    }

    #endregion

    #region Review Modal & Toast

    static void BuildReviewModal(RectTransform plannerRoot, UiRefs r)
    {
        RectTransform modal = Stretch("ReviewModal", plannerRoot, 0f, 0f, 0f, 0f);
        r.reviewModal = modal.gameObject;

        Image overlay = AddImage(modal, new Color(0.02f, 0.03f, 0.06f, 0.62f), false, true);
        overlay.raycastTarget = true;

        RectTransform shadow = MakeRect("Shadow", modal);
        shadow.anchorMin = new Vector2(0.5f, 0.5f);
        shadow.anchorMax = new Vector2(0.5f, 0.5f);
        shadow.pivot = new Vector2(0.5f, 0.5f);
        shadow.sizeDelta = new Vector2(776f, 636f);
        shadow.anchoredPosition = new Vector2(0f, -10f);
        AddImage(shadow, new Color(0f, 0f, 0f, 0.35f), true, false);

        RectTransform panel = MakeRect("Panel", modal);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(760f, 620f);
        panel.anchoredPosition = Vector2.zero;
        AddImage(panel, PageCream, true, false);

        RectTransform titleRt = TopLeft("Title", panel, 34f, 24f, 520f, 34f);
        AddText(titleRt, "Review plan", 25f, Ink, TextAlignmentOptions.MidlineLeft, titleFont);

        RectTransform subtitleRt = TopLeft("Subtitle", panel, 36f, 58f, 620f, 20f);
        AddText(subtitleRt, "Week 1  -  Day 1 to 7  -  check everything before you start.", 9.2f, InkMuted,
            TextAlignmentOptions.MidlineLeft);

        RectTransform closeRt = TopRight("CloseX", panel, 22f, 20f, 28f, 28f);
        Image closeBg;
        Button closeX = AddButton(closeRt, new Color(0f, 0f, 0f, 0.04f), true, out closeBg);
        RectTransform closeLabelRt = Stretch("Label", closeRt, 0f, 0f, 0f, 0f);
        AddText(closeLabelRt, "x", 13f, InkSoft, TextAlignmentOptions.Center, null, false);
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(closeX.onClick, r.planner.CloseReviewModal);

        AddDivider(panel, 92f, 34f, 34f, LineSoft);

        RectTransform summaryLabelRt = TopLeft("SummaryLabel", panel, 36f, 104f, 300f, 14f);
        TextMeshProUGUI summaryLabel = AddText(summaryLabelRt, "YOUR WEEK", 7.6f, InkMuted,
            TextAlignmentOptions.MidlineLeft, null, false);
        summaryLabel.characterSpacing = 1.4f;

        // Scroll ringkasan
        RectTransform viewport = TopLeft("SummaryViewport", panel, 34f, 124f, 692f, 300f);
        AddImage(viewport, new Color(1f, 1f, 1f, 0f), false, true);
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = MakeRect("Summary", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        r.reviewSummary = AddText(content, "", 12.2f, new Color(0.298f, 0.271f, 0.220f, 1f),
            TextAlignmentOptions.TopLeft, null, true, 7f);

        ContentSizeFitter summaryFitter = content.gameObject.AddComponent<ContentSizeFitter>();
        summaryFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;

        // Kotak peringatan
        RectTransform warnBox = TopLeft("WarningBox", panel, 34f, 438f, 692f, 92f);
        AddImage(warnBox, new Color(0.973f, 0.937f, 0.859f, 1f), true, false);

        RectTransform warnLabelRt = TopLeft("Label", warnBox, 14f, 8f, 300f, 14f);
        TextMeshProUGUI warnLabel = AddText(warnLabelRt, "WARNINGS", 7.2f, InkMuted, TextAlignmentOptions.MidlineLeft,
            null, false);
        warnLabel.characterSpacing = 1.4f;

        RectTransform warnTextRt = TopLeft("Text", warnBox, 14f, 24f, 664f, 60f);
        r.reviewWarnings = AddText(warnTextRt, "", 10f, new Color(0.416f, 0.290f, 0.157f, 1f),
            TextAlignmentOptions.TopLeft, null, true, 4f);

        // Tombol
        RectTransform keepRt = TopLeft("KeepPlanning", panel, 354f, 548f, 170f, 44f);
        Image keepBg;
        r.closeReview = AddButton(keepRt, new Color(0.169f, 0.208f, 0.278f, 1f), true, out keepBg);
        RectTransform keepLabelRt = Stretch("Label", keepRt, 0f, 0f, 0f, 0f);
        AddText(keepLabelRt, "Keep planning", 10.5f, CreamText, TextAlignmentOptions.Center, null, false);

        RectTransform startRt = TopLeft("StartAnyway", panel, 536f, 548f, 190f, 44f);
        Image startBg;
        r.startAnyway = AddButton(startRt, GoldButtonIdle, true, out startBg);
        RectTransform startLabelRt = Stretch("Label", startRt, 0f, 0f, 0f, 0f);
        AddText(startLabelRt, "Start anyway", 10.5f, new Color(0.212f, 0.161f, 0.078f, 1f),
            TextAlignmentOptions.Center, null, false);

        modal.gameObject.SetActive(false);
    }

    static void BuildToast(RectTransform plannerRoot, UiRefs r)
    {
        RectTransform toast = MakeRect("Toast", plannerRoot);
        toast.anchorMin = new Vector2(0.5f, 0f);
        toast.anchorMax = new Vector2(0.5f, 0f);
        toast.pivot = new Vector2(0.5f, 0f);
        toast.anchoredPosition = new Vector2(0f, 26f);
        toast.sizeDelta = new Vector2(600f, 44f);

        AddImage(toast, new Color(0.082f, 0.125f, 0.200f, 1f), true, false);

        // Bayangan tipis supaya toast tetap terbaca di atas halaman buku.
        Outline toastShadow = toast.gameObject.AddComponent<Outline>();
        toastShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        toastShadow.effectDistance = new Vector2(0f, -3f);

        // Canvas terpisah dengan override sorting: toast selalu tampil paling atas.
        Canvas toastCanvas = toast.gameObject.AddComponent<Canvas>();
        toastCanvas.overrideSorting = true;
        toastCanvas.sortingOrder = 90;

        r.toastGroup = toast.gameObject.AddComponent<CanvasGroup>();

        RectTransform textRt = Stretch("Text", toast, 16f, 0f, 16f, 0f);
        r.toastText = AddText(textRt, "", 10.2f, CreamText, TextAlignmentOptions.Center, null, false);

        r.toastRoot = toast.gameObject;
        toast.gameObject.SetActive(false);
    }

    #endregion

    #region Wiring

    static void ApplyWiring(UiRefs r)
    {
        SerializedObject so = new SerializedObject(r.planner);

        SetProperty(so, "weekPage", r.weekPage);
        SetProperty(so, "ledgerPage", r.ledgerPage);
        SetProperty(so, "marketPage", r.marketPage);
        SetProperty(so, "townPage", r.townPage);
        SetProperty(so, "journalPage", r.journalPage);
        SetProperty(so, "ledgerBodyText", r.ledgerBody);
        SetProperty(so, "marketBodyText", r.marketBody);
        SetProperty(so, "townBodyText", r.townBody);
        SetProperty(so, "journalBodyText", r.journalBody);

        SetProperty(so, "weekLabelText", r.weekLabel);
        SetProperty(so, "goldText", r.gold);
        SetProperty(so, "debtText", r.debt);
        SetProperty(so, "nextPaymentText", r.nextPayment);
        SetProperty(so, "nextPaymentSubText", r.nextPaymentSub);
        SetProperty(so, "livingCostText", r.livingCost);
        SetProperty(so, "livingCostSubText", r.livingCostSub);
        SetProperty(so, "goldAfterText", r.goldAfter);
        SetProperty(so, "goldAfterSubText", r.goldAfterSub);
        SetProperty(so, "energyText", r.energy);
        SetProperty(so, "energyFill", r.energyFill);
        SetProperty(so, "warningChipText", r.warningChip);
        SetProperty(so, "warningChipBackground", r.warningChipBackground);
        SetProperty(so, "openReviewButton", r.reviewButton);

        SetProperty(so, "categoriesView", r.categoriesView);
        SetProperty(so, "cardsView", r.cardsView);
        SetProperty(so, "categoryTitleText", r.categoryTitle);
        SetProperty(so, "deckHintText", r.deckHint);
        SetProperty(so, "selectedSlotText", r.selectedSlotText);
        SetProperty(so, "backToCategoriesButton", r.backButton);
        SetProperty(so, "cardsContainer", r.cardsContainer);
        SetProperty(so, "cardPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath));

        SetProperty(so, "reviewModal", r.reviewModal);
        SetProperty(so, "reviewSummaryText", r.reviewSummary);
        SetProperty(so, "reviewWarningsText", r.reviewWarnings);
        SetProperty(so, "startAnywayButton", r.startAnyway);
        SetProperty(so, "closeReviewButton", r.closeReview);

        SetProperty(so, "toastRoot", r.toastRoot);
        SetProperty(so, "toastText", r.toastText);
        SetProperty(so, "toastCanvasGroup", r.toastGroup);

        // Day views
        SerializedProperty days = so.FindProperty("dayViews");
        if (days != null)
        {
            days.arraySize = r.days.Count;
            for (int i = 0; i < r.days.Count; i++)
            {
                SerializedProperty element = days.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("dayIndex").intValue = r.days[i].index;
                element.FindPropertyRelative("slotsText").objectReferenceValue = r.days[i].slots;
                element.FindPropertyRelative("badgeRoot").objectReferenceValue = r.days[i].badgeRoot;
                element.FindPropertyRelative("badgeText").objectReferenceValue = r.days[i].badge;
            }
        }

        // Category views
        SerializedProperty cats = so.FindProperty("categoryViews");
        if (cats != null)
        {
            cats.arraySize = r.categories.Count;
            for (int i = 0; i < r.categories.Count; i++)
            {
                SerializedProperty element = cats.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("categoryName").stringValue = r.categories[i].name;
                element.FindPropertyRelative("button").objectReferenceValue = r.categories[i].button;
                element.FindPropertyRelative("countText").objectReferenceValue = r.categories[i].count;
            }
        }

        // Tabs
        SerializedProperty tabs = so.FindProperty("bookTabs");
        if (tabs != null)
        {
            tabs.arraySize = r.tabs.Count;
            for (int i = 0; i < r.tabs.Count; i++)
            {
                SerializedProperty element = tabs.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("tab").intValue = (int)r.tabs[i].tab;
                element.FindPropertyRelative("button").objectReferenceValue = r.tabs[i].button;
                element.FindPropertyRelative("background").objectReferenceValue = r.tabs[i].background;
                element.FindPropertyRelative("title").objectReferenceValue = r.tabs[i].title;
                element.FindPropertyRelative("activeColor").colorValue = r.tabs[i].active;
                element.FindPropertyRelative("idleColor").colorValue = r.tabs[i].idle;
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetProperty(SerializedObject so, string name, UnityEngine.Object value)
    {
        SerializedProperty property = so.FindProperty(name);
        if (property == null)
        {
            Debug.LogError("[PlannerBookBuilder] Field tidak ditemukan: " + name);
            return;
        }
        property.objectReferenceValue = value;
    }

    static void SetProperty(SerializedObject so, string name, Color value)
    {
        SerializedProperty property = so.FindProperty(name);
        if (property == null)
        {
            Debug.LogError("[PlannerBookBuilder] Field tidak ditemukan: " + name);
            return;
        }
        property.colorValue = value;
    }

    #endregion
}
#endif
