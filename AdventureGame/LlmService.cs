using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdventureGame;

// Holds the storyline + 3 enemies returned from the LLM (or the fallback).
// Interludes provide narrative bridges between encounters — one entry per
// encounter, shown just before the corresponding fight to extend the storyline.
public class Adventure
{
    public string Intro { get; set; } = "";
    public List<Enemy> Enemies { get; set; } = new();
    public List<string> Interludes { get; set; } = new();
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
        Timeout = TimeSpan.FromMinutes(3)
    };

    // Ask the LLM for a themed adventure. Never throws — returns fallback on error.
    public static async Task<Adventure> GenerateAdventureAsync(string theme, ClassType playerClass, string playerName)
    {
        // Prompt is crafted to force strict JSON output so we can deserialize safely.
        string prompt =
            $"You are a game master creating a {theme} themed adventure for a {playerClass} named {playerName}. " +
            "Respond ONLY with valid JSON in this exact shape (no markdown, no commentary): " +
            "{\"intro\":\"3-4 sentence storyline intro that sets the world, stakes, and hero's motivation\"," +
            "\"interludes\":[\"3-4 sentence scene leading to the first encounter\"," +
            "\"3-4 sentence scene between the first and second encounters that deepens the story and reveals a new detail\"," +
            "\"3-4 sentence scene between the second and third encounters that raises the stakes toward the climax\"]," +
            "\"enemies\":[{\"name\":\"...\",\"description\":\"1 sentence\",\"hp\":15-30,\"attack\":4-10}," +
            "{\"name\":\"...\",\"description\":\"1 sentence\",\"hp\":20-30,\"attack\":5-10}," +
            "{\"name\":\"...\",\"description\":\"1 sentence\",\"hp\":25-40,\"attack\":6-10}]}. " +
            "Enemies must escalate in difficulty and fit the theme. Interludes must reference the hero by name and connect narratively to the enemy that follows.";

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
                int hp = e.HP > 0 ? e.HP : 20;
                adventure.Enemies.Add(new Enemy
                {
                    Name = string.IsNullOrWhiteSpace(e.Name) ? "Nameless Horror" : e.Name,
                    Description = e.Description ?? "",
                    HP = hp,
                    MaxHP = hp,
                    Attack = e.Attack > 0 ? e.Attack : 5,
                    SpecialName = string.IsNullOrWhiteSpace(e.Special) ? "Savage Blow" : e.Special
                });
            }

            // Copy up to 3 interludes; pad with empties so index-alignment with enemies is safe.
            if (dto.Interludes != null)
            {
                foreach (string s in dto.Interludes.Take(3))
                    adventure.Interludes.Add(s ?? "");
            }
            while (adventure.Interludes.Count < adventure.Enemies.Count)
                adventure.Interludes.Add("");

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
                Intro = "Adrift on a derelict starship, emergency lights flicker as something stirs in the dark corridors. Life support is failing, the crew is gone, and the only voice on comms is your own. Whatever killed this ship is still aboard — and it knows you're here.",
                Enemies = new List<Enemy>
                {
                    new Enemy { Name = "Malfunctioning Drone", Description = "A sparking security bot with a cracked lens.", HP = 18, MaxHP = 18, Attack = 5,  SpecialName = "Overload Discharge" },
                    new Enemy { Name = "Xeno Stalker",         Description = "A sleek predator that hunts by heat.",       HP = 26, MaxHP = 26, Attack = 7,  SpecialName = "Pouncing Rend" },
                    new Enemy { Name = "Rogue AI Core",        Description = "A pulsing crystal wired into the ship's soul.", HP = 38, MaxHP = 38, Attack = 10, SpecialName = "Neural Cascade" },
                },
                Interludes = new List<string>
                {
                    "You push off from the airlock and drift into a corridor lit only by the red pulse of the ship's alarm. The deck plates groan beneath your boots as if the ship itself is uneasy. Ahead, a servo whines, spins up, and swivels toward you.",
                    "Sparks still hiss from the drone's shattered chassis when you find a crew log smeared across a console: 'Something is loose in the vents. It learns.' The lights die completely for three heartbeats. When they return, wet claw marks glisten on the bulkhead beside you.",
                    "You reach the reactor core to find the ship's AI has rewritten its own containment protocols. Screens flash your name in a language you don't remember teaching it. The core hums, brightens, and begins to speak — and it sounds, impossibly, pleased to finally meet you."
                }
            };
        }
        if (t.Contains("horror"))
        {
            return new Adventure
            {
                Intro = "The old manor breathes around you. Every hallway you walk seems to grow longer than the last. You came here to bury a family secret, but the house was waiting for a guest — and now the doors have chosen to close behind you.",
                Enemies = new List<Enemy>
                {
                    new Enemy { Name = "Whispering Shade",  Description = "A shadow that speaks in your own voice.", HP = 16, MaxHP = 16, Attack = 6,  SpecialName = "Soul-Echo" },
                    new Enemy { Name = "Bone Marionette",   Description = "Strung on invisible threads, it dances closer.", HP = 24, MaxHP = 24, Attack = 8,  SpecialName = "Puppet's Waltz" },
                    new Enemy { Name = "The Hollow Host",   Description = "The manor's true owner, made of doors and teeth.", HP = 40, MaxHP = 40, Attack = 11, SpecialName = "Devouring Threshold" },
                },
                Interludes = new List<string>
                {
                    "Portraits watch you from every wall, their eyes tracked with dust in a way that suggests they only recently stopped moving. In the parlor, a phonograph starts on its own, whispering a lullaby you half-remember from childhood. The shadow in the corner clears its throat — and answers in your own voice.",
                    "In the shade's dying echo you catch a name: your grandmother's. A locked nursery door drifts open at the end of the hall, revealing a cradle strung with too many strings. Something inside sits up, joints clicking, and turns its wooden face toward the sound of your breathing.",
                    "You find the house's true heart behind a wall of doors that will not open the same way twice. Every keyhole shows you a different room, and every room shows you a piece of yourself you'd rather not see. The floor tilts, the walls yawn, and the manor — hungry at last — steps forward to greet you."
                }
            };
        }
        // Default: fantasy.
        return new Adventure
        {
            Intro = "The kingdom's last hope walks into the Whispering Wood, sword drawn, heart steady. Word came at dawn that the old evil has stirred beneath the roots, and no army will march where you march now. Only you, and the trees, and whatever the trees have been hiding.",
            Enemies = new List<Enemy>
            {
                new Enemy { Name = "Goblin Scout",  Description = "A wiry green raider with a rusted dagger.", HP = 15, MaxHP = 15, Attack = 5,  SpecialName = "Backstab" },
                new Enemy { Name = "Dire Wolf",     Description = "Amber eyes gleam between the trees.",       HP = 25, MaxHP = 25, Attack = 7,  SpecialName = "Throat Lunge" },
                new Enemy { Name = "Ancient Wyrm",  Description = "Its scales shimmer with old, cruel magic.", HP = 40, MaxHP = 40, Attack = 11, SpecialName = "Dragon's Breath" },
            },
            Interludes = new List<string>
            {
                "The forest path narrows to a deer trail, and the deer trail narrows to nothing at all. You hear a giggle, sharp and mean, from the bramble to your left. A crooked shape drops from a branch and bares its teeth in what it clearly believes is a smile.",
                "You wipe the goblin's dagger on the moss and press deeper. The wood grows quiet — the wrong kind of quiet, the kind that means every small creature has already fled. Yellow eyes bloom in the dark between two trunks, then a second pair, then a shape big enough to blot out the moon.",
                "Beyond the wolf's lair the ground turns black and glassy, cooled to stone by some ancient heat. Bones the size of wagons jut from the earth, and the air tastes of iron and old smoke. Something vast uncoils in the ruin ahead, opens one slow, golden eye, and remembers what heroes taste like."
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

        [JsonPropertyName("interludes")]
        public List<string>? Interludes { get; set; }

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
        [JsonPropertyName("special")]
        public string? Special { get; set; }
    }
}
