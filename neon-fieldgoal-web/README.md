# Neon FieldGoal Web

Standalone web build for `Neon FieldGoal`.

## What This Includes

- Mobile-first arcade field goal game in a single-page build
- Local `Community Heat` backend for localhost testing
- GitHub Pages-ready frontend deployment path
- Configurable remote leaderboard endpoint via `config.js`

## Local Run

From this folder:

```bash
node server.js
```

Then open:

```text
http://127.0.0.1:4173
```

That gives you:

- static game hosting
- `GET /api/community-heat`
- `POST /api/community-heat`
- persistent local leaderboard storage in `data/community-heat.json`

## GitHub Pages

GitHub Pages is a good host for the frontend, but it does **not** run the Node API in `server.js`.

This project is set up so that:

- on `localhost`, the game auto-uses the local API
- on GitHub Pages, the game stays fully playable and falls back to local/offline leaderboard mode unless you point it at a real hosted API

## Remote Leaderboard Config

Edit [config.js](/Users/drewtobar/Documents/neon-fieldgoal-web/config.js) and set:

```js
window.NFG_REMOTE_BOARD_URL = "https://your-api-host.example.com/api/community-heat";
```

If left blank, the game will:

- use `localhost` API automatically when served locally
- stay offline/local on GitHub Pages or other static hosts

## GitHub Workflow

The Pages workflow publishes the contents of this folder as a static site.

After the workflow is enabled in GitHub:

1. Turn on GitHub Pages for the repository.
2. Use the GitHub Actions deployment.
3. Optionally set `config.js` to a hosted leaderboard API later.

## Notes

- `data/community-heat.json` is the local seed board for localhost testing.
- `server.js` supports `NFG_BOARD_PATH=/path/to/file.json` for isolated test data.
