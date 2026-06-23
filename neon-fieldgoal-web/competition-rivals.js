(function (root, factory) {
  const api = factory();
  if (typeof module !== "undefined" && module.exports) {
    module.exports = api;
  }
  root.NFGCompetitionRivals = api;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  const DEFAULT_MODE_KEYS = ["scoreAttack", "longBomb", "suddenDeath"];

  function createEmptyCommunityBoardStore(modeKeys = DEFAULT_MODE_KEYS) {
    return Object.fromEntries(modeKeys.map((modeKey) => [modeKey, []]));
  }

  function createEmptyCompetitionQueue() {
    return [];
  }

  function sanitizeHandle(value) {
    return String(value || "")
      .toUpperCase()
      .replace(/[^A-Z0-9]/g, "")
      .slice(0, 10) || "GRID01";
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
        score: parseInt(proof.summary.score || "0", 10) || 0,
        goals: parseInt(proof.summary.goals || "0", 10) || 0,
        maxStreak: parseInt(proof.summary.maxStreak || "0", 10) || 0,
        longestKick: parseInt(proof.summary.longestKick || "0", 10) || 0
      }
      : null;
    return {
      ticketId,
      sessionId,
      runId,
      handle: sanitizeHandle(proof.handle),
      modeKey:
        proof.modeKey === "longBomb"
          ? "longBomb"
          : proof.modeKey === "suddenDeath"
            ? "suddenDeath"
            : "scoreAttack",
      submittedAt: typeof proof.submittedAt === "string" ? proof.submittedAt : new Date().toISOString(),
      summary
    };
  }

  function normalizeCompetitionEntry(entry, fallbackSource = "remote") {
    if (!entry || typeof entry !== "object") {
      return null;
    }
    const normalized = {
      handle: sanitizeHandle(entry.handle || "GRID01"),
      score: parseInt(entry.score || "0", 10) || 0,
      goals: parseInt(entry.goals || "0", 10) || 0,
      longestKick: parseInt(entry.longestKick || "0", 10) || 0,
      maxStreak: parseInt(entry.maxStreak || "0", 10) || 0,
      stamp: typeof entry.stamp === "string" ? entry.stamp : new Date().toISOString(),
      source:
        entry.source === "player" || entry.source === "seed" || entry.source === "remote"
          ? entry.source
          : fallbackSource,
      modeKey:
        entry.modeKey === "longBomb"
          ? "longBomb"
          : entry.modeKey === "suddenDeath"
            ? "suddenDeath"
            : "scoreAttack"
    };
    const proof = sanitizeCompetitionProof(entry.proof);
    if (proof) {
      normalized.proof = proof;
    }
    return normalized.score > 0 ? normalized : null;
  }

  function sortCompetitionBoard(rows) {
    return rows.sort((a, b) =>
      b.score - a.score ||
      b.longestKick - a.longestKick ||
      b.maxStreak - a.maxStreak ||
      b.goals - a.goals ||
      String(b.stamp).localeCompare(String(a.stamp))
    );
  }

  function shapeCompetitionBoard(rows, fallbackSource = "remote", limit = 7) {
    return sortCompetitionBoard(
      rows
        .map((entry) => normalizeCompetitionEntry(entry, fallbackSource))
        .filter(Boolean)
    ).slice(0, limit);
  }

  function createSeedCommunityEntries(modeKey, communitySeedNames = {}) {
    const names = communitySeedNames[modeKey] || communitySeedNames.scoreAttack || [];
    return names
      .map((handle, index) => {
        const isLongBomb = modeKey === "longBomb";
        const isSuddenDeath = modeKey === "suddenDeath";
        return {
          handle,
          score: isLongBomb ? 42 - index * 6 : isSuddenDeath ? 28 - index * 4 : 84 - index * 12,
          goals: isLongBomb ? 7 - index : isSuddenDeath ? 5 - Math.floor(index * 0.7) : 12 - index * 2,
          longestKick: isLongBomb ? 65 - index * 5 : isSuddenDeath ? 50 - index * 3 : 55 - index * 5,
          maxStreak: isLongBomb ? 4 - Math.floor(index / 2) : isSuddenDeath ? 3 - Math.floor(index / 2) : 5 - index,
          stamp: `seed-${modeKey}-${index + 1}`,
          modeKey,
          source: "seed"
        };
      })
      .filter((entry) => entry.score > 0);
  }

  function hashSeed(value) {
    return Array.from(String(value || "")).reduce((total, char) => (total * 31 + char.charCodeAt(0)) % 1000003, 17);
  }

  function createDailyRivals(options = {}) {
    const {
      modeKey = "scoreAttack",
      dateKey = "",
      localBest = 0,
      communitySeedNames = {}
    } = options;
    const baseSeed = hashSeed(`${dateKey}:${modeKey}`);
    const baseBoard = createSeedCommunityEntries(modeKey, communitySeedNames);
    const scoreBase = modeKey === "longBomb" ? 14 : modeKey === "suddenDeath" ? 10 : 22;
    const taunts = [
      "Own the lane before they do.",
      "Clip their run and steal the heat.",
      "Beat the target and jack the spotlight."
    ];
    return Array.from({ length: 3 }, (_, index) => {
      const seedRow = baseBoard[index % Math.max(1, baseBoard.length)] || null;
      const seededScore = seedRow?.score || scoreBase + 14 * index;
      const targetScore = Math.max(
        scoreBase + index * (modeKey === "longBomb" ? 4 : modeKey === "suddenDeath" ? 3 : 7),
        Math.min(seededScore, Math.max(scoreBase, Math.round(localBest * (0.58 + index * 0.14))))
      );
      const handle = seedRow?.handle || `${modeKey.toUpperCase().slice(0, 4)}${index + 1}`;
      return {
        id: `${dateKey}-${modeKey}-${index}`,
        handle,
        title: index === 0 ? "Style Clash" : index === 1 ? "Heat Race" : "Crown Chase",
        taunt: taunts[(baseSeed + index) % taunts.length],
        targetScore: Math.max(modeKey === "suddenDeath" ? 6 : 8, targetScore),
        rewardXp: 16 + index * 8
      };
    });
  }

  function getNextLiveRival(rivals, score) {
    return rivals.find((rival) => score < rival.targetScore) || null;
  }

  function filterCompetitionQueue(savedQueue, limit = 12) {
    return Array.isArray(savedQueue)
      ? savedQueue
        .map((entry) => normalizeCompetitionEntry(entry, "player"))
        .filter(Boolean)
        .slice(-limit)
      : createEmptyCompetitionQueue();
  }

  function buildCompetitionRun(entry) {
    return normalizeCompetitionEntry({
      handle: entry?.handle,
      score: entry?.score,
      goals: entry?.goals,
      longestKick: entry?.longestKick,
      maxStreak: entry?.maxStreak,
      stamp: entry?.stamp || new Date().toISOString(),
      source: entry?.source || "player",
      modeKey: entry?.modeKey,
      proof: entry?.proof || null
    }, "player");
  }

  function getRenderedCommunityBoard(options = {}) {
    const {
      remoteRows = [],
      pendingRuns = [],
      localRows = [],
      modeKey = "scoreAttack",
      limit = 7
    } = options;
    if (remoteRows.length > 0) {
      const pendingRows = pendingRuns
        .filter((entry) => entry.modeKey === modeKey)
        .map((entry) => ({ ...entry, source: "player" }));
      return shapeCompetitionBoard(remoteRows.concat(pendingRows), "remote", limit);
    }
    return Array.isArray(localRows) ? localRows : [];
  }

  function createCommunityBoardStore(savedBoard, options = {}) {
    const {
      communitySeedNames = {},
      modeKeys = DEFAULT_MODE_KEYS,
      limit = 7
    } = options;
    const board = createEmptyCommunityBoardStore(modeKeys);
    for (const modeKey of modeKeys) {
      const sourceRows = Array.isArray(savedBoard?.[modeKey]) && savedBoard[modeKey].length > 0
        ? savedBoard[modeKey]
        : createSeedCommunityEntries(modeKey, communitySeedNames);
      board[modeKey] = shapeCompetitionBoard(sourceRows, "seed", limit);
    }
    return board;
  }

  function applyRemoteCompetitionPayload(payload, options = {}) {
    const {
      preferredModeKey = "scoreAttack",
      currentRemoteBoard = null,
      modeKeys = DEFAULT_MODE_KEYS,
      limit = 7
    } = options;
    const nextRemoteBoard = createEmptyCommunityBoardStore(modeKeys);
    for (const modeKey of modeKeys) {
      nextRemoteBoard[modeKey] = Array.isArray(currentRemoteBoard?.[modeKey])
        ? [...currentRemoteBoard[modeKey]]
        : [];
    }
    let applied = false;

    if (Array.isArray(payload)) {
      nextRemoteBoard[preferredModeKey] = shapeCompetitionBoard(payload, "remote", limit);
      applied = nextRemoteBoard[preferredModeKey].length > 0;
    } else if (payload && typeof payload === "object") {
      for (const modeKey of modeKeys) {
        const rows =
          Array.isArray(payload[modeKey]) ? payload[modeKey] :
          Array.isArray(payload.rows) && modeKey === preferredModeKey ? payload.rows :
          Array.isArray(payload.board?.[modeKey]) ? payload.board[modeKey] :
          null;
        if (rows) {
          nextRemoteBoard[modeKey] = shapeCompetitionBoard(rows, "remote", limit);
          applied = applied || nextRemoteBoard[modeKey].length > 0;
        }
      }
    }

    return {
      remoteBoard: nextRemoteBoard,
      remoteActive: applied || Object.values(nextRemoteBoard).some((rows) => rows.length > 0),
      applied
    };
  }

  function applyLocalCompetitionRun(board, entry, limit = 7) {
    const nextBoard = Array.isArray(board) ? [...board] : [];
    nextBoard.push(entry);
    sortCompetitionBoard(nextBoard);
    const rank = nextBoard.findIndex((row) => row === entry) + 1;
    return {
      board: nextBoard.slice(0, limit),
      rank: rank > 0 && rank <= limit ? rank : null
    };
  }

  return {
    createEmptyCommunityBoardStore,
    createEmptyCompetitionQueue,
    sanitizeHandle,
    sanitizeCompetitionProof,
    normalizeCompetitionEntry,
    sortCompetitionBoard,
    shapeCompetitionBoard,
    createSeedCommunityEntries,
    hashSeed,
    createDailyRivals,
    getNextLiveRival,
    filterCompetitionQueue,
    buildCompetitionRun,
    getRenderedCommunityBoard,
    createCommunityBoardStore,
    applyRemoteCompetitionPayload,
    applyLocalCompetitionRun
  };
});
