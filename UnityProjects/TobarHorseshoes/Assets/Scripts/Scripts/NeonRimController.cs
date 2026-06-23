using System.Collections;
using UnityEngine;

public class NeonRimController : MonoBehaviour
{
    [Header("Material")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private string emissionColorProperty = "_EmissionColor";
    [SerializeField] private Color neonColor = new Color(1f, 0.3f, 0f);

    [Header("Pulse")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float baseIntensity = 2.5f;
    [SerializeField] private float pulseAmplitude = 0.5f;

    [Header("Score Flash")]
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float flashIntensity = 10f;

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private MaterialPropertyBlock propertyBlock;
    private Coroutine flashRoutine;
    private bool isFlashing;
    private float flashTimer;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        propertyBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (targetRenderer == null)
        {
            return;
        }

        if (isFlashing)
        {
            flashTimer += Time.deltaTime;
            if (flashTimer >= flashDuration)
            {
                isFlashing = false;
                flashTimer = 0f;
            }
        }

        float pulse = baseIntensity + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
        float intensity = isFlashing ? flashIntensity : pulse;
        Color emission = (isFlashing ? Color.white : neonColor) * Mathf.LinearToGammaSpace(Mathf.Max(0f, intensity));
        ApplyEmission(emission);
    }

    public void FlashOnScore()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashEffect());
    }

    private IEnumerator FlashEffect()
    {
        isFlashing = true;
        flashTimer = 0f;
        yield return new WaitForSeconds(flashDuration);
        isFlashing = false;
        flashTimer = 0f;
        flashRoutine = null;
    }

    private void ApplyEmission(Color emissionColor)
    {
        targetRenderer.GetPropertyBlock(propertyBlock);
        int propId = emissionColorProperty == "_EmissionColor"
            ? EmissionColorId
            : Shader.PropertyToID(emissionColorProperty);
        propertyBlock.SetColor(propId, emissionColor);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
