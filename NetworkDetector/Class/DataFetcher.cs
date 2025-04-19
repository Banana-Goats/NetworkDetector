using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using WUApiLib;
using Microsoft.VisualBasic.Devices;
using Newtonsoft.Json.Linq;

namespace NetworkDetector
{
    public class DataFetcher
    {
        private readonly NetworkDetector mainForm;

        public DataFetcher(NetworkDetector form)
        {
            mainForm = form;
        }

        private void LogError(string message) => mainForm?.LogError(message);

        public async Task<(string wanIp, string isp, bool mobile)> GetWanIpAndIspAsync()
        {
            try
            {
                // Use the URL that includes the desired fields.
                string url = "https://pro.ip-api.com/json/?key=APLimLdoSBHfzWP&fields=isp,mobile,query";
                string response = await SystemData.Client.GetStringAsync(url);
                var json = JObject.Parse(response);

                string ip = json["query"]?.ToString();
                string isp = json["isp"]?.ToString();
                bool mobile = json["mobile"] != null ? json["mobile"].ToObject<bool>() : false;

                return (ip, isp, mobile);
            }
            catch (Exception ex)
            {
                mainForm.LogError($"Error fetching WAN IP, ISP, and mobile status: {ex.Message}");
                return (null, null, false);
            }
        }

        public string GetCPUInfo()
        {
            string cpuInfo = string.Empty;
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        cpuInfo = obj["Name"]?.ToString() ?? "Unknown CPU";
                        break; // single CPU assumed
                    }
                }

