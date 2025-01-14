using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;
using Action = System.Action;

using NetworkDetector.Class;

namespace NetworkDetector
{
    public partial class NetworkDetector : Form
    {
        // Fields read from App.config
        private readonly int _tcpPort;
        private readonly string _hourlyTaskName;
        private readonly string _baseUrl;
        private readonly string _versionNumber;
        private readonly string _dansApp;
        private readonly string _callpoplite;

        private readonly DataFetcher dataFetcher;
        private readonly WindowsSettingsManager _windowsSettingsManager;
        private CleanupManager _cleanupManager;
        private MaekoVersionUpdater _maekoVersionUpdater;

        private string gatheredData;

        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem VersionMenuItem;
        private ToolStripMenuItem TaskRun;
        private ToolStripMenuItem UpdateMenuItem;
        private System.Windows.Forms.Timer intervalTimer;
        private System.Windows.Forms.Timer cleanupTimer;

        private static readonly HttpClient client = new HttpClient();

        public NetworkDetector()
        {
            InitializeComponent();

            _tcpPort = int.Parse(ConfigurationManager.AppSettings["TcpPort"]);
            _hourlyTaskName = ConfigurationManager.AppSettings["HourlyTaskName"];
            _baseUrl = ConfigurationManager.AppSettings["BananaGoatsBaseUrl"];
            _versionNumber = ConfigurationManager.AppSettings["VersionNumber"];
            _dansApp = ConfigurationManager.AppSettings["DansApp"];
            _callpoplite = ConfigurationManager.AppSettings["CallPopLite"];

            dataFetcher = new DataFetcher(this);
            _windowsSettingsManager = new WindowsSettingsManager(this);

            // 3. Set up NotifyIcon, ContextMenu, and other UI elements
            notifyIcon = new NotifyIcon();
            contextMenu = new ContextMenuStrip();

            VersionMenuItem = new ToolStripMenuItem("Version : NA") { Enabled = false };
            contextMenu.Items.Add(VersionMenuItem);

            contextMenu.Items.Add("Show", null, ShowForm);

            UpdateMenuItem = new ToolStripMenuItem("Update")
            {
                Enabled = true
            };
            UpdateMenuItem.Click += button7_Click;
            contextMenu.Items.Add(UpdateMenuItem);

            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.Icon = this.Icon;
            notifyIcon.Visible = true;
            notifyIcon.Text = "Network Detector";

            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;

            this.MaximizeBox = false;
            this.MinimizeBox = true;

            // 4. Timer for sending data every 2 minutes
            intervalTimer = new System.Windows.Forms.Timer
            {
                Interval = 2 * 60 * 1000
            };
            intervalTimer.Tick += SendData;
            intervalTimer.Start();

            _cleanupManager = new CleanupManager(this, logTextBox)
            {
                CleanupInterval = 6 * 60 * 60 * 1000 // e.g., 6 hours in ms
            };

            _cleanupManager.StartCleanup();

            string connectionString = ConfigurationManager.ConnectionStrings["SQL"]?.ConnectionString;
            _maekoVersionUpdater = new MaekoVersionUpdater(
                connectionString,
                LogError,
                message => logTextBox.AppendText($"{DateTime.Now}: {message}\r\n")
                );


            this.Load += NetworkDetector_Load;
        }

        private async void NetworkDetector_Load(object sender, EventArgs e)
        {
            // Create scheduled task if not exists
            if (!IsScheduledTaskExists("Network Detector"))
            {
                CreateScheduledTask();
            }

            // Check Windows registry & GPO settings
            _windowsSettingsManager.CheckGpoSettings();

            if (IsScheduledTaskExists(_hourlyTaskName))
            {
                using (TaskService ts = new TaskService())
                {
                    ts.RootFolder.DeleteTask(_hourlyTaskName, false);
                    LogError($"{_hourlyTaskName} was found and removed.");
                }
            }


            try
            {
                string VersionNumber = Version.Text;
                VersionMenuItem.Text = $"Version Number {VersionNumber}";
                this.Text = $"Network Manager {VersionNumber}";

                await StaticSpecsAsync();
            }
            catch (Exception ex)
            {
                LogError($"Exception in NetworkDetector_Load: {ex.Message}");
            }
        }

        #region SendingData

