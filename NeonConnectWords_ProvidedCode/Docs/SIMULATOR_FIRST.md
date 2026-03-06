# Simulator-First Notes

The provided codebase is scene-first: game rules are spread across `MonoBehaviour` logic, animation coroutines, UI, and effects. A simulator-first version should invert that so one pure rules engine owns:

- board state
- turn order
- letter draws
- word detection
- cascade resolution
- scoring
- power-up inventory
- win/loss conditions

## New simulator layer

These files were added under `Assets/Scripts/Simulation/`:

- `SimulationModels.cs`
- `SimulationDictionary.cs`
- `NeonConnectSimulator.cs`
- `SimulationService.cs`

## What it does

`NeonConnectSimulator` is a pure state machine. It does not instantiate objects, play sounds, or animate tiles. It accepts actions and returns deterministic turn results.

Supported actions:

- start a game
- drop the current letter into a column
- arm a wildcard for the current player
- bomb a row
- bomb a column
- swap two tiles
- advance timed mode manually

## Unity integration direction

The intended flow is:

1. `SimulationService` owns the simulator instance.
2. UI or input asks `SimulationService` to perform an action.
3. The returned `SimulationTurnResult` tells the Unity layer what changed.
4. Board visuals, particles, camera, and audio react to the simulator result instead of deciding rules themselves.

That means `BoardManager`, `GameManager`, `PowerUpManager`, and `UIManager` should become adapters over time, not rule owners.

## Why this is better

- deterministic: the same seed and inputs produce the same match
- testable: the rules can be exercised without scene setup
- portable: the same engine can power editor tools, bots, replay systems, and backend validation
- simpler debugging: you can inspect state transitions without waiting on animations

## Current status

The original provided managers are still present and unchanged. The simulator layer is added alongside them so you can pivot safely without losing the existing scene code.

## Recommended next step

Refactor `BoardManager` first:

- stop treating the scene as the source of truth
- call `SimulationService.DropAtColumn`
- animate toward the board state returned by the simulator
- use `SimulationTurnResult.Cascades` to drive clears, floating scores, particles, and combo feedback

Once that is in place, move score, combo, and power-up UI off direct gameplay logic and onto simulator state snapshots.
