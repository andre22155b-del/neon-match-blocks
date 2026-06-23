# Addem Starter: Start Here

## Recommended stack
Yes: Swift + SwiftUI + WidgetKit is the right stack for this app.

Pros:
- Native performance and platform fit.
- Single UI approach for app and widget.
- Beginner-friendly docs and templates in Xcode.

Cons:
- WidgetKit has strict refresh rules and limited interactivity.
- UI polish takes time compared with plain utility apps.
- Cross-platform reuse is low (macOS-first).

## High-level implementation plan
1. Create macOS SwiftUI app target.
2. Add Widget Extension target.
3. Add App Group capability to both targets.
4. Add shared model/store files to both targets.
5. Build main app UI (list, search, tags, quick add).
6. Add menu bar quick capture.
7. Build widget using pinned links from shared storage.
8. Verify persistence + timeline refresh behavior.
9. Add tests for storage and organizer logic.

## v1 scope
- Save/open links
- Tags + notes
- Pin important links
- Search + tag filters
- Menu bar quick add
- Small + medium widget
- Local-only JSON persistence
