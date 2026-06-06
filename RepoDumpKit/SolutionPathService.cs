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

        IReadOnlyList<string> savedPaths = ReadSavedPaths(configFilePath);

        if (savedPaths.Count > 0)
        {
            Console.WriteLine("Recent paths:");

            for (int index = 0; index < savedPaths.Count; index++)
            {
                Console.WriteLine($"{index + 1}. {savedPaths[index]}");
            }

            Console.Write("Choose a number, press Enter for 1, or paste a new path: ");

            while (true)
            {
                string typed = (Console.ReadLine() ?? string.Empty).Trim();
                string selectedPath;

                if (string.IsNullOrWhiteSpace(typed))
                {
                    selectedPath = savedPaths[0];
                }
                else if (TryGetSavedPathByNumber(typed, savedPaths, out string? savedPath))
                {
                    selectedPath = savedPath;
                }
                else
                {
                    selectedPath = typed;
                }

                if (Directory.Exists(selectedPath))
                {
                    SavePath(configFilePath, selectedPath);
                    return selectedPath;
                }

                Console.Write("Invalid path. Choose a listed number or enter a valid full path: ");
            }
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

    private static IReadOnlyList<string> ReadSavedPaths(string configFilePath)
    {
        if (!File.Exists(configFilePath))
        {
            return [];
        }

        try
        {
            string[] storedPaths = File.ReadAllLines(configFilePath);
            List<string> validPaths = [];
            HashSet<string> seenPaths = new(AppSettings.PathComparer);

            foreach (string storedPath in storedPaths)
            {
                string path = storedPath.Trim();

                if (string.IsNullOrWhiteSpace(path) || !seenPaths.Add(path))
                {
                    continue;
                }

                if (Directory.Exists(path))
                {
                    validPaths.Add(path);

                    if (validPaths.Count == AppSettings.MaxSavedSolutionPaths)
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine($"Stored path '{path}' not found or is invalid.");
                }
            }

            return validPaths;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading config file: {ex.GetType().Name}: {ex.Message}");
            return [];
        }
    }

    private static bool TryGetSavedPathByNumber(string input, IReadOnlyList<string> savedPaths, out string? savedPath)
    {
        savedPath = null;

        if (!int.TryParse(input, out int selectedNumber))
        {
            return false;
        }

        if (selectedNumber < 1 || selectedNumber > savedPaths.Count)
        {
            return false;
        }

        savedPath = savedPaths[selectedNumber - 1];
        return true;
    }

    private static void SavePath(string configFilePath, string path)
    {
        try
        {
            string fullPath = Path.GetFullPath(path.Trim());
            List<string> savedPaths = ReadSavedPaths(configFilePath)
                .Where(savedPath => !string.Equals(savedPath, fullPath, AppSettings.PathComparison))
                .Prepend(fullPath)
                .Take(AppSettings.MaxSavedSolutionPaths)
                .ToList();

            File.WriteAllLines(configFilePath, savedPaths);
            Console.WriteLine($"Path saved to {configFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving path to config file: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
