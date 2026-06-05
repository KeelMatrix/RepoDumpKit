namespace RepoDumpKit;

internal static class SolutionPathService
{
    public static string GetSolutionPath(string[] args)
    {
        string configFilePath = Path.Combine(AppContext.BaseDirectory, AppSettings.ConfigFileName);

        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            string argumentPath = args[0].Trim();

            if (!Directory.Exists(argumentPath))
            {
                Console.WriteLine($"Command-line path is invalid: {argumentPath}");
                return string.Empty;
            }

            SavePath(configFilePath, argumentPath);
            return argumentPath;
        }

        string? existingPath = ReadSavedPath(configFilePath);

        if (!string.IsNullOrWhiteSpace(existingPath))
        {
            Console.WriteLine($"Saved path: {existingPath}");
            Console.Write("Press Enter to use it, or type a new path: ");
            string? typed = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(typed))
            {
                return existingPath;
            }

            string newTyped = typed.Trim();

            while (!Directory.Exists(newTyped))
            {
                Console.Write("Invalid path. Enter a valid full path: ");
                newTyped = (Console.ReadLine() ?? string.Empty).Trim();
            }

            SavePath(configFilePath, newTyped);
            return newTyped;
        }

        while (true)
        {
            Console.WriteLine("Enter the full path to the solution or repository folder:");
            string candidate = (Console.ReadLine() ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(candidate) && Directory.Exists(candidate))
            {
                SavePath(configFilePath, candidate);
                return candidate;
            }

            Console.WriteLine("Invalid path or path does not exist. Please try again.");
        }
    }

    private static string? ReadSavedPath(string configFilePath)
    {
        if (!File.Exists(configFilePath))
        {
            return null;
        }

        try
        {
            string existingPath = File.ReadAllText(configFilePath).Trim();

            if (Directory.Exists(existingPath))
            {
                return existingPath;
            }

            Console.WriteLine($"Stored path '{existingPath}' not found or is invalid.");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading config file: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private static void SavePath(string configFilePath, string path)
    {
        try
        {
            File.WriteAllText(configFilePath, path);
            Console.WriteLine($"Path saved to {configFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving path to config file: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
