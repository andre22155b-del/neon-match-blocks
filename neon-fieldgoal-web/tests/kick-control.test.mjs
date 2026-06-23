import { describe, expect, it } from "vitest";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const KickControl = require("../kick-control.js");

describe("kick control", () => {
  it("keeps center aim stable inside the deadzone", () => {
    const gesture = KickControl.computeKickGesture({
      startX: 100,
      startY: 300,
      currentX: 104,
      currentY: 170,
      startPlayerX: 0.5,
      padWidth: 300,
      oneThumbMode: false,
      config: {}
    });

    expect(gesture.power).toBeGreaterThan(0.5);
    expect(gesture.aim).toBe(0);
  });

  it("grades a tall upward swipe as a strong release", () => {
    const gesture = KickControl.computeKickGesture({
      startX: 100,
      startY: 320,
      currentX: 122,
      currentY: 110,
      startPlayerX: 0.5,
      padWidth: 300,
      oneThumbMode: true,
      config: {
        oneThumbLaneDragRange: 0.26
      }
    });

    expect(gesture.release.quality).toBeGreaterThan(0.72);
    expect(["ELITE RELEASE", "CLEAN RELEASE"]).toContain(gesture.release.grade);
    expect(gesture.laneTargetX).toBeGreaterThan(0.5);
  });

  it("marks a flat diagonal swipe as a weaker release", () => {
    const gesture = KickControl.computeKickGesture({
      startX: 100,
      startY: 320,
      currentX: 220,
      currentY: 250,
      startPlayerX: 0.5,
      padWidth: 300,
      oneThumbMode: false,
      config: {}
    });

    expect(gesture.release.quality).toBeLessThan(0.6);
    expect(gesture.release.grade).toBe("OFF-ANGLE RELEASE");
    expect(Math.abs(gesture.aim)).toBeGreaterThan(0.2);
  });
});
