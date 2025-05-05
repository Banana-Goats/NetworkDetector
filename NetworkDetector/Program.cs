using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NetworkDetector.Helpers;
using NetworkDetector.Services.Interfaces;
using NetworkDetector.Services.Implementations;

namespace NetworkDetector
{
    internal static class Program
    {
        private const string MUTEX_NAME = "NetworkDetector";
        private const string LOG_FILE_NAME = "errorlog.txt";

        [STAThread]
        static void Main()
        {
            var services = new ServiceCollection();

            ConfigureServices(services);

            using (var provider = services.BuildServiceProvider())
            {
                bool createdNew;
                using (var mutex = new Mutex(true, MUTEX_NAME, out createdNew))
                {
                    if (!createdNew)
                        return;

                    SetupGlobalExceptionHandlers();

                    ApplicationConfiguration.Initialize();
                    var mainForm = provider.GetRequiredService<NetworkDetector>();
                    Application.Run(mainForm);
                }
            }
        }

        private static void SetupGlobalExceptionHandlers()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) =>
            {
                LogToFile("UI Thread Exception", e.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                if (ex != null)
                {
                    LogToFile("Non-UI Thread Exception", ex);
                }
            };
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<DatabaseService>();

            services.AddSingleton<INetworkInfoService>(sp =>
            {
                var db = sp.GetRequiredService<DatabaseService>();
                var svc = new NetworkInfoService(db);
                // synchronously block so it's loaded at startup
                svc.InitializeAsync().GetAwaiter().GetResult();
                return svc;
            });

            // your data‐gathering services:
            //services.AddSingleton<INetworkInfoService, NetworkInfoService>();
            services.AddSingleton<IHardwareInfoService, HardwareInfoService>();
            services.AddSingleton<ICompanyService, CompanyService>();
            services.AddSingleton<ISharePointService, SharePointService>();

            services.AddSingleton<DatabaseService>();
            services.AddSingleton<CommandExecutor>();
            services.AddSingleton<NetworkDetector>();
        }

        private static void LogToFile(string exceptionType, Exception ex)
        {
            try
            {
                string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_FILE_NAME);

                string logMessage = $@"
                [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exceptionType}
                {ex}
                --------------------------------------------------------------
                ";

                File.AppendAllText(logFilePath, logMessage);
            }
            catch
            {

            }
        }
    }
}
