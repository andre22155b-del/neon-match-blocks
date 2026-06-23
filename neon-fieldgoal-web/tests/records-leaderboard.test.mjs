import { describe, expect, it } from "vitest";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const {
  createEmptyRecordBucket,
  createEmptyRecordsStore,
  createEmptyLeaderboardStore,
  hydrateRecordsStore,
  hydrateLeaderboardStore,
  ensureRecordBucket,
  ensureLeaderboardBucket,
  applyRunToStores
} = require("../records-leaderboard.js");

describe("records and leaderboard helpers", () => {
  it("creates empty store shapes", () => {
    expect(createEmptyRecordBucket()).toEqual({
      bestScore: 0,
      bestStreak: 0,
      bestLong: 0
    });
    expect(createEmptyRecordsStore()).toHaveProperty("scoreAttack");
    expect(createEmptyLeaderboardStore()).toHaveProperty("longBomb");
  });

  it("hydrates records from saved data and legacy fallbacks", () => {
    const saved = hydrateRecordsStore({
      scoreAttack: { bestScore: 70, bestStreak: 4, bestLong: 45 }
    }, null);
    expect(saved.scoreAttack.bestScore).toBe(70);

    const legacy = hydrateRecordsStore(null, {
      bestScore: 33,
      bestStreak: 2,
      bestLong: 30
    });
    expect(legacy.scoreAttack.bestLong).toBe(30);
  });

  it("hydrates leaderboard rows and keeps valid scored entries", () => {
    const leaderboard = hydrateLeaderboardStore({
      scoreAttack: [
        { score: 80, goals: 7, longestKick: 50, maxStreak: 4, stamp: "2026-03-23T00:00:00.000Z" },
        { score: 0, goals: 0, longestKick: 0, maxStreak: 0, stamp: "" }
      ]
    }, 5);
    expect(leaderboard.scoreAttack).toHaveLength(1);
    expect(leaderboard.scoreAttack[0].score).toBe(80);
  });

  it("ensures missing record and leaderboard buckets", () => {
    const records = {};
    const leaderboard = {};
    expect(ensureRecordBucket(records, "scoreAttack")).toEqual({
      bestScore: 0,
      bestStreak: 0,
      bestLong: 0
    });
    expect(ensureLeaderboardBucket(leaderboard, "scoreAttack")).toEqual([]);
  });

  it("applies a run to records and leaderboard state", () => {
    const result = applyRunToStores({
      records: createEmptyRecordsStore(),
      leaderboard: createEmptyLeaderboardStore(),
      modeKey: "scoreAttack",
      score: 95,
      goals: 8,
      longestKick: 55,
      maxStreak: 5,
      stamp: "2026-03-23T00:00:00.000Z",
      leaderboardLimit: 5
    });
    expect(result.records.scoreAttack.bestScore).toBe(95);
    expect(result.records.scoreAttack.bestLong).toBe(55);
    expect(result.leaderboard.scoreAttack).toHaveLength(1);
    expect(result.lastRunRank).toBe(1);
    expect(result.runEntry.score).toBe(95);
  });
});
