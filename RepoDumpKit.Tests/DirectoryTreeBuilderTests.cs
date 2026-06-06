namespace RepoDumpKit.Tests;

public sealed class DirectoryTreeBuilderTests
{
    [Fact]
    public void BuildCompactDirectoryTreeOutput_RendersRootAndFilesInExpectedGroups()
    {
        using var temp = new TestDirectory();
        temp.CreateFile("README.md", "readme");
        temp.CreateFile("src/Program.cs", "code");
        temp.CreateFile("src/RepoDumpKit.csproj", "project");

        TreeBuildResult result = DirectoryTreeBuilder.BuildCompactDirectoryTreeOutput(
            temp.RootPath,
            [],
            []);

        Assert.Contains(new DirectoryInfo(temp.RootPath).Name + "/", result.Output);
        Assert.Contains("  README.md", result.Output);
        Assert.Contains("  src/", result.Output);
        Assert.Contains("    RepoDumpKit.csproj", result.Output);
        Assert.Contains("    Program.cs", result.Output);
        Assert.True(result.WrittenEntries >= 4);
        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public void BuildCompactDirectoryTreeOutput_OmitsGitDirectory()
    {
        using var temp = new TestDirectory();
        temp.CreateFile(".git/config", "[core]");
        temp.CreateFile("src/App.cs", "code");

        TreeBuildResult result = DirectoryTreeBuilder.BuildCompactDirectoryTreeOutput(
            temp.RootPath,
            [],
            []);

        Assert.Contains(".git/ [git metadata omitted]", result.Output);
        Assert.DoesNotContain("config", result.Output);
        Assert.True(result.OmittedEntries >= 1);
    }

    [Fact]
    public void BuildCompactDirectoryTreeOutput_MarksIgnoredFiles()
    {
        using var temp = new TestDirectory();
        temp.CreateFile("bin/app.dll", "binary-ish");
        var ignored = new HashSet<string>(AppSettings.PathComparer) { "bin/app.dll" };

        TreeBuildResult result = DirectoryTreeBuilder.BuildCompactDirectoryTreeOutput(
            temp.RootPath,
            ignored,
            []);

        Assert.Contains("app.dll [ignored file; content not dumped]", result.Output);
    }

    [Fact]
    public void BuildCompactDirectoryTreeOutput_CompactsIgnoredDirectoryWithoutTrackedGitIgnore()
    {
        using var temp = new TestDirectory();
        temp.CreateFile("ignored/data.tmp", "data");
        temp.CreateFile("ignored/child/nested.tmp", "data");
        var ignored = new HashSet<string>(AppSettings.PathComparer) { "ignored" };

        TreeBuildResult result = DirectoryTreeBuilder.BuildCompactDirectoryTreeOutput(
            temp.RootPath,
            ignored,
            []);

        Assert.Contains("ignored/ [ignored subtree compacted; direct_dirs=1; direct_files=1]", result.Output);
        Assert.DoesNotContain("nested.tmp", result.Output);
        Assert.Equal(1, result.CompactedIgnoredSubtrees);
        Assert.True(result.OmittedEntries >= 2);
    }

    [Fact]
    public void BuildCompactDirectoryTreeOutput_DoesNotCompactIgnoredDirectoryThatContainsTrackedGitIgnore()
    {
        using var temp = new TestDirectory();
        temp.CreateFile("ignored/.gitignore", "!keep.txt");
        temp.CreateFile("ignored/keep.txt", "data");
        var ignored = new HashSet<string>(AppSettings.PathComparer) { "ignored" };
        var trackedGitIgnoreFiles = new HashSet<string>(AppSettings.PathComparer) { "ignored/.gitignore" };

        TreeBuildResult result = DirectoryTreeBuilder.BuildCompactDirectoryTreeOutput(
            temp.RootPath,
            ignored,
            trackedGitIgnoreFiles);

        Assert.Contains("ignored/", result.Output);
        Assert.Contains(".gitignore [ignored file; content not dumped]", result.Output);
        Assert.Contains("keep.txt [ignored file; content not dumped]", result.Output);
        Assert.Equal(0, result.CompactedIgnoredSubtrees);
    }
}
