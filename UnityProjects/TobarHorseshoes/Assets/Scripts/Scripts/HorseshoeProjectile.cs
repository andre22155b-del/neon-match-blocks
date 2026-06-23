using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HorseshoeProjectile : MonoBehaviour
{
    public event Action<HorseshoeProjectile> Settled;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip stakePingClip;

    [NonSerialized] public int playerIndex;

    private Rigidbody body;
    private GameConfig config;
    private bool hasBeenThrown;
    private bool hasSettled;
    private float stableTimer;

    public bool TouchedStake { get; private set; }
    public Rigidbody Body => body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void ApplyConfig(GameConfig gameConfig)
    {
        config = gameConfig;
        body.mass = config.shoeMass;
#if UNITY_6000_0_OR_NEWER
        body.linearDamping = config.shoeDrag;
        body.angularDamping = config.shoeAngularDrag;
#else
        body.drag = config.shoeDrag;
        body.angularDrag = config.shoeAngularDrag;
#endif
    }

    public void PlaceAt(Transform origin)
    {
        transform.SetPositionAndRotation(origin.position, origin.rotation);
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = Vector3.zero;
#else
        body.velocity = Vector3.zero;
#endif
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
        TouchedStake = false;
        hasBeenThrown = false;
        hasSettled = false;
        stableTimer = 0f;
    }

    public void MarkThrown()
    {
        hasBeenThrown = true;
        body.isKinematic = false;
    }

    private void FixedUpdate()
    {
        if (!hasBeenThrown || hasSettled || config == null)
        {
            return;
        }

        float linearSpeed =
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity.magnitude;
#else
            body.velocity.magnitude;
#endif

        bool stable = linearSpeed <= config.settleVelocityThreshold &&
                      body.angularVelocity.magnitude <= config.settleAngularVelocityThreshold;

        if (stable)
        {
            stableTimer += Time.fixedDeltaTime;
            if (stableTimer >= config.settleStableSeconds)
            {
                hasSettled = true;
                Settled?.Invoke(this);
            }
        }
        else
        {
            stableTimer = 0f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag("Stake"))
        {
            return;
        }

        TouchedStake = true;
        if (audioSource != null && stakePingClip != null)
        {
            audioSource.PlayOneShot(stakePingClip, 1f);
        }
    }
}
