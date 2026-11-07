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
/// Membuat scene lokasi dengan kamera isometrik seperti CityOverWorld,
/// karakter pemain yang terlihat, dan interaksi sesuai jenis lokasi.
/// </summary>
public static class LocationSceneBuilder
{
    private const string CharacterPrefabPath = "Assets/Character_Player.prefab";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string MaterialFolder = "Assets/Materials/Locations";

    [MenuItem("Bloom Lunas/Rebuild Location Scenes")]
    public static void RebuildAll()
    {
        EnsureFolders();
        GameObject characterPrefab = EnsureCharacterPrefab();

        BuildMarket(characterPrefab);
        BuildGathering(characterPrefab);
        BuildHomeDrying(characterPrefab);
        BuildMerchantGuild(characterPrefab);

        AssetDatabase.SaveAssets();
        Debug.Log("[LocationSceneBuilder] Market, GatheringArea, HomeDrying, MerchantGuild rebuilt.");
    }

    #region Scenes

    private static void BuildMarket(GameObject characterPrefab)
    {
        Scene scene = BeginScene(LocationMode.Market);
        Material grass = Mat("Grass", new Color(0.36f, 0.45f, 0.28f));
        Material dirt = Mat("Dirt", new Color(0.56f, 0.44f, 0.30f));
        Material wood = Mat("Wood", new Color(0.52f, 0.36f, 0.23f));
        Material woodDark = Mat("WoodDark", new Color(0.33f, 0.22f, 0.15f));
        Material cloth = Mat("ClothRed", new Color(0.62f, 0.25f, 0.20f));
        Material clothCream = Mat("ClothCream", new Color(0.86f, 0.79f, 0.62f));
        Material clay = Mat("Clay", new Color(0.66f, 0.34f, 0.24f));
        Material gold = Mat("Gold", new Color(0.83f, 0.66f, 0.32f), 0.35f, 0.3f);

        BuildGround(grass, dirt);
        AddTree(new Vector3(-6f, 0f, 3.5f), 1.1f);
        AddTree(new Vector3(6f, 0f, 3.8f), 1.0f);

        // Warung utama
        GameObject stall = Group("MarketStall", new Vector3(0f, 0f, 1.6f));
        CubeChild(stall.transform, "Counter", new Vector3(0f, 0.45f, 0f), new Vector3(3.4f, 0.9f, 1.0f), wood);
        CubeChild(stall.transform, "CounterTop", new Vector3(0f, 0.94f, 0f), new Vector3(3.55f, 0.08f, 1.12f), woodDark);
        for (int i = 0; i < 4; i++)
        {
            float x = i % 2 == 0 ? -1.55f : 1.55f;
            float z = i < 2 ? -0.45f : 0.45f;
            CubeChild(stall.transform, "Post", new Vector3(x, 1.55f, z), new Vector3(0.10f, 3.1f, 0.10f), woodDark);
        }
        CubeChild(stall.transform, "Canopy", new Vector3(0f, 3.12f, 0f), new Vector3(3.9f, 0.12f, 2.2f), cloth);
        CubeChild(stall.transform, "CanopyTrim", new Vector3(0f, 3.20f, 0f), new Vector3(4.0f, 0.06f, 2.3f), clothCream);

        // Barang dagangan
        for (int i = 0; i < 6; i++)
        {
            float x = -1.1f + i * 0.44f;
            Cyl(stall.transform, "Bottle" + i, new Vector3(x, 1.12f, 0.12f), new Vector3(0.10f, 0.16f, 0.10f), clay);
            Sph(stall.transform, "Fruit" + i, new Vector3(x, 1.04f, -0.22f), new Vector3(0.16f, 0.16f, 0.16f), gold);
        }
        CubeChild(stall.transform, "CoinBox", new Vector3(1.25f, 1.00f, 0.20f), new Vector3(0.35f, 0.12f, 0.28f), gold);
        AddInteractable(stall, LocationInteractable.Kind.MarketStall);
        AddLabel("MARKET", new Vector3(0f, 3.7f, 1.6f));

        // Warung samping
        AddSideStall(new Vector3(-4.4f, 0f, 2.6f), clothCream, wood, woodDark);
        AddSideStall(new Vector3(4.4f, 0f, 2.6f), cloth, wood, woodDark);

        // Peti dan karung
        CubeChild(null, "Crate1", new Vector3(-3.0f, 0.28f, 0.8f), new Vector3(0.9f, 0.56f, 0.9f), wood);
        CubeChild(null, "Crate2", new Vector3(-3.0f, 0.84f, 0.8f), new Vector3(0.75f, 0.5f, 0.75f), woodDark);
        Cyl(null, "Sack1", new Vector3(3.2f, 0.3f, 1.2f), new Vector3(0.35f, 0.32f, 0.35f), clothCream);
        Cyl(null, "Sack2", new Vector3(3.7f, 0.28f, 0.7f), new Vector3(0.32f, 0.3f, 0.32f), clothCream);

        AddExit(new Vector3(-6f, 0f, -3f));
        FinishScene(scene, characterPrefab, "Market");
    }

