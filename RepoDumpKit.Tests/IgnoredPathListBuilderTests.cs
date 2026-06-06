namespace RepoDumpKit.Tests;

public sealed class IgnoredPathListBuilderTests
{
    [Fact]
    public void BuildCompactIgnoredPathList_ReturnsNoneForEmptyInput()
    {
        CompactPathListResult result = IgnoredPathListBuilder.BuildCompactIgnoredPathList([]);

        Assert.Equal("(none)" + Environment.NewLine, result.Output);
        Assert.Equal(1, result.WrittenEntries);
        Assert.Equal(0, result.CompactedSubtrees);
        Assert.Equal(0, result.OmittedPathCount);
        Assert.Equal(0, result.TotalInputPaths);
    }

    [Fact]
    public void BuildCompactIgnoredPathList_SortsAndNormalizesPaths()
    {
        var ignored = new HashSet<string>(AppSettings.PathComparer)
        {
            "obj\\Release\\app.dll",
            "bin/Debug/app.dll",
            "./.vs/settings.json"
        };

        CompactPathListResult result = IgnoredPathListBuilder.BuildCompactIgnoredPathList(ignored);

        Assert.Equal(".vs/settings.json" + Environment.NewLine + "bin/Debug/app.dll" + Environment.NewLine + "obj/Release/app.dll" + Environment.NewLine, result.Output);
        Assert.Equal(3, result.WrittenEntries);
        Assert.Equal(0, result.CompactedSubtrees);
        Assert.Equal(0, result.OmittedPathCount);
        Assert.Equal(3, result.TotalInputPaths);
    }

    [Fact]
    public void BuildCompactIgnoredPathList_CompactsLargeSubtrees()
    {
        var ignored = new HashSet<string>(AppSettings.PathComparer);

        for (int i = 0; i < AppSettings.MaxIgnoredPathsPerRenderedSubtree + 1; i++)
        {
            ignored.Add($"obj/generated/file-{i:D3}.cs");
        }

        CompactPathListResult result = IgnoredPathListBuilder.BuildCompactIgnoredPathList(ignored);

        Assert.Contains("obj/ [compacted ignored subtree:", result.Output);
        Assert.Equal(1, result.WrittenEntries);
        Assert.Equal(1, result.CompactedSubtrees);
        Assert.Equal(ignored.Count, result.OmittedPathCount);
        Assert.Equal(ignored.Count, result.TotalInputPaths);
    }

    [Fact]
    public void BuildCompactIgnoredPathList_SkipsRootAndWhitespaceEntries()
    {
        var ignored = new HashSet<string>(AppSettings.PathComparer)
        {
            ".",
            "   ",
            "bin/app.dll"
        };

        CompactPathListResult result = IgnoredPathListBuilder.BuildCompactIgnoredPathList(ignored);

        Assert.Equal("bin/app.dll" + Environment.NewLine, result.Output);
        Assert.Equal(1, result.WrittenEntries);
        Assert.Equal(3, result.TotalInputPaths);
    }
}
