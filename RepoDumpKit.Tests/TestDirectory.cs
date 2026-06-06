namespace RepoDumpKit.Tests;

internal sealed class TestDirectory : IDisposable
{
    public string RootPath { get; }

    public TestDirectory()
    {
        RootPath = Path.Combine(Path.GetTempPath(), "RepoDumpKit.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
    }

    public string CreateDirectory(string relativePath)
    {
        string path = Path.Combine(RootPath, FromRepositoryPath(relativePath));
        Directory.CreateDirectory(path);
        return path;
    }

    public string CreateFile(string relativePath, string content = "test")
    {
        string path = Path.Combine(RootPath, FromRepositoryPath(relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    public string CreateFile(string relativePath, byte[] content)
    {
        string path = Path.Combine(RootPath, FromRepositoryPath(relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
        return path;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup only. Failing cleanup should not hide test failures.
        }
    }

    private static string FromRepositoryPath(string relativePath)
    {
        return relativePath.Replace('/', Path.DirectorySeparatorChar);
    }
}
