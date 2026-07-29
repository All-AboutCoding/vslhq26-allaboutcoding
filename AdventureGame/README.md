# Obe-One CLI Adventure

A tiny text-based adventure RPG for the command line, built in .NET 10.
Storyline and enemies are generated dynamically by a **local Ollama** model,
and the end-of-game reward image is created by **Pollinations.ai** (no API key needed).

## Prerequisites

1. **.NET 10 SDK** (already installed via Visual Studio 2026).
2. **Ollama** — https://ollama.com/download
   - After installing, open a terminal and run:
	 ```
	 ollama pull llama3.2
	 ```
   - Make sure Ollama is running (it usually auto-starts as a background service).
3. **Internet connection** for the reward image (Pollinations.ai).

If Ollama isn't running the game still works — it falls back to built-in
storylines per theme so you can always play.

## Run

From the repo root:

```
dotnet run --project AdventureGame
```

## How to play

1. Enter your hero's name.
2. Pick a class: Warrior, Mage, or Archer.
3. Pick a theme: Fantasy, Sci-Fi, Horror (or type your own).
4. Fight 3 escalating enemies. Each turn choose:
   - **A**ttack — deal damage
   - **D**efend — halve the next incoming hit
   - **S**pecial — a powerful class ability (limited uses)
5. HP carries over between fights.
6. Win or lose, a 4K 16:9 reward image is generated and saved to your **My Pictures**
   folder (you can pick a different folder at the prompt).
7. Play again or exit.

## Project layout

- `Program.cs` — entry point + replay loop
- `Game.cs` — full playthrough orchestration + combat loop
- `Player.cs` — player stats, class presets, special abilities
- `Enemy.cs` — enemy model
- `LlmService.cs` — Ollama HTTP calls + JSON parsing + fallback data
- `ImageService.cs` — Pollinations.ai image download + save-to-disk
