using System.Collections.Generic;
using UnityEngine;

public class RefereePresentationController : MonoBehaviour
{
    [Header("Config")]
    public NeonFieldGoalConfig config;
    public AudioSource voiceSource;
    public string celebrateTrigger = "Celebrate";
    public float celebrationHoldSeconds = 1.1f;
    public float poseLerpSpeed = 9f;
    public float armRaiseDegrees = 110f;

    [Header("Anchors")]
    public Transform[] refereeAnchors;
    public Transform[] refereeRoots;
    public Transform[] leftArmPivots;
    public Transform[] rightArmPivots;

    private readonly List<GameObject> spawnedReferees = new List<GameObject>();
    private Animator[] animators = new Animator[0];
    private Quaternion[] leftArmBaseRotations = new Quaternion[0];
    private Quaternion[] rightArmBaseRotations = new Quaternion[0];
    private Vector3[] rootBasePositions = new Vector3[0];
    private Quaternion[] rootBaseRotations = new Quaternion[0];
    private float celebrationTimer;
    private float celebrationBlend;

    private void Awake()
    {
        if (voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.spatialBlend = 0f;
        }

        CacheArmRotations();
        CacheRootTransforms();
        CacheAnimators();
    }

    public void ApplyConfig(NeonFieldGoalConfig fieldGoalConfig)
    {
        config = fieldGoalConfig;
        if (config == null)
        {
            return;
        }

        voiceSource.volume = config.voiceVolume;

        if (config.refereePrefabs != null && config.refereePrefabs.Length > 0)
        {
            InstallPrefabRefs();
        }

        if (config.refereeAnimatorController != null)
        {
            ApplyAnimatorController(config.refereeAnimatorController);
        }
    }

    public void CelebrateGoal()
    {
        celebrationTimer = celebrationHoldSeconds;

        if (animators != null)
        {
            for (int i = 0; i < animators.Length; i++)
            {
                if (animators[i] != null)
                {
                    animators[i].SetTrigger(celebrateTrigger);
                }
            }
        }

        if (voiceSource != null)
        {
            AudioClip clip = config != null ? config.itsGoodVoiceClip : null;
            if (clip != null)
            {
                voiceSource.PlayOneShot(clip, config.voiceVolume);
            }
        }
    }

    public void ResetPose()
    {
        celebrationTimer = 0f;
        celebrationBlend = 0f;
        ApplyArmPose(0f);
        ApplyRootPose(0f);
    }

    private void Update()
    {
        if (celebrationTimer > 0f)
        {
            celebrationTimer -= Time.deltaTime;
        }

        float targetBlend = celebrationTimer > 0f ? 1f : 0f;
        celebrationBlend = Mathf.MoveTowards(celebrationBlend, targetBlend, Time.deltaTime * poseLerpSpeed);
        ApplyArmPose(celebrationBlend);

        ApplyRootPose(celebrationBlend);
    }

    private void ApplyArmPose(float blend)
    {
        int leftCount = leftArmPivots == null ? 0 : Mathf.Min(leftArmPivots.Length, leftArmBaseRotations.Length);
        for (int i = 0; i < leftCount; i++)
        {
            if (leftArmPivots[i] != null)
            {
                Quaternion target = leftArmBaseRotations[i] * Quaternion.Euler(0f, 0f, -armRaiseDegrees);
                leftArmPivots[i].localRotation = Quaternion.Slerp(leftArmBaseRotations[i], target, blend);
            }
        }

        int rightCount = rightArmPivots == null ? 0 : Mathf.Min(rightArmPivots.Length, rightArmBaseRotations.Length);
        for (int i = 0; i < rightCount; i++)
        {
            if (rightArmPivots[i] != null)
            {
                Quaternion target = rightArmBaseRotations[i] * Quaternion.Euler(0f, 0f, armRaiseDegrees);
                rightArmPivots[i].localRotation = Quaternion.Slerp(rightArmBaseRotations[i], target, blend);
            }
        }
    }

