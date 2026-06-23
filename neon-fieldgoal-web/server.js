const http = require("node:http");
const fs = require("node:fs");
const path = require("node:path");
const crypto = require("node:crypto");
const { URL } = require("node:url");
const CompetitionValidation = require("./competition-validation.js");

const HOST = process.env.HOST || "127.0.0.1";
const PORT = Number.parseInt(process.env.PORT || "4173", 10);
const ROOT_DIR = __dirname;
const DEFAULT_DATA_DIR = path.join(ROOT_DIR, "data");
const BOARD_PATH = process.env.NFG_BOARD_PATH
  ? path.resolve(process.env.NFG_BOARD_PATH)
  : path.join(DEFAULT_DATA_DIR, "community-heat.json");
const TELEMETRY_PATH = process.env.NFG_TELEMETRY_PATH
  ? path.resolve(process.env.NFG_TELEMETRY_PATH)
  : path.join(DEFAULT_DATA_DIR, "telemetry-log.ndjson");
const DATA_DIR = path.dirname(BOARD_PATH);
const BOARD_LIMIT = 25;
const TELEMETRY_RECENT_LIMIT = 24;
const RUN_TICKET_TTL_MS = 20 * 60 * 1000;
let lastSmokeReport = {
  status: "idle",
  updatedAt: null,
  results: []
};
const runTickets = new Map();

const MIME_TYPES = {
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".svg": "image/svg+xml",
  ".ico": "image/x-icon"
};

const createSeedBoard = () => ({
  scoreAttack: [
    { handle: "GRID01", score: 108, goals: 24, longestKick: 60, maxStreak: 8, stamp: "2026-03-16T22:14:00.000Z", source: "remote", modeKey: "scoreAttack" },
    { handle: "NEONQB", score: 101, goals: 22, longestKick: 55, maxStreak: 7, stamp: "2026-03-16T21:18:00.000Z", source: "remote", modeKey: "scoreAttack" },
    { handle: "ARC4DE", score: 96, goals: 21, longestKick: 50, maxStreak: 7, stamp: "2026-03-16T19:42:00.000Z", source: "remote", modeKey: "scoreAttack" }
  ],
  longBomb: [
    { handle: "BANGER", score: 72, goals: 12, longestKick: 65, maxStreak: 5, stamp: "2026-03-16T23:03:00.000Z", source: "remote", modeKey: "longBomb" },
    { handle: "SWAY99", score: 66, goals: 11, longestKick: 65, maxStreak: 4, stamp: "2026-03-16T20:45:00.000Z", source: "remote", modeKey: "longBomb" },
    { handle: "WINDUP", score: 60, goals: 10, longestKick: 60, maxStreak: 4, stamp: "2026-03-16T18:10:00.000Z", source: "remote", modeKey: "longBomb" }
  ],
  suddenDeath: [
    { handle: "LASTLIFE", score: 44, goals: 6, longestKick: 50, maxStreak: 4, stamp: "2026-03-16T22:40:00.000Z", source: "remote", modeKey: "suddenDeath" },
    { handle: "ONEKICK", score: 39, goals: 5, longestKick: 47, maxStreak: 4, stamp: "2026-03-16T20:11:00.000Z", source: "remote", modeKey: "suddenDeath" },
    { handle: "CLUTCHUP", score: 33, goals: 4, longestKick: 45, maxStreak: 3, stamp: "2026-03-16T19:05:00.000Z", source: "remote", modeKey: "suddenDeath" }
  ]
});

const ensureDataDir = () => {
  fs.mkdirSync(DATA_DIR, { recursive: true });
};

const sanitizeHandle = (value) => CompetitionValidation.sanitizeHandle(value);

const normalizeModeKey = (value) => CompetitionValidation.normalizeModeKey(value);

const normalizeEntry = (entry) => {
  if (!entry || typeof entry !== "object") {
    return null;
  }
  const modeKey = normalizeModeKey(entry.modeKey);
  const score = Math.max(0, Math.min(9999, Number.parseInt(entry.score, 10) || 0));
  const goals = Math.max(0, Math.min(999, Number.parseInt(entry.goals, 10) || 0));
  const longestKick = Math.max(20, Math.min(65, Number.parseInt(entry.longestKick, 10) || 20));
  const maxStreak = Math.max(0, Math.min(99, Number.parseInt(entry.maxStreak, 10) || 0));
  const stampValue = String(entry.stamp || "");
  const stamp = Number.isNaN(Date.parse(stampValue))
    ? new Date().toISOString()
    : new Date(stampValue).toISOString();
  return {
    handle: sanitizeHandle(entry.handle),
    score,
    goals,
    longestKick,
    maxStreak,
    stamp,
    source: "remote",
    modeKey
  };
};

