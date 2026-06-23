const TIER_LADDER = Object.freeze([
  Object.freeze({ id: "rookie", label: "Rookie", minXp: 0, color: "#7ae8ff", trail: "#48d7ff" }),
  Object.freeze({ id: "street", label: "Street", minXp: 30, color: "#7dffb8", trail: "#49ff9d" }),
  Object.freeze({ id: "pro", label: "Pro", minXp: 75, color: "#ffd67c", trail: "#ffb75f" }),
  Object.freeze({ id: "elite", label: "Elite", minXp: 140, color: "#ff9bd4", trail: "#ff75be" }),
  Object.freeze({ id: "neon_legend", label: "Neon Legend", minXp: 230, color: "#bb8fff", trail: "#8d5dff" }),
]);

const clamp = (v, min, max) => Math.max(min, Math.min(max, v));

export const getTierLadder = () => TIER_LADDER;

export const resolveTier = (xpPoints) => {
  const points = Number.isFinite(xpPoints) ? Math.max(0, xpPoints) : 0;
  let tier = TIER_LADDER[0];
  for (let i = 0; i < TIER_LADDER.length; i++) {
    if (points >= TIER_LADDER[i].minXp) tier = TIER_LADDER[i];
    else break;
  }
  return tier;
};

export const computeTierProgress = (xpPoints) => {
  const points = Number.isFinite(xpPoints) ? Math.max(0, xpPoints) : 0;
  const tier = resolveTier(points);
  const tierIndex = TIER_LADDER.findIndex((entry) => entry.id === tier.id);
  const nextTier = TIER_LADDER[tierIndex + 1] || null;

  if (!nextTier) {
    return {
      tier,
      nextTier: null,
      progress: 1,
      currentXp: points,
      tierFloorXp: tier.minXp,
      xpToNext: 0,
      xpSpan: 1,
      xpInTier: points - tier.minXp,
    };
  }

  const xpSpan = Math.max(1, nextTier.minXp - tier.minXp);
  const xpInTier = clamp(points - tier.minXp, 0, xpSpan);
  return {
    tier,
    nextTier,
    progress: clamp(xpInTier / xpSpan, 0, 1),
    currentXp: points,
    tierFloorXp: tier.minXp,
    xpToNext: Math.max(0, nextTier.minXp - points),
    xpSpan,
    xpInTier,
  };
};

export const didTierUp = (previousXp, nextXp) => {
  const prev = resolveTier(previousXp);
  const next = resolveTier(nextXp);
  return prev.id !== next.id && next.minXp > prev.minXp;
};

const sanitizeName = (value, fallback) => {
  const safe = String(value || "")
    .trim()
    .replace(/\s+/g, " ")
    .slice(0, 24)
    .toUpperCase();
  return safe || fallback;
};

export const buildOfflineLeaderboard = ({ playerName, bestScore, ladderPoints } = {}) => {
  const safeName = sanitizeName(playerName, "PLAYER ONE");
  const safeBest = Math.max(0, Math.round(Number.isFinite(bestScore) ? bestScore : 0));
  const safeLadder = Math.max(0, Math.round(Number.isFinite(ladderPoints) ? ladderPoints : 0));

  const bots = [
    { name: "NEON KID", bestScore: 56 },
    { name: "RIM KRAKEN", bestScore: 49 },
    { name: "GLOW OPS", bestScore: 44 },
    { name: "PARK WIZ", bestScore: 39 },
    { name: "FASTBREAK XR", bestScore: 33 },
  ];

  const playerEntry = {
    name: safeName,
    bestScore: Math.max(safeBest, Math.round(safeLadder * 0.42) + 10),
  };

  const entries = [...bots, playerEntry]
    .sort((a, b) => b.bestScore - a.bestScore)
    .slice(0, 6)
    .map((entry, index) => ({
      rank: index + 1,
      name: entry.name,
      bestScore: entry.bestScore,
    }));

  return entries;
};

export const buildOfflineRecentMatches = ({ playerName, lastScore } = {}) => {
  const safeName = sanitizeName(playerName, "PLAYER ONE");
  const safeScore = Math.max(0, Math.round(Number.isFinite(lastScore) ? lastScore : 28));
  const now = Date.now();

  const rows = [
    { playerName: safeName, mode: "solo", won: safeScore >= 28, score: safeScore },
    { playerName: "NEON KID", mode: "duel", won: true, score: 36 },
    { playerName: "RIM KRAKEN", mode: "duel", won: false, score: 24 },
    { playerName: safeName, mode: "practice", won: true, score: Math.max(18, safeScore - 4) },
  ];

  return rows.map((match, index) => ({
    ...match,
    matchId: `offline-${now - index * 997}`,
  }));
};
