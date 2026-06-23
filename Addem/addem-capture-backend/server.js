import { createServer } from "node:http";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { randomUUID } from "node:crypto";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const PORT = Number(process.env.PORT || 8787);
const HOST = process.env.HOST || "127.0.0.1";
const API_TOKEN = process.env.ADDEM_API_TOKEN || "";
const DB_DIR = path.join(__dirname, "data");
const DB_FILE = path.join(DB_DIR, "captures.json");

let writeQueue = Promise.resolve();

bootstrap()
  .then(() => {
    const server = createServer(requestHandler);
    server.listen(PORT, HOST, () => {
      console.log(`AddEm capture backend listening on http://${HOST}:${PORT}`);
      if (API_TOKEN) {
        console.log("Token auth enabled (ADDEM_API_TOKEN)");
      } else {
        console.log("Token auth disabled (set ADDEM_API_TOKEN to enable)");
      }
    });
  })
  .catch((error) => {
    console.error("Failed to start backend:", error);
    process.exit(1);
  });

async function bootstrap() {
  await mkdir(DB_DIR, { recursive: true });
  try {
    await readFile(DB_FILE, "utf8");
  } catch {
    await writeFile(DB_FILE, "[]\n", "utf8");
  }
}

async function requestHandler(req, res) {
  try {
    setCORSHeaders(res);

    if (req.method === "OPTIONS") {
      res.writeHead(204);
      res.end();
      return;
    }

    const url = new URL(req.url || "/", `http://${req.headers.host || `${HOST}:${PORT}`}`);

    if (req.method === "GET" && url.pathname === "/health") {
      sendJSON(res, 200, {
        ok: true,
        service: "addem-capture-backend",
        timestamp: new Date().toISOString()
      });
      return;
    }

    if (req.method === "GET" && url.pathname === "/captures") {
      if (!isAuthorized(req)) {
        sendJSON(res, 401, { error: "Unauthorized" });
        return;
      }

      const limit = clampNumber(url.searchParams.get("limit"), 1, 500, 50);
      const captures = await readCaptures();
      sendJSON(res, 200, {
        count: captures.length,
        items: captures.slice(0, limit)
      });
      return;
    }

    if (req.method === "POST" && url.pathname === "/captures") {
      if (!isAuthorized(req)) {
        sendJSON(res, 401, { error: "Unauthorized" });
        return;
      }

      const payload = await readBodyJSON(req);
      const capture = validateAndNormalizeCapture(payload);

      await enqueueWrite(async () => {
        const captures = await readCaptures();
        const deduped = captures.filter(
          (item) => !(item.url === capture.url && item.title === capture.title && item.notes === capture.notes)
        );
        deduped.unshift(capture);
        await writeCaptures(deduped);
      });

      sendJSON(res, 201, {
        ok: true,
        id: capture.id,
        message: "Capture saved",
        capture
      });
      return;
    }

    if (req.method === "DELETE" && url.pathname.startsWith("/captures/")) {
      if (!isAuthorized(req)) {
        sendJSON(res, 401, { error: "Unauthorized" });
        return;
      }

      const id = decodeURIComponent(url.pathname.replace("/captures/", "")).trim();
      if (!id) {
        sendJSON(res, 400, { error: "Missing capture id" });
        return;
      }

      let deleted = false;
      await enqueueWrite(async () => {
        const captures = await readCaptures();
        const next = captures.filter((item) => item.id !== id);
        deleted = next.length !== captures.length;
        await writeCaptures(next);
      });

      sendJSON(res, deleted ? 200 : 404, {
        ok: deleted,
        message: deleted ? "Capture deleted" : "Capture not found"
      });
      return;
    }

    sendJSON(res, 404, {
      error: "Not Found",
      routes: [
        "GET /health",
        "GET /captures",
        "POST /captures",
        "DELETE /captures/:id"
      ]
    });
  } catch (error) {
    sendJSON(res, 400, { error: error instanceof Error ? error.message : String(error) });
  }
}

function setCORSHeaders(res) {
  res.setHeader("Access-Control-Allow-Origin", "*");
  res.setHeader("Access-Control-Allow-Methods", "GET,POST,DELETE,OPTIONS");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");
}

function isAuthorized(req) {
  if (!API_TOKEN) {
    return true;
  }

  const auth = req.headers.authorization || "";
  return auth === `Bearer ${API_TOKEN}`;
}

function sendJSON(res, statusCode, payload) {
  res.writeHead(statusCode, { "Content-Type": "application/json; charset=utf-8" });
  res.end(`${JSON.stringify(payload)}\n`);
}

function clampNumber(raw, min, max, fallback) {
  const value = Number(raw);
  if (!Number.isFinite(value)) {
    return fallback;
  }
  return Math.min(max, Math.max(min, Math.floor(value)));
}

async function readBodyJSON(req) {
  let data = "";

  for await (const chunk of req) {
    data += chunk;
    if (data.length > 1024 * 1024) {
      throw new Error("Payload too large (max 1MB)");
    }
  }

  if (!data.trim()) {
    throw new Error("Request body is required");
  }

  try {
    return JSON.parse(data);
  } catch {
    throw new Error("Invalid JSON body");
  }
}

function validateAndNormalizeCapture(payload) {
  if (!payload || typeof payload !== "object") {
    throw new Error("Capture must be an object");
  }

  const rawURL = String(payload.url || "").trim();
  if (!rawURL) {
    throw new Error("url is required");
  }

  let normalizedURL = rawURL;
  if (!/^[a-zA-Z][a-zA-Z\d+\-.]*:/.test(normalizedURL)) {
    normalizedURL = `https://${normalizedURL}`;
  }

  let parsedURL;
  try {
    parsedURL = new URL(normalizedURL);
  } catch {
    throw new Error("url is not valid");
  }

  if (!(parsedURL.protocol === "http:" || parsedURL.protocol === "https:")) {
    throw new Error("url must use http or https");
  }

  const title = String(payload.title || parsedURL.hostname || "Untitled").trim() || "Untitled";
  const notes = String(payload.notes || "").trim();
  const source = String(payload.source || "extension").trim() || "extension";
  const createdAt = parseDate(payload.createdAt) || new Date().toISOString();
  const tags = normalizeTags(payload.tags, parsedURL.hostname);

  return {
    id: String(payload.id || randomUUID()),
    title,
    url: parsedURL.toString(),
    notes,
    tags,
    source,
    createdAt
  };
}

function parseDate(raw) {
  if (typeof raw !== "string") {
    return null;
  }

  const date = new Date(raw);
  if (Number.isNaN(date.getTime())) {
    return null;
  }

  return date.toISOString();
}

function normalizeTags(value, hostname) {
  const fromPayload = Array.isArray(value)
    ? value
    : typeof value === "string"
      ? value.split(",")
      : [];

  const normalized = fromPayload
    .map((tag) => String(tag).trim().toLowerCase())
    .filter(Boolean);

  const base = hostname.replace(/^www\./, "").split(".")[0];
  if (base) {
    normalized.unshift(base.toLowerCase());
  }
  normalized.push("web");

  return Array.from(new Set(normalized));
}

async function readCaptures() {
  const raw = await readFile(DB_FILE, "utf8");
  const parsed = JSON.parse(raw);
  if (!Array.isArray(parsed)) {
    return [];
  }

  return parsed;
}

async function writeCaptures(captures) {
  await writeFile(DB_FILE, `${JSON.stringify(captures, null, 2)}\n`, "utf8");
}

function enqueueWrite(task) {
  writeQueue = writeQueue.then(task, task);
  return writeQueue;
}
