import { describe, expect, it } from "vitest";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const GoalSystem = require("../goal-system.js");

describe("goal system", () => {
  it("builds a narrower wide-field goal geometry", () => {
    const geom = GoalSystem.computeGoalGeometry({
      w: 1180,
      h: 530,
      isWide: true,
      horizonY: 130,
      lateral: 0,
      goalSwayDisplay: 0,
      yardLine: 20,
      config: {
        cameraGoalParallax: 0.28,
        cameraPostWidthScale: 0.13,
        cameraPostHeightScale: 0.26
      }
    });

    expect(geom.postHalfWidth).toBeLessThan(190);
    expect(geom.postHeight).toBeGreaterThan(300);
    expect(geom.crossbarY).toBeGreaterThan(geom.goalBaseY);
  });

  it("creates a finite live goal frame from geometry", () => {
    const frame = GoalSystem.createGoalFrame({
      goalCenterX: 590,
      goalBaseY: 156,
      crossbarY: 335,
      postHalfWidth: 178,
      postHeight: 374
    });

    expect(frame.leftX).toBe(412);
    expect(frame.rightX).toBe(768);
    expect(frame.uprightTopY).toBeCloseTo(141.04, 2);
  });
});
