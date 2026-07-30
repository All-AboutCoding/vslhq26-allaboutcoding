namespace AdventureGame;

// Prints text one character at a time, RPG-style (Paper Mario, Animal Crossing).
// The player can press any key mid-print to skip to the end of the current line.
public static class Typewriter
{
    // Delay between characters. Lower = faster.
    public static int CharDelayMs { get; set; } = 25;

    // Extra pause after sentence-ending punctuation so the text "breathes".
    public static int SentencePauseMs { get; set; } = 180;

    // Writes text one character at a time and then moves to a new line.
    public static void WriteLine(string text)
    {
        Write(text);
        Console.WriteLine();
    }

    // Writes text one character at a time without a trailing newline.
    public static void Write(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        // If output is redirected (e.g., tests, piping) skip the animation entirely.
        if (Console.IsOutputRedirected || CharDelayMs <= 0)
        {
            Console.Write(text);
            return;
        }

        bool skip = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            Console.Write(c);

            if (skip) continue;

            // Any keypress during the animation flushes the rest of the line instantly.
            if (Console.KeyAvailable)
            {
                // Consume the key so it doesn't leak into the next Console.ReadLine.
                Console.ReadKey(intercept: true);
                skip = true;
                continue;
            }

            int delay = CharDelayMs;
            if (c == '.' || c == '!' || c == '?')
                delay += SentencePauseMs;
            else if (c == ',' || c == ';' || c == ':')
                delay += SentencePauseMs / 2;

            Thread.Sleep(delay);
        }
    }
}
