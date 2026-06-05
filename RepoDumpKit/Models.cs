namespace RepoDumpKit;

internal sealed record FileEntry(string FullPath, string RelativePath);

internal sealed record SkippedItem(string RelativePath, string Reason, string Detail);

internal sealed record ScanResult(IReadOnlyList<FileEntry> IncludedFiles, IReadOnlyList<SkippedItem> SkippedItems);

internal sealed record GitIgnoredResult(HashSet<string> IgnoredPaths, int? ExitCode, string ErrorOutput);

internal sealed record FileMetadata(string SizeBytesText, string LastWriteUtcText);

internal sealed record DumpWriteStats(int FilesWritten, int ReadErrors);

internal sealed record TreeEntry(
    string FullPath,
    string RelativePath,
    string Name,
    bool IsDirectory,
    bool IsIgnored,
    bool IsReparsePoint);

internal sealed record TreeBuildResult(
    string Output,
    int WrittenEntries,
    int OmittedEntries,
    int CompactedIgnoredSubtrees,
    int MaxDepthOmissions,
    int ErrorCount,
    int MaxEntriesPerDirectory,
    int MaxDepth);

internal sealed record CompactPathListResult(
    string Output,
    int WrittenEntries,
    int CompactedSubtrees,
    int OmittedPathCount,
    int TotalInputPaths);

internal sealed record ProcessCapture(bool Started, int? ExitCode, string Output, string ErrorOutput)
{
    public static ProcessCapture NotStarted(string errorMessage)
    {
        return new ProcessCapture(false, null, string.Empty, errorMessage);
    }
}

internal sealed record DirectChildCount(int? Directories, int? Files)
{
    public int KnownTotal => (Directories ?? 0) + (Files ?? 0);

    public string DirectoriesText => Directories.HasValue ? Directories.Value.ToString() : "unknown";

    public string FilesText => Files.HasValue ? Files.Value.ToString() : "unknown";

    public static DirectChildCount Known(int directories, int files)
    {
        return new DirectChildCount(directories, files);
    }

    public static DirectChildCount Unknown()
    {
        return new DirectChildCount(null, null);
    }
}

internal sealed record BinaryProbeResult(bool Success, bool IsBinary, string? ErrorMessage)
{
    public static BinaryProbeResult Text()
    {
        return new BinaryProbeResult(true, false, null);
    }

    public static BinaryProbeResult Binary()
    {
        return new BinaryProbeResult(true, true, null);
    }

    public static BinaryProbeResult Failed(string errorMessage)
    {
        return new BinaryProbeResult(false, false, errorMessage);
    }
}

internal sealed record FileContentReadResult(
    bool Success,
    string Content,
    string EncodingName,
    string LineCountText,
    string Sha256Text,
    string ErrorMessage)
{
    public static FileContentReadResult Ok(string content, string encodingName, int lineCount, string sha256)
    {
        return new FileContentReadResult(true, content, encodingName, lineCount.ToString(), sha256, string.Empty);
    }

    public static FileContentReadResult Error(string errorMessage)
    {
        return new FileContentReadResult(false, string.Empty, "N/A", "N/A", "N/A", errorMessage);
    }
}

internal sealed class TreeBuildStats
{
    public int WrittenEntries { get; set; }

    public int OmittedEntries { get; set; }

    public int CompactedIgnoredSubtrees { get; set; }

    public int MaxDepthOmissions { get; set; }

    public int ErrorCount { get; set; }
}

internal sealed class CompactPathListStats
{
    public int WrittenEntries { get; set; }

    public int CompactedSubtrees { get; set; }

    public int OmittedPathCount { get; set; }
}

internal sealed class PathTrieNode
{
    public Dictionary<string, PathTrieNode> Children { get; } = new(AppSettings.PathComparer);

    public bool IsTerminal { get; set; }

    public int LeafCount { get; set; }

    public PathTrieNode GetOrAdd(string segment)
    {
        if (!Children.TryGetValue(segment, out PathTrieNode? child))
        {
            child = new PathTrieNode();
            Children.Add(segment, child);
        }

        return child;
    }
}
