using System;
using System.Configuration;
using Microsoft.Data.SqlClient;
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
using ScheduledTask = Microsoft.Win32.TaskScheduler.Task;
using Action = System.Action;
using NetworkDetector.Class;
using NetworkDetector.Helpers;
using System.Net;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Data;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using NetworkDetector.Services.Interfaces;


namespace NetworkDetector
{
    public partial class NetworkDetector : Form
    {
        private static readonly int WM_TASKBARCREATED = RegisterWindowMessage("TaskbarCreated");

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int RegisterWindowMessage(string lpString);

        private bool isNetworkDetectorDownloading = false;

        private SystemData _systemData = new SystemData();

        // Fields read from App.config
        private readonly string _hourlyTaskName;
        private readonly string _baseUrl;
        private readonly string _versionNumber;
        private readonly string _dansApp;
        private readonly string _callpoplite;
        private readonly string _salessheet;

        private readonly DataFetcher dataFetcher;

        private CleanupManager _cleanupManager;
        private MaekoVersionUpdater _maekoVersionUpdater;

        private string gatheredData;

        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem VersionMenuItem;
        private ToolStripMenuItem TaskRun;
        private ToolStripMenuItem UpdateMenuItem;

        private System.Windows.Forms.Timer gatherTimer;
        private System.Windows.Forms.Timer sendTimer;
        private System.Windows.Forms.Timer cleanupTimer;
        private System.Windows.Forms.Timer notifyIconTimer;        

        private readonly CommandExecutor commandExecutor;
        private readonly DatabaseService _databaseService = new DatabaseService();

        private static ConcurrentDictionary<string, Task> activeDownloadTasks = new ConcurrentDictionary<string, Task>();

        private readonly INetworkInfoService _networkInfo;
        private readonly IHardwareInfoService _hardwareInfo;
        private readonly ISharePointService _sharePoint;

        public NetworkDetector(INetworkInfoService networkInfo,
        IHardwareInfoService hardwareInfo,
        ISharePointService sharePoint)
        {
            InitializeComponent();

            _networkInfo = networkInfo;
            _hardwareInfo = hardwareInfo;
            _sharePoint = sharePoint;

            _hourlyTaskName = ConfigurationManager.AppSettings["HourlyTaskName"];
            _baseUrl = ConfigurationManager.AppSettings["BananaGoatsBaseUrl"];
            _versionNumber = ConfigurationManager.AppSettings["VersionNumber"];
            _dansApp = ConfigurationManager.AppSettings["DansApp"];
            _callpoplite = ConfigurationManager.AppSettings["CallPopLite"];
            _salessheet = ConfigurationManager.AppSettings["SalesSheet"];

            dataFetcher = new DataFetcher(this);

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

            gatherTimer = new System.Windows.Forms.Timer
            {
                Interval = 60 * 1000
            };
            gatherTimer.Tick += async (s, e) => await GatherDynamicDataAsync();
            gatherTimer.Start();

            sendTimer = new System.Windows.Forms.Timer
            {
                Interval = 15 * 1000
            };
            sendTimer.Tick += async (sender, e) => await SendData();
            sendTimer.Start();

            notifyIconTimer = new System.Windows.Forms.Timer
            {
                Interval = 15000
            };
            notifyIconTimer.Tick += (s, e) =>
            {
                if (notifyIcon == null || !notifyIcon.Visible)
                {
                    LogError("NotifyIcon missing. Recreating NotifyIcon.");
                    RecreateNotifyIcon();
                }
            };
            notifyIconTimer.Start();

            _cleanupManager = new CleanupManager(this, logTextBox)
            {
                CleanupInterval = 6 * 60 * 60 * 1000
            };
            _cleanupManager.StartCleanup();

            string connectionString = ConfigurationManager.ConnectionStrings["SQL"]?.ConnectionString;

            _maekoVersionUpdater = new MaekoVersionUpdater(
                connectionString,
                LogError,
                message => SafeUpdate(() => logTextBox.AppendText($"{DateTime.Now}: {message}\r\n"))
            );

            try
            {
                commandExecutor = new CommandExecutor();
            }
            catch (InvalidOperationException ex)
            {

                LogError($"Configuration Error: {ex.Message}");

                return;
            }
        }

