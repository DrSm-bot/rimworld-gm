# API Specification

## Rimworld Mod HTTP API

The mod exposes a local HTTP server on `localhost:18800`.

---

## Endpoints

### `GET /health`

Health check endpoint.

**Response:**
```json
{
  "status": "ok",
  "game_running": true,
  "colony_loaded": true,
  "mod_version": "0.1.0"
}
```

---

### `GET /state`

Get current colony state.

**Response:**
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

---

### `POST /event`

Trigger a game event.

**Request:**
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

**Supported Events:**
| Event Type | Description | Params |
|------------|-------------|--------|
| `raid` | Enemy raid | `faction`, `points`, `arrival_mode` |
| `manhunter` | Manhunter pack | `animal_kind`, `count` |
| `cargo_pod` | Cargo pod drop | `contents`, `count` |
| `wanderer` | Wanderer joins | `pawn_kind` |
| `solar_flare` | Solar flare | `duration_days` |
| `toxic_fallout` | Toxic fallout | `duration_days` |
| `psychic_drone` | Psychic drone | `gender`, `level` |
| `trader` | Trader arrives | `trader_kind` |
| `inspiration` | Give inspiration | `colonist`, `type` |

**Response:**
```json
{
  "success": true,
  "message": "Raid triggered successfully",
  "event_id": "evt_12345"
}
```

---

### `POST /message`

Display a message in-game.

**Request:**
```json
{
  "text": "Your cook is looking stressed...",
  "type": "info",
  "duration": 5
}
```

**Message Types:**
- `info` — Neutral notification
- `positive` — Good news (green)
- `negative` — Bad news (red)
- `dramatic` — Center screen dramatic text

**Response:**
```json
{
  "success": true
}
```

---

## MCP Tool Definitions

### `rimworld_get_status`

Get current colony status.

```json
{
  "name": "rimworld_get_status",
  "description": "Get the current status of the Rimworld colony including colonists, resources, and threats",
  "inputSchema": {
    "type": "object",
    "properties": {
      "include_colonists": {
        "type": "boolean",
        "description": "Include detailed colonist info",
        "default": true
      },
      "include_resources": {
        "type": "boolean", 
        "description": "Include resource counts",
        "default": true
      }
    }
  }
}
```

### `rimworld_trigger_event`

Trigger a game event.

```json
{
  "name": "rimworld_trigger_event",
  "description": "Trigger an event in Rimworld (raids, cargo drops, weather, etc.)",
  "inputSchema": {
    "type": "object",
    "properties": {
      "event_type": {
        "type": "string",
        "enum": ["raid", "manhunter", "cargo_pod", "wanderer", "solar_flare", "toxic_fallout", "psychic_drone", "trader", "inspiration"],
        "description": "Type of event to trigger"
      },
      "intensity": {
        "type": "string",
        "enum": ["low", "medium", "high"],
        "description": "Event intensity/difficulty",
        "default": "medium"
      },
      "target_colonist": {
        "type": "string",
        "description": "Target colonist name (for inspiration events)"
      }
    },
    "required": ["event_type"]
  }
}
```

### `rimworld_send_message`

Send an in-game message.

```json
{
  "name": "rimworld_send_message",
  "description": "Display a message to the player in Rimworld",
  "inputSchema": {
    "type": "object",
    "properties": {
      "text": {
        "type": "string",
        "description": "Message text to display"
      },
      "style": {
        "type": "string",
        "enum": ["info", "positive", "negative", "dramatic"],
        "default": "info"
      }
    },
    "required": ["text"]
  }
}
```

---

## Error Handling

All endpoints return errors in this format:

```json
{
  "success": false,
  "error": "GAME_NOT_RUNNING",
  "message": "Rimworld is not currently running"
}
```

**Error Codes:**
- `GAME_NOT_RUNNING` — Game executable not detected
- `NO_COLONY_LOADED` — No save game loaded
- `INVALID_EVENT` — Unknown event type
- `EVENT_FAILED` — Event could not be triggered (conditions not met)
- `RATE_LIMITED` — Too many requests (cooldown active)
