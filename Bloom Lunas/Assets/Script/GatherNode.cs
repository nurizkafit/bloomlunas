using UnityEngine;

/// <summary>
/// Tanaman/bahan di Gathering Area. Sekali petik memberi bahan,
/// lalu menghilang sementara dan tumbuh lagi setelah beberapa detik.
/// </summary>
public class GatherNode : MonoBehaviour
{
    [SerializeField] private string materialName = "Turmeric";
    [SerializeField] private int amount = 1;
    [SerializeField] private float respawnSeconds = 25f;

    private Renderer[] renderers;
    private Collider[] colliders;

    public string MaterialName => materialName;
    public int Amount => amount;
    public bool Available { get; private set; } = true;

    private void Awake()
    {
        CacheVisuals();
    }

    public void Configure(string material, int yield, float respawn)
    {
        materialName = material;
        amount = Mathf.Max(1, yield);
        respawnSeconds = Mathf.Max(3f, respawn);
        CacheVisuals();
    }

    private void CacheVisuals()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
    }

    public void Collect()
    {
        if (!Available) return;

        Available = false;
        SetVisible(false);
        Invoke(nameof(Respawn), respawnSeconds);
    }

    private void Respawn()
    {
        Available = true;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (renderers == null || colliders == null) CacheVisuals();

        foreach (Renderer r in renderers)
        {
            if (r != null) r.enabled = visible;
        }

        foreach (Collider c in colliders)
        {
            if (c != null) c.enabled = visible;
        }
    }
}