        private async Task StaticSpecsAsync()
        {
            try
            {
                // Gather basic system data
                string machineNameValue = Environment.MachineName;
                string cpuInfo = dataFetcher.GetCPUInfo();
                string ramInfo = dataFetcher.GetRamInfo();
                string windowsOS = dataFetcher.GetWindowsOS();
                string buildNumber = dataFetcher.GetBuildNumber();

                // Update UI
                machineNameTextBox.Text = machineNameValue;
                cpuInfoTextBox.Text = cpuInfo;
                ramInfoTextBox.Text = ramInfo;
                windowsOsTextBox.Text = windowsOS;
                buildNumberTextBox.Text = buildNumber;

                logTextBox.Clear();

                // SharePoint info
                string latestFileDate = await dataFetcher.GetLatestSharepointFileDateAsync();
                latestFileDateTextBox.Text = latestFileDate;

                // Pending updates
                string pendingUpdates = await dataFetcher.GetPendingUpdatesAsync();
                pendingUpdatesTextBox.Text = pendingUpdates;
            }
            catch (Exception ex)
            {
                LogError($"Exception in StaticSpecs: {ex.Message}");
            }
        }

        private async Task GatherDynamicDataAsync()
        {
            try
            {
                (string wanIp, string isp) = await dataFetcher.GetWanIpAndIspAsync();

                // IP or ISP might fail—default to fallback text
                string displayWanIp = !string.IsNullOrEmpty(wanIp) ? wanIp : "IP Failed";
                string displayIsp = !string.IsNullOrEmpty(isp) ? isp : "ISP Failed";

                // Thread-safe UI update
                if (wanIpTextBox.InvokeRequired)
                {
                    wanIpTextBox.Invoke(new Action(() =>
                    {
                        wanIpTextBox.Text = displayWanIp;
                        ispTextBox.Text = displayIsp;
                    }));
                }
                else
                {
                    wanIpTextBox.Text = displayWanIp;
                    ispTextBox.Text = displayIsp;
                }

                string storageInfo = dataFetcher.GetStorageInfo();
                if (storageInfoTextBox.InvokeRequired)
                {
                    storageInfoTextBox.Invoke(new Action(() =>
                    {
                        storageInfoTextBox.Text = storageInfo;
                    }));
                }
                else
                {
                    storageInfoTextBox.Text = storageInfo;
                }
            }
            catch (Exception ex)
            {
                LogError($"Exception in GatherDynamicData: {ex.Message}");
            }
        }

