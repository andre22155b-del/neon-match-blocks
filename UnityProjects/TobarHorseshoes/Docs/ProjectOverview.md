# TobarHorseshoes Project Overview

## 1. Project Summary

This Unity project contains 3 distinct game modules inside one workspace:

- `Neon FieldGoal`
  - A mobile-first neon arcade football kicking game.
  - This is the most advanced and most actively developed system in the repo.
- `Neon Match Blocks`
  - A competitive memory/reflex game with AI support, rarity scoring, and arcade presentation.
- `Tobar Horseshoes`
  - An earlier horseshoes prototype with swipe throwing, scoring, and basic neon feedback.

The project uses:

- Unity 6
- Universal Render Pipeline (URP)
- UGUI
- TextMeshPro
- Unity Input System
- Unity Test Framework

Current build settings include:

- `Assets/Scenes/SampleScene.unity`
- `Assets/Scenes/NeonFieldGoal.unity`

`Assets/Scenes/NeonMatchBlocks.unity` exists but is not currently enabled in build settings.

---

## 2. High-Level File Structure

```text
Assets/
  Scenes/
    SampleScene.unity
    NeonFieldGoal.unity
    NeonMatchBlocks.unity

  Scripts/
    FieldGoal/
    NeonMatchBlocks/
    Scripts/              # legacy horseshoes prototype scripts
    Editor/

  Settings/
    NeonFieldGoalConfig.asset
    URP render pipeline assets
    renderer assets
    volume/profile assets

  NeonFieldGoal/
    Materials/
    Prefabs/

  NeonMatchBlocks/
    Art/
    Materials/
    Prefabs/

  Tests/
    EditMode/Editor/NeonFieldGoalTests.cs

Docs/
  concept art / mockups / PR notes
```

Important folders:

- `Assets/Scripts/FieldGoal`
  - Full Neon FieldGoal runtime.
- `Assets/Scripts/NeonMatchBlocks`
  - Full Neon Match Blocks runtime.
- `Assets/Scripts/Scripts`
  - Horseshoes runtime.
- `Assets/Scripts/Editor`
  - Scene generators and editor setup tools.
- `Assets/Settings`
  - Config assets and render pipeline assets.

---

## 3. Main Scenes

### `Assets/Scenes/NeonFieldGoal.unity`

Main Neon FieldGoal scene.

- Uses the field-goal runtime in `Assets/Scripts/FieldGoal`
- Uses `NeonFieldGoalConfig.asset`
- Can be rebuilt/generated from editor tooling

### `Assets/Scenes/NeonMatchBlocks.unity`

Main Neon Match Blocks scene.

- Uses the match-blocks runtime in `Assets/Scripts/NeonMatchBlocks`
- Can be generated from editor tooling

### `Assets/Scenes/SampleScene.unity`

Legacy/general scene.

- Often associated with the older horseshoes prototype systems in `Assets/Scripts/Scripts`

---

## 4. Main Script Overview

## 4A. Neon FieldGoal Runtime

Folder: `Assets/Scripts/FieldGoal`

### Core orchestration

- `FieldGoalGameManager.cs`
  - Central controller for Neon FieldGoal.
  - Owns the round/game loop, mode selection, round state, spawning, score, persistence, and integration between all gameplay systems.
  - Defines `FieldGoalLoopState`:
    - `Menu`
    - `Aim`
    - `ChargingPower`
    - `Kick`
    - `BallInAir`
    - `Result`
    - `ProgressionUpdate`

- `NeonFieldGoalConfig.cs`
  - Main `ScriptableObject` containing tuning and asset references.
  - Used by nearly every FieldGoal system.

- `NeonFieldGoalUI.cs`
  - Entire UI layer for title screen, HUD, announcements, mode selection, and results.
  - Exposes UI events back to the game manager.

### Input and player movement

- `KickInputController.cs`
  - Handles drag/swipe kick input.
  - Produces:
    - `power`
    - `aim`
    - preview updates
    - release event
  - Includes pointer-locking and release debounce.

- `KickerLaneMover.cs`
  - Handles slow lateral movement of the player/kicker in a fixed lane.
  - Supports mobile hold buttons and keyboard left/right fallback.

- `FieldGoalHoldButton.cs`
  - UI hold button component for left/right movement.
  - Emits pressed/released state changes.

### Ball and kick physics

