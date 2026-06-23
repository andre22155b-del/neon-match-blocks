using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Tobar Horseshoes/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Pitch Distances")]
    public float distance25FeetMeters = 7.62f;
    public float distance40FeetMeters = 12.19f;

    [Header("Throw")]
    public float minThrowImpulse = 7.5f;
    public float maxThrowImpulse = 18f;
    public float launchAngleDegrees = 14f;
    public float maxAimDegrees = 16f;
    public float maxSpinTorque = 20f;

    [Header("Physics")]
    public float shoeMass = 1f;
    public float shoeDrag = 0.08f;
    public float shoeAngularDrag = 0.05f;
    public float settleVelocityThreshold = 0.1f;
    public float settleAngularVelocityThreshold = 0.25f;
    public float settleStableSeconds = 0.25f;

    [Header("Scoring")]
    public float closestPointDistanceMeters = 0.1524f; // 6 inches
    public int pointsRinger = 3;
    public int pointsLeaner = 1;
    public int pointsClosest = 1;
    public int scoreToWin = 21;

    [Header("Aim Assist")]
    [Range(0f, 1f)] public float aimAssistLow = 0.15f;
    [Range(0f, 1f)] public float aimAssistHigh = 0.3f;

    [Header("Input")]
    public float maxSwipePixels = 600f;
    public float maxAimDragPixels = 240f;
    public float maxSpinDragPixels = 220f;
}
