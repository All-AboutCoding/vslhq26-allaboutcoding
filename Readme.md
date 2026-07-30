# Fantasy AI Adventure

A tiny text-based adventure RPG for the command line, built in .NET 10. Storylines, enemies, interludes, and custom character classes are generated dynamically by a **local Ollama** model. A 4K reward image is produced by **Pollinations.ai** at the end of every run.

## Team

- **Team name:** AllAboutCoding

### Members

- Tracy Rickman — @tracyrickman
- Ben Lopez — @blopez01

## Category

- **Primary:** Creative application
- **Secondary:** Azure OpenAI / LLM app

## Architecture

The application is a single-project .NET 10 console app (`AdventureGame`) organized into small, focused components:

- **`Program.cs`** — entry point and replay loop.
- **`Game.cs`** — orchestrates one full playthrough: setup → 3 encounters with narrative interludes → ending → reward image. Also owns the turn-based combat loop.
- **`Player.cs`** — player stats, class presets (Warrior / Mage / Archer / Custom), health potion, and class-specific special abilities.
- **`Enemy.cs`** — enemy model with HP, attack, and a one-shot signature move.
- **`LlmService.cs`** — talks to a locally-running Ollama instance (`llama3.2`) via `http://localhost:11434/api/generate`. Splits generation into several small, focused JSON prompts (intro, three enemies, three interludes, custom-class stats) instead of one large payload, which dramatically improves JSON reliability. Falls back to hand-authored content per piece on any failure.
- **`ImageService.cs`** — downloads and saves the end-of-game reward image from Pollinations.ai.
- **`Typewriter.cs`** — incremental text rendering for narrative sections.

Runtime flow: the user names their hero and picks a class (Warrior / Mage / Archer or a fully custom class whose stats and signature move are designed by the LLM). Ollama then produces the adventure — a themed intro, three escalating enemies, and three narrative interludes — over a series of small JSON calls. The player fights three turn-based encounters with HP carrying over, then Pollinations.ai renders a themed victory or defeat image and saves it to disk.

## Setup

**Prerequisites**

1. **.NET 10 SDK** (installed with Visual Studio 2026, or via https://dotnet.microsoft.com).
2. **Ollama** — https://ollama.com/download
   - After installing, pull the model used by the game:
     ```
     ollama pull llama3.2
     ```
   - Make sure Ollama is running (it usually auto-starts as a background service on `http://localhost:11434`).
3. **Internet connection** for the reward image (Pollinations.ai — no API key required).

No environment variables or configuration files are required.

**Run the game**

From the repo root:

```
dotnet run --project AdventureGame
```

If Ollama isn't running or a sub-prompt fails, the game still plays — the affected pieces fall back to built-in content per theme.
