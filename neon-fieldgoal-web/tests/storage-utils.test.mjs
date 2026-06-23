import { describe, expect, it } from "vitest";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const {
  loadJson,
  saveJson,
  loadNumber,
  removeKey
} = require("../storage-utils.js");

function createMemoryStorage(seed = {}) {
  const store = new Map(Object.entries(seed));
  return {
    getItem(key) {
      return store.has(key) ? store.get(key) : null;
    },
    setItem(key, value) {
      store.set(key, String(value));
    },
    removeItem(key) {
      store.delete(key);
    }
  };
}

describe("storage utils", () => {
  it("loads and saves json safely", () => {
    const storage = createMemoryStorage();
    expect(saveJson(storage, "alpha", { score: 42 })).toBe(true);
    expect(loadJson(storage, "alpha", null)).toEqual({ score: 42 });
  });

  it("returns fallbacks for invalid json or missing keys", () => {
    const storage = createMemoryStorage({ broken: "{not-json" });
    expect(loadJson(storage, "broken", { safe: true })).toEqual({ safe: true });
    expect(loadJson(storage, "missing", 12)).toBe(12);
  });

  it("loads numbers and removes keys safely", () => {
    const storage = createMemoryStorage({ best: "88" });
    expect(loadNumber(storage, "best", 0)).toBe(88);
    expect(removeKey(storage, "best")).toBe(true);
    expect(loadNumber(storage, "best", 5)).toBe(5);
  });
});