                // Extract manufacturer and model
                if (cpuInfo.Contains("Intel"))
                {
                    string model = ExtractIntelModel(cpuInfo);
                    cpuInfo = $"Intel {model}";
                }
                else if (cpuInfo.Contains("AMD"))
                {
                    string model = ExtractAMDModel(cpuInfo);
                    cpuInfo = $"AMD {model}";
                }
            }
            catch
            {
                cpuInfo = "Unknown CPU";
            }

            return cpuInfo;
        }

        private string ExtractIntelModel(string cpuName)
        {
            // Regex patterns to capture various Intel CPU series (e.g., i5-10300H, Core(TM) 7 150U, Xeon, etc.)
            string[] patterns = new[]
            {
                @"(i\d{1,2}-\w+)",          // Matches models like i5-10300H, i7-12700K, etc.
                @"(Xeon\s+\w+[\-\w]*)",     // Matches Xeon variants
                @"(Pentium\s+\w+)",         // Matches Pentium variants
                @"(Celeron\s+\w+)",         // Matches Celeron variants
                @"Core\(TM\)\s+(\d+\s+\S+)" // Matches patterns like "Core(TM) 7 150U"
            };

            foreach (var pattern in patterns)
            {
                Match match = Regex.Match(cpuName, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return "Unknown Model";
        }

        private string ExtractAMDModel(string cpuName)
        {
            // Example: AMD Ryzen 5 3600 6-Core Processor -> Ryzen 5 3600
            string[] parts = cpuName.Split(' ');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Equals("Ryzen", StringComparison.OrdinalIgnoreCase))
                {
                    return $"{parts[i]} {parts[i + 1]} {parts[i + 2]}";
                }
            }
            return "Unknown Model";
        }

        public string GetRamInfo()
        {
            try
            {
                var computerInfo = new ComputerInfo();
                ulong totalMemory = computerInfo.TotalPhysicalMemory;
                return $"{(totalMemory / (1024 * 1024 * 1024)):0}GB";
            }
            catch
            {
                return "Unknown RAM";
            }
        }

        public string GetStorageInfo()
        {
            try
            {
                DriveInfo cDrive = new DriveInfo("C");
                long totalSize = cDrive.TotalSize;
                long availableSpace = cDrive.AvailableFreeSpace;
                long usedSpace = totalSize - availableSpace;
                return $"{(usedSpace / (1024 * 1024 * 1024)):0}/{(totalSize / (1024 * 1024 * 1024)):0}GB";
            }
            catch
            {
                return "Unknown Storage";
            }
        }

        public string GetWindowsOS()
        {
            try
            {
                string osName = string.Empty;

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject os in searcher.Get())
                    {
                        osName = os["Caption"].ToString();
                        break;
                    }
                }

                if (osName.StartsWith("Microsoft"))
                {
                    osName = osName.Replace("Microsoft", "").Trim();
                }

                return osName;
            }
            catch
            {
                return "Unknown OS";
            }
        }

        public string GetBuildNumber()
        {
            try
            {
                return Environment.OSVersion.Version.Build.ToString();
            }
            catch
            {
                return "Unknown Build";
            }
        }

        public async Task<string> GetLatestSharepointFileDateAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Locate OneDrive directories
                    List<string> oneDrivePaths = new List<string>();

                    // Method 1: OneDrive environment variable
                    string oneDriveEnv = Environment.GetEnvironmentVariable("OneDrive");
                    if (!string.IsNullOrEmpty(oneDriveEnv) && Directory.Exists(oneDriveEnv))
                    {
                        oneDrivePaths.Add(oneDriveEnv);
                    }

                    // Method 2: Search user profile for OneDrive
                    string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    var foundOneDriveDirs = Directory.GetDirectories(userProfilePath, "OneDrive*", SearchOption.TopDirectoryOnly);
                    foreach (var dir in foundOneDriveDirs)
                    {
                        if (Directory.Exists(dir) && !oneDrivePaths.Contains(dir))
                        {
                            oneDrivePaths.Add(dir);
                        }
                    }

                    if (oneDrivePaths.Count == 0)
                    {
                        return "OneDrive folder not found.";
                    }

                    // Search SharePoint folders
                    List<string> sharepointFolders = new List<string>();

                    foreach (var oneDrivePath in oneDrivePaths)
                    {
                        var folders = Directory.GetDirectories(oneDrivePath, "*sharepoint*", SearchOption.AllDirectories)
                                               .Where(dir => dir.IndexOf("sharepoint", StringComparison.OrdinalIgnoreCase) >= 0);

                        foreach (var folder in folders)
                        {
                            sharepointFolders.Add(folder);
                        }
                    }

                    sharepointFolders = sharepointFolders.Distinct().ToList();

                    if (sharepointFolders.Count == 0)
                    {
                        return "No Sharepoint";
                    }

                    FileInfo latestFile = null;
                    foreach (var sharepointFolder in sharepointFolders)
                    {
                        var allFiles = Directory.GetFiles(sharepointFolder, "*.*", SearchOption.AllDirectories);
                        if (allFiles.Length == 0) continue;

                        var currentLatestFile = allFiles
                            .Select(f => new FileInfo(f))
                            .OrderByDescending(fi => fi.LastWriteTime)
                            .FirstOrDefault();

                        if (currentLatestFile != null)
                        {
                            if (latestFile == null || currentLatestFile.LastWriteTime > latestFile.LastWriteTime)
                            {
                                latestFile = currentLatestFile;
                            }
                        }
                    }

                    if (latestFile != null)
                    {
                        return latestFile.LastWriteTime.ToString("dd/MM/yyyy");
                    }
                    else
                    {
                        return "No Sharepoint Files";
                    }
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            });
        }

        public async Task<string> GetPendingUpdatesAsync()
        {
            return await Task.Run(async () =>
            {
                StringBuilder resultText = new StringBuilder();
                var allUpdates = new List<UpdateRecord>();
                bool hasSecurityUpdates = false;

                try
                {
                    dynamic updateSession = Activator.CreateInstance(Type.GetTypeFromProgID("Microsoft.Update.Session"));
                    dynamic updateSearcher = updateSession.CreateUpdateSearcher();

                    dynamic searchResult = updateSearcher.Search("IsInstalled=0 AND IsHidden=0");

                    if (searchResult.Updates.Count == 0)
                    {
                        resultText.AppendLine("No");
                    }
                    else
                    {
                        foreach (dynamic update in searchResult.Updates)
                        {
                            Guid? updateGuid = null;
                            try
                            {
                                string guidString = update.Identity.UpdateID.ToString();
                                updateGuid = Guid.Parse(guidString);
                            }
                            catch
                            {
                                continue;
                            }

                            string kbNumber = null;
                            if (update.KBArticleIDs != null && update.KBArticleIDs.Count > 0)
                            {
                                kbNumber = update.KBArticleIDs[0].ToString();
                            }

                            bool isSecurity = false;
                            foreach (dynamic category in update.Categories)
                            {
                                if (category.Name.Contains("Security", StringComparison.OrdinalIgnoreCase))
                                {
                                    isSecurity = true;
                                    hasSecurityUpdates = true;
                                }
                            }

                            allUpdates.Add(new UpdateRecord
                            {
                                UpdateID = updateGuid.Value,
                                Name = update.Title,
                                KBNumber = kbNumber,
                                UpdateType = isSecurity ? "Security" : GetFirstCategoryName(update.Categories),
                                ReleaseDate = update.LastDeploymentChangeTime // might be null
                            });
                        }

                        resultText.AppendLine(hasSecurityUpdates ? "Yes" : "No");
                    }

                    //await SaveUpdatesToDatabaseAsync(allUpdates);
                }
                catch (Exception ex)
                {
                    resultText.AppendLine($"Error retrieving updates: {ex.Message}");
                }
                finally
                {

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                return resultText.ToString();
            });
        }

        public async Task<string> GetCompanyNameAsync(string machineName)
        {
            string companyName = "Unknown Company"; // Default value if no match is found

            try
            {
                var dbService = new DatabaseService();
                string query = "SELECT CompanyName FROM computers WHERE machinename = @MachineName";
                var parameters = new Dictionary<string, object>
                {
                    { "@MachineName", machineName }
                };

                var result = await dbService.ExecuteScalarAsync(query, parameters);
                if (result != null)
                {
                    companyName = result.ToString();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error fetching CompanyName: {ex.Message}");
            }

            return companyName;
        }        

        private string GetFirstCategoryName(dynamic categories)
        {
            try
            {
                if (categories != null && categories.Count > 0)
                {
                    return categories[0].Name;
                }
            }
            catch { /* ignore */ }
            return "Other";
        }

        public class UpdateRecord
        {
            public Guid UpdateID { get; set; }          // Main unique identifier (primary key in DB)
            public string Name { get; set; }            // e.g., "2023-07 Cumulative Update..."
            public string KBNumber { get; set; }        // e.g., "KB5026361" if available
            public string UpdateType { get; set; }      // "Security", "Critical", etc.
            public DateTime? ReleaseDate { get; set; }  // 'update.LastDeploymentChangeTime'
        }
    }
}
