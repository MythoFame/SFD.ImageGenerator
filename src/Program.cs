// SFD.ImageGenerator - GUI entry point.

using Eto.Forms;

namespace SFD.ImageGenerator;

internal static class Program
{
    [STAThread]
    private static void Main(string[] _)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            ReportFatal(e.ExceptionObject as Exception);

        try
        {
            using var app = new Application(Eto.Platform.Detect);
            app.UnhandledException += (_, e) =>
                ReportFatal(e.ExceptionObject as Exception);
            app.Run(new MainForm());
        }
        catch (Exception ex)
        {
            ReportFatal(ex);
        }
    }

    /// <summary>
    /// Last-resort error reporting: the app is WinExe (no console), so without
    /// this a startup crash would exit silently. Always appends to error.log
    /// beside the binary; shows a dialog on a best-effort basis.
    /// </summary>
    private static void ReportFatal(Exception? ex)
    {
        var message = ex?.ToString() ?? "Unknown fatal error.";
        try
        {
            File.AppendAllText(
                Path.Combine(AppContext.BaseDirectory, "error.log"),
                $"[{DateTimeOffset.Now:O}]{Environment.NewLine}{message}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never throw.
        }

        try
        {
            MessageBox.Show(message, "SFD Image Generator - fatal error",
                MessageBoxButtons.OK, MessageBoxType.Error);
        }
        catch
        {
            // No platform loaded (yet); the log file above is the fallback.
        }
    }
}
