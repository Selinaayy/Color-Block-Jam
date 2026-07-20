using UnityEngine;
using System.Collections;

public class ScatterPieces : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Seconds after play before pieces scatter")]
    public float delay = 0.05f;

    [Header("Force Settings")]
    [Tooltip("Horizontal scatter force")]
    public float sideForce = 4f;

    [Tooltip("Upward force")]
    public float upForce = 1.5f;

    [Tooltip("Rotation force applied to pieces")]
    public float torqueForce = 2f;

    [Header("Randomness")]
    [Tooltip("Random force multiplier")]
    public float randomForceMultiplier = 1f;

    [Header("Physics")]
    [Tooltip("Disables kinematic mode automatically when enabled")]
    public bool disableKinematic = true;

    [Tooltip("Clears rigidbody constraints")]
    public bool clearConstraints = true;

    [Header("Debug")]
    public bool runOnStart = false;

    private Rigidbody[] pieceRigidbodies;
    private bool hasScattered;

    void Awake()
    {
        pieceRigidbodies = ComponentCacheUtility.CollectRigidbodiesInChildren(transform, true);
    }

    void Start()
    {
        if (runOnStart)
        {
            TriggerScatter();
        }
    }

    public void TriggerScatter()
    {
        if (hasScattered)
        {
            return;
        }

        hasScattered = true;
        StartCoroutine(ScatterRoutine());
    }

    IEnumerator ScatterRoutine()
    {
        yield return new WaitForSeconds(delay);

        if (pieceRigidbodies == null || pieceRigidbodies.Length == 0)
        {
            pieceRigidbodies = ComponentCacheUtility.CollectRigidbodiesInChildren(transform, true);
        }

        foreach (Rigidbody rb in pieceRigidbodies)
        {
            if (rb == null) continue;

            if (disableKinematic)
            {
                rb.isKinematic = false;
            }

            if (clearConstraints)
            {
                rb.constraints = RigidbodyConstraints.None;
            }

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;

            Vector2 random2D = Random.insideUnitCircle.normalized;
            Vector3 direction = new Vector3(random2D.x, 0f, random2D.y);

            float randomMultiplier = Random.Range(0.7f, 1.3f) * randomForceMultiplier;

            Vector3 force = direction * sideForce * randomMultiplier + Vector3.up * upForce;

            rb.AddForce(force, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);
        }
    }
}
