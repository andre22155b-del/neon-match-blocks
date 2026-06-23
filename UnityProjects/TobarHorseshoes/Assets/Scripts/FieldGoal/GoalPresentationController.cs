using UnityEngine;

public class GoalPresentationController : MonoBehaviour
{
    [Header("References")]
    public NeonFieldGoalConfig config;
    public NeonFieldGoalUI ui;
    public AudioSource sfxSource;
    public AudioSource crowdSource;
    public AudioSource ambientSource;
    public AudioSource announcerSource;

    [Header("Glow")]
    public Renderer[] goalRenderers;
    public Renderer[] endZoneRenderers;
    public Light[] crowdLights;
    public string emissionProperty = "_EmissionColor";
    public Color baseGlowColor = new Color(0.22f, 1f, 0.96f);
    public Color scoreGlowColor = new Color(1f, 0.72f, 0.24f);
    public float pulseSpeed = 2.4f;
    public float pulseAmplitude = 0.42f;

    private MaterialPropertyBlock propertyBlock;
    private int emissionPropertyId;
    private float[] baseCrowdIntensities;
    private float flashTimer;
    private float kickFlashTimer;
    private float crowdBoostTimer;
    private float finalDriveTimer;
    private float ambientTargetVolume;
    private float currentCrowdDuckScale = 1f;
    private float targetCrowdDuckScale = 1f;
    private float currentAmbientDuckScale = 1f;
    private float targetAmbientDuckScale = 1f;
    private float cueElapsedTime = 999f;
    private float cueDuckTimer;
    private FieldGoalAudioCue activeCue;
    private int currentStreak;
    private int currentMultiplier = 1;
    private int currentYardLine = 20;
    private float currentWindStrength;
    private bool currentClutchActive;
    private bool currentMovingGoalLive;

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

        if (announcerSource == null)
        {
            announcerSource = gameObject.AddComponent<AudioSource>();
            announcerSource.playOnAwake = false;
            announcerSource.spatialBlend = 0f;
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
        ambientTargetVolume = config != null ? config.ambientVolume : 0.3f;
        if (ambientSource != null)
        {
            ambientSource.loop = true;
            ambientSource.clip = ResolveAmbientClip();
            ambientSource.volume = ambientTargetVolume;
        }
    }

