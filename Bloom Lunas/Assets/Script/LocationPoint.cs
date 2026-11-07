using UnityEngine;

public class LocationPoint : MonoBehaviour
{
    [Header("Informasi Lokasi")]
    [Tooltip("Nama lokasi/gedung yang akan ditampilkan di pop up (bisa diedit di Inspector)")]
    [SerializeField] private string locationName = "Gathering area";

    [Tooltip("Deskripsi atau info tambahan mengenai lokasi ini")]
    [TextArea(2, 4)]
    [SerializeField] private string locationDescription = "Open Mon 6:00-17:00 - You are here";

    [Header("Posisi Tujuan Karakter")]
    [Tooltip("Titik berdiri karakter saat sampai di gedung ini (opsional). Jika kosong, menggunakan posisi gedung ini.")]
    [SerializeField] private Transform walkPoint;

    [Tooltip("Offset posisi jika tidak menggunakan walkPoint khusus (misal jika ingin berdiri sedikit di depan gedung)")]
    [SerializeField] private Vector3 walkOffset = Vector3.zero;

    public string LocationName => locationName;
    public string LocationDescription => locationDescription;

    /// <summary>
    /// Mendapatkan koordinat tujuan untuk karakter.
    /// </summary>
    public Vector3 GetDestination(float characterY)
    {
        Vector3 target = walkPoint != null ? walkPoint.position : (transform.position + walkOffset);
        // Menjaga agar karakter tetap berada pada ketinggian Y karakter saat ini
        target.y = characterY;
        return target;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 dest = walkPoint != null ? walkPoint.position : (transform.position + walkOffset);
        Gizmos.DrawWireSphere(dest, 0.35f);
        Gizmos.DrawLine(transform.position, dest);
    }
}

