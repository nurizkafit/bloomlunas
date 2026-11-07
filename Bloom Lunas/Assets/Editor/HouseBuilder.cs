using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Membuat ulang scene House: interior sederhana dengan kasur, dapur,
/// meja shop, pintu keluar, kamera, dan sistem interaksi.
/// </summary>
public static class HouseBuilder
{
    private const string ScenePath = "Assets/Scenes/House.unity";
    private const string CharacterPrefabPath = "Assets/Character_Player.prefab";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string MaterialFolder = "Assets/Materials/House";

    [MenuItem("Bloom Lunas/Rebuild House Scene")]
    public static void RebuildHouse()
    {
        try
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "House");
            }

            GameObject characterPrefab = EnsureCharacterPrefab();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ConfigureLighting();
            BuildRoom();
            BuildBed();
            BuildKitchen();
            BuildDoor();
            BuildDecor();
            BuildPlayerAndCamera(characterPrefab);
            BuildSystems();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();

            Debug.Log("[HouseBuilder] House scene rebuilt at " + ScenePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[HouseBuilder] Failed: " + e);
            throw;
        }
    }

    private static void ConfigureLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.38f, 0.35f, 0.30f, 1f);
        RenderSettings.ambientIntensity = 1f;

        GameObject sunObj = new GameObject("Directional Light", typeof(Light));
        Light sun = sunObj.GetComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.93f, 0.82f);
        sun.intensity = 0.8f;
        sunObj.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

        GameObject lampObj = new GameObject("Lamp Light", typeof(Light));
        Light lamp = lampObj.GetComponent<Light>();
        lamp.type = LightType.Point;
        lamp.color = new Color(1f, 0.82f, 0.58f);
        lamp.intensity = 1.6f;
        lamp.range = 7f;
        lampObj.transform.position = new Vector3(0f, 2.6f, 0.5f);
    }

    private static void BuildRoom()
    {
        Material wood = MakeMaterial("Wood", new Color(0.52f, 0.36f, 0.23f), 0.15f);
        Material woodDark = MakeMaterial("WoodDark", new Color(0.33f, 0.22f, 0.15f), 0.1f);
        Material plaster = MakeMaterial("Plaster", new Color(0.86f, 0.80f, 0.70f), 0.05f);
        Material plasterWarm = MakeMaterial("PlasterWarm", new Color(0.78f, 0.69f, 0.57f), 0.05f);

        Cube("Floor", new Vector3(0f, -0.1f, 0f), new Vector3(9f, 0.2f, 7f), wood);
        Cube("BackWall", new Vector3(0f, 1.6f, 3.3f), new Vector3(9f, 3.2f, 0.2f), plaster);
        Cube("LeftWall", new Vector3(-4.5f, 1.6f, 0f), new Vector3(0.2f, 3.2f, 6.6f), plasterWarm);
        Cube("RightWall", new Vector3(4.5f, 1.6f, 0f), new Vector3(0.2f, 3.2f, 6.6f), plasterWarm);

        // Rug
        Cube("Rug", new Vector3(0f, 0.02f, 0.4f), new Vector3(4.8f, 0.04f, 3.2f),
            MakeMaterial("Rug", new Color(0.45f, 0.52f, 0.40f), 0f));

        // Window with a bright panel
        Cube("Window", new Vector3(0f, 1.9f, 3.18f), new Vector3(2.4f, 1.5f, 0.08f),
            MakeMaterial("Window", new Color(0.72f, 0.82f, 0.86f), 0.6f));
        Cube("WindowFrameTop", new Vector3(0f, 2.68f, 3.16f), new Vector3(2.6f, 0.1f, 0.12f), woodDark);
        Cube("WindowFrameBottom", new Vector3(0f, 1.12f, 3.16f), new Vector3(2.6f, 0.1f, 0.12f), woodDark);
        Cube("WindowFrameLeft", new Vector3(-1.26f, 1.9f, 3.16f), new Vector3(0.1f, 1.6f, 0.12f), woodDark);
        Cube("WindowFrameRight", new Vector3(1.26f, 1.9f, 3.16f), new Vector3(0.1f, 1.6f, 0.12f), woodDark);
    }

    private static void BuildBed()
    {
        Material woodDark = MakeMaterial("WoodDark", new Color(0.33f, 0.22f, 0.15f), 0.1f);
        Material mattress = MakeMaterial("Mattress", new Color(0.94f, 0.90f, 0.80f), 0f);
        Material blanket = MakeMaterial("Blanket", new Color(0.48f, 0.55f, 0.43f), 0f);
        Material pillow = MakeMaterial("Pillow", new Color(0.97f, 0.95f, 0.90f), 0f);

        GameObject root = new GameObject("Bed");
        root.transform.position = new Vector3(-2.3f, 0f, 1.25f);

        CubeChild(root.transform, "Frame", new Vector3(0f, 0.22f, 0f), new Vector3(2.2f, 0.44f, 1.4f), woodDark);
        CubeChild(root.transform, "Mattress", new Vector3(0f, 0.52f, 0f), new Vector3(2.05f, 0.24f, 1.25f), mattress);
        CubeChild(root.transform, "Pillow", new Vector3(-0.62f, 0.70f, 0.10f), new Vector3(0.7f, 0.16f, 0.5f), pillow);
        CubeChild(root.transform, "Blanket", new Vector3(0.28f, 0.66f, 0f), new Vector3(1.35f, 0.08f, 1.2f), blanket);
        CubeChild(root.transform, "Headboard", new Vector3(-1.08f, 0.78f, 0f), new Vector3(0.12f, 1.1f, 1.4f), woodDark);

        // Bantal kecil tambahan
        CubeChild(root.transform, "Pillow2", new Vector3(-0.55f, 0.72f, -0.10f), new Vector3(0.62f, 0.12f, 0.42f), pillow);

        HouseInteractable marker = root.AddComponent<HouseInteractable>();
        marker.SetKind(HouseInteractable.Kind.Bed);
        AddLabel("SLEEP", new Vector3(-2.3f, 1.55f, 1.2f));
    }

    private static void BuildKitchen()
    {
        Material wood = MakeMaterial("Wood", new Color(0.52f, 0.36f, 0.23f), 0.15f);
        Material woodDark = MakeMaterial("WoodDark", new Color(0.33f, 0.22f, 0.15f), 0.1f);
        Material stone = MakeMaterial("Stone", new Color(0.72f, 0.70f, 0.66f), 0.25f);
        Material metal = MakeMaterial("Metal", new Color(0.24f, 0.24f, 0.24f), 0.7f, 0.6f);
        Material clay = MakeMaterial("Clay", new Color(0.66f, 0.34f, 0.24f), 0.05f);

        GameObject root = new GameObject("Kitchen");
        root.transform.position = new Vector3(2.1f, 0f, 2.55f);

        CubeChild(root.transform, "Counter", new Vector3(0f, 0.46f, 0f), new Vector3(2.6f, 0.92f, 0.8f), wood);
        CubeChild(root.transform, "CounterTop", new Vector3(0f, 0.96f, 0f), new Vector3(2.75f, 0.08f, 0.92f), stone);
        CubeChild(root.transform, "Stove", new Vector3(-0.7f, 1.02f, 0f), new Vector3(0.55f, 0.06f, 0.55f), metal);

        CylinderChild(root.transform, "Pot", new Vector3(-0.7f, 1.13f, 0f), new Vector3(0.19f, 0.11f, 0.19f), clay);
        CylinderChild(root.transform, "Lid", new Vector3(-0.7f, 1.25f, 0f), new Vector3(0.17f, 0.015f, 0.17f), metal);

        CubeChild(root.transform, "Shelf", new Vector3(0.6f, 1.55f, 0.1f), new Vector3(1.1f, 0.08f, 0.36f), woodDark);
        CylinderChild(root.transform, "Jar1", new Vector3(0.35f, 1.72f, 0.1f), new Vector3(0.11f, 0.13f, 0.11f), clay);
        CylinderChild(root.transform, "Jar2", new Vector3(0.72f, 1.70f, 0.1f), new Vector3(0.09f, 0.11f, 0.09f), clay);
        CylinderChild(root.transform, "Jar3", new Vector3(1.02f, 1.68f, 0.1f), new Vector3(0.08f, 0.09f, 0.08f), clay);

        HouseInteractable marker = root.AddComponent<HouseInteractable>();
        marker.SetKind(HouseInteractable.Kind.Kitchen);
        AddLabel("COOK", new Vector3(2.1f, 2.35f, 2.4f));
    }

    private static void BuildDoor()
    {
        Material woodDark = MakeMaterial("WoodDark", new Color(0.33f, 0.22f, 0.15f), 0.1f);
        Material bronze = MakeMaterial("Bronze", new Color(0.69f, 0.52f, 0.28f), 0.4f, 0.5f);

        GameObject root = new GameObject("Door");
        root.transform.position = new Vector3(-4.38f, 0f, -1.5f);

        CubeChild(root.transform, "DoorPanel", new Vector3(0f, 1.15f, 0f), new Vector3(0.10f, 2.3f, 1.3f), woodDark);
        CubeChild(root.transform, "FrameTop", new Vector3(0.04f, 2.35f, 0f), new Vector3(0.14f, 0.12f, 1.55f), woodDark);
        CylinderChild(root.transform, "Handle", new Vector3(0.08f, 1.05f, 0.45f), new Vector3(0.05f, 0.04f, 0.05f), bronze);

        HouseInteractable marker = root.AddComponent<HouseInteractable>();
        marker.SetKind(HouseInteractable.Kind.Door);
        AddLabel("LEAVE", new Vector3(-4.1f, 2.6f, -1.5f));
    }

    private static void BuildDecor()
    {
        Material wood = MakeMaterial("Wood", new Color(0.52f, 0.36f, 0.23f), 0.15f);
        Material leaf = MakeMaterial("Leaf", new Color(0.30f, 0.44f, 0.26f), 0f);
        Material clay = MakeMaterial("Clay", new Color(0.66f, 0.34f, 0.24f), 0.05f);

        // Tanaman di sudut
        GameObject plant = new GameObject("Plant");
        plant.transform.position = new Vector3(-3.6f, 0f, 2.4f);
        CylinderChild(plant.transform, "Pot", new Vector3(0f, 0.22f, 0f), new Vector3(0.28f, 0.24f, 0.28f), clay);
        CylinderChild(plant.transform, "Stem", new Vector3(0f, 0.6f, 0f), new Vector3(0.05f, 0.5f, 0.05f), wood);
        SphereChild(plant.transform, "Leaves", new Vector3(0f, 0.95f, 0f), new Vector3(0.55f, 0.45f, 0.55f), leaf);

        // Rak kecil di dinding kanan
        CubeChild(null, "WallShelf", new Vector3(4.2f, 1.9f, 1.2f), new Vector3(0.4f, 0.06f, 1.4f), wood);
        CubeChild(null, "ShelfJar", new Vector3(4.2f, 2.05f, 1.0f), new Vector3(0.16f, 0.22f, 0.16f), clay);
        CubeChild(null, "ShelfBook1", new Vector3(4.2f, 2.02f, 1.45f), new Vector3(0.26f, 0.16f, 0.06f), leaf);
    }

    private static void BuildPlayerAndCamera(GameObject characterPrefab)
    {
        Vector3 playerPosition = new Vector3(0f, 0.708f, 0.4f);
        GameObject player = null;

        if (characterPrefab != null)
        {
            player = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab);
            player.transform.position = playerPosition;
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            player.name = "Player";
        }

        GameObject camObj = new GameObject("Main Camera", typeof(Camera));
        camObj.tag = "MainCamera";
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.20f, 0.16f, 0.13f, 1f);
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;

        camObj.transform.position = playerPosition + new Vector3(1.62f, 8.982f, -6.86f);
        camObj.transform.rotation = Quaternion.Euler(51.681f, 0f, 0f);

        IsometricCameraFollow follow = camObj.AddComponent<IsometricCameraFollow>();
        follow.Setup(player != null ? player.transform : null);
    }

    private static GameObject EnsureCharacterPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
        if (existing != null) return existing;

        EditorSceneManager.OpenScene("Assets/Scenes/CityOverWorld.unity");
        CharacterMovement movement = Object.FindFirstObjectByType<CharacterMovement>();
        if (movement == null)
        {
            Debug.LogWarning("[HouseBuilder] CharacterMovement not found in CityOverWorld; House will have no player character.");
            return null;
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(movement.gameObject, CharacterPrefabPath);
        AssetDatabase.SaveAssets();
        Debug.Log("[HouseBuilder] Saved player character prefab.");
        return prefab;
    }

    private static void BuildSystems()
    {
        GameObject systems = new GameObject("HouseSystems", typeof(HouseManager), typeof(HouseUI));
        systems.transform.position = Vector3.zero;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif

        GameObject canvasObj = new GameObject("HouseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static void AddSceneToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == ScenePath)
            {
                scenes[i] = new EditorBuildSettingsScene(ScenePath, true);
                found = true;
                break;
            }
        }

        if (!found) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static Material MakeMaterial(string name, Color color, float smoothness = 0.1f, float metallic = 0f)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }

    private static GameObject CubeChild(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }

    private static GameObject CylinderChild(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }

    private static GameObject SphereChild(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }

    private static void AddLabel(string text, Vector3 position)
    {
        GameObject obj = new GameObject("Label_" + text, typeof(TextMeshPro));
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one * 0.3f;

        TextMeshPro label = obj.GetComponent<TextMeshPro>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        label.text = text;
        label.fontSize = 9f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.24f, 0.18f, 0.11f, 1f);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.rectTransform.sizeDelta = new Vector2(8f, 1.5f);
    }
}
