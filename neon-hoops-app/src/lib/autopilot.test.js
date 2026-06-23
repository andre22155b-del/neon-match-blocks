import { describe, expect, it } from "vitest";
import { computeReleaseAssist, getSmartDefaults } from "./autopilot.js";

describe("autopilot", () => {
  it("returns platform smart defaults", () => {
    expect(getSmartDefaults({ platform: "ios" }).controlProfile).toBe("smooth");
    expect(getSmartDefaults({ platform: "android" }).visualTuningPreset).toBe("performance_mobile");
    expect(getSmartDefaults({ platform: "desktop" }).aimMode).toBe("pro");
  });

  it("keeps ai releases unchanged", () => {
    const result = computeReleaseAssist({
      timingOffset: 1.3,
      releaseType: "late",
      isAI: true,
    });
    expect(result.releaseType).toBe("late");
    expect(result.assistStrength).toBe(0);
  });

  it("promotes near-perfect player releases", () => {
    const result = computeReleaseAssist({
      timingOffset: 1.06,
      releaseType: "late",
      misses: 2,
      controlProfile: "rookie",
      aimMode: "casual",
      shotDifficulty: "easy",
      isAI: false,
    });
    expect(result.releaseType).toBe("perfect");
    expect(Math.abs(result.timingOffset)).toBeLessThan(1.06);
    expect(result.assistStrength).toBeGreaterThan(0.5);
  });

  it("keeps out-of-window releases unchanged", () => {
    const result = computeReleaseAssist({
      timingOffset: -2.2,
      releaseType: "early",
      misses: 0,
      controlProfile: "precision",
      aimMode: "pro",
      shotDifficulty: "hard",
    });
    expect(result.releaseType).toBe("early");
    expect(result.timingOffset).toBe(-2.2);
  });

  it("normalizes already perfect release", () => {
    const result = computeReleaseAssist({
      timingOffset: 0.3,
      releaseType: "normal",
      controlProfile: "balanced",
      aimMode: "pro",
      shotDifficulty: "medium",
    });
    expect(result.releaseType).toBe("perfect");
  });

  it("returns normal for middle assist band", () => {
    const result = computeReleaseAssist({
      timingOffset: 1.25,
      releaseType: "late",
      misses: 1,
      controlProfile: "balanced",
      aimMode: "casual",
      shotDifficulty: "medium",
    });
    expect(["normal", "perfect"]).toContain(result.releaseType);
    expect(result.assistStrength).toBeGreaterThan(0);
  });

  it("uses fallback assist tables for unknown profile and difficulty", () => {
    const result = computeReleaseAssist({
      timingOffset: 1.18,
      releaseType: "late",
      controlProfile: "unknown",
      shotDifficulty: "unknown",
      aimMode: "pro",
      misses: 0,
    });
    expect(result.assistStrength).toBeGreaterThan(0);
    expect(result.timingOffset).toBeLessThan(1.18);
  });

  it("damps negative offsets inside assist window", () => {
    const result = computeReleaseAssist({
      timingOffset: -1.14,
      releaseType: "early",
      misses: 1,
      controlProfile: "smooth",
      aimMode: "casual",
      shotDifficulty: "easy",
      isAI: false,
    });
    expect(result.timingOffset).toBeLessThan(0);
    expect(Math.abs(result.timingOffset)).toBeLessThan(1.14);
  });
});
