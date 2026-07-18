# DEAD WALLS - V1 Narrative Premise

> **Status:** V1 canon - 2026-07-18
>
> **Scope:** World pitch, player role and opening copy. This document does not add a campaign, quests, factions, bosses or enemy variants.

## Canon premise

The world ended. The siege did not.

Beyond the Wall, the dead no longer arrive as an army; they arrive like weather. The last inhabited fortress survives because the Castle Heart can rekindle a failed stand from its first morning. The Heart restores the settlement, but it cannot preserve everything. Grave Essence and the run's technology are lost. Only Last Embers - the compressed memory of decisions that almost held - pass into the next stand.

The player is the fortress's **Steward**, not its champion. The Steward assigns the living, grows the garrison, spends Grave Essence through the Castle Heart and answers the Council while the Wall remains the only line between settlement and horde. There is no final wave and no promised victory. The purpose of a stand is to hold longer, learn more and leave stronger Last Embers behind.

## World pitch

Hold the last inhabited fortress against a horde that grows by number, not by monsters. Build its worker economy, field up to 1,000 archers and shape a different Castle Heart each stand; when the Wall falls, the Heart rekindles the siege and preserves the Last Embers of what you learned.

### Store-short version

Manage the last fortress by day and hold its Wall by night. Every fall reshapes the Castle Heart, and every failed stand leaves Last Embers for the next.

## Opening copy

| Surface | Final copy | Purpose |
|---|---|---|
| Product title | `DEAD WALLS` | Stable title. |
| Main-menu line | `THE WORLD ENDED. THE SIEGE DID NOT.` | Establishes the world and endless structure without exposition. |
| New-run action | `BEGIN THE STAND` | Gives a run its narrative name while remaining a clear action. |
| Continue action | `CONTINUE — DAY {N}` | Keeps an existing live run explicit. |
| Game Over title | `THE WALL HAS FALLEN` | Names the single fail state. |
| Game Over bridge | `THE RUN ENDS HERE. WHAT REMAINS WILL STRENGTHEN THE NEXT STAND.` | Connects death to meta progression. |
| Restart action | `BEGIN NEXT RUN` | Keeps the post-death transaction unambiguous. |

The main menu is the opening. V1 does not add a prologue modal, cutscene, narration or forced lore page. The player reads one line, chooses a clear action and reaches the simulation immediately. First-run onboarding remains mechanical and non-modal.

## Term bible

| Term | Canon meaning | Boundary |
|---|---|---|
| **The Steward** | The player's role: allocator of people, resources, defense and technology. | Not a named hero, monarch, archer or battlefield avatar. |
| **The Wall** | The fortress's only fail state and the physical focus of every stand. | No Gate/Core second life and no interior combat phase. |
| **A stand** | One complete attempt from the first morning until the Wall falls. | Runtime and telemetry may still use the technical word `run`. |
| **Castle Heart** | The relic beneath the fortress that converts Grave Essence into temporary run technology and rekindles a failed stand. | It is not a speaking character, god, prophecy or second health bar. |
| **Grave Essence** | Energy recovered from the current horde and spent only inside the current Castle Heart graph. | It does not survive death and is not meta currency. |
| **Last Embers** | Persistent memory left by a failed stand and spent on permanent meta upgrades. | They are not literal souls and do not replace Grave Essence. |
| **The Council** | The living settlement's authored, context-aware management decisions. | No free-form AI canon, named faction campaign or awareness of previous stands. |
| **The dead / the horde** | One visually consistent enemy mass whose threat comes from count and flow. | No boss, elite, origin reveal or taxonomy is required for V1. |

## Deliberate unknowns

V1 does not answer who built the Castle Heart, where the dead came from, what kingdom preceded the fortress or whether another settlement exists. These unknowns protect the game's scale and prevent Council events from inventing incompatible canon. Any future answer requires a new owner decision and a revision of this document.

## Tone guardrails

- Short, severe and concrete; never ornate fantasy exposition.
- Human stakes come from workers, the Council, the Wall and the size of the horde.
- No chosen-one language, prophecy, named villain or heroic power fantasy.
- No joke copy on death, Castle Heart purchases or Council consequences.
- Player-facing headline copy stays uppercase and should fit without scrolling.
- Council micro-stories may describe professions and immediate dilemmas, but may not create permanent factions or explain the world's origin.

## Implementation ownership

- `MainMenuSceneUI` owns the stable title, opening line and new-stand action constants.
- `MobileCastleSceneSetupWindow` applies the same copy to `MainMenuScene` and exposes an idempotent repair command.
- `MetaUpgradeCatalogSO.Presentation` continues to own death-screen and Last Embers copy.
- `NarrativePresentationTests` prevents the scene, runtime constants and this canon from drifting apart.
