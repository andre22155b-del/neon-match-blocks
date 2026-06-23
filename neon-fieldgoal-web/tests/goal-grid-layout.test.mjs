import { describe, expect, it } from "vitest";
import GoalGridLayout from "../goal-grid-layout.js";

describe("goal-grid-layout", () => {
  const goalAtPlane = {
    centerX: 200,
    goalBaseY: 140,
    postHalfWidth: 60,
    postHeight: 120,
    crossbarY: 207.2
  };

  it("centers the stack within the uprights instead of pinning it too high", () => {
    const frame = GoalGridLayout.computeGoalGridPlaneFrame(goalAtPlane, 9, 8);

    expect(frame.top).toBeGreaterThan(goalAtPlane.goalBaseY - goalAtPlane.postHeight * 0.04);
    expect(frame.bottom).toBeLessThan(goalAtPlane.crossbarY);
    expect(frame.bottom).toBeGreaterThan(goalAtPlane.goalBaseY);
    expect(frame.left).toBeLessThan(goalAtPlane.centerX);
    expect(frame.right).toBeGreaterThan(goalAtPlane.centerX);
  });

  it("finds the overlapping occupied impact cell", () => {
    const frame = GoalGridLayout.computeGoalGridPlaneFrame(goalAtPlane, 9, 8);
    const cells = Array.from({ length: 9 }, () => Array.from({ length: 8 }, () => null));
    cells[4][3] = { color: "#56f2ff" };
    cells[4][4] = { color: "#ff5dd8" };

    const planePoint = {
      x: frame.left + 3 * frame.cellWidth + frame.cellWidth * 0.5,
      y: frame.top + 4 * frame.cellHeight + frame.cellHeight * 0.5,
      radius: Math.min(frame.cellWidth, frame.cellHeight) * 0.32
    };

    const anchor = GoalGridLayout.findGoalGridImpactCell(cells, frame, planePoint, 9, 8);
    expect(anchor).toEqual({ row: 4, col: 3, color: "#56f2ff" });
  });

  it("treats points outside the stack frame as misses", () => {
    const frame = GoalGridLayout.computeGoalGridPlaneFrame(goalAtPlane, 9, 8);
    const outsidePoint = {
      x: frame.right + 20,
      y: frame.bottom + 20,
      radius: 3
    };

    expect(GoalGridLayout.planePointInsideGoalGrid(outsidePoint, frame)).toBe(false);
    expect(
      GoalGridLayout.findGoalGridImpactCell(
        Array.from({ length: 9 }, () => Array.from({ length: 8 }, () => ({ color: "#56f2ff" }))),
        frame,
        outsidePoint,
        9,
        8
      )
    ).toBeNull();
  });
});
