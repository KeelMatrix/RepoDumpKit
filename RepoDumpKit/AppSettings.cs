namespace RepoDumpKit;

internal static class AppSettings
{
    public const string ConfigFileName = "solution_path_config.txt";
    public const string DumpFormatVersion = "3";
    public const string PreservedGitIgnoredCommand =
        "/d /c \"(git ls-files --cached & git ls-files -o -i --exclude-standard) ^| git check-ignore --no-index --stdin\"";

    public const int MaxSavedSolutionPaths = 10;
    public const int MaxTreeEntriesPerDirectory = 80;
    public const int MaxTreeDepth = 16;
    public const int MaxIgnoredPathsPerRenderedSubtree = 250;
    public const int MaxIgnoredPathChildrenPerDirectory = 80;
    public const int MaxIgnoredPathRenderDepth = 12;
    public const int MaxOtherNotIncludedItemsToWrite = 2000;

    public static readonly StringComparer PathComparer =
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    public static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".ico",
        ".wasm", ".dll", ".exe", ".pdb", ".obj", ".bin",
        ".zip", ".7z", ".rar", ".gz", ".tar", ".nupkg",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".sqlite", ".sqlite3", ".db",
        ".mp3", ".wav", ".ogg", ".mp4", ".mov", ".avi", ".mkv",
        ".ttf", ".otf", ".woff", ".woff2",
        ".class", ".jar", ".so", ".dylib", ".a", ".lib"
    };
}
