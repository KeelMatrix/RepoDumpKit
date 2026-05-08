namespace SolutionScannerX;

internal static class FormattingUtility
{
    public static int CountLines(string text)
    {
        if (text.Length == 0)
        {
            return 0;
        }

        using var reader = new StringReader(text);
        int count = 0;

        while (reader.ReadLine() is not null)
        {
            count++;
        }

        return count;
    }

    public static string FormatExitCode(int? exitCode)
    {
        return exitCode.HasValue ? exitCode.Value.ToString() : "N/A";
    }

    public static string FormatByteSize(long bytes)
    {
        double kilobytes = bytes / 1024d;
        double megabytes = kilobytes / 1024d;
        return $"{megabytes:0.00} MB ({kilobytes:0.0} KB, {bytes:N0} bytes)";
    }
}
