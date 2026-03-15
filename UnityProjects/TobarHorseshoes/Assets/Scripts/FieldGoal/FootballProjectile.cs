using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FootballProjectile : MonoBehaviour
{
    public event Action<FootballProjectile> Settled;
    public event Action<FootballProjectile> OutOfBounds;

    [Header("Visuals")]
    public Transform visualRoot;
    public MeshFilter visualMeshFilter;
    public MeshRenderer visualMeshRenderer;

    private Rigidbody body;
    private NeonFieldGoalConfig config;
    private GameObject spawnedVisual;
    private Vector3 spawnReference;
    private float stableTimer;
    private float kickLifetime;
    private bool hasBeenKicked;
    private bool hasSettled;
    private bool hasScored;
    private bool sentOutOfBounds;

    public Rigidbody Body => body;
    public bool HasBeenKicked => hasBeenKicked;
    public bool HasSettled => hasSettled;
    public bool HasScored => hasScored;
    public float KickLifetime => kickLifetime;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        if (visualMeshFilter == null)
        {
            visualMeshFilter = GetComponentInChildren<MeshFilter>();
        }

        if (visualMeshRenderer == null)
        {
            visualMeshRenderer = GetComponentInChildren<MeshRenderer>();
        }
    }

    public void ApplyConfig(NeonFieldGoalConfig fieldGoalConfig)
    {
        config = fieldGoalConfig;
        if (config == null)
        {
            return;
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        body.mass = config.footballMass;
#if UNITY_6000_0_OR_NEWER
        body.linearDamping = config.footballLinearDamping;
        body.angularDamping = config.footballAngularDamping;
        body.linearVelocity = Vector3.zero;
#else
        body.drag = config.footballLinearDamping;
        body.angularDrag = config.footballAngularDamping;
        body.velocity = Vector3.zero;
#endif
        body.angularVelocity = Vector3.zero;
    }

    public void ApplyAppearance(GameObject visualPrefab, Mesh customMesh, Material customMaterial)
    {
        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        if (spawnedVisual != null)
        {
            Destroy(spawnedVisual);
            spawnedVisual = null;
        }

        if (visualPrefab != null)
        {
            if (visualMeshRenderer != null)
            {
                visualMeshRenderer.enabled = false;
            }

            spawnedVisual = Instantiate(visualPrefab, visualRoot);
            spawnedVisual.transform.localPosition = Vector3.zero;
            spawnedVisual.transform.localRotation = Quaternion.identity;
            spawnedVisual.transform.localScale = Vector3.one;
            return;
        }

        if (visualMeshFilter == null)
        {
            visualMeshFilter = GetComponentInChildren<MeshFilter>();
        }

        if (visualMeshRenderer == null)
        {
            visualMeshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        if (visualMeshFilter != null && customMesh != null)
        {
            visualMeshFilter.sharedMesh = customMesh;
        }

        if (visualMeshRenderer != null && customMaterial != null)
        {
            visualMeshRenderer.enabled = true;
            visualMeshRenderer.sharedMaterial = customMaterial;
        }
    }

    public void PlaceAt(Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            return;
        }

        transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        spawnReference = spawnPoint.position;
        stableTimer = 0f;
        kickLifetime = 0f;
        hasBeenKicked = false;
        hasSettled = false;
        hasScored = false;
        sentOutOfBounds = false;

#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = Vector3.zero;
#else
        body.velocity = Vector3.zero;
#endif
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
    }

    public void Kick(Vector3 launchDirection, float impulse, float curveTorque)
    {
        if (hasBeenKicked)
        {
            return;
        }

        hasBeenKicked = true;
        body.isKinematic = false;
        body.AddForce(launchDirection * impulse, ForceMode.Impulse);
        body.AddTorque(Vector3.up * curveTorque, ForceMode.Impulse);
    }

    public bool MarkGoalScored()
    {
        if (hasScored)
        {
            return false;
        }

        hasScored = true;
        return true;
    }

    private void FixedUpdate()
    {
        if (!hasBeenKicked || config == null)
        {
            return;
        }

        kickLifetime += Time.fixedDeltaTime;

        if (!sentOutOfBounds)
        {
            bool fellAway = transform.position.y <= config.outOfBoundsY;
            bool tooFar = Vector3.Distance(transform.position, spawnReference) >= config.outOfBoundsDistance;
            bool expired = kickLifetime >= config.maxBallLifeSeconds;
            if (fellAway || tooFar || expired)
            {
                sentOutOfBounds = true;
                OutOfBounds?.Invoke(this);
            }
        }

        if (hasSettled)
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
}
