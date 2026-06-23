# TobarHorseshoes AI Review Packet

## Purpose

This is the short handoff version of the full project overview.

Use it when another AI needs to:

- understand the repo quickly
- identify the main gameplay systems
- review architecture and dependencies
- focus on the highest-value code paths first

For the full version, see:

- [ProjectOverview.md](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Docs/ProjectOverview.md)

---

## 1. Repo At A Glance

This Unity 6 workspace contains 3 game modules:

1. `Neon FieldGoal`
   - Main active game
   - Mobile-first neon arcade football kicking game
   - Most advanced architecture in the repo
2. `Neon Match Blocks`
   - Competitive memory/reflex game with AI support
3. `Tobar Horseshoes`
   - Older horseshoes prototype

Current build settings include:

- `Assets/Scenes/SampleScene.unity`
- `Assets/Scenes/NeonFieldGoal.unity`

`Assets/Scenes/NeonMatchBlocks.unity` exists but is not currently enabled in build settings.

---

## 2. Main Folders

```text
Assets/
  Scenes/
    SampleScene.unity
    NeonFieldGoal.unity
    NeonMatchBlocks.unity

  Scripts/
    FieldGoal/         # main active game runtime
    NeonMatchBlocks/   # memory/reflex runtime
    Scripts/           # horseshoes runtime
    Editor/            # scene generators / setup tools

  Settings/
    NeonFieldGoalConfig.asset
    URP assets / renderer assets / volume assets

  Tests/
    EditMode/Editor/NeonFieldGoalTests.cs

Docs/
  ProjectOverview.md
  ProjectReviewPacket.md
```

Most important runtime folder:

- `Assets/Scripts/FieldGoal`

Most important editor/tooling folder:

- `Assets/Scripts/Editor`

---

## 3. Highest-Priority Entry Points

If another AI only reads a few files first, start here:

### Neon FieldGoal

- [FieldGoalGameManager.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/FieldGoal/FieldGoalGameManager.cs)
- [NeonFieldGoalConfig.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/FieldGoal/NeonFieldGoalConfig.cs)
- [NeonFieldGoalUI.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/FieldGoal/NeonFieldGoalUI.cs)
- [FootballProjectile.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/FieldGoal/FootballProjectile.cs)
- [FieldGoalKickMath.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/FieldGoal/FieldGoalKickMath.cs)
- [FieldGoalScoring.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/FieldGoal/FieldGoalScoring.cs)
- [GoalDetector.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/FieldGoal/GoalDetector.cs)
- [NeonFieldGoalAutoSetupEditor.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Editor/NeonFieldGoalAutoSetupEditor.cs)

### Neon Match Blocks

- [GameManagerCompetitive.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/NeonMatchBlocks/GameManagerCompetitive.cs)
- [Block.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/NeonMatchBlocks/Block.cs)
- [UIManager.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/NeonMatchBlocks/UIManager.cs)

### Tobar Horseshoes

- [GameManager.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Scripts/GameManager.cs)
- [GameConfig.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Scripts/GameConfig.cs)
- [ThrowInput.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Scripts/ThrowInput.cs)
- [ThrowController.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Scripts/ThrowController.cs)

---

## 4. Neon FieldGoal System Summary

`Neon FieldGoal` is the main game under active development.

Core fantasy:

- first-person neon arcade field-goal kicking
- starts at 20 yards
- every make moves the next kick back 5 yards
- `3 points` for normal makes
- `5 points` once kick distance reaches `50+ yards`
- misses are forgiving and fast
- moving goal and wind add difficulty at deeper distances
- local progression and score-attack style replayability are already wired in

### Core Runtime Roles

- `FieldGoalGameManager`
  - owns the main game loop and system integration
- `NeonFieldGoalConfig`
  - shared tuning and asset source
- `NeonFieldGoalUI`
  - title screen, HUD, announcements, results
- `KickInputController`
  - swipe/power/aim input
- `KickerLaneMover`
  - slow left/right movement
- `FootballProjectile`
  - actual kick physics object
- `FieldGoalKickMath`
  - pure kick logic
- `FieldGoalScoring`
  - pure score and progression rules
- `GoalDetector`
  - validates real makes vs misses
- `FieldGoalWindMath`
  - wind scaling and labels
