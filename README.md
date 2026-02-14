# 🎮 Rimworld Game Master

**AI-powered Game Master for Rimworld via MCP (Model Context Protocol)**

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
┌─────────────┐     HTTP      ┌─────────────┐     MCP      ┌─────────────┐
│  Rimworld   │◄────────────►│ MCP Server  │◄────────────►│  OpenClaw   │
│    Mod      │  localhost    │  (Bridge)   │              │   Agent     │
└─────────────┘               └─────────────┘              └─────────────┘
      │                             │                            │
      ▼                             ▼                            ▼
 Game Events               Tool Definitions              "Trigger a raid!"
 Colony State              State Translation             "How's the colony?"
 In-Game Messages          Error Handling               "Send encouragement"
```

---

## 📦 Components

### `/mod` — Rimworld Mod (C#)
The game-side component that:
- Runs a local HTTP server (port 18800)
- Exposes colony state via `/state`
- Accepts event commands via `/event`
- Displays AI messages in-game via `/message`

### `/mcp-server` — MCP Bridge (Python/TypeScript)
The bridge that:
- Connects to the Rimworld mod's HTTP API
- Exposes MCP tools for AI agents
- Handles errors gracefully (game offline, etc.)

### `/docs` — Documentation
Design docs, API specs, and guides.

---

## 🚀 Planned Features

### Phase 1: Event Triggering
- [ ] Trigger incidents (raids, manhunters, cargo drops)
- [ ] Environmental events (solar flare, toxic fallout)
- [ ] Positive events (wanderer joins, cargo pod)

### Phase 2: State Observation
- [ ] Read colonist info (mood, health, skills)
- [ ] Track resources and wealth
- [ ] Monitor threats and map conditions

### Phase 3: Game Master Mode
- [ ] Narrative commentary
- [ ] Dynamic difficulty suggestions
- [ ] Story hooks and drama creation

### Phase 4: Interactive Play
- [ ] Answer questions about colony state
- [ ] Suggest strategies
- [ ] Custom scenario creation

---

## 🛠️ Tech Stack

| Component | Technology |
|-----------|------------|
| Rimworld Mod | C# / .NET 3.5 / Harmony |
| MCP Server | Python or TypeScript |
| Protocol | MCP (Model Context Protocol) |
| Local Comm | HTTP REST / WebSocket |

---

## 📋 Status

**Current Phase:** 📝 Documentation & Planning

- [x] Feasibility research
- [x] Architecture design
- [ ] API specification
- [ ] Mod scaffolding
- [ ] MCP server scaffolding
- [ ] MVP implementation

---

## 🙏 Acknowledgments

- [Twitch Toolkit](https://github.com/hodlhodl1132/twitchtoolkit) — Inspiration and reference implementation
- [Rimworld Modding Wiki](https://rimworldwiki.com/wiki/Modding_Tutorials) — Documentation
- [Model Context Protocol](https://modelcontextprotocol.io/) — AI integration standard

---

## 📄 License

MIT License — See [LICENSE](LICENSE)

---

*Built with 🦞 by Clawd & DrSm*
