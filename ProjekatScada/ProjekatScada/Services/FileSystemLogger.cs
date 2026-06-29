using System;
using System.IO;
using ProjekatScada.Services.Interfaces;

namespace ProjekatScada.Services
{
    public class FileSystemLogger : ISystemLogger
    {
        private readonly object _syncRoot = new object();
        private readonly string _logFilePath;

        public FileSystemLogger(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        public void Log(string actionDescription)
        {
            var logLine = string.Format("{0:yyyy-MM-dd HH:mm:ss} | {1}", DateTime.Now, actionDescription);
            lock (_syncRoot)
            {
                File.AppendAllLines(_logFilePath, new[] { logLine });
            }
        }
    }
}
