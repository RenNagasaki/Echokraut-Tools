using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FF14_Echokraut.Helpers
{
    static internal class LogHelper
    {
        static string logPath = "logs\\" + DateTime.Now.ToString().Replace(":", "-").Replace(" ", "_") + "_" + "App.log";

        static internal void logData(string data)
        {
            MainWindow.mainWindow.tBlock_log.Text += DateTime.Now.ToString() + " -> " + data + "\r\n";
            MainWindow.mainWindow.scroll_Log.ScrollToBottom();
            if (!Directory.Exists("logs"))
                Directory.CreateDirectory("logs");

            File.WriteAllText(logPath, MainWindow.mainWindow.tBlock_log.Text);
        }

        static internal void logThread(string logMsg)
        {
            MainWindow.mainWindow.Dispatcher.Invoke(() =>
            {
                logData(logMsg);
            });
        }
    }
}