    public void SetRunIntensity(int streak, int multiplier, bool clutchActive, bool movingGoalLive, int yardLine, float windStrength01)
    {
        currentStreak = Mathf.Max(0, streak);
        currentMultiplier = Mathf.Max(1, multiplier);
        currentYardLine = Mathf.Max(1, yardLine);
        currentWindStrength = Mathf.Clamp01(windStrength01);
        currentClutchActive = clutchActive;
        currentMovingGoalLive = movingGoalLive;
        float baseAmbientVolume = config != null ? config.ambientVolume : 0.3f;
        float ambientScale = FieldGoalAudioIdentity.GetAmbientScale(clutchActive, currentMultiplier, movingGoalLive);
        ambientScale += FieldGoalWindMath.GetDistancePressure01(config, currentYardLine) * 0.08f;
        ambientScale += currentWindStrength * 0.06f;
        float ambientCap = config != null ? Mathf.Max(1f, config.ambientMaxMixScale) : 1.24f;
        ambientTargetVolume = baseAmbientVolume * Mathf.Min(ambientScale, ambientCap);
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
            ambientSource.volume = ambientTargetVolume;
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

    public void PlayKick(bool longBombLine, bool perfectKick, bool clutchActive)
    {
        kickFlashTimer = config != null ? config.kickGlowDuration : 0.16f;
        PlaySfx(
            config != null ? config.kickClip : null,
            FieldGoalAudioFactory.GetKickClip(),
            FieldGoalAudioIdentity.GetKickSfxScale(longBombLine, perfectKick, clutchActive));
    }

    public void PlayGoalCelebration(FieldGoalScoreResult scoreResult, int streak)
    {
        float flashDuration = config != null ? config.goalLightFlashDuration : 0.5f;
        float crowdScale = FieldGoalAudioIdentity.GetGoalCrowdScale(scoreResult, streak);
        flashTimer = flashDuration * (scoreResult.LongBomb ? 1.22f : 1f);
        crowdBoostTimer = Mathf.Max(crowdBoostTimer, scoreResult.LongBomb ? 1.45f : 1.1f);
        PlaySfx(config != null ? config.goalClip : null, FieldGoalAudioFactory.GetGoalClip(), 1f);
        PlayCue(FieldGoalAudioIdentity.GetGoalCue(scoreResult));
        PlayCrowd(config != null ? config.crowdClip : null, FieldGoalAudioFactory.GetCrowdClip(), crowdScale);

        if (ui != null)
        {
            ui.PulseGoals();
            ui.PulseStatus();
        }
    }

    public void PlayMissFeedback(bool clutchActive, bool longBombLine, bool brokeHeat)
    {
        PlaySfx(config != null ? config.missClip : null, FieldGoalAudioFactory.GetMissClip(), 0.8f);
        PlayCue(FieldGoalAudioIdentity.GetMissCue(GoalCrossingMissType.None, longBombLine, clutchActive, brokeHeat));
        if (ui != null)
        {
            ui.PulseStatus();
        }
    }

    public void PlayNearMissFeedback(GoalCrossingMissType missType, bool longBombLine, bool clutchActive)
    {
        kickFlashTimer = Mathf.Max(kickFlashTimer, 0.12f);
        crowdBoostTimer = Mathf.Max(crowdBoostTimer, 0.32f);
        PlaySfx(config != null ? config.missClip : null, FieldGoalAudioFactory.GetMissClip(), 0.5f);
        PlayCue(FieldGoalAudioIdentity.GetMissCue(missType, longBombLine, clutchActive, false));
        PlayCrowd(
            config != null ? config.crowdClip : null,
            FieldGoalAudioFactory.GetCrowdClip(),
            FieldGoalAudioIdentity.GetNearMissCrowdScale(longBombLine, clutchActive));
        if (ui != null)
        {
            ui.PulseStatus();
        }
    }

    public void PlayFinalDrive()
    {
        finalDriveTimer = 3.1f;
        PlayCue(FieldGoalAudioCue.FinalDrive);
        PlayCrowd(config != null ? config.crowdClip : null, FieldGoalAudioFactory.GetCrowdClip(), 0.65f);
    }

    public void PlayMovingGoalActivated()
    {
        PlayCue(FieldGoalAudioCue.MovingGoal);
        PlayCrowd(config != null ? config.crowdClip : null, FieldGoalAudioFactory.GetCrowdClip(), 0.46f);
    }

    public void ResetPresentation()
    {
        flashTimer = 0f;
        kickFlashTimer = 0f;
        crowdBoostTimer = 0f;
        finalDriveTimer = 0f;
        ambientTargetVolume = config != null ? config.ambientVolume : 0.3f;
        currentCrowdDuckScale = 1f;
        targetCrowdDuckScale = 1f;
        currentAmbientDuckScale = 1f;
        targetAmbientDuckScale = 1f;
        cueElapsedTime = 999f;
        cueDuckTimer = 0f;
        activeCue = FieldGoalAudioCue.None;
        currentYardLine = config != null ? Mathf.Max(1, config.startingYardLine) : 20;
        currentWindStrength = 0f;
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;

        if (kickFlashTimer > 0f)
        {
            kickFlashTimer -= deltaTime;
        }

        if (crowdBoostTimer > 0f)
        {
            crowdBoostTimer -= deltaTime;
        }

        if (finalDriveTimer > 0f)
        {
            finalDriveTimer -= deltaTime;
        }

        cueElapsedTime += deltaTime;
        if (cueDuckTimer > 0f)
        {
            cueDuckTimer -= deltaTime;
            if (cueDuckTimer <= 0f)
            {
                targetCrowdDuckScale = 1f;
                targetAmbientDuckScale = 1f;
                activeCue = FieldGoalAudioCue.None;
            }
        }

        float duckRecoverSharpness = config != null ? Mathf.Max(1f, config.audioDuckRecoverSharpness) : 8.5f;
        float duckBlend = 1f - Mathf.Exp(-duckRecoverSharpness * deltaTime);
        currentCrowdDuckScale = Mathf.Lerp(currentCrowdDuckScale, targetCrowdDuckScale, duckBlend);
        currentAmbientDuckScale = Mathf.Lerp(currentAmbientDuckScale, targetAmbientDuckScale, duckBlend);

        if (ambientSource != null)
        {
            float blend = 1f - Mathf.Exp(-3.2f * deltaTime);
            ambientSource.volume = Mathf.Lerp(ambientSource.volume, ambientTargetVolume * currentAmbientDuckScale, blend);
        }

        float baseIntensity = config != null ? config.goalLightBaseIntensity : 1.8f;
        float flashIntensity = config != null ? config.goalLightFlashIntensity : 8.5f;
        float distancePressure = FieldGoalWindMath.GetDistancePressure01(config, currentYardLine);
        float comboPressure = Mathf.Clamp(currentStreak - 1, 0, 5) * 0.09f + Mathf.Clamp(currentMultiplier - 1, 0, 4) * 0.14f;
        float livePulseAmplitude = pulseAmplitude + distancePressure * 0.16f + currentWindStrength * 0.08f;
        float pulse = baseIntensity +
            distancePressure * 0.82f +
            currentWindStrength * 0.25f +
            comboPressure +
            Mathf.Sin(time * pulseSpeed) * livePulseAmplitude;
        if (currentMovingGoalLive)
        {
            pulse += 0.18f;
        }

        if (currentClutchActive)
        {
            pulse += 0.2f;
        }

        float activeIntensity = pulse;
        Color activeColor = Color.Lerp(baseGlowColor, scoreGlowColor, distancePressure * 0.26f + currentWindStrength * 0.1f + comboPressure * 0.08f);
        if (kickFlashTimer > 0f)
        {
            float kickDuration = Mathf.Max(0.01f, config != null ? config.kickGlowDuration : 0.16f);
            float kickBlend = Mathf.Sin((1f - Mathf.Clamp01(kickFlashTimer / kickDuration)) * Mathf.PI);
            activeIntensity += kickBlend * ((config != null ? config.kickGlowBoost : 0.7f) + 0.55f);
            activeColor = Color.Lerp(activeColor, Color.white, kickBlend * 0.22f);
        }

        if (flashTimer > 0f)
        {
            flashTimer -= deltaTime;
            float flashBlend = Mathf.Clamp01(flashTimer / Mathf.Max(0.01f, config != null ? config.goalLightFlashDuration : 0.5f));
            activeIntensity = Mathf.Lerp(pulse, flashIntensity * (1f + comboPressure * 0.08f), flashBlend);
            activeColor = Color.Lerp(activeColor, Color.Lerp(scoreGlowColor, Color.white, 0.28f), flashBlend);
        }

        ApplyEmission(goalRenderers, activeColor, activeIntensity);
        ApplyEmission(endZoneRenderers, Color.Lerp(activeColor, scoreGlowColor, 0.16f), activeIntensity * 1.05f);
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
            float distancePressure = FieldGoalWindMath.GetDistancePressure01(config, currentYardLine);
            float idleWave = 0.88f + 0.18f * Mathf.Sin(Time.unscaledTime * 1.6f + i * 0.8f);
            idleWave += Mathf.Clamp(currentMultiplier - 1, 0, 4) * 0.03f;
            idleWave += Mathf.Clamp(currentStreak - 1, 0, 4) * 0.015f;
            idleWave += distancePressure * 0.12f;
            idleWave += currentWindStrength * 0.08f;
            if (currentMovingGoalLive)
            {
                idleWave += 0.05f;
            }

            if (currentClutchActive)
            {
                idleWave += 0.04f;
            }

            float idleIntensity = baseIntensity * idleWave;
            float boostedIntensity = idleIntensity;

            if (finalDriveBoosted)
            {
                boostedIntensity = Mathf.Max(
                    boostedIntensity,
                    baseIntensity * (config != null ? config.finalDriveCrowdBoostMultiplier : 1.95f) *
                    (0.92f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f + i)) * 0.18f));
            }

