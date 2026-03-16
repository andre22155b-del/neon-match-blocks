# Add Neon FieldGoal arcade mode with audio identity and game-feel polish

## A. What system was analyzed
- Player input and responsiveness for lateral movement, kick charging, aim release, and trajectory preview.
- Ball flight and scoring flow, including release smoothing, gravity shaping, yard-line progression, long bombs, moving-goal challenge, and round timing.
- Presentation systems around HUD readability, title/results flow, referee reactions, goal lighting, camera juice, and crowd feedback.
- Audio layering across kick, goal, miss, announcer, crowd, and ambient playback.

## B. Problems found
- The project did not have a dedicated field-goal arcade loop, so there was no complete kickoff-to-results experience for this mode.
- Live run state was not clear enough during play. Score, goals, longest kick, kick value, and moving-goal state all needed to be surfaced better.
- Miss feedback was too flat. Near misses were not classified, which made wide/low misses feel the same as generic failures.
- Big moments were not escalated enough across visuals, refs, crowd, and audio, especially for 50+ yard makes and clutch play.
- Audio overlap was a risk. Without cue priority and ducking, announcer-style stings, crowd one-shots, and ambient could stack into a muddy mix.
- Local Unity CLI validation is currently unreliable on this machine because batchmode hangs during licensing startup.

## C. Improvements made
- Added a full `Neon FieldGoal` arcade mode with:
  - 20-yard start.
  - Move back 5 yards after each make.
  - 50+ yard kicks worth 5 points.
  - Moving goal starting at the 50-yard line.
  - Slow lateral movement and swipe-up kick release.
- Added smooth kick release/gravity tuning and trajectory preview so the ball arc feels more readable and arcade-friendly.
- Added richer gameplay state management:
  - score, goals, longest made kick, multiplier, and yard-line context on the HUD.
  - persistent best score and best longest-kick records.
  - title screen, mode select, and stronger results messaging.
- Added near-miss classification and feedback for wide-left, wide-right, low, and combined miss cases.
- Added stronger spectacle systems:
  - neon turf and field markings.
  - moving-goal controller.
  - hotter goal presentation and referee hype.
  - camera juice and clutch escalation.
- Added an audio identity layer:
  - deterministic cue selection for perfect kicks, long bombs, heat, clutch, near misses, streak breaks, final drive, and moving-goal activation.
  - procedural fallback clips so the mode still has personality without final assets.
  - cue priority, cooldown spacing, and crowd/ambient ducking under announcer moments.
- Added or expanded EditMode coverage around scoring, yard progression, gravity/release shaping, moving-goal behavior, miss classification, and audio cue logic.

## D. How the changes improve gameplay feel
- The mode now has a full arcade loop instead of isolated mechanics.
- Risk/reward is clearer and more satisfying: deeper kicks score more, the goal starts moving later, and misses keep pressure on the clock.
- The ball release and preview are easier to trust, which makes aiming feel fairer and more responsive.
- Players get cleaner feedback on why they missed, which supports faster skill-building and less frustration.
- Big makes now feel like events, not just score increments, thanks to better layering across UI, refs, lights, crowd, camera, and announcer cues.
- The mix is more readable because high-value moments can take focus without every sound firing at once.

## E. Risks and things to test
- Do a real in-editor and on-device play pass for mix balance:
  - `announcerVolume`
  - `crowdDuckUnderAnnouncer`
  - `ambientDuckUnderAnnouncer`
  - `announcerCueGapSeconds`
- Verify that trajectory preview still matches live physics after any future tuning to impulse, gravity, or curve.
- Playtest the moving-goal threshold and 50+ yard scoring to confirm the challenge curve feels fun instead of punishing.
- Confirm HUD readability in landscape on smaller mobile screens.
- Re-run Unity scene generation and tests once the local licensing startup issue is fixed. Batchmode on this machine is still hanging before reliable validation completes.

## Validation
- Branch contains the full `Neon FieldGoal` feature plus the follow-up audio identity and mix polish.
- Code-level validation and targeted test additions were completed.
- Unity batch validation is still blocked locally by licensing startup hangs, so the main remaining risk is environment verification and final feel tuning rather than a known gameplay logic defect.
