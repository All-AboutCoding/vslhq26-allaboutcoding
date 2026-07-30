namespace AdventureGame;

// Orchestrates one full playthrough: setup -> 3 encounters -> ending + reward image.
// Written as a plain sequential method so the flow reads top-to-bottom.
public static class Game
{
    private static readonly Random Rng = new Random();

    // Run one complete game. Returns nothing — Program.cs handles the replay loop.
    public static async Task PlayAsync()
    {
        Console.Clear();
        PrintBanner();

        // ---- 1. Gather player info ----
        string name = Prompt("Enter your hero's name: ", defaultValue: "Hero");
        ClassType classType = PromptClass();
        string theme = PromptTheme();

        Player player = Player.CreateForClass(name, classType);

        // ---- 2. Ask the LLM for a themed adventure ----
        Console.WriteLine();
        Console.WriteLine("Consulting the loremaster for your adventure...");
        Adventure adventure = await LlmService.GenerateAdventureAsync(theme, classType, name);

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Typewriter.WriteLine(adventure.Intro);
        Console.ResetColor();
        Console.WriteLine();
        Pause();

        // ---- 3. Fight 3 encounters, HP carries over ----
        bool victory = true;
        for (int i = 0; i < adventure.Enemies.Count; i++)
        {
            // Narrative interlude before each encounter to lengthen the storyline.
            if (i < adventure.Interludes.Count)
            {
                string interlude = adventure.Interludes[i];
                if (!string.IsNullOrWhiteSpace(interlude))
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Typewriter.WriteLine(interlude);
                    Console.ResetColor();
                    Console.WriteLine();
                    Pause();
                }
            }

            Enemy enemy = adventure.Enemies[i];
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"--- Encounter {i + 1} of 3: {enemy.Name} ---");
            Console.ResetColor();
            Typewriter.WriteLine(enemy.Description);
            Console.WriteLine();

            RunCombat(player, enemy);

