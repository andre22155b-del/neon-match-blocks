# Addem Architecture (Beginner-Friendly)

## Recommended stack
- Swift + SwiftUI: native macOS UI, easiest path for beginners.
- WidgetKit: native widgets and timeline refresh support.
- Local JSON storage in App Group: simple, transparent, no heavy database setup.

## Why this stack
Pros:
- Lowest setup complexity.
- One language for app + widget.
- Easy to inspect/backup raw JSON.
- Fast for small-to-medium personal link collections.

Cons:
- JSON is not ideal for huge datasets or complex queries.
- No built-in migration layer like Core Data.
- You manually manage concurrency and data validation.

## Data model
`LinkItem`
- id (UUID)
- urlString (String)
- title (String)
- tags ([String])
- notes (String)
- isPinned (Bool)
- createdAt (Date)
- lastOpenedAt (Date?)

## Data sharing
- App and widget both read/write one JSON file in App Group container.
- `LinkStore` handles app reads/writes.
- Widget uses `LinkStoreReader` (read-only).
- App triggers widget refresh via `WidgetCenter.shared.reloadAllTimelines()` after save.

## Widget behavior
- Show pinned links first.
- If no pinned links, show recent links.
- Small widget: top 2 links.
- Medium widget: top 4 links.
- Timeline refresh every 30 minutes + manual reload on app save.
