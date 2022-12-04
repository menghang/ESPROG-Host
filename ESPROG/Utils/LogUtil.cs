using Serilog;
using System;
using System.Windows.Controls;

namespace ESPROG.Utils
{
    class LogUtil
    {
        private static readonly TextBox ui;

        public LogUtil(TextBox textbox)
        {
            ui = textbox;
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Debug()
                .WriteTo.Async(a => a.File("logs\\log.log", rollingInterval: RollingInterval.Day))
                .CreateLogger();
        }

        private string BuildFullLog(string log, string level)
        {
            string timestamp = DateTime.Now.ToString("yy-MM-dd HH:mm:ss");
            return string.Format("[{0}][{1}] {2}{3}", timestamp, level, log, Environment.NewLine);
        }

        private void LogUi(string fullLog)
        {
            ui.Dispatcher.BeginInvoke(() =>
            {
                ui.AppendText(fullLog);
                ui.AppendText(Environment.NewLine);
                ui.ScrollToEnd();
            });
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
            LogUi(fullLog);
        }

        public void Clear()
        {
            ui.Dispatcher.BeginInvoke(() => ui.Clear());
        }
    }
}
