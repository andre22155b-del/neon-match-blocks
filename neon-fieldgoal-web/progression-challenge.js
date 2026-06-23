(function (root, factory) {
  const api = factory();
  if (typeof module !== "undefined" && module.exports) {
    module.exports = api;
  }
  root.NFGProgressionChallenge = api;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  function createDefaultProgression() {
    return {
      xp: 0,
      level: 1,
      totalGoals: 0,
      totalPerfects: 0,
      totalLongBombs: 0,
      totalGridDrops: 0,
      totalRowClears: 0
    };
  }

  function getTierForXp(xp, tiers) {
    let current = tiers[0];
    for (const tier of tiers) {
      if (xp >= tier.xpRequired) {
        current = tier;
      } else {
        break;
      }
    }
    return current;
  }

  function getProgressionModifiers(tier) {
    return {
      perfectWindowBonus: tier?.perfectWindowBonus || 0,
      windMitigation: tier?.windMitigation || 0,
      longBombPointBonus: tier?.longBombPointBonus || 0,
      gridBonusBoost: tier?.gridBonusBoost || 0
    };
  }

  function getSeededTemplateForKey(key, templates) {
    const seed = Array.from(String(key || "")).reduce((total, char) => total + char.charCodeAt(0), 0);
    return templates[seed % templates.length];
  }

  function createChallengeFromTemplate(keyField, keyValue, template, extra = {}) {
    return {
      [keyField]: keyValue,
      id: template.id,
      title: template.title,
      copy: template.copy,
      metric: template.metric,
      target: template.target,
      modeKey: template.modeKey,
      accumulate: !!template.accumulate,
      rewardSkin: template.rewardSkin,
      progress: 0,
      completed: false,
      rewarded: false,
      ...extra
    };
  }

  function createDailyChallenge(dateKey, templates) {
    const template = getSeededTemplateForKey(dateKey, templates);
    return createChallengeFromTemplate("dateKey", dateKey, template);
  }

  function createWeeklyChallenge(weekKey, templates) {
    const template = getSeededTemplateForKey(weekKey, templates);
    return createChallengeFromTemplate("weekKey", weekKey, template, {
      bonusXp: template.bonusXp || 0
    });
  }

  function getChallengeRunMetric(challenge, runSnapshot) {
    switch (challenge.metric) {
      case "perfectGoals":
        return runSnapshot.runStats?.perfectGoals || 0;
      case "longBombGoals":
        return runSnapshot.runStats?.longBombGoals || 0;
      case "suddenRuns":
        return runSnapshot.runStats?.suddenRuns || 0;
      case "gridDrops":
        return runSnapshot.runStats?.gridDrops || 0;
      case "gridRowClears":
        return runSnapshot.runStats?.gridRowClears || 0;
      case "gridMatchBlocks":
        return runSnapshot.runStats?.gridMatchBlocks || 0;
      case "gridMaxCombo":
        return runSnapshot.runStats?.maxGridCombo || 0;
      case "maxStreak":
        return runSnapshot.maxStreak || 0;
      case "goals":
        return runSnapshot.goals || 0;
      case "score":
      default:
        return runSnapshot.score || 0;
    }
  }

  function advanceChallengeProgress(challenge, runSnapshot, activeModeKey) {
    const summary = {
      challenge: challenge ? { ...challenge } : null,
      progressChanged: false,
      completedNow: false,
      runMetric: 0
    };

    if (!challenge) {
      return summary;
    }

    if (challenge.modeKey !== "any" && challenge.modeKey !== activeModeKey) {
      return summary;
    }

    const runMetric = getChallengeRunMetric(challenge, runSnapshot);
    const nextProgress = challenge.accumulate
      ? Math.min(challenge.target, challenge.progress + runMetric)
      : Math.min(challenge.target, Math.max(challenge.progress, runMetric));

    summary.runMetric = runMetric;
    summary.progressChanged = nextProgress !== challenge.progress;
    summary.completedNow = !challenge.completed && nextProgress >= challenge.target;
    summary.challenge = {
      ...challenge,
      progress: nextProgress,
      completed: challenge.completed || nextProgress >= challenge.target,
      rewarded: challenge.rewarded || summary.completedNow
    };
    return summary;
  }

  function calculateProgressionXp(runSummary) {
    return Math.round(
      18 +
      (runSummary.score || 0) * 0.7 +
      (runSummary.goals || 0) * 10 +
      (runSummary.runStats?.longBombGoals || 0) * 8 +
      (runSummary.runStats?.perfectGoals || 0) * 9 +
      (runSummary.runStats?.suddenRuns || 0) * 35 +
      (runSummary.runStats?.gridDrops || 0) * 2 +
      (runSummary.runStats?.gridMatchBlocks || 0) * 4 +
      (runSummary.runStats?.gridRowClears || 0) * 18 +
      (runSummary.runStats?.maxGridCombo || 0) * 6 +
      (runSummary.runStats?.gridJackpots || 0) * 28 +
      (runSummary.maxStreak || 0) * 4 +
      (runSummary.lastRunChallenge?.bonusXp || 0) +
      (runSummary.lastRunWeekly?.bonusXp || 0) +
      (runSummary.lastRunRivals?.xpBonus || 0)
    );
  }

  return {
    createDefaultProgression,
    getTierForXp,
    getProgressionModifiers,
    getSeededTemplateForKey,
    createDailyChallenge,
    createWeeklyChallenge,
    getChallengeRunMetric,
    advanceChallengeProgress,
    calculateProgressionXp
  };
});
