(function (root, factory) {
  const api = factory();
  if (typeof module !== "undefined" && module.exports) {
    module.exports = api;
  }
  root.NFGAnalyticsTelemetry = api;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  function createTelemetryId(prefix = "evt") {
    return `${prefix}-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;
  }

  function createTelemetryState() {
    return {
      sessionId: createTelemetryId("sess"),
      runId: null,
      queue: [],
      recentEvents: [],
      endpoint: "",
      syncInFlight: false,
      lastFlushAt: 0,
      remoteActive: false,
      syncStatus: "LOCAL TELEMETRY READY",
      lastAcceptedCount: 0
    };
  }

  function normalizeModeKey(value) {
    return value === "longBomb"
      ? "longBomb"
      : value === "suddenDeath"
        ? "suddenDeath"
        : "scoreAttack";
  }

  function sanitizeTelemetryValue(value, depth = 0) {
    if (depth > 3) {
      return undefined;
    }
    if (value == null) {
      return null;
    }
    if (typeof value === "string") {
      return value.slice(0, 140);
    }
    if (typeof value === "number") {
      return Number.isFinite(value) ? Math.round(value * 1000) / 1000 : undefined;
    }
    if (typeof value === "boolean") {
      return value;
    }
    if (Array.isArray(value)) {
      return value
        .slice(0, 12)
        .map((entry) => sanitizeTelemetryValue(entry, depth + 1))
        .filter((entry) => entry !== undefined);
    }
    if (typeof value === "object") {
      const out = {};
      for (const [key, entry] of Object.entries(value).slice(0, 24)) {
        const nextValue = sanitizeTelemetryValue(entry, depth + 1);
        if (nextValue !== undefined) {
          out[String(key).slice(0, 40)] = nextValue;
        }
      }
      return out;
    }
    return undefined;
  }

  function sanitizeEventType(type) {
    const cleaned = String(type || "")
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9._-]/g, "_")
      .slice(0, 48);
    return cleaned || "event";
  }

  function createTelemetryEvent(type, payload = {}, context = {}) {
    return {
      id: createTelemetryId("evt"),
      type: sanitizeEventType(type),
      ts: new Date().toISOString(),
      sessionId: String(context.sessionId || "local").slice(0, 64),
      runId: context.runId ? String(context.runId).slice(0, 64) : null,
      modeKey: normalizeModeKey(context.modeKey),
      gameState: String(context.gameState || "").slice(0, 24),
      payload: sanitizeTelemetryValue(payload) || {}
    };
  }

  function filterTelemetryQueue(savedQueue, limit = 80) {
    return Array.isArray(savedQueue)
      ? savedQueue
        .map((event) => {
          if (!event || typeof event !== "object") {
            return null;
          }
          return createTelemetryEvent(event.type || "event", event.payload || {}, {
            sessionId: event.sessionId,
            runId: event.runId,
            modeKey: event.modeKey,
            gameState: event.gameState
          });
        })
        .filter(Boolean)
        .slice(-limit)
      : [];
  }

  function queueTelemetryEvent(queue, event, limit = 80) {
    return [...(Array.isArray(queue) ? queue : []), event].slice(-limit);
  }

  function pushRecentTelemetry(recentEvents, event, limit = 12) {
    return [event, ...(Array.isArray(recentEvents) ? recentEvents : [])].slice(0, limit);
  }

  function getTelemetryEndpoint(options = {}) {
    const {
      globalEndpoint = "",
      location = null
    } = options;
    const hostName = location ? String(location.hostname || "").toLowerCase() : "";
    const isLocalHost = hostName === "localhost" || hostName === "127.0.0.1" || hostName === "[::1]";
    const autoSameOriginEndpoint =
      location &&
      /^https?:$/i.test(location.protocol || "") &&
      isLocalHost &&
      location.origin &&
      location.origin !== "null"
        ? `${location.origin.replace(/\/+$/, "")}/api/telemetry`
        : "";
    return String(globalEndpoint || autoSameOriginEndpoint || "").trim().replace(/\/+$/, "");
  }

  function shouldFlushTelemetry(options = {}) {
    const {
      endpoint = "",
      online = true,
      syncInFlight = false,
      lastFlushAt = 0,
      cooldownMs = 10000,
      queueLength = 0,
      batchSize = 8,
      gameState = "title",
      now = Date.now(),
      force = false
    } = options;

    if (!endpoint || syncInFlight || !online || queueLength <= 0) {
      return false;
    }
    if (force) {
      return true;
    }
    if (gameState === "ball") {
      return false;
    }
    if (queueLength >= batchSize) {
      return true;
    }
    return now - lastFlushAt >= cooldownMs;
  }

  function buildTelemetryBatch(options = {}) {
    const {
      sessionId = "local",
      runId = null,
      reason = "manual",
      events = []
    } = options;
    if (!Array.isArray(events) || events.length === 0) {
      return null;
    }
    return {
      sessionId: String(sessionId || "local").slice(0, 64),
      runId: runId ? String(runId).slice(0, 64) : null,
      reason: String(reason || "manual").slice(0, 32),
      sentAt: new Date().toISOString(),
      events: events.map((event) => ({
        id: String(event.id || createTelemetryId("evt")).slice(0, 64),
        type: sanitizeEventType(event.type),
        ts: typeof event.ts === "string" ? event.ts : new Date().toISOString(),
        sessionId: String(event.sessionId || sessionId || "local").slice(0, 64),
        runId: event.runId ? String(event.runId).slice(0, 64) : null,
        modeKey: normalizeModeKey(event.modeKey),
        gameState: String(event.gameState || "").slice(0, 24),
        payload: sanitizeTelemetryValue(event.payload) || {}
      }))
    };
  }

  function buildRunTelemetrySummary(summary = {}) {
    return sanitizeTelemetryValue({
      modeKey: normalizeModeKey(summary.modeKey),
      score: summary.score || 0,
      goals: summary.goals || 0,
      maxStreak: summary.maxStreak || 0,
      longestKick: summary.longestKick || 0,
      suddenDeathFailed: !!summary.suddenDeathFailed,
      runStats: {
        perfectGoals: summary.runStats?.perfectGoals || 0,
        longBombGoals: summary.runStats?.longBombGoals || 0,
        suddenRuns: summary.runStats?.suddenRuns || 0,
        gridDrops: summary.runStats?.gridDrops || 0,
        gridRowClears: summary.runStats?.gridRowClears || 0,
        gridMatchBlocks: summary.runStats?.gridMatchBlocks || 0,
        maxGridCombo: summary.runStats?.maxGridCombo || 0,
        gridJackpots: summary.runStats?.gridJackpots || 0
      },
      progression: {
        xpGained: summary.lastRunProgress?.xpGained || 0,
        levelUp: summary.lastRunProgress?.levelUp || 0,
        tierName: summary.lastRunProgress?.tierName || ""
      },
      rivals: {
        beatCount: summary.lastRunRivals?.beatCount || 0,
        xpBonus: summary.lastRunRivals?.xpBonus || 0
      },
      challenges: {
        dailyCompleted: !!summary.lastRunChallenge?.completedNow,
        weeklyCompleted: !!summary.lastRunWeekly?.completedNow
      },
      ranks: {
        local: summary.lastRunRank ?? null,
        community: summary.lastCommunityRank ?? null
      }
    });
  }

  return {
    createTelemetryId,
    createTelemetryState,
    createTelemetryEvent,
    filterTelemetryQueue,
    queueTelemetryEvent,
    pushRecentTelemetry,
    getTelemetryEndpoint,
    shouldFlushTelemetry,
    buildTelemetryBatch,
    buildRunTelemetrySummary
  };
});
