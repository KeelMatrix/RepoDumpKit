using System.Text;

namespace RepoDumpKit.Tests;

public sealed class DumpWriterTests
{
    [Fact]
    public async Task WriteDump_WritesExpectedSectionsManifestAndNumberedContent()
    {
        using var temp = new TestDirectory();
        string readmePath = temp.CreateFile("README.md", "alpha\nbeta");
        string sourcePath = temp.CreateFile("src/App.cs", "class App { }\n");
        var scanResult = new ScanResult(
            [new FileEntry(readmePath, "README.md"), new FileEntry(sourcePath, "src/App.cs")],
            [new SkippedItem("bin/app.dll", "BINARY_EXTENSION", "Extension '.dll' is excluded from text dumps.")]);
        var ignoredResult = new GitIgnoredResult(new HashSet<string>(AppSettings.PathComparer) { "bin/app.dll" }, 0, string.Empty);
        var treeResult = new TreeBuildResult("Repo/\n  README.md\n  src/\n", 2, 0, 0, 0, 0, 80, 16);

        string output = await WriteToString(temp.RootPath, "Repo", ignoredResult, [], treeResult, scanResult);

        Assert.Contains("========== DUMP START ==========" , output);
        Assert.Contains("FORMAT_VERSION: " + AppSettings.DumpFormatVersion, output);
        Assert.Contains("========== SECTION START: DIRECTORY TREE ==========", output);
        Assert.Contains("GIT_IGNORED_PATHS_START", output);
        Assert.Contains("bin/app.dll", output);
        Assert.Contains("00001", output);
        Assert.Contains("README.md", output);
        Assert.Contains("000001 | alpha", output);
        Assert.Contains("000002 | beta", output);
        Assert.Contains("========== DUMP END ==========" , output);
    }

    [Fact]
    public async Task WriteDump_ReturnsReadErrorCountWhenFileCannotBeRead()
    {
        using var temp = new TestDirectory();
        string missingPath = Path.Combine(temp.RootPath, "missing.txt");
        var scanResult = new ScanResult([new FileEntry(missingPath, "missing.txt")], []);
        var ignoredResult = new GitIgnoredResult([], 1, "");
        var treeResult = new TreeBuildResult("Repo/\n", 1, 0, 0, 0, 0, 80, 16);

        await using var memory = new MemoryStream();
        await using var writer = new StreamWriter(memory, new UTF8Encoding(false), leaveOpen: true);

        DumpWriteStats stats = await DumpWriter.WriteDump(writer, temp.RootPath, "Repo", ignoredResult, [], treeResult, scanResult);
        await writer.FlushAsync();
        string output = Encoding.UTF8.GetString(memory.ToArray());

        Assert.Equal(1, stats.FilesWritten);
        Assert.Equal(1, stats.ReadErrors);
        Assert.Contains("READ_STATUS: ERROR", output);
        Assert.Contains("[REPODUMPKIT_READ_ERROR]", output);
    }

    [Fact]
    public async Task WriteDump_WritesNoneMarkersWhenNoIgnoredOrSkippedPathsExist()
    {
        using var temp = new TestDirectory();
        var scanResult = new ScanResult([], []);
        var ignoredResult = new GitIgnoredResult([], 1, string.Empty);
        var treeResult = new TreeBuildResult("Repo/\n", 1, 0, 0, 0, 0, 80, 16);

        string output = await WriteToString(temp.RootPath, "Repo", ignoredResult, [], treeResult, scanResult);

        Assert.Contains("GIT_IGNORED_PATHS_START" + Environment.NewLine + "(none)", output);
        Assert.Contains("OTHER_NOT_INCLUDED_START" + Environment.NewLine + "REASON\tRELATIVE_PATH\tDETAIL" + Environment.NewLine + "(none)", output);
        Assert.Contains("GITIGNORE_FILES_INCLUDED_START" + Environment.NewLine + "(none)", output);
    }

    private static async Task<string> WriteToString(
        string solutionPath,
        string solutionName,
        GitIgnoredResult ignoredResult,
        HashSet<string> trackedGitIgnoreFiles,
        TreeBuildResult treeResult,
        ScanResult scanResult)
    {
        await using var memory = new MemoryStream();
        await using var writer = new StreamWriter(memory, new UTF8Encoding(false), leaveOpen: true);
        await DumpWriter.WriteDump(writer, solutionPath, solutionName, ignoredResult, trackedGitIgnoreFiles, treeResult, scanResult);
        await writer.FlushAsync();
        return Encoding.UTF8.GetString(memory.ToArray());
    }
}
