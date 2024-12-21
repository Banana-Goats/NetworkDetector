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

namespace NetworkDetector
{
    public class DataFetcher
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public async Task<(string wanIp, string isp)> GetWanIpAndIspAsync()
        {
            try
            {
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                string response = await httpClient.GetStringAsync("https://ipinfo.io/json");
                var json = Newtonsoft.Json.Linq.JObject.Parse(response);
                string ip = json["ip"]?.ToString();
                string org = json["org"]?.ToString();

                // Just return the org as is for now, remove the regex to simplify debugging
                // If org is null or empty, you'll know something is off with the response
                return (ip, org);
            }
            catch (Exception ex)
            {
                // Log the error somewhere visible
                Console.WriteLine("GetWanIpAndIspAsync Exception: " + ex.Message);
                // Or if you have access to the form's logTextBox, invoke and log it there
                return (null, null);
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
                        cpuInfo = obj["Name"].ToString();
                        break; // single CPU assumed
                    }
                }

                // Extract manufacturer and model
                if (cpuInfo.Contains("Intel"))
                {
                    cpuInfo = "Intel " + ExtractIntelModel(cpuInfo);
                }
                else if (cpuInfo.Contains("AMD"))
                {
                    cpuInfo = "AMD " + ExtractAMDModel(cpuInfo);
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
            // Example: Intel(R) Core(TM) i5-10300H CPU @ 2.50GHz -> i5-10300H
            string[] parts = cpuName.Split(' ');
            foreach (string part in parts)
            {
                if (part.StartsWith("i") && part.Contains("-"))
                {
                    return part;
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
                        return "No Sharepoint folders found.";
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
                        return "No files found in Sharepoint folders.";
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
            return await Task.Run(() =>
            {
                StringBuilder pendingUpdates = new StringBuilder();
                UpdateSession updateSession = null;
                IUpdateSearcher updateSearcher = null;

                try
                {
                    updateSession = new UpdateSession();
                    updateSearcher = updateSession.CreateUpdateSearcher();
                    ISearchResult searchResult = updateSearcher.Search("IsInstalled=0 AND IsHidden=0");

                    if (searchResult.Updates.Count == 0)
                    {
                        pendingUpdates.AppendLine("None");
                    }
                    else
                    {
                        foreach (IUpdate update in searchResult.Updates)
                        {
                            pendingUpdates.AppendLine(update.Title);
                        }
                    }
                }
                catch (Exception ex)
                {
                    pendingUpdates.AppendLine($"Error retrieving updates: {ex.Message}");
                }
                finally
                {
                    if (updateSearcher != null) Marshal.ReleaseComObject(updateSearcher);
                    if (updateSession != null) Marshal.ReleaseComObject(updateSession);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                return pendingUpdates.ToString();
            });
        }
    }
}
