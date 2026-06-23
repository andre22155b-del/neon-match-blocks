(function (root, factory) {
  const api = factory();
  if (typeof module !== "undefined" && module.exports) {
    module.exports = api;
  }
  root.NFGHybridCore = api;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  function getCellColor(cell) {
    return cell && typeof cell.color === "string" ? cell.color : "";
  }

  function clearGoalGridRows(cells, rows, cols, collapseColumns) {
    let totalCleared = 0;

    while (true) {
      const filledRows = [];
      for (let row = 0; row < rows; row += 1) {
        let full = true;
        for (let col = 0; col < cols; col += 1) {
          if (!cells[row][col]) {
            full = false;
            break;
          }
        }
        if (full) {
          filledRows.push(row);
        }
      }

      if (filledRows.length === 0) {
        break;
      }

      for (const row of filledRows) {
        for (let col = 0; col < cols; col += 1) {
          cells[row][col] = null;
        }
      }
      totalCleared += filledRows.length;
      collapseColumns(cells);
    }

    return totalCleared;
  }

  function fillSparseGoalGrid(cells, rows, cols, createPiece, palette, rng = Math.random) {
    const occupied = cells.flat().filter(Boolean).length;
    if (occupied >= 8) {
      return cells;
    }

    for (let row = rows - 1; row >= Math.max(0, rows - 3); row -= 1) {
      const forcedGap = (row + Math.floor(rng() * cols)) % cols;
      for (let col = 0; col < cols; col += 1) {
        if (col === forcedGap) {
          continue;
        }
        if (!cells[row][col]) {
          cells[row][col] = createPiece(palette[(row + col) % palette.length]);
        }
      }
    }

    return cells;
  }

  function collectConnectedColorGroup(cells, rows, cols, startRow, startCol, visited = new Set()) {
    const startCell = cells[startRow]?.[startCol];
    const color = getCellColor(startCell);
    if (!color) {
      return [];
    }

    const stack = [{ row: startRow, col: startCol }];
    const group = [];

    while (stack.length) {
      const current = stack.pop();
      const key = `${current.row}:${current.col}`;
      if (visited.has(key)) {
        continue;
      }
      visited.add(key);
      const cell = cells[current.row]?.[current.col];
      if (getCellColor(cell) !== color) {
        continue;
      }

      group.push({
        row: current.row,
        col: current.col,
        color
      });

      if (current.row > 0) {
        stack.push({ row: current.row - 1, col: current.col });
      }
      if (current.row < rows - 1) {
        stack.push({ row: current.row + 1, col: current.col });
      }
      if (current.col > 0) {
        stack.push({ row: current.row, col: current.col - 1 });
      }
      if (current.col < cols - 1) {
        stack.push({ row: current.row, col: current.col + 1 });
      }
    }

    return group;
  }

  function resolveGoalGridMatchHit(cells, rows, cols, options) {
    const {
      hitRow = 0,
      hitCol = 0,
      minGroupSize = 3,
      collapseColumns
    } = options || {};

    const hitCell = cells[hitRow]?.[hitCol];
    const hitColor = getCellColor(hitCell);
    if (!hitColor || typeof collapseColumns !== "function") {
      return {
        hitColor,
        hitCount: 0,
        matchedCount: 0,
        clearEvents: 0,
        cascadeCount: 0,
        largestGroup: 0,
        totalDestroyed: 0,
        groups: [],
        boardEmpty: cells.flat().every((cell) => !cell)
      };
    }

    cells[hitRow][hitCol] = null;

    const groups = [];
    let matchedCount = 0;
    let clearEvents = 0;
    let largestGroup = 0;
    let cascadeCount = 0;
    let affectedCols = new Set([hitCol]);

    const initialVisited = new Set();
    const initialGroupMap = new Map();
    const neighbors = [
      { row: hitRow - 1, col: hitCol },
      { row: hitRow + 1, col: hitCol },
      { row: hitRow, col: hitCol - 1 },
      { row: hitRow, col: hitCol + 1 }
    ];

    for (const neighbor of neighbors) {
      if (neighbor.row < 0 || neighbor.row >= rows || neighbor.col < 0 || neighbor.col >= cols) {
        continue;
      }
      if (getCellColor(cells[neighbor.row]?.[neighbor.col]) !== hitColor) {
        continue;
      }
      const group = collectConnectedColorGroup(cells, rows, cols, neighbor.row, neighbor.col, initialVisited);
      for (const cell of group) {
        initialGroupMap.set(`${cell.row}:${cell.col}`, cell);
      }
    }

    const initialGroup = Array.from(initialGroupMap.values());
    if (initialGroup.length >= minGroupSize) {
      clearEvents += 1;
      matchedCount += initialGroup.length;
      largestGroup = Math.max(largestGroup, initialGroup.length);
      cascadeCount = 1;
      groups.push(initialGroup);
      for (const cell of initialGroup) {
        cells[cell.row][cell.col] = null;
        affectedCols.add(cell.col);
      }
    }

    collapseColumns(cells);

    while (affectedCols.size > 0) {
      const visited = new Set();
      const cascadeGroups = [];
      for (let row = 0; row < rows; row += 1) {
        for (let col = 0; col < cols; col += 1) {
          if (!cells[row][col]) {
            continue;
          }
          const key = `${row}:${col}`;
          if (visited.has(key)) {
            continue;
          }
          const group = collectConnectedColorGroup(cells, rows, cols, row, col, visited);
          if (
            group.length >= minGroupSize &&
            group.some((cell) => affectedCols.has(cell.col))
          ) {
            cascadeGroups.push(group);
          }
        }
      }

      if (cascadeGroups.length === 0) {
        break;
      }

      const nextAffectedCols = new Set();
      cascadeCount += 1;
      for (const group of cascadeGroups) {
        clearEvents += 1;
        matchedCount += group.length;
        largestGroup = Math.max(largestGroup, group.length);
        groups.push(group);
        for (const cell of group) {
          cells[cell.row][cell.col] = null;
          nextAffectedCols.add(cell.col);
        }
      }
      collapseColumns(cells);
      affectedCols = nextAffectedCols;
    }

    return {
      hitColor,
      hitCount: 1,
      matchedCount,
      clearEvents,
      cascadeCount,
      largestGroup,
      totalDestroyed: 1 + matchedCount,
      groups,
      boardEmpty: cells.flat().every((cell) => !cell)
    };
  }

  function computeGridScoreBreakdown(options) {
    const {
      hitCount = 0,
      matchedCount = 0,
      hitPointBase = 1,
      matchPointBase = 1
    } = options || {};

    const hitPoints = hitCount * hitPointBase;
    const matchPoints = matchedCount * matchPointBase;

    return {
      hitPoints,
      matchPoints,
      total: hitPoints + matchPoints
    };
  }

  function computeGridBonusBreakdown(options) {
    return computeGridScoreBreakdown(options);
  }

  return {
    clearGoalGridRows,
    fillSparseGoalGrid,
    collectConnectedColorGroup,
    resolveGoalGridMatchHit,
    computeGridScoreBreakdown,
    computeGridBonusBreakdown
  };
});
