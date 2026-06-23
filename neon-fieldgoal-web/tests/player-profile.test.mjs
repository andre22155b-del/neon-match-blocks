import { describe, expect, it } from "vitest";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const {
  createDefaultSettings,
  createDefaultProgression,
  createDefaultCosmetics,
  hydrateSettings,
  hydrateProgression,
  hydrateCosmetics,
  applyRunToProgression
} = require("../player-profile.js");

const tiers = [
  { level: 1, xpRequired: 0, rank: "Street Rookie" },
  { level: 2, xpRequired: 100, rank: "Heat Reader" },
  { level: 3, xpRequired: 250, rank: "Grid Phantom" }
];

const getTierForXp = (xp, list) => {
  let current = list[0];
  for (const tier of list) {
    if (xp >= tier.xpRequired) {
      current = tier;
    }
  }
  return current;
};

describe("player profile helpers", () => {
  it("creates sane default settings and cosmetics", () => {
    expect(createDefaultSettings()).toEqual({
      audioEnabled: true,
      shakeScale: 1,
      fxLevel: "full",
      hapticsEnabled: true
    });
    expect(createDefaultProgression()).toMatchObject({
      xp: 0,
      level: 1,
      totalGoals: 0
    });
    expect(createDefaultCosmetics([
      { id: "cyan" },
      { id: "solar", rewardOnly: true },
      { id: "glacier", rewardOnly: true }
    ])).toEqual({
      selectedSkin: "cyan",
      rewardUnlocks: {
        solar: false,
        glacier: false
      }
    });
  });

  it("hydrates settings and cosmetics safely", () => {
    expect(hydrateSettings({
      audioEnabled: false,
      shakeScale: 0.45,
      fxLevel: "low",
      hapticsEnabled: false
    })).toEqual({
      audioEnabled: false,
      shakeScale: 0.45,
      fxLevel: "low",
      hapticsEnabled: false
    });

    const cosmetics = hydrateCosmetics({
      selectedSkin: "solar",
      rewardUnlocks: { solar: true, glacier: false }
    }, [
      { id: "cyan" },
      { id: "solar", rewardOnly: true },
      { id: "glacier", rewardOnly: true }
    ]);
    expect(cosmetics.selectedSkin).toBe("solar");
    expect(cosmetics.rewardUnlocks.solar).toBe(true);
    expect(cosmetics.rewardUnlocks.glacier).toBe(false);
  });

  it("hydrates progression and recalculates tier level from xp", () => {
    const progression = hydrateProgression({
      xp: "180",
      level: "1",
      totalGoals: "5",
      totalGridDrops: "8"
    }, getTierForXp, tiers);
    expect(progression.xp).toBe(180);
    expect(progression.level).toBe(2);
    expect(progression.totalGoals).toBe(5);
    expect(progression.totalGridDrops).toBe(8);
  });

  it("applies a run summary to progression totals and level-up state", () => {
    const result = applyRunToProgression({
      progression: createDefaultProgression(),
      runSummary: {
        goals: 4,
        runStats: {
          perfectGoals: 2,
          longBombGoals: 1,
          gridDrops: 6,
          gridRowClears: 1
        }
      },
      calculateXp: () => 130,
      getTierForXp,
      tiers
    });
    expect(result.progression.xp).toBe(130);
    expect(result.progression.level).toBe(2);
    expect(result.progression.totalGoals).toBe(4);
    expect(result.progression.totalPerfects).toBe(2);
    expect(result.lastRunProgress).toEqual({
      xpGained: 130,
      levelUp: 1,
      tierName: "Heat Reader"
    });
  });
});
