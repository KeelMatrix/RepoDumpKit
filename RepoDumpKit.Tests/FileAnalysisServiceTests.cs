using System.Security.Cryptography;
using System.Text;

namespace RepoDumpKit.Tests;

public sealed class FileAnalysisServiceTests
{
    [Fact]
    public async Task ReadFileContent_ReadsUtf8TextAndReportsMetadata()
    {
        using var temp = new TestDirectory();
        string path = temp.CreateFile("sample.txt", "alpha\nbeta");
        string expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("alpha\nbeta"))).ToLowerInvariant();

        FileContentReadResult result = await FileAnalysisService.ReadFileContent(path);

        Assert.True(result.Success);
        Assert.Equal("alpha\nbeta", result.Content);
        Assert.Equal("utf-8", result.EncodingName);
        Assert.Equal("2", result.LineCountText);
        Assert.Equal(expectedHash, result.Sha256Text);
        Assert.Equal(string.Empty, result.ErrorMessage);
    }

    [Fact]
    public async Task ReadFileContent_ReturnsErrorResultForMissingFile()
    {
        using var temp = new TestDirectory();
        string missingPath = Path.Combine(temp.RootPath, "missing.txt");

        FileContentReadResult result = await FileAnalysisService.ReadFileContent(missingPath);

        Assert.False(result.Success);
        Assert.Equal("N/A", result.EncodingName);
        Assert.Equal("N/A", result.LineCountText);
        Assert.Equal("N/A", result.Sha256Text);
        Assert.NotEmpty(result.ErrorMessage);
    }

    [Fact]
    public void GetFileMetadata_ReturnsSizeAndLastWriteUtcForExistingFile()
    {
        using var temp = new TestDirectory();
        string path = temp.CreateFile("sample.txt", "abc");

        FileMetadata metadata = FileAnalysisService.GetFileMetadata(path);

        Assert.Equal("3", metadata.SizeBytesText);
        Assert.NotEqual("MISSING", metadata.LastWriteUtcText);
    }

    [Fact]
    public void GetFileMetadata_ReturnsMissingForAbsentFile()
    {
        using var temp = new TestDirectory();
        string path = Path.Combine(temp.RootPath, "missing.txt");

        FileMetadata metadata = FileAnalysisService.GetFileMetadata(path);

        Assert.Equal("MISSING", metadata.SizeBytesText);
        Assert.Equal("MISSING", metadata.LastWriteUtcText);
    }

    [Fact]
    public void LooksBinaryBySample_ReturnsTextForEmptyFile()
    {
        using var temp = new TestDirectory();
        string path = temp.CreateFile("empty.txt", string.Empty);

        BinaryProbeResult result = FileAnalysisService.LooksBinaryBySample(path);

        Assert.True(result.Success);
        Assert.False(result.IsBinary);
    }

    [Fact]
    public void LooksBinaryBySample_ReturnsTextForUtf8BomFile()
    {
        using var temp = new TestDirectory();
        string path = temp.CreateFile("bom.txt", [0xEF, 0xBB, 0xBF, (byte)'a']);

        BinaryProbeResult result = FileAnalysisService.LooksBinaryBySample(path);

        Assert.True(result.Success);
        Assert.False(result.IsBinary);
    }

    [Fact]
    public void LooksBinaryBySample_ReturnsBinaryWhenSampleContainsNullByte()
    {
        using var temp = new TestDirectory();
        string path = temp.CreateFile("binary.dat", [(byte)'a', 0x00, (byte)'b']);

        BinaryProbeResult result = FileAnalysisService.LooksBinaryBySample(path);

        Assert.True(result.Success);
        Assert.True(result.IsBinary);
    }

    [Fact]
    public void LooksBinaryBySample_ReturnsFailureForMissingFile()
    {
        using var temp = new TestDirectory();
        string path = Path.Combine(temp.RootPath, "missing.txt");

        BinaryProbeResult result = FileAnalysisService.LooksBinaryBySample(path);

        Assert.False(result.Success);
        Assert.False(result.IsBinary);
        Assert.NotEmpty(result.ErrorMessage);
    }
}
