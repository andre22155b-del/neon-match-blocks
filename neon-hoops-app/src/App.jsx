import React, { useCallback, useEffect, useRef, useState } from "react";
import { computeAdaptiveQuality, computeFrameDelta, stepFixedAccumulator } from "./lib/timestep.js";
import {
  API_BASE,
  BACKEND_ENABLED,
  finishMatchSession,
  getBackendHealth,
  getLeaderboard,
  getRecentMatches,
  startMatchSession,
} from "./lib/backend.js";
import { DEFAULT_GAME_SETUP, sanitizeGameSetup } from "./lib/gameSetup.js";
import {
  buildOfflineLeaderboard,
  buildOfflineRecentMatches,
  computeTierProgress,
  didTierUp,
  resolveTier,
} from "./lib/progression.js";
import { createInputCalibration, detectMobilePlatform } from "./lib/inputCalibration.js";
import { computeReleaseAssist, getSmartDefaults } from "./lib/autopilot.js";
import { SHOT_FEEL_CONFIG, getDifficultyScalar } from "./lib/shotFeelConfig.js";

let _ac = null;
const getAC = () => {
  if (!_ac) _ac = new (window.AudioContext || window.webkitAudioContext)();
  return _ac;
};

const tone = (freq, type, dur, gain = 0.2, freqEnd = null, delay = 0) => {
  try {
    const ctx = getAC();
    const osc = ctx.createOscillator();
    const g = ctx.createGain();
    osc.type = type;
    osc.frequency.setValueAtTime(freq, ctx.currentTime + delay);
    if (freqEnd && freqEnd > 0) {
      osc.frequency.exponentialRampToValueAtTime(freqEnd, ctx.currentTime + delay + dur);
    }
    g.gain.setValueAtTime(0.001, ctx.currentTime + delay);
    g.gain.linearRampToValueAtTime(gain, ctx.currentTime + delay + 0.01);
    g.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + delay + dur);
    osc.connect(g);
    g.connect(ctx.destination);
    osc.start(ctx.currentTime + delay);
    osc.stop(ctx.currentTime + delay + dur + 0.05);
  } catch (_) {}
};

const noise = (dur, filterFreq, filterType = "bandpass", gain = 0.12, delay = 0, q = 2.2) => {
  try {
    const ctx = getAC();
    const buf = ctx.createBuffer(1, Math.ceil(ctx.sampleRate * dur), ctx.sampleRate);
    const d = buf.getChannelData(0);
    for (let i = 0; i < d.length; i++) d[i] = Math.random() * 2 - 1;
    const src = ctx.createBufferSource();
    src.buffer = buf;
    const f = ctx.createBiquadFilter();
    f.type = filterType;
    f.frequency.value = filterFreq;
    f.Q.value = q;
    const g = ctx.createGain();
    g.gain.setValueAtTime(gain, ctx.currentTime + delay);
    g.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + delay + dur);
    src.connect(f);
    f.connect(g);
    g.connect(ctx.destination);
    src.start(ctx.currentTime + delay);
  } catch (_) {}
};

const SFX = {
  shoot: () => {
    noise(0.12, 1200, "highpass", 0.12);
    tone(180, "triangle", 0.09, 0.12, 120);
  },
  chainNet: () => {
    noise(0.18, 4500, "highpass", 0.14);
    noise(0.11, 2200, "bandpass", 0.11, 0.03, 3.5);
    [2200, 1700, 1300, 980].forEach((f, i) => tone(f, "square", 0.055, 0.07, f * 0.7, i * 0.03));
  },
  swishBass: () => {
    tone(88, "sine", 0.18, 0.18, 56);
    tone(64, "triangle", 0.16, 0.1, 46, 0.02);
  },
  rim: () => {
    tone(200, "sawtooth", 0.2, 0.16, 95);
    tone(920, "square", 0.06, 0.09, 620, 0.04);
    noise(0.07, 1500, "bandpass", 0.1);
  },
  floor: (impact = 0.5) => {
    const hit = Math.max(0.2, Math.min(1, impact));
    tone(105 - hit * 16, "triangle", 0.08 + hit * 0.05, 0.08 + hit * 0.08, 70 - hit * 10);
    noise(0.03 + hit * 0.03, 600 + hit * 700, "lowpass", 0.05 + hit * 0.06);
  },
  miss: () => {
    tone(260, "sine", 0.24, 0.12, 170);
    tone(190, "sine", 0.2, 0.08, 120, 0.08);
  },
  fireOn: () => {
    [360, 510, 720, 940].forEach((f, i) => tone(f, "sawtooth", 0.09, 0.1, null, i * 0.06));
    noise(0.26, 900, "bandpass", 0.12, 0.08, 1.2);
  },
  fireOff: () => {
    tone(700, "triangle", 0.08, 0.09, 240);
    tone(380, "triangle", 0.09, 0.08, 170, 0.06);
  },
  urgent: () => {
    tone(1300, "square", 0.05, 0.1);
    tone(1500, "square", 0.05, 0.07, null, 0.06);
  },
  aimLock: () => tone(1450, "sine", 0.03, 0.045, 1350),
  releasePerfect: () => {
    tone(1850, "square", 0.04, 0.11);
    tone(2350, "triangle", 0.035, 0.08, null, 0.03);
  },
  releaseSoft: () => {
    tone(980, "sine", 0.05, 0.055, 780);
  },
  whoosh: (intensity = 0.5) => {
    noise(0.09 + intensity * 0.08, 3200 + intensity * 1600, "highpass", 0.07 + intensity * 0.06);
  },
  gameOver: () => {
    [520, 420, 320, 230].forEach((f, i) => tone(f, "sine", 0.26, 0.14, null, i * 0.18));
  },
  rankUp: () => {
    [460, 580, 760, 980].forEach((f, i) => tone(f, "triangle", 0.08, 0.11, null, i * 0.05));
    noise(0.18, 3200, "highpass", 0.08, 0.03, 1.4);
  },
  chargeRise: (power = 0.5) => {
    const base = 220 + power * 420;
    tone(base, "sine", 0.04, 0.04, base * 1.03);
  },
  ui: () => tone(740, "sine", 0.055, 0.08),
};

const CW = 1280;
const CH = 720;
const COURT_ASPECT = CW / CH;
const GAME_LENGTH = 90;
const BASE_GRAVITY = 1850;
const POWER_EXP = 1.85;
const GAME_SPEED = 1.34;
const SIM_DT = 1 / 84;
const MAX_SUBSTEPS = 6;
const FOLLOW_THROUGH_WINDOW = 0.17;
const CLUTCH_SECONDS = 10;
const MAX_PARTICLES = 40;
const RIM_SIZE_MUL = 1.5;
const BALL_SIZE_MUL = 1.06;
const BACKBOARD_SIZE_MUL = SHOT_FEEL_CONFIG.hoop.backboardScale;
const HUD_HEIGHT_RATIO = 0.075;
const COURT_HEIGHT_RATIO = 0.925;
const HUD_COMPACT_SCALE = 0.33;
const HUD_CENTER_PANEL_MIN_WIDTH = Math.round(174 * HUD_COMPACT_SCALE + 48);
const HUD_TIMER_RING_SIZE = Math.round(86 * HUD_COMPACT_SCALE);
const HUD_TIMER_FONT_SIZE = Math.round(30 * HUD_COMPACT_SCALE + 2);
const HUD_QUIT_FONT_SIZE = Math.max(9, Math.round(11 * HUD_COMPACT_SCALE));
const HUD_QUIT_PAD_Y = Math.max(3, Math.round(5 * HUD_COMPACT_SCALE));
const HUD_QUIT_PAD_X = Math.max(6, Math.round(9 * HUD_COMPACT_SCALE));
const DEFAULT_HOOP_RADIUS = Math.round(40 * RIM_SIZE_MUL);
const HOOP_BASE_Y = SHOT_FEEL_CONFIG.hoop.rimBaseY;
const HOOP_DEPTH_FORWARD = SHOT_FEEL_CONFIG.hoop.rimDepthForward;
const TIMING_LATERAL_MOVE_RANGE = 120;
const TIMING_ENTRY_Y_RATIO = 0.4;
const TIMING_RIM_CLEARANCE_BONUS = 20;
const TIMING_MAKE_RADIUS_BOOST = 1.12;
const TIMING_SWISH_RADIUS_BOOST = 1.08;
const CAMERA_PITCH_DEG = 8;
const CAMERA_FOV_DEG = SHOT_FEEL_CONFIG.camera.fovDeg;
const CAMERA_VERTICAL_OFFSET = SHOT_FEEL_CONFIG.camera.verticalOffsetPx;
const CAMERA_PERSPECTIVE_SHEAR = -0.055;
const CAMERA_PERSPECTIVE_SCALE_Y = 0.92;
const RIM_VISUAL_Y_SCALE = 0.68;
const MIN_LAUNCH_ANGLE_DEG = 45;
const MAX_LAUNCH_ANGLE_DEG = 65;
const SHOT_MIN_SPEED = 560;
const SHOT_MAX_SPEED = 1280;
const SWIPE_DEAD_ZONE_PX = 10;
const RIM_CYLINDER_RESTITUTION = SHOT_FEEL_CONFIG.physics.rimRestitution;
const RIM_CYLINDER_THICKNESS = 8;
const NET_SIM_COLS = 10;
const NET_SIM_ROWS = 8;
const SWISH_SLOW_MO_SCALE = 0.82;
const SWISH_SLOW_MO_DURATION = 0.15;
const TIMING_ONLY_SHOTS = true;
const BACKEND_RETRY_COOLDOWN_MS = 15000;
const TIMING_HOLD_MS = 900;
const TIMING_RELEASE_COOLDOWN_MS = 400;
const TIMING_SCORE_WINDOW_RATIO = 0.7;
const SMOOTH_EASY_TUNE = Object.freeze({
  arcHeightTuning: 1.08,
  lateralForgiveness: 1.28,
  releaseForgiveness: 1.3,
});
const ARC_HEIGHT_TUNE_RANGE = { min: 0.82, max: 1.22 };
const LATERAL_FORGIVENESS_RANGE = { min: 0.78, max: 1.3 };
const RELEASE_FORGIVENESS_RANGE = { min: 0.78, max: 1.3 };
const CASUAL_AIM_SMOOTH = 0.3;
const PRO_AIM_SMOOTH = 0.24;
const CASUAL_VERTICAL_ASSIST = 0.12;
const CASUAL_CHARGE_SWEEP_SPEED = 0.92;
const PRO_CHARGE_SWEEP_SPEED = 1.22;
const FLOOR_Y = 682;
const SHADOW_MAX_BLUR = 22;
const ELASTIC_MAX_DRAG = 190;
const DYNAMIC_ASSIST_MAX = 0.45;
const DYNAMIC_ASSIST_GAIN = 0.055;
const DYNAMIC_ASSIST_DECAY = 0.014;
const AIR_DRAG_COEFF = SHOT_FEEL_CONFIG.physics.airDragCoeff;
const LINEAR_DAMP_COEFF = 0.012;
const SPIN_DAMP_COEFF = 0.028;
const REST_VELOCITY_EPS = 8;
const FLOOR_PENETRATION_SLOP = 1.1;
const FLIGHT_SUBSTEP_PIXELS = 54;
const FLIGHT_MAX_SUBSTEPS = 4;
const RIM_WORLD_LOCK = true;
const SHOT_TUNING_BASE = Object.freeze({
  minSwipePixels: SHOT_FEEL_CONFIG.input.minDragPixels,
  maxSwipePixels: SHOT_FEEL_CONFIG.input.maxDragPixels,
  minSwipeTime: 40,
  maxSwipeTime: 420,
  maxPower: 0.98,
  minPower: 0.16,
  arcBoost: 1,
  gravityScale: SHOT_FEEL_CONFIG.physics.gravityScale,
  aimAssistStrength: 1,
  aimAssistMaxAngleDeg: 180,
  screenCenterAssistRadius: 1,
  horizontalSensitivity: 1,
  verticalSensitivity: 1,
  previewSteps: 80,
  previewStepTime: SIM_DT,
  releaseCooldownMs: 0,
  sloppySwipePenalty: 0,
});
const SHOT_TUNING_PRESETS = Object.freeze({
  casual: Object.freeze({
    aimAssistStrength: 0.78,
    aimAssistMaxAngleDeg: 24,
    screenCenterAssistRadius: 0.7,
    maxPower: 0.9,
    arcBoost: 1.12,
    horizontalSensitivity: 0.9,
    sloppySwipePenalty: 0.05,
  }),
  competitive: Object.freeze({
    aimAssistStrength: 0.25,
    aimAssistMaxAngleDeg: 10,
    screenCenterAssistRadius: 0.35,
    maxPower: 0.95,
    arcBoost: 1.05,
    horizontalSensitivity: 1.25,
    sloppySwipePenalty: 0.3,
  }),
  arcade: Object.freeze({
    aimAssistStrength: 0.65,
    aimAssistMaxAngleDeg: 22,
    screenCenterAssistRadius: 0.7,
    maxPower: 1.05,
    arcBoost: 1.35,
    horizontalSensitivity: 1,
    sloppySwipePenalty: 0.1,
  }),
});
const SHOT_TUNING_LABELS = Object.freeze({
  casual: "Casual Forgiving",
  competitive: "Competitive Skill",
  arcade: "Arcade Wild",
});
const BANK_SHOT_TUNING_DEFAULT = Object.freeze({
  boardBounceRestitution: 1,
  rimPullAfterBoard: 1,
});
const RIM_FEEL_TUNING_DEFAULT = Object.freeze({
  rimInChanceMul: 1,
  rimSoftness: 1,
  floorBounceSoftness: 1,
});
const VISUAL_TUNING_PRESETS = Object.freeze({
  competitive_clean: Object.freeze({
    courtPaintIntensity: 0.86,
    centerLaneFade: 0.58,
    seamGlow: 0.88,
    hazeStrength: 0.62,
    rimDepthGlow: 0.9,
    courtWidthMul: 1.12,
    courtFarWidthMul: 0.82,
    lanePulseGain: 0.76,
    qualityFloor: 0.54,
    qualityCeil: 0.95,
    particleBudgetMul: 0.78,
    particleDrawStep: 1,
    particleBlurMul: 0.92,
    trailMaxMul: 0.82,
    trailFadeMul: 1.05,
    previewStepMul: 0.9,
    fireSpawnRateMul: 1.08,
  }),
  neon_premium: Object.freeze({
    courtPaintIntensity: 1.18,
    centerLaneFade: 0.78,
    seamGlow: 1.24,
    hazeStrength: 0.95,
    rimDepthGlow: 1.2,
    courtWidthMul: 1.2,
    courtFarWidthMul: 0.76,
    lanePulseGain: 1.05,
    qualityFloor: 0.56,
    qualityCeil: 1,
    particleBudgetMul: 1,
    particleDrawStep: 1,
    particleBlurMul: 1,
    trailMaxMul: 1,
    trailFadeMul: 1,
    previewStepMul: 1,
    fireSpawnRateMul: 1,
  }),
  performance_mobile: Object.freeze({
    courtPaintIntensity: 0.86,
    centerLaneFade: 0.56,
    seamGlow: 0.82,
    hazeStrength: 0.52,
    rimDepthGlow: 0.82,
    courtWidthMul: 1.08,
    courtFarWidthMul: 0.86,
    lanePulseGain: 0.74,
    qualityFloor: 0.5,
    qualityCeil: 0.84,
    particleBudgetMul: 0.58,
    particleDrawStep: 2,
    particleBlurMul: 0.72,
    trailMaxMul: 0.56,
    trailFadeMul: 1.28,
    previewStepMul: 0.72,
    fireSpawnRateMul: 1.35,
  }),
});
const VISUAL_TUNING_LABELS = Object.freeze({
  competitive_clean: "Competitive Clean",
  neon_premium: "Neon Premium",
  performance_mobile: "Performance Mobile",
});
let VISUAL_TUNE = VISUAL_TUNING_PRESETS.neon_premium;
let ShotTuning = {
  ...SHOT_TUNING_BASE,
  ...SHOT_TUNING_PRESETS.casual,
};
let BankShotTuning = {
  ...BANK_SHOT_TUNING_DEFAULT,
};
let RimFeelTuning = {
  ...RIM_FEEL_TUNING_DEFAULT,
};
const applyShotTuningPreset = (presetId) => {
  const preset = SHOT_TUNING_PRESETS[presetId] || SHOT_TUNING_PRESETS.casual;
  ShotTuning = { ...SHOT_TUNING_BASE, ...preset };
  return ShotTuning;
};
const applyBankShotTuning = (patch) => {
  BankShotTuning = { ...BankShotTuning, ...patch };
  return BankShotTuning;
};
const applyRimFeelTuning = (patch) => {
  RimFeelTuning = { ...RimFeelTuning, ...patch };
  return RimFeelTuning;
};
const applyVisualTuningPreset = (presetId) => {
  VISUAL_TUNE = VISUAL_TUNING_PRESETS[presetId] || VISUAL_TUNING_PRESETS.neon_premium;
  return VISUAL_TUNE;
};
const getGravity = () => BASE_GRAVITY * ShotTuning.gravityScale;
const getDragPowerMin = () => ShotTuning.minPower;
const getDragPowerMax = () => ShotTuning.maxPower;
const DAILY_MODIFIERS = ["none", "small_rim", "quick_return", "hot_zone", "no_preview", "wind"];
const AI_STYLES = ["sniper", "rhythm", "chaos", "clutch"];
const EASY_PLUS_ASSIST_TUNING = {
  timingPenaltyMul: 0.72,
  lateralPenaltyMul: 0.72,
  lateralAimMul: 0.8,
  spreadMul: 0.86,
  assistAdd: 0.08,
  releaseWidthMul: 1.18,
  centerJitterMul: 0.55,
  centerDriftMul: 0.66,
  powerDragMul: 0.84,
  powerDragDenomMul: 1.18,
};
const TIMING_DIFFICULTY_TUNING = Object.freeze({
  easy: Object.freeze({
    windowWidth: 0.46,
    wobbleMul: 0.54,
    forceMakeAccuracyMin: 0.04,
    forceSwishAccuracyMin: 0.62,
    makeWindowMul: 1.68,
    forgivingWindowMul: 1.42,
    makeRadiusMul: 1.36,
    swishRadiusMul: 1.22,
    entryTubeMul: 1.42,
    entryAssistMul: 1.42,
    arcLift: 52,
  }),
  medium: Object.freeze({
    windowWidth: 0.4,
    wobbleMul: 0.72,
    forceMakeAccuracyMin: 0.08,
    forceSwishAccuracyMin: 0.72,
    makeWindowMul: 1.34,
    forgivingWindowMul: 1.24,
    makeRadiusMul: 1.22,
    swishRadiusMul: 1.12,
    entryTubeMul: 1.24,
    entryAssistMul: 1.24,
    arcLift: 42,
  }),
  hard: Object.freeze({
    windowWidth: 0.34,
    wobbleMul: 1.02,
    forceMakeAccuracyMin: 0.18,
    forceSwishAccuracyMin: 0.82,
    makeWindowMul: 0.98,
    forgivingWindowMul: 0.96,
    makeRadiusMul: 0.96,
    swishRadiusMul: 0.92,
    entryTubeMul: 0.96,
    entryAssistMul: 0.92,
    arcLift: 24,
  }),
});
const DIFFICULTY_TUNING = {
  easy: {
    label: "Easy",
    color: "#66ffd2",
    idealPower: 0.74,
    idealAngle: 53,
    spreadMul: 0.42,
    releaseWidthMul: 2.05,
    assistBias: 0.28,
    pointsMul: 1,
    timingPenaltyMul: 0.68,
    lateralPenaltyMul: 0.58,
    lateralAimMul: 0.76,
    centerDriftMul: 0.55,
    centerJitter: 0.02,
    powerDragMul: 0.72,
    powerDragDenomMul: 1.2,
  },
  medium: {
    label: "Medium",
    color: "#7fd9ff",
    idealPower: 0.8,
    idealAngle: 56,
    spreadMul: 0.88,
    releaseWidthMul: 1.22,
    assistBias: 0.1,
    pointsMul: 1.1,
  },
  hard: {
    label: "Hard",
    color: "#ff8b7f",
    idealPower: 0.84,
    idealAngle: 58,
    spreadMul: 1.28,
    releaseWidthMul: 0.82,
    assistBias: -0.08,
    pointsMul: 1.24,
  },
};
const AIM_MODES = {
  casual: {
    label: "Casual",
    releaseWidthMul: 1.78,
    assistMul: 1.9,
    spreadMul: 0.48,
  },
  pro: {
    label: "Pro",
    releaseWidthMul: 0.84,
    assistMul: 0.84,
    spreadMul: 1.12,
  },
};
const PHYSICS_PROFILES = {
  arcade: {
    label: "Arcade",
    color: "#7effd4",
    floorRestitution: 0.66,
    floorFriction: 0.9,
    maxFloorBounces: 3,
    rimReflect: 1.58,
    rimKickX: 132,
    rimKickY: 92,
    settleSeconds: 1.04,
    makeDropBoost: 1.08,
    floorShakeMul: 1.12,
    floorSfxMul: 1.08,
  },
  balanced: {
    label: "Balanced",
    color: "#7fd9ff",
    floorRestitution: 0.54,
    floorFriction: 0.85,
    maxFloorBounces: 2,
    rimReflect: 1.74,
    rimKickX: 112,
    rimKickY: 80,
    settleSeconds: 0.92,
    makeDropBoost: 1.02,
    floorShakeMul: 1,
    floorSfxMul: 0.96,
  },
  sim: {
    label: "Sim",
    color: "#ffb072",
    floorRestitution: 0.42,
    floorFriction: 0.81,
    maxFloorBounces: 2,
    rimReflect: 1.88,
    rimKickX: 96,
    rimKickY: 72,
    settleSeconds: 0.84,
    makeDropBoost: 0.98,
    floorShakeMul: 0.94,
    floorSfxMul: 0.88,
  },
};
const PHYSICS_PROFILE_IDS = ["arcade", "balanced", "sim"];
const PHYSICS_PROFILE_ALIASES = Object.freeze({
  realistic: "balanced",
  park: "sim",
});
const resolvePhysicsProfile = (profileId) => {
  if (PHYSICS_PROFILES[profileId]) return profileId;
  return PHYSICS_PROFILE_ALIASES[profileId] || DEFAULT_GAME_SETUP.physicsProfile;
};
const BALL_COLORWAYS = {
  orange: {
    label: "Solar Orange",
    hue: 24,
    body: "#ff8a32",
    hot: "#ffd8a2",
    dark: "#7b2600",
    halo: "rgba(255,150,62,0.44)",
    seam: "rgba(88,30,0,0.82)",
    glowBlur: 15,
  },
  cyan: {
    label: "Neon Cyan",
    hue: 192,
    body: "#34e9ff",
    hot: "#cbfcff",
    dark: "#0f3f78",
    halo: "rgba(68,229,255,0.46)",
    seam: "rgba(10,52,88,0.84)",
    glowBlur: 16,
  },
  purple: {
    label: "Neon Purple",
    hue: 282,
    body: "#7a29e0",
    hot: "#ffd4ff",
    dark: "#1f0744",
    halo: "rgba(214,120,255,0.66)",
    seam: "rgba(241,166,255,0.94)",
    glowBlur: 26,
  },
  lime: {
    label: "Volt Lime",
    hue: 102,
    body: "#9bff41",
    hot: "#efffbe",
    dark: "#3f6b09",
    halo: "rgba(169,255,83,0.48)",
    seam: "rgba(48,77,12,0.85)",
    glowBlur: 17,
  },
  pink: {
    label: "Laser Pink",
    hue: 332,
    body: "#ff54be",
    hot: "#ffd3ee",
    dark: "#6f1a53",
    halo: "rgba(255,108,203,0.52)",
    seam: "rgba(92,20,66,0.86)",
    glowBlur: 18,
  },
};
const CONTROL_PROFILES = {
  rookie: {
    label: "Rookie",
    color: "#8fffb0",
    assistBoost: 0.28,
    windowMul: 1.58,
    spreadMul: 0.5,
    timingPenaltyMul: 0.34,
    chargeSpeedMul: 0.64,
    powerLerpMul: 0.62,
    verticalAssistMul: 1.8,
    previewBlend: 0.12,
    aimSensitivity: 0.76,
    dynamicGainMul: 1.4,
    releaseSmoothMul: 1.32,
    minFlick: 0.46,
  },
  smooth: {
    label: "Smooth",
    color: "#7effd4",
    assistBoost: 0.2,
    windowMul: 1.34,
    spreadMul: 0.66,
    timingPenaltyMul: 0.54,
    chargeSpeedMul: 0.78,
    powerLerpMul: 0.72,
    verticalAssistMul: 1.42,
    previewBlend: 0.15,
    aimSensitivity: 0.84,
    dynamicGainMul: 1.2,
    releaseSmoothMul: 1.22,
    minFlick: 0.42,
  },
  balanced: {
    label: "Balanced",
    color: "#7fd9ff",
    assistBoost: 0.1,
    windowMul: 1.08,
    spreadMul: 0.9,
    timingPenaltyMul: 0.78,
    chargeSpeedMul: 0.96,
    powerLerpMul: 0.9,
    verticalAssistMul: 1.08,
    previewBlend: 0.18,
    aimSensitivity: 0.95,
    dynamicGainMul: 1,
    releaseSmoothMul: 1.06,
    minFlick: 0.34,
  },
  precision: {
    label: "Precision",
    color: "#ffb072",
    assistBoost: 0.02,
    windowMul: 0.92,
    spreadMul: 1.08,
    timingPenaltyMul: 1,
    chargeSpeedMul: 1.12,
    powerLerpMul: 1.08,
    verticalAssistMul: 0.9,
    previewBlend: 0.24,
    aimSensitivity: 1.08,
    dynamicGainMul: 0.8,
    releaseSmoothMul: 0.94,
    minFlick: 0.26,
  },
};
const SHOT_CALLOUTS = {
  perfect: ["BUCKETS", "TOO EASY", "NOCODED"],
  swish: ["CASH", "SILENT", "TOO EASY"],
  make: ["MADE", "THAT'S TUFF", "RIMMED IN"],
  rimIn: ["RIMMED IN", "SOFT ROLL", "CLUTCH TOUCH"],
  rim: ["BROKE SHOT", "RATTLED OUT", "FRONT RIM"],
  miss: ["BRICK", "AIRBALL", "NOT TODAY"],
};

const clamp = (v, min, max) => Math.max(min, Math.min(max, v));
const lerp = (a, b, t) => a + (b - a) * t;
const rand = (a, b) => a + Math.random() * (b - a);
const powerCurve = (t) => Math.pow(clamp(t, 0, 1), POWER_EXP);

const aiStyleLabel = (style) =>
  style === "sniper"
    ? "Sniper"
    : style === "rhythm"
      ? "Rhythm"
      : style === "chaos"
        ? "Chaos"
        : style === "player"
          ? "Manual"
          : !style
            ? "--"
        : "Clutch";

const dailyModifierLabel = (mod) =>
  mod === "small_rim"
    ? "Small Rim"
    : mod === "quick_return"
      ? "Quick Return"
      : mod === "hot_zone"
        ? "Hot Zone"
        : mod === "no_preview"
          ? "No Preview"
          : mod === "wind"
            ? "Cross Wind"
        : "Standard";

const toArray = (value) => {
  if (Array.isArray(value)) return value;
  if (value && typeof value === "object" && Array.isArray(value.entries)) return value.entries;
  if (value && typeof value === "object" && Array.isArray(value.matches)) return value.matches;
  return [];
};

const randomFrom = (arr) => arr[(Math.random() * arr.length) | 0];
const nextCallout = (lane, key, fallback) => {
  const pool = SHOT_CALLOUTS[key];
  if (!lane || !Array.isArray(pool) || pool.length === 0) return fallback;
  lane.calloutCursor = (lane.calloutCursor + 1) % pool.length;
  return pool[lane.calloutCursor];
};
const getModeLabel = ({ practiceMode, vrMode, oneViewMode }) => {
  if (practiceMode) return "practice";
  if (vrMode) return "vr";
  if (oneViewMode) return "solo";
  return "duel";
};
const hoopX = (lane) => lane.hoop.x + (lane.hoopOffsetX || 0);
const hoopY = (lane) => lane.hoop.y + (lane.hoopOffsetY || 0);
const getRimCenterY = (lane, y = hoopY(lane)) => y + HOOP_DEPTH_FORWARD * 0.74;
const getBallColorway = (id) => BALL_COLORWAYS[id] || BALL_COLORWAYS.purple;
const getManualPull = (lane) => {
  if (!lane || lane.isAI) return 0;
  if (typeof lane.manualPull === "number") return clamp(lane.manualPull, -1, 1);
  const aimX = lane.aimX ?? lane.centerX;
  const aimY = lane.aimY ?? CH * 0.78;
  const xNorm = clamp((aimX - lane.centerX) / 210, -1, 1);
  const yNorm = clamp((aimY - CH * 0.72) / 185, -1, 1);
  const pullFromX = xNorm * lane.side;
  return clamp(yNorm * 0.72 + pullFromX * 0.56, -1, 1);
};
const getBallRadiusAtY = (y) => {
  const depth = clamp((y - 210) / (640 - 210), 0, 1);
  return (19 + depth * 15.8) * BALL_SIZE_MUL;
};
const getBallRadius = (lane) => getBallRadiusAtY(lane.ball?.y ?? FLOOR_Y);
const getBallGroundYAt = (y) => FLOOR_Y - getBallRadiusAtY(y) * 0.9;
const clampArcHeightTune = (v) => clamp(v, ARC_HEIGHT_TUNE_RANGE.min, ARC_HEIGHT_TUNE_RANGE.max);
const clampLateralForgiveness = (v) =>
  clamp(v, LATERAL_FORGIVENESS_RANGE.min, LATERAL_FORGIVENESS_RANGE.max);
const clampReleaseForgiveness = (v) =>
  clamp(v, RELEASE_FORGIVENESS_RANGE.min, RELEASE_FORGIVENESS_RANGE.max);
const normalizeDragDistance = (distancePx) => {
  const minPx = SHOT_FEEL_CONFIG.input.minDragPixels;
  const maxPx = SHOT_FEEL_CONFIG.input.maxDragPixels;
  const clamped = clamp(distancePx, minPx, maxPx);
  return clamp((clamped - minPx) / Math.max(1, maxPx - minPx), 0, 1);
};
const computeTouchPowerNorm = (distancePx, difficulty) => {
  const rawNorm = normalizeDragDistance(distancePx);
  const softZone = clamp(SHOT_FEEL_CONFIG.power.softDeadZoneNorm, 0.08, 0.6);
  const softStrength = clamp(SHOT_FEEL_CONFIG.power.softDeadZoneStrength, 0, 0.9);
  const curveExp = clamp(SHOT_FEEL_CONFIG.power.curveExponent, 0.8, 2.4);
  let norm = Math.pow(rawNorm, curveExp);
  if (rawNorm < softZone) {
    const zoneT = clamp(rawNorm / Math.max(0.01, softZone), 0, 1);
    norm *= lerp(1 - softStrength, 1, zoneT);
  }
  if (rawNorm < 0.2) {
    norm *= 1 + (1 - rawNorm / 0.2) * SHOT_FEEL_CONFIG.power.shortShotBoost;
  }
  const diffScale = getDifficultyScalar(SHOT_FEEL_CONFIG.power.difficultyScale, difficulty, 1);
  const scaled = norm * SHOT_FEEL_CONFIG.power.multiplier * diffScale;
  return clamp(scaled, 0, 1);
};
const TAU = Math.PI * 2;
const SETTINGS_STORAGE_KEY = "neonhoopz-settings-v2";
const SHOT_TELEMETRY_STORAGE_KEY = "neonhoopz-shot-telemetry-v1";
const SHOT_DIFFICULTY_IDS = ["easy", "medium", "hard"];
const AUTO_TUNE_MIN_ATTEMPTS = 20;
const AUTO_ARC_CALIBRATION_INTERVAL = 20;
const AUTO_ARC_CLEAR_MIN = SHOT_FEEL_CONFIG.arc.minRimClearancePx;
const AUTO_ARC_CLEAR_MAX = SHOT_FEEL_CONFIG.arc.minRimClearancePx + 20;
const createEmptyDifficultyTelemetry = () => ({
  attempts: 0,
  makes: 0,
  swishes: 0,
  rimIns: 0,
  rimOuts: 0,
  misses: 0,
});
const createEmptyShotTelemetry = () => ({
  easy: createEmptyDifficultyTelemetry(),
  medium: createEmptyDifficultyTelemetry(),
  hard: createEmptyDifficultyTelemetry(),
  updatedAt: null,
});
const createArcCalBucket = () => ({
  shots: 0,
  clearanceSum: 0,
  low: 0,
  high: 0,
  makes: 0,
});
const createArcCalState = () => ({
  easy: createArcCalBucket(),
  medium: createArcCalBucket(),
  hard: createArcCalBucket(),
});
const normalizeShotTelemetry = (raw) => {
  const base = createEmptyShotTelemetry();
  if (!raw || typeof raw !== "object") return base;
  for (const key of SHOT_DIFFICULTY_IDS) {
    const bucket = raw[key];
    if (!bucket || typeof bucket !== "object") continue;
    base[key] = {
      attempts: Math.max(0, Math.round(Number(bucket.attempts) || 0)),
      makes: Math.max(0, Math.round(Number(bucket.makes) || 0)),
      swishes: Math.max(0, Math.round(Number(bucket.swishes) || 0)),
      rimIns: Math.max(0, Math.round(Number(bucket.rimIns) || 0)),
      rimOuts: Math.max(0, Math.round(Number(bucket.rimOuts) || 0)),
      misses: Math.max(0, Math.round(Number(bucket.misses) || 0)),
    };
  }
  base.updatedAt = Number.isFinite(raw.updatedAt) ? Math.round(raw.updatedAt) : null;
  return base;
};
const loadShotTelemetry = () => {
  if (typeof window === "undefined") return createEmptyShotTelemetry();
  try {
    const raw = window.localStorage.getItem(SHOT_TELEMETRY_STORAGE_KEY);
    if (!raw) return createEmptyShotTelemetry();
    return normalizeShotTelemetry(JSON.parse(raw));
  } catch (_) {
    return createEmptyShotTelemetry();
  }
};
const saveShotTelemetry = (telemetry) => {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.setItem(SHOT_TELEMETRY_STORAGE_KEY, JSON.stringify(telemetry));
  } catch (_) {}
};
const detectRuntimeInputCalibration = () => {
  if (typeof window === "undefined" || typeof navigator === "undefined") return createInputCalibration();
  return createInputCalibration({
    platform: detectMobilePlatform(navigator.userAgent || ""),
    maxTouchPoints: Number(navigator.maxTouchPoints || 0),
    devicePixelRatio: Number(window.devicePixelRatio || 1),
    viewportWidth: Number(window.innerWidth || 390),
    viewportHeight: Number(window.innerHeight || 844),
  });
};
const detectViewportMetrics = () => {
  if (typeof window === "undefined") {
    return { width: CW, height: CH, dpr: 1 };
  }
  return {
    width: Math.max(280, Math.round(window.innerWidth || CW)),
    height: Math.max(280, Math.round(window.innerHeight || CH)),
    dpr: Math.max(1, Math.min(3, Number(window.devicePixelRatio || 1))),
  };
};
const loadStoredSettings = () => {
  if (typeof window === "undefined") return null;
  try {
    const raw = window.localStorage.getItem(SETTINGS_STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw);
    return parsed && typeof parsed === "object" ? parsed : null;
  } catch (_) {
    return null;
  }
};
const saveStoredSettings = (settings) => {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.setItem(SETTINGS_STORAGE_KEY, JSON.stringify(settings));
  } catch (_) {}
};
const wrapAngle = (angle) => {
  if (!Number.isFinite(angle)) return 0;
  const wrapped = angle % TAU;
  return wrapped < 0 ? wrapped + TAU : wrapped;
};
const pointSegmentDistance = (px, py, x1, y1, x2, y2) => {
  const dx = x2 - x1;
  const dy = y2 - y1;
  const denom = dx * dx + dy * dy;
  if (denom <= 1e-6) return Math.hypot(px - x1, py - y1);
  const t = clamp(((px - x1) * dx + (py - y1) * dy) / denom, 0, 1);
  const cx = x1 + dx * t;
  const cy = y1 + dy * t;
  return Math.hypot(px - cx, py - cy);
};
const applyBallDamping = (ball, dt) => {
  const speed = Math.hypot(ball.vx, ball.vy);
  const drag = Math.exp(-AIR_DRAG_COEFF * speed * dt * 0.0015);
  const linearDamp = Math.exp(-LINEAR_DAMP_COEFF * dt * 60);
  ball.vx *= drag * linearDamp;
  ball.vy *= drag * linearDamp;
  ball.spin *= Math.exp(-SPIN_DAMP_COEFF * dt * 60);
};
const clampBallRestJitter = (ball, floorY) => {
  if (Math.abs(ball.vx) < REST_VELOCITY_EPS) ball.vx = 0;
  if (Math.abs(ball.vy) < REST_VELOCITY_EPS && Math.abs(ball.y - floorY) < FLOOR_PENETRATION_SLOP * 3) {
    ball.vy = 0;
  }
};
const createNetState = () => ({
  cols: NET_SIM_COLS,
  rows: NET_SIM_ROWS,
  strandDisp: new Float32Array(NET_SIM_COLS + 1),
  strandVel: new Float32Array(NET_SIM_COLS + 1),
  inwardDisp: new Float32Array(NET_SIM_COLS + 1),
  inwardVel: new Float32Array(NET_SIM_COLS + 1),
  glow: 0,
});
const resetNetState = (lane) => {
  lane.netState = createNetState();
};
const kickNet = (lane, force = 1, suction = 0.55) => {
  const net = lane.netState;
  if (!net) return;
  for (let i = 0; i <= net.cols; i++) {
    const spread = 0.8 + Math.sin((i / Math.max(1, net.cols)) * Math.PI) * 0.5;
    net.strandVel[i] += force * (32 + spread * 72) * rand(0.92, 1.08);
    net.inwardVel[i] += suction * (16 + spread * 22) * rand(0.9, 1.12);
  }
  net.glow = Math.max(net.glow, clamp(0.35 + force * 0.45, 0.25, 1.35));
};
const updateNet = (lane, dt) => {
  const net = lane.netState;
  if (!net) return;
  const spring = 58;
  const damp = 10.5;
  const inwardSpring = 42;
  const inwardDamp = 9;
  const coupling = 18;
  for (let i = 0; i <= net.cols; i++) {
    const d = net.strandDisp[i];
    const v = net.strandVel[i];
    let nextV = v + (-spring * d - damp * v) * dt;
    if (i > 0) nextV += (net.strandDisp[i - 1] - d) * coupling * dt;
    if (i < net.cols) nextV += (net.strandDisp[i + 1] - d) * coupling * dt;
    const nextD = d + nextV * dt;
    net.strandVel[i] = nextV;
    net.strandDisp[i] = nextD;

    const id = net.inwardDisp[i];
    const iv = net.inwardVel[i];
    let nextIV = iv + (-inwardSpring * id - inwardDamp * iv) * dt;
    if (i > 0) nextIV += (net.inwardDisp[i - 1] - id) * (coupling * 0.58) * dt;
    if (i < net.cols) nextIV += (net.inwardDisp[i + 1] - id) * (coupling * 0.58) * dt;
    const nextID = id + nextIV * dt;
    net.inwardVel[i] = nextIV;
    net.inwardDisp[i] = nextID;
  }
  net.glow = Math.max(0, net.glow - dt * 3.2);
};
const solveLaunchVelocity = (lane, angleDeg, speedScale = 1) => {
  const hx = hoopX(lane);
  const x0 = lane.ball.x;
  const dx = hx - x0;
  const dir = Math.sign(dx) || 1;
  const minArcAngle = Math.max(MIN_LAUNCH_ANGLE_DEG, SHOT_FEEL_CONFIG.arc.minArcAngleDeg);
  const theta = (clamp(angleDeg, minArcAngle, MAX_LAUNCH_ANGLE_DEG) * Math.PI) / 180;
  const cosT = Math.max(0.16, Math.cos(theta));
  const speedT = clamp((speedScale - 0.72) / 0.48, 0, 1);
  const basePower = lerp(SHOT_MIN_SPEED, SHOT_MAX_SPEED, speedT);
  const depthHint = Math.max(0.44, Math.min(1.08, (lane.ball.y - getRimCenterY(lane)) / 640));
  const lateralTarget = clamp(dx / depthHint, -420, 420);
  let vx = dir * basePower * cosT;
  vx = lerp(vx, lateralTarget, 0.84);
  const vy = -basePower * Math.sin(theta);
  return {
    vx,
    vy,
    speed: Math.hypot(vx, vy),
  };
};
const recoverBallIfInvalid = (lane) => {
  if (!lane?.ball) return false;
  const ball = lane.ball;
  const invalid =
    !Number.isFinite(ball.x) ||
    !Number.isFinite(ball.y) ||
    !Number.isFinite(ball.vx) ||
    !Number.isFinite(ball.vy) ||
    !Number.isFinite(ball.spin) ||
    Math.abs(ball.x) > CW * 5 ||
    Math.abs(ball.y) > CH * 5 ||
    Math.abs(ball.vx) > 5000 ||
    Math.abs(ball.vy) > 7000;
  if (!invalid) return false;

  const baseHue = getBallColorway(lane.ballColor).hue;
  ball.x = lane.home?.x ?? CW * 0.5;
  ball.y = lane.home?.y ?? CH * 0.82;
  ball.vx = 0;
  ball.vy = 0;
  ball.spin = 0;
  ball.hue = Number.isFinite(ball.hue) ? ball.hue : baseHue;
  lane.state = "idle";
  lane.shotResolved = false;
  lane.madeShot = false;
  lane.forceMake = false;
  lane.forceSwish = false;
  lane.telemetryAttemptLogged = false;
  lane.telemetryOutcomeLogged = false;
  lane.crossChecked = false;
  lane.clearedRimPlane = false;
  lane.trail.length = 0;
  lane.followWindow = 0;
  lane.followSpinBoost = 0;
  lane.followArcAdjust = 0;
  lane.preShotLateral = 0;
  lane.dragAimX = 0;
  lane.dragAimY = 1;
  lane.dragDist = 0;
  lane.lastArcClearPx = 0;
  lane.lastArcApexY = 0;
  lane.lastArcRimPlaneY = 0;
  lane.chargeElapsed = 0;
  lane.timingCharge = getDragPowerMin();
  lane.timingAccuracy = 0;
  lane.timingWobble = 0;
  lane.bezierFlightTime = 0.82;
  lane.wobbleAmp = 0;
  lane.impactVibe = 0;
  lane.sideSpin = 0;
  lane.floorBounces = 0;
  lane.settleTimer = 0;
  lane.recycleOut = false;
  lane.recycleTimer = 0;
  lane.previewPath = null;
  lane.previewTTL = 0;
  lane.previewAlpha = 0;
  lane.boardHitCooldown = 0;
  lane.hitBackboard = false;
  lane.rimHitCooldown = 0;
  lane.rimTouched = false;
  lane.rimHitType = "";
  if (!lane.netState) lane.netState = createNetState();
  return true;
};
const getBackboardRect = (lane, x = hoopX(lane), y = hoopY(lane)) => {
  const r = lane.hoop.r;
  const halfW = r * 2.22 * BACKBOARD_SIZE_MUL;
  const top = y - r * 2.52 * BACKBOARD_SIZE_MUL;
  const bottom = y - r * 0.24 * BACKBOARD_SIZE_MUL;
  return { left: x - halfW, right: x + halfW, top, bottom, halfW };
};
const applyBackboardCollision = (lane, ball, prevX, prevY, stability = 0.5) => {
  const board = getBackboardRect(lane);
  const ballR = getBallRadiusAtY(ball.y) * 0.78;
  const boardBounce = clamp(
    BankShotTuning.boardBounceRestitution * SHOT_FEEL_CONFIG.physics.backboardDamping,
    0.6,
    1
  );
  const rimPullMul = clamp(BankShotTuning.rimPullAfterBoard, 0.3, 1.8);
  if (
    ball.x < board.left - ballR ||
    ball.x > board.right + ballR ||
    ball.y < board.top - ballR ||
    ball.y > board.bottom + ballR
  ) {
    return false;
  }

  const fromLeft = prevX <= board.left - ballR && ball.x > board.left - ballR;
  const fromRight = prevX >= board.right + ballR && ball.x < board.right + ballR;
  const fromTop = prevY <= board.top - ballR && ball.y > board.top - ballR;
  const fromBottom = prevY >= board.bottom + ballR && ball.y < board.bottom + ballR;

  let nx = 0;
  let ny = 0;
  if (fromLeft) nx = -1;
  else if (fromRight) nx = 1;
  else if (fromTop) ny = -1;
  else if (fromBottom) ny = 1;
  else {
    const dL = Math.abs(ball.x - board.left);
    const dR = Math.abs(board.right - ball.x);
    const dT = Math.abs(ball.y - board.top);
    const dB = Math.abs(board.bottom - ball.y);
    const minD = Math.min(dL, dR, dT, dB);
    if (minD === dL) nx = -1;
    else if (minD === dR) nx = 1;
    else if (minD === dT) ny = -1;
    else ny = 1;
  }

  if (nx !== 0) {
    ball.x = nx < 0 ? board.left - ballR : board.right + ballR;
    ball.vx = -ball.vx * (0.56 + stability * 0.22) * boardBounce;
    ball.vy += 18 * (0.9 + boardBounce * 0.2);
  } else {
    ball.y = ny < 0 ? board.top - ballR : board.bottom + ballR;
    ball.vy = -ball.vy * (0.5 + stability * 0.2) * boardBounce;
    if (ny > 0) {
      ball.vy = Math.max(ball.vy, (90 + (1 - stability) * 45) * (0.9 + boardBounce * 0.15));
    }
  }

  const hx = hoopX(lane);
  const pull = clamp((0.2 + stability * 0.12) * rimPullMul, 0.08, 0.62);
  ball.vx = lerp(ball.vx, (hx - ball.x) * (2.1 * rimPullMul), pull);
  if (ball.vy < 90) ball.vy = 90 + (1 - stability) * 40;
  return true;
};
const getAimAssistGate = (lane, ballX, ballY, hoopXPos, hoopYPos) => {
  const strength = clamp(ShotTuning.aimAssistStrength, 0, 2);
  if (strength <= 0) return 0;

  let angleGate = 1;
  if (ShotTuning.aimAssistMaxAngleDeg < 179.9) {
    const ax = (lane?.aimX ?? hoopXPos) - ballX;
    const ay = (lane?.aimY ?? hoopYPos) - ballY;
    const hx = hoopXPos - ballX;
    const hy = hoopYPos - ballY;
    const al = Math.hypot(ax, ay) || 1;
    const hl = Math.hypot(hx, hy) || 1;
    const dot = clamp((ax * hx + ay * hy) / (al * hl), -1, 1);
    const angleDeg = (Math.acos(dot) * 180) / Math.PI;
    angleGate = clamp(1 - angleDeg / Math.max(1, ShotTuning.aimAssistMaxAngleDeg), 0, 1);
  }

  let centerGate = 1;
  if (ShotTuning.screenCenterAssistRadius < 0.999) {
    const aimX = lane?.aimX ?? lane?.centerX ?? hoopXPos;
    const normOff = Math.abs(aimX - (lane?.centerX ?? hoopXPos)) / (CW * 0.5);
    centerGate = clamp(1 - normOff / Math.max(0.05, ShotTuning.screenCenterAssistRadius), 0, 1);
  }

  return strength * angleGate * centerGate;
};
const blendPreviewPath = (prev, next, blend = 0.24) => {
  if (!prev?.points?.length || !next?.points?.length) return next;
  const t = clamp(blend, 0.05, 1);
  const minLen = Math.min(prev.points.length, next.points.length);
  const points = [];
  for (let i = 0; i < minLen; i++) {
    points.push({
      x: lerp(prev.points[i].x, next.points[i].x, t),
      y: lerp(prev.points[i].y, next.points[i].y, t),
    });
  }
  if (next.points.length > minLen) {
    for (let i = minLen; i < next.points.length; i++) points.push(next.points[i]);
  }
  return {
    ...next,
    points,
    landingX: lerp(prev.landingX ?? next.landingX, next.landingX, t),
    alignment: lerp(prev.alignment ?? next.alignment, next.alignment, t),
  };
};

const getReleaseTimingData = (power, center, width) => {
  const safeW = Math.max(0.04, width * 0.5);
  const timingOffset = (power - center) / safeW;
  const timingAbs = Math.abs(timingOffset);
  let releaseType = "normal";
  if (timingAbs <= 1) releaseType = "perfect";
  else if (timingOffset < -1) releaseType = "early";
  else releaseType = "late";
  return { timingOffset, timingAbs, releaseType, isPerfect: releaseType === "perfect" };
};

const predictShotPath = (lane) => {
  const tune = DIFFICULTY_TUNING[lane.shotDifficulty] || DIFFICULTY_TUNING.medium;
  const aimTune = AIM_MODES[lane.aimMode] || AIM_MODES.casual;
  const controlTune = CONTROL_PROFILES[lane.controlProfile] || CONTROL_PROFILES.smooth;
  const easyPlusActive = lane.shotDifficulty === "easy" && !lane.isAI && lane.easyPlusAssist;
  const easyPlusTune = easyPlusActive ? EASY_PLUS_ASSIST_TUNING : null;
  const center = lane.releaseWindow?.center ?? 0.7;
  const width = lane.releaseWindow?.width ?? 0.14;
  const releaseForgiveness = clampReleaseForgiveness(lane.releaseForgiveness ?? 1);
  const lateralForgiveness = clampLateralForgiveness(lane.lateralForgiveness ?? 1);
  const arcHeightTuning = clampArcHeightTune(lane.arcHeightTuning ?? 1);
  const baseTiming = getReleaseTimingData(lane.power, center, width);
  const assistedTiming = computeReleaseAssist({
    timingOffset: baseTiming.timingOffset,
    releaseType: baseTiming.releaseType,
    misses: lane.misses || 0,
    controlProfile: lane.controlProfile,
    aimMode: lane.aimMode,
    shotDifficulty: lane.shotDifficulty,
    isAI: lane.isAI,
  });
  const timingOffset = assistedTiming.timingOffset;
  const timingAbs = Math.abs(timingOffset) / releaseForgiveness;
  const timingReleaseType = assistedTiming.releaseType;
  const timing = {
    timingOffset,
    timingAbs,
    releaseType: timingReleaseType,
    isPerfect: timingReleaseType === "perfect",
  };
  if (TIMING_ONLY_SHOTS && !lane.isAI) {
    const charge = clamp(lane.timingCharge ?? lane.power ?? lane.powerLinear ?? 0, 0, 1);
    const bx = lane.ball.x;
    const by = lane.ball.y;
    const hx = hoopX(lane);
    const hy = getRimCenterY(lane);
    const assistRatio = clamp((lane.dynamicAssist || 0) / Math.max(0.001, DYNAMIC_ASSIST_MAX), 0, 1);
    const wobble = lane.state === "charging" ? 0 : (lane.timingWobble || 0) * (1 - assistRatio * 0.65);
    const lateralOffset = Math.abs((lane.ball?.x ?? bx) - lane.home.x);
    const lateralLift = clamp(lateralOffset / TIMING_LATERAL_MOVE_RANGE, 0, 1) * 12;
    const dragAimOffset = clamp(lane.dragAimX || 0, -1, 1) * lane.hoop.r * 0.62;
    const targetX = hx + wobble + dragAimOffset * (1 - assistRatio * 0.38);
    const targetY = hy + lane.hoop.r * TIMING_ENTRY_Y_RATIO;
    const gravity = getGravity();
    let flightTime = clamp(lane.bezierFlightTime || lerp(1.16, 1.02, charge), 1, 1.36);
    const rimClearance =
      lane.hoop.r * (1.32 + charge * 0.5) +
      52 +
      TIMING_RIM_CLEARANCE_BONUS +
      SHOT_FEEL_CONFIG.physics.arcClearanceBonusPx +
      lateralLift;
    for (let i = 0; i < 7; i++) {
      const vyProbe = (targetY - by - 0.5 * gravity * flightTime * flightTime) / flightTime;
      const apexY = by - (vyProbe * vyProbe) / (2 * gravity);
      if (apexY <= targetY - rimClearance) break;
      flightTime = Math.min(1.22, flightTime + 0.05);
    }
    const vx = (targetX - bx) / flightTime;
    const vy = (targetY - by - 0.5 * gravity * flightTime * flightTime) / flightTime;
    const previewStepMul = clamp(
      Number.isFinite(VISUAL_TUNE.previewStepMul) ? VISUAL_TUNE.previewStepMul : 1,
      0.55,
      1
    );
    const previewSteps = Math.max(
      18,
      Math.round(Math.max(26, Math.min(ShotTuning.previewSteps, 64)) * previewStepMul)
    );
      const previewTime = Math.min(1.32, flightTime + 0.2);
    const points = [];
    for (let i = 0; i <= previewSteps; i++) {
      const t = i / previewSteps;
      const s = t * previewTime;
      const x = bx + vx * s;
      const y = by + vy * s + 0.5 * gravity * s * s;
      points.push({ x, y });
    }
    // Continue the same launch model until floor contact so landing marker matches real flight.
    let landingX = bx + vx * flightTime;
    let landingY = getBallGroundYAt(targetY);
    const landingWindow = Math.max(1.45, flightTime + 0.92);
    const landingSteps = 96;
    for (let i = 0; i <= landingSteps; i++) {
      const t = i / landingSteps;
      const s = t * landingWindow;
      const x = bx + vx * s;
      const y = by + vy * s + 0.5 * gravity * s * s;
      const groundY = getBallGroundYAt(y);
      landingX = x;
      landingY = groundY;
      if (y >= groundY) break;
    }
    const alignment = clamp(1 - Math.abs(wobble) / (lane.hoop.r * (TIMING_SCORE_WINDOW_RATIO * 2.3)), 0, 1);
    return {
      points,
      landingX,
      floorY: landingY,
      alignment,
      timing,
      stability: clamp(0.4 + (lane.timingAccuracy || 0) * 0.6, 0.1, 1),
      vx,
      vy,
    };
  }
  const casual = lane.aimMode === "casual";
  const timingPenaltyMul =
    (casual ? 0.56 : 1) *
    controlTune.timingPenaltyMul *
    (tune.timingPenaltyMul ?? 1) *
    (easyPlusTune?.timingPenaltyMul ?? 1);
  const lateralPenaltyMul = (tune.lateralPenaltyMul ?? 1) * (easyPlusTune?.lateralPenaltyMul ?? 1);
  const lateralAimMul = (tune.lateralAimMul ?? 1) * (easyPlusTune?.lateralAimMul ?? 1);
  const idealPower = tune.idealPower;
  const idealAngle = tune.idealAngle;
  const hx = hoopX(lane);
  const hy = getRimCenterY(lane);

  const angleScore = clamp(1 - Math.abs(lane.angle - 53) / 18, 0, 1);
  const powerScore = clamp(1 - Math.abs(lane.power - center) / Math.max(0.06, width * 1.1), 0, 1);
  const timingScore =
    timingReleaseType === "perfect"
      ? 1
      : timingReleaseType === "early"
        ? clamp((casual ? 0.86 : 0.78) - timingAbs * (casual ? 0.11 : 0.16) * timingPenaltyMul, 0.3, 0.84)
        : clamp((casual ? 0.82 : 0.72) - timingAbs * (casual ? 0.1 : 0.14) * timingPenaltyMul, 0.28, 0.82);
  const stability = clamp(angleScore * 0.37 + powerScore * 0.37 + timingScore * 0.26, 0.05, 1);

  let aimAssist =
    timingReleaseType === "perfect" ? 0.36 + stability * 0.34 : 0.08 + stability * 0.2;
  if (casual) aimAssist += 0.12;
  if (easyPlusActive) aimAssist += easyPlusTune.assistAdd;
  aimAssist += lane.dynamicAssist || 0;
  aimAssist += controlTune.assistBoost;
  aimAssist = clamp((aimAssist + tune.assistBias) * aimTune.assistMul, 0.02, 0.96);
  aimAssist = clamp(aimAssist * (1 + (lateralForgiveness - 1) * 0.48), 0.02, 0.98);
  aimAssist *= getAimAssistGate(lane, lane.ball.x, lane.ball.y, hx, hy);

  const powerErr = Math.abs(lane.power - idealPower);
  const angleErr = Math.abs(lane.angle - idealAngle) / 20;
  let spread = 3 + powerErr * 21 + angleErr * 16 * lateralPenaltyMul;
  spread *= tune.spreadMul;
  spread *= aimTune.spreadMul;
  spread *= controlTune.spreadMul;
  spread *= easyPlusTune?.spreadMul ?? 1;
  spread *= 1 + clamp(lane.sloppySwipe || 0, 0, 1) * ShotTuning.sloppySwipePenalty;
  spread *= 1.06 - stability * 0.42;
  spread *= clamp(1 - (lateralForgiveness - 1) * 0.4, 0.66, 1.2);
  if (timingReleaseType === "perfect") spread *= 0.62;

  const manualPull = getManualPull(lane);
  const dir = Math.sign(hx - lane.ball.x) || 1;
  const inputNorm = clamp(
    (lane.powerLinear - getDragPowerMin()) / Math.max(0.001, getDragPowerMax() - getDragPowerMin()),
    0,
    1
  );
  const swipeNormRaw =
    lane.swipePixels > 0
      ? clamp(
          (lane.swipePixels - ShotTuning.minSwipePixels) /
            Math.max(1, ShotTuning.maxSwipePixels - ShotTuning.minSwipePixels),
          0,
          1
        )
      : inputNorm;
  const shortBoost = swipeNormRaw < 0.3 ? lerp(1.08, 1, swipeNormRaw / 0.3) : 1;
  const longDamp = swipeNormRaw > 0.84 ? lerp(1, 0.84, (swipeNormRaw - 0.84) / 0.16) : 1;
  const dynamicPowerScale = shortBoost * longDamp;
  const angleBias = ((lane.angle - idealAngle) / 15) * lateralAimMul;
  let launchAngle = clamp(
    lane.angle + angleBias * 2.2 - manualPull * (casual ? 6.4 : 7.2),
    MIN_LAUNCH_ANGLE_DEG,
    MAX_LAUNCH_ANGLE_DEG
  );
  launchAngle += (arcHeightTuning - 1) * 7.5;
  let speedScale = clamp(0.78 + lane.power * 0.46, 0.72, 1.2) * dynamicPowerScale;
  speedScale *= clamp(1 - (arcHeightTuning - 1) * 0.14, 0.86, 1.16);
  if (timingReleaseType === "early") {
    launchAngle -= 4.2 + Math.min(5.2, timingAbs * 1.8);
    speedScale *= 1.05;
  } else if (timingReleaseType === "late") {
    launchAngle += 3.4 + Math.min(4.6, timingAbs * 1.5);
    speedScale *= 0.93;
  }
  launchAngle = clamp(launchAngle, MIN_LAUNCH_ANGLE_DEG, MAX_LAUNCH_ANGLE_DEG);
  if (lane.power > 0.92) speedScale *= 0.84;

  const previewFlick = Math.max(lane.flickStrength || 0, controlTune.minFlick);
  const flickLift = previewFlick - controlTune.minFlick;
  launchAngle = clamp(
    launchAngle + flickLift * 3.4 - (lane.followArcAdjust || 0) * 0.02,
    MIN_LAUNCH_ANGLE_DEG,
    MAX_LAUNCH_ANGLE_DEG
  );
  const launch = solveLaunchVelocity(lane, launchAngle, speedScale);
  const gravity = getGravity();
  let vx = launch.vx;
  let vy = launch.vy;
  const desiredApexY = hy - lane.hoop.r * (1.05 + SHOT_FEEL_CONFIG.arc.minRimClearancePx / 100);
  const riseNeeded = Math.max(0, lane.ball.y - desiredApexY);
  if (riseNeeded > 0) {
    const minUpVel = Math.sqrt(2 * gravity * riseNeeded);
    vy = -Math.max(Math.abs(vy), minUpVel);
  }

  const dxToRim = hx - lane.ball.x;
  const depthToRim = Math.max(160, lane.ball.y - hy);
  const tApprox = clamp(
    depthToRim / Math.max(620, Math.abs(vy) + Math.abs(vx) * 0.35),
    0.45,
    1.05
  );
  const targetVy = (hy - lane.ball.y - 0.5 * gravity * tApprox * tApprox) / tApprox;
  const assistScale = 1 - Math.abs(manualPull) * 0.5;
  const nearRimRadius = lane.hoop.r * 2.2;
  const nearRim = clamp(1 - Math.abs(dxToRim) / nearRimRadius, 0, 1);
  const assistLerp = clamp(aimAssist * 0.18 * assistScale + nearRim * 0.05, 0.015, 0.22);
  vx = lerp(vx, dxToRim / tApprox, assistLerp);
  vy = lerp(vy, targetVy, assistLerp * 0.58);

  const deterministicSpread = Math.sin(lane.angle * 0.27 + lane.power * 11.3 + lane.streak * 0.61);
  vx += deterministicSpread * spread * 0.9;
  vy += deterministicSpread * spread * 0.42;

  const points = [];
  let px = lane.ball.x;
  let py = lane.ball.y;
  let pvx = vx;
  let pvy = vy;
  const previewDt = ShotTuning.previewStepTime;
  const previewSteps = ShotTuning.previewSteps;
  let boardCooldown = 0;
  let landingX = px;
  let landingY = getBallGroundYAt(py);
  let crossingDist = Infinity;
  let lastPy = py;
  for (let i = 0; i < previewSteps; i++) {
    const prevX = px;
    const prevY = py;
    points.push({ x: px, y: py });
    px += pvx * previewDt;
    py += pvy * previewDt;
    pvy += gravity * previewDt;
    const dampingProbe = { vx: pvx, vy: pvy, spin: 0 };
    applyBallDamping(dampingProbe, previewDt);
    pvx = dampingProbe.vx;
    pvy = dampingProbe.vy;

    boardCooldown = Math.max(0, boardCooldown - previewDt);
    if (boardCooldown <= 0) {
      const previewBall = { x: px, y: py, vx: pvx, vy: pvy };
      if (applyBackboardCollision(lane, previewBall, prevX, prevY, stability)) {
        px = previewBall.x;
        py = previewBall.y;
        pvx = previewBall.vx;
        pvy = previewBall.vy;
        boardCooldown = 0.065;
      }
    }

    if (pvy > 40) {
      const toX = hx - px;
      const toY = hy - py;
      const rimAssistRadius = lane.hoop.r * 4.8;
      const rimDist = Math.hypot(toX, toY);
      if (rimDist < rimAssistRadius) {
      const settle =
          (lane.releaseSmooth || 0.5) *
          clamp((rimAssistRadius - rimDist) / rimAssistRadius, 0, 1) *
          previewDt;
        pvx = lerp(pvx, pvx + toX * 2.12, settle * 0.72);
        pvy = lerp(pvy, pvy + toY * 1.28, settle * 0.42);
      }
    }
    if (lastPy < hy && py >= hy && pvy > 0) crossingDist = Math.min(crossingDist, Math.hypot(px - hx, py - hy));
    lastPy = py;
    const groundY = getBallGroundYAt(py);
    if (py >= groundY) {
      landingX = px;
      landingY = groundY;
      break;
    }
    landingX = px;
    landingY = groundY;
  }
  const alignment = clamp(1 - crossingDist / (lane.hoop.r * 1.2), 0, 1);
  return { points, landingX, floorY: landingY, alignment, timing, stability, vx, vy };
};

const createLane = ({
  id,
  label,
  centerX,
  side,
  isAI,
  accent,
  avatar,
  rank,
  ballSkin,
  ballColor = "purple",
  courtSkin,
  aiStyle = "rhythm",
  shotDifficulty = "medium",
  aimMode = "casual",
  controlProfile = "rookie",
  dominantHand = "right",
  aimSensitivityScale = 1,
  easyPlusAssist = false,
  arcHeightTuning = 1,
  lateralForgiveness = 1,
  releaseForgiveness = 1,
}) => {
  const home = { x: centerX + side * 115, y: 620 };
  return {
    id,
    label,
    centerX,
    side,
    isAI,
    accent,
    avatar,
    rank,
    ballSkin,
    ballColor,
    courtSkin,
    home,
    hoop: { x: centerX, y: HOOP_BASE_Y, r: DEFAULT_HOOP_RADIUS },
    state: "idle",
    powerLinear: 0.35,
    power: 0.2,
    powerDir: 1,
    angle: 53,
    angleDir: 1,
    shotStep: 0,
    prevX: home.x,
    prevY: home.y,
    prevVx: 0,
    prevVy: 0,
    renderX: home.x,
    renderY: home.y,
    renderVx: 0,
    renderVy: 0,
    breathPhase: rand(0, Math.PI * 2),
    wristSnap: 0,
    guideRelease: 0,
    followHold: 0,
    handJitter: 0,
    fingerSeed: [rand(-1, 1), rand(-1, 1), rand(-1, 1), rand(-1, 1)],
    crossChecked: false,
    clearedRimPlane: false,
    releaseBurst: 0,
    releaseFlash: 0,
    releaseCue: 0,
    releaseSpark: 0,
    landingPulse: 0,
    wasPerfectWindow: false,
    chargeStartLinear: 0.35,
    rimJolt: 0,
    rimJoltPhase: 0,
    boardHitCooldown: 0,
    hitBackboard: false,
    rimHitCooldown: 0,
    rimTouched: false,
    rimHitType: "",
    hoopOffsetX: 0,
    hoopOffsetY: 0,
    hoopMovePhase: rand(0, Math.PI * 2),
    hoopMoveAmpX: 0,
    hoopMoveAmpY: 0,
    hoopMoveSpeed: rand(1.2, 1.9),
    hoopMoveCooldown: rand(0.6, 1.6),
    hoopPulse: 0.35,
    lanePulse: 0,
    scorePop: 0,
    comboPop: 0,
    netWave: 0,
    trail: [],
    previewAlpha: 0,
    previewTTL: 0,
    previewPath: null,
    previewCalcCooldown: 0,
    previewKey: "",
    releaseWindow: { center: 0.7, width: 0.14 },
    chargeElapsed: 0,
    timingCharge: 0.2,
    timingAccuracy: 0,
    timingWobble: 0,
    bezierFlightTime: 0.82,
    releaseType: "normal",
    releaseAssist: 0,
    shotStability: 0.5,
    aimAssist: 0,
    flickStrength: 0,
    flickVelocity: 0,
    aimX: home.x,
    aimY: home.y - 120,
    shotResolved: false,
    madeShot: false,
    forceMake: false,
    forceSwish: false,
    telemetryAttemptLogged: false,
    telemetryOutcomeLogged: false,
    floorBounces: 0,
    settleTimer: 0,
    dynamicAssist: 0,
    ballSquash: 0,
    impactVibe: 0,
    sideSpin: 0,
    followWindow: 0,
    followSpinBoost: 0,
    followArcAdjust: 0,
    manualPull: 0,
    preShotLateral: 0,
    dragAimX: 0,
    dragAimY: 1,
    dragDist: 0,
    lastArcClearPx: 0,
    lastArcApexY: 0,
    lastArcRimPlaneY: 0,
    releaseSmooth: 0.5,
    wobbleAmp: 0,
    wobblePhase: 0,
    swipePixels: 0,
    swipeMs: 0,
    sloppySwipe: 0,
    nextReleaseAt: 0,
    chargeToneCooldown: 0,
    aimLockCooldown: 0,
    fireSpawnTimer: 0,
    backspinBias: 0,
    netState: createNetState(),
    aiCooldown: rand(0.7, 1.4),
    aiHold: 0,
    aiStyle,
    shotDifficulty,
    aimMode,
    controlProfile,
    dominantHand,
    aimSensitivityScale,
    easyPlusAssist,
    arcHeightTuning,
    lateralForgiveness,
    releaseForgiveness,
    aiSkillBias: rand(-0.06, 0.08),
    dailyModifier: "none",
    quickReturnMul: 1,
    hotZoneX: centerX,
    hotZoneR: 42,
    windForce: 0,
    score: 0,
    streak: 0,
    misses: 0,
    rimFire: false,
    outcomeTier: "idle",
    clutchBonusActive: false,
    hotZoneBoost: false,
    ball: {
      x: home.x,
      y: home.y,
      vx: 0,
      vy: 0,
      spin: 0,
      hue: getBallColorway(ballColor).hue,
    },
    launchX: home.x,
    launchY: home.y,
    returnTime: 0,
    flash: "",
    flashColor: "#66f5ff",
    flashAlpha: 0,
    calloutCursor: (Math.random() * 3) | 0,
    recycleOut: false,
    recycleTimer: 0,
    stats: {
      attempts: 0,
      makes: 0,
      swishes: 0,
      rims: 0,
      misses: 0,
      perfectReleases: 0,
      earlyReleases: 0,
      lateReleases: 0,
      totalStability: 0,
      totalAngleError: 0,
      clutchPoints: 0,
      hotZoneMakes: 0,
      rimIn: 0,
    },
  };
};

function spawnBurst(arr, x, y, color, count = 18, speed = 360, quality = 1) {
  const particleBudgetMul = Number.isFinite(VISUAL_TUNE.particleBudgetMul)
    ? VISUAL_TUNE.particleBudgetMul
    : 1;
  const scaledCount = Math.max(3, Math.round(count * quality * 0.85 * particleBudgetMul));
  for (let i = 0; i < scaledCount; i++) {
    const a = (Math.PI * 2 * i) / scaledCount + Math.random() * 0.35;
    const s = speed * (0.45 + Math.random() * 0.9);
    arr.push({
      x,
      y,
      vx: Math.cos(a) * s,
      vy: Math.sin(a) * s - Math.random() * 140,
      life: 1,
      size: 2 + Math.random() * 4,
      color,
      shrink: 0.93,
    });
  }
  const particleCap = Math.max(18, Math.round(MAX_PARTICLES * particleBudgetMul));
  if (arr.length > particleCap) arr.splice(0, arr.length - particleCap);
}

function spawnFire(arr, x, y, quality = 1) {
  const particleBudgetMul = Number.isFinite(VISUAL_TUNE.particleBudgetMul)
    ? VISUAL_TUNE.particleBudgetMul
    : 1;
  const colors = ["#ff4500", "#ff6b00", "#ffd700", "#ff2f00", "#ff9a00"];
  const count = Math.max(1, Math.round(3 * quality * particleBudgetMul));
  for (let i = 0; i < count; i++) {
    arr.push({
      x: x + rand(-26, 26),
      y: y + rand(-6, 6),
      vx: rand(-40, 40),
      vy: rand(-220, -120),
      life: 0.92,
      size: rand(4, 9),
      color: colors[(Math.random() * colors.length) | 0],
      shrink: 0.95,
    });
  }
  const particleCap = Math.max(18, Math.round(MAX_PARTICLES * particleBudgetMul));
  if (arr.length > particleCap) arr.splice(0, arr.length - particleCap);
}

function drawGlowLine(ctx, x1, y1, x2, y2, color, width = 2.2, blur = 12, alpha = 1) {
  ctx.save();
  ctx.strokeStyle = color;
  ctx.lineCap = "round";
  ctx.globalAlpha = alpha * 0.2;
  ctx.lineWidth = width + 6;
  ctx.shadowColor = color;
  ctx.shadowBlur = blur * 2;
  ctx.beginPath();
  ctx.moveTo(x1, y1);
  ctx.lineTo(x2, y2);
  ctx.stroke();
  ctx.globalAlpha = alpha;
  ctx.lineWidth = width;
  ctx.shadowBlur = blur;
  ctx.beginPath();
  ctx.moveTo(x1, y1);
  ctx.lineTo(x2, y2);
  ctx.stroke();
  ctx.restore();
}

function drawBackground(ctx, t, parallax = 0, chargeGlow = 0) {
  const haze = VISUAL_TUNE.hazeStrength;
  const centerFade = VISUAL_TUNE.centerLaneFade;
  const bg = ctx.createRadialGradient(CW * 0.5, CH * 0.33, 80, CW * 0.5, CH * 0.5, CW);
  bg.addColorStop(0, "#1f5f96");
  bg.addColorStop(0.46, "#102949");
  bg.addColorStop(1, "#040a14");
  ctx.fillStyle = bg;
  ctx.fillRect(0, 0, CW, CH);

  const bloom = ctx.createRadialGradient(CW * 0.5, CH * 0.24, 40, CW * 0.5, CH * 0.25, CW * 0.55);
  bloom.addColorStop(0, `rgba(125,235,255,${0.2 * haze})`);
  bloom.addColorStop(0.6, `rgba(30,170,255,${0.08 * haze})`);
  bloom.addColorStop(1, "transparent");
  ctx.fillStyle = bloom;
  ctx.fillRect(0, 0, CW, CH);
  const sideGlow = ctx.createLinearGradient(0, 0, CW, 0);
  sideGlow.addColorStop(0, `rgba(255,88,206,${0.06 + haze * 0.035})`);
  sideGlow.addColorStop(0.25, "transparent");
  sideGlow.addColorStop(0.75, "transparent");
  sideGlow.addColorStop(1, `rgba(96,244,255,${0.055 + haze * 0.03})`);
  ctx.fillStyle = sideGlow;
  ctx.fillRect(0, 0, CW, CH);
  const centerBand = ctx.createLinearGradient(CW * 0.38, 0, CW * 0.62, 0);
  centerBand.addColorStop(0, "transparent");
  centerBand.addColorStop(0.45, `rgba(6,18,34,${0.18 + centerFade * 0.18})`);
  centerBand.addColorStop(0.55, `rgba(6,18,34,${0.18 + centerFade * 0.18})`);
  centerBand.addColorStop(1, "transparent");
  ctx.fillStyle = centerBand;
  ctx.fillRect(0, 0, CW, CH);
  const farHaze = ctx.createLinearGradient(0, 0, 0, CH * 0.48);
  farHaze.addColorStop(0, `rgba(142,102,255,${0.045 + haze * 0.08})`);
  farHaze.addColorStop(0.72, `rgba(52,86,180,${0.02 + haze * 0.04})`);
  farHaze.addColorStop(1, "transparent");
  ctx.fillStyle = farHaze;
  ctx.fillRect(0, 0, CW, CH * 0.58);

  ctx.save();
  ctx.strokeStyle = `rgba(80,160,255,${0.06 + haze * 0.03})`;
  ctx.lineWidth = 1;
  for (let y = 0; y < CH; y += 34) {
    const wobble = Math.sin(t * 0.0007 + y * 0.04) * 3;
    const drift = Math.sin(t * 0.00025 + y * 0.03) * 8 + parallax * 0.5;
    ctx.beginPath();
    ctx.moveTo(-60 + drift, y + wobble);
    ctx.lineTo(CW + 60 + drift * 0.45, y - wobble);
    ctx.stroke();
  }
  // Architectural vanishing lines to pull the eye upward.
  const vanishX = CW * 0.5;
  const vanishY = CH * 0.06;
  ctx.strokeStyle = `rgba(108,206,255,${0.05 + haze * 0.035})`;
  for (let i = 0; i <= 18; i++) {
    const k = i / 18;
    const x = lerp(-120, CW + 120, k);
    ctx.beginPath();
    ctx.moveTo(x, CH + 36);
    ctx.lineTo(vanishX + (x - CW * 0.5) * 0.12, vanishY);
    ctx.stroke();
  }
  ctx.restore();

  if (chargeGlow > 0.01) {
    const bloom = ctx.createRadialGradient(CW * 0.5, CH * 0.72, 50, CW * 0.5, CH * 0.72, CW * 0.45);
    bloom.addColorStop(0, `rgba(0, 238, 255, ${0.07 + chargeGlow * 0.2})`);
    bloom.addColorStop(1, "transparent");
    ctx.fillStyle = bloom;
    ctx.fillRect(0, 0, CW, CH);
  }

  const floor = ctx.createLinearGradient(0, CH - 240, 0, CH);
  floor.addColorStop(0, "rgba(176,104,255,0.14)");
  floor.addColorStop(0.44, "rgba(140,72,234,0.22)");
  floor.addColorStop(0.78, "rgba(109,52,208,0.29)");
  floor.addColorStop(1, "rgba(86,34,176,0.42)");
  ctx.fillStyle = floor;
  ctx.fillRect(0, CH - 240, CW, 240);
}

function drawArcadeRig(ctx, leftLane, rightLane, t) {
  if (!leftLane || !rightLane) return;
  const centerFade = VISUAL_TUNE.centerLaneFade;
  const rimDepthGlow = VISUAL_TUNE.rimDepthGlow;

  const nearY = 708;
  const farY = 236;
  const leftOuterNear = leftLane.centerX - 336;
  const leftOuterFar = leftLane.centerX - 84;
  const rightOuterNear = rightLane.centerX + 336;
  const rightOuterFar = rightLane.centerX + 84;

  ctx.save();

  // Outer side rails.
  drawGlowLine(ctx, leftOuterNear, nearY, leftOuterFar, farY, "#6fd8ff", 2.5, 11, 0.92);
  drawGlowLine(ctx, rightOuterNear, nearY, rightOuterFar, farY, "#6fd8ff", 2.5, 11, 0.92);

  // Top and floor frame.
  drawGlowLine(ctx, leftOuterFar, farY, rightOuterFar, farY, "#7be7ff", 2.2, 8, 0.74);
  drawGlowLine(ctx, leftOuterNear, nearY, rightOuterNear, nearY, "#56cfff", 2.2, 9, 0.85);

  // Center divider and cage-style netting across both lanes.
  const centerNear = (leftLane.centerX + rightLane.centerX) * 0.5;
  const dividerPulse = 0.6 + Math.sin(t * 0.0034) * 0.28;
  const dividerGlow = ctx.createLinearGradient(centerNear - 90, nearY, centerNear + 90, farY);
  dividerGlow.addColorStop(0, "transparent");
  dividerGlow.addColorStop(0.22, `rgba(197,122,255,${0.14 + dividerPulse * 0.08})`);
  dividerGlow.addColorStop(0.5, `rgba(112,232,255,${0.2 + dividerPulse * 0.16})`);
  dividerGlow.addColorStop(0.78, `rgba(197,122,255,${0.14 + dividerPulse * 0.08})`);
  dividerGlow.addColorStop(1, "transparent");
  ctx.fillStyle = dividerGlow;
  ctx.fillRect(centerNear - 130, farY - 8, 260, nearY - farY + 24);
  drawGlowLine(
    ctx,
    centerNear,
    nearY - 8,
    centerNear,
    farY + 14,
    "rgba(168,242,255,0.92)",
    2.3,
    12,
    0.64 + centerFade * 0.2 + dividerPulse * 0.18
  );
  const centerMatte = ctx.createLinearGradient(centerNear - 130, nearY, centerNear + 130, farY);
  centerMatte.addColorStop(0, "transparent");
  centerMatte.addColorStop(0.25, `rgba(46,20,82,${0.13 + centerFade * 0.09})`);
  centerMatte.addColorStop(0.5, `rgba(12,30,48,${0.18 + centerFade * 0.12})`);
  centerMatte.addColorStop(0.75, `rgba(46,20,82,${0.13 + centerFade * 0.09})`);
  centerMatte.addColorStop(1, "transparent");
  ctx.fillStyle = centerMatte;
  ctx.fillRect(centerNear - 160, farY, 320, nearY - farY + 12);

  ctx.strokeStyle = "rgba(180,230,255,0.2)";
  ctx.lineWidth = 1;
  for (let i = 0; i <= 20; i++) {
    const p = i / 20;
    const y = lerp(nearY, farY, p);
    const wobble = Math.sin(t * 0.0018 + i * 0.42) * 1.4;
    const lx = lerp(leftOuterNear, leftOuterFar, p);
    const rx = lerp(rightOuterNear, rightOuterFar, p);
    ctx.beginPath();
    ctx.moveTo(lx, y + wobble);
    ctx.lineTo(rx, y - wobble);
    ctx.stroke();
  }
  for (let i = 0; i <= 28; i++) {
    const p = i / 28;
    const xN = lerp(leftOuterNear, rightOuterNear, p);
    const xF = lerp(leftOuterFar, rightOuterFar, p);
    ctx.beginPath();
    ctx.moveTo(xN, nearY);
    ctx.lineTo(xF, farY);
    ctx.stroke();
  }

  // Shared backboard plate to reinforce arcade simulator angle.
  const rigR = Math.max(leftLane.hoop.r, rightLane.hoop.r);
  const boardY = farY - rigR * 3.15;
  const boardH = rigR * 2.55;
  const boardX = leftLane.hoop.x - rigR * 3.35;
  const boardW = (rightLane.hoop.x - leftLane.hoop.x) + rigR * 6.7;
  const boardGrad = ctx.createLinearGradient(boardX, boardY, boardX, boardY + boardH);
  boardGrad.addColorStop(0, "rgba(12,32,54,0.62)");
  boardGrad.addColorStop(1, "rgba(10,24,40,0.48)");
  ctx.fillStyle = boardGrad;
  ctx.fillRect(boardX, boardY, boardW, boardH);
  drawGlowLine(ctx, boardX, boardY, boardX + boardW, boardY, "#86ddff", 1.8, 9 * rimDepthGlow);
  drawGlowLine(
    ctx,
    boardX,
    boardY + boardH,
    boardX + boardW,
    boardY + boardH,
    "#86ddff",
    1.8,
    9 * rimDepthGlow
  );
  drawGlowLine(ctx, boardX, boardY, boardX, boardY + boardH, "#86ddff", 1.4, 7 * rimDepthGlow, 0.7);
  drawGlowLine(
    ctx,
    boardX + boardW,
    boardY,
    boardX + boardW,
    boardY + boardH,
    "#86ddff",
    1.4,
    7 * rimDepthGlow,
    0.7
  );

  ctx.restore();
}

function drawLaneSurface(ctx, lane, t, turnPulse = 0, chargeGlow = 0, quality = 1) {
  const paintIntensity = VISUAL_TUNE.courtPaintIntensity;
  const lanePulseGain = VISUAL_TUNE.lanePulseGain;
  const nearY = 706;
  const farY = 252;
  const nearHalf = Math.round(310 * VISUAL_TUNE.courtWidthMul);
  const farHalf = Math.round(86 * VISUAL_TUNE.courtFarWidthMul);

  const p1 = { x: lane.centerX - nearHalf, y: nearY };
  const p2 = { x: lane.centerX + nearHalf, y: nearY };
  const p3 = { x: lane.centerX + farHalf, y: farY };
  const p4 = { x: lane.centerX - farHalf, y: farY };
  const court = lane.courtSkin || "neon";
  const courtStops =
    court === "carbon"
      ? ["#1b1a28", "#211f33", "#2a2540"]
      : court === "sunset"
        ? ["#3d1636", "#5f1e4a", "#7f2863"]
        : ["#220f3d", "#33185a", "#4a2480"];

  const lanePoint = (k, u = 0.5) => {
    const lx = lerp(p1.x, p4.x, k);
    const ly = lerp(p1.y, p4.y, k);
    const rx = lerp(p2.x, p3.x, k);
    const ry = lerp(p2.y, p3.y, k);
    return { x: lerp(lx, rx, u), y: lerp(ly, ry, u) };
  };
  const strokeCourtLine = (pts, color, width = 1.8, glow = 10, alpha = 0.9, closed = false) => {
    if (!pts || pts.length < 2) return;
    ctx.save();
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    ctx.globalAlpha = alpha * 0.22;
    ctx.strokeStyle = color;
    ctx.lineWidth = width + 3.2;
    ctx.shadowColor = color;
    ctx.shadowBlur = glow * 1.8;
    ctx.beginPath();
    ctx.moveTo(pts[0].x, pts[0].y);
    for (let i = 1; i < pts.length; i++) ctx.lineTo(pts[i].x, pts[i].y);
    if (closed) ctx.closePath();
    ctx.stroke();
    ctx.globalAlpha = alpha;
    ctx.lineWidth = width;
    ctx.shadowBlur = glow;
    ctx.beginPath();
    ctx.moveTo(pts[0].x, pts[0].y);
    for (let i = 1; i < pts.length; i++) ctx.lineTo(pts[i].x, pts[i].y);
    if (closed) ctx.closePath();
    ctx.stroke();
    ctx.restore();
  };
  const buildArc = (uCenter, kCenter, uRadius, kRadius, start, end, segments) => {
    const pts = [];
    const count = Math.max(8, segments);
    for (let i = 0; i <= count; i++) {
      const a = lerp(start, end, i / count);
      const u = clamp(uCenter + Math.cos(a) * uRadius, 0.02, 0.98);
      const k = clamp(kCenter - Math.sin(a) * kRadius, 0.02, 0.98);
      pts.push(lanePoint(k, u));
    }
    return pts;
  };

  ctx.save();
  const g = ctx.createLinearGradient(0, nearY, 0, farY);
  g.addColorStop(0, courtStops[0]);
  g.addColorStop(0.55, courtStops[1]);
  g.addColorStop(1, courtStops[2]);
  ctx.fillStyle = g;
  ctx.beginPath();
  ctx.moveTo(p1.x, p1.y);
  ctx.lineTo(p2.x, p2.y);
  ctx.lineTo(p3.x, p3.y);
  ctx.lineTo(p4.x, p4.y);
  ctx.closePath();
  ctx.fill();

  const matte = ctx.createLinearGradient(0, nearY, 0, farY);
  matte.addColorStop(0, `rgba(9,5,20,${0.2 + paintIntensity * 0.08})`);
  matte.addColorStop(0.58, `rgba(22,11,44,${0.14 + paintIntensity * 0.08})`);
  matte.addColorStop(1, `rgba(34,16,58,${0.08 + paintIntensity * 0.08})`);
  ctx.fillStyle = matte;
  ctx.beginPath();
  ctx.moveTo(p1.x, p1.y);
  ctx.lineTo(p2.x, p2.y);
  ctx.lineTo(p3.x, p3.y);
  ctx.lineTo(p4.x, p4.y);
  ctx.closePath();
  ctx.fill();
  const fullPaint = ctx.createLinearGradient(p1.x, nearY, p4.x, farY);
  fullPaint.addColorStop(0, `rgba(189,92,255,${0.18 + paintIntensity * 0.1})`);
  fullPaint.addColorStop(0.5, `rgba(144,68,236,${0.14 + paintIntensity * 0.08})`);
  fullPaint.addColorStop(1, `rgba(92,46,188,${0.11 + paintIntensity * 0.07})`);
  ctx.fillStyle = fullPaint;
  ctx.beginPath();
  ctx.moveTo(p1.x, p1.y);
  ctx.lineTo(p2.x, p2.y);
  ctx.lineTo(p3.x, p3.y);
  ctx.lineTo(p4.x, p4.y);
  ctx.closePath();
  ctx.fill();

  // Layered depth bands make the lane feel fuller and more 4D.
  const depthBands = quality > 0.9 ? 7 : quality > 0.82 ? 6 : 4;
  for (let i = 0; i < depthBands; i++) {
    const k0 = i / depthBands;
    const k1 = (i + 1) / depthBands;
    const aL = { x: lerp(p1.x, p4.x, k0), y: lerp(p1.y, p4.y, k0) };
    const aR = { x: lerp(p2.x, p3.x, k0), y: lerp(p2.y, p3.y, k0) };
    const bL = { x: lerp(p1.x, p4.x, k1), y: lerp(p1.y, p4.y, k1) };
    const bR = { x: lerp(p2.x, p3.x, k1), y: lerp(p2.y, p3.y, k1) };
    const bandAlpha = (1 - k0) * (0.12 + paintIntensity * 0.05);
    const depthGrad = ctx.createLinearGradient(0, aL.y, 0, bL.y);
    depthGrad.addColorStop(0, `rgba(226,136,255,${bandAlpha * 0.7})`);
    depthGrad.addColorStop(1, `rgba(66,30,136,${bandAlpha * 0.3})`);
    ctx.fillStyle = depthGrad;
    ctx.beginPath();
    ctx.moveTo(aL.x, aL.y);
    ctx.lineTo(aR.x, aR.y);
    ctx.lineTo(bR.x, bR.y);
    ctx.lineTo(bL.x, bL.y);
    ctx.closePath();
    ctx.fill();
  }

  ctx.save();
  ctx.beginPath();
  ctx.moveTo(p1.x, p1.y);
  ctx.lineTo(p2.x, p2.y);
  ctx.lineTo(p3.x, p3.y);
  ctx.lineTo(p4.x, p4.y);
  ctx.closePath();
  ctx.clip();
  const depthRays = quality > 0.9 ? 6 : 4;
  for (let i = 0; i < depthRays; i++) {
    const rayOffset = ((i / Math.max(1, depthRays - 1)) - 0.5) * nearHalf * 1.15;
    const ray = ctx.createLinearGradient(
      lane.centerX + rayOffset * 0.22,
      farY + 10,
      lane.centerX + rayOffset,
      nearY
    );
    ray.addColorStop(0, "transparent");
    ray.addColorStop(0.55, "rgba(255,182,255,0.08)");
    ray.addColorStop(1, "rgba(120,62,218,0.18)");
    ctx.fillStyle = ray;
    ctx.fillRect(p1.x - 30, farY, nearHalf * 2 + 60, nearY - farY + 30);
  }
  ctx.restore();
  const laneSpec = ctx.createLinearGradient(0, nearY, 0, farY);
  laneSpec.addColorStop(0, `rgba(255,212,255,${0.06 + paintIntensity * 0.05})`);
  laneSpec.addColorStop(0.42, "transparent");
  laneSpec.addColorStop(1, `rgba(58,24,110,${0.12 + paintIntensity * 0.08})`);
  ctx.fillStyle = laneSpec;
  ctx.beginPath();
  ctx.moveTo(p1.x, p1.y);
  ctx.lineTo(p2.x, p2.y);
  ctx.lineTo(p3.x, p3.y);
  ctx.lineTo(p4.x, p4.y);
  ctx.closePath();
  ctx.fill();

  drawGlowLine(
    ctx,
    p1.x,
    p1.y,
    p4.x,
    p4.y,
    "#ca7dff",
    2,
    10,
    0.82 + chargeGlow * 0.3 + lane.lanePulse * 0.2
  );
  drawGlowLine(
    ctx,
    p2.x,
    p2.y,
    p3.x,
    p3.y,
    "#ca7dff",
    2,
    10,
    0.82 + chargeGlow * 0.3 + lane.lanePulse * 0.2
  );

  ctx.strokeStyle = "rgba(217,172,255,0.2)";
  ctx.lineWidth = 1;
  const rowCount = quality > 0.9 ? 10 : quality > 0.82 ? 8 : 6;
  const colCount = quality > 0.9 ? 12 : quality > 0.82 ? 9 : 7;
  for (let i = 0; i <= rowCount; i++) {
    const k = i / rowCount;
    const lx = lerp(p1.x, p4.x, k);
    const ly = lerp(p1.y, p4.y, k);
    const rx = lerp(p2.x, p3.x, k);
    const ry = lerp(p2.y, p3.y, k);
    ctx.beginPath();
    ctx.moveTo(lx, ly);
    ctx.lineTo(rx, ry);
    ctx.stroke();
  }

  ctx.strokeStyle = "rgba(245,206,255,0.18)";
  for (let i = 0; i <= colCount; i++) {
    const a = i / colCount;
    const yL = lerp(p1.y, p4.y, a);
    const yR = lerp(p2.y, p3.y, a);
    const xL = lerp(p1.x, p4.x, a);
    const xR = lerp(p2.x, p3.x, a);
    const ixL = lerp(xL, lane.centerX, 0.12);
    const ixR = lerp(xR, lane.centerX, 0.12);
    ctx.beginPath();
    ctx.moveTo(xL, yL);
    ctx.lineTo(ixL, lerp(yL, farY, 0.18));
    ctx.stroke();
    ctx.beginPath();
    ctx.moveTo(xR, yR);
    ctx.lineTo(ixR, lerp(yR, farY, 0.18));
    ctx.stroke();
  }

  const edgePulse = 0.2 + Math.abs(Math.sin(t * 0.003)) * 0.25 + turnPulse * 0.25;
  ctx.fillStyle = `rgba(214,132,255,${edgePulse})`;
  ctx.fillRect(p1.x + 70, p1.y - 8, nearHalf * 2 - 140, 3);
  if (lane.lanePulse > 0.01) {
    const pulse = lane.lanePulse * lanePulseGain;
    const laneEnergy = ctx.createLinearGradient(0, nearY, 0, farY);
    laneEnergy.addColorStop(0, `rgba(251,174,255,${pulse * 0.22})`);
    laneEnergy.addColorStop(0.4, `rgba(188,118,255,${pulse * 0.12})`);
    laneEnergy.addColorStop(1, "transparent");
    ctx.fillStyle = laneEnergy;
    ctx.beginPath();
    ctx.moveTo(p1.x, p1.y);
    ctx.lineTo(p2.x, p2.y);
    ctx.lineTo(p3.x, p3.y);
    ctx.lineTo(p4.x, p4.y);
    ctx.closePath();
    ctx.fill();
  }

  // Neon basketball court markings: key, free throw ring, restricted arc, three-point arc.
  ctx.save();
  ctx.beginPath();
  ctx.moveTo(p1.x, p1.y);
  ctx.lineTo(p2.x, p2.y);
  ctx.lineTo(p3.x, p3.y);
  ctx.lineTo(p4.x, p4.y);
  ctx.closePath();
  ctx.clip();

  const lineMain = "rgba(247,178,255,0.96)";
  const lineAlt = "rgba(169,236,255,0.92)";
  const arcSegments = quality > 0.92 ? 34 : quality > 0.82 ? 24 : 16;

  const keyPoly = [lanePoint(0.94, 0.36), lanePoint(0.94, 0.64), lanePoint(0.69, 0.7), lanePoint(0.69, 0.3)];
  const keyFill = ctx.createLinearGradient(0, lanePoint(0.94, 0.5).y, 0, lanePoint(0.69, 0.5).y);
  keyFill.addColorStop(0, "rgba(214,132,255,0.2)");
  keyFill.addColorStop(1, "rgba(96,196,255,0.08)");
  ctx.fillStyle = keyFill;
  ctx.beginPath();
  ctx.moveTo(keyPoly[0].x, keyPoly[0].y);
  ctx.lineTo(keyPoly[1].x, keyPoly[1].y);
  ctx.lineTo(keyPoly[2].x, keyPoly[2].y);
  ctx.lineTo(keyPoly[3].x, keyPoly[3].y);
  ctx.closePath();
  ctx.fill();
  strokeCourtLine(keyPoly, lineMain, 1.8, 10, 0.82, true);

  const hoopGuide = [lanePoint(0.965, 0.32), lanePoint(0.965, 0.68)];
  strokeCourtLine(hoopGuide, lineAlt, 1.35, 8, 0.72, false);

  const freeThrowArc = buildArc(0.5, 0.69, 0.18, 0.11, Math.PI * 0.06, Math.PI * 0.94, arcSegments * 0.65);
  strokeCourtLine(freeThrowArc, lineAlt, 1.55, 9, 0.78, false);

  const restrictedArc = buildArc(0.5, 0.93, 0.095, 0.048, Math.PI * 0.12, Math.PI * 0.88, arcSegments * 0.5);
  strokeCourtLine(restrictedArc, lineMain, 1.35, 8, 0.8, false);

  const threeArc = buildArc(0.5, 0.915, 0.44, 0.47, Math.PI * 0.08, Math.PI * 0.92, arcSegments);
  strokeCourtLine(threeArc, lineMain, 1.95, 12, 0.9, false);
  strokeCourtLine([lanePoint(0.96, 0.16), threeArc[0]], lineMain, 1.75, 10, 0.86, false);
  strokeCourtLine(
    [lanePoint(0.96, 0.84), threeArc[threeArc.length - 1]],
    lineMain,
    1.75,
    10,
    0.86,
    false
  );

  const centerStripe = [lanePoint(0.98, 0.5), lanePoint(0.08, 0.5)];
  strokeCourtLine(centerStripe, "rgba(196,130,255,0.66)", 1.2, 7, 0.5, false);

  // Lane hash marks and edge LEDs for a fuller court read.
  const hashBands = [0.78, 0.69, 0.6, 0.51];
  hashBands.forEach((k, idx) => {
    const leftA = lanePoint(k, 0.16);
    const leftB = lanePoint(k, 0.24);
    const rightA = lanePoint(k, 0.84);
    const rightB = lanePoint(k, 0.76);
    strokeCourtLine([leftA, leftB], idx % 2 === 0 ? lineMain : lineAlt, 1.25, 7, 0.72, false);
    strokeCourtLine([rightA, rightB], idx % 2 === 0 ? lineMain : lineAlt, 1.25, 7, 0.72, false);
  });

  const edgeNodes = quality > 0.9 ? 10 : 7;
  for (let i = 0; i <= edgeNodes; i++) {
    const a = i / edgeNodes;
    [0.06, 0.94].forEach((u, idx) => {
      const pt = lanePoint(0.18 + a * 0.76, u);
      const nodeColor = idx === 0 ? "rgba(226,152,255,0.84)" : "rgba(138,226,255,0.82)";
      ctx.save();
      ctx.globalAlpha = 0.72;
      ctx.fillStyle = nodeColor;
      ctx.shadowColor = nodeColor;
      ctx.shadowBlur = 9;
      ctx.beginPath();
      ctx.arc(pt.x, pt.y, 1.35, 0, Math.PI * 2);
      ctx.fill();
      ctx.restore();
    });
  }
  ctx.restore();

  // Moving light sweep across lane.
  const sweepX = ((t * 0.32 + lane.centerX * 0.3) % (nearHalf * 4)) - nearHalf * 2;
  const sweep = ctx.createLinearGradient(
    lane.centerX + sweepX - 90,
    nearY,
    lane.centerX + sweepX + 90,
    farY
  );
  sweep.addColorStop(0, "transparent");
  sweep.addColorStop(0.5, "rgba(244,173,255,0.14)");
  sweep.addColorStop(1, "transparent");
  ctx.fillStyle = sweep;
  ctx.beginPath();
  ctx.moveTo(p1.x, p1.y);
  ctx.lineTo(p2.x, p2.y);
  ctx.lineTo(p3.x, p3.y);
  ctx.lineTo(p4.x, p4.y);
  ctx.closePath();
  ctx.fill();

  if (lane.dailyModifier === "hot_zone") {
    const hzY = nearY - 78;
    const hzX = lane.hotZoneX;
    const hzW = 58;
    const hzH = 18;
    ctx.save();
    ctx.globalAlpha = lane.hotZoneBoost ? 0.34 : 0.2;
    ctx.strokeStyle = lane.hotZoneBoost ? "#ffd67f" : "#9ef7ff";
    ctx.shadowColor = lane.hotZoneBoost ? "#ffad49" : "#7ef0ff";
    ctx.shadowBlur = lane.hotZoneBoost ? 14 : 9;
    ctx.lineWidth = lane.hotZoneBoost ? 2.2 : 1.5;
    ctx.beginPath();
    ctx.ellipse(hzX, hzY, hzW, hzH, 0, 0, Math.PI * 2);
    ctx.stroke();
    ctx.restore();
  }

  if (turnPulse > 0.01) {
    ctx.strokeStyle = lane.accent;
    ctx.globalAlpha = 0.1 + turnPulse * 0.35;
    ctx.lineWidth = 4;
    ctx.strokeRect(lane.centerX - nearHalf - 12, farY - 24, nearHalf * 2 + 24, nearY - farY + 42);
  }
  ctx.restore();
}

function drawBackboardAndHoop(ctx, lane, t, quality = 1) {
  const rimDepth = VISUAL_TUNE.rimDepthGlow;
  const r = lane.hoop.r;
  const x = hoopX(lane);
  const y = hoopY(lane);
  const joltX = Math.sin(t * 0.04 + lane.rimJoltPhase) * lane.rimJolt * 8;
  const joltY = Math.cos(t * 0.05 + lane.rimJoltPhase) * lane.rimJolt * 1.1;
  const hx = x + joltX;
  const hy = y + joltY;
  const firePulse = 0.45 + Math.sin(t * 0.02) * 0.2 + lane.hoopPulse * 0.32;
  const rimPrimary = lane.rimFire ? "#ffd86d" : "#ff8a12";
  const rimSecondary = lane.rimFire ? "#ff8f3f" : "#ffb84a";
  const rimCore = lane.rimFire ? "#ffe8a8" : "#fff1cc";
  const boardPrimary = lane.rimFire ? "#ffd086" : "#ff9a1f";
  const boardSecondary = lane.rimFire ? "#fff0bf" : "#ffc85a";
  const rimY = getRimCenterY(lane, hy);
  const depthScale = clamp(1.02 + (rimY / CH) * 0.06, 1, 1.16);
  const netPulse = clamp((lane.netState?.glow || 0) + lane.netWave * 0.2, 0, 1.6);

  ctx.save();

  const reflY = rimY + lane.hoop.r * 2.65;
  const refl = ctx.createRadialGradient(hx, reflY, 8, hx, reflY, lane.hoop.r * 2.35);
  refl.addColorStop(0, lane.rimFire ? "rgba(255,180,95,0.18)" : "rgba(110,220,255,0.16)");
  refl.addColorStop(1, "transparent");
  ctx.fillStyle = refl;
  ctx.beginPath();
  ctx.ellipse(hx, reflY, lane.hoop.r * 2.5, lane.hoop.r * 0.58, 0, 0, Math.PI * 2);
  ctx.fill();
  const rimContactShadow = ctx.createRadialGradient(hx, rimY + 20, 4, hx, rimY + 20, r * 2);
  rimContactShadow.addColorStop(0, `rgba(0,0,0,${0.24 + rimDepth * 0.08})`);
  rimContactShadow.addColorStop(1, "transparent");
  ctx.fillStyle = rimContactShadow;
  ctx.beginPath();
  ctx.ellipse(hx, rimY + 20, r * 1.95, r * 0.6, 0, 0, Math.PI * 2);
  ctx.fill();

  const board = getBackboardRect(lane, hx, hy);
  const boardW = board.right - board.left;
  const boardH = board.bottom - board.top;
  const portalHalfW = lane.hoop.r * 0.72;
  const portalTop = board.top + boardH * 0.26;
  const portalBottom = portalTop + boardH * 0.47;
  const portalLeft = hx - portalHalfW;
  const portalRight = hx + portalHalfW;

  const boardGrad = ctx.createLinearGradient(board.left, board.top, board.right, board.bottom);
  boardGrad.addColorStop(0, `rgba(8,14,24,${0.74 + rimDepth * 0.07})`);
  boardGrad.addColorStop(0.5, `rgba(11,18,30,${0.62 + rimDepth * 0.06})`);
  boardGrad.addColorStop(1, `rgba(6,10,20,${0.66 + rimDepth * 0.06})`);
  ctx.fillStyle = boardGrad;
  ctx.fillRect(board.left, board.top, boardW, boardH);
  // Subtle rim shadow projection on the backboard for stronger depth cue.
  const rimBoardShadow = ctx.createRadialGradient(hx, board.bottom - 6, 3, hx, board.bottom - 6, lane.hoop.r * 1.1);
  rimBoardShadow.addColorStop(0, "rgba(0,0,0,0.28)");
  rimBoardShadow.addColorStop(1, "transparent");
  ctx.fillStyle = rimBoardShadow;
  ctx.beginPath();
  ctx.ellipse(hx, board.bottom - 6, lane.hoop.r * 1.02, lane.hoop.r * 0.3, 0, 0, Math.PI * 2);
  ctx.fill();
  const boardDepth = Math.max(3, r * 0.16);
  const boardEdgeGrad = ctx.createLinearGradient(board.left, board.bottom, board.left, board.bottom + boardDepth);
  boardEdgeGrad.addColorStop(0, "rgba(58,130,175,0.42)");
  boardEdgeGrad.addColorStop(1, "rgba(6,16,30,0.88)");
  ctx.fillStyle = boardEdgeGrad;
  ctx.fillRect(board.left + 3, board.bottom, boardW - 6, boardDepth);
  drawGlowLine(ctx, board.left, board.top, board.right, board.top, boardPrimary, 3.2, 15 * rimDepth);
  drawGlowLine(ctx, board.left, board.bottom, board.right, board.bottom, boardPrimary, 3.2, 15 * rimDepth);
  drawGlowLine(ctx, board.left, board.top, board.left, board.bottom, boardPrimary, 2.8, 13 * rimDepth);
  drawGlowLine(ctx, board.right, board.top, board.right, board.bottom, boardPrimary, 2.8, 13 * rimDepth);

  // Inspired by the reference image: larger glowing frame plus angled side wing lines.
  const frameInset = Math.max(10, r * 0.22);
  const outerTop = board.top - frameInset * 0.36;
  const outerBottom = board.bottom + frameInset * 0.2;
  const outerLeft = board.left - frameInset * 0.45;
  const outerRight = board.right + frameInset * 0.45;
  const frameSkewX = lane.side * (12 + lane.hoopPulse * 3);
  const frameSkewY = -6;
  drawGlowLine(ctx, outerLeft + frameSkewX, outerTop + frameSkewY, outerRight + frameSkewX, outerTop + frameSkewY, boardSecondary, 2.4, 12 * rimDepth, 0.85);
  drawGlowLine(ctx, outerLeft, outerBottom, outerRight, outerBottom, boardSecondary, 2.1, 10 * rimDepth, 0.72);
  drawGlowLine(ctx, outerLeft, outerBottom, outerLeft + frameSkewX, outerTop + frameSkewY, boardSecondary, 2.1, 10 * rimDepth, 0.72);
  drawGlowLine(ctx, outerRight, outerBottom, outerRight + frameSkewX, outerTop + frameSkewY, boardSecondary, 2.1, 10 * rimDepth, 0.72);

  const wingX = lane.side * (26 + lane.hoopPulse * 4);
  const wingY = -Math.max(10, r * 0.24);
  drawGlowLine(ctx, board.left, board.bottom, board.left + wingX, board.bottom + wingY, boardPrimary, 2.4, 11 * rimDepth, 0.78);
  drawGlowLine(ctx, board.right, board.bottom, board.right + wingX, board.bottom + wingY, boardPrimary, 2.4, 11 * rimDepth, 0.78);
  const boardCast = ctx.createLinearGradient(board.left, board.bottom + 1, board.left, board.bottom + 28);
  boardCast.addColorStop(0, `rgba(0,0,0,${0.18 + rimDepth * 0.06})`);
  boardCast.addColorStop(1, "transparent");
  ctx.fillStyle = boardCast;
  ctx.fillRect(board.left - 8, board.bottom, boardW + 16, 30);

  // Inner target square.
  drawGlowLine(ctx, portalLeft, portalTop, portalRight, portalTop, boardSecondary, 2.4, 10);
  drawGlowLine(ctx, portalLeft, portalBottom, portalRight, portalBottom, boardSecondary, 2.4, 10);
  drawGlowLine(ctx, portalLeft, portalTop, portalLeft, portalBottom, boardSecondary, 2.4, 10);
  drawGlowLine(ctx, portalRight, portalTop, portalRight, portalBottom, boardSecondary, 2.4, 10);

  const portalCy = (portalTop + portalBottom) * 0.5;
  const portalGlow = ctx.createRadialGradient(hx, portalCy, 8, hx, portalCy, lane.hoop.r * 1.7);
  portalGlow.addColorStop(0, lane.rimFire ? "rgba(255,198,126,0.2)" : "rgba(255,158,66,0.18)");
  portalGlow.addColorStop(1, "transparent");
  ctx.fillStyle = portalGlow;
  ctx.fillRect(board.left + 8, board.top + 8, boardW - 16, boardH - 16);

  const rimAura = ctx.createRadialGradient(hx, rimY, 10, hx, rimY, lane.hoop.r * 2.9);
  rimAura.addColorStop(0, lane.rimFire ? "rgba(255,210,125,0.24)" : "rgba(255,148,56,0.3)");
  rimAura.addColorStop(0.5, lane.rimFire ? "rgba(255,120,36,0.1)" : "rgba(255,112,24,0.12)");
  rimAura.addColorStop(1, "transparent");
  ctx.fillStyle = rimAura;
  ctx.beginPath();
  ctx.ellipse(hx, rimY, lane.hoop.r * 2.9, lane.hoop.r * 1.25, 0, 0, Math.PI * 2);
  ctx.fill();

  ctx.save();
  ctx.translate(hx, rimY);
  ctx.scale(depthScale, RIM_VISUAL_Y_SCALE * depthScale);
  const rimDepthShiftX = lane.side * (11 + lane.hoopPulse * 5 + lane.rimJolt * 2.5);
  const rimDepthShiftY = -8 - lane.hoopPulse * 3;
  const rimDepthPulse = clamp(0.55 + lane.hoopPulse * 0.38 + lane.rimJolt * 0.2, 0.45, 1);

  // 4D rim depth shell: back ring + connectors creates a strong extruded look.
  ctx.globalAlpha = 0.58 * rimDepthPulse;
  ctx.strokeStyle = lane.rimFire ? "rgba(255,190,105,0.92)" : "rgba(255,118,18,0.96)";
  ctx.lineWidth = 13.5 + rimDepth * 1.6;
  ctx.shadowColor = lane.rimFire ? "rgba(255,206,120,0.88)" : "rgba(255,132,26,0.88)";
  ctx.shadowBlur = 24 * rimDepth;
  ctx.beginPath();
  ctx.arc(rimDepthShiftX, rimDepthShiftY, r * 0.96, 0, Math.PI * 2);
  ctx.stroke();

  const rimBraces = 8;
  ctx.lineCap = "round";
  for (let i = 0; i < rimBraces; i++) {
    const a = (i / rimBraces) * TAU + Math.PI / rimBraces;
    const fx = Math.cos(a) * (r - 1.2);
    const fy = Math.sin(a) * (r - 1.2);
    const bx = Math.cos(a) * (r * 0.94) + rimDepthShiftX;
    const by = Math.sin(a) * (r * 0.94) + rimDepthShiftY;
    ctx.globalAlpha = 0.34 + (Math.sin(t * 0.012 + i * 0.68) * 0.08 + 0.08) * rimDepthPulse;
    ctx.strokeStyle = lane.rimFire ? "rgba(255,228,156,0.9)" : "rgba(255,180,96,0.92)";
    ctx.lineWidth = 4.8 + rimDepth * 0.48;
    ctx.shadowColor = lane.rimFire ? "rgba(255,206,126,0.84)" : "rgba(255,152,78,0.82)";
    ctx.shadowBlur = 14 * rimDepth;
    ctx.beginPath();
    ctx.moveTo(fx, fy);
    ctx.lineTo(bx, by);
    ctx.stroke();
  }

  if (lane.rimFire) {
    ["#ff2d00", "#ff6b00", "#ffd700"].forEach((c, i) => {
      ctx.strokeStyle = c;
      ctx.lineWidth = 6.8 + i * 1.45;
      ctx.globalAlpha = 0.35 + firePulse * 0.3;
      ctx.shadowColor = c;
      ctx.shadowBlur = 20 + i * 10;
      ctx.beginPath();
      ctx.arc(0, 0, r + i * 3, 0, Math.PI * 2);
      ctx.stroke();
    });
  } else {
    // Two-tone neon tube halo around the rim.
    [r + 6.8, r + 3.3].forEach((rad, i) => {
      ctx.globalAlpha = 0.5 - i * 0.12 + lane.hoopPulse * 0.14;
      ctx.lineWidth = i === 0 ? 12.8 + rimDepth * 1.8 : 8.8 + rimDepth * 1.24;
      ctx.strokeStyle = i === 0 ? rimSecondary : rimPrimary;
      ctx.shadowColor = i === 0 ? rimSecondary : rimPrimary;
      ctx.shadowBlur = (i === 0 ? 33 : 24) * rimDepth;
      ctx.beginPath();
      ctx.arc(0, 0, rad, 0, Math.PI * 2);
      ctx.stroke();
    });
  }

  ctx.globalAlpha = 1;
  ctx.strokeStyle = lane.rimFire ? "#ffd700" : rimPrimary;
  ctx.lineWidth = 23.2 + rimDepth * 2.9;
  ctx.shadowColor = lane.rimFire ? "#ffd700" : rimPrimary;
  ctx.shadowBlur = (lane.rimFire ? 40 : 34 + lane.hoopPulse * 18) * rimDepth;
  ctx.beginPath();
  ctx.arc(0, 0, r, 0, Math.PI * 2);
  ctx.stroke();

  // Secondary steel ring keeps the double-rim silhouette.
  ctx.strokeStyle = lane.rimFire ? "rgba(255,185,70,0.9)" : "rgba(255,152,80,0.92)";
  ctx.lineWidth = 16 + rimDepth * 1.8;
  ctx.shadowColor = lane.rimFire ? "rgba(255,170,65,0.8)" : "rgba(255,132,58,0.84)";
  ctx.shadowBlur = (lane.rimFire ? 25 : 18) * rimDepth;
  ctx.beginPath();
  ctx.arc(0, 0, r - 5.2, 0, Math.PI * 2);
  ctx.stroke();

  ctx.strokeStyle = lane.rimFire ? "rgba(255,240,170,0.95)" : rimCore;
  ctx.lineWidth = 3.4 + rimDepth * 0.35;
  ctx.shadowBlur = 14 * rimDepth;
  ctx.beginPath();
  ctx.arc(0, 0, r - 2.8, 0, Math.PI * 2);
  ctx.stroke();

  ctx.strokeStyle = lane.rimFire ? "rgba(255,120,40,0.75)" : rimSecondary;
  ctx.lineWidth = 3.1 + rimDepth * 0.3;
  ctx.shadowBlur = 12 * rimDepth;
  ctx.beginPath();
  ctx.arc(0, 0, r + 4.4, 0, Math.PI * 2);
  ctx.stroke();

  // Time dimension sweep highlights make the rim feel alive.
  const sweepA = (t * 0.028) % TAU;
  ctx.globalAlpha = 0.92;
  ctx.strokeStyle = lane.rimFire ? "rgba(255,255,212,0.96)" : "rgba(255,241,194,0.94)";
  ctx.lineWidth = 5.4 + rimDepth * 0.5;
  ctx.shadowColor = lane.rimFire ? "rgba(255,219,140,0.88)" : "rgba(255,194,122,0.88)";
  ctx.shadowBlur = 18 * rimDepth;
  ctx.beginPath();
  ctx.arc(0, 0, r + 1.2, sweepA, sweepA + 0.55);
  ctx.stroke();
  ctx.globalAlpha = 0.72;
  ctx.beginPath();
  ctx.arc(rimDepthShiftX, rimDepthShiftY, r * 0.94, sweepA + Math.PI, sweepA + Math.PI + 0.38);
  ctx.stroke();

  ctx.strokeStyle = "rgba(0,0,0,0.32)";
  ctx.shadowBlur = 0;
  ctx.lineWidth = 2.2;
  ctx.beginPath();
  ctx.arc(0, r * 0.25, r - 5.8, Math.PI * 0.12, Math.PI * 0.88);
  ctx.stroke();
  ctx.restore();

  if (lane.releaseFlash > 0.01) {
    ctx.globalAlpha = lane.releaseFlash;
    ctx.strokeStyle = lane.rimFire ? "#fff6cb" : rimCore;
    ctx.shadowColor = lane.rimFire ? "#ffbd66" : rimSecondary;
    ctx.shadowBlur = 26;
    ctx.lineWidth = 3.4;
    ctx.beginPath();
    ctx.ellipse(hx, rimY, r + 12, 11, 0, 0, Math.PI * 2);
    ctx.stroke();
    ctx.globalAlpha = 1;
  }

  const netPrimary = "rgba(246,252,255,0.98)";
  const netSecondary = "rgba(222,245,255,0.94)";
  const netRows = lane.netState?.rows || NET_SIM_ROWS;
  const netCols = lane.netState?.cols || NET_SIM_COLS;
  const denseNet = quality > 0.9;
  const midNet = quality > 0.8;
  const topY = rimY + 8;
  const rowGap = quality > 0.9 ? 7.2 : 6.4;
  const topRadius = r * 0.99;
  const bottomScale = 0.42;
  // Net collar depth rings (cylinder-like) for 3D read.
  for (let ring = 0; ring < 3; ring++) {
    const ringY = topY + ring * 2.2;
    const ringAlpha = 0.34 - ring * 0.08;
    ctx.strokeStyle = `rgba(236,250,255,${ringAlpha})`;
    ctx.lineWidth = 1.2;
    ctx.shadowColor = "rgba(200,245,255,0.45)";
    ctx.shadowBlur = 5;
    ctx.beginPath();
    ctx.ellipse(hx, ringY, topRadius * (1 - ring * 0.03), topRadius * 0.21, 0, 0, Math.PI * 2);
    ctx.stroke();
  }
  const nodeAt = (col, row) => {
    const rowT = row / netRows;
    const colT = col / netCols;
    const baseX = hx - topRadius + colT * topRadius * 2;
    const taper = lerp(1, bottomScale, rowT);
    const colIdx = lane.netState ? clamp(Math.round(colT * lane.netState.cols), 0, lane.netState.cols) : 0;
    const strandDrop = lane.netState ? lane.netState.strandDisp[colIdx] : 0;
    const inward = lane.netState ? lane.netState.inwardDisp[colIdx] : 0;
    const rowEase = rowT * 0.3 + rowT * rowT * 0.7;
    const sway =
      Math.sin(t * 0.006 + col * 0.44 + row * 0.28) * lane.netWave * 4.8 +
      Math.cos(t * 0.004 + row * 0.52) * lane.netWave * 1.35;
    const suctionScale = clamp(1 - inward * 0.0032 * rowEase, 0.72, 1.1);
    return {
      x: hx + (baseX - hx) * taper * suctionScale + sway + lane.side * inward * rowEase * 0.12,
      y: topY + row * rowGap + strandDrop * rowEase,
    };
  };

  const netVolume = ctx.createLinearGradient(0, topY, 0, topY + rowGap * netRows + 10);
  netVolume.addColorStop(0, "rgba(25,35,48,0.06)");
  netVolume.addColorStop(0.42, `rgba(12,18,28,${0.1 + rimDepth * 0.06})`);
  netVolume.addColorStop(1, `rgba(6,10,18,${0.18 + rimDepth * 0.06})`);
  ctx.fillStyle = netVolume;
  ctx.beginPath();
  for (let col = 0; col <= netCols; col++) {
    const p = nodeAt(col, 0);
    if (col === 0) ctx.moveTo(p.x, p.y);
    else ctx.lineTo(p.x, p.y);
  }
  for (let col = netCols; col >= 0; col--) {
    const p = nodeAt(col, netRows);
    ctx.lineTo(p.x, p.y);
  }
  ctx.closePath();
  ctx.fill();

  ctx.save();

  // Vertical chain strands.
  for (let col = 0; col <= netCols; col++) {
    for (let row = 1; row <= netRows; row++) {
      const a = nodeAt(col, row - 1);
      const b = nodeAt(col, row);
      const alt = (col + row) % 2 === 0;
      ctx.strokeStyle = alt ? netSecondary : netPrimary;
      ctx.shadowColor = alt ? netSecondary : netPrimary;
      ctx.shadowBlur = 10 + netPulse * 8;
      ctx.globalAlpha = 0.42 + (1 - row / (netRows + 1)) * 0.34;
      ctx.lineWidth = row < 3 ? 1.7 : 1.18;
      ctx.beginPath();
      ctx.moveTo(a.x, a.y);
      ctx.lineTo(b.x, b.y);
      ctx.stroke();
    }
  }

  // Horizontal loops.
  for (let row = 1; row <= netRows; row++) {
    for (let col = 1; col <= netCols; col++) {
      const a = nodeAt(col - 1, row);
      const b = nodeAt(col, row);
      const alt = (col + row) % 2 === 0;
      ctx.strokeStyle = alt ? netPrimary : netSecondary;
      ctx.shadowColor = alt ? netPrimary : netSecondary;
      ctx.shadowBlur = 7 + netPulse * 6;
      ctx.globalAlpha = 0.28 + (1 - row / (netRows + 1)) * 0.24;
      ctx.lineWidth = 1.02;
      ctx.beginPath();
      ctx.moveTo(a.x, a.y);
      ctx.lineTo(b.x, b.y);
      ctx.stroke();
    }
  }

  // Diagonal links for fuller 3D chain feel.
  if (denseNet || midNet) {
    for (let row = 1; row <= netRows; row++) {
      for (let col = 1; col <= netCols; col++) {
        const a = nodeAt(col - 1, row - 1);
        const b = nodeAt(col, row);
        const c = nodeAt(col, row - 1);
        const d = nodeAt(col - 1, row);
        ctx.globalAlpha = 0.16 + (1 - row / (netRows + 1)) * 0.16;
        ctx.lineWidth = 0.82;
        ctx.strokeStyle = row % 2 === 0 ? netPrimary : netSecondary;
        ctx.shadowColor = row % 2 === 0 ? netPrimary : netSecondary;
        ctx.shadowBlur = 5 + netPulse * 4;
        ctx.beginPath();
        ctx.moveTo(a.x, a.y);
        ctx.lineTo(b.x, b.y);
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(c.x, c.y);
        ctx.lineTo(d.x, d.y);
        ctx.stroke();
      }
    }
  }

  // Knot points and bottom ring.
  for (let row = 0; row <= netRows; row++) {
    for (let col = 0; col <= netCols; col++) {
      const p = nodeAt(col, row);
      const alt = (col + row) % 2 === 0;
      const twinkle = 0.12 + Math.abs(Math.sin(t * 0.01 + col * 0.55 + row * 0.35)) * 0.22;
      ctx.fillStyle = alt ? netPrimary : netSecondary;
      ctx.shadowColor = alt ? netPrimary : netSecondary;
      ctx.shadowBlur = 7 + netPulse * 6;
      ctx.globalAlpha = 0.32 + (1 - row / (netRows + 1)) * 0.4 + twinkle * 0.2;
      ctx.beginPath();
      ctx.arc(p.x, p.y, row < 2 ? 1.58 : 1.25, 0, Math.PI * 2);
      ctx.fill();
    }
  }
  const leftBottom = nodeAt(0, netRows);
  const rightBottom = nodeAt(netCols, netRows);
  const bottomCx = (leftBottom.x + rightBottom.x) * 0.5;
  const bottomW = Math.abs(rightBottom.x - leftBottom.x) * 0.5;
  const bottomY = (leftBottom.y + rightBottom.y) * 0.5 + 1.2;
  ctx.globalAlpha = 0.4 + netPulse * 0.12;
  ctx.lineWidth = 1.45;
  ctx.strokeStyle = netPrimary;
  ctx.shadowColor = netPrimary;
  ctx.shadowBlur = 10;
  ctx.beginPath();
  ctx.ellipse(bottomCx, bottomY, bottomW, 4.5, 0, 0, Math.PI * 2);
  ctx.stroke();
  ctx.globalAlpha = 0.24 + netPulse * 0.08;
  ctx.strokeStyle = netSecondary;
  ctx.shadowColor = netSecondary;
  ctx.shadowBlur = 8;
  ctx.beginPath();
  ctx.ellipse(bottomCx, bottomY + 0.2, bottomW * 0.9, 3.6, 0, 0, Math.PI * 2);
  ctx.stroke();

  // Chain spark FX on made shots for more reactive net feedback.
  if (netPulse > 0.18) {
    const sparkCount = Math.round(6 + netPulse * 8);
    for (let i = 0; i < sparkCount; i++) {
      const ang = (TAU * i) / Math.max(1, sparkCount) + t * 0.004;
      const jitter = Math.sin(t * 0.01 + i * 1.9) * 3.2;
      const sx = hx + Math.cos(ang) * (r * 0.62 + jitter);
      const sy = topY + rowGap * (1.5 + (i % netRows) * 0.48) + Math.sin(t * 0.013 + i * 0.6) * 2.8;
      ctx.globalAlpha = clamp(0.18 + netPulse * 0.22, 0.1, 0.45);
      ctx.fillStyle = i % 2 === 0 ? "rgba(244,252,255,0.95)" : "rgba(189,245,255,0.92)";
      ctx.shadowColor = i % 2 === 0 ? "#f4fcff" : "#9defff";
      ctx.shadowBlur = 8 + netPulse * 6;
      ctx.beginPath();
      ctx.arc(sx, sy, 1 + netPulse * 0.9, 0, TAU);
      ctx.fill();
    }
  }
  ctx.restore();

  drawGlowLine(ctx, hx + lane.side * 78, hy - 54, hx + lane.side * 36, hy - 4, rimPrimary, 2.1, 9);
  drawGlowLine(ctx, hx + lane.side * 78, hy - 26, hx + lane.side * 34, hy - 3, rimSecondary, 2.1, 9);

  if (lane.streak >= 2) {
    ctx.font = '700 20px "Orbitron", sans-serif';
    ctx.textAlign = "center";
    ctx.fillStyle = lane.rimFire ? "#ffd66d" : rimPrimary;
    ctx.shadowColor = lane.rimFire ? "#ff8f2d" : rimSecondary;
    ctx.shadowBlur = 16;
    ctx.globalAlpha = 0.8 + lane.comboPop * 0.2;
    ctx.fillText(`x${lane.streak}`, hx, hy - 82);
    ctx.globalAlpha = 1;
  }
  if (lane.scorePop > 0.01) {
    ctx.font = '700 26px "Orbitron", sans-serif';
    ctx.textAlign = "center";
    ctx.fillStyle = lane.rimFire ? "#ffe0a4" : "#9effca";
    ctx.shadowColor = lane.rimFire ? "#ffb367" : "#61ffd1";
    ctx.shadowBlur = 18;
    ctx.globalAlpha = lane.scorePop;
    ctx.fillText("+", hx + lane.side * 56, hy - 26 - lane.scorePop * 22);
    ctx.globalAlpha = 1;
  }
  ctx.restore();
}

function drawRimFrontOverlay(ctx, lane) {
  const hx = hoopX(lane);
  const hy = hoopY(lane);
  const rimY = getRimCenterY(lane, hy);
  const r = lane.hoop.r;
  const depthScale = clamp(1.02 + (rimY / CH) * 0.06, 1, 1.16);
  const ballY = lane.renderY ?? lane.ball?.y ?? lane.home.y;
  const shouldOcclude = ballY < rimY + r * 0.42;
  if (!shouldOcclude) return;

  const rimPrimary = lane.rimFire ? "#ffd35a" : "#ff9a2c";
  const rimCore = lane.rimFire ? "#fff2c2" : "#ffc186";
  ctx.save();
  ctx.translate(hx, rimY);
  ctx.scale(depthScale, RIM_VISUAL_Y_SCALE * depthScale);
  ctx.globalAlpha = 0.95;
  ctx.strokeStyle = rimPrimary;
  ctx.lineWidth = 10.5;
  ctx.shadowColor = rimPrimary;
  ctx.shadowBlur = 18;
  ctx.beginPath();
  ctx.arc(0, 0, r, Math.PI * 0.02, Math.PI * 0.98);
  ctx.stroke();
  ctx.strokeStyle = rimCore;
  ctx.lineWidth = 3.2;
  ctx.shadowBlur = 10;
  ctx.beginPath();
  ctx.arc(0, 0, r - 2.5, Math.PI * 0.04, Math.PI * 0.96);
  ctx.stroke();
  ctx.restore();
}

function drawShooter(ctx, lane, t) {
  const bx = lane.home.x;
  const by = lane.home.y;
  const facing = lane.side === -1 ? 1 : -1;
  const domSign = facing;
  const snap = lane.wristSnap;
  const guidePeel = lane.guideRelease;
  const follow = lane.followHold;
  const charge = lane.state === "charging" ? lane.power : 0;
  const tremor = Math.sin(t * 0.011 + lane.breathPhase) * 0.9 + lane.handJitter * 1.3;
  const pocketX = bx + facing * 16;
  const pocketY = by - 22;
  const flyingBlend = lane.state === "flying" ? clamp(1 - lane.releaseBurst, 0, 1) : lane.state === "returning" ? 1 : 0;

  let handAnchorX = lane.ball.x;
  let handAnchorY = lane.ball.y + 4;
  handAnchorX = lerp(handAnchorX, pocketX + facing * (2 + follow * 7), flyingBlend);
  handAnchorY = lerp(handAnchorY, pocketY - 9 - snap * 11 - follow * 13, flyingBlend);
  handAnchorX += tremor * 0.45;
  handAnchorY += tremor * 0.2;

  ctx.save();

  ctx.fillStyle = "rgba(6,16,28,0.74)";
  ctx.beginPath();
  ctx.ellipse(bx - facing * 22, by + 48, 54, 74, 0, 0, Math.PI * 2);
  ctx.fill();

  ctx.fillStyle = lane.id === "player" ? "#40e5ff" : "#ff6f4a";
  ctx.globalAlpha = 0.2;
  ctx.beginPath();
  ctx.ellipse(bx - facing * 22, by + 48, 54, 74, 0, 0, Math.PI * 2);
  ctx.fill();
  ctx.globalAlpha = 1;

  ctx.fillStyle = "#f0b98f";
  ctx.beginPath();
  ctx.arc(bx - facing * 38, by - 30, 15, 0, Math.PI * 2);
  ctx.fill();

  const domX = handAnchorX + domSign * (12 + follow * 8) + snap * 2.4;
  const domY = handAnchorY + 11 - snap * 9 - follow * 10;
  const guideX = handAnchorX - domSign * (17 + guidePeel * 20);
  const guideY = handAnchorY + 8 - guidePeel * 4.2;
  const shoulderY = by + 6;
  const domShoulderX = bx - facing * 22;
  const guideShoulderX = bx - facing * 8;
  const domElbowX = lerp(domShoulderX, domX, 0.56) - domSign * (8 + snap * 3);
  const domElbowY = lerp(shoulderY, domY, 0.56) + 8 + follow * 2;
  const guideElbowX = lerp(guideShoulderX, guideX, 0.56) + domSign * (7 + guidePeel * 4);
  const guideElbowY = lerp(shoulderY + 2, guideY, 0.56) + 8;

  const drawArm = (sx, sy, ex, ey, hx, hy, tone = "#eab58e") => {
    ctx.strokeStyle = tone;
    ctx.lineWidth = 9;
    ctx.lineCap = "round";
    ctx.beginPath();
    ctx.moveTo(sx, sy);
    ctx.lineTo(ex, ey);
    ctx.lineTo(hx, hy);
    ctx.stroke();
  };

  drawArm(domShoulderX, shoulderY, domElbowX, domElbowY, domX - domSign * 4, domY + 1, "#eebd98");
  drawArm(guideShoulderX, shoulderY + 2, guideElbowX, guideElbowY, guideX + domSign * 3, guideY + 2, "#e5b087");

  const drawGloveHand = (hx, hy, rot, side, openAmt, dominant) => {
    ctx.save();
    ctx.translate(hx, hy);
    ctx.rotate(rot);
    const scale = dominant ? 1.22 : 1.16;
    ctx.scale(scale, scale);

    const palmGrad = ctx.createRadialGradient(-4, -5, 1, 0, 2, 14);
    palmGrad.addColorStop(0, "#68717f");
    palmGrad.addColorStop(0.55, "#515a67");
    palmGrad.addColorStop(1, "#404853");
    ctx.fillStyle = palmGrad;
    ctx.shadowColor = "rgba(0,0,0,0.28)";
    ctx.shadowBlur = 6;
    ctx.beginPath();
    ctx.ellipse(0, 0, 12.4, 9.2, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = "rgba(255,220,95,0.86)";
    ctx.lineWidth = 1.4;
    ctx.beginPath();
    ctx.ellipse(0, 0, 9.6, 6.8, 0, 0, Math.PI * 2);
    ctx.stroke();

    const drawFinger = (baseX, baseY, bend, seed) => {
      const segA = 5.8;
      const segB = 4.7;
      const a1 = -Math.PI * 0.46 + seed * 0.07;
      const a2 = a1 + side * (0.06 + bend * 0.44);
      const kx = baseX + Math.cos(a1) * segA;
      const ky = baseY + Math.sin(a1) * segA;
      const tx = kx + Math.cos(a2) * segB;
      const ty = ky + Math.sin(a2) * segB;
      ctx.strokeStyle = "#596271";
      ctx.lineCap = "round";
      ctx.lineWidth = 3.2;
      ctx.beginPath();
      ctx.moveTo(baseX, baseY);
      ctx.lineTo(kx, ky);
      ctx.stroke();
      ctx.lineWidth = 2.8;
      ctx.beginPath();
      ctx.moveTo(kx, ky);
      ctx.lineTo(tx, ty);
      ctx.stroke();
      ctx.fillStyle = "rgba(255,215,120,0.82)";
      ctx.beginPath();
      ctx.arc(tx, ty, 0.95, 0, Math.PI * 2);
      ctx.fill();
    };

    const baseX = [-7.2, -2.4, 2.4, 7.2];
    for (let i = 0; i < 4; i++) {
      const control = i === 1 || i === 2;
      const fx = baseX[i] + lane.fingerSeed[i] * 0.55;
      const fy = -4.7 - openAmt * 4.1 - (control ? 1.4 : 0);
      const bend = clamp(0.64 - openAmt * 0.45 + (dominant ? follow * 0.2 : guidePeel * 0.1), 0.08, 0.9);
      drawFinger(fx, fy, bend, lane.fingerSeed[i] + i * 0.05);
    }

    // Thumb.
    drawFinger(8.5 * side, -1.2, clamp(0.32 + openAmt * 0.3, 0.2, 0.7), lane.fingerSeed[1] * 0.6);

    if (charge > 0.05 && lane.state === "charging") {
      ctx.strokeStyle = "rgba(255,230,150,0.42)";
      ctx.lineWidth = 0.9;
      ctx.beginPath();
      ctx.moveTo(-2.2, -8.8 - charge * 2.4);
      ctx.lineTo(2.4, -9.5 - charge * 2.4);
      ctx.stroke();
    }
    ctx.restore();
  };

  drawGloveHand(guideX, guideY, 0.16 + guidePeel * 0.55, 1, guidePeel * 0.98, false);
  drawGloveHand(domX, domY, -0.2 - snap * 0.38 - follow * 0.2, -1, 0.16 + follow * 0.22, true);

  if (lane.state === "charging" || lane.state === "idle") {
    ctx.strokeStyle = "rgba(255,220,95,0.2)";
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(domX + domSign * 5, domY - 1);
    ctx.lineTo(handAnchorX, handAnchorY - 2);
    ctx.lineTo(guideX - domSign * 5, guideY - 1);
    ctx.stroke();
  }

  ctx.restore();
}

function drawVrHands(ctx, lane, t) {
  const breathing = Math.sin(t * 0.002 + lane.breathPhase) * 3.4;
  const tension = lane.state === "charging" ? lane.power * 10.5 : 0;
  const jitter = lane.state === "charging" ? lane.handJitter * 2.4 : 0.25;
  const follow = lane.followHold;
  const guidePeel = lane.guideRelease;
  const snap = lane.wristSnap;
  const inFlight = lane.state === "flying" || lane.state === "returning";
  const releaseBlend = inFlight ? clamp(1 - lane.releaseBurst, 0, 1) : 0;

  let ballAnchorX = lane.ball.x;
  let ballAnchorY = lane.ball.y + 6;
  if (inFlight) {
    ballAnchorX = lane.home.x + Math.sin(t * 0.009 + 0.8) * 1.8;
    ballAnchorY = lane.home.y + 12;
  }

  // Dominant hand under the ball, guide hand on side and peeling away first.
  const domX = ballAnchorX - 34 + Math.sin(t * 0.01) * 1.4 + jitter;
  const domY = ballAnchorY + 27 + breathing - snap * 11 - follow * 16;
  const guideX = ballAnchorX + 37 + guidePeel * 34 + Math.sin(t * 0.009 + 1.2) * 1.7 - jitter * 0.8;
  const guideY = ballAnchorY + 18 + breathing * 0.6 - guidePeel * 9.5;
  const forearmY = CH + 10 + breathing * 0.4;
  const domScale = 1.24 + lane.power * 0.08 + follow * 0.05;
  const guideScale = 1.18 + lane.power * 0.05 + guidePeel * 0.02;

  ctx.save();

  const drawForearm3D = (startX, startY, wristX, wristY, side = 1, accent = "#ffd65f") => {
    ctx.save();
    const elbowX = lerp(startX, wristX, 0.42) + side * 8;
    const elbowY = lerp(startY, wristY, 0.42) + 12;

    // Main sleeve tube.
    ctx.strokeStyle = "#2f343e";
    ctx.lineWidth = 30;
    ctx.lineCap = "round";
    ctx.shadowColor = "rgba(0,0,0,0.28)";
    ctx.shadowBlur = 10;
    ctx.beginPath();
    ctx.moveTo(startX, startY);
    ctx.quadraticCurveTo(elbowX, elbowY, wristX, wristY);
    ctx.stroke();

    // Top highlight for cylindrical look.
    ctx.strokeStyle = "rgba(122,132,150,0.55)";
    ctx.lineWidth = 10;
    ctx.shadowBlur = 0;
    ctx.beginPath();
    ctx.moveTo(startX + side * 4, startY - 2);
    ctx.quadraticCurveTo(elbowX + side * 5, elbowY - 4, wristX + side * 4, wristY - 4);
    ctx.stroke();

    // Accent stripe and wrist strap.
    ctx.strokeStyle = accent;
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.moveTo(startX + side * 5, startY - 1);
    ctx.quadraticCurveTo(elbowX + side * 7, elbowY - 1, wristX + side * 6, wristY - 3);
    ctx.stroke();

    ctx.fillStyle = "#232833";
    ctx.beginPath();
    ctx.ellipse(wristX, wristY + 2, 17, 10, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = "rgba(255,220,95,0.95)";
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.ellipse(wristX, wristY + 2, 14.5, 8.2, 0, 0, Math.PI * 2);
    ctx.stroke();
    ctx.restore();
  };

  const drawFinger = (fx, fy, baseAngle, bend, side, seed, glow = false) => {
    const segA = 8.8;
    const segB = 7.2;
    const angleA = baseAngle + seed * 0.05;
    const angleB = angleA + side * (0.08 + bend * 0.42);
    const knx = fx + Math.cos(angleA) * segA;
    const kny = fy + Math.sin(angleA) * segA;
    const tipx = knx + Math.cos(angleB) * segB;
    const tipy = kny + Math.sin(angleB) * segB;

    ctx.strokeStyle = "#565d69";
    ctx.lineCap = "round";
    ctx.lineWidth = 5.6;
    ctx.shadowColor = "rgba(0,0,0,0.28)";
    ctx.shadowBlur = 5;
    ctx.beginPath();
    ctx.moveTo(fx, fy);
    ctx.lineTo(knx, kny);
    ctx.stroke();
    ctx.lineWidth = 4.7;
    ctx.beginPath();
    ctx.moveTo(knx, kny);
    ctx.lineTo(tipx, tipy);
    ctx.stroke();

    ctx.fillStyle = "#5f6674";
    ctx.shadowBlur = 0;
    ctx.beginPath();
    ctx.arc(knx, kny, 2.7, 0, Math.PI * 2);
    ctx.fill();
    ctx.beginPath();
    ctx.arc(tipx, tipy, 2.1, 0, Math.PI * 2);
    ctx.fill();
    if (glow) {
      ctx.fillStyle = "rgba(255,225,130,0.85)";
      ctx.beginPath();
      ctx.arc(tipx, tipy, 1.4, 0, Math.PI * 2);
      ctx.fill();
    }
  };

  const drawHand3D = (hx, hy, rot, side = 1, open = 0, dominant = false, scale = 1) => {
    ctx.save();
    ctx.translate(hx, hy);
    ctx.rotate(rot);
    ctx.scale(scale, scale);

    const palmGrad = ctx.createRadialGradient(-5, -7, 1, 0, 4, 24);
    palmGrad.addColorStop(0, "#717a88");
    palmGrad.addColorStop(0.55, "#555d69");
    palmGrad.addColorStop(1, "#414854");
    ctx.fillStyle = palmGrad;
    ctx.shadowColor = "rgba(0,0,0,0.3)";
    ctx.shadowBlur = 9;
    ctx.beginPath();
    ctx.ellipse(0, 2, 21, 16, 0, 0, Math.PI * 2);
    ctx.fill();

    // Palm edge highlight for depth.
    ctx.strokeStyle = "rgba(195,210,235,0.38)";
    ctx.lineWidth = 1.2;
    ctx.shadowBlur = 0;
    ctx.beginPath();
    ctx.ellipse(-2, 0, 17.5, 12.3, -0.1, Math.PI * 0.78, Math.PI * 1.86);
    ctx.stroke();

    const fingerSpread = [-12.8, -4.9, 3.8, 12.2];
    for (let i = 0; i < 4; i++) {
      const control = i === 1 || i === 2;
      const seed = lane.fingerSeed[i] * 0.55 + Math.sin(t * 0.01 + i * 0.9) * 0.04;
      const baseX = fingerSpread[i] + lane.fingerSeed[i] * 0.8;
      const baseY = -5.2 + (control ? -1.25 : 0.35) - open * 2.6;
      const baseAngle = -Math.PI * 0.52 + (i - 1.5) * 0.1 + side * 0.03;
      const bend = clamp(0.66 - open * 0.52 + (dominant ? follow * 0.2 : guidePeel * 0.12), 0.08, 0.92);
      drawFinger(baseX, baseY, baseAngle, bend, side, seed, control && (tension > 2 || lane.releaseType === "perfect"));
    }

    // Thumb.
    const thumbOpen = clamp(open * 0.7 + (dominant ? 0.06 : 0.12), 0.04, 0.9);
    drawFinger(14.8 * side, -1.3, side * 0.2, thumbOpen, side, lane.fingerSeed[1] * 0.4, false);

    // Wrist cuff.
    const cuffGrad = ctx.createLinearGradient(-15, 12, 15, 12);
    cuffGrad.addColorStop(0, "#2d3440");
    cuffGrad.addColorStop(0.5, "#3a414e");
    cuffGrad.addColorStop(1, "#252b35");
    ctx.fillStyle = cuffGrad;
    ctx.beginPath();
    ctx.ellipse(0, 13.5, 14.5, 7.2, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = "rgba(255,220,95,0.9)";
    ctx.lineWidth = 1.7;
    ctx.beginPath();
    ctx.ellipse(0, 13.5, 11.2, 5.4, 0, 0, Math.PI * 2);
    ctx.stroke();

    if (lane.state === "charging") {
      ctx.strokeStyle = "rgba(255,230,150,0.42)";
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.moveTo(-5, -13 - tension * 0.06);
      ctx.lineTo(4, -14 - tension * 0.06);
      ctx.stroke();
    }
    ctx.restore();
  };

  drawForearm3D(domX - 22, forearmY, domX, domY, -1, "rgba(255,220,95,0.88)");
  drawForearm3D(guideX + 22, forearmY, guideX, guideY, 1, "rgba(255,220,95,0.82)");

  // Guide hand peels away first; dominant hand snaps and holds follow-through.
  drawHand3D(
    guideX,
    guideY,
    0.15 + guidePeel * 0.66 + releaseBlend * 0.1,
    1,
    clamp(guidePeel * 1.06 + releaseBlend * 0.2, 0, 1.1),
    false,
    guideScale
  );
  drawHand3D(
    domX,
    domY,
    -0.22 - snap * 0.42 - follow * 0.2 - releaseBlend * 0.08,
    -1,
    clamp(0.18 + follow * 0.24, 0.1, 0.7),
    true,
    domScale
  );

  // Release tip accents to sell fingertip separation moment.
  if (lane.state === "flying" && lane.releaseBurst > 0.01) {
    ctx.save();
    const sparkA = lane.releaseBurst * 0.85;
    ctx.globalAlpha = sparkA;
    ctx.strokeStyle = lane.releaseType === "perfect" ? "rgba(190,255,235,0.9)" : "rgba(255,210,150,0.8)";
    ctx.lineWidth = 1.4;
    ctx.shadowColor = lane.releaseType === "perfect" ? "#7dffe7" : "#ffd086";
    ctx.shadowBlur = 10;
    for (let i = 0; i < 4; i++) {
      const a = -Math.PI * 0.55 + i * 0.19;
      const sx = domX + Math.cos(a) * 16;
      const sy = domY - 10 + Math.sin(a) * 6;
      ctx.beginPath();
      ctx.moveTo(sx, sy);
      ctx.lineTo(sx + Math.cos(a) * (10 + lane.releaseBurst * 8), sy - (3 + lane.releaseBurst * 4));
      ctx.stroke();
    }
    ctx.restore();
  }

  // Subtle thumb-triangle line while set for shot.
  if (!inFlight || lane.releaseBurst > 0.45) {
    ctx.strokeStyle = "rgba(255,220,95,0.23)";
    ctx.lineWidth = 1.1;
    ctx.beginPath();
    ctx.moveTo(domX + 11, domY - 2);
    ctx.lineTo(ballAnchorX, ballAnchorY + 2);
    ctx.lineTo(guideX - 11, guideY - 2);
    ctx.stroke();
  }

  ctx.restore();
}

function drawLaneHandsOverlay(ctx, lane, t) {
  const side = lane.side === -1 ? -1 : 1;
  const inFlight = lane.state === "flying" || lane.state === "returning";
  const breathing = Math.sin(t * 0.002 + lane.breathPhase) * 2.8;
  const tension = lane.state === "charging" ? lane.power * 9.8 : 0;
  const jitter = lane.state === "charging" ? lane.handJitter * 2.4 : 0.24;
  const snap = lane.wristSnap;
  const guidePeel = lane.guideRelease;
  const follow = lane.followHold;
  const releaseBlend = inFlight ? clamp(1 - lane.releaseBurst, 0, 1) : 0;
  const anchorX = lerp(lane.ball.x, lane.home.x + side * 6, releaseBlend);
  const anchorY = lerp(lane.ball.y + 8, lane.home.y - 24, releaseBlend) + breathing;
  const alpha = lane.state === "idle" ? 0.86 : lane.state === "charging" ? 1 : 0.96;

  const domX = anchorX - side * (22 - follow * 8) + jitter;
  const domY = anchorY + 26 - snap * 20 - follow * 20;
  const guideX = anchorX + side * (26 + guidePeel * 54) - jitter * 0.65;
  const guideY = anchorY + 8 - guidePeel * 18 + breathing * 0.3;

  ctx.save();
  ctx.globalAlpha = alpha;

  const drawForearm = (sx, sy, ex, ey, armSide = 1, accent = "#ffd65f") => {
    ctx.save();
    const elbowX = lerp(sx, ex, 0.43) + armSide * 8;
    const elbowY = lerp(sy, ey, 0.43) + 11;
    ctx.strokeStyle = "#2b313b";
    ctx.lineWidth = 31;
    ctx.lineCap = "round";
    ctx.shadowColor = "rgba(0,0,0,0.34)";
    ctx.shadowBlur = 12;
    ctx.beginPath();
    ctx.moveTo(sx, sy);
    ctx.quadraticCurveTo(elbowX, elbowY, ex, ey);
    ctx.stroke();

    ctx.strokeStyle = "rgba(150,165,188,0.52)";
    ctx.lineWidth = 10;
    ctx.shadowBlur = 0;
    ctx.beginPath();
    ctx.moveTo(sx + armSide * 4, sy - 3);
    ctx.quadraticCurveTo(elbowX + armSide * 5, elbowY - 4, ex + armSide * 4, ey - 5);
    ctx.stroke();

    ctx.strokeStyle = accent;
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.moveTo(sx + armSide * 5, sy - 1.5);
    ctx.quadraticCurveTo(elbowX + armSide * 7, elbowY - 1.5, ex + armSide * 6, ey - 3.5);
    ctx.stroke();

    ctx.fillStyle = "#232833";
    ctx.beginPath();
    ctx.ellipse(ex, ey + 2, 16, 9.5, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = "rgba(255,220,95,0.94)";
    ctx.lineWidth = 1.9;
    ctx.beginPath();
    ctx.ellipse(ex, ey + 2, 12.8, 6.8, 0, 0, Math.PI * 2);
    ctx.stroke();
    ctx.restore();
  };

  const drawHand = (hx, hy, rot, handSide, openAmt, dominant = false) => {
    ctx.save();
    ctx.translate(hx, hy);
    ctx.rotate(rot);
    ctx.scale(dominant ? 1.7 : 1.56, dominant ? 1.7 : 1.56);

    const palmGrad = ctx.createRadialGradient(-4, -6, 1, 0, 2, 16);
    palmGrad.addColorStop(0, "#707b89");
    palmGrad.addColorStop(0.55, "#565f6c");
    palmGrad.addColorStop(1, "#414955");
    ctx.fillStyle = palmGrad;
    ctx.shadowColor = "rgba(0,0,0,0.32)";
    ctx.shadowBlur = 9;
    ctx.beginPath();
    ctx.ellipse(0, 0.5, 12.8, 9.8, 0, 0, Math.PI * 2);
    ctx.fill();

    ctx.strokeStyle = "rgba(255,224,110,0.86)";
    ctx.lineWidth = 1.2;
    ctx.shadowBlur = 0;
    ctx.beginPath();
    ctx.ellipse(-0.8, 0, 9.4, 6.7, -0.06, 0, Math.PI * 2);
    ctx.stroke();

    const drawDigit = (bx, by, seed, bend, glowTip = false) => {
      const baseA = -Math.PI * 0.51 + seed * 0.09;
      const midA = baseA + handSide * (0.1 + bend * 0.36);
      const tipA = midA + handSide * (0.08 + bend * 0.28);
      const s1 = 5.8;
      const s2 = 4.7;
      const s3 = 3.3;
      const k1x = bx + Math.cos(baseA) * s1;
      const k1y = by + Math.sin(baseA) * s1;
      const k2x = k1x + Math.cos(midA) * s2;
      const k2y = k1y + Math.sin(midA) * s2;
      const tx = k2x + Math.cos(tipA) * s3;
      const ty = k2y + Math.sin(tipA) * s3;

      ctx.strokeStyle = "#596372";
      ctx.lineCap = "round";
      ctx.lineWidth = 3;
      ctx.beginPath();
      ctx.moveTo(bx, by);
      ctx.lineTo(k1x, k1y);
      ctx.stroke();

      ctx.lineWidth = 2.6;
      ctx.beginPath();
      ctx.moveTo(k1x, k1y);
      ctx.lineTo(k2x, k2y);
      ctx.stroke();

      ctx.lineWidth = 2.2;
      ctx.beginPath();
      ctx.moveTo(k2x, k2y);
      ctx.lineTo(tx, ty);
      ctx.stroke();

      ctx.fillStyle = "#656f7f";
      ctx.beginPath();
      ctx.arc(k1x, k1y, 1.15, 0, Math.PI * 2);
      ctx.fill();
      ctx.beginPath();
      ctx.arc(k2x, k2y, 1.05, 0, Math.PI * 2);
      ctx.fill();
      ctx.fillStyle = glowTip ? "rgba(255,230,150,0.95)" : "rgba(215,220,230,0.86)";
      ctx.beginPath();
      ctx.arc(tx, ty, 0.95, 0, Math.PI * 2);
      ctx.fill();
    };

    const offsets = [-7.8, -2.7, 2.6, 7.8];
    for (let i = 0; i < 4; i++) {
      const control = i === 1 || i === 2;
      const seed =
        lane.fingerSeed[i] * 0.9 +
        (i - 1.5) * 0.07 +
        Math.sin(t * 0.011 + i * 0.8) * 0.05;
      const baseX = offsets[i] + lane.fingerSeed[i] * 0.5;
      const baseY = -4.6 - openAmt * 4.8 - (control ? 1.5 : 0.4);
      const bend = clamp(
        0.7 - openAmt * 0.55 + (dominant ? follow * 0.22 : guidePeel * 0.15),
        0.08,
        0.92
      );
      drawDigit(baseX, baseY, seed, bend, control && (lane.releaseType === "perfect" || tension > 2.4));
    }

    const thumbBend = clamp(0.34 + openAmt * 0.38 + (dominant ? 0.1 : 0), 0.16, 0.86);
    drawDigit(8.8 * handSide, -1.2, lane.fingerSeed[1] * 0.5, thumbBend, false);

    if (lane.state === "charging" && tension > 0.6) {
      ctx.strokeStyle = "rgba(255,230,150,0.45)";
      ctx.lineWidth = 0.9;
      ctx.beginPath();
      ctx.moveTo(-4.5, -12.5 - tension * 0.05);
      ctx.lineTo(4.4, -13.4 - tension * 0.05);
      ctx.stroke();
    }

    ctx.restore();
  };

  drawForearm(domX - side * 76, CH + 16, domX, domY, -side, "rgba(255,220,95,0.9)");
  drawForearm(guideX + side * 78, CH + 16, guideX, guideY, side, "rgba(255,220,95,0.84)");

  // Guide hand peels away first; dominant hand stays under ball and snaps through release.
  drawHand(
    guideX,
    guideY,
    0.16 * side + guidePeel * 0.72 * side + releaseBlend * 0.08,
    side,
    clamp(guidePeel * 1.12 + releaseBlend * 0.25, 0.06, 1.2),
    false
  );
  drawHand(
    domX,
    domY,
    -0.26 * side - snap * 0.56 * side - follow * 0.26 * side,
    -side,
    clamp(0.14 + follow * 0.28, 0.08, 0.76),
    true
  );

  // Contact triangle while set, matching real two-hand set shot posture.
  if (!inFlight || lane.releaseBurst > 0.35) {
    ctx.strokeStyle = "rgba(255,220,95,0.22)";
    ctx.lineWidth = 1.2;
    ctx.beginPath();
    ctx.moveTo(domX + side * 10, domY - 2);
    ctx.lineTo(anchorX, anchorY + 2);
    ctx.lineTo(guideX - side * 10, guideY - 2);
    ctx.stroke();
  }

  if (lane.state === "flying" && lane.releaseBurst > 0.01) {
    ctx.save();
    ctx.globalAlpha = lane.releaseBurst * 0.8;
    ctx.strokeStyle =
      lane.releaseType === "perfect"
        ? "rgba(182,255,232,0.92)"
        : "rgba(255,212,154,0.84)";
    ctx.lineWidth = 1.45;
    ctx.shadowColor = lane.releaseType === "perfect" ? "#7ffde4" : "#ffd286";
    ctx.shadowBlur = 10;
    for (let i = 0; i < 4; i++) {
      const a = -Math.PI * 0.58 + i * 0.17 * side;
      const sx = domX + Math.cos(a) * 16;
      const sy = domY - 9 + Math.sin(a) * 6;
      ctx.beginPath();
      ctx.moveTo(sx, sy);
      ctx.lineTo(sx + Math.cos(a) * (10 + lane.releaseBurst * 8), sy - (3 + lane.releaseBurst * 4));
      ctx.stroke();
    }
    ctx.restore();
  }

  ctx.restore();
}

function drawVrReleaseMeter(ctx, lane, t) {
  const w = 260;
  const h = 10;
  const x = CW * 0.5 - w * 0.5;
  const y = CH - 48;
  const p = lane.power;
  const center = lane.releaseWindow?.center ?? 0.7;
  const width = lane.releaseWindow?.width ?? 0.14;

  ctx.save();
  ctx.globalAlpha = 0.85;
  ctx.fillStyle = "rgba(10,20,32,0.7)";
  ctx.fillRect(x, y, w, h);
  const shift = ((t * 0.0025) % 1) * w;
  const grad = ctx.createLinearGradient(x + shift - w, y, x + shift, y);
  grad.addColorStop(0, "#ff5555");
  grad.addColorStop(0.45, "#ffd86a");
  grad.addColorStop(0.7, "#7dffb2");
  grad.addColorStop(1, "#4cefff");
  ctx.fillStyle = grad;
  ctx.fillRect(x, y, w * p, h);

  const zx = x + w * (center - width * 0.5);
  const zw = w * width;
  ctx.strokeStyle = "rgba(170,255,210,0.95)";
  ctx.lineWidth = 1.2;
  ctx.strokeRect(zx, y - 2, zw, h + 4);
  if (p >= center - width * 0.5 && p <= center + width * 0.5 && lane.state === "charging") {
    ctx.fillStyle = "rgba(150,255,210,0.2)";
    ctx.fillRect(zx, y - 2, zw, h + 4);
  }
  ctx.restore();
}

function drawBallShadow(ctx, lane) {
  const floorY = FLOOR_Y;
  const ballX = lane.renderX ?? lane.ball.x;
  const ballY = lane.renderY ?? lane.ball.y;
  const ballVx = lane.renderVx ?? lane.ball.vx;
  const ballVy = lane.renderVy ?? lane.ball.vy;
  const depth = clamp((ballY - 200) / 470, 0, 1);
  const sx = lerp(hoopX(lane), ballX, 0.82);
  const sy = floorY;
  const height = clamp(floorY - ballY, 0, 420);
  const speed = Math.hypot(ballVx || 0, ballVy || 0);
  const skid = clamp(Math.abs(ballVx || 0) / 380, 0, 1);
  const squash = clamp(lane.ballSquash || 0, 0, 1);
  const hNorm = clamp(height / 420, 0, 1);
  const rimFade =
    lane.rimTouched || (lane.rimHitCooldown || 0) > 0
      ? clamp(1 - (lane.rimHitCooldown || 0) * 8.5, 0.35, 0.8)
      : 1;
  const alpha = clamp((0.32 - hNorm * 0.2 + speed / 5200) * rimFade, 0.05, 0.38);
  const scaleMul = 1 + hNorm * 0.5;
  const w = (14 + depth * 28 + skid * 7 + squash * 4) * BALL_SIZE_MUL * scaleMul;
  const h = (6 + depth * 11 - squash * 1.5) * (0.9 + BALL_SIZE_MUL * 0.1) * (0.86 + hNorm * 0.35);
  const blurPx = 3 + hNorm * SHADOW_MAX_BLUR;
  ctx.save();
  ctx.filter = `blur(${blurPx}px)`;
  ctx.globalAlpha = alpha * 0.5;
  ctx.fillStyle = "rgba(0,0,0,0.35)";
  ctx.beginPath();
  ctx.ellipse(sx, sy + 1.5, w * 1.28, h * 1.45, 0, 0, Math.PI * 2);
  ctx.fill();
  ctx.globalAlpha = alpha;
  ctx.fillStyle = "rgba(0,0,0,0.62)";
  ctx.beginPath();
  ctx.ellipse(sx + (ballVx || 0) * 0.004, sy, w, h, 0, 0, Math.PI * 2);
  ctx.fill();
  ctx.filter = "none";
  // Contact reflection band for extra depth on the lane surface.
  ctx.globalAlpha = clamp(alpha * 0.28, 0.04, 0.14);
  const reflectionHue = getBallColorway(lane.ballColor).hue;
  ctx.fillStyle = `hsla(${reflectionHue}, 95%, 72%, 0.9)`;
  ctx.beginPath();
  ctx.ellipse(
    sx - (ballVx || 0) * 0.01,
    sy - 2.8,
    Math.max(5, w * 0.68),
    Math.max(2.5, h * 0.52),
    0,
    0,
    Math.PI * 2
  );
  ctx.fill();
  ctx.restore();
}

function drawArcTrail(ctx, lane) {
  if (lane.trail.length < 2) return;
  const colorway = getBallColorway(lane.ballColor);
  const seamBoost = VISUAL_TUNE.seamGlow;
  ctx.save();
  for (let i = 1; i < lane.trail.length; i++) {
    const a = lane.trail[i - 1];
    const b = lane.trail[i];
    const life = clamp((a.life + b.life) * 0.5, 0, 1);
    ctx.strokeStyle = lane.rimFire
      ? `hsla(${lane.ball.hue}, 98%, 62%, ${life * (0.65 + seamBoost * 0.2)})`
      : `hsla(${colorway.hue}, 96%, 68%, ${life * (0.58 + seamBoost * 0.18)})`;
    ctx.shadowColor = lane.rimFire ? `hsl(${lane.ball.hue}, 98%, 62%)` : colorway.body;
    ctx.shadowBlur = 8 * seamBoost;
    ctx.lineWidth = 1.1 + life * 2.2;
    ctx.beginPath();
    ctx.moveTo(a.x, a.y);
    ctx.lineTo(b.x, b.y);
    ctx.stroke();
  }
  ctx.restore();
}

function drawBall(ctx, lane, quality = 1) {
  const r = getBallRadius(lane);
  const vibe = lane.impactVibe || 0;
  const vibeOffsetX = vibe > 0.001 ? Math.sin((lane.ball.spin || 0) * 22) * vibe * 2.1 : 0;
  const vibeOffsetY = vibe > 0.001 ? Math.cos((lane.ball.spin || 0) * 24) * vibe * 1.5 : 0;
  const x = (lane.renderX ?? lane.ball.x) + vibeOffsetX;
  const y = (lane.renderY ?? lane.ball.y) + vibeOffsetY;
  const vx = lane.renderVx ?? lane.ball.vx;
  const vy = lane.renderVy ?? lane.ball.vy;
  const renderQuality = clamp(quality, 0.55, 1);
  const seamBoost = VISUAL_TUNE.seamGlow;

  ctx.save();
  const skin = lane.ballSkin || "classic";
  const colorway = getBallColorway(lane.ballColor);
  const glow = lane.rimFire && lane.state === "flying";
  const plasma = skin === "plasma";
  const prism = skin === "prism";
  const neonPurple = lane.ballColor === "purple" && !plasma && !prism;
  const hue = lane.ball.hue;
  const baseHue = colorway.hue;
  const bodyColor = prism
    ? `hsl(${(baseHue + hue) % 360}, 92%, 58%)`
    : plasma
      ? `hsl(${(baseHue + 140 + (hue % 90)) % 360}, 92%, 56%)`
      : neonPurple
        ? `hsl(${(baseHue + 6 + (hue % 18)) % 360}, 90%, ${glow ? 58 : 46}%)`
      : glow
        ? `hsl(${hue}, 92%, 56%)`
        : colorway.body;
  const hotEdge = prism
    ? `hsl(${(baseHue + hue + 120) % 360}, 96%, 64%)`
    : plasma
      ? `hsl(${(baseHue + 220) % 360}, 98%, 78%)`
      : neonPurple
        ? "hsl(297, 100%, 86%)"
      : glow
        ? colorway.hot
        : colorway.hot;
  const darkEdge = prism
    ? `hsl(${(baseHue + hue + 260) % 360}, 72%, 30%)`
    : plasma
      ? `hsl(${(baseHue + 290) % 360}, 66%, 26%)`
      : neonPurple
        ? "hsl(266, 84%, 18%)"
      : glow
        ? colorway.dark
      : colorway.dark;
  const seamLed = prism
    ? `hsla(${(baseHue + hue + 160) % 360}, 98%, 78%, 0.95)`
    : plasma
      ? `hsla(${(baseHue + 220) % 360}, 98%, 82%, 0.9)`
      : neonPurple
        ? "hsla(302, 100%, 86%, 0.98)"
      : `hsla(${baseHue}, 96%, 80%, 0.88)`;
  const seamPulse = 0.5 + Math.sin((lane.ball.spin || 0) * 1.8 + y * 0.02) * 0.2;
  const speed = Math.hypot(vx || 0, vy || 0);
  const squash = clamp(lane.ballSquash || 0, 0, 1);
  if (squash > 0.001) {
    const sx = 1 + squash * 0.06;
    const sy = 1 - squash * 0.08;
    ctx.translate(x, y);
    ctx.scale(sx, sy);
    ctx.translate(-x, -y);
  }

  if (lane.state === "flying" && speed > 50) {
    const inv = 1 / Math.max(speed, 1);
    const dirX = vx * inv;
    const dirY = vy * inv;
    const motionA = clamp(speed / 900, 0.08, 0.8);
    const trailSlices = renderQuality >= 0.9 ? 4 : renderQuality >= 0.75 ? 3 : 2;
    ctx.save();
    ctx.globalAlpha = motionA * (0.55 + renderQuality * 0.2);
    ctx.strokeStyle = neonPurple
      ? "hsla(300, 100%, 78%, 0.92)"
      : lane.rimFire
      ? `hsla(${hue}, 100%, 68%, 0.85)`
      : `hsla(${baseHue}, 98%, 72%, 0.84)`;
    ctx.shadowColor = neonPurple ? "hsl(296, 95%, 66%)" : lane.rimFire ? `hsl(${hue}, 96%, 62%)` : colorway.body;
    ctx.shadowBlur = (neonPurple ? 20 : 14) * seamBoost;
    ctx.lineCap = "round";
    for (let i = 0; i < trailSlices; i++) {
      const k = trailSlices > 1 ? i / (trailSlices - 1) : 1;
      const tail = r * (1.4 + k * 1.8);
      const spread = (i - 1.5) * r * 0.26;
      const px = x - dirX * tail + -dirY * spread * 0.1;
      const py = y - dirY * tail + dirX * spread * 0.1;
      ctx.lineWidth = 1.1 + (1 - k) * 2.3;
      ctx.beginPath();
      ctx.moveTo(x - dirX * (r * 0.2), y - dirY * (r * 0.2));
      ctx.lineTo(px, py);
      ctx.stroke();
    }
    ctx.restore();
  }

  const halo = ctx.createRadialGradient(x, y, r * 0.25, x, y, r * 2.2);
  halo.addColorStop(
    0,
    prism
      ? `hsla(${(baseHue + 85) % 360}, 88%, 68%, 0.5)`
      : plasma
        ? `hsla(${(baseHue + 150) % 360}, 90%, 64%, 0.44)`
        : colorway.halo
  );
  halo.addColorStop(1, "transparent");
  ctx.fillStyle = halo;
  ctx.beginPath();
  ctx.arc(x, y, r * 2.2, 0, Math.PI * 2);
  ctx.fill();
  if (neonPurple) {
    const corona = ctx.createRadialGradient(x, y, r * 0.25, x, y, r * 3.1);
    corona.addColorStop(0, `rgba(255,210,255,${0.18 + seamBoost * 0.08})`);
    corona.addColorStop(0.45, `rgba(218,112,255,${0.14 + seamBoost * 0.08})`);
    corona.addColorStop(1, "transparent");
    ctx.fillStyle = corona;
    ctx.beginPath();
    ctx.arc(x, y, r * 3.1, 0, TAU);
    ctx.fill();
  }

  const spinPhase = Math.sin((lane.ball.spin || 0) * 0.85);
  const lightX = x - r * (0.34 + spinPhase * 0.08);
  const lightY = y - r * (0.38 + Math.cos((lane.ball.spin || 0) * 0.72) * 0.05);
  const g = ctx.createRadialGradient(lightX, lightY, r * 0.08, x, y, r);
  g.addColorStop(0, hotEdge);
  g.addColorStop(0.4, bodyColor);
  g.addColorStop(1, darkEdge);
  ctx.fillStyle = g;
  ctx.shadowColor = prism || plasma || glow ? bodyColor : colorway.body;
  ctx.shadowBlur = (glow ? Math.max(24, colorway.glowBlur + 4) : colorway.glowBlur) * seamBoost;
  ctx.beginPath();
  ctx.arc(x, y, r, 0, Math.PI * 2);
  ctx.fill();
  // Bottom ambient occlusion keeps the sphere grounded.
  const ao = ctx.createRadialGradient(x, y + r * 0.52, r * 0.25, x, y + r * 0.52, r * 1.12);
  ao.addColorStop(0, "rgba(0,0,0,0.34)");
  ao.addColorStop(1, "transparent");
  ctx.globalAlpha = 0.5;
  ctx.fillStyle = ao;
  ctx.beginPath();
  ctx.arc(x, y, r, 0, TAU);
  ctx.fill();
  ctx.globalAlpha = 1;
  if (renderQuality > 0.68) {
    // Pebble grain pass for a less flat basketball surface.
    const grainCount = renderQuality > 0.9 ? 28 : 18;
    ctx.save();
    ctx.translate(x, y);
    ctx.rotate(lane.ball.spin * 0.35);
    ctx.globalAlpha = 0.12 + renderQuality * 0.06;
    ctx.fillStyle = "rgba(255,255,255,0.18)";
    for (let i = 0; i < grainCount; i++) {
      const a = (TAU * i) / grainCount + (i % 3) * 0.11;
      const radialBand = 0.2 + ((i * 17) % 100) / 100 * 0.68;
      const pr = r * radialBand;
      const gx = Math.cos(a) * pr;
      const gy = Math.sin(a) * pr;
      if (gx * gx + gy * gy > (r * 0.9) * (r * 0.9)) continue;
      const dotR = Math.max(0.45, r * (0.018 + ((i * 9) % 7) * 0.003));
      ctx.beginPath();
      ctx.arc(gx, gy, dotR, 0, TAU);
      ctx.fill();
    }
    ctx.restore();
  }
  // Crisp rim light for cleaner high-res read on moving shots.
  const rimLight = ctx.createRadialGradient(
    x - r * (0.42 + spinPhase * 0.04),
    y - r * 0.46,
    0,
    x - r * 0.18,
    y - r * 0.2,
    r * 0.95
  );
  rimLight.addColorStop(0, "rgba(255,255,255,0.7)");
  rimLight.addColorStop(0.35, "rgba(255,235,205,0.24)");
  rimLight.addColorStop(1, "transparent");
  ctx.fillStyle = rimLight;
  ctx.beginPath();
  ctx.arc(x, y, r, 0, Math.PI * 2);
  ctx.fill();
  if (lane.state === "flying") {
    const airRim = ctx.createRadialGradient(x + r * 0.22, y - r * 0.15, r * 0.05, x, y, r * 1.22);
    airRim.addColorStop(0, "rgba(170,250,255,0.34)");
    airRim.addColorStop(0.5, "rgba(86,235,255,0.14)");
    airRim.addColorStop(1, "transparent");
    ctx.fillStyle = airRim;
    ctx.beginPath();
    ctx.arc(x, y, r * 1.05, 0, TAU);
    ctx.fill();
  }
  // Outer contour reads cleaner at small mobile scales.
  ctx.strokeStyle = lane.rimFire
    ? `hsla(${hue}, 96%, 74%, 0.78)`
    : `hsla(${baseHue}, 94%, 80%, 0.7)`;
  ctx.lineWidth = Math.max(1, r * 0.055);
  ctx.shadowColor = lane.rimFire ? `hsl(${hue}, 95%, 64%)` : colorway.hot;
  ctx.shadowBlur = 10 * seamBoost;
  ctx.beginPath();
  ctx.arc(x, y, r * 0.965, 0, TAU);
  ctx.stroke();

  ctx.strokeStyle = colorway.seam;
  ctx.lineWidth = (neonPurple ? Math.max(1.8, r * 0.132) : Math.max(1.35, r * 0.115)) * (0.9 + seamBoost * 0.1);
  ctx.save();
  ctx.translate(x, y);
  ctx.rotate(lane.ball.spin);
  ctx.beginPath();
  ctx.moveTo(0, -r + 1);
  ctx.lineTo(0, r - 1);
  ctx.stroke();
  ctx.beginPath();
  ctx.moveTo(-r + 1, 0);
  ctx.lineTo(r - 1, 0);
  ctx.stroke();
  ctx.beginPath();
  ctx.arc(-r * 0.5, 0, r * 0.78, -0.35, 0.35);
  ctx.stroke();
  ctx.beginPath();
  ctx.arc(r * 0.5, 0, r * 0.78, Math.PI - 0.35, Math.PI + 0.35);
  ctx.stroke();

  // LED seam pass: crisp neon line + bloom for lit basketball channels.
  ctx.globalAlpha = (neonPurple ? 0.84 : 0.72) + seamPulse * 0.2 * seamBoost;
  ctx.strokeStyle = seamLed;
  ctx.lineWidth = (neonPurple ? Math.max(1.25, r * 0.068) : Math.max(0.9, r * 0.048)) * (0.9 + seamBoost * 0.2);
  ctx.shadowColor = seamLed;
  ctx.shadowBlur = ((neonPurple ? 16 : 10) + seamPulse * 6) * seamBoost;
  ctx.beginPath();
  ctx.moveTo(0, -r + 1);
  ctx.lineTo(0, r - 1);
  ctx.stroke();
  ctx.beginPath();
  ctx.moveTo(-r + 1, 0);
  ctx.lineTo(r - 1, 0);
  ctx.stroke();
  ctx.beginPath();
  ctx.arc(-r * 0.5, 0, r * 0.78, -0.35, 0.35);
  ctx.stroke();
  ctx.beginPath();
  ctx.arc(r * 0.5, 0, r * 0.78, Math.PI - 0.35, Math.PI + 0.35);
  ctx.stroke();

  // Micro LED nodes at seam intersections.
  ctx.fillStyle = seamLed;
  ctx.shadowColor = seamLed;
  ctx.shadowBlur = ((neonPurple ? 17 : 12) + seamPulse * 5) * seamBoost;
  [
    [0, -r + 1],
    [0, r - 1],
    [-r + 1, 0],
    [r - 1, 0],
    [0, 0],
  ].forEach(([sx, sy]) => {
    ctx.beginPath();
    ctx.arc(sx, sy, neonPurple ? Math.max(1.05, r * 0.054) : Math.max(0.8, r * 0.045), 0, Math.PI * 2);
    ctx.fill();
  });
  ctx.restore();
  ctx.restore();
}

function drawTrajectoryGuide(ctx, lane, quality = 1) {
  if (lane.previewAlpha <= 0.01) return;
  if (lane.state !== "charging" && (lane.previewTTL || 0) <= 0.01) return;
  if (lane.isAI && quality < 0.94) return;
  if (lane.dailyModifier === "no_preview" && lane.state === "charging") return;

  const preview = lane.previewPath || predictShotPath(lane);
  if (!preview.points.length) return;
  const previewFraction =
    lane.shotDifficulty === "hard" ? 0.4 : lane.shotDifficulty === "medium" ? 0.75 : 1;
  const visiblePointCount = Math.max(6, Math.floor(preview.points.length * previewFraction));
  const visiblePoints = preview.points.slice(0, visiblePointCount);
  if (!visiblePoints.length) return;

  const powerT = clamp(lane.power, 0, 1);
  const hue = lerp(198, 26, powerT);
  const alignmentBoost = preview.alignment;
  const timingBoost = preview.timing.isPerfect ? 0.35 : 0;
  const ttlFade = lane.state === "charging" ? 1 : clamp((lane.previewTTL || 0) / 0.12, 0, 1);
  const alpha = clamp(lane.previewAlpha * ttlFade * (0.34 + alignmentBoost * 0.5 + timingBoost), 0.08, 1);
  const glow = 7 + alignmentBoost * 10 + (preview.timing.isPerfect ? 8 : 0);
  const lineW = 1.2 + alignmentBoost * 0.8 + (preview.timing.isPerfect ? 0.6 : 0);

  ctx.save();
  ctx.globalAlpha = alpha * 0.92;
  ctx.strokeStyle = `hsla(${hue}, 96%, ${56 + alignmentBoost * 14}%, 0.95)`;
  ctx.shadowColor = `hsla(${hue}, 98%, 64%, 1)`;
  ctx.shadowBlur = glow;
  ctx.lineWidth = lineW;
  const stride = quality < 0.8 ? 5 : quality < 0.9 ? 4 : 3;
  for (let i = 0; i < visiblePoints.length; i += stride) {
    const p = visiblePoints[i];
    const dotT = i / Math.max(1, visiblePoints.length - 1);
    const dotR = 1.6 + (1 - dotT) * 1.5 + alignmentBoost * 0.4;
    ctx.globalAlpha = alpha * (0.35 + (1 - dotT) * 0.65);
    ctx.beginPath();
    ctx.fillStyle = `hsla(${hue}, 96%, ${58 + alignmentBoost * 14}%, 0.9)`;
    ctx.arc(p.x, p.y, dotR, 0, TAU);
    ctx.fill();
  }
  // faint connective guide so the path still reads as a curve.
  ctx.globalAlpha = alpha * 0.22;
  ctx.beginPath();
  ctx.moveTo(visiblePoints[0].x, visiblePoints[0].y);
  for (let i = stride; i < visiblePoints.length; i += stride) {
    ctx.lineTo(visiblePoints[i].x, visiblePoints[i].y);
  }
  ctx.stroke();

  const hx = hoopX(lane);
  const rimNearDist = Math.abs(preview.landingX - hx);
  const rimSnapTolerance = lane.hoop.r * 0.92;
  const nearRim = clamp(1 - rimNearDist / rimSnapTolerance, 0, 1);
  const snapToRim = clamp(nearRim * 0.36 + (preview.timing.isPerfect ? 0.08 : 0), 0, 0.44);
  const landingPoint = preview.points[preview.points.length - 1] || visiblePoints[visiblePoints.length - 1];
  const landingX = previewFraction < 0.5 ? landingPoint.x : preview.landingX;
  const landingY = previewFraction < 0.5 ? landingPoint.y : preview.floorY;
  const circleX = lerp(landingX, hx, snapToRim);
  const pulse = lane.landingPulse > 0.01 ? 1 + lane.landingPulse * 0.5 : 1;
  const rad = (14 - alignmentBoost * 8) * pulse;
  ctx.globalAlpha = clamp(alpha * 0.9 + lane.landingPulse * 0.35, 0.18, 1);
  ctx.fillStyle = `hsla(${hue}, 95%, 62%, 0.22)`;
  ctx.strokeStyle = `hsla(${hue}, 98%, 68%, 0.95)`;
  ctx.shadowBlur = 16 + alignmentBoost * 10;
  ctx.lineWidth = 2;
  ctx.beginPath();
  ctx.arc(circleX, landingY, rad, 0, Math.PI * 2);
  ctx.fill();
  ctx.stroke();
  ctx.restore();
}

function computePreviewArcMetrics(lane, preview) {
  if (!lane || !preview?.points?.length) return null;
  const hx = hoopX(lane);
  const hy = getRimCenterY(lane);
  const rimPlaneY = hy - lane.hoop.r * 0.22;
  let apex = preview.points[0];
  for (let i = 1; i < preview.points.length; i++) {
    if (preview.points[i].y < apex.y) apex = preview.points[i];
  }
  const clearancePx = rimPlaneY - apex.y;
  return {
    apexX: apex.x,
    apexY: apex.y,
    rimPlaneY,
    clearancePx,
    arcOk: clearancePx >= AUTO_ARC_CLEAR_MIN,
    rimX: hx,
  };
}

function drawArcDebugOverlay(ctx, lane) {
  if (!lane) return;
  const preview = lane.previewPath || predictShotPath(lane);
  if (!preview?.points?.length) return;
  const metrics = computePreviewArcMetrics(lane, preview);
  if (!metrics) return;
  const { rimX: hx, rimPlaneY, apexX, apexY, clearancePx, arcOk } = metrics;

  ctx.save();
  ctx.globalAlpha = 0.9;
  ctx.strokeStyle = arcOk ? "rgba(110,255,190,0.95)" : "rgba(255,120,120,0.95)";
  ctx.lineWidth = 1.6;
  ctx.setLineDash([8, 6]);
  ctx.beginPath();
  ctx.moveTo(hx - lane.hoop.r * 2.1, rimPlaneY);
  ctx.lineTo(hx + lane.hoop.r * 2.1, rimPlaneY);
  ctx.stroke();
  ctx.setLineDash([]);

  ctx.fillStyle = "rgba(255,245,180,0.95)";
  ctx.shadowColor = "rgba(255,220,120,0.9)";
  ctx.shadowBlur = 10;
  ctx.beginPath();
  ctx.arc(apexX, apexY, 4, 0, TAU);
  ctx.fill();

  ctx.strokeStyle = arcOk ? "rgba(110,255,190,0.6)" : "rgba(255,120,120,0.6)";
  ctx.lineWidth = 1;
  ctx.beginPath();
  ctx.moveTo(apexX, apexY);
  ctx.lineTo(apexX, rimPlaneY);
  ctx.stroke();

  ctx.font = '700 10px "Orbitron", sans-serif';
  ctx.textAlign = "left";
  ctx.fillStyle = arcOk ? "#84ffd4" : "#ff8d8d";
  ctx.shadowBlur = 0;
  ctx.fillText(
    `ARC CLEAR ${Math.round(clearancePx)}px (min ${Math.round(AUTO_ARC_CLEAR_MIN)}px)`,
    hx - lane.hoop.r * 2.1,
    rimPlaneY - 8
  );
  ctx.fillStyle = "rgba(176,228,255,0.9)";
  ctx.fillText(
    `ANGLE ${Math.round(lane.angle)}°  POWER ${Math.round((lane.power || 0) * 100)}%`,
    hx - lane.hoop.r * 2.1,
    rimPlaneY - 20
  );
  ctx.restore();
}

function drawShotAssist(ctx, lane, t, quality = 1) {
  if (quality < 0.8) return;
  if (lane.isAI && quality < 0.92) return;
  const hx = hoopX(lane);
  const hy = getRimCenterY(lane);
  if (lane.state === "charging") {
    const powerA = clamp(0.2 + lane.power * 0.7, 0.2, 0.92);
    const ringR = 18 + lane.power * 18 + Math.sin(t * 0.018) * 1.8;
    ctx.save();
    ctx.globalAlpha = powerA * 0.88;
    ctx.strokeStyle = lane.releaseType === "perfect" ? "#8dffd8" : lane.id === "player" ? "#66ecff" : "#ffae88";
    ctx.shadowColor = ctx.strokeStyle;
    ctx.shadowBlur = 12;
    ctx.lineWidth = 2.2;
    ctx.beginPath();
    ctx.arc(lane.ball.x, lane.ball.y, ringR, 0, Math.PI * 2);
    ctx.stroke();

    const dx = hx - lane.ball.x;
    const dy = hy - lane.ball.y;
    const dist = Math.max(1, Math.hypot(dx, dy));
    const dirX = dx / dist;
    const dirY = dy / dist;
    ctx.setLineDash([6, 10]);
    ctx.globalAlpha = 0.34 + lane.power * 0.45;
    ctx.lineWidth = 1.6;
    ctx.beginPath();
    ctx.moveTo(lane.ball.x + dirX * (ringR + 3), lane.ball.y + dirY * (ringR + 3));
    ctx.lineTo(hx - dirX * 10, hy - dirY * 10);
    ctx.stroke();
    ctx.setLineDash([]);

    // Elastic drag tether: ball to drag point for clearer physical release feedback.
    const dragDX = (lane.aimX ?? lane.ball.x) - lane.ball.x;
    const dragDY = (lane.aimY ?? lane.ball.y) - lane.ball.y;
    const dragDist = Math.hypot(dragDX, dragDY);
    if (dragDist > 4) {
      const pull = Math.min(ELASTIC_MAX_DRAG, dragDist);
      const inv = 1 / Math.max(dragDist, 1);
      const anchorX = lane.ball.x + dragDX * inv * pull;
      const anchorY = lane.ball.y + dragDY * inv * pull;
      ctx.globalAlpha = 0.2 + lane.power * 0.5;
      ctx.strokeStyle = lane.id === "player" ? "rgba(120,245,255,0.92)" : "rgba(255,170,132,0.92)";
      ctx.shadowColor = lane.id === "player" ? "#62ecff" : "#ff9d7d";
      ctx.shadowBlur = 14;
      ctx.lineWidth = 1.6 + lane.power * 1.4;
      ctx.beginPath();
      ctx.moveTo(lane.ball.x, lane.ball.y);
      const midX = lerp(lane.ball.x, anchorX, 0.5) + lane.side * 8 * lane.power;
      const midY = lerp(lane.ball.y, anchorY, 0.5) - 10 * lane.power;
      ctx.quadraticCurveTo(midX, midY, anchorX, anchorY);
      ctx.stroke();
      ctx.globalAlpha = 0.5 + lane.power * 0.35;
      ctx.beginPath();
      ctx.arc(anchorX, anchorY, 3 + lane.power * 2.5, 0, TAU);
      ctx.fillStyle = lane.id === "player" ? "rgba(158,255,246,0.95)" : "rgba(255,205,172,0.95)";
      ctx.fill();
    }

    const gate = lane.releaseWindow?.center ?? 0.7;
    const gatePulse = 0.5 + Math.sin(t * 0.015 + gate * 6) * 0.5;
    ctx.globalAlpha = 0.18 + gatePulse * 0.36;
    ctx.strokeStyle = "#9dffd4";
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.arc(lane.ball.x, lane.ball.y, ringR + 6 + gate * 8, Math.PI * 0.14, Math.PI * 0.86);
    ctx.stroke();
    ctx.restore();
  }

  if (lane.state === "flying") {
    const dx = hx - lane.ball.x;
    const dy = hy - lane.ball.y;
    const dist = Math.max(1, Math.hypot(dx, dy));
    const dirX = dx / dist;
    const dirY = dy / dist;
    const speed = Math.hypot(lane.ball.vx || 0, lane.ball.vy || 0);
    const guideA = clamp(speed / 1000, 0.12, 0.72);
    ctx.save();
    ctx.globalAlpha = guideA * 0.72;
    ctx.strokeStyle = lane.rimFire ? `hsla(${lane.ball.hue}, 100%, 70%, 0.9)` : "rgba(120,240,255,0.78)";
    ctx.shadowColor = lane.rimFire ? `hsl(${lane.ball.hue}, 92%, 60%)` : "#78e9ff";
    ctx.shadowBlur = 10;
    ctx.lineWidth = 1.7;
    ctx.beginPath();
    ctx.moveTo(lane.ball.x, lane.ball.y);
    ctx.lineTo(hx, hy);
    ctx.stroke();

    for (let i = 0; i < 3; i++) {
      const d = 24 + i * 18;
      const cx = lane.ball.x + dirX * d;
      const cy = lane.ball.y + dirY * d;
      const nx = -dirY;
      const ny = dirX;
      ctx.lineWidth = 1.2 + (2 - i) * 0.35;
      ctx.beginPath();
      ctx.moveTo(cx - dirX * 7 + nx * 5, cy - dirY * 7 + ny * 5);
      ctx.lineTo(cx, cy);
      ctx.lineTo(cx - dirX * 7 - nx * 5, cy - dirY * 7 - ny * 5);
      ctx.stroke();
    }
    ctx.restore();
  }
}

function drawParticles(ctx, particles) {
  const drawStep = Math.max(
    1,
    Math.round(Number.isFinite(VISUAL_TUNE.particleDrawStep) ? VISUAL_TUNE.particleDrawStep : 1)
  );
  const blurMul = clamp(
    Number.isFinite(VISUAL_TUNE.particleBlurMul) ? VISUAL_TUNE.particleBlurMul : 1,
    0.62,
    1
  );
  for (let i = 0; i < particles.length; i += drawStep) {
    const p = particles[i];
    if (p.life <= 0.01) continue;
    ctx.save();
    ctx.globalAlpha = p.life;
    ctx.fillStyle = p.color;
    ctx.shadowColor = p.color;
    ctx.shadowBlur = 12 * blurMul;
    ctx.beginPath();
    ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
  }
}

function drawFlashText(ctx, lane) {
  if (lane.flashAlpha <= 0.01 || !lane.flash) return;
  const tier = lane.outcomeTier || "idle";
  const scale = tier === "perfect" ? 1.16 : tier === "swish" ? 1.08 : tier === "miss" ? 0.96 : 1;
  const yLift = tier === "perfect" ? 10 : tier === "rim_in" ? 6 : 0;
  ctx.save();
  ctx.globalAlpha = lane.flashAlpha;
  ctx.font = `700 ${Math.round(30 * scale)}px "Orbitron", sans-serif`;
  ctx.textAlign = "center";
  ctx.fillStyle = lane.flashColor;
  ctx.shadowColor = lane.flashColor;
  ctx.shadowBlur = tier === "perfect" ? 30 : 24;
  ctx.fillText(lane.flash, lane.centerX, 128 - yLift);
  ctx.restore();
}

function drawReleaseCue(ctx, lane) {
  if (lane.releaseCue <= 0.01) return;
  const cueX = lane.launchX ?? lane.home.x;
  const cueY = lane.launchY ?? lane.home.y;
  const cueColor =
    lane.releaseType === "perfect"
      ? "#b8fff1"
      : lane.releaseType === "early"
        ? "#ffd38f"
        : "#ffb0a0";
  const radius = 10 + (1 - lane.releaseCue) * 34;
  ctx.save();
  ctx.globalAlpha = lane.releaseCue * 0.75;
  ctx.strokeStyle = cueColor;
  ctx.lineWidth = 2.1;
  ctx.shadowColor = cueColor;
  ctx.shadowBlur = 16;
  ctx.beginPath();
  ctx.arc(cueX, cueY, radius, 0, Math.PI * 2);
  ctx.stroke();
  if (lane.releaseSpark > 0.01) {
    ctx.globalAlpha = lane.releaseSpark * 0.85;
    for (let i = 0; i < 7; i++) {
      const a = (Math.PI * 2 * i) / 7 + (1 - lane.releaseCue) * 0.9;
      const r = 8 + lane.releaseSpark * 15;
      const x = cueX + Math.cos(a) * r;
      const y = cueY + Math.sin(a) * r;
      ctx.beginPath();
      ctx.arc(x, y, 1.6 + lane.releaseSpark * 1.4, 0, Math.PI * 2);
      ctx.fillStyle = cueColor;
      ctx.fill();
    }
  }
  ctx.restore();
}

function drawPressureBox(ctx, lane, x, y, t) {
  const width = 178;
  const height = 54;
  const tune = DIFFICULTY_TUNING[lane.shotDifficulty] || DIFFICULTY_TUNING.medium;
  const angleFit = clamp(1 - Math.abs(lane.angle - tune.idealAngle) / 15, 0, 1);

  ctx.save();
  ctx.fillStyle = "rgba(8,18,30,0.42)";
  ctx.fillRect(x, y, width, height);
  ctx.strokeStyle = lane.id === "player" ? "rgba(66,222,255,0.52)" : "rgba(255,143,106,0.52)";
  ctx.lineWidth = 1.2;
  ctx.strokeRect(x, y, width, height);

  ctx.fillStyle = "#a8d8ff";
  ctx.font = '600 10px "Saira", sans-serif';
  ctx.fillText(`${lane.label} PRESSURE`, x + 8, y + 14);

  const bx = x + 8;
  const by = y + 18;
  const bw = width - 16;
  const bh = 10;

  ctx.fillStyle = "rgba(255,255,255,0.08)";
  ctx.fillRect(bx, by, bw, bh);

  const filled = bw * lane.power;
  const shift = ((t * 0.002 + (lane.id === "player" ? 0 : 0.5)) % 1) * bw;
  const bar = ctx.createLinearGradient(bx + shift - bw, by, bx + shift, by);
  bar.addColorStop(0, "#ff4545");
  bar.addColorStop(0.45, "#ffd400");
  bar.addColorStop(0.7, "#63ff9a");
  bar.addColorStop(1, "#00ffe1");
  ctx.fillStyle = bar;
  ctx.fillRect(bx, by, filled, bh);
  if (lane.power > 0.82 && lane.state === "charging") {
    const pulse = 0.45 + Math.sin(t * 0.02) * 0.25;
    ctx.globalAlpha = clamp(pulse, 0.25, 0.85);
    ctx.fillStyle = lane.id === "player" ? "rgba(132,255,220,0.65)" : "rgba(255,192,136,0.65)";
    ctx.fillRect(bx, by, filled, bh);
    ctx.globalAlpha = 1;
  }

  // Dynamic perfect pressure window.
  const perfectCenter = lane.releaseWindow?.center ?? 0.7;
  const perfectWidth = lane.releaseWindow?.width ?? 0.14;
  const perfectL = bx + bw * (perfectCenter - perfectWidth * 0.5);
  const perfectW = bw * perfectWidth;
  ctx.strokeStyle = "rgba(255,255,255,0.35)";
  ctx.shadowColor = "#80ffcd";
  ctx.shadowBlur = 10;
  ctx.strokeRect(perfectL, by - 2, perfectW, bh + 4);
  if (
    lane.power >= perfectCenter - perfectWidth * 0.5 &&
    lane.power <= perfectCenter + perfectWidth * 0.5 &&
    lane.state === "charging"
  ) {
    ctx.fillStyle = "rgba(120,255,206,0.2)";
    ctx.fillRect(perfectL, by - 2, perfectW, bh + 4);
  }

  const stateText =
    lane.state === "charging"
      ? lane.isAI
        ? "RELEASE"
        : TIMING_ONLY_SHOTS
          ? "HOLD TO CHARGE"
          : "DRAG AIM + RELEASE"
      : lane.state === "flying"
        ? "SHOT IN AIR"
        : lane.state === "returning"
          ? "RELOADING"
          : "HOLD + RELEASE";

  ctx.fillStyle = lane.state === "charging" ? "#63ff9a" : "#9bbad1";
  ctx.fillText(stateText, x + 8, y + 41);

  if (!lane.isAI) {
    const assistPct = Math.round(((lane.dynamicAssist || 0) / DYNAMIC_ASSIST_MAX) * 100);
    const ax = x + 56;
    const ay = y + 45;
    const aw = 52;
    const ah = 3;
    ctx.fillStyle = "rgba(255,255,255,0.09)";
    ctx.fillRect(ax, ay, aw, ah);
    const assistFill = clamp((lane.dynamicAssist || 0) / DYNAMIC_ASSIST_MAX, 0, 1);
    const ag = ctx.createLinearGradient(ax, ay, ax + aw, ay);
    ag.addColorStop(0, "#70d6ff");
    ag.addColorStop(1, "#7effb6");
    ctx.fillStyle = ag;
    ctx.fillRect(ax, ay, aw * assistFill, ah);
    ctx.fillStyle = "rgba(155,210,235,0.9)";
    ctx.font = '600 7px "Saira", sans-serif';
    ctx.fillText(`ADAPT ${assistPct}%`, ax - 50, ay + 4);
  }

  // Dial meter (replaces numeric angle text).
  const cx = x + width - 24;
  const cy = y + 30;
  const radius = 15;
  ctx.beginPath();
  ctx.arc(cx, cy, radius, Math.PI * 0.76, Math.PI * 2.24);
  ctx.strokeStyle = "rgba(255,255,255,0.24)";
  ctx.lineWidth = 1.6;
  ctx.stroke();

  const pointerA = (Math.PI * 0.75) + ((lane.angle - 42) / 26) * (Math.PI * 1.5);
  const idealA = (Math.PI * 0.75) + ((tune.idealAngle - 42) / 26) * (Math.PI * 1.5);
  for (let i = 0; i <= 5; i++) {
    const a = Math.PI * 0.76 + (i / 5) * Math.PI * 1.48;
    const ix = cx + Math.cos(a) * (radius - 5);
    const iy = cy + Math.sin(a) * (radius - 5);
    const ox = cx + Math.cos(a) * (radius + 2);
    const oy = cy + Math.sin(a) * (radius + 2);
    ctx.strokeStyle = "rgba(180,220,255,0.28)";
    ctx.lineWidth = 0.9;
    ctx.beginPath();
    ctx.moveTo(ix, iy);
    ctx.lineTo(ox, oy);
    ctx.stroke();
  }
  ctx.beginPath();
  ctx.moveTo(cx + Math.cos(idealA) * (radius - 7), cy + Math.sin(idealA) * (radius - 7));
  ctx.lineTo(cx + Math.cos(idealA) * (radius + 3), cy + Math.sin(idealA) * (radius + 3));
  ctx.strokeStyle = "rgba(140,255,205,0.9)";
  ctx.lineWidth = 1.05;
  ctx.stroke();

  const pointerCol = `hsl(${lerp(8, 182, angleFit)}, 92%, ${lerp(60, 68, angleFit)}%)`;
  ctx.beginPath();
  ctx.moveTo(cx, cy);
  ctx.lineTo(cx + Math.cos(pointerA) * (radius - 3), cy + Math.sin(pointerA) * (radius - 3));
  ctx.strokeStyle = pointerCol;
  ctx.lineWidth = 1.8;
  ctx.stroke();
  ctx.fillStyle = pointerCol;
  ctx.beginPath();
  ctx.arc(cx, cy, 3, 0, Math.PI * 2);
  ctx.fill();
  ctx.restore();
}

function NeonHoopzHeader({ playerName, backendStatus }) {
  const networkOffline = backendStatus !== "online";
  return (
    <div style={{ marginBottom: 12, width: "100%", display: "flex", flexDirection: "column", alignItems: "center" }}>
      <div
        className="logoBallPulse"
        style={{
          margin: "0 auto 14px",
          width: 132,
          height: 132,
          borderRadius: "50%",
          border: "4px solid rgba(255, 162, 46, 0.86)",
          boxShadow:
            "0 0 18px rgba(255,156,32,0.65), 0 0 44px rgba(255,126,26,0.5), inset 0 0 34px rgba(255,142,20,0.62)",
          display: "grid",
          placeItems: "center",
          position: "relative",
          background:
            "radial-gradient(circle at 32% 28%, rgba(255,228,176,0.92) 0%, rgba(255,176,75,0.74) 18%, rgba(214,98,13,0.84) 58%, rgba(103,36,0,0.9) 100%)",
          overflow: "hidden",
        }}
      >
        <div style={{ position: "absolute", inset: 0, borderRadius: "50%", boxShadow: "inset 0 0 30px rgba(0,0,0,0.38)" }} />
        <div className="logoBallSeam logoBallSeamVertical" />
        <div className="logoBallSeam logoBallSeamHorizontal" />
        <div className="logoBallSeam logoBallSeamDiagA" />
        <div className="logoBallSeam logoBallSeamDiagB" />
        <div
          style={{
            position: "absolute",
            inset: 6,
            borderRadius: "50%",
            border: "1px solid rgba(255, 240, 210, 0.6)",
            boxShadow: "0 0 20px rgba(255, 200, 120, 0.38)",
          }}
        />
        <div
          style={{
            position: "absolute",
            top: 16,
            left: 24,
            width: 46,
            height: 24,
            borderRadius: 999,
            transform: "rotate(-16deg)",
            background: "radial-gradient(circle, rgba(255,240,220,0.82) 0%, rgba(255,240,220,0.12) 72%, transparent 100%)",
            filter: "blur(0.2px)",
          }}
        />
      </div>

      <h1
        style={{
          position: "relative",
          margin: "0 0 12px",
          width: "100%",
          textAlign: "center",
          fontFamily: "Orbitron, sans-serif",
          fontSize: "clamp(36px, 7vw, 72px)",
          lineHeight: 1.02,
          fontWeight: 900,
          fontStyle: "italic",
          letterSpacing: 1,
          textTransform: "uppercase",
        }}
      >
        <span style={{ color: "#e0ffff", textShadow: "0 0 8px #fff, 0 0 25px #00ffff" }}>NEON</span>
        <span style={{ marginLeft: 14, color: "#78e9ff", textShadow: "0 0 20px #00ffff" }}>HOOPZ</span>
        <span
          style={{
            position: "absolute",
            inset: 0,
            opacity: 0.2,
            color: "#baf9ff",
            filter: "blur(2px)",
            animation: "flicker 3s infinite",
            pointerEvents: "none",
          }}
        >
          NEON HOOPZ
        </span>
      </h1>

      <p style={{ margin: "0 0 14px", width: "100%", textAlign: "center", fontSize: 11, letterSpacing: "0.3em", textTransform: "uppercase", color: "rgba(56, 220, 255, 0.45)" }}>
        Live From Forrest City, USA
      </p>

      <div
        style={{
          margin: "0 auto",
          width: "100%",
          maxWidth: 760,
          display: "flex",
          justifyContent: "space-between",
          gap: 10,
          borderTop: "1px solid rgba(28, 84, 112, 0.45)",
          paddingTop: 8,
          fontSize: 10,
          textTransform: "uppercase",
          color: "rgba(72, 194, 222, 0.55)",
          letterSpacing: 1.1,
          flexWrap: "wrap",
        }}
      >
        <span>{`Player Tag: ${playerName || "PLAYER ONE"}`}</span>
        <span
          style={{
            color: networkOffline ? "#ff2e63" : "#70ffc4",
            textShadow: networkOffline ? "0 0 10px #ff2e63" : "0 0 10px #70ffc4",
            animation: networkOffline ? "pulse 1.2s infinite" : "none",
          }}
        >
          {networkOffline ? "● Network Offline" : "● Network Online"}
        </span>
      </div>
    </div>
  );
}

export default function NeonHoopz() {
  const showDevPanel = import.meta.env.DEV;
  const [screen, setScreen] = useState("menu");
  const [timeLeft, setTimeLeft] = useState(GAME_LENGTH);
  const [vrMode, setVrMode] = useState(false);
  const [oneViewMode, setOneViewMode] = useState(false);
  const [practiceMode, setPracticeMode] = useState(false);
  const [movingRims, setMovingRims] = useState(false);
  const [matchLength, setMatchLength] = useState(GAME_LENGTH);
  const [dailyMode, setDailyMode] = useState(false);
  const [shotDifficulty, setShotDifficulty] = useState(DEFAULT_GAME_SETUP.shotDifficulty);
  const [shotTuningPreset, setShotTuningPreset] = useState("casual");
  const [visualTuningPreset, setVisualTuningPreset] = useState("neon_premium");
  const [boardBounceRestitution, setBoardBounceRestitution] = useState(
    BANK_SHOT_TUNING_DEFAULT.boardBounceRestitution
  );
  const [rimPullAfterBoard, setRimPullAfterBoard] = useState(
    BANK_SHOT_TUNING_DEFAULT.rimPullAfterBoard
  );
  const [rimInChanceMul, setRimInChanceMul] = useState(RIM_FEEL_TUNING_DEFAULT.rimInChanceMul);
  const [rimSoftness, setRimSoftness] = useState(RIM_FEEL_TUNING_DEFAULT.rimSoftness);
  const [floorBounceSoftness, setFloorBounceSoftness] = useState(
    RIM_FEEL_TUNING_DEFAULT.floorBounceSoftness
  );
  const [easyPlusAssist, setEasyPlusAssist] = useState(true);
  const [aimMode, setAimMode] = useState(DEFAULT_GAME_SETUP.aimMode);
  const [dominantHand, setDominantHand] = useState(DEFAULT_GAME_SETUP.dominantHand);
  const [aimSensitivityScale, setAimSensitivityScale] = useState(1);
  const [controlProfile, setControlProfile] = useState(DEFAULT_GAME_SETUP.controlProfile);
  const [debugArcOverlay, setDebugArcOverlay] = useState(false);
  const [autoArcCalibrate, setAutoArcCalibrate] = useState(true);
  const [autoArcCalStatus, setAutoArcCalStatus] = useState(
    `Auto arc calibration armed (${AUTO_ARC_CALIBRATION_INTERVAL} shots per pass).`
  );
  const [autoArcCalProgress, setAutoArcCalProgress] = useState({
    easy: 0,
    medium: 0,
    hard: 0,
  });
  const [menuAccordionOpen, setMenuAccordionOpen] = useState("difficulty");
  const [shotMechanic, setShotMechanic] = useState("combo");
  const [physicsProfile, setPhysicsProfile] = useState(DEFAULT_GAME_SETUP.physicsProfile);
  const [arcHeightTuning, setArcHeightTuning] = useState(SMOOTH_EASY_TUNE.arcHeightTuning);
  const [lateralForgiveness, setLateralForgiveness] = useState(SMOOTH_EASY_TUNE.lateralForgiveness);
  const [releaseForgiveness, setReleaseForgiveness] = useState(SMOOTH_EASY_TUNE.releaseForgiveness);
  const [dailyModifier, setDailyModifier] = useState("none");
  const [ballSkin, setBallSkin] = useState(DEFAULT_GAME_SETUP.ballSkin);
  const [playerBallColor, setPlayerBallColor] = useState(DEFAULT_GAME_SETUP.playerBallColor);
  const [opponentBallColor, setOpponentBallColor] = useState(DEFAULT_GAME_SETUP.opponentBallColor);
  const [courtSkin, setCourtSkin] = useState("neon");
  const [playerName, setPlayerName] = useState("PLAYER ONE");
  const [backendStatus, setBackendStatus] = useState("checking");
  const [viewport, setViewport] = useState(() => detectViewportMetrics());
  const [backendLeaderboard, setBackendLeaderboard] = useState([]);
  const [recentMatches, setRecentMatches] = useState([]);
  const [shotTelemetry, setShotTelemetry] = useState(() => loadShotTelemetry());
  const [bestScore, setBestScore] = useState(0);
  const [ladderPoints, setLadderPoints] = useState(0);
  const [achievements, setAchievements] = useState({
    releaseArtist: false,
    cleanShooter: false,
    clutchFinisher: false,
  });
  const [hud, setHud] = useState({
    playerScore: 0,
    opponentScore: 0,
    playerStreak: 0,
    opponentStreak: 0,
    playerFire: false,
    opponentFire: false,
    playerStyle: "",
    opponentStyle: "",
  });
  const [finalState, setFinalState] = useState(null);
  const [rankUpFx, setRankUpFx] = useState({
    active: false,
    label: "",
    color: "#9ce8ff",
  });

  const screenRef = useRef("menu");
  const canvasRef = useRef(null);
  const rafRef = useRef(null);
  const lanesRef = useRef([]);
  const particlesRef = useRef([]);
  const sessionRef = useRef(null);
  const sessionEpochRef = useRef(0);
  const sanityLoggedRef = useRef(false);
  const keyHeldRef = useRef(false);
  const pointerRef = useRef({
    active: false,
    id: null,
    startX: 0,
    startY: 0,
    startT: 0,
    startPower: 0.35,
    rawX: 0,
    rawY: 0,
    x: 0,
    y: 0,
    lastX: 0,
    lastY: 0,
    lastMoveMs: 0,
    history: [],
    releaseY: 0,
    releaseTracking: false,
    releaseExpire: 0,
    pointerType: "touch",
    inputCal: detectRuntimeInputCalibration(),
    moveAvgMs: 16,
    jitterAvg: 0,
  });
  const inputCalRef = useRef(detectRuntimeInputCalibration());
  const timeLeftRef = useRef(GAME_LENGTH);
  const ladderPointsRef = useRef(0);
  const autoArcCalibrateRef = useRef(autoArcCalibrate);
  const arcHeightTuningRef = useRef(arcHeightTuning);
  const lateralForgivenessRef = useRef(lateralForgiveness);
  const releaseForgivenessRef = useRef(releaseForgiveness);
  const telemetrySaveTimerRef = useRef(null);
  const pendingTelemetryRef = useRef(loadShotTelemetry());
  const autoArcCalRef = useRef(createArcCalState());
  const fxRef = useRef({
    zoom: 1,
    tilt: 0,
    followRoll: 0,
    followPitch: 0,
    followDriftX: 0,
    followDriftY: 0,
    shotRush: 0,
    shake: 0,
    shakeX: 0,
    shakeY: 0,
    chargeGlow: 0,
    parallax: 0,
    camX: 0,
    camY: 0,
    quality: 1,
    clutchPulse: 0,
    heavyFrames: 0,
    lowLatencyBoost: 0,
    renderAlpha: 1,
    slowMoTime: 0,
  });
  const uiTapRef = useRef(0);
  const audioReadyRef = useRef(false);
  const rankUpTimeoutRef = useRef(null);
  const settingsHydratedRef = useRef(false);
  const backendRefreshSeqRef = useRef(0);
  const backendRetryAtRef = useRef(0);

  const ensureAudioReady = useCallback(() => {
    if (audioReadyRef.current) return;
    try {
      const ctx = getAC();
      if (ctx?.state === "suspended" && ctx.resume) ctx.resume();
      audioReadyRef.current = true;
    } catch (_) {}
  }, []);

  const playUiTap = useCallback(() => {
    const nowMs = performance.now();
    if (nowMs - uiTapRef.current < 95) return;
    uiTapRef.current = nowMs;
    SFX.ui();
  }, []);

  const triggerRankUpCeremony = useCallback((tier) => {
    if (!tier) return;
    if (rankUpTimeoutRef.current) clearTimeout(rankUpTimeoutRef.current);
    setRankUpFx({
      active: true,
      label: tier.label,
      color: tier.color,
    });
    SFX.rankUp();
    rankUpTimeoutRef.current = setTimeout(() => {
      setRankUpFx((prev) => (prev.active ? { ...prev, active: false } : prev));
      rankUpTimeoutRef.current = null;
    }, 2400);
  }, []);

  useEffect(
    () => () => {
      if (rankUpTimeoutRef.current) clearTimeout(rankUpTimeoutRef.current);
    },
    []
  );

  useEffect(() => {
    screenRef.current = screen;
  }, [screen]);

  useEffect(() => {
    timeLeftRef.current = timeLeft;
  }, [timeLeft]);

  useEffect(() => {
    ladderPointsRef.current = ladderPoints;
  }, [ladderPoints]);

  useEffect(() => {
    autoArcCalibrateRef.current = autoArcCalibrate;
    if (autoArcCalibrate) {
      setAutoArcCalStatus(`Auto arc calibration armed (${AUTO_ARC_CALIBRATION_INTERVAL} shots per pass).`);
    } else {
      setAutoArcCalStatus("Auto arc calibration paused.");
    }
  }, [autoArcCalibrate]);

  useEffect(() => {
    arcHeightTuningRef.current = arcHeightTuning;
  }, [arcHeightTuning]);

  useEffect(() => {
    lateralForgivenessRef.current = lateralForgiveness;
  }, [lateralForgiveness]);

  useEffect(() => {
    releaseForgivenessRef.current = releaseForgiveness;
  }, [releaseForgiveness]);

  useEffect(() => {
    pendingTelemetryRef.current = shotTelemetry;
    if (telemetrySaveTimerRef.current) return;
    telemetrySaveTimerRef.current = setTimeout(() => {
      telemetrySaveTimerRef.current = null;
      saveShotTelemetry(pendingTelemetryRef.current);
    }, 1000);
  }, [shotTelemetry]);

  useEffect(
    () => () => {
      if (telemetrySaveTimerRef.current) {
        clearTimeout(telemetrySaveTimerRef.current);
        telemetrySaveTimerRef.current = null;
      }
      saveShotTelemetry(pendingTelemetryRef.current);
    },
    []
  );

  useEffect(() => {
    applyShotTuningPreset(shotTuningPreset);
  }, [shotTuningPreset]);

  useEffect(() => {
    applyVisualTuningPreset(visualTuningPreset);
  }, [visualTuningPreset]);

  useEffect(() => {
    applyBankShotTuning({
      boardBounceRestitution,
      rimPullAfterBoard,
    });
  }, [boardBounceRestitution, rimPullAfterBoard]);

  useEffect(() => {
    applyRimFeelTuning({
      rimInChanceMul,
      rimSoftness,
      floorBounceSoftness,
    });
  }, [rimInChanceMul, rimSoftness, floorBounceSoftness]);

  useEffect(() => {
    try {
      const saved = window.localStorage.getItem("neonhoopz-player-name");
      if (saved && saved.trim()) setPlayerName(saved.trim().slice(0, 24));
    } catch (_) {}
  }, []);

  useEffect(() => {
    try {
      window.localStorage.setItem("neonhoopz-player-name", playerName.trim().slice(0, 24));
    } catch (_) {}
  }, [playerName]);

  useEffect(() => {
    if (settingsHydratedRef.current) return;
    const stored = loadStoredSettings();
    const inputCal = inputCalRef.current;
    const smartDefaults = getSmartDefaults({ platform: inputCal.platform });
    const oneOf = (value, choices, fallback) =>
      choices.includes(value) ? value : fallback;

    if (stored) {
      setVrMode(!!stored.vrMode);
      setOneViewMode(!!stored.oneViewMode);
      setPracticeMode(!!stored.practiceMode);
      setMovingRims(typeof stored.movingRims === "boolean" ? stored.movingRims : false);
      setDailyMode(!!stored.dailyMode);
      setShotDifficulty(oneOf(stored.shotDifficulty, Object.keys(DIFFICULTY_TUNING), shotDifficulty));
      setShotTuningPreset(oneOf(stored.shotTuningPreset, Object.keys(SHOT_TUNING_PRESETS), shotTuningPreset));
      setVisualTuningPreset(
        oneOf(stored.visualTuningPreset, Object.keys(VISUAL_TUNING_PRESETS), visualTuningPreset)
      );
      setEasyPlusAssist(typeof stored.easyPlusAssist === "boolean" ? stored.easyPlusAssist : easyPlusAssist);
      setAimMode(oneOf(stored.aimMode, Object.keys(AIM_MODES), aimMode));
      setDominantHand(oneOf(stored.dominantHand, ["left", "right"], dominantHand));
      setControlProfile(oneOf(stored.controlProfile, Object.keys(CONTROL_PROFILES), controlProfile));
      setShotMechanic(oneOf(stored.shotMechanic, ["hold", "aim", "release", "combo"], shotMechanic));
      setPhysicsProfile(
        oneOf(resolvePhysicsProfile(stored.physicsProfile), PHYSICS_PROFILE_IDS, physicsProfile)
      );
      setBallSkin(oneOf(stored.ballSkin, ["classic", "plasma", "prism"], ballSkin));
      setPlayerBallColor(oneOf(stored.playerBallColor, Object.keys(BALL_COLORWAYS), playerBallColor));
      setOpponentBallColor(oneOf(stored.opponentBallColor, Object.keys(BALL_COLORWAYS), opponentBallColor));
      setCourtSkin(oneOf(stored.courtSkin, ["neon", "carbon", "sunset"], courtSkin));
      setDebugArcOverlay(!!stored.debugArcOverlay);
      setAutoArcCalibrate(
        typeof stored.autoArcCalibrate === "boolean" ? stored.autoArcCalibrate : true
      );
      if (Number.isFinite(stored.aimSensitivityScale)) {
        setAimSensitivityScale(clamp(stored.aimSensitivityScale, 0.7, 1.4));
      }
      if (Number.isFinite(stored.arcHeightTuning)) {
        setArcHeightTuning(clampArcHeightTune(stored.arcHeightTuning));
      }
      if (Number.isFinite(stored.lateralForgiveness)) {
        setLateralForgiveness(clampLateralForgiveness(stored.lateralForgiveness));
      }
      if (Number.isFinite(stored.releaseForgiveness)) {
        setReleaseForgiveness(clampReleaseForgiveness(stored.releaseForgiveness));
      }
      if (Number.isFinite(stored.bestScore)) setBestScore(Math.max(0, Math.round(stored.bestScore)));
      if (Number.isFinite(stored.ladderPoints)) setLadderPoints(Math.max(0, Math.round(stored.ladderPoints)));
      if (stored.achievements && typeof stored.achievements === "object") {
        setAchievements({
          releaseArtist: !!stored.achievements.releaseArtist,
          cleanShooter: !!stored.achievements.cleanShooter,
          clutchFinisher: !!stored.achievements.clutchFinisher,
        });
      }
    } else {
      setControlProfile(smartDefaults.controlProfile);
      setShotTuningPreset(smartDefaults.shotTuningPreset);
      setAimMode(smartDefaults.aimMode);
      setPhysicsProfile(resolvePhysicsProfile(smartDefaults.physicsProfile));
      setVisualTuningPreset(smartDefaults.visualTuningPreset);
      setEasyPlusAssist(!!smartDefaults.easyPlusAssist);
      setAimSensitivityScale(clamp(smartDefaults.aimSensitivityScale, 0.7, 1.4));
      setArcHeightTuning(SMOOTH_EASY_TUNE.arcHeightTuning);
      setLateralForgiveness(SMOOTH_EASY_TUNE.lateralForgiveness);
      setReleaseForgiveness(SMOOTH_EASY_TUNE.releaseForgiveness);
    }
    settingsHydratedRef.current = true;
  }, [
    aimMode,
    ballSkin,
    controlProfile,
    courtSkin,
    debugArcOverlay,
    autoArcCalibrate,
    dominantHand,
    easyPlusAssist,
    arcHeightTuning,
    lateralForgiveness,
    releaseForgiveness,
    shotMechanic,
    opponentBallColor,
    physicsProfile,
    playerBallColor,
    shotDifficulty,
    shotTuningPreset,
    visualTuningPreset,
  ]);

  useEffect(() => {
    if (!settingsHydratedRef.current) return;
    saveStoredSettings({
      vrMode,
      oneViewMode,
      practiceMode,
      movingRims,
      dailyMode,
      shotDifficulty,
      shotTuningPreset,
      visualTuningPreset,
      easyPlusAssist,
      aimMode,
      dominantHand,
      controlProfile,
      shotMechanic,
      debugArcOverlay,
      autoArcCalibrate,
      physicsProfile,
      aimSensitivityScale,
      arcHeightTuning,
      lateralForgiveness,
      releaseForgiveness,
      ballSkin,
      playerBallColor,
      opponentBallColor,
      courtSkin,
      bestScore,
      ladderPoints,
      achievements,
    });
  }, [
    achievements,
    aimMode,
    aimSensitivityScale,
    arcHeightTuning,
    ballSkin,
    bestScore,
    controlProfile,
    courtSkin,
    debugArcOverlay,
    autoArcCalibrate,
    dailyMode,
    dominantHand,
    easyPlusAssist,
    lateralForgiveness,
    ladderPoints,
    movingRims,
    oneViewMode,
    opponentBallColor,
    physicsProfile,
    playerBallColor,
    practiceMode,
    releaseForgiveness,
    shotMechanic,
    shotDifficulty,
    shotTuningPreset,
    visualTuningPreset,
    vrMode,
  ]);

  const refreshBackendPanels = useCallback(async () => {
    const useOfflinePanels = () => {
      setBackendLeaderboard(
        buildOfflineLeaderboard({
          playerName,
          bestScore,
          ladderPoints,
        })
      );
      setRecentMatches(
        buildOfflineRecentMatches({
          playerName,
          lastScore: bestScore,
        })
      );
      setBackendStatus("offline");
    };
    if (!BACKEND_ENABLED) {
      useOfflinePanels();
      return;
    }
    const refreshSeq = backendRefreshSeqRef.current + 1;
    backendRefreshSeqRef.current = refreshSeq;
    const now = Date.now();
    if (now < backendRetryAtRef.current) {
      if (refreshSeq !== backendRefreshSeqRef.current) return;
      useOfflinePanels();
      return;
    }
    try {
      await getBackendHealth();
      const [board, matches] = await Promise.all([getLeaderboard(8), getRecentMatches(6)]);
      if (refreshSeq !== backendRefreshSeqRef.current) return;
      backendRetryAtRef.current = 0;
      setBackendLeaderboard(toArray(board));
      setRecentMatches(toArray(matches));
      setBackendStatus("online");
    } catch (_) {
      if (refreshSeq !== backendRefreshSeqRef.current) return;
      backendRetryAtRef.current = Date.now() + BACKEND_RETRY_COOLDOWN_MS;
      useOfflinePanels();
    }
  }, [bestScore, ladderPoints, playerName]);

  useEffect(() => {
    if (screen !== "menu") return;
    let canceled = false;
    const run = async () => {
      if (canceled) return;
      await refreshBackendPanels();
    };
    run();
    const interval = setInterval(run, 25000);
    return () => {
      canceled = true;
      backendRefreshSeqRef.current += 1;
      clearInterval(interval);
    };
  }, [refreshBackendPanels, screen]);

  useEffect(() => {
    const unlock = () => ensureAudioReady();
    window.addEventListener("pointerdown", unlock, { passive: true });
    window.addEventListener("keydown", unlock);
    return () => {
      window.removeEventListener("pointerdown", unlock);
      window.removeEventListener("keydown", unlock);
    };
  }, [ensureAudioReady]);

  useEffect(() => {
    const refreshInputCalibration = () => {
      const cal = detectRuntimeInputCalibration();
      inputCalRef.current = cal;
      pointerRef.current.inputCal = cal;
      setViewport(detectViewportMetrics());
    };
    refreshInputCalibration();
    window.addEventListener("resize", refreshInputCalibration);
    window.addEventListener("orientationchange", refreshInputCalibration);
    return () => {
      window.removeEventListener("resize", refreshInputCalibration);
      window.removeEventListener("orientationchange", refreshInputCalibration);
    };
  }, []);

  const syncHud = useCallback(() => {
    const [player, opponent] = lanesRef.current;
    if (!player) return;
    setHud({
      playerScore: player.score,
      opponentScore: opponent?.score || 0,
      playerStreak: player.streak,
      opponentStreak: opponent?.streak || 0,
      playerFire: player.rimFire,
      opponentFire: opponent?.rimFire || false,
      playerStyle: player.aiStyle || "",
      opponentStyle: opponent?.aiStyle || "",
    });
  }, []);

  const applyShotFeelToActiveLanes = useCallback((nextArc, nextLateral, nextRelease) => {
    const safeArc = clampArcHeightTune(nextArc);
    const safeLateral = clampLateralForgiveness(nextLateral);
    const safeRelease = clampReleaseForgiveness(nextRelease);
    lanesRef.current.forEach((lane) => {
      if (!lane) return;
      lane.arcHeightTuning = safeArc;
      lane.lateralForgiveness = safeLateral;
      lane.releaseForgiveness = safeRelease;
    });
  }, []);

  const initializeMatch = useCallback((modifierOverride = null, setupOverride = null) => {
    const setup = setupOverride || sanitizeGameSetup({
      shotDifficulty,
      aimMode,
      dominantHand,
      controlProfile,
      physicsProfile,
      ballSkin,
      playerBallColor,
      opponentBallColor,
      arcHeightTuning,
      lateralForgiveness,
      releaseForgiveness,
    });
    const safeShotDifficulty = setup.shotDifficulty;
    const safeAimMode = setup.aimMode;
    const safeDominantHand = setup.dominantHand;
    const safeControlProfile = setup.controlProfile;
    const safeBallSkin = setup.ballSkin;
    const safePlayerBallColor = setup.playerBallColor;
    const safeOpponentBallColor = setup.opponentBallColor;
    const effectiveEasyPlusAssist = safeShotDifficulty === "easy" ? true : easyPlusAssist;
    const effectiveAimSensitivity =
      safeShotDifficulty === "easy" ? Math.min(aimSensitivityScale, 1) : aimSensitivityScale;
    const activeDailyModifier = dailyMode && !practiceMode ? modifierOverride || dailyModifier : "none";
    const singleLaneSession = vrMode || oneViewMode || practiceMode;
    const playerHotZoneX = 292;
    const opponentHotZoneX = 988;
    const player = createLane({
      id: "player",
      label: "PLAYER",
      centerX: 340,
      side: -1,
      isAI: false,
      accent: "#36e7ff",
      avatar: "🧢",
      rank: "Rising Pro",
      ballSkin: safeBallSkin,
      ballColor: safePlayerBallColor,
      courtSkin,
      aiStyle: "player",
      shotDifficulty: safeShotDifficulty,
      aimMode: safeAimMode,
      controlProfile: safeControlProfile,
      dominantHand: safeDominantHand,
      aimSensitivityScale: effectiveAimSensitivity,
      easyPlusAssist: effectiveEasyPlusAssist,
      arcHeightTuning: clampArcHeightTune(arcHeightTuning),
      lateralForgiveness: clampLateralForgiveness(lateralForgiveness),
      releaseForgiveness: clampReleaseForgiveness(releaseForgiveness),
    });
    player.dailyModifier = activeDailyModifier;
    player.hotZoneX = playerHotZoneX;
    player.hotZoneR = 62;
    if (activeDailyModifier === "small_rim") player.hoop.r *= 0.94;
    if (activeDailyModifier === "quick_return") player.quickReturnMul = 1.5;
    if (activeDailyModifier === "wind") player.windForce = rand(-78, 78);
    if (singleLaneSession) {
      player.centerX = CW * 0.5;
      player.hoop = {
        x: CW * 0.5,
        y: vrMode ? HOOP_BASE_Y - 14 : HOOP_BASE_Y,
        r: DEFAULT_HOOP_RADIUS + (vrMode ? 6 : 2),
      };
      player.home = { x: CW * 0.5 - (vrMode ? 0 : 108), y: vrMode ? CH - 110 : 620 };
      player.ball.x = player.home.x;
      player.ball.y = player.home.y;
      player.side = -1;
      player.label = vrMode ? "VR PLAYER" : practiceMode ? "PRACTICE" : "SOLO";
      player.avatar = vrMode ? "🖐️" : practiceMode ? "🎯" : "🧢";
      player.rank = vrMode ? "Shooter" : practiceMode ? "Open Gym" : "Solo Run";
      player.hotZoneX = CW * 0.5;
      lanesRef.current = [player];
    } else {
      const aiStyle = randomFrom(AI_STYLES);
      const opponent = createLane({
        id: "opponent",
        label: "OPPONENT",
        centerX: 940,
        side: 1,
        isAI: true,
        accent: "#ff7a52",
        avatar: "🤖",
        rank: "Street Bot",
        ballSkin: "classic",
        ballColor: safeOpponentBallColor,
        courtSkin,
        aiStyle,
        shotDifficulty: safeShotDifficulty,
        aimMode: safeAimMode,
        controlProfile: safeControlProfile,
        dominantHand: "right",
        aimSensitivityScale: 1,
        easyPlusAssist: false,
        arcHeightTuning: clampArcHeightTune(arcHeightTuning),
        lateralForgiveness: clampLateralForgiveness(lateralForgiveness),
        releaseForgiveness: clampReleaseForgiveness(releaseForgiveness),
      });
      opponent.dailyModifier = activeDailyModifier;
      opponent.hotZoneX = opponentHotZoneX;
      opponent.hotZoneR = 62;
      if (activeDailyModifier === "small_rim") opponent.hoop.r *= 0.94;
      if (activeDailyModifier === "quick_return") opponent.quickReturnMul = 1.5;
      if (activeDailyModifier === "wind") opponent.windForce = rand(-78, 78);
      lanesRef.current = [player, opponent];
    }
    particlesRef.current = [];
    fxRef.current.zoom = 1;
    fxRef.current.tilt = 0;
    fxRef.current.followRoll = 0;
    fxRef.current.followPitch = 0;
    fxRef.current.followDriftX = 0;
    fxRef.current.followDriftY = 0;
    fxRef.current.shotRush = 0;
    fxRef.current.shake = 0;
    fxRef.current.chargeGlow = 0;
    fxRef.current.camX = 0;
    fxRef.current.camY = 0;
    fxRef.current.clutchPulse = 0;
    const initQualityFloor = clamp(
      Number.isFinite(VISUAL_TUNE.qualityFloor) ? VISUAL_TUNE.qualityFloor : 0.48,
      0.35,
      0.9
    );
    const initQualityCeil = clamp(
      Number.isFinite(VISUAL_TUNE.qualityCeil) ? VISUAL_TUNE.qualityCeil : 1,
      initQualityFloor,
      1
    );
    fxRef.current.quality = clamp(0.92, initQualityFloor, initQualityCeil);
    fxRef.current.heavyFrames = 0;
    fxRef.current.lowLatencyBoost = 0;
    fxRef.current.slowMoTime = 0;
    syncHud();
  }, [
    ballSkin,
    playerBallColor,
    opponentBallColor,
    arcHeightTuning,
    lateralForgiveness,
    releaseForgiveness,
    courtSkin,
    dailyMode,
    dailyModifier,
    oneViewMode,
    practiceMode,
    aimMode,
    dominantHand,
    aimSensitivityScale,
    easyPlusAssist,
    controlProfile,
    shotDifficulty,
    syncHud,
    vrMode,
  ]);

  const startCharging = useCallback((lane) => {
    if (!lane || lane.state !== "idle") return;
    const tune = DIFFICULTY_TUNING[lane.shotDifficulty] || DIFFICULTY_TUNING.medium;
    const aimTune = AIM_MODES[lane.aimMode] || AIM_MODES.casual;
    const controlTune = CONTROL_PROFILES[lane.controlProfile] || CONTROL_PROFILES.smooth;
    const easyPlusActive = lane.shotDifficulty === "easy" && !lane.isAI && lane.easyPlusAssist;
    const easyPlusTune = easyPlusActive ? EASY_PLUS_ASSIST_TUNING : null;
    const timingTune = TIMING_DIFFICULTY_TUNING[lane.shotDifficulty] || TIMING_DIFFICULTY_TUNING.medium;
    const releaseForgiveness = clampReleaseForgiveness(lane.releaseForgiveness ?? 1);
    lane.state = "charging";
    lane.powerLinear = clamp(lane.powerLinear, getDragPowerMin(), getDragPowerMax());
    lane.chargeStartLinear = lane.powerLinear;
    lane.power = powerCurve(lane.powerLinear);
    lane.previewAlpha = 1;
    lane.previewTTL = 0.3;
    lane.releaseType = "normal";
    lane.releaseAssist = 0;
    lane.followWindow = 0;
    lane.followSpinBoost = 0;
    lane.followArcAdjust = 0;
    lane.manualPull = 0;
    lane.preShotLateral = 0;
    lane.dragAimX = 0;
    lane.dragAimY = 1;
    lane.dragDist = 0;
    lane.lastArcClearPx = 0;
    lane.lastArcApexY = 0;
    lane.lastArcRimPlaneY = 0;
    lane.chargeElapsed = 0;
    lane.timingCharge = clamp(lane.power, 0, 1);
    lane.timingAccuracy = 0;
    lane.timingWobble = 0;
    lane.forceMake = false;
    lane.forceSwish = false;
    lane.bezierFlightTime = 0.82;
    lane.clearedRimPlane = false;
    lane.releaseSmooth = 0.5;
    lane.flickStrength = 0;
    lane.wobbleAmp = 0;
    lane.previewPath = null;
    lane.previewCalcCooldown = 0;
    lane.previewKey = "";
    lane.swipePixels = 0;
    lane.swipeMs = 0;
    lane.sloppySwipe = 0;
    lane.landingPulse = 0;
    lane.wasPerfectWindow = false;
    lane.aimLockCooldown = 0;
    lane.hotZoneBoost = false;
    lane.clutchBonusActive = false;
    lane.outcomeTier = "charge";
    lane.hitBackboard = false;
    lane.boardHitCooldown = 0;
    lane.rimHitCooldown = 0;
    lane.rimTouched = false;
    lane.rimHitType = "";
    lane.telemetryAttemptLogged = false;
    lane.telemetryOutcomeLogged = false;
    lane.aimX = lane.aimX || lane.centerX;
    lane.aimY = lane.aimY || CH * 0.75;
    if (TIMING_ONLY_SHOTS && !lane.isAI) {
      lane.releaseWindow = { center: 0.5, width: timingTune.windowWidth };
    } else {
      const centerJitter = (tune.centerJitter ?? 0.05) * (easyPlusTune?.centerJitterMul ?? 1);
      lane.releaseWindow = {
        center: clamp(0.69 + rand(-centerJitter, centerJitter) + (lane.streak >= 2 ? 0.01 : 0), 0.58, 0.82),
        width: clamp(
          (0.14 - lane.streak * 0.008 + rand(-0.015, 0.01)) *
            tune.releaseWidthMul *
            (easyPlusTune?.releaseWidthMul ?? 1) *
            aimTune.releaseWidthMul *
            controlTune.windowMul *
            releaseForgiveness *
            (1 + lane.dynamicAssist * 0.5),
          0.1,
          0.36
        ),
      };
    }
  }, []);

  const recordShotAttempt = useCallback((lane) => {
    if (!lane || lane.id !== "player") return;
    if (lane.telemetryAttemptLogged) return;
    lane.telemetryAttemptLogged = true;
    lane.telemetryOutcomeLogged = false;
    const difficulty = SHOT_DIFFICULTY_IDS.includes(lane.shotDifficulty) ? lane.shotDifficulty : "medium";
    setShotTelemetry((prev) => {
      const current = prev && typeof prev === "object" ? prev : createEmptyShotTelemetry();
      const bucket = current[difficulty] || createEmptyDifficultyTelemetry();
      return {
        ...current,
        [difficulty]: {
          ...bucket,
          attempts: Math.max(0, Math.round(Number(bucket.attempts) || 0) + 1),
        },
        updatedAt: Date.now(),
      };
    });
  }, []);

  const runAutoArcCalibration = useCallback(
    (lane, outcome) => {
      if (!lane || lane.id !== "player" || !autoArcCalibrateRef.current) return;
      const difficulty = SHOT_DIFFICULTY_IDS.includes(lane.shotDifficulty) ? lane.shotDifficulty : "medium";
      const isMake = outcome === "swish" || outcome === "make" || outcome === "rim_in";
      const state = autoArcCalRef.current;
      if (!state[difficulty]) state[difficulty] = createArcCalBucket();
      const bucket = state[difficulty];
      const clearance = Number.isFinite(lane.lastArcClearPx) ? lane.lastArcClearPx : 0;
      bucket.shots += 1;
      bucket.clearanceSum += clearance;
      if (clearance < AUTO_ARC_CLEAR_MIN) bucket.low += 1;
      if (clearance > AUTO_ARC_CLEAR_MAX) bucket.high += 1;
      if (isMake) bucket.makes += 1;

      setAutoArcCalProgress((prev) => ({
        ...prev,
        [difficulty]: Math.min(bucket.shots, AUTO_ARC_CALIBRATION_INTERVAL),
      }));
      if (bucket.shots < AUTO_ARC_CALIBRATION_INTERVAL) {
        setAutoArcCalStatus(
          `Auto arc calibration collecting ${difficulty.toUpperCase()} ${bucket.shots}/${AUTO_ARC_CALIBRATION_INTERVAL}.`
        );
        return;
      }

      const avgClear = bucket.clearanceSum / Math.max(1, bucket.shots);
      const lowRate = bucket.low / Math.max(1, bucket.shots);
      const highRate = bucket.high / Math.max(1, bucket.shots);
      const makeRate = bucket.makes / Math.max(1, bucket.shots);
      let nextArc = arcHeightTuningRef.current;
      let nextLateral = lateralForgivenessRef.current;
      let nextRelease = releaseForgivenessRef.current;

      if (avgClear < AUTO_ARC_CLEAR_MIN || lowRate > 0.28) {
        nextArc += clamp((AUTO_ARC_CLEAR_MIN - avgClear) / 160, 0.015, 0.055);
      } else if (avgClear > AUTO_ARC_CLEAR_MAX || highRate > 0.42) {
        nextArc -= clamp((avgClear - AUTO_ARC_CLEAR_MAX) / 180, 0.012, 0.045);
      }

      if (makeRate < 0.45) {
        nextLateral += 0.03;
        nextRelease += 0.03;
      } else if (makeRate > 0.76 && lowRate < 0.12 && highRate < 0.25) {
        nextLateral -= 0.02;
        nextRelease -= 0.02;
      }

      nextArc = clampArcHeightTune(nextArc);
      nextLateral = clampLateralForgiveness(nextLateral);
      nextRelease = clampReleaseForgiveness(nextRelease);

      const tuned =
        Math.abs(nextArc - arcHeightTuningRef.current) > 0.001 ||
        Math.abs(nextLateral - lateralForgivenessRef.current) > 0.001 ||
        Math.abs(nextRelease - releaseForgivenessRef.current) > 0.001;

      if (tuned) {
        arcHeightTuningRef.current = nextArc;
        lateralForgivenessRef.current = nextLateral;
        releaseForgivenessRef.current = nextRelease;
        setArcHeightTuning(nextArc);
        setLateralForgiveness(nextLateral);
        setReleaseForgiveness(nextRelease);
        applyShotFeelToActiveLanes(nextArc, nextLateral, nextRelease);
        setAutoArcCalStatus(
          `Auto arc calibration tuned ${difficulty.toUpperCase()} · ARC ${Math.round(nextArc * 100)}% · LAT ${Math.round(
            nextLateral * 100
          )}% · REL ${Math.round(nextRelease * 100)}%.`
        );
      } else {
        setAutoArcCalStatus(
          `Auto arc calibration stable ${difficulty.toUpperCase()} · clear ${Math.round(avgClear)}px · make ${Math.round(
            makeRate * 100
          )}%.`
        );
      }

      state[difficulty] = createArcCalBucket();
      setAutoArcCalProgress((prev) => ({ ...prev, [difficulty]: 0 }));
    },
    [applyShotFeelToActiveLanes]
  );

  const recordShotOutcome = useCallback((lane, outcome) => {
    if (!lane || lane.id !== "player") return;
    if (lane.telemetryOutcomeLogged) return;
    lane.telemetryOutcomeLogged = true;
    const difficulty = SHOT_DIFFICULTY_IDS.includes(lane.shotDifficulty) ? lane.shotDifficulty : "medium";
    setShotTelemetry((prev) => {
      const current = prev && typeof prev === "object" ? prev : createEmptyShotTelemetry();
      const bucket = current[difficulty] || createEmptyDifficultyTelemetry();
      const next = {
        attempts: Math.max(0, Math.round(Number(bucket.attempts) || 0)),
        makes: Math.max(0, Math.round(Number(bucket.makes) || 0)),
        swishes: Math.max(0, Math.round(Number(bucket.swishes) || 0)),
        rimIns: Math.max(0, Math.round(Number(bucket.rimIns) || 0)),
        rimOuts: Math.max(0, Math.round(Number(bucket.rimOuts) || 0)),
        misses: Math.max(0, Math.round(Number(bucket.misses) || 0)),
      };
      if (outcome === "swish") {
        next.makes += 1;
        next.swishes += 1;
      } else if (outcome === "make") {
        next.makes += 1;
      } else if (outcome === "rim_in") {
        next.makes += 1;
        next.rimIns += 1;
      } else if (outcome === "rim_out") {
        next.rimOuts += 1;
        next.misses += 1;
      } else {
        next.misses += 1;
      }
      return {
        ...current,
        [difficulty]: next,
        updatedAt: Date.now(),
      };
    });
    runAutoArcCalibration(lane, outcome);
  }, [runAutoArcCalibration]);

  const resetShotTelemetry = useCallback(() => {
    setShotTelemetry(createEmptyShotTelemetry());
  }, []);

  const registerMiss = useCallback((lane) => {
    lane.streak = 0;
    lane.misses += 1;
    lane.comboPop = 0;
    lane.stats.misses += 1;
    const controlTune = CONTROL_PROFILES[lane.controlProfile] || CONTROL_PROFILES.smooth;
    const gain =
      (lane.aimMode === "casual" ? DYNAMIC_ASSIST_GAIN : DYNAMIC_ASSIST_GAIN * 0.45) *
      controlTune.dynamicGainMul;
    lane.dynamicAssist = clamp(lane.dynamicAssist + gain, 0, DYNAMIC_ASSIST_MAX);
    if (!lane.isAI && lane.misses >= 2) {
      lane.dynamicAssist = clamp(lane.dynamicAssist + gain * 0.75, 0, DYNAMIC_ASSIST_MAX);
    }
    if (lane.misses >= 2 && lane.rimFire) {
      lane.rimFire = false;
      SFX.fireOff();
    }
  }, []);

  const toReturning = useCallback((lane) => {
    lane.state = "returning";
    lane.returnTime = 0.14 / (lane.quickReturnMul || 1);
    lane.crossChecked = true;
    lane.previewTTL = Math.max(lane.previewTTL, 0.2);
    lane.settleTimer = 0;
    lane.recycleOut = !!lane.shotResolved;
    lane.recycleTimer = lane.recycleOut ? 0.18 : 0;
  }, []);

  const releaseShot = useCallback(
    (lane) => {
      if (!lane || lane.state !== "charging") return;
      const nowMs = performance.now();
      if (nowMs < (lane.nextReleaseAt || 0)) return;
      lane.nextReleaseAt = nowMs + (TIMING_ONLY_SHOTS && !lane.isAI ? TIMING_RELEASE_COOLDOWN_MS : ShotTuning.releaseCooldownMs);

      const pRef = pointerRef.current;
      if (
        !lane.isAI &&
        !TIMING_ONLY_SHOTS &&
        pRef.pointerType === "touch" &&
        lane.swipePixels > 0 &&
        lane.swipePixels < SWIPE_DEAD_ZONE_PX &&
        lane.powerLinear < getDragPowerMin() + 0.04
      ) {
        lane.previewTTL = 0;
        lane.previewAlpha = 0;
        lane.state = "idle";
        return;
      }
      const tune = DIFFICULTY_TUNING[lane.shotDifficulty] || DIFFICULTY_TUNING.medium;
      const aimTune = AIM_MODES[lane.aimMode] || AIM_MODES.casual;
      const controlTune = CONTROL_PROFILES[lane.controlProfile] || CONTROL_PROFILES.smooth;
      const releaseForgiveness = clampReleaseForgiveness(lane.releaseForgiveness ?? 1);
      const lateralForgiveness = clampLateralForgiveness(lane.lateralForgiveness ?? 1);
      const easyPlusActive = lane.shotDifficulty === "easy" && !lane.isAI && lane.easyPlusAssist;
      const easyPlusTune = easyPlusActive ? EASY_PLUS_ASSIST_TUNING : null;
      const casual = lane.aimMode === "casual";
      const releaseCenter = lane.releaseWindow?.center ?? 0.7;
      const releaseWidth = lane.releaseWindow?.width ?? 0.14;
      const timingCharge = clamp(
        TIMING_ONLY_SHOTS && !lane.isAI ? lane.timingCharge ?? lane.power : lane.power,
        0,
        1
      );
      const baseTiming = getReleaseTimingData(timingCharge, releaseCenter, releaseWidth);
      const assistedTiming = computeReleaseAssist({
        timingOffset: baseTiming.timingOffset,
        releaseType: baseTiming.releaseType,
        misses: lane.misses || 0,
        controlProfile: lane.controlProfile,
        aimMode: lane.aimMode,
        shotDifficulty: lane.shotDifficulty,
        isAI: lane.isAI,
      });
      const timingOffset = assistedTiming.timingOffset;
      const timingAbs = Math.abs(timingOffset) / releaseForgiveness;
      const releaseType = assistedTiming.releaseType;
      lane.releaseAssist = assistedTiming.assistStrength;
      const timingPenaltyMul =
        (casual ? 0.56 : 1) *
        controlTune.timingPenaltyMul *
        (tune.timingPenaltyMul ?? 1) *
        (easyPlusTune?.timingPenaltyMul ?? 1);
      lane.releaseType = releaseType;
      lane.stats.attempts += 1;
      lane.telemetryOutcomeLogged = false;
      recordShotAttempt(lane);
      if (releaseType === "perfect") lane.stats.perfectReleases += 1;
      else if (releaseType === "early") lane.stats.earlyReleases += 1;
      else lane.stats.lateReleases += 1;

      let flickStrength = 0;
      let flickVelocity = 0;
      let flickVelocityX = 0;
      let timingForceMake = false;
      let timingForceSwish = false;
      const timingTune = TIMING_DIFFICULTY_TUNING[lane.shotDifficulty] || TIMING_DIFFICULTY_TUNING.medium;
      if (!lane.isAI) {
        const history = pRef.history;
        if (history.length >= 2) {
          const last = history[history.length - 1];
          let first = history[0];
          for (let i = history.length - 1; i >= 0; i--) {
            if (last.t - history[i].t > 90) {
              first = history[i];
              break;
            }
          }
          const dt = Math.max(0.012, (last.t - first.t) / 1000);
          const vy = (last.y - first.y) / dt;
          const vx = (last.x - first.x) / dt;
          flickVelocity = vy;
          flickVelocityX = vx;
          flickStrength = clamp((-vy - 180) / 1200, 0, 1);
        } else if (casual) {
          flickStrength = Math.max(0.38, controlTune.minFlick);
        }
      } else {
        flickStrength = rand(Math.max(0.28, controlTune.minFlick * 0.84), 0.72);
      }
      if (casual) flickStrength = Math.max(flickStrength, controlTune.minFlick * 0.92);
      lane.flickStrength = flickStrength;
      lane.flickVelocity = flickVelocity;
      lane.sideSpin = clamp(flickVelocityX / 1300, -1, 1);

      const angleScore = clamp(1 - Math.abs(lane.angle - 53) / 18, 0, 1);
      const powerScore = clamp(1 - Math.abs(timingCharge - releaseCenter) / Math.max(0.06, releaseWidth * 1.1), 0, 1);
      const timingScore =
        releaseType === "perfect"
          ? 1
          : releaseType === "early"
            ? clamp((casual ? 0.86 : 0.78) - timingAbs * (casual ? 0.11 : 0.16) * timingPenaltyMul, 0.28, 0.84)
            : clamp((casual ? 0.82 : 0.72) - timingAbs * (casual ? 0.1 : 0.14) * timingPenaltyMul, 0.28, 0.82);
      const flickScore = clamp(0.42 + flickStrength * 0.58, 0, 1);
      const stability =
        angleScore * 0.34 + powerScore * 0.33 + timingScore * 0.21 + flickScore * 0.12;
      const idealPower = tune.idealPower;
      const idealAngle = tune.idealAngle;
      lane.shotStability = clamp(stability, 0.05, 1);
      if (casual) lane.shotStability = clamp(lane.shotStability + 0.08, 0.05, 1);
      if (TIMING_ONLY_SHOTS && !lane.isAI) {
        lane.timingCharge = timingCharge;
        lane.timingAccuracy = clamp(1 - Math.abs(timingCharge - 0.5) * 0.45, 0, 1);
        const assistRatio = clamp(
          (lane.dynamicAssist || 0) / Math.max(0.001, DYNAMIC_ASSIST_MAX),
          0,
          1
        );
        const wobbleDampen = 1 - assistRatio * 0.72;
        lane.timingWobble =
          (Math.random() - 0.5) *
          lane.hoop.r *
          2 *
          0.085 *
          (1 - lane.timingAccuracy) *
          timingTune.wobbleMul *
          wobbleDampen;
        lane.bezierFlightTime = clamp(lerp(1.16, 1.02, timingCharge), 1, 1.36);
        const easyMercy =
          lane.shotDifficulty === "easy" && (timingAbs < 1.7 || assistRatio > 0.16);
        const mediumMercy =
          lane.shotDifficulty === "medium" && assistRatio > 0.32 && timingAbs < 1.25;
        timingForceMake =
          lane.timingAccuracy >= timingTune.forceMakeAccuracyMin ||
          Math.abs(lane.timingWobble) < lane.hoop.r * 0.9 * timingTune.forgivingWindowMul ||
          easyMercy ||
          mediumMercy;
        timingForceSwish =
          lane.timingAccuracy >= timingTune.forceSwishAccuracyMin ||
          releaseType === "perfect" ||
          (lane.shotDifficulty === "easy" && timingAbs < 0.55 && lane.timingAccuracy > 0.45);
      } else {
        timingForceMake = false;
        timingForceSwish = false;
      }
      lane.stats.totalStability += lane.shotStability;
      lane.stats.totalAngleError += Math.abs(lane.angle - idealAngle);
      lane.aimAssist =
        releaseType === "perfect"
          ? 0.36 + lane.shotStability * 0.34
          : 0.08 + lane.shotStability * 0.2;
      if (casual) lane.aimAssist += 0.12;
      if (easyPlusActive) lane.aimAssist += easyPlusTune.assistAdd;
      lane.aimAssist += controlTune.assistBoost;
      lane.aimAssist += lane.dynamicAssist;
      lane.aimAssist = clamp((lane.aimAssist + tune.assistBias) * aimTune.assistMul, 0.02, 0.96);
      lane.aimAssist = clamp(lane.aimAssist * (1 + (lateralForgiveness - 1) * 0.45), 0.02, 0.98);
      lane.aimAssist *= getAimAssistGate(lane, lane.ball.x, lane.ball.y, hoopX(lane), getRimCenterY(lane));

      lane.state = "flying";
      lane.launchX = lane.ball.x;
      lane.launchY = lane.ball.y;
      lane.releaseBurst = 1.12;
      lane.releaseFlash = releaseType === "perfect" ? 1 : 0.45;
      lane.releaseCue = releaseType === "perfect" ? 1 : 0.7;
      lane.releaseSpark = releaseType === "perfect" ? 1 : 0.55;
      lane.wasPerfectWindow = false;
      lane.wristSnap = 1.22;
      lane.guideRelease = 1.15;
      lane.followHold = 0.48;
      lane.crossChecked = false;
      lane.clearedRimPlane = false;
      lane.shotResolved = false;
      lane.madeShot = false;
      lane.forceMake = timingForceMake;
      lane.forceSwish = timingForceSwish;
      lane.hitBackboard = false;
      lane.boardHitCooldown = 0;
      lane.floorBounces = 0;
      lane.settleTimer = 0;
      lane.recycleOut = false;
      lane.recycleTimer = 0;
      lane.shotStep = 0;
      lane.prevX = lane.ball.x;
      lane.prevY = lane.ball.y;
      lane.prevVx = lane.ball.vx;
      lane.prevVy = lane.ball.vy;
      lane.previewTTL = 0.12;
      lane.previewAlpha = Math.max(lane.previewAlpha, 0.55);
      lane.previewPath = null;
      lane.previewCalcCooldown = 0;
      lane.previewKey = "";
      lane.followWindow = FOLLOW_THROUGH_WINDOW;
      lane.followSpinBoost = 0;
      lane.followArcAdjust = 0;
      lane.backspinBias = clamp(flickStrength * 0.45 + (lane.aimMode === "casual" ? 0.08 : 0), 0, 0.5);
      lane.releaseSmooth = clamp(
        0.18 +
          lane.shotStability * 0.72 +
          (releaseType === "perfect" ? 0.24 : 0) -
          (releaseType === "early" || releaseType === "late" ? 0.14 : 0),
        0.05,
        1.2
      );
      lane.releaseSmooth *= lane.aimMode === "casual" ? 1.36 : 1;
      lane.releaseSmooth *= controlTune.releaseSmoothMul;
      lane.wobbleAmp = clamp((1 - lane.shotStability) * (releaseType === "perfect" ? 1.6 : 2.8), 0, 2.8);
      lane.wobblePhase = rand(0, Math.PI * 2);
      const releasePreview = predictShotPath(lane);
      const releaseArcMetrics = computePreviewArcMetrics(lane, releasePreview);
      if (releaseArcMetrics) {
        lane.lastArcClearPx = releaseArcMetrics.clearancePx;
        lane.lastArcApexY = releaseArcMetrics.apexY;
        lane.lastArcRimPlaneY = releaseArcMetrics.rimPlaneY;
      } else {
        lane.lastArcClearPx = 0;
        lane.lastArcApexY = 0;
        lane.lastArcRimPlaneY = 0;
      }
      lane.ball.vx = releasePreview.vx;
      lane.ball.vy = releasePreview.vy;
      if (TIMING_ONLY_SHOTS && !lane.isAI) {
        const lateralLiftBoost =
          clamp(Math.abs(lane.preShotLateral || 0) / TIMING_LATERAL_MOVE_RANGE, 0, 1) * 8;
        const riseBoost = lerp(
          28,
          28 +
            timingTune.arcLift +
            (TIMING_RIM_CLEARANCE_BONUS + SHOT_FEEL_CONFIG.physics.arcClearanceBonusPx) * 0.45 +
            lateralLiftBoost,
          lane.timingAccuracy || 0.5
        );
        lane.ball.vy -= riseBoost;
      }
      const microVariance = TIMING_ONLY_SHOTS && !lane.isAI ? 0 : clamp((1 - lane.shotStability) * 10, 0.5, 8);
      if (microVariance > 0) {
        lane.ball.vx += rand(-microVariance, microVariance);
        lane.ball.vy += rand(-microVariance * 0.7, microVariance * 0.7);
      }
      if (lane.isAI) {
        lane.ball.vx += rand(-10, 10);
        lane.ball.vy += rand(-12, 12);
      }
      lane.prevVx = lane.ball.vx;
      lane.prevVy = lane.ball.vy;
      const releaseSpinBase = 0.32 + flickStrength * 0.86;
      const releaseSpinSide = lane.sideSpin * 0.58 + clamp(lane.manualPull, -1, 1) * 0.22;
      lane.ball.spin = wrapAngle((lane.ball.spin || 0) + releaseSpinBase + releaseSpinSide);

      const releaseEnergy = clamp(0.35 + lane.power * 0.62 + flickStrength * 0.42, 0.2, 1.4);
      const releaseSide = lane.side === -1 ? 1 : -1;
      fxRef.current.followRoll += releaseSide * (0.012 + releaseEnergy * 0.018);
      fxRef.current.followPitch += 0.012 + releaseEnergy * 0.02;
      fxRef.current.followDriftX += releaseSide * (5 + releaseEnergy * 10);
      fxRef.current.followDriftY += -(6 + releaseEnergy * 8);
      fxRef.current.shotRush = Math.max(fxRef.current.shotRush, 0.46 + releaseEnergy * 0.5);

      SFX.shoot();
      SFX.whoosh(clamp(0.35 + lane.power * 0.6 + flickStrength * 0.3, 0.2, 1));
      if (releaseType === "perfect") SFX.releasePerfect();
      else SFX.releaseSoft();
      if (releaseType === "perfect" && typeof navigator !== "undefined" && navigator.vibrate) {
        navigator.vibrate(12);
      }
    },
    [recordShotAttempt]
  );

  const resolveShot = useCallback(
    (lane) => {
      if (lane.shotResolved) return;
      const physicsTune =
        PHYSICS_PROFILES[resolvePhysicsProfile(physicsProfile)] || PHYSICS_PROFILES.arcade;
      const hx = hoopX(lane);
      const hy = getRimCenterY(lane);
      const dx = lane.ball.x - hx;
      const dy = lane.ball.y - hy;
      const dist = Math.sqrt(dx * dx + dy * dy);
      const safeDist = Math.max(0.001, dist);
      const nx = dx / safeDist;
      const ny = dy / safeDist;
      const stability = lane.shotStability || 0.5;
      const smoothness = lane.releaseSmooth || 0.5;
      const timingTune = TIMING_DIFFICULTY_TUNING[lane.shotDifficulty] || TIMING_DIFFICULTY_TUNING.medium;
      const timingHuman = TIMING_ONLY_SHOTS && !lane.isAI;
      let effectiveDist =
        dist + (1 - stability) * 5.5 + rand(-2, 2) * (1 - stability) - smoothness * 2.4;
      if (TIMING_ONLY_SHOTS && !lane.isAI) {
        const makeWobbleWindow =
          lane.hoop.r * 2 * TIMING_SCORE_WINDOW_RATIO * timingTune.makeWindowMul;
        const inWindow = Math.abs(lane.timingWobble || 0) < makeWobbleWindow;
        const forgivingWindow =
          Math.abs(lane.timingWobble || 0) < lane.hoop.r * 1.1 * timingTune.forgivingWindowMul;
        if (inWindow || forgivingWindow) {
          effectiveDist = Math.min(effectiveDist, lane.hoop.r * 0.08);
        }
        if (lane.forceMake) {
          effectiveDist = Math.min(
            effectiveDist,
            lane.hoop.r * (lane.forceSwish ? 0.03 : 0.1)
          );
          if (lane.forceSwish) lane.rimTouched = false;
        }
      }
      let makeRadius = lane.hoop.r * (0.62 + stability * 0.22 + smoothness * 0.08);
      let swishRadius = lane.hoop.r * (0.24 + stability * 0.24 + smoothness * 0.06);
      const globalAccuracyScale = clamp(SHOT_FEEL_CONFIG.forgiveness.globalAccuracyScale, 0.8, 1.4);
      if (timingHuman) {
        const makeScale = getDifficultyScalar(SHOT_FEEL_CONFIG.forgiveness.makeRadiusScale, lane.shotDifficulty, 1);
        makeRadius *= timingTune.makeRadiusMul * TIMING_MAKE_RADIUS_BOOST * makeScale * globalAccuracyScale;
        swishRadius *= timingTune.swishRadiusMul * TIMING_SWISH_RADIUS_BOOST * makeScale * globalAccuracyScale;
      } else {
        makeRadius *= globalAccuracyScale;
        swishRadius *= globalAccuracyScale * 0.96;
      }
      const easySwishBias =
        lane.shotDifficulty === "easy" && !lane.isAI ? (lane.easyPlusAssist ? 1.14 : 1.08) : 1;
      const swishRadiusBiased = swishRadius * easySwishBias;
      const rimDist = Math.abs(effectiveDist - lane.hoop.r);
      const rimThreshold = lane.hoop.r * (0.35 + (1 - stability) * 0.14);
      const incomingSpeed = Math.hypot(lane.ball.vx || 0, lane.ball.vy || 0);
      const incomingAngle = Math.atan2(Math.max(1, lane.ball.vy || 0), Math.max(1, Math.abs(lane.ball.vx || 0)));
      const cleanDrop = incomingAngle > 0.95 && incomingAngle < 1.54;
      const ballRadius = getBallRadiusAtY(lane.ball.y) * 0.74;
      const innerTubeBase = Math.max(lane.hoop.r * 0.34, lane.hoop.r - ballRadius * 0.78);
      const innerTube = timingHuman ? innerTubeBase * timingTune.entryTubeMul : innerTubeBase;
      const prevX = lane.prevX ?? lane.ball.x;
      const prevY = lane.prevY ?? lane.ball.y;
      const corePlaneY = hy + lane.hoop.r * 0.14;
      const crossedCorePlane = prevY < corePlaneY && lane.ball.y >= corePlaneY;
      const segmentCoreDistance = pointSegmentDistance(
        hx,
        corePlaneY,
        prevX,
        prevY,
        lane.ball.x,
        lane.ball.y
      );
      const segmentCoreGate = segmentCoreDistance <= innerTube * 0.62;
      const visibleDropGate =
        Math.abs(dx) <= innerTube &&
        lane.ball.y >= hy - lane.hoop.r * 0.18 &&
        lane.ball.y <= hy + lane.hoop.r * 0.88;
      const descendingThroughRim = lane.ball.vy > 70 && prevY < hy + lane.hoop.r * 0.36;
      const clearedRimPlane = !!lane.clearedRimPlane;
      const passedRimDepth = lane.ball.y >= hy + lane.hoop.r * 0.02;
      const makeVisualGate =
        ((visibleDropGate || crossedCorePlane || segmentCoreGate) && descendingThroughRim) ||
        (cleanDrop && descendingThroughRim && (crossedCorePlane || segmentCoreGate));
      const forceMakeVisualGate =
        !!lane.forceMake && (visibleDropGate || crossedCorePlane || segmentCoreGate);
      const quality = fxRef.current.quality || 1;
      const clutchActive = timeLeftRef.current <= CLUTCH_SECONDS;
      lane.clutchBonusActive = false;
      const tune = DIFFICULTY_TUNING[lane.shotDifficulty] || DIFFICULTY_TUNING.medium;
      const hotZoneActive =
        lane.dailyModifier === "hot_zone" &&
        Math.abs((lane.launchX ?? lane.home.x) - lane.hotZoneX) <= lane.hotZoneR;
      lane.hotZoneBoost = hotZoneActive;
      lane.shotResolved = true;
      lane.settleTimer = 0;
      const rimSoftnessLocal = clamp(RimFeelTuning.rimSoftness, 0.68, 1.4);
      const rimChanceMul = clamp(RimFeelTuning.rimInChanceMul, 0.55, 1.75);

      const applyRimDeflection = (strength = 1) => {
        const reflectMul = clamp(1.04 - (rimSoftnessLocal - 1) * 0.5, 0.56, 1.1);
        const kickMul = clamp(0.98 - (rimSoftnessLocal - 1) * 0.62, 0.42, 1.02);
        const dot = lane.ball.vx * nx + lane.ball.vy * ny;
        if (dot > 0) {
          lane.ball.vx -= dot * nx * physicsTune.rimReflect * reflectMul * strength;
          lane.ball.vy -= dot * ny * physicsTune.rimReflect * reflectMul * strength;
        }
        const stabilitySoft = 0.78 + stability * 0.2;
        lane.ball.vx += nx * (physicsTune.rimKickX * 0.82 + rand(-14, 14)) * kickMul * strength * stabilitySoft;
        lane.ball.vy += ny * (physicsTune.rimKickY * 0.82 + rand(-12, 12)) * kickMul * strength * stabilitySoft;
      };

      const canCountAsMake =
        clearedRimPlane &&
        passedRimDepth &&
        effectiveDist < makeRadius * 1.03 &&
        (makeVisualGate || !TIMING_ONLY_SHOTS || lane.isAI || forceMakeVisualGate);
      if (canCountAsMake) {
        lane.madeShot = true;
        const swish =
          clearedRimPlane &&
          passedRimDepth &&
          ((!lane.rimTouched && cleanDrop && effectiveDist < swishRadiusBiased) ||
            (!lane.rimTouched && lane.releaseType === "perfect" && stability > 0.7 && cleanDrop)) &&
          makeVisualGate;
        lane.netWave = 1;
        lane.hoopPulse = 1;
        kickNet(lane, swish ? 1.35 : 1.02, swish ? 0.92 : 0.8);
        if (lane.netState) lane.netState.glow = Math.max(lane.netState.glow, swish ? 1.22 : 0.95);
        lane.lanePulse = Math.max(lane.lanePulse || 0, swish ? 0.92 : 0.72);
        lane.misses = 0;
        lane.streak += 1;
        lane.stats.makes += 1;
        if (swish) lane.stats.swishes += 1;

        let pts = swish ? 3 : 2;
        if (lane.rimFire) pts += 1;
        if (clutchActive) {
          pts += 1;
          lane.stats.clutchPoints += 1;
          lane.clutchBonusActive = true;
        }
        if (hotZoneActive) {
          pts += 1;
          lane.stats.hotZoneMakes += 1;
        }
        pts = Math.max(1, Math.round(pts * tune.pointsMul));
        lane.score += pts;
        lane.dynamicAssist = Math.max(
          0,
          lane.dynamicAssist - DYNAMIC_ASSIST_DECAY * (lane.releaseType === "perfect" ? 1.35 : 1)
        );

        if (lane.streak >= 3 && !lane.rimFire) {
          lane.rimFire = true;
          SFX.fireOn();
        }

        const perfectSwish = swish && lane.releaseType === "perfect" && stability > 0.85;
        lane.outcomeTier = perfectSwish ? "perfect" : swish ? "swish" : "make";
        recordShotOutcome(lane, swish ? "swish" : "make");
        const banked = lane.hitBackboard && !swish;
        const makeCallout = banked
          ? "OFF GLASS WET"
          : perfectSwish
            ? nextCallout(lane, "perfect", "WET")
            : swish
              ? nextCallout(lane, "swish", "WET")
              : nextCallout(lane, "make", "WET");
        lane.flash = `${makeCallout} +${pts}`;
        lane.flashColor = lane.rimFire ? "#ffd87a" : "#61ffd1";
        lane.flashAlpha = 1;
        lane.scorePop = 1;
        lane.comboPop = 1;

        SFX.chainNet();
        if (swish) SFX.swishBass();
        spawnBurst(
          particlesRef.current,
          hx,
          hy + 8,
          lane.rimFire ? "#ffd700" : "#5dffd5",
          22,
          340,
          quality
        );
        spawnBurst(
          particlesRef.current,
          hx,
          hy + lane.hoop.r * 0.58,
          "#f2fbff",
          swish ? 18 : 12,
          swish ? 280 : 220,
          quality
        );
        if (swish) {
          spawnBurst(
            particlesRef.current,
            hx,
            hy + 2,
            lane.rimFire ? "#ffcf67" : "#6ffff5",
            28,
            420,
            quality
          );
          fxRef.current.shake = Math.max(fxRef.current.shake, perfectSwish ? 0.82 : 0.62);
          fxRef.current.slowMoTime = Math.max(
            fxRef.current.slowMoTime || 0,
            SWISH_SLOW_MO_DURATION
          );
          fxRef.current.shakeY = (fxRef.current.shakeY || 0) + 1.2;
        }
        if (perfectSwish) {
          lane.releaseFlash = Math.max(lane.releaseFlash, 1);
          lane.hoopPulse = 1.25;
          spawnBurst(particlesRef.current, hx, hy - 8, "#fff2b0", 36, 520, quality);
        }
        // Let the ball continue naturally through the net before floor bounce.
        // Keep position continuous; only steer velocity toward a cleaner drop line.
        lane.ball.x = lerp(lane.ball.x, hx, swish ? 0.42 : 0.32);
        lane.ball.y = Math.max(lane.ball.y, hy - lane.hoop.r * 0.02);
        lane.ball.vx = lerp(lane.ball.vx, (hx - lane.ball.x) * 1.55, swish ? 0.42 : 0.3);
        const swishDropBoost = SHOT_FEEL_CONFIG.physics.swishDropBoost * (swish ? 1 : 0.5);
        const aimedDropVy =
          (Math.max(lane.ball.vy, swish ? 232 : 214) + rand(16, 52) + swishDropBoost) *
          physicsTune.makeDropBoost;
        lane.ball.vy = lerp(lane.ball.vy, aimedDropVy, swish ? 0.72 : 0.62);
        lane.ball.spin += (swish ? 0.22 : 0.12) * lane.side;
        if (lane.ball.y < hy + lane.hoop.r * 0.18) lane.ball.vy += 42;
      } else if (rimDist < rimThreshold || effectiveDist < lane.hoop.r * (1.18 + (1 - stability) * 0.12)) {
        const speedWindow = clamp(1 - Math.max(0, incomingSpeed - 460) / 540, 0.58, 1.08);
        const angleWindow = clamp(1 - Math.abs(incomingAngle - 1.2) / 0.62, 0.6, 1.08);
        const spinWindow = 1 + clamp(lane.backspinBias || 0, 0, 0.45) * 0.28;
        const rimInChance = clamp(
          (0.06 + stability * 0.2 + (lane.releaseType === "perfect" ? 0.12 : 0)) *
            rimChanceMul *
            speedWindow *
            angleWindow *
            spinWindow,
          0.02,
          0.74
        );
        const rimInVisualGate =
          clearedRimPlane &&
          passedRimDepth &&
          (visibleDropGate || crossedCorePlane || segmentCoreGate);
        const rimIn = rimInVisualGate && rimDist < rimThreshold * 0.72 && Math.random() < rimInChance;
        lane.netWave = rimIn ? 0.8 : 0.52;
        lane.rimJolt = 0.8 + (1 - stability) * 0.7;
        lane.rimJoltPhase = Math.random() * Math.PI * 2;
        kickNet(lane, rimIn ? 0.95 : 0.42, rimIn ? 0.65 : 0.22);
        if (rimIn) {
          lane.madeShot = true;
          lane.stats.rimIn += 1;
          recordShotOutcome(lane, "rim_in");
          lane.stats.makes += 1;
          lane.streak += 1;
          lane.misses = 0;
          let pts = 2;
          if (lane.rimFire) pts += 1;
          if (clutchActive) {
            pts += 1;
            lane.stats.clutchPoints += 1;
            lane.clutchBonusActive = true;
          }
          if (hotZoneActive) {
            pts += 1;
            lane.stats.hotZoneMakes += 1;
          }
          pts = Math.max(1, Math.round(pts * tune.pointsMul));
          lane.score += pts;
          lane.dynamicAssist = Math.max(0, lane.dynamicAssist - DYNAMIC_ASSIST_DECAY * 0.85);
          if (lane.streak >= 3 && !lane.rimFire) {
            lane.rimFire = true;
            SFX.fireOn();
          }
          lane.outcomeTier = "rim_in";
          lane.lanePulse = Math.max(lane.lanePulse || 0, 0.58);
          lane.flash = `${nextCallout(lane, "rimIn", "WET ROLL")} +${pts}`;
          lane.flashColor = "#ffc87a";
          lane.flashAlpha = 1;
          lane.scorePop = 0.85;
          lane.comboPop = 0.7;
          SFX.rim();
          SFX.chainNet();
          fxRef.current.shake = Math.max(fxRef.current.shake, 0.32);
          spawnBurst(particlesRef.current, hx, hy + 2, "#ffc06f", 16, 300, quality);
          applyRimDeflection(0.55);
          lane.ball.vx = lerp(lane.ball.vx, (hx - lane.ball.x) * 1.4, 0.38);
          lane.ball.vy = (Math.max(lane.ball.vy, 260) + rand(15, 55)) * physicsTune.makeDropBoost;
        } else {
          lane.madeShot = false;
          lane.stats.rims += 1;
          lane.outcomeTier = "rim";
          recordShotOutcome(lane, "rim_out");
          lane.lanePulse = Math.max(lane.lanePulse || 0, 0.28);
          registerMiss(lane);
          lane.flash = nextCallout(lane, "rim", "BROKE SHOT");
          lane.flashColor = "#ff8f4f";
          lane.flashAlpha = 1;
          SFX.rim();
          fxRef.current.shake = Math.max(fxRef.current.shake, 0.22);
          spawnBurst(particlesRef.current, hx, hy, "#ff8f4f", 10, 250, quality);
          applyRimDeflection(1);
          lane.ball.vy = clamp(Math.max(96, lane.ball.vy * 0.92), 96, 420);
          lane.ball.vx *= 0.96;
        }
      } else {
        lane.madeShot = false;
        lane.outcomeTier = "miss";
        recordShotOutcome(lane, "miss");
        lane.lanePulse = Math.max(lane.lanePulse || 0, 0.18);
        registerMiss(lane);
        lane.flash = nextCallout(lane, "miss", "BROKE SHOT");
        lane.flashColor = "#ff4f58";
        lane.flashAlpha = 1;
        SFX.miss();
        lane.ball.vy = Math.max(lane.ball.vy, 220);
      }

      syncHud();
    },
    [physicsProfile, recordShotOutcome, registerMiss, syncHud]
  );

  const submitBackendMatch = useCallback(
    (payload) => {
      const currentSession = sessionRef.current;
      const sessionId = typeof currentSession === "string" ? currentSession : currentSession?.id;
      const sessionEpoch =
        typeof currentSession === "string"
          ? sessionEpochRef.current
          : currentSession?.epoch ?? sessionEpochRef.current;
      if (!sessionId) return;
      finishMatchSession(sessionId, payload)
        .then((result) => {
          setBackendStatus("online");
          backendRetryAtRef.current = 0;
          const nextBoard = toArray(result?.leaderboard);
          if (nextBoard.length > 0) setBackendLeaderboard(nextBoard);
          refreshBackendPanels();
        })
        .catch(() => {
          backendRetryAtRef.current = Date.now() + BACKEND_RETRY_COOLDOWN_MS;
          setBackendStatus("offline");
        })
        .finally(() => {
          const nextSession = sessionRef.current;
          const nextSessionEpoch =
            typeof nextSession === "string"
              ? sessionEpochRef.current
              : nextSession?.epoch ?? sessionEpochRef.current;
          if (nextSessionEpoch === sessionEpoch) {
            sessionRef.current = { id: null, epoch: sessionEpoch };
          }
        });
    },
    [refreshBackendPanels]
  );

  const startGame = useCallback(() => {
    const setup = sanitizeGameSetup({
      shotDifficulty,
      aimMode,
      dominantHand,
      controlProfile,
      physicsProfile: resolvePhysicsProfile(physicsProfile),
      ballSkin,
      playerBallColor,
      opponentBallColor,
    });
    if (setup.changed) {
      setShotDifficulty(setup.shotDifficulty);
      setAimMode(setup.aimMode);
      setDominantHand(setup.dominantHand);
      setControlProfile(setup.controlProfile);
      setPhysicsProfile(setup.physicsProfile);
      setBallSkin(setup.ballSkin);
      setPlayerBallColor(setup.playerBallColor);
      setOpponentBallColor(setup.opponentBallColor);
      if (!sanityLoggedRef.current) {
        // eslint-disable-next-line no-console
        console.warn("Neon Hoopz sanity fallback applied:", setup.issues);
        sanityLoggedRef.current = true;
      }
    }
    const nextModifier =
      dailyMode && !practiceMode ? randomFrom(DAILY_MODIFIERS.filter((m) => m !== "none")) : "none";
    setDailyModifier(nextModifier);
    initializeMatch(nextModifier, setup);
    const safeName =
      (playerName || "PLAYER ONE")
        .trim()
        .replace(/\s+/g, " ")
        .slice(0, 24) || "PLAYER ONE";
    sessionEpochRef.current += 1;
    const startEpoch = sessionEpochRef.current;
    sessionRef.current = { id: null, epoch: startEpoch };
    if (
      !practiceMode &&
      BACKEND_ENABLED &&
      backendStatus === "online" &&
      Date.now() >= backendRetryAtRef.current
    ) {
      startMatchSession({
        playerName: safeName,
        mode: getModeLabel({ practiceMode, vrMode, oneViewMode }),
        difficulty: setup.shotDifficulty,
        aimMode: setup.aimMode,
        controlProfile: setup.controlProfile,
        physicsProfile: setup.physicsProfile,
        dailyMode,
      })
        .then((result) => {
          if (sessionEpochRef.current !== startEpoch) return;
          sessionRef.current = { id: result?.sessionId || null, epoch: startEpoch };
          setBackendStatus("online");
        })
        .catch(() => {
          if (sessionEpochRef.current !== startEpoch) return;
          backendRetryAtRef.current = Date.now() + BACKEND_RETRY_COOLDOWN_MS;
          setBackendStatus("offline");
        });
    } else if (!practiceMode) {
      setBackendStatus("offline");
    }
    setFinalState(null);
    const nextLength = practiceMode ? GAME_LENGTH : dailyMode ? 60 : GAME_LENGTH;
    setMatchLength(nextLength);
    setTimeLeft(nextLength);
    setScreen("game");
  }, [
    aimMode,
    ballSkin,
    backendStatus,
    controlProfile,
    dailyMode,
    dominantHand,
    initializeMatch,
    oneViewMode,
    opponentBallColor,
    physicsProfile,
    playerBallColor,
    playerName,
    practiceMode,
    shotDifficulty,
    vrMode,
  ]);

  useEffect(() => {
    if (screen !== "game") return;
    if (practiceMode) return;

    let finished = false;
    const timer = setInterval(() => {
      setTimeLeft((t) => {
        if (finished) return 0;
        if (t <= 1) {
          finished = true;
          clearInterval(timer);
          const [player, opponent] = lanesRef.current;
          const singleLaneSession = vrMode || oneViewMode || practiceMode;
          const pScore = player?.score || 0;
          const oScore = opponent?.score || 0;
          const pStats = { ...(player?.stats || {}) };
          const oStats = { ...(opponent?.stats || {}) };
          const won = singleLaneSession ? pScore >= 34 : pScore > oScore;
          const loss = !singleLaneSession && pScore < oScore;
          const tie = !singleLaneSession && pScore === oScore;
          const challengeCleared = dailyMode && !practiceMode ? pScore >= 34 : false;
          const baseRankDelta = singleLaneSession
            ? pScore
            : won
              ? 10 + Math.max(0, Math.min(10, pScore - oScore))
              : tie
                ? 2
                : -Math.max(4, Math.min(12, oScore - pScore + 4));
          const dailyRankBonus = challengeCleared ? 8 : 0;
          const rankDelta = baseRankDelta + dailyRankBonus;
          setBestScore((prev) => Math.max(prev, pScore));
          const prevLadderPoints = ladderPointsRef.current;
          const nextLadderPoints = Math.max(0, prevLadderPoints + rankDelta);
          if (didTierUp(prevLadderPoints, nextLadderPoints)) {
            triggerRankUpCeremony(resolveTier(nextLadderPoints));
          }
          ladderPointsRef.current = nextLadderPoints;
          setLadderPoints(nextLadderPoints);
          setAchievements((prev) => ({
            releaseArtist: prev.releaseArtist || (pStats.perfectReleases || 0) >= 8,
            cleanShooter:
              prev.cleanShooter ||
              ((pStats.makes || 0) >= 14 && ((pStats.misses || 0) + (pStats.rims || 0)) <= 5),
            clutchFinisher: prev.clutchFinisher || (pStats.clutchPoints || 0) >= 6,
          }));
          setFinalState({
            playerScore: pScore,
            opponentScore: oScore,
            dailyMode,
            challengeCleared,
            dailyModifier,
            shotDifficulty: player?.shotDifficulty || "medium",
            controlProfile: player?.controlProfile || "rookie",
            singleLaneSession,
            rankDelta,
            won,
            loss,
            tie,
            aiStyle: opponent?.aiStyle || "",
            playerStats: pStats,
            opponentStats: oStats,
          });
          submitBackendMatch({
            score: pScore,
            opponentScore: oScore,
            won,
            tie,
            mode: getModeLabel({ practiceMode, vrMode, oneViewMode }),
            difficulty: player?.shotDifficulty || shotDifficulty,
            aimMode: player?.aimMode || aimMode,
            controlProfile: player?.controlProfile || controlProfile,
            physicsProfile,
            dailyMode,
            durationSec: matchLength,
            stats: pStats,
            opponentStyle: opponent?.aiStyle || "",
          });
          SFX.gameOver();
          setScreen("gameover");
          return 0;
        }
        if (t <= CLUTCH_SECONDS) SFX.urgent();
        return t - 1;
      });
    }, 1000);

    return () => clearInterval(timer);
  }, [
    aimMode,
    controlProfile,
    dailyMode,
    dailyModifier,
    matchLength,
    oneViewMode,
    physicsProfile,
    practiceMode,
    screen,
    shotDifficulty,
    submitBackendMatch,
    triggerRankUpCeremony,
    vrMode,
  ]);

  useEffect(() => {
    if (screen !== "game") return;
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;
    const physicsTune =
      PHYSICS_PROFILES[resolvePhysicsProfile(physicsProfile)] || PHYSICS_PROFILES.arcade;

    const getCanvasPoint = (e) => {
      const rect = canvas.getBoundingClientRect();
      if (!rect || rect.width <= 0 || rect.height <= 0) return { x: CW * 0.5, y: CH * 0.78 };
      const px = (e.clientX - rect.left) * (CW / rect.width);
      const py = (e.clientY - rect.top) * (CH / rect.height);
      return { x: px, y: py };
    };

    const pushPointerHistory = (pt, tMs) => {
      const pref = pointerRef.current;
      pref.history.push({ x: pt.x, y: pt.y, t: tMs });
      while (pref.history.length > 0 && tMs - pref.history[0].t > 240) pref.history.shift();
      if (pref.history.length > 12) pref.history.shift();
    };

    const clearActiveInput = () => {
      const pref = pointerRef.current;
      pref.active = false;
      pref.releaseTracking = false;
      keyHeldRef.current = false;
    };

    const pointerDown = (e) => {
      if (screenRef.current !== "game") return;
      const [player] = lanesRef.current;
      if (!player) return;
      const pt = getCanvasPoint(e);
      const pref = pointerRef.current;
      pref.active = true;
      pref.id = e.pointerId;
      pref.rawX = pt.x;
      pref.rawY = pt.y;
      pref.x = pt.x;
      pref.y = pt.y;
      pref.startT = performance.now();
      pref.lastX = pt.x;
      pref.lastY = pt.y;
      pref.lastMoveMs = performance.now();
      pref.history = [];
      pref.releaseTracking = false;
      pref.pointerType = e?.pointerType || "touch";
      pref.inputCal = inputCalRef.current;
      pref.moveAvgMs = 16;
      pref.jitterAvg = 0;
      if (canvas.setPointerCapture) {
        try {
          canvas.setPointerCapture(e.pointerId);
        } catch (_) {}
      }
      pushPointerHistory(pt, performance.now());
      if (vrMode || oneViewMode || practiceMode || pt.x <= CW * 0.58) {
        startCharging(player);
        player.aimX = pt.x;
        player.aimY = pt.y;
        pref.startX = pt.x;
        pref.startY = pt.y;
        pref.startPower = player.chargeStartLinear ?? player.powerLinear;
        keyHeldRef.current = true;
      }
    };

    const pointerMove = (e) => {
      if (screenRef.current !== "game") return;
      const [player] = lanesRef.current;
      if (!player) return;
      const pref = pointerRef.current;
      const nowMs = performance.now();
      const inReleaseWindow = pref.releaseTracking && nowMs <= pref.releaseExpire;
      if (!pref.active && !inReleaseWindow) return;
      if (pref.active && pref.id !== e.pointerId) return;
      const pt = getCanvasPoint(e);
      const dy = pt.y - pref.lastY;
      const moveMsRaw = pref.lastMoveMs > 0 ? Math.max(1, nowMs - pref.lastMoveMs) : 16.7;
      pref.moveAvgMs = lerp(pref.moveAvgMs || 16.7, moveMsRaw, 0.2);
      const moveDt = Math.min(0.05, Math.max(0.001, moveMsRaw / 1000));
      const inputCal = pref.inputCal || inputCalRef.current;
      const smoothRate =
        (inputCal?.smoothingRate || 26) * (pref.pointerType === "touch" ? 1.04 : 0.92);
      const jitterDrag = Math.hypot(pt.x - (pref.rawX || pt.x), pt.y - (pref.rawY || pt.y));
      pref.jitterAvg = lerp(pref.jitterAvg || 0, jitterDrag, 0.18);
      const jitterBrake = clamp(1 - (pref.jitterAvg || 0) / 34, 0.58, 1);
      const basePointerSmoothing = 1 - Math.exp(-smoothRate * jitterBrake * moveDt);
      const pointerSmoothing = clamp(
        basePointerSmoothing * (1 + SHOT_FEEL_CONFIG.input.smoothing * 0.22),
        0.08,
        0.78
      );
      pushPointerHistory(pt, nowMs);
      const recent = pref.history.slice(-4);
      let filteredX = pt.x;
      let filteredY = pt.y;
      if (recent.length >= 2) {
        let sumX = 0;
        let sumY = 0;
        let totalW = 0;
        for (let i = 0; i < recent.length; i++) {
          const w = i + 1;
          sumX += recent[i].x * w;
          sumY += recent[i].y * w;
          totalW += w;
        }
        filteredX = sumX / totalW;
        filteredY = sumY / totalW;
      }
      pref.rawX = lerp(pref.rawX || filteredX, filteredX, 0.72);
      pref.rawY = lerp(pref.rawY || filteredY, filteredY, 0.72);
      pref.x = lerp(pref.x, pref.rawX, pointerSmoothing);
      pref.y = lerp(pref.y, pref.rawY, pointerSmoothing);
      pref.lastX = pt.x;
      pref.lastY = pt.y;
      pref.lastMoveMs = nowMs;
      if (player && player.followWindow > 0) {
        const norm = clamp(Math.abs(dy) / 18, 0, 1);
        if (dy < -1) {
          player.followSpinBoost = clamp(player.followSpinBoost + norm * 0.1, 0, 0.3);
        } else if (dy > 1) {
          player.followArcAdjust = clamp(player.followArcAdjust + norm * 16, 0, 55);
        }
      }
      if (pref.releaseTracking && nowMs > pref.releaseExpire) {
        pref.releaseTracking = false;
      }
    };

    const pointerUp = (e) => {
      if (screenRef.current !== "game") return;
      const [player] = lanesRef.current;
      if (!player) return;
      const pref = pointerRef.current;
      pref.active = false;
      pref.id = e?.pointerId ?? pref.id;
      const nowMs = performance.now();
      pref.lastMoveMs = nowMs;
      pref.releaseY = pref.y;
      pref.releaseTracking = true;
      pref.releaseExpire = nowMs + FOLLOW_THROUGH_WINDOW * 1000;
      const swipeDx = pref.x - pref.startX;
      const swipeDy = pref.y - pref.startY;
      const swipePixels = Math.hypot(swipeDx, swipeDy);
      const swipeMs = Math.max(1, nowMs - (pref.startT || nowMs));
      const inputCal = pref.inputCal || inputCalRef.current;
      const calibratedSwipePixels = swipePixels * (inputCal?.swipeDistanceMul || 1);
      const calibratedSwipeMs = swipeMs * (inputCal?.swipeTimeMul || 1);
      player.swipePixels = calibratedSwipePixels;
      player.swipeMs = calibratedSwipeMs;
      const swipeDistanceNorm = clamp(
        (calibratedSwipePixels - ShotTuning.minSwipePixels) /
          Math.max(1, ShotTuning.maxSwipePixels - ShotTuning.minSwipePixels),
        0,
        1
      );
      const swipeTimeNorm = clamp(
        (calibratedSwipeMs - ShotTuning.minSwipeTime) /
          Math.max(1, ShotTuning.maxSwipeTime - ShotTuning.minSwipeTime),
        0,
        1
      );
      const steadiness = clamp(1 - Math.abs(swipeTimeNorm - 0.55) * 1.35, 0, 1);
      const steadyBias = inputCal?.steadyBias || 1;
      const runtimeStability = clamp(
        1 - (pref.jitterAvg || 0) / 20 - Math.max(0, (pref.moveAvgMs || 16) - 16) / 90,
        0.45,
        1
      );
      player.sloppySwipe = clamp(
        (1 - swipeDistanceNorm) * 0.45 + (1 - steadiness * steadyBias) * 0.55,
        0,
        1
      );
      player.sloppySwipe *= clamp(0.7 + runtimeStability * 0.34, 0.7, 1.04);
      if (canvas.releasePointerCapture && e?.pointerId !== undefined) {
        try {
          canvas.releasePointerCapture(e.pointerId);
        } catch (_) {}
      }
      releaseShot(player);
      keyHeldRef.current = false;
    };

    const pointerCancel = (e) => {
      const pref = pointerRef.current;
      if (!pref.active) return;
      if (e?.pointerId !== undefined && pref.id !== e.pointerId) return;
      clearActiveInput();
      if (canvas.releasePointerCapture && e?.pointerId !== undefined) {
        try {
          canvas.releasePointerCapture(e.pointerId);
        } catch (_) {}
      }
      const [player] = lanesRef.current;
      if (player?.state === "charging") {
        player.state = "idle";
        player.previewTTL = 0;
        player.previewAlpha = 0;
      }
    };

    const keyDown = (e) => {
      if (screenRef.current !== "game") return;
      if (showDevPanel && e.code === "KeyG") {
        e.preventDefault();
        setDebugArcOverlay((v) => !v);
        playUiTap();
        return;
      }
      if (e.code === "Escape") {
        e.preventDefault();
        sessionEpochRef.current += 1;
        sessionRef.current = { id: null, epoch: sessionEpochRef.current };
        setScreen("menu");
        SFX.ui();
        return;
      }
      if (e.code === "Space" || e.code === "KeyF") {
        e.preventDefault();
        if (keyHeldRef.current) return;
        const [player] = lanesRef.current;
        startCharging(player);
        keyHeldRef.current = true;
      }
    };

    const keyUp = (e) => {
      if (screenRef.current !== "game") return;
      if (e.code === "Space" || e.code === "KeyF") {
        e.preventDefault();
        const [player] = lanesRef.current;
        releaseShot(player);
        keyHeldRef.current = false;
      }
    };

    const onWindowBlur = () => {
      clearActiveInput();
      const [player] = lanesRef.current;
      if (player?.state === "charging") {
        player.state = "idle";
        player.previewTTL = 0;
        player.previewAlpha = 0;
      }
    };

    canvas.addEventListener("pointerdown", pointerDown);
    window.addEventListener("pointermove", pointerMove);
    window.addEventListener("pointerup", pointerUp);
    window.addEventListener("pointercancel", pointerCancel);
    window.addEventListener("blur", onWindowBlur);
    window.addEventListener("keydown", keyDown);
    window.addEventListener("keyup", keyUp);

    let last = performance.now();
    let accumulator = 0;

    const updateLane = (lane, dt) => {
      recoverBallIfInvalid(lane);
      lane.hoopMovePhase += dt * lane.hoopMoveSpeed;
      lane.hoopMoveCooldown -= dt;
      if (!RIM_WORLD_LOCK && movingRims) {
        if (lane.hoopMoveCooldown <= 0) {
          const startMove = Math.random() < 0.58;
          if (startMove) {
            lane.hoopMoveAmpX = rand(8, 24) * (Math.random() < 0.5 ? -1 : 1);
            lane.hoopMoveAmpY = rand(2, 12) * (Math.random() < 0.5 ? -1 : 1);
            lane.hoopMoveCooldown = rand(1.2, 2.4);
          } else {
            lane.hoopMoveAmpX = 0;
            lane.hoopMoveAmpY = 0;
            lane.hoopMoveCooldown = rand(0.9, 2);
          }
        }
        const targetX = Math.sin(lane.hoopMovePhase) * lane.hoopMoveAmpX;
        const targetY = Math.cos(lane.hoopMovePhase * 1.28 + lane.breathPhase * 0.3) * lane.hoopMoveAmpY;
        lane.hoopOffsetX = lerp(lane.hoopOffsetX, targetX, 0.09);
        lane.hoopOffsetY = lerp(lane.hoopOffsetY, targetY, 0.09);
      } else {
        lane.hoopMoveAmpX = 0;
        lane.hoopMoveAmpY = 0;
        lane.hoopOffsetX = 0;
        lane.hoopOffsetY = 0;
      }

      if (lane.state === "idle") {
        lane.powerLinear = Math.max(0.05, lane.powerLinear - dt * 0.12);
        lane.power = powerCurve(lane.powerLinear);
        lane.preShotLateral = lerp(lane.preShotLateral || 0, 0, 0.16);
        lane.ball.x = lerp(lane.ball.x, lane.home.x + (lane.preShotLateral || 0), 0.2);
        lane.ball.y = lerp(lane.ball.y, lane.home.y, 0.2);
      }

      if (lane.state === "charging") {
        const controlTune = CONTROL_PROFILES[lane.controlProfile] || CONTROL_PROFILES.smooth;
        const tune = DIFFICULTY_TUNING[lane.shotDifficulty] || DIFFICULTY_TUNING.medium;
        if (lane.isAI) {
          const chargeSpeedBase =
            lane.aimMode === "casual" ? CASUAL_CHARGE_SWEEP_SPEED : PRO_CHARGE_SWEEP_SPEED;
          lane.powerLinear += lane.powerDir * dt * chargeSpeedBase * controlTune.chargeSpeedMul;
          if (lane.powerLinear > getDragPowerMax()) {
            lane.powerLinear = getDragPowerMax();
            lane.powerDir = -1;
          }
          if (lane.powerLinear < getDragPowerMin()) {
            lane.powerLinear = getDragPowerMin();
            lane.powerDir = 1;
          }
        } else {
          if (TIMING_ONLY_SHOTS) {
            const timingTune =
              TIMING_DIFFICULTY_TUNING[lane.shotDifficulty] || TIMING_DIFFICULTY_TUNING.medium;
            const timingManual = pointerRef.current.active;
            if (timingManual) {
              const px = pointerRef.current.x || lane.home.x;
              const py = pointerRef.current.y || lane.home.y;
              const startX = pointerRef.current.startX || px;
              const startY = pointerRef.current.startY || py;
              const dragX = px - startX;
              const dragY = py - startY;
              const dragDistRaw = Math.hypot(dragX, dragY);
              const dragDist = clamp(dragDistRaw, 0, SHOT_FEEL_CONFIG.input.maxDragPixels);
              lane.dragDist = lerp(lane.dragDist || 0, dragDist, SHOT_FEEL_CONFIG.input.smoothing);
              const targetCharge = computeTouchPowerNorm(lane.dragDist, lane.shotDifficulty);
              const targetPowerLinear = lerp(getDragPowerMin(), getDragPowerMax(), targetCharge);
              lane.powerLinear = lerp(lane.powerLinear, targetPowerLinear, 0.28);
              lane.timingCharge = clamp(targetCharge, 0, 1);
              lane.power = lane.timingCharge;

              const directionDeadZone = SHOT_FEEL_CONFIG.input.directionDeadZonePx;
              const activeDx = Math.abs(dragX) < directionDeadZone ? 0 : dragX;
              const activeDy = Math.abs(dragY) < directionDeadZone ? 0 : dragY;
              const rawDirX = clamp(activeDx / Math.max(1, SHOT_FEEL_CONFIG.input.maxDragPixels), -1, 1);
              const rawDirY = clamp((startY - py) / Math.max(1, SHOT_FEEL_CONFIG.input.maxDragPixels), -1, 1);
              const rawMag = clamp(Math.hypot(rawDirX, rawDirY), 0, 1);
              if (rawMag > 0.0001) {
                const invMag = 1 / rawMag;
                const dirWeight = Math.pow(rawMag, SHOT_FEEL_CONFIG.input.directionCurve);
                lane.dragAimX = lerp(lane.dragAimX || 0, rawDirX * invMag * dirWeight, 0.24);
                lane.dragAimY = lerp(lane.dragAimY || 1, rawDirY * invMag * dirWeight, 0.24);
              } else {
                lane.dragAimX = lerp(lane.dragAimX || 0, 0, 0.2);
                lane.dragAimY = lerp(lane.dragAimY || 1, 1, 0.14);
              }
              const targetOffset = clamp(lane.dragAimX || 0, -1, 1) * TIMING_LATERAL_MOVE_RANGE;
              lane.preShotLateral = lerp(lane.preShotLateral || 0, targetOffset, 0.28);
            } else {
              const chargeSpeedBase =
                lane.aimMode === "casual" ? CASUAL_CHARGE_SWEEP_SPEED : PRO_CHARGE_SWEEP_SPEED;
              lane.powerLinear += lane.powerDir * dt * chargeSpeedBase * 0.72 * controlTune.chargeSpeedMul;
              if (lane.powerLinear > getDragPowerMax()) {
                lane.powerLinear = getDragPowerMax();
                lane.powerDir = -1;
              }
              if (lane.powerLinear < getDragPowerMin()) {
                lane.powerLinear = getDragPowerMin();
                lane.powerDir = 1;
              }
              lane.timingCharge = clamp(
                (lane.powerLinear - getDragPowerMin()) / Math.max(0.001, getDragPowerMax() - getDragPowerMin()),
                0,
                1
              );
              lane.power = lane.timingCharge;
              lane.preShotLateral = lerp(lane.preShotLateral || 0, 0, 0.18);
              lane.dragAimX = lerp(lane.dragAimX || 0, 0, 0.2);
              lane.dragAimY = lerp(lane.dragAimY || 1, 1, 0.14);
              lane.dragDist = lerp(lane.dragDist || 0, 0, 0.22);
            }
            const laneClamp = TIMING_LATERAL_MOVE_RANGE + lane.hoop.r * 0.18;
            lane.ball.x = clamp(
              lerp(lane.ball.x, lane.home.x + (lane.preShotLateral || 0), 0.26),
              lane.centerX - laneClamp,
              lane.centerX + laneClamp
            );
            lane.ball.y = lerp(lane.ball.y, lane.home.y, 0.24);
            lane.aimX = lane.ball.x;
            lane.aimY = lane.ball.y - 96;
            const lateralAngleBias =
              clamp((lane.preShotLateral || 0) / TIMING_LATERAL_MOVE_RANGE, -1, 1) * 4.1;
            const verticalAngleBias = (clamp(lane.dragAimY || 1, -1, 1) - 0.35) * 10.5;
            lane.angle = lerp(
              lane.angle,
              tune.idealAngle + SHOT_FEEL_CONFIG.physics.arcBoostDeg + lateralAngleBias + verticalAngleBias,
              0.16
            );
            lane.manualPull = lerp(
              lane.manualPull || 0,
              -clamp((lane.preShotLateral || 0) / TIMING_LATERAL_MOVE_RANGE, -1, 1) * 0.2,
              0.24
            );
            const releaseCenterBase =
              lane.shotDifficulty === "easy" ? 0.58 : lane.shotDifficulty === "hard" ? 0.66 : 0.62;
            lane.releaseWindow.center = clamp(releaseCenterBase + (1 - lane.timingCharge) * 0.03, 0.54, 0.82);
            lane.releaseWindow.width = clamp(
              timingTune.windowWidth * (lane.shotDifficulty === "easy" ? 1.08 : 1),
              0.16,
              0.48
            );
          } else {
            const playerManualAim = pointerRef.current.active;
            if (!playerManualAim && keyHeldRef.current) {
              const chargeSpeedBase =
                lane.aimMode === "casual" ? CASUAL_CHARGE_SWEEP_SPEED : PRO_CHARGE_SWEEP_SPEED;
              lane.powerLinear += lane.powerDir * dt * chargeSpeedBase * 0.82 * controlTune.chargeSpeedMul;
              if (lane.powerLinear > getDragPowerMax()) {
                lane.powerLinear = getDragPowerMax();
                lane.powerDir = -1;
              }
              if (lane.powerLinear < getDragPowerMin()) {
                lane.powerLinear = getDragPowerMin();
                lane.powerDir = 1;
              }
              lane.angle = lerp(lane.angle, tune.idealAngle, 0.08);
              lane.manualPull = lerp(lane.manualPull || 0, 0, 0.22);
            } else {
              lane.powerLinear = clamp(lane.powerLinear, getDragPowerMin(), getDragPowerMax());
            }
          }
        }
        if (!TIMING_ONLY_SHOTS || lane.isAI) {
          lane.power = powerCurve(lane.powerLinear);
        }

        const playerManualAim = !TIMING_ONLY_SHOTS && !lane.isAI && pointerRef.current.active;
        if (playerManualAim) {
          const inputCal = pointerRef.current.inputCal || inputCalRef.current;
          const runtimeMoveMs = pointerRef.current.moveAvgMs || 16;
          const runtimeJitter = pointerRef.current.jitterAvg || 0;
          const runtimeResponseMul = clamp(
            1 - Math.max(0, runtimeMoveMs - 16) / 140 + runtimeJitter / 140,
            0.84,
            1.08
          );
          const runtimeAssistMul = clamp(
            1 + Math.max(0, runtimeMoveMs - 16) / 120 + runtimeJitter / 120,
            1,
            1.22
          );
          const tune = DIFFICULTY_TUNING[lane.shotDifficulty] || DIFFICULTY_TUNING.medium;
          const easyPlusActive = lane.shotDifficulty === "easy" && !lane.isAI && lane.easyPlusAssist;
          const easyPlusTune = easyPlusActive ? EASY_PLUS_ASSIST_TUNING : null;
          const casual = lane.aimMode === "casual";
          const px = pointerRef.current.x || lane.centerX;
          const py = pointerRef.current.y || CH * 0.78;
          const startX = pointerRef.current.startX || px;
          const startY = pointerRef.current.startY || py;
          const basePower = pointerRef.current.startPower || lane.chargeStartLinear || lane.powerLinear;
          const userAimScale = clamp(lane.aimSensitivityScale || 1, 0.7, 1.5);
          const difficultyLateralAimMul = (tune.lateralAimMul ?? 1) * (easyPlusTune?.lateralAimMul ?? 1);
          const difficultyPowerDragMul = (tune.powerDragMul ?? 1) * (easyPlusTune?.powerDragMul ?? 1);
          const difficultyPowerDenomMul = (tune.powerDragDenomMul ?? 1) * (easyPlusTune?.powerDragDenomMul ?? 1);
          const centerDriftMul = (tune.centerDriftMul ?? 1) * (easyPlusTune?.centerDriftMul ?? 1);
          const aimSmoothBase = (casual ? CASUAL_AIM_SMOOTH * 0.92 : PRO_AIM_SMOOTH * 0.88) * runtimeResponseMul;
          const aimSmooth = aimSmoothBase * (0.9 + userAimScale * 0.12);
          lane.aimX = lerp(lane.aimX || lane.centerX, px, aimSmooth);
          lane.aimY = lerp(lane.aimY || CH * 0.78, py, aimSmooth * 0.88);
          const handSign = lane.dominantHand === "left" ? -1 : 1;
          const forwardSign = -lane.side * handSign;
          const horizontalSensitivity = clamp(
            ShotTuning.horizontalSensitivity * (inputCal?.lateralMul || 1),
            0.35,
            2.4
          );
          const verticalSensitivity = clamp(
            ShotTuning.verticalSensitivity * (inputCal?.verticalMul || 1),
            0.35,
            2.4
          );
          const dragDX = lane.aimX - startX;
          const dragDY = lane.aimY - startY;
          const forwardDrag =
            dragDX * forwardSign * horizontalSensitivity - dragDY * 0.22 * verticalSensitivity;
          const powerDenom =
            ((casual ? 272 : 236) / userAimScale) *
            difficultyPowerDenomMul *
            (inputCal?.powerDenomMul || 1) *
            runtimeAssistMul;
          const rawPowerDrag = clamp(forwardDrag / powerDenom, -1, 1);
          const shapedPowerDrag =
            Math.sign(rawPowerDrag) * Math.pow(Math.abs(rawPowerDrag), 1.18);
          const powerDragNorm = clamp(
            shapedPowerDrag < 0 ? shapedPowerDrag * 1.34 : shapedPowerDrag * 1.06,
            -1,
            1
          );
          const pullTarget = clamp(
            -powerDragNorm + (dragDY * verticalSensitivity) / (casual ? 560 : 500),
            -1,
            1
          );
          lane.manualPull = lerp(
            lane.manualPull || 0,
            pullTarget,
            (casual ? 0.2 : 0.24) * clamp(controlTune.powerLerpMul, 0.62, 1.2)
          );
          const lateralDenom =
            ((casual ? 270 : 230) / userAimScale) * (inputCal?.lateralDenomMul || 1) * runtimeAssistMul;
          const lateralDrag = clamp((dragDX * handSign * horizontalSensitivity) / lateralDenom, -1, 1);
          const normX = lateralDrag;
          const shapedX = Math.sign(normX) * Math.pow(Math.abs(normX), 0.9);
          const aimSensitivity = clamp(controlTune.aimSensitivity, 0.62, 1.22);
          const targetAngle = clamp(
            tune.idealAngle + shapedX * (casual ? 13.2 : 15.2) * aimSensitivity * difficultyLateralAimMul,
            41,
            72
          );
          let targetLinear = clamp(
            basePower + powerDragNorm * (casual ? 0.76 : 0.82) * difficultyPowerDragMul,
            getDragPowerMin(),
            getDragPowerMax()
          );
          const damping =
            (casual ? 0.34 : 0.38) *
            (0.84 + userAimScale * 0.16) *
            clamp(controlTune.powerLerpMul, 0.62, 1.2);
          lane.angle = lerp(lane.angle, targetAngle, casual ? 0.22 : 0.18);
          lane.powerLinear = lerp(
            lane.powerLinear,
            targetLinear,
            damping * (inputCal?.powerResponseMul || 1)
          );
          lane.power = powerCurve(lane.powerLinear);
          lane.releaseWindow.center = clamp(
            0.67 + (1 - lane.powerLinear) * 0.08 * centerDriftMul,
            0.56,
            0.84
          );
        } else if (!TIMING_ONLY_SHOTS && vrMode && !lane.isAI) {
          const px = pointerRef.current.x || CW * 0.5;
          const handSign = lane.dominantHand === "left" ? -1 : 1;
          const targetAngle = clamp(53 + ((px - CW * 0.5) * handSign) / 7.2, 41, 71);
          lane.angle = lerp(lane.angle, targetAngle, 0.18);
        } else if (lane.isAI) {
          lane.angle += lane.angleDir * dt * 45;
          if (lane.angle > 68) {
            lane.angle = 68;
            lane.angleDir = -1;
          }
          if (lane.angle < 42) {
            lane.angle = 42;
            lane.angleDir = 1;
          }
        }
        if (!playerManualAim && lane.isAI) {
          lane.manualPull = lerp(lane.manualPull || 0, 0, 0.14);
          lane.releaseWindow.center = clamp(
            lane.releaseWindow.center + lane.angleDir * dt * 0.02,
            0.56,
            0.84
          );
        }
        const timingNow = getReleaseTimingData(
          lane.power,
          lane.releaseWindow?.center ?? 0.7,
          lane.releaseWindow?.width ?? 0.14
        );
        if (timingNow.isPerfect && !lane.wasPerfectWindow) {
          lane.landingPulse = 1;
          if (!lane.isAI && lane.aimLockCooldown <= 0) {
            SFX.aimLock();
            lane.aimLockCooldown = 0.11;
          }
        }
        lane.wasPerfectWindow = timingNow.isPerfect;
        lane.aimLockCooldown = Math.max(0, lane.aimLockCooldown - dt);

        lane.previewCalcCooldown -= dt;
        const previewKey = [
          Math.round(lane.power * 100),
          Math.round(lane.angle * 10),
          Math.round((lane.releaseWindow?.center ?? 0.7) * 100),
          Math.round((lane.releaseWindow?.width ?? 0.14) * 100),
          Math.round(hoopX(lane)),
          Math.round(getRimCenterY(lane)),
          lane.releaseType,
          lane.aimMode,
          lane.controlProfile,
        ].join("|");
        if (lane.previewCalcCooldown <= 0 || lane.previewKey !== previewKey || !lane.previewPath) {
          const nextPreview = predictShotPath(lane);
          const blend = controlTune.previewBlend;
          lane.previewPath = blendPreviewPath(lane.previewPath, nextPreview, blend);
          const previewArcMetrics = computePreviewArcMetrics(lane, lane.previewPath);
          if (previewArcMetrics) {
            lane.lastArcClearPx = previewArcMetrics.clearancePx;
            lane.lastArcApexY = previewArcMetrics.apexY;
            lane.lastArcRimPlaneY = previewArcMetrics.rimPlaneY;
          }
          lane.previewKey = previewKey;
          lane.previewCalcCooldown = lane.isAI ? 1 / 22 : 1 / 30;
        }

        lane.chargeToneCooldown -= dt;
        if (lane.chargeToneCooldown <= 0) {
          SFX.chargeRise(lane.power);
          lane.chargeToneCooldown = 0.14;
        }
        lane.previewTTL = 0.3;
      }

      if (lane.state === "flying") {
        const hx = hoopX(lane);
        const hy = getRimCenterY(lane);
        const baseStepCount = Math.max(
          1,
          Math.ceil((Math.hypot(lane.ball.vx || 0, lane.ball.vy || 0) * dt) / FLIGHT_SUBSTEP_PIXELS)
        );
        const flightSubsteps = Math.min(FLIGHT_MAX_SUBSTEPS, baseStepCount);
        const stepDt = dt / flightSubsteps;

        // 200ms micro follow-through input after release.
        if (lane.followWindow > 0) {
          lane.followWindow = Math.max(0, lane.followWindow - dt);
          if (lane.followSpinBoost > 0.001) {
            lane.ball.spin = wrapAngle(lane.ball.spin + lane.followSpinBoost * 26 * dt);
            lane.ball.vy -= lane.followSpinBoost * 120 * dt;
            lane.followSpinBoost *= Math.max(0, 1 - dt * 8.5);
          }
          if (lane.followArcAdjust > 0.01) {
            lane.ball.vy += lane.followArcAdjust * dt;
            lane.followArcAdjust *= Math.max(0, 1 - dt * 10.5);
          }
        } else if (!lane.isAI) {
          pointerRef.current.releaseTracking = false;
        }

        if (lane.rimFire || lane.ballSkin === "plasma" || lane.ballSkin === "prism") {
          lane.ball.hue = (lane.ball.hue + dt * (lane.rimFire ? 820 : 260)) % 360;
        }

        for (let substep = 0; substep < flightSubsteps; substep++) {
          lane.shotStep += stepDt;
          lane.prevX = lane.ball.x;
          lane.prevY = lane.ball.y;
          lane.prevVx = lane.ball.vx;
          lane.prevVy = lane.ball.vy;
          lane.ball.x += lane.ball.vx * stepDt;
          lane.ball.y += lane.ball.vy * stepDt;
          lane.ball.vy += getGravity() * stepDt;
          if (lane.dailyModifier === "wind") {
            lane.ball.vx += lane.windForce * stepDt;
          }
          const bypassTimingDamping =
            TIMING_ONLY_SHOTS && !lane.isAI && !lane.crossChecked;
          if (!bypassTimingDamping) {
            applyBallDamping(lane.ball, stepDt);
          }
          lane.ball.spin = wrapAngle(lane.ball.spin + stepDt * 12);
          const rimPlaneY = hy - lane.hoop.r * 0.22;
          if (lane.ball.y <= rimPlaneY) lane.clearedRimPlane = true;

          // Unstable release adds slight in-air wobble.
          if (lane.wobbleAmp > 0.01) {
            lane.wobblePhase += stepDt * (14 + (1 - lane.shotStability) * 10);
            const w = Math.sin(lane.wobblePhase) * lane.wobbleAmp * 26 * stepDt;
            lane.ball.x += w;
            lane.ball.vx += w * 3.4;
            lane.wobbleAmp *= Math.max(0, 1 - stepDt * 2.1);
          }

          lane.trail.push({ x: lane.ball.x, y: lane.ball.y, life: 1 });
          const trailMaxMul = clamp(
            Number.isFinite(VISUAL_TUNE.trailMaxMul) ? VISUAL_TUNE.trailMaxMul : 1,
            0.45,
            1
          );
          const trailMax = Math.max(8, Math.round(18 * trailMaxMul));
          if (lane.trail.length > trailMax) lane.trail.shift();

          lane.boardHitCooldown = Math.max(0, lane.boardHitCooldown - stepDt);
          if (!lane.shotResolved && lane.boardHitCooldown <= 0) {
            const boardHit = applyBackboardCollision(
              lane,
              lane.ball,
              lane.prevX ?? lane.ball.x,
              lane.prevY ?? lane.ball.y,
              lane.shotStability || 0.5
            );
            if (boardHit) {
              lane.hitBackboard = true;
              lane.boardHitCooldown = 0.08;
              lane.netWave = Math.max(lane.netWave, 0.45);
              lane.rimJolt = Math.max(lane.rimJolt, 0.25);
              lane.rimJoltPhase = Math.random() * Math.PI * 2;
              SFX.rim();
              spawnBurst(
                particlesRef.current,
                lane.ball.x,
                lane.ball.y,
                "#8ce6ff",
                8,
                180,
                fxRef.current.quality || 1
              );
            }
          }
          const timingTune = TIMING_DIFFICULTY_TUNING[lane.shotDifficulty] || TIMING_DIFFICULTY_TUNING.medium;
          const timingHuman = TIMING_ONLY_SHOTS && !lane.isAI;
          const timingBallR = getBallRadiusAtY(lane.ball.y) * 0.74;
          const timingTube = Math.max(lane.hoop.r * 0.44, lane.hoop.r - timingBallR * 0.66);
          const timingForceMakeTube =
            timingHuman &&
            lane.forceMake &&
            lane.clearedRimPlane &&
            lane.ball.vy > 42 &&
            Math.abs(lane.ball.x - hx) <= timingTube * timingTune.entryTubeMul * 1.08 &&
            lane.ball.y >= hy - lane.hoop.r * 0.24 &&
            lane.ball.y <= hy + lane.hoop.r * 0.98;
          const timingGraceTube =
            timingHuman &&
            lane.forceMake &&
            lane.clearedRimPlane &&
            lane.ball.vy > 26 &&
            Math.abs(lane.ball.x - hx) <= timingTube * timingTune.entryTubeMul * 1.42 &&
            lane.ball.y >= hy - lane.hoop.r * 0.42 &&
            lane.ball.y <= hy + lane.hoop.r * 1.08;

          lane.rimHitCooldown = Math.max(0, lane.rimHitCooldown - stepDt);
          const allowRimCollision =
            (!TIMING_ONLY_SHOTS || lane.isAI || lane.crossChecked) && !timingForceMakeTube && !timingGraceTube;
          if (allowRimCollision && !lane.shotResolved && lane.rimHitCooldown <= 0) {
            const rimY = hy;
            const rimRadius = lane.hoop.r;
            const ballR = getBallRadiusAtY(lane.ball.y) * 0.76;
            const dxR = lane.ball.x - hx;
            const dyR = lane.ball.y - rimY;
            const centerDist = Math.hypot(dxR, dyR);
            const ringDelta = Math.abs(centerDist - rimRadius);
            const inRimBand =
              ringDelta <= ballR * 0.92 + RIM_CYLINDER_THICKNESS &&
              lane.ball.y > rimY - rimRadius * 0.45 &&
              lane.ball.y < rimY + rimRadius * 0.72;
            const risingUnderRim =
              lane.ball.vy < -80 &&
              (lane.prevY ?? lane.ball.y) > rimY - rimRadius * 0.18 &&
              lane.ball.y > rimY - rimRadius * 0.28;
            const descendGate = !TIMING_ONLY_SHOTS || lane.isAI || lane.ball.vy > 14;
            if (inRimBand && !risingUnderRim && descendGate) {
              lane.rimTouched = true;
              if (Math.abs(dxR) > Math.abs(dyR) * 1.08) lane.rimHitType = "side";
              else if (dyR < 0) lane.rimHitType = "back";
              else lane.rimHitType = "front";
              const safeDist = Math.max(0.001, centerDist);
              const nx = dxR / safeDist;
              const ny = dyR / safeDist;
              const normalDot = lane.ball.vx * nx + lane.ball.vy * ny;
              if (normalDot < 0) {
                lane.ball.vx -= (1 + RIM_CYLINDER_RESTITUTION) * normalDot * nx;
                lane.ball.vy -= (1 + RIM_CYLINDER_RESTITUTION) * normalDot * ny;
              } else {
                lane.ball.vx += nx * 32 * stepDt;
                lane.ball.vy += ny * 26 * stepDt;
              }
              const kickMul = lane.rimHitType === "side" ? 1.08 : lane.rimHitType === "back" ? 0.92 : 1;
              const carryVx = lane.prevVx ?? lane.ball.vx;
              lane.ball.vx += nx * rand(12, 26) * kickMul;
              lane.ball.vy += ny * rand(10, 22) * kickMul;
              lane.ball.vx += (lane.sideSpin || 0) * 7.5;
              const tx = -ny;
              const ty = nx;
              const tangentSpeed = lane.ball.vx * tx + lane.ball.vy * ty;
              lane.ball.vx += tx * tangentSpeed * 0.09;
              lane.ball.vy += ty * tangentSpeed * 0.09;
              lane.ball.vx = lerp(lane.ball.vx, carryVx * 0.62, 0.2);
              lane.ball.spin += (lane.rimHitType === "side" ? 0.1 : 0.06) * (Math.sign(nx) || 1);
              lane.impactVibe = Math.max(lane.impactVibe || 0, 0.92);
              lane.rimJolt = Math.max(lane.rimJolt, 0.48);
              lane.rimJoltPhase = Math.random() * Math.PI * 2;
              kickNet(lane, 0.42, 0.2);
              SFX.rim();
              lane.rimHitCooldown = 0.05;
            }
          }

          // Smooth entry assist near rim for cleaner release feel.
          // Timing-only mode already solves launch to target; extra steering there can destabilize shots.
          const allowEntryAssist = !TIMING_ONLY_SHOTS || lane.isAI || timingTune.entryAssistMul > 0.01;
          if (lane.ball.vy > 40 && !lane.shotResolved && allowEntryAssist) {
            const toX = hx - lane.ball.x;
            const toY = hy - lane.ball.y;
            const rimAssistRadius = lane.hoop.r * 4.8;
            const rimDist = Math.hypot(toX, toY);
            if (rimDist < rimAssistRadius) {
              const timingWindow =
                TIMING_ONLY_SHOTS &&
                !lane.isAI &&
                Math.abs(lane.timingWobble || 0) <
                  lane.hoop.r * 2 * TIMING_SCORE_WINDOW_RATIO;
              const timingBoost = timingWindow ? 1.45 : 1;
              const settle =
                lane.releaseSmooth *
                clamp((rimAssistRadius - rimDist) / rimAssistRadius, 0, 1) *
                stepDt *
                timingBoost;
              const timingAssistMul = TIMING_ONLY_SHOTS && !lane.isAI ? timingTune.entryAssistMul : 1;
              const targetVx = toX * 1.42;
              const targetVy = lane.ball.vy + clamp(toY * 0.62, -170, 170);
              lane.ball.vx = lerp(lane.ball.vx, targetVx, settle * 0.38 * timingAssistMul);
              lane.ball.vy = lerp(lane.ball.vy, targetVy, settle * 0.28 * timingAssistMul);
            }
          }

          // Small inward correction when near the rim center keeps shots skill-based but not punishing.
          if (!lane.shotResolved && lane.ball.vy > 18) {
            const toX = hx - lane.ball.x;
            const toY = hy - lane.ball.y;
            const centerDist = Math.hypot(toX, toY);
            const toleranceMul = getDifficultyScalar(
              SHOT_FEEL_CONFIG.forgiveness.toleranceRadiusMul,
              lane.shotDifficulty,
              0.38
            );
            const centerPull = getDifficultyScalar(
              SHOT_FEEL_CONFIG.forgiveness.centerPull,
              lane.shotDifficulty,
              0.16
            );
            const toleranceRadius = lane.hoop.r * (1 + toleranceMul);
            if (
              centerDist <= toleranceRadius &&
              lane.ball.y >= hy - lane.hoop.r * 0.52 &&
              lane.ball.y <= hy + lane.hoop.r * 1.18
            ) {
              const inward = (1 - centerDist / Math.max(1, toleranceRadius)) * centerPull;
              lane.ball.vx = lerp(lane.ball.vx, lane.ball.vx + toX * 0.86, inward * stepDt * 8);
              lane.ball.vy = lerp(lane.ball.vy, lane.ball.vy + toY * 0.34, inward * stepDt * 6);
            }
          }

          if (timingForceMakeTube) {
            const makePlaneY = hy + lane.hoop.r * 0.08;
            const crossedMakePlane = (lane.prevY ?? lane.ball.y) < makePlaneY && lane.ball.y >= makePlaneY;
            if (crossedMakePlane || lane.ball.y > hy + lane.hoop.r * 0.86) {
              if (lane.forceSwish) lane.rimTouched = false;
              resolveShot(lane);
            }
          }

          if (!lane.crossChecked && lane.ball.vy > 0) {
            const entryArmY = hy - lane.hoop.r * 0.62;
            const crossedEntryArm = lane.prevY < entryArmY && lane.ball.y >= entryArmY;
            const nearRimApproach =
              pointSegmentDistance(hx, hy, lane.prevX, lane.prevY, lane.ball.x, lane.ball.y) <
              lane.hoop.r * 1.08;
            if (crossedEntryArm || nearRimApproach) lane.crossChecked = true;
          }
          if (lane.crossChecked && !lane.shotResolved && lane.ball.vy > 0) {
            const ballR = getBallRadiusAtY(lane.ball.y) * 0.74;
            const innerBase = Math.max(lane.hoop.r * 0.36, lane.hoop.r - ballR * 0.78);
            const innerWindow =
              TIMING_ONLY_SHOTS && !lane.isAI ? innerBase * timingTune.entryTubeMul * 1.12 : innerBase;
            const coreResolveY = hy + lane.hoop.r * 0.12;
            const insideTube =
              lane.clearedRimPlane &&
              Math.abs(lane.ball.x - hx) <= innerWindow &&
              lane.ball.y >= hy - lane.hoop.r * 0.18 &&
              lane.ball.y <= hy + lane.hoop.r * 0.78;
            const crossedCorePlane = lane.prevY < coreResolveY && lane.ball.y >= coreResolveY;
            const nearCorePath =
              pointSegmentDistance(hx, coreResolveY, lane.prevX, lane.prevY, lane.ball.x, lane.ball.y) <
              innerWindow * 0.62;
            const pastResolveDepth = lane.ball.y > hy + lane.hoop.r * 1.02;
            if ((insideTube && crossedCorePlane) || (insideTube && nearCorePath) || (insideTube && pastResolveDepth)) {
              resolveShot(lane);
            }
          }

          // Floor collision and bounce physics.
          const floorContactY = getBallGroundYAt(lane.ball.y);
          if (lane.ball.y >= floorContactY && lane.ball.vy > 0) {
            if (!lane.shotResolved) {
              lane.madeShot = false;
              lane.outcomeTier = "miss";
              lane.shotResolved = true;
              recordShotOutcome(lane, "miss");
              registerMiss(lane);
              lane.flash = nextCallout(lane, "miss", "BROKE SHOT");
              lane.flashColor = "#ff4f58";
              lane.flashAlpha = 1;
              SFX.miss();
              syncHud();
            }
            const impactSpeed = lane.ball.vy;
            const penetration = Math.max(0, lane.ball.y - floorContactY);
            lane.ball.y -= Math.min(penetration, FLOOR_PENETRATION_SLOP + impactSpeed * 0.0025);
            if (lane.ball.y > floorContactY) lane.ball.y = floorContactY;
            lane.floorBounces += 1;
            const impact = clamp(impactSpeed / 580, 0.12, 1);
            const floorSoftness = clamp(RimFeelTuning.floorBounceSoftness, 0.68, 1.42);
            const restitutionSoftMul = clamp(1 - (floorSoftness - 1) * 0.36, 0.76, 1.14);
            const frictionSoftMul = clamp(1 + (floorSoftness - 1) * 0.22, 0.84, 1.2);
            const restitutionBase = physicsTune.floorRestitution * (lane.madeShot ? 0.9 : 1);
            const bounceDamp = clamp(1 - impact * 0.1 - Math.min(0.12, lane.floorBounces * 0.03), 0.72, 0.96);
            const restitution = clamp(restitutionBase * bounceDamp * restitutionSoftMul, 0.22, 0.82);
            lane.ball.vy = -Math.max(72, impactSpeed * restitution);
            const spinToRoll = clamp(lane.ball.spin * 0.018, -120, 120);
            const floorGrip = clamp(
              physicsTune.floorFriction * (0.9 - impact * 0.14 + lane.floorBounces * 0.02) * frictionSoftMul,
              0.66,
              0.96
            );
            lane.ball.vx = (lane.ball.vx + spinToRoll) * floorGrip;
            lane.ball.vx += (lane.sideSpin || 0) * impact * 14;
            lane.ball.spin = lane.ball.spin * (0.74 + (1 - impact) * 0.12) + lane.ball.vx * 0.009;
            if (lane.floorBounces >= 2 && Math.abs(lane.ball.vy) < 120) {
              lane.ball.vy *= 0.78;
            }
            clampBallRestJitter(lane.ball, floorContactY);
            lane.ballSquash = Math.max(lane.ballSquash, impact);
            lane.impactVibe = Math.max(lane.impactVibe || 0, Math.min(1, impact));
            lane.settleTimer = Math.max(lane.settleTimer, 0.22);
            spawnBurst(
              particlesRef.current,
              lane.ball.x,
              FLOOR_Y + 2,
              lane.madeShot ? "#8deaff" : "#ff9f6f",
              lane.floorBounces === 1 ? 8 : 5,
              165,
              fxRef.current.quality || 1
            );
            SFX.floor(clamp(impact * physicsTune.floorSfxMul, 0.2, 1));
            if (impact > 0.78) SFX.rim();
            fxRef.current.shake = Math.max(
              fxRef.current.shake,
              (0.08 + impact * 0.2) * physicsTune.floorShakeMul
            );
          }

          if (lane.ball.y > CH + 90 || lane.ball.x < -120 || lane.ball.x > CW + 120) {
            if (!lane.shotResolved) {
              lane.madeShot = false;
              lane.shotResolved = true;
              recordShotOutcome(lane, "miss");
              registerMiss(lane);
              lane.flash = nextCallout(lane, "miss", "BROKE SHOT");
              lane.flashColor = "#ff4f58";
              lane.flashAlpha = 1;
              SFX.miss();
              syncHud();
            }
            toReturning(lane);
            return;
          }

          if (lane.shotResolved) {
            lane.settleTimer += stepDt;
            const lowMotion = Math.abs(lane.ball.vx) < 90 && Math.abs(lane.ball.vy) < 130;
            if (
              lane.floorBounces >= physicsTune.maxFloorBounces ||
              (lane.floorBounces >= 1 && lowMotion) ||
              lane.settleTimer > physicsTune.settleSeconds
            ) {
              toReturning(lane);
              return;
            }
          }
        }
      }

      if (lane.state === "returning") {
        const resetToIdleWithFreshBall = () => {
          lane.ball.x = lane.home.x;
          lane.ball.y = lane.home.y;
          lane.ball.vx = 0;
          lane.ball.vy = 0;
          lane.ball.spin = rand(-2, 2);
          lane.ball.hue =
            lane.rimFire || lane.ballSkin === "plasma" || lane.ballSkin === "prism"
              ? rand(0, 360)
              : getBallColorway(lane.ballColor).hue + rand(-6, 6);
          lane.state = "idle";
          lane.trail.length = 0;
          lane.followWindow = 0;
          lane.followSpinBoost = 0;
          lane.followArcAdjust = 0;
          lane.preShotLateral = 0;
          lane.dragAimX = 0;
          lane.dragAimY = 1;
          lane.dragDist = 0;
          lane.lastArcClearPx = 0;
          lane.lastArcApexY = 0;
          lane.lastArcRimPlaneY = 0;
          lane.releaseSmooth = 0.5;
      lane.previewPath = null;
      lane.previewCalcCooldown = 0;
      lane.previewKey = "";
      lane.rimHitCooldown = 0;
      lane.rimTouched = false;
      lane.rimHitType = "";
          lane.wobbleAmp = 0;
          lane.wasPerfectWindow = false;
          lane.shotResolved = false;
          lane.clearedRimPlane = false;
          lane.madeShot = false;
          lane.forceMake = false;
          lane.forceSwish = false;
          lane.telemetryAttemptLogged = false;
          lane.telemetryOutcomeLogged = false;
          lane.hitBackboard = false;
          lane.boardHitCooldown = 0;
          lane.rimHitCooldown = 0;
          lane.rimTouched = false;
          lane.rimHitType = "";
          lane.floorBounces = 0;
          lane.settleTimer = 0;
          lane.ballSquash = 0;
          lane.impactVibe = 0;
          lane.sideSpin = 0;
          lane.chargeElapsed = 0;
          lane.timingCharge = getDragPowerMin();
          lane.timingAccuracy = 0;
          lane.timingWobble = 0;
          lane.forceMake = false;
          lane.forceSwish = false;
          lane.bezierFlightTime = 0.82;
          lane.lanePulse = Math.max(0, (lane.lanePulse || 0) * 0.25);
          lane.outcomeTier = "idle";
          lane.recycleOut = false;
          lane.recycleTimer = 0;
          lane.releaseFlash = Math.max(lane.releaseFlash, 0.28);
          resetNetState(lane);
          spawnBurst(
            particlesRef.current,
            lane.home.x,
            lane.home.y + 4,
            "rgba(140,245,255,0.92)",
            8,
            170,
            fxRef.current.quality || 1
          );
        };

        if (lane.recycleOut) {
          lane.recycleTimer -= dt;
          const outSpeedX = 520 + Math.abs(lane.ball.vx) * 0.22;
          lane.ball.x += lane.side * outSpeedX * dt;
          lane.ball.y += Math.max(130, lane.ball.vy + 90) * dt;
          lane.ball.vy += getGravity() * 0.58 * dt;
          lane.ball.spin = wrapAngle(lane.ball.spin + dt * 12);
          if (
            lane.recycleTimer <= 0 ||
            lane.ball.x < -160 ||
            lane.ball.x > CW + 160 ||
            lane.ball.y > CH + 120
          ) {
            resetToIdleWithFreshBall();
          }
        } else {
          lane.returnTime -= dt;
          const returnBlend = 1 - Math.exp(-17 * dt);
          lane.ball.x = lerp(lane.ball.x, lane.home.x, returnBlend);
          lane.ball.y = lerp(lane.ball.y, lane.home.y, returnBlend);
          lane.ball.spin = wrapAngle(lane.ball.spin + dt * 5);
          if (lane.returnTime <= 0 || Math.hypot(lane.ball.x - lane.home.x, lane.ball.y - lane.home.y) < 2) {
            resetToIdleWithFreshBall();
          }
        }
      }

      if (lane.isAI) {
        if (lane.state === "idle") {
          lane.aiCooldown -= dt;
          if (lane.aiCooldown <= 0) {
            startCharging(lane);
            const clutchNow = timeLeftRef.current <= CLUTCH_SECONDS;
            if (lane.aiStyle === "sniper") lane.aiHold = rand(0.52, 0.92);
            else if (lane.aiStyle === "rhythm") lane.aiHold = rand(0.34, 0.62);
            else if (lane.aiStyle === "chaos") lane.aiHold = rand(0.15, 0.95);
            else lane.aiHold = clutchNow ? rand(0.18, 0.42) : rand(0.3, 0.72);
          }
        } else if (lane.state === "charging") {
          const clutchNow = timeLeftRef.current <= CLUTCH_SECONDS;
          lane.aiHold -= dt * (lane.aiStyle === "clutch" && clutchNow ? 1.35 : 1);
          const nearPerfect = Math.abs(lane.power - lane.releaseWindow.center) <= lane.releaseWindow.width * 0.36;
          const releaseChance =
            lane.aiStyle === "sniper"
              ? 0.32
              : lane.aiStyle === "rhythm"
                ? 0.24
                : lane.aiStyle === "chaos"
                  ? 0.14
                  : clutchNow
                    ? 0.4
                    : 0.2;
          if ((nearPerfect && Math.random() < releaseChance) || lane.aiHold <= 0) {
            releaseShot(lane);
            if (lane.aiStyle === "sniper") lane.aiCooldown = rand(0.78, 1.36);
            else if (lane.aiStyle === "rhythm") lane.aiCooldown = rand(0.62, 1.02);
            else if (lane.aiStyle === "chaos") lane.aiCooldown = rand(0.42, 1.22);
            else lane.aiCooldown = clutchNow ? rand(0.35, 0.84) : rand(0.6, 1.1);
          }
        }
      }

      lane.releaseBurst = Math.max(0, lane.releaseBurst - dt * 3.1);
      lane.releaseFlash = Math.max(0, lane.releaseFlash - dt * 8);
      lane.releaseCue = Math.max(0, lane.releaseCue - dt * 3.8);
      lane.releaseSpark = Math.max(0, lane.releaseSpark - dt * 5.3);
      lane.previewTTL = Math.max(0, lane.previewTTL - dt);
      if (lane.state === "charging") {
        lane.previewAlpha = Math.min(1, lane.previewAlpha + dt * 4.2);
      } else if (lane.previewTTL > 0.01) {
        lane.previewAlpha = Math.max(0, lane.previewAlpha - dt * 6.8);
      } else {
        lane.previewAlpha = 0;
      }
      lane.landingPulse = Math.max(0, lane.landingPulse - dt * 3.4);
      lane.netWave = Math.max(0, lane.netWave - dt * 1.6);
      updateNet(lane, dt);
      lane.rimJolt = Math.max(0, lane.rimJolt - dt * 3.2);
      lane.hoopPulse = Math.max(0.34, lane.hoopPulse - dt * 0.85);
      lane.lanePulse = Math.max(0, (lane.lanePulse || 0) - dt * 1.22);
      lane.scorePop = Math.max(0, lane.scorePop - dt * 1.7);
      lane.comboPop = Math.max(0, lane.comboPop - dt * 1.2);
      lane.flashAlpha = Math.max(0, lane.flashAlpha - dt * 1.25);
      lane.wristSnap = Math.max(0, lane.wristSnap - dt * 4.0);
      lane.guideRelease = Math.max(0, lane.guideRelease - dt * 2.3);
      lane.followHold = Math.max(0, lane.followHold - dt * 0.58);
      lane.ballSquash = Math.max(0, lane.ballSquash - dt * 8.4);
      lane.impactVibe = Math.max(0, (lane.impactVibe || 0) - dt * 11.2);
      lane.sideSpin = (lane.sideSpin || 0) * Math.max(0, 1 - dt * 1.35);
      if (lane.state !== "charging") {
        lane.dynamicAssist = Math.max(0, lane.dynamicAssist - dt * 0.006);
      }
      lane.breathPhase += dt * 1.4;
      lane.handJitter = lane.state === "charging" ? clamp((lane.power - 0.7) * 2.8, 0, 1) : 0;
      for (let i = lane.trail.length - 1; i >= 0; i--) {
        const trailFadeMul = clamp(
          Number.isFinite(VISUAL_TUNE.trailFadeMul) ? VISUAL_TUNE.trailFadeMul : 1,
          0.85,
          1.5
        );
        lane.trail[i].life -= dt * 2.3 * trailFadeMul;
        if (lane.trail[i].life <= 0.02) lane.trail.splice(i, 1);
      }

      if (lane.rimFire) {
        lane.fireSpawnTimer -= dt;
        if (lane.fireSpawnTimer <= 0) {
          const quality = fxRef.current.quality || 1;
          spawnFire(particlesRef.current, hoopX(lane), getRimCenterY(lane) - 4, quality);
          const fireSpawnRateMul = clamp(
            Number.isFinite(VISUAL_TUNE.fireSpawnRateMul) ? VISUAL_TUNE.fireSpawnRateMul : 1,
            0.9,
            1.6
          );
          lane.fireSpawnTimer = Math.max(0.015, (0.03 / Math.max(0.72, quality)) * fireSpawnRateMul);
        }
      } else {
        lane.fireSpawnTimer = 0;
      }
    };

    const updateParticles = (dt) => {
      const arr = particlesRef.current;
      const fx = fxRef.current;
      const particleBudgetMul = clamp(
        Number.isFinite(VISUAL_TUNE.particleBudgetMul) ? VISUAL_TUNE.particleBudgetMul : 1,
        0.45,
        1
      );
      const budgetMul = clamp(
        0.45 + (fx.quality || 1) * 0.55 - (fx.lowLatencyBoost || 0) * 0.14,
        0.28,
        1
      );
      const baseParticleCap = Math.max(18, Math.round(MAX_PARTICLES * particleBudgetMul));
      const maxParticles = Math.max(14, Math.round(baseParticleCap * budgetMul));
      if (arr.length > maxParticles) arr.splice(0, arr.length - maxParticles);
      for (let i = arr.length - 1; i >= 0; i--) {
        const p = arr[i];
        p.x += p.vx * dt;
        p.y += p.vy * dt;
        p.vy += 620 * dt;
        p.life -= dt * 1.7;
        p.size *= p.shrink;
        if (p.life <= 0.01 || p.size < 0.22) arr.splice(i, 1);
      }
    };
    const updatePhysicsTick = (simDt) => {
      const lanes = lanesRef.current;
      for (const lane of lanes) updateLane(lane, simDt);
      updateParticles(simDt);
    };

    const frame = (now) => {
      const frameDt = computeFrameDelta(now, last, 0.08);
      last = now;
      const fx = fxRef.current;
      const heavyFrame = frameDt > 0.024;
      fx.heavyFrames = heavyFrame
        ? Math.min(12, (fx.heavyFrames || 0) + 1)
        : Math.max(0, (fx.heavyFrames || 0) - 1.35);
      const targetLowLatency = fx.heavyFrames >= 3 ? 1 : 0;
      fx.lowLatencyBoost = lerp(fx.lowLatencyBoost || 0, targetLowLatency, 0.2);
      const qualityFloor = clamp(
        Number.isFinite(VISUAL_TUNE.qualityFloor) ? VISUAL_TUNE.qualityFloor : 0.48,
        0.35,
        0.9
      );
      const qualityCeil = clamp(
        Number.isFinite(VISUAL_TUNE.qualityCeil) ? VISUAL_TUNE.qualityCeil : 1,
        qualityFloor,
        1
      );
      const lowQualityTarget = lerp(
        Math.min(qualityCeil, 0.62),
        qualityFloor,
        clamp(fx.lowLatencyBoost || 0, 0, 1)
      );
      fx.quality = clamp(
        computeAdaptiveQuality(fx.quality, frameDt, 0.016, lowQualityTarget, 0.24),
        qualityFloor,
        qualityCeil
      );
      if ((fx.slowMoTime || 0) > 0) {
        fx.slowMoTime = Math.max(0, (fx.slowMoTime || 0) - frameDt);
      }
      const slowMoScale = (fx.slowMoTime || 0) > 0 ? SWISH_SLOW_MO_SCALE : 1;
      const effectiveGameSpeed = GAME_SPEED * slowMoScale;
      const scaledFrameDt = frameDt * effectiveGameSpeed;
      const backlogBefore = accumulator + scaledFrameDt;
      const stepped = stepFixedAccumulator(
        accumulator,
        frameDt,
        effectiveGameSpeed,
        SIM_DT,
        MAX_SUBSTEPS
      );
      accumulator = stepped.accumulator;
      let simSteps = stepped.steps > 0 ? stepped.steps : frameDt > 0 ? 1 : 0;
      simSteps = Math.min(simSteps, MAX_SUBSTEPS);
      let simStepDt =
        stepped.steps > 0 ? stepped.simDt : Math.min(1 / 55, Math.max(1 / 240, scaledFrameDt));
      if (stepped.steps >= MAX_SUBSTEPS && backlogBefore > stepped.simDt * MAX_SUBSTEPS) {
        simSteps = MAX_SUBSTEPS;
        simStepDt = Math.min(1 / 55, backlogBefore / MAX_SUBSTEPS);
        accumulator = 0;
      }
      const clutchActive = timeLeftRef.current <= CLUTCH_SECONDS;

      for (let i = 0; i < simSteps; i++) {
        updatePhysicsTick(simStepDt);
      }
      const lanes = lanesRef.current;
      fx.renderAlpha = clamp(accumulator / SIM_DT, 0, 1);
      for (const lane of lanes) {
        const alpha = lane.state === "flying" ? fx.renderAlpha : 1;
        lane.renderX = lerp(lane.prevX ?? lane.ball.x, lane.ball.x, alpha);
        lane.renderY = lerp(lane.prevY ?? lane.ball.y, lane.ball.y, alpha);
        lane.renderVx = lerp(lane.prevVx ?? lane.ball.vx, lane.ball.vx, alpha);
        lane.renderVy = lerp(lane.prevVy ?? lane.ball.vy, lane.ball.vy, alpha);
      }

      const [player, opponent] = lanes;
      const chargeAmount =
        ((player?.state === "charging" ? player.power : 0) +
          (opponent?.state === "charging" ? opponent.power : 0)) /
        2;
      fx.chargeGlow = lerp(fx.chargeGlow, chargeAmount, 0.08);
      const focusLane = lanes.find((l) => l.state === "flying");
      const focusX = focusLane ? (focusLane.renderX ?? focusLane.ball.x) - CW * 0.5 : 0;
      const focusY = focusLane ? (focusLane.renderY ?? focusLane.ball.y) - CH * 0.42 : 0;
      const airParallax =
        focusLane && focusLane.state === "flying"
          ? clamp((FLOOR_Y - (focusLane.renderY ?? focusLane.ball.y)) / 420, 0, 1)
          : 0;
      const camFollowLerp = clamp(SHOT_FEEL_CONFIG.camera.followLerp, 0.05, 0.2);
      const arcFollowBoost = clamp(SHOT_FEEL_CONFIG.camera.arcFollowBoost, 0, 0.08);
      fx.camX = lerp(fx.camX, -focusX * (0.082 + arcFollowBoost * 0.25), camFollowLerp);
      fx.camY = lerp(
        fx.camY,
        -focusY * (0.058 + arcFollowBoost * 0.22) - airParallax * (7 + arcFollowBoost * 64) + (clutchActive ? -10 : 0),
        camFollowLerp
      );
      fx.clutchPulse = lerp(fx.clutchPulse, clutchActive ? 0.65 + Math.sin(now * 0.015) * 0.2 : 0, 0.1);
      fx.zoom = lerp(
        fx.zoom,
        1 + fx.chargeGlow * 0.038 + fx.clutchPulse * 0.02 + (focusLane ? 0.012 : 0),
        0.12
      );
      const releaseTilt =
        ((player?.releaseBurst || 0) - (opponent?.releaseBurst || 0)) * 0.01;
      fx.tilt = lerp(fx.tilt, releaseTilt, 0.12);
      fx.parallax = 0;
      fx.shake = Math.max(0, fx.shake - frameDt * 2.2);
      fx.shakeX = rand(-1, 1) * fx.shake * 6;
      fx.shakeY = rand(-1, 1) * fx.shake * 4;
      fx.followRoll = lerp(fx.followRoll, 0, 0.1);
      fx.followPitch = lerp(fx.followPitch, 0, 0.1);
      fx.followDriftX = lerp(fx.followDriftX, 0, 0.08);
      fx.followDriftY = lerp(fx.followDriftY, 0, 0.08);
      fx.shotRush = lerp(fx.shotRush, 0, 0.11);
      const headBob = vrMode ? Math.sin(now * 0.0018 + (player?.breathPhase || 0)) * 2.2 : 0;

      let shotCamX = fx.followDriftX;
      let shotCamY = fx.followDriftY;
      let shotCamRoll = fx.followRoll;
      const fovZoomBase = clamp(60 / Math.max(40, CAMERA_FOV_DEG), 0.9, 1.1);
      let shotCamZoom = 1.01 * fovZoomBase + fx.followPitch * 0.72;
      if (focusLane && focusLane.state === "flying") {
        const hx = hoopX(focusLane);
        const hy = hoopY(focusLane);
        const focusBallX = focusLane.renderX ?? focusLane.ball.x;
        const focusBallY = focusLane.renderY ?? focusLane.ball.y;
        const toRimX = (hx - focusBallX) * 0.03;
        const toRimY = (hy - focusBallY) * 0.028;
        const rush = clamp(fx.shotRush, 0, 1);
        shotCamX += toRimX * rush;
        shotCamY += toRimY * rush;
        shotCamRoll += clamp((focusLane.ball.vx || 0) / 24000, -0.004, 0.004) * rush;
        shotCamZoom += rush * 0.018;
      }

      ctx.save();
      ctx.translate(
        CW * 0.5 + fx.shakeX + fx.camX + shotCamX,
        CH * 0.5 + fx.shakeY + headBob + fx.camY + shotCamY + CAMERA_VERTICAL_OFFSET
      );
      ctx.rotate((fx.tilt + shotCamRoll) * 0.12);
      ctx.scale(fx.zoom * shotCamZoom * 1.03, fx.zoom * (0.97 - fx.followPitch * 0.1));
      ctx.translate(-CW * 0.5, -CH * 0.5);
      ctx.translate(CW * 0.5, CH * 0.5);
      ctx.transform(
        1,
        CAMERA_PERSPECTIVE_SHEAR,
        0,
        CAMERA_PERSPECTIVE_SCALE_Y,
        0,
        0
      );
      ctx.translate(-CW * 0.5, -CH * 0.5);
      drawBackground(ctx, now, fx.parallax, fx.chargeGlow + fx.clutchPulse * 0.45);

      if (player && opponent) {
        const playerTurnPulse =
          player.state === "idle" || player.state === "charging"
            ? 0.35 + Math.sin(now * 0.01) * 0.2
            : 0;
        const oppTurnPulse =
          opponent.state === "idle" || opponent.state === "charging"
            ? 0.25 + Math.sin(now * 0.009 + 0.6) * 0.15
            : 0;
        drawLaneSurface(ctx, player, now, playerTurnPulse, fx.chargeGlow, fx.quality);
        drawLaneSurface(ctx, opponent, now, oppTurnPulse, fx.chargeGlow * 0.8, fx.quality);
        drawArcadeRig(ctx, player, opponent, now);

        drawBackboardAndHoop(ctx, player, now, fx.quality);
        drawBackboardAndHoop(ctx, opponent, now, fx.quality);

        drawTrajectoryGuide(ctx, player, fx.quality);
        drawTrajectoryGuide(ctx, opponent, fx.quality);
        if (showDevPanel && debugArcOverlay) {
          drawArcDebugOverlay(ctx, player);
          drawArcDebugOverlay(ctx, opponent);
        }
        drawShotAssist(ctx, player, now, fx.quality);
        drawShotAssist(ctx, opponent, now, fx.quality);

        drawReleaseCue(ctx, player);
        drawReleaseCue(ctx, opponent);

        drawArcTrail(ctx, player);
        drawArcTrail(ctx, opponent);

        drawBallShadow(ctx, player);
        drawBallShadow(ctx, opponent);

        drawBall(ctx, player, fx.quality);
        drawBall(ctx, opponent, fx.quality);
        drawRimFrontOverlay(ctx, player);
        drawRimFrontOverlay(ctx, opponent);

        drawParticles(ctx, particlesRef.current);

        drawFlashText(ctx, player);
        drawFlashText(ctx, opponent);
      } else if (player) {
        const turnPulse =
          player.state === "idle" || player.state === "charging"
            ? 0.42 + Math.sin(now * 0.01) * 0.22
            : 0.12;
        drawLaneSurface(ctx, player, now, turnPulse, fx.chargeGlow, fx.quality);
        drawBackboardAndHoop(ctx, player, now, fx.quality);
        drawTrajectoryGuide(ctx, player, fx.quality);
        if (showDevPanel && debugArcOverlay) {
          drawArcDebugOverlay(ctx, player);
        }
        drawShotAssist(ctx, player, now, fx.quality);
        drawArcTrail(ctx, player);
        drawBallShadow(ctx, player);
        drawBall(ctx, player, fx.quality);
        drawRimFrontOverlay(ctx, player);
        drawReleaseCue(ctx, player);
        drawParticles(ctx, particlesRef.current);
        drawFlashText(ctx, player);
      }
      if (clutchActive) {
        const pulseA = 0.08 + fx.clutchPulse * 0.12;
        const edge = ctx.createLinearGradient(0, 0, 0, CH);
        edge.addColorStop(0, `rgba(255,90,70,${pulseA})`);
        edge.addColorStop(0.28, "transparent");
        edge.addColorStop(0.72, "transparent");
        edge.addColorStop(1, `rgba(255,90,70,${pulseA * 0.9})`);
        ctx.fillStyle = edge;
        ctx.fillRect(0, 0, CW, CH);
      }
      ctx.restore();

      if (showDevPanel && player && opponent) {
        drawPressureBox(ctx, player, 12, 8, now);
        drawPressureBox(ctx, opponent, CW - 190, 8, now);
      } else if (showDevPanel && player && !vrMode) {
        drawPressureBox(ctx, player, Math.round(CW * 0.5 - 89), 8, now);
      }
      if (showDevPanel && debugArcOverlay) {
        ctx.save();
        ctx.font = '700 10px "Orbitron", sans-serif';
        ctx.fillStyle = "rgba(130,245,255,0.9)";
        ctx.textAlign = "left";
        ctx.fillText("ARC DEBUG [G] TOGGLE", 16, CH - 14);
        ctx.restore();
      }

      const vignette = ctx.createRadialGradient(CW * 0.5, CH * 0.5, CH * 0.28, CW * 0.5, CH * 0.5, CH * 0.86);
      vignette.addColorStop(0, "transparent");
      vignette.addColorStop(1, "rgba(0,0,0,0.58)");
      ctx.fillStyle = vignette;
      ctx.fillRect(0, 0, CW, CH);

      rafRef.current = requestAnimationFrame(frame);
    };

    rafRef.current = requestAnimationFrame(frame);

    return () => {
      cancelAnimationFrame(rafRef.current);
      canvas.removeEventListener("pointerdown", pointerDown);
      window.removeEventListener("pointermove", pointerMove);
      window.removeEventListener("pointerup", pointerUp);
      window.removeEventListener("pointercancel", pointerCancel);
      window.removeEventListener("blur", onWindowBlur);
      window.removeEventListener("keydown", keyDown);
      window.removeEventListener("keyup", keyUp);
      keyHeldRef.current = false;
      pointerRef.current.releaseTracking = false;
      pointerRef.current.active = false;
      pointerRef.current.pointerType = "touch";
      pointerRef.current.moveAvgMs = 16;
      pointerRef.current.jitterAvg = 0;
    };
  }, [
    debugArcOverlay,
    recordShotOutcome,
    registerMiss,
    playUiTap,
    releaseShot,
    resolveShot,
    screen,
    startCharging,
    syncHud,
    toReturning,
    movingRims,
    oneViewMode,
    physicsProfile,
    practiceMode,
    vrMode,
  ]);

  const livePlayer = lanesRef.current[0];
  const liveOpponent = lanesRef.current[1];
  const singleLaneSession = vrMode || oneViewMode || practiceMode || !liveOpponent || finalState?.singleLaneSession;
  const winner = singleLaneSession
    ? (finalState?.playerScore ?? hud.playerScore) >= 38
      ? "PRO PERFORMANCE"
      : (finalState?.playerScore ?? hud.playerScore) >= 22
        ? "SOLID SESSION"
        : "KEEP GRINDING"
    : finalState && finalState.playerScore !== finalState.opponentScore
      ? finalState.playerScore > finalState.opponentScore
        ? "YOU WIN"
        : "OPPONENT WINS"
      : "TIE GAME";
  const timerRatio = clamp((matchLength > 0 ? timeLeft / matchLength : 1), 0, 1);
  const clutchActive = !practiceMode && timeLeft <= CLUTCH_SECONDS;
  const timerRing = `conic-gradient(${clutchActive ? "#ff4f54" : "#41e5ff"} ${timerRatio * 360}deg, rgba(255,255,255,0.12) 0)`;
  const playerStreakMeter = clamp((hud.playerStreak % 5) / 5, 0, 1);
  const oppStreakMeter = clamp((hud.opponentStreak % 5) / 5, 0, 1);
  const playerXp = ladderPoints + (finalState?.playerScore ?? hud.playerScore);
  const playerTierProgress = computeTierProgress(playerXp);
  const playerRank = playerTierProgress.tier.label;
  const playerRankColor = playerTierProgress.tier.color;
  const playerRankTrail = playerTierProgress.tier.trail;
  const oppTierProgress = computeTierProgress((finalState?.opponentScore ?? hud.opponentScore) * 4);
  const oppRank = oppTierProgress.tier.label;
  const oppRankColor = oppTierProgress.tier.color;
  const comboMultiplier = Math.max(1, 1 + Math.floor((hud.playerStreak || 0) / 3));
  const comboScale = 1 + clamp((livePlayer?.comboPop || 0) * 0.22, 0, 0.34);
  const playerPressure = clamp(livePlayer?.power ?? 0, 0, 1);
  const opponentPressure = clamp(liveOpponent?.power ?? 0, 0, 1);
  const playerPressureState =
    livePlayer?.state === "charging"
      ? "CHARGING"
      : livePlayer?.state === "flying"
        ? "RELEASED"
        : livePlayer?.state === "returning"
          ? "RELOAD"
          : "READY";
  const opponentPressureState =
    liveOpponent?.state === "charging"
      ? "CHARGING"
      : liveOpponent?.state === "flying"
        ? "RELEASED"
        : liveOpponent?.state === "returning"
          ? "RELOAD"
          : "READY";
  const safeScreen = screen === "menu" || screen === "game" || screen === "gameover" ? screen : "menu";
  const viewportW = Math.max(280, viewport.width || CW);
  const viewportH = Math.max(280, viewport.height || CH);
  const viewportPortrait = viewportH > viewportW;
  const compactViewport = viewportW < 980 || viewportH < 740;
  const mobileViewport = viewportW < 760 || (viewportPortrait && viewportW < 980);
  const dynamicHudScale = vrMode
    ? 1
    : clamp(HUD_COMPACT_SCALE * (mobileViewport ? 0.92 : compactViewport ? 1 : 1.08), 0.28, 0.42);
  const dynamicHudRatio = vrMode ? 0 : clamp(mobileViewport ? 0.115 : compactViewport ? 0.095 : 0.082, 0.075, 0.13);
  const dynamicCourtRatio = vrMode ? 1 : 1 - dynamicHudRatio;
  const dynamicHudCenterMinWidth = clamp(
    Math.round(HUD_CENTER_PANEL_MIN_WIDTH * (mobileViewport ? 0.92 : 1)),
    88,
    168
  );
  const dynamicHudTimerRingSize = clamp(
    Math.round(HUD_TIMER_RING_SIZE * (mobileViewport ? 0.8 : compactViewport ? 0.9 : 1)),
    18,
    36
  );
  const dynamicHudTimerFontSize = clamp(
    Math.round(HUD_TIMER_FONT_SIZE * (mobileViewport ? 0.82 : compactViewport ? 0.9 : 1)),
    10,
    24
  );
  const dynamicHudQuitFontSize = clamp(
    Math.round(HUD_QUIT_FONT_SIZE * (mobileViewport ? 0.86 : 1)),
    8,
    14
  );
  const dynamicHudQuitPadY = clamp(
    Math.round(HUD_QUIT_PAD_Y * (mobileViewport ? 0.78 : 0.9)),
    2,
    8
  );
  const dynamicHudQuitPadX = clamp(
    Math.round(HUD_QUIT_PAD_X * (mobileViewport ? 0.8 : 0.92)),
    4,
    10
  );
  const dynamicHudColumns = mobileViewport ? "minmax(0, 1fr) auto minmax(0, 1fr)" : "1fr auto 1fr";
  const gameFrameHeightPx = safeScreen === "game" ? viewportH : Math.min(viewportH * 0.98, 1040);
  const hudPixelHeight = vrMode ? 0 : Math.round(gameFrameHeightPx * dynamicHudRatio);
  const courtAvailableHeight = Math.max(220, gameFrameHeightPx - hudPixelHeight);
  const courtAvailableWidth = Math.max(320, viewportW);
  const fitWidthByHeight = courtAvailableHeight * COURT_ASPECT;
  const courtViewportWidth = Math.round(Math.min(courtAvailableWidth, fitWidthByHeight));
  const courtViewportHeight = Math.round(courtViewportWidth / COURT_ASPECT);
  const normalizedBackendLeaderboard =
    Array.isArray(backendLeaderboard)
      ? backendLeaderboard
          .filter((row) => row && typeof row === "object")
          .map((row, index) => ({
            rank: Math.max(1, Number.isFinite(row.rank) ? Math.round(row.rank) : index + 1),
            name: String(row.name || "PLAYER").slice(0, 24),
            bestScore: Math.max(0, Number.isFinite(row.bestScore) ? Math.round(row.bestScore) : 0),
          }))
      : [];
  const normalizedRecentMatches =
    Array.isArray(recentMatches)
      ? recentMatches
          .filter((row) => row && typeof row === "object")
          .map((row, index) => ({
            matchId: String(row.matchId || `match-${index}`),
            playerName: String(row.playerName || "PLAYER").slice(0, 24),
            mode: String(row.mode || "solo"),
            score: Math.max(0, Number.isFinite(row.score) ? Math.round(row.score) : 0),
          }))
      : [];
  const showHudDebug = false;
  const unlockPlasma = bestScore >= 18 || achievements.releaseArtist;
  const unlockPrism = bestScore >= 36 || (achievements.releaseArtist && achievements.clutchFinisher);
  const unlockCarbon = bestScore >= 22 || achievements.cleanShooter;
  const unlockSunset = bestScore >= 42 || (achievements.cleanShooter && achievements.clutchFinisher);
  const finalStats = finalState?.playerStats || null;
  const finalAttempts = finalStats?.attempts || 0;
  const finalAccuracy = finalAttempts > 0 ? Math.round(((finalStats?.makes || 0) / finalAttempts) * 100) : 0;
  const finalPerfectRate = finalAttempts > 0 ? Math.round(((finalStats?.perfectReleases || 0) / finalAttempts) * 100) : 0;
  const finalStability = finalAttempts > 0 ? Math.round(((finalStats?.totalStability || 0) / finalAttempts) * 100) : 0;
  const currentDifficulty = DIFFICULTY_TUNING[shotDifficulty] || DIFFICULTY_TUNING.medium;
  const currentAimMode = AIM_MODES[aimMode] || AIM_MODES.casual;
  const currentControl = CONTROL_PROFILES[controlProfile] || CONTROL_PROFILES.rookie;
  const currentPhysicsProfileId = resolvePhysicsProfile(physicsProfile);
  const currentPhysics = PHYSICS_PROFILES[currentPhysicsProfileId] || PHYSICS_PROFILES.arcade;
  const currentShotTuningLabel = SHOT_TUNING_LABELS[shotTuningPreset] || SHOT_TUNING_LABELS.casual;
  const currentVisualTuningLabel =
    VISUAL_TUNING_LABELS[visualTuningPreset] || VISUAL_TUNING_LABELS.neon_premium;
  const easyPlusActive = shotDifficulty === "easy" && easyPlusAssist;
  const menuLeaderboardRows =
    normalizedBackendLeaderboard.length > 0
      ? normalizedBackendLeaderboard
      : buildOfflineLeaderboard({ playerName, bestScore, ladderPoints });
  const menuRecentRows =
    normalizedRecentMatches.length > 0
      ? normalizedRecentMatches
      : buildOfflineRecentMatches({ playerName, lastScore: bestScore });
  const shotTelemetrySafe = normalizeShotTelemetry(shotTelemetry);
  const telemetryRows = SHOT_DIFFICULTY_IDS.map((id) => {
    const bucket = shotTelemetrySafe[id] || createEmptyDifficultyTelemetry();
    const attempts = Math.max(0, Number(bucket.attempts) || 0);
    const makes = Math.max(0, Number(bucket.makes) || 0);
    const swishes = Math.max(0, Number(bucket.swishes) || 0);
    const rimOuts = Math.max(0, Number(bucket.rimOuts) || 0);
    const misses = Math.max(0, Number(bucket.misses) || 0);
    const makeRate = attempts > 0 ? Math.round((makes / attempts) * 100) : 0;
    const swishRate = makes > 0 ? Math.round((swishes / makes) * 100) : 0;
    return {
      id,
      label: (DIFFICULTY_TUNING[id] || DIFFICULTY_TUNING.medium).label,
      color: (DIFFICULTY_TUNING[id] || DIFFICULTY_TUNING.medium).color,
      attempts,
      makes,
      swishes,
      rimOuts,
      misses,
      makeRate,
      swishRate,
    };
  });
  const currentTelemetry = telemetryRows.find((row) => row.id === shotDifficulty) || telemetryRows[1];
  const telemetryUpdatedLabel = shotTelemetrySafe.updatedAt
    ? new Date(shotTelemetrySafe.updatedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })
    : "N/A";
  const telemetryAutoTune = (() => {
    const attempts = currentTelemetry?.attempts || 0;
    const makeRate = currentTelemetry?.makeRate || 0;
    const swishRate = currentTelemetry?.swishRate || 0;
    const rimOutRate = attempts > 0 ? (currentTelemetry?.rimOuts || 0) / attempts : 0;
    const missRate = attempts > 0 ? (currentTelemetry?.misses || 0) / attempts : 0;
    const patch = {};
    const reasons = [];
    const addNumericPatch = (key, nextValue, currentValue) => {
      if (!Number.isFinite(nextValue) || !Number.isFinite(currentValue)) return;
      if (Math.abs(nextValue - currentValue) >= 0.01) {
        patch[key] = Number(nextValue.toFixed(3));
      }
    };

    if (attempts < AUTO_TUNE_MIN_ATTEMPTS) {
      return {
        ready: false,
        severity: "info",
        title: "Collect More Shot Data",
        summary: `Need ${AUTO_TUNE_MIN_ATTEMPTS - attempts} more attempts on ${currentTelemetry.label}.`,
        reasons: ["Auto-tune activates after enough attempts so it does not overfit."],
        patch,
        stats: { attempts, makeRate, swishRate, rimOutRate, missRate },
      };
    }

    if (makeRate < 38) {
      addNumericPatch(
        "releaseForgiveness",
        clampReleaseForgiveness(releaseForgiveness + 0.06),
        releaseForgiveness
      );
      addNumericPatch(
        "lateralForgiveness",
        clampLateralForgiveness(lateralForgiveness + 0.06),
        lateralForgiveness
      );
      addNumericPatch("arcHeightTuning", clampArcHeightTune(arcHeightTuning + 0.04), arcHeightTuning);
      addNumericPatch("aimSensitivityScale", clamp(aimSensitivityScale * 0.97, 0.7, 1.4), aimSensitivityScale);
      if (shotDifficulty === "easy" && !easyPlusAssist) patch.easyPlusAssist = true;
      if (makeRate < 30 && aimMode !== "casual") patch.aimMode = "casual";
      if (makeRate < 24 && controlProfile !== "rookie") patch.controlProfile = "rookie";
      reasons.push("Hit rate is low; widening release and lateral forgiveness.");
      reasons.push("Adding arc lift so shots clear the front rim more often.");
    } else if (rimOutRate > 0.22) {
      addNumericPatch("arcHeightTuning", clampArcHeightTune(arcHeightTuning + 0.05), arcHeightTuning);
      addNumericPatch(
        "releaseForgiveness",
        clampReleaseForgiveness(releaseForgiveness + 0.05),
        releaseForgiveness
      );
      addNumericPatch("aimSensitivityScale", clamp(aimSensitivityScale * 0.98, 0.7, 1.4), aimSensitivityScale);
      reasons.push("Rim-out rate is high; biasing higher arc and cleaner entry angle.");
    } else if (makeRate > 78 && swishRate > 48 && attempts >= 16) {
      addNumericPatch(
        "releaseForgiveness",
        clampReleaseForgiveness(releaseForgiveness - 0.04),
        releaseForgiveness
      );
      addNumericPatch(
        "lateralForgiveness",
        clampLateralForgiveness(lateralForgiveness - 0.04),
        lateralForgiveness
      );
      addNumericPatch("aimSensitivityScale", clamp(aimSensitivityScale * 1.03, 0.7, 1.4), aimSensitivityScale);
      if (controlProfile === "rookie") patch.controlProfile = "smooth";
      reasons.push("You are highly accurate; tightening assist for higher ceiling control.");
    } else {
      reasons.push("Current tune is balanced for your latest shot profile.");
    }

    const patchKeys = Object.keys(patch);
    return {
      ready: true,
      severity: patchKeys.length > 0 ? "action" : "stable",
      title: patchKeys.length > 0 ? "Auto Tune Recommendation" : "Tune Is Stable",
      summary:
        patchKeys.length > 0
          ? `Based on ${attempts} ${currentTelemetry.label} attempts, apply ${patchKeys.length} tune updates.`
          : `No tuning change needed from ${attempts} ${currentTelemetry.label} attempts.`,
      reasons,
      patch,
      stats: { attempts, makeRate, swishRate, rimOutRate, missRate },
    };
  })();
  const applyTelemetryRecommendation = () => {
    const patch = telemetryAutoTune?.patch || {};
    const keys = Object.keys(patch);
    if (keys.length === 0) return;
    const nextArc = Number.isFinite(patch.arcHeightTuning)
      ? clampArcHeightTune(patch.arcHeightTuning)
      : arcHeightTuningRef.current;
    const nextLateral = Number.isFinite(patch.lateralForgiveness)
      ? clampLateralForgiveness(patch.lateralForgiveness)
      : lateralForgivenessRef.current;
    const nextRelease = Number.isFinite(patch.releaseForgiveness)
      ? clampReleaseForgiveness(patch.releaseForgiveness)
      : releaseForgivenessRef.current;
    if (Number.isFinite(patch.arcHeightTuning)) {
      arcHeightTuningRef.current = nextArc;
      setArcHeightTuning(nextArc);
    }
    if (Number.isFinite(patch.lateralForgiveness)) {
      lateralForgivenessRef.current = nextLateral;
      setLateralForgiveness(nextLateral);
    }
    if (Number.isFinite(patch.releaseForgiveness)) {
      releaseForgivenessRef.current = nextRelease;
      setReleaseForgiveness(nextRelease);
    }
    applyShotFeelToActiveLanes(nextArc, nextLateral, nextRelease);
    if (Number.isFinite(patch.aimSensitivityScale)) {
      setAimSensitivityScale(clamp(patch.aimSensitivityScale, 0.7, 1.4));
    }
    if (typeof patch.easyPlusAssist === "boolean") setEasyPlusAssist(patch.easyPlusAssist);
    if (typeof patch.aimMode === "string" && AIM_MODES[patch.aimMode]) setAimMode(patch.aimMode);
    if (typeof patch.controlProfile === "string" && CONTROL_PROFILES[patch.controlProfile]) {
      setControlProfile(patch.controlProfile);
    }
    SFX.ui();
  };
  const autoTunePatchEntries = Object.entries(telemetryAutoTune.patch || {});
  const difficultyPreset =
    shotDifficulty === "easy" && easyPlusAssist
      ? "easy_plus"
      : shotDifficulty === "hard" && aimMode === "pro"
        ? "pro"
        : aimMode === "casual" && shotDifficulty !== "easy"
          ? "casual"
          : shotDifficulty;
  const difficultyPresetLabel =
    difficultyPreset === "easy_plus"
      ? "Easy+"
      : difficultyPreset === "casual"
        ? "Casual"
        : difficultyPreset === "pro"
          ? "Pro"
          : (DIFFICULTY_TUNING[difficultyPreset] || currentDifficulty).label;
  const applyDifficultyPreset = (presetId) => {
    if (presetId === "easy_plus") {
      setShotDifficulty("easy");
      setEasyPlusAssist(true);
      setAimMode("casual");
      return;
    }
    if (presetId === "casual") {
      setShotDifficulty("medium");
      setEasyPlusAssist(false);
      setAimMode("casual");
      return;
    }
    if (presetId === "pro") {
      setShotDifficulty("hard");
      setEasyPlusAssist(false);
      setAimMode("pro");
      setControlProfile("precision");
      return;
    }
    setShotDifficulty(presetId);
    if (presetId !== "easy") setEasyPlusAssist(false);
  };
  const mechanicsLabel =
    shotMechanic === "hold" ? "Hold" : shotMechanic === "aim" ? "Aim" : shotMechanic === "release" ? "Release" : "Combo";
  const toggleAccordion = (groupId) => {
    setMenuAccordionOpen((current) => (current === groupId ? null : groupId));
  };
  const selectAccordionOption = (groupId, fn) => {
    if (typeof fn === "function") fn();
    setMenuAccordionOpen((current) => (current === groupId ? null : current));
  };
  const renderHintGlyph = (kind, color) => {
    if (kind === "hold") {
      return (
        <svg width="30" height="30" viewBox="0 0 32 32" fill="none" aria-hidden="true">
          <circle cx="16" cy="16" r="13" stroke={color} strokeOpacity="0.35" strokeWidth="2" />
          <rect x="10.5" y="11" width="4.2" height="10.2" rx="1.8" fill={color} />
          <rect x="14.9" y="9.2" width="3.8" height="12.6" rx="1.8" fill={color} fillOpacity="0.95" />
          <rect x="18.9" y="10.1" width="3.4" height="10.5" rx="1.6" fill={color} fillOpacity="0.8" />
          <path d="M9.5 19.4C10.7 18.3 12.5 18.4 13.6 19.6L15.1 21.2C16.3 22.5 18.3 22.7 19.7 21.7L22.3 19.8" stroke={color} strokeWidth="1.8" strokeLinecap="round" />
        </svg>
      );
    }
    if (kind === "aim") {
      return (
        <svg width="30" height="30" viewBox="0 0 32 32" fill="none" aria-hidden="true">
          <circle cx="16" cy="16" r="8.5" stroke={color} strokeWidth="2" />
          <circle cx="16" cy="16" r="2.2" fill={color} />
          <path d="M16 3.7V8.1M16 23.9V28.3M3.7 16H8.1M23.9 16H28.3" stroke={color} strokeWidth="2" strokeLinecap="round" />
          <path d="M8.8 22.1C12.5 19.3 18.3 18.7 22.6 20.7" stroke={color} strokeOpacity="0.7" strokeWidth="1.6" strokeLinecap="round" />
        </svg>
      );
    }
    if (kind === "release") {
      return (
        <svg width="30" height="30" viewBox="0 0 32 32" fill="none" aria-hidden="true">
          <path d="M8.3 21.4C10 18.4 13.3 16.5 16.8 16.5H22.6" stroke={color} strokeWidth="2" strokeLinecap="round" />
          <path d="M20.3 13.6L24.8 16.5L20.3 19.4" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
          <rect x="9.2" y="9.8" width="3.5" height="8.9" rx="1.6" fill={color} />
          <rect x="12.9" y="8.5" width="3.2" height="10.2" rx="1.6" fill={color} fillOpacity="0.92" />
          <rect x="16.3" y="9.3" width="2.9" height="8.8" rx="1.4" fill={color} fillOpacity="0.8" />
          <path d="M9.5 24.4C11.2 26 14.2 26.3 16.2 25.1" stroke={color} strokeOpacity="0.75" strokeWidth="1.6" strokeLinecap="round" />
        </svg>
      );
    }
    return (
      <svg width="30" height="30" viewBox="0 0 32 32" fill="none" aria-hidden="true">
        <path d="M8.2 20.8L12.4 16.6L15.4 19.6L19.9 15.1L23.8 19" stroke={color} strokeWidth="2.1" strokeLinecap="round" strokeLinejoin="round" />
        <circle cx="9" cy="10.2" r="1.7" fill={color} />
        <circle cx="16" cy="8.2" r="2" fill={color} />
        <circle cx="23.3" cy="9.8" r="1.8" fill={color} />
        <rect x="6.6" y="22.1" width="18.8" height="3.2" rx="1.5" fill={color} fillOpacity="0.28" />
        <path d="M25.8 14.2L28.7 15.8L25.8 17.4" stroke={color} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
    );
  };

  return (
    <div
      onClickCapture={(e) => {
        ensureAudioReady();
        const target = e.target;
        if (target && typeof target.closest === "function" && target.closest("button")) {
          playUiTap();
        }
      }}
      style={{
        minHeight: "100dvh",
        height: safeScreen === "game" ? "100dvh" : "auto",
        width: "100%",
        background:
          "radial-gradient(circle at 50% 20%, #16345b 0%, #081321 42%, #040a13 100%)",
        color: "#d8f2ff",
        padding: safeScreen === "game" ? 0 : 14,
        display: "flex",
        alignItems: safeScreen === "game" ? "stretch" : "center",
        justifyContent: safeScreen === "game" ? "stretch" : "center",
        fontFamily: "Saira, sans-serif",
      }}
    >
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=Orbitron:wght@500;700;900&family=Saira:wght@400;500;600;700&display=swap');
        * { box-sizing: border-box; }
        @keyframes hintFloat {
          0%, 100% { transform: translateY(0); opacity: 0.75; }
          50% { transform: translateY(-6px); opacity: 1; }
        }
        @keyframes hudPulse {
          0%, 100% { box-shadow: 0 0 0 rgba(80,220,255,0.0); }
          50% { box-shadow: 0 0 18px rgba(80,220,255,0.2); }
        }
        @keyframes rankCeremony {
          0% { opacity: 0; transform: translateY(-14px) scale(0.94); }
          18% { opacity: 1; transform: translateY(0) scale(1.04); }
          72% { opacity: 1; transform: translateY(0) scale(1); }
          100% { opacity: 0; transform: translateY(-8px) scale(0.98); }
        }
        @keyframes flicker {
          0%, 18%, 22%, 100% { opacity: 0.22; }
          19%, 21% { opacity: 0.08; }
          60% { opacity: 0.3; }
        }
        @keyframes logoPulse {
          0%, 100% {
            transform: scale(1);
            filter: brightness(1);
          }
          50% {
            transform: scale(1.03);
            filter: brightness(1.18);
          }
        }
        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.68; }
        }
        .logoBallPulse { animation: logoPulse 2.2s ease-in-out infinite; }
        .logoBallSeam {
          position: absolute;
          top: 50%;
          left: 50%;
          border-radius: 999px;
          background: rgba(66, 20, 0, 0.88);
          box-shadow: 0 0 12px rgba(255, 200, 130, 0.2);
          transform-origin: center;
        }
        .logoBallSeamVertical { width: 7px; height: 140px; transform: translate(-50%, -50%); }
        .logoBallSeamHorizontal { width: 140px; height: 7px; transform: translate(-50%, -50%); }
        .logoBallSeamDiagA { width: 152px; height: 7px; transform: translate(-50%, -50%) rotate(22deg); }
        .logoBallSeamDiagB { width: 152px; height: 7px; transform: translate(-50%, -50%) rotate(-22deg); }
        .frame {
          --hud-scale: 1;
          --hud-ratio: ${HUD_HEIGHT_RATIO};
          --court-ratio: ${COURT_HEIGHT_RATIO};
          width: min(1320px, 100%);
          border: 2px solid #3ecfff;
          background: linear-gradient(180deg, rgba(8,20,34,0.95), rgba(5,13,24,0.98));
          border-radius: 10px;
          box-shadow: 0 0 30px rgba(0, 210, 255, 0.25), inset 0 0 30px rgba(0, 90, 140, 0.2);
          overflow: hidden;
        }
        .gameFrame {
          width: min(1320px, 100%);
          height: min(var(--frame-height, 98dvh), 1040px);
          max-height: var(--frame-height, 98dvh);
          display: grid;
          grid-template-rows: calc(var(--hud-ratio) * 100%) calc(var(--court-ratio) * 100%);
        }
        .gameFrameFullscreen {
          width: 100%;
          max-width: none;
          height: 100%;
          max-height: none;
          border: 0;
          border-radius: 0;
          box-shadow: none;
          background: transparent;
          overflow: hidden;
        }
        .gameFrameFullscreen .courtWrap {
          border-top: 0;
        }
        .gameFrameFullscreen .courtViewport {
          max-width: 100%;
          max-height: 100%;
          margin: auto;
        }
        .hud {
          display: grid;
          grid-template-columns: 1fr auto 1fr;
          gap: calc(4px * var(--hud-scale));
          align-items: center;
          padding: calc(1px * var(--hud-scale)) calc(3px * var(--hud-scale));
          border-bottom: 1px solid rgba(110, 220, 255, 0.05);
          background: linear-gradient(90deg, rgba(10,30,50,0.07), rgba(8,20,35,0.06));
          backdrop-filter: blur(3px);
        }
        .scoreCol { text-align: center; padding: calc(1px * var(--hud-scale)) calc(3px * var(--hud-scale)); border: 1px solid rgba(130,220,255,0.05); background: rgba(8,20,33,0.08); backdrop-filter: blur(3px); }
        .scoreCol.turn { animation: hudPulse 1.2s ease-in-out infinite; border-color: rgba(120,230,255,0.26); }
        .title { font-family: Orbitron, sans-serif; letter-spacing: 1.5px; font-size: calc(8.5px * var(--hud-scale)); opacity: 0.7; }
        .scoreVal { font-family: Orbitron, sans-serif; font-size: calc(29px * var(--hud-scale)); line-height: 1; transition: transform 0.2s ease; }
        .timer { font-family: Orbitron, sans-serif; font-size: calc(31px * var(--hud-scale)); letter-spacing: 1.6px; opacity: 0.92; }
        .fire { color: #ff9b2f; text-shadow: 0 0 8px #ff6b2f, 0 0 18px #ffb347; }
        .badge { font-size: calc(9px * var(--hud-scale)); padding: calc(2px * var(--hud-scale)) calc(6px * var(--hud-scale)); border: 1px solid rgba(160,230,255,0.35); margin-left: calc(5px * var(--hud-scale)); letter-spacing: 1px; }
        .streakMeter { height: calc(5px * var(--hud-scale)); background: rgba(255,255,255,0.1); margin-top: calc(4px * var(--hud-scale)); overflow: hidden; border-radius: 8px; }
        .streakFill { height: 100%; background: linear-gradient(90deg, #ff4f4f, #ffd86c, #61ffd1); box-shadow: 0 0 10px rgba(97,255,209,0.5); }
        .courtWrap {
          min-height: 0;
          height: 100%;
          border-top: 1px solid rgba(130,220,255,0.2);
          background: #050e18;
          display: flex;
          justify-content: center;
          align-items: stretch;
          overflow: hidden;
        }
        .courtViewport {
          width: min(100%, var(--court-fit-width, 100%));
          height: min(100%, var(--court-fit-height, 100%));
          max-width: 100%;
          max-height: 100%;
          aspect-ratio: 16 / 9;
        }
        .menuFrame {
          padding: 20px 22px 24px;
          background:
            radial-gradient(ellipse 72% 38% at 50% -10%, rgba(20,96,168,0.24) 0%, transparent 72%),
            linear-gradient(180deg, rgba(7,18,33,0.95), rgba(4,10,18,0.98));
          overflow-y: auto;
          max-height: min(96vh, 1040px);
        }
        .menuTopbar {
          display: flex;
          align-items: center;
          justify-content: center;
          gap: 16px;
          margin: 0 auto 16px;
          width: min(860px, 100%);
          border: 1px solid rgba(0,180,220,0.18);
          background: rgba(8,20,38,0.55);
          backdrop-filter: blur(10px);
          border-radius: 10px;
          padding: 8px 12px;
        }
        .playerTagWrapper {
          display: flex;
          align-items: center;
          gap: 10px;
          flex-wrap: wrap;
        }
        .playerTagLabel {
          font-family: "Orbitron", sans-serif;
          font-size: 10px;
          letter-spacing: 0.18em;
          text-transform: uppercase;
          color: #7ea8c5;
        }
        .playerTagInput {
          background: rgba(0,212,255,0.07);
          border: 1px solid rgba(0,180,220,0.24);
          border-radius: 6px;
          color: #e8f4ff;
          font-family: "Orbitron", sans-serif;
          font-size: 12px;
          font-weight: 600;
          letter-spacing: 0.1em;
          padding: 8px 12px;
          outline: none;
          min-width: 190px;
        }
        .playerTagInput:focus {
          border-color: #00d4ff;
          box-shadow: 0 0 0 2px rgba(0,212,255,0.16), 0 0 14px rgba(0,212,255,0.32);
        }
        .networkStatus {
          display: flex;
          align-items: center;
          gap: 8px;
          flex-wrap: wrap;
          justify-content: flex-end;
          font-size: 10px;
          letter-spacing: 0.12em;
          text-transform: uppercase;
          color: #7ea8c5;
        }
        .statusDot {
          width: 8px;
          height: 8px;
          border-radius: 50%;
          background: #ff4a4a;
          box-shadow: 0 0 9px #ff4a4a;
          animation: statusBlink 2s ease-in-out infinite;
        }
        .statusDot.online {
          background: #00ff88;
          box-shadow: 0 0 9px #00ff88;
          animation: none;
        }
        .statusLabel { color: #ff8c8c; }
        .statusLabel.online { color: #8dffc6; }
        .statusUrl { color: #5d8aa8; font-size: 9px; }
        .main-grid {
          display: grid;
          grid-template-columns: minmax(0, 1fr) 320px;
          grid-template-rows: auto 1fr;
          gap: 14px;
          align-items: start;
        }
        .settings-section { grid-column: 1; grid-row: 1; min-width: 0; }
        .section-title {
          font-family: "Orbitron", sans-serif;
          font-size: 9px;
          letter-spacing: 0.22em;
          color: #7ea8c5;
          text-transform: uppercase;
          margin-bottom: 10px;
          padding-left: 2px;
        }
        .accordion-stack { display: grid; gap: 6px; }
        .accordion-item {
          background: rgba(8,20,38,0.84);
          border: 1px solid rgba(0,180,220,0.2);
          border-radius: 10px;
          overflow: hidden;
          backdrop-filter: blur(8px);
          transition: border-color 0.2s ease, box-shadow 0.2s ease;
        }
        .accordion-item:hover { border-color: rgba(0,212,255,0.4); }
        .accordion-item.open {
          border-color: rgba(0,212,255,0.44);
          box-shadow: 0 0 0 1px rgba(0,212,255,0.08), 0 8px 22px rgba(0,0,0,0.3);
        }
        .accordion-header {
          width: 100%;
          border: 0;
          background: transparent;
          display: grid;
          grid-template-columns: 170px 1fr auto;
          align-items: center;
          gap: 10px;
          min-height: 50px;
          padding: 10px 14px;
          text-align: left;
          cursor: pointer;
          color: #d9f5ff;
          font-family: "Orbitron", sans-serif;
          font-size: 10px;
          letter-spacing: 0.16em;
          text-transform: uppercase;
        }
        .accordion-value {
          justify-self: center;
          color: #ffb260;
          font-family: "Rajdhani", sans-serif;
          font-size: 12px;
          font-weight: 700;
          letter-spacing: 0.1em;
          text-transform: uppercase;
          text-align: center;
        }
        .accordion-chevron {
          justify-self: end;
          font-size: 9px;
          color: #6e9fbf;
          transition: transform 0.25s ease, color 0.2s ease;
        }
        .accordion-item.open .accordion-chevron {
          transform: rotate(180deg);
          color: #00d4ff;
        }
        .accordion-body {
          max-height: 0;
          overflow: hidden;
          transition: max-height 0.32s ease;
        }
        .accordion-item.open .accordion-body { max-height: 920px; }
        .accordion-inner {
          padding: 8px 12px 12px;
          border-top: 1px solid rgba(0,180,220,0.16);
        }
        .options-grid {
          display: flex;
          flex-wrap: wrap;
          gap: 8px;
          margin-top: 6px;
        }
        .opt-btn {
          background: transparent;
          border: 1px solid rgba(0,180,220,0.2);
          border-radius: 6px;
          color: #7da0b8;
          cursor: pointer;
          font-family: "Rajdhani", sans-serif;
          font-size: 12px;
          font-weight: 700;
          letter-spacing: 0.1em;
          padding: 7px 12px;
          text-transform: uppercase;
          transition: all 0.15s ease;
        }
        .opt-btn:hover {
          border-color: rgba(0,212,255,0.45);
          color: #dff4ff;
        }
        .opt-btn.active {
          border-color: #ff8c00;
          color: #ffb260;
          background: rgba(255,140,0,0.12);
          box-shadow: 0 0 12px rgba(255,140,0,0.4);
        }
        .color-options {
          display: flex;
          flex-wrap: wrap;
          gap: 8px;
          margin-top: 6px;
        }
        .color-opt {
          display: inline-flex;
          align-items: center;
          gap: 8px;
          border: 1px solid rgba(0,180,220,0.2);
          border-radius: 6px;
          padding: 6px 10px;
          cursor: pointer;
          font-size: 11px;
          font-weight: 700;
          letter-spacing: 0.08em;
          text-transform: uppercase;
          color: #7da0b8;
          background: transparent;
          transition: all 0.15s;
        }
        .color-opt:hover { border-color: rgba(0,212,255,0.45); color: #dff4ff; }
        .color-opt.active {
          border-color: #ff8c00;
          color: #ffb260;
          background: rgba(255,140,0,0.12);
        }
        .color-dot { width: 10px; height: 10px; border-radius: 50%; display: inline-block; }
        .slider-section { margin-top: 6px; display: grid; gap: 10px; }
        .slider-row {
          display: grid;
          grid-template-columns: 160px 1fr 54px;
          align-items: center;
          gap: 10px;
        }
        .slider-label {
          font-size: 10px;
          font-weight: 700;
          letter-spacing: 0.1em;
          text-transform: uppercase;
          color: #7da0b8;
        }
        .slider-val {
          font-family: "Orbitron", sans-serif;
          font-size: 10px;
          letter-spacing: 0.08em;
          color: #00d4ff;
          text-align: right;
        }
        .accordion-body input[type="range"] {
          width: 100%;
          height: 3px;
          background: rgba(0,212,255,0.16);
          border-radius: 999px;
          appearance: none;
          cursor: pointer;
        }
        .accordion-body input[type="range"]::-webkit-slider-thumb {
          appearance: none;
          width: 13px;
          height: 13px;
          border-radius: 50%;
          background: #00d4ff;
          box-shadow: 0 0 8px rgba(0,212,255,0.5);
        }
        .side-panel {
          grid-column: 2;
          grid-row: 1 / 3;
          display: flex;
          flex-direction: column;
          gap: 12px;
        }
        .glass-card {
          background: rgba(8,20,38,0.86);
          border: 1px solid rgba(0,180,220,0.18);
          border-radius: 14px;
          backdrop-filter: blur(12px);
          overflow: hidden;
        }
        .card-header {
          padding: 12px 14px 9px;
          font-family: "Orbitron", sans-serif;
          font-size: 9px;
          letter-spacing: 0.2em;
          color: #7ea8c5;
          text-transform: uppercase;
          border-bottom: 1px solid rgba(0,180,220,0.14);
          display: flex;
          align-items: center;
          gap: 7px;
        }
        .live-dot {
          width: 5px;
          height: 5px;
          border-radius: 50%;
          background: #ff8c00;
          box-shadow: 0 0 6px #ff8c00;
          animation: statusBlink 1.5s ease-in-out infinite;
        }
        .leaderboard-row, .match-row {
          display: grid;
          align-items: center;
          gap: 8px;
          padding: 8px 14px;
          border-bottom: 1px solid rgba(0,180,220,0.07);
          font-size: 11px;
          letter-spacing: 0.06em;
        }
        .leaderboard-row:last-child, .match-row:last-child { border-bottom: 0; }
        .leaderboard-row { grid-template-columns: 28px 1fr auto; }
        .match-row { grid-template-columns: 1fr auto auto; }
        .leaderboard-row:hover, .match-row:hover { background: rgba(0,212,255,0.08); }
        .rank { font-family: "Orbitron", sans-serif; font-size: 10px; color: #7ea8c5; }
        .rank.top { color: #ffb260; }
        .player-name, .match-player { color: #e8f4ff; font-weight: 700; }
        .player-name.you, .match-player.you { color: #ffb260; }
        .score-badge, .match-score { font-family: "Orbitron", sans-serif; color: #00d4ff; }
        .match-type {
          padding: 2px 8px;
          border: 1px solid rgba(0,180,220,0.2);
          border-radius: 4px;
          color: #6e9fbf;
          font-size: 9px;
          letter-spacing: 0.1em;
          text-transform: uppercase;
        }
        .footer-bar {
          grid-column: 1;
          grid-row: 2;
          display: flex;
          align-items: center;
          justify-content: space-between;
          gap: 14px;
          padding-top: 12px;
          flex-wrap: wrap;
        }
        .tier-info {
          display: flex;
          align-items: center;
          gap: 12px;
          flex-wrap: wrap;
        }
        .tier-chip {
          background: rgba(255,140,0,0.12);
          border: 1px solid rgba(255,140,0,0.3);
          border-radius: 4px;
          padding: 4px 10px;
          font-family: "Orbitron", sans-serif;
          font-size: 10px;
          letter-spacing: 0.14em;
          color: #ffb260;
          text-transform: uppercase;
        }
        .stat-pill {
          display: inline-flex;
          gap: 6px;
          color: #7da0b8;
          font-size: 11px;
          letter-spacing: 0.1em;
          text-transform: uppercase;
        }
        .stat-pill strong {
          font-family: "Orbitron", sans-serif;
          color: #00d4ff;
        }
        .start-btn {
          background: linear-gradient(135deg, #005f88 0%, #003f60 100%);
          border: 1px solid #00d4ff;
          border-radius: 10px;
          color: #00d4ff;
          cursor: pointer;
          font-family: "Orbitron", sans-serif;
          font-size: 13px;
          font-weight: 700;
          letter-spacing: 0.18em;
          padding: 14px 34px;
          text-transform: uppercase;
          box-shadow: 0 0 20px rgba(0,212,255,0.2), inset 0 1px 0 rgba(255,255,255,0.06);
          transition: all 0.2s;
        }
        .start-btn:hover {
          box-shadow: 0 0 30px rgba(0,212,255,0.42), 0 0 60px rgba(0,212,255,0.15);
          transform: translateY(-1px);
        }
        .achievementsRow {
          grid-column: 1 / 3;
          display: flex;
          gap: 8px;
          flex-wrap: wrap;
        }
        .achievementPill {
          font-size: 10px;
          padding: 3px 8px;
          border: 1px solid rgba(120,180,210,0.24);
          color: #86a6bb;
          letter-spacing: 0.09em;
          text-transform: uppercase;
        }
        @media (max-width: 1080px) {
          .main-grid {
            grid-template-columns: 1fr;
          }
          .side-panel {
            grid-column: 1;
            grid-row: 3;
          }
          .footer-bar {
            grid-column: 1;
            grid-row: 2;
          }
          .achievementsRow { grid-column: 1; }
        }
        .btn {
          border: 1.5px solid #38d4ff;
          background: transparent;
          color: #9ce8ff;
          font-family: Orbitron, sans-serif;
          letter-spacing: 1px;
          padding: 12px 24px;
          cursor: pointer;
          transition: 0.16s ease;
        }
        .btn:hover { transform: scale(1.03); filter: brightness(1.2); }
        .btn:disabled {
          cursor: not-allowed;
          filter: saturate(0.5);
        }
      `}</style>

      {rankUpFx.active && (
        <div
          style={{
            position: "fixed",
            top: 20,
            left: "50%",
            transform: "translateX(-50%)",
            zIndex: 40,
            pointerEvents: "none",
            padding: "10px 18px",
            border: `1px solid ${rankUpFx.color}AA`,
            borderRadius: 8,
            background: "rgba(8,20,33,0.84)",
            boxShadow: `0 0 28px ${rankUpFx.color}66`,
            color: rankUpFx.color,
            letterSpacing: 1.8,
            fontFamily: "Orbitron, sans-serif",
            fontSize: 14,
            textTransform: "uppercase",
            animation: "rankCeremony 2.4s ease-out forwards",
          }}
        >
          Rank Up • {rankUpFx.label}
        </div>
      )}

      {safeScreen === "menu" && (
        <div className="frame menuFrame">
          <NeonHoopzHeader playerName={playerName} backendStatus={backendStatus} />
          <div className="menuTopbar">
            <div className="playerTagWrapper">
              <span className="playerTagLabel">Player Tag</span>
              <input
                className="playerTagInput"
                value={playerName}
                spellCheck={false}
                onChange={(e) =>
                  setPlayerName(
                    (e.target.value || "")
                      .toUpperCase()
                      .replace(/\s+/g, " ")
                      .slice(0, 24)
                  )
                }
              />
            </div>
          </div>

          <div className="main-grid">
            <div className="settings-section">
              <div className="section-title">Settings</div>
              <div className="accordion-stack">
                <div className={`accordion-item ${menuAccordionOpen === "difficulty" ? "open" : ""}`}>
                  <button className="accordion-header" onClick={() => toggleAccordion("difficulty")}>
                    <span>Difficulty</span>
                    <span className="accordion-value">{difficultyPresetLabel}</span>
                    <span className="accordion-chevron">▼</span>
                  </button>
                  <div className="accordion-body">
                    <div className="accordion-inner">
                      <div className="options-grid">
                        {[
                          ["easy", "Easy", "#66ffd2"],
                          ["medium", "Medium", "#7fd9ff"],
                          ["hard", "Hard", "#ff8b7f"],
                          ["easy_plus", "Easy+", "#8effd7"],
                          ["casual", "Casual", "#89d4ff"],
                          ["pro", "Pro", "#ffbd86"],
                        ].map(([id, label, color]) => (
                          <button
                            key={id}
                            className={`opt-btn ${difficultyPreset === id ? "active" : ""}`}
                            onClick={() => selectAccordionOption("difficulty", () => applyDifficultyPreset(id))}
                            style={difficultyPreset === id ? { borderColor: color, color, boxShadow: `0 0 14px ${color}44` } : null}
                          >
                            {label}
                          </button>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>

                <div className={`accordion-item ${menuAccordionOpen === "playstyle" ? "open" : ""}`}>
                  <button className="accordion-header" onClick={() => toggleAccordion("playstyle")}>
                    <span>Play Style</span>
                    <span className="accordion-value">{currentControl.label}</span>
                    <span className="accordion-chevron">▼</span>
                  </button>
                  <div className="accordion-body">
                    <div className="accordion-inner">
                      <div className="options-grid">
                        {Object.entries(CONTROL_PROFILES).map(([id, profile]) => (
                          <button
                            key={id}
                            className={`opt-btn ${controlProfile === id ? "active" : ""}`}
                            onClick={() => selectAccordionOption("playstyle", () => setControlProfile(id))}
                            style={controlProfile === id ? { borderColor: profile.color, color: profile.color } : null}
                          >
                            {profile.label}
                          </button>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>

                <div className={`accordion-item ${menuAccordionOpen === "handedness" ? "open" : ""}`}>
                  <button className="accordion-header" onClick={() => toggleAccordion("handedness")}>
                    <span>Handedness</span>
                    <span className="accordion-value">{dominantHand === "left" ? "Left Hand" : "Right Hand"}</span>
                    <span className="accordion-chevron">▼</span>
                  </button>
                  <div className="accordion-body">
                    <div className="accordion-inner">
                      <div className="options-grid">
                        {[
                          ["right", "Right Hand"],
                          ["left", "Left Hand"],
                        ].map(([id, label]) => (
                          <button
                            key={id}
                            className={`opt-btn ${dominantHand === id ? "active" : ""}`}
                            onClick={() => selectAccordionOption("handedness", () => setDominantHand(id))}
                          >
                            {label}
                          </button>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>

                <div className={`accordion-item ${menuAccordionOpen === "mechanics" ? "open" : ""}`}>
                  <button className="accordion-header" onClick={() => toggleAccordion("mechanics")}>
                    <span>Shot Mechanics</span>
                    <span className="accordion-value">{mechanicsLabel}</span>
                    <span className="accordion-chevron">▼</span>
                  </button>
                  <div className="accordion-body">
                    <div className="accordion-inner">
                      <div className="options-grid">
                        {[
                          ["hold", "Hold"],
                          ["aim", "Aim"],
                          ["release", "Release"],
                          ["combo", "Combo"],
                        ].map(([id, label]) => (
                          <button
                            key={id}
                            className={`opt-btn ${shotMechanic === id ? "active" : ""}`}
                            onClick={() => selectAccordionOption("mechanics", () => setShotMechanic(id))}
                          >
                            {label}
                          </button>
                        ))}
                        {Object.entries(AIM_MODES).map(([id, mode]) => (
                          <button
                            key={id}
                            className={`opt-btn ${aimMode === id ? "active" : ""}`}
                            onClick={() => selectAccordionOption("mechanics", () => setAimMode(id))}
                          >
                            Aim: {mode.label}
                          </button>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>

                <div className={`accordion-item ${menuAccordionOpen === "modifiers" ? "open" : ""}`}>
                  <button className="accordion-header" onClick={() => toggleAccordion("modifiers")}>
                    <span>Modifiers</span>
                    <span className="accordion-value">
                      {[vrMode, dailyMode, oneViewMode, practiceMode, movingRims].filter(Boolean).length} Active
                    </span>
                    <span className="accordion-chevron">▼</span>
                  </button>
                  <div className="accordion-body">
                    <div className="accordion-inner">
                      <div className="options-grid">
                        <button className={`opt-btn ${vrMode ? "active" : ""}`} onClick={() => setVrMode((v) => !v)}>
                          VR First-Person
                        </button>
                        <button
                          className={`opt-btn ${dailyMode ? "active" : ""}`}
                          onClick={() => {
                            if (!dailyMode && practiceMode) setPracticeMode(false);
                            setDailyMode((v) => !v);
                          }}
                        >
                          Daily Challenge
                        </button>
                        <button className={`opt-btn ${oneViewMode ? "active" : ""}`} onClick={() => setOneViewMode((v) => !v)}>
                          One View
                        </button>
                        <button
                          className={`opt-btn ${practiceMode ? "active" : ""}`}
                          onClick={() =>
                            setPracticeMode((v) => {
                              const next = !v;
                              if (next) {
                                setDailyMode(false);
                                setOneViewMode(true);
                              }
                              return next;
                            })
                          }
                        >
                          Practice
                        </button>
                        <button className={`opt-btn ${movingRims ? "active" : ""}`} onClick={() => setMovingRims((v) => !v)}>
                          Moving Rims
                        </button>
                      </div>
                    </div>
                  </div>
                </div>

                <div className={`accordion-item ${menuAccordionOpen === "shotfeel" ? "open" : ""}`}>
                  <button className="accordion-header" onClick={() => toggleAccordion("shotfeel")}>
                    <span>Shot Feel Tune</span>
                    <span className="accordion-value">Custom</span>
                    <span className="accordion-chevron">▼</span>
                  </button>
                  <div className="accordion-body">
                    <div className="accordion-inner">
                      <div className="slider-section">
                        <div className="slider-row">
                          <span className="slider-label">Arc Height</span>
                          <input
                            type="range"
                            min={Math.round(ARC_HEIGHT_TUNE_RANGE.min * 100)}
                            max={Math.round(ARC_HEIGHT_TUNE_RANGE.max * 100)}
                            step={1}
                            value={Math.round(arcHeightTuning * 100)}
                            onChange={(e) => setArcHeightTuning(clampArcHeightTune(Number(e.target.value) / 100))}
                          />
                          <span className="slider-val">{Math.round(arcHeightTuning * 100)}%</span>
                        </div>
                        <div className="slider-row">
                          <span className="slider-label">Lateral Forgiveness</span>
                          <input
                            type="range"
                            min={Math.round(LATERAL_FORGIVENESS_RANGE.min * 100)}
                            max={Math.round(LATERAL_FORGIVENESS_RANGE.max * 100)}
                            step={1}
                            value={Math.round(lateralForgiveness * 100)}
                            onChange={(e) => setLateralForgiveness(clampLateralForgiveness(Number(e.target.value) / 100))}
                          />
                          <span className="slider-val">{Math.round(lateralForgiveness * 100)}%</span>
                        </div>
                        <div className="slider-row">
                          <span className="slider-label">Release Forgiveness</span>
                          <input
                            type="range"
                            min={Math.round(RELEASE_FORGIVENESS_RANGE.min * 100)}
                            max={Math.round(RELEASE_FORGIVENESS_RANGE.max * 100)}
                            step={1}
                            value={Math.round(releaseForgiveness * 100)}
                            onChange={(e) => setReleaseForgiveness(clampReleaseForgiveness(Number(e.target.value) / 100))}
                          />
                          <span className="slider-val">{Math.round(releaseForgiveness * 100)}%</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div className={`accordion-item ${menuAccordionOpen === "colors" ? "open" : ""}`}>
                  <button className="accordion-header" onClick={() => toggleAccordion("colors")}>
                    <span>Ball Color</span>
                    <span className="accordion-value">
                      {getBallColorway(playerBallColor).label} / {getBallColorway(opponentBallColor).label}
                    </span>
                    <span className="accordion-chevron">▼</span>
                  </button>
                  <div className="accordion-body">
                    <div className="accordion-inner">
                      <div className="section-title" style={{ marginBottom: 4 }}>Player Ball</div>
                      <div className="color-options">
                        {Object.entries(BALL_COLORWAYS).map(([id, colorway]) => (
                          <button
                            key={`player-${id}`}
                            className={`color-opt ${playerBallColor === id ? "active" : ""}`}
                            onClick={() => setPlayerBallColor(id)}
                          >
                            <span className="color-dot" style={{ background: colorway.body, boxShadow: `0 0 8px ${colorway.body}` }} />
                            {colorway.label}
                          </button>
                        ))}
                      </div>
                      <div className="section-title" style={{ marginTop: 10, marginBottom: 4 }}>Opponent Ball</div>
                      <div className="color-options">
                        {Object.entries(BALL_COLORWAYS).map(([id, colorway]) => (
                          <button
                            key={`opponent-${id}`}
                            className={`color-opt ${opponentBallColor === id ? "active" : ""}`}
                            onClick={() => setOpponentBallColor(id)}
                          >
                            <span className="color-dot" style={{ background: colorway.body, boxShadow: `0 0 8px ${colorway.body}` }} />
                            {colorway.label}
                          </button>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>

                <div className={`accordion-item ${menuAccordionOpen === "skins" ? "open" : ""}`}>
                  <button className="accordion-header" onClick={() => toggleAccordion("skins")}>
                    <span>Skins</span>
                    <span className="accordion-value">{ballSkin} / {courtSkin}</span>
                    <span className="accordion-chevron">▼</span>
                  </button>
                  <div className="accordion-body">
                    <div className="accordion-inner">
                      <div className="section-title" style={{ marginBottom: 4 }}>Ball Skin</div>
                      <div className="options-grid">
                        {[
                          ["classic", "Classic", true],
                          ["plasma", "Plasma", unlockPlasma],
                          ["prism", "Prism", unlockPrism],
                        ].map(([id, label, unlocked]) => (
                          <button
                            key={id}
                            disabled={!unlocked}
                            className={`opt-btn ${ballSkin === id ? "active" : ""}`}
                            onClick={() => unlocked && setBallSkin(id)}
                          >
                            {label} {!unlocked ? "🔒" : ""}
                          </button>
                        ))}
                      </div>
                      <div className="section-title" style={{ marginTop: 10, marginBottom: 4 }}>Court Skin</div>
                      <div className="options-grid">
                        {[
                          ["neon", "Neon", true],
                          ["carbon", "Carbon", unlockCarbon],
                          ["sunset", "Sunset", unlockSunset],
                        ].map(([id, label, unlocked]) => (
                          <button
                            key={id}
                            disabled={!unlocked}
                            className={`opt-btn ${courtSkin === id ? "active" : ""}`}
                            onClick={() => unlocked && setCourtSkin(id)}
                          >
                            {label} {!unlocked ? "🔒" : ""}
                          </button>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>

                {showDevPanel && (
                  <div className={`accordion-item ${menuAccordionOpen === "devtune" ? "open" : ""}`}>
                    <button className="accordion-header" onClick={() => toggleAccordion("devtune")}>
                      <span>Dev Tuning</span>
                      <span className="accordion-value">Advanced</span>
                      <span className="accordion-chevron">▼</span>
                    </button>
                    <div className="accordion-body">
                      <div className="accordion-inner">
                        <div className="options-grid">
                          {[
                            ["casual", SHOT_TUNING_LABELS.casual],
                            ["competitive", SHOT_TUNING_LABELS.competitive],
                            ["arcade", SHOT_TUNING_LABELS.arcade],
                          ].map(([id, label]) => (
                            <button
                              key={id}
                              className={`opt-btn ${shotTuningPreset === id ? "active" : ""}`}
                              onClick={() => setShotTuningPreset(id)}
                            >
                              {label}
                            </button>
                          ))}
                          {[
                            ["competitive_clean", VISUAL_TUNING_LABELS.competitive_clean],
                            ["neon_premium", VISUAL_TUNING_LABELS.neon_premium],
                            ["performance_mobile", VISUAL_TUNING_LABELS.performance_mobile],
                          ].map(([id, label]) => (
                            <button
                              key={id}
                              className={`opt-btn ${visualTuningPreset === id ? "active" : ""}`}
                              onClick={() => setVisualTuningPreset(id)}
                            >
                              {label}
                            </button>
                          ))}
                          <button
                            className={`opt-btn ${debugArcOverlay ? "active" : ""}`}
                            onClick={() => setDebugArcOverlay((v) => !v)}
                          >
                            Arc Debug {debugArcOverlay ? "ON" : "OFF"}
                          </button>
                        </div>
                        <div className="slider-section" style={{ marginTop: 10 }}>
                          <div className="slider-row">
                            <span className="slider-label">Aim Sensitivity</span>
                            <input
                              type="range"
                              min={70}
                              max={140}
                              step={1}
                              value={Math.round(aimSensitivityScale * 100)}
                              onChange={(e) => {
                                const v = Number(e.target.value);
                                setAimSensitivityScale(clamp(v / 100, 0.7, 1.4));
                              }}
                            />
                            <span className="slider-val">{Math.round(aimSensitivityScale * 100)}%</span>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </div>

            <div className="side-panel">
              <div className="glass-card">
                <div className="card-header">
                  <span className="live-dot" />
                  Live Leaderboard
                </div>
                {menuLeaderboardRows.slice(0, 5)
                  .map((entry, idx) => (
                    <div key={`lb-${entry.rank}-${entry.name}-${idx}`} className="leaderboard-row">
                      <span className={`rank ${idx < 2 ? "top" : ""}`}>#{entry.rank}</span>
                      <span className={`player-name ${String(entry.name || "").toUpperCase() === playerName.toUpperCase() ? "you" : ""}`}>{entry.name}</span>
                      <span className="score-badge">{entry.bestScore}</span>
                    </div>
                  ))}
              </div>

              <div className="glass-card">
                <div className="card-header">Recent Matches</div>
                {menuRecentRows.slice(0, 5)
                  .map((match, idx) => (
                    <div key={`match-${match.matchId || idx}`} className="match-row">
                      <span className={`match-player ${String(match.playerName || "").toUpperCase() === playerName.toUpperCase() ? "you" : ""}`}>{match.playerName}</span>
                      <span className="match-type">{String(match.mode || "solo").toUpperCase()}</span>
                      <span className="match-score">{match.score}</span>
                    </div>
                  ))}
              </div>

              <div className="glass-card">
                <div className="card-header">Shot Telemetry</div>
                <div style={{ display: "grid", gap: 6 }}>
                  {telemetryRows.map((row) => (
                    <div key={`telemetry-${row.id}`} style={{ border: "1px solid rgba(100,180,220,0.25)", borderRadius: 8, padding: "6px 8px", background: "rgba(8,18,30,0.28)" }}>
                      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 8 }}>
                        <span style={{ fontSize: 10, letterSpacing: 1.2, color: row.color, textTransform: "uppercase" }}>{row.label}</span>
                        <span style={{ fontSize: 10, color: "#9fe4ff" }}>{row.makeRate}% Hit</span>
                      </div>
                      <div style={{ marginTop: 4, fontSize: 10, color: "#9bb8c9", letterSpacing: 0.6 }}>
                        A {row.attempts} • M {row.makes} • SW {row.swishes} • RO {row.rimOuts} • MISS {row.misses}
                      </div>
                    </div>
                  ))}
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 8, marginTop: 2 }}>
                    <span style={{ fontSize: 9, color: "rgba(150,190,210,0.8)", letterSpacing: 1 }}>Updated {telemetryUpdatedLabel}</span>
                    <button className="btn" style={{ padding: "4px 8px", fontSize: 10 }} onClick={resetShotTelemetry}>
                      Reset Data
                    </button>
                  </div>
                </div>
              </div>

              <div className="glass-card">
                <div className="card-header">Auto Tune</div>
                <div style={{ display: "grid", gap: 8 }}>
                  <div style={{ fontSize: 11, color: "#b8d7e8", lineHeight: 1.38 }}>
                    {telemetryAutoTune.summary}
                  </div>
                  <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
                    <button
                      className="btn"
                      style={{ padding: "5px 10px", fontSize: 10 }}
                      onClick={() => setAutoArcCalibrate((v) => !v)}
                    >
                      Auto Arc {autoArcCalibrate ? "ON" : "OFF"}
                    </button>
                    <button
                      className="btn"
                      style={{ padding: "5px 10px", fontSize: 10 }}
                      onClick={() => {
                        autoArcCalRef.current = createArcCalState();
                        setAutoArcCalProgress({ easy: 0, medium: 0, hard: 0 });
                        setAutoArcCalStatus(
                          autoArcCalibrate
                            ? `Auto arc calibration armed (${AUTO_ARC_CALIBRATION_INTERVAL} shots per pass).`
                            : "Auto arc calibration paused."
                        );
                        SFX.ui();
                      }}
                    >
                      Reset Arc Cal
                    </button>
                  </div>
                  <div style={{ fontSize: 10, color: "rgba(153, 197, 219, 0.95)", lineHeight: 1.35 }}>
                    {autoArcCalStatus}
                  </div>
                  <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
                    {SHOT_DIFFICULTY_IDS.map((id) => {
                      const label = (DIFFICULTY_TUNING[id] || DIFFICULTY_TUNING.medium).label;
                      const color = (DIFFICULTY_TUNING[id] || DIFFICULTY_TUNING.medium).color;
                      const progress = Math.max(
                        0,
                        Math.min(AUTO_ARC_CALIBRATION_INTERVAL, Number(autoArcCalProgress[id]) || 0)
                      );
                      return (
                        <span
                          key={`arc-cal-progress-${id}`}
                          style={{
                            fontSize: 10,
                            letterSpacing: 0.75,
                            borderRadius: 999,
                            border: `1px solid ${color}66`,
                            background: "rgba(8,26,42,0.24)",
                            padding: "2px 8px",
                            color,
                          }}
                        >
                          {label.toUpperCase()} {progress}/{AUTO_ARC_CALIBRATION_INTERVAL}
                        </span>
                      );
                    })}
                  </div>
                  <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
                    <span
                      style={{
                        fontSize: 10,
                        letterSpacing: 0.9,
                        borderRadius: 999,
                        border: "1px solid rgba(120,210,255,0.45)",
                        background: "rgba(8,26,42,0.36)",
                        padding: "2px 8px",
                        color: "#9fe4ff",
                      }}
                    >
                      HIT {telemetryAutoTune.stats.makeRate}%
                    </span>
                    <span
                      style={{
                        fontSize: 10,
                        letterSpacing: 0.9,
                        borderRadius: 999,
                        border: "1px solid rgba(120,210,255,0.35)",
                        background: "rgba(8,26,42,0.28)",
                        padding: "2px 8px",
                        color: "#a9d5ef",
                      }}
                    >
                      RO {Math.round(telemetryAutoTune.stats.rimOutRate * 100)}%
                    </span>
                    <span
                      style={{
                        fontSize: 10,
                        letterSpacing: 0.9,
                        borderRadius: 999,
                        border: "1px solid rgba(120,210,255,0.35)",
                        background: "rgba(8,26,42,0.28)",
                        padding: "2px 8px",
                        color: "#a9d5ef",
                      }}
                    >
                      SW {telemetryAutoTune.stats.swishRate}%
                    </span>
                  </div>
                  <div style={{ display: "grid", gap: 4 }}>
                    {telemetryAutoTune.reasons.map((reason, idx) => (
                      <div key={`autotune-reason-${idx}`} style={{ fontSize: 10, color: "rgba(155,184,201,0.95)" }}>
                        • {reason}
                      </div>
                    ))}
                  </div>
                  {autoTunePatchEntries.length > 0 && (
                    <div style={{ display: "grid", gap: 4 }}>
                      {autoTunePatchEntries.map(([key, value]) => (
                        <div
                          key={`autotune-patch-${key}`}
                          style={{
                            fontSize: 10,
                            color: "#89ffcf",
                            letterSpacing: 0.5,
                            border: "1px solid rgba(80,190,150,0.38)",
                            borderRadius: 6,
                            padding: "4px 6px",
                            background: "rgba(7,28,22,0.28)",
                          }}
                        >
                          {String(key).replace(/([A-Z])/g, " $1").toUpperCase()}:{" "}
                          {typeof value === "number" ? value.toFixed(2) : String(value)}
                        </div>
                      ))}
                    </div>
                  )}
                  <button
                    className="btn"
                    style={{ padding: "5px 10px", fontSize: 10, justifySelf: "start" }}
                    disabled={!telemetryAutoTune.ready || autoTunePatchEntries.length === 0}
                    onClick={applyTelemetryRecommendation}
                  >
                    Apply Recommendation
                  </button>
                </div>
              </div>
            </div>

            <div className="footer-bar">
              <div className="tier-info">
                <span className="tier-chip">{playerRank}</span>
                <span className="stat-pill">XP <strong>{ladderPoints}</strong></span>
                <span className="stat-pill">Best <strong>{bestScore}</strong></span>
                {dailyMode && !practiceMode && (
                  <span className="stat-pill">Mode <strong>{dailyModifierLabel(dailyModifier)}</strong></span>
                )}
              </div>
              <button className="start-btn" onClick={startGame}>▶ START SIMULATOR</button>
            </div>

            <div className="achievementsRow">
              {[
                ["Release Artist", achievements.releaseArtist],
                ["Clean Shooter", achievements.cleanShooter],
                ["Clutch Finisher", achievements.clutchFinisher],
              ].map(([label, on]) => (
                <div
                  key={label}
                  className="achievementPill"
                  style={
                    on
                      ? {
                          borderColor: "rgba(127,255,200,0.6)",
                          color: "#b8ffe6",
                          boxShadow: "0 0 10px rgba(120,255,205,0.2)",
                        }
                      : null
                  }
                >
                  {on ? "✓ " : ""}
                  {label}
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {safeScreen === "game" && (
        <div
          className={`frame gameFrame ${safeScreen === "game" ? "gameFrameFullscreen" : ""}`}
          style={{
            "--hud-ratio": dynamicHudRatio,
            "--court-ratio": dynamicCourtRatio,
            "--hud-scale": dynamicHudScale,
            "--frame-height": `${gameFrameHeightPx}px`,
            "--court-fit-width": `${courtViewportWidth}px`,
            "--court-fit-height": `${courtViewportHeight}px`,
          }}
        >
          {!vrMode && (
            <div className="hud" style={{ gridTemplateColumns: dynamicHudColumns }}>
              <div className={`scoreCol ${(livePlayer?.state === "idle" || livePlayer?.state === "charging") ? "turn" : ""}`}>
                <div className="title">
                  {livePlayer?.avatar || "🧢"} PLAYER{" "}
                  <span className="badge" style={{ borderColor: `${playerRankColor}99`, color: playerRankColor }}>
                    {playerRank}
                  </span>
                </div>
                <div
                  className="scoreVal"
                  style={{
                    color: "#61ecff",
                    textShadow: "0 0 12px #2de1ff",
                    transform: `scale(${1 + (livePlayer?.scorePop || 0) * 0.14})`,
                  }}
                >
                  {hud.playerScore}
                </div>
                <div style={{ fontSize: 10, letterSpacing: 1.2, opacity: 0.72 }}>Manual</div>
                <div className={hud.playerFire ? "fire" : ""} style={{ letterSpacing: 1.5 }}>
                  STREAK {hud.playerStreak} {hud.playerFire ? "• FIRE 🔥" : ""}
                </div>
                <div className="streakMeter"><div className="streakFill" style={{ width: `${playerStreakMeter * 100}%` }} /></div>
                <div style={{ marginTop: 4, display: "grid", gap: 2 }}>
                  <div style={{ fontSize: 8, letterSpacing: 1.2, opacity: 0.78, color: "#89cfe8" }}>
                    PRESSURE • {playerPressureState}
                  </div>
                  <div
                    style={{
                      height: 4,
                      borderRadius: 999,
                      background: "rgba(255,255,255,0.1)",
                      overflow: "hidden",
                    }}
                  >
                    <div
                      style={{
                        width: `${playerPressure * 100}%`,
                        height: "100%",
                        borderRadius: 999,
                        background:
                          playerPressure > 0.82 && livePlayer?.state === "charging"
                            ? "linear-gradient(90deg, #ffd86c, #63ff9a, #00ffe1)"
                            : "linear-gradient(90deg, #3ea9ff, #67efff)",
                        boxShadow:
                          playerPressure > 0.82 && livePlayer?.state === "charging"
                            ? "0 0 12px rgba(135,255,210,0.85)"
                            : "0 0 8px rgba(95,225,255,0.65)",
                        transition: "width 90ms linear, box-shadow 140ms ease",
                      }}
                    />
                  </div>
                </div>
              </div>

              <div
                style={{
                  textAlign: "center",
                  minWidth: dynamicHudCenterMinWidth,
                  border: "1px solid rgba(120,220,255,0.09)",
                  padding: "1px 3px",
                  background: "rgba(8,20,33,0.12)",
                  backdropFilter: "blur(2px)",
                }}
              >
                <div className="title">{practiceMode ? "PRACTICE" : "TIME"}</div>
                <div style={{ fontSize: 7, letterSpacing: 1, opacity: 0.62, marginTop: 1 }}>
                  {practiceMode ? "OPEN GYM" : dailyMode ? "DAILY CHALLENGE" : singleLaneSession ? "SOLO SESSION" : "RANKED DUEL"}
                </div>
                <div style={{ fontSize: 7, letterSpacing: 0.95, marginTop: 1, color: "#9dd0e8" }}>
                  {practiceMode ? (movingRims ? "MOVING RIMS: ON" : "MOVING RIMS: OFF") : dailyModifierLabel(dailyMode ? dailyModifier : "none")}
                </div>
                <div
                  style={{
                    marginTop: 2,
                    fontFamily: "Orbitron, sans-serif",
                    fontSize: 11,
                    letterSpacing: 1.2,
                    color: comboMultiplier > 1 ? "#ffb870" : "#8ddfff",
                    textShadow:
                      comboMultiplier > 1
                        ? "0 0 12px rgba(255,180,110,0.8)"
                        : "0 0 9px rgba(140,220,255,0.55)",
                    transform: `scale(${comboScale})`,
                    transition: "transform 0.16s ease",
                  }}
                >
                  COMBO x{comboMultiplier}
                </div>
                <div
                  style={{
                    marginTop: 2,
                    fontSize: 8,
                    letterSpacing: 0.9,
                    color: "rgba(165,210,232,0.92)",
                  }}
                >
                  {`${currentDifficulty.label.toUpperCase()} DATA • HIT ${currentTelemetry.makeRate}% • SW ${currentTelemetry.swishRate}% • RO ${currentTelemetry.rimOuts}`}
                </div>
                <div
                  style={{
                    marginTop: 3,
                    display: "grid",
                    gridTemplateColumns: "42px auto",
                    gap: 8,
                    alignItems: "center",
                    justifyContent: "center",
                  }}
                >
                  <div
                    style={{
                      width: 42,
                      height: 42,
                      borderRadius: "50%",
                      background: `conic-gradient(${playerRankTrail} ${playerTierProgress.progress * 360}deg, rgba(255,255,255,0.14) 0)`,
                      padding: 3,
                    }}
                  >
                    <div
                      style={{
                        width: "100%",
                        height: "100%",
                        borderRadius: "50%",
                        border: `1px solid ${playerRankColor}AA`,
                        color: playerRankColor,
                        display: "grid",
                        placeItems: "center",
                        fontFamily: "Orbitron, sans-serif",
                        fontSize: 9,
                        letterSpacing: 1.1,
                        background: "rgba(5,14,24,0.95)",
                      }}
                    >
                      {playerRank.split(" ").map((part) => part[0]).join("").slice(0, 2)}
                    </div>
                  </div>
                  <div style={{ minWidth: 92 }}>
                    <div style={{ fontSize: 9, letterSpacing: 1.1, color: playerRankColor }}>
                      {playerRank.toUpperCase()}
                    </div>
                    <div className="streakMeter" style={{ marginTop: 3, height: 4 }}>
                      <div
                        className="streakFill"
                        style={{
                          width: `${Math.round(playerTierProgress.progress * 100)}%`,
                          background: `linear-gradient(90deg, ${playerRankTrail}, ${playerRankColor})`,
                        }}
                      />
                    </div>
                    <div style={{ fontSize: 8, letterSpacing: 0.9, marginTop: 2, color: "#95b9ce" }}>
                      {playerTierProgress.nextTier
                        ? `${playerTierProgress.xpToNext} XP TO ${playerTierProgress.nextTier.label.toUpperCase()}`
                        : "MAX TIER"}
                    </div>
                  </div>
                </div>
                {showHudDebug && (
                  <>
                <div
                  style={{
                    fontSize: 9,
                    letterSpacing: 1.3,
                    marginTop: 1,
                    color: currentDifficulty.color,
                    textShadow: `0 0 10px ${currentDifficulty.color}`,
                  }}
                >
                  DIFFICULTY: {currentDifficulty.label.toUpperCase()}
                </div>
                <div
                  style={{
                    fontSize: 9,
                    letterSpacing: 1.3,
                    marginTop: 1,
                    color: easyPlusActive ? "#9fffd9" : "#74a3bc",
                    textShadow: easyPlusActive ? "0 0 10px rgba(140,255,220,0.65)" : "none",
                  }}
                >
                  EASY+ ASSIST: {easyPlusActive ? "ACTIVE" : "OFF"}
                </div>
                <div
                  style={{
                    fontSize: 9,
                    letterSpacing: 1.3,
                    marginTop: 1,
                    color: "#ffd79f",
                    textShadow: "0 0 10px rgba(255,215,160,0.65)",
                  }}
                >
                  AIM MODE: {currentAimMode.label.toUpperCase()}
                </div>
                <div
                  style={{
                    fontSize: 9,
                    letterSpacing: 1.3,
                    marginTop: 1,
                    color: currentControl.color,
                    textShadow: `0 0 10px ${currentControl.color}`,
                  }}
                >
                  SHOT CONTROL: {currentControl.label.toUpperCase()}
                </div>
                <div
                  style={{
                    fontSize: 9,
                    letterSpacing: 1.3,
                    marginTop: 1,
                    color: "#9deeff",
                    textShadow: "0 0 10px rgba(130,240,255,0.6)",
                  }}
                >
                  AIM SENS: {Math.round(aimSensitivityScale * 100)}%
                </div>
                <div
                  style={{
                    fontSize: 9,
                    letterSpacing: 1.3,
                    marginTop: 1,
                    color: currentPhysics.color,
                    textShadow: `0 0 10px ${currentPhysics.color}`,
                  }}
                >
                  PHYSICS: {currentPhysics.label.toUpperCase()}
                </div>
                <div
                  style={{
                    fontSize: 9,
                    letterSpacing: 1.2,
                    marginTop: 1,
                    color: "#9dffd2",
                    textShadow: "0 0 10px rgba(157,255,210,0.45)",
                  }}
                >
                  SHOT FEEL: {currentShotTuningLabel.toUpperCase()}
                </div>
                <div
                  style={{
                    fontSize: 9,
                    letterSpacing: 1.2,
                    marginTop: 1,
                    color: "#f3b8ff",
                    textShadow: "0 0 10px rgba(243,184,255,0.5)",
                  }}
                >
                  VISUAL LOOK: {currentVisualTuningLabel.toUpperCase()}
                </div>
                  </>
                )}
                <div
                  style={{
                    width: dynamicHudTimerRingSize,
                    height: dynamicHudTimerRingSize,
                    margin: "2px auto 0",
                    borderRadius: "50%",
                    background: timerRing,
                    padding: 3,
                  }}
                >
                  <div style={{ width: "100%", height: "100%", borderRadius: "50%", background: "rgba(5,15,24,0.8)", display: "flex", alignItems: "center", justifyContent: "center", boxShadow: "inset 0 0 12px rgba(0,120,170,0.2)" }}>
                    <div
                      className="timer"
                      style={{
                        fontSize: dynamicHudTimerFontSize,
                        color: practiceMode ? "#69dfff" : clutchActive ? "#ff5252" : "#ff9b3a",
                      }}
                    >
                      {practiceMode ? "∞" : timeLeft}
                    </div>
                  </div>
                </div>
                {clutchActive && (
                  <div style={{ marginTop: 3, fontSize: 10, letterSpacing: 1.6, color: "#ff9d88", textShadow: "0 0 10px #ff6050" }}>
                    CLUTCH TIME
                  </div>
                )}
                <button
                  className="btn"
                  style={{
                    fontSize: dynamicHudQuitFontSize,
                    padding: `${dynamicHudQuitPadY}px ${dynamicHudQuitPadX}px`,
                    marginTop: 4,
                  }}
                  onClick={() => {
                    sessionEpochRef.current += 1;
                    sessionRef.current = { id: null, epoch: sessionEpochRef.current };
                    setScreen("menu");
                  }}
                >
                  QUIT
                </button>
              </div>

              {liveOpponent ? (
                <div className={`scoreCol ${(liveOpponent?.state === "idle" || liveOpponent?.state === "charging") ? "turn" : ""}`}>
                  <div className="title">
                    {liveOpponent?.avatar || "🤖"} OPPONENT{" "}
                    <span className="badge" style={{ borderColor: `${oppRankColor}99`, color: oppRankColor }}>
                      {oppRank}
                    </span>
                  </div>
                  <div
                    className="scoreVal"
                    style={{
                      color: "#ff8e6b",
                      textShadow: "0 0 12px #ff6a49",
                      transform: `scale(${1 + (liveOpponent?.scorePop || 0) * 0.14})`,
                    }}
                  >
                    {hud.opponentScore}
                  </div>
                  <div style={{ fontSize: 10, letterSpacing: 1.2, opacity: 0.72 }}>{aiStyleLabel(hud.opponentStyle)}</div>
                  <div className={hud.opponentFire ? "fire" : ""} style={{ letterSpacing: 1.5 }}>
                    STREAK {hud.opponentStreak} {hud.opponentFire ? "• FIRE 🔥" : ""}
                  </div>
                  <div className="streakMeter"><div className="streakFill" style={{ width: `${oppStreakMeter * 100}%` }} /></div>
                  <div style={{ marginTop: 4, display: "grid", gap: 2 }}>
                    <div style={{ fontSize: 8, letterSpacing: 1.2, opacity: 0.78, color: "#f2b6a6" }}>
                      PRESSURE • {opponentPressureState}
                    </div>
                    <div
                      style={{
                        height: 4,
                        borderRadius: 999,
                        background: "rgba(255,255,255,0.1)",
                        overflow: "hidden",
                      }}
                    >
                      <div
                        style={{
                          width: `${opponentPressure * 100}%`,
                          height: "100%",
                          borderRadius: 999,
                          background:
                            opponentPressure > 0.82 && liveOpponent?.state === "charging"
                              ? "linear-gradient(90deg, #ffd48a, #ff9f6f, #ff7360)"
                              : "linear-gradient(90deg, #ffb387, #ff8d72)",
                          boxShadow:
                            opponentPressure > 0.82 && liveOpponent?.state === "charging"
                              ? "0 0 12px rgba(255,170,125,0.85)"
                              : "0 0 8px rgba(255,145,110,0.65)",
                          transition: "width 90ms linear, box-shadow 140ms ease",
                        }}
                      />
                    </div>
                  </div>
                </div>
              ) : (
                <div className="scoreCol" style={{ display: "grid", placeItems: "center" }}>
                  <div className="title">{practiceMode ? "PRACTICE MODE" : "ONE VIEW MODE"}</div>
                  <div style={{ fontSize: 12, letterSpacing: 1.2, opacity: 0.8, color: "#9dcbe6" }}>
                    {movingRims ? "Moving rims active" : "Static rims"}
                  </div>
                </div>
              )}
            </div>
          )}

          <div className="courtWrap">
            <div className="courtViewport" style={{ width: `${courtViewportWidth}px`, height: `${courtViewportHeight}px` }}>
              <canvas
                ref={canvasRef}
                width={CW}
                height={CH}
                style={{
                  width: "100%",
                  height: "100%",
                  display: "block",
                  background: "#050e18",
                  touchAction: "none",
                  userSelect: "none",
                }}
              />
            </div>
          </div>
        </div>
      )}

      {safeScreen === "gameover" && (
        <div className="frame" style={{ padding: 32, textAlign: "center" }}>
          <h2
            style={{
              fontFamily: "Orbitron, sans-serif",
              fontSize: "clamp(34px, 6vw, 62px)",
              letterSpacing: 5,
              margin: 0,
              color: "#ff8f46",
              textShadow: "0 0 16px #ff7e2f",
            }}
          >
            GAME OVER
          </h2>

          <p
            style={{
              marginTop: 8,
              fontSize: 28,
              fontFamily: "Orbitron, sans-serif",
              color: winner === "YOU WIN" ? "#6fffaa" : winner === "TIE GAME" ? "#8acfff" : "#ff6b6b",
            }}
          >
            {winner}
          </p>
          {finalState?.dailyMode && (
            <p
              style={{
                marginTop: 4,
                fontSize: 15,
                letterSpacing: 1.2,
                color: finalState.challengeCleared ? "#7bffbf" : "#ff9b8a",
              }}
            >
              Daily Challenge: {finalState.challengeCleared ? "CLEARED" : "FAILED"} (target 34+)
            </p>
          )}
          {finalState?.dailyMode && (
            <p style={{ marginTop: 2, fontSize: 12, letterSpacing: 1.1, color: "#96bdd2" }}>
              Modifier: {dailyModifierLabel(finalState.dailyModifier || "none")}
            </p>
          )}
          <p
            style={{
              marginTop: 2,
              fontSize: 12,
              letterSpacing: 1.1,
              color:
                (DIFFICULTY_TUNING[finalState?.shotDifficulty || shotDifficulty] || DIFFICULTY_TUNING.medium).color,
            }}
          >
            Difficulty: {(DIFFICULTY_TUNING[finalState?.shotDifficulty || shotDifficulty] || DIFFICULTY_TUNING.medium).label}
          </p>
          <p
            style={{
              marginTop: 2,
              fontSize: 12,
              letterSpacing: 1.1,
              color:
                (CONTROL_PROFILES[finalState?.controlProfile || controlProfile] || CONTROL_PROFILES.rookie).color,
            }}
          >
            Shot Control: {(CONTROL_PROFILES[finalState?.controlProfile || controlProfile] || CONTROL_PROFILES.rookie).label}
          </p>
          <p style={{ marginTop: 2, fontSize: 12, letterSpacing: 1.1, color: "#9dffd2" }}>
            Shot Feel: {currentShotTuningLabel}
          </p>
          {!singleLaneSession && finalState?.aiStyle && (
            <p style={{ marginTop: 2, fontSize: 12, letterSpacing: 1.1, color: "#96bdd2" }}>
              Opponent style: {aiStyleLabel(finalState.aiStyle)}
            </p>
          )}

          <div
            style={{
              marginTop: 16,
              display: "flex",
              justifyContent: "center",
              gap: 26,
              flexWrap: "wrap",
            }}
          >
            <div style={{ border: "1px solid rgba(110,220,255,0.3)", padding: "12px 20px", minWidth: 180 }}>
              <div className="title">PLAYER</div>
              <div className="scoreVal" style={{ color: "#61ecff" }}>
                {finalState?.playerScore ?? hud.playerScore}
              </div>
            </div>
            <div style={{ border: "1px solid rgba(255,155,110,0.35)", padding: "12px 20px", minWidth: 180 }}>
              <div className="title">{singleLaneSession ? "XP" : "OPPONENT"}</div>
              <div className="scoreVal" style={{ color: "#ff8e6b" }}>
                {singleLaneSession ? ladderPoints : finalState?.opponentScore ?? hud.opponentScore}
              </div>
            </div>
          </div>

          <div style={{ marginTop: 14, fontSize: 15, letterSpacing: 1.2, color: finalState?.rankDelta >= 0 ? "#91ffd0" : "#ff9a89" }}>
            Rank Delta: {finalState?.rankDelta >= 0 ? "+" : ""}{finalState?.rankDelta ?? 0}
          </div>
          <div style={{ marginTop: 6, fontSize: 13, letterSpacing: 1.2, color: playerRankColor }}>
            Tier: {playerRank}
          </div>

          {finalStats && (
            <div style={{ marginTop: 16, display: "flex", gap: 10, justifyContent: "center", flexWrap: "wrap" }}>
              {[
                ["ACC", `${finalAccuracy}%`],
                ["PERFECT", `${finalPerfectRate}%`],
                ["STABILITY", `${finalStability}%`],
                ["RIM IN", `${finalStats.rimIn || 0}`],
                ["CLUTCH PTS", `${finalStats.clutchPoints || 0}`],
              ].map(([k, v]) => (
                <div key={k} style={{ border: "1px solid rgba(120,210,255,0.28)", minWidth: 108, padding: "8px 10px" }}>
                  <div className="title">{k}</div>
                  <div style={{ fontFamily: "Orbitron, sans-serif", fontSize: 20, marginTop: 3 }}>{v}</div>
                </div>
              ))}
            </div>
          )}

          <div style={{ marginTop: 24, display: "flex", gap: 14, justifyContent: "center", flexWrap: "wrap" }}>
            <button className="btn" style={{ fontSize: 18, padding: "12px 26px" }} onClick={startGame}>
              PLAY AGAIN
            </button>
            <button
              className="btn"
              style={{ fontSize: 16, padding: "12px 22px", borderColor: "#ff8f6a", color: "#ffad8e" }}
              onClick={() => {
                sessionEpochRef.current += 1;
                sessionRef.current = { id: null, epoch: sessionEpochRef.current };
                setScreen("menu");
              }}
            >
              MENU
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
