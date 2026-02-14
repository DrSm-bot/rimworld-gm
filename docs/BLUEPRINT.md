# Phase 1 Implementation Blueprint

This blueprint defines the architecture for the Rimworld mod HTTP API implementation with thread-safe game interaction.

## Goals

- Keep network I/O off the game thread
- Execute all RimWorld API calls on Unity main thread
- Provide clear separation of concerns and testable boundaries

---

## Core Pattern: Command Queue + MainThread Pump

```
HTTP Thread(s)
  -> parse/validate request
  -> enqueue GameCommand
  -> wait (bounded timeout) for result

Main Thread (GameComponent tick)
  -> dequeue GameCommand
  -> execute via RimWorld APIs
  -> complete result (success/error)
```

**Rule:** No direct `Find.*`, `Map`, `IncidentWorker`, `Messages`, etc. from listener threads.

---

## Proposed `mod/Source/` Layout

```
mod/Source/
  RimworldGM.cs                    # bootstrap + constants
  Http/HttpServer.cs               # HttpListener lifecycle + routing
  Http/RequestParser.cs            # JSON parse + schema-level validation
  Http/HttpResponseWriter.cs       # status + JSON envelopes
  Core/CommandDispatcher.cs        # thread-safe queue + request correlation
  Core/GameComponent_RimworldGM.cs # main-thread pump (GameComponent)
  Core/GameCommand.cs              # command models/enums
  Core/CommandResult.cs            # success/error result envelope
  Handlers/StateSnapshotBuilder.cs # read-only colony snapshot
  Handlers/EventHandler.cs         # IncidentDef mapping + execution
  Handlers/MessageHandler.cs       # in-game message rendering
  Util/Json.cs                     # shared serializer helpers
  Util/CooldownGate.cs             # event cooldown/rate limit
```

---

## Class Responsibilities

## `HttpServer`
- Start/stop `HttpListener`
- Route:
  - `GET /health`
  - `GET /state`
  - `POST /event`
  - `POST /message`
- Convert request into `GameCommand`
- Enqueue via `CommandDispatcher`
- Await result with timeout (e.g. 2000-5000ms)

## `CommandDispatcher`
- Thread-safe queue (`ConcurrentQueue<GameCommand>`)
- `Enqueue(command) -> requestId`
- Result registry keyed by `requestId`
- `TryComplete(requestId, result)` called by main-thread pump

## `GameComponent_RimworldGM`
- Runs each game tick
- Drains queue with per-tick safety cap (e.g. max 10 commands)
- Calls `EventHandler`, `MessageHandler`, `StateSnapshotBuilder`
- Completes command result back into dispatcher

## `StateSnapshotBuilder`
- Reads colony, colonists, resources, threats
- Supports flags (`include_colonists`, `include_resources`)
- Returns serializable DTOs only

## `EventHandler`
- Maps public `event_type` to `IncidentDef`
- Validates params + defaults
- Applies cooldown gate
- Returns structured success/error

## `MessageHandler`
- Maps API `type` to Rimworld message/letter API
- Supports `info|positive|negative|dramatic`

---

## Request Lifecycle (Example: `POST /event`)

1. HTTP thread receives payload.
2. `RequestParser` validates JSON and required fields.
3. Creates `GameCommand { kind=TriggerEvent, args=... }`.
4. `CommandDispatcher.Enqueue()` stores pending request.
5. `GameComponent_RimworldGM` dequeues on main thread.
6. `EventHandler.TriggerEvent()` executes against RimWorld API.
7. Result (`success/error + code + message`) set in dispatcher.
8. HTTP thread returns mapped HTTP status + JSON body.

---

## Error/Status Mapping

Follow `docs/API.md` contract:
- Validation failures -> `400`
- Invalid game state -> `409`
- Cooldown -> `429`
- Game/mod unavailable -> `503`
- Success -> `200`

---

## Safety & Stability Rules

- Bounded command queue size (e.g. 256), reject with `429` when saturated
- Bounded per-request wait timeout
- Cooldown by event type + global cooldown
- Never throw raw exceptions to HTTP client; return envelope
- Log with request id for traceability

---

## Phase 1 Acceptance Checklist

- [ ] Main menu: `/health` works, `/state` returns `NO_COLONY_LOADED`
- [ ] Active colony: `/state` returns non-empty payload
- [ ] `/event` success for at least 5 event types
- [ ] `/message` renders all 4 styles
- [ ] Repeated burst calls do not freeze game thread
- [ ] Start/quit game does not leave stale listener thread

---

## Notes for Phase 2+ (Skill/MCP)

- Keep HTTP contract stable; skill and MCP should remain thin adapters
- Avoid introducing endpoint churn before MVP feedback
- Add richer observability only after baseline stability