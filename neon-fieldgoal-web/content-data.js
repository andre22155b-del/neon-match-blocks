(function (root, factory) {
  const api = factory();
  if (typeof module !== "undefined" && module.exports) {
    module.exports = api;
  }
  root.NFGContentData = api;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  const progressionTiers = [
    { level: 1, xpRequired: 0, rank: "Street Rookie", perkTitle: "Raw Neon Boots", perkCopy: "No bonus yet. Land kicks to start building your edge.", perfectWindowBonus: 0, windMitigation: 0, longBombPointBonus: 0, gridBonusBoost: 0 },
    { level: 2, xpRequired: 80, rank: "Heat Reader", perkTitle: "Focus Window", perkCopy: "Perfect release window gets slightly wider.", perfectWindowBonus: 0.015, windMitigation: 0, longBombPointBonus: 0, gridBonusBoost: 0 },
    { level: 3, xpRequired: 190, rank: "Wind Cutter", perkTitle: "Drift Guard", perkCopy: "Wind influence softens just enough to reward clean reads.", perfectWindowBonus: 0.015, windMitigation: 0.08, longBombPointBonus: 0, gridBonusBoost: 0 },
    { level: 4, xpRequired: 340, rank: "Grid Phantom", perkTitle: "Color Read", perkCopy: "Goal-grid color groups become easier to read at speed.", perfectWindowBonus: 0.02, windMitigation: 0.08, longBombPointBonus: 1, gridBonusBoost: 1 },
    { level: 5, xpRequired: 560, rank: "Neon Captain", perkTitle: "Full Heat", perkCopy: "Wider perfects and softer drift keep your pressure kicks cleaner.", perfectWindowBonus: 0.03, windMitigation: 0.14, longBombPointBonus: 1, gridBonusBoost: 1 },
    { level: 6, xpRequired: 860, rank: "Arc Runner", perkTitle: "Clean Arc", perkCopy: "Perfect window edges get a little more forgiving.", perfectWindowBonus: 0.034, windMitigation: 0.14, longBombPointBonus: 1, gridBonusBoost: 1 },
    { level: 7, xpRequired: 1240, rank: "Barrier Ghost", perkTitle: "Gap Reader", perkCopy: "Threading walls gets easier when your reads are clean.", perfectWindowBonus: 0.038, windMitigation: 0.17, longBombPointBonus: 1, gridBonusBoost: 2 },
    { level: 8, xpRequired: 1700, rank: "Sky Burner", perkTitle: "Launch Heat", perkCopy: "Wind drift keeps softening and deep kicks stay readable.", perfectWindowBonus: 0.04, windMitigation: 0.2, longBombPointBonus: 2, gridBonusBoost: 2 },
    { level: 9, xpRequired: 2240, rank: "Pulse Lord", perkTitle: "Pressure Control", perkCopy: "Late-round and long-bomb kicks hold their line better.", perfectWindowBonus: 0.043, windMitigation: 0.22, longBombPointBonus: 2, gridBonusBoost: 2 },
    { level: 10, xpRequired: 2860, rank: "Mustang Prime", perkTitle: "Full Stadium Heat", perkCopy: "Top-tier control, softer drift, full arcade authority.", perfectWindowBonus: 0.05, windMitigation: 0.25, longBombPointBonus: 2, gridBonusBoost: 3 },
    { level: 11, xpRequired: 3560, rank: "Neon Oracle", perkTitle: "Grid Read", perkCopy: "Goal-grid breaks and clears stay easier to read under pressure.", perfectWindowBonus: 0.053, windMitigation: 0.26, longBombPointBonus: 2, gridBonusBoost: 3 },
    { level: 12, xpRequired: 4340, rank: "Lane Hacker", perkTitle: "Combo Voltage", perkCopy: "Consecutive grid smashes keep your pressure reads calmer.", perfectWindowBonus: 0.056, windMitigation: 0.27, longBombPointBonus: 3, gridBonusBoost: 4 },
    { level: 13, xpRequired: 5220, rank: "Grid Reaper", perkTitle: "Chain Breaker", perkCopy: "Color clears and deep makes spike your run higher.", perfectWindowBonus: 0.06, windMitigation: 0.29, longBombPointBonus: 3, gridBonusBoost: 4 },
    { level: 14, xpRequired: 6200, rank: "Festival Ace", perkTitle: "Takeover Meter", perkCopy: "The whole lane gets hotter when you keep the grid alive.", perfectWindowBonus: 0.064, windMitigation: 0.31, longBombPointBonus: 3, gridBonusBoost: 5 },
    { level: 15, xpRequired: 7280, rank: "Mustang Myth", perkTitle: "Full Hybrid Chaos", perkCopy: "Peak control, peak wind read, peak hybrid chaos.", perfectWindowBonus: 0.07, windMitigation: 0.33, longBombPointBonus: 4, gridBonusBoost: 6 }
  ];

  const modes = {
    scoreAttack: { key: "scoreAttack", label: "Score Attack", subtitle: "Arcade kick + block clear", hudTag: "Score Attack", launchRules: "20 YD START • HIT BLOCKS FOR 1 PT • MATCH COLORS FOR BONUS", kickoffSub: "KICK THROUGH THE STACK", resultLabel: "Score Attack", roundSeconds: 60, startYardLine: 20 },
    longBomb: { key: "longBomb", label: "Long Bomb Only", subtitle: "Deep kicks • bigger risk", hudTag: "Long Bomb", launchRules: "50 YD START • DEEP SHOTS BREAK BLOCKS • COLOR CLEARS SCORE BIG", kickoffSub: "DEEP SHOT MODE", resultLabel: "Long Bomb Only", roundSeconds: 45, startYardLine: 50 },
    suddenDeath: { key: "suddenDeath", label: "Sudden Death", subtitle: "One miss ends it", hudTag: "Sudden Death", launchRules: "35 YD START • ONE MISS ENDS THE RUN • BREAK BLOCKS FOR BONUS", kickoffSub: "DON'T MISS", resultLabel: "Sudden Death", roundSeconds: 90, startYardLine: 35, suddenDeath: true }
  };

  const skinDefs = [
    { id: "cyan", label: "Cyan Core", copy: "Default trail with clean arcade glow.", primary: "#00f5ff", secondary: "#00ff88", accent: "#00f5ff", trail: "rgba(0,245,255,0.7)", unlockLevel: 1 },
    { id: "magenta", label: "Mag Surge", copy: "Hot pink trail unlocked at level 2.", primary: "#ff006e", secondary: "#9b00ff", accent: "#ff4da1", trail: "rgba(255,0,110,0.78)", unlockLevel: 2 },
    { id: "lime", label: "Lime Burn", copy: "Sharp green cut for level 3 heat.", primary: "#00ff88", secondary: "#9cff00", accent: "#9cff00", trail: "rgba(0,255,136,0.78)", unlockLevel: 3 },
    { id: "phantom", label: "Phantom Grid", copy: "Deep purple line set unlocked at level 4.", primary: "#9b00ff", secondary: "#00f5ff", accent: "#c06dff", trail: "rgba(155,0,255,0.82)", unlockLevel: 4 },
    { id: "solar", label: "Solar Circuit", copy: "Daily reward skin with gold-pink flash.", primary: "#ffe600", secondary: "#ff006e", accent: "#ffe600", trail: "rgba(255,230,0,0.82)", unlockLevel: 1, rewardOnly: true },
    { id: "nova", label: "Nova Flicker", copy: "Electric violet burn unlocked at level 6.", primary: "#ff7a1a", secondary: "#ff006e", accent: "#ffd166", trail: "rgba(255,122,26,0.8)", unlockLevel: 6 },
    { id: "glacier", label: "Glacier Rush", copy: "Weekly reward skin with ice-blue pressure glow.", primary: "#8bf3ff", secondary: "#3c7dff", accent: "#ffffff", trail: "rgba(139,243,255,0.82)", unlockLevel: 1, rewardOnly: true },
    { id: "ember", label: "Ember Drive", copy: "High-rank inferno trail unlocked at level 8.", primary: "#ff5f1f", secondary: "#ffe600", accent: "#ffb347", trail: "rgba(255,95,31,0.82)", unlockLevel: 8 },
    { id: "void", label: "Void Luxe", copy: "Late-game blacklight trail unlocked at level 10.", primary: "#7b2cff", secondary: "#00f5ff", accent: "#ff4da1", trail: "rgba(123,44,255,0.82)", unlockLevel: 10 },
    { id: "prism", label: "Prism Riot", copy: "Festival glass trail unlocked at level 12.", primary: "#7df9ff", secondary: "#ff4cd7", accent: "#ffe600", trail: "rgba(125,249,255,0.86)", unlockLevel: 12 },
    { id: "glitch", label: "Glitch Voltage", copy: "Scanline glitch arc unlocked at level 14.", primary: "#5bff98", secondary: "#7b2cff", accent: "#ffffff", trail: "rgba(91,255,152,0.84)", unlockLevel: 14 },
    { id: "mustang", label: "Mustang Myth", copy: "Top-rank aura with championship neon.", primary: "#ffe600", secondary: "#00f5ff", accent: "#ff006e", trail: "rgba(255,230,0,0.9)", unlockLevel: 15 }
  ];

  const dailyChallengeTemplates = [
    { id: "perfect_pair", title: "HIT 2 PERFECTS", copy: "Any mode. Land two perfect kicks today.", metric: "perfectGoals", target: 2, modeKey: "any", accumulate: true, rewardSkin: "solar" },
    { id: "heat_streak", title: "MAKE 4 IN A ROW", copy: "Any mode. Build a four-kick streak.", metric: "maxStreak", target: 4, modeKey: "any", accumulate: false, rewardSkin: "solar" },
    { id: "banger_duo", title: "MAKE 2 DEEP SHOTS", copy: "Any mode. Hit two kicks from 50 yards or more.", metric: "longBombGoals", target: 2, modeKey: "any", accumulate: true, rewardSkin: "solar" },
    { id: "grid_drop_12", title: "BREAK 12 BLOCKS", copy: "Any mode. Break twelve goal-grid blocks today.", metric: "gridDrops", target: 12, modeKey: "any", accumulate: true, rewardSkin: "solar" },
    { id: "grid_rows_2", title: "GET 2 COLOR CLEARS", copy: "Any mode. Hit the right block and trigger two color clears.", metric: "gridRowClears", target: 2, modeKey: "any", accumulate: true, rewardSkin: "solar" },
    { id: "score_attack_45", title: "SCORE 45", copy: "Score Attack only. Reach 45 points in one run.", metric: "score", target: 45, modeKey: "scoreAttack", accumulate: false, rewardSkin: "solar" },
    { id: "long_bomb_24", title: "SCORE 24 IN LONG BOMB", copy: "Long Bomb Only. Reach 24 points in one run.", metric: "score", target: 24, modeKey: "longBomb", accumulate: false, rewardSkin: "solar" },
    { id: "sudden_death_four", title: "MAKE 4 IN SUDDEN DEATH", copy: "Sudden Death only. Stay alive long enough to land four kicks.", metric: "goals", target: 4, modeKey: "suddenDeath", accumulate: false, rewardSkin: "solar" }
  ];

  const weeklyChallengeTemplates = [
    { id: "weekly_score_grind", title: "SCORE 120", copy: "All week. Build score with block breaks and color clears.", metric: "score", target: 120, modeKey: "any", accumulate: true, rewardSkin: "glacier", bonusXp: 120 },
    { id: "weekly_banger_pack", title: "MAKE 8 DEEP SHOTS", copy: "All week. Hit eight kicks from 50 yards or more.", metric: "longBombGoals", target: 8, modeKey: "any", accumulate: true, rewardSkin: "glacier", bonusXp: 120 },
    { id: "weekly_grid_crash", title: "BREAK 60 BLOCKS", copy: "All week. Break down the goal stack across your runs.", metric: "gridDrops", target: 60, modeKey: "any", accumulate: true, rewardSkin: "glacier", bonusXp: 140 },
    { id: "weekly_row_burn", title: "GET 8 COLOR CLEARS", copy: "All week. Turn clean hits into color clears.", metric: "gridRowClears", target: 8, modeKey: "any", accumulate: true, rewardSkin: "glacier", bonusXp: 150 },
    { id: "weekly_sudden_heat", title: "WIN 3 SUDDEN DEATH RUNS", copy: "All week. Clear Sudden Death three times with at least 3 goals.", metric: "suddenRuns", target: 3, modeKey: "suddenDeath", accumulate: true, rewardSkin: "glacier", bonusXp: 120 }
  ];

  const communitySeedNames = {
    scoreAttack: ["GRIDFOX", "LASERLIL", "WINDKID", "CYANACE", "ARCBYTE"],
    longBomb: ["BANGERBOY", "PINKSNAP", "DOINKDOC", "POSTGHOST", "HEATCUT"],
    suddenDeath: ["LASTLIFE", "ONEKICK", "NERVEKID", "CLUTCHUP", "GAPKING"]
  };

  return {
    progressionTiers,
    modes,
    skinDefs,
    dailyChallengeTemplates,
    weeklyChallengeTemplates,
    communitySeedNames
  };
});
