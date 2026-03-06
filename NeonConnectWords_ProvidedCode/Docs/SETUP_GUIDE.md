# Neon Connect Words — Unity Setup Guide
## Complete Scene Assembly Instructions

---

## 1. Project Setup

1. Create a new **Unity 3D** project (Unity 2022 LTS or later recommended).
2. Install **TextMeshPro** via Package Manager (Window → Package Manager → Unity Registry).
3. Copy all scripts from `Scripts/` into `Assets/Scripts/` maintaining the folder structure:
   - `Assets/Scripts/Core/` → BoardManager, GameManager, WordChecker, LetterTile, CameraAndParticles, PuzzleManager
   - `Assets/Scripts/UI/` → UIManager
   - `Assets/Scripts/Audio/` → AudioManager
   - `Assets/Scripts/PowerUps/` → PowerUpManager
   - `Assets/Scripts/Tutorials/` → TutorialManager

---

## 2. Scene Hierarchy

```
Scene: MainGame
│
├── [MANAGERS]
│   ├── GameManager          (GameManager.cs)
│   ├── BoardManager         (BoardManager.cs)
│   ├── WordChecker          (WordChecker.cs)
│   ├── AudioManager         (AudioManager.cs)
│   ├── UIManager            (UIManager.cs)
│   ├── PowerUpManager       (PowerUpManager.cs)
│   ├── TutorialManager      (TutorialManager.cs)
│   ├── PuzzleManager        (PuzzleManager.cs)
│   └── ParticleManager      (ParticleManager.cs — on same GO as CameraController)
│
├── [CAMERA]
│   └── Main Camera          (CameraController.cs)
│       Position: (0, 4, -12)
│       Rotation: (10, 0, 0)
│       FOV: 60
│
├── [BOARD]
│   ├── BoardOrigin          (empty transform, position 0,0,0)
│   ├── BoardBackground      (Quad or Plane mesh for backdrop)
│   └── ColumnColliders
│       ├── ColCollider_0 … ColCollider_6  (Box Colliders covering each column)
│
├── [LIGHTING]
│   ├── DirectionalLight     (soft warm key light, intensity 0.8)
│   ├── BoardPointLight      (Point Light, assigned to ParticleManager.boardLight)
│   │   Position: (0, 3, -2), Color: #00FFFF, Intensity: 1
│   └── RimLight             (Directional from behind, color #FF00FF, intensity 0.4)
│
├── [UI — Canvas, Screen Space Overlay]
│   ├── HUDPanel
│   │   ├── Player1Score     (TextMeshProUGUI)
│   │   ├── Player2Score     (TextMeshProUGUI)
│   │   ├── TurnIndicator    (Panel + TextMeshProUGUI)
│   │   ├── CurrentLetter    (Panel + TextMeshProUGUI)
│   │   ├── ComboMultiplier  (TextMeshProUGUI)
│   │   ├── StreakBar        (Slider)
│   │   └── TimerGroup       (Panel with Image fill + TextMeshProUGUI)
│   │
│   ├── MainMenuPanel
│   │   ├── TitleText
│   │   ├── PlayButton       (→ ShowModeSelect)
│   │   └── SettingsButton
│   │
│   ├── ModeSelectPanel
│   │   ├── ClassicButton    (→ OnPlayClassic)
│   │   ├── TimedButton      (→ OnPlayTimed)
│   │   ├── PuzzleButton     (→ OnPlayPuzzle)
│   │   └── TutorialButton   (→ OnPlayTutorial)
│   │
│   ├── GameOverPanel        (CanvasGroup for fade)
│   │   ├── TitleText
│   │   ├── ScoresText
│   │   ├── PlayAgainButton
│   │   └── MainMenuButton
│   │
│   ├── SettingsPanel
│   │   ├── MusicToggle
│   │   ├── SFXToggle
│   │   ├── ColorBlindToggle
│   │   ├── ReducedFXToggle
│   │   ├── LetterSizeSlider
│   │   └── CloseButton
│   │
│   ├── PowerUpBar
│   │   ├── WildcardButton + CountText
│   │   ├── BombButton + CountText
│   │   └── SwapButton + CountText
│   │
│   ├── FloatingScoreCanvas  (assign as parent for floating score prefabs)
│   ├── MessageBanner        (TextMeshProUGUI + CanvasGroup)
│   └── TutorialOverlay
│       ├── TitleText
│       ├── BodyText
│       ├── NextButton
│       ├── SkipButton
│       └── HighlightArrow   (Image with arrow sprite)
│
└── [AUDIO]
    ├── MusicSource          (AudioSource, loop=true)
    └── SFXSource            (AudioSource)
```

