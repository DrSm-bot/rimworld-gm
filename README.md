# 🎮 Rimworld Game Master

**AI-powered Game Master for Rimworld via OpenClaw Skill (MVP) → MCP (Production)**

Let your AI assistant become a mischievous (or helpful) Game Master, triggering events, observing your colony, and adding narrative flavor to your Rimworld experience.

---

## 🎯 Vision

Imagine playing Rimworld while your AI assistant watches along:

> **Clawd:** "Your colony looks comfortable. Perhaps *too* comfortable..."
>
> *A manhunter pack of squirrels appears on the horizon*
>
> **You:** "CLAWD!"
>
> **Clawd:** "Consider it... character development. 🦞"

---

## 🏗️ Architecture

```
┌─────────────┐     HTTP      ┌─────────────┐   Skill/MCP   ┌─────────────┐
│  Rimworld   │◄────────────►│ Local API   │◄──────────────►│  OpenClaw   │
│    Mod      │  localhost    │   Bridge    │                │   Agent     │
└─────────────┘               └─────────────┘                └─────────────┘
      │                             │                              │
      ▼                             ▼                              ▼
 Game Events                 Contract + Safety             "Trigger a raid!"
 Colony State                Error Handling                "How's the colony?"
 In-Game Messages            Tests                         "Send encouragement"
```

---

## 📦 Components

### `/mod` — Rimworld Mod (C#)
The game-side component that:
- Runs a local HTTP server (port 18800)
- Exposes colony state via `/state`
- Accepts event commands via `/event`
- Displays AI messages in-game via `/message`

### `/mcp-server` — MCP Bridge (Python)
The bridge that:
- Connects to the Rimworld mod's HTTP API
- Exposes MCP tools for AI agents
- Handles errors gracefully (game offline, etc.)

### `/docs` — Documentation
- `docs/RESEARCH.md` — feasibility + risks
- `docs/API.md` — endpoint contract + status/error mapping
- `docs/BLUEPRINT.md` — Phase-1 implementation architecture

### `/scripts` — Test Utilities
- `scripts/test-api.py` — contract checks (mock + real server)

---

## 🚀 Delivery Strategy

1. **MVP via OpenClaw Skill** (fast iteration, prove loop)  
2. **Production via MCP Server** (portable + future-proof)

---

## 📋 Status

**Current Phase:** ✅ Phase 1 Complete — Ready for Testing

- [x] Feasibility research (`docs/RESEARCH.md`)
- [x] Architecture design
- [x] API specification draft (`docs/API.md`)
- [x] Mod scaffolding (`mod/`)
- [x] MCP scaffolding (`mcp-server/`)
- [x] Phase 1 mod implementation (HTTP server + queue + handlers)
- [ ] Skill MVP integration
- [ ] MCP production integration

---

## 🛠️ Tech Stack

| Component | Technology |
|-----------|------------|
| Rimworld Mod | C# / .NET 3.5 / Harmony |
| Skill/MCP Bridge | Python |
| Protocol | Local HTTP (MVP), MCP (Production) |

---

## 🙏 Acknowledgments

- [Twitch Toolkit](https://github.com/hodlhodl1132/twitchtoolkit) — Inspiration and reference implementation
- [Rimworld Modding Wiki](https://rimworldwiki.com/wiki/Modding_Tutorials) — Documentation
- [Model Context Protocol](https://modelcontextprotocol.io/) — Integration standard

---

## 📄 License

MIT License — See [LICENSE](LICENSE)

---

*Built with 🦞 by Clawd & DrSm*