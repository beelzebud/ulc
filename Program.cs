using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpCompress.Archives;
using SharpCompress.Common;

public class Program
{
    public static string remoteUrl = "https://buildbot.libretro.com/nightly/windows/x86_64/latest/";
    public static string assetsUrl = "https://buildbot.libretro.com/assets/frontend/assets.zip";
    public static string infoUrl = "https://buildbot.libretro.com/assets/frontend/info.zip";
    public static string databaseUrl = "https://buildbot.libretro.com/assets/frontend/database-rdb.zip";
    public static string retroarchUrl = "https://buildbot.libretro.com/nightly/windows/x86_64/RetroArch_update.7z";

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}

public class MainForm : Form
{
    private Button updateCoresButton;
    private Button updateAssetsButton;
    private Button updateCoreInfoButton;
    private Button updateDatabaseButton;
    private Button updateAllButton;
    private Button stopButton;
    private Button aboutButton;
    private Button updateRetroarchButton;
    private TextBox textBox;
    private TextBox dllPathTextBox;
    private TextBox assetsPathTextBox;
    private TextBox infoPathTextBox;
    private TextBox databasePathTextBox;
    private TextBox retroarchPathTextBox;
    private Button browseDllButton;
    private Button browseAssetsButton;
    private Button browseInfoButton;
    private Button browseDatabaseButton;
    private Button browseRetroarchButton;
    private bool isProcessRunning = false;
    private ProgressBar progressBar;
    private ProgressBar progressBarOverall;

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
        this.Size = new System.Drawing.Size(500, 650);
        this.Text = "libretro Updater";
        this.Icon = new System.Drawing.Icon(@"D:\Build\ulc\ulc\Resources\ulc.ico");
        this.BackColor = Color.DarkGray;

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

        updateCoresButton = new Button() { Text = "Update libretro Cores", Dock = DockStyle.Top };
        updateCoresButton.Click += new EventHandler(UpdateCoresButton_Click);
        dllPathTextBox = new TextBox() { Dock = DockStyle.Fill };
        dllPathTextBox.Text = @"D:\Emulators\RetroArch\RetroArch-Win64\cores\";

        updateAssetsButton = new Button() { Text = "Update libretro Assets", Dock = DockStyle.Top };
        updateAssetsButton.Click += new EventHandler(UpdateAssetsButton_Click);
        assetsPathTextBox = new TextBox() { Dock = DockStyle.Fill };
        assetsPathTextBox.Text = @"D:\Emulators\RetroArch\RetroArch-Win64\assets\";

        updateCoreInfoButton = new Button() { Text = "Update Core Info", Dock = DockStyle.Top };
        updateCoreInfoButton.Click += new EventHandler(UpdateCoreInfoButton_Click);
        infoPathTextBox = new TextBox() { Dock = DockStyle.Fill };
        infoPathTextBox.Text = @"D:\Emulators\RetroArch\RetroArch-Win64\info\";

        updateDatabaseButton = new Button() { Text = "Update libretro Database", Dock = DockStyle.Top };
        updateDatabaseButton.Click += new EventHandler(UpdateDatabaseButton_Click);
        databasePathTextBox = new TextBox() { Dock = DockStyle.Fill };
        databasePathTextBox.Text = @"D:\Emulators\RetroArch\RetroArch-Win64\database\";

        updateRetroarchButton = new Button() { Text = "Update Retroarch", Dock = DockStyle.Top };
        updateRetroarchButton.Click += new EventHandler(UpdateRetroarchButton_Click);
        retroarchPathTextBox = new TextBox() { Dock = DockStyle.Fill };
        retroarchPathTextBox.Text = @"D:\Emulators\RetroArch\";

        updateAllButton = new Button() { Text = "Update Everything", Dock = DockStyle.Top };
        updateAllButton.Click += new EventHandler(UpdateAllButton_Click);

        browseDllButton = new Button() { Text = "Browse", Dock = DockStyle.Right };
        browseDllButton.Click += new EventHandler(BrowseDllButton_Click);

        browseAssetsButton = new Button() { Text = "Browse", Dock = DockStyle.Right };
        browseAssetsButton.Click += new EventHandler(BrowseAssetsButton_Click);

        browseInfoButton = new Button() { Text = "Browse", Dock = DockStyle.Right };
        browseInfoButton.Click += new EventHandler(BrowseInfoButton_Click);

        browseDatabaseButton = new Button() { Text = "Browse", Dock = DockStyle.Right };
        browseDatabaseButton.Click += new EventHandler(BrowseDatabaseButton_Click);

        browseRetroarchButton = new Button() { Text = "Browse", Dock = DockStyle.Right };
        browseRetroarchButton.Click += new EventHandler(BrowseRetroarchButton_Click);

        progressBar = new ProgressBar() { Dock = DockStyle.Bottom, Height = 20 };

        progressBarOverall = new ProgressBar() { Dock = DockStyle.Bottom, Height = 20, Maximum = 5, Value = 0 }; // 5 steps for "Update All"

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

        TableLayoutPanel infoPanel = new TableLayoutPanel();
        infoPanel.Dock = DockStyle.Top;
        infoPanel.AutoSize = true;
        infoPanel.ColumnCount = 3;
        infoPanel.Controls.Add(new Label() { Text = "Info location:" }, 0, 0);
        infoPanel.Controls.Add(infoPathTextBox, 1, 0);
        infoPanel.Controls.Add(browseInfoButton, 2, 0);
        infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        TableLayoutPanel databasePanel = new TableLayoutPanel();
        databasePanel.Dock = DockStyle.Top;
        databasePanel.AutoSize = true;
        databasePanel.ColumnCount = 3;
        databasePanel.Controls.Add(new Label() { Text = "Database location:" }, 0, 0);
        databasePanel.Controls.Add(databasePathTextBox, 1, 0);
        databasePanel.Controls.Add(browseDatabaseButton, 2, 0);
        databasePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        databasePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        databasePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        TableLayoutPanel retroarchPanel = new TableLayoutPanel();
        retroarchPanel.Dock = DockStyle.Top;
        retroarchPanel.AutoSize = true;
        retroarchPanel.ColumnCount = 3;
        retroarchPanel.Controls.Add(new Label() { Text = "Retroarch location:" }, 0, 0);
        retroarchPanel.Controls.Add(retroarchPathTextBox, 1, 0);
        retroarchPanel.Controls.Add(browseRetroarchButton, 2, 0);
        retroarchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        retroarchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        retroarchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        updateCoresButton.ForeColor = Color.Lime;
        updateCoresButton.BackColor = Color.Black;
        updateAssetsButton.ForeColor = Color.Lime;
        updateAssetsButton.BackColor = Color.Black;
        updateCoreInfoButton.ForeColor = Color.Lime;
        updateCoreInfoButton.BackColor = Color.Black;
        updateDatabaseButton.ForeColor = Color.Lime;
        updateDatabaseButton.BackColor = Color.Black;
        updateRetroarchButton.ForeColor = Color.Lime;
        updateRetroarchButton.BackColor = Color.Black;
        updateAllButton.ForeColor = Color.Lime;
        updateAllButton.BackColor = Color.Black;    
        stopButton.ForeColor = Color.Lime;
        stopButton.BackColor = Color.Black;
        aboutButton.ForeColor = Color.Lime;
        aboutButton.BackColor = Color.Black;
        browseDllButton.ForeColor = Color.Lime;
        browseDllButton.BackColor = Color.Black;
        browseAssetsButton.ForeColor = Color.Lime;
        browseAssetsButton.BackColor = Color.Black;
        browseInfoButton.ForeColor = Color.Lime;
        browseInfoButton.BackColor = Color.Black;
        browseDatabaseButton.ForeColor = Color.Lime;
        browseDatabaseButton.BackColor = Color.Black;
        browseRetroarchButton.ForeColor = Color.Lime;
        browseRetroarchButton.BackColor = Color.Black;

        updateCoresButton.FlatStyle = FlatStyle.Flat;
        updateCoresButton.FlatAppearance.BorderColor = Color.Lime;
        updateAssetsButton.FlatStyle = FlatStyle.Flat;
        updateAssetsButton.FlatAppearance.BorderColor = Color.Lime;
        updateCoreInfoButton.FlatStyle = FlatStyle.Flat;
        updateCoreInfoButton.FlatAppearance.BorderColor = Color.Lime;
        updateDatabaseButton.FlatStyle = FlatStyle.Flat;
        updateDatabaseButton.FlatAppearance.BorderColor = Color.Lime;
        updateRetroarchButton.FlatStyle = FlatStyle.Flat;
        updateRetroarchButton.FlatAppearance.BorderColor = Color.Lime;
        updateAllButton.FlatStyle = FlatStyle.Flat;
        updateAllButton.FlatAppearance.BorderColor = Color.Lime;
        stopButton.FlatStyle = FlatStyle.Flat;
        stopButton.FlatAppearance.BorderColor = Color.Lime;
        browseDllButton.FlatStyle = FlatStyle.Flat;
        browseDllButton.FlatAppearance.BorderColor = Color.Lime;
        browseAssetsButton.FlatStyle = FlatStyle.Flat;
        browseAssetsButton.FlatAppearance.BorderColor = Color.Lime;
        browseInfoButton.FlatStyle = FlatStyle.Flat;
        browseInfoButton.FlatAppearance.BorderColor = Color.Lime;
        browseDatabaseButton.FlatStyle = FlatStyle.Flat;
        browseDatabaseButton.FlatAppearance.BorderColor = Color.Lime;
        browseRetroarchButton.FlatStyle = FlatStyle.Flat;
        browseRetroarchButton.FlatAppearance.BorderColor = Color.Lime;

