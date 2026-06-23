using UnityEngine;

[CreateAssetMenu(fileName = "NeonFieldGoalConfig", menuName = "Neon FieldGoal/Config")]
public class NeonFieldGoalConfig : ScriptableObject
{
    [Header("Round")]
    public float roundDurationSeconds = 60f;
    public float clutchModeRoundDurationSeconds = 35f;
    public float clutchWindowSeconds = 10f;
    public float respawnDelayAfterGoal = 0.9f;
    public float respawnDelayAfterMiss = 0.65f;

    [Header("Movement")]
    public float laneHalfWidth = 3.25f;
    public float moverSpeed = 2.35f;
    public float moverAcceleration = 18f;
    public float moverDeceleration = 24f;
    public float moverLeanAngle = 8f;
    public float moverLeanSharpness = 10f;

    [Header("Input")]
    [Range(0f, 0.3f)] public float kickReleaseDebounceSeconds = 0.1f;

    [Header("Kick")]
    public float minKickImpulse = 7.1f;
    public float maxKickImpulse = 10.15f;
    public float launchAngleDegrees = 38f;
    public float maxAimDegrees = 11f;
    [Range(0f, 1f)] public float aimAssistStrength = 0.22f;
    [Range(0.45f, 1.5f)] public float kickPowerExponent = 0.82f;
    [Range(0f, 1f)] public float perfectKickCenter = 0.84f;
    [Range(0.02f, 0.4f)] public float perfectKickWindow = 0.12f;
    public float perfectKickImpulseBonus = 0.42f;
    public float maxCurveTorque = 1.35f;
    public float maxBallLifeSeconds = 5f;
    public int trajectorySampleCount = 18;
    public float trajectorySampleStep = 0.08f;
    [Range(0f, 1f)] public float minKickPower = 0.1f;

    [Header("Football Physics")]
    public float footballMass = 0.6f;
    public float footballLinearDamping = 0.05f;
    public float footballAngularDamping = 0.08f;
    public float kickReleaseDuration = 0.08f;
    [Range(0.5f, 3f)] public float kickReleaseEase = 1.45f;
    [Range(0.2f, 1.5f)] public float releaseGravityScale = 0.72f;
    [Range(0.2f, 1.5f)] public float risingGravityScale = 0.94f;
    [Range(0.2f, 1.8f)] public float fallingGravityScale = 1.08f;
    public float settleVelocityThreshold = 0.2f;
    public float settleAngularVelocityThreshold = 0.45f;
    public float settleStableSeconds = 0.25f;
    public float outOfBoundsY = -2f;
    public float outOfBoundsDistance = 55f;

    [Header("Goal")]
    public float goalDistance = 21.5f;
    public float crossbarHeight = 3.05f;
    public float uprightInnerHalfWidth = 2.82f;
    public float goalPlaneDepthPadding = 0.05f;

    [Header("Defense")]
    [Min(1)] public int defenseBaseBlockerCount = 3;
    [Min(1)] public int defenseMaxBlockerCount = 5;
    [Min(1)] public int defenseExtraBlockerStartYardLine = 35;
    [Min(1)] public int defenseMaxBlockerYardLine = 50;
    [Min(0.5f)] public float defenseLineForwardOffset = 4.75f;
    [Min(0.5f)] public float defenseBlockerSpacing = 1.6f;
    [Min(0.18f)] public float defenseBarWidth = 1.18f;
    [Min(0.18f)] public float defenseBarDepth = 0.48f;
    [Min(0.5f)] public float defenseBarBaseHeight = 1.08f;
    [Min(0f)] public float defenseBarExtraHeight = 0.62f;
    [Min(0f)] public float defenseBarBaseTravel = 0.34f;
    [Min(0f)] public float defenseBarExtraTravel = 0.46f;
    [Min(0f)] public float defenseBarBasePulseSpeed = 1.05f;
    [Min(0f)] public float defenseBarExtraPulseSpeed = 0.62f;
    [Min(0f)] public float defenseBarKickSurge = 0.28f;
    [Min(0.1f)] public float defenseJumpDuration = 0.54f;
    [Min(0f)] public float defenseReactionDelay = 0.05f;
    [Min(0f)] public float defenseStaggerDelay = 0.03f;

    [Header("Wind")]
    [Min(1)] public int windStartYardLine = 35;
    [Min(1)] public int windMaxYardLine = 60;
    [Min(0f)] public float windBaseAcceleration = 0.42f;
    [Min(0f)] public float windExtraAcceleration = 1.02f;
    [Range(0f, 0.75f)] public float perfectKickWindResistance = 0.18f;

    [Header("Moving Goal")]
    [Min(20)] public int movingGoalStartYardLine = 50;
    [Min(20)] public int movingGoalFullChallengeYardLine = 65;
    [Min(0f)] public float movingGoalBaseSideOffset = 0.34f;
    [Min(0f)] public float movingGoalExtraSideOffset = 0.18f;
    [Min(0f)] public float movingGoalBaseSpeed = 0.32f;
    [Min(0f)] public float movingGoalExtraSpeed = 0.18f;
    [Min(0.1f)] public float movingGoalSharpness = 5.5f;

