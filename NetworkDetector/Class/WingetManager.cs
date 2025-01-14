using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NetworkDetector.Class
{
    public class WingetManager
    {
        /// <summary>
        /// Checks if a program is installed using winget.
        /// </summary>
        public async Task<bool> IsProgramInstalledAsync(string programName)
        {
            return await Task.Run(() =>
            {
                using (Process listProcess = new Process())
                {
                    listProcess.StartInfo.FileName = "winget";
                    listProcess.StartInfo.Arguments = $"list \"{programName}\"";
                    listProcess.StartInfo.RedirectStandardOutput = true;
                    listProcess.StartInfo.UseShellExecute = false;
                    listProcess.StartInfo.CreateNoWindow = true;

                    listProcess.Start();
                    string output = listProcess.StandardOutput.ReadToEnd();
                    listProcess.WaitForExit();

                    // Return true if program appears in the list output.
                    return output.IndexOf(programName, StringComparison.OrdinalIgnoreCase) >= 0;
                }
            });
        }

        /// <summary>
        /// Uninstalls a program using winget.
        /// </summary>
        public async Task UninstallProgramAsync(string programName)
        {
            await Task.Run(() =>
            {
                using (Process uninstallProcess = new Process())
                {
                    uninstallProcess.StartInfo.FileName = "winget";
                    uninstallProcess.StartInfo.Arguments = $"uninstall \"{programName}\" -h --silent";
                    uninstallProcess.StartInfo.UseShellExecute = false;
                    uninstallProcess.StartInfo.CreateNoWindow = true;

                    uninstallProcess.Start();
                    uninstallProcess.WaitForExit();
                }
            });
        }
    }
}
