using System.Text;

namespace SolutionScannerX;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        string solutionPath = SolutionPathService.GetSolutionPath(args);

        if (string.IsNullOrWhiteSpace(solutionPath) || !Directory.Exists(solutionPath))
        {
            Console.WriteLine("Invalid solution path provided or path does not exist. Exiting.");
            return;
        }

        solutionPath = Path.GetFullPath(solutionPath);

        string solutionName = new DirectoryInfo(solutionPath).Name;
        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string outputFileName = Path.Combine(desktopPath, $"{solutionName}-{timestamp}.txt");

        Console.WriteLine();
        Console.WriteLine($"Repository root: {solutionPath}");
        Console.WriteLine($"Output file:     {outputFileName}");
        Console.WriteLine();

        GitIgnoredResult ignoredResult = await GitRepositoryService.GetIgnoredFilesFromGit(solutionPath);
        HashSet<string> trackedGitIgnoreFiles = await GitRepositoryService.GetTrackedGitIgnoreFiles(solutionPath);

        Console.WriteLine("Building compact tree /f-style directory overview.");
        TreeBuildResult treeResult = DirectoryTreeBuilder.BuildCompactDirectoryTreeOutput(
            solutionPath,
            ignoredResult.IgnoredPaths,
            trackedGitIgnoreFiles);

        Console.WriteLine("Scanning repository files.");
        ScanResult scanResult = RepositoryScanner.ScanRepository(
            solutionPath,
            ignoredResult.IgnoredPaths,
            trackedGitIgnoreFiles);

        Console.WriteLine($"Included text files:          {scanResult.IncludedFiles.Count}");
        Console.WriteLine($"Included tracked .gitignore:  {scanResult.IncludedFiles.Count(x => PathUtility.IsGitIgnorePath(x.RelativePath))}");
        Console.WriteLine($"Git ignored paths reported:   {ignoredResult.IgnoredPaths.Count}");
        Console.WriteLine($"Other scanner exclusions:     {scanResult.SkippedItems.Count}");
        Console.WriteLine($"Tree entries written:         {treeResult.WrittenEntries}");
        Console.WriteLine($"Tree entries compacted:       {treeResult.OmittedEntries}");
        Console.WriteLine();

        try
        {
            DumpWriteStats writeStats;

            await using (var writer = new StreamWriter(outputFileName, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                writeStats = await DumpWriter.WriteDump(
                    writer,
                    solutionPath,
                    solutionName,
                    ignoredResult,
                    trackedGitIgnoreFiles,
                    treeResult,
                    scanResult);
            }

            long outputFileSizeBytes = new FileInfo(outputFileName).Length;

            Console.WriteLine();
            Console.WriteLine("Dump created successfully.");
            Console.WriteLine($"Path:             {outputFileName}");
            Console.WriteLine($"Dump file size:   {FormattingUtility.FormatByteSize(outputFileSizeBytes)}");
            Console.WriteLine($"Files written:    {writeStats.FilesWritten}");
            Console.WriteLine($"Read errors:      {writeStats.ReadErrors}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Processing failed: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine();
        Console.WriteLine("Program finished. Press any key to exit.");
        Console.ReadKey();
    }
}
