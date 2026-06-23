(function (root, factory) {
  const api = factory();
  if (typeof module !== "undefined" && module.exports) {
    module.exports = api;
  }
  root.NFGCompetitionValidation = api;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  const MODE_RULES = {
    scoreAttack: { minRunMs: 35000, maxRunMs: 15 * 60 * 1000 },
    longBomb: { minRunMs: 25000, maxRunMs: 15 * 60 * 1000 },
    suddenDeath: { minRunMs: 1000, maxRunMs: 15 * 60 * 1000 }
  };

  function normalizeModeKey(value) {
    return value === "longBomb"
      ? "longBomb"
      : value === "suddenDeath"
        ? "suddenDeath"
        : "scoreAttack";
  }

  function sanitizeHandle(value) {
    return String(value || "")
      .toUpperCase()
      .replace(/[^A-Z0-9]/g, "")
      .slice(0, 10) || "GRID01";
  }

  function clampInt(value, min, max) {
    return Math.max(min, Math.min(max, parseInt(value || "0", 10) || 0));
  }

  function sanitizeEntry(entry) {
    if (!entry || typeof entry !== "object") {
      return null;
    }
    const normalized = {
      handle: sanitizeHandle(entry.handle),
      score: clampInt(entry.score, 0, 9999),
      goals: clampInt(entry.goals, 0, 999),
      longestKick: clampInt(entry.longestKick, 20, 65),
      maxStreak: clampInt(entry.maxStreak, 0, 99),
      modeKey: normalizeModeKey(entry.modeKey),
      stamp: typeof entry.stamp === "string" ? entry.stamp : new Date().toISOString()
    };
    return normalized.score > 0 ? normalized : null;
  }

  function sanitizeRunTicket(ticket) {
    if (!ticket || typeof ticket !== "object") {
      return null;
    }
    const id = String(ticket.id || ticket.ticketId || "").slice(0, 80);
    const sessionId = String(ticket.sessionId || "").slice(0, 64);
    const runId = String(ticket.runId || "").slice(0, 64);
    if (!id || !sessionId || !runId) {
      return null;
    }
    const issuedAt = Number.isNaN(Date.parse(ticket.issuedAt))
      ? null
      : new Date(ticket.issuedAt).toISOString();
    const expiresAt = Number.isNaN(Date.parse(ticket.expiresAt))
      ? null
      : new Date(ticket.expiresAt).toISOString();
    if (!issuedAt || !expiresAt) {
      return null;
    }
    const usedAt = Number.isNaN(Date.parse(ticket.usedAt))
      ? null
      : new Date(ticket.usedAt).toISOString();
    return {
      id,
      sessionId,
      runId,
      handle: sanitizeHandle(ticket.handle),
      modeKey: normalizeModeKey(ticket.modeKey),
      issuedAt,
      expiresAt,
      usedAt
    };
  }

  function sanitizeCompetitionProof(proof) {
    if (!proof || typeof proof !== "object") {
      return null;
    }
    const ticketId = String(proof.ticketId || "").slice(0, 80);
    const sessionId = String(proof.sessionId || "").slice(0, 64);
    const runId = String(proof.runId || "").slice(0, 64);
    if (!ticketId || !sessionId || !runId) {
      return null;
    }
    const summary = proof.summary && typeof proof.summary === "object"
      ? {
        score: clampInt(proof.summary.score, 0, 9999),
        goals: clampInt(proof.summary.goals, 0, 999),
        maxStreak: clampInt(proof.summary.maxStreak, 0, 99),
        longestKick: clampInt(proof.summary.longestKick, 20, 65)
      }
      : null;
    return {
      ticketId,
      sessionId,
      runId,
      handle: sanitizeHandle(proof.handle),
      modeKey: normalizeModeKey(proof.modeKey),
      submittedAt: Number.isNaN(Date.parse(proof.submittedAt))
        ? new Date().toISOString()
        : new Date(proof.submittedAt).toISOString(),
      summary
    };
  }

  function buildCompetitionProof(options = {}) {
    const ticket = sanitizeRunTicket(options.ticket);
    if (!ticket) {
      return null;
    }
    const summary = options.summary && typeof options.summary === "object"
      ? {
        score: clampInt(options.summary.score, 0, 9999),
        goals: clampInt(options.summary.goals, 0, 999),
        maxStreak: clampInt(options.summary.maxStreak, 0, 99),
        longestKick: clampInt(options.summary.longestKick, 20, 65)
      }
      : null;
    return {
      ticketId: ticket.id,
      sessionId: ticket.sessionId,
      runId: ticket.runId,
      handle: sanitizeHandle(options.handle || ticket.handle),
      modeKey: normalizeModeKey(options.modeKey || ticket.modeKey),
      submittedAt: new Date().toISOString(),
      summary
    };
  }

  function normalizeTelemetryEvent(event) {
    if (!event || typeof event !== "object") {
      return null;
    }
    const runId = String(event.runId || "").slice(0, 64);
    if (!runId) {
      return null;
    }
    return {
      id: String(event.id || "").slice(0, 64),
      type: String(event.type || "").trim().toLowerCase(),
      ts: Number.isNaN(Date.parse(event.ts))
        ? null
        : new Date(event.ts).toISOString(),
      sessionId: String(event.sessionId || "").slice(0, 64),
      runId,
      modeKey: normalizeModeKey(event.modeKey),
      payload: event.payload && typeof event.payload === "object" ? event.payload : {}
    };
  }

  function summarizeRunTelemetry(events) {
    const rows = Array.isArray(events)
      ? events.map(normalizeTelemetryEvent).filter(Boolean).sort((a, b) => Date.parse(a.ts || 0) - Date.parse(b.ts || 0))
      : [];
    const summary = {
      runStart: null,
      runEnd: null,
      kickLaunchCount: 0,
      kickResultCount: 0,
      goalCount: 0,
      totalEarned: 0,
      finalScoreAfter: null,
      sessionId: rows[0]?.sessionId || "",
      runId: rows[0]?.runId || "",
      modeKey: rows[0]?.modeKey || "scoreAttack"
    };

    rows.forEach((event) => {
      if (event.type === "run_start" && !summary.runStart) {
        summary.runStart = event;
      }
      if (event.type === "run_end") {
        summary.runEnd = event;
      }
      if (event.type === "kick_launch") {
        summary.kickLaunchCount += 1;
      }
      if (event.type === "kick_result") {
        summary.kickResultCount += 1;
        if (event.payload?.isGoal) {
          summary.goalCount += 1;
        }
        summary.totalEarned += clampInt(event.payload?.totalEarned, 0, 9999);
        if (typeof event.payload?.scoreAfter !== "undefined") {
          summary.finalScoreAfter = clampInt(event.payload.scoreAfter, 0, 9999);
        }
      }
    });

    return summary;
  }

  function validateCompetitionSubmission(options = {}) {
    const entry = sanitizeEntry(options.entry);
    const proof = sanitizeCompetitionProof(options.proof);
    const ticket = sanitizeRunTicket(options.ticket);
    const nowMs = typeof options.now === "number" ? options.now : Date.now();
    if (!entry) {
      return { accepted: false, reason: "invalid_entry" };
    }
    if (!proof) {
      return { accepted: false, reason: "proof_missing" };
    }
    if (!ticket) {
      return { accepted: false, reason: "ticket_missing" };
    }
    if (ticket.usedAt) {
      return { accepted: false, reason: "ticket_used" };
    }
    if (proof.ticketId !== ticket.id) {
      return { accepted: false, reason: "ticket_mismatch" };
    }
    if (proof.sessionId !== ticket.sessionId || proof.runId !== ticket.runId) {
      return { accepted: false, reason: "run_identity_mismatch" };
    }
    if (proof.modeKey !== ticket.modeKey || entry.modeKey !== ticket.modeKey) {
      return { accepted: false, reason: "mode_mismatch" };
    }
    if (proof.handle !== ticket.handle || entry.handle !== ticket.handle) {
      return { accepted: false, reason: "handle_mismatch" };
    }
    if (nowMs > Date.parse(ticket.expiresAt)) {
      return { accepted: false, reason: "ticket_expired" };
    }

    const telemetry = summarizeRunTelemetry(options.telemetryEvents || []);
    if (!telemetry.runStart || !telemetry.runEnd) {
      return { accepted: false, reason: "telemetry_incomplete", telemetry };
    }
    if (telemetry.sessionId !== ticket.sessionId || telemetry.runId !== ticket.runId) {
      return { accepted: false, reason: "telemetry_identity_mismatch", telemetry };
    }
    if (telemetry.modeKey !== ticket.modeKey) {
      return { accepted: false, reason: "telemetry_mode_mismatch", telemetry };
    }

    const rules = MODE_RULES[entry.modeKey] || MODE_RULES.scoreAttack;
    const durationMs = Date.parse(telemetry.runEnd.ts || 0) - Date.parse(ticket.issuedAt);
    if (!Number.isFinite(durationMs) || durationMs < rules.minRunMs || durationMs > rules.maxRunMs) {
      return { accepted: false, reason: "duration_invalid", telemetry };
    }

    const runPayload = telemetry.runEnd.payload || {};
    if (clampInt(runPayload.score, 0, 9999) !== entry.score) {
      return { accepted: false, reason: "score_mismatch", telemetry };
    }
    if (clampInt(runPayload.goals, 0, 999) !== entry.goals) {
      return { accepted: false, reason: "goals_mismatch", telemetry };
    }
    if (clampInt(runPayload.maxStreak, 0, 99) !== entry.maxStreak) {
      return { accepted: false, reason: "streak_mismatch", telemetry };
    }
    if (clampInt(runPayload.longestKick, 20, 65) !== entry.longestKick) {
      return { accepted: false, reason: "longest_mismatch", telemetry };
    }
    if (proof.summary) {
      if (
        proof.summary.score !== entry.score ||
        proof.summary.goals !== entry.goals ||
        proof.summary.maxStreak !== entry.maxStreak ||
        proof.summary.longestKick !== entry.longestKick
      ) {
        return { accepted: false, reason: "proof_summary_mismatch", telemetry };
      }
    }
    if (telemetry.goalCount !== entry.goals) {
      return { accepted: false, reason: "goal_count_mismatch", telemetry };
    }
    if (telemetry.totalEarned !== entry.score) {
      return { accepted: false, reason: "earned_score_mismatch", telemetry };
    }
    if (telemetry.finalScoreAfter !== entry.score) {
      return { accepted: false, reason: "final_score_mismatch", telemetry };
    }
    if (telemetry.kickResultCount < Math.max(1, entry.goals)) {
      return { accepted: false, reason: "kick_results_missing", telemetry };
    }
    if (telemetry.kickLaunchCount < telemetry.kickResultCount) {
      return { accepted: false, reason: "kick_launch_mismatch", telemetry };
    }

    return {
      accepted: true,
      reason: "verified",
      telemetry,
      durationMs
    };
  }

  return {
    MODE_RULES,
    normalizeModeKey,
    sanitizeHandle,
    sanitizeEntry,
    sanitizeRunTicket,
    sanitizeCompetitionProof,
    buildCompetitionProof,
    summarizeRunTelemetry,
    validateCompetitionSubmission
  };
});
