using System;
using System.IO;
using System.Text;

namespace NomadGisMobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Global exception handlers to capture crashes and write stack traces to a file
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // Continue with normal startup
        }

        private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                LogException(e.ExceptionObject as Exception, "CurrentDomain.UnhandledException");
            }
            catch { }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                LogException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            }
            catch { }
        }

        private static void LogException(Exception? ex, string source)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== Unhandled exception: " + DateTime.UtcNow.ToString("u") + " ===");
                sb.AppendLine("Source: " + source);
                if (ex != null)
                {
                    sb.AppendLine(ex.ToString());
                }
                else
                {
                    sb.AppendLine("(no exception object)");
                }
                sb.AppendLine();

                var path = Path.Combine(FileSystem.AppDataDirectory, "crashlog.txt");
                File.AppendAllText(path, sb.ToString());
            }
            catch { }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}