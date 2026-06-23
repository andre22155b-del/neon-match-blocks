using UnityEngine;

public class FieldGoalDefenseController : MonoBehaviour
{
    [Header("References")]
    public NeonFieldGoalConfig config;
    public Transform defenseRoot;
    public Transform[] blockerRoots;
    public Transform[] bodyVisuals;
    public Transform[] leftArmPivots;
    public Transform[] rightArmPivots;
    public Transform[] hitboxTransforms;
    public FieldGoalDefenseHitbox[] hitboxes;

    private Vector3 baseDefenseRootLocalPosition;
    private Vector3[] baseBodyLocalPositions;
    private Vector3[] baseBodyLocalScales;
    private Quaternion[] baseBodyLocalRotations;
    private Quaternion[] baseLeftArmLocalRotations;
    private Quaternion[] baseRightArmLocalRotations;
    private Vector3[] baseHitboxLocalPositions;
    private Vector3[] baseHitboxLocalScales;
    private float contestPower = 0.8f;
    private float contestTimer = 999f;
    private int currentGoalsMade;
    private int currentYardLine = 20;
    private int cachedBlockerCount = -1;
    private bool contestActive;
    private bool contestWasPerfect;

    private void Awake()
    {
        CacheRig();
        BindHitboxes();
    }

    public void ApplyConfig(NeonFieldGoalConfig fieldGoalConfig)
    {
        config = fieldGoalConfig;
        CacheRig();
        BindHitboxes();
    }

    public void ResetDefense(int yardLine, int goalsMade, bool snapInstant)
    {
        currentYardLine = Mathf.Max(1, yardLine);
        currentGoalsMade = Mathf.Max(0, goalsMade);
        contestTimer = 999f;
        contestPower = 0.8f;
        contestWasPerfect = false;
        contestActive = false;
        UpdateDefenseRootPosition();
        UpdateBlockers(Time.time, snapInstant);
    }

    public void ShowMenuPose(int yardLine)
    {
        ResetDefense(yardLine, 0, true);
    }

    public void ReactToKick(float power, bool perfectKick)
    {
        contestPower = Mathf.Clamp01(power);
        contestWasPerfect = perfectKick;
        contestTimer = 0f;
        contestActive = true;
    }

    private void Update()
    {
        if (config == null)
        {
            return;
        }

        UpdateDefenseRootPosition();

        if (contestActive)
        {
            contestTimer += Time.deltaTime;
            float maxContestTime =
                Mathf.Max(0f, config.defenseReactionDelay) +
                Mathf.Max(0, GetActiveBlockerCount(config, currentYardLine) - 1) * Mathf.Max(0f, config.defenseStaggerDelay) +
                Mathf.Max(0.1f, config.defenseJumpDuration) +
                0.12f;
            if (contestTimer >= maxContestTime)
            {
                contestActive = false;
            }
        }

        UpdateBlockers(Time.time, false);
    }