        private async void NetworkDetector_Load(object sender, EventArgs e)
        {            
            await GatherDynamicDataAsync();            

            CreateScheduledTask();

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
                _systemData.MachineName = Environment.MachineName;
                _systemData.CPU = dataFetcher.GetCPUInfo();
                _systemData.Ram = dataFetcher.GetRamInfo();
                _systemData.WindowsOS = dataFetcher.GetWindowsOS();
                _systemData.BuildNumber = dataFetcher.GetBuildNumber();
                _systemData.CompanyName = await dataFetcher.GetCompanyNameAsync(_systemData.MachineName);
                _systemData.LatestSharePointFileDate = await dataFetcher.GetLatestSharepointFileDateAsync();

                await CheckAndRetrieveLocationAsync();

                SafeUpdate(() =>
                {
                    machineNameTextBox.Text = _systemData.MachineName;
                    cpuInfoTextBox.Text = _systemData.CPU;
                    ramInfoTextBox.Text = _systemData.Ram;
                    windowsOsTextBox.Text = _systemData.WindowsOS;
                    buildNumberTextBox.Text = _systemData.BuildNumber;
                    CompanyTextBox.Text = _systemData.CompanyName;
                    latestFileDateTextBox.Text = _systemData.LatestSharePointFileDate;
                    logTextBox.Clear();
                });

                _systemData.PendingUpdates = await dataFetcher.GetPendingUpdatesAsync();

                SafeUpdate(() =>
                {
                    pendingUpdatesTextBox.Text = _systemData.PendingUpdates;
                });
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
                (string wanIp, string isp, bool mobile) = await dataFetcher.GetWanIpAndIspAsync();

                _systemData.WanIp = !string.IsNullOrEmpty(wanIp) ? wanIp : "IP Failed";
                _systemData.Isp = !string.IsNullOrEmpty(isp) ? isp : "ISP Failed";
                _systemData.Mobile = mobile;

                _systemData.StorageInfo = dataFetcher.GetStorageInfo();

                SafeUpdate(() =>
                {
                    wanIpTextBox.Text = _systemData.WanIp;
                    ispTextBox.Text = _systemData.Isp;
                    storageInfoTextBox.Text = _systemData.StorageInfo;
                    textBox1.Text = mobile ? "Mobile" : "Not Mobile";
                });
            }
            catch (Exception ex)
            {
                LogError($"Exception in GatherDynamicData: {ex.Message}");
            }
        }

        private async Task SendData()
        {
            logTextBox.Clear();

            await CheckAndRetrieveLocationAsync();

            if (string.IsNullOrEmpty(_systemData.Location))
            {
                LogError("Location is null or empty. Aborting SendData() call.");
                return;
            }            

            if (!this.IsHandleCreated)
            {
                return;
            }

            string machineName = _systemData.MachineName;

            await _maekoVersionUpdater.CheckAndUpdateMaekoVersionsAsync(machineName);
            await UpdateDatabaseAsync();            

            await CheckAndResetColumnAsync(machineName, "AppUpdate", _versionNumber, "Network Detector");

            if (!isNetworkDetectorDownloading)
            {
                await Task.Delay(100000);

                await CheckAndResetColumnAsync(machineName, "TillUpdater", _dansApp, "Till Updater");
                await CheckAndResetColumnAsync(machineName, "CallPopLite", _callpoplite, "Call Pop Lite");
                await CheckAndResetColumnAsync(machineName, "SalesSheet", _salessheet, "Sales Sheet");
                await CheckAndDownloadFormRefreshAsync(machineName);
                await CheckAndRunCommandsAsync(machineName);
                await CheckAndRunSpeedTestAsync(machineName);
            }
            else
            {
                SafeUpdate(() =>
                    logTextBox.AppendText("Other updates skipped because Network Detector is still downloading.\r\n")
                );
            }         

        }

