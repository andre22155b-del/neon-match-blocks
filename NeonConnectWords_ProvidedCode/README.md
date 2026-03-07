# Neon Connect Words

Unity mobile word-combo game prototype focused on:
- portrait one-handed play
- solo-first Classic mode
- gravity board word creation
- neon arcade combo feedback

The Unity project lives in this folder and is intended to support:
- Android release builds for Google Play
- iOS Xcode export for App Store release work
- GitHub-based source control and CI/CD workflows

## Open In Unity

- Project folder: `NeonConnectWords_ProvidedCode`
- Scene: `Assets/Scenes/NeonConnectWords.unity`
- Recommended Unity version: `6000.3.6f1`

## Build Entry Points

Editor build methods:
- Android APK: `NeonConnectWordsAndroidBuildEditor.BuildApk`
- Android AAB: `NeonConnectWordsAndroidBuildEditor.BuildAab`
- iOS Xcode export: `NeonConnectWordsiOSBuildEditor.BuildXcodeProject`

Output folders:
- Android: `Builds/Android`
- iOS: `Builds/iOS`

## Release Docs

- [Mobile Release Guide](Docs/MOBILE_RELEASE_GUIDE.md)
- [First Google Play Upload Runbook](Docs/FIRST_GOOGLE_PLAY_UPLOAD.md)
- [Store Submission Checklist](Docs/STORE_SUBMISSION_CHECKLIST.md)
- [Versioning Policy](Docs/VERSIONING_POLICY.md)
- [Setup Guide](Docs/SETUP_GUIDE.md)
