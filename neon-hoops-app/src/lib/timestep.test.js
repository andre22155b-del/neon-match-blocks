import { describe, expect, it } from "vitest";
import {
  clampValue,
  computeAdaptiveQuality,
  computeFrameDelta,
  lerpValue,
  stepFixedAccumulator,
} from "./timestep.js";

describe("timestep utilities", () => {
  it("clamps and lerps values correctly", () => {
    expect(clampValue(12, 0, 10)).toBe(10);
    expect(clampValue(-3, 0, 10)).toBe(0);
    expect(clampValue(4, 0, 10)).toBe(4);
    expect(lerpValue(0, 10, 0.25)).toBe(2.5);
  });

  it("computes frame delta with clamping and guards", () => {
    expect(computeFrameDelta(1016, 1000)).toBeCloseTo(0.016, 5);
    expect(computeFrameDelta(1100, 1000, 0.05)).toBe(0.05);
    expect(computeFrameDelta(900, 1000)).toBe(0);
    expect(computeFrameDelta(Number.NaN, 1000)).toBe(0);
    expect(computeFrameDelta(1000, Number.POSITIVE_INFINITY)).toBe(0);
  });

  it("adapts quality to frame delta", () => {
    const fast = computeAdaptiveQuality(1, 0.016);
    const slow = computeAdaptiveQuality(1, 0.03);
    expect(fast).toBeLessThanOrEqual(1);
    expect(fast).toBeGreaterThan(0.9);
    expect(slow).toBeLessThan(fast);

    const custom = computeAdaptiveQuality(0.8, 0.03, 0.01, 0.5, 0.5);
    expect(custom).toBe(0.65);
  });

  it("steps fixed accumulator with sane defaults", () => {
    const out = stepFixedAccumulator(0, 0.033, 1.2, 1 / 90, 8);
    expect(out.steps).toBeGreaterThanOrEqual(1);
    expect(out.accumulator).toBeGreaterThanOrEqual(0);
    expect(out.accumulator).toBeLessThan(1 / 90);
  });

  it("handles invalid values and spiral guard", () => {
    const invalid = stepFixedAccumulator(Number.NaN, Number.NaN, Number.NaN, Number.NaN, Number.NaN);
    expect(invalid.steps).toBe(0);
    expect(invalid.accumulator).toBe(0);
    expect(invalid.simDt).toBeCloseTo(1 / 90, 5);

    const spiral = stepFixedAccumulator(0, 1.5, 4, 1 / 120, 3);
    expect(spiral.steps).toBe(3);
    expect(spiral.accumulator).toBeLessThanOrEqual(1 / 120);
  });
});
