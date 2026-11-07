using UnityEngine;

/// <summary>Penanda objek yang bisa diklik di scene lokasi.</summary>
public class LocationInteractable : MonoBehaviour
{
    public enum Kind
    {
        MarketStall,
        GatherNode,
        DryingRack,
        GuildDesk,
        Exit
    }

    [SerializeField] private Kind kind = Kind.MarketStall;

    public Kind InteractableKind => kind;

    public void SetKind(Kind value)
    {
        kind = value;
    }
}
