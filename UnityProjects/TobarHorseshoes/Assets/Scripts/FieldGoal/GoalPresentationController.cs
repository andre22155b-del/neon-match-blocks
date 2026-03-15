using UnityEngine;

public class GoalPresentationController : MonoBehaviour
{
    [Header("References")]
    public NeonFieldGoalConfig config;
    public NeonFieldGoalUI ui;
    public AudioSource sfxSource;
    public AudioSource crowdSource;
    public AudioSource ambientSource;

    [Header("Glow")]
    public Renderer[] goalRenderers;
    public Renderer[] endZoneRenderers;
    public Light[] crowdLights;
    public string emissionProperty = "_EmissionColor";
    public Color baseGlowColor = new Color(0.14f, 0.95f, 1f);
    public Color scoreGlowColor = new Color(1f, 0.62f, 0.18f);
    public float pulseSpeed = 2.4f;
    public float pulseAmplitude = 0.35f;

    private MaterialPropertyBlock propertyBlock;
    private int emissionPropertyId;
    private float[] baseCrowdIntensities;
    private float flashTimer;
    private float kickFlashTimer;
    private float crowdBoostTimer;
    private float finalDriveTimer;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        emissionPropertyId = Shader.PropertyToID(emissionProperty);

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
        }

        if (crowdSource == null)
        {
            crowdSource = gameObject.AddComponent<AudioSource>();
            crowdSource.playOnAwake = false;
            crowdSource.spatialBlend = 0f;
        }

        if (ambientSource == null)
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.spatialBlend = 0f;
            ambientSource.loop = true;
        }

        baseCrowdIntensities = new float[crowdLights == null ? 0 : crowdLights.Length];
        for (int i = 0; i < baseCrowdIntensities.Length; i++)
        {
            baseCrowdIntensities[i] = crowdLights[i] != null ? crowdLights[i].intensity : 1f;
        }
    }

    public void ApplyConfig(NeonFieldGoalConfig fieldGoalConfig)
    {
        config = fieldGoalConfig;
        if (ambientSource != null)
        {
            ambientSource.loop = true;
            ambientSource.clip = ResolveAmbientClip();
            ambientSource.volume = config != null ? config.ambientVolume : 0.3f;
        }
    }

    public void PlayAmbient()
    {
        if (ambientSource == null)
        {
            return;
        }

        if (ambientSource.clip == null)
        {
            ambientSource.clip = ResolveAmbientClip();
        }

        if (!ambientSource.isPlaying && ambientSource.clip != null)
        {
            ambientSource.volume = config != null ? config.ambientVolume : 0.3f;
            ambientSource.Play();
        }
    }

    public void StopAmbient()
    {
        if (ambientSource != null && ambientSource.isPlaying)
        {
            ambientSource.Stop();
        }
    }

    public void PlayKick()
    {
        kickFlashTimer = config != null ? config.kickGlowDuration : 0.16f;
        PlaySfx(config != null ? config.kickClip : null, FieldGoalAudioFactory.GetKickClip(), 0.86f);
    }

    public void PlayGoalCelebration()
    {
        flashTimer = config != null ? config.goalLightFlashDuration : 0.5f;
        crowdBoostTimer = 1.1f;
        PlaySfx(config != null ? config.goalClip : null, FieldGoalAudioFactory.GetGoalClip(), 1f);
        PlayCrowd(config != null ? config.crowdClip : null, FieldGoalAudioFactory.GetCrowdClip(), 1f);

        if (ui != null)
        {
            ui.PulseGoals();
            ui.PulseStatus();
        }
    }

    public void PlayMissFeedback()
    {
        PlaySfx(config != null ? config.missClip : null, FieldGoalAudioFactory.GetMissClip(), 0.8f);
        if (ui != null)
        {
            ui.PulseStatus();
        }
    }

    public void PlayFinalDrive()
    {
        finalDriveTimer = 3.1f;
        PlayCrowd(config != null ? config.crowdClip : null, FieldGoalAudioFactory.GetCrowdClip(), 0.5f);
    }

    public void ResetPresentation()
    {
        flashTimer = 0f;
        kickFlashTimer = 0f;
        crowdBoostTimer = 0f;
        finalDriveTimer = 0f;
    }

    private void Update()
    {
        if (kickFlashTimer > 0f)
        {
            kickFlashTimer -= Time.deltaTime;
        }

        if (crowdBoostTimer > 0f)
        {
            crowdBoostTimer -= Time.deltaTime;
        }

        if (finalDriveTimer > 0f)
        {
            finalDriveTimer -= Time.deltaTime;
        }

        float baseIntensity = config != null ? config.goalLightBaseIntensity : 1.8f;
        float flashIntensity = config != null ? config.goalLightFlashIntensity : 8.5f;
        float pulse = baseIntensity + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
        float activeIntensity = pulse;
        Color activeColor = baseGlowColor;
        if (kickFlashTimer > 0f)
        {
            float kickDuration = Mathf.Max(0.01f, config != null ? config.kickGlowDuration : 0.16f);
            float kickBlend = Mathf.Sin((1f - Mathf.Clamp01(kickFlashTimer / kickDuration)) * Mathf.PI);
            activeIntensity += kickBlend * (config != null ? config.kickGlowBoost : 0.7f);
        }

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            float flashBlend = Mathf.Clamp01(flashTimer / Mathf.Max(0.01f, config != null ? config.goalLightFlashDuration : 0.5f));
            activeIntensity = Mathf.Lerp(pulse, flashIntensity, flashBlend);
            activeColor = Color.Lerp(baseGlowColor, scoreGlowColor, flashBlend);
        }

        ApplyEmission(goalRenderers, activeColor, activeIntensity);
        ApplyEmission(endZoneRenderers, activeColor, activeIntensity * 0.85f);
        UpdateCrowdLights(flashTimer > 0f, crowdBoostTimer > 0f, finalDriveTimer > 0f);
    }

    private void ApplyEmission(Renderer[] renderers, Color color, float intensity)
    {
        if (renderers == null)
        {
            return;
        }

        Color emission = color * Mathf.LinearToGammaSpace(Mathf.Max(0f, intensity));
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rendererRef = renderers[i];
            if (rendererRef == null)
            {
                continue;
            }

            rendererRef.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(emissionPropertyId, emission);
            rendererRef.SetPropertyBlock(propertyBlock);
        }
    }

    private void UpdateCrowdLights(bool scored, bool crowdBoosted, bool finalDriveBoosted)
    {
        if (crowdLights == null)
        {
            return;
        }

        for (int i = 0; i < crowdLights.Length; i++)
        {
            Light crowdLight = crowdLights[i];
            if (crowdLight == null)
            {
                continue;
            }

            float baseIntensity = i < baseCrowdIntensities.Length ? baseCrowdIntensities[i] : 1f;
            float idleIntensity = baseIntensity * (0.88f + 0.18f * Mathf.Sin(Time.time * 1.6f + i * 0.8f));
            float boostedIntensity = idleIntensity;

            if (finalDriveBoosted)
            {
                boostedIntensity = Mathf.Max(
                    boostedIntensity,
                    baseIntensity * (config != null ? config.finalDriveCrowdBoostMultiplier : 1.95f) *
                    (0.92f + Mathf.Abs(Mathf.Sin(Time.time * 6f + i)) * 0.18f));
            }

            if (crowdBoosted || scored)
            {
                float strobe = 0.94f + Mathf.Abs(Mathf.Sin(Time.time * 18f + i * 0.9f)) * 0.36f;
                boostedIntensity = Mathf.Max(
                    boostedIntensity,
                    baseIntensity * (config != null ? config.crowdBoostMultiplier : 2.25f) * strobe);
            }

            crowdLight.intensity = boostedIntensity;
        }
    }

    private void PlaySfx(AudioClip preferred, AudioClip fallback, float volumeScale)
    {
        if (sfxSource == null)
        {
            return;
        }

        AudioClip clip = preferred != null ? preferred : fallback;
        if (clip == null)
        {
            return;
        }

        float volume = (config != null ? config.masterSfxVolume : 1f) * volumeScale;
        sfxSource.PlayOneShot(clip, volume);
    }

    private void PlayCrowd(AudioClip preferred, AudioClip fallback, float volumeScale)
    {
        if (crowdSource == null)
        {
            return;
        }

        AudioClip clip = preferred != null ? preferred : fallback;
        if (clip == null)
        {
            return;
        }

        float volume = (config != null ? config.crowdVolume : 0.8f) * volumeScale;
        crowdSource.PlayOneShot(clip, volume);
    }

    private AudioClip ResolveAmbientClip()
    {
        return config != null && config.ambientLoopClip != null
            ? config.ambientLoopClip
            : FieldGoalAudioFactory.GetAmbientClip();
    }
}
