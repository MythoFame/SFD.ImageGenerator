// SFD.ImageGenerator - GUI entry point.

using Eto.Forms;

namespace SFD.ImageGenerator;

internal static class Program
{
    [STAThread]
    private static void Main(string[] _)
    {
        using var app = new Application(Eto.Platform.Detect);
        app.Run(new MainForm());
    }
}
