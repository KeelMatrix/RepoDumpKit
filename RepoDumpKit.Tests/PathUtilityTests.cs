namespace RepoDumpKit.Tests;

public sealed class PathUtilityTests
{
    [Theory]
    [InlineData("", ".")]
    [InlineData("   ", ".")]
    [InlineData("./src/file.cs", "src/file.cs")]
    [InlineData("././src\\file.cs", "src/file.cs")]
    [InlineData("/src/file.cs", "src/file.cs")]
    [InlineData("src\\nested\\file.cs", "src/nested/file.cs")]
    public void NormalizeGitPath_ProducesStableForwardSlashPaths(string input, string expected)
    {
        Assert.Equal(expected, PathUtility.NormalizeGitPath(input));
    }

    [Fact]
    public void NormalizeRelativePath_ReturnsRepositoryStyleRelativePath()
    {
        using var temp = new TestDirectory();
        string filePath = temp.CreateFile("src/Nested/File.cs");

        string relativePath = PathUtility.NormalizeRelativePath(temp.RootPath, filePath);

        Assert.Equal("src/Nested/File.cs", relativePath);
    }

    [Fact]
    public void NormalizeRelativePathSafe_FallsBackToNormalizedInput_WhenRelativePathCannotBeComputed()
    {
        string normalized = PathUtility.NormalizeRelativePathSafe(null!, "C:\\Repo\\File.cs");

        Assert.Equal("C:/Repo/File.cs", normalized);
    }

    [Theory]
    [InlineData(".gitignore")]
    [InlineData("src/.gitignore")]
    [InlineData("SRC/.GITIGNORE")]
    public void IsGitIgnorePath_DetectsGitIgnoreByFileName(string relativePath)
    {
        Assert.True(PathUtility.IsGitIgnorePath(relativePath));
    }

    [Theory]
    [InlineData("README.md", 1)]
    [InlineData("RepoDumpKit.slnx", 1)]
    [InlineData("src/RepoDumpKit.csproj", 2)]
    [InlineData("src/file.cs", 3)]
    [InlineData(".gitignore", 0)]
    public void GetFileSortGroup_SortsNavigationFilesBeforeRegularContent(string relativePath, int expectedGroup)
    {
        Assert.Equal(expectedGroup, PathUtility.GetFileSortGroup(relativePath));
    }

    [Theory]
    [InlineData("README.md", true)]
    [InlineData("docker-compose.yaml", true)]
    [InlineData("RepoDumpKit.csproj", true)]
    [InlineData("source.cs", false)]
    public void IsProjectNavigationFile_ReturnsExpectedClassification(string fileName, bool expected)
    {
        Assert.Equal(expected, PathUtility.IsProjectNavigationFile(fileName));
    }

    [Theory]
    [InlineData("src", "src/")]
    [InlineData("src/", "src/")]
    [InlineData(".", ".")]
    public void EnsureTrailingSlash_AddsSlashExceptForRootMarker(string input, string expected)
    {
        Assert.Equal(expected, PathUtility.EnsureTrailingSlash(input));
    }

    [Fact]
    public void IsPathIgnoredByGit_ReturnsTrueForDirectIgnoredFile()
    {
        var ignored = new HashSet<string>(AppSettings.PathComparer) { "bin/output.dll" };

        Assert.True(PathUtility.IsPathIgnoredByGit("bin/output.dll", ignored));
    }

    [Fact]
    public void IsPathIgnoredByGit_ReturnsTrueForChildrenOfIgnoredDirectory()
    {
        var ignored = new HashSet<string>(AppSettings.PathComparer) { "bin/" };

        Assert.True(PathUtility.IsPathIgnoredByGit("bin/Debug/app.dll", ignored));
    }

    [Fact]
    public void IsPathIgnoredByGit_ReturnsFalseForRootMarkerAndUnmatchedPaths()
    {
        var ignored = new HashSet<string>(AppSettings.PathComparer) { "bin/" };

        Assert.False(PathUtility.IsPathIgnoredByGit(".", ignored));
        Assert.False(PathUtility.IsPathIgnoredByGit("src/File.cs", ignored));
    }

    [Fact]
    public void HasTrackedGitIgnoreUnderDirectory_ReturnsTrueForDescendantGitIgnore()
    {
        var tracked = new HashSet<string>(AppSettings.PathComparer) { "ignored/.gitignore" };

        Assert.True(PathUtility.HasTrackedGitIgnoreUnderDirectory(tracked, "ignored"));
    }

    [Fact]
    public void HasTrackedGitIgnoreUnderDirectory_ReturnsTrueForRootWhenAnyTrackedGitIgnoreExists()
    {
        var tracked = new HashSet<string>(AppSettings.PathComparer) { "nested/.gitignore" };

        Assert.True(PathUtility.HasTrackedGitIgnoreUnderDirectory(tracked, "."));
    }

    [Fact]
    public void ReadOutputLines_SplitsAllLinesWithoutKeepingLineTerminators()
    {
        string[] lines = PathUtility.ReadOutputLines("one\r\ntwo\nthree").ToArray();

        Assert.Equal(new[] { "one", "two", "three" }, lines);
    }
}
