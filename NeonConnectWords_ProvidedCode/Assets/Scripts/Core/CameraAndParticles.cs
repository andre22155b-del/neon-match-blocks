using System.Collections;
using UnityEngine;

/// <summary>
/// CameraController: Handles smooth camera motion — idle sway, combo zoom,
/// tilt reactions, and cinematic shots for epic drops.
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Idle Motion")]
    public float idleSwayAmplitude = 0.04f;
    public float idleSwaySpeed = 0.4f;
    public float idleTiltAmplitude = 1.5f;

    [Header("Combo Shot")]
    public float comboZoomAmount = 0.8f;
    public float comboZoomDuration = 0.6f;
    public float comboReturnDuration = 1.0f;

    [Header("Shake")]
    public float shakeIntensity = 0.12f;
    public float shakeDuration = 0.3f;

    private Vector3 basePosition;
    private Quaternion baseRotation;
    private float baseFOV;
    private Camera cam;
    private bool inCinematic = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        cam = GetComponent<Camera>();
        basePosition = transform.position;
        baseRotation = transform.rotation;
        baseFOV = cam ? cam.fieldOfView : 60f;
    }

    private void Update()
    {
        if (!inCinematic) ApplyIdleSway();
    }

    private void ApplyIdleSway()
    {
        float t = Time.time * idleSwaySpeed;
        Vector3 sway = new Vector3(
            Mathf.Sin(t) * idleSwayAmplitude,
            Mathf.Cos(t * 0.7f) * idleSwayAmplitude * 0.5f,
            0f);
        float tiltZ = Mathf.Sin(t * 0.5f) * idleTiltAmplitude;

        transform.position = Vector3.Lerp(transform.position, basePosition + sway, Time.deltaTime * 3f);
        transform.rotation = Quaternion.Lerp(transform.rotation,
            baseRotation * Quaternion.Euler(0, 0, tiltZ), Time.deltaTime * 2f);
    }

    // Called on epic combo
    public void PlayComboShot()
    {
        if (!inCinematic) StartCoroutine(ComboShotRoutine());
    }

    private IEnumerator ComboShotRoutine()
    {
        inCinematic = true;
        float startFOV = cam ? cam.fieldOfView : baseFOV;

        // Zoom in
        float elapsed = 0f;
        while (elapsed < comboZoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / comboZoomDuration;
            if (cam) cam.fieldOfView = Mathf.Lerp(startFOV, baseFOV - comboZoomAmount * 10f, EaseOutCubic(t));
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        // Zoom back
        elapsed = 0f;
        float zoomedFOV = cam ? cam.fieldOfView : baseFOV;
        while (elapsed < comboReturnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / comboReturnDuration;
            if (cam) cam.fieldOfView = Mathf.Lerp(zoomedFOV, baseFOV, EaseOutCubic(t));
            yield return null;
        }
        if (cam) cam.fieldOfView = baseFOV;
        inCinematic = false;
    }

    public void Shake()
    {
        StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float strength = Mathf.Lerp(shakeIntensity, 0f, elapsed / shakeDuration);
            transform.position = basePosition + Random.insideUnitSphere * strength;
            yield return null;
        }
        transform.position = basePosition;
    }

    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
}

// ============================================================

/// <summary>
/// ParticleManager: Spawns and manages all VFX — word completion bursts,
/// epic connection explosions, small per-tile sparks, and board lighting pulses.
/// Supports reduced-FX mode for mobile performance.
/// </summary>
public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject wordCompletePrefab;   // medium burst
    public GameObject epicComboPrefab;      // large multi-colour explosion
    public GameObject smallSparkPrefab;     // tiny per-tile pop
    public GameObject trailPrefab;          // used by letter tile drop trails

    [Header("Board Glow")]
    public Light boardLight;
    public float boardLightBaseIntensity = 1f;
    public float boardLightComboIntensity = 5f;
    public Color[] comboLightColors;

    [Header("Performance")]
    public bool reducedFX = false;
    private int comboColorIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetReducedFX(bool on)
    {
        reducedFX = on;
    }

    public void SpawnWordParticles(Vector3 worldPos)
    {
        if (wordCompletePrefab)
        {
            GameObject go = Instantiate(wordCompletePrefab, worldPos, Quaternion.identity);
            Destroy(go, 3f);
        }
        PulseBoardLight(false);
    }

    public void SpawnEpicParticles(Vector3 worldPos)
    {
        if (reducedFX) { SpawnWordParticles(worldPos); return; }

        if (epicComboPrefab)
        {
            GameObject go = Instantiate(epicComboPrefab, worldPos, Quaternion.identity);
            Destroy(go, 4f);
        }
        PulseBoardLight(true);
        CameraController.Instance?.Shake();
    }

    public void SpawnSmallParticle(Vector3 worldPos)
    {
        if (reducedFX) return;
        if (smallSparkPrefab)
        {
            GameObject go = Instantiate(smallSparkPrefab, worldPos, Quaternion.identity);
            Destroy(go, 1.5f);
        }
    }

    private void PulseBoardLight(bool epic)
    {
        if (boardLight == null) return;
        StartCoroutine(LightPulse(epic));
    }

    private IEnumerator LightPulse(bool epic)
    {
        float targetIntensity = epic ? boardLightComboIntensity : boardLightBaseIntensity * 2.5f;
        Color targetColor = comboLightColors.Length > 0
            ? comboLightColors[comboColorIndex % comboLightColors.Length]
            : Color.white;
        comboColorIndex++;

        float elapsed = 0f;
        float riseTime = 0.15f;
        float fallTime = epic ? 0.8f : 0.4f;

        Color startColor = boardLight.color;
        float startIntensity = boardLight.intensity;

        // Rise
        while (elapsed < riseTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / riseTime;
            boardLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            boardLight.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        // Fall
        elapsed = 0f;
        while (elapsed < fallTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallTime;
            boardLight.intensity = Mathf.Lerp(targetIntensity, boardLightBaseIntensity, t);
            boardLight.color = Color.Lerp(targetColor, startColor, t);
            yield return null;
        }

        boardLight.intensity = boardLightBaseIntensity;
        boardLight.color = startColor;
    }
}
