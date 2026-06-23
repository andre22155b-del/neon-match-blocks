# TobarHorseshoes AI Brief

## What This Repo Is

Unity 6 workspace with 3 game modules:

1. `Neon FieldGoal`
   - Main active game
   - First-person, mobile-first neon arcade football kicking game
2. `Neon Match Blocks`
   - Competitive memory/reflex game with AI
3. `Tobar Horseshoes`
   - Older horseshoes prototype

Current build settings include:

- `Assets/Scenes/SampleScene.unity`
- `Assets/Scenes/NeonFieldGoal.unity`

---

## What To Review First

Primary review target: `Neon FieldGoal`

Start here:

- [FieldGoalGameManager.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/FieldGoal/FieldGoalGameManager.cs)
- [NeonFieldGoalConfig.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/FieldGoal/NeonFieldGoalConfig.cs)
- [NeonFieldGoalUI.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/FieldGoal/NeonFieldGoalUI.cs)
- [FootballProjectile.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/FieldGoal/FootballProjectile.cs)
- [FieldGoalKickMath.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/FieldGoal/FieldGoalKickMath.cs)
- [FieldGoalScoring.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/FieldGoal/FieldGoalScoring.cs)
- [GoalDetector.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/FieldGoal/GoalDetector.cs)
- [NeonFieldGoalAutoSetupEditor.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Editor/NeonFieldGoalAutoSetupEditor.cs)

---

## Neon FieldGoal In One Minute

Core loop:

`aim -> power -> kick -> ball flight -> make/miss -> score/progression -> next kick`

Current rules:

- start at `20 yards`
- every make moves the next kick back `5 yards`
- normal make = `3 points`
- `50+ yards` = `5 points`
- misses are forgiving and keep you on the same yard line
- wind increases with distance
- moving goal activates at longer range
- abstract neon timing bars can block kicks
- score-attack progression and local records are saved

Target feel:

- arcade score attack
- mobile-first usability
- Gen Z neon / party-game energy
- forgiving but addictive

---

## Main System Roles

- `FieldGoalGameManager`
  - central hub for mode flow, round state, scoring, spawning, persistence, and subsystem wiring
- `NeonFieldGoalConfig`
  - main tuning and asset dependency
- `KickInputController`
  - swipe input and release data
- `KickerLaneMover`
  - left/right player movement
- `FootballProjectile`
  - live projectile physics
- `FieldGoalKickMath`
  - pure kick calculations
- `FieldGoalScoring`
  - pure scoring, yard-line, and multiplier rules
- `GoalDetector`
  - validates real makes and classifies misses
- `FieldGoalWindMath`
  - wind scaling and labels
- `FieldGoalDefenseController`
  - timing-bar blockers
- `FieldGoalMovingGoalController`
  - side-to-side moving uprights
- `FieldGoalCameraJuiceController`
  - first-person camera sway, shake, ball follow
- `GoalPresentationController`
  - FX/audio/glow feedback
- `NeonFieldGoalUI`
  - title, HUD, announcements, results
- `FieldGoalProgression`
  - persistent score-attack profile and reward bonuses

---

## How The FieldGoal Systems Connect

1. UI starts a mode through `FieldGoalGameManager`.
2. Input feeds preview data into the manager.
3. The manager updates the aim guide using kick math + wind.
4. On release, the manager converts input into launch values.
5. `FootballProjectile` simulates the kick.
6. `GoalDetector` reports make or miss.
7. The manager updates score, streak, yard line, progression, and UI.
8. Camera, presentation, refs, moving goal, and blockers react around that result.

Important dependency pattern:

- `FieldGoalGameManager` is the orchestration hub.
- `NeonFieldGoalConfig` is the shared data source.
- Scene generation matters because the scene is partly editor-built, not only hand-authored.

---

## Biggest Review Questions

1. Is `FieldGoalGameManager` too large and too coupled?
2. Does trajectory preview actually match live ball physics?
3. Is the first-person camera helping aim clarity on mobile?
4. Are timing bars readable and fair instead of cluttering the view?
5. Are score, progression, and reward bonuses easy to reason about?
6. Does scene auto-generation create maintenance risk?
7. Are there performance risks from runtime-created visuals and UI?

---

## Other Modules

`Neon Match Blocks`

- Competitive memory/reflex game
- Main files:
  - [GameManagerCompetitive.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/NeonMatchBlocks/GameManagerCompetitive.cs)
  - [Block.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/NeonMatchBlocks/Block.cs)
  - [UIManager.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/NeonMatchBlocks/UIManager.cs)

`Tobar Horseshoes`

- Older horseshoes prototype
- Main files:
  - [GameManager.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Scripts/GameManager.cs)
  - [GameConfig.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Scripts/GameConfig.cs)
  - [ThrowInput.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Scripts/ThrowInput.cs)

---

## Useful Supporting Docs

- Full overview: [ProjectOverview.md](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Docs/ProjectOverview.md)
- Condensed review packet: [ProjectReviewPacket.md](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Docs/ProjectReviewPacket.md)

---

## Ready-To-Paste Reviewer Prompt

```text
Review this Unity project as a senior gameplay engineer.

Focus first on the Neon FieldGoal module.

Please analyze:
- architecture and system boundaries
- input, aim preview, kick physics, scoring, UI, and persistence flow
- whether FieldGoalGameManager is overly coupled
- whether kick preview and real projectile behavior are likely to stay aligned
- whether the first-person camera and timing bars support or hurt mobile usability
- whether scene auto-generation creates maintenance or debugging risk
- any real gameplay-feel, clarity, performance, or data-ownership problems

Start with findings, not compliments.
Prioritize bugs, structural risks, and weak gameplay-readability points.
```
