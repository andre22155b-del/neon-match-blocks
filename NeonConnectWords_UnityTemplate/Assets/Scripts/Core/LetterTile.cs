using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Visual component for a single board tile.
/// </summary>
public class LetterTile : MonoBehaviour
{
    [Header("References")]
    public Transform visualRoot;
    public TMP_Text letterLabel;
    public MeshRenderer[] tintRenderers;
    public TrailRenderer trailRenderer;
    public ParticleSystem matchParticles;
    public ParticleSystem clearParticles;

    [Header("Animation")]
    public float bounceScale = 1.08f;
    public float bounceDuration = 0.12f;

    public char Letter { get; private set; } = 'A';
    public int OwnerIndex { get; private set; }
    public bool IsWildcard { get; private set; }

    private MaterialPropertyBlock propertyBlock;
    private Vector3 baseScale = Vector3.one;
    private bool previewMode;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        if (visualRoot != null)
        {
            baseScale = visualRoot.localScale;
        }

        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
        }
    }

    public void SetLetter(char letter)
    {
        Letter = char.ToUpperInvariant(letter);
        UpdateLabel();
    }

    public void SetOwner(int ownerIndex, Material materialOverride)
    {
        OwnerIndex = ownerIndex;

        if (tintRenderers != null)
        {
            for (int i = 0; i < tintRenderers.Length; i++)
            {
                if (tintRenderers[i] == null)
                {
                    continue;
                }

                if (materialOverride != null)
                {
                    tintRenderers[i].material = materialOverride;
                }
            }
        }

        ApplyTint(previewMode ? 0.35f : 1f);
        UpdateLabel();
    }

    public void SetWildcard(bool wildcard)
    {
        IsWildcard = wildcard;
        UpdateLabel();
        ApplyTint(previewMode ? 0.35f : 1f);
    }

    public void SetPreviewMode(float alpha)
    {
        previewMode = true;
        ApplyTint(alpha);
    }

    public void ClearPreviewMode()
    {
        previewMode = false;
        ApplyTint(1f);
    }

    public void EnableTrail(bool enabled)
    {
        if (trailRenderer != null)
        {
            trailRenderer.emitting = enabled;
        }
    }

    public IEnumerator PlayLandingBounce()
    {
        if (visualRoot == null)
        {
            yield break;
        }

        float elapsed = 0f;
        Vector3 expandedScale = baseScale * bounceScale;

        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / bounceDuration);
            visualRoot.localScale = Vector3.LerpUnclamped(baseScale, expandedScale, 1f - Mathf.Pow(1f - t, 2f));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / bounceDuration);
            visualRoot.localScale = Vector3.LerpUnclamped(expandedScale, baseScale, t);
            yield return null;
        }

        visualRoot.localScale = baseScale;
    }

    public void PlayMatchPulse()
    {
        if (matchParticles != null)
        {
            matchParticles.Play();
        }
    }

    public void PlayClearEffect()
    {
        if (clearParticles != null)
        {
            clearParticles.Play();
        }
    }

    private void UpdateLabel()
    {
        if (letterLabel == null)
        {
            return;
        }

        letterLabel.text = IsWildcard ? "*" : Letter.ToString();
        letterLabel.color = IsWildcard ? Color.white : (OwnerIndex == 0 ? new Color(0.35f, 1f, 1f) : new Color(1f, 0.35f, 0.9f));
    }

    private void ApplyTint(float alpha)
    {
        if (tintRenderers == null)
        {
            return;
        }

        Color baseColor = IsWildcard
            ? new Color(1f, 1f, 1f, alpha)
            : OwnerIndex == 0
                ? new Color(0.1f, 0.95f, 1f, alpha)
                : new Color(1f, 0.2f, 0.85f, alpha);

        Color emission = baseColor * (IsWildcard ? 3.5f : 2.5f);

        for (int i = 0; i < tintRenderers.Length; i++)
        {
            MeshRenderer renderer = tintRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", baseColor);
            propertyBlock.SetColor("_Color", baseColor);
            propertyBlock.SetColor("_EmissionColor", emission);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}