- `FieldGoalDefenseController`
  - abstract neon timing bars that can block kicks
- `FieldGoalMovingGoalController`
  - side-to-side moving uprights at longer range
- `FieldGoalCameraJuiceController`
  - first-person camera sway, punch, follow, slow-mo
- `GoalPresentationController`
  - visual/audio feedback
- `FieldGoalProgression`
  - persistent score attack profile and reward bonuses

---

## 5. Neon FieldGoal System Connections

### Main gameplay flow

1. `NeonFieldGoalUI` starts a mode.
2. `FieldGoalGameManager` initializes round state.
3. `KickInputController` sends live preview data.
4. `FieldGoalGameManager` routes preview into `KickAimGuide`.
5. `FieldGoalGameManager` converts release input into kick values using:
   - `FieldGoalKickMath`
   - `FieldGoalWindMath`
   - `FieldGoalScoring`
   - progression reward modifiers
6. `FootballProjectile` launches and simulates the kick.
7. `GoalDetector` decides make or miss.
8. `FieldGoalGameManager` updates:
   - score
   - streak
   - yard line
   - multiplier
   - longest kick
   - progression/profile data
9. Presentation systems react:
   - `GoalPresentationController`
   - `FieldGoalCameraJuiceController`
   - `RefereePresentationController`
   - `NeonFieldGoalUI`
10. Round ends, results show, profile persists.

### Key dependency pattern

`FieldGoalGameManager` is the hub.

Most FieldGoal subsystems are not independent. They connect through:

- shared config via `NeonFieldGoalConfig`
- orchestration via `FieldGoalGameManager`
- event-style callbacks between projectile, detector, UI, and presentation systems

---

## 6. Neon FieldGoal Key Variables And Rules

Primary tuning asset:

- [NeonFieldGoalConfig.asset](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Settings/NeonFieldGoalConfig.asset)

Important config categories:

- round duration and clutch timing
- lane movement speed and width
- kick impulse, angle, aim limits, power shaping
- perfect-kick window and bonuses
- custom gravity and release feel
- goal dimensions
- wind start and max challenge
- moving-goal thresholds and speed
- defense/timing-bar spacing and motion
- camera shake, sway, FOV, follow timing
- audio layering and clip slots
- scoring and multiplier rules

Current important rule set:

- start at `20 yards`
- move back `5 yards` after each make
- standard make = `3 points`
- `50+ yards` = `5 points`
- misses keep you on the same line
- wind gets stronger with distance
- moving goal activates at deeper range
- game supports score-attack style progression and local records

Important state in `FieldGoalGameManager`:

- current mode
- loop state
- score
- goals
- streak
- multiplier
- longest made kick
- current yard line
- current wind vector / strength
- active projectile reference
- persistent progression profile

---

## 7. Neon FieldGoal Review Priorities

If another AI is reviewing this system, highest-value questions are:

1. Does the kick preview match live kick physics?
2. Does the first-person camera improve aim clarity or interfere with it?
3. Is `FieldGoalGameManager` getting too large and coupling too many responsibilities?
4. Are the timing bars readable and fair on mobile-sized screens?
5. Are score, progression, and reward modifiers easy to reason about?
6. Does the scene-generator/editor setup create hidden maintenance risk?
7. Are there performance issues from runtime-created materials, FX, or UI?
8. Are persistence and save defaults robust if data changes later?

Strongest review targets:

- player control responsiveness
- scoring clarity
- camera-to-gameplay readability
- data ownership between config, pure math, and manager logic

---

## 8. Neon Match Blocks Summary

This is a separate competitive arcade memory game.

Main files:

- [GameManagerCompetitive.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/NeonMatchBlocks/GameManagerCompetitive.cs)
- [Block.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/NeonMatchBlocks/Block.cs)
- [UIManager.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/NeonMatchBlocks/UIManager.cs)
- [SoundManager.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/NeonMatchBlocks/SoundManager.cs)
- [ParticleManager.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/NeonMatchBlocks/ParticleManager.cs)

Core loop:

- generate board
- reveal blocks
- evaluate pair
- score combo/rarity
- handle player vs AI turns
- advance levels

Important traits:

- uses singleton-style UI/audio/VFX managers
- supports rarity-based scoring
- AI remembers seen faces
- includes `DOTweenFallback` so it can run without DOTween installed

