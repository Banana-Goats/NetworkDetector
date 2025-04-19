using System.IO;
using System.Management;
using Microsoft.VisualBasic.Devices;
using NetworkDetector.Services.Interfaces;

namespace NetworkDetector.Services.Implementations
{
    public class HardwareInfoService : IHardwareInfoService
    {
        public string GetCpuInfo()
        {
            using var searcher = new ManagementObjectSearcher("select Name from Win32_Processor");
            foreach (var item in searcher.Get())
                return item["Name"]?.ToString() ?? "Unknown";
            return "Unknown";
        }

        public string GetRamInfo()
        {
            var comp = new ComputerInfo();
            ulong total = comp.TotalPhysicalMemory;
            return $"{total / (1024 * 1024)} MB";
        }

        public string GetStorageInfo()
        {
            DriveInfo d = new DriveInfo(Path.GetPathRoot(System.Environment.SystemDirectory));
            long free = d.AvailableFreeSpace;
            long total = d.TotalSize;
            return $"{free / (1024 * 1024)} MB free of {total / (1024 * 1024)} MB";
        }

        public (string OsName, string BuildNumber) GetOsInfo()
        {
            using var searcher = new ManagementObjectSearcher("select Caption, BuildNumber from Win32_OperatingSystem");
            foreach (var item in searcher.Get())
            {
                string caption = item["Caption"]?.ToString() ?? "Unknown OS";
                string build = item["BuildNumber"]?.ToString() ?? "Unknown Build";
                return (caption, build);
            }
            return ("Unknown OS", "Unknown Build");
        }
    }
}
