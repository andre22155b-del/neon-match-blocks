export const SHOT_FEEL_CONFIG = Object.freeze({
  hoop: Object.freeze({
    rimBaseY: 116,
    rimDepthForward: 14,
    backboardScale: 1.42,
  }),
  arc: Object.freeze({
    minRimClearancePx: 28,
    minArcAngleDeg: 48,
  }),
  // Input shaping for touch drag.
  input: Object.freeze({
    minDragPixels: 10,
    maxDragPixels: 250,
    smoothing: 0.24,
    directionDeadZonePx: 8,
    directionCurve: 0.9,
  }),
  // Power mapping from drag distance.
  power: Object.freeze({
    multiplier: 1.08,
    curveExponent: 1.24,
    softDeadZoneNorm: 0.28,
    softDeadZoneStrength: 0.52,
    shortShotBoost: 0.12,
    difficultyScale: Object.freeze({
      easy: 1.08,
      medium: 1.0,
      hard: 0.94,
    }),
  }),
  // Ball and collision motion.
  physics: Object.freeze({
    gravityScale: 1,
    arcBoostDeg: 3.2,
    arcClearanceBonusPx: 20,
    airDragCoeff: 0.02,
    rimRestitution: 0.72,
    backboardDamping: 0.86,
    swishDropBoost: 28,
  }),
  // Forgiveness and rim-centering assist.
  forgiveness: Object.freeze({
    globalAccuracyScale: 1.12,
    toleranceRadiusMul: Object.freeze({
      easy: 0.5,
      medium: 0.42,
      hard: 0.32,
    }),
    centerPull: Object.freeze({
      easy: 0.24,
      medium: 0.18,
      hard: 0.12,
    }),
    makeRadiusScale: Object.freeze({
      easy: 1.2,
      medium: 1.12,
      hard: 1.04,
    }),
  }),
  // Subtle camera follow polish during flight.
  camera: Object.freeze({
    fovDeg: 58,
    verticalOffsetPx: 58,
    followLerp: 0.1,
    arcFollowBoost: 0.022,
  }),
});

export const getDifficultyScalar = (map, difficulty, fallback) => {
  if (!map || typeof map !== "object") return fallback;
  const value = map[difficulty];
  return Number.isFinite(value) ? value : fallback;
};
