using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class WorldInteractionManager : MonoBehaviour
{
    [Header("Referensi Utama")]
    [Tooltip("Kamera utama scene. Jika dikosongkan, akan otomatis menggunakan Camera.main")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("Karakter pemain (character-e). Jika dikosongkan, akan dicari otomatis")]
    [SerializeField] private CharacterMovement playerCharacter;

    [Tooltip("Sistem UI Pop up saat gedung diklik (Opsional, kosongkan jika tidak dipakai)")]
    [SerializeField] private LocationPopupUI popupUI;

    [Tooltip("Rutinitas harian otomatis (Opsional, dicari otomatis jika ada di scene)")]
    [SerializeField] private DailyScheduleRunner scheduleRunner;

    [Header("Pengaturan Klik & Raycast")]
    [Tooltip("Layer bangunan yang dapat diklik (Default: Everything)")]
    [SerializeField] private LayerMask clickableLayers = ~0;

    [Tooltip("Jarak maksimal deteksi raycast klik")]
    [SerializeField] private float maxRayDistance = 300f;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (playerCharacter == null)
        {
            playerCharacter = FindFirstObjectByType<CharacterMovement>();
        }

        if (popupUI == null)
        {
            popupUI = FindFirstObjectByType<LocationPopupUI>();
        }

        if (scheduleRunner == null)
        {
            scheduleRunner = FindFirstObjectByType<DailyScheduleRunner>();
        }
    }

    private void Update()
    {
        if (WasMouseClicked(out Vector2 screenPosition))
        {
            HandleClick(screenPosition);
        }
    }

    /// <summary>
    /// Mendeteksi klik mouse dengan dukungan New Input System dan Legacy Input.
    /// </summary>
    private bool WasMouseClicked(out Vector2 screenPosition)
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

    private void HandleClick(Vector2 screenPosition)
    {
        // Abaikan klik jika kursor sedang berada di atas elemen UI (tombol, panel, dll.)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, clickableLayers))
        {
            // Deteksi komponen LocationPoint pada objek yang diklik atau parent-nya
            LocationPoint location = hit.collider.GetComponentInParent<LocationPoint>();
            if (location != null)
            {
                // Jika karakter sedang bergerak otomatis mengikuti rutinitas jadwal,
                // jeda rutinitas agar pemain bisa menjelajah manual di luar jadwal
                if (scheduleRunner != null && scheduleRunner.IsControllingMovement)
                {
                    scheduleRunner.TogglePause();
                }

                OnLocationClicked(location);
            }
        }
    }

    private void OnLocationClicked(LocationPoint location)
    {
        if (playerCharacter == null)
        {
            playerCharacter = FindFirstObjectByType<CharacterMovement>();
            if (playerCharacter == null)
            {
                Debug.LogWarning("CharacterMovement (character-e) tidak ditemukan di scene!");
                return;
            }
        }

        // Tutup pop-up sebelumnya saat karakter mulai bergerak menuju tujuan baru
        if (popupUI != null)
        {
            popupUI.ClosePopup();
        }

        // Dapatkan titik tujuan dengan ketinggian Y karakter saat ini
        Vector3 destination = location.GetDestination(playerCharacter.transform.position.y);

        // Perintahkan karakter berjalan ke lokasi; setelah tiba, tampilkan pop-up jika ada
        playerCharacter.MoveTo(destination, () =>
        {
            if (popupUI != null)
            {
                popupUI.ShowPopup(location);
            }
        });
    }
}
