using System;
using System.Threading;
using System.Windows.Forms;

namespace NetworkDetector
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "NetworkDetector", out createdNew))
            {
                if (createdNew)
                {
                    // Initialize application configuration
                    ApplicationConfiguration.Initialize();
                    Application.Run(new NetworkDetector());
                }
                else
                {
                    // Application is already running.
                    // Optionally, you can bring the existing instance to the foreground or exit silently.

                    // To exit silently, simply return without any message.
                    return;

                    // If you prefer to show a message, uncomment the following line:
                    // MessageBox.Show("Another instance of Network Detector is already running.", "Instance Already Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