            if (crowdBoosted || scored)
            {
                float strobe = 0.94f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 18f + i * 0.9f)) * 0.36f;
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
        crowdSource.PlayOneShot(clip, volume * currentCrowdDuckScale);
    }

    private void PlayCue(FieldGoalAudioCue cue)
    {
        if (cue == FieldGoalAudioCue.None || announcerSource == null)
        {
            return;
        }

        float baseCooldown = config != null ? config.announcerCueGapSeconds : 0.18f;
        if (!FieldGoalAudioIdentity.ShouldPlayCue(cue, activeCue, cueElapsedTime, baseCooldown))
        {
            return;
        }

        AudioClip clip = ResolveCueClip(cue);
        if (clip == null)
        {
            return;
        }

        float crowdDuckFloor = config != null ? config.crowdDuckUnderAnnouncer : 0.82f;
        float ambientDuckFloor = config != null ? config.ambientDuckUnderAnnouncer : 0.68f;
        float duckIntensity = FieldGoalAudioIdentity.GetCueDuckIntensity(cue);
        float crowdDuckScale = Mathf.Lerp(1f, crowdDuckFloor, duckIntensity);
        float ambientDuckScale = Mathf.Lerp(1f, ambientDuckFloor, duckIntensity);
        currentCrowdDuckScale = Mathf.Min(currentCrowdDuckScale, crowdDuckScale);
        currentAmbientDuckScale = Mathf.Min(currentAmbientDuckScale, ambientDuckScale);
        targetCrowdDuckScale = crowdDuckScale;
        targetAmbientDuckScale = ambientDuckScale;
        cueDuckTimer = Mathf.Max(baseCooldown, FieldGoalAudioIdentity.GetCueCooldown(cue));
        cueElapsedTime = 0f;
        activeCue = cue;

        float volume = (config != null ? config.announcerVolume : 0.9f) * FieldGoalAudioIdentity.GetCueVolumeScale(cue);
        announcerSource.PlayOneShot(clip, volume);
    }

    private AudioClip ResolveCueClip(FieldGoalAudioCue cue)
    {
        switch (cue)
        {
            case FieldGoalAudioCue.Perfect:
                return config != null && config.perfectKickStingClip != null
                    ? config.perfectKickStingClip
                    : FieldGoalAudioFactory.GetCueClip(cue);
            case FieldGoalAudioCue.LongBomb:
                return config != null && config.longBombStingClip != null
                    ? config.longBombStingClip
                    : FieldGoalAudioFactory.GetCueClip(cue);
            case FieldGoalAudioCue.Heat:
                return config != null && config.heatStingClip != null
                    ? config.heatStingClip
                    : FieldGoalAudioFactory.GetCueClip(cue);
            case FieldGoalAudioCue.Clutch:
                return config != null && config.clutchStingClip != null
                    ? config.clutchStingClip
                    : FieldGoalAudioFactory.GetCueClip(cue);
            case FieldGoalAudioCue.NearMiss:
                return config != null && config.nearMissStingClip != null
                    ? config.nearMissStingClip
                    : FieldGoalAudioFactory.GetCueClip(cue);
            case FieldGoalAudioCue.StreakBreak:
                return config != null && config.streakBreakStingClip != null
                    ? config.streakBreakStingClip
                    : FieldGoalAudioFactory.GetCueClip(cue);
            case FieldGoalAudioCue.FinalDrive:
                return config != null && config.finalDriveStingClip != null
                    ? config.finalDriveStingClip
                    : FieldGoalAudioFactory.GetCueClip(cue);
            case FieldGoalAudioCue.MovingGoal:
                return config != null && config.movingGoalStingClip != null
                    ? config.movingGoalStingClip
                    : FieldGoalAudioFactory.GetCueClip(cue);
            default:
                return null;
        }
    }

    private AudioClip ResolveAmbientClip()
    {
        return config != null && config.ambientLoopClip != null
            ? config.ambientLoopClip
            : FieldGoalAudioFactory.GetAmbientClip();
    }
}
