namespace RepoDumpKit.Tests;

public sealed class SolutionPathServiceTests : IDisposable
{
    private readonly TextReader _originalIn = Console.In;
    private readonly TextWriter _originalOut = Console.Out;

    public void Dispose()
    {
        Console.SetIn(_originalIn);
        Console.SetOut(_originalOut);
        DeleteConfigFile();
    }

    [Fact]
    public void GetSolutionPath_ReturnsCommandLinePathAndSavesItToHistory()
    {
        DeleteConfigFile();
        using var temp = new TestDirectory();
        string path = temp.CreateDirectory("repo");
        using var output = new StringWriter();
        Console.SetOut(output);

        string result = SolutionPathService.GetSolutionPath([path]);

        Assert.Equal(path, result);
        string[] storedPaths = File.ReadAllLines(ConfigFilePath);
        Assert.Equal(new[] { Path.GetFullPath(path) }, storedPaths);
        Assert.Contains("Path saved to", output.ToString());
    }

    [Fact]
    public void GetSolutionPath_ReturnsEmptyStringForInvalidCommandLinePath()
    {
        DeleteConfigFile();
        using var temp = new TestDirectory();
        string missingPath = Path.Combine(temp.RootPath, "missing");
        using var output = new StringWriter();
        Console.SetOut(output);

        string result = SolutionPathService.GetSolutionPath([missingPath]);

        Assert.Equal(string.Empty, result);
        Assert.False(File.Exists(ConfigFilePath));
        Assert.Contains("Command-line path is invalid", output.ToString());
    }

    [Fact]
    public void GetSolutionPath_UsesFirstSavedPathWhenUserPressesEnter()
    {
        DeleteConfigFile();
        using var temp = new TestDirectory();
        string first = temp.CreateDirectory("first");
        string second = temp.CreateDirectory("second");
        File.WriteAllLines(ConfigFilePath, [first, second]);
        Console.SetIn(new StringReader(Environment.NewLine));
        using var output = new StringWriter();
        Console.SetOut(output);

        string result = SolutionPathService.GetSolutionPath([]);

        Assert.Equal(first, result);
        string[] storedPaths = File.ReadAllLines(ConfigFilePath);
        Assert.Equal(Path.GetFullPath(first), storedPaths[0]);
        Assert.Equal(Path.GetFullPath(second), storedPaths[1]);
        Assert.Contains("Recent paths:", output.ToString());
        Assert.Contains("1. " + first, output.ToString());
    }

    [Fact]
    public void GetSolutionPath_UsesNumberedSavedPathAndMovesItToTop()
    {
        DeleteConfigFile();
        using var temp = new TestDirectory();
        string first = temp.CreateDirectory("first");
        string second = temp.CreateDirectory("second");
        File.WriteAllLines(ConfigFilePath, [first, second]);
        Console.SetIn(new StringReader("2" + Environment.NewLine));
        using var output = new StringWriter();
        Console.SetOut(output);

        string result = SolutionPathService.GetSolutionPath([]);

        Assert.Equal(second, result);
        string[] storedPaths = File.ReadAllLines(ConfigFilePath);
        Assert.Equal(Path.GetFullPath(second), storedPaths[0]);
        Assert.Equal(Path.GetFullPath(first), storedPaths[1]);
    }

    [Fact]
    public void GetSolutionPath_AcceptsPastedPathWhenSavedPathsExist()
    {
        DeleteConfigFile();
        using var temp = new TestDirectory();
        string first = temp.CreateDirectory("first");
        string pasted = temp.CreateDirectory("pasted");
        File.WriteAllLines(ConfigFilePath, [first]);
        Console.SetIn(new StringReader(pasted + Environment.NewLine));
        using var output = new StringWriter();
        Console.SetOut(output);

        string result = SolutionPathService.GetSolutionPath([]);

        Assert.Equal(pasted, result);
        string[] storedPaths = File.ReadAllLines(ConfigFilePath);
        Assert.Equal(Path.GetFullPath(pasted), storedPaths[0]);
        Assert.Equal(Path.GetFullPath(first), storedPaths[1]);
    }

