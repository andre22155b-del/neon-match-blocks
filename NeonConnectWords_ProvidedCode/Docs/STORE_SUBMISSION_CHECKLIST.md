# Store Submission Checklist

## Shared

- confirm portrait orientation
- confirm offline core gameplay works
- confirm no placeholder text remains
- confirm profanity filter / dictionary exclusions are correct
- confirm privacy policy URL exists
- confirm app name, subtitle, and short description are finalized
- capture screenshots on target phone sizes
- capture gameplay video for store/social use
- test on at least one low/mid Android device and one modern iPhone

## Google Play

- confirm Android signing secrets exist in GitHub:
  - `ANDROID_KEYSTORE_BASE64`
  - `ANDROID_KEYSTORE_PASSWORD`
  - `ANDROID_KEYALIAS_NAME`
  - `ANDROID_KEYALIAS_PASSWORD`
- generate signed `.aab`
- increment Android version code
- complete Play Store listing
- upload feature graphic, icon, screenshots
- complete content rating questionnaire
- complete data safety form
- set closed/internal testing track first
- validate ANR/crash-free launch on test devices

## App Store

- export iOS Xcode project
- archive signed iOS build in Xcode
- increment iOS build number
- upload to App Store Connect
- fill App Privacy section
- provide age rating information
- complete screenshots for required devices
- test via TestFlight before submission

## Product Checks For This Game

- board remains readable during combos
- bottom letter tray does not cover active play space
- touch input is accurate near screen edges
- power-up buttons are reachable one-handed
- reduced-FX mode preserves clarity
- combo reward feedback feels satisfying without hurting frame rate