    [Header("Yard Progression")]
    [Min(1)] public int startingYardLine = 20;
    [Min(1)] public int yardsPerGoalStep = 5;
    [Min(0.01f)] public float worldUnitsPerYard = 0.1f;

    [Header("Scoring")]
    public int pointsPerGoal = 3;
    [Min(1)] public int longBombStartYardLine = 50;
    [Min(1)] public int longBombPointsPerGoal = 5;
    public int perfectKickBonusPoints = 2;
    public int clutchBonusPoints = 1;
    public int clutchPerfectBonusPoints = 2;
    public int makesPerMultiplierStep = 2;
    public int maxScoreMultiplier = 5;
    public int clutchMinimumMultiplier = 2;

    [Header("Goal Lights")]
    public float goalLightBaseIntensity = 2.15f;
    public float goalLightFlashIntensity = 10.5f;
    public float goalLightFlashDuration = 0.65f;
    public float kickGlowBoost = 0.7f;
    public float kickGlowDuration = 0.16f;
    public float crowdBoostMultiplier = 2.25f;
    public float finalDriveCrowdBoostMultiplier = 1.95f;

    [Header("Camera Juice")]
    [Range(50f, 85f)] public float cameraFirstPersonFov = 68f;
    [Range(1.4f, 2.1f)] public float cameraFirstPersonEyeHeight = 1.72f;
    [Range(1f, 2.2f)] public float cameraFirstPersonBackOffset = 1.35f;
    [Range(2.5f, 4.5f)] public float cameraFirstPersonLookHeight = 3.15f;
    [Min(0f)] public float cameraIdleSwayDistance = 0.018f;
    [Min(0f)] public float cameraIdleSwaySpeed = 1.45f;
    [Min(0f)] public float cameraIdleRollAngle = 0.3f;
    [Min(0f)] public float cameraBallFollowDistance = 5.2f;
    [Min(0f)] public float cameraBallFollowHeight = 1.85f;
    [Min(0f)] public float cameraBallFollowLookAhead = 1.45f;
    [Min(0.1f)] public float cameraBallFollowSharpness = 4.8f;
    [Min(0f)] public float cameraDistanceFovBoost = 4.2f;
    public float cameraKickShake = 0.14f;
    public float cameraGoalShake = 0.26f;
    public float cameraMissShake = 0.1f;
    public float cameraFinalDriveShake = 0.16f;
    public float cameraShakeAngle = 1.65f;
    public float cameraShakeDistance = 0.12f;
    public float cameraKickbackDistance = 0.22f;
    public float cameraKickFovBoost = 1.35f;
    public float cameraGoalFovBoost = 4.1f;
    public float cameraClutchFovBoost = 1.6f;
    public float cameraClutchRollAngle = 0.7f;
    public float cameraShakeDecay = 2.8f;
    public float cameraPositionSharpness = 11f;
    public float cameraRotationSharpness = 9f;
    public float cameraFovSharpness = 8f;
    [Range(0.05f, 1f)] public float perfectKickSlowMoScale = 0.86f;
    public float perfectKickSlowMoDuration = 0.08f;
    [Range(0.05f, 1f)] public float perfectGoalSlowMoScale = 0.72f;
    public float perfectGoalSlowMoDuration = 0.12f;
    [Range(0.05f, 1f)] public float nearMissSlowMoScale = 0.9f;
    public float nearMissSlowMoDuration = 0.06f;

    [Header("Audio")]
    [Range(0f, 1f)] public float masterSfxVolume = 0.92f;
    [Range(0f, 1f)] public float crowdVolume = 0.88f;
    [Range(0f, 1f)] public float ambientVolume = 0.34f;
    [Range(0f, 1f)] public float voiceVolume = 1f;
    [Range(0f, 1f)] public float announcerVolume = 0.92f;
    [Range(0.05f, 0.75f)] public float announcerCueGapSeconds = 0.18f;
    [Range(0.5f, 1f)] public float crowdDuckUnderAnnouncer = 0.82f;
    [Range(0.4f, 1f)] public float ambientDuckUnderAnnouncer = 0.68f;
    [Range(1f, 16f)] public float audioDuckRecoverSharpness = 8.5f;
    [Range(1f, 1.6f)] public float ambientMaxMixScale = 1.24f;
    public AudioClip kickClip;
    public AudioClip goalClip;
    public AudioClip missClip;
    public AudioClip crowdClip;
    public AudioClip ambientLoopClip;
    public AudioClip itsGoodVoiceClip;
    public AudioClip perfectKickStingClip;
    public AudioClip longBombStingClip;
    public AudioClip heatStingClip;
    public AudioClip clutchStingClip;
    public AudioClip nearMissStingClip;
    public AudioClip streakBreakStingClip;
    public AudioClip finalDriveStingClip;
    public AudioClip movingGoalStingClip;

    [Header("Asset Slots")]
    public GameObject footballVisualPrefab;
    public Mesh footballMesh;
    public Material footballMaterial;
    public GameObject[] refereePrefabs;
    public RuntimeAnimatorController refereeAnimatorController;
}
