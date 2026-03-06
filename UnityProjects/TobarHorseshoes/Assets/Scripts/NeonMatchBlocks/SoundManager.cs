using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global SFX manager.
/// Setup:
/// 1) Add this script to a scene object.
/// 2) Assign an AudioSource (2D recommended for arcade feedback).
/// 3) Add clip entries with names:
///    Flip, Match, Wrong, Combo, LevelComplete, Countdown, Tap
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [System.Serializable]
    public class ClipEntry
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private List<ClipEntry> clips = new List<ClipEntry>();

    private readonly Dictionary<string, ClipEntry> clipMap = new Dictionary<string, ClipEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake = false;
        RebuildMap();
    }

    public void RebuildMap()
    {
        clipMap.Clear();
        for (int i = 0; i < clips.Count; i++)
        {
            ClipEntry entry = clips[i];
            if (entry != null && !string.IsNullOrEmpty(entry.id))
            {
                clipMap[entry.id] = entry;
            }
        }
    }

    public void Play(string id, float pitch = 1f, float volumeScale = 1f)
    {
        if (!clipMap.TryGetValue(id, out ClipEntry entry) || entry.clip == null)
        {
            return;
        }

        float finalVolume = Mathf.Clamp01(entry.volume * volumeScale);

        if (Mathf.Approximately(pitch, 1f))
        {
            sfxSource.PlayOneShot(entry.clip, finalVolume);
            return;
        }

        GameObject tempGO = new GameObject("SFX_" + id);
        tempGO.transform.SetParent(transform);

        AudioSource temp = tempGO.AddComponent<AudioSource>();
        temp.playOnAwake = false;
        temp.spatialBlend = sfxSource.spatialBlend;
        temp.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
        temp.pitch = pitch;
        temp.volume = finalVolume;
        temp.clip = entry.clip;
        temp.Play();

        float t = Mathf.Max(0.1f, entry.clip.length / Mathf.Abs(pitch));
        Destroy(tempGO, t + 0.1f);
    }
}
