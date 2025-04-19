using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.Devices;
using NetworkDetector.Services.Interfaces;

namespace NetworkDetector.Services.Implementations
{
    public class HardwareInfoService : IHardwareInfoService
    {
        public string GetCpuInfo()
        {
            try
            {
                // Retrieve raw CPU name
                string cpuName = string.Empty;
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                foreach (var obj in searcher.Get())
                {
                    cpuName = obj["Name"]?.ToString() ?? string.Empty;
                    break;
                }

                if (string.IsNullOrWhiteSpace(cpuName))
                    return "Unknown CPU";

                // Manufacturer-specific formatting
                if (cpuName.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var model = ExtractIntelModel(cpuName);
                    return model is not null
                        ? $"Intel {model}"
                        : "Intel (Unknown Model)";
                }
                else if (cpuName.IndexOf("AMD", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var model = ExtractAMDModel(cpuName);
                    return model is not null
                        ? $"AMD {model}"
                        : "AMD (Unknown Model)";
                }

                // Fallback: use raw name
                return cpuName;
            }
            catch
            {
                return "Unknown CPU";
            }
        }

        private string ExtractIntelModel(string cpuName)
        {
            // Patterns for Intel models
            string[] patterns = new[]
            {
                @"i[3579]\-\w+",          // e.g. i5-10300H
                @"Xeon\s+\w+[\-\w]*",    // e.g. Xeon E-2176G
                @"Pentium\s+\w+",         // Pentium variants
                @"Celeron\s+\w+",         // Celeron variants
                @"Core\(TM\)\s*(\d+)"   // Core(TM) series
            };

            foreach (var pat in patterns)
            {
                var match = Regex.Match(cpuName, pat, RegexOptions.IgnoreCase);
                if (match.Success)
                    return match.Value.Trim();
            }
            return null;
        }

        private string ExtractAMDModel(string cpuName)
        {
            // e.g. AMD Ryzen 5 3600 6-Core Processor -> Ryzen 5 3600
            var tokens = cpuName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int idx = Array.FindIndex(tokens, t => t.Equals("Ryzen", StringComparison.OrdinalIgnoreCase));
            if (idx >= 0 && idx + 2 < tokens.Length)
                return string.Join(' ', tokens[idx], tokens[idx + 1], tokens[idx + 2]);
            return null;
        }

        public string GetRamInfo()
        {
            try
            {
                var comp = new ComputerInfo();
                ulong totalBytes = comp.TotalPhysicalMemory;
                // Convert to GB
                double gb = totalBytes / 1024d / 1024d / 1024d;
                return $"{gb:0} GB";
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
                // Use system drive
                var root = Path.GetPathRoot(Environment.SystemDirectory);
                var drive = new DriveInfo(root);
                double totalGB = drive.TotalSize / 1024d / 1024d / 1024d;
                double usedGB = (drive.TotalSize - drive.AvailableFreeSpace) / 1024d / 1024d / 1024d;
                return $"{usedGB:0}/{totalGB:0} GB";
            }
            catch
            {
                return "Unknown Storage";
            }
        }

        public (string OsName, string BuildNumber) GetOsInfo()
        {
            try
            {
                string caption = string.Empty;
                using var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
                foreach (var obj in searcher.Get())
                {
                    caption = obj["Caption"]?.ToString() ?? string.Empty;
                    break;
                }

                if (caption.StartsWith("Microsoft ", StringComparison.OrdinalIgnoreCase))
                    caption = caption.Substring(10).Trim(); // remove 'Microsoft '

                string build = Environment.OSVersion.Version.Build.ToString();
                return (string.IsNullOrWhiteSpace(caption) ? "Unknown OS" : caption, build);
            }
            catch
            {
                return ("Unknown OS", "Unknown Build");
            }
        }
    }
}