- `FootballProjectile.cs`
  - Football physics object.
  - Handles:
    - launch
    - kick-release blending
    - custom gravity profile
    - wind acceleration
    - curve torque
    - settle detection
    - out-of-bounds detection
    - defense-block events
    - visual trail state

- `FieldGoalKickMath.cs`
  - Pure math helper for kick behavior.
  - Computes:
    - shaped power
    - perfect-kick weight
    - perfect-kick detection
    - launch direction
    - impulse
    - curve torque
    - initial velocity
    - release curve
    - gravity scale
    - preview trajectory sampling

- `KickAimGuide.cs`
  - Visual trajectory preview.
  - Renders a glowing line and landing marker.
  - Uses the same kick math + wind that the actual projectile uses.

### Goal detection and scoring

- `GoalDetector.cs`
  - Checks whether the football crosses the goal plane front-to-back.
  - Valid goal requires:
    - crossing between uprights
    - above crossbar
  - Also classifies misses:
    - `WideLeft`
    - `WideRight`
    - `Low`
    - combined variants

- `FieldGoalScoring.cs`
  - Rule system for:
    - mode labels
    - round duration by mode
    - clutch logic
    - multipliers
    - yard-line progression
    - long-bomb scoring
    - moving-goal thresholds

- `FieldGoalProgression.cs`
  - Persistent score-attack progression system.
  - Stores:
    - best score
    - best longest kick
    - best streak
    - lifetime stats
    - top local runs
  - Calculates reward tiers and reward bonuses.

### Dynamic difficulty systems

- `FieldGoalWindMath.cs`
  - Converts yard line into wind difficulty and HUD labels.
  - Calculates actual lateral wind acceleration applied to the ball.

- `FieldGoalMovingGoalController.cs`
  - Makes the goal sway left/right once the moving-goal threshold is reached.

- `FieldGoalDefenseController.cs`
  - Controls the abstract neon timing bars that act as blockers.
  - Positions them downfield, changes their active count by distance, and animates vertical bar motion.

- `FieldGoalDefenseHitbox.cs`
  - Small metadata wrapper for individual timing-bar hitboxes.

### Camera and presentation

- `FieldGoalCameraJuiceController.cs`
  - First-person camera system.
  - Handles:
    - idle sway
    - kick shake
    - goal shake
    - clutch pulse
    - dynamic FOV
    - slow motion
    - ball follow camera

- `FieldGoalCameraMath.cs`
  - Pure helper math for:
    - first-person camera placement
    - look rotation
    - pressure-based FOV
    - kick/goal trauma
    - slow-motion values

- `GoalPresentationController.cs`
  - Controls:
    - goal/emissive flashes
    - end-zone glow
    - crowd lights
    - crowd/ambient/announcer mixing
    - presentation state based on streak, multiplier, yard line, wind

- `RefereePresentationController.cs`
  - Controls placeholder referee celebration posing and voice timing.
  - Supports prefab refs and animator controller injection from config.

### Audio identity

- `FieldGoalAudioIdentity.cs`
  - Determines which cue should play based on gameplay context.
  - Cue types:
    - `Perfect`
    - `LongBomb`
    - `Heat`
    - `Clutch`
    - `NearMiss`
    - `StreakBreak`
    - `FinalDrive`
    - `MovingGoal`
  - Also defines cue priority, cooldown, ducking, and mix scales.

- `FieldGoalAudioFactory.cs`
  - Generates fallback procedural audio clips for kicks, goals, misses, crowd, ambient, and cue stings.
  - Used when authored audio assets are missing.

---

## 4B. Neon Match Blocks Runtime

Folder: `Assets/Scripts/NeonMatchBlocks`

### Core loop

- `GameManagerCompetitive.cs`
  - Main controller for the match-blocks game.
  - Handles:
    - level progression
    - board generation
    - reflex mini-game
    - timer
    - player turns
    - AI turns
    - scoring
    - combos
    - winner resolution

- `Block.cs`
  - Logic for one memory block.
  - Handles:
    - reveal/hide animation
    - match state
    - rarity visuals
    - input click
    - pair bonus data

### Supporting managers

- `UIManager.cs`
  - Match-blocks UI singleton.
  - Handles HUD, reflex mini-game, floating text, turn indicators, level result panels.

- `ParticleManager.cs`
  - Global particle spawner singleton.
  - Handles match, wrong, combo, and level-complete effects.

