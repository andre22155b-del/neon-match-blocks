# Neon FieldGoal Architecture Brief

## Product Direction
- Platform: mobile-web-first
- Core mode: short-session arcade score attack
- Playability: offline-capable by default
- Online layer: server-driven competition and content when available
- Identity: neon arcade sports hybrid with a goal-grid scoring layer

## Pillars
1. Fast, readable, one-thumb-first kicking loop
2. Offline gameplay never blocked by backend availability
3. Server-driven competition, resets, rivals, and content over time
4. Data-driven content so design can tune the game without deep runtime edits
5. Safe incremental modularization instead of a full rewrite

## Current Architecture
- Main runtime: [index.html](/Users/drewtobar/Documents/neon-fieldgoal-web/index.html)
- Backend/local API: [server.js](/Users/drewtobar/Documents/neon-fieldgoal-web/server.js)
- Shared hybrid helper logic: [hybrid-logic.js](/Users/drewtobar/Documents/neon-fieldgoal-web/hybrid-logic.js)
- Static content data: [content-data.js](/Users/drewtobar/Documents/neon-fieldgoal-web/content-data.js)
- Browser smoke coverage: [__smoke_test.html](/Users/drewtobar/Documents/neon-fieldgoal-web/__smoke_test.html)
- Logic tests: [hybrid-logic.test.mjs](/Users/drewtobar/Documents/neon-fieldgoal-web/tests/hybrid-logic.test.mjs)

## Recommended Target Structure
- `runtime`: game loop, rendering, input, animation state
- `gameplay`: kick math, goal resolution, obstacle logic, grid scoring
- `content`: modes, tiers, skins, challenges, rivals, season definitions
- `progression`: XP, unlocks, records, rewards
- `competition`: sync queue, leaderboard shaping, rival state
- `platform`: local storage, network detection, server endpoints, analytics hooks

## Immediate Engineering Moves
1. Keep extracting static data from the main HTML runtime
2. Keep pure gameplay helpers shared between browser runtime and tests
3. Add analytics hooks around session length, misses, quit points, and reward conversions
4. Preserve offline-first gameplay while letting the backend override rivals, challenges, and seasonal flags
5. Delay accounts and economy complexity until retention proves out

## Backend Direction
- v1 backend should own:
  - trusted leaderboard writes
  - featured rivals
  - daily/weekly reset state
  - seasonal flags and active content packs
- v1 backend should not block:
  - local play
  - local progression
  - local records

## Content Direction
- Design/content should own:
  - mode definitions
  - progression tiers
  - challenge templates
  - skin definitions
  - rival seeds / seasonal rotations
- Engineering should provide:
  - content validation
  - admin-safe loading
  - fallback defaults for offline play

## Testing Direction
- Keep browser smoke for menu-to-run validation
- Expand Vitest for pure logic:
  - grid scoring
  - challenge progression
  - leaderboard shaping
  - rival generation
- Add regression checks before every live content update

## Biggest Current Risks
- Too much runtime still lives in one HTML file
- Leaderboard trust still depends on lightweight local/server assumptions
- Content tuning is improving but still tightly coupled to runtime code
- No analytics layer yet for real retention tuning

## Next Best Steps
1. Extract challenge/progression helper logic into shared modules
2. Add a small content validation test pass
3. Add analytics event hooks
4. Decide the first real server-owned seasonal content payload shape