        private async void SendData(object sender, EventArgs e)
        {
            logTextBox.Clear();

            if (!this.IsHandleCreated)
            {
                return;
            }

            // 1. Gather dynamic data
            await GatherDynamicDataAsync();

            // 2. Build data string
            string machineName = machineNameTextBox.Text;
            string cpuInfo = cpuInfoTextBox.Text;
            string ramInfo = ramInfoTextBox.Text;
            string wanIp = wanIpTextBox.Text;
            string isp = ispTextBox.Text;
            string storageInfo = storageInfoTextBox.Text;
            string windowsOS = windowsOsTextBox.Text;
            string buildNumber = buildNumberTextBox.Text;
            string version = Version.Text; // from the control
            string pendingUpdates = pendingUpdatesTextBox.Text;
            string lastSharePointFile = latestFileDateTextBox.Text;

            // Prepare data
            string gatheredData = $"(MainData)|{machineName}|{wanIp}|{isp}|{cpuInfo}|{ramInfo}|{storageInfo}|{windowsOS}|{buildNumber}|{version}|{pendingUpdates}|{lastSharePointFile}";

            if (string.IsNullOrEmpty(gatheredData))
            {
                logTextBox.AppendText("No data to send.\r\n");
                return;
            }

            // IP address from user input (ipAddressTextBox)
            string serverIp = ipAddressTextBox.Text;

            try
            {
                using (var tcpClient = new TcpClient())
                {
                    logTextBox.AppendText($"Attempting to connect to server at {serverIp}:{_tcpPort}\r\n");

                    // 3. Connect & send
                    await tcpClient.ConnectAsync(serverIp, _tcpPort);
                    logTextBox.AppendText("Connected to server\r\n");

                    using (var stream = tcpClient.GetStream())
                    {
                        byte[] data = Encoding.UTF8.GetBytes(gatheredData);
                        await stream.WriteAsync(data, 0, data.Length);
                        logTextBox.AppendText("Data Sent");

                        // 4. Get response
                        byte[] buffer = new byte[1024];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        logTextBox.AppendText($"Received: {response}\r\n");
                    }

                    tcpClient.Close();

                    // 5. Check DB columns for updates
                    await CheckAndUpdateTillUpdaterColumn(machineName);
                    await CheckAndUpdateCallPopLiteColumn(machineName);
                    await CheckAndUpdateAppUpdateColumn(machineName);

                    await _maekoVersionUpdater.CheckAndUpdateMaekoVersionsAsync(machineName);


                }
            }
            catch (SocketException ex)
            {
                LogError($"SocketException: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogError($"Exception: {ex.Message}");
            }
        }

        private async void SendButton_Click(object sender, EventArgs e)
        {
            SendData(sender, e);
        }

        #endregion

        #region TaskSetup

        private void CreateScheduledTask()
        {
            RemoveFromStartup();

            try
            {
                string taskName = "Network Detector";
                string exePath = @"C:\Network Detector\NetworkDetector.exe";

                string taskCreateCommand =
                    $"schtasks /create /tn \"{taskName}\" /tr \"\\\"{exePath}\\\"\" /sc onlogon /rl highest /f";

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {taskCreateCommand}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Verb = "runas",
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (process.ExitCode != 0)
                    {
                        MessageBox.Show($"Failed to create scheduled task. Error: {error}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    Console.WriteLine("Output: " + output);
                    Console.WriteLine("Error: " + error);
                }
            }
            catch (Exception ex)
            {
                LogError($"An exception occurred: {ex.Message}");
            }
        }

        private bool IsScheduledTaskExists(string taskName)
        {
            using (TaskService ts = new TaskService())
            {
                Microsoft.Win32.TaskScheduler.Task task = ts.FindTask(taskName, true);
                return task != null;
            }
        }

        private void RemoveFromStartup()
        {
            string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = Path.Combine(startupPath, "NetworkDetector.lnk");
            if (File.Exists(shortcutPath))
            {
                try
                {
                    File.Delete(shortcutPath);
                }
                catch (Exception ex)
                {
                    LogError($"Failed to remove startup shortcut: {ex.Message}");
                }
            }
        }

        public bool IsUserAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        #endregion

        #region Buttons

        private void ShowForm(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        private void HideForm(object sender, EventArgs e)
        {
            this.Hide();
            this.ShowInTaskbar = false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            ipAddressTextBox.ReadOnly = !checkBox1.Checked;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Example: create a .bat file and schedule it
            string batFilePath = @"C:\Scripts\Maeko to Onedrive.bat";

            try
            {
                // Ensure that the directory 'C:\Scripts' exists
                string scriptsDirectory = Path.GetDirectoryName(batFilePath);
                if (!Directory.Exists(scriptsDirectory))
                {
                    Directory.CreateDirectory(scriptsDirectory);
                }

                // Get the store name from the textbox
                string storeName = storenametxt.Text;

                // Create the .bat file content
                string batFileContent =
                    @"xcopy C:\Maeko\UTILS ""C:\Users\admin\OneDrive - Ableworld UK\" + storeName + @""" /i /d /e /c /y"
                    + Environment.NewLine +
                    @"xcopy C:\Maeko\UTILS ""C:\Users\admin\OneDrive - Ableworld UK\" + storeName + @" Sharepoint"" /i /d /e /c /y";

                // Write the content to the .bat file
                File.WriteAllText(batFilePath, batFileContent);

                // Create a new task definition
                using (TaskService ts = new TaskService())
                {
                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = "Runs a Maeko to Onedrive.bat file every hour every day";

                    // Start at 09:00
                    DateTime startTime = DateTime.Today.AddHours(9);

                    DailyTrigger dailyTrigger = new DailyTrigger
                    {
                        DaysInterval = 1,
                        StartBoundary = startTime
                    };
                    dailyTrigger.Repetition = new RepetitionPattern(TimeSpan.FromHours(1), TimeSpan.FromDays(1));
                    td.Triggers.Add(dailyTrigger);

                    td.Actions.Add(new ExecAction(batFilePath, null, null));

                    ts.RootFolder.RegisterTaskDefinition(@"Maeko To Onedrive", td);
                }

                MessageBox.Show("Task created successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogError($"Error creating task: {ex.Message}");
            }
        }

        private void Button6_Click(object sender, EventArgs e)
        {
            panel1.Visible = true;
            panel3.Visible = false;
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            // Download updated "Network Detector"
            await DownloadVersionFolderAsync(_versionNumber, "Network Detector");
        }

        private void button8_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
            panel3.Visible = true;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string machineName = machineNameTextBox.Text;
            await CheckAndUpdateTillUpdaterColumn(machineName);
            await CheckAndUpdateCallPopLiteColumn(machineName);
            await CheckAndUpdateAppUpdateColumn(machineName);



        }

        private void button2_Click(object sender, EventArgs e)
        {
            _windowsSettingsManager.ResetFeatureUpdateVersionPolicy();
            _windowsSettingsManager.WindowsUpdate();
            _windowsSettingsManager.CheckGpoSettings();
        }

        #endregion

        #region Updater

        private async Task CheckAndUpdateAppUpdateColumn(string machineName)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["SQL"]?.ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string selectQuery = "SELECT AppUpdate FROM Computers WHERE MachineName = @MachineName";
                    string updateQuery = "UPDATE Computers SET AppUpdate = 'No' WHERE MachineName = @MachineName";

                    using (SqlCommand selectCommand = new SqlCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@MachineName", machineName);
                        var appUpdateValue = await selectCommand.ExecuteScalarAsync();

                        if (appUpdateValue != null
                            && appUpdateValue.ToString().Equals("Yes", StringComparison.OrdinalIgnoreCase))
                        {
                            using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                            {
                                updateCommand.Parameters.AddWithValue("@MachineName", machineName);
                                await updateCommand.ExecuteNonQueryAsync();

                                logTextBox.AppendText($"Update Requested for : {machineName}.\r\n");
                                await DownloadVersionFolderAsync(_versionNumber, "Network Detector");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Database Exception: {ex.Message}. Skipping update and moving on.");
            }
        }

        private async Task CheckAndUpdateTillUpdaterColumn(string machineName)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["SQL"]?.ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string selectQuery = "SELECT TillUpdater FROM Computers WHERE MachineName = @MachineName";
                    string updateQuery = "UPDATE Computers SET TillUpdater = 'No' WHERE MachineName = @MachineName";

                    using (SqlCommand selectCommand = new SqlCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@MachineName", machineName);
                        var appUpdateValue = await selectCommand.ExecuteScalarAsync();

                        if (appUpdateValue != null
                            && appUpdateValue.ToString().Equals("Yes", StringComparison.OrdinalIgnoreCase))
                        {
                            using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                            {
                                updateCommand.Parameters.AddWithValue("@MachineName", machineName);
                                await updateCommand.ExecuteNonQueryAsync();

                                logTextBox.AppendText($"Till Updater Update Requested for : {machineName}.\r\n");
                                await DownloadVersionFolderAsync(_dansApp, "Till Updater");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Database Exception: {ex.Message}. Skipping update and moving on.");
            }
        }

        private async Task CheckAndUpdateCallPopLiteColumn(string machineName)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["SQL"]?.ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string selectQuery = "SELECT CallPopLite FROM Computers WHERE MachineName = @MachineName";
                    string updateQuery = "UPDATE Computers SET CallPopLite = 'No' WHERE MachineName = @MachineName";

                    using (SqlCommand selectCommand = new SqlCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@MachineName", machineName);
                        var appUpdateValue = await selectCommand.ExecuteScalarAsync();

                        if (appUpdateValue != null
                            && appUpdateValue.ToString().Equals("Yes", StringComparison.OrdinalIgnoreCase))
                        {
                            using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                            {
                                updateCommand.Parameters.AddWithValue("@MachineName", machineName);
                                await updateCommand.ExecuteNonQueryAsync();

                                logTextBox.AppendText($"Till Updater Update Requested for : {machineName}.\r\n");
                                await DownloadVersionFolderAsync(_callpoplite, "Call Pop Lite");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Database Exception: {ex.Message}. Skipping update and moving on.");
            }
        }

        private async Task DownloadVersionFolderAsync(string version, string folderName)
        {
            string downloadUrl = $"{_baseUrl}BG%20Menu/{Uri.EscapeDataString(version)}";
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), folderName);
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            string tempZipFile = Path.Combine(Path.GetTempPath(), $"{version}.zip");
            string tempExtractPath = Path.Combine(Path.GetTempPath(), $"Extracted_{version}");

            try
            {
                // -------------------------------
                // 1) RETRY DOWNLOAD PART
                // -------------------------------
                int maxRetries = 3;
                TimeSpan backoffDelay = TimeSpan.FromSeconds(30);

                bool downloadSucceeded = false;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            // Possibly increase the timeout, but still keep it finite
                            client.Timeout = TimeSpan.FromMinutes(10);

                            LogError($"Download attempt {attempt} starting...");

                            HttpResponseMessage response = await client.GetAsync(downloadUrl);
                            response.EnsureSuccessStatusCode(); // Throws if not 2xx

                            byte[] fileData = await response.Content.ReadAsByteArrayAsync();
                            File.WriteAllBytes(tempZipFile, fileData);

                            // If we get here, download succeeded
                            downloadSucceeded = true;
                            LogError("Download succeeded!");
                            break;
                        }
                    }
                    catch (HttpRequestException ex) when (attempt < maxRetries)
                    {
                        // If it’s a network/server error, we can log and wait before retrying
                        LogError($"Download attempt {attempt} failed: {ex.Message}. Retrying in {backoffDelay.TotalSeconds} seconds...");
                        await Task.Delay(backoffDelay);
                    }
                    catch (TaskCanceledException ex) when (attempt < maxRetries)
                    {
                        // Occurs if .NET canceled due to timeout
                        LogError($"Download timed out on attempt {attempt}: {ex.Message}. Retrying in {backoffDelay.TotalSeconds} seconds...");
                        await Task.Delay(backoffDelay);
                    }
                }

                // If after all retries we still haven't succeeded, bail out
                if (!downloadSucceeded)
                {
                    LogError("All download attempts have failed. Aborting.");
                    return;
                }

                // -----------------------------------------
                // 2) PROCESS THE DOWNLOADED FILE (if any)
                // -----------------------------------------
                ZipFile.ExtractToDirectory(tempZipFile, tempExtractPath);

                CopyFilesRecursively(
                    new DirectoryInfo(tempExtractPath),
                    new DirectoryInfo(appDataPath));

                File.Delete(tempZipFile);
                Directory.Delete(tempExtractPath, true);

                // If the extracted folder has an Update.bat, run it
                string batFilePath = Path.Combine(appDataPath, "Update.bat");
                RunBatFile(batFilePath);
            }
            catch (Exception ex)
            {
                LogError($"Error downloading the version folder: {ex.Message}");
            }
        }

        private void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                DirectoryInfo targetSubDir = target.CreateSubdirectory(dir.Name);
                CopyFilesRecursively(dir, targetSubDir);
            }

