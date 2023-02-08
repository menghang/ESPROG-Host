using Serilog;
using System;
using System.Windows.Controls;

namespace ESPROG.Services
{
    class LogService
    {
        private readonly TextBox ui;

        public LogService(TextBox textbox)
        {
            ui = textbox;
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .WriteTo.Debug(outputTemplate: "{Message}")
#endif
                .WriteTo.Async(a => a.File("logs\\log.log", rollingInterval: RollingInterval.Day, outputTemplate: "{Message}"))
                .MinimumLevel.Debug()
                .CreateLogger();
        }

        private string BuildFullLog(string log, string logLevel)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            return string.Format("[{0}] [{1}] {2}{3}", timestamp, logLevel, log, Environment.NewLine);
        }

        public void Debug(string log)
        {
            string fullLog = BuildFullLog(log, "D");
            Log.Debug(fullLog);
        }

        public void Info(string log)
        {
            string fullLog = BuildFullLog(log, "I");
            Log.Information(fullLog);
            ui.Dispatcher.BeginInvoke(() =>
            {
                ui.AppendText(fullLog);
                ui.ScrollToEnd();
            });
        }

        public void Error(string log)
        {
            string fullLog = BuildFullLog(log, "E");
            Log.Error(fullLog);
            ui.Dispatcher.BeginInvoke(() =>
            {
                if (ui.LineCount > 128)
                {
                    ui.Clear();
                }
                ui.AppendText(fullLog);
                ui.ScrollToEnd();
            });
        }

        public void ClearLogBox()
        {
            ui.Dispatcher.BeginInvoke(() => ui.Clear());
        }
    }
}
