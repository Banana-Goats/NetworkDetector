using System;
using System.IO;
using System.Linq;
using NetworkDetector.Services.Interfaces;

namespace NetworkDetector.Services.Implementations
{
    public class SharePointService : ISharePointService
    {
        public string GetLatestSharePointFileDate()
        {
            try
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var sharepointDirs = Directory
                    .EnumerateDirectories(userProfile, "*sharepoint*", SearchOption.AllDirectories);

                var mostRecentFile = sharepointDirs
                    .SelectMany(dir => Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
                    .Select(path => new FileInfo(path))
                    .OrderByDescending(fi => fi.LastWriteTime)
                    .FirstOrDefault();

                return mostRecentFile == null
                    ? "No files found"
                    : mostRecentFile.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                return "Error retrieving date";
            }
        }
    }
}
