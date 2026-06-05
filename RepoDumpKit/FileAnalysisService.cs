using System.Security.Cryptography;
using System.Text;

namespace RepoDumpKit;

internal static class FileAnalysisService
{
    public static async Task<FileContentReadResult> ReadFileContent(string filePath)
    {
        try
        {
            byte[] bytes = await File.ReadAllBytesAsync(filePath);
            string sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

            await using var memoryStream = new MemoryStream(bytes);
            using var reader = new StreamReader(memoryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            string content = await reader.ReadToEndAsync();
            string encodingName = reader.CurrentEncoding.WebName;

            return FileContentReadResult.Ok(
                content,
                encodingName,
                FormattingUtility.CountLines(content),
                sha256);
        }
        catch (Exception ex)
        {
            return FileContentReadResult.Error($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    public static FileMetadata GetFileMetadata(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            return new FileMetadata(
                fileInfo.Exists ? fileInfo.Length.ToString() : "MISSING",
                fileInfo.Exists ? fileInfo.LastWriteTimeUtc.ToString("O") : "MISSING");
        }
        catch (Exception ex)
        {
            return new FileMetadata("ERROR", $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    public static BinaryProbeResult LooksBinaryBySample(string filePath)
    {
        try
        {
            using FileStream stream = File.OpenRead(filePath);
            byte[] buffer = new byte[8192];
            int read = stream.Read(buffer, 0, buffer.Length);

            if (read == 0)
            {
                return BinaryProbeResult.Text();
            }

            if (HasTextBom(buffer, read))
            {
                return BinaryProbeResult.Text();
            }

            for (int i = 0; i < read; i++)
            {
                if (buffer[i] == 0)
                {
                    return BinaryProbeResult.Binary();
                }
            }

            return BinaryProbeResult.Text();
        }
        catch (Exception ex)
        {
            return BinaryProbeResult.Failed($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static bool HasTextBom(byte[] bytes, int length)
    {
        if (length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return true;
        }

        if (length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return true;
        }

        if (length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return true;
        }

        if (length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
        {
            return true;
        }

        if (length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
        {
            return true;
        }

        return false;
    }
}
