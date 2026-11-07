using UnityEngine;

/// <summary>
/// Kamera isometrik seperti CityOverWorld: sudut tinggi, menghadap ke bawah.
/// Kamera mengikuti karakter agar karakter selalu terlihat di scene.
/// </summary>
public class IsometricCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(1.62f, 9.69f, -6.86f);
    [SerializeField] private Vector3 eulerAngles = new Vector3(51.681f, 0f, 0f);
    [SerializeField] private bool follow = true;

    public void Setup(Transform followTarget)
    {
        target = followTarget;
        if (target != null)
        {
            offset = transform.position - target.position;
        }
        eulerAngles = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(eulerAngles);
    }

    private void LateUpdate()
    {
        if (!follow || target == null) return;

        transform.position = target.position + offset;
        transform.rotation = Quaternion.Euler(eulerAngles);
    }
}