- `SoundManager.cs`
  - Global SFX singleton that looks up sounds by ID.

- `DOTweenFallback.cs`
  - Local fallback for projects where DOTween is not installed.
  - Makes animations resolve instantly instead of breaking gameplay.

---

## 4C. Tobar Horseshoes Runtime

Folder: `Assets/Scripts/Scripts`

### Core loop

- `GameManager.cs`
  - Main controller for the horseshoes prototype.
  - Handles:
    - turn order
    - throw spawning
    - settle wait
    - round scoring
    - score-to-win condition

- `GameConfig.cs`
  - Tuning `ScriptableObject` for distances, throw force, aim assist, spin, settle, and scoring.

- `GameEnums.cs`
  - Shared enums:
    - `AimAssistMode`
    - `PitchDistanceMode`

### Throwing and physics

- `ThrowInput.cs`
  - Swipe/mouse input for power, aim, and spin.

- `ThrowController.cs`
  - Computes launch force and torque using config + aim assist.

- `HorseshoeProjectile.cs`
  - Handles projectile physics state and settle detection.

- `StakeTarget.cs`
  - Detects ringers, leaners, and closest distance.

- `ThrowResult.cs`
  - Lightweight struct for one throw outcome.

### UI and environment helpers

- `TobarUI.cs`
  - Basic UI + fallback IMGUI controls.

- `SurfaceDampingZone.cs`
  - Trigger volume that increases damping on shoes in sand pits.

- `NeonRimController.cs`
  - Emissive pulse/flash controller.

- `NetEnergyNodes.cs`
  - Decorative animated neon nodes.

- `OrbitalRings.cs`
  - Decorative rotating rings with burst effect.

---

## 4D. Editor / Scene Generation Scripts

Folder: `Assets/Scripts/Editor`

- `NeonFieldGoalAutoSetupEditor.cs`
  - Builds the Neon FieldGoal scene, materials, football prefab, UI, goal, defense bars, refs, lighting, post-processing, and config defaults.

- `NeonMatchBlocksAutoSetupEditor.cs`
  - Builds the Neon Match Blocks scene, block prefab, board environment, UI, particles, and managers.

- `NeonMatchBlocksAutoBootstrapEditor.cs`
  - Auto-runs the match-block setup in editor one time.

- `TobarAutoSetupEditor.cs`
  - Builds the prototype horseshoes scene in the current open scene.

---

## 5. System Connections

## 5A. Neon FieldGoal

### Runtime flow

1. `NeonFieldGoalUI` fires mode select
2. `FieldGoalGameManager` starts the round
3. `KickerLaneMover` handles left/right positioning
4. `KickInputController` emits preview and kick release
5. `FieldGoalGameManager` computes kick using:
   - `FieldGoalKickMath`
   - `FieldGoalWindMath`
   - `FieldGoalScoring`
6. `KickAimGuide` previews the same physics used by the actual kick
7. `FootballProjectile` flies with release blending, custom gravity, wind, and curve
8. `GoalDetector` evaluates crossings
9. `FieldGoalGameManager` applies result logic:
   - score
   - streak
   - yard-line progression
   - local progression profile
10. Presentation reacts through:
   - `GoalPresentationController`
   - `RefereePresentationController`
   - `FieldGoalCameraJuiceController`
   - `FieldGoalMovingGoalController`
   - `FieldGoalDefenseController`
11. Round ends, results show, and progression is saved via `PlayerPrefs`

### Primary references owned by `FieldGoalGameManager`

- `config`
- `ui`
- `kickerLaneMover`
- `kickInputController`
- `goalDetector`
- `goalPresentationController`
- `refereePresentationController`
- `kickAimGuide`
- `cameraJuiceController`
- `movingGoalController`
- `defenseController`
- `ballSpawnPoint`
- `footballPrefab`

### Main event wiring

- `KickInputController.KickReleased` -> `FieldGoalGameManager.OnKickReleased`
- `KickInputController.PreviewChanged` -> `FieldGoalGameManager.OnKickPreviewChanged`
- `GoalDetector.GoalScored` -> `FieldGoalGameManager.OnGoalScored`
- `GoalDetector.GoalCrossingMissed` -> `FieldGoalGameManager.OnGoalCrossingMissed`
- `FootballProjectile.Settled` -> manager settle handling
- `FootballProjectile.OutOfBounds` -> manager miss handling
- `FootballProjectile.BlockedByDefense` -> manager blocked-kick handling
- `NeonFieldGoalUI.RestartPressed` -> restart round
- `NeonFieldGoalUI.ModeSelected` -> start selected mode