            foreach (FileInfo file in source.GetFiles())
            {
                string targetFilePath = Path.Combine(target.FullName, file.Name);
                file.CopyTo(targetFilePath, true);
            }
        }

        private void RunBatFile(string batFilePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(batFilePath) && File.Exists(batFilePath))
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = batFilePath,
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    Process.Start(processInfo);
                }
                else
                {
                    LogError("The .bat file does not exist or could not be found.");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error running the .bat file: {ex.Message}");
            }
        }


        #endregion

        #region Helper Methods

        public void LogError(string message)
        {
            AppendLog($"ERROR: {message}");
        }

        private void AppendLog(string message)
        {
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action(() =>
                {
                    logTextBox.AppendText($"{DateTime.Now}: {message}\r\n");
                }));
            }
            else
            {
                logTextBox.AppendText($"{DateTime.Now}: {message}\r\n");
            }
        }

        #endregion

        #region Form Overrides

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // CS_NOCLOSE = 0x200 to disable close button
                cp.ClassStyle |= 0x200;
                return cp;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide(); // Minimizes to tray
            }
        }

        #endregion

        private async void button5_Click(object sender, EventArgs e)
        {
            // Instantiate the winget manager.
            WingetManager wingetManager = new WingetManager();

            // Define the list of programs you want to check and uninstall.
            string[] programsToUninstall = { 
                "Microsoft OneNote - en-us", 
                "Microsoft OneNote - de-de", 
                "Microsoft OneNote - fr-fr",
                "Microsoft OneNote - nl-nl",
                "Microsoft OneNote - it-it" 
            };

            foreach (string program in programsToUninstall)
            {
                try
                {
                    bool isInstalled = await wingetManager.IsProgramInstalledAsync(program);

                    if (isInstalled)
                    {
                        logTextBox.AppendText($"Uninstalling {program}...\r\n");

                        await wingetManager.UninstallProgramAsync(program);

                        logTextBox.AppendText($"{program} uninstalled.\r\n");
                    }
                    else
                    {
                        logTextBox.AppendText($"{program} is not installed.\r\n");
                    }
                }
                catch (Exception ex)
                {

                    logTextBox.AppendText($"Error uninstalling {program}: {ex.Message}\r\n");
                }
            }
        }
    }
}
