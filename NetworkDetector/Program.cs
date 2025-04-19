using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace NetworkDetector
{
    internal static class Program
    {
        private const string MUTEX_NAME = "NetworkDetector";
        private const string LOG_FILE_NAME = "errorlog.txt";

        [STAThread]
        static void Main()
        {
            // Ensure only one instance of NetworkDetector runs
            bool createdNew;
            using (Mutex mutex = new Mutex(true, MUTEX_NAME, out createdNew))
            {
                if (!createdNew)
                {
                    // Another instance is already running. Exit silently or show a message if you prefer.
                    return;
                }

                // 1. Set up global exception handlers
                SetupGlobalExceptionHandlers();

                // 2. Standard WinForms init
                ApplicationConfiguration.Initialize(); // .NET 6+ WinForms template call
                Application.Run(new NetworkDetector());
            }
        }

        /// <summary>
        /// Attaches event handlers for unhandled exceptions (both UI and non-UI threads).
        /// These are logged to a text file in the application's directory.
        /// </summary>
        private static void SetupGlobalExceptionHandlers()
        {
            // Catch exceptions on the WinForms UI thread
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) =>
            {
                LogToFile("UI Thread Exception", e.Exception);
                // Optionally show a message or continue silently
                // MessageBox.Show($"An error occurred: {e.Exception.Message}");
            };

            // Catch exceptions on all other threads in the AppDomain
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                if (ex != null)
                {
                    LogToFile("Non-UI Thread Exception", ex);
                }
                // If you want the application to terminate, you could re-throw or call Environment.Exit
                // Environment.FailFast("Fatal exception", ex);
            };
        }

        /// <summary>
        /// Writes exception information to a text file in the same folder as the EXE.
        /// </summary>
        private static void LogToFile(string exceptionType, Exception ex)
        {
            try
            {
                // Build the path to errorlog.txt in the application's base directory
                string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_FILE_NAME);

                // Compose the log message
                string logMessage = $@"
[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exceptionType}
{ex}
--------------------------------------------------------------
";

                // Append to the file
                File.AppendAllText(logFilePath, logMessage);
            }
            catch
            {
                // If logging fails, we can't do much. Possibly write to EventLog or ignore.
            }
        }
    }
}