---

## 5B. Neon Match Blocks

### Runtime flow

1. `GameManagerCompetitive` prepares the level
2. It builds the board from a randomized pair deck
3. Player or AI selects a `Block`
4. `Block.Reveal()` animation finishes
5. `GameManagerCompetitive.EvaluatePair()` checks match/mismatch
6. On match:
   - score added
   - combo grows
   - `ParticleManager` plays FX
   - `SoundManager` plays SFX
   - `UIManager` updates HUD and floating text
7. On mismatch:
   - combo resets
   - blocks hide
   - turn swaps
8. Level ends on time up or board cleared

### Important manager connections

- `Block.OnMouseDown()` -> `GameManagerCompetitive.TrySelectBlock`
- `GameManagerCompetitive` uses:
  - `UIManager.Instance`
  - `SoundManager.Instance`
  - `ParticleManager.Instance`

### AI logic

- Maintains `aiMemory` mapping face IDs to remembered blocks
- Chooses known pairs first when possible
- Accuracy improves by level

---

## 5C. Tobar Horseshoes

### Runtime flow

1. `ThrowInput` captures swipe/mouse input
2. `GameManager` receives power/aim/spin release
3. `ThrowController` computes throw vector and torque
4. `HorseshoeProjectile` is launched and settles
5. `GameManager` asks `StakeTarget` whether it was:
   - ringer
   - leaner
   - closest
6. Score differential is awarded
7. Round loops until someone reaches `scoreToWin`

---

## 6. Important Variables And Game Logic

## 6A. Neon FieldGoal Key Variables

Defined mostly in `NeonFieldGoalConfig.cs` and `NeonFieldGoalConfig.asset`.

### Round and mode

- `roundDurationSeconds`
- `clutchModeRoundDurationSeconds`
- `clutchWindowSeconds`

### Movement

- `laneHalfWidth`
- `moverSpeed`
- `moverAcceleration`
- `moverDeceleration`
- `moverLeanAngle`

### Kick feel

- `minKickImpulse`
- `maxKickImpulse`
- `launchAngleDegrees`
- `maxAimDegrees`
- `aimAssistStrength`
- `kickPowerExponent`

### Perfect kick

- `perfectKickCenter`
- `perfectKickWindow`
- `perfectKickImpulseBonus`

### Football physics

- `kickReleaseDuration`
- `kickReleaseEase`
- `releaseGravityScale`
- `risingGravityScale`
- `fallingGravityScale`
- `settleVelocityThreshold`
- `outOfBoundsDistance`

### Goal dimensions

- `goalDistance`
- `crossbarHeight`
- `uprightInnerHalfWidth`
- `goalPlaneDepthPadding`

### Defense timing bars

- `defenseBaseBlockerCount`
- `defenseMaxBlockerCount`
- `defenseExtraBlockerStartYardLine`
- `defenseMaxBlockerYardLine`
- `defenseLineForwardOffset`
- `defenseBlockerSpacing`
- `defenseBarWidth`
- `defenseBarDepth`
- `defenseBarBaseHeight`
- `defenseBarExtraHeight`
- `defenseBarBaseTravel`
- `defenseBarExtraTravel`
- `defenseBarKickSurge`

### Wind

- `windStartYardLine`
- `windMaxYardLine`
- `windBaseAcceleration`
- `windExtraAcceleration`
- `perfectKickWindResistance`

### Moving goal

- `movingGoalStartYardLine`
- `movingGoalFullChallengeYardLine`
- `movingGoalBaseSideOffset`
- `movingGoalExtraSideOffset`
- `movingGoalBaseSpeed`
- `movingGoalExtraSpeed`

### Yard-line progression

- `startingYardLine`
- `yardsPerGoalStep`
- `worldUnitsPerYard`

### Scoring

- `pointsPerGoal`
- `longBombStartYardLine`
- `longBombPointsPerGoal`
- `perfectKickBonusPoints`
- `clutchBonusPoints`
- `clutchPerfectBonusPoints`
- `makesPerMultiplierStep`
- `maxScoreMultiplier`
- `clutchMinimumMultiplier`