    [Fact]
    public void GetSolutionPath_RePromptsUntilUserProvidesValidSelectionOrPath()
    {
        DeleteConfigFile();
        using var temp = new TestDirectory();
        string valid = temp.CreateDirectory("valid");
        File.WriteAllLines(ConfigFilePath, [valid]);
        Console.SetIn(new StringReader("99" + Environment.NewLine + valid + Environment.NewLine));
        using var output = new StringWriter();
        Console.SetOut(output);

        string result = SolutionPathService.GetSolutionPath([]);

        Assert.Equal(valid, result);
        Assert.Contains("Invalid path. Choose a listed number or enter a valid full path:", output.ToString());
    }

    [Fact]
    public void GetSolutionPath_WithNoSavedPaths_AsksForPathAndSavesValidInput()
    {
        DeleteConfigFile();
        using var temp = new TestDirectory();
        string repo = temp.CreateDirectory("repo");
        Console.SetIn(new StringReader(repo + Environment.NewLine));
        using var output = new StringWriter();
        Console.SetOut(output);

        string result = SolutionPathService.GetSolutionPath([]);

        Assert.Equal(repo, result);
        Assert.Equal(new[] { Path.GetFullPath(repo) }, File.ReadAllLines(ConfigFilePath));
        Assert.Contains("Enter the full path", output.ToString());
    }

    [Fact]
    public void GetSolutionPath_IgnoresMissingSavedPathsAndAcceptsNewInput()
    {
        DeleteConfigFile();
        using var temp = new TestDirectory();
        string missing = Path.Combine(temp.RootPath, "missing");
        string valid = temp.CreateDirectory("valid");
        File.WriteAllLines(ConfigFilePath, [missing]);
        Console.SetIn(new StringReader(valid + Environment.NewLine));
        using var output = new StringWriter();
        Console.SetOut(output);

        string result = SolutionPathService.GetSolutionPath([]);

        Assert.Equal(valid, result);
        Assert.Equal(new[] { Path.GetFullPath(valid) }, File.ReadAllLines(ConfigFilePath));
        Assert.Contains("not found or is invalid", output.ToString());
    }

    [Fact]
    public void GetSolutionPath_TrimsHistoryToMaximumNumberOfSavedPaths()
    {
        DeleteConfigFile();
        using var temp = new TestDirectory();
        var savedPaths = new List<string>();

        for (int i = 0; i < AppSettings.MaxSavedSolutionPaths; i++)
        {
            savedPaths.Add(temp.CreateDirectory($"old-{i}"));
        }

        string newest = temp.CreateDirectory("newest");
        File.WriteAllLines(ConfigFilePath, savedPaths);
        string result = SolutionPathService.GetSolutionPath([newest]);

        Assert.Equal(newest, result);
        string[] storedPaths = File.ReadAllLines(ConfigFilePath);
        Assert.Equal(AppSettings.MaxSavedSolutionPaths, storedPaths.Length);
        Assert.Equal(Path.GetFullPath(newest), storedPaths[0]);
        Assert.DoesNotContain(Path.GetFullPath(savedPaths[^1]), storedPaths);
    }

    [Fact]
    public void GetSolutionPath_RemovesDuplicatePathFromExistingHistoryWhenSaving()
    {
        DeleteConfigFile();
        using var temp = new TestDirectory();
        string first = temp.CreateDirectory("first");
        string second = temp.CreateDirectory("second");
        File.WriteAllLines(ConfigFilePath, [first, second, first]);

        string result = SolutionPathService.GetSolutionPath([second]);

        Assert.Equal(second, result);
        string[] storedPaths = File.ReadAllLines(ConfigFilePath);
        Assert.Equal(new[] { Path.GetFullPath(second), Path.GetFullPath(first) }, storedPaths);
    }

    private static string ConfigFilePath => Path.Combine(AppContext.BaseDirectory, AppSettings.ConfigFileName);

    private static void DeleteConfigFile()
    {
        try
        {
            File.Delete(ConfigFilePath);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }
}
