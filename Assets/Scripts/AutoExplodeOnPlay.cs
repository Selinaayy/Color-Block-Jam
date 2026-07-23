using UnityEngine;
using System.Collections;

public class AutoExplodeOnPlay : MonoBehaviour
{
    public static AutoExplodeOnPlay Instance { get; private set; }

    [Header("Explosion Settings")]
    public float explosionForce = 12f;
    public float explosionRadius = 3f;
    public float upwardsModifier = 1f;
    public float randomTorque = 2f;

    [Header("Optional")]
    public Transform explosionCenter;
    public float delay = 0.05f;

    private Rigidbody[] pieceRigidbodies;
    private bool hasExploded;

    void Awake()
    {
        pieceRigidbodies = ComponentCacheUtility.CollectRigidbodiesInChildren(transform, true);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SceneObjectRegistry.RegisterAutoExplode(this);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        SceneObjectRegistry.UnregisterAutoExplode(this);
    }

    public void TriggerExplosion()
    {
        if (hasExploded)
        {
            return;
        }

        hasExploded = true;
        StartCoroutine(ExplodeRoutine());
    }

    private IEnumerator ExplodeRoutine()
    {
        yield return new WaitForSeconds(delay);
        Vector3 center = explosionCenter != null ? explosionCenter.position : transform.position;

        if (pieceRigidbodies == null || pieceRigidbodies.Length == 0)
        {
            pieceRigidbodies = ComponentCacheUtility.CollectRigidbodiesInChildren(transform, true);
        }

        foreach (Rigidbody rb in pieceRigidbodies)
        {
            if (rb == null) continue;
            rb.isKinematic = false;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
            rb.AddExplosionForce(explosionForce, center, explosionRadius, upwardsModifier, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * randomTorque, ForceMode.Impulse);
        }
    }
}
