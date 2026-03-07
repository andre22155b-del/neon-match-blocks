# Mobile Release Guide

This project can be developed in GitHub and released to both Google Play and the App Store.

## What GitHub Handles

- Unity source control
- pull requests and review
- issue tracking and milestone planning
- CI workflows for Android and iOS export
- release tags and changelog history

## What GitHub Does Not Replace

- Google Play Console
- App Store Connect
- Apple signing and provisioning
- store metadata management

## Recommended Release Flow

1. Develop in feature branches.
2. Merge into a release branch after device validation.
3. Run the GitHub Actions mobile build workflow manually.
4. Upload Android `.aab` to Google Play Console.
5. Upload signed iOS archive through Xcode / Transporter to App Store Connect.
6. Soft launch on Android first.
7. Tune retention, UX, and crash issues.
8. Ship iOS after Android metrics look healthy.

## GitHub Actions Secrets

For Unity CI:
- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

Optional for Android signing:
- `ANDROID_KEYSTORE_BASE64`
- `ANDROID_KEYSTORE_PASSWORD`
- `ANDROID_KEYALIAS_NAME`
- `ANDROID_KEYALIAS_PASSWORD`

Optional for App Store automation later:
- `APPSTORE_CONNECT_ISSUER_ID`
- `APPSTORE_CONNECT_KEY_ID`
- `APPSTORE_CONNECT_PRIVATE_KEY`
- `APPLE_TEAM_ID`

## Build Outputs

Android:
- `Builds/Android/NeonConnectWords.apk`
- `Builds/Android/NeonConnectWords.aab`

iOS:
- `Builds/iOS/` exported Xcode project

## Recommended Branch Strategy

- `main`
  Stable release-ready code.
- `codex/...`
  Feature and implementation branches.
- `release/android-soft-launch`
  Optional pre-store stabilization branch.

## Versioning

Track at minimum:
- app version: `major.minor.patch`
- Android version code: increment every Play upload
- iOS build number: increment every TestFlight/App Store upload

## Current Project Reality

Ready now:
- Unity project under source control
- deterministic Android/iOS build entry points
- portrait mobile-first gameplay direction

Still requires manual store work:
- final app icon and splash assets
- privacy policy and store listing
- signed Android keystore strategy
- Apple certificates, provisioning, and App Store Connect setup
- device testing on real Android/iPhone hardware
