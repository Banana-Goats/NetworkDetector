using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NetworkDetector.Services.Interfaces;
using static System.Environment;

namespace NetworkDetector.Services.Implementations
{
    public class SharePointService : ISharePointService
    {
        public string GetLatestSharePointFileDate()
        {
            try
            {
                // 1) Find OneDrive roots
                var oneDriveRoots = new List<string>();
                string env = Environment.GetEnvironmentVariable("OneDrive");
                if (!string.IsNullOrEmpty(env) && Directory.Exists(env))
                    oneDriveRoots.Add(env);

                // fallback: search under %UserProfile% for OneDrive*
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var dirs = Directory.EnumerateDirectories(userProfile, "OneDrive*", SearchOption.TopDirectoryOnly);
                foreach (var d in dirs)
                    if (!oneDriveRoots.Contains(d, StringComparer.OrdinalIgnoreCase))
                        oneDriveRoots.Add(d);

                if (oneDriveRoots.Count == 0)
                    return "OneDrive not found";

                // 2) Setup enumeration options
                var opts = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    MaxRecursionDepth = 8
                };

                FileInfo latest = null;

                // 3) Scan each OneDrive root for "sharepoint" folders
                foreach (var root in oneDriveRoots)
                {
                    // find any folder under root whose name contains "sharepoint"
                    var spDirs = Directory.EnumerateDirectories(root, "*sharepoint*", opts);

                    foreach (var dir in spDirs)
                    {
                        foreach (var filePath in Directory.EnumerateFiles(dir, "*.*", opts))
                        {
                            var fi = new FileInfo(filePath);
                            if (latest == null || fi.LastWriteTime > latest.LastWriteTime)
                                latest = fi;
                        }
                    }
                }

                if (latest == null)
                    return "No SharePoint files";

                return latest.LastWriteTime.ToString("dd-MM-yyyy");
            }
            catch (Exception ex)
            {
                // diagnose the real error in Debug output
                System.Diagnostics.Debug.WriteLine($"[SharePointService] Error: {ex.GetType().Name}: {ex.Message}");
                return "Error retrieving date";
            }
        }
    }
}
