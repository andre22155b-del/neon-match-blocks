const configuredBase = String(import.meta.env.VITE_API_URL || "").trim();
const API_BASE = configuredBase || "http://localhost:8787";
const BACKEND_ENABLED = Boolean(configuredBase) || import.meta.env.MODE === "test";

const request = async (path, options = {}) => {
  if (!BACKEND_ENABLED) {
    throw new Error("Backend disabled: set VITE_API_URL to enable online services.");
  }
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), options.timeoutMs ?? 2800);
  try {
    const response = await fetch(`${API_BASE}${path}`, {
      method: options.method || "GET",
      headers: {
        "Content-Type": "application/json",
        ...(options.headers || {}),
      },
      body: options.body ? JSON.stringify(options.body) : undefined,
      signal: controller.signal,
    });
    const payload = await response.json().catch(() => ({}));
    if (!response.ok) {
      throw new Error(payload.error || `Request failed (${response.status})`);
    }
    return payload;
  } finally {
    clearTimeout(timeout);
  }
};

export const getBackendHealth = () => request("/health");
export const getLeaderboard = (limit = 8) => request(`/api/leaderboard?limit=${Math.max(1, Math.min(50, limit))}`);
export const getRecentMatches = (limit = 6) =>
  request(`/api/recent-matches?limit=${Math.max(1, Math.min(50, limit))}`);
export const getPlayerProfile = (name) => request(`/api/player/${encodeURIComponent(name)}`);
export const getBackendConfig = () => request("/api/config");
export const startMatchSession = (payload) => request("/api/session/start", { method: "POST", body: payload });
export const finishMatchSession = (sessionId, payload) =>
  request(`/api/session/${encodeURIComponent(sessionId)}/finish`, { method: "POST", body: payload });

export { API_BASE, BACKEND_ENABLED };
