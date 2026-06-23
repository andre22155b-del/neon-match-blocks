using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FootballProjectile : MonoBehaviour
{
    public event Action<FootballProjectile> Settled;
    public event Action<FootballProjectile> OutOfBounds;
    public event Action<FootballProjectile, FieldGoalDefenseHitbox> BlockedByDefense;

    [Header("Visuals")]
    public Transform visualRoot;
    public MeshFilter visualMeshFilter;
    public MeshRenderer visualMeshRenderer;

    private Rigidbody body;
    private NeonFieldGoalConfig config;
    private GameObject spawnedVisual;
    private GameObject appliedVisualPrefab;
    private Mesh appliedVisualMesh;
    private Material appliedVisualMaterial;
    private TrailRenderer cachedTrailRenderer;
    private Vector3 spawnReference;
    private Vector3 kickReleaseVelocity;
    private Vector3 windAcceleration;
    private Quaternion visualBaseLocalRotation;
    private float stableTimer;
    private float kickLifetime;
    private float kickReleaseElapsed;
    private float appliedKickReleaseBlend;
    private float kickCurveTorque;
    private float spiralSpinAngle;
    private bool hasBeenKicked;
    private bool hasSettled;
    private bool hasScored;
    private bool sentOutOfBounds;
    private bool sentBlockedByDefense;
    private bool visualRotationCached;

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

        cachedTrailRenderer = GetComponent<TrailRenderer>();
        ApplyTrailStyle(false, 0f);
        CacheVisualRotation();
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
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
#if UNITY_6000_0_OR_NEWER
        body.linearDamping = config.footballLinearDamping;
        body.angularDamping = config.footballAngularDamping;
#else
        body.drag = config.footballLinearDamping;
        body.angularDrag = config.footballAngularDamping;
#endif
        ResetBodyMotion(true);
        ApplyTrailStyle(false, 0f);
    }

    public void ApplyAppearance(GameObject visualPrefab, Mesh customMesh, Material customMaterial)
    {
        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        if (visualPrefab == appliedVisualPrefab &&
            customMesh == appliedVisualMesh &&
            customMaterial == appliedVisualMaterial &&
            ((visualPrefab != null && spawnedVisual != null) || (visualPrefab == null && spawnedVisual == null)))
        {
            CacheVisualRotation();
            return;
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
            appliedVisualPrefab = visualPrefab;
            appliedVisualMesh = null;
            appliedVisualMaterial = null;
            CacheVisualRotation();
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

        appliedVisualPrefab = null;
        appliedVisualMesh = customMesh;
        appliedVisualMaterial = customMaterial;
        CacheVisualRotation();
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
        kickReleaseElapsed = 0f;
        appliedKickReleaseBlend = 0f;
        kickReleaseVelocity = Vector3.zero;
        windAcceleration = Vector3.zero;
        kickCurveTorque = 0f;
        spiralSpinAngle = 0f;
        hasBeenKicked = false;
        hasSettled = false;
        hasScored = false;
        sentOutOfBounds = false;
        sentBlockedByDefense = false;

        ResetBodyMotion(true);
        if (cachedTrailRenderer == null)
        {
            cachedTrailRenderer = GetComponent<TrailRenderer>();
        }

        if (cachedTrailRenderer != null)
        {
            cachedTrailRenderer.emitting = false;
            cachedTrailRenderer.Clear();
        }
        ApplyTrailStyle(false, 0f);
        RestoreVisualRotation();
    }

    public void Kick(Vector3 launchDirection, float impulse, float curveTorque, Vector3 flightWindAcceleration)
    {
        if (hasBeenKicked)
        {
            return;
        }

        hasBeenKicked = true;
        ResetBodyMotion(false);
        kickReleaseVelocity = FieldGoalKickMath.ComputeInitialVelocity(config, launchDirection, impulse);
        windAcceleration = flightWindAcceleration;
        kickCurveTorque = curveTorque;
        kickReleaseElapsed = 0f;
        appliedKickReleaseBlend = 0f;
        ApplyTrailStyle(true, Mathf.InverseLerp(
            config != null ? config.minKickImpulse : 7f,
            config != null ? config.maxKickImpulse : 11f,
            impulse));

        float releaseDuration = FieldGoalKickMath.GetKickReleaseDuration(config);
        if (releaseDuration <= 0.0001f)
        {
            SetLinearVelocity(kickReleaseVelocity);
            body.AddTorque(Vector3.up * kickCurveTorque, ForceMode.Impulse);
            appliedKickReleaseBlend = 1f;
            kickReleaseElapsed = releaseDuration;
            return;
        }

        float openingBlend = FieldGoalKickMath.EvaluateKickReleaseBlend(config, Mathf.Clamp01(Time.fixedDeltaTime / releaseDuration));
        ApplyKickRelease(openingBlend);
        kickReleaseElapsed = Mathf.Min(Time.fixedDeltaTime, releaseDuration);
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
        ApplyKickReleaseStep(Time.fixedDeltaTime);
        ApplyCustomGravity();

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
            GetLinearVelocity().magnitude;

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

    private void LateUpdate()
    {
        if (!hasBeenKicked || visualRoot == null || visualRoot == transform || body == null)
        {
            return;
        }

        Vector3 linearVelocity = GetLinearVelocity();
        float speed = linearVelocity.magnitude;
        UpdateTrailForSpeed(speed);
        if (speed <= 0.08f)
        {
            return;
        }

        spiralSpinAngle = Mathf.Repeat(spiralSpinAngle + Time.deltaTime * Mathf.Lerp(540f, 1500f, Mathf.InverseLerp(0f, 14f, speed)), 360f);
        Quaternion targetRotation = Quaternion.LookRotation(linearVelocity.normalized, Vector3.up) * Quaternion.AngleAxis(spiralSpinAngle, Vector3.forward);
        float rotationBlend = 1f - Mathf.Exp(-12f * Time.deltaTime);
        visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, targetRotation, rotationBlend);
    }

    private void ApplyKickReleaseStep(float deltaTime)
    {
        float releaseDuration = FieldGoalKickMath.GetKickReleaseDuration(config);
        if (releaseDuration <= 0.0001f || appliedKickReleaseBlend >= 0.9999f)
        {
            appliedKickReleaseBlend = 1f;
            return;
        }

        kickReleaseElapsed = Mathf.Min(releaseDuration, kickReleaseElapsed + deltaTime);
        float releaseBlend = FieldGoalKickMath.EvaluateKickReleaseBlend(config, kickReleaseElapsed / releaseDuration);
        ApplyKickRelease(releaseBlend);
    }

    private void ApplyKickRelease(float releaseBlend)
    {
        float clampedBlend = Mathf.Clamp01(releaseBlend);
        float releaseDelta = clampedBlend - appliedKickReleaseBlend;
        if (releaseDelta <= 0f)
        {
            return;
        }

        SetLinearVelocity(GetLinearVelocity() + kickReleaseVelocity * releaseDelta);
        body.AddTorque(Vector3.up * (kickCurveTorque * releaseDelta), ForceMode.Impulse);
        appliedKickReleaseBlend = clampedBlend;
    }

    private void ApplyCustomGravity()
    {
        if (body == null || body.isKinematic)
        {
            return;
        }

        float releaseDuration = FieldGoalKickMath.GetKickReleaseDuration(config);
        float releaseProgress = releaseDuration > 0.0001f
            ? Mathf.Clamp01(kickReleaseElapsed / releaseDuration)
            : 1f;
        float gravityScale = FieldGoalKickMath.GetGravityScale(config, GetLinearVelocity().y, releaseProgress);
        body.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
        if (windAcceleration.sqrMagnitude > 0.0001f)
        {
            body.AddForce(windAcceleration, ForceMode.Acceleration);
        }
    }

    private Vector3 GetLinearVelocity()
    {
        if (body == null || body.isKinematic)
        {
            return Vector3.zero;
        }

#if UNITY_6000_0_OR_NEWER
        return body.linearVelocity;
#else
        return body.velocity;
#endif
    }

    private void SetLinearVelocity(Vector3 value)
    {
        if (body == null || body.isKinematic)
        {
            return;
        }

#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = value;
#else
        body.velocity = value;
#endif
    }

    private void SetAngularVelocity(Vector3 value)
    {
        if (body == null || body.isKinematic)
        {
            return;
        }

        body.angularVelocity = value;
    }

    private void ResetBodyMotion(bool makeKinematic)
    {
        if (body == null)
        {
            return;
        }

        body.useGravity = false;
        if (makeKinematic)
        {
            body.isKinematic = true;
            body.Sleep();
            return;
        }

        if (body.isKinematic)
        {
            body.isKinematic = false;
        }

        SetLinearVelocity(Vector3.zero);
        SetAngularVelocity(Vector3.zero);
        body.WakeUp();
    }

    private void ApplyTrailStyle(bool launched, float intensity01)
    {
        if (cachedTrailRenderer == null)
        {
            cachedTrailRenderer = GetComponent<TrailRenderer>();
        }

        if (cachedTrailRenderer == null)
        {
            return;
        }

        float clampedIntensity = Mathf.Clamp01(intensity01);
        cachedTrailRenderer.emitting = launched;
        cachedTrailRenderer.time = launched ? Mathf.Lerp(0.18f, 0.28f, clampedIntensity) : 0.12f;
        cachedTrailRenderer.startWidth = launched ? Mathf.Lerp(0.18f, 0.28f, clampedIntensity) : 0.12f;
        cachedTrailRenderer.endWidth = launched ? Mathf.Lerp(0.028f, 0.05f, clampedIntensity) : 0.02f;
        cachedTrailRenderer.minVertexDistance = 0.035f;
        cachedTrailRenderer.alignment = LineAlignment.View;
        cachedTrailRenderer.numCornerVertices = 6;
        cachedTrailRenderer.numCapVertices = 8;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.38f, 1f, 0.76f), 0f),
                new GradientColorKey(new Color(0.44f, 0.98f, 1f), 0.35f),
                new GradientColorKey(new Color(1f, 0.38f, 0.9f), 0.82f),
                new GradientColorKey(new Color(1f, 0.86f, 0.42f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(launched ? 0.72f : 0.26f, 0f),
                new GradientAlphaKey(launched ? 0.44f : 0.14f, 0.55f),
                new GradientAlphaKey(0.02f, 1f)
            });
        cachedTrailRenderer.colorGradient = gradient;
    }

    private void UpdateTrailForSpeed(float speed)
    {
        if (cachedTrailRenderer == null || !cachedTrailRenderer.emitting)
        {
            return;
        }

        float speed01 = Mathf.InverseLerp(0f, 14f, speed);
        cachedTrailRenderer.startWidth = Mathf.Lerp(cachedTrailRenderer.startWidth, Mathf.Lerp(0.16f, 0.28f, speed01), Time.deltaTime * 9f);
        cachedTrailRenderer.endWidth = Mathf.Lerp(cachedTrailRenderer.endWidth, Mathf.Lerp(0.025f, 0.052f, speed01), Time.deltaTime * 9f);
    }

    private void CacheVisualRotation()
    {
        if (visualRoot == null)
        {
            return;
        }

        visualBaseLocalRotation = visualRoot.localRotation;
        visualRotationCached = true;
    }

    private void RestoreVisualRotation()
    {
        if (!visualRotationCached || visualRoot == null || visualRoot == transform)
        {
            return;
        }

        visualRoot.localRotation = visualBaseLocalRotation;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!hasBeenKicked || hasScored || sentBlockedByDefense || collision == null)
        {
            return;
        }

        FieldGoalDefenseHitbox defenseHitbox = collision.collider != null
            ? collision.collider.GetComponent<FieldGoalDefenseHitbox>()
            : null;
        if (defenseHitbox == null && collision.transform != null)
        {
            defenseHitbox = collision.transform.GetComponentInParent<FieldGoalDefenseHitbox>();
        }

        if (defenseHitbox == null)
        {
            return;
        }

        sentBlockedByDefense = true;
        BlockedByDefense?.Invoke(this, defenseHitbox);
    }
}
