using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum LocationMode
{
    Market,
    Gathering,
    Drying,
    Guild
}

/// <summary>
/// Pengatur interaksi scene lokasi. Satu komponen dipakai untuk Market,
/// Gathering Area, Home Drying, dan Merchant Guild.
/// </summary>
public class LocationSceneManager : MonoBehaviour
{
    [SerializeField] private LocationMode mode = LocationMode.Market;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CharacterMovement character;
    [SerializeField] private LocationSceneUI ui;
    [SerializeField] private float maxRayDistance = 150f;

    private CityMarketUI marketUI;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (ui == null) ui = GetComponent<LocationSceneUI>();
        if (ui == null) ui = FindFirstObjectByType<LocationSceneUI>();
        if (character == null) character = FindFirstObjectByType<CharacterMovement>();

        if (GameManager.Instance == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }
    }

    private void Start()
    {
        if (ui == null) return;
        ui.SetMode(Title(), Hint());
        ui.RefreshStatus();
    }

    public void SetMode(LocationMode value)
    {
        mode = value;
    }

    public void BackToCity()
    {
        SceneManager.LoadScene("CityOverWorld");
    }

    private void Update()
    {
        if (!WasClicked(out Vector2 screenPosition)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance)) return;

        LocationInteractable interactable = hit.collider.GetComponentInParent<LocationInteractable>();
        if (interactable == null) return;

        switch (interactable.InteractableKind)
        {
            case LocationInteractable.Kind.Exit:
                BackToCity();
                break;
            case LocationInteractable.Kind.MarketStall:
                OpenMarket();
                break;
            case LocationInteractable.Kind.GatherNode:
                Gather(hit.collider.GetComponentInParent<GatherNode>());
                break;
            case LocationInteractable.Kind.DryingRack:
                OpenDryingInfo();
                break;
            case LocationInteractable.Kind.GuildDesk:
                OpenGuildInfo();
                break;
        }
    }

    private bool WasClicked(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }
#endif
        screenPosition = Vector2.zero;
        return false;
    }

    private void OpenMarket()
    {
        if (marketUI == null)
        {
            marketUI = FindFirstObjectByType<CityMarketUI>(FindObjectsInactive.Include);
        }
        if (marketUI == null)
        {
            marketUI = gameObject.AddComponent<CityMarketUI>();
        }

        marketUI.Open();
    }

    private void Gather(GatherNode node)
    {
        if (node == null || !node.Available) return;

        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        gm.AddMaterial(node.MaterialName, node.Amount);
        node.Collect();

        ui?.ShowMessage($"Found {node.Amount} {node.MaterialName}.");
        ui?.RefreshStatus();
    }

    private void OpenDryingInfo()
    {
        string plan = CurrentPlanLine();
        string body =
            plan + "\n\n" +
            "Home drying yard. Herbs are dried here before they are crafted.\n\n" +
            "Plan a Processing card with the Home drying yard station, then come here during that slot to earn the schedule bonus.";

        ui?.OpenInfo("HOME DRYING", body);
    }

    private void OpenGuildInfo()
    {
        string plan = CurrentPlanLine();
        string body =
            plan + "\n\n" +
            "Merchant Guild handles appointments and commissions.\n\n" +
            "Appointment / Commission: Mon-Sat, 08:00-17:00.\n" +
            "Visit: Mon, Wed, Sat, 08:00-12:00 at the home drying yard.\n\n" +
            "Plan a Guild card, then come here during its time for the schedule bonus.";

        ui?.OpenInfo("MERCHANT GUILD", body);
    }

    private string CurrentPlanLine()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return "No plan data.";

        ActivityData plan = gm.GetPlanCoveringSlot(gm.CurrentDay, GameManager.TimeSlots[gm.CitySlotIndex]);
        return plan != null
            ? $"Current plan: {plan.activityName} at {plan.timeSlot}."
            : "No activity is scheduled for this slot.";
    }

    private string Title()
    {
        switch (mode)
        {
            case LocationMode.Market: return "MARKET";
            case LocationMode.Gathering: return "GATHERING AREA";
            case LocationMode.Drying: return "HOME DRYING";
            default: return "MERCHANT GUILD";
        }
    }

    private string Hint()
    {
        switch (mode)
        {
            case LocationMode.Market:
                return "Click the market stall to buy ingredients or sell your jamu and salep.";
            case LocationMode.Gathering:
                return "Click a plant to gather ingredients. It grows back after a while.";
            case LocationMode.Drying:
                return "Click a drying rack to check the processing station.";
            default:
                return "Click the guild desk to check appointments and commissions.";
        }
    }
}
