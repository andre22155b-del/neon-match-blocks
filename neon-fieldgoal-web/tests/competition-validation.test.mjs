import { describe, expect, it } from "vitest";

const {
  sanitizeRunTicket,
  buildCompetitionProof,
  summarizeRunTelemetry,
  validateCompetitionSubmission
} = require("../competition-validation.js");

describe("competition validation helpers", () => {
  it("summarizes run telemetry into a verifiable digest", () => {
    const telemetry = summarizeRunTelemetry([
      { type: "run_start", ts: "2026-03-23T08:00:00.000Z", sessionId: "sess-1", runId: "run-1", modeKey: "scoreAttack", payload: {} },
      { type: "kick_launch", ts: "2026-03-23T08:00:05.000Z", sessionId: "sess-1", runId: "run-1", modeKey: "scoreAttack", payload: {} },
      { type: "kick_result", ts: "2026-03-23T08:00:06.000Z", sessionId: "sess-1", runId: "run-1", modeKey: "scoreAttack", payload: { isGoal: true, totalEarned: 11, scoreAfter: 11 } },
      { type: "run_end", ts: "2026-03-23T08:01:00.000Z", sessionId: "sess-1", runId: "run-1", modeKey: "scoreAttack", payload: { score: 11, goals: 1, maxStreak: 1, longestKick: 20 } }
    ]);

    expect(telemetry.kickLaunchCount).toBe(1);
    expect(telemetry.kickResultCount).toBe(1);
    expect(telemetry.goalCount).toBe(1);
    expect(telemetry.totalEarned).toBe(11);
    expect(telemetry.finalScoreAfter).toBe(11);
  });

  it("accepts a valid ticket plus telemetry-backed submission", () => {
    const ticket = sanitizeRunTicket({
      id: "ticket-123",
      sessionId: "sess-1",
      runId: "run-1",
      handle: "grid01",
      modeKey: "scoreAttack",
      issuedAt: "2026-03-23T08:00:00.000Z",
      expiresAt: "2026-03-23T08:20:00.000Z"
    });
    const proof = buildCompetitionProof({
      ticket,
      handle: "grid01",
      modeKey: "scoreAttack",
      summary: {
        score: 11,
        goals: 1,
        maxStreak: 1,
        longestKick: 20
      }
    });
    const telemetryEvents = [
      { type: "run_start", ts: "2026-03-23T08:00:01.000Z", sessionId: "sess-1", runId: "run-1", modeKey: "scoreAttack", payload: {} },
      { type: "kick_launch", ts: "2026-03-23T08:00:05.000Z", sessionId: "sess-1", runId: "run-1", modeKey: "scoreAttack", payload: {} },
      { type: "kick_result", ts: "2026-03-23T08:00:06.000Z", sessionId: "sess-1", runId: "run-1", modeKey: "scoreAttack", payload: { isGoal: true, totalEarned: 11, scoreAfter: 11 } },
      { type: "run_end", ts: "2026-03-23T08:01:00.000Z", sessionId: "sess-1", runId: "run-1", modeKey: "scoreAttack", payload: { score: 11, goals: 1, maxStreak: 1, longestKick: 20 } }
    ];

    const result = validateCompetitionSubmission({
      entry: {
        handle: "grid01",
        score: 11,
        goals: 1,
        longestKick: 20,
        maxStreak: 1,
        modeKey: "scoreAttack",
        stamp: "2026-03-23T08:01:00.000Z"
      },
      proof,
      ticket,
      telemetryEvents,
      now: Date.parse("2026-03-23T08:01:02.000Z")
    });

    expect(result.accepted).toBe(true);
    expect(result.reason).toBe("verified");
  });

  it("rejects a submission when telemetry score does not match the board score", () => {
    const ticket = sanitizeRunTicket({
      id: "ticket-999",
      sessionId: "sess-1",
      runId: "run-1",
      handle: "grid01",
      modeKey: "scoreAttack",
      issuedAt: "2026-03-23T08:00:00.000Z",
      expiresAt: "2026-03-23T08:20:00.000Z"
    });
    const proof = buildCompetitionProof({
      ticket,
      handle: "grid01",
      modeKey: "scoreAttack",
      summary: {
        score: 22,
        goals: 2,
        maxStreak: 2,
        longestKick: 25
      }
    });
    const telemetryEvents = [
      { type: "run_start", ts: "2026-03-23T08:00:01.000Z", sessionId: "sess-1", runId: "run-1", modeKey: "scoreAttack", payload: {} },
      { type: "kick_launch", ts: "2026-03-23T08:00:05.000Z", sessionId: "sess-1", runId: "run-1", modeKey: "scoreAttack", payload: {} },
      { type: "kick_result", ts: "2026-03-23T08:00:06.000Z", sessionId: "sess-1", runId: "run-1", modeKey: "scoreAttack", payload: { isGoal: true, totalEarned: 11, scoreAfter: 11 } },
      { type: "run_end", ts: "2026-03-23T08:01:00.000Z", sessionId: "sess-1", runId: "run-1", modeKey: "scoreAttack", payload: { score: 22, goals: 2, maxStreak: 2, longestKick: 25 } }
    ];

    const result = validateCompetitionSubmission({
      entry: {
        handle: "grid01",
        score: 22,
        goals: 2,
        longestKick: 25,
        maxStreak: 2,
        modeKey: "scoreAttack",
        stamp: "2026-03-23T08:01:00.000Z"
      },
      proof,
      ticket,
      telemetryEvents,
      now: Date.parse("2026-03-23T08:01:02.000Z")
    });

    expect(result.accepted).toBe(false);
    expect(result.reason).toBe("goal_count_mismatch");
  });
});