---

## 3. Prefabs to Create

### A. LetterTile Prefab
```
LetterTile (empty root)
├── TileMesh          (3D Cube or custom beveled mesh, scale 0.9,0.9,0.2)
│   └── Material:     NeonLetter_P1 / NeonLetter_P2 (see Materials section)
├── GlowMesh          (slightly larger cube, additive material, alpha 0.3)
├── LetterLabel       (TextMesh3D, centered, font size 4)
├── TrailRenderer     (component on root, disabled by default)
├── HighlightParticles (Particle System — star burst, yellow/white)
├── RemoveParticles   (Particle System — dissolve, blue/cyan)
└── BoxCollider       (size 0.9, 0.9, 0.9)
Components on root: LetterTile.cs
```

### B. FloatingScore Prefab
```
FloatingScore (empty root)
├── ScoreText         (TextMeshProUGUI, anchored center, font size 28, bold)
├── CanvasGroup       (for fade)
└── RectTransform     (pivot 0.5, 0.5)
```

### C. WordComplete Particle Prefab
- Particle System, burst of 30–50 particles
- Shape: Sphere radius 0.3
- Velocity: random, speed 2–5
- Color: gradient cyan → white → transparent
- Lifetime: 0.8s

### D. EpicCombo Particle Prefab
- Particle System, burst of 100+ particles
- Multiple emitters: star sparks + ring shockwave
- Colors: cycle through cyan, magenta, yellow
- Lifetime: 1.5s, use sub-emitters for trails

### E. ColumnHighlight Prefab (7 instances, for tutorials)
- Vertical glowing plane or line renderer
- Semi-transparent additive material, animated pulse alpha

---

## 4. Materials

### NeonLetter_P1 (Player 1 — Cyan)
- Shader: Universal Render Pipeline/Lit (or Standard)
- Albedo: #001C1C
- Emission: ON, Color: #00FFFF, Intensity: 3
- Metallic: 0.3, Smoothness: 0.9

### NeonLetter_P2 (Player 2 — Magenta)
- Same as above but Emission Color: #FF00FF

### NeonLetter_Wildcard (White)
- Emission Color: #FFFFFF, Intensity: 5 (pulsed by script)

### BoardBackground
- Dark navy/black panel with subtle grid lines
- Albedo: #05051A, slight reflection

### GlowAdditive
- Shader: Sprites/Default or Unlit/Transparent
- Blend: Additive, Alpha 0.3

---

## 5. Inspector Wiring Checklist

### GameManager
- [ ] boardManager → BoardManager GO
- [ ] uiManager → UIManager GO
- [ ] audioManager → AudioManager GO
- [ ] tutorialManager → TutorialManager GO
- [ ] powerUpManager → PowerUpManager GO
- [ ] classicTargetScore = 100
- [ ] timedDuration = 120
- [ ] letterPool = "EEEEEEEEEEEEAAAAAAAAAIIIIIIOOOOOOUUUU..." (full string in script)

### BoardManager
- [ ] letterPrefab → LetterTile prefab
- [ ] boardOrigin → BoardOrigin transform
- [ ] wordChecker → WordChecker GO
- [ ] uiManager → UIManager GO
- [ ] audioManager → AudioManager GO
- [ ] powerUpManager → PowerUpManager GO
- [ ] particleManager → ParticleManager GO
- [ ] playerMaterials[0] → NeonLetter_P1
- [ ] playerMaterials[1] → NeonLetter_P2
- [ ] boardLayerMask → "BoardLayer" (assign column colliders to this layer)

### UIManager
- [ ] Assign all TextMeshProUGUI and Button references
- [ ] playerTurnColors[0] = #00FFFF, [1] = #FF00FF

### AudioManager
- [ ] musicSource → MusicSource AudioSource
- [ ] sfxSource → SFXSource AudioSource
- [ ] Assign AudioClip slots (see Placeholder Audio section)

