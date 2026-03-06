using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Neon cube block logic.
/// Setup (Prefab):
/// 1) Add Collider on root.
/// 2) Assign visualRoot (the object to animate).
/// 3) Assign faceRoot (child object containing face sprite/model) and keep it hidden by default.
/// 4) Optional: assign faceSpriteRenderer OR faceMeshRenderer for face visuals.
/// 5) Optional: assign edgeGlowRenderer / glowLight for rarity glow.
/// </summary>
public enum BlockRarity
{
    Normal = 0,
    Rare = 1,
    Legendary = 2
}

public class Block : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private GameObject faceRoot;
    [SerializeField] private SpriteRenderer faceSpriteRenderer;
    [SerializeField] private MeshRenderer faceMeshRenderer;
    [SerializeField] private Renderer edgeGlowRenderer;
    [SerializeField] private Light glowLight;

    [Header("Flip / Hover Animation")]
    [SerializeField] private float liftAmount = 0.24f;
    [SerializeField] private float flipDuration = 0.28f;
    [SerializeField] private float hoverAmplitude = 0.05f;
    [SerializeField] private float hoverDuration = 1.1f;

    [Header("Rarity Glow")]
    [SerializeField] private Color normalGlow = new Color(0.0f, 0.95f, 1f);
    [SerializeField] private Color rareGlow = new Color(0.1f, 1f, 0.45f);
    [SerializeField] private Color legendaryGlow = new Color(1f, 0.62f, 0.15f);

    [Header("Score Bonus (Pair)")]
    [SerializeField] private int rarePairBonus = 75;
    [SerializeField] private int legendaryPairBonus = 200;

    public int FaceId { get; private set; }
    public BlockRarity Rarity { get; private set; }
    public bool IsMatched { get; private set; }
    public bool IsRevealed { get; private set; }
    public bool IsAnimating { get; private set; }

    private GameManagerCompetitive manager;
    private Sequence flipSequence;
    private Tween hoverTween;
    private Tween glowTween;
    private Vector3 baseLocalPos;
    private float baseLightIntensity;
    private MaterialPropertyBlock glowProps;

    private void Awake()
    {
        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        baseLocalPos = visualRoot.localPosition;

        if (glowLight != null)
        {
            baseLightIntensity = glowLight.intensity;
        }
    }

    public void Setup(int faceId, Sprite faceSprite, BlockRarity rarity, GameManagerCompetitive owner)
    {
        FaceId = faceId;
        Rarity = rarity;
        manager = owner;

        IsMatched = false;
        IsRevealed = false;
        IsAnimating = false;

        if (faceRoot != null)
        {
            faceRoot.SetActive(false);
        }

        ApplyFace(faceSprite);
        ApplyRarityVisuals(rarity);
        StartIdleHover();
    }

    private void OnMouseDown()
    {
        manager?.TrySelectBlock(this);
    }

    public void Reveal(Action onComplete = null)
    {
        if (IsMatched || IsRevealed || IsAnimating)
        {
            return;
        }

        IsAnimating = true;
        SoundManager.Instance?.Play("Flip");

        KillFlip();
        flipSequence = DOTween.Sequence();

        flipSequence.Join(visualRoot.DOLocalMoveY(baseLocalPos.y + liftAmount, flipDuration * 0.55f).SetEase(Ease.OutQuad));
        flipSequence.Join(visualRoot.DOLocalRotate(
            new Vector3(0f, visualRoot.localEulerAngles.y + 180f, 0f),
            flipDuration,
            RotateMode.FastBeyond360).SetEase(Ease.InOutSine));

        flipSequence.InsertCallback(flipDuration * 0.45f, delegate
        {
            if (faceRoot != null)
            {
                faceRoot.SetActive(true);
            }
        });

        flipSequence.Append(visualRoot.DOLocalMoveY(baseLocalPos.y, flipDuration * 0.35f).SetEase(Ease.OutBounce));
        flipSequence.OnComplete(delegate
        {
            IsRevealed = true;
            IsAnimating = false;
            onComplete?.Invoke();
        });
    }

    public void Hide(Action onComplete = null)
    {
        if (IsMatched || !IsRevealed || IsAnimating)
        {
            return;
        }

        IsAnimating = true;
        SoundManager.Instance?.Play("Flip", 0.95f);

        KillFlip();
        flipSequence = DOTween.Sequence();

        flipSequence.Join(visualRoot.DOLocalMoveY(baseLocalPos.y + liftAmount * 0.65f, flipDuration * 0.45f).SetEase(Ease.OutQuad));
        flipSequence.Join(visualRoot.DOLocalRotate(
            new Vector3(0f, visualRoot.localEulerAngles.y + 180f, 0f),
            flipDuration,
            RotateMode.FastBeyond360).SetEase(Ease.InOutSine));

        flipSequence.InsertCallback(flipDuration * 0.45f, delegate
        {
            if (faceRoot != null)
            {
                faceRoot.SetActive(false);
            }
        });

        flipSequence.Append(visualRoot.DOLocalMoveY(baseLocalPos.y, flipDuration * 0.35f).SetEase(Ease.OutSine));
        flipSequence.OnComplete(delegate
        {
            IsRevealed = false;
            IsAnimating = false;
            onComplete?.Invoke();
        });
    }

    public void SetMatched()
    {
        IsMatched = true;
        IsRevealed = true;

        if (faceRoot != null)
        {
            faceRoot.SetActive(true);
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        KillIdleTweens();
        visualRoot.DOPunchScale(Vector3.one * 0.12f, 0.22f, 14, 0.8f);
    }

    public void PlayWrongFeedback()
    {
        visualRoot.DOPunchPosition(new Vector3(0.08f, 0f, 0f), 0.25f, 18, 0.8f);
    }

    public int GetRarityPairBonus()
    {
        if (Rarity == BlockRarity.Rare)
        {
            return rarePairBonus;
        }

        if (Rarity == BlockRarity.Legendary)
        {
            return legendaryPairBonus;
        }

        return 0;
    }

    private void ApplyFace(Sprite faceSprite)
    {
        if (faceSpriteRenderer != null)
        {
            faceSpriteRenderer.sprite = faceSprite;
        }

        if (faceMeshRenderer != null && faceSprite != null)
        {
            Material mat = faceMeshRenderer.material;
            if (mat != null && mat.HasProperty("_MainTex"))
            {
                mat.mainTexture = faceSprite.texture;
            }
        }
    }

    private void ApplyRarityVisuals(BlockRarity rarity)
    {
        Color glow = normalGlow;
        float intensity = 1.1f;
        float scale = 1f;

        if (rarity == BlockRarity.Rare)
        {
            glow = rareGlow;
            intensity = 1.35f;
            scale = 1.03f;
        }
        else if (rarity == BlockRarity.Legendary)
        {
            glow = legendaryGlow;
            intensity = 1.7f;
            scale = 1.07f;
        }

        if (edgeGlowRenderer != null)
        {
            if (glowProps == null)
            {
                glowProps = new MaterialPropertyBlock();
            }

            edgeGlowRenderer.GetPropertyBlock(glowProps);
            glowProps.SetColor("_EmissionColor", glow * intensity);
            glowProps.SetColor("_BaseColor", glow);
            edgeGlowRenderer.SetPropertyBlock(glowProps);
        }

        if (glowLight != null)
        {
            glowLight.color = glow;
            glowLight.intensity = baseLightIntensity * intensity;
        }

        visualRoot.localScale = Vector3.one * scale;
    }

    private void StartIdleHover()
    {
        KillIdleTweens();

        hoverTween = visualRoot.DOLocalMoveY(baseLocalPos.y + hoverAmplitude, hoverDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        if (glowLight != null)
        {
            float target = glowLight.intensity * 1.15f;
            glowTween = DOTween.To(() => glowLight.intensity, x => glowLight.intensity = x, target, hoverDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    private void KillFlip()
    {
        if (flipSequence != null && flipSequence.IsActive())
        {
            flipSequence.Kill();
        }
    }

    private void KillIdleTweens()
    {
        if (hoverTween != null && hoverTween.IsActive())
        {
            hoverTween.Kill();
        }

        if (glowTween != null && glowTween.IsActive())
        {
            glowTween.Kill();
        }
    }

    private void OnDestroy()
    {
        KillFlip();
        KillIdleTweens();
    }
}
