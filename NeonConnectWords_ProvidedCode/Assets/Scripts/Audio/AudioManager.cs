using System.Collections;
using UnityEngine;

/// <summary>
/// AudioManager: Handles all game audio — background synthwave music,
/// letter drop sounds, word completion chimes, power-up sounds, and cascades.
/// Music tempo dynamically adapts to player streaks.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // -----------------------------------------------------------------------
    // Inspector References
    // -----------------------------------------------------------------------
    [Header("Music")]
    public AudioSource musicSource;
    public AudioClip[] musicTracks;         // 0=classic loop, 1=timed/intense loop
    [Range(0f, 1f)] public float musicVolume = 0.6f;
    [Range(0.8f, 1.5f)] public float basePitch = 1f;

    [Header("SFX Sources")]
    public AudioSource sfxSource;
    public AudioSource uiSource;

    [Header("SFX Clips")]
    public AudioClip dropStartClip;
    public AudioClip letterLandClip;
    public AudioClip wordCompleteClip;
    public AudioClip multiWordClip;
    public AudioClip epicComboClip;
    public AudioClip cascadeClip;
    public AudioClip powerUpWildcardClip;
    public AudioClip powerUpBombClip;
    public AudioClip powerUpSwapClip;
    public AudioClip uiClickClip;
    public AudioClip uiHoverClip;
    public AudioClip swapClip;

    [Header("Chime Pitches")]
    public float[] chimeScalePitches = { 1f, 1.122f, 1.26f, 1.498f, 1.682f, 2f };  // major scale

    // -----------------------------------------------------------------------
    // Settings
    // -----------------------------------------------------------------------
    private bool musicEnabled = true;
    private bool sfxEnabled = true;
    private float currentPitch;
    private Coroutine tempoRoutine;

    // -----------------------------------------------------------------------
    // Unity Lifecycle
    // -----------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        currentPitch = basePitch;
    }

    // -----------------------------------------------------------------------
    // Music Control
    // -----------------------------------------------------------------------
    public void PlayMusic(bool intense = false)
    {
        if (!musicEnabled || !musicSource) return;

        AudioClip track = intense && musicTracks.Length > 1 ? musicTracks[1] : musicTracks[0];
        if (track != null)
        {
            musicSource.clip = track;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.pitch = basePitch;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicSource) musicSource.Stop();
    }

    public void RaiseTempo(int streakLevel)
    {
        if (tempoRoutine != null) StopCoroutine(tempoRoutine);
        float targetPitch = Mathf.Clamp(basePitch + streakLevel * 0.04f, basePitch, 1.4f);
        tempoRoutine = StartCoroutine(SmoothPitchShift(targetPitch, 0.8f));
    }

    public void ResetTempo()
    {
        if (tempoRoutine != null) StopCoroutine(tempoRoutine);
        tempoRoutine = StartCoroutine(SmoothPitchShift(basePitch, 1.5f));
    }

    private IEnumerator SmoothPitchShift(float targetPitch, float duration)
    {
        float startPitch = musicSource ? musicSource.pitch : basePitch;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Lerp(startPitch, targetPitch, elapsed / duration);
            if (musicSource) musicSource.pitch = p;
            yield return null;
        }
        if (musicSource) musicSource.pitch = targetPitch;
    }

    // -----------------------------------------------------------------------
    // SFX Playback
    // -----------------------------------------------------------------------

    public void PlayDropStart()
    {
        PlaySFX(dropStartClip, 0.7f, RandomPitch(0.9f, 1.1f));
    }

    /// <param name="heightNormalized">0=top drop, 1=bottom row. Higher = higher pitch.</param>
    public void PlayLetterLand(float heightNormalized)
    {
        float pitch = Mathf.Lerp(1.4f, 0.8f, heightNormalized);
        PlaySFX(letterLandClip, 0.9f, pitch);
    }

    public void PlayWordComplete(int wordLength, int wordCount)
    {
        if (!sfxEnabled || !sfxSource || !wordCompleteClip) return;

        // Play a chime at the pitch corresponding to word length
        int chimeIndex = Mathf.Clamp(wordLength - 3, 0, chimeScalePitches.Length - 1);
        sfxSource.pitch = chimeScalePitches[chimeIndex];
        sfxSource.PlayOneShot(wordCompleteClip, 0.85f);

        // Layer additional chime for multi-word
        if (wordCount > 1 && multiWordClip)
        {
            StartCoroutine(DelayedSFX(multiWordClip, 0.12f, 0.7f, chimeScalePitches[Mathf.Min(chimeIndex + 1, chimeScalePitches.Length - 1)]));
        }
    }

    public void PlayEpicCombo(int wordCount)
    {
        PlaySFX(epicComboClip, 1f, Mathf.Lerp(1f, 1.3f, wordCount / 5f));
    }

    public void PlayCascade()
    {
        PlaySFX(cascadeClip, 0.6f, RandomPitch(0.95f, 1.05f));
    }

    public void PlayPowerUp(PowerUpType type)
    {
        AudioClip clip = type switch
        {
            PowerUpType.Wildcard => powerUpWildcardClip,
            PowerUpType.Bomb => powerUpBombClip,
            PowerUpType.Swap => powerUpSwapClip,
            _ => null
        };
        PlaySFX(clip, 1f, 1f);
    }

    public void PlaySwap()
    {
        PlaySFX(swapClip, 0.8f, RandomPitch(0.9f, 1.1f));
    }

    public void PlayUIClick()
    {
        if (uiSource && uiClickClip) uiSource.PlayOneShot(uiClickClip, 0.5f);
    }

    public void PlayUIHover()
    {
        if (uiSource && uiHoverClip) uiSource.PlayOneShot(uiHoverClip, 0.3f);
    }

    // -----------------------------------------------------------------------
    // Settings
    // -----------------------------------------------------------------------
    public void SetMusicEnabled(bool on)
    {
        musicEnabled = on;
        if (musicSource) musicSource.mute = !on;
    }

    public void SetSFXEnabled(bool on)
    {
        sfxEnabled = on;
        if (sfxSource) sfxSource.mute = !on;
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------
    private void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (!sfxEnabled || !sfxSource || clip == null) return;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
    }

    private IEnumerator DelayedSFX(AudioClip clip, float delay, float volume, float pitch)
    {
        yield return new WaitForSeconds(delay);
        PlaySFX(clip, volume, pitch);
    }

    private float RandomPitch(float min, float max) => Random.Range(min, max);
}
