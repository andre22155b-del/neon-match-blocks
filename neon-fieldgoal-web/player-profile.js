(function (root, factory) {
  const api = factory();
  if (typeof module !== "undefined" && module.exports) {
    module.exports = api;
  }
  root.NFGPlayerProfile = api;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  function createDefaultSettings() {
    return {
      audioEnabled: true,
      shakeScale: 1,
      fxLevel: "full",
      hapticsEnabled: true
    };
  }

  function createDefaultProgression() {
    return {
      xp: 0,
      level: 1,
      totalGoals: 0,
      totalPerfects: 0,
      totalLongBombs: 0,
      totalGridDrops: 0,
      totalRowClears: 0
    };
  }

  function createDefaultCosmetics(skinDefs = []) {
    const defaultSkin = skinDefs[0]?.id || "cyan";
    const rewardUnlocks = {};
    for (const skin of skinDefs) {
      if (skin?.rewardOnly) {
        rewardUnlocks[skin.id] = false;
      }
    }
    return {
      selectedSkin: defaultSkin,
      rewardUnlocks
    };
  }

  function parseStatNumber(value, fallback = 0) {
    const parsed = Number.parseInt(value || "0", 10);
    return Number.isFinite(parsed) ? parsed : fallback;
  }

  function hydrateSettings(saved) {
    const settings = createDefaultSettings();
    if (!saved || typeof saved !== "object") {
      return settings;
    }
    settings.audioEnabled = saved.audioEnabled !== false;
    settings.shakeScale = saved.shakeScale === 0.45 ? 0.45 : 1;
    settings.fxLevel = saved.fxLevel === "low" ? "low" : "full";
    settings.hapticsEnabled = saved.hapticsEnabled !== false;
    return settings;
  }

  function hydrateProgression(saved, getTierForXp, tiers = []) {
    const progression = createDefaultProgression();
    if (saved && typeof saved === "object") {
      progression.xp = parseStatNumber(saved.xp, 0);
      progression.level = Math.max(1, parseStatNumber(saved.level, 1));
      progression.totalGoals = parseStatNumber(saved.totalGoals, 0);
      progression.totalPerfects = parseStatNumber(saved.totalPerfects, 0);
      progression.totalLongBombs = parseStatNumber(saved.totalLongBombs, 0);
      progression.totalGridDrops = parseStatNumber(saved.totalGridDrops, 0);
      progression.totalRowClears = parseStatNumber(saved.totalRowClears, 0);
    }
    if (typeof getTierForXp === "function" && tiers.length > 0) {
      progression.level = getTierForXp(progression.xp, tiers)?.level || progression.level;
    }
    return progression;
  }

  function hydrateCosmetics(saved, skinDefs = []) {
    const cosmetics = createDefaultCosmetics(skinDefs);
    if (saved && typeof saved === "object" && typeof saved.selectedSkin === "string") {
      cosmetics.selectedSkin = saved.selectedSkin;
    }
    if (saved && saved.rewardUnlocks && typeof saved.rewardUnlocks === "object") {
      for (const key of Object.keys(cosmetics.rewardUnlocks)) {
        cosmetics.rewardUnlocks[key] = saved.rewardUnlocks[key] === true;
      }
    }
    return cosmetics;
  }

  function applyRunToProgression(options) {
    const {
      progression,
      runSummary,
      calculateXp,
      getTierForXp,
      tiers = []
    } = options || {};
    const safeProgression = {
      ...createDefaultProgression(),
      ...(progression || {})
    };
    const previousTier = typeof getTierForXp === "function" && tiers.length > 0
      ? getTierForXp(safeProgression.xp, tiers) || { level: safeProgression.level || 1, rank: "Street Rookie" }
      : { level: safeProgression.level || 1, rank: "Street Rookie" };
    const xpGained = Math.max(0, Math.round(typeof calculateXp === "function" ? calculateXp(runSummary || {}) : 0));

    safeProgression.xp += xpGained;
    safeProgression.totalGoals += runSummary?.goals || 0;
    safeProgression.totalPerfects += runSummary?.runStats?.perfectGoals || 0;
    safeProgression.totalLongBombs += runSummary?.runStats?.longBombGoals || 0;
    safeProgression.totalGridDrops += runSummary?.runStats?.gridDrops || 0;
    safeProgression.totalRowClears += runSummary?.runStats?.gridRowClears || 0;

    const nextTier = typeof getTierForXp === "function" && tiers.length > 0
      ? getTierForXp(safeProgression.xp, tiers) || previousTier
      : previousTier;
    safeProgression.level = nextTier.level || safeProgression.level || 1;

    return {
      progression: safeProgression,
      lastRunProgress: {
        xpGained,
        levelUp: Math.max(0, (nextTier.level || 1) - (previousTier.level || 1)),
        tierName: nextTier.rank || "Street Rookie"
      }
    };
  }

  return {
    createDefaultSettings,
    createDefaultProgression,
    createDefaultCosmetics,
    hydrateSettings,
    hydrateProgression,
    hydrateCosmetics,
    applyRunToProgression
  };
});
