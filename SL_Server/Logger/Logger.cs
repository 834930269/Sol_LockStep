using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace SL_Server
{
    public class Logger
    {
        public static Serilog.Core.Logger Instance { get { return coreLogger; } }

        private static Serilog.Core.Logger coreLogger;

        private static Logger warpLogger;
        /// <summary>
        /// 创建一个全局Log对象
        /// </summary>
        /// <param name="logFileName"></param>
        public static void Create(string logFileName)
        {
            warpLogger = (warpLogger == null) ? new Logger(logFileName) : warpLogger;
        }

        private Logger(string logFileName)
        {
            coreLogger = new LoggerConfiguration().
                WriteTo.Console().
                WriteTo.File($"{logFileName}-.txt", rollingInterval: RollingInterval.Day).
                CreateLogger();
        }

    }
}
