using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Core")]
    public GameConfig config;
    public ThrowInput throwInput;
    public ThrowController throwController;
    public TobarUI ui;

    [Header("Scene")]
    public StakeTarget targetStake;
    public Transform nearStake;
    public Transform farStake;
    public Transform throwOrigin;
    public HorseshoeProjectile horseshoePrefab;
    public Transform spawnedShoeParent;

    [Header("Options")]
    public AimAssistMode startAimAssist = AimAssistMode.Low;
    public PitchDistanceMode startDistance = PitchDistanceMode.Feet25;

    private readonly List<ThrowResult> roundThrows = new List<ThrowResult>(4);
    private readonly List<HorseshoeProjectile> spawnedShoes = new List<HorseshoeProjectile>(4);
    private readonly int[] scores = new int[2];

    private AimAssistMode aimAssistMode;
    private PitchDistanceMode distanceMode;
    private HorseshoeProjectile activeShoe;
    private int currentPlayerIndex;
    private int throwsInRound;
    private bool waitingForSettle;
    private bool gameOver;

    private void Start()
    {
        aimAssistMode = startAimAssist;
        distanceMode = startDistance;

        throwInput.SetConfig(config);
        throwController.SetConfig(config);
        throwController.SetThrowOrigin(throwOrigin);

        throwInput.PowerChanged += OnPowerChanged;
        throwInput.AimChanged += OnAimChanged;
        throwInput.ThrowReleased += OnThrowReleased;

        if (ui != null)
        {
            ui.AimAssistChanged += OnAimAssistChanged;
            ui.DistanceModeChanged += OnDistanceModeChanged;
        }

        ApplyPitchDistance(distanceMode);
        StartNewRound();
    }

    private void OnDestroy()
    {
        if (throwInput != null)
        {
            throwInput.PowerChanged -= OnPowerChanged;
            throwInput.AimChanged -= OnAimChanged;
            throwInput.ThrowReleased -= OnThrowReleased;
        }

        if (ui != null)
        {
            ui.AimAssistChanged -= OnAimAssistChanged;
            ui.DistanceModeChanged -= OnDistanceModeChanged;
        }

        if (activeShoe != null)
        {
            activeShoe.Settled -= OnShoeSettled;
        }
    }

    private void OnPowerChanged(float power)
    {
        if (ui != null)
        {
            ui.SetPower(power);
        }
    }

    private void OnAimChanged(float aim)
    {
        if (ui != null)
        {
            ui.SetAim(aim);
        }
    }

    private void OnThrowReleased(float power, float aim, float spin)
    {
        if (gameOver || waitingForSettle || activeShoe == null)
        {
            return;
        }

        waitingForSettle = true;
        throwController.Throw(activeShoe, power, aim, spin, aimAssistMode);
    }

    private void OnShoeSettled(HorseshoeProjectile shoe)
    {
        shoe.Settled -= OnShoeSettled;

        bool isRinger = targetStake != null && targetStake.IsRinger(shoe);
        bool isLeaner = !isRinger && targetStake != null && targetStake.IsLeaner(shoe);

        ThrowResult result = new ThrowResult
        {
            playerIndex = currentPlayerIndex,
            ringer = isRinger,
            leaner = isLeaner,
            distanceToStake = targetStake != null ? targetStake.HorizontalDistanceToStake(shoe.transform.position) : float.MaxValue
        };

        roundThrows.Add(result);
        throwsInRound += 1;
        waitingForSettle = false;
        activeShoe = null;

        if (ui != null)
        {
            ui.SetPower(0f);
        }

        if (throwsInRound >= 4)
        {
            FinishRound();
            return;
        }

        currentPlayerIndex = throwsInRound % 2;
        SpawnNextShoe();
    }

    private void SpawnNextShoe()
    {
        if (horseshoePrefab == null || throwOrigin == null)
        {
            return;
        }

        activeShoe = Instantiate(horseshoePrefab, spawnedShoeParent == null ? transform : spawnedShoeParent);
        activeShoe.playerIndex = currentPlayerIndex;
        activeShoe.ApplyConfig(config);
        activeShoe.PlaceAt(throwOrigin);
        activeShoe.Settled += OnShoeSettled;
        spawnedShoes.Add(activeShoe);

        if (ui != null)
        {
            ui.SetTurn(currentPlayerIndex, ThrowsByPlayerInRound(currentPlayerIndex) + 1);
        }
    }

    private int ThrowsByPlayerInRound(int playerIndex)
    {
        int count = 0;
        for (int i = 0; i < roundThrows.Count; i++)
        {
            if (roundThrows[i].playerIndex == playerIndex)
            {
                count++;
            }
        }

        return count;
    }

    private void FinishRound()
    {
        int ringersP1 = CountResults(0, true, false);
        int ringersP2 = CountResults(1, true, false);
        int canceledRingers = Mathf.Min(ringersP1, ringersP2);

        ringersP1 -= canceledRingers;
        ringersP2 -= canceledRingers;

        int leanersP1 = CountResults(0, false, true);
        int leanersP2 = CountResults(1, false, true);

        int rawP1 = ringersP1 * config.pointsRinger + leanersP1 * config.pointsLeaner;
        int rawP2 = ringersP2 * config.pointsRinger + leanersP2 * config.pointsLeaner;

        float closestP1 = ClosestDistance(0);
        float closestP2 = ClosestDistance(1);

        if (closestP1 <= config.closestPointDistanceMeters || closestP2 <= config.closestPointDistanceMeters)
        {
            if (closestP1 < closestP2)
            {
                rawP1 += config.pointsClosest;
            }
            else if (closestP2 < closestP1)
            {
                rawP2 += config.pointsClosest;
            }
        }

        int awardedP1 = 0;
        int awardedP2 = 0;

        if (rawP1 > rawP2)
        {
            awardedP1 = rawP1 - rawP2;
            scores[0] += awardedP1;
        }
        else if (rawP2 > rawP1)
        {
            awardedP2 = rawP2 - rawP1;
            scores[1] += awardedP2;
        }

        if (ui != null)
        {
            ui.SetScore(scores[0], scores[1]);
            ui.AddRoundLog(BuildRoundLog(awardedP1, awardedP2, ringersP1, ringersP2, leanersP1, leanersP2));
        }

        if (scores[0] >= config.scoreToWin || scores[1] >= config.scoreToWin)
        {
            gameOver = true;
            if (ui != null)
            {
                int winner = scores[0] >= config.scoreToWin ? 1 : 2;
                ui.ShowWinner("Player " + winner + " wins " + scores[0] + "-" + scores[1]);
            }
            return;
        }

        StartNewRound();
    }

    private string BuildRoundLog(int awardedP1, int awardedP2, int ringersP1, int ringersP2, int leanersP1, int leanersP2)
    {
        if (awardedP1 > 0)
        {
            return "Round: P1 +" + awardedP1 + " (R" + ringersP1 + " L" + leanersP1 + ")";
        }

        if (awardedP2 > 0)
        {
            return "Round: P2 +" + awardedP2 + " (R" + ringersP2 + " L" + leanersP2 + ")";
        }

        return "Round: no score";
    }

    private int CountResults(int playerIndex, bool ringers, bool leaners)
    {
        int count = 0;
        for (int i = 0; i < roundThrows.Count; i++)
        {
            ThrowResult result = roundThrows[i];
            if (result.playerIndex != playerIndex)
            {
                continue;
            }

            if (ringers && result.ringer)
            {
                count++;
            }

            if (leaners && result.leaner)
            {
                count++;
            }
        }

        return count;
    }

    private float ClosestDistance(int playerIndex)
    {
        float best = float.MaxValue;
        for (int i = 0; i < roundThrows.Count; i++)
        {
            ThrowResult result = roundThrows[i];
            if (result.playerIndex != playerIndex)
            {
                continue;
            }

            if (result.distanceToStake < best)
            {
                best = result.distanceToStake;
            }
        }

        return best;
    }

    private void StartNewRound()
    {
        throwsInRound = 0;
        currentPlayerIndex = 0;
        waitingForSettle = false;
        roundThrows.Clear();
        DestroySpawnedShoes();
        SpawnNextShoe();
    }

    private void DestroySpawnedShoes()
    {
        for (int i = 0; i < spawnedShoes.Count; i++)
        {
            HorseshoeProjectile shoe = spawnedShoes[i];
            if (shoe != null)
            {
                Destroy(shoe.gameObject);
            }
        }

        spawnedShoes.Clear();
    }

    private void OnAimAssistChanged(AimAssistMode mode)
    {
        aimAssistMode = mode;
        if (ui != null)
        {
            ui.AddRoundLog("Aim assist: " + mode);
        }
    }

    private void OnDistanceModeChanged(PitchDistanceMode mode)
    {
        distanceMode = mode;
        ApplyPitchDistance(distanceMode);
        if (ui != null)
        {
            ui.AddRoundLog("Distance: " + (mode == PitchDistanceMode.Feet25 ? "25 ft" : "40 ft"));
        }
    }

    private void ApplyPitchDistance(PitchDistanceMode mode)
    {
        if (nearStake == null || farStake == null || config == null)
        {
            return;
        }

        float meters = mode == PitchDistanceMode.Feet25 ? config.distance25FeetMeters : config.distance40FeetMeters;
        farStake.position = nearStake.position + nearStake.forward * meters;

        if (targetStake != null)
        {
            targetStake.stakeTransform = farStake;
        }

        throwController.SetTargetStake(farStake);
    }
}
