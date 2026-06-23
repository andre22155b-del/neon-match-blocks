import { describe, expect, it } from "vitest";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const {
  createTelemetryState,
  createTelemetryEvent,
  filterTelemetryQueue,
  queueTelemetryEvent,
  pushRecentTelemetry,
  getTelemetryEndpoint,
  shouldFlushTelemetry,
  buildTelemetryBatch,
  buildRunTelemetrySummary
} = require("../analytics-telemetry.js");

describe("analytics and telemetry helpers", () => {
  it("creates the default telemetry state shape", () => {
    const state = createTelemetryState();
    expect(state.sessionId).toMatch(/^sess-/);
    expect(state.queue).toEqual([]);
    expect(state.recentEvents).toEqual([]);
    expect(state.syncStatus).toBe("LOCAL TELEMETRY READY");
  });

  it("creates sanitized telemetry events", () => {
    const event = createTelemetryEvent("Kick Launch!", {
      power: 0.81234,
      nested: { huge: "x".repeat(200) }
    }, {
      sessionId: "sess-1",
      runId: "run-1",
      modeKey: "longBomb",
      gameState: "aim"
    });
    expect(event.type).toBe("kick_launch_");
    expect(event.modeKey).toBe("longBomb");
    expect(event.payload.power).toBe(0.812);
    expect(event.payload.nested.huge.length).toBeLessThanOrEqual(140);
  });

  it("filters and queues telemetry events with limits", () => {
    const queue = filterTelemetryQueue([
      { type: "boot", sessionId: "s", modeKey: "scoreAttack", gameState: "title", payload: { ok: true } },
      null
    ], 10);
    expect(queue).toHaveLength(1);

    const next = queueTelemetryEvent(queue, createTelemetryEvent("run_start"), 1);
    expect(next).toHaveLength(1);

    const recent = pushRecentTelemetry([], createTelemetryEvent("kick_result"), 2);
    expect(recent[0].type).toBe("kick_result");
  });

  it("builds local telemetry endpoints and flush rules correctly", () => {
    const endpoint = getTelemetryEndpoint({
      globalEndpoint: "",
      location: {
        hostname: "127.0.0.1",
        protocol: "http:",
        origin: "http://127.0.0.1:4173"
      }
    });
    expect(endpoint).toBe("http://127.0.0.1:4173/api/telemetry");

    expect(shouldFlushTelemetry({
      endpoint,
      online: true,
      syncInFlight: false,
      lastFlushAt: 0,
      cooldownMs: 1000,
      queueLength: 4,
      batchSize: 8,
      gameState: "aim",
      now: 5000
    })).toBe(true);

    expect(shouldFlushTelemetry({
      endpoint,
      online: true,
      syncInFlight: false,
      lastFlushAt: 4900,
      cooldownMs: 1000,
      queueLength: 2,
      batchSize: 8,
      gameState: "ball",
      now: 5000
    })).toBe(false);
  });

  it("builds telemetry batches and run summaries", () => {
    const batch = buildTelemetryBatch({
      sessionId: "sess-1",
      runId: "run-1",
      reason: "run_end",
      events: [
        createTelemetryEvent("run_start", { modeLabel: "Score Attack" }, {
          sessionId: "sess-1",
          runId: "run-1",
          modeKey: "scoreAttack",
          gameState: "aim"
        })
      ]
    });
    expect(batch.events).toHaveLength(1);
    expect(batch.reason).toBe("run_end");

    const summary = buildRunTelemetrySummary({
      modeKey: "suddenDeath",
      score: 44,
      goals: 6,
      maxStreak: 4,
      longestKick: 50,
      suddenDeathFailed: false,
      runStats: {
        perfectGoals: 2,
        longBombGoals: 1,
        suddenRuns: 1,
        gridDrops: 8,
        gridRowClears: 2,
        gridMatchBlocks: 6,
        maxGridCombo: 3,
        gridJackpots: 1
      },
      lastRunProgress: { xpGained: 120, levelUp: 1, tierName: "Wind Cutter" },
      lastRunRivals: { beatCount: 2, xpBonus: 24 },
      lastRunChallenge: { completedNow: true },
      lastRunWeekly: { completedNow: false },
      lastRunRank: 1,
      lastCommunityRank: 3
    });
    expect(summary.modeKey).toBe("suddenDeath");
    expect(summary.runStats.gridDrops).toBe(8);
    expect(summary.runStats.gridMatchBlocks).toBe(6);
    expect(summary.progression.xpGained).toBe(120);
    expect(summary.ranks.community).toBe(3);
  });
});