    private void CacheArmRotations()
    {
        leftArmBaseRotations = new Quaternion[leftArmPivots == null ? 0 : leftArmPivots.Length];
        for (int i = 0; i < leftArmBaseRotations.Length; i++)
        {
            leftArmBaseRotations[i] = leftArmPivots[i] != null ? leftArmPivots[i].localRotation : Quaternion.identity;
        }

        rightArmBaseRotations = new Quaternion[rightArmPivots == null ? 0 : rightArmPivots.Length];
        for (int i = 0; i < rightArmBaseRotations.Length; i++)
        {
            rightArmBaseRotations[i] = rightArmPivots[i] != null ? rightArmPivots[i].localRotation : Quaternion.identity;
        }
    }

    private void CacheAnimators()
    {
        if (refereeRoots == null)
        {
            animators = new Animator[0];
            return;
        }

        animators = new Animator[refereeRoots.Length];
        for (int i = 0; i < refereeRoots.Length; i++)
        {
            animators[i] = refereeRoots[i] != null ? refereeRoots[i].GetComponentInChildren<Animator>() : null;
        }
    }

    private void CacheRootTransforms()
    {
        if (refereeRoots == null)
        {
            rootBasePositions = new Vector3[0];
            rootBaseRotations = new Quaternion[0];
            return;
        }

        rootBasePositions = new Vector3[refereeRoots.Length];
        rootBaseRotations = new Quaternion[refereeRoots.Length];
        for (int i = 0; i < refereeRoots.Length; i++)
        {
            rootBasePositions[i] = refereeRoots[i] != null ? refereeRoots[i].localPosition : Vector3.zero;
            rootBaseRotations[i] = refereeRoots[i] != null ? refereeRoots[i].localRotation : Quaternion.identity;
        }
    }

    private void ApplyRootPose(float blend)
    {
        if (refereeRoots == null)
        {
            return;
        }

        int rootCount = Mathf.Min(refereeRoots.Length, Mathf.Min(rootBasePositions.Length, rootBaseRotations.Length));
        for (int i = 0; i < rootCount; i++)
        {
            if (refereeRoots[i] == null)
            {
                continue;
            }

            float wave = Time.time * 9f + i * 0.75f;
            float bounce = blend * Mathf.Abs(Mathf.Sin(wave)) * 0.13f;
            float sway = blend * Mathf.Sin(wave * 0.55f) * 8f;
            float scalePulse = blend * (0.06f + Mathf.Abs(Mathf.Sin(wave * 1.2f)) * 0.05f);

            refereeRoots[i].localPosition = rootBasePositions[i] + Vector3.up * bounce;
            refereeRoots[i].localRotation = rootBaseRotations[i] * Quaternion.Euler(0f, sway, 0f);
            refereeRoots[i].localScale = Vector3.one * (1f + scalePulse);
        }
    }

    private void InstallPrefabRefs()
    {
        ClearSpawnedRefs();

        if (refereeAnchors == null || refereeAnchors.Length == 0)
        {
            return;
        }

        for (int i = 0; i < refereeAnchors.Length; i++)
        {
            Transform anchor = refereeAnchors[i];
            if (anchor == null)
            {
                continue;
            }

            GameObject prefab = config.refereePrefabs[Mathf.Min(i, config.refereePrefabs.Length - 1)];
            if (prefab == null)
            {
                continue;
            }

            GameObject instance = Instantiate(prefab, anchor);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            spawnedReferees.Add(instance);

            if (refereeRoots != null && i < refereeRoots.Length && refereeRoots[i] != null)
            {
                refereeRoots[i].gameObject.SetActive(false);
            }
        }

        CacheAnimators();
        CacheRootTransforms();
    }

    private void ApplyAnimatorController(RuntimeAnimatorController controller)
    {
        if (controller == null)
        {
            return;
        }

        if (animators == null)
        {
            CacheAnimators();
        }

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
            {
                animators[i].runtimeAnimatorController = controller;
            }
        }
    }

    private void ClearSpawnedRefs()
    {
        for (int i = 0; i < spawnedReferees.Count; i++)
        {
            if (spawnedReferees[i] != null)
            {
                Destroy(spawnedReferees[i]);
            }
        }

        spawnedReferees.Clear();

        if (refereeRoots == null)
        {
            return;
        }

        for (int i = 0; i < refereeRoots.Length; i++)
        {
            if (refereeRoots[i] != null)
            {
                refereeRoots[i].gameObject.SetActive(true);
            }
        }
    }
}
