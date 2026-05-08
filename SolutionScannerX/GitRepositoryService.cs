using System.Diagnostics;
using System.Text;

namespace SolutionScannerX;

internal static class GitRepositoryService
{
    public static async Task<GitIgnoredResult> GetIgnoredFilesFromGit(string solutionPath)
    {
        var ignoredItems = new HashSet<string>(AppSettings.PathComparer);

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = AppSettings.PreservedGitIgnoredCommand,
            WorkingDirectory = solutionPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        Console.WriteLine($"Running 'git {processStartInfo.Arguments}' in '{solutionPath}'...");

        ProcessCapture result = await ProcessRunner.RunProcess(processStartInfo);

        foreach (string line in PathUtility.ReadOutputLines(result.Output))
        {
            string normalizedPath = PathUtility.NormalizeGitPath(line);

            if (!string.IsNullOrWhiteSpace(normalizedPath))
            {
                ignoredItems.Add(normalizedPath);
            }
        }

        bool noIgnoredPathsExit = result.ExitCode == 1 && ignoredItems.Count == 0;
        bool successfulEnough = result.ExitCode == 0 || noIgnoredPathsExit || ignoredItems.Count > 0;

        if (successfulEnough)
        {
            Console.WriteLine($"Found {ignoredItems.Count} ignored items via git.");
        }
        else
        {
            Console.WriteLine($"Git command exited with error code {FormattingUtility.FormatExitCode(result.ExitCode)}.");

            if (!string.IsNullOrWhiteSpace(result.ErrorOutput))
            {
                Console.WriteLine($"Git error output: {result.ErrorOutput.Trim()}");
            }

            Console.WriteLine("Warning: ignored files could not be determined reliably.");
        }

        return new GitIgnoredResult(ignoredItems, result.ExitCode, result.ErrorOutput);
    }

    public static async Task<HashSet<string>> GetTrackedGitIgnoreFiles(string solutionPath)
    {
        var trackedGitIgnoreFiles = new HashSet<string>(AppSettings.PathComparer);

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/d /c \"git ls-files\"",
            WorkingDirectory = solutionPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        ProcessCapture result = await ProcessRunner.RunProcess(processStartInfo);

        if (result.ExitCode != 0)
        {
            Console.WriteLine("Warning: tracked .gitignore files could not be determined. Non-ignored .gitignore files will still be included.");
            return trackedGitIgnoreFiles;
        }

        foreach (string line in PathUtility.ReadOutputLines(result.Output))
        {
            string normalizedPath = PathUtility.NormalizeGitPath(line);

            if (PathUtility.IsGitIgnorePath(normalizedPath))
            {
                trackedGitIgnoreFiles.Add(normalizedPath);
            }
        }

        Console.WriteLine($"Tracked .gitignore files found: {trackedGitIgnoreFiles.Count}");
        return trackedGitIgnoreFiles;
    }
}
