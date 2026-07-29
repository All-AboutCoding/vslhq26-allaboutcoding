using System.Net.Http;

namespace AdventureGame;

// Downloads the end-of-game reward image from Pollinations.ai and
// saves it to disk. Pollinations is a free HTTP GET endpoint that
// returns an image for any given text prompt — no API key required.
public static class ImageService
{
    private static readonly HttpClient Http = new HttpClient
    {
        Timeout = TimeSpan.FromMinutes(3) // Image gen can take a bit.
    };

    // Build the URL, download the image bytes, ask the user where to save
    // it (defaulting to My Pictures), and write the file.
    // Returns the saved file path, or null on failure.
    public static async Task<string?> GenerateAndSaveAsync(string prompt)
    {
        try
        {
            // Pollinations spec: https://image.pollinations.ai/prompt/{prompt}?width=..&height=..
            // 4K 16:9 = 3840 x 2160. `nologo=true` removes the watermark.
            string encoded = Uri.EscapeDataString(prompt);
            string url = $"https://image.pollinations.ai/prompt/{encoded}?width=3840&height=2160&nologo=true";

            Console.WriteLine();
            Console.WriteLine("Generating your reward image (this can take 20-60 seconds)...");

            byte[] bytes = await Http.GetByteArrayAsync(url);

            // Ask the user where to save. Default = My Pictures.
            string defaultDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            Console.Write($"Save folder [{defaultDir}]: ");
            string? input = Console.ReadLine();
            string dir = string.IsNullOrWhiteSpace(input) ? defaultDir : input.Trim();

            // Make sure the folder exists — create if the user typed a new path.
            Directory.CreateDirectory(dir);

            string filename = $"AdventureReward_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            string fullPath = Path.Combine(dir, filename);

            await File.WriteAllBytesAsync(fullPath, bytes);
            return fullPath;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Image generation failed: {ex.Message}]");
            Console.ResetColor();
            return null;
        }
    }
}