const sortBoard = (board) => {
  board.sort((a, b) => {
    if (b.score !== a.score) {
      return b.score - a.score;
    }
    if (b.longestKick !== a.longestKick) {
      return b.longestKick - a.longestKick;
    }
    if (b.maxStreak !== a.maxStreak) {
      return b.maxStreak - a.maxStreak;
    }
    return Date.parse(b.stamp) - Date.parse(a.stamp);
  });
  return board;
};

const shapeBoard = (board) => {
  const nextBoard = {
    scoreAttack: [],
    longBomb: [],
    suddenDeath: []
  };
  for (const modeKey of Object.keys(nextBoard)) {
    const rows = Array.isArray(board?.[modeKey]) ? board[modeKey] : [];
    nextBoard[modeKey] = sortBoard(rows.map(normalizeEntry).filter(Boolean)).slice(0, BOARD_LIMIT);
  }
  return nextBoard;
};

const entriesMatch = (a, b) =>
  a &&
  b &&
  a.handle === b.handle &&
  a.score === b.score &&
  a.goals === b.goals &&
  a.longestKick === b.longestKick &&
  a.maxStreak === b.maxStreak &&
  a.modeKey === b.modeKey &&
  a.stamp === b.stamp;

const pruneRunTickets = (nowMs = Date.now()) => {
  for (const [ticketId, ticket] of runTickets.entries()) {
    if (!ticket || !ticket.expiresAt || Date.parse(ticket.expiresAt) <= nowMs) {
      runTickets.delete(ticketId);
    }
  }
};

const createRunTicket = (payload = {}) => {
  pruneRunTickets();
  const sessionId = String(payload.sessionId || "").slice(0, 64);
  const runId = String(payload.runId || "").slice(0, 64);
  if (!sessionId || !runId) {
    return null;
  }
  const ticket = {
    id: `ticket-${crypto.randomUUID()}`,
    sessionId,
    runId,
    handle: sanitizeHandle(payload.handle),
    modeKey: normalizeModeKey(payload.modeKey),
    issuedAt: new Date().toISOString(),
    expiresAt: new Date(Date.now() + RUN_TICKET_TTL_MS).toISOString(),
    usedAt: null
  };
  runTickets.set(ticket.id, ticket);
  return ticket;
};

const getTelemetryEventsForRun = (runId, sessionId = "") => {
  if (!runId || !fs.existsSync(TELEMETRY_PATH)) {
    return [];
  }
  try {
    const raw = fs.readFileSync(TELEMETRY_PATH, "utf8").trim();
    if (!raw) {
      return [];
    }
    return raw
      .split("\n")
      .filter(Boolean)
      .map((line) => {
        try {
          return JSON.parse(line);
        } catch (error) {
          return null;
        }
      })
      .filter((event) =>
        event &&
        event.runId === runId &&
        (!sessionId || event.sessionId === sessionId)
      );
  } catch (error) {
    return [];
  }
};

const loadBoard = () => {
  ensureDataDir();
  if (!fs.existsSync(BOARD_PATH)) {
    const seed = shapeBoard(createSeedBoard());
    fs.writeFileSync(BOARD_PATH, JSON.stringify(seed, null, 2));
    return seed;
  }
  try {
    const parsed = JSON.parse(fs.readFileSync(BOARD_PATH, "utf8"));
    return shapeBoard(parsed);
  } catch (error) {
    const seed = shapeBoard(createSeedBoard());
    fs.writeFileSync(BOARD_PATH, JSON.stringify(seed, null, 2));
    return seed;
  }
};

const saveBoard = (board) => {
  ensureDataDir();
  fs.writeFileSync(BOARD_PATH, JSON.stringify(shapeBoard(board), null, 2));
};

const sendJson = (res, statusCode, payload) => {
  res.writeHead(statusCode, {
    "Content-Type": "application/json; charset=utf-8",
    "Cache-Control": "no-store",
    "Access-Control-Allow-Origin": "*",
    "Access-Control-Allow-Methods": "GET,POST,OPTIONS",
    "Access-Control-Allow-Headers": "Content-Type"
  });
  res.end(JSON.stringify(payload));
};