### PowerUpManager
- [ ] boardManager, audioManager, uiManager references

### TutorialManager
- [ ] Configure TutorialStep array (3 entries minimum):
  - Step 0: "Welcome!" / "Drop a letter into any column." / requiresPlayerAction=true / highlightColumns=[3]
  - Step 1: "Great!" / "Spell a word by filling the board — letters line up horizontally, vertically, or diagonally!" / requiresPlayerAction=true
  - Step 2: "Advanced!" / "Use power-ups to clear tiles and chain combos for massive points!" / requiresPlayerAction=false

---

## 6. Placeholder Audio

Since we cannot include audio files, generate or source free alternatives:

| Slot | Description | Source Suggestion |
|------|-------------|-------------------|
| musicTracks[0] | Chill synthwave loop | freemusicarchive.org, lofi synthwave |
| musicTracks[1] | Intense synthwave | Higher BPM version or pitch-up of above |
| dropStartClip | Whoosh / air displacement | freesound.org "swoosh" |
| letterLandClip | Soft thud / plastic click | freesound.org "click plastic" |
| wordCompleteClip | Chime / bell ding | freesound.org "chime" |
| multiWordClip | Chord strum / layered chime | freesound.org "arpeggio" |
| epicComboClip | Rising synth swell + impact | freesound.org "power up synth" |
| cascadeClip | Cascading water tones | freesound.org "cascade" |
| powerUpWildcardClip | Star sparkle | freesound.org "magic sparkle" |
| powerUpBombClip | Deep boom / explosion | freesound.org "explosion soft" |
| powerUpSwapClip | Swipe / swoosh | freesound.org "swipe" |
| uiClickClip | Soft UI click | freesound.org "button click" |

All audio clips should be:
- Format: MP3 or WAV
- Normalized to -6dBFS
- Loop points set for music clips

---

## 7. Word List Asset

1. Download a free word list: https://github.com/dwyl/english-words (words_alpha.txt)
2. Filter to words 3–10 letters long.
3. Import as a TextAsset into Unity (`Assets/Resources/wordlist.txt`)
4. Assign to WordChecker.wordListAsset in Inspector.
5. The built-in list (GetBuiltInWordList) works as a fallback prototype.

---

## 8. Build Settings

### Desktop
- Platform: Windows / Mac / Linux
- Resolution: 1920×1080 default, resizable
- Quality: High

### Mobile (iOS / Android)
- Platform: iOS or Android
- Resolution: Auto (portrait or landscape, adjust board camera)
- Quality: Medium (enable reducedFX toggle by default)
- Enable: PlayerSettings → Accelerometer Frequency: Disabled (touch only)
- Add mobile touch input support:

```csharp
// In BoardManager.HandleMouseInput(), replace mouse raycast with:
if (Input.touchCount > 0)
{
    Touch touch = Input.GetTouch(0);
    Ray ray = Camera.main.ScreenPointToRay(touch.position);
    // ... rest of logic unchanged
}
```

---

## 9. Performance Notes

- Board uses object pooling for LetterTile prefabs (add PoolManager for production).
- Particle systems should have Max Particles capped:
  - WordComplete: 60
  - EpicCombo: 150
  - SmallSpark: 20
- Disable shadow casting on letter tile meshes (cosmetic, not needed).
- Use `Camera.main` caching; avoid per-frame `FindObjectOfType` calls.
- Trie precomputation in `Awake()` ensures word lookup is O(n) per scan.

---

## 10. Extension Points

The architecture is designed for easy expansion:

| Feature | Where to Add |
|---------|-------------|
| Online Multiplayer | GameManager (Netcode/Mirror/Photon integration) |
| New Power-Up | PowerUpManager.PowerUpType enum + new activation method |
| Cosmetic Skins | LetterTile.SetOwner() — pass alternate material sets |
| More Game Modes | GameMode enum + GameManager switch |
| Leaderboards | New LeaderboardManager.cs, hook into GameManager.EndGame |
| AI Opponent | New AIPlayer.cs — implement WordChecker scanning for best moves |

---

*Neon Connect Words — Unity C# Codebase v1.0*
*Scripts: BoardManager, WordChecker, GameManager, LetterTile, UIManager, AudioManager, PowerUpManager, TutorialManager, PuzzleManager, CameraController, ParticleManager*
