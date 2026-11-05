using System;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    [Header("Pengaturan Gerakan")]
    [Tooltip("Kecepatan jalan karakter")]
    [SerializeField] private float moveSpeed = 4f;

    [Tooltip("Kecepatan rotasi karakter saat berputar menghadap target")]
    [SerializeField] private float rotationSpeed = 12f;

    [Tooltip("Jarak toleransi sebelum karakter dianggap sudah sampai di tujuan")]
    [SerializeField] private float stoppingDistance = 0.15f;

    [Header("Animasi (Opsional)")]
    [Tooltip("Komponen Animator jika model memiliki animasi jalan (otomatis dicari jika kosong)")]
    [SerializeField] private Animator animator;
    [Tooltip("Nama parameter boolean pada Animator untuk status jalan")]
    [SerializeField] private string walkingParamName = "isWalking";

    private Vector3 targetPosition;
    private bool isMoving = false;
    private Action onArrivedCallback;
    private int walkingHash;
    private bool hasAnimator;

    public bool IsMoving => isMoving;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        hasAnimator = animator != null;
        if (hasAnimator && !string.IsNullOrEmpty(walkingParamName))
        {
            walkingHash = Animator.StringToHash(walkingParamName);
        }

        targetPosition = transform.position;
    }

    private void Update()
    {
        if (!isMoving) return;

        // Ambil posisi target dengan Y yang sama dengan karakter agar tidak melayang/tenggelam
        Vector3 currentPos = transform.position;
        Vector3 destination = new Vector3(targetPosition.x, currentPos.y, targetPosition.z);
        float distance = Vector3.Distance(currentPos, destination);

        if (distance <= stoppingDistance)
        {
            ReachDestination();
            return;
        }

        // Putar badan karakter menghadap ke arah jalan
        Vector3 moveDirection = (destination - currentPos).normalized;
        moveDirection.y = 0;
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Geser posisi karakter menuju tujuan secara halus
        transform.position = Vector3.MoveTowards(currentPos, destination, moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Memerintahkan karakter untuk berjalan ke posisi tertentu.
    /// </summary>
    public void MoveTo(Vector3 destination, Action onArrived = null)
    {
        targetPosition = destination;
        onArrivedCallback = onArrived;

        Vector3 currentPos = transform.position;
        Vector3 destOnPlane = new Vector3(destination.x, currentPos.y, destination.z);

        // Jika sudah berada di lokasi tujuan, langsung panggil callback
        if (Vector3.Distance(currentPos, destOnPlane) <= stoppingDistance)
        {
            isMoving = false;
            SetWalkingAnimation(false);
            Action callback = onArrivedCallback;
            onArrivedCallback = null;
            callback?.Invoke();
            return;
        }

        isMoving = true;
        SetWalkingAnimation(true);
    }

    /// <summary>
    /// Menghentikan gerakan karakter saat ini.
    /// </summary>
    public void StopMoving()
    {
        isMoving = false;
        onArrivedCallback = null;
        SetWalkingAnimation(false);
    }

    private void ReachDestination()
    {
        isMoving = false;
        SetWalkingAnimation(false);

        Action callback = onArrivedCallback;
        onArrivedCallback = null;
        callback?.Invoke();
    }

    private void SetWalkingAnimation(bool walking)
    {
        if (hasAnimator && walkingHash != 0)
        {
            // Pastikan parameter ada sebelum set bool
            foreach (var param in animator.parameters)
            {
                if (param.nameHash == walkingHash && param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool(walkingHash, walking);
                    break;
                }
            }
        }
    }
}

