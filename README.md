# Task PWA — Offline-First Task Manager

An offline-first, installable Progressive Web App (PWA) task manager built with:

- **Frontend**: Vue 3 + Vite + Vuetify 3 + Pinia + Vue Router
- **Backend**: ASP.NET Core 9 Web API (Controllers) + EF Core + SQLite

## Project Structure

```
task-pwa/
  client/    Vue 3 PWA frontend
  server/    ASP.NET Core Web API backend
```

---

## Running the Server

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Steps

```bash
cd server
dotnet run
```

The API will be available at `http://localhost:5015`.

Swagger UI is available at `http://localhost:5015/swagger` in development.

The SQLite database (`taskpwa.db`) is created automatically on first run.

---

## Running the Client

### Prerequisites
- Node.js 18+

### Steps

```bash
cd client
npm install
npm run dev
```

The app will be available at `http://localhost:5173`.

> **Note**: The client connects to the API at `http://localhost:5015` by default.
> You can override this via the `VITE_API_BASE` environment variable in a `.env.local` file.

---

## API Overview

### Authentication
All endpoints require an `X-User-Key: <guid>` header to partition data per user.

### Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/tasks/changes?since={isoDateTime}` | Get tasks changed since timestamp |
| `POST` | `/api/sync` | Apply a batch of operations and get server changes |

### Sync Request Body (`POST /api/sync`)
```json
{
  "clientId": "<guid>",
  "lastSyncAt": "2026-01-01T00:00:00.000Z",
  "operations": [
    {
      "opId": "<guid>",
      "type": "upsert",
      "entityId": "<task-guid>",
      "createdAt": "2026-01-01T00:01:00.000Z",
      "task": {
        "id": "<task-guid>",
        "title": "Buy milk",
        "notes": null,
        "isCompleted": false,
        "dueAt": null,
        "updatedAt": "2026-01-01T00:01:00.000Z",
        "deletedAt": null,
        "baseVersion": null
      }
    }
  ]
}
```

---

## Multi-Device Sync

1. On the first device, go to **Settings** and copy your **User Key**.
2. On the second device, go to **Settings**, paste the key, and click **Apply Pasted Key**.
3. Click **Sync Now** on both devices.

Tasks are partitioned by `userKey`. The server uses last-write-wins (by `updatedAt`) for conflict resolution in v1.

---

## PWA Features

- **Installable**: Chrome will prompt to install when using the production build (`npm run build && npm run preview`).
- **Offline**: All CRUD is backed by IndexedDB. Changes are queued in an outbox and synced when online.
- **Update Prompt**: A snackbar appears when a new service worker version is available.

---

## Development Notes

- The client uses `VITE_API_BASE` env variable (default: `http://localhost:5015`) to reach the API.
- CORS is configured for `http://localhost:5173` and `http://localhost:3000` in development.
- The SQLite DB file is excluded from git.
