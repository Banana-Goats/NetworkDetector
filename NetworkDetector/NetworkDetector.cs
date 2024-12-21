using System.Diagnostics;
using Microsoft.VisualBasic.Devices;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;
using System.Text;
using Microsoft.Win32.TaskScheduler;
using System.Security.Principal;
using Microsoft.Win32;
using System.Management;
using System.Net.Http;
using System.IO.Compression;
using Microsoft.VisualBasic;
using System.DirectoryServices;
using WUApiLib;
using System.Runtime.InteropServices;

namespace NetworkDetector
{
    public partial class NetworkDetector : Form
    {
        private const int TcpPort = 50550;
        private const string HourlyTaskName = "Network Detector Hourly";
        private string headOfficeIpAddress = "195.62.221.62";

        private string gatheredData;

        private string machineName;

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

            dataFetcher = new DataFetcher();

            // Existing initialization code...
            notifyIcon = new NotifyIcon();
            contextMenu = new ContextMenuStrip();

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

            intervalTimer = new System.Windows.Forms.Timer();
            intervalTimer.Interval = 2 * 60 * 1000; // 2 minutes in milliseconds
            intervalTimer.Tick += SendData;
            intervalTimer.Start();


            //this.Load += new System.EventHandler(this.button7_Click);
            this.Load += new System.EventHandler(this.NetworkDetector_Load);

            
        }

        private async void NetworkDetector_Load(object sender, EventArgs e)
        {
            string VersionNumber = Version.Text;
            VersionMenuItem.Text = $"Version Number {VersionNumber}";
            this.Text = $"Network Manager {VersionNumber}";
            StaticSpecs();
        }

        #region SendingData
        private async void StaticSpecs()
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
                logTextBox.AppendText($"Exception in StaticSpecs: {ex.Message}\r\n");
            }
        }

        private async System.Threading.Tasks.Task GatherDynamicData()
        {
            try
            {
                (string wanIp, string isp) = await dataFetcher.GetWanIpAndIspAsync();
                if (wanIp == null)
                {
                    logTextBox.AppendText("Unable to retrieve WAN IP address\r\n");
                    return;
                }

                string storageInfo = dataFetcher.GetStorageInfo();



                // Update the text boxes with the dynamic data
                Invoke(new System.Action(() =>
                {
                    wanIpTextBox.Text = wanIp;
                    ispTextBox.Text = isp;
                    storageInfoTextBox.Text = storageInfo;

                }));

                logTextBox.AppendText($"Dynamic Data Gathered: {wanIp} | {isp} | {storageInfo}\r\n");
            }
            catch (Exception ex)
            {
                logTextBox.AppendText($"Exception: {ex.Message}\r\n");
            }
        }

        private async void SendData(object sender, EventArgs e)
        {
            if (!this.IsHandleCreated)
            {
                return;
            }

            await GatherDynamicData(); // Gather dynamic data before sending

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
                var client = new TcpClient();
                logTextBox.AppendText($"Attempting to connect to server at {serverIp}:{TcpPort}\r\n");

                await client.ConnectAsync(serverIp, TcpPort);
                logTextBox.AppendText("Connected to server\r\n");

                var stream = client.GetStream();

                byte[] data = Encoding.UTF8.GetBytes(gatheredData);
                await stream.WriteAsync(data, 0, data.Length);
                logTextBox.AppendText($"Sent: {gatheredData}\r\n");

                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                logTextBox.AppendText($"Received: {response}\r\n");

                client.Close();
                logTextBox.Clear();
            }
            catch (SocketException ex)
            {
                logTextBox.AppendText($"SocketException: {ex.Message}\r\n");
            }
            catch (Exception ex)
            {
                logTextBox.AppendText($"Exception: {ex.Message}\r\n");
            }
        }

        private async void SendButton(object sender, EventArgs e)
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
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "cmd.exe";
                psi.Arguments = $"/c {taskCreateCommand}";
                psi.UseShellExecute = false;  // False to capture output and errors
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.Verb = "runas";  // Ensures the command runs with admin privileges
                psi.CreateNoWindow = true;

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
                MessageBox.Show($"An exception occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            string shortcutPath = System.IO.Path.Combine(startupPath, "NetworkDetector.lnk");
            if (System.IO.File.Exists(shortcutPath))
            {
                System.IO.File.Delete(shortcutPath);
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
            if (checkBox1.Checked)
            {
                ipAddressTextBox.ReadOnly = false;
            }
            else
            {
                ipAddressTextBox.ReadOnly = true;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Define the path to your .bat file
            string batFilePath = @"C:\Scripts\Maeko to Onedrive.bat";

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

            MessageBox.Show("Task created successfully.");
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
                        MessageBox.Show("Windows Update policy key does not exist. Creating key...");
                        using (RegistryKey newKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"))
                        {
                            newKey.SetValue("TargetReleaseVersion", 1, RegistryValueKind.DWord);
                            newKey.SetValue("TargetReleaseVersionInfo", "24H2", RegistryValueKind.String);
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
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private async System.Threading.Tasks.Task DownloadVersionFolderAsync(string version)
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
                MessageBox.Show($"Error downloading the version folder: {ex.Message}", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show("The .bat file does not exist or could not be found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running the .bat file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "cmd.exe";
                psi.Arguments = $"/c {taskCreateCommand}";
                psi.UseShellExecute = false;  // False to capture output and errors
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.Verb = "runas";  // Ensures the command runs with admin privileges
                psi.CreateNoWindow = true;

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
                MessageBox.Show($"An exception occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

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
    }
}
