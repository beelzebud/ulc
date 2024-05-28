using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

public class Program
{
    static string remoteUrl = "https://buildbot.libretro.com/nightly/windows/x86_64/latest/";
    static string assetsUrl = "https://buildbot.libretro.com/assets/frontend/assets.zip";

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }

    public class MainForm : Form
    {
        private Button updateCoresButton;
        private Button updateAssetsButton;
        private Button stopButton;
        private Button aboutButton;
        private TextBox textBox;
        private TextBox dllPathTextBox;
        private TextBox assetsPathTextBox;
        private Button browseDllButton;
        private Button browseAssetsButton;
        private bool isProcessRunning = false;

        public MainForm()
        {
            try
            {
                InitializeComponents();
                stopButton.Enabled = false; // Initially disable the Stop button
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred during initialization: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void InitializeComponents()
        {
            this.Size = new System.Drawing.Size(500, 400);
            this.Text = "Update libretro Cores and Assets";
            this.Icon = new System.Drawing.Icon(@"D:\Build\ulc\ulc\Resources\ulc.ico");
            this.BackColor = Color.DarkGray;

            updateCoresButton = new Button() { Text = "Update libretro Cores", Dock = DockStyle.Top };
            updateCoresButton.Click += new EventHandler(UpdateCoresButton_Click);

            updateAssetsButton = new Button() { Text = "Update libretro Assets", Dock = DockStyle.Top };
            updateAssetsButton.Click += new EventHandler(UpdateAssetsButton_Click);

            stopButton = new Button() { Text = "Stop", Dock = DockStyle.Bottom };
            stopButton.Click += new EventHandler(StopButton_Click);

            aboutButton = new Button() { Text = "About", Dock = DockStyle.Bottom };
            aboutButton.Click += AboutButton_Click;
            aboutButton.FlatStyle = FlatStyle.Flat;
            aboutButton.FlatAppearance.BorderColor = Color.Red;

            textBox = new TextBox() { Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, AcceptsReturn = true };

            Panel textBoxPanel = new Panel();
            textBoxPanel.Dock = DockStyle.Fill;
            textBoxPanel.Controls.Add(textBox);
            textBox.ForeColor = Color.Lime;
            textBox.BackColor = Color.Black;

            dllPathTextBox = new TextBox() { Dock = DockStyle.Fill };
            dllPathTextBox.Text = @"D:\Emulators\RetroArch\RetroArch-Win64\cores\";

            assetsPathTextBox = new TextBox() { Dock = DockStyle.Fill };
            assetsPathTextBox.Text = @"D:\Emulators\RetroArch\RetroArch-Win64\assets\";

            browseDllButton = new Button() { Text = "Browse", Dock = DockStyle.Right };
            browseDllButton.Click += new EventHandler(BrowseDllButton_Click);

            browseAssetsButton = new Button() { Text = "Browse", Dock = DockStyle.Right };
            browseAssetsButton.Click += new EventHandler(BrowseAssetsButton_Click);

            TableLayoutPanel dllPanel = new TableLayoutPanel();
            dllPanel.Dock = DockStyle.Top;
            dllPanel.AutoSize = true;
            dllPanel.ColumnCount = 3;
            dllPanel.Controls.Add(new Label() { Text = "Core location:" }, 0, 0);
            dllPanel.Controls.Add(dllPathTextBox, 1, 0);
            dllPanel.Controls.Add(browseDllButton, 2, 0);
            dllPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            dllPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            dllPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            TableLayoutPanel assetsPanel = new TableLayoutPanel();
            assetsPanel.Dock = DockStyle.Top;
            assetsPanel.AutoSize = true;
            assetsPanel.ColumnCount = 3;
            assetsPanel.Controls.Add(new Label() { Text = "Assets location:" }, 0, 0);
            assetsPanel.Controls.Add(assetsPathTextBox, 1, 0);
            assetsPanel.Controls.Add(browseAssetsButton, 2, 0);
            assetsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            assetsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            assetsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            updateCoresButton.ForeColor = Color.Lime;
            updateCoresButton.BackColor = Color.Black;
            updateAssetsButton.ForeColor = Color.Lime;
            updateAssetsButton.BackColor = Color.Black;
            stopButton.ForeColor = Color.Lime;
            stopButton.BackColor = Color.Black;
            aboutButton.ForeColor = Color.Lime;
            aboutButton.BackColor = Color.Black;
            browseDllButton.ForeColor = Color.Lime;
            browseDllButton.BackColor = Color.Black;
            browseAssetsButton.ForeColor = Color.Lime;
            browseAssetsButton.BackColor = Color.Black;

            updateCoresButton.FlatStyle = FlatStyle.Flat;
            updateCoresButton.FlatAppearance.BorderColor = Color.Lime;
            updateAssetsButton.FlatStyle = FlatStyle.Flat;
            updateAssetsButton.FlatAppearance.BorderColor = Color.Lime;
            stopButton.FlatStyle = FlatStyle.Flat;
            stopButton.FlatAppearance.BorderColor = Color.Lime;
            browseDllButton.FlatStyle = FlatStyle.Flat;
            browseDllButton.FlatAppearance.BorderColor = Color.Lime;
            browseAssetsButton.FlatStyle = FlatStyle.Flat;
            browseAssetsButton.FlatAppearance.BorderColor = Color.Lime;

            Controls.Add(textBoxPanel);
            Controls.Add(assetsPanel);
            Controls.Add(dllPanel);
            Controls.Add(updateAssetsButton);
            Controls.Add(updateCoresButton);
            Controls.Add(stopButton);
            Controls.Add(aboutButton);
        }

        private void EnableButtons(bool enable)
        {
            updateCoresButton.Enabled = enable;
            updateAssetsButton.Enabled = enable;
            stopButton.Enabled = !enable;
            browseDllButton.Enabled = enable;
            browseAssetsButton.Enabled = enable;
        }

        private void AboutButton_Click(object sender, EventArgs e)
        {
            AboutForm aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            isProcessRunning = false;
            stopButton.Enabled = false;
            textBox.AppendText("Process stopped by user.\r\n");
        }

        private async void UpdateCoresButton_Click(object sender, EventArgs e)
        {
            isProcessRunning = true;
            EnableButtons(false);

            textBox.AppendText("Now starting the core update process...\r\n");

            string dllDirectory = dllPathTextBox.Text;
            string[] dllFiles = Directory.GetFiles(dllDirectory, "*.dll");

            textBox.AppendText($"Found {dllFiles.Length} libretro cores in {dllDirectory}\r\n");

            foreach (string dllFile in dllFiles)
            {
                if (!isProcessRunning)
                    break;

                string fileName = Path.GetFileNameWithoutExtension(dllFile);
                string zipUrl = remoteUrl + fileName + ".dll.zip";
                string tempPath = Path.Combine(dllDirectory, "temp");

                try
                {
                    await UpdateFilesAsync(tempPath, zipUrl, dllDirectory, textBox).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    textBox.AppendText($"Error updating {fileName}: {ex.Message}\r\n");
                }
            }

            textBox.AppendText("Libretro core updates have been completed.\r\n");
            EnableButtons(true);
        }

        private async void UpdateAssetsButton_Click(object sender, EventArgs e)
        {
            isProcessRunning = true;
            EnableButtons(false);

            textBox.AppendText("Now starting the asset update process...\r\n");

            string assetsDirectory = assetsPathTextBox.Text;
            string tempPath = Path.Combine(assetsDirectory, "temp");

            try
            {
                await UpdateFilesAsync(tempPath, assetsUrl, assetsDirectory, textBox).ConfigureAwait(false);
                textBox.AppendText("Libretro asset updates have been completed.\r\n");
            }
            catch (Exception ex)
            {
                textBox.AppendText($"Error updating assets: {ex.Message}\r\n");
            }

            EnableButtons(true);
        }

        private async Task UpdateFilesAsync(string tempPath, string zipUrl, string destinationDirectory, TextBox textBox)
        {
            try
            {
                Directory.CreateDirectory(tempPath);
                string zipPath = Path.Combine(tempPath, "temp.zip");

                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage response = await client.GetAsync(zipUrl).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        using (Stream contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false),
                                      fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await contentStream.CopyToAsync(fileStream).ConfigureAwait(false);
                        }
                    }
                }

                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            string directoryPath = Path.Combine(destinationDirectory, entry.FullName);
                            Directory.CreateDirectory(directoryPath);
                            continue;
                        }

                        string destFilePath = Path.Combine(destinationDirectory, entry.FullName);
                        string tempFilePath = Path.Combine(tempPath, entry.FullName);

                        Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath));
                        entry.ExtractToFile(tempFilePath, true);

                        if (File.Exists(destFilePath))
                        {
                            DateTime remoteLastModified = entry.LastWriteTime.DateTime;
                            DateTime localLastModified = File.GetLastWriteTime(destFilePath);

                            if (remoteLastModified > localLastModified)
                            {
                                File.Copy(tempFilePath, destFilePath, true);
                                textBox.AppendText($"Updated {entry.FullName}\r\n");
                            }
                            else
                            {
                                textBox.AppendText($"Skipping {entry.FullName} (up to date)\r\n");
                            }
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destFilePath));
                            File.Copy(tempFilePath, destFilePath);
                            textBox.AppendText($"Copied {entry.FullName} (new file)\r\n");
                        }
                    }
                }

                File.Delete(zipPath);
                Directory.Delete(tempPath, true);

                textBox.AppendText("Download and extraction completed\r\n");
            }
            catch (Exception ex)
            {
                textBox.AppendText($"Error: {ex.Message}\r\n");
            }
        }

        private void BrowseDllButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    dllPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void BrowseAssetsButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    assetsPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        public class AboutForm : Form
        {
            public AboutForm()
            {
                InitializeComponents();
            }

            private void InitializeComponents()
            {
                this.Text = "About";
                this.Size = new Size(225, 125);
                this.Icon = new System.Drawing.Icon(@"D:\Build\ulc\ulc\Resources\ulc.ico");

                PictureBox iconPictureBox = new PictureBox()
                {
                    Image = Icon.ToBitmap(),
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    Dock = DockStyle.Fill,
                    Height = 25,
                    Width = 25,
                };

                Label titleLabel = new Label() { Text = "Update libretro Cores and assets", Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter };
                Label infoLabel = new Label() { Text = "©2024 John N. Bilbrey and ChatGPT", Dock = DockStyle.Bottom, TextAlign = ContentAlignment.BottomCenter };

                Controls.Add(titleLabel);
                Controls.Add(iconPictureBox);
                Controls.Add(infoLabel);
            }
        }
    }
}
