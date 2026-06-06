namespace RepoDumpKit.Tests;

public sealed class RepositoryScannerTests
{
    [Fact]
    public void ScanRepository_IncludesReadableTextFiles()
    {
        using var temp = new TestDirectory();
        temp.CreateFile("README.md", "readme");
        temp.CreateFile("src/App.cs", "code");

        ScanResult result = RepositoryScanner.ScanRepository(temp.RootPath, [], []);

        Assert.Contains(result.IncludedFiles, file => file.RelativePath == "README.md");
        Assert.Contains(result.IncludedFiles, file => file.RelativePath == "src/App.cs");
        Assert.Empty(result.SkippedItems);
    }

    [Fact]
    public void ScanRepository_SkipsGitMetadataDirectory()
    {
        using var temp = new TestDirectory();
        temp.CreateFile(".git/config", "[core]");
        temp.CreateFile("src/App.cs", "code");

        ScanResult result = RepositoryScanner.ScanRepository(temp.RootPath, [], []);

        Assert.Contains(result.SkippedItems, item => item.RelativePath == ".git/" && item.Reason == "GIT_METADATA_DIRECTORY");
        Assert.DoesNotContain(result.IncludedFiles, file => file.RelativePath == ".git/config");
    }

    [Fact]
    public void ScanRepository_SkipsFilesByBinaryExtension()
    {
        using var temp = new TestDirectory();
        temp.CreateFile("image.png", "not actually binary");

        ScanResult result = RepositoryScanner.ScanRepository(temp.RootPath, [], []);

        Assert.Empty(result.IncludedFiles);
        Assert.Contains(result.SkippedItems, item => item.RelativePath == "image.png" && item.Reason == "BINARY_EXTENSION");
    }

    [Fact]
    public void ScanRepository_SkipsFilesWithBinaryContent()
    {
        using var temp = new TestDirectory();
        temp.CreateFile("data.txt", [(byte)'a', 0x00, (byte)'b']);

        ScanResult result = RepositoryScanner.ScanRepository(temp.RootPath, [], []);

        Assert.Empty(result.IncludedFiles);
        Assert.Contains(result.SkippedItems, item => item.RelativePath == "data.txt" && item.Reason == "BINARY_CONTENT");
    }

    [Fact]
    public void ScanRepository_SkipsIgnoredFilesAndDirectories()
    {
        using var temp = new TestDirectory();
        temp.CreateFile("bin/output.txt", "ignored");
        temp.CreateFile("obj/cache.txt", "ignored");
        temp.CreateFile("src/App.cs", "included");
        var ignored = new HashSet<string>(AppSettings.PathComparer) { "bin/output.txt", "obj" };

        ScanResult result = RepositoryScanner.ScanRepository(temp.RootPath, ignored, []);

        Assert.Single(result.IncludedFiles);
        Assert.Equal("src/App.cs", result.IncludedFiles[0].RelativePath);
        Assert.DoesNotContain(result.IncludedFiles, file => file.RelativePath.StartsWith("bin/", StringComparison.Ordinal));
        Assert.DoesNotContain(result.IncludedFiles, file => file.RelativePath.StartsWith("obj/", StringComparison.Ordinal));
    }

    [Fact]
    public void ScanRepository_IncludesTrackedGitIgnoreEvenWhenParentDirectoryIsIgnored()
    {
        using var temp = new TestDirectory();
        temp.CreateFile("ignored/.gitignore", "!keep.txt");
        temp.CreateFile("ignored/ignored.txt", "ignored");
        var ignored = new HashSet<string>(AppSettings.PathComparer) { "ignored" };
        var trackedGitIgnoreFiles = new HashSet<string>(AppSettings.PathComparer) { "ignored/.gitignore" };

        ScanResult result = RepositoryScanner.ScanRepository(temp.RootPath, ignored, trackedGitIgnoreFiles);

        Assert.Contains(result.IncludedFiles, file => file.RelativePath == "ignored/.gitignore");
        Assert.DoesNotContain(result.IncludedFiles, file => file.RelativePath == "ignored/ignored.txt");
    }

    [Fact]
    public void ScanRepository_OrdersIncludedFilesByNavigationPriorityThenPath()
    {
        using var temp = new TestDirectory();
        temp.CreateFile("src/App.cs", "code");
        temp.CreateFile("README.md", "readme");
        temp.CreateFile("src/RepoDumpKit.csproj", "project");
        temp.CreateFile(".gitignore", "bin/");

        ScanResult result = RepositoryScanner.ScanRepository(temp.RootPath, [], []);

        Assert.Equal(new[] { ".gitignore", "README.md", "src/RepoDumpKit.csproj", "src/App.cs" }, result.IncludedFiles.Select(file => file.RelativePath).ToArray());
    }
}
