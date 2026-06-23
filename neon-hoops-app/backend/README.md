# Neon Hoopz Backend

Local backend service for persistent game data.

## Run

```bash
npm run backend
```

Default URL: `http://localhost:8787`

## Endpoints

- `GET /health`
- `GET /api/config`
- `GET /api/leaderboard?limit=10`
- `GET /api/recent-matches?limit=6`
- `GET /api/player/:name`
- `POST /api/session/start`
- `POST /api/session/:sessionId/finish`

Data persists to `backend/data/game-data.json`.
