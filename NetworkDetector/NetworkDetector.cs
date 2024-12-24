using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Data.SqlClient;
using Task = System.Threading.Tasks.Task;
using Action = System.Action;

namespace NetworkDetector
{
    public partial class NetworkDetector : Form
    {
        private const int TcpPort = 50550;
        private const string HourlyTaskName = "Network Detector Hourly";
        private string headOfficeIpAddress = "195.62.221.62";

        private string gatheredData;

        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem lastIpMenuItem;
        private ToolStripMenuItem hourlyTaskMenuItem;
        private ToolStripMenuItem startUpMenuItem;
        private ToolStripMenuItem VersionMenuItem;
        private ToolStripMenuItem TaskRun;
        private ToolStripMenuItem UpdateMenuItem;
        private System.Windows.Forms.Timer intervalTimer;

        // Variables for folder download functionality
        private const int port = 50547;
        private const string serverIp = "http://bananagoats.co.uk";
        private readonly string baseUrl = $"{serverIp}:{port}/";
        private const string versionNumber = "Network Detector Update";

        private static readonly HttpClient client = new HttpClient();

        private DataFetcher dataFetcher;

        public NetworkDetector()
        {
            InitializeComponent();

            dataFetcher = new DataFetcher(this);

            // Initialize NotifyIcon and ContextMenuStrip
            notifyIcon = new NotifyIcon();
            contextMenu = new ContextMenuStrip();

            // Initialize Context Menu Items
            VersionMenuItem = new ToolStripMenuItem("Version : NA");
            VersionMenuItem.Enabled = false;
            contextMenu.Items.Add(VersionMenuItem);

            lastIpMenuItem = new ToolStripMenuItem("Last Recorded IP: N/A");
            lastIpMenuItem.Enabled = false;
            contextMenu.Items.Add(lastIpMenuItem);

            contextMenu.Items.Add("Show", null, ShowForm);

            TaskRun = new ToolStripMenuItem("Start Up Task");
            TaskRun.Click += new EventHandler(CreateScheduledTask);

            if (!IsScheduledTaskExists("Network Detector"))
            {
                contextMenu.Items.Add(TaskRun);
            }

            hourlyTaskMenuItem = new ToolStripMenuItem("Hourly Run Task");
            hourlyTaskMenuItem.Click += new EventHandler(CreateHourlyScheduledTask);

            if (!IsScheduledTaskExists(HourlyTaskName))
            {
                contextMenu.Items.Add(hourlyTaskMenuItem);
            }

            UpdateMenuItem = new ToolStripMenuItem("Update");
            UpdateMenuItem.Enabled = true;
            UpdateMenuItem.Click += new EventHandler(button7_Click);
            contextMenu.Items.Add(UpdateMenuItem);

            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.Icon = this.Icon;
            notifyIcon.Visible = true;
            notifyIcon.Text = "Network Detector";

            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;

            this.MaximizeBox = false;
            this.MinimizeBox = true;

            // Initialize Timer
            intervalTimer = new System.Windows.Forms.Timer();
            intervalTimer.Interval = 2 * 60 * 1000; // 2 minutes in milliseconds
            intervalTimer.Tick += SendData;
            intervalTimer.Start();

            this.Load += new System.EventHandler(this.NetworkDetector_Load);
        }

