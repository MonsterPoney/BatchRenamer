using System;
using System.IO;

namespace BatchRenamer {
    class Logger {
        public string logName;
        public bool isWritten = false;
        public Logger(string path, string logTitle) {

            string dateLog = DateTime.Now.ToString("yyyy-MM-dd");
            int indiceLog = 1;
            string nomFic = path + "Log_" + dateLog + "_" + indiceLog.ToString() + ".log";

            // If logfile exists
            while (File.Exists(nomFic)) {
                indiceLog++;
                nomFic = path + "Log_" + dateLog + "_" + indiceLog.ToString() + ".log";
            }

            logName = nomFic;
            System.IO.File.WriteAllText(logName, $"-_-_-_- {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} - {logTitle} -_-_-_-{Environment.NewLine}");
        }

        public void WriteLog(string msg, string msgException = null) {
            var dateHeureLog = DateTime.Now;

            using (var sw = new StreamWriter(this.logName, true)) {
                sw.WriteLine(dateHeureLog.ToString("yyyy-MM-dd HH:mm:ss") + ">" + msg);

                if (msgException != null) {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.Black;
                    if (msgException.Contains("System.FormatException"))
                        Console.WriteLine("Synthax error can cause this exception");
                    Console.WriteLine($"Exception !!! logFile : {logName}");
                    Console.ResetColor();
                    sw.WriteLine("___________________________________________________________________________");
                    sw.WriteLine(msgException);
                    sw.WriteLine("___________________________________________________________________________");
                    Console.WriteLine("Press any key to continue");
                    Console.ReadKey();
                }
            }
            isWritten = true;
        }
    }
}