            if (player.IsDead)
            {
                victory = false;
                break;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"You defeated the {enemy.Name}!  (HP: {player.HP}/{player.MaxHP})");
            Console.ResetColor();
        }

        // ---- 4. Ending narrative ----
        Console.WriteLine();
        Console.ForegroundColor = victory ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine(victory ? "*** VICTORY ***" : "*** DEFEAT ***");
        Console.ResetColor();

        string outcome = await LlmService.GenerateOutcomeNarrativeAsync(theme, name, victory);
        Typewriter.WriteLine(outcome);
        Console.WriteLine();

        // ---- 5. Reward image ----
        // Tone words swap based on the outcome per the requirement.
        string tone = victory ? "bright, epic, triumphant, golden light" : "dark, gloomy, somber, shadowy";
        string imagePrompt =
            $"{theme} scene of a {classType} named {name}, {tone}, cinematic, highly detailed, 4k, 16:9";

        string? savedPath = await ImageService.GenerateAndSaveAsync(imagePrompt);
        if (savedPath != null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Reward image saved to: {savedPath}");
            Console.ResetColor();
        }
    }

    // Turn-based combat loop between the player and one enemy.
    // Player picks Attack / Defend / Special each turn; enemy then attacks.
    private static void RunCombat(Player player, Enemy enemy)
    {
        while (!player.IsDead && !enemy.IsDead)
        {
            // Show current status.
            string potionLabel = player.HasHealthPotion ? "available" : "used";
            Console.WriteLine($"[You: {player.HP}/{player.MaxHP} HP   |   {enemy.Name}: {enemy.HP} HP   |   {player.SpecialName} left: {player.SpecialUsesRemaining}   |   Potion: {potionLabel}]");
            string actionMenu = player.HasHealthPotion
                ? "Choose: (A)ttack  (D)efend  (S)pecial  (H)eal"
                : "Choose: (A)ttack  (D)efend  (S)pecial";
            Console.WriteLine(actionMenu);
            Console.Write("> ");

            string? choice = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(choice)) continue;

            switch (choice[0])
            {
                case 'h':
                    // Health potion consumes the player's turn — the enemy still retaliates after.
                    if (!player.HasHealthPotion)
                    {
                        Console.WriteLine("You have no health potion remaining — pick again.");
                        continue;
                    }
                    int healed = player.UseHealthPotion(Rng);
                    if (healed <= 0)
                    {
                        Console.WriteLine("You're already at full health — the potion would be wasted. Pick again.");
                        player.HasHealthPotion = true; // Refund since nothing was gained.
                        continue;
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"You quaff a health potion and recover {healed} HP.  (HP: {player.HP}/{player.MaxHP})");
                    Console.ResetColor();
                    break;

                case 'a':
                    // Basic attack with small variance.
                    int atk = player.Attack + Rng.Next(0, 4);
                    enemy.TakeDamage(atk);
                    Console.WriteLine($"You strike for {atk} damage.");
                    break;

                case 'd':
                    // Defending halves the next incoming hit and skips your attack.
                    player.IsDefending = true;
                    Console.WriteLine("You brace yourself, ready to take the next blow.");
                    break;

                case 's':
                    if (player.SpecialUsesRemaining <= 0)
                    {
                        Console.WriteLine("You have no special uses remaining — pick again.");
                        continue; // No enemy turn if the player made an invalid choice.
                    }
                    int sdmg = player.UseSpecial(enemy, Rng);
                    Console.WriteLine($"You unleash {player.SpecialName} for {sdmg} damage!");
                    break;

                default:
                    Console.WriteLine("Unknown command — pick A, D, S, or H.");
                    continue;
            }

            // Enemy retaliates if it survived.
            if (enemy.IsDead) break;

            // Enemy may unleash its one-shot special at any point during its turn.
            // Trigger: the first turn the enemy drops below half HP — a desperate strike.
            // Using the special consumes the enemy's turn (no normal attack this round).
            if (enemy.HasSpecial && enemy.MaxHP > 0 && enemy.HP * 2 < enemy.MaxHP)
            {
                int sdmg = enemy.UseSpecial(player, Rng);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{enemy.Name} unleashes {enemy.SpecialName} for {sdmg} damage!");
                Console.ResetColor();
                Console.WriteLine();
                continue;
            }

            int incoming = enemy.Attack + Rng.Next(0, 3);
            int actual = player.TakeDamage(incoming);
            Console.WriteLine($"{enemy.Name} hits you for {actual} damage.");
            Console.WriteLine();
        }
    }

    // ---- Small input helpers, kept inline for simplicity ----

    private static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("=============================================");
        Console.WriteLine("            FANTASY AI ADVENTURE");
        Console.WriteLine("=============================================");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static string Prompt(string label, string defaultValue)
    {
        Console.Write(label);
        string? s = Console.ReadLine();
        return string.IsNullOrWhiteSpace(s) ? defaultValue : s.Trim();
    }

    private static ClassType PromptClass()
    {
        while (true)
        {
            Console.WriteLine("Choose your class:");
            Console.WriteLine("  1) Warrior  (HP 40, Atk 8, Def 3, Power Strike)");
            Console.WriteLine("  2) Mage     (HP 25, Atk 10, Def 1, Fireball)");
            Console.WriteLine("  3) Archer   (HP 30, Atk 9, Def 2, Piercing Shot)");
            Console.Write("> ");
            string? c = Console.ReadLine()?.Trim();
            if (c == "1") return ClassType.Warrior;
            if (c == "2") return ClassType.Mage;
            if (c == "3") return ClassType.Archer;
            Console.WriteLine("Please pick 1, 2, or 3.");
        }
    }

    private static string PromptTheme()
    {
        Console.WriteLine("Choose a theme:");
        Console.WriteLine("  1) Fantasy");
        Console.WriteLine("  2) Sci-Fi");
        Console.WriteLine("  3) Horror");
        Console.WriteLine("  (or type your own)");
        Console.Write("> ");
        string? c = Console.ReadLine()?.Trim();
        return c switch
        {
            "1" or "" or null => "Fantasy",
            "2" => "Sci-Fi",
            "3" => "Horror",
            _ => c!
        };
    }

    private static void Pause()
    {
        Console.WriteLine("(press Enter to continue)");
        Console.ReadLine();
    }
}
