namespace RepoDumpKit;

internal static class PathUtility
{
    public static bool IsPathIgnoredByGit(string relativePath, HashSet<string> ignoredItems)
    {
        string normalized = NormalizeGitPath(relativePath);

        if (string.IsNullOrWhiteSpace(normalized) || normalized == ".")
        {
            return false;
        }

        string withoutTrailingSlash = normalized.TrimEnd('/');

        if (ignoredItems.Contains(normalized) ||
            ignoredItems.Contains(withoutTrailingSlash) ||
            ignoredItems.Contains(withoutTrailingSlash + "/"))
        {
            return true;
        }

        string current = withoutTrailingSlash;

        while (true)
        {
            int slashIndex = current.LastIndexOf('/');

            if (slashIndex <= 0)
            {
                return false;
            }

            current = current[..slashIndex];

            if (ignoredItems.Contains(current) || ignoredItems.Contains(current + "/"))
            {
                return true;
            }
        }
    }

    public static bool HasTrackedGitIgnoreUnderDirectory(HashSet<string> trackedGitIgnoreFiles, string relativeDirectoryPath)
    {
        string directory = NormalizeGitPath(relativeDirectoryPath).TrimEnd('/');

        if (string.IsNullOrWhiteSpace(directory) || directory == ".")
        {
            return trackedGitIgnoreFiles.Count > 0;
        }

        string prefix = directory + "/";

        return trackedGitIgnoreFiles.Any(path => path.StartsWith(prefix, AppSettings.PathComparison));
    }

    public static string NormalizeRelativePath(string rootPath, string path)
    {
        string relativePath = Path.GetRelativePath(rootPath, path);
        return NormalizeGitPath(relativePath);
    }

    public static string NormalizeRelativePathSafe(string rootPath, string path)
    {
        try
        {
            return NormalizeRelativePath(rootPath, path);
        }
        catch
        {
            return NormalizeGitPath(path);
        }
    }

    public static string NormalizeGitPath(string path)
    {
        string normalized = path.Trim().Replace('\\', '/');

        while (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        normalized = normalized.TrimStart('/');

        return string.IsNullOrWhiteSpace(normalized) ? "." : normalized;
    }

    public static bool IsGitIgnorePath(string relativePath)
    {
        return GetFileNameFromNormalizedPath(relativePath).Equals(".gitignore", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetFileNameFromNormalizedPath(string relativePath)
    {
        string normalized = NormalizeGitPath(relativePath);
        int slashIndex = normalized.LastIndexOf('/');

        return slashIndex >= 0 ? normalized[(slashIndex + 1)..] : normalized;
    }

    public static int GetFileSortGroup(string relativePath)
    {
        if (IsGitIgnorePath(relativePath))
        {
            return 0;
        }

        string fileName = GetFileNameFromNormalizedPath(relativePath);

        if (!relativePath.Contains('/', StringComparison.Ordinal) && IsProjectNavigationFile(fileName))
        {
            return 1;
        }

        if (IsProjectNavigationFile(fileName))
        {
            return 2;
        }

        return 3;
    }

    public static bool IsProjectNavigationFile(string fileName)
    {
        string lower = fileName.ToLowerInvariant();
        string extension = Path.GetExtension(lower);

        if (lower is
            "readme.md" or
            "package.json" or
            "package-lock.json" or
            "pnpm-lock.yaml" or
            "yarn.lock" or
            "composer.json" or
            "requirements.txt" or
            "pyproject.toml" or
            "cargo.toml" or
            "go.mod" or
            "pom.xml" or
            "build.gradle" or
            "settings.gradle" or
            "dockerfile" or
            "docker-compose.yml" or
            "docker-compose.yaml")
        {
            return true;
        }

        return extension is
            ".sln" or
            ".slnx" or
            ".csproj" or
            ".fsproj" or
            ".vbproj" or
            ".props" or
            ".targets" or
            ".proj";
    }

    public static bool IsReparsePoint(FileAttributes attributes)
    {
        return (attributes & FileAttributes.ReparsePoint) != 0;
    }

    public static string EnsureTrailingSlash(string path)
    {
        string normalized = NormalizeGitPath(path);

        if (normalized == ".")
        {
            return normalized;
        }

        return normalized.EndsWith('/') ? normalized : normalized + "/";
    }

    public static IEnumerable<string> ReadOutputLines(string text)
    {
        using var reader = new StringReader(text);

        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }
}