### Camera

- `cameraFirstPersonFov`
- `cameraFirstPersonEyeHeight`
- `cameraFirstPersonBackOffset`
- `cameraFirstPersonLookHeight`
- `cameraKickShake`
- `cameraGoalShake`
- `cameraDistanceFovBoost`
- slow-motion fields

### Audio

- `masterSfxVolume`
- `crowdVolume`
- `ambientVolume`
- `voiceVolume`
- `announcerVolume`
- ducking and cue gap fields
- clip asset slots

### Asset slots

- football visual prefab / mesh / material
- referee prefabs
- referee animator controller
- optional clip assignments

---

## 6B. Neon FieldGoal Current Rule Set

Based on the current code:

- Starts at the `20-yard line`
- Every make moves the next kick back `5 yards`
- Regular made kick = `3 points`
- `50+ yards` = `5 points`
- Misses are forgiving:
  - player stays on same yard line
  - timer continues to drain
- Wind begins on deeper kicks
- Moving goal activates at `50 yards`
- Timing bars act as abstract blockers
- Clutch mode increases multiplier pressure late in the round
- Score Attack progression persists locally

Reward tiers:

- `STREET ROOKIE`
- `HEAT HAND`
- `WIND CUTTER`
- `OVERDRIVE`

Progression rewards affect:

- perfect window size
- wind resistance on perfect kicks
- perfect-kick point bonus

---

## 6C. Neon Match Blocks Key Logic

### Board progression

- `levelGrids` defines board size per level
- board must have an even number of cells
- `totalPairs = cellCount / 2`

### Scoring

- `baseMatchScore`
- rarity bonus:
  - `Rare = 75`
  - `Legendary = 200`
- combo multiplier:
  - `1 + (combo - 1) * comboStepMultiplier`

### AI

- stores known blocks in `aiMemory`
- chooses known pairs when possible
- accuracy scales with level

### End-of-level resolution

- winner by score
- tie-break by max combo

---

## 6D. Tobar Horseshoes Key Logic

### Throw tuning

- `distance25FeetMeters`
- `distance40FeetMeters`
- `minThrowImpulse`
- `maxThrowImpulse`
- `launchAngleDegrees`
- `maxAimDegrees`
- `maxSpinTorque`

### Scoring

- ringers cancel each other
- leaners count if not ringer
- closest point can add bonus
- only point differential is awarded each round
- first to `scoreToWin` wins

---

## 7. Dependencies Between Systems

## 7A. Package Dependencies

From `Packages/manifest.json`:

- `com.unity.render-pipelines.universal`
- `com.unity.ugui`
- `com.unity.inputsystem`
- `com.unity.test-framework`
- `com.unity.timeline`
- `com.unity.visualscripting`

Practical runtime dependencies:

- URP is required for the neon look, emissive materials, and post-processing
- UGUI + TMP are required for runtime UI
- Input System is used by event system/editor setup compatibility

---

## 7B. Neon FieldGoal Dependencies

### Central dependency hub

`FieldGoalGameManager` depends on almost every other FieldGoal runtime component.

### Math/helper dependencies

- `FieldGoalGameManager` -> `FieldGoalKickMath`
- `FieldGoalGameManager` -> `FieldGoalScoring`
- `FieldGoalGameManager` -> `FieldGoalWindMath`
- `FieldGoalGameManager` -> `FieldGoalProgression`
- `FieldGoalCameraJuiceController` -> `FieldGoalCameraMath`
- `GoalPresentationController` -> `FieldGoalAudioIdentity`

### Config dependency

Almost every FieldGoal script reads from `NeonFieldGoalConfig`.

### Persistence dependency

- `FieldGoalGameManager` -> `PlayerPrefs`
- `FieldGoalGameManager` -> `JsonUtility`

---

## 7C. Neon Match Blocks Dependencies

- `GameManagerCompetitive` depends on:
  - `Block`
  - `UIManager.Instance`
  - `SoundManager.Instance`
  - `ParticleManager.Instance`
- `Block` depends on DOTween-style animation APIs
- If DOTween is not installed, `DOTweenFallback.cs` keeps logic from breaking

This module is more singleton-driven than Neon FieldGoal.

---

## 7D. Tobar Horseshoes Dependencies

