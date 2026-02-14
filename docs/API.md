# API Specification

## Rimworld Mod HTTP API

The mod exposes a local HTTP server on `http://localhost:18800`.

**Design constraints:**
- HTTP listener thread must never call RimWorld APIs directly.
- Gameplay-affecting commands are queued and executed on the main thread.
- All JSON is UTF-8.

---

## Common Conventions

### Headers
- `Content-Type: application/json` for all `POST` requests.

### Error Envelope
All endpoint errors return:

```json
{
  "success": false,
  "error": "ERROR_CODE",
  "message": "Human-readable explanation"
}
```

### HTTP Status Policy
- `200 OK` — success
- `400 Bad Request` — invalid payload/params
- `409 Conflict` — valid request but impossible in current game state
- `429 Too Many Requests` — cooldown/rate limit active
- `503 Service Unavailable` — game/mod not ready

---

## Endpoint: `GET /health`

### Purpose
Health check and runtime readiness.

### Request
No body.

### Success (`200`)
```json
{
  "status": "ok",
  "game_running": true,
  "colony_loaded": true,
  "mod_version": "0.1.0",
  "queue_depth": 0,
  "uptime_seconds": 123
}
```

### Schema (response)
```json
{
  "type": "object",
  "required": ["status", "game_running", "colony_loaded", "mod_version", "queue_depth", "uptime_seconds"],
  "properties": {
    "status": { "type": "string", "enum": ["ok", "degraded"] },
    "game_running": { "type": "boolean" },
    "colony_loaded": { "type": "boolean" },
    "mod_version": { "type": "string" },
    "queue_depth": { "type": "integer", "minimum": 0 },
    "uptime_seconds": { "type": "integer", "minimum": 0 }
  }
}
```

### Error mapping
| Condition | HTTP | `error` |
|-----------|------|---------|
| Mod not initialized yet | 503 | `MOD_NOT_READY` |

---

## Endpoint: `GET /state`

### Purpose
Return current colony state.

### Query Parameters
| Name | Type | Default | Description |
|------|------|---------|-------------|
| `include_colonists` | boolean | `true` | Include detailed colonist list |
| `include_resources` | boolean | `true` | Include resources object |

### Success (`200`)
```json
{
  "colony": {
    "name": "New Arrivals",
    "wealth": 45230,
    "day": 42,
    "season": "Summer",
    "quadrum": "Jugust"
  },
  "colonists": [
    {
      "name": "Jenkins",
      "mood": 65,
      "health": 100,
      "current_activity": "Cooking",
      "skills": {
        "cooking": 12,
        "shooting": 5,
        "melee": 3
      },
      "traits": ["Industrious", "Gourmand"]
    }
  ],
  "resources": {
    "silver": 1200,
    "food": 45,
    "medicine": 12,
    "components": 8
  },
  "threats": {
    "active_raids": 0,
    "nearby_enemies": false,
    "toxic_fallout": false
  }
}
```

### Schema (response)
```json
{
  "type": "object",
  "required": ["colony", "threats"],
  "properties": {
    "colony": {
      "type": "object",
      "required": ["name", "wealth", "day", "season", "quadrum"],
      "properties": {
        "name": { "type": "string" },
        "wealth": { "type": "integer", "minimum": 0 },
        "day": { "type": "integer", "minimum": 0 },
        "season": { "type": "string" },
        "quadrum": { "type": "string" }
      }
    },
    "colonists": { "type": "array" },
    "resources": { "type": "object" },
    "threats": {
      "type": "object",
      "required": ["active_raids", "nearby_enemies", "toxic_fallout"],
      "properties": {
        "active_raids": { "type": "integer", "minimum": 0 },
        "nearby_enemies": { "type": "boolean" },
        "toxic_fallout": { "type": "boolean" }
      }
    }
  }
}
```

### Error mapping
| Condition | HTTP | `error` |
|-----------|------|---------|
| No colony/map loaded | 409 | `NO_COLONY_LOADED` |
| Game process unavailable | 503 | `GAME_NOT_RUNNING` |

