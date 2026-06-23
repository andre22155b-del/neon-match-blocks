# Neon Connect Words Setup Guide

## 1. Create the Unity project

1. Open Unity Hub and create a new `3D (URP)` project in Unity `2022.3 LTS` or newer.
2. Copy this template folder's `Assets/Scripts` and `Assets/Data` into your Unity project's `Assets` folder.
3. Import `TextMeshPro` when Unity prompts for TMP essentials.

## 2. Scene hierarchy

Create one scene named `MainGame` with this structure:

```text
MainGame
|- Managers
|  |- GameManager
|  |- BoardManager
|  |- WordChecker
|  |- AudioManager
|  |- UIManager
|  |- PowerUpManager
|  |- TutorialManager
|  |- PuzzleManager
|  |- ParticleManager
|- Main Camera
|- BoardOrigin
|- ColumnColliders
|  |- Column_0
|  |- Column_1
|  |- Column_2
|  |- Column_3
|  |- Column_4
|  |- Column_5
|  |- Column_6
|- Canvas
|  |- MainMenuPanel
|  |- ModeSelectPanel
|  |- HUDPanel
|  |- TutorialPanel
|  |- GameOverPanel
|  |- FloatingScoreCanvas
|- Directional Light
|- BoardGlowLight
```

## 3. Board setup

1. Place `BoardOrigin` at `(0, 0, 0)`.
2. Add 7 narrow box colliders under `ColumnColliders`.
3. Space them by the same value as `BoardManager.cellSize` (`1.15` by default).
4. Put those colliders on a layer like `BoardInput` and assign that layer to `BoardManager.boardInputMask`.
5. Position the camera around `(0, 4.5, -9.5)` with rotation `(18, 0, 0)`.

## 4. Letter prefab

Create a prefab called `LetterTile`:

```text
LetterTile
|- VisualRoot
|  |- CubeMesh
|  |- GlowMesh
|- LetterLabel (TextMeshPro 3D)
|- TrailRenderer
|- MatchParticles
|- ClearParticles
```

Attach `LetterTile.cs` to the root and wire:

- `visualRoot` -> `VisualRoot`
- `letterLabel` -> `LetterLabel`
- `tintRenderers` -> the mesh renderers you want tinted
- `trailRenderer` -> optional
- `matchParticles` / `clearParticles` -> optional

Use emissive URP materials:

- Player 1: cyan emission
- Player 2: magenta emission
- Wildcard: white emission

## 5. Manager component wiring

### GameManager

- Add `GameManager.cs`
- Assign `boardManager`, `uiManager`, `audioManager`, `powerUpManager`, `tutorialManager`, `puzzleManager`
- Keep `playerCount = 2`
- Tune `classicTargetScore` and `timedDuration` as needed

### BoardManager

- Add `BoardManager.cs`
- Assign `boardOrigin`
- Assign `letterTilePrefab`
- Assign `wordChecker`, `uiManager`, `audioManager`, `powerUpManager`
- Assign two player materials to `playerMaterials`
- Assign the `BoardInput` layer mask

### WordChecker

- Add `WordChecker.cs`
- Assign `NeonDictionary.txt` from `Assets/Data`
- Keep `minWordLength = 3`

### AudioManager

- Add `AudioManager.cs`
- Create three audio sources:
  - `MusicSource`
  - `SfxSource`
  - `UiSource`
- Assign placeholder clips for drop, word, combo, cascade, wildcard, bomb, swap, and UI click

### PowerUpManager

- Add `PowerUpManager.cs`
- Assign `uiManager` and `audioManager`
- Leave default unlock turns or tune them

### TutorialManager

- Add `TutorialManager.cs`
- Assign `boardManager`, `uiManager`, `powerUpManager`
- Optional: create 7 glow objects and assign them to `columnHighlights`

### PuzzleManager

- Add `PuzzleManager.cs`
- Assign `boardManager` and `uiManager`
- Optional: author puzzle entries directly in the inspector

### ParticleManager

- Add `ParticleManager` from `CameraAndParticles.cs`
- Assign optional prefabs for:
  - drop impact
  - word burst
  - combo burst
  - bomb burst
- Assign `BoardGlowLight`

### CameraController

- Add `CameraController` from `CameraAndParticles.cs` to `Main Camera`

## 6. Canvas and HUD

Recommended HUD elements:

- `ModeText`
- `TurnText`
- `CurrentLetterText`
- `ComboText`
- `ComboSlider`
- `TimerGroup` with `TimerText` and a filled `Image`
- `PuzzleObjectiveText`
- Power-up buttons with count texts
- `MessageText` with `CanvasGroup`
- `GameOverTitleText`
- `GameOverScoresText`
- `TutorialTitleText`
- `TutorialBodyText`
- `TutorialNextButton`
- `TutorialSkipButton`

Attach `UIManager.cs` to a `UIManager` object and wire each field. Missing optional UI fields are handled safely, so you can start minimal and add polish later.

## 7. Mobile performance notes

For low-end devices:

- Enable `BoardManager.reducedAnimationMode`
- Enable `ParticleManager.reducedFxMode`
- Use unlit emissive materials instead of expensive post-processing bloom
- Keep particle systems short-lived and under 60 particles
- Avoid rigidbodies on tiles; this template already uses lightweight transform animation

## 8. Recommended scene polish

- Add a dark gradient board backdrop behind the grid
- Use bloom sparingly if targeting desktop only
- Use one point light (`BoardGlowLight`) and one directional light
- Add a subtle synthwave skybox or static quad background

## 9. How to test

1. Press Play.
2. Click `Play`.
3. Choose `Classic`, `Timed`, `Puzzle`, or `Tutorial`.
4. Hover over a column to preview the drop.
5. Click a column to place the current letter.
6. Use wildcard, bomb, and swap after they unlock.

## 10. Suggested first extensions

- Add touch-specific column buttons for mobile-only builds
- Replace the sample dictionary with a larger filtered English word list
- Add local save data for audio/settings and best scores
- Add pooling for particle prefabs if you expect heavy combo chains
