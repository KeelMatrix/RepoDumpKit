using System.Diagnostics;

namespace RepoDumpKit;

internal static class ProcessRunner
{
    public static async Task<ProcessCapture> RunProcess(ProcessStartInfo processStartInfo)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = processStartInfo
            };

            bool started = process.Start();

            if (!started)
            {
                return ProcessCapture.NotStarted("Process.Start returned false.");
            }

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            string output = await outputTask;
            string error = await errorTask;

            return new ProcessCapture(true, process.ExitCode, output, error);
        }
        catch (Exception ex)
        {
            return ProcessCapture.NotStarted($"{ex.GetType().Name}: {ex.Message}");
        }
    }
}
