using UnityEngine;

public class TaxiZombieHit : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody carRb;

    [Header("Launch Feel")]
    public float impulsePerKmh = 0.25f;
    public float upBoost = 0.6f;
    public float forwardBoost = 1.2f;
    public float maxImpulse = 40f;

    [Header("Spin")]
    public float torquePerKmh = 0.15f;
    public float maxTorque = 25f;

    [Header("Filter")]
    public float minKmhToRagdoll = 10f;
    public LayerMask zombieLayers;

    private void Awake()
    {
        if (!carRb) carRb = GetComponentInParent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Layer filter (recommended)
        if (((1 << collision.gameObject.layer) & zombieLayers) == 0)
            return;

        ZombieRagdoll rag = collision.collider.GetComponentInParent<ZombieRagdoll>();
        if (!rag) return;

        //Car speed
        float speedKmh = (carRb ? carRb.linearVelocity.magnitude : 0f) * 3.6f;
        if (speedKmh < minKmhToRagdoll) return;

        ContactPoint cp = collision.GetContact(0);
        Vector3 hitPoint = cp.point;

        //Direction: mostly car forward, with some contact normal correction
        Vector3 dir = (transform.forward * forwardBoost + Vector3.up * upBoost).normalized;

        float impulseMag = Mathf.Clamp(speedKmh * impulsePerKmh, 0f, maxImpulse);
        Vector3 impulse = dir * impulseMag;

        //Spin: random-ish axis around up + sideways
        Vector3 torqueAxis = (Vector3.up + transform.right * Random.Range(-0.6f, 0.6f)).normalized;
        float torqueMag = Mathf.Clamp(speedKmh * torquePerKmh, 0f, maxTorque);

        rag.HitAndLaunch(impulse, hitPoint, torqueAxis, torqueMag);
    }
}
