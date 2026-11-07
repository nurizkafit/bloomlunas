using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Mengatur interaksi di House: tidur di kasur, craft di dapur,
/// dan keluar kembali ke CityOverWorld. Penjualan dilakukan di Market.
/// </summary>
public class HouseManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private HouseUI ui;
    [SerializeField] private float maxRayDistance = 100f;

    public GameManager Game => GameManager.Instance;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (ui == null) ui = GetComponent<HouseUI>();
        if (ui == null) ui = FindFirstObjectByType<HouseUI>();

        if (GameManager.Instance == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }
    }

    private void Start()
    {
        if (ui != null) ui.RefreshStatus();
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

        HouseInteractable interactable = hit.collider.GetComponentInParent<HouseInteractable>();
        if (interactable == null) return;

        switch (interactable.InteractableKind)
        {
            case HouseInteractable.Kind.Bed:
                ui?.OpenSleep();
                break;
            case HouseInteractable.Kind.Kitchen:
                ui?.OpenCraft();
                break;
            case HouseInteractable.Kind.Door:
                LeaveHouse();
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

    public void Sleep()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        gm.RestoreEnergy();
        ui?.ClosePanels();
        ui?.ShowMessage("You rested. Energy restored to " + gm.Energy + ".");
        ui?.RefreshStatus();
    }

    public void Craft(string recipeName)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.ExecuteCraft(recipeName))
        {
            ui?.ShowMessage("Crafted " + recipeName + ". It is ready to sell.");
        }
        else
        {
            gm.HasIngredients(recipeName, out List<string> missing);
            string detail = missing != null && missing.Count > 0 ? string.Join(", ", missing) : "ingredients";
            ui?.ShowMessage("Not enough ingredients. Missing: " + detail + ".");
        }

        ui?.RefreshCraft();
        ui?.RefreshStatus();
    }

    public void LeaveHouse()
    {
        SceneManager.LoadScene("CityOverWorld");
    }

    public string CurrentTimeLabel()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return "--:--";
        return GameManager.DayShortNames[gm.CurrentDay] + " " + GameManager.TimeSlots[gm.CitySlotIndex];
    }
}
