# Tobar Horseshoes Prototype Setup (Unity 6.3 LTS)

This guide finishes the prototype in your existing project.

## Fast path (recommended)
1. Open the scene you want to use.
2. Run `Tools > Tobar Horseshoes > Auto Setup Prototype Scene`.
3. Press Play and test throws immediately.

## Scripts already added
- `Assets/Scripts/Scripts/GameManager.cs`
- `Assets/Scripts/Scripts/GameConfig.cs`
- `Assets/Scripts/Scripts/GameEnums.cs`
- `Assets/Scripts/Scripts/ThrowInput.cs`
- `Assets/Scripts/Scripts/ThrowController.cs`
- `Assets/Scripts/Scripts/HorseshoeProjectile.cs`
- `Assets/Scripts/Scripts/StakeTarget.cs`
- `Assets/Scripts/Scripts/SurfaceDampingZone.cs`
- `Assets/Scripts/Scripts/TobarUI.cs`
- `Assets/Scripts/Scripts/ThrowResult.cs`

## Prefab list
- `HorseshoePrefab`
- `StakePrefab`
- `SandPitPrefab` (box trigger + `SurfaceDampingZone`)
- `GameRoot` (manager object, not a mesh prefab)

## Scene build steps
1. Open `Assets/Scenes/SampleScene.unity`.
2. Create the yard:
- Ground plane with grass material.
- Wooden fence, porch, lawn chairs, grill model.
- Add particle system at grill for subtle smoke.
3. Lighting and ambience:
- Directional Light: warm color, late-afternoon angle (X around 35-50).
- Add AudioSource in scene with looping cicada ambience.
4. Stakes and pits:
- Create `NearStake` (cylinder). Tag it `Stake`.
- Duplicate for `FarStake` and tag it `Stake`.
- Place one sand pit under each stake (box/cylinder with trigger collider).
- Add `SurfaceDampingZone` to each pit trigger.
5. Horseshoe prefab:
- Use torus/mesh as shoe.
- Add `Rigidbody` and `HorseshoeProjectile`.
- Add `AudioSource` + assign metal ping clip.
- Save as `HorseshoePrefab`.
6. Target evaluator:
- Add `StakeTarget` to an empty object named `TargetStake`.
- Set `stakeTransform` to `FarStake`.
- Optionally create a child object `RingerZoneCenter` at stake center and assign to `zoneCenter`.
7. Throw origin:
- Create empty object `ThrowOrigin` near the near stake, facing the far stake.
8. Manager object:
- Create empty object `GameRoot`.
- Add components: `GameManager`, `ThrowInput`, `ThrowController`.
- Assign references on `GameManager`:
  - `config`: GameConfig asset
  - `throwInput`: component on GameRoot
  - `throwController`: component on GameRoot
  - `ui`: UI root object with `TobarUI`
  - `targetStake`: TargetStake object
  - `nearStake`/`farStake`: stake transforms
  - `throwOrigin`: ThrowOrigin
  - `horseshoePrefab`: HorseshoePrefab
  - `spawnedShoeParent`: optional empty `SpawnedShoes`
- Assign references on `ThrowController`:
  - `config`, `throwOrigin`, `targetStake` (FarStake transform)
9. Create `GameConfig` asset:
- Right click Project > Create > Tobar Horseshoes > Game Config.
- Keep defaults, or tune throw force and aim assist.

## UI setup
1. Create Canvas and EventSystem.
2. Add UI elements:
- Slider: `PowerMeter`
- Text: `ScoreText`
- Text: `TurnText`
- Text: `RoundLogText`
- Text: `GameOverText`
- Dropdown: `AimAssistDropdown` with options `Off`, `Low`, `High`
- Dropdown: `DistanceDropdown` with options `25 ft`, `40 ft`
- Optional Image/RectTransform: `AimReticle`
3. Add `TobarUI` component to a `UIRoot` object and wire fields.
4. Assign `UIRoot` to `GameManager.ui`.

## Gameplay rules implemented
- Two players, two shoes each round (4 total throws).
- Distance toggle: 25 ft / 40 ft.
- Swipe drag controls power and aim.
- Left/right drag adds spin torque.
- Ringer detection: cylinder zone + orientation check after shoe is still for 0.25s.
- Scoring:
- Ringer = 3
- Leaner = 1
- Closest within 6 inches = 1
- Equal ringers are canceled per round.
- Only one side scores per round (difference scoring).
- First to 21 wins.

## Run in Editor
1. Press Play.
2. Click/touch drag and release to throw.
3. Watch score, round log, and winner text update.

## Build iOS
1. File > Build Settings > iOS > Switch Platform.
2. Open Player Settings and set:
- Bundle Identifier
- Version
- Target iOS version
3. Build to an empty folder.
4. Open generated Xcode project.
5. In Xcode, set Team signing and run on device.

## Build Android
1. File > Build Settings > Android > Switch Platform.
2. Player Settings:
- Package Name
- Minimum API level
- IL2CPP + ARM64 for Play Store builds
3. Build APK/AAB.
4. Install on device or upload AAB.
