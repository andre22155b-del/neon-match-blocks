using UnityEngine;

/// <summary>
/// Global particle spawner.
/// Setup:
/// 1) Create manager object in scene and attach this script.
/// 2) Assign particle prefabs (normal/rare/legendary/wrong/combo/level complete).
/// </summary>
public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [Header("Match FX")]
    [SerializeField] private ParticleSystem normalMatchFx;
    [SerializeField] private ParticleSystem rareMatchFx;
    [SerializeField] private ParticleSystem legendaryMatchFx;
    [SerializeField] private ParticleSystem ringFx;

    [Header("Other FX")]
    [SerializeField] private ParticleSystem wrongFx;
    [SerializeField] private ParticleSystem comboFx;
    [SerializeField] private ParticleSystem levelCompleteFx;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void PlayMatch(Vector3 position, BlockRarity rarity)
    {
        ParticleSystem mainPrefab = normalMatchFx;
        float scale = 1f;

        if (rarity == BlockRarity.Rare)
        {
            mainPrefab = rareMatchFx != null ? rareMatchFx : normalMatchFx;
            scale = 1.2f;
        }
        else if (rarity == BlockRarity.Legendary)
        {
            mainPrefab = legendaryMatchFx != null ? legendaryMatchFx : (rareMatchFx != null ? rareMatchFx : normalMatchFx);
            scale = 1.45f;
        }

        Spawn(mainPrefab, position, scale);

        if (ringFx != null)
        {
            Spawn(ringFx, position, scale * 0.9f);
        }
    }

    public void PlayWrong(Vector3 position)
    {
        Spawn(wrongFx, position, 1f);
    }

    public void PlayCombo(Vector3 position, float scale = 1f)
    {
        Spawn(comboFx, position, scale);
    }

    public void PlayLevelComplete(Vector3 position)
    {
        Spawn(levelCompleteFx, position, 1.5f);
    }

    private void Spawn(ParticleSystem prefab, Vector3 position, float scale)
    {
        if (prefab == null)
        {
            return;
        }

        ParticleSystem ps = Instantiate(prefab, position, Quaternion.identity);
        ps.transform.localScale *= scale;
        ps.Play();

        float lifetime = GetLifetime(ps);
        Destroy(ps.gameObject, lifetime + 0.25f);
    }

    private float GetLifetime(ParticleSystem ps)
    {
        var main = ps.main;
        float duration = main.duration;
        float startLife;

        if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
        {
            startLife = Mathf.Max(main.startLifetime.constantMin, main.startLifetime.constantMax);
        }
        else
        {
            startLife = main.startLifetime.constant;
        }

        return duration + startLife;
    }
}
