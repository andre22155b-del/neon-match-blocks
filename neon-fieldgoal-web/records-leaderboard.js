(function (root, factory) {
  const api = factory();
  if (typeof module !== "undefined" && module.exports) {
    module.exports = api;
  }
  root.NFGRecordsLeaderboard = api;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  const DEFAULT_MODE_KEYS = ["scoreAttack", "longBomb", "suddenDeath"];

  function createEmptyRecordBucket() {
    return {
      bestScore: 0,
      bestStreak: 0,
      bestLong: 0
    };
  }

  function createEmptyRecordsStore(modeKeys = DEFAULT_MODE_KEYS) {
    return Object.fromEntries(modeKeys.map((modeKey) => [modeKey, createEmptyRecordBucket()]));
  }

  function createEmptyLeaderboardStore(modeKeys = DEFAULT_MODE_KEYS) {
    return Object.fromEntries(modeKeys.map((modeKey) => [modeKey, []]));
  }

  function normalizeRecordBucket(bucket) {
    return {
      bestScore: Math.max(0, Number.parseInt(bucket?.bestScore, 10) || 0),
      bestStreak: Math.max(0, Number.parseInt(bucket?.bestStreak, 10) || 0),
      bestLong: Math.max(0, Number.parseInt(bucket?.bestLong, 10) || 0)
    };
  }

  function normalizeLeaderboardEntry(entry, modeKey = "scoreAttack") {
    const score = Math.max(0, Number.parseInt(entry?.score, 10) || 0);
    if (!(score > 0)) {
      return null;
    }
    return {
      modeKey,
      score,
      goals: Math.max(0, Number.parseInt(entry?.goals, 10) || 0),
      longestKick: Math.max(0, Number.parseInt(entry?.longestKick, 10) || 0),
      maxStreak: Math.max(0, Number.parseInt(entry?.maxStreak, 10) || 0),
      stamp: typeof entry?.stamp === "string" ? entry.stamp : ""
    };
  }

  function sortLeaderboard(board) {
    return board.sort((a, b) =>
      b.score - a.score ||
      b.longestKick - a.longestKick ||
      b.maxStreak - a.maxStreak ||
      b.goals - a.goals ||
      String(b.stamp).localeCompare(String(a.stamp))
    );
  }

  function hydrateRecordsStore(savedRecords, legacy = {}, modeKeys = DEFAULT_MODE_KEYS) {
    const records = createEmptyRecordsStore(modeKeys);
    if (savedRecords && typeof savedRecords === "object") {
      for (const modeKey of modeKeys) {
        if (savedRecords[modeKey]) {
          records[modeKey] = normalizeRecordBucket(savedRecords[modeKey]);
        }
      }
      return records;
    }

    if (legacy && typeof legacy === "object") {
      records.scoreAttack = normalizeRecordBucket({
        bestScore: legacy.bestScore,
        bestStreak: legacy.bestStreak,
        bestLong: legacy.bestLong
      });
    }
    return records;
  }

  function hydrateLeaderboardStore(savedLeaderboard, limit = 5, modeKeys = DEFAULT_MODE_KEYS) {
    const store = createEmptyLeaderboardStore(modeKeys);
    if (!savedLeaderboard || typeof savedLeaderboard !== "object") {
      return store;
    }
    for (const modeKey of modeKeys) {
      const rows = Array.isArray(savedLeaderboard[modeKey]) ? savedLeaderboard[modeKey] : [];
      store[modeKey] = sortLeaderboard(
        rows
          .map((entry) => normalizeLeaderboardEntry(entry, modeKey))
          .filter(Boolean)
      ).slice(0, limit);
    }
    return store;
  }

  function ensureRecordBucket(records, modeKey) {
    if (!records[modeKey]) {
      records[modeKey] = createEmptyRecordBucket();
    }
    return records[modeKey];
  }

  function ensureLeaderboardBucket(leaderboard, modeKey) {
    if (!Array.isArray(leaderboard[modeKey])) {
      leaderboard[modeKey] = [];
    }
    return leaderboard[modeKey];
  }

  function applyRunToStores(options = {}) {
    const {
      records = createEmptyRecordsStore(),
      leaderboard = createEmptyLeaderboardStore(),
      modeKey = "scoreAttack",
      score = 0,
      goals = 0,
      longestKick = 0,
      maxStreak = 0,
      stamp = new Date().toISOString(),
      leaderboardLimit = 5
    } = options;

    const nextRecords = {
      ...records,
      [modeKey]: {
        ...ensureRecordBucket({ ...records }, modeKey)
      }
    };
    const record = nextRecords[modeKey];
    record.bestScore = Math.max(record.bestScore, score);
    record.bestStreak = Math.max(record.bestStreak, maxStreak);
    record.bestLong = Math.max(record.bestLong, longestKick);

    const nextLeaderboard = {
      ...leaderboard,
      [modeKey]: [...ensureLeaderboardBucket({ ...leaderboard }, modeKey)]
    };

    let lastRunRank = null;
    let runEntry = null;

    if (score > 0 || goals > 0) {
      runEntry = normalizeLeaderboardEntry({
        score,
        goals,
        longestKick,
        maxStreak,
        stamp
      }, modeKey);
      if (runEntry) {
        nextLeaderboard[modeKey].push(runEntry);
        sortLeaderboard(nextLeaderboard[modeKey]);
        const rank = nextLeaderboard[modeKey].findIndex((entry) => entry === runEntry) + 1;
        lastRunRank = rank > 0 && rank <= leaderboardLimit ? rank : null;
        nextLeaderboard[modeKey] = nextLeaderboard[modeKey].slice(0, leaderboardLimit);
      }
    }

    return {
      records: nextRecords,
      leaderboard: nextLeaderboard,
      lastRunRank,
      runEntry
    };
  }

  return {
    createEmptyRecordBucket,
    createEmptyRecordsStore,
    createEmptyLeaderboardStore,
    normalizeRecordBucket,
    normalizeLeaderboardEntry,
    sortLeaderboard,
    hydrateRecordsStore,
    hydrateLeaderboardStore,
    ensureRecordBucket,
    ensureLeaderboardBucket,
    applyRunToStores
  };
});
