using AdventureGame;

// Top-level entry point.
// Runs one full adventure, then asks the player if they want to play again.
// The game itself lives in Game.PlayAsync(); this file is only the outer loop.
while (true)
{
    await Game.PlayAsync();

    Console.WriteLine();
    Console.Write("Play again? (Y/N): ");
    string? again = Console.ReadLine()?.Trim().ToLowerInvariant();

    // Anything other than "y" / "yes" exits the game.
    if (again != "y" && again != "yes")
    {
        Console.WriteLine("Thanks for playing!");
        break;
    }
}
