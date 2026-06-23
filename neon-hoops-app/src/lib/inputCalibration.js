const clamp = (v, min, max) => Math.max(min, Math.min(max, v));

export const detectMobilePlatform = (userAgent = "") => {
  const ua = String(userAgent || "").toLowerCase();
  if (/iphone|ipad|ipod/.test(ua)) return "ios";
  if (/android/.test(ua)) return "android";
  return "desktop";
};

export const createInputCalibration = ({
  platform = "desktop",
  maxTouchPoints = 0,
  devicePixelRatio = 1,
  viewportWidth = 390,
  viewportHeight = 844,
} = {}) => {
  const touchLike = maxTouchPoints > 0 || platform === "ios" || platform === "android";
  const minViewport = Math.max(280, Math.min(Math.abs(viewportWidth || 390), Math.abs(viewportHeight || 844)));
  const viewportScale = clamp(minViewport / 390, 0.78, 1.28);
  const dprScale = clamp((devicePixelRatio || 1) / 2, 0.72, 1.34);

  let smoothingRate = 22;
  let powerDenomMul = 1;
  let lateralMul = 1;
  let verticalMul = 1;
  let swipeDistanceMul = 1;
  let swipeTimeMul = 1;
  let lateralDenomMul = 1;
  let powerResponseMul = 1;

  if (platform === "ios") {
    smoothingRate = 30;
    powerDenomMul = 1.02;
    lateralMul = 1.04;
    verticalMul = 1.02;
    swipeDistanceMul = 1.06;
    swipeTimeMul = 0.96;
    lateralDenomMul = 1.02;
    powerResponseMul = 1.04;
  } else if (platform === "android") {
    smoothingRate = 34;
    powerDenomMul = 1.1;
    lateralMul = 1.08;
    verticalMul = 1.05;
    swipeDistanceMul = 1.12;
    swipeTimeMul = 1.05;
    lateralDenomMul = 1.06;
    powerResponseMul = 1.08;
  } else {
    smoothingRate = 22;
    powerDenomMul = 0.96;
    lateralMul = 0.96;
    verticalMul = 1;
    swipeDistanceMul = 1;
    swipeTimeMul = 1;
    lateralDenomMul = 0.98;
    powerResponseMul = 0.96;
  }

  const viewportComp = clamp(1 + (1 - viewportScale) * 0.22, 0.9, 1.16);
  const dprComp = clamp(1 + (dprScale - 1) * 0.12, 0.92, 1.16);

  return {
    platform,
    touchLike,
    smoothingRate: clamp(smoothingRate * dprComp, 18, 44),
    powerDenomMul: clamp(powerDenomMul * viewportComp, 0.82, 1.24),
    lateralMul: clamp(lateralMul, 0.82, 1.24),
    verticalMul: clamp(verticalMul, 0.82, 1.24),
    lateralDenomMul: clamp(lateralDenomMul * viewportComp, 0.86, 1.2),
    powerResponseMul: clamp(powerResponseMul, 0.82, 1.24),
    swipeDistanceMul: clamp(swipeDistanceMul * viewportComp, 0.88, 1.2),
    swipeTimeMul: clamp(swipeTimeMul, 0.84, 1.2),
    steadyBias: clamp(touchLike ? 1.08 : 0.98, 0.9, 1.14),
  };
};
