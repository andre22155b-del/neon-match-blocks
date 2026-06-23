export const DEFAULT_GAME_SETUP = {
  shotDifficulty: "easy",
  aimMode: "casual",
  dominantHand: "right",
  controlProfile: "rookie",
  physicsProfile: "arcade",
  ballSkin: "classic",
  playerBallColor: "purple",
  opponentBallColor: "cyan",
};

export const GAME_SETUP_OPTIONS = {
  shotDifficulty: ["easy", "medium", "hard"],
  aimMode: ["casual", "pro"],
  dominantHand: ["right", "left"],
  controlProfile: ["rookie", "smooth", "balanced", "precision"],
  physicsProfile: ["arcade", "balanced", "sim"],
  ballSkin: ["classic", "plasma", "prism"],
  playerBallColor: ["orange", "cyan", "purple", "lime", "pink"],
  opponentBallColor: ["orange", "cyan", "purple", "lime", "pink"],
};
const PHYSICS_PROFILE_ALIASES = Object.freeze({
  realistic: "balanced",
  park: "sim",
});

const sanitizeField = (key, value) => {
  const allowed = GAME_SETUP_OPTIONS[key] || [];
  const fallback = DEFAULT_GAME_SETUP[key];
  const nextValue =
    key === "physicsProfile" ? PHYSICS_PROFILE_ALIASES[value] || value : value;
  return allowed.includes(nextValue) ? nextValue : fallback;
};

export const sanitizeGameSetup = (input = {}) => {
  const sanitized = {};
  const issues = [];

  for (const key of Object.keys(DEFAULT_GAME_SETUP)) {
    const next = sanitizeField(key, input[key]);
    sanitized[key] = next;
    if (next !== input[key]) {
      issues.push({
        key,
        received: input[key],
        applied: next,
      });
    }
  }

  return {
    ...sanitized,
    changed: issues.length > 0,
    issues,
  };
};
