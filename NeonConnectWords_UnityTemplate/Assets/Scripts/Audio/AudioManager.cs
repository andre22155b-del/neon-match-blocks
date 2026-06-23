using UnityEngine;

/// <summary>
/// Central audio wrapper for music, gameplay SFX, and UI feedback.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource uiSource;

    [Header("Music")]
    public AudioClip classicMusic;
    public AudioClip timedMusic;

    [Header("Gameplay SFX")]
    public AudioClip dropClip;
    public AudioClip wordClip;
    public AudioClip comboClip;
    public AudioClip cascadeClip;
    public AudioClip bombClip;
    public AudioClip wildcardClip;
    public AudioClip swapClip;

    [Header("UI SFX")]
    public AudioClip clickClip;

    public void PlayMusic(bool intense)
    {
        if (musicSource == null)
        {
            return;
        }

        AudioClip chosen = intense && timedMusic != null ? timedMusic : classicMusic;
        if (chosen == null)
        {
            return;
        }

        if (musicSource.clip == chosen && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = chosen;
        musicSource.loop = true;
        musicSource.pitch = 1f;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void PlayDrop()
    {
        PlaySfx(dropClip, 0.85f, Random.Range(0.96f, 1.06f));
    }

    public void PlayCascade()
    {
        PlaySfx(cascadeClip, 0.7f, Random.Range(0.98f, 1.08f));
    }

    public void PlayWordClear(int wordCount, int cascadeDepth)
    {
        AudioClip clip = (wordCount > 1 || cascadeDepth > 1) && comboClip != null ? comboClip : wordClip;
        float pitch = Mathf.Lerp(1f, 1.28f, Mathf.Clamp01((wordCount + cascadeDepth - 2) / 4f));
        PlaySfx(clip, 0.95f, pitch);
    }

    public void PlayPowerUp(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Wildcard:
                PlaySfx(wildcardClip, 0.95f, 1f);
                break;
            case PowerUpType.Bomb:
                PlaySfx(bombClip, 1f, 1f);
                break;
            case PowerUpType.Swap:
                PlaySfx(swapClip, 0.9f, 1.08f);
                break;
        }
    }

    public void PlayUIClick()
    {
        if (uiSource != null && clickClip != null)
        {
            uiSource.PlayOneShot(clickClip, 0.65f);
        }
    }

    private void PlaySfx(AudioClip clip, float volume, float pitch)
    {
        if (sfxSource == null || clip == null)
        {
            return;
        }

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
    }
}
