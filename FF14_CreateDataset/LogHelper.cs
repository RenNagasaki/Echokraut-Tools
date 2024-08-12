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
        static string logText = "";
        static internal void logData(string data)
        {
            Console.WriteLine(data);
            logText += DateTime.Now.ToString() + " -> " + data + "\r\n";
            if (!Directory.Exists("logs"))
                Directory.CreateDirectory("logs");

            File.WriteAllText(logPath, logText);
        }
    }
}
