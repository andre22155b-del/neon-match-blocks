import { describe, expect, it } from "vitest";
import {
  buildOfflineLeaderboard,
  buildOfflineRecentMatches,
  computeTierProgress,
  didTierUp,
  getTierLadder,
  resolveTier,
} from "./progression.js";

describe("progression", () => {
  it("resolves ladder tiers in order", () => {
    expect(resolveTier(0).label).toBe("Rookie");
    expect(resolveTier(34).label).toBe("Street");
    expect(resolveTier(90).label).toBe("Pro");
    expect(resolveTier(160).label).toBe("Elite");
    expect(resolveTier(280).label).toBe("Neon Legend");
  });

  it("falls back to rookie for invalid tier input", () => {
    expect(resolveTier(Number.NaN).label).toBe("Rookie");
  });

  it("computes tier progress between thresholds", () => {
    const p = computeTierProgress(50);
    expect(p.tier.label).toBe("Street");
    expect(p.nextTier?.label).toBe("Pro");
    expect(p.progress).toBeGreaterThan(0);
    expect(p.progress).toBeLessThan(1);
    expect(p.xpToNext).toBe(25);
  });

  it("handles invalid progress input safely", () => {
    const p = computeTierProgress(Number.NaN);
    expect(p.currentXp).toBe(0);
    expect(p.tier.label).toBe("Rookie");
  });

  it("clamps max tier progress at 100 percent", () => {
    const p = computeTierProgress(999);
    expect(p.tier.label).toBe("Neon Legend");
    expect(p.progress).toBe(1);
    expect(p.nextTier).toBeNull();
  });

  it("detects promotions only when crossing up", () => {
    expect(didTierUp(10, 35)).toBe(true);
    expect(didTierUp(90, 80)).toBe(false);
    expect(didTierUp(90, 94)).toBe(false);
  });

  it("builds stable offline leaderboard including player", () => {
    const rows = buildOfflineLeaderboard({ playerName: "drew", bestScore: 42, ladderPoints: 40 });
    expect(rows.length).toBeGreaterThan(0);
    expect(rows[0].rank).toBe(1);
    expect(rows.some((entry) => entry.name === "DREW")).toBe(true);
  });

  it("normalizes leaderboard fallback inputs", () => {
    const rows = buildOfflineLeaderboard({ playerName: "   ", bestScore: Number.NaN, ladderPoints: Number.NaN });
    expect(rows.some((entry) => entry.name === "PLAYER ONE")).toBe(true);
  });

  it("builds offline recent matches with ids", () => {
    const rows = buildOfflineRecentMatches({ playerName: "neo", lastScore: 31 });
    expect(rows.length).toBeGreaterThan(2);
    expect(rows[0].matchId.startsWith("offline-")).toBe(true);
    expect(rows.some((row) => row.playerName === "NEO")).toBe(true);
  });

  it("builds recent match fallbacks for invalid values", () => {
    const rows = buildOfflineRecentMatches({ playerName: "", lastScore: Number.NaN });
    expect(rows[0].playerName).toBe("PLAYER ONE");
    expect(typeof rows[0].won).toBe("boolean");
  });

  it("exposes a frozen ladder array", () => {
    const ladder = getTierLadder();
    expect(Array.isArray(ladder)).toBe(true);
    expect(ladder[0].label).toBe("Rookie");
  });
});
