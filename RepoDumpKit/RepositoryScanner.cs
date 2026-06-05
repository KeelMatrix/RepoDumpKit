namespace RepoDumpKit;

internal static class RepositoryScanner
{
    public static ScanResult ScanRepository(
        string solutionPath,
        HashSet<string> ignoredItems,
        HashSet<string> trackedGitIgnoreFiles)
    {
        var includedFiles = new List<FileEntry>();
        var skippedItems = new List<SkippedItem>();

        CollectDirectory(solutionPath, solutionPath, ignoredItems, trackedGitIgnoreFiles, includedFiles, skippedItems);

        List<FileEntry> sortedIncludedFiles = [.. includedFiles
            .OrderBy(x => PathUtility.GetFileSortGroup(x.RelativePath))
            .ThenBy(x => x.RelativePath, AppSettings.PathComparer)];

        List<SkippedItem> sortedSkippedItems = [.. skippedItems
            .OrderBy(x => x.RelativePath, AppSettings.PathComparer)
            .ThenBy(x => x.Reason, StringComparer.Ordinal)];

        return new ScanResult(sortedIncludedFiles, sortedSkippedItems);
    }

    private static void CollectDirectory(
        string currentDirectory,
        string solutionPath,
        HashSet<string> ignoredItems,
        HashSet<string> trackedGitIgnoreFiles,
        List<FileEntry> includedFiles,
        List<SkippedItem> skippedItems)
    {
        DirectoryInfo directoryInfo;

        try
        {
            directoryInfo = new DirectoryInfo(currentDirectory);
        }
        catch (Exception ex)
        {
            skippedItems.Add(new SkippedItem(
                PathUtility.NormalizeRelativePathSafe(solutionPath, currentDirectory),
                "DIRECTORY_INFO_ERROR",
                $"{ex.GetType().Name}: {ex.Message}"));
            return;
        }

        if (directoryInfo.Name.Equals(".git", StringComparison.OrdinalIgnoreCase))
        {
            skippedItems.Add(new SkippedItem(
                PathUtility.EnsureTrailingSlash(PathUtility.NormalizeRelativePathSafe(solutionPath, currentDirectory)),
                "GIT_METADATA_DIRECTORY",
                "Git internal metadata is never dumped as file content."));
            return;
        }

        if (PathUtility.IsReparsePoint(directoryInfo.Attributes))
        {
            skippedItems.Add(new SkippedItem(
                PathUtility.EnsureTrailingSlash(PathUtility.NormalizeRelativePathSafe(solutionPath, currentDirectory)),
                "REPARSE_POINT_DIRECTORY",
                "Skipped to avoid symlink or junction loops."));
            return;
        }

        string relativeDirectoryPath = PathUtility.NormalizeRelativePath(solutionPath, currentDirectory);

        if (relativeDirectoryPath != "." &&
            PathUtility.IsPathIgnoredByGit(relativeDirectoryPath, ignoredItems) &&
            !PathUtility.HasTrackedGitIgnoreUnderDirectory(trackedGitIgnoreFiles, relativeDirectoryPath))
        {
            return;
        }

        foreach (string filePath in SafeGetFiles(currentDirectory, solutionPath, skippedItems))
        {
            CollectFile(filePath, solutionPath, ignoredItems, trackedGitIgnoreFiles, includedFiles, skippedItems);
        }

        foreach (string directoryPath in SafeGetDirectories(currentDirectory, solutionPath, skippedItems))
        {
            CollectDirectory(directoryPath, solutionPath, ignoredItems, trackedGitIgnoreFiles, includedFiles, skippedItems);
        }
    }

    private static void CollectFile(
        string filePath,
        string solutionPath,
        HashSet<string> ignoredItems,
        HashSet<string> trackedGitIgnoreFiles,
        List<FileEntry> includedFiles,
        List<SkippedItem> skippedItems)
    {
        string relativeFilePath = PathUtility.NormalizeRelativePath(solutionPath, filePath);
        bool isTrackedGitIgnore = trackedGitIgnoreFiles.Contains(relativeFilePath);

        if (PathUtility.IsPathIgnoredByGit(relativeFilePath, ignoredItems) && !isTrackedGitIgnore)
        {
            return;
        }

        FileInfo fileInfo;

        try
        {
            fileInfo = new FileInfo(filePath);
        }
        catch (Exception ex)
        {
            skippedItems.Add(new SkippedItem(relativeFilePath, "FILE_INFO_ERROR", $"{ex.GetType().Name}: {ex.Message}"));
            return;
        }

        if (!isTrackedGitIgnore && AppSettings.BinaryExtensions.Contains(fileInfo.Extension))
        {
            skippedItems.Add(new SkippedItem(relativeFilePath, "BINARY_EXTENSION", $"Extension '{fileInfo.Extension}' is excluded from text dumps."));
            return;
        }

        if (!isTrackedGitIgnore)
        {
            BinaryProbeResult binaryProbe = FileAnalysisService.LooksBinaryBySample(filePath);

            if (!binaryProbe.Success)
            {
                skippedItems.Add(new SkippedItem(relativeFilePath, "READ_CHECK_FAILED", binaryProbe.ErrorMessage ?? "Could not inspect file."));
                return;
            }

            if (binaryProbe.IsBinary)
            {
                skippedItems.Add(new SkippedItem(relativeFilePath, "BINARY_CONTENT", "NUL bytes detected in file sample."));
                return;
            }
        }

        includedFiles.Add(new FileEntry(filePath, relativeFilePath));
    }

    private static string[] SafeGetFiles(string directoryPath, string solutionPath, List<SkippedItem> skippedItems)
    {
        try
        {
            return Directory.GetFiles(directoryPath);
        }
        catch (Exception ex)
        {
            skippedItems.Add(new SkippedItem(
                PathUtility.EnsureTrailingSlash(PathUtility.NormalizeRelativePathSafe(solutionPath, directoryPath)),
                "FILE_ENUMERATION_FAILED",
                $"{ex.GetType().Name}: {ex.Message}"));
            return [];
        }
    }

    private static string[] SafeGetDirectories(string directoryPath, string solutionPath, List<SkippedItem> skippedItems)
    {
        try
        {
            return Directory.GetDirectories(directoryPath);
        }
        catch (Exception ex)
        {
            skippedItems.Add(new SkippedItem(
                PathUtility.EnsureTrailingSlash(PathUtility.NormalizeRelativePathSafe(solutionPath, directoryPath)),
                "DIRECTORY_ENUMERATION_FAILED",
                $"{ex.GetType().Name}: {ex.Message}"));
            return [];
        }
    }
}
