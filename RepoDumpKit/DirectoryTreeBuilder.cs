using System.Text;

namespace RepoDumpKit;

internal static class DirectoryTreeBuilder
{
    public static TreeBuildResult BuildCompactDirectoryTreeOutput(
        string solutionPath,
        HashSet<string> ignoredItems,
        HashSet<string> trackedGitIgnoreFiles)
    {
        var output = new StringBuilder();
        var stats = new TreeBuildStats();
        string rootName = new DirectoryInfo(solutionPath).Name;

        output.AppendLine(rootName + "/");
        RenderTreeDirectory(solutionPath, solutionPath, ignoredItems, trackedGitIgnoreFiles, output, "  ", 0, stats);

        return new TreeBuildResult(
            output.ToString(),
            stats.WrittenEntries,
            stats.OmittedEntries,
            stats.CompactedIgnoredSubtrees,
            stats.MaxDepthOmissions,
            stats.ErrorCount,
            AppSettings.MaxTreeEntriesPerDirectory,
            AppSettings.MaxTreeDepth);
    }

    private static void RenderTreeDirectory(
        string directoryPath,
        string solutionPath,
        HashSet<string> ignoredItems,
        HashSet<string> trackedGitIgnoreFiles,
        StringBuilder output,
        string indent,
        int depth,
        TreeBuildStats stats)
    {
        if (depth >= AppSettings.MaxTreeDepth)
        {
            output.AppendLine($"{indent}... [tree depth cap reached; subtree omitted]");
            stats.OmittedEntries++;
            stats.MaxDepthOmissions++;
            return;
        }

        List<TreeEntry> entries;

        try
        {
            entries = [.. Directory.GetDirectories(directoryPath)
                .Select(path => CreateTreeEntry(path, solutionPath, isDirectory: true, ignoredItems))
                .Concat(Directory.GetFiles(directoryPath)
                    .Select(path => CreateTreeEntry(path, solutionPath, isDirectory: false, ignoredItems)))
                .OrderBy(entry => entry.IsIgnored)
                .ThenBy(entry => entry.IsDirectory ? 0 : 1)
                .ThenBy(entry => entry.IsDirectory ? 0 : PathUtility.GetFileSortGroup(entry.RelativePath))
                .ThenBy(entry => entry.Name, AppSettings.PathComparer)];
        }
        catch (Exception ex)
        {
            output.AppendLine($"{indent}[tree enumeration error: {ex.GetType().Name}: {ex.Message}]");
            stats.ErrorCount++;
            return;
        }

        int writtenInDirectory = 0;

        foreach (TreeEntry entry in entries)
        {
            if (writtenInDirectory >= AppSettings.MaxTreeEntriesPerDirectory)
            {
                int omitted = entries.Count - writtenInDirectory;
                output.AppendLine($"{indent}... [omitted {omitted} more entries in this directory; TREE_MAX_ENTRIES_PER_DIRECTORY={AppSettings.MaxTreeEntriesPerDirectory}]");
                stats.OmittedEntries += omitted;
                break;
            }

            writtenInDirectory++;
            stats.WrittenEntries++;

            if (entry.IsDirectory)
            {
                if (entry.Name.Equals(".git", StringComparison.OrdinalIgnoreCase))
                {
                    output.AppendLine($"{indent}.git/ [git metadata omitted]");
                    stats.OmittedEntries++;
                    continue;
                }

                if (entry.IsReparsePoint)
                {
                    output.AppendLine($"{indent}{entry.Name}/ [reparse point omitted]");
                    stats.OmittedEntries++;
                    continue;
                }

                bool canCompactIgnoredDirectory = entry.IsIgnored &&
                    !PathUtility.HasTrackedGitIgnoreUnderDirectory(trackedGitIgnoreFiles, entry.RelativePath);

                if (canCompactIgnoredDirectory)
                {
                    DirectChildCount childCount = CountDirectChildren(entry.FullPath);
                    output.AppendLine($"{indent}{entry.Name}/ [ignored subtree compacted; direct_dirs={childCount.DirectoriesText}; direct_files={childCount.FilesText}]");
                    stats.CompactedIgnoredSubtrees++;
                    stats.OmittedEntries += childCount.KnownTotal;
                    continue;
                }

                output.AppendLine($"{indent}{entry.Name}/");
                RenderTreeDirectory(entry.FullPath, solutionPath, ignoredItems, trackedGitIgnoreFiles, output, indent + "  ", depth + 1, stats);
            }
            else
            {
                output.AppendLine(entry.IsIgnored
                    ? $"{indent}{entry.Name} [ignored file; content not dumped]"
                    : $"{indent}{entry.Name}");
            }
        }
    }

    private static TreeEntry CreateTreeEntry(string path, string solutionPath, bool isDirectory, HashSet<string> ignoredItems)
    {
        string relativePath = PathUtility.NormalizeRelativePath(solutionPath, path);
        string name = Path.GetFileName(path);
        bool isIgnored = PathUtility.IsPathIgnoredByGit(relativePath, ignoredItems);
        bool isReparsePoint = false;

        if (isDirectory)
        {
            try
            {
                isReparsePoint = PathUtility.IsReparsePoint(new DirectoryInfo(path).Attributes);
            }
            catch
            {
                isReparsePoint = false;
            }
        }

        return new TreeEntry(path, relativePath, name, isDirectory, isIgnored, isReparsePoint);
    }

    private static DirectChildCount CountDirectChildren(string directoryPath)
    {
        try
        {
            int directories = Directory.GetDirectories(directoryPath).Length;
            int files = Directory.GetFiles(directoryPath).Length;
            return DirectChildCount.Known(directories, files);
        }
        catch
        {
            return DirectChildCount.Unknown();
        }
    }
}
