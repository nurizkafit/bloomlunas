#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Membangun HUD rutinitas harian di CityOverWorld dan memasang
/// komponen DailyScheduleRunner. Jalankan lewat menu Bloom Lunas.
/// </summary>
public static class CityRoutineBuilder
{
    private const string ScenePath = "Assets/Scenes/CityOverWorld.unity";

    private static readonly Color PanelColor = new Color(0.075f, 0.110f, 0.180f, 0.97f);
    private static readonly Color Gold = new Color(0.910f, 0.784f, 0.475f, 1f);
    private static readonly Color Cream = new Color(0.965f, 0.945f, 0.890f, 1f);
    private static readonly Color Muted = new Color(0.612f, 0.647f, 0.710f, 1f);
    private static readonly Color MutedLight = new Color(0.760f, 0.788f, 0.835f, 1f);
    private static readonly Color DarkText = new Color(0.09f, 0.12f, 0.19f, 1f);

    private static Sprite roundedSprite;
    private static Sprite smallSprite;
    private static TMP_FontAsset uiFont;
    private static TMP_FontAsset clockFont;

    [MenuItem("Bloom Lunas/Rebuild City Routine HUD")]
    public static void RebuildCityRoutine()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath);

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[CityRoutineBuilder] Canvas tidak ditemukan di CityOverWorld.");
            return;
        }

        Transform oldHud = canvas.transform.Find("RoutineHUD");
        if (oldHud != null) Object.DestroyImmediate(oldHud.gameObject);

        GameObject oldRunner = GameObject.Find("RoutineSystem");
        if (oldRunner != null) Object.DestroyImmediate(oldRunner);

        roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI_Rounded.png");
        smallSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI_SlotRounded.png");

        // Font Xmind punya atlas SDF tanpa padding sehingga pecah di ukuran kecil.
        // HUD memakai LiberationSans yang tajam, dengan Redaction untuk jam.
        uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        clockFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Font/Redaction/Redaction-Regular SDF.asset");

        CharacterMovement character = Object.FindFirstObjectByType<CharacterMovement>();

        GameObject runnerGo = new GameObject("RoutineSystem");
        DailyScheduleRunner runner = runnerGo.AddComponent<DailyScheduleRunner>();

        // Panel utama HUD (kiri atas)
        RectTransform hud = TopLeft("RoutineHUD", canvas.transform, 24f, 24f, 430f, 226f);
        hud.SetAsLastSibling();
        AddImage(hud, PanelColor, true, false);

        Outline outline = hud.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.78f, 0.635f, 0.376f, 0.35f);
        outline.effectDistance = new Vector2(1f, -1f);

        RectTransform accent = TopBar("Accent", hud, 0f, 0f, 0f, 3f);
        AddImage(accent, Gold, true, false, smallSprite);

        TextMeshProUGUI day = AddText(TopLeft("DayLabel", hud, 20f, 14f, 220f, 18f),
            "MON  -  DAY 1", 11.5f, Gold, TextAlignmentOptions.MidlineLeft);
        day.characterSpacing = 1.1f;

        TextMeshProUGUI clock = AddText(TopRight("Clock", hud, 18f, 8f, 160f, 38f),
            "06:00", 30f, Cream, TextAlignmentOptions.MidlineRight, clockFont);

        AddText(TopLeft("NowLabel", hud, 20f, 56f, 60f, 12f),
            "NOW", 9f, Muted, TextAlignmentOptions.MidlineLeft);

        TextMeshProUGUI activity = AddText(TopLeft("ActivityLabel", hud, 20f, 70f, 390f, 28f),
            "No activities planned", 20f, Cream, TextAlignmentOptions.MidlineLeft);

        TextMeshProUGUI location = AddText(TopLeft("LocationLabel", hud, 20f, 100f, 390f, 18f),
            "", 12.5f, MutedLight, TextAlignmentOptions.MidlineLeft);

        RectTransform barBg = TopLeft("ProgressBg", hud, 20f, 126f, 390f, 6f);
        AddImage(barBg, new Color(0f, 0f, 0f, 0.35f), true, false, smallSprite);

        RectTransform fillRt = Stretch("ProgressFill", barBg, 0f, 0f, 0f, 0f);
        Image fill = AddImage(fillRt, Gold, true, false, smallSprite);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;

        TextMeshProUGUI next = AddText(TopLeft("NextLabel", hud, 20f, 142f, 390f, 18f),
            "", 10.5f, MutedLight, TextAlignmentOptions.MidlineLeft);

        Button pause = MakeButton("PauseButton", hud, "Pause", 20f, 178f, 100f, 32f,
            new Color(0.12f, 0.16f, 0.23f, 1f), Cream);
        Button planner = MakeButton("PlannerButton", hud, "Planner", 130f, 178f, 110f, 32f,
            Gold, DarkText);
        Button nextDay = MakeButton("NextDayButton", hud, "Next day", 250f, 178f, 130f, 32f,
            Gold, DarkText);
        nextDay.gameObject.SetActive(false);

        UnityEventTools.AddVoidPersistentListener(pause.onClick, runner.TogglePause);
        UnityEventTools.AddVoidPersistentListener(planner.onClick, runner.OpenPlanner);
        UnityEventTools.AddVoidPersistentListener(nextDay.onClick, runner.NextDay);

        TextMeshProUGUI pauseLabel = pause.GetComponentInChildren<TextMeshProUGUI>(true);

        SerializedObject so = new SerializedObject(runner);
        so.FindProperty("character").objectReferenceValue = character;
        so.FindProperty("dayLabel").objectReferenceValue = day;
        so.FindProperty("clockLabel").objectReferenceValue = clock;
        so.FindProperty("activityLabel").objectReferenceValue = activity;
        so.FindProperty("locationLabel").objectReferenceValue = location;
        so.FindProperty("nextLabel").objectReferenceValue = next;
        so.FindProperty("pauseLabel").objectReferenceValue = pauseLabel;
        so.FindProperty("progressFill").objectReferenceValue = fill;
        so.FindProperty("pauseButton").objectReferenceValue = pause.gameObject;
        so.FindProperty("nextDayButton").objectReferenceValue = nextDay.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (character == null)
        {
            Debug.LogWarning("[CityRoutineBuilder] CharacterMovement tidak ditemukan; isi manual di Inspector.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[CityRoutineBuilder] Selesai: HUD dan DailyScheduleRunner ditambahkan ke CityOverWorld.");
    }

    #region Helpers

    private static RectTransform MakeRect(string name, Transform parent)
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

    private static RectTransform Stretch(string name, Transform parent, float left, float bottom, float right, float top)
    {
        RectTransform rt = MakeRect(name, parent);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }

    private static RectTransform TopLeft(string name, Transform parent, float x, float y, float width, float height)
    {
        RectTransform rt = MakeRect(name, parent);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(width, height);
        return rt;
    }

    private static RectTransform TopRight(string name, Transform parent, float x, float y, float width, float height)
    {
        RectTransform rt = MakeRect(name, parent);
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-x, -y);
        rt.sizeDelta = new Vector2(width, height);
        return rt;
    }

    private static RectTransform TopBar(string name, Transform parent, float left, float right, float top, float height)
    {
        RectTransform rt = MakeRect(name, parent);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(left, -top - height);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }

    private static Image AddImage(RectTransform rt, Color color, bool rounded, bool raycast, Sprite sprite = null)
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

    private static TextMeshProUGUI AddText(RectTransform rt, string content, float size, Color color,
        TextAlignmentOptions align, TMP_FontAsset font = null)
    {
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset useFont = font != null ? font : uiFont;

        if (useFont != null)
        {
            tmp.font = useFont;
            tmp.fontSharedMaterial = useFont.material;
        }

        tmp.text = content;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.richText = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    private static Button MakeButton(string name, Transform parent, string label, float x, float y,
        float width, float height, Color background, Color textColor)
    {
        RectTransform rt = TopLeft(name, parent, x, y, width, height);
        Image img = AddImage(rt, background, true, true);

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
        colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        RectTransform labelRt = Stretch("Label", rt, 6f, 0f, 6f, 0f);
        AddText(labelRt, label, 11.5f, textColor, TextAlignmentOptions.Center);

        return button;
    }

    #endregion
}
#endif