        private async void NetworkDetector_Load(object sender, EventArgs e)
        {
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
                string machineNameValue = Environment.MachineName;
                string cpuInfo = dataFetcher.GetCPUInfo();
                string ramInfo = dataFetcher.GetRamInfo();
                string windowsOS = dataFetcher.GetWindowsOS();
                string buildNumber = dataFetcher.GetBuildNumber();

                // Update UI with static data
                machineNameTextBox.Text = machineNameValue;
                cpuInfoTextBox.Text = cpuInfo;
                ramInfoTextBox.Text = ramInfo;
                windowsOsTextBox.Text = windowsOS;
                buildNumberTextBox.Text = buildNumber;

                logTextBox.Clear();
                logTextBox.AppendText($"Static Data Gathered: {machineNameValue} | {cpuInfo} | {ramInfo} | {windowsOS} | {buildNumber}\r\n");

                string latestFileDate = await dataFetcher.GetLatestSharepointFileDateAsync();
                latestFileDateTextBox.Text = latestFileDate;

                // Fetch pending updates
                string pendingUpdates = await dataFetcher.GetPendingUpdatesAsync();
                pendingUpdatesTextBox.Text = pendingUpdates;

                logTextBox.AppendText($"Pending Updates: {pendingUpdates}\r\n");
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

                // Assign default values if data retrieval failed
                string displayWanIp = !string.IsNullOrEmpty(wanIp) ? wanIp : "IP Failed";
                string displayIsp = !string.IsNullOrEmpty(isp) ? isp : "ISP Failed";

                // Update the text boxes with the dynamic data
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

                logTextBox.AppendText($"Dynamic Data Gathered: {displayWanIp} | {displayIsp} | {storageInfo}\r\n");
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

            await GatherDynamicDataAsync(); // Gather dynamic data before sending

            string machineName = machineNameTextBox.Text;
            string cpuInfo = cpuInfoTextBox.Text;
            string ramInfo = ramInfoTextBox.Text;
            string wanIp = wanIpTextBox.Text;
            string isp = ispTextBox.Text;
            string storageInfo = storageInfoTextBox.Text;
            string windowsOS = windowsOsTextBox.Text;
            string buildNumber = buildNumberTextBox.Text;
            string version = Version.Text;
            string pendingUpdates = pendingUpdatesTextBox.Text;
            string lastsharepointfile = latestFileDateTextBox.Text;

            string gatheredData = $"(MainData)|{machineName}|{wanIp}|{isp}|{cpuInfo}|{ramInfo}|{storageInfo}|{windowsOS}|{buildNumber}|{version}|{pendingUpdates}|{lastsharepointfile}";

            if (string.IsNullOrEmpty(gatheredData))
            {
                logTextBox.AppendText("No data to send.\r\n");
                return;
            }

            string serverIp = ipAddressTextBox.Text;

            try
            {
                using (var tcpClient = new TcpClient())
                {
                    logTextBox.AppendText($"Attempting to connect to server at {serverIp}:{TcpPort}\r\n");

                    await tcpClient.ConnectAsync(serverIp, TcpPort);
                    logTextBox.AppendText("Connected to server\r\n");

                    using (var stream = tcpClient.GetStream())
                    {
                        byte[] data = Encoding.UTF8.GetBytes(gatheredData);
                        await stream.WriteAsync(data, 0, data.Length);
                        logTextBox.AppendText($"Sent: {gatheredData}\r\n");

                        byte[] buffer = new byte[1024];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        logTextBox.AppendText($"Received: {response}\r\n");
                    }

                    tcpClient.Close();
                    

                    await CheckAndUpdateAppUpdateColumn(machineName);
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

        private void CreateScheduledTask(object sender, EventArgs e)
        {
            RemoveFromStartup();

            try
            {
                // Define the command for creating the task
                string taskName = "Network Detector";
                string exePath = @"C:\Network Detector\NetworkDetector.exe";  // Ensure this is a local path

                // Properly quote the exePath to handle spaces, double quotes for passing to schtasks
                string taskCreateCommand = $"schtasks /create /tn \"{taskName}\" /tr \"\\\"{exePath}\\\"\" /sc onlogon /rl highest /f";

                // Start the process to create the scheduled task
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {taskCreateCommand}",
                    UseShellExecute = false,  // False to capture output and errors
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Verb = "runas",  // Ensures the command runs with admin privileges
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (process.ExitCode == 0)
                    {
                        MessageBox.Show("Scheduled task created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to create scheduled task. Error: {error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    // Optionally, log the output and error to the console or file for debugging purposes
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

        private bool IsUserAdministrator()
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
            // Define the path to your .bat file
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
                    @"xcopy C:\Maeko\UTILS ""C:\Users\admin\OneDrive - Ableworld UK\" + storeName + @""" /i /d /e /c /y" + Environment.NewLine +
                    @"xcopy C:\Maeko\UTILS ""C:\Users\admin\OneDrive - Ableworld UK\" + storeName + @" Sharepoint"" /i /d /e /c /y";

                // Write the content to the .bat file
                File.WriteAllText(batFilePath, batFileContent);

                // Create a new task definition
                using (TaskService ts = new TaskService())
                {
                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = "Runs a Maeko to Onedrive.bat file every hour every day";

                    // Set the start time to 09:00 AM
                    DateTime startTime = DateTime.Today.AddHours(9);

                    // Create a trigger that will start at 09:00 AM and repeat every hour for the duration of one day
                    DailyTrigger dailyTrigger = new DailyTrigger { DaysInterval = 1, StartBoundary = startTime };
                    dailyTrigger.Repetition = new RepetitionPattern(TimeSpan.FromHours(1), TimeSpan.FromDays(1));
                    td.Triggers.Add(dailyTrigger);

                    // Create an action that will run the .bat file
                    td.Actions.Add(new ExecAction(batFilePath, null, null));

                    // Register the task in the root folder
                    ts.RootFolder.RegisterTaskDefinition(@"Maeko To Onedrive", td);
                }

                MessageBox.Show("Task created successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogError($"Error creating task: {ex.Message}");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Check if the user is running as administrator
            if (!IsUserAdministrator())
            {
                MessageBox.Show("This operation requires administrator privileges. Please run the application as an administrator.", "Admin Access Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Open the registry key where the target feature update is stored
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", true))
                {
                    if (key == null)
                    {
                        DialogResult result = MessageBox.Show("Windows Update policy key does not exist. Create it?", "Create Registry Key", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result == DialogResult.Yes)
                        {
                            using (RegistryKey newKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"))
                            {
                                newKey.SetValue("TargetReleaseVersion", 1, RegistryValueKind.DWord);
                                newKey.SetValue("TargetReleaseVersionInfo", "24H2", RegistryValueKind.String);
                            }
                            MessageBox.Show("Registry key created and values set successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        // Set the values for TargetReleaseVersion and TargetReleaseVersionInfo
                        key.SetValue("TargetReleaseVersion", 1, RegistryValueKind.DWord);
                        key.SetValue("TargetReleaseVersionInfo", "24H2", RegistryValueKind.String);
                        MessageBox.Show("Target feature update version set to Windows 11 24H2.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"An error occurred: {ex.Message}");
            }
        }

        private void Button6_Click(object sender, EventArgs e)
        {
            panel1.Visible = true;
            panel3.Visible = false;
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            await DownloadVersionFolderAsync(versionNumber);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
            panel3.Visible = true;
        }

        #endregion

        #region Updater

        private async Task CheckAndUpdateAppUpdateColumn(string machineName)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["SQL"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string selectQuery = "SELECT AppUpdate FROM DeviceConfig WHERE Machine = @MachineName";
                    string updateQuery = "UPDATE DeviceConfig SET AppUpdate = 'No' WHERE Machine = @MachineName";

                    using (SqlCommand selectCommand = new SqlCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@MachineName", machineName);

                        var appUpdateValue = await selectCommand.ExecuteScalarAsync();

                        if (appUpdateValue != null && appUpdateValue.ToString().Equals("Yes", StringComparison.OrdinalIgnoreCase))
                        {
                            using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                            {
                                updateCommand.Parameters.AddWithValue("@MachineName", machineName);
                                await updateCommand.ExecuteNonQueryAsync();
                                logTextBox.AppendText($"AppUpdate value for {machineName} set to 'No'.\r\n");
                            }
                        }
                        // No else block needed as per original code
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Database Exception: {ex.Message}. Skipping update and moving on.");
            }
        }

        private async Task DownloadVersionFolderAsync(string version)
        {
            try
            {
                string downloadUrl = $"{baseUrl}BG%20Menu/{Uri.EscapeDataString(version)}";
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Network Detector");

                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }

                string tempZipFile = Path.Combine(Path.GetTempPath(), $"{version}.zip");
                string tempExtractPath = Path.Combine(Path.GetTempPath(), $"Extracted_{version}");

                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(downloadUrl);
                    response.EnsureSuccessStatusCode();

                    byte[] fileData = await response.Content.ReadAsByteArrayAsync();

                    File.WriteAllBytes(tempZipFile, fileData);

                    ZipFile.ExtractToDirectory(tempZipFile, tempExtractPath);

                    CopyFilesRecursively(new DirectoryInfo(tempExtractPath), new DirectoryInfo(appDataPath));

                    File.Delete(tempZipFile);
                    Directory.Delete(tempExtractPath, true);

                    string batFilePath = Path.Combine(appDataPath, "Update.bat");

                    RunBatFile(batFilePath);
                }
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
                    ProcessStartInfo processInfo = new ProcessStartInfo()
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

        private void CreateHourlyScheduledTask(object sender, EventArgs e)
        {
            try
            {
                // Get the path to the current executable
                string exePath = Application.ExecutablePath;

                // Build the command to create the scheduled task
                string taskCreateCommand = $"schtasks /create /tn \"{HourlyTaskName}\" /tr \"\\\"{exePath}\\\"\" /sc hourly /rl highest /f";

                // Start the process to create the scheduled task
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {taskCreateCommand}",
                    UseShellExecute = false,  // False to capture output and errors
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Verb = "runas",  // Ensures the command runs with admin privileges
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (process.ExitCode == 0)
                    {
                        MessageBox.Show("Hourly scheduled task created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Remove the menu item, as the task now exists
                        contextMenu.Items.Remove(hourlyTaskMenuItem);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to create hourly scheduled task. Error: {error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"An exception occurred: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods


        public void LogInfo(string message)
        {
            AppendLog($"INFO: {message}");
        }

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
                cp.ClassStyle |= 0x200;  // CS_NOCLOSE
                return cp;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide(); // Hide the form
            }
        }

        #endregion
    }
}