---

## Endpoint: `POST /event`

### Purpose
Queue and trigger a game event.

### Request body
```json
{
  "event_type": "raid",
  "params": {
    "faction": "Pirate",
    "points": 500,
    "arrival_mode": "EdgeWalkIn"
  }
}
```

### Schema (request)
```json
{
  "type": "object",
  "required": ["event_type"],
  "properties": {
    "event_type": {
      "type": "string",
      "enum": [
        "raid", "manhunter", "cargo_pod", "wanderer",
        "solar_flare", "toxic_fallout", "psychic_drone",
        "trader", "inspiration"
      ]
    },
    "params": {
      "type": "object",
      "default": {}
    }
  },
  "additionalProperties": false
}
```

### Supported events and params
| Event Type | Description | Params |
|------------|-------------|--------|
| `raid` | Enemy raid | `faction?`, `points?`, `arrival_mode?` |
| `manhunter` | Manhunter pack | `animal_kind?`, `count?` |
| `cargo_pod` | Cargo pod drop | `contents?`, `count?` |
| `wanderer` | Wanderer joins | `pawn_kind?` |
| `solar_flare` | Solar flare | `duration_days?` |
| `toxic_fallout` | Toxic fallout | `duration_days?` |
| `psychic_drone` | Psychic drone | `gender?`, `level?` |
| `trader` | Trader arrives | `trader_kind?` |
| `inspiration` | Give inspiration | `colonist?`, `type?` |

### Success (`200`)
```json
{
  "success": true,
  "message": "Raid triggered successfully",
  "event_id": "evt_12345"
}
```

### Schema (response)
```json
{
  "type": "object",
  "required": ["success", "message"],
  "properties": {
    "success": { "type": "boolean", "const": true },
    "message": { "type": "string" },
    "event_id": { "type": "string" }
  }
}
```

### Error mapping
| Condition | HTTP | `error` |
|-----------|------|---------|
| Missing/invalid JSON body | 400 | `INVALID_REQUEST` |
| Unknown event type | 400 | `INVALID_EVENT` |
| Event blocked by cooldown | 429 | `RATE_LIMITED` |
| Colony not in valid state for event | 409 | `EVENT_FAILED` |
| Game unavailable | 503 | `GAME_NOT_RUNNING` |

---

## Endpoint: `POST /message`

### Purpose
Display a message in-game.

### Request body
```json
{
  "text": "Your cook is looking stressed...",
  "type": "info",
  "duration": 5
}
```

### Schema (request)
```json
{
  "type": "object",
  "required": ["text"],
  "properties": {
    "text": { "type": "string", "minLength": 1, "maxLength": 280 },
    "type": {
      "type": "string",
      "enum": ["info", "positive", "negative", "dramatic"],
      "default": "info"
    },
    "duration": { "type": "integer", "minimum": 1, "maximum": 30, "default": 5 }
  },
  "additionalProperties": false
}
```

### Success (`200`)
```json
{
  "success": true
}
```

### Error mapping
| Condition | HTTP | `error` |
|-----------|------|---------|
| Missing or empty `text` | 400 | `INVALID_REQUEST` |
| No colony loaded | 409 | `NO_COLONY_LOADED` |
| Game unavailable | 503 | `GAME_NOT_RUNNING` |

---

## MCP Tool Definitions (Current Bridge)

### `rimworld_get_status`
Get current colony status.

### `rimworld_trigger_event`
Trigger a game event.

### `rimworld_send_message`
Send an in-game message.

(These map directly to `/state`, `/event`, `/message`.)

---

## Canonical Error Codes

- `MOD_NOT_READY` — Mod initialized but HTTP layer not ready
- `GAME_NOT_RUNNING` — Game executable not detected
- `NO_COLONY_LOADED` — No save game loaded / no active map
- `INVALID_REQUEST` — Payload validation failed
- `INVALID_EVENT` — Unknown or unsupported event type
- `EVENT_FAILED` — Event could not execute in current game conditions
- `RATE_LIMITED` — Cooldown active / too many requests