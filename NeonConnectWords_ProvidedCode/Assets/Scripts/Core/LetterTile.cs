using System.Collections;
using UnityEngine;

/// <summary>
/// LetterTile: Represents a single letter tile in the 3D world.
/// Handles neon visuals, trails, highlighting, wildcard state, and removal animation.
/// Attach this to the Letter prefab.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class LetterTile : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Public State
    // -----------------------------------------------------------------------
    public char Letter { get; private set; }
    public int OwnerIndex { get; private set; } = -1;
    public bool IsWildcard { get; private set; } = false;

    // -----------------------------------------------------------------------
    // Inspector References
    // -----------------------------------------------------------------------
    [Header("Visuals")]
    public TextMesh letterLabel;       // 3D text mesh
    public Renderer tileRenderer;
    public Renderer glowRenderer;      // separate inner glow mesh (optional)
    public TrailRenderer trailRenderer;
    public ParticleSystem highlightParticles;
    public ParticleSystem removeParticles;

    [Header("Animation")]
    public float highlightPulseSpeed = 4f;
    public float highlightPulseMagnitude = 0.15f;
    public Color wildcardColor = Color.white;

    // -----------------------------------------------------------------------
    // Internal
    // -----------------------------------------------------------------------
    private Material tileMat;
    private Color baseEmission;
    private bool isPulsing = false;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    // -----------------------------------------------------------------------
    // Initialisation
    // -----------------------------------------------------------------------
    private void Awake()
    {
        if (tileRenderer == null) tileRenderer = GetComponent<Renderer>();
        tileMat = tileRenderer.material;  // instance material
        if (trailRenderer) trailRenderer.enabled = false;
    }

    // -----------------------------------------------------------------------
    // Setup API
    // -----------------------------------------------------------------------

    public void SetLetter(char c)
    {
        Letter = c;
        if (letterLabel) letterLabel.text = c.ToString();
    }

    public void SetOwner(int playerIndex, Material mat)
    {
        OwnerIndex = playerIndex;
        tileRenderer.material = mat;
        tileMat = tileRenderer.material;
        baseEmission = tileMat.GetColor(EmissionColorID);
    }

    public void SetPreviewMode(float alpha, int playerIndex)
    {
        Color c = tileRenderer.material.color;
        c.a = alpha;
        tileRenderer.material.color = c;
        // Disable collider so it doesn't interfere with raycasts
        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;
    }

    public void SetWildcard(bool value)
    {
        IsWildcard = value;
        if (value)
        {
            Letter = '*';
            if (letterLabel) letterLabel.text = "★";
            tileMat.SetColor(EmissionColorID, wildcardColor * 3f);
        }
    }

    // -----------------------------------------------------------------------
    // Trail
    // -----------------------------------------------------------------------
    public void EnableTrail(bool on)
    {
        if (trailRenderer) trailRenderer.enabled = on;
    }

    // -----------------------------------------------------------------------
    // Word Highlight
    // -----------------------------------------------------------------------
    public void PlayWordHighlight()
    {
        if (!isPulsing) StartCoroutine(PulseHighlight());
        if (highlightParticles) highlightParticles.Play();
    }

    private IEnumerator PulseHighlight()
    {
        isPulsing = true;
        float elapsed = 0f;
        float duration = 0.6f;
        Vector3 originalScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(elapsed * highlightPulseSpeed * Mathf.PI * 2f) * highlightPulseMagnitude;
            transform.localScale = originalScale * pulse;

            // Brighten emission
            float brightness = 2f + Mathf.Sin(elapsed * highlightPulseSpeed * Mathf.PI * 2f) * 1.5f;
            tileMat.SetColor(EmissionColorID, baseEmission * brightness);

            yield return null;
        }

        transform.localScale = originalScale;
        tileMat.SetColor(EmissionColorID, baseEmission);
        isPulsing = false;
    }

    // -----------------------------------------------------------------------
    // Remove Animation
    // -----------------------------------------------------------------------
    public void PlayRemoveAnimation()
    {
        StartCoroutine(RemoveRoutine());
    }

    private IEnumerator RemoveRoutine()
    {
        if (removeParticles) removeParticles.Play();

        float elapsed = 0f;
        float duration = 0.28f;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = startScale * (1f - EaseInQuad(t));
            Color c = tileMat.color;
            c.a = 1f - t;
            tileMat.color = c;
            yield return null;
        }
    }

    // -----------------------------------------------------------------------
    // Gentle Idle Bob (optional cosmetic)
    // -----------------------------------------------------------------------
    private float bobOffset;
    public void EnableIdleBob(float offset = 0f)
    {
        bobOffset = offset;
        StartCoroutine(IdleBob());
    }

    private IEnumerator IdleBob()
    {
        Vector3 basePos = transform.localPosition;
        while (true)
        {
            float y = Mathf.Sin((Time.time + bobOffset) * 1.5f) * 0.04f;
            transform.localPosition = basePos + Vector3.up * y;
            yield return null;
        }
    }

    // -----------------------------------------------------------------------
    // Easing
    // -----------------------------------------------------------------------
    private float EaseInQuad(float t) => t * t;
}
