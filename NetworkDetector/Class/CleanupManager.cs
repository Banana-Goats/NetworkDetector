using System;
using System.IO;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace NetworkDetector
{
    public class CleanupManager
    {
        private readonly NetworkDetector _mainForm;
        private readonly TextBox _logTextBox;
        private readonly Timer _cleanupTimer;


        public int CleanupInterval
        {
            get => _cleanupTimer.Interval;
            set => _cleanupTimer.Interval = value;
        }

        public CleanupManager(NetworkDetector mainForm, TextBox logTextBox)
        {
            _mainForm = mainForm;
            _logTextBox = logTextBox;

            // Create a WinForms timer for periodic cleanup
            _cleanupTimer = new Timer();
            _cleanupTimer.Tick += CleanupTimer_Tick;
        }

        public void StartCleanup()
        {
            _cleanupTimer.Start();
        }

        private void CleanupTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Example logging method from the form, if it exists
                _mainForm.LogError("Running periodic cleanup...");

                // GC to free memory
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Example: Truncate log if it’s too long
                if (_logTextBox.Lines.Length > 5000)
                {
                    _logTextBox.Clear();
                    _logTextBox.AppendText($"{DateTime.Now}: Logs truncated by cleanup.\r\n");
                }

                // Clean up old leftover directories in %TEMP% (e.g., "Extracted_*")
                CleanupOldTempDirectories();
            }
            catch (Exception ex)
            {
                _mainForm.LogError($"Cleanup Timer Error: {ex.Message}");
            }
        }

        private void CleanupOldTempDirectories()
        {
            string tempPath = Path.GetTempPath();
            var dirs = Directory.GetDirectories(tempPath, "Extracted_*", SearchOption.TopDirectoryOnly);

            foreach (string dirPath in dirs)
            {
                try
                {
                    var di = new DirectoryInfo(dirPath);
                    // E.g., older than 24 hours
                    if (DateTime.Now - di.CreationTime > TimeSpan.FromHours(24))
                    {
                        di.Delete(true);
                        _mainForm.LogError($"Removed old temp folder: {dirPath}");
                    }
                }
                catch (Exception ex)
                {
                    _mainForm.LogError($"Error removing {dirPath}: {ex.Message}");
                }
            }
        }
    }
}
