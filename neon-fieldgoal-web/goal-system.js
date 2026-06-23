(function (root, factory) {
  const api = factory();
  if (typeof module !== "undefined" && module.exports) {
    module.exports = api;
  }
  root.NFGGoalSystem = api;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
  }

  function lerp(a, b, t) {
    return a + (b - a) * t;
  }

  function finiteOr(value, fallback) {
    return Number.isFinite(value) ? value : fallback;
  }

  function computeGoalGeometry(options = {}) {
    const w = finiteOr(options.w, 0);
    const h = finiteOr(options.h, 0);
    const isWide = !!options.isWide;
    const horizonY = finiteOr(options.horizonY, h * 0.3);
    const lateral = finiteOr(options.lateral, 0);
    const goalSwayDisplay = finiteOr(options.goalSwayDisplay, 0);
    const yardLine = finiteOr(options.yardLine, 20);
    const config = options.config || {};
    const cameraGoalParallax = finiteOr(config.cameraGoalParallax, 0.28);
    const cameraPostWidthScale = finiteOr(config.cameraPostWidthScale, 0.13);
    const cameraPostHeightScale = finiteOr(config.cameraPostHeightScale, 0.26);

    const goalParallax = isWide ? cameraGoalParallax * 0.76 : cameraGoalParallax;
    const goalCenterX = w * 0.5 - lateral * w * goalParallax + goalSwayDisplay * w * 0.18;
    const goalBaseY = horizonY + (isWide ? 26 : 24);
    const goalScale = clamp(
      (isWide ? 1.18 : 1.1) - (yardLine - 20) * (isWide ? 0.0034 : 0.0058),
      isWide ? 0.9 : 0.78,
      isWide ? 1.18 : 1.1
    );
    const postHalfWidth = w * (isWide ? 0.128 : cameraPostWidthScale * 0.84) * goalScale;
    const postHeight = h * (isWide ? 0.56 : cameraPostHeightScale * 1.06) * goalScale;
    const crossbarY = goalBaseY + postHeight * 0.48;

    return {
      goalCenterX,
      goalBaseY,
      goalScale,
      postHalfWidth,
      postHeight,
      crossbarY
    };
  }

  function createGoalFrame(geom = {}) {
    const centerX = finiteOr(geom.goalCenterX, 0);
    const postHalfWidth = finiteOr(geom.postHalfWidth, 0);
    const crossbarY = finiteOr(geom.crossbarY, 0);
    const goalBaseY = finiteOr(geom.goalBaseY, 0);
    const postHeight = finiteOr(geom.postHeight, 0);
    return {
      centerX,
      leftX: centerX - postHalfWidth,
      rightX: centerX + postHalfWidth,
      crossbarY,
      uprightTopY: goalBaseY - postHeight * 0.04,
      goalBaseY,
      postHalfWidth,
      postHeight
    };
  }

  function isFiniteGoalGeometry(geom = {}) {
    return [
      geom.w,
      geom.h,
      geom.goalCenterX,
      geom.goalBaseY,
      geom.crossbarY,
      geom.postHalfWidth,
      geom.postHeight
    ].every(Number.isFinite) && geom.w > 4 && geom.h > 4 && geom.postHalfWidth > 0 && geom.postHeight > 0;
  }

  function computeGoalGridFrame(goalFrame = {}, rows = 1, cols = 1) {
    const safeRows = Math.max(1, rows || 1);
    const safeCols = Math.max(1, cols || 1);
    const uprightTopY = finiteOr(goalFrame.goalBaseY, 0) - finiteOr(goalFrame.postHeight, 0) * 0.04;
    const crossbarY = finiteOr(goalFrame.crossbarY, uprightTopY);
    const centerX = finiteOr(goalFrame.centerX, 0);
    const postHalfWidth = finiteOr(goalFrame.postHalfWidth, 0);
    const top = lerp(uprightTopY, crossbarY, 0.14);
    const bottom = lerp(uprightTopY, crossbarY, 0.92);
    const left = centerX - postHalfWidth * 0.95;
    const right = centerX + postHalfWidth * 0.95;
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

  return {
    computeGoalGeometry,
    createGoalFrame,
    isFiniteGoalGeometry,
    computeGoalGridFrame
  };
});