    private static void BuildGathering(GameObject characterPrefab)
    {
        Scene scene = BeginScene(LocationMode.Gathering);
        Material grass = Mat("Grass", new Color(0.33f, 0.46f, 0.27f));
        Material dirt = Mat("Dirt", new Color(0.52f, 0.41f, 0.28f));
        Material leaf = Mat("Leaf", new Color(0.24f, 0.40f, 0.22f));
        Material leafLight = Mat("LeafLight", new Color(0.38f, 0.55f, 0.30f));
        Material wood = Mat("Wood", new Color(0.42f, 0.29f, 0.18f));
        Material turmeric = Mat("Turmeric", new Color(0.86f, 0.52f, 0.16f));
        Material ginger = Mat("Ginger", new Color(0.76f, 0.64f, 0.36f));
        Material kencur = Mat("Kencur", new Color(0.92f, 0.88f, 0.72f));
        Material lemongrass = Mat("Lemongrass", new Color(0.55f, 0.70f, 0.32f));
        Material tamarind = Mat("Tamarind", new Color(0.45f, 0.28f, 0.18f));
        Material palm = Mat("Palm", new Color(0.30f, 0.48f, 0.25f));

        BuildGround(grass, dirt);
        AddTree(new Vector3(-6.5f, 0f, 3.8f), 1.3f);
        AddTree(new Vector3(6.3f, 0f, 3.2f), 1.2f);
        AddTree(new Vector3(6.6f, 0f, -2.2f), 1.0f);
        AddBush(new Vector3(-5.8f, 0f, 0.8f), leaf, 0.8f);
        AddBush(new Vector3(5.6f, 0f, 1.6f), leafLight, 0.7f);

        AddGatherNode("Turmeric", new Vector3(-4.2f, 0f, 2.0f), turmeric, leaf, 2);
        AddGatherNode("Ginger", new Vector3(-2.0f, 0f, 3.3f), ginger, leafLight, 2);
        AddGatherNode("Kencur", new Vector3(0.4f, 0f, 2.2f), kencur, leaf, 1);
        AddGatherNode("Lemongrass", new Vector3(2.7f, 0f, 3.1f), lemongrass, leafLight, 2);
        AddGatherNode("Tamarind", new Vector3(4.6f, 0f, 1.8f), tamarind, leaf, 1);
        AddGatherNode("Palm sugar", new Vector3(-4.6f, 0f, -1.6f), palm, wood, 1);

        AddExit(new Vector3(5.8f, 0f, -3.0f));
        FinishScene(scene, characterPrefab, "GatheringArea");
    }

