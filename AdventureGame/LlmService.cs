using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdventureGame;

// Holds the storyline + 3 enemies returned from the LLM (or the fallback).
public class Adventure
{
    public string Intro { get; set; } = "";
    public List<Enemy> Enemies { get; set; } = new();
}

// Talks to a locally-running Ollama instance to generate the storyline
// and enemy roster. Falls back to hardcoded content on any failure so
// the game is playable even if Ollama isn't installed / running.
public static class LlmService
{
    // Ollama's default local endpoint. No API key required.
    private const string OllamaUrl = "http://localhost:11434/api/generate";

    // Small, fast default model. User must have run: `ollama pull llama3.2`
    private const string Model = "llama3.2";

    // Shared HttpClient with a generous timeout (local models can be slow on cold start).
    private static readonly HttpClient Http = new HttpClient
    {
        Timeout = TimeSpan.FromMinutes(2)
    };

    // Ask the LLM for a themed adventure. Never throws — returns fallback on error.
    public static async Task<Adventure> GenerateAdventureAsync(string theme, ClassType playerClass, string playerName)
    {
        // Prompt is crafted to force strict JSON output so we can deserialize safely.
        string prompt =
            $"You are a game master creating a short {theme} themed adventure for a {playerClass} named {playerName}. " +
            "Respond ONLY with valid JSON in this exact shape (no markdown, no commentary): " +
            "{\"intro\":\"2-3 sentence storyline intro\"," +
            "\"enemies\":[{\"name\":\"...\",\"description\":\"1 sentence\",\"hp\":15-30,\"attack\":4-9}," +
            "{\"name\":\"...\",\"description\":\"1 sentence\",\"hp\":20-35,\"attack\":5-10}," +
            "{\"name\":\"...\",\"description\":\"1 sentence\",\"hp\":25-45,\"attack\":6-12}]}. " +
            "Enemies must escalate in difficulty and fit the theme.";

        try
        {
            // Ollama /api/generate request body. `format:"json"` tells it to
            // constrain output to JSON. `stream:false` gives us one response.
            var request = new
            {
                model = Model,
                prompt = prompt,
                stream = false,
                format = "json"
            };

            HttpResponseMessage resp = await Http.PostAsJsonAsync(OllamaUrl, request);
            resp.EnsureSuccessStatusCode();

            // Ollama wraps the model's response text in a JSON envelope: { "response": "...json string..." }
            OllamaResponse? envelope = await resp.Content.ReadFromJsonAsync<OllamaResponse>();
            if (envelope == null || string.IsNullOrWhiteSpace(envelope.Response))
                return Fallback(theme);

            // Parse the inner JSON payload (the actual adventure data).
            AdventureDto? dto = JsonSerializer.Deserialize<AdventureDto>(
                envelope.Response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dto == null || dto.Enemies == null || dto.Enemies.Count < 3)
                return Fallback(theme);

            // Convert DTO -> real Enemy objects, taking only the first 3.
            Adventure adventure = new Adventure { Intro = dto.Intro ?? "" };
            foreach (var e in dto.Enemies.Take(3))
            {
                adventure.Enemies.Add(new Enemy
                {
                    Name = string.IsNullOrWhiteSpace(e.Name) ? "Nameless Horror" : e.Name,
                    Description = e.Description ?? "",
                    HP = e.HP > 0 ? e.HP : 20,
                    Attack = e.Attack > 0 ? e.Attack : 5
                });
            }
            return adventure;
        }
        catch (Exception ex)
        {
            // Print a friendly warning and use fallback content so the game still runs.
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[LLM unavailable: {ex.Message}. Using built-in adventure.]");
            Console.ResetColor();
            return Fallback(theme);
        }
    }

    // Optional short flavor text for the ending. Falls back to a canned line.
    public static async Task<string> GenerateOutcomeNarrativeAsync(string theme, string playerName, bool victory)
    {
        string prompt = victory
            ? $"Write ONE short triumphant sentence (max 25 words) celebrating {playerName}'s victory in a {theme} adventure. Plain text only."
            : $"Write ONE short somber sentence (max 25 words) mourning {playerName}'s defeat in a {theme} adventure. Plain text only.";

        try
        {
            var request = new { model = Model, prompt = prompt, stream = false };
            HttpResponseMessage resp = await Http.PostAsJsonAsync(OllamaUrl, request);
            resp.EnsureSuccessStatusCode();
            OllamaResponse? envelope = await resp.Content.ReadFromJsonAsync<OllamaResponse>();
            return envelope?.Response?.Trim() ?? DefaultOutcome(victory, playerName);
        }
        catch
        {
            return DefaultOutcome(victory, playerName);
        }
    }

    private static string DefaultOutcome(bool victory, string name) =>
        victory
            ? $"{name} stands victorious as the last echoes of battle fade into legend."
            : $"{name} falls, their story ending in shadow — but perhaps another hero will rise.";

    // Hardcoded backup adventures per common theme so the demo always works.
    private static Adventure Fallback(string theme)
    {
        string t = (theme ?? "").ToLowerInvariant();
        if (t.Contains("sci"))
        {
            return new Adventure
            {
                Intro = "Adrift on a derelict starship, emergency lights flicker as something stirs in the dark corridors.",
                Enemies = new List<Enemy>
                {
                    new Enemy { Name = "Malfunctioning Drone", Description = "A sparking security bot with a cracked lens.", HP = 18, Attack = 5 },
                    new Enemy { Name = "Xeno Stalker",         Description = "A sleek predator that hunts by heat.",       HP = 26, Attack = 7 },
                    new Enemy { Name = "Rogue AI Core",        Description = "A pulsing crystal wired into the ship's soul.", HP = 38, Attack = 10 },
                }
            };
        }
        if (t.Contains("horror"))
        {
            return new Adventure
            {
                Intro = "The old manor breathes around you. Every hallway you walk seems to grow longer than the last.",
                Enemies = new List<Enemy>
                {
                    new Enemy { Name = "Whispering Shade",  Description = "A shadow that speaks in your own voice.", HP = 16, Attack = 6 },
                    new Enemy { Name = "Bone Marionette",   Description = "Strung on invisible threads, it dances closer.", HP = 24, Attack = 8 },
                    new Enemy { Name = "The Hollow Host",   Description = "The manor's true owner, made of doors and teeth.", HP = 40, Attack = 11 },
                }
            };
        }
        // Default: fantasy.
        return new Adventure
        {
            Intro = "The kingdom's last hope walks into the Whispering Wood, sword drawn, heart steady.",
            Enemies = new List<Enemy>
            {
                new Enemy { Name = "Goblin Scout",  Description = "A wiry green raider with a rusted dagger.", HP = 15, Attack = 5 },
                new Enemy { Name = "Dire Wolf",     Description = "Amber eyes gleam between the trees.",       HP = 25, Attack = 7 },
                new Enemy { Name = "Ancient Wyrm",  Description = "Its scales shimmer with old, cruel magic.", HP = 40, Attack = 11 },
            }
        };
    }

    // ---- DTOs used only for JSON deserialization ----

    private class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }

    private class AdventureDto
    {
        [JsonPropertyName("intro")]
        public string? Intro { get; set; }

        [JsonPropertyName("enemies")]
        public List<EnemyDto>? Enemies { get; set; }
    }

    private class EnemyDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        [JsonPropertyName("hp")]
        public int HP { get; set; }
        [JsonPropertyName("attack")]
        public int Attack { get; set; }
    }
}
