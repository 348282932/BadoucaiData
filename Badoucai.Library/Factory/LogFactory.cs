using System;
using System.IO;
using System.Linq;
using log4net;

namespace Badoucai.Library
{
    public static class LogFactory
    {
        private static readonly string path = AppDomain.CurrentDomain.BaseDirectory.Contains("\\bin\\") ? AppDomain.CurrentDomain.BaseDirectory.Remove(AppDomain.CurrentDomain.BaseDirectory.IndexOf("bin", StringComparison.Ordinal) - 1) + "\\Log\\" : AppDomain.CurrentDomain.BaseDirectory + "Log\\";

        private static ILog _logger;

        public static void Info(string content)
        {
            //_logger = LogManager.GetLogger("Info");

            //content = $"{content}";

            //_logger.Info(content);

            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine(content);

            Console.ResetColor();
        }

        public static void Error(string content)
        {
            _logger = LogManager.GetLogger("Error");

            content = $"{content}";

            _logger.Error(content);

            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine(content);

            Console.ResetColor();
        }

        public static void Warn(string content)
        {
            _logger = LogManager.GetLogger("Warn");

            content = $"{content}";

            _logger.Warn(content);

            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine(content);

            Console.ResetColor();
        }

        public static void Debug(string content)
        {
            _logger = LogManager.GetLogger("Debug");

            content = $"{content}";

            _logger.Debug(content);

            Console.BackgroundColor = ConsoleColor.White;

            Console.WriteLine(content);

            Console.ResetColor();
        }

        [Obsolete("log4net 无效时使用")]
        public static void SetErrorLog(string content)
        {
            var fileName = $"{path}\\Error\\";

            try
            {
                if (!Directory.Exists(fileName))
                    Directory.CreateDirectory(fileName);

                var filePath = $"{fileName}{DateTime.Now:yyyy-MM-dd}.txt";

                if (!File.Exists(filePath))
                    File.Create(filePath).Close();

                var writer = File.AppendText(filePath);

                writer.WriteLineAsync(DateTime.Now.ToString("HH:mm:ss") + Environment.NewLine + content);

                writer.Close();

                writer.Dispose();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        [Obsolete("log4net 无效时使用")]
        public static void SetInfoLog(string content)
        {
            var fileName = $"{path}\\Info\\";

            try
            {
                if (!Directory.Exists(fileName))
                    Directory.CreateDirectory(fileName);

                var filePath = $"{fileName}{DateTime.Now:yyyy-MM-dd}.txt";

                if (!File.Exists(filePath))
                    File.Create(filePath).Close();

                var writer = File.AppendText(filePath);

                writer.WriteLineAsync(DateTime.Now.ToString("HH:mm:ss") + Environment.NewLine + content);

                writer.Close();

                writer.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }
}
