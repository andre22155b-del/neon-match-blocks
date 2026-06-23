export const clampValue = (v, min, max) => Math.max(min, Math.min(max, v));
export const lerpValue = (a, b, t) => a + (b - a) * t;

export function computeFrameDelta(nowMs, lastMs, maxFrameDelta = 0.08) {
  if (!Number.isFinite(nowMs) || !Number.isFinite(lastMs)) return 0;
  const raw = (nowMs - lastMs) / 1000;
  return clampValue(raw, 0, maxFrameDelta);
}

export function computeAdaptiveQuality(prevQuality, frameDelta, threshold = 0.02, lowQuality = 0.68, lerpAlpha = 0.18) {
  const target = frameDelta > threshold ? lowQuality : 1;
  return lerpValue(prevQuality, target, lerpAlpha);
}

export function stepFixedAccumulator(
  accumulator,
  frameDelta,
  gameSpeed,
  simDt = 1 / 90,
  maxSubsteps = 8
) {
  const safeAccumulator = Number.isFinite(accumulator) ? Math.max(0, accumulator) : 0;
  const safeFrameDelta = Number.isFinite(frameDelta) ? Math.max(0, frameDelta) : 0;
  const safeGameSpeed = Number.isFinite(gameSpeed) ? Math.max(0.1, gameSpeed) : 1;
  const safeSimDt = Number.isFinite(simDt) ? Math.max(0.001, simDt) : 1 / 90;
  const safeMaxSubsteps = Number.isFinite(maxSubsteps) ? Math.max(1, Math.floor(maxSubsteps)) : 8;

  let nextAccumulator = safeAccumulator + safeFrameDelta * safeGameSpeed;
  let steps = 0;

  while (nextAccumulator >= safeSimDt && steps < safeMaxSubsteps) {
    nextAccumulator -= safeSimDt;
    steps += 1;
  }

  // If we hit the substep ceiling, keep only a tiny remainder to prevent spiral-of-death feel.
  if (steps === safeMaxSubsteps && nextAccumulator > safeSimDt * 2) {
    nextAccumulator = safeSimDt;
  }

  return { steps, accumulator: nextAccumulator, simDt: safeSimDt };
}
