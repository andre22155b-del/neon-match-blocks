import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  API_BASE,
  finishMatchSession,
  getBackendConfig,
  getBackendHealth,
  getLeaderboard,
  getPlayerProfile,
  getRecentMatches,
  startMatchSession,
} from "./backend.js";

describe("backend client", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("requests health with defaults", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ ok: true }),
    });
    global.fetch = fetchMock;

    const res = await getBackendHealth();

    expect(res.ok).toBe(true);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, options] = fetchMock.mock.calls[0];
    expect(url).toBe(`${API_BASE}/health`);
    expect(options.method).toBe("GET");
    expect(options.body).toBeUndefined();
    expect(options.headers["Content-Type"]).toBe("application/json");
  });

  it("clamps leaderboard and recent match limits", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ entries: [], matches: [] }),
    });
    global.fetch = fetchMock;

    await getLeaderboard(500);
    await getRecentMatches(0);

    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE}/api/leaderboard?limit=50`);
    expect(fetchMock.mock.calls[1][0]).toBe(`${API_BASE}/api/recent-matches?limit=1`);
  });

  it("encodes player names in profile route", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ profile: { name: "A B" } }),
    });
    global.fetch = fetchMock;

    await getPlayerProfile("A B");

    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE}/api/player/A%20B`);
  });

  it("posts session payloads", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 201,
      json: () => Promise.resolve({ sessionId: "s1" }),
    });
    global.fetch = fetchMock;

    await startMatchSession({ playerName: "TEST", mode: "duel" });
    await finishMatchSession("s1", { score: 10 });

    const [, startOptions] = fetchMock.mock.calls[0];
    const [finishUrl, finishOptions] = fetchMock.mock.calls[1];
    expect(startOptions.method).toBe("POST");
    expect(startOptions.body).toBe(JSON.stringify({ playerName: "TEST", mode: "duel" }));
    expect(finishUrl).toBe(`${API_BASE}/api/session/s1/finish`);
    expect(finishOptions.body).toBe(JSON.stringify({ score: 10 }));
  });

  it("throws server error message when request fails", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 400,
      json: () => Promise.resolve({ error: "bad request" }),
    });
    global.fetch = fetchMock;

    await expect(startMatchSession({})).rejects.toThrow("bad request");
  });

  it("falls back to status message when response json fails", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 503,
      json: () => Promise.reject(new Error("invalid json")),
    });
    global.fetch = fetchMock;

    await expect(getBackendConfig()).rejects.toThrow("Request failed (503)");
  });

  it("aborts pending request on timeout", async () => {
    vi.useFakeTimers();
    try {
      const fetchMock = vi.fn((_, options) => {
        return new Promise((_, reject) => {
          options.signal.addEventListener("abort", () => reject(new Error("aborted")));
        });
      });
      global.fetch = fetchMock;

      const pending = getBackendHealth();
      const handled = pending.catch((err) => err);
      await vi.advanceTimersByTimeAsync(3000);
      const err = await handled;
      expect(err.message).toContain("aborted");
    } finally {
      vi.useRealTimers();
    }
  });
});
