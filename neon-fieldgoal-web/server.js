const http = require("node:http");
const fs = require("node:fs");
const path = require("node:path");
const { URL } = require("node:url");

const HOST = process.env.HOST || "127.0.0.1";
const PORT = Number.parseInt(process.env.PORT || "4173", 10);
const ROOT_DIR = __dirname;
const DEFAULT_DATA_DIR = path.join(ROOT_DIR, "data");
const BOARD_PATH = process.env.NFG_BOARD_PATH
  ? path.resolve(process.env.NFG_BOARD_PATH)
  : path.join(DEFAULT_DATA_DIR, "community-heat.json");
const DATA_DIR = path.dirname(BOARD_PATH);
const BOARD_LIMIT = 25;

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
  ]
});

const ensureDataDir = () => {
  fs.mkdirSync(DATA_DIR, { recursive: true });
};

const sanitizeHandle = (value) => {
  const cleaned = String(value || "")
    .toUpperCase()
    .replace(/[^A-Z0-9]/g, "")
    .slice(0, 10);
  return cleaned || "GRID01";
};

const normalizeModeKey = (value) => value === "longBomb" ? "longBomb" : "scoreAttack";

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
    longBomb: []
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

        const board = loadBoard();
        if (!board[entry.modeKey].some((row) => entriesMatch(row, entry))) {
          board[entry.modeKey].push(entry);
        }
        board[entry.modeKey] = sortBoard(board[entry.modeKey]).slice(0, BOARD_LIMIT);
        saveBoard(board);

        const rank = board[entry.modeKey].findIndex((row) => entriesMatch(row, entry)) + 1;

        sendJson(res, 200, {
          accepted: true,
          rank: rank > 0 ? rank : null,
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