- `GameManager` depends on:
  - `GameConfig`
  - `ThrowInput`
  - `ThrowController`
  - `HorseshoeProjectile`
  - `StakeTarget`
  - `TobarUI`

This is a simpler direct-reference architecture.

---

## 8. Testing And Validation

Tests currently present:

- `Assets/Tests/EditMode/Editor/NeonFieldGoalTests.cs`

Coverage focuses on deterministic FieldGoal logic:

- goal crossing validity
- miss classification
- single-score guard on football
- fallback audio availability
- audio cue priority
- camera math
- kick math
- progression logic
- wind logic

There is no comparable automated suite visible for Match Blocks or Horseshoes.

---

## 9. Assets And Generated Content

## 9A. Neon FieldGoal Assets

Folder: `Assets/NeonFieldGoal`

- `Materials/`
  - neon field, goal, trajectory, football, referee, accent materials
- `Prefabs/FootballProjectile.prefab`

These are often created/maintained by `NeonFieldGoalAutoSetupEditor.cs`.

## 9B. Neon Match Blocks Assets

Folder: `Assets/NeonMatchBlocks`

- `Art/`
  - sports face images
- `Materials/`
  - neon board/cube/floor materials
- `Prefabs/`
  - block prefab
  - particle prefabs

These are often created/maintained by `NeonMatchBlocksAutoSetupEditor.cs`.

---

## 10. Important Architecture Notes For Another AI

- This repo is not a single game. It is a shared Unity workspace containing multiple prototypes/games.
- `Assets/Scripts/Scripts` is the legacy horseshoes runtime folder and has a confusing generic name.
- `Neon FieldGoal` is the primary/current feature set and has the deepest architecture.
- Scene generation is heavily editor-driven. Do not assume scenes were hand-maintained.
- `NeonMatchBlocks` uses singleton manager patterns.
- `NeonFieldGoal` is more modular and data-driven, with several pure helper classes.
- `NeonFieldGoal` uses both runtime self-healing and editor scene generation:
  - runtime code can create missing camera/defense/guide pieces
  - editor tooling can rebuild the full scene
- `DOTweenFallback.cs` exists specifically so the match-blocks module still works if DOTween is absent.

---

## 11. Recommended Review Focus Areas

If another AI is reviewing this project, the most valuable focus areas are:

- Neon FieldGoal:
  - input responsiveness
  - trajectory preview vs actual physics consistency
  - camera readability in first-person/mobile
  - score/persistence correctness
  - timing-bar and moving-goal readability
  - performance/allocation hotspots in runtime-generated visuals and audio

- Neon Match Blocks:
  - turn flow correctness
  - AI fairness and memory behavior
  - singleton coupling
  - board generation and scaling

- Tobar Horseshoes:
  - scoring correctness
  - throw feel
  - simpler modernization opportunities

---

## 12. Key Entry Points

Start here for each module:

- Neon FieldGoal:
  - `Assets/Scripts/FieldGoal/FieldGoalGameManager.cs`
  - `Assets/Scripts/FieldGoal/NeonFieldGoalConfig.cs`
  - `Assets/Scripts/Editor/NeonFieldGoalAutoSetupEditor.cs`

- Neon Match Blocks:
  - `Assets/Scripts/NeonMatchBlocks/GameManagerCompetitive.cs`
  - `Assets/Scripts/NeonMatchBlocks/Block.cs`
  - `Assets/Scripts/Editor/NeonMatchBlocksAutoSetupEditor.cs`

- Tobar Horseshoes:
  - `Assets/Scripts/Scripts/GameManager.cs`
  - `Assets/Scripts/Scripts/GameConfig.cs`
  - `Assets/Scripts/Editor/TobarAutoSetupEditor.cs`

---

## 13. Quick Summary

This project is a multi-game Unity workspace with:

- a sophisticated arcade football game (`Neon FieldGoal`)
- a competitive memory game (`Neon Match Blocks`)
- an older horseshoes prototype (`Tobar Horseshoes`)

The strongest engineering structure currently exists in `Neon FieldGoal`, where:

- the game manager orchestrates the round
- config is centralized in a `ScriptableObject`
- kick, scoring, wind, camera, progression, and audio identity each have dedicated helper systems
- editor tooling can regenerate the scene and default assets

For any future review or refactor, `Neon FieldGoal` should be treated as the primary product and the other two modules as secondary/legacy systems unless otherwise specified.