    private static void BuildHomeDrying(GameObject characterPrefab)
    {
        Scene scene = BeginScene(LocationMode.Drying);
        Material grass = Mat("Grass", new Color(0.38f, 0.45f, 0.28f));
        Material dirt = Mat("Dirt", new Color(0.58f, 0.45f, 0.31f));
        Material wood = Mat("Wood", new Color(0.52f, 0.36f, 0.23f));
        Material woodDark = Mat("WoodDark", new Color(0.33f, 0.22f, 0.15f));
        Material leaf = Mat("Leaf", new Color(0.34f, 0.47f, 0.26f));
        Material herb = Mat("Herb", new Color(0.55f, 0.58f, 0.30f));
        Material clay = Mat("Clay", new Color(0.66f, 0.34f, 0.24f));

        BuildGround(grass, dirt);
        AddTree(new Vector3(-6f, 0f, 3.5f), 1.1f);
        AddBush(new Vector3(6f, 0f, 2.8f), leaf, 0.9f);

        // Gudang kecil
        CubeChild(null, "ShedFloor", new Vector3(-4.6f, 0.05f, 2.4f), new Vector3(3.2f, 0.1f, 2.4f), woodDark);
        CubeChild(null, "ShedBack", new Vector3(-4.6f, 1.3f, 3.6f), new Vector3(3.2f, 2.5f, 0.18f), wood);
        CubeChild(null, "ShedLeft", new Vector3(-6.2f, 1.3f, 2.4f), new Vector3(0.18f, 2.5f, 2.4f), wood);
        CubeChild(null, "ShedRoof", new Vector3(-4.6f, 2.6f, 2.4f), new Vector3(3.6f, 0.12f, 2.8f), woodDark);

        // Rak pengering
        AddRack(new Vector3(-2.6f, 0f, 1.4f), wood, woodDark, herb);
        AddRack(new Vector3(0.4f, 0f, 1.4f), wood, woodDark, herb);
        AddRack(new Vector3(3.4f, 0f, 1.4f), wood, woodDark, herb);

        // Meja dan botol
        CubeChild(null, "Table", new Vector3(2.3f, 0.45f, -0.8f), new Vector3(2.2f, 0.9f, 1.0f), wood);
        for (int i = 0; i < 4; i++)
        {
            Cyl(null, "Jar" + i, new Vector3(1.5f + i * 0.5f, 1.02f, -0.8f), new Vector3(0.12f, 0.16f, 0.12f), clay);
        }

        AddExit(new Vector3(-6.2f, 0f, -2.8f));
        FinishScene(scene, characterPrefab, "HomeDrying");
    }

    private static void BuildMerchantGuild(GameObject characterPrefab)
    {
        Scene scene = BeginScene(LocationMode.Guild);
        Material stone = Mat("Stone", new Color(0.55f, 0.52f, 0.46f));
        Material wood = Mat("Wood", new Color(0.44f, 0.30f, 0.19f));
        Material woodDark = Mat("WoodDark", new Color(0.28f, 0.19f, 0.13f));
        Material clothRed = Mat("ClothRed", new Color(0.58f, 0.22f, 0.18f));
        Material gold = Mat("Gold", new Color(0.82f, 0.65f, 0.30f), 0.3f, 0.3f);
        Material paper = Mat("Paper", new Color(0.91f, 0.87f, 0.75f));

        CubeChild(null, "GuildFloor", new Vector3(0f, 0.05f, 0.5f), new Vector3(16f, 0.1f, 12f), stone);
        CubeChild(null, "GuildBackWall", new Vector3(0f, 1.8f, 6.2f), new Vector3(16f, 3.6f, 0.3f), woodDark);
        CubeChild(null, "GuildLeftWall", new Vector3(-8f, 1.8f, 0.5f), new Vector3(0.3f, 3.6f, 12f), woodDark);
        CubeChild(null, "GuildRightWall", new Vector3(8f, 1.8f, 0.5f), new Vector3(0.3f, 3.6f, 12f), woodDark);

        // Meja resepsionis
        GameObject desk = Group("GuildDesk", new Vector3(0f, 0f, 2.2f));
        CubeChild(desk.transform, "Desk", new Vector3(0f, 0.5f, 0f), new Vector3(5.0f, 1.0f, 1.1f), wood);
        CubeChild(desk.transform, "DeskTop", new Vector3(0f, 1.04f, 0f), new Vector3(5.2f, 0.08f, 1.25f), woodDark);
        CubeChild(desk.transform, "Ledger", new Vector3(-1.0f, 1.12f, 0.1f), new Vector3(0.8f, 0.08f, 0.55f), paper);
        CubeChild(desk.transform, "Inkwell", new Vector3(1.2f, 1.12f, 0.1f), new Vector3(0.16f, 0.18f, 0.16f), gold);
        AddInteractable(desk, LocationInteractable.Kind.GuildDesk);
        AddLabel("GUILD", new Vector3(0f, 2.0f, 2.2f));

        // Papan pengumuman
        CubeChild(null, "NoticeBoard", new Vector3(0f, 2.3f, 6.0f), new Vector3(3.2f, 1.8f, 0.12f), wood);
        for (int i = 0; i < 4; i++)
        {
            CubeChild(null, "Notice" + i, new Vector3(-1.0f + i * 0.65f, 2.3f, 5.9f), new Vector3(0.5f, 0.7f, 0.03f), paper);
        }

        // Bendera dan meja
        CubeChild(null, "BannerLeft", new Vector3(-7.6f, 2.2f, 2.8f), new Vector3(0.08f, 2.6f, 1.4f), clothRed);
        CubeChild(null, "BannerRight", new Vector3(7.6f, 2.2f, 2.8f), new Vector3(0.08f, 2.6f, 1.4f), clothRed);

        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -3.8f : 3.8f;
            CubeChild(null, "Table" + i, new Vector3(x, 0.45f, -1.2f), new Vector3(2.0f, 0.9f, 1.4f), wood);
            CubeChild(null, "Chair" + i, new Vector3(x, 0.45f, -2.6f), new Vector3(0.7f, 0.9f, 0.7f), woodDark);
        }

