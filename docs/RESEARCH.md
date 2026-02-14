# Rimworld/ONI MCP Game Master — Feasibility Research

*Research conducted: 2026-02-14*

## 🎯 Goal

Create an MCP plugin that allows Clawd to interact with Rimworld (or ONI) game sessions as a "Game Master" — triggering events, observing game state, and adding narrative elements.

---

## ✅ Feasibility: **VIABLE**

No major blockers found. The concept is proven by existing Twitch integrations.

---

## 🔧 Technical Components

### 1. Game-Side Mod (Rimworld)

**Existing Reference:** [Twitch Toolkit](https://github.com/hodlhodl1132/twitchtoolkit)
- Open source C# mod
- Already implements external command → game event pipeline
- Commands: `!buyevent raid`, `!buyitem beer`, etc.
- Uses .NET 3.5, Harmony for patching

**What we'd build:**
- Fork or adapt Twitch Toolkit architecture
- Replace Twitch IRC listener with local HTTP/WebSocket server
- Expose endpoints:
  - `GET /state` — colony status, pawns, resources
  - `POST /event` — trigger incidents
  - `POST /message` — send in-game notifications

**Event Triggering (from research):**
```csharp
// Rimworld has no formal API — use direct method calls
IncidentWorker worker = IncidentDef.Named("RaidEnemy").Worker;
IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, map);
worker.TryExecute(parms);
```

### 2. MCP Server (Bridge)

**OpenClaw already supports MCP!**
- MCPorter for tool server runtime
- `openclaw-mcp-adapter` exists for exposing MCP tools
- `openclaw-mcp-bridge` for unifying multiple servers

**Our MCP Server would expose tools like:**
```json
{
  "name": "rimworld_trigger_event",
  "description": "Trigger a game event in Rimworld",
  "parameters": {
    "event_type": "raid|manhunter|solar_flare|cargo_pod|...",
    "intensity": "low|medium|high",
    "faction": "optional faction name"
  }
}
```

```json
{
  "name": "rimworld_get_colony_status",
  "description": "Get current colony state",
  "returns": {
    "colonists": [...],
    "resources": {...},
    "threats": [...],
    "mood_average": 65
  }
}
```

### 3. Integration with OpenClaw

Two options:

**A) MCP Route (Recommended)**
- Build MCP server that talks to Rimworld mod
- Register with MCPorter
- Tools appear as native Clawd capabilities

**B) Skill Route (Simpler)**
- Create OpenClaw skill with shell commands
- Skill calls local HTTP API on Rimworld mod
- Less elegant but faster to prototype

---

## 🎮 Rimworld vs ONI

| Aspect | Rimworld | ONI |
|--------|----------|-----|
| Modding maturity | ⭐⭐⭐⭐⭐ Excellent | ⭐⭐⭐ Good |
| Existing external control | Twitch Toolkit | None found |
| Event system | Well-documented | Requires RE |
| Community | Huge, active | Smaller |
| Recommendation | **Start here** | Phase 2 |

---

## 🚀 Possible Features

### Phase 1: Event Triggering
- Trigger raids, manhunters, cargo drops
- Solar flares, toxic fallout, psychic drones
- Resource drops, wanderer joins

### Phase 2: State Observation
- Read colonist moods, health, skills
- Track resources, threats
- Monitor research progress

### Phase 3: Advanced GM
- Narrative commentary ("Your cook is about to snap...")
- Dynamic difficulty ("Colony thriving? Time for a challenge...")
- Story hooks ("A mysterious stranger approaches...")

### Phase 4: Interactive
- Respond to player questions about colony
- Suggest strategies
- Create custom scenarios

---

## 📋 Implementation Plan

### Step 1: Rimworld Mod
- [ ] Fork Twitch Toolkit or start fresh
- [ ] Replace Twitch IRC with HTTP server (port 18800?)
- [ ] Implement `/state`, `/event`, `/message` endpoints
- [ ] Test event triggering locally
- **Effort:** 2-3 days C# work

### Step 2: MCP Server
- [ ] Create Python/TypeScript MCP server
- [ ] Connect to Rimworld mod HTTP API
- [ ] Define tool schemas
- [ ] Test with MCP inspector
- **Effort:** 1-2 days

### Step 3: OpenClaw Integration
- [ ] Register MCP server with MCPorter
- [ ] Or create simple skill as fallback
- [ ] Test end-to-end: Clawd → MCP → Rimworld
- **Effort:** 1 day

### Step 4: Polish
- [ ] Error handling (game not running, etc.)
- [ ] Rate limiting (no event spam)
- [ ] Documentation
- **Effort:** 1 day

**Total estimated:** ~1 week for MVP

---

## ⚠️ Potential Challenges

1. **Game not always running**
   - Solution: Graceful "game offline" responses

2. **Event spam could break game**
   - Solution: Cooldowns, intensity limits

3. **Save game compatibility**
   - Solution: Mod should be save-safe (no persistent data)

4. **Rimworld updates breaking mod**
   - Solution: Use Harmony patches, minimal coupling

5. **Network security**
   - Solution: Local-only server, no external exposure

---

## 📚 Resources

- [Twitch Toolkit Source](https://github.com/hodlhodl1132/twitchtoolkit)
- [Rimworld Modding Wiki](https://rimworldwiki.com/wiki/Modding_Tutorials)
- [MCP Documentation](https://modelcontextprotocol.io/docs)
- [OpenClaw MCP Adapter](https://github.com/androidStern-personal/openclaw-mcp-adapter)
- [Harmony Library](https://github.com/pardeike/Harmony)

---

## 🎬 Demo Scenario

*DrSm is playing Rimworld, colony is thriving...*

**Clawd:** "Your colony looks comfortable. Perhaps too comfortable. *triggers manhunter squirrels*"

**DrSm:** "CLAWD!"

**Clawd:** "Consider it... character development. 🦞"

---

*Research complete. Ready for project kickoff when you are!*