        private async Task UpdateDatabaseAsync()
        {
            // Gather data from the model
            string machineName = _systemData.MachineName;
            string store = _systemData.Location;
            string cpuInfo = _systemData.CPU;
            string ramInfo = _systemData.Ram;
            string storageInfo = _systemData.StorageInfo;
            string wanIp = _systemData.WanIp;
            string isp = _systemData.Isp;
            string windowsOS = _systemData.WindowsOS;
            string buildNumber = _systemData.BuildNumber;

            string version = _versionNumber;
            string pendingUpdates = string.IsNullOrWhiteSpace(_systemData.PendingUpdates) ? "Failed" : _systemData.PendingUpdates;
            string lastSharePointFile = string.IsNullOrWhiteSpace(_systemData.LatestSharePointFileDate) ? "Failed" : _systemData.LatestSharePointFileDate;
            string clientVersion = Version.Text;

            // Define the update query including the Mobile column
            string updateQuery = @"
                UPDATE TBPC
                SET 
                    Store = @Store,
                    CPU = @CPU,
                    Ram = @Ram,
                    HHD = @HHD,
                    IP = @IP,
                    ISP = @ISP,
                    OS = @OS,
                    OS_Version = @OS_Version,
                    Client_Version = @Client_Version,
                    OS_Updates = @OS_Updates,
                    Sharepoint_Sync = @Sharepoint_Sync,
                    Mobile = @Mobile,
                    Pulse_Time = GETDATE()
                WHERE Name = @Name;";

            // Prepare parameters, now including Mobile
            var parameters = new Dictionary<string, object>
                {
                    { "@Name", machineName },
                    { "@Store", _systemData.Location ?? "Unknown Store" },
                    { "@CPU", _systemData.CPU ?? "Unknown CPU" },
                    { "@Ram", _systemData.Ram ?? "Unknown RAM" },
                    { "@HHD", _systemData.StorageInfo ?? "Unknown Storage" },
                    { "@IP", _systemData.WanIp ?? "Unknown IP" },
                    { "@ISP", _systemData.Isp ?? "Unknown ISP" },
                    { "@OS", _systemData.WindowsOS ?? "Unknown OS" },
                    { "@OS_Version", _systemData.BuildNumber ?? "Unknown Build" },
                    { "@Client_Version", clientVersion ?? "Unknown Client Version" },
                    { "@OS_Updates", _systemData.PendingUpdates ?? "None" },
                    { "@Sharepoint_Sync", _systemData.LatestSharePointFileDate ?? "No Date" },
                    { "@Mobile", _systemData.Mobile }
                };

            try
            {
                // Attempt to update first
                int rowsAffected = await _databaseService.ExecuteNonQueryAsync(updateQuery, parameters).ConfigureAwait(false);
                if (rowsAffected == 0)
                {
                    // Define the insert query if update did not affect any rows, now including Mobile
                    string insertQuery = @"
                INSERT INTO TBPC 
                (
                    Name, Store, CPU, Ram, HHD, IP, ISP, OS, OS_Version, 
                    Client_Version, OS_Updates, Sharepoint_Sync, Mobile, Pulse_Time
                )
                VALUES 
                (
                    @Name, @Store, @CPU, @Ram, @HHD, @IP, @ISP, @OS, @OS_Version, 
                    @Client_Version, @OS_Updates, @Sharepoint_Sync, @Mobile, GETDATE()
                );";

                    int insertRows = await _databaseService.ExecuteNonQueryAsync(insertQuery, parameters).ConfigureAwait(false);
                    SafeUpdate(() =>
                    {
                        if (insertRows > 0)
                        {
                            LogError("No matching record found. Inserted new record successfully.");
                        }
                        else
                        {
                            LogError("Insert operation did not affect any rows.");
                        }
                    });
                }
                else
                {
                    SafeUpdate(() => LogError("Database updated successfully."));
                }
            }
            catch (Exception ex)
            {
                LogError($"Database update/insert error: {ex.Message}");
            }
        }