        AddExit(new Vector3(-5.5f, 0f, -3.0f));
        AddGuildLamp(new Vector3(0f, 3.2f, 1.2f));
        FinishScene(scene, characterPrefab, "MerchantGuild");
    }

    #endregion

    #region Shared scene parts

    private static Scene BeginScene(LocationMode mode)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.60f, 0.58f, 0.50f, 1f);

        GameObject sunObj = new GameObject("Directional Light", typeof(Light));
        Light sun = sunObj.GetComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.92f, 0.78f);
        sun.intensity = 1.2f;
        sunObj.transform.rotation = Quaternion.Euler(52f, -28f, 0f);

        return scene;
    }

    private static void FinishScene(Scene scene, GameObject characterPrefab, string sceneName)
    {
        BuildPlayerAndCamera(characterPrefab);
        BuildSystems(sceneName);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/" + sceneName + ".unity");
        AddSceneToBuildSettings("Assets/Scenes/" + sceneName + ".unity");
    }

    private static void BuildGround(Material grass, Material dirt)
    {
        CubeChild(null, "Ground", new Vector3(0f, -0.1f, 0f), new Vector3(24f, 0.2f, 18f), grass);
        CubeChild(null, "Path", new Vector3(0f, 0.01f, -0.4f), new Vector3(3.2f, 0.03f, 11f), dirt);
        CubeChild(null, "PathCross", new Vector3(0f, 0.02f, 1.2f), new Vector3(10f, 0.03f, 2.4f), dirt);

        // Pagar sederhana di belakang
        for (int i = 0; i < 9; i++)
        {
            CubeChild(null, "FencePost", new Vector3(-8f + i * 2f, 0.55f, 5.6f), new Vector3(0.12f, 1.1f, 0.12f), dirt);
        }
        CubeChild(null, "FenceRail", new Vector3(0f, 0.8f, 5.6f), new Vector3(16.2f, 0.08f, 0.08f), dirt);
    }

    private static void BuildPlayerAndCamera(GameObject characterPrefab)
    {
        Vector3 playerPosition = new Vector3(0f, 0.708f, -1.2f);
        GameObject player = null;

        if (characterPrefab != null)
        {
            player = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab);
            player.transform.position = playerPosition;
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            player.name = "Player";
        }

        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camObj.tag = "MainCamera";
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.42f, 0.55f, 0.66f, 1f);
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 150f;

        camObj.transform.position = playerPosition + new Vector3(1.62f, 8.982f, -6.86f);
        camObj.transform.rotation = Quaternion.Euler(51.681f, 0f, 0f);

        IsometricCameraFollow follow = camObj.AddComponent<IsometricCameraFollow>();
        follow.Setup(player != null ? player.transform : null);
    }

    private static void BuildSystems(string sceneName)
    {
        GameObject canvasObj = new GameObject("LocationCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif

        GameObject systems = new GameObject("SceneSystems", typeof(LocationSceneUI), typeof(LocationSceneManager));
        LocationSceneManager manager = systems.GetComponent<LocationSceneManager>();
        manager.SetMode(SceneMode(sceneName));
    }

    private static LocationMode SceneMode(string sceneName)
    {
        switch (sceneName)
        {
            case "Market": return LocationMode.Market;
            case "GatheringArea": return LocationMode.Gathering;
            case "HomeDrying": return LocationMode.Drying;
            default: return LocationMode.Guild;
        }
    }

    private static GameObject EnsureCharacterPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
        if (existing != null) return existing;

        EditorSceneManager.OpenScene("Assets/Scenes/CityOverWorld.unity");
        CharacterMovement movement = Object.FindFirstObjectByType<CharacterMovement>();
        if (movement == null)
        {
            Debug.LogWarning("[LocationSceneBuilder] CharacterMovement not found in CityOverWorld.");
            return null;
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(movement.gameObject, CharacterPrefabPath);
        AssetDatabase.SaveAssets();
        Debug.Log("[LocationSceneBuilder] Saved player character prefab.");
        return prefab;
    }

    private static void AddSceneToBuildSettings(string path)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == path)
            {
                scenes[i] = new EditorBuildSettingsScene(path, true);
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    #endregion

    #region Props

    private static void AddSideStall(Vector3 position, Material cloth, Material wood, Material woodDark)
    {
        GameObject stall = Group("SideStall", position);
        CubeChild(stall.transform, "Counter", new Vector3(0f, 0.42f, 0f), new Vector3(2.2f, 0.84f, 0.9f), wood);
        CubeChild(stall.transform, "Post1", new Vector3(-1f, 1.3f, -0.4f), new Vector3(0.1f, 2.6f, 0.1f), woodDark);
        CubeChild(stall.transform, "Post2", new Vector3(1f, 1.3f, -0.4f), new Vector3(0.1f, 2.6f, 0.1f), woodDark);
        CubeChild(stall.transform, "Canopy", new Vector3(0f, 2.62f, 0f), new Vector3(2.5f, 0.1f, 1.6f), cloth);
        Cyl(stall.transform, "Pot", new Vector3(-0.4f, 0.92f, 0f), new Vector3(0.14f, 0.12f, 0.14f), woodDark);
        Sph(stall.transform, "Produce", new Vector3(0.3f, 0.92f, 0f), new Vector3(0.25f, 0.2f, 0.25f), cloth);
    }

    private static void AddTree(Vector3 position, float scale)
    {
        Material trunk = Mat("Wood", new Color(0.42f, 0.29f, 0.18f));
        Material leaf = Mat("Leaf", new Color(0.24f, 0.40f, 0.22f));

        GameObject tree = Group("Tree", position);
        Cyl(tree.transform, "Trunk", new Vector3(0f, 1.0f * scale, 0f), new Vector3(0.18f * scale, 1.0f * scale, 0.18f * scale), trunk);
        Sph(tree.transform, "Canopy", new Vector3(0f, 2.2f * scale, 0f), new Vector3(1.1f * scale, 0.9f * scale, 1.1f * scale), leaf);
        Sph(tree.transform, "Canopy2", new Vector3(0.5f * scale, 1.9f * scale, 0.2f), new Vector3(0.7f * scale, 0.6f * scale, 0.7f * scale), leaf);
    }

    private static void AddBush(Vector3 position, Material leaf, float scale)
    {
        GameObject bush = Group("Bush", position);
        Sph(bush.transform, "Leaves", new Vector3(0f, 0.32f * scale, 0f), new Vector3(0.7f * scale, 0.5f * scale, 0.7f * scale), leaf);
        Sph(bush.transform, "Leaves2", new Vector3(0.35f * scale, 0.26f * scale, 0.1f), new Vector3(0.45f * scale, 0.4f * scale, 0.45f * scale), leaf);
    }

    private static void AddGatherNode(string material, Vector3 position, Material accent, Material leaf, int amount)
    {
        GameObject node = Group("Node_" + material, position);
        node.transform.localScale = Vector3.one * 1.3f;
        Sph(node.transform, "Bush", new Vector3(0f, 0.35f, 0f), new Vector3(0.65f, 0.5f, 0.65f), leaf);
        Sph(node.transform, "Bloom1", new Vector3(-0.3f, 0.55f, 0.1f), new Vector3(0.18f, 0.18f, 0.18f), accent);
        Sph(node.transform, "Bloom2", new Vector3(0.25f, 0.6f, -0.1f), new Vector3(0.15f, 0.15f, 0.15f), accent);
        Cyl(node.transform, "Stem", new Vector3(0f, 0.65f, 0f), new Vector3(0.04f, 0.3f, 0.04f), leaf);

        LocationInteractable marker = node.AddComponent<LocationInteractable>();
        marker.SetKind(LocationInteractable.Kind.GatherNode);

        GatherNode gather = node.AddComponent<GatherNode>();
        gather.Configure(material, amount, 22f);

        AddLabel(material.ToUpperInvariant(), position + new Vector3(0f, 1.3f, 0f));
    }

    private static void AddRack(Vector3 position, Material wood, Material woodDark, Material herb)
    {
        GameObject rack = Group("DryingRack", position);
        for (int i = 0; i < 4; i++)
        {
            float x = i % 2 == 0 ? -0.9f : 0.9f;
            float z = i < 2 ? -0.35f : 0.35f;
            CubeChild(rack.transform, "Leg", new Vector3(x, 0.45f, z), new Vector3(0.08f, 0.9f, 0.08f), woodDark);
        }
        CubeChild(rack.transform, "Frame", new Vector3(0f, 0.92f, 0f), new Vector3(2.0f, 0.08f, 0.9f), wood);
        CubeChild(rack.transform, "Tray", new Vector3(0f, 0.55f, 0f), new Vector3(1.85f, 0.06f, 0.8f), wood);
        for (int i = 0; i < 5; i++)
        {
            Sph(rack.transform, "Herb" + i, new Vector3(-0.7f + i * 0.35f, 0.64f, 0f), new Vector3(0.22f, 0.10f, 0.18f), herb);
        }

        LocationInteractable marker = rack.AddComponent<LocationInteractable>();
        marker.SetKind(LocationInteractable.Kind.DryingRack);
        AddLabel("DRYING", position + new Vector3(0f, 1.4f, 0f));
    }

    private static void AddGuildLamp(Vector3 position)
    {
        GameObject lampObj = new GameObject("Guild Lamp", typeof(Light));
        Light lamp = lampObj.GetComponent<Light>();
        lamp.type = LightType.Point;
        lamp.color = new Color(1f, 0.84f, 0.6f);
        lamp.intensity = 1.8f;
        lamp.range = 12f;
        lampObj.transform.position = position;
    }

    private static void AddExit(Vector3 position)
    {
        Material woodDark = Mat("WoodDark", new Color(0.33f, 0.22f, 0.15f));
        Material gold = Mat("Gold", new Color(0.83f, 0.66f, 0.32f), 0.35f, 0.3f);

        GameObject exit = Group("ExitToCity", position);
        Cyl(exit.transform, "Post", new Vector3(0f, 0.75f, 0f), new Vector3(0.08f, 0.75f, 0.08f), woodDark);
        CubeChild(exit.transform, "Sign", new Vector3(0f, 1.55f, 0f), new Vector3(1.4f, 0.5f, 0.08f), gold);

        LocationInteractable marker = exit.AddComponent<LocationInteractable>();
        marker.SetKind(LocationInteractable.Kind.Exit);
        AddLabel("CITY", position + new Vector3(0f, 2.2f, 0f));
    }

    #endregion

    #region Helpers

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Materials", "Locations");
    }

    private static Material Mat(string name, Color color, float smoothness = 0.05f, float metallic = 0f)
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

    private static GameObject Group(string name, Vector3 position)
    {
        GameObject root = new GameObject(name);
        root.transform.position = position;
        return root;
    }

    private static GameObject CubeChild(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        if (parent != null) obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }

    private static GameObject Cyl(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        if (parent != null) obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }

    private static GameObject Sph(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = name;
        if (parent != null) obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }

    private static void AddInteractable(GameObject root, LocationInteractable.Kind kind)
    {
        LocationInteractable marker = root.AddComponent<LocationInteractable>();
        marker.SetKind(kind);
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
        label.color = new Color(0.22f, 0.17f, 0.11f, 1f);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.rectTransform.sizeDelta = new Vector2(8f, 1.5f);
    }

    #endregion
}