This module is less connected to the newer FieldGoal architecture.

---

## 9. Tobar Horseshoes Summary

This is an older, more prototype-style system.

Main files:

- [GameManager.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Scripts/GameManager.cs)
- [GameConfig.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Scripts/GameConfig.cs)
- [ThrowInput.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Scripts/ThrowInput.cs)
- [ThrowController.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Scripts/ThrowController.cs)
- [HorseshoeProjectile.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Scripts/HorseshoeProjectile.cs)
- [StakeTarget.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Scripts/StakeTarget.cs)
- [TobarUI.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Scripts/TobarUI.cs)

Core loop:

- player swipe input
- throw computation
- projectile flight
- target evaluation
- round scoring
- win condition

Important traits:

- more direct scene-wired architecture
- less modular than FieldGoal
- heavily config-driven through `GameConfig`

---

## 10. Editor / Generated Scene Dependency

One important architecture note:

Large parts of the project are scene-generated.

Important editor scripts:

- [NeonFieldGoalAutoSetupEditor.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Editor/NeonFieldGoalAutoSetupEditor.cs)
- [NeonMatchBlocksAutoSetupEditor.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Editor/NeonMatchBlocksAutoSetupEditor.cs)
- [TobarAutoSetupEditor.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Scripts/Editor/TobarAutoSetupEditor.cs)

This means another AI should not assume:

- all scene references were hand-authored
- current scene content is the single source of truth

For major scene questions, review both:

- runtime scripts
- auto-setup editor scripts

---

## 11. Package / Engine Dependencies

Important packages from `Packages/manifest.json`:

- URP
- UGUI
- Unity Input System
- TextMeshPro
- Unity Test Framework

Soft dependency:

- DOTween-style behavior in Match Blocks, protected by `DOTweenFallback.cs`

Persistence dependencies:

- `PlayerPrefs`
- `JsonUtility`

---

## 12. Test Coverage

Existing automated tests are mainly for FieldGoal:

- [NeonFieldGoalTests.cs](/Users/drewtobar/Documents/UnityProjects/TobarHorseshoes/Assets/Tests/EditMode/Editor/NeonFieldGoalTests.cs)

The FieldGoal tests focus mostly on deterministic logic, such as:

- kick math
- scoring
- wind behavior
- camera helper math
- progression rules
- miss / goal logic

There is much less automated coverage for:

- scene wiring
- UI behavior
- play-mode interaction
- Match Blocks
- Horseshoes

---

## 13. Known Architecture Notes

- `FieldGoalGameManager` is the central hub and likely the first place to watch for future size/coupling issues.
- `NeonFieldGoalConfig` is a strong data source and a major dependency point across the FieldGoal module.
- `Assets/Scripts/Scripts` is the horseshoes runtime folder despite the generic name.
- Scene auto-setup scripts are important to understanding what the runtime expects.
- FieldGoal is the cleanest system to review for modern structure.
- Match Blocks relies more on singleton access patterns.
- Horseshoes is the most prototype-like of the three.

---

## 14. Best Prompt For Another AI Reviewer

Use this if you want another AI to review the codebase quickly:

```text
Review this Unity project as a senior gameplay engineer.

Focus first on the Neon FieldGoal module, then note any important architecture patterns in Neon Match Blocks and Tobar Horseshoes.

Please analyze:
- main runtime systems
- how input, camera, projectile physics, scoring, UI, and persistence connect
- whether the FieldGoalGameManager is too coupled
- whether kick preview and live ball physics are likely to stay aligned
- whether data ownership between config, pure helper classes, and scene MonoBehaviours is clear
- whether scene auto-generation introduces maintainability risk
- any likely gameplay-feel, readability, or mobile usability risks

Start with findings, not compliments.
Prioritize real bugs, structural risks, and weak coupling points.
```

---

## 15. Short Summary

If another AI only needs the short version:

- this repo has 3 Unity game modules
- `Neon FieldGoal` is the main active game
- `FieldGoalGameManager` is the central integration point
- `NeonFieldGoalConfig` is the main tuning dependency
- `NeonFieldGoalAutoSetupEditor` matters because scene setup is partly generated
- biggest review targets are gameplay feel, camera clarity, manager coupling, scoring readability, and mobile usability
