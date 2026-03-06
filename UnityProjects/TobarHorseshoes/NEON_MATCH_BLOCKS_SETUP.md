# Neon Match Blocks - Scene Setup Blueprint

This setup assumes Unity URP + TextMeshPro + DOTween imported.

## Fastest path (one click)
Run this Unity menu item:

- `Tools > Neon Match Blocks > Auto Setup Complete Scene`

What it does automatically:
- Creates scene: `Assets/Scenes/NeonMatchBlocks.unity`
- Creates Neon cube prefab + material
- Creates placeholder sports face sprites
- Creates particle prefabs
- Builds Canvas/HUD/reflex panel/result panel
- Adds and wires `GameManagerCompetitive`, `UIManager`, `SoundManager`, `ParticleManager`

After running:
- Press Play for a full placeholder playable loop.
- Replace placeholder sprites, particles, and audio clips with your final assets.

## 1) Package prerequisites
- Import DOTween (`Tools > Demigiant > DOTween Utility Panel > Setup DOTween...`).
- Ensure TextMeshPro essentials are imported.

## 2) Folder layout (recommended)
- `Assets/Scripts/NeonMatchBlocks`
- `Assets/Prefabs/NeonMatchBlocks`
- `Assets/Art/NeonMatchBlocks/Sprites`
- `Assets/Audio/NeonMatchBlocks/SFX`
- `Assets/VFX/NeonMatchBlocks`

## 3) Scene hierarchy (exact blueprint)
Create this hierarchy in your game scene:

```text
NeonMatchBlocksScene
├── Main Camera (tilt X = 10 to 15 deg)
├── Directional Light
├── BoardRoot
├── Managers
│   ├── GameManagerCompetitive (script)
│   ├── UIManager (script)
│   ├── SoundManager (script + AudioSource)
│   └── ParticleManager (script)
├── EventSystem
└── Canvas (Screen Space - Overlay)
    ├── HUD
    │   ├── P1ScoreText (TMP)
    │   ├── P2ScoreText (TMP)
    │   ├── P1ComboText (TMP)
    │   ├── P2ComboText (TMP)
    │   ├── TurnText (TMP)
    │   ├── LevelText (TMP)
    │   ├── TimerText (TMP)
    │   ├── P1TurnGlow (Image)
    │   └── P2TurnGlow (Image)
    ├── ReflexPanel
    │   ├── CountdownText (TMP)
    │   ├── ResultText (TMP)
    │   ├── LeftTapButton (Button)
    │   │   └── LeftFlash (Image)
    │   └── RightTapButton (Button)
    │       └── RightFlash (Image)
    ├── LevelResultPanel
    │   ├── ResultTitleText (TMP)
    │   └── ResultBodyText (TMP)
    └── FloatingTextPrefab (TMP) [disabled prefab-like template]
```

## 4) NeonCube prefab setup
Create `NeonCube.prefab` with this structure:

```text
NeonCube (root)
├── Block.cs
├── BoxCollider
├── VisualRoot (mesh object)
│   ├── CubeMesh (neon material)
│   ├── FaceRoot (inactive by default)
│   │   └── FaceSpriteQuad OR FaceSpriteRenderer
│   └── GlowLight (optional)
```

Assign in `Block.cs`:
- `visualRoot` = `VisualRoot`
- `faceRoot` = `FaceRoot`
- `faceSpriteRenderer` or `faceMeshRenderer`
- `edgeGlowRenderer` = neon edge renderer
- `glowLight` = optional point light

## 5) Script inspector wiring

### GameManagerCompetitive
- `Block Prefab` -> `NeonCube.prefab`
- `Board Root` -> `BoardRoot`
- `Sports Faces` -> add sprites (soccer, basketball, football, baseball, boxing, etc)
- `Play Vs AI` -> true/false
- tune `Block Spacing`, score and timing values

### UIManager
- Assign all HUD TMP references
- Assign `P1TurnGlow`, `P2TurnGlow`
- Assign Reflex panel references:
  - countdown/result text
  - left/right buttons
  - left/right flash images
- Assign result panel references
- Assign `Floating Text Prefab` (TMP)

### SoundManager
- Add `AudioSource`
- Add clip entries with ids:
  - `Flip`
  - `Match`
  - `Wrong`
  - `Combo`
  - `LevelComplete`
  - `Countdown`
  - `Tap`

### ParticleManager
- Assign prefabs:
  - `Normal Match FX`
  - `Rare Match FX`
  - `Legendary Match FX`
  - `Ring FX`
  - `Wrong FX`
  - `Combo FX`
  - `Level Complete FX`

## 6) Quick gameplay validation checklist
- Start level -> Reflex mini-game panel appears.
- Winner of reflex starts turn.
- Flip two blocks:
  - match -> score, combo, particles, chime.
  - mismatch -> glitch/wrong FX, blocks flip back, turn swaps.
- AI mode picks blocks after delay.
- Rare/legendary blocks show stronger glow and FX.
- Timer and level result panel behave correctly.
- Level progression goes from small to larger grids.

## 7) Common fixes
- `DG.Tweening` compile error: DOTween not installed/setup.
- No click interaction: ensure each cube has a Collider and camera sees blocks.
- No floating text: `UIManager.canvas` or `floatingTextPrefab` not assigned.
- No sounds: `SoundManager` clip IDs must exactly match script names.

## Script locations
- `Assets/Scripts/NeonMatchBlocks/GameManagerCompetitive.cs`
- `Assets/Scripts/NeonMatchBlocks/Block.cs`
- `Assets/Scripts/NeonMatchBlocks/ParticleManager.cs`
- `Assets/Scripts/NeonMatchBlocks/SoundManager.cs`
- `Assets/Scripts/NeonMatchBlocks/UIManager.cs`
