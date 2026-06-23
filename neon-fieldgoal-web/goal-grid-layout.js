(function (root, factory) {
  const api = factory();
  if (typeof module !== "undefined" && module.exports) {
    module.exports = api;
  }
  root.NFGGoalGridLayout = api;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  function lerp(a, b, t) {
    return a + (b - a) * t;
  }

  function computeGoalGridPlaneFrame(goalAtPlane, rows, cols) {
    const safeRows = Math.max(1, rows || 1);
    const safeCols = Math.max(1, cols || 1);
    const uprightTopY = goalAtPlane.goalBaseY - goalAtPlane.postHeight * 0.04;
    const top = lerp(uprightTopY, goalAtPlane.crossbarY, 0.14);
    const bottom = lerp(uprightTopY, goalAtPlane.crossbarY, 0.92);
    const left = goalAtPlane.centerX - goalAtPlane.postHalfWidth * 0.95;
    const right = goalAtPlane.centerX + goalAtPlane.postHalfWidth * 0.95;
    const cellWidth = (right - left) / safeCols;
    const cellHeight = (bottom - top) / safeRows;
    const gap = Math.max(1.5, Math.min(cellWidth, cellHeight) * 0.045);
    return {
      top,
      bottom,
      left,
      right,
      rows: safeRows,
      cols: safeCols,
      cellWidth,
      cellHeight,
      gap,
      uprightTopY
    };
  }

  function planePointInsideGoalGrid(planePoint, frame) {
    if (!planePoint || !frame) {
      return false;
    }
    return (
      planePoint.x + planePoint.radius >= frame.left &&
      planePoint.x - planePoint.radius <= frame.right &&
      planePoint.y + planePoint.radius >= frame.top &&
      planePoint.y - planePoint.radius <= frame.bottom
    );
  }

  function findGoalGridImpactCell(cells, frame, planePoint, rows, cols) {
    if (!Array.isArray(cells) || !frame || !planePoint) {
      return null;
    }
    const safeRows = Math.max(0, rows || 0);
    const safeCols = Math.max(0, cols || 0);
    let anchor = null;
    let bestDistance = Number.POSITIVE_INFINITY;

    for (let row = 0; row < safeRows; row += 1) {
      for (let col = 0; col < safeCols; col += 1) {
        const cell = cells[row]?.[col];
        if (!cell) {
          continue;
        }
        const x = frame.left + col * frame.cellWidth + frame.gap;
        const y = frame.top + row * frame.cellHeight + frame.gap;
        const width = frame.cellWidth - frame.gap * 2;
        const height = frame.cellHeight - frame.gap * 2;
        const hit =
          planePoint.x + planePoint.radius >= x &&
          planePoint.x - planePoint.radius <= x + width &&
          planePoint.y + planePoint.radius >= y &&
          planePoint.y - planePoint.radius <= y + height;
        if (!hit) {
          continue;
        }
        const centerX = x + width * 0.5;
        const centerY = y + height * 0.5;
        const distance = Math.hypot(planePoint.x - centerX, planePoint.y - centerY);
        if (distance < bestDistance) {
          bestDistance = distance;
          anchor = { row, col, color: cell.color };
        }
      }
    }

    return anchor;
  }

  return {
    computeGoalGridPlaneFrame,
    planePointInsideGoalGrid,
    findGoalGridImpactCell
  };
});
