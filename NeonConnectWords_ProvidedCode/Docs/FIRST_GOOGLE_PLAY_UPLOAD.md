# First Google Play Upload Runbook

This is the exact sequence to produce the first signed Android App Bundle for `Neon Connect Words` and upload it to Google Play Console.

## 1. Confirm Project Identity

Before the first upload, confirm these values in the Unity project:

- product name: `Neon Connect Words`
- Android package ID: `com.andrewtobar.neonconnectwords`
- app version: `0.1.0`
- Android version code: `1`

These are currently set in:
- `ProjectSettings/ProjectSettings.asset`

## 2. Create The Google Play App

In Google Play Console:

1. Create a new app.
2. Use the app name `Neon Connect Words`.
3. Choose the default language.
4. Set app type to `App`.
5. Choose `Free`.

Important:
- The package name in Play Console must match `com.andrewtobar.neonconnectwords`.
- Once the app is created, changing the package ID later is not practical for this launch path.

## 3. Prepare A Release Keystore

Create one release keystore and keep it safe. Do not rotate it casually after launch.

Example:

```bash
keytool -genkeypair \
  -v \
  -keystore neon-connect-words.keystore \
  -alias neonconnectwords \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000
```

Record these values securely:

- keystore file
- keystore password
- alias name
- alias password

## 4. Add GitHub Secrets

In the GitHub repo, add these secrets:

Unity:
- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

Android signing:
- `ANDROID_KEYSTORE_BASE64`
- `ANDROID_KEYSTORE_PASSWORD`
- `ANDROID_KEYALIAS_NAME`
- `ANDROID_KEYALIAS_PASSWORD`

To create `ANDROID_KEYSTORE_BASE64` on macOS:

```bash
base64 -i /path/to/neon-connect-words.keystore | pbcopy
```

Paste the copied value into the repo secret.

## 5. Add Store Assets

Prepare and keep these in the repo under `StoreAssets/GooglePlay/`:

- app icon source
- feature graphic
- phone screenshots
- short description draft
- full description draft
- privacy policy URL note

## 6. Validate Before Building

Check these before the first upload:

- the game runs in portrait
- the board stays readable during combos
- the letter tray does not hide active play space
- one-handed touch works near the screen edges
- reduced-FX mode does not break readability
- the game has no placeholder art/text

## 7. Run The GitHub Build

In GitHub:

1. Open `Actions`
2. Open `Neon Connect Words Mobile Build`
3. Click `Run workflow`
4. Choose `android-aab`
5. Start the workflow

Expected output artifact:
- `neon-connect-words-android-aab`

Inside it:
- `NeonConnectWords.aab`

## 8. Upload To Internal Testing First

In Google Play Console:

1. Open `Testing > Internal testing`
2. Create a release
3. Upload the generated `.aab`
4. Save and review the release
5. Roll out to internal testing

Do not go to production first.

## 9. Finish Play Console Requirements

Complete these sections before broader rollout:

- app access
- ads declaration
- content rating
- data safety
- privacy policy
- store listing
- screenshots and graphics
- tester access

## 10. Test The Internal Build

Install the internal-test build and verify:

- startup is stable
- touch input is accurate
- score/combo UI fits on real devices
- performance holds on a mid-tier Android device
- no obvious battery/heat spike appears during short sessions

## 11. Before The Second Upload

Increment:

- `bundleVersion` from `0.1.0` to the next app version you want
- `AndroidBundleVersionCode` from `1` to `2`

Rule:
- `bundleVersion` is the user-facing version
- `AndroidBundleVersionCode` must go up on every Play upload

## First Upload Success Criteria

The first upload is successful when:

- the signed `.aab` is generated in GitHub Actions
- Google Play accepts the bundle
- the internal test release installs correctly
- the app runs on a real Android device without obvious blocking issues