const sendFile = (res, filePath) => {
  const ext = path.extname(filePath).toLowerCase();
  const contentType = MIME_TYPES[ext] || "application/octet-stream";
  const stream = fs.createReadStream(filePath);
  stream.on("error", () => {
    sendJson(res, 404, { error: "not_found" });
  });
  res.writeHead(200, {
    "Content-Type": contentType,
    "Cache-Control": ext === ".html" ? "no-store" : "public, max-age=300",
    "Access-Control-Allow-Origin": "*"
  });
  stream.pipe(res);
};

const readJsonBody = (req) =>
  new Promise((resolve, reject) => {
    let raw = "";
    req.on("data", (chunk) => {
      raw += chunk;
      if (raw.length > 64 * 1024) {
        reject(new Error("payload_too_large"));
        req.destroy();
      }
    });
    req.on("end", () => {
      if (!raw.trim()) {
        resolve({});
        return;
      }
      try {
        resolve(JSON.parse(raw));
      } catch (error) {
        reject(new Error("invalid_json"));
      }
    });
    req.on("error", reject);
  });

const sanitizeTelemetryValue = (value, depth = 0) => {
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
};

const normalizeTelemetryEvent = (event, batch = {}) => {
  if (!event || typeof event !== "object") {
    return null;
  }
  const type = String(event.type || batch.type || "")
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9._-]/g, "_")
    .slice(0, 48);
  if (!type) {
    return null;
  }
  const stampValue = String(event.ts || batch.sentAt || "");
  const ts = Number.isNaN(Date.parse(stampValue))
    ? new Date().toISOString()
    : new Date(stampValue).toISOString();
  return {
    id: String(event.id || `${type}-${Date.now()}`).slice(0, 64),
    type,
    ts,
    sessionId: String(event.sessionId || batch.sessionId || "local").slice(0, 64),
    runId: event.runId ? String(event.runId).slice(0, 64) : batch.runId ? String(batch.runId).slice(0, 64) : null,
    modeKey: normalizeModeKey(event.modeKey || batch.modeKey),
    gameState: String(event.gameState || "").slice(0, 24),
    payload: sanitizeTelemetryValue(event.payload) || {}
  };
};

const appendTelemetryBatch = (payload) => {
  ensureDataDir();
  const rawEvents = Array.isArray(payload?.events) ? payload.events : [payload];
  const normalized = rawEvents
    .map((event) => normalizeTelemetryEvent(event, payload))
    .filter(Boolean);
  if (normalized.length === 0) {
    return [];
  }
  const content = normalized.map((event) => JSON.stringify(event)).join("\n") + "\n";
  fs.appendFileSync(TELEMETRY_PATH, content);
  return normalized;
};

