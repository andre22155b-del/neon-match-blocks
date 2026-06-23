import { describe, expect, it } from "vitest";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const {
  clearGoalGridRows,
  fillSparseGoalGrid,
  collectConnectedColorGroup,
  resolveGoalGridMatchHit,
  computeGridScoreBreakdown
} = require("../hybrid-logic.js");

function makeCell(id) {
  return { id, dropOffset: 0, color: "#00f5ff" };
}

function collapseColumns(cells) {
  const rows = cells.length;
  const cols = cells[0].length;
  for (let col = 0; col < cols; col += 1) {
    const stacked = [];
    for (let row = rows - 1; row >= 0; row -= 1) {
      const cell = cells[row][col];
      if (cell) {
        stacked.push({ cell, row });
      }
    }
    for (let row = rows - 1, index = 0; row >= 0; row -= 1) {
      const next = stacked[index];
      if (next) {
        cells[row][col] = next.cell;
        cells[row][col].dropOffset = Math.max(cells[row][col].dropOffset || 0, next.row - row);
        index += 1;
      } else {
        cells[row][col] = null;
      }
    }
  }
}

describe("hybrid goal-grid helpers", () => {
  it("clears full rows and collapses the remaining blocks", () => {
    const cells = [
      [null, null, null, null, null],
      [makeCell("a"), null, null, null, null],
      [makeCell("b"), makeCell("c"), makeCell("d"), makeCell("e"), makeCell("f")],
      [makeCell("g"), makeCell("h"), makeCell("i"), makeCell("j"), makeCell("k")]
    ];

    const cleared = clearGoalGridRows(cells, cells.length, cells[0].length, collapseColumns);

    expect(cleared).toBe(2);
    expect(cells[3][0]?.id).toBe("a");
    expect(cells[0].every((cell) => cell === null)).toBe(true);
  });

  it("fills sparse boards but keeps a forced gap in each seeded row", () => {
    const rows = 6;
    const cols = 5;
    const cells = Array.from({ length: rows }, () => Array.from({ length: cols }, () => null));
    const palette = ["#00f5ff", "#ff4cd7"];
    let rngCalls = 0;
    const rng = () => {
      rngCalls += 1;
      return 0;
    };

    fillSparseGoalGrid(cells, rows, cols, (color) => ({ color }), palette, rng);

    const bottomRows = cells.slice(rows - 3);
    for (const row of bottomRows) {
      const emptyCount = row.filter((cell) => !cell).length;
      expect(emptyCount).toBeGreaterThanOrEqual(1);
    }
    expect(rngCalls).toBeGreaterThan(0);
  });

  it("collects connected groups by color using 4-direction adjacency", () => {
    const cells = [
      [makeCell("a"), makeCell("b"), null],
      [{ ...makeCell("c"), color: "#ff5dd8" }, { ...makeCell("d"), color: "#ff5dd8" }, null],
      [null, { ...makeCell("e"), color: "#ff5dd8" }, makeCell("f")]
    ];

    const group = collectConnectedColorGroup(cells, 3, 3, 1, 0);

    expect(group).toHaveLength(3);
    expect(group.every((cell) => cell.color === "#ff5dd8")).toBe(true);
  });

  it("resolves a hit into a direct break plus same-color clear and cascade", () => {
    const x = "#56f2ff";
    const p = "#ff5dd8";
    const cells = [
      [null, null, null, null],
      [null, { ...makeCell("p1"), color: p }, null, null],
      [{ ...makeCell("x1"), color: x }, { ...makeCell("x2"), color: x }, { ...makeCell("x3"), color: x }, null],
      [{ ...makeCell("x4"), color: x }, { ...makeCell("p2"), color: p }, { ...makeCell("p3"), color: p }, { ...makeCell("p4"), color: p }]
    ];

    const resolved = resolveGoalGridMatchHit(cells, 4, 4, {
      hitRow: 2,
      hitCol: 1,
      minGroupSize: 3,
      collapseColumns
    });

    expect(resolved.hitCount).toBe(1);
    expect(resolved.matchedCount).toBe(7);
    expect(resolved.clearEvents).toBe(2);
    expect(resolved.cascadeCount).toBe(2);
    expect(resolved.totalDestroyed).toBe(8);
    expect(cells.flat().filter(Boolean)).toHaveLength(0);
  });

  it("computes hit and color-match score deterministically", () => {
    const bonus = computeGridScoreBreakdown({
      hitCount: 1,
      matchedCount: 6,
      gridBonusBoost: 2,
      hitPointBase: 1,
      matchPointBase: 1
    });

    expect(bonus).toEqual({
      hitPoints: 1,
      matchPoints: 6,
      total: 7
    });
  });
});
