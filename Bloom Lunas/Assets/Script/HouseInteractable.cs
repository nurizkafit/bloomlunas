using UnityEngine;

/// <summary>Penanda objek yang bisa diklik di dalam House.</summary>
public class HouseInteractable : MonoBehaviour
{
    public enum Kind
    {
        Bed,
        Kitchen,
        Shop,
        Door
    }

    [SerializeField] private Kind kind = Kind.Bed;

    public Kind InteractableKind => kind;

    public void SetKind(Kind value)
    {
        kind = value;
    }
}
