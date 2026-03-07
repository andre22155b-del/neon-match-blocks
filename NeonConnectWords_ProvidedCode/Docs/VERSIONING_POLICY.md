# Versioning Policy

Use this policy for every store build.

## Version Fields

The project currently uses:

- `bundleVersion`
  User-facing version string
- `AndroidBundleVersionCode`
  Monotonic integer required by Google Play
- `buildNumber.iPhone`
  Monotonic integer for iOS uploads

These live in:
- `ProjectSettings/ProjectSettings.asset`

## Current Baseline

- app version: `0.1.0`
- Android version code: `1`
- iOS build number: `1`

## Rules

1. Increase `AndroidBundleVersionCode` on every Android upload.
2. Increase `buildNumber.iPhone` on every TestFlight or App Store upload.
3. Increase `bundleVersion` whenever the release is meaningfully different to the player.
4. Do not reuse a version code or iOS build number after a failed store upload attempt.

## Practical Scheme

For this project, use:

- `0.1.x`
  pre-launch test builds
- `0.2.x`
  soft launch builds
- `1.0.0`
  public launch

Example progression:

- `0.1.0` / Android `1` / iPhone `1`
- `0.1.1` / Android `2` / iPhone `2`
- `0.1.2` / Android `3` / iPhone `3`
- `0.2.0` / Android `4` / iPhone `4`

## Update Checklist Per Release

1. Decide the next `bundleVersion`.
2. Increment `AndroidBundleVersionCode` if shipping to Google Play.
3. Increment `buildNumber.iPhone` if shipping to TestFlight/App Store.
4. Commit the version change before the build.
5. Tag the release after the store artifact is accepted.

## What Not To Do

- do not upload two Android builds with the same version code
- do not upload two iOS builds with the same iPhone build number
- do not leave the player-facing version static across meaningful releases