const getTelemetrySummary = () => {
  if (!fs.existsSync(TELEMETRY_PATH)) {
    return {
      totalEvents: 0,
      lastBatchAt: null,
      recent: []
    };
  }
  try {
    const raw = fs.readFileSync(TELEMETRY_PATH, "utf8").trim();
    if (!raw) {
      return {
        totalEvents: 0,
        lastBatchAt: null,
        recent: []
      };
    }
    const lines = raw.split("\n").filter(Boolean);
    const recent = lines
      .slice(-TELEMETRY_RECENT_LIMIT)
      .map((line) => {
        try {
          return JSON.parse(line);
        } catch (error) {
          return null;
        }
      })
      .filter(Boolean);
    return {
      totalEvents: lines.length,
      lastBatchAt: recent[recent.length - 1]?.ts || null,
      recent
    };
  } catch (error) {
    return {
      totalEvents: 0,
      lastBatchAt: null,
      recent: []
    };
  }
};

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url || "/", `http://${req.headers.host || `${HOST}:${PORT}`}`);
  const pathname = url.pathname;

  if (req.method === "OPTIONS") {
    res.writeHead(204, {
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Methods": "GET,POST,OPTIONS",
      "Access-Control-Allow-Headers": "Content-Type"
    });
    res.end();
    return;
  }

  if (pathname === "/api/community-heat/session") {
    if (req.method === "POST") {
      try {
        const payload = await readJsonBody(req);
        const ticket = createRunTicket(payload);
        if (!ticket) {
          sendJson(res, 400, { error: "invalid_ticket_request" });
          return;
        }
        sendJson(res, 200, {
          accepted: true,
          ticket
        });
      } catch (error) {
        sendJson(res, 400, { error: error.message || "invalid_request" });
      }
      return;
    }

    sendJson(res, 405, { error: "method_not_allowed" });
    return;
  }

  if (pathname === "/api/community-heat") {
    if (req.method === "GET") {
      const board = loadBoard();
      sendJson(res, 200, {
        board,
        meta: {
          boardLimit: BOARD_LIMIT,
          updatedAt: new Date().toISOString()
        }
      });
      return;
    }

    if (req.method === "POST") {
      try {
        const payload = await readJsonBody(req);
        const entry = normalizeEntry(payload);
        if (!entry) {
          sendJson(res, 400, { error: "invalid_entry" });
          return;
        }

        const proof = CompetitionValidation.sanitizeCompetitionProof(payload.proof);
        if (!proof) {
          sendJson(res, 422, { error: "proof_missing" });
          return;
        }
        pruneRunTickets();
        const ticket = runTickets.get(proof.ticketId) || null;
        const telemetryEvents = getTelemetryEventsForRun(proof.runId, proof.sessionId);
        const verification = CompetitionValidation.validateCompetitionSubmission({
          entry,
          proof,
          ticket,
          telemetryEvents,
          now: Date.now()
        });
        if (!verification.accepted) {
          sendJson(res, 422, {
            error: "run_unverified",
            reason: verification.reason,
            telemetry: verification.telemetry || null
          });
          return;
        }

        const board = loadBoard();
        if (!board[entry.modeKey].some((row) => entriesMatch(row, entry))) {
          board[entry.modeKey].push(entry);
        }
        board[entry.modeKey] = sortBoard(board[entry.modeKey]).slice(0, BOARD_LIMIT);
        saveBoard(board);
        ticket.usedAt = new Date().toISOString();
        runTickets.set(ticket.id, ticket);

        const rank = board[entry.modeKey].findIndex((row) => entriesMatch(row, entry)) + 1;

        sendJson(res, 200, {
          accepted: true,
          verified: true,
          trust: "telemetry_ticket_verified",
          rank: rank > 0 ? rank : null,
          verification: {
            reason: verification.reason,
            durationMs: verification.durationMs,
            kickLaunchCount: verification.telemetry?.kickLaunchCount || 0,
            kickResultCount: verification.telemetry?.kickResultCount || 0
          },
          board
        });
      } catch (error) {
        sendJson(res, 400, { error: error.message || "invalid_request" });
      }
      return;
    }

    sendJson(res, 405, { error: "method_not_allowed" });
    return;
  }

  if (pathname === "/health") {
    sendJson(res, 200, { ok: true });
    return;
  }

  if (pathname === "/api/telemetry") {
    if (req.method === "GET") {
      const summary = getTelemetrySummary();
      sendJson(res, 200, {
        ok: true,
        totalEvents: summary.totalEvents,
        lastBatchAt: summary.lastBatchAt,
        recent: summary.recent
      });
      return;
    }

    if (req.method === "POST") {
      try {
        const payload = await readJsonBody(req);
        const accepted = appendTelemetryBatch(payload);
        const summary = getTelemetrySummary();
        sendJson(res, 200, {
          accepted: true,
          acceptedCount: accepted.length,
          totalEvents: summary.totalEvents,
          lastBatchAt: summary.lastBatchAt
        });
      } catch (error) {
        sendJson(res, 400, { error: error.message || "invalid_request" });
      }
      return;
    }

    sendJson(res, 405, { error: "method_not_allowed" });
    return;
  }

  if (pathname === "/api/smoke-report") {
    if (req.method === "GET") {
      sendJson(res, 200, lastSmokeReport);
      return;
    }

    if (req.method === "POST") {
      try {
        const payload = await readJsonBody(req);
        lastSmokeReport = {
          status: payload?.status === "PASS" ? "PASS" : payload?.status === "FAIL" ? "FAIL" : "unknown",
          updatedAt: new Date().toISOString(),
          uncaught: payload?.uncaught ?? null,
          results: Array.isArray(payload?.results) ? payload.results : []
        };
        sendJson(res, 200, { accepted: true, updatedAt: lastSmokeReport.updatedAt });
      } catch (error) {
        sendJson(res, 400, { error: error.message || "invalid_request" });
      }
      return;
    }

    sendJson(res, 405, { error: "method_not_allowed" });
    return;
  }

  const relativePath = pathname === "/" ? "/index.html" : pathname;
  const filePath = path.normalize(path.join(ROOT_DIR, relativePath));
  if (!filePath.startsWith(ROOT_DIR)) {
    sendJson(res, 403, { error: "forbidden" });
    return;
  }

  if (fs.existsSync(filePath) && fs.statSync(filePath).isFile()) {
    sendFile(res, filePath);
    return;
  }

  sendJson(res, 404, { error: "not_found" });
});

server.listen(PORT, HOST, () => {
  console.log(`Neon FieldGoal server live at http://${HOST}:${PORT}`);
});
