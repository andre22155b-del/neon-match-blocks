import { randomUUID } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import http from "node:http";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const DATA_DIR = join(__dirname, "data");
const DATA_FILE = join(DATA_DIR, "game-data.json");
const PORT = Number(process.env.PORT || 8787);
const HOST = process.env.HOST || "127.0.0.1";
const CHALLENGE_ROTATION = [
  { id: "hot-hand", label: "Hot Hand", targetScore: 34 },
  { id: "ice-veins", label: "Ice Veins", targetScore: 40 },
  { id: "clean-release", label: "Clean Release", targetScore: 28 },
  { id: "arc-master", label: "Arc Master", targetScore: 44 },
];

const clamp = (v, min, max) => Math.max(min, Math.min(max, v));
const toNum = (v, fallback = 0) => {
  const n = Number(v);
  return Number.isFinite(n) ? n : fallback;
};
const dayKey = (date = new Date()) => {
  const y = date.getUTCFullYear();
  const m = String(date.getUTCMonth() + 1).padStart(2, "0");
  const d = String(date.getUTCDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
};
const rotateDaily = (key = dayKey()) => {
  const seed = key
    .replaceAll("-", "")
    .split("")
    .reduce((sum, ch) => sum + Number(ch), 0);
  const challenge = CHALLENGE_ROTATION[seed % CHALLENGE_ROTATION.length];
  return {
    ...challenge,
    day: key,
    generatedAt: new Date().toISOString(),
  };
};
const rankTier = (points) => {
  if (points >= 85) return "Legend";
  if (points >= 55) return "Diamond";
  if (points >= 32) return "Gold";
  if (points >= 16) return "Silver";
  return "Bronze";
};
const normalizeName = (value) => {
  const txt = (value ?? "").toString().trim().replace(/\s+/g, " ");
  if (!txt) return "PLAYER ONE";
  return txt.slice(0, 24);
};
const createEmptyStore = () => ({
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  players: {},
  sessions: {},
  matches: [],
  dailyChallenge: rotateDaily(),
});

const ensureStoreFile = () => {
  mkdirSync(DATA_DIR, { recursive: true });
  if (!existsSync(DATA_FILE)) {
    writeFileSync(DATA_FILE, `${JSON.stringify(createEmptyStore(), null, 2)}\n`, "utf8");
  }
};

const loadStore = () => {
  ensureStoreFile();
  try {
    const raw = readFileSync(DATA_FILE, "utf8");
    const parsed = JSON.parse(raw);
    if (!parsed || typeof parsed !== "object") return createEmptyStore();
    if (!parsed.players || typeof parsed.players !== "object") parsed.players = {};
    if (!parsed.sessions || typeof parsed.sessions !== "object") parsed.sessions = {};
    if (!Array.isArray(parsed.matches)) parsed.matches = [];
    if (!parsed.dailyChallenge || parsed.dailyChallenge.day !== dayKey()) {
      parsed.dailyChallenge = rotateDaily();
    }
    return parsed;
  } catch {
    return createEmptyStore();
  }
};

let store = loadStore();

const saveStore = () => {
  store.updatedAt = new Date().toISOString();
  writeFileSync(DATA_FILE, `${JSON.stringify(store, null, 2)}\n`, "utf8");
};

const getOrCreatePlayer = (name) => {
  const safe = normalizeName(name);
  if (!store.players[safe]) {
    store.players[safe] = {
      name: safe,
      gamesPlayed: 0,
      wins: 0,
      losses: 0,
      ties: 0,
      totalScore: 0,
      averageScore: 0,
      bestScore: 0,
      bestStreak: 0,
      perfectReleaseRate: 0,
      lastDifficulty: "medium",
      lastMode: "duel",
      updatedAt: new Date().toISOString(),
      createdAt: new Date().toISOString(),
    };
  }
  return store.players[safe];
};

const buildLeaderboard = (limit = 10) => {
  const entries = Object.values(store.players)
    .sort((a, b) => {
      if (b.bestScore !== a.bestScore) return b.bestScore - a.bestScore;
      if (b.averageScore !== a.averageScore) return b.averageScore - a.averageScore;
      if (b.wins !== a.wins) return b.wins - a.wins;
      return b.gamesPlayed - a.gamesPlayed;
    })
    .slice(0, clamp(limit, 1, 50))
    .map((p, i) => ({
      rank: i + 1,
      name: p.name,
      bestScore: p.bestScore,
      averageScore: p.averageScore,
      gamesPlayed: p.gamesPlayed,
      wins: p.wins,
      tier: rankTier(p.bestScore),
    }));
  return entries;
};

const send = (res, status, payload) => {
  const body = JSON.stringify(payload);
  res.writeHead(status, {
    "Content-Type": "application/json; charset=utf-8",
    "Content-Length": Buffer.byteLength(body),
    "Access-Control-Allow-Origin": "*",
    "Access-Control-Allow-Headers": "Content-Type",
    "Access-Control-Allow-Methods": "GET,POST,OPTIONS",
  });
  res.end(body);
};

const readJson = (req) =>
  new Promise((resolve, reject) => {
    let raw = "";
    req.on("data", (chunk) => {
      raw += chunk;
      if (raw.length > 1_000_000) reject(new Error("Payload too large"));
    });
    req.on("end", () => {
      if (!raw) {
        resolve({});
        return;
      }
      try {
        resolve(JSON.parse(raw));
      } catch {
        reject(new Error("Invalid JSON payload"));
      }
    });
    req.on("error", () => reject(new Error("Request read failed")));
  });

const server = http.createServer(async (req, res) => {
  const method = req.method || "GET";
  const url = new URL(req.url || "/", `http://${req.headers.host || "localhost"}`);

  if (!store.dailyChallenge || store.dailyChallenge.day !== dayKey()) {
    store.dailyChallenge = rotateDaily();
    saveStore();
  }

  if (method === "OPTIONS") {
    res.writeHead(204, {
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Headers": "Content-Type",
      "Access-Control-Allow-Methods": "GET,POST,OPTIONS",
    });
    res.end();
    return;
  }

  try {
    if (method === "GET" && url.pathname === "/health") {
      send(res, 200, {
        ok: true,
        service: "neon-hoopz-backend",
        now: new Date().toISOString(),
        players: Object.keys(store.players).length,
      });
      return;
    }

    if (method === "GET" && url.pathname === "/api/config") {
      send(res, 200, {
        game: "Neon Hoopz",
        dailyChallenge: store.dailyChallenge,
        leaderboardCount: Object.keys(store.players).length,
      });
      return;
    }

    if (method === "GET" && url.pathname === "/api/leaderboard") {
      const limit = toNum(url.searchParams.get("limit"), 10);
      send(res, 200, {
        entries: buildLeaderboard(limit),
        updatedAt: store.updatedAt,
      });
      return;
    }

    if (method === "GET" && url.pathname === "/api/recent-matches") {
      const limit = clamp(toNum(url.searchParams.get("limit"), 8), 1, 50);
      send(res, 200, {
        matches: store.matches.slice(0, limit),
      });
      return;
    }

    if (method === "GET" && url.pathname.startsWith("/api/player/")) {
      const playerName = normalizeName(decodeURIComponent(url.pathname.slice("/api/player/".length)));
      const player = store.players[playerName];
      if (!player) {
        send(res, 404, { error: "Player not found" });
        return;
      }
      send(res, 200, {
        profile: player,
      });
      return;
    }

    if (method === "POST" && url.pathname === "/api/session/start") {
      const payload = await readJson(req);
      const playerName = normalizeName(payload.playerName);
      getOrCreatePlayer(playerName);
      const sessionId = randomUUID();
      store.sessions[sessionId] = {
        sessionId,
        playerName,
        mode: payload.mode || "duel",
        difficulty: payload.difficulty || "medium",
        aimMode: payload.aimMode || "casual",
        controlProfile: payload.controlProfile || "rookie",
        physicsProfile: payload.physicsProfile || "arcade",
        dailyMode: !!payload.dailyMode,
        createdAt: new Date().toISOString(),
        finishedAt: null,
      };
      saveStore();
      send(res, 201, {
        sessionId,
        dailyChallenge: store.dailyChallenge,
      });
      return;
    }

    const finishMatch = url.pathname.match(/^\/api\/session\/([^/]+)\/finish$/);
    if (method === "POST" && finishMatch) {
      const sessionId = finishMatch[1];
      const session = store.sessions[sessionId];
      if (!session) {
        send(res, 404, { error: "Session not found" });
        return;
      }
      if (session.finishedAt) {
        send(res, 409, { error: "Session already finished" });
        return;
      }

      const payload = await readJson(req);
      const player = getOrCreatePlayer(session.playerName);
      const score = Math.max(0, Math.round(toNum(payload.score, 0)));
      const opponentScore = Math.max(0, Math.round(toNum(payload.opponentScore, 0)));
      const stats = payload.stats && typeof payload.stats === "object" ? payload.stats : {};
      const attempts = Math.max(0, Math.round(toNum(stats.attempts, 0)));
      const perfect = Math.max(0, Math.round(toNum(stats.perfectReleases, 0)));
      const perfectRate = attempts > 0 ? Math.round((perfect / attempts) * 100) : 0;
      const won = !!payload.won;
      const tie = !!payload.tie;

      player.gamesPlayed += 1;
      player.totalScore += score;
      player.bestScore = Math.max(player.bestScore, score);
      player.bestStreak = Math.max(player.bestStreak, Math.round(toNum(stats.bestStreak, stats.streak || 0)));
      player.averageScore = Number((player.totalScore / Math.max(1, player.gamesPlayed)).toFixed(1));
      player.perfectReleaseRate = Number(
        (
          (player.perfectReleaseRate * Math.max(0, player.gamesPlayed - 1) + perfectRate) /
          Math.max(1, player.gamesPlayed)
        ).toFixed(1)
      );
      player.lastDifficulty = payload.difficulty || session.difficulty || "medium";
      player.lastMode = payload.mode || session.mode || "duel";
      if (tie) player.ties += 1;
      else if (won) player.wins += 1;
      else player.losses += 1;
      player.updatedAt = new Date().toISOString();

      session.finishedAt = new Date().toISOString();
      session.result = {
        score,
        opponentScore,
        won,
        tie,
        mode: payload.mode || session.mode,
        durationSec: Math.max(0, Math.round(toNum(payload.durationSec, 0))),
      };

      const match = {
        matchId: randomUUID(),
        playerName: player.name,
        score,
        opponentScore,
        won,
        tie,
        mode: payload.mode || session.mode,
        difficulty: payload.difficulty || session.difficulty,
        aimMode: payload.aimMode || session.aimMode,
        controlProfile: payload.controlProfile || session.controlProfile,
        physicsProfile: payload.physicsProfile || session.physicsProfile,
        dailyMode: !!(payload.dailyMode ?? session.dailyMode),
        perfectRate,
        createdAt: session.finishedAt,
      };
      store.matches.unshift(match);
      if (store.matches.length > 200) store.matches.length = 200;

      saveStore();
      send(res, 200, {
        ok: true,
        player,
        match,
        leaderboard: buildLeaderboard(10),
      });
      return;
    }

    send(res, 404, { error: "Not found" });
  } catch (error) {
    send(res, 400, { error: error?.message || "Bad request" });
  }
});

server.on("error", (error) => {
  // eslint-disable-next-line no-console
  console.error(`Backend failed to start on ${HOST}:${PORT} (${error.code || "ERR"})`);
  process.exit(1);
});

server.listen(PORT, HOST, () => {
  // eslint-disable-next-line no-console
  console.log(`Neon Hoopz backend listening on http://${HOST}:${PORT}`);
});