        Controls.Add(textBoxPanel);
		Controls.Add(updateAllButton);
        Controls.Add(retroarchPanel);
        Controls.Add(databasePanel);
        Controls.Add(infoPanel);
        Controls.Add(assetsPanel);
        Controls.Add(dllPanel);
        Controls.Add(updateRetroarchButton);
        Controls.Add(updateDatabaseButton);
        Controls.Add(updateCoreInfoButton);
        Controls.Add(updateAssetsButton);
        Controls.Add(updateCoresButton);
        Controls.Add(progressBarOverall);
        Controls.Add(progressBar);
        Controls.Add(stopButton);
        Controls.Add(aboutButton);
    }
    private async void UpdateAllButton_Click(object sender, EventArgs e)
    {
        isProcessRunning = true;
        EnableButtons(false);
        progressBarOverall.Value = 0; // Reset the overall progress
        textBox.AppendText("Starting full update process...\r\n");

        try
        {
            await UpdateCoresAsync();
            progressBarOverall.Value++; // Increment overall progress

            await UpdateAssetsAsync();
            progressBarOverall.Value++;

            await UpdateCoreInfoAsync();
            progressBarOverall.Value++;

            await UpdateDatabaseAsync();
            progressBarOverall.Value++;

            await UpdateRetroarchAsync();
            progressBarOverall.Value++;

            textBox.AppendText("Full update process completed.\r\n");
        }
        catch (Exception ex)
        {
            textBox.AppendText($"Error during full update: {ex.Message}\r\n");
        }
        finally
        {
            EnableButtons(true);
            isProcessRunning = false;
        }
    }
    // New Async Methods for Updates
    private async Task UpdateCoresAsync()
    {
        textBox.AppendText("Now starting the core update process...\r\n");
        string dllDirectory = dllPathTextBox.Text;
        string[] dllFiles = Directory.GetFiles(dllDirectory, "*.dll");
        textBox.AppendText($"Found {dllFiles.Length} libretro cores in {dllDirectory}\r\n");

        foreach (string dllFile in dllFiles)
        {
            if (!isProcessRunning) break;

            string fileName = Path.GetFileNameWithoutExtension(dllFile);
            string zipUrl = Program.remoteUrl + fileName + ".dll.zip";
            string tempPath = Path.Combine(dllDirectory, "temp");

            try
            {
                await UpdateFilesAsync(tempPath, zipUrl, dllDirectory, textBox).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                textBox.AppendText($"Error updating {fileName}: {ex.Message}\r\n");
            }
        }
        textBox.AppendText("Libretro core updates have been completed.\r\n");
    }

    private async Task UpdateAssetsAsync()
    {
        textBox.AppendText("Now starting the asset update process...\r\n");
        string assetsDirectory = assetsPathTextBox.Text;
        string tempPath = Path.Combine(assetsDirectory, "temp");

        try
        {
            await UpdateFilesAsync(tempPath, Program.assetsUrl, assetsDirectory, textBox).ConfigureAwait(true);
            textBox.AppendText("Libretro asset updates have been completed.\r\n");
        }
        catch (Exception ex)
        {
            textBox.AppendText($"Error updating assets: {ex.Message}\r\n");
        }
    }

    private async Task UpdateCoreInfoAsync()
    {
        textBox.AppendText("Now starting the core info update process...\r\n");
        string infoDirectory = infoPathTextBox.Text;
        string tempPath = Path.Combine(infoDirectory, "temp");

        try
        {
            await UpdateFilesAsync(tempPath, Program.infoUrl, infoDirectory, textBox).ConfigureAwait(true);
            textBox.AppendText("Libretro core info updates have been completed.\r\n");
        }
        catch (Exception ex)
        {
            textBox.AppendText($"Error updating core info: {ex.Message}\r\n");
        }
    }

    private async Task UpdateDatabaseAsync()
    {
        textBox.AppendText("Now starting the database update process...\r\n");
        string databaseDirectory = databasePathTextBox.Text;
        string tempPath = Path.Combine(databaseDirectory, "temp");

        try
        {
            await UpdateFilesAsync(tempPath, Program.databaseUrl, databaseDirectory, textBox).ConfigureAwait(true);
            textBox.AppendText("Libretro database updates have been completed.\r\n");
        }
        catch (Exception ex)
        {
            textBox.AppendText($"Error updating database: {ex.Message}\r\n");
        }
    }

    private async Task UpdateRetroarchAsync()
    {
        textBox.AppendText("Now starting the Retroarch update process...\r\n");
        string retroarchDirectory = retroarchPathTextBox.Text;
        string tempPath = Path.Combine(retroarchDirectory, "temp");

        try
        {
            await UpdateFilesAsync7z(tempPath, Program.retroarchUrl, retroarchDirectory, textBox).ConfigureAwait(true);
            textBox.AppendText("Retroarch updates have been completed.\r\n");
        }
        catch (Exception ex)
        {
            textBox.AppendText($"Error updating Retroarch: {ex.Message}\r\n");
        }
    }

    private void EnableButtons(bool enable)
    {
        updateCoresButton.Enabled = enable;
        updateAssetsButton.Enabled = enable;
        updateCoreInfoButton.Enabled = enable;
        updateDatabaseButton.Enabled = enable;
        updateRetroarchButton.Enabled = enable;
		updateAllButton.Enabled = enable;
        stopButton.Enabled = !enable;
        browseDllButton.Enabled = enable;
        browseAssetsButton.Enabled = enable;
        browseInfoButton.Enabled = enable;
        browseDatabaseButton.Enabled = enable;
        browseRetroarchButton.Enabled = enable;
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
            string zipUrl = Program.remoteUrl + fileName + ".dll.zip";
            string tempPath = Path.Combine(dllDirectory, "temp");

            try
            {
                await UpdateFilesAsync(tempPath, zipUrl, dllDirectory, textBox).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                textBox.AppendText($"Network error updating {fileName}: {ex.Message}\r\n");
            }
            catch (IOException ex)
            {
                textBox.AppendText($"File error updating {fileName}: {ex.Message}\r\n");
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
            await UpdateFilesAsync(tempPath, Program.assetsUrl, assetsDirectory, textBox).ConfigureAwait(false);
            textBox.AppendText("Libretro asset updates have been completed.\r\n");
        }
        catch (HttpRequestException ex)
        {
            textBox.AppendText($"Network error updating assets: {ex.Message}\r\n");
        }
        catch (IOException ex)
        {
            textBox.AppendText($"File error updating assets: {ex.Message}\r\n");
        }
        catch (Exception ex)
        {
            textBox.AppendText($"Error updating assets: {ex.Message}\r\n");
        }

        EnableButtons(true);
    }

    private async void UpdateCoreInfoButton_Click(object sender, EventArgs e)
    {
        isProcessRunning = true;
        EnableButtons(false);

        textBox.AppendText("Now starting the core info update process...\r\n");

        string infoDirectory = infoPathTextBox.Text;
        string tempPath = Path.Combine(infoDirectory, "temp");

        try
        {
            await UpdateFilesAsync(tempPath, Program.infoUrl, infoDirectory, textBox).ConfigureAwait(false);
            textBox.AppendText("Libretro core info updates have been completed.\r\n");
        }
        catch (HttpRequestException ex)
        {
            textBox.AppendText($"Network error updating core info: {ex.Message}\r\n");
        }
        catch (IOException ex)
        {
            textBox.AppendText($"File error updating core info: {ex.Message}\r\n");
        }
        catch (Exception ex)
        {
            textBox.AppendText($"Error updating core info: {ex.Message}\r\n");
        }

        EnableButtons(true);
    }

    private async void UpdateDatabaseButton_Click(object sender, EventArgs e)
    {
        isProcessRunning = true;
        EnableButtons(false);

        textBox.AppendText("Now starting the database update process...\r\n");

        string databaseDirectory = databasePathTextBox.Text;
        string tempPath = Path.Combine(databaseDirectory, "temp");

        try
        {
            await UpdateFilesAsync(tempPath, Program.databaseUrl, databaseDirectory, textBox).ConfigureAwait(false);
            textBox.AppendText("Libretro database updates have been completed.\r\n");
        }
        catch (HttpRequestException ex)
        {
            textBox.AppendText($"Network error updating database: {ex.Message}\r\n");
        }
        catch (IOException ex)
        {
            textBox.AppendText($"File error updating database: {ex.Message}\r\n");
        }
        catch (Exception ex)
        {
            textBox.AppendText($"Error updating database: {ex.Message}\r\n");
        }

        EnableButtons(true);
    }

    private async void UpdateRetroarchButton_Click(object sender, EventArgs e)
    {
        isProcessRunning = true;
        EnableButtons(false);

        textBox.AppendText("Now starting the Retroarch update process...\r\n");

        string retroarchDirectory = retroarchPathTextBox.Text;
        string tempPath = Path.Combine(retroarchDirectory, "temp");

        try
        {
            await UpdateFilesAsync7z(tempPath, Program.retroarchUrl, retroarchDirectory, textBox).ConfigureAwait(false);
            textBox.AppendText("Retroarch updates have been completed.\r\n");
        }
        catch (HttpRequestException ex)
        {
            textBox.AppendText($"Network error updating Retroarch: {ex.Message}\r\n");
        }
        catch (IOException ex)
        {
            textBox.AppendText($"File error updating Retroarch: {ex.Message}\r\n");
        }
        catch (Exception ex)
        {
            textBox.AppendText($"Error updating Retroarch: {ex.Message}\r\n");
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
                progressBar.Maximum = archive.Entries.Count;
                progressBar.Value = 0;

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

                    progressBar.Value++;
                }
            }

            File.Delete(zipPath);
            Directory.Delete(tempPath, true);

            textBox.AppendText("Download and extraction completed\r\n");
        }
        catch (HttpRequestException ex)
        {
            textBox.AppendText($"Network error: {ex.Message}\r\n");
        }
        catch (IOException ex)
        {
            textBox.AppendText($"File error: {ex.Message}\r\n");
        }
        catch (Exception ex)
        {
            textBox.AppendText($"Error: {ex.Message}\r\n");
        }
    }

    private async Task UpdateFilesAsync7z(string tempPath, string sevenZipUrl, string destinationDirectory, TextBox textBox)
    {
        try
        {
            Directory.CreateDirectory(tempPath);
            string sevenZipPath = Path.Combine(tempPath, "temp.7z");

            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(sevenZipUrl).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false),
                                  fileStream = new FileStream(sevenZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await contentStream.CopyToAsync(fileStream).ConfigureAwait(false);
                    }
                }
            }

            using (var archive = ArchiveFactory.Open(sevenZipPath))
            {
                progressBar.Maximum = archive.Entries.Count();
                progressBar.Value = 0;

                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        string destinationPath = Path.Combine(destinationDirectory, entry.Key);
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                        entry.WriteToFile(destinationPath, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                        textBox.AppendText($"Extracted {entry.Key}\r\n");
                    }

                    progressBar.Value++;
                }
            }

            File.Delete(sevenZipPath);
            Directory.Delete(tempPath, true);

            textBox.AppendText("Download and extraction completed\r\n");
        }
        catch (HttpRequestException ex)
        {
            textBox.AppendText($"Network error: {ex.Message}\r\n");
        }
        catch (IOException ex)
        {
            textBox.AppendText($"File error: {ex.Message}\r\n");
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

    private void BrowseInfoButton_Click(object sender, EventArgs e)
    {
        using (FolderBrowserDialog dialog = new FolderBrowserDialog())
        {
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                infoPathTextBox.Text = dialog.SelectedPath;
            }
        }
    }

    private void BrowseDatabaseButton_Click(object sender, EventArgs e)
    {
        using (FolderBrowserDialog dialog = new FolderBrowserDialog())
        {
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                databasePathTextBox.Text = dialog.SelectedPath;
            }
        }
    }

    private void BrowseRetroarchButton_Click(object sender, EventArgs e)
    {
        using (FolderBrowserDialog dialog = new FolderBrowserDialog())
        {
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                retroarchPathTextBox.Text = dialog.SelectedPath;
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
            this.Size = new Size(225, 225);
            this.Icon = new System.Drawing.Icon(@"D:\Build\ulc\ulc\Resources\ulc.ico");

            PictureBox iconPictureBox = new PictureBox()
            {
                Image = Icon.ToBitmap(),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Dock = DockStyle.Fill,
                Height = 50,
                Width = 50,
            };

            Label titleLabel = new Label() { Text = "libretro Updater", Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter };
            Label versionLabel = new Label() { Text = "v1.1", Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter };
            Label infoLabel = new Label() { Text = "©2024 John N. Bilbrey and ChatGPT", Dock = DockStyle.Bottom, TextAlign = ContentAlignment.BottomCenter };

            Controls.Add(versionLabel);
            Controls.Add(titleLabel);
            Controls.Add(iconPictureBox);
            Controls.Add(infoLabel);
        }
    }
}
