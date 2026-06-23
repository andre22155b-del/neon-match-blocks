# AddEm Chrome Extension (Starter)

This extension gives AddEm a browser plugin surface like your "AI Mode" example:
- Toolbar button near the URL bar
- One-click save of current page
- Save selected text via right-click
- Optional sync to your backend endpoint
- Local queue fallback when endpoint is not configured

## What it does right now

- Captures:
  - `url`
  - `title`
  - `notes` (selected text or manual)
  - `tags`
  - `source`
  - `createdAt`
- If `endpointUrl` is configured, it sends `POST` JSON to your endpoint.
- If endpoint is missing/fails, it stores capture in local pending queue.
- Badge on the AddEm icon shows pending count.

## Install in Chrome (Developer mode)

1. Open `chrome://extensions`
2. Turn on `Developer mode` (top right)
3. Click `Load unpacked`
4. Select this folder:
   - `/Users/drewtobar/Documents/Addem/addem-browser-extension-chrome`

## First-time setup

### Start local AddEm backend (recommended)

Use the backend created for this project:

```bash
cd /Users/drewtobar/Documents/Addem/addem-capture-backend
npm start
```

Then in extension settings use:
- `Backend Capture Endpoint URL`: `http://127.0.0.1:8787/captures`

1. Click AddEm extension icon -> `Configure Endpoint`
2. Set:
   - `Backend Capture Endpoint URL` (optional for now)
   - `Auth Token` (optional)
   - `AddEm Deep Link Base` (default `addem://capture`)
3. Save settings.

## Usage

### Popup capture
1. Open any page.
2. Click AddEm icon in toolbar.
3. Confirm URL/title/notes/tags.
4. Click `Save`.

### Right-click capture
- Right-click page -> `Save page to AddEm`
- Highlight text -> right-click -> `Save selection to AddEm`

### Sync pending queue
- In popup, click `Sync Pending`.

## Backend payload format

The extension sends JSON like:

```json
{
  "id": "uuid",
  "title": "Page Title",
  "url": "https://example.com",
  "notes": "Optional selected text",
  "tags": ["example", "web"],
  "source": "popup",
  "createdAt": "2026-03-04T12:00:00.000Z"
}
```

## Suggested endpoint contract (v1)

- Method: `POST`
- URL: `/captures`
- Auth: `Authorization: Bearer <token>` (optional)
- Response: `200-299` for success

## Next integration step with AddEm app

When your macOS app supports a custom URL scheme handler (`addem://capture?...`),
`Open AddEm` will deep-link directly into Quick Add with prefilled fields.
