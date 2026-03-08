using SixLabors.ImageSharp;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;

namespace Edi.ImageWatermark.ManualTest;

public partial class MainForm : Form
{
    private string? _selectedImagePath;
    private MemoryStream? _watermarkedStream;
    private Color _watermarkColor = Color.White;

    public MainForm()
    {
        InitializeComponent();

        cmbPosition.DataSource = Enum.GetValues<WatermarkPosition>();
        cmbPosition.SelectedItem = WatermarkPosition.BottomRight;

        pnlColorPreview.BackColor = _watermarkColor;
    }

    private void BtnSelectImage_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select an image",
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|All files|*.*"
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        _selectedImagePath = dlg.FileName;

        picOriginal.Image?.Dispose();
        picOriginal.Image = Image.FromFile(_selectedImagePath);

        var info = SixLabors.ImageSharp.Image.Identify(_selectedImagePath);
        lblImageInfo.Text = $"{Path.GetFileName(_selectedImagePath)}  ({info.Width}×{info.Height})";
        lblImageInfo.ForeColor = SystemColors.ControlText;

        btnApplyWatermark.Enabled = true;
        btnSave.Enabled = false;

        picWatermarked.Image?.Dispose();
        picWatermarked.Image = null;
    }

    private void BtnColor_Click(object? sender, EventArgs e)
    {
        using var dlg = new ColorDialog { Color = _watermarkColor, FullOpen = true };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _watermarkColor = dlg.Color;
            pnlColorPreview.BackColor = _watermarkColor;
        }
    }

    private void BtnApplyWatermark_Click(object? sender, EventArgs e)
    {
        if (_selectedImagePath is null) return;

        var text = txtWatermarkText.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("Please enter watermark text.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Cursor = Cursors.WaitCursor;

            using var fileStream = File.OpenRead(_selectedImagePath);
            using var watermarker = new ImageWatermarker(fileStream);

            var position = (WatermarkPosition)cmbPosition.SelectedItem!;
            var fontSize = (int)nudFontSize.Value;
            var padding = (int)nudPadding.Value;

            var color = SixLabors.ImageSharp.Color.FromRgba(
                _watermarkColor.R, _watermarkColor.G, _watermarkColor.B, _watermarkColor.A);

            _watermarkedStream?.Dispose();
            _watermarkedStream = watermarker.AddWatermark(text, color, position, padding, fontSize);

            if (_watermarkedStream is not null)
            {
                picWatermarked.Image?.Dispose();
                picWatermarked.Image = Image.FromStream(_watermarkedStream);
                btnSave.Enabled = true;
            }
            else
            {
                MessageBox.Show("Watermark was not applied (image below pixel threshold).",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error applying watermark:\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (_watermarkedStream is null) return;

        var ext = Path.GetExtension(_selectedImagePath)?.ToLowerInvariant() ?? ".png";

        using var dlg = new SaveFileDialog
        {
            Title = "Save watermarked image",
            FileName = Path.GetFileNameWithoutExtension(_selectedImagePath) + "_watermarked" + ext,
            Filter = $"Image file (*{ext})|*{ext}|All files|*.*"
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            _watermarkedStream.Position = 0;
            using var fs = File.Create(dlg.FileName);
            _watermarkedStream.CopyTo(fs);
            MessageBox.Show("Saved successfully!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving file:\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
