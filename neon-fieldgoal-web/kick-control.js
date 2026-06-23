(function (root, factory) {
  const api = factory();
  if (typeof module !== "undefined" && module.exports) {
    module.exports = api;
  }
  root.NFGKickControl = api;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
  }

  function lerp(a, b, t) {
    return a + (b - a) * t;
  }

  function finiteOr(value, fallback) {
    return Number.isFinite(value) ? value : fallback;
  }

  function shapeSignedValue(rawValue, deadzone, exponent = 1.18) {
    const sign = rawValue < 0 ? -1 : 1;
    const magnitude = Math.abs(rawValue);
    if (magnitude <= deadzone) {
      return 0;
    }
    const normalized = clamp((magnitude - deadzone) / Math.max(1 - deadzone, 0.0001), 0, 1);
    return sign * Math.pow(normalized, exponent);
  }

  function getReleaseGrade(quality) {
    if (quality >= 0.88) {
      return "ELITE RELEASE";
    }
    if (quality >= 0.74) {
      return "CLEAN RELEASE";
    }
    if (quality >= 0.56) {
      return "RUSHED RELEASE";
    }
    return "OFF-ANGLE RELEASE";
  }

  function normalizeReleaseProfile(profile = {}) {
    const quality = clamp(finiteOr(profile.quality, 0.82), 0, 1);
    const verticality = clamp(finiteOr(profile.verticality, 0.82), 0, 1);
    const commitment = clamp(finiteOr(profile.commitment, 0.84), 0, 1);
    const lateralBias = clamp(finiteOr(profile.lateralBias, 0), -0.45, 0.45);
    return {
      quality,
      grade: profile.grade || getReleaseGrade(quality),
      verticality,
      commitment,
      curveScale: finiteOr(profile.curveScale, lerp(0.86, 1.08, quality)),
      aimTightness: finiteOr(profile.aimTightness, lerp(0.9, 1.02, quality)),
      lateralBias,
      arcLift: finiteOr(profile.arcLift, (verticality - 0.62) * 26 + (quality - 0.68) * 18),
      dropPenalty: finiteOr(profile.dropPenalty, Math.max(0, 0.72 - quality) * 28),
      oneThumbMode: !!profile.oneThumbMode,
      laneTargetX: finiteOr(profile.laneTargetX, 0.5)
    };
  }

  function computeKickGesture(options = {}) {
    const config = options.config || {};
    const startX = finiteOr(options.startX, 0);
    const startY = finiteOr(options.startY, 0);
    const currentX = finiteOr(options.currentX, startX);
    const currentY = finiteOr(options.currentY, startY);
    const startPlayerX = finiteOr(options.startPlayerX, 0.5);
    const padWidth = Math.max(finiteOr(options.padWidth, 240), 160);
    const oneThumbMode = !!options.oneThumbMode;
    const dx = currentX - startX;
    const dy = startY - currentY;

    const powerDeadzonePx = Math.max(8, finiteOr(config.kickPowerDeadzonePx, 18));
    const powerRangePx = Math.max(80, finiteOr(config.kickPowerRangePx, 176));
    const powerNorm = clamp((dy - powerDeadzonePx) / powerRangePx, 0, 1);
    const power = Math.pow(powerNorm, 0.92);

    const aimRangePx = Math.max(120, padWidth * finiteOr(config.kickAimRangeFactor, 0.34));
    const aimDeadzonePx = Math.max(6, finiteOr(config.kickAimDeadzonePx, 10));
    const rawAim = clamp(dx / aimRangePx, -1.2, 1.2);
    const shapedAim = shapeSignedValue(rawAim, clamp(aimDeadzonePx / aimRangePx, 0, 0.28), 1.18);

    const laneDragRange = finiteOr(config.oneThumbLaneDragRange, 0.26);
    const laneRangePx = Math.max(180, padWidth * finiteOr(config.oneThumbLaneRangeFactor, 0.9));
    const laneDelta = clamp(dx / laneRangePx, -laneDragRange, laneDragRange);
    const laneTargetX = oneThumbMode
      ? clamp(startPlayerX + laneDelta, 0.16, 0.84)
      : clamp(startPlayerX, 0.16, 0.84);

    const aim = clamp(
      oneThumbMode
        ? shapedAim * 0.72 - (laneDelta / Math.max(laneDragRange, 0.0001)) * 0.12
        : shapedAim,
      -1,
      1
    );

    const swipeDistance = Math.hypot(dx, dy);
    const verticality = clamp(dy / Math.max(swipeDistance, 1), 0, 1);
    const commitment = clamp((dy - powerDeadzonePx * 0.5) / Math.max(100, powerRangePx * 0.78), 0, 1);
    const sidewaysPenalty = clamp(
      Math.abs(dx) / Math.max(oneThumbMode ? padWidth * 0.88 : padWidth * 0.52, 120),
      0,
      1
    );
    const laneCommit = oneThumbMode
      ? clamp(Math.abs(laneTargetX - startPlayerX) / Math.max(laneDragRange, 0.0001), 0, 1)
      : 0;
    const quality = clamp(
      verticality * 0.5 +
      commitment * 0.32 +
      (1 - sidewaysPenalty) * 0.18 -
      laneCommit * 0.04,
      0,
      1
    );

    const release = normalizeReleaseProfile({
      quality,
      verticality,
      commitment,
      lateralBias: clamp(rawAim, -1, 1) * (1 - quality) * (oneThumbMode ? 0.22 : 0.3),
      oneThumbMode,
      laneTargetX
    });

    return {
      dx,
      dy,
      power,
      aim,
      laneTargetX,
      release
    };
  }

  return {
    computeKickGesture,
    normalizeReleaseProfile,
    getReleaseGrade
  };
});
