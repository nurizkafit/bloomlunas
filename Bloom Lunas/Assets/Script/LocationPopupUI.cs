using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocationPopupUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel utama pop-up yang akan dimunculkan atau disembunyikan")]
    [SerializeField] private GameObject popupPanel;

    [Tooltip("Komponen teks untuk nama lokasi / judul pop-up (contoh: Gathering area)")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("Komponen teks untuk deskripsi atau jam operasional lokasi (opsional)")]
    [SerializeField] private TMP_Text descriptionText;

    [Tooltip("Tombol Close ('X') untuk menutup pop-up ini")]
    [SerializeField] private Button closeButton;

    [Header("Planned Activity Display (Integrasi Buku Planning)")]
    [Tooltip("Komponen teks untuk menampilkan aktivitas yang sudah diplanning di tempat ini")]
    [SerializeField] private TMP_Text plannedActivityText;

    [Tooltip("Container / baris aktivitas (otomatis diaktifkan jika ada plan, atau disembunyikan jika kosong)")]
    [SerializeField] private GameObject plannedActivityContainer;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePopup);
        }

        // Pastikan pop-up tertutup saat game baru dimulai
        ClosePopup();
    }

    /// <summary>
    /// Menampilkan pop-up dan mengisi teks sesuai lokasi yang dikunjungi.
    /// </summary>
    public void ShowPopup(LocationPoint location)
    {
        if (location == null) return;

        if (titleText != null)
        {
            titleText.text = location.LocationName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = location.LocationDescription;
        }

        // Cek apakah ada jadwal/planning aktivitas di tempat ini dari GameManager
        CheckPlannedActivity(location.LocationName);

        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    private void CheckPlannedActivity(string locName)
    {
        if (GameManager.Instance == null)
        {
            if (plannedActivityContainer != null) plannedActivityContainer.SetActive(false);
            return;
        }

        var allPlans = GameManager.Instance.GetAllPlans();
        ActivityData matchedActivity = null;

        foreach (var p in allPlans)
        {
            if (p.activity == null) continue;

            string targetLoc = p.activity.locationName.Trim();
            if (!string.IsNullOrEmpty(targetLoc) &&
                (locName.IndexOf(targetLoc, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 targetLoc.IndexOf(locName, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                matchedActivity = p.activity;
                break;
            }
        }

        if (matchedActivity != null)
        {
            if (plannedActivityText != null)
            {
                plannedActivityText.text = matchedActivity.activityName;
            }

            if (plannedActivityContainer != null)
            {
                plannedActivityContainer.SetActive(true);
            }
        }
        else
        {
            if (plannedActivityContainer != null)
            {
                plannedActivityContainer.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Menutup atau menyembunyikan pop-up UI.
    /// </summary>
    public void ClosePopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Mengecek apakah pop-up sedang terbuka.
    /// </summary>
    public bool IsOpen()
    {
        if (popupPanel != null) return popupPanel.activeSelf;
        return gameObject.activeSelf;
    }
}
