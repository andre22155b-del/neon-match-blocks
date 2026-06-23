import { describe, expect, it } from "vitest";
import {
  DEFAULT_GAME_SETUP,
  GAME_SETUP_OPTIONS,
  sanitizeGameSetup,
} from "./gameSetup.js";

describe("sanitizeGameSetup", () => {
  it("passes through valid setup values", () => {
    const input = {
      shotDifficulty: "easy",
      aimMode: "pro",
      dominantHand: "left",
      controlProfile: "precision",
      physicsProfile: "sim",
      ballSkin: "prism",
      playerBallColor: "pink",
      opponentBallColor: "lime",
    };
    const result = sanitizeGameSetup(input);
    expect(result.changed).toBe(false);
    expect(result.issues).toEqual([]);
    expect(result.shotDifficulty).toBe("easy");
    expect(result.aimMode).toBe("pro");
    expect(result.dominantHand).toBe("left");
    expect(result.controlProfile).toBe("precision");
    expect(result.physicsProfile).toBe("sim");
    expect(result.ballSkin).toBe("prism");
    expect(result.playerBallColor).toBe("pink");
    expect(result.opponentBallColor).toBe("lime");
  });

  it("maps legacy physics profiles to motion presets", () => {
    const realistic = sanitizeGameSetup({ physicsProfile: "realistic" });
    const park = sanitizeGameSetup({ physicsProfile: "park" });
    expect(realistic.physicsProfile).toBe("balanced");
    expect(park.physicsProfile).toBe("sim");
  });

  it("falls back invalid setup fields to defaults", () => {
    const result = sanitizeGameSetup({
      shotDifficulty: "impossible",
      aimMode: "assist",
      dominantHand: "upside_down",
      controlProfile: "god",
      physicsProfile: "moon",
      ballSkin: "wireframe",
      playerBallColor: "ultraviolet",
      opponentBallColor: "unknown",
    });

    expect(result.changed).toBe(true);
    expect(result.shotDifficulty).toBe(DEFAULT_GAME_SETUP.shotDifficulty);
    expect(result.aimMode).toBe(DEFAULT_GAME_SETUP.aimMode);
    expect(result.controlProfile).toBe(DEFAULT_GAME_SETUP.controlProfile);
    expect(result.physicsProfile).toBe(DEFAULT_GAME_SETUP.physicsProfile);
    expect(result.ballSkin).toBe(DEFAULT_GAME_SETUP.ballSkin);
    expect(result.playerBallColor).toBe(DEFAULT_GAME_SETUP.playerBallColor);
    expect(result.opponentBallColor).toBe(DEFAULT_GAME_SETUP.opponentBallColor);
    expect(result.issues).toHaveLength(Object.keys(DEFAULT_GAME_SETUP).length);
  });

  it("uses defaults when fields are missing", () => {
    const result = sanitizeGameSetup({});
    for (const key of Object.keys(DEFAULT_GAME_SETUP)) {
      expect(result[key]).toBe(DEFAULT_GAME_SETUP[key]);
    }
  });

  it("option lists stay aligned with defaults", () => {
    for (const [key, fallback] of Object.entries(DEFAULT_GAME_SETUP)) {
      expect(GAME_SETUP_OPTIONS[key]).toContain(fallback);
    }
  });

  it("still sanitizes when option list key is missing", () => {
    const backup = GAME_SETUP_OPTIONS.ballSkin;
    delete GAME_SETUP_OPTIONS.ballSkin;
    try {
      const result = sanitizeGameSetup({ ballSkin: "prism" });
      expect(result.ballSkin).toBe(DEFAULT_GAME_SETUP.ballSkin);
      expect(result.changed).toBe(true);
    } finally {
      GAME_SETUP_OPTIONS.ballSkin = backup;
    }
  });
});
