using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Forms;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

public class MainForm : Form
{
    private readonly TextBox _folderTextBox;
    private readonly Button _browseButton;
    private readonly ListBox _fileListBox;
    private readonly Button _printButton;
    private readonly ProgressBar _progressBar;
    private readonly Label _statusLabel;
    private readonly Label _folderLabel;
    private readonly Label _filesLabel;

    public MainForm()
    {
        Text = "PDF Folder Printer";
        Size = new System.Drawing.Size(620, 500);
        MinimumSize = new System.Drawing.Size(500, 420);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;

        // Set the printer icon on the form window
        string iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        if (File.Exists(iconPath))
            Icon = new System.Drawing.Icon(iconPath);

        // Folder label
        _folderLabel = new Label
        {
            Text = "Folder:",
            Location = new System.Drawing.Point(12, 16),
            AutoSize = true
        };

        // Folder text box
        _folderTextBox = new TextBox
        {
            Location = new System.Drawing.Point(60, 13),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Width = 440,
            ReadOnly = true,
            BackColor = System.Drawing.SystemColors.Window
        };

        // Browse button
        _browseButton = new Button
        {
            Text = "Browse...",
            Location = new System.Drawing.Point(508, 11),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Width = 88,
            Height = 26
        };
        _browseButton.Click += BrowseButton_Click;

        // Files label
        _filesLabel = new Label
        {
            Text = "PDF files found:",
            Location = new System.Drawing.Point(12, 52),
            AutoSize = true
        };

        // File list box
        _fileListBox = new ListBox
        {
            Location = new System.Drawing.Point(12, 70),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Width = 584,
            Height = 300,
            HorizontalScrollbar = true
        };

        // Progress bar
        _progressBar = new ProgressBar
        {
            Location = new System.Drawing.Point(12, 385),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Width = 584,
            Height = 22,
            Minimum = 0,
            Value = 0
        };

        // Status label
        _statusLabel = new Label
        {
            Text = "Select a folder to begin.",
            Location = new System.Drawing.Point(12, 415),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AutoSize = false,
            Width = 480,
            Height = 20
        };

        // Print button
        _printButton = new Button
        {
            Text = "Print All PDFs",
            Location = new System.Drawing.Point(496, 410),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Width = 100,
            Height = 30,
            Enabled = false
        };
        _printButton.Click += PrintButton_Click;

        Controls.AddRange([
            _folderLabel, _folderTextBox, _browseButton,
            _filesLabel, _fileListBox,
            _progressBar, _statusLabel, _printButton
        ]);
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        string? selectedPath = null;

        // Run on a dedicated STA thread to prevent freezing on modern Windows
        var thread = new Thread(() =>
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select the folder containing PDF files",
                UseDescriptionForTitle = true,
                AutoUpgradeEnabled = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
                selectedPath = dialog.SelectedPath;
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (selectedPath is null)
            return;

        _folderTextBox.Text = selectedPath;
        LoadPdfFiles(selectedPath);
    }

    private void LoadPdfFiles(string folderPath)
    {
        _fileListBox.Items.Clear();
        _progressBar.Value = 0;

        string[] files = Directory.GetFiles(folderPath, "*.pdf", SearchOption.TopDirectoryOnly);

        if (files.Length == 0)
        {
            _statusLabel.Text = "No PDF files found in the selected folder.";
            _printButton.Enabled = false;
            return;
        }

        foreach (string file in files)
            _fileListBox.Items.Add(Path.GetFileName(file));

        _progressBar.Maximum = files.Length;
        _statusLabel.Text = $"{files.Length} PDF file(s) ready to print.";
        _printButton.Enabled = true;
    }

    private async void PrintButton_Click(object? sender, EventArgs e)
    {
        string folderPath = _folderTextBox.Text;
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            MessageBox.Show("Please select a valid folder first.", "No Folder Selected",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string[] files = Directory.GetFiles(folderPath, "*.pdf", SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
        {
            MessageBox.Show("No PDF files found in the selected folder.", "Nothing to Print",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _printButton.Enabled = false;
        _browseButton.Enabled = false;
        _progressBar.Value = 0;
        _progressBar.Maximum = files.Length;

        for (int i = 0; i < files.Length; i++)
        {
            string fileName = Path.GetFileName(files[i]);
            _statusLabel.Text = $"Printing {i + 1}/{files.Length}: {fileName}";

            await PrintPdfAsync(files[i]);

            _progressBar.Value = i + 1;
        }

        _statusLabel.Text = $"Done! {files.Length} file(s) sent to the printer.";
        _printButton.Enabled = true;
        _browseButton.Enabled = true;

        MessageBox.Show($"All {files.Length} PDF file(s) have been sent to the printer.",
            "Printing Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static async Task PrintPdfAsync(string pdfPath)
    {
        // Load the PDF using the WinRT Windows.Data.Pdf API
        StorageFile file = await StorageFile.GetFileFromPathAsync(pdfPath);
        PdfDocument pdfDoc = await PdfDocument.LoadFromFileAsync(file);

        // Render every page to a Bitmap then send to PrintDocument
        var pages = new List<Bitmap>();
        for (uint i = 0; i < pdfDoc.PageCount; i++)
        {
            using PdfPage page = pdfDoc.GetPage(i);
            var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            await page.RenderToStreamAsync(stream);

            using var dotnetStream = stream.AsStreamForRead();
            pages.Add(new Bitmap(dotnetStream));
        }

        // Marshal printing back onto the UI/STA thread
        var tcs = new TaskCompletionSource();
        System.Windows.Forms.Application.OpenForms[0]!.Invoke(() =>
        {
            int pageIndex = 0;
            using var pd = new PrintDocument();
            pd.PrintPage += (_, args) =>
            {
                Bitmap bmp = pages[pageIndex++];
                // Scale to fit the printable area while keeping aspect ratio
                Rectangle area = args.MarginBounds;
                float scale = Math.Min((float)area.Width / bmp.Width, (float)area.Height / bmp.Height);
                int w = (int)(bmp.Width * scale);
                int h = (int)(bmp.Height * scale);
                int x = area.X + (area.Width - w) / 2;
                int y = area.Y + (area.Height - h) / 2;
                args.Graphics!.DrawImage(bmp, x, y, w, h);
                args.HasMorePages = pageIndex < pages.Count;
            };
            pd.EndPrint += (_, _) =>
            {
                foreach (var b in pages) b.Dispose();
                tcs.SetResult();
            };
            pd.Print();
        });

        await tcs.Task;
    }
}