        private async Task CheckAndRetrieveLocationAsync()
        {
            string machineName = _systemData.MachineName;
            string location = null;

            try
            {
                // Create an instance of the DatabaseService
                var dbService = new DatabaseService();

                // Define the select query and parameters
                string selectQuery = "SELECT Location FROM Computers WHERE MachineName = @MachineName";
                var parameters = new Dictionary<string, object>
                    {
                        { "@MachineName", machineName }
                    };

                // Try to retrieve the location for the current machine
                object result = await dbService.ExecuteScalarAsync(selectQuery, parameters).ConfigureAwait(false);
                if (result != null && result != DBNull.Value && !string.IsNullOrWhiteSpace(result.ToString()))
                {
                    location = result.ToString();
                }

                // If no record found or the location is empty, insert or update to set it to "Unknown"
                if (string.IsNullOrWhiteSpace(location))
                {
                    string insertOrUpdateQuery = @"
                        IF NOT EXISTS (SELECT 1 FROM Computers WHERE MachineName = @MachineName)
                        BEGIN
                            INSERT INTO Computers (MachineName, Location) 
                            VALUES (@MachineName, 'Unknown')
                        END
                        ELSE
                        BEGIN
                            UPDATE Computers SET Location = 'Unknown' WHERE MachineName = @MachineName
                        END";
                    await dbService.ExecuteNonQueryAsync(insertOrUpdateQuery, parameters).ConfigureAwait(false);

                    // Re-read the location value
                    result = await dbService.ExecuteScalarAsync(selectQuery, parameters).ConfigureAwait(false);
                    if (result != null && result != DBNull.Value)
                    {
                        location = result.ToString();
                    }
                }

                // Update the model and UI
                _systemData.Location = location;
                SafeUpdate(() =>
                {
                    LocationTextBox.Text = location ?? string.Empty;
                });
            }
            catch (Exception ex)
            {
                LogError($"Error checking/retrieving location: {ex.Message}");
            }
        }

        private async void SendButton_Click(object sender, EventArgs e)
        {
            SendData();
        }

        #endregion

        #region TaskSetup

