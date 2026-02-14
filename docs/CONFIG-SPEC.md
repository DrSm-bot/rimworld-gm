# CONFIG-SPEC.md

## Goal

Define a safe-by-default configuration model for RimworldGM networking:

- **Default:** local-only (`127.0.0.1`), no token required
- **Power mode (LAN):** explicit opt-in (`0.0.0.0` or LAN IP), token required, rate limiting required

This spec is for review before implementation.

---

## File Location

`RimworldGM/Config/Settings.xml` (or equivalent mod config path used by RimWorld mod settings persistence)

---

## Settings.xml Schema (v1)

```xml
<RimworldGMSettings>
  <network>
    <bindAddress>127.0.0.1</bindAddress>
    <port>18800</port>
    <authToken></authToken>
    <allowedCidrs></allowedCidrs>
    <allowLan>false</allowLan>
  </network>

  <security>
    <requireTokenForLan>true</requireTokenForLan>
    <maxRequestsPerMinute>60</maxRequestsPerMinute>
    <maxRequestBodyBytes>16384</maxRequestBodyBytes>
    <enableDangerousEvents>false</enableDangerousEvents>
  </security>

  <ui>
    <showLanWarningOnce>true</showLanWarningOnce>
    <showStartupSummary>true</showStartupSummary>
  </ui>
</RimworldGMSettings>
```

### Field Definitions

#### `network.bindAddress`
- Type: string
- Default: `127.0.0.1`
- Meaning: Interface to bind HTTP listener to.
- Allowed (v1):
  - `127.0.0.1` (safe default)
  - `0.0.0.0` (all interfaces, LAN mode)
  - specific LAN IP (optional support in v1)

#### `network.port`
- Type: int
- Default: `18800`
- Valid range: `1024-65535`

#### `network.authToken`
- Type: string
- Default: empty
- Required when LAN mode is active.
- Header: `Authorization: Bearer <token>` (preferred) or `X-RimworldGM-Token: <token>`.

#### `network.allowedCidrs`
- Type: string (comma-separated CIDR list)
- Default: empty (= no CIDR filter)
- Example: `192.168.178.0/24,10.0.0.0/8`

#### `network.allowLan`
- Type: bool
- Default: `false`
- Intent gate for LAN exposure (must be true for non-local bind).

#### `security.requireTokenForLan`
- Type: bool
- Default: `true`
- Must remain true for v1 implementation.

#### `security.maxRequestsPerMinute`
- Type: int
- Default: `60`
- Applied globally (v1). Optional per-endpoint override later.

#### `security.maxRequestBodyBytes`
- Type: int
- Default: `16384` (16 KB)
- Request bodies exceeding this return `413` or `400` with `INVALID_REQUEST`.

#### `security.enableDangerousEvents`
- Type: bool
- Default: `false`
- If false, block event types tagged as high-impact (e.g. strong raids), return `EVENT_BLOCKED`.

#### `ui.showLanWarningOnce`
- Type: bool
- Default: `true`
- Show one-time warning popup when LAN mode is enabled.

#### `ui.showStartupSummary`
- Type: bool
- Default: `true`
- Log/status message on startup with bind/port/mode.

---

## Defaults / Mode Behavior

## Local Mode (default)

Conditions:
- `bindAddress = 127.0.0.1`
- `allowLan = false`

Rules:
- Token optional (not required)
- CIDR filter ignored
- API only reachable locally on same machine

## LAN Mode (opt-in)

Conditions:
- `bindAddress != 127.0.0.1` OR `allowLan = true`

Hard rules:
1. `allowLan` must be true
2. `authToken` must be non-empty
3. `maxRequestsPerMinute` must be >= 10
4. `maxRequestBodyBytes` must be >= 1024

If rules are violated:
- Server does **not** start in LAN mode
- Fallback to local-only mode and log explicit warning

---

## Request Validation / Security Rules

1. **Auth check (LAN only):**
   - reject unauthorized with `401 UNAUTHORIZED`
2. **Rate limit:**
   - when exceeded: `429 RATE_LIMITED`
3. **Body size limit:**
   - oversize: `413 PAYLOAD_TOO_LARGE` (or `400 INVALID_REQUEST` fallback)
4. **Event guardrails:**
   - enforce cooldown and optional dangerous-event block
5. **Logging hygiene:**
   - never log full token
   - never log full message payloads in debug by default

---

## Error Codes (Additions)

- `UNAUTHORIZED` — missing/invalid token for LAN mode
- `EVENT_BLOCKED` — event blocked by safety policy (`enableDangerousEvents=false`)
- `LAN_MODE_INVALID_CONFIG` — LAN requested but required settings missing

---

## Security Disclaimers (README text draft)

### Short Warning (README)

> LAN mode exposes your Rimworld control API to other devices on your network.
> Only enable it if you understand the risk. Use a strong token and trusted network.

### Full Warning (README)

> **Security Notice:**
> By enabling LAN mode (`bindAddress=0.0.0.0`), you open an API that can trigger in-game events.
> Use this only on trusted networks, set a strong token, and consider CIDR allowlisting.
> The project defaults to localhost to avoid accidental exposure.

### In-Game Warning (one-time popup text)

> RimworldGM LAN mode is enabled.
> Your control API is reachable from other devices on your network.
> Ensure `authToken` is set and only trusted clients can connect.

---

## Example Configs

## A) Safe Local Default

```xml
<RimworldGMSettings>
  <network>
    <bindAddress>127.0.0.1</bindAddress>
    <port>18800</port>
    <authToken></authToken>
    <allowedCidrs></allowedCidrs>
    <allowLan>false</allowLan>
  </network>
  <security>
    <requireTokenForLan>true</requireTokenForLan>
    <maxRequestsPerMinute>60</maxRequestsPerMinute>
    <maxRequestBodyBytes>16384</maxRequestBodyBytes>
    <enableDangerousEvents>false</enableDangerousEvents>
  </security>
</RimworldGMSettings>
```

## B) LAN Mode (Power User)

```xml
<RimworldGMSettings>
  <network>
    <bindAddress>0.0.0.0</bindAddress>
    <port>18800</port>
    <authToken>CHANGE_ME_LONG_RANDOM_TOKEN</authToken>
    <allowedCidrs>192.168.178.0/24</allowedCidrs>
    <allowLan>true</allowLan>
  </network>
  <security>
    <requireTokenForLan>true</requireTokenForLan>
    <maxRequestsPerMinute>60</maxRequestsPerMinute>
    <maxRequestBodyBytes>16384</maxRequestBodyBytes>
    <enableDangerousEvents>false</enableDangerousEvents>
  </security>
</RimworldGMSettings>
```

---

## Implementation Notes (for next step)

1. Add settings loader with sane defaults + validation.
2. Enforce "LAN hard rules" before listener start.
3. Add token middleware in `HttpServer` for LAN mode.
4. Add CIDR allowlist check for client IP (optional in first LAN iteration if too much complexity).
5. Extend docs/API with auth + new errors.
6. Add startup log summary:
   - mode (local/lan)
   - bind address/port
   - token configured yes/no (never print token)

---

## Out of Scope (this spec)

- TLS/HTTPS termination inside mod
- Internet exposure / NAT traversal
- Full user account/auth model
- Cloud relay services

These are future concerns; v1 is LAN-only controlled exposure.
