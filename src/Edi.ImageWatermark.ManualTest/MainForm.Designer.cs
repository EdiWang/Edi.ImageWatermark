namespace Edi.ImageWatermark.ManualTest;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        btnSelectImage = new Button();
        btnApplyWatermark = new Button();
        btnSave = new Button();
        txtWatermarkText = new TextBox();
        lblWatermarkText = new Label();
        lblFontSize = new Label();
        nudFontSize = new NumericUpDown();
        lblPadding = new Label();
        nudPadding = new NumericUpDown();
        cmbPosition = new ComboBox();
        lblPosition = new Label();
        btnColor = new Button();
        lblColor = new Label();
        pnlColorPreview = new Panel();
        picOriginal = new PictureBox();
        picWatermarked = new PictureBox();
        lblOriginal = new Label();
        lblWatermarked = new Label();
        splitContainer = new SplitContainer();
        pnlControls = new Panel();
        lblImageInfo = new Label();

        ((System.ComponentModel.ISupportInitialize)nudFontSize).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudPadding).BeginInit();
        ((System.ComponentModel.ISupportInitialize)picOriginal).BeginInit();
        ((System.ComponentModel.ISupportInitialize)picWatermarked).BeginInit();
        ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
        splitContainer.Panel1.SuspendLayout();
        splitContainer.Panel2.SuspendLayout();
        splitContainer.SuspendLayout();
        pnlControls.SuspendLayout();
        SuspendLayout();

        // pnlControls
        pnlControls.Dock = DockStyle.Top;
        pnlControls.Height = 100;
        pnlControls.Padding = new Padding(8);

        // Row 1: Select image, watermark text, font size
        btnSelectImage.Text = "Select Image...";
        btnSelectImage.Location = new Point(8, 12);
        btnSelectImage.Size = new Size(110, 28);
        btnSelectImage.Click += BtnSelectImage_Click;

        lblImageInfo.Location = new Point(125, 17);
        lblImageInfo.Size = new Size(200, 20);
        lblImageInfo.Text = "No image selected";
        lblImageInfo.ForeColor = SystemColors.GrayText;

        lblWatermarkText.Text = "Text:";
        lblWatermarkText.Location = new Point(330, 17);
        lblWatermarkText.AutoSize = true;

        txtWatermarkText.Text = "© Edi.Wang";
        txtWatermarkText.Location = new Point(365, 13);
        txtWatermarkText.Size = new Size(150, 23);

        lblFontSize.Text = "Size:";
        lblFontSize.Location = new Point(525, 17);
        lblFontSize.AutoSize = true;

        nudFontSize.Minimum = 8;
        nudFontSize.Maximum = 200;
        nudFontSize.Value = 20;
        nudFontSize.Location = new Point(560, 13);
        nudFontSize.Size = new Size(60, 23);

        // Row 2: Position, padding, color, apply, save
        lblPosition.Text = "Position:";
        lblPosition.Location = new Point(8, 55);
        lblPosition.AutoSize = true;

        cmbPosition.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbPosition.Location = new Point(68, 51);
        cmbPosition.Size = new Size(120, 23);

        lblPadding.Text = "Padding:";
        lblPadding.Location = new Point(200, 55);
        lblPadding.AutoSize = true;

        nudPadding.Minimum = 0;
        nudPadding.Maximum = 500;
        nudPadding.Value = 10;
        nudPadding.Location = new Point(260, 51);
        nudPadding.Size = new Size(60, 23);

        lblColor.Text = "Color:";
        lblColor.Location = new Point(330, 55);
        lblColor.AutoSize = true;

        pnlColorPreview.Location = new Point(375, 51);
        pnlColorPreview.Size = new Size(24, 24);
        pnlColorPreview.BackColor = Color.White;
        pnlColorPreview.BorderStyle = BorderStyle.FixedSingle;

        btnColor.Text = "Pick...";
        btnColor.Location = new Point(405, 50);
        btnColor.Size = new Size(60, 26);
        btnColor.Click += BtnColor_Click;

        btnApplyWatermark.Text = "Apply Watermark";
        btnApplyWatermark.Location = new Point(480, 50);
        btnApplyWatermark.Size = new Size(120, 28);
        btnApplyWatermark.Enabled = false;
        btnApplyWatermark.Click += BtnApplyWatermark_Click;

        btnSave.Text = "Save As...";
        btnSave.Location = new Point(608, 50);
        btnSave.Size = new Size(90, 28);
        btnSave.Enabled = false;
        btnSave.Click += BtnSave_Click;

        pnlControls.Controls.AddRange([
            btnSelectImage, lblImageInfo,
            lblWatermarkText, txtWatermarkText,
            lblFontSize, nudFontSize,
            lblPosition, cmbPosition,
            lblPadding, nudPadding,
            lblColor, pnlColorPreview, btnColor,
            btnApplyWatermark, btnSave
        ]);

        // splitContainer
        splitContainer.Dock = DockStyle.Fill;
        splitContainer.SplitterDistance = 400;
        splitContainer.SplitterWidth = 6;

        // Left panel - Original
        lblOriginal.Text = "Original";
        lblOriginal.Dock = DockStyle.Top;
        lblOriginal.Height = 22;
        lblOriginal.TextAlign = ContentAlignment.MiddleCenter;
        lblOriginal.Font = new Font(lblOriginal.Font, FontStyle.Bold);
        lblOriginal.BackColor = SystemColors.ControlLight;

        picOriginal.Dock = DockStyle.Fill;
        picOriginal.SizeMode = PictureBoxSizeMode.Zoom;
        picOriginal.BackColor = SystemColors.AppWorkspace;

        splitContainer.Panel1.Controls.Add(picOriginal);
        splitContainer.Panel1.Controls.Add(lblOriginal);

        // Right panel - Watermarked
        lblWatermarked.Text = "Watermarked";
        lblWatermarked.Dock = DockStyle.Top;
        lblWatermarked.Height = 22;
        lblWatermarked.TextAlign = ContentAlignment.MiddleCenter;
        lblWatermarked.Font = new Font(lblWatermarked.Font, FontStyle.Bold);
        lblWatermarked.BackColor = SystemColors.ControlLight;

        picWatermarked.Dock = DockStyle.Fill;
        picWatermarked.SizeMode = PictureBoxSizeMode.Zoom;
        picWatermarked.BackColor = SystemColors.AppWorkspace;

        splitContainer.Panel2.Controls.Add(picWatermarked);
        splitContainer.Panel2.Controls.Add(lblWatermarked);

        // MainForm
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(900, 600);
        MinimumSize = new Size(750, 400);
        Text = "Edi.ImageWatermark - Manual Test";
        Controls.Add(splitContainer);
        Controls.Add(pnlControls);

        ((System.ComponentModel.ISupportInitialize)nudFontSize).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudPadding).EndInit();
        ((System.ComponentModel.ISupportInitialize)picOriginal).EndInit();
        ((System.ComponentModel.ISupportInitialize)picWatermarked).EndInit();
        splitContainer.Panel1.ResumeLayout(false);
        splitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
        splitContainer.ResumeLayout(false);
        pnlControls.ResumeLayout(false);
        pnlControls.PerformLayout();
        ResumeLayout(false);
    }

    private Button btnSelectImage;
    private Button btnApplyWatermark;
    private Button btnSave;
    private TextBox txtWatermarkText;
    private Label lblWatermarkText;
    private Label lblFontSize;
    private NumericUpDown nudFontSize;
    private Label lblPadding;
    private NumericUpDown nudPadding;
    private ComboBox cmbPosition;
    private Label lblPosition;
    private Button btnColor;
    private Label lblColor;
    private Panel pnlColorPreview;
    private PictureBox picOriginal;
    private PictureBox picWatermarked;
    private Label lblOriginal;
    private Label lblWatermarked;
    private SplitContainer splitContainer;
    private Panel pnlControls;
    private Label lblImageInfo;
}
