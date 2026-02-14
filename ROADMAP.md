# 🗺️ Roadmap

## Strategy: Skill → MCP

**MVP via OpenClaw Skill** (fast iteration, proof of concept)  
**Production via MCP Server** (universal, future-proof)

---

## Project Docs

- Feasibility: `docs/RESEARCH.md`
- API Contract: `docs/API.md`
- Phase-1 Architecture Blueprint: `docs/BLUEPRINT.md`

---

## Phase 0: Foundation ✅
*Status: Complete*

- [x] Feasibility research
- [x] Architecture design
- [x] API specification (draft)
- [x] Repository setup
- [x] Basic scaffolding

---

## Phase 1: Rimworld Mod
*Status: Complete ✅*
*Estimate: 2-3 days implementation + 1 day integration testing*

The game-side component that exposes colony data and accepts commands.

### 1.1 HTTP Server + Dispatch
- [x] Implement `HttpListener` on port 18800
- [x] Parse and validate request payloads
- [x] Queue commands for main-thread execution (`CommandDispatcher`)
- [x] Graceful startup/shutdown

### 1.2 Endpoints
- [x] `GET /health` — Health check + queue stats
- [x] `GET /state` — Colony status (colonists, resources, threats)
- [x] `POST /event` — Trigger incidents
- [x] `POST /message` — Display in-game messages

### 1.3 Event System
- [x] Map event types to `IncidentDef`
- [x] Implement intensity → points calculation
- [x] Handle event failures gracefully
- [x] Add cooldowns to prevent spam

### 1.4 Testing
- [x] Run `scripts/test-api.py --mock`
- [x] Run `scripts/test-api.py --base-url http://localhost:18800`
- [x] Verify thread safety under repeated event calls
- [x] Test during gameplay (main menu, active colony, pause/speed changes)

**Deliverable:** Working Rimworld mod that responds to HTTP requests safely

---

## Phase 2: OpenClaw Skill (MVP)
*Status: Complete ✅*
*Estimate: 1 day*

Quick integration via shell commands for rapid testing.

### 2.1 CLI Tool
- [x] Create `rimworld-gm` CLI (bash or Python)
- [x] Commands: `status`, `event <type>`, `message <text>`
- [x] JSON output for easy parsing

### 2.2 Skill Definition
- [x] Create `SKILL.md` with usage examples
- [x] Document available events
- [x] Add to OpenClaw skills directory

### 2.3 Testing
- [x] Test via OpenClaw: "trigger a raid in Rimworld"
- [x] Test state queries: "how's my colony doing?"
- [x] Test messaging: "send encouragement to my colonists"

**Deliverable:** Working Skill that Clawd can use to interact with Rimworld

---

## Phase 3: Network Hardening + MCP (Production)
*Status: In Progress (3a complete, 3b pending)*
*Estimate: 2 days*

Universal integration via Model Context Protocol plus secure LAN operation.

### 3a LAN Mode Security (Complete ✅)
- [x] Configurable bind address + port
- [x] LAN mode explicit opt-in (`allowLan=true`)
- [x] Token auth in LAN mode (`Authorization: Bearer`)
- [x] Global rate limiting
- [x] Request body size limit
- [x] Dangerous event blocking policy

### 3.1 MCP Implementation (3b)
- [ ] Finalize tool schemas
- [ ] Implement tool handlers
- [ ] Add error handling (game offline, etc.)
- [ ] Add rate limiting

### 3.2 MCPorter Integration
- [ ] Register with OpenClaw's MCPorter
- [ ] Test tool discovery
- [ ] Verify end-to-end flow

### 3.3 Standalone Mode
- [ ] Test with MCP Inspector
- [ ] Document usage with Claude Desktop
- [ ] Test with other MCP clients

**Deliverable:** MCP server usable by any MCP-compatible client

---

## Phase 4: Game Master Features
*Status: Future*
*Estimate: Ongoing*

Advanced features that make the integration actually fun.

### 4.1 Smart Events
- [ ] Context-aware event selection
- [ ] "Colony thriving? Time for challenge"
- [ ] Narrative-driven triggers

### 4.2 Commentary
- [ ] Mood warnings ("Your cook is stressed...")
- [ ] Achievement recognition
- [ ] Dramatic narration mode

### 4.3 Interactive Play
- [ ] Answer questions about colony
- [ ] Strategy suggestions
- [ ] Custom scenario creation

### 4.4 ONI Support (Stretch)
- [ ] Research ONI modding
- [ ] Port core concepts
- [ ] Separate mod, shared MCP server

---

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-02-14 | Start with Skill, then MCP | Fast MVP, then polish |
| 2026-02-14 | Local HTTP only | Security, simplicity |
| 2026-02-14 | Rimworld first, ONI later | Better modding ecosystem |

---

## Success Metrics

### MVP (Phase 2 Complete)
- [ ] Can trigger at least 5 different events
- [ ] Can read basic colony status
- [ ] Works during normal gameplay
- [ ] No crashes or save corruption

### Production (Phase 3 Complete)
- [ ] Works with OpenClaw AND Claude Desktop
- [ ] Documented for public use
- [ ] <1s latency for commands
- [ ] Graceful offline handling

### Game Master (Phase 4)
- [ ] Actually fun to use
- [ ] Adds to gameplay experience
- [ ] Community feedback positive

---

## Timeline (Optimistic)

```
Week 1: Phase 1 (Mod) + Phase 2 (Skill MVP)
Week 2: Phase 3 (MCP) + Polish
Week 3+: Phase 4 (Features) as time allows
```

---

*Last updated: 2026-02-14*