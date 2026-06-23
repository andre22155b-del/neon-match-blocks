import { describe, expect, it } from "vitest";
import { createInputCalibration, detectMobilePlatform } from "./inputCalibration.js";

describe("inputCalibration", () => {
  it("detects ios/android/desktop platforms", () => {
    expect(detectMobilePlatform("Mozilla iPhone")).toBe("ios");
    expect(detectMobilePlatform("Mozilla Android")).toBe("android");
    expect(detectMobilePlatform("Mozilla Macintosh")).toBe("desktop");
  });

  it("handles missing user agent safely", () => {
    expect(detectMobilePlatform()).toBe("desktop");
  });

  it("builds ios touch profile", () => {
    const cal = createInputCalibration({
      platform: "ios",
      maxTouchPoints: 5,
      devicePixelRatio: 3,
      viewportWidth: 390,
      viewportHeight: 844,
    });
    expect(cal.touchLike).toBe(true);
    expect(cal.smoothingRate).toBeGreaterThan(22);
    expect(cal.platform).toBe("ios");
  });

  it("builds android touch profile", () => {
    const cal = createInputCalibration({
      platform: "android",
      maxTouchPoints: 10,
      devicePixelRatio: 2.8,
      viewportWidth: 412,
      viewportHeight: 915,
    });
    expect(cal.touchLike).toBe(true);
    expect(cal.powerDenomMul).toBeGreaterThan(1);
    expect(cal.swipeDistanceMul).toBeGreaterThan(1);
  });

  it("builds desktop pointer profile", () => {
    const cal = createInputCalibration({
      platform: "desktop",
      maxTouchPoints: 0,
      devicePixelRatio: 1,
      viewportWidth: 1440,
      viewportHeight: 900,
    });
    expect(cal.touchLike).toBe(false);
    expect(cal.powerDenomMul).toBeLessThan(1.1);
    expect(cal.powerResponseMul).toBeLessThanOrEqual(1);
  });

  it("clamps extreme viewport and dpr values", () => {
    const cal = createInputCalibration({
      platform: "android",
      maxTouchPoints: 10,
      devicePixelRatio: 12,
      viewportWidth: 120,
      viewportHeight: 160,
    });
    expect(cal.smoothingRate).toBeLessThanOrEqual(44);
    expect(cal.powerDenomMul).toBeLessThanOrEqual(1.24);
    expect(cal.lateralDenomMul).toBeLessThanOrEqual(1.2);
  });

  it("handles empty options safely", () => {
    const cal = createInputCalibration();
    expect(cal.platform).toBe("desktop");
    expect(typeof cal.smoothingRate).toBe("number");
    expect(typeof cal.steadyBias).toBe("number");
  });

  it("uses fallback viewport and dpr defaults when zeros are provided", () => {
    const cal = createInputCalibration({
      platform: "ios",
      maxTouchPoints: 0,
      devicePixelRatio: 0,
      viewportWidth: 0,
      viewportHeight: 0,
    });
    expect(cal.touchLike).toBe(true);
    expect(cal.smoothingRate).toBeGreaterThan(18);
  });
});
