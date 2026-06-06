namespace RepoDumpKit.Tests;

public sealed class ModelsTests
{
    [Fact]
    public void DirectChildCount_Known_ReturnsCountsAndTotal()
    {
        DirectChildCount count = DirectChildCount.Known(2, 3);

        Assert.Equal(2, count.Directories);
        Assert.Equal(3, count.Files);
        Assert.Equal(5, count.KnownTotal);
        Assert.Equal("2", count.DirectoriesText);
        Assert.Equal("3", count.FilesText);
    }

    [Fact]
    public void DirectChildCount_Unknown_ReturnsUnknownTextAndZeroKnownTotal()
    {
        DirectChildCount count = DirectChildCount.Unknown();

        Assert.Null(count.Directories);
        Assert.Null(count.Files);
        Assert.Equal(0, count.KnownTotal);
        Assert.Equal("unknown", count.DirectoriesText);
        Assert.Equal("unknown", count.FilesText);
    }

    [Fact]
    public void BinaryProbeResult_FactoriesCreateExpectedStates()
    {
        Assert.Equal(new BinaryProbeResult(true, false, null), BinaryProbeResult.Text());
        Assert.Equal(new BinaryProbeResult(true, true, null), BinaryProbeResult.Binary());
        Assert.Equal(new BinaryProbeResult(false, false, "failed"), BinaryProbeResult.Failed("failed"));
    }

    [Fact]
    public void FileContentReadResult_FactoriesCreateExpectedStates()
    {
        FileContentReadResult ok = FileContentReadResult.Ok("content", "utf-8", 1, "abc");
        FileContentReadResult error = FileContentReadResult.Error("missing");

        Assert.True(ok.Success);
        Assert.Equal("content", ok.Content);
        Assert.Equal("utf-8", ok.EncodingName);
        Assert.Equal("1", ok.LineCountText);
        Assert.Equal("abc", ok.Sha256Text);
        Assert.False(error.Success);
        Assert.Equal("N/A", error.EncodingName);
        Assert.Equal("missing", error.ErrorMessage);
    }

    [Fact]
    public void ProcessCapture_NotStartedCreatesFailureCaptureWithoutExitCode()
    {
        ProcessCapture capture = ProcessCapture.NotStarted("not found");

        Assert.False(capture.Started);
        Assert.Null(capture.ExitCode);
        Assert.Equal(string.Empty, capture.Output);
        Assert.Equal("not found", capture.ErrorOutput);
    }

    [Fact]
    public void PathTrieNode_GetOrAddReturnsSameChildForSameSegment()
    {
        var node = new PathTrieNode();

        PathTrieNode first = node.GetOrAdd("src");
        PathTrieNode second = node.GetOrAdd("src");

        Assert.Same(first, second);
        Assert.Single(node.Children);
    }
}
