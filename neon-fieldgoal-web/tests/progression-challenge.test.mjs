import { describe, expect, it } from "vitest";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const {
  createDefaultProgression,
  getTierForXp,
  getProgressionModifiers,
  createDailyChallenge,
  createWeeklyChallenge,
  advanceChallengeProgress,
  calculateProgressionXp
} = require("../progression-challenge.js");

describe("progression and challenge helpers", () => {
  const tiers = [
    { level: 1, xpRequired: 0, perfectWindowBonus: 0, windMitigation: 0, longBombPointBonus: 0, gridBonusBoost: 0 },
    { level: 2, xpRequired: 100, perfectWindowBonus: 0.02, windMitigation: 0.1, longBombPointBonus: 1, gridBonusBoost: 2 }
  ];

  const dailyTemplates = [
    { id: "a", title: "A", copy: "A", metric: "score", target: 20, modeKey: "any", accumulate: false, rewardSkin: "solar" },
    { id: "b", title: "B", copy: "B", metric: "gridDrops", target: 12, modeKey: "any", accumulate: true, rewardSkin: "solar" }
  ];

  const weeklyTemplates = [
    { id: "w", title: "W", copy: "W", metric: "suddenRuns", target: 3, modeKey: "suddenDeath", accumulate: true, rewardSkin: "glacier", bonusXp: 120 }
  ];

  it("creates the default progression shape", () => {
    expect(createDefaultProgression()).toEqual({
      xp: 0,
      level: 1,
      totalGoals: 0,
      totalPerfects: 0,
      totalLongBombs: 0,
      totalGridDrops: 0,
      totalRowClears: 0
    });
  });

  it("returns the correct tier and modifiers for xp", () => {
    const tier = getTierForXp(140, tiers);
    expect(tier.level).toBe(2);
    expect(getProgressionModifiers(tier)).toEqual({
      perfectWindowBonus: 0.02,
      windMitigation: 0.1,
      longBombPointBonus: 1,
      gridBonusBoost: 2
    });
  });

  it("creates daily and weekly challenges from seeded templates", () => {
    const daily = createDailyChallenge("2026-03-23", dailyTemplates);
    const weekly = createWeeklyChallenge("2026-W13", weeklyTemplates);
    expect(daily.dateKey).toBe("2026-03-23");
    expect(daily.id).toBeTruthy();
    expect(weekly.weekKey).toBe("2026-W13");
    expect(weekly.bonusXp).toBe(120);
  });

  it("advances accumulate and non-accumulate challenges correctly", () => {
    const accumulate = {
      id: "b",
      title: "B",
      metric: "gridDrops",
      target: 12,
      modeKey: "any",
      accumulate: true,
      rewardSkin: "solar",
      progress: 4,
      completed: false,
      rewarded: false
    };
    const resultA = advanceChallengeProgress(accumulate, {
      score: 10,
      goals: 1,
      maxStreak: 2,
      runStats: { gridDrops: 5 }
    }, "scoreAttack");
    expect(resultA.challenge.progress).toBe(9);
    expect(resultA.completedNow).toBe(false);

    const oneShot = {
      id: "a",
      title: "A",
      metric: "score",
      target: 20,
      modeKey: "any",
      accumulate: false,
      rewardSkin: "solar",
      progress: 0,
      completed: false,
      rewarded: false
    };
    const resultB = advanceChallengeProgress(oneShot, {
      score: 22,
      goals: 3,
      maxStreak: 3,
      runStats: {}
    }, "scoreAttack");
    expect(resultB.challenge.progress).toBe(20);
    expect(resultB.completedNow).toBe(true);
    expect(resultB.challenge.completed).toBe(true);
  });

  it("computes progression xp from the full run summary", () => {
    const xp = calculateProgressionXp({
      score: 100,
      goals: 8,
      maxStreak: 5,
      runStats: {
        longBombGoals: 2,
        perfectGoals: 3,
        suddenRuns: 1,
        gridDrops: 10,
        gridRowClears: 2,
        maxGridCombo: 4,
        gridJackpots: 1
      },
      lastRunChallenge: { bonusXp: 45 },
      lastRunWeekly: { bonusXp: 120 },
      lastRunRivals: { xpBonus: 32 }
    });

    expect(xp).toBe(571);
  });
});
