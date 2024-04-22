using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

public class Program
{
    static string remoteUrl = "https://buildbot.libretro.com/nightly/windows/x86_64/latest/";

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }

    public class MainForm : Form
    {
        Button button;
        TextBox textBox;
        TextBox dllPathTextBox;
        Button browseButton;

        public MainForm()
        {
            this.Size = new System.Drawing.Size(550, 600);
            this.Text = "Update libretro Cores";

            button = new Button() { Text = "Update DLLs", Dock = DockStyle.Top };
            button.Click += new EventHandler(Button_Click);

            textBox = new TextBox() { Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, AcceptsReturn = true };

            dllPathTextBox = new TextBox() { Dock = DockStyle.Fill };
            dllPathTextBox.Text = @"D:\Emulators\RetroArch\RetroArch-Win64\cores\"; // Default path

            browseButton = new Button() { Text = "Browse", Dock = DockStyle.Right };
            browseButton.Click += new EventHandler(BrowseButton_Click);

            // Create a new TableLayoutPanel.
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Top;
            panel.AutoSize = true;
            panel.ColumnCount = 3;

            // Add the label and browse button to the panel.
            panel.Controls.Add(new Label() { Text = "libretro Cores:" }, 0, 0);
            panel.Controls.Add(browseButton, 1, 0);
            panel.Controls.Add(dllPathTextBox, 2, 0);

            // Set the column style to make the text box stretch to the edge of the window.
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Add the panel and button to the form.
            Controls.Add(panel);
            Controls.Add(button);
            Controls.Add(textBox);
        }

        private async void Button_Click(object sender, EventArgs e)
        {
            textBox.AppendText("Button clicked, starting update process...\r\n");

            string dllDirectory = dllPathTextBox.Text; // The directory where your DLL files are located
            string[] dllFiles = Directory.GetFiles(dllDirectory, "*.dll"); // Get all DLL files in the directory

            textBox.AppendText($"Found {dllFiles.Length} DLL files in {dllDirectory}\r\n");

            foreach (string dllFile in dllFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(dllFile); // Get the file name without the extension
                string zipUrl = remoteUrl + fileName + ".dll.zip"; // Append ".dll.zip" to the file name

                string tempPath = Path.Combine(dllDirectory, "temp"); // Temporary directory

                try
                {
                    await UpdateDLLsAsync(tempPath, zipUrl, dllDirectory, textBox);
                }
                catch (Exception ex)
                {
                    textBox.AppendText($"Error: {ex.Message}\r\n");
                    // Log additional exception details if needed
                }
            }
        }

        private async Task UpdateDLLsAsync(string tempPath, string zipUrl, string userDirectory, TextBox textBox)
        {
            try
            {
                // Ensure the destination directory exists
                Directory.CreateDirectory(tempPath);

                string zipPath = Path.Combine(tempPath, "temp.zip");

                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage response = await client.GetAsync(zipUrl))
                    {
                        response.EnsureSuccessStatusCode();

                        using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                                      fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await contentStream.CopyToAsync(fileStream);
                        }
                    }
                }

                // Get the names of the files in the zip file
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string fileName = Path.GetFileName(entry.FullName);
                        string destFilePath = Path.Combine(userDirectory, fileName);
                        string tempFilePath = Path.Combine(tempPath, entry.FullName);

                        // Extract files to temp directory
                        entry.ExtractToFile(tempFilePath, true);

                        // Check if the file exists in the user directory
                        if (File.Exists(destFilePath))
                        {
                            DateTime remoteLastModified = entry.LastWriteTime.DateTime; // Convert DateTimeOffset to DateTime
                            DateTime localLastModified = File.GetLastWriteTime(destFilePath);

                            // Compare last modified timestamps
                            if (remoteLastModified > localLastModified)
                            {
                                // Remote file is newer, update it
                                File.Copy(tempFilePath, destFilePath, true);
                                textBox.AppendText($"Updated {fileName}\r\n");
                            }
                            else
                            {
                                // Local file is up to date
                                textBox.AppendText($"Skipping {fileName} (up to date)\r\n");
                            }
                        }
                        else
                        {
                            // File doesn't exist locally, copy it
                            File.Copy(tempFilePath, destFilePath);
                            textBox.AppendText($"Copied {fileName} (new file)\r\n");
                        }
                    }
                }

                // Clean up: delete temporary files
                File.Delete(zipPath);
                Directory.Delete(tempPath, true);

                textBox.AppendText("Download and extraction completed\r\n");
            }
            catch (Exception ex)
            {
                textBox.AppendText($"Error: {ex.Message}\r\n");
                // Log additional exception details if needed
            }
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    dllPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }
    }
}

