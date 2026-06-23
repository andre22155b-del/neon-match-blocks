# AddEm Capture Backend (Tiny Local Server)

This is a minimal backend for your AddEm browser extension.

## Features
- `POST /captures` to save captures
- `GET /captures` to list captures
- `DELETE /captures/:id` to remove a capture
- `GET /health` health check
- JSON file storage (`data/captures.json`)
- Optional bearer token auth (`ADDEM_API_TOKEN`)

## Run

From this folder:

```bash
cd /Users/drewtobar/Documents/Addem/addem-capture-backend
npm start
```

Server runs on:

- `http://127.0.0.1:8787`

## Optional auth token

```bash
ADDEM_API_TOKEN=your-secret-token npm start
```

When token is set, send:

- `Authorization: Bearer your-secret-token`

## Extension setup

In AddEm extension options, set:

- `Backend Capture Endpoint URL`: `http://127.0.0.1:8787/captures`
- `Auth Token`: (same token, if enabled)

## API examples

### Health
```bash
curl http://127.0.0.1:8787/health
```

### Save capture
```bash
curl -X POST http://127.0.0.1:8787/captures \
  -H 'Content-Type: application/json' \
  -d '{
    "title":"Apple Dev",
    "url":"https://developer.apple.com",
    "notes":"SwiftUI docs",
    "tags":["dev","docs"],
    "source":"extension"
  }'
```

### List captures
```bash
curl http://127.0.0.1:8787/captures
```