    public static int GetActiveBlockerCount(NeonFieldGoalConfig config, int yardLine)
    {
        int baseCount = config != null ? Mathf.Max(1, config.defenseBaseBlockerCount) : 3;
        int maxCount = config != null ? Mathf.Max(baseCount, config.defenseMaxBlockerCount) : 5;
        int rampStart = config != null ? Mathf.Max(1, config.defenseExtraBlockerStartYardLine) : 35;
        int rampEnd = config != null ? Mathf.Max(rampStart, config.defenseMaxBlockerYardLine) : 50;

        if (yardLine < rampStart || maxCount <= baseCount)
        {
            return baseCount;
        }

        float t = rampEnd > rampStart ? Mathf.InverseLerp(rampStart, rampEnd, yardLine) : 1f;
        return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(baseCount, maxCount, t)), baseCount, maxCount);
    }

    public static float GetChallenge01(NeonFieldGoalConfig config, int yardLine)
    {
        int rampStart = config != null ? Mathf.Max(1, config.defenseExtraBlockerStartYardLine) : 35;
        int rampEnd = config != null ? Mathf.Max(rampStart, config.defenseMaxBlockerYardLine) : 50;
        return rampEnd > rampStart ? Mathf.InverseLerp(rampStart, rampEnd, yardLine) : (yardLine >= rampStart ? 1f : 0f);
    }

    private void CacheRig()
    {
        if (defenseRoot == null)
        {
            defenseRoot = transform;
        }

        int blockerCount = blockerRoots != null ? blockerRoots.Length : 0;
        if (cachedBlockerCount == blockerCount &&
            baseBodyLocalPositions != null &&
            baseBodyLocalScales != null &&
            baseBodyLocalRotations != null &&
            baseLeftArmLocalRotations != null &&
            baseRightArmLocalRotations != null &&
            baseHitboxLocalPositions != null &&
            baseHitboxLocalScales != null)
        {
            return;
        }

        baseDefenseRootLocalPosition = defenseRoot != null ? defenseRoot.localPosition : Vector3.zero;
        baseBodyLocalPositions = new Vector3[blockerCount];
        baseBodyLocalScales = new Vector3[blockerCount];
        baseBodyLocalRotations = new Quaternion[blockerCount];
        baseLeftArmLocalRotations = new Quaternion[blockerCount];
        baseRightArmLocalRotations = new Quaternion[blockerCount];
        baseHitboxLocalPositions = new Vector3[blockerCount];
        baseHitboxLocalScales = new Vector3[blockerCount];

        for (int i = 0; i < blockerCount; i++)
        {
            if (bodyVisuals != null && i < bodyVisuals.Length && bodyVisuals[i] != null)
            {
                baseBodyLocalPositions[i] = bodyVisuals[i].localPosition;
                baseBodyLocalScales[i] = bodyVisuals[i].localScale;
                baseBodyLocalRotations[i] = bodyVisuals[i].localRotation;
            }
            else
            {
                baseBodyLocalPositions[i] = Vector3.zero;
                baseBodyLocalScales[i] = Vector3.one;
                baseBodyLocalRotations[i] = Quaternion.identity;
            }

            if (leftArmPivots != null && i < leftArmPivots.Length && leftArmPivots[i] != null)
            {
                baseLeftArmLocalRotations[i] = leftArmPivots[i].localRotation;
            }
            else
            {
                baseLeftArmLocalRotations[i] = Quaternion.identity;
            }

            if (rightArmPivots != null && i < rightArmPivots.Length && rightArmPivots[i] != null)
            {
                baseRightArmLocalRotations[i] = rightArmPivots[i].localRotation;
            }
            else
            {
                baseRightArmLocalRotations[i] = Quaternion.identity;
            }

            if (hitboxTransforms != null && i < hitboxTransforms.Length && hitboxTransforms[i] != null)
            {
                baseHitboxLocalPositions[i] = hitboxTransforms[i].localPosition;
                baseHitboxLocalScales[i] = hitboxTransforms[i].localScale;
            }
            else
            {
                baseHitboxLocalPositions[i] = Vector3.zero;
                baseHitboxLocalScales[i] = Vector3.one;
            }
        }

        cachedBlockerCount = blockerCount;
    }

    private void BindHitboxes()
    {
        if (hitboxes == null)
        {
            return;
        }

        for (int i = 0; i < hitboxes.Length; i++)
        {
            if (hitboxes[i] == null)
            {
                continue;
            }

            hitboxes[i].controller = this;
            hitboxes[i].blockerIndex = i;
            if (string.IsNullOrEmpty(hitboxes[i].blockerLabel))
            {
                hitboxes[i].blockerLabel = "TIMING BAR";
            }
        }
    }

    private void UpdateDefenseRootPosition()
    {
        if (defenseRoot == null)
        {
            return;
        }

        Vector3 localPosition = baseDefenseRootLocalPosition;
        float configuredForwardOffset = config != null
            ? Mathf.Max(0.5f, config.defenseLineForwardOffset)
            : baseDefenseRootLocalPosition.z;
        localPosition.z = configuredForwardOffset + FieldGoalScoring.GetKickDepthOffset(config, currentGoalsMade);
        defenseRoot.localPosition = localPosition;
    }

    private void UpdateBlockers(float time, bool snapInstant)
    {
        if (blockerRoots == null || blockerRoots.Length == 0)
        {
            return;
        }

        CacheRig();

        int blockerCount = blockerRoots.Length;
        int activeCount = Mathf.Clamp(GetActiveBlockerCount(config, currentYardLine), 0, blockerCount);
        float challenge = GetChallenge01(config, currentYardLine);
        float spacing = config != null ? Mathf.Max(0.75f, config.defenseBlockerSpacing) : 1.55f;
        float barWidth = config != null ? Mathf.Max(0.18f, config.defenseBarWidth) : 1.18f;
        float barDepth = config != null ? Mathf.Max(0.18f, config.defenseBarDepth) : 0.48f;
        float baseHeight = config != null ? Mathf.Max(0.5f, config.defenseBarBaseHeight) : 1.08f;
        float extraHeight = config != null ? Mathf.Max(0f, config.defenseBarExtraHeight) : 0.62f;
        float baseTravel = config != null ? Mathf.Max(0f, config.defenseBarBaseTravel) : 0.34f;
        float extraTravel = config != null ? Mathf.Max(0f, config.defenseBarExtraTravel) : 0.46f;
        float pulseSpeed =
            (config != null ? Mathf.Max(0f, config.defenseBarBasePulseSpeed) : 1.05f) +
            (config != null ? Mathf.Max(0f, config.defenseBarExtraPulseSpeed) : 0.62f) * challenge;
        float kickSurge = (config != null ? Mathf.Max(0f, config.defenseBarKickSurge) : 0.28f) * Mathf.Lerp(0.9f, 1.12f, contestPower);
        float barThickness = Mathf.Lerp(0.16f, 0.22f, challenge);
        if (contestWasPerfect)
        {
            kickSurge *= 1.08f;
        }

        float positionBlend = snapInstant ? 1f : 1f - Mathf.Exp(-9f * Time.deltaTime);
        for (int i = 0; i < blockerCount; i++)
        {
            Transform blockerRoot = blockerRoots[i];
            if (blockerRoot == null)
            {
                continue;
            }

            bool active = i < activeCount;
            if (blockerRoot.gameObject.activeSelf != active)
            {
                blockerRoot.gameObject.SetActive(active);
            }

            if (!active)
            {
                continue;
            }

            float slot = i - (activeCount - 1) * 0.5f;
            float baseX = slot * spacing;
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * (pulseSpeed + i * 0.12f) + i * 0.82f);
            float pulseTravel = baseTravel + extraTravel * challenge;
            float rhythmOffset = Mathf.Lerp(-pulseTravel, pulseTravel, pulse);
            float contest = EvaluateContest(i);
            float barCenterHeight = baseHeight + extraHeight * challenge + rhythmOffset + contest * kickSurge;
            barCenterHeight = Mathf.Max(0.55f, barCenterHeight);

            Vector3 targetLocalPosition = new Vector3(baseX, 0f, 0f);
            blockerRoot.localPosition = Vector3.Lerp(blockerRoot.localPosition, targetLocalPosition, positionBlend);

            if (bodyVisuals != null && i < bodyVisuals.Length && bodyVisuals[i] != null)
            {
                Transform bodyVisual = bodyVisuals[i];
                Vector3 targetBodyPosition = baseBodyLocalPositions[i];
                targetBodyPosition.y = barCenterHeight;
                bodyVisual.localPosition = Vector3.Lerp(bodyVisual.localPosition, targetBodyPosition, positionBlend);

                Vector3 targetBodyScale = new Vector3(
                    Mathf.Max(0.28f, barWidth * (1f + Mathf.Abs(slot) * 0.04f)),
                    barThickness,
                    Mathf.Max(0.18f, barDepth));
                bodyVisual.localScale = Vector3.Lerp(bodyVisual.localScale, targetBodyScale, positionBlend);

                bodyVisuals[i].localRotation =
                    baseBodyLocalRotations[i] *
                    Quaternion.Euler(
                        Mathf.Sin(time * (pulseSpeed * 1.35f) + i * 0.7f) * 2.8f,
                        Mathf.Lerp(-5f, 5f, pulse),
                        Mathf.Sin(time * (pulseSpeed * 1.6f) + i) * 1.6f);
            }

            if (leftArmPivots != null && i < leftArmPivots.Length && leftArmPivots[i] != null)
            {
                leftArmPivots[i].localRotation =
                    baseLeftArmLocalRotations[i] *
                    Quaternion.Euler(0f, 0f, Mathf.Lerp(-10f, -22f, contest));
            }

            if (rightArmPivots != null && i < rightArmPivots.Length && rightArmPivots[i] != null)
            {
                rightArmPivots[i].localRotation =
                    baseRightArmLocalRotations[i] *
                    Quaternion.Euler(0f, 0f, Mathf.Lerp(10f, 22f, contest));
            }

            if (hitboxTransforms != null && i < hitboxTransforms.Length && hitboxTransforms[i] != null)
            {
                Transform hitbox = hitboxTransforms[i];
                hitbox.localPosition = new Vector3(0f, barCenterHeight, 0f);
                Vector3 hitboxScale = new Vector3(
                    Mathf.Max(0.28f, barWidth * 1.14f),
                    Mathf.Max(0.16f, barThickness * 2.2f),
                    Mathf.Max(0.22f, barDepth * 1.16f));
                hitbox.localScale = hitboxScale;
            }
        }
    }

    private float EvaluateContest(int blockerIndex)
    {
        if (!contestActive)
        {
            return 0f;
        }

        float reactionDelay = config != null ? Mathf.Max(0f, config.defenseReactionDelay) : 0.05f;
        float staggerDelay = config != null ? Mathf.Max(0f, config.defenseStaggerDelay) : 0.03f;
        float jumpDuration = config != null ? Mathf.Max(0.1f, config.defenseJumpDuration) : 0.54f;
        float startTime = reactionDelay + blockerIndex * staggerDelay;
        if (contestTimer <= startTime)
        {
            return 0f;
        }

        float normalizedTime = (contestTimer - startTime) / jumpDuration;
        if (normalizedTime >= 1f)
        {
            return 0f;
        }

        return Mathf.Sin(Mathf.Clamp01(normalizedTime) * Mathf.PI);
    }
}
