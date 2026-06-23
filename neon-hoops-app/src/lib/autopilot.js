const clamp = (v, min, max) => Math.max(min, Math.min(max, v));

const PROFILE_ASSIST = Object.freeze({
  rookie: 0.48,
  smooth: 0.34,
  balanced: 0.2,
  precision: 0.08,
});

const DIFFICULTY_ASSIST = Object.freeze({
  easy: 0.16,
  medium: 0.08,
  hard: 0,
});

export const getSmartDefaults = ({ platform = "desktop" } = {}) => {
  if (platform === "ios") {
    return {
      controlProfile: "smooth",
      shotTuningPreset: "casual",
      aimMode: "casual",
      physicsProfile: "balanced",
      visualTuningPreset: "neon_premium",
      easyPlusAssist: true,
      aimSensitivityScale: 1.02,
    };
  }
  if (platform === "android") {
    return {
      controlProfile: "rookie",
      shotTuningPreset: "casual",
      aimMode: "casual",
      physicsProfile: "arcade",
      visualTuningPreset: "performance_mobile",
      easyPlusAssist: true,
      aimSensitivityScale: 1.08,
    };
  }
  return {
    controlProfile: "balanced",
    shotTuningPreset: "competitive",
    aimMode: "pro",
    physicsProfile: "arcade",
    visualTuningPreset: "competitive_clean",
    easyPlusAssist: false,
    aimSensitivityScale: 1,
  };
};

export const computeReleaseAssist = ({
  timingOffset = 0,
  releaseType = "normal",
  misses = 0,
  controlProfile = "balanced",
  aimMode = "pro",
  shotDifficulty = "medium",
  isAI = false,
} = {}) => {
  if (isAI) {
    return {
      releaseType,
      timingOffset,
      assistStrength: 0,
    };
  }

  const profileAssist = PROFILE_ASSIST[controlProfile] ?? PROFILE_ASSIST.balanced;
  const modeAssist = aimMode === "casual" ? 0.14 : 0;
  const difficultyAssist = DIFFICULTY_ASSIST[shotDifficulty] ?? DIFFICULTY_ASSIST.medium;
  const missAssist = clamp(misses * 0.08, 0, 0.24);
  const assistStrength = clamp(profileAssist + modeAssist + difficultyAssist + missAssist, 0, 0.86);

  const absTiming = Math.abs(timingOffset);
  if (absTiming <= 1) {
    return {
      releaseType: "perfect",
      timingOffset,
      assistStrength,
    };
  }

  const assistWindow = 1 + assistStrength;
  if (absTiming > assistWindow) {
    return {
      releaseType,
      timingOffset,
      assistStrength,
    };
  }

  const nearPerfectThreshold = 1 + assistStrength * 0.22;
  const sign = timingOffset < 0 ? -1 : 1;
  const dampedOffset = sign * Math.max(0.05, absTiming - assistStrength * 0.42);

  return {
    releaseType: absTiming <= nearPerfectThreshold ? "perfect" : "normal",
    timingOffset: dampedOffset,
    assistStrength,
  };
};