        private void CreateScheduledTask()
        {
            try
            {
                string taskName = "Network Detector";
                string exePath = @"C:\Network Detector\NetworkDetector.exe";

                using (TaskService ts = new TaskService())
                {
                    // Create a new task definition
                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = "Runs Network Detector on user logon with highest privileges.";

                    // Set the trigger to log on
                    td.Triggers.Add(new LogonTrigger());

                    // Set the action to run the executable
                    td.Actions.Add(new ExecAction(exePath, null, Path.GetDirectoryName(exePath)));

                    // Set the task to run with highest privileges
                    td.Principal.RunLevel = TaskRunLevel.Highest;

                    // Configure settings to ensure the task runs regardless of power conditions
                    td.Settings.DisallowStartIfOnBatteries = false; // Allow task to start on battery
                    td.Settings.StopIfGoingOnBatteries = false;     // Do not stop the task if the system switches to battery
                    td.Settings.AllowHardTerminate = true;
                    td.Settings.StartWhenAvailable = true;
                    td.Settings.Hidden = false;

                    // Retrieve the current user's username
                    string currentUser = ts.UserName;

                    // Register the task in the root folder with CreateOrUpdate flag
                    ts.RootFolder.RegisterTaskDefinition(
                        taskName,
                        td,
                        TaskCreation.CreateOrUpdate,
                        currentUser,    // Corrected from TaskService.Instance.User to ts.UserName
                        null,
                        TaskLogonType.InteractiveToken);

                    LogError($"Scheduled task '{taskName}' created or updated successfully.");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                LogError($"Permission error while creating/updating scheduled task: {ex.Message}");
                MessageBox.Show($"Permission error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                LogError($"An exception occurred while creating/updating scheduled task: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void button4_Click(object sender, EventArgs e)
        {
            string batFilePath = @"C:\Scripts\Maeko to Onedrive.bat";

            try
            {
                string scriptsDirectory = Path.GetDirectoryName(batFilePath);
                if (!Directory.Exists(scriptsDirectory))
                {
                    Directory.CreateDirectory(scriptsDirectory);
                }

                string storeName = storenametxt.Text;

                string batFileContent =
                    @"xcopy C:\Maeko\UTILS ""C:\Users\admin\OneDrive - Ableworld UK\" + storeName + @""" /i /d /e /c /y"
                    + Environment.NewLine +
                    @"xcopy C:\Maeko\UTILS ""C:\Users\admin\OneDrive - Ableworld UK\" + storeName + @" Sharepoint"" /i /d /e /c /y";

                File.WriteAllText(batFilePath, batFileContent);

                using (TaskService ts = new TaskService())
                {
                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = "Runs a Maeko to Onedrive.bat file every hour every day";

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

        private async void button7_Click(object sender, EventArgs e)
        {
            await DownloadVersionFolderAsync(_versionNumber, "Network Detector");
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string machineName = machineNameTextBox.Text;
            await CheckAndResetColumnAsync(machineName, "AppUpdate", _versionNumber, "Network Detector");
            await CheckAndResetColumnAsync(machineName, "TillUpdater", _dansApp, "Till Updater");
            await CheckAndResetColumnAsync(machineName, "CallPopLite", _callpoplite, "Call Pop Lite");
            await CheckAndResetColumnAsync(machineName, "SalesSheet", _salessheet, "Sales Sheet");
            await CheckAndDownloadFormRefreshAsync(machineName);
            await CheckAndRunCommandsAsync(machineName);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Commands commandsForm = new Commands();

            commandsForm.Show();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            // Disable Button2 to prevent multiple clicks
            button2.Enabled = false;

            try
            {
                // Create a Progress<string> instance to handle progress updates
                var progress = new Progress<string>(message =>
                {
                    // Append progress messages to the logTextBox
                    SafeUpdate(() => logTextBox.AppendText($"{message}\r\n"));
                });

                // Execute all commands asynchronously
                bool success = await commandExecutor.ExecuteAllCommandsAsync(progress);

                // Log the final status
                if (success)
                {
                    SafeUpdate(() => logTextBox.AppendText($"All commands executed successfully.\r\n"));
                }
                else
                {
                    SafeUpdate(() => logTextBox.AppendText($"Some commands failed to execute. Check logs for details.\r\n"));
                }
            }
            catch (Exception ex)
            {
                // Log any unexpected exceptions
                SafeUpdate(() => logTextBox.AppendText($"Error executing all commands: {ex.Message}\r\n"));
            }
            finally
            {
                // Re-enable Button2 after operation completes
                button2.Enabled = true;
            }
        }

        #endregion

        #region Updater

        private async Task CheckAndResetColumnAsync(string machineName, string columnName, string downloadVersion, string downloadFolderName)
        {
            string downloadKey = $"{machineName}_{columnName}_{downloadVersion}";

            if (activeDownloadTasks.ContainsKey(downloadKey))
            {
                LogError($"Download for {downloadKey} is already in progress; skipping duplicate request.");
                return;
            }

            string connectionString = ConfigurationManager.ConnectionStrings["SQL"]?.ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync().ConfigureAwait(false);
                    // Check if the column is set to "Yes"
                    string selectQuery = $"SELECT {columnName} FROM Computers WHERE MachineName = @MachineName";
                    using (SqlCommand selectCommand = new SqlCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@MachineName", machineName);
                        var value = await selectCommand.ExecuteScalarAsync().ConfigureAwait(false);

                        if (value != null && value.ToString().Equals("Yes", StringComparison.OrdinalIgnoreCase))
                        {
                            // Prevent other downloads if Network Detector is already busy (for non-AppUpdate columns)
                            if (columnName != "AppUpdate" && isNetworkDetectorDownloading)
                            {
                                SafeUpdate(() => logTextBox.AppendText($"Skipping {columnName} because Network Detector is downloading.\r\n"));
                                return;
                            }

                            if (columnName == "AppUpdate")
                            {
                                isNetworkDetectorDownloading = true;
                            }

                            SafeUpdate(() => logTextBox.AppendText($"{columnName} update requested for: {machineName}.\r\n"));

                            // Start the download task and add it to our active downloads dictionary.
                            Task downloadTask = DownloadVersionFolderAsync(downloadVersion, downloadFolderName);
                            if (!activeDownloadTasks.TryAdd(downloadKey, downloadTask))
                            {
                                LogError($"Failed to add download task for {downloadKey} to active downloads.");
                                return;
                            }

                            // Await the download
                            await downloadTask;
                            // Remove the completed task
                            activeDownloadTasks.TryRemove(downloadKey, out _);

                            if (columnName == "AppUpdate")
                            {
                                isNetworkDetectorDownloading = false;
                                await Task.Delay(20000); // 20-second delay
                            }
                        }
                        else
                        {
                            LogError($"Value Set To No For {columnName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Database Exception in {columnName}: {ex.Message}. Skipping update.");
            }
        }

        private async Task DownloadVersionFolderAsync(string version, string folderName)
        {
            string downloadUrl = $"{_baseUrl}BG%20Menu/{Uri.EscapeDataString(version)}";
            string machineName = Environment.MachineName;

            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), folderName);
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            string tempZipFile = Path.Combine(Path.GetTempPath(), $"{version}.zip");
            string tempExtractPath = Path.Combine(Path.GetTempPath(), $"Extracted_{version}");

            int maxRetries = 3;
            TimeSpan backoffDelay = TimeSpan.FromSeconds(30);
            bool downloadSucceeded = false;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                // Create a CancellationTokenSource for the current download attempt.
                using (var cts = new CancellationTokenSource())
                {
                    try
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                        request.Headers.Add("X-MachineName", machineName);

                        LogError($"Download attempt {attempt} starting...");
                        HttpResponseMessage response = await SystemData.Client.SendAsync(request, cts.Token).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();

                        byte[] fileData = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        File.WriteAllBytes(tempZipFile, fileData);

                        downloadSucceeded = true;
                        LogError("Download succeeded!");
                        break;
                    }
                    // Catch TaskCanceledException for retries
                    catch (TaskCanceledException ex) when (attempt < maxRetries)
                    {
                        LogError($"Download timed out or was canceled on attempt {attempt}: {ex.Message}. Retrying in {backoffDelay.TotalSeconds} seconds...");
                        await Task.Delay(backoffDelay).ConfigureAwait(false);
                    }
                    // Catch HttpRequestException for retries
                    catch (HttpRequestException ex) when (attempt < maxRetries)
                    {
                        LogError($"Download attempt {attempt} failed: {ex.Message}. Retrying in {backoffDelay.TotalSeconds} seconds...");
                        await Task.Delay(backoffDelay).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException ex)
                    {
                        LogError($"Download attempt {attempt} was canceled: {ex.Message}");
                        return;
                    }
                }
            }

            if (!downloadSucceeded)
            {
                LogError("All download attempts have failed. Aborting.");
                return;
            }

            // Process the downloaded zip (extract, copy, run .bat, etc.)
            try
            {
                ZipFile.ExtractToDirectory(tempZipFile, tempExtractPath);
                CopyFilesRecursively(new DirectoryInfo(tempExtractPath), new DirectoryInfo(appDataPath));

                File.Delete(tempZipFile);
                Directory.Delete(tempExtractPath, true);

                // Optionally run an update batch file.
                string batFilePath = Path.Combine(appDataPath, "Update.bat");
                RunBatFile(batFilePath);
            }
            catch (Exception ex)
            {
                LogError($"Error processing downloaded files: {ex.Message}");
            }
        }

        private async Task CheckAndDownloadFormRefreshAsync(string machineName)
        {
            try
            {
                var dbService = new DatabaseService();

                var parameters = new Dictionary<string, object>
                    {
                        { "@MachineName", machineName }
                    };

                // Query the Computers table for FormRefresh and CompanyName.
                string selectQuery = @"
            SELECT FormRefresh, CompanyName 
            FROM Computers 
            WHERE MachineName = @MachineName";
                DataTable dt = await dbService.ExecuteQueryAsync(selectQuery, parameters).ConfigureAwait(false);

                string formRefreshStatus = null;
                string companyName = null;
                if (dt.Rows.Count > 0)
                {
                    formRefreshStatus = dt.Rows[0]["FormRefresh"]?.ToString();
                    companyName = dt.Rows[0]["CompanyName"]?.ToString();
                }

                // If FormRefresh is set to "Yes" and a company name exists, perform the download.
                if (string.Equals(formRefreshStatus, "Yes", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(companyName))
                {
                    LogError($"FormRefresh is set to 'Yes' for company: {companyName}. Initiating download...");

                    string downloadUrl = $"{_baseUrl}Forms/{Uri.EscapeDataString(companyName)}";

                    string appDataPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Network Detector",
                        companyName
                    );
                    Directory.CreateDirectory(appDataPath);

                    string tempZipPath = Path.Combine(Path.GetTempPath(), $"{companyName}.zip");
                    string tempExtractPath = Path.Combine(Path.GetTempPath(), $"Extracted_{companyName}");

                    try
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                        request.Headers.Add("X-MachineName", machineName);

                        LogError($"Starting download from {downloadUrl}...");
                        HttpResponseMessage response = await SystemData.Client.SendAsync(request).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();
                        byte[] zipData = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        await File.WriteAllBytesAsync(tempZipPath, zipData).ConfigureAwait(false);
                        LogError("Download completed successfully.");

                        LogError($"Extracting ZIP file to {tempExtractPath}...");
                        ZipFile.ExtractToDirectory(tempZipPath, tempExtractPath);

                        CopyFilesRecursively(new DirectoryInfo(tempExtractPath), new DirectoryInfo(appDataPath));
                        LogError("Files copied to AppData successfully.");

                        string targetPath = @"C:\Maeko\UTILS\DocTemplates";
                        Directory.CreateDirectory(targetPath);

                        CopyFilesRecursively(new DirectoryInfo(appDataPath), new DirectoryInfo(targetPath));
                        LogError($"Files copied to target directory {targetPath} successfully.");
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error during download or extraction: {ex.Message}");
                    }
                    finally
                    {
                        // Clean up temporary files.
                        if (File.Exists(tempZipPath))
                        {
                            try
                            {
                                File.Delete(tempZipPath);
                                LogError($"Deleted temporary ZIP file: {tempZipPath}");
                            }
                            catch (Exception ex)
                            {
                                LogError($"Error deleting temporary ZIP file: {ex.Message}");
                            }
                        }
                        if (Directory.Exists(tempExtractPath))
                        {
                            try
                            {
                                Directory.Delete(tempExtractPath, true);
                                LogError($"Deleted temporary extraction folder: {tempExtractPath}");
                            }
                            catch (Exception ex)
                            {
                                LogError($"Error deleting temporary extraction folder: {ex.Message}");
                            }
                        }
                    }

                    // Reset the FormRefresh flag to "No" in the database.
                    string updateQuery = "UPDATE Computers SET FormRefresh = 'No' WHERE MachineName = @MachineName";
                    int rowsAffected = await dbService.ExecuteNonQueryAsync(updateQuery, parameters).ConfigureAwait(false);
                    if (rowsAffected > 0)
                    {
                        LogError("FormRefresh flag reset to 'No' successfully.");
                    }
                    else
                    {
                        LogError("Failed to reset FormRefresh flag.");
                    }
                }
                else
                {
                    LogError($"Value Set To No For Form Refresh");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error in CheckAndDownloadFormRefreshAsync: {ex.Message}");
            }
        }

        private async Task CheckAndRunCommandsAsync(string machineName)
        {
            var dbService = new DatabaseService();

            try
            {
                string selectQuery = "SELECT Commands FROM Computers WHERE MachineName = @MachineName";
                var parameters = new Dictionary<string, object>
                    {
                        { "@MachineName", machineName }
                    };

                object result = await dbService.ExecuteScalarAsync(selectQuery, parameters).ConfigureAwait(false);
                string commandsStatus = result != null && result != DBNull.Value ? result.ToString() : string.Empty;

                // If the flag is set to "Yes", execute all commands.
                if (!string.IsNullOrWhiteSpace(commandsStatus) &&
                    commandsStatus.Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase))
                {
                    LogError($"Commands flag is set to 'Yes' for machine {machineName}. Executing commands...");

                    // Create a progress reporter to capture output messages.
                    var progress = new Progress<string>(message =>
                    {
                        // Append progress messages to the logTextBox on the UI thread.
                        SafeUpdate(() => logTextBox.AppendText($"{message}\r\n"));
                    });

                    // Execute all commands using your existing commandExecutor.
                    bool success = await commandExecutor.ExecuteAllCommandsAsync(progress);
                    if (success)
                    {
                        LogError("All commands executed successfully.");
                    }
                    else
                    {
                        LogError("Some commands failed to execute. Check logs for details.");
                    }

                    // Update the Commands flag to "No".
                    string updateQuery = "UPDATE Computers SET Commands = 'No' WHERE MachineName = @MachineName";
                    int rowsAffected = await dbService.ExecuteNonQueryAsync(updateQuery, parameters).ConfigureAwait(false);
                    if (rowsAffected > 0)
                    {
                        LogError("Commands flag reset to 'No' successfully.");
                    }
                    else
                    {
                        LogError("Failed to reset Commands flag.");
                    }
                }
                else
                {
                    LogError($"Value Set To No For Running Commands");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error in CheckAndRunCommandsAsync: {ex.Message}");
            }
        }

        private async Task CheckAndRunSpeedTestAsync(string machineName)
        {
            try
            {
                var dbService = new DatabaseService();
                var parameters = new Dictionary<string, object>
                {
                    { "@MachineName", machineName }
                };

                string selectQuery = "SELECT SpeedTest FROM Computers WHERE MachineName = @MachineName";
                object result = await dbService.ExecuteScalarAsync(selectQuery, parameters).ConfigureAwait(false);

                if (result != null && result.ToString().Equals("Yes", StringComparison.OrdinalIgnoreCase))
                {
                    string updateQuery = "UPDATE Computers SET SpeedTest = 'No' WHERE MachineName = @MachineName";
                    await dbService.ExecuteNonQueryAsync(updateQuery, parameters).ConfigureAwait(false);

                    try
                    {
                        SpeedTest();
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error running SpeedTest: {ex.Message}");
                    }
                }
                else
                {
                    LogError($"Value Set To No For Speed Test");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error in CheckAndRunSpeedTestAsync: {ex.Message}");
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
                        WindowStyle = ProcessWindowStyle.Hidden,
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
            AppendLog($"{message}");
        }

        private void AppendLog(string message)
        {
            SafeUpdate(() => logTextBox.AppendText($"{DateTime.Now}: {message}\r\n"));
        }

        public void SafeUpdate(Action updateAction)
        {
            if (InvokeRequired)
            {
                Invoke(updateAction);
            }
            else
            {
                updateAction();
            }
        }

        #endregion

        #region Form Overrides

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x200; // CS_NOCLOSE to disable close button
                return cp;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_TASKBARCREATED)
            {
                RecreateNotifyIcon();
            }
            base.WndProc(ref m);
        }

        private void RecreateNotifyIcon()
        {
            if (notifyIcon != null)
            {
                notifyIcon.Dispose();
            }

            notifyIcon = new NotifyIcon
            {
                Icon = this.Icon,
                Text = "Network Detector",
                ContextMenuStrip = contextMenu,
                Visible = true
            };
        }

        #endregion

        private async void SpeedTest()
        {
            try
            {
                SystemData systemInfo = _systemData;
                string speedtestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "speedtest.exe");

                if (!File.Exists(speedtestPath))
                {
                    LogError($"Speedtest CLI not found at: {speedtestPath}");
                    return;
                }

                LogError("Starting speed test...");

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = speedtestPath,
                    Arguments = "--accept-license --accept-gdpr --format=json",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();

                    string result = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    LogError("Speed test completed successfully.");

                    // Parse JSON response
                    JObject json = JObject.Parse(result);
                    double downloadSpeed = json["download"]["bandwidth"].ToObject<double>() * 8 / 1_000_000;
                    double uploadSpeed = json["upload"]["bandwidth"].ToObject<double>() * 8 / 1_000_000;
                    double ping = json["ping"]["latency"].ToObject<double>();

                    LogError($"Speed test results: Download = {downloadSpeed} Mbps, Upload = {uploadSpeed} Mbps, Ping = {ping} ms.");
                    LogError("Sending speed test data to database...");

                    await Task.Run(() => SaveSpeedTestToDatabase(systemInfo, downloadSpeed, uploadSpeed, ping));

                    LogError("Speed test data sent to database successfully.");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error in SpeedTest: {ex.Message}");
            }
        }

        private void SaveSpeedTestToDatabase(SystemData systemInfo, double download, double upload, double ping)
        {
            try
            {
                LogError("Attempting to save speed test data to database...");

                string connectionString = ConfigurationManager.ConnectionStrings["SQL"]?.ConnectionString;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
                MERGE INTO SpeedTestResults AS target
                USING (SELECT @MachineName AS MachineName) AS source
                ON target.MachineName = source.MachineName
                WHEN MATCHED THEN 
                    UPDATE SET 
                        Location = @Location,
                        DownloadSpeed = @DownloadSpeed,
                        UploadSpeed = @UploadSpeed,
                        Ping = @Ping
                WHEN NOT MATCHED THEN 
                    INSERT (MachineName, Location, DownloadSpeed, UploadSpeed, Ping) 
                    VALUES (@MachineName, @Location, @DownloadSpeed, @UploadSpeed, @Ping);";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MachineName", systemInfo.MachineName);
                        cmd.Parameters.AddWithValue("@Location", systemInfo.Location ?? "Unknown");
                        cmd.Parameters.AddWithValue("@DownloadSpeed", Math.Round(download, 2));
                        cmd.Parameters.AddWithValue("@UploadSpeed", Math.Round(upload, 2));
                        cmd.Parameters.AddWithValue("@Ping", Math.Round(ping, 2));

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                LogError("Speed test data saved to database successfully.");
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed.
                LogError($"Error saving speed test data: {ex.Message}");
            }
        }
    }
}
