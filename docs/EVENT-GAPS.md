# EVENT-GAPS.md

## Purpose

Track current event coverage vs. Rimworld storyteller capabilities, and define a practical expansion order.

Status note:
- This is planning/documentation only.
- Event expansion implementation is intentionally lower priority than AI Storyteller work.

---

## 1) Currently Implemented (v0.1)

These are currently mapped and callable through `/event`:

- `raid` -> `RaidEnemy`
- `solar_flare` -> `SolarFlare`
- `cold_snap` -> `ColdSnap`
- `manhunter` -> `ManhunterPack`
- `cargo_pod` -> `ResourcePodCrash`

Current coverage: **5 core events**.

---

## 2) Missing Events (Categorized)

## Threats

High gameplay impact incidents not yet exposed:

- Infestation
- Mech Cluster
- Psychic Ship / Poison Ship variants
- Siege / Breach raid variants
- Sapper-style raid variants

## Environment / World Conditions

- Toxic Fallout
- Volcanic Winter
- Eclipse
- Heat Wave
- Cold Snap variants and duration controls

## Positive / Utility

- Wanderer Joins
- Trader Visits (different trader types)
- Resource pod variants (targeted categories)
- Inspiration triggers (pawn-targeted)
- Quest helper/seed events (where practical)

## Misc / Campaign-Adjacent

- Caravan-related incidents
- Faction relation shifts / diplomatic events
- Utility incidents tied to storyteller pacing

---

## 3) Priority Recommendation (for later implementation)

## Priority A (highest value, lowest ambiguity)

1. `toxic_fallout`
2. `volcanic_winter`
3. `eclipse`
4. `wanderer`
5. `trader`

Reason:
- Strong narrative/control impact
- Relatively clear user intent
- Good balance of challenge and flavor without deep edge-case logic

## Priority B (high impact, moderate complexity)

1. `mech_cluster`
2. `infestation`
3. `psychic_ship`
4. Raid variants (`siege`, `breach`, `sapper`)

Reason:
- Important for parity with storyteller threat variety
- Needs tighter validation and safety tuning

## Priority C (advanced / optional)

1. Quest-linked incidents
2. Caravan-specific incidents
3. Faction relation manipulation

Reason:
- Bigger integration complexity
- More side-effects and design decisions needed

---

## 4) Technical Notes (IncidentDef mapping)

Mapping is handled in `mod/Source/Handlers/EventHandler.cs`.

Current known defNames in codebase:

- `RaidEnemy`
- `SolarFlare`
- `ColdSnap`
- `ManhunterPack`
- `ResourcePodCrash`

### Implementation guidance for new mappings

- Validate event key before cooldown accounting.
- Keep dangerous events behind policy checks (`enableDangerousEvents`).
- Preserve current error contract:
  - `INVALID_EVENT`
  - `EVENT_BLOCKED`
  - `RATE_LIMITED`
  - `EVENT_FAILED`
  - `NO_COLONY_LOADED`

### Safety guidance

- Default behavior should remain safe/predictable.
- New high-impact events should be categorized as dangerous by default.
- Prefer explicit intensity/points constraints over open-ended parameters.

---

## Appendix: Storyteller Parity Reality Check

Current system is intentionally focused (small reliable surface).

Not yet parity-complete with vanilla storyteller breadth, but already strong for:
- controlled AI-driven narrative sessions
- supervised challenge injection
- remote play augmentation

This is acceptable for MVP/Phase 3a and aligns with current priorities.
