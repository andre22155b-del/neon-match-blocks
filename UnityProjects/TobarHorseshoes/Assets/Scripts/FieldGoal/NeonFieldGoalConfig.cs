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

    [Header("Scoring")]
    public int pointsPerGoal = 3;
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

    [Header("Audio")]
    [Range(0f, 1f)] public float masterSfxVolume = 0.92f;
    [Range(0f, 1f)] public float crowdVolume = 0.88f;
    [Range(0f, 1f)] public float ambientVolume = 0.34f;
    [Range(0f, 1f)] public float voiceVolume = 1f;
    public AudioClip kickClip;
    public AudioClip goalClip;
    public AudioClip missClip;
    public AudioClip crowdClip;
    public AudioClip ambientLoopClip;
    public AudioClip itsGoodVoiceClip;

    [Header("Asset Slots")]
    public GameObject footballVisualPrefab;
    public Mesh footballMesh;
    public Material footballMaterial;
    public GameObject[] refereePrefabs;
    public RuntimeAnimatorController refereeAnimatorController;
}
