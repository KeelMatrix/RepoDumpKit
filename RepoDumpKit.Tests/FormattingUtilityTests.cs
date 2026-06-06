namespace RepoDumpKit.Tests;

public sealed class FormattingUtilityTests
{
    [Theory]
    [InlineData("", 0)]
    [InlineData("one", 1)]
    [InlineData("one\n", 1)]
    [InlineData("one\ntwo", 2)]
    [InlineData("one\r\ntwo\r\nthree", 3)]
    public void CountLines_ReturnsLogicalLineCount(string text, int expected)
    {
        Assert.Equal(expected, FormattingUtility.CountLines(text));
    }

    [Theory]
    [InlineData(null, "N/A")]
    [InlineData(0, "0")]
    [InlineData(128, "128")]
    [InlineData(-1, "-1")]
    public void FormatExitCode_HandlesNullAndNumbers(int? exitCode, string expected)
    {
        Assert.Equal(expected, FormattingUtility.FormatExitCode(exitCode));
    }

    [Fact]
    public void FormatByteSize_FormatsBytesKilobytesAndMegabytes()
    {
        string formatted = FormattingUtility.FormatByteSize(0);

        Assert.Equal("0.00 MB (0.0 KB, 0 bytes)", formatted);
    }
}
