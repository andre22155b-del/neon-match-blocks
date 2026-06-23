# Addem: Xcode Checklist (macOS + Widget)

1. Create app project:
- Open Xcode.
- File -> New -> Project -> App.
- Platform: macOS, Interface: SwiftUI, Language: Swift.
- Product Name: Addem.

2. Add widget extension:
- File -> New -> Target -> Widget Extension.
- Name it `AddemWidget`.
- Keep SwiftUI lifecycle.

3. Enable app groups for both targets:
- Addem target -> Signing & Capabilities -> + Capability -> App Groups.
- AddemWidget target -> Signing & Capabilities -> App Groups.
- Add same ID in both, e.g. `group.com.yourname.addem`.
- Update `AppGroupConfig.groupID` to match exactly.

4. Add shared files to both targets:
- `Shared/AppGroupConfig.swift`
- `Shared/LinkItem.swift`
- `Shared/SmartOrganizer.swift`
- `Shared/LinkStore.swift`

5. Add app files to Addem target:
- `AddemApp/AddemApp.swift`
- `AddemApp/Theme.swift`
- `AddemApp/ContentView.swift`
- `AddemApp/AddLinkSheet.swift`
- `AddemApp/MenuBarAddView.swift`

6. Add widget file to AddemWidget target:
- `AddemWidget/AddemWidget.swift`

7. Run app on macOS:
- Add a few links.
- Pin some links.

8. Test widget:
- Add widget to desktop.
- Confirm pinned links appear.
- Add/edit links in app; widget should refresh.

9. Sanity checks:
- Invalid URLs are blocked.
- Search + tag filter work.
- Open button launches browser.
- Delete row removes entry.
