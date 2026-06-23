using System.Collections;
using UnityEngine;

/// <summary>
/// Light camera motion used for drops and combos.
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Idle Motion")]
    public float swayAmount = 0.06f;
    public float swaySpeed = 0.45f;
    public float tiltAmount = 1.25f;

    [Header("Drop Response")]
    public float dropKickDistance = 0.2f;
    public float dropKickDuration = 0.12f;

    [Header("Combo Response")]
    public float comboZoom = 6f;
    public float comboDuration = 0.45f;

    private Vector3 basePosition;
    private Quaternion baseRotation;
    private Camera cachedCamera;
    private float baseFov;
    private Coroutine activeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        cachedCamera = GetComponent<Camera>();
        basePosition = transform.position;
        baseRotation = transform.rotation;
        baseFov = cachedCamera != null ? cachedCamera.fieldOfView : 60f;
    }

    private void Update()
    {
        if (activeRoutine != null)
        {
            return;
        }

        float t = Time.time * swaySpeed;
        transform.position = basePosition + new Vector3(Mathf.Sin(t) * swayAmount, Mathf.Cos(t * 0.7f) * swayAmount * 0.5f, 0f);
        transform.rotation = baseRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.6f) * tiltAmount);
    }

    public void KickForDrop()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(DropKickRoutine());
    }

    public void PlayComboShot()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(ComboRoutine());
    }

    private IEnumerator DropKickRoutine()
    {
        Vector3 start = transform.position;
        Vector3 pushed = basePosition + Vector3.back * dropKickDistance;
        float elapsed = 0f;

        while (elapsed < dropKickDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dropKickDuration);
            transform.position = Vector3.Lerp(start, pushed, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < dropKickDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dropKickDuration);
            transform.position = Vector3.Lerp(pushed, basePosition, t);
            yield return null;
        }

        transform.position = basePosition;
        activeRoutine = null;
    }

    private IEnumerator ComboRoutine()
    {
        if (cachedCamera == null)
        {
            activeRoutine = null;
            yield break;
        }

        float startFov = cachedCamera.fieldOfView;
        float targetFov = Mathf.Max(34f, baseFov - comboZoom);
        float elapsed = 0f;

        while (elapsed < comboDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / comboDuration);
            cachedCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < comboDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / comboDuration);
            cachedCamera.fieldOfView = Mathf.Lerp(targetFov, baseFov, t);
            yield return null;
        }

        cachedCamera.fieldOfView = baseFov;
        activeRoutine = null;
    }
}

/// <summary>
/// Central VFX helper. All particle prefabs are optional.
/// </summary>
public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject dropImpactPrefab;
    public GameObject wordBurstPrefab;
    public GameObject comboBurstPrefab;
    public GameObject bombBurstPrefab;

    [Header("Optional Glow Light")]
    public Light boardGlowLight;
    public bool reducedFxMode;
    public float lightPulseIntensity = 2.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetReducedFxMode(bool enabled)
    {
        reducedFxMode = enabled;
    }

    public void SpawnDropImpact(Vector3 worldPosition)
    {
        Spawn(dropImpactPrefab, worldPosition, 1.5f);
        PulseLight(new Color(0.35f, 1f, 1f));
    }

    public void SpawnWordBurst(Vector3 worldPosition)
    {
        Spawn(wordBurstPrefab, worldPosition, 2.5f);
        PulseLight(new Color(0.25f, 1f, 0.95f));
    }

    public void SpawnComboBurst(Vector3 worldPosition, int cascadeDepth)
    {
        if (reducedFxMode)
        {
            SpawnWordBurst(worldPosition);
            return;
        }

        Spawn(comboBurstPrefab, worldPosition, 3f);
        PulseLight(Color.Lerp(new Color(0.25f, 1f, 1f), new Color(1f, 0.25f, 0.85f), Mathf.Clamp01(cascadeDepth / 4f)));
    }

    public void SpawnBombBurst(Vector3 worldPosition)
    {
        Spawn(reducedFxMode ? wordBurstPrefab : bombBurstPrefab, worldPosition, 2.5f);
        PulseLight(new Color(1f, 0.6f, 0.2f));
    }

    private void Spawn(GameObject prefab, Vector3 worldPosition, float lifetime)
    {
        if (prefab == null)
        {
            return;
        }

        GameObject instance = Instantiate(prefab, worldPosition, Quaternion.identity);
        Destroy(instance, lifetime);
    }

    private void PulseLight(Color targetColor)
    {
        if (boardGlowLight == null)
        {
            return;
        }

        StopAllCoroutines();
        StartCoroutine(PulseLightRoutine(targetColor));
    }

    private IEnumerator PulseLightRoutine(Color targetColor)
    {
        float startIntensity = boardGlowLight.intensity;
        Color startColor = boardGlowLight.color;

        float elapsed = 0f;
        while (elapsed < 0.12f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.12f);
            boardGlowLight.intensity = Mathf.Lerp(startIntensity, lightPulseIntensity, t);
            boardGlowLight.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.35f);
            boardGlowLight.intensity = Mathf.Lerp(lightPulseIntensity, startIntensity, t);
            boardGlowLight.color = Color.Lerp(targetColor, startColor, t);
            yield return null;
        }

        boardGlowLight.intensity = startIntensity;
        boardGlowLight.color = startColor;
    }
}
