using Eto.Drawing;
using Eto.Forms;

namespace SFD.ImageGenerator;

/// <summary>
/// Main application window: image drop area + Browse / OK / Cancel buttons.
/// </summary>
public sealed class MainForm : Form
{
    private static readonly string[] SupportedExtensions =
    [
        ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff", ".webp", ".ico",
    ];

    private static readonly string[] PlaceholderLines =
    [
        "Drag image here",
        "or click Browse…",
    ];

    private readonly Panel _dropPanel = new();
    private readonly Drawable _preview = new();

    private Bitmap? _currentImage;

    private bool HasImage => _currentImage is not null;

    public MainForm()
    {
        Title = "SFD Image Generator";
        MinimumSize = new Size(500, 400);
        Size = new Size(600, 500);

        _dropPanel.BackgroundColor = Colors.WhiteSmoke;
        _dropPanel.MinimumSize = new Size(400, 300);
        _dropPanel.AllowDrop = true;
        _dropPanel.Content = _preview;

        // Custom-drawn preview: always bounded by the drop area, so even
        // huge images scale down to fit instead of pushing the layout.
        _preview.AllowDrop = true;
        _preview.Paint += OnPreviewPaint;
        _preview.SizeChanged += (_, _) => _preview.Invalidate();

        // Drag & drop wiring (panel + preview, as events go to the control under the cursor).
        foreach (var control in new Control[] { _dropPanel, _preview })
        {
            control.DragEnter += OnDragEnter;
            control.DragOver += OnDragEnter;
            control.DragDrop += OnDragDrop;
        }

        var browseButton = new Button { Text = "Browse…" };
        browseButton.Click += (_, _) => BrowseForImage();

        var okButton = new Button { Text = "OK" };
        okButton.Click += (_, _) => OnOk();

        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Click += (_, _) => OnCancel();

        // Bottom row: Browse on the left, OK/Cancel on the right.
        var buttonRow = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Items =
            {
                browseButton,
                new StackLayoutItem(null, true),
                okButton,
                cancelButton,
            },
        };

        var layout = new DynamicLayout { Padding = new Padding(10), Spacing = new Size(5, 5) };
        layout.Add(_dropPanel, yscale: true);
        layout.Add(buttonRow);

        Content = layout;
    }

    private void OnPreviewPaint(object? sender, PaintEventArgs e)
    {
        var area = new RectangleF(0, 0, _preview.Width, _preview.Height);
        e.Graphics.FillRectangle(Colors.WhiteSmoke, area);

        var image = _currentImage;
        if (image is null)
        {
            DrawPlaceholder(e.Graphics, area);
            return;
        }

        var imageSize = image.Size;
        if (imageSize.Width <= 0 || imageSize.Height <= 0 || area.Width <= 0 || area.Height <= 0)
            return;

        // Scale down to fit the area, preserving aspect ratio. Never upscale.
        var scale = Math.Min(area.Width / imageSize.Width, area.Height / imageSize.Height);
        scale = Math.Min(scale, 1f);

        var width = imageSize.Width * scale;
        var height = imageSize.Height * scale;
        var dest = new RectangleF(
            area.X + ((area.Width - width) / 2),
            area.Y + ((area.Height - height) / 2),
            width,
            height);

        e.Graphics.ImageInterpolation = ImageInterpolation.High;
        e.Graphics.DrawImage(image, dest);
    }

    private static void DrawPlaceholder(Graphics graphics, RectangleF area)
    {
        var font = SystemFonts.Label();

        var sizes = new SizeF[PlaceholderLines.Length];
        var totalHeight = 0f;
        for (var i = 0; i < PlaceholderLines.Length; i++)
        {
            sizes[i] = graphics.MeasureString(font, PlaceholderLines[i]);
            totalHeight += sizes[i].Height;
        }

        var y = area.Y + ((area.Height - totalHeight) / 2);
        for (var i = 0; i < PlaceholderLines.Length; i++)
        {
            var x = area.X + ((area.Width - sizes[i].Width) / 2);
            graphics.DrawText(font, Colors.Gray, x, y, PlaceholderLines[i]);
            y += sizes[i].Height;
        }
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        e.Effects = IsDroppable(e.Data) ? DragEffects.Copy : DragEffects.None;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        // Dropped bitmap (e.g. from a browser) takes precedence.
        if (e.Data.ContainsImage && e.Data.Image is Bitmap dropped)
        {
            SetImage(new Bitmap(dropped));
            return;
        }

        var path = GetDroppedFilePath(e.Data);
        if (path is not null)
            LoadImageFromFile(path);
    }

    private void OnCancel()
    {
        if (HasImage)
            ClearImage();
        else
            Application.Instance.Quit();
    }

    private void OnOk()
    {
        if (_currentImage is null)
        {
            MessageBox.Show(this, "No image loaded. Drop an image or click Browse… first.",
                "SFD Image Generator", MessageBoxButtons.OK, MessageBoxType.Information);
            return;
        }

        string output;
        try
        {
            output = FrameDataFormat.EncodeFrame(_currentImage);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not encode image:\n{ex.Message}",
                "SFD Image Generator", MessageBoxButtons.OK, MessageBoxType.Error);
            return;
        }

        string path;
        bool usedFallback;
        try
        {
            path = ScriptDataFile.Write(output, out usedFallback);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not write output file:\n{ex.Message}",
                "SFD Image Generator", MessageBoxButtons.OK, MessageBoxType.Error);
            return;
        }

        var size = _currentImage.Size;
        var note = size.Width > 128 || size.Height > 128
            ? "\n\nNote: larger than the recommended 128x128 (each pixel is a live game object)."
            : string.Empty;
        var fallbackNote = usedFallback
            ? "\n\nShared folder not found, wrote beside the application instead."
            : string.Empty;
        MessageBox.Show(this, $"Wrote {output.Length} characters to:\n{path}{note}{fallbackNote}",
            "SFD Image Generator", MessageBoxButtons.OK, MessageBoxType.Information);
    }

    private void BrowseForImage()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select image",
            MultiSelect = false,
            CheckFileExists = true,
        };
        dialog.Filters.Add(new FileFilter("Image files", SupportedExtensions));
        dialog.Filters.Add(new FileFilter("All files", ".*"));

        if (dialog.ShowDialog(this) == DialogResult.Ok && !string.IsNullOrWhiteSpace(dialog.FileName))
            LoadImageFromFile(dialog.FileName);
    }

    private void LoadImageFromFile(string path)
    {
        try
        {
            SetImage(new Bitmap(path));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not load image:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxType.Error);
        }
    }

    private void SetImage(Bitmap image)
    {
        _currentImage?.Dispose();
        _currentImage = image;
        _preview.Invalidate();
    }

    private void ClearImage()
    {
        _currentImage?.Dispose();
        _currentImage = null;
        _preview.Invalidate();
    }

    private static bool IsDroppable(DataObject data)
    {
        if (data.ContainsImage)
            return true;
        return GetDroppedFilePath(data) is not null;
    }

    private static string? GetDroppedFilePath(DataObject data)
    {
        if (!data.ContainsUris)
            return null;

        var uris = data.Uris;
        if (uris is null)
            return null;

        foreach (var uri in uris)
        {
            if (!uri.IsFile)
                continue;
            var path = uri.LocalPath;
            if (IsSupportedExtension(path))
                return path;
        }

        return null;
    }

    private static bool IsSupportedExtension(string path)
    {
        var ext = Path.GetExtension(path);
        return SupportedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    protected override void OnUnLoad(EventArgs e)
    {
        _currentImage?.Dispose();
        _currentImage = null;
        base.OnUnLoad(e);
    }
}
