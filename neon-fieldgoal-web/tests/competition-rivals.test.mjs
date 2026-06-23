import { describe, expect, it } from "vitest";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const {
  communitySeedNames
} = require("../content-data.js");
const {
  createEmptyCommunityBoardStore,
  sanitizeHandle,
  normalizeCompetitionEntry,
  shapeCompetitionBoard,
  createSeedCommunityEntries,
  createDailyRivals,
  getNextLiveRival,
  filterCompetitionQueue,
  buildCompetitionRun,
  createCommunityBoardStore,
  getRenderedCommunityBoard,
  applyRemoteCompetitionPayload,
  applyLocalCompetitionRun
} = require("../competition-rivals.js");

describe("competition and rival helpers", () => {
  it("creates the expected empty board store shape", () => {
    expect(createEmptyCommunityBoardStore()).toEqual({
      scoreAttack: [],
      longBomb: [],
      suddenDeath: []
    });
  });

  it("sanitizes handles and normalizes valid competition rows", () => {
    expect(sanitizeHandle("arc kid!!")).toBe("ARCKID");
    expect(normalizeCompetitionEntry({
      handle: "grid kid",
      score: "42",
      goals: "5",
      longestKick: "50",
      maxStreak: "3",
      stamp: "2026-03-23T00:00:00.000Z",
      modeKey: "longBomb",
      source: "player"
    })).toEqual({
      handle: "GRIDKID",
      score: 42,
      goals: 5,
      longestKick: 50,
      maxStreak: 3,
      stamp: "2026-03-23T00:00:00.000Z",
      source: "player",
      modeKey: "longBomb"
    });
    expect(normalizeCompetitionEntry({ score: 0 })).toBeNull();
  });

  it("shapes seed boards and keeps only the highest rows", () => {
    const seeds = createSeedCommunityEntries("scoreAttack", communitySeedNames);
    expect(seeds.length).toBeGreaterThan(0);
    const board = shapeCompetitionBoard([
      ...seeds,
      { handle: "zzz", score: 999, goals: 9, longestKick: 65, maxStreak: 6, stamp: "2026-03-23T01:00:00.000Z", modeKey: "scoreAttack", source: "player" }
    ], "remote", 3);
    expect(board).toHaveLength(3);
    expect(board[0].handle).toBe("ZZZ");
    expect(board[0].score).toBe(999);
  });

  it("creates deterministic rivals and picks the next live target", () => {
    const rivals = createDailyRivals({
      modeKey: "scoreAttack",
      dateKey: "2026-03-23",
      localBest: 120,
      communitySeedNames
    });
    expect(rivals).toHaveLength(3);
    expect(rivals[0].id).toBe("2026-03-23-scoreAttack-0");
    expect(rivals[0].targetScore).toBeGreaterThanOrEqual(36);
    expect(getNextLiveRival(rivals, 0)?.handle).toBe(rivals[0].handle);
    expect(getNextLiveRival(rivals, 999)).toBeNull();
  });

  it("hydrates queues, local boards, and remote payloads", () => {
    const queue = filterCompetitionQueue([
      { handle: "ok1", score: 12, goals: 2, longestKick: 35, maxStreak: 1, modeKey: "scoreAttack", source: "player", stamp: "2026-03-23T00:00:00.000Z" },
      { handle: "bad", score: 0, goals: 0, longestKick: 0, maxStreak: 0, modeKey: "scoreAttack", source: "player", stamp: "2026-03-23T00:00:00.000Z" },
      { handle: "ok2", score: 18, goals: 3, longestKick: 40, maxStreak: 2, modeKey: "longBomb", source: "player", stamp: "2026-03-23T00:00:00.000Z" }
    ], 5);
    expect(queue).toHaveLength(2);

    const store = createCommunityBoardStore({
      scoreAttack: [{ handle: "saved", score: 55, goals: 4, longestKick: 45, maxStreak: 3, modeKey: "scoreAttack", source: "player", stamp: "2026-03-23T00:00:00.000Z" }]
    }, {
      communitySeedNames,
      limit: 5
    });
    expect(store.scoreAttack[0].handle).toBe("SAVED");
    expect(store.longBomb.length).toBeGreaterThan(0);

    const remote = applyRemoteCompetitionPayload({
      rows: [
        { handle: "remote1", score: 88, goals: 7, longestKick: 55, maxStreak: 4, modeKey: "scoreAttack", source: "remote", stamp: "2026-03-23T00:00:00.000Z" }
      ]
    }, {
      preferredModeKey: "scoreAttack",
      currentRemoteBoard: store,
      limit: 5
    });
    expect(remote.remoteActive).toBe(true);
    expect(remote.remoteBoard.scoreAttack[0].handle).toBe("REMOTE1");
  });

  it("builds player runs and merges local plus remote board views", () => {
    const entry = buildCompetitionRun({
      handle: "me",
      score: 64,
      goals: 6,
      longestKick: 55,
      maxStreak: 4,
      modeKey: "scoreAttack",
      stamp: "2026-03-23T00:00:00.000Z"
    });
    expect(entry.handle).toBe("ME");

    const localResult = applyLocalCompetitionRun([
      { handle: "AAA", score: 40, goals: 4, longestKick: 45, maxStreak: 3, modeKey: "scoreAttack", source: "seed", stamp: "seed-a" }
    ], entry, 5);
    expect(localResult.rank).toBe(1);
    expect(localResult.board[0].handle).toBe("ME");

    const rendered = getRenderedCommunityBoard({
      remoteRows: [
        { handle: "REMOTE", score: 70, goals: 6, longestKick: 60, maxStreak: 4, modeKey: "scoreAttack", source: "remote", stamp: "r1" }
      ],
      pendingRuns: [entry],
      localRows: localResult.board,
      modeKey: "scoreAttack",
      limit: 5
    });
    expect(rendered.map((row) => row.handle)).toEqual(["REMOTE", "ME"]);
  });
});
