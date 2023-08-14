namespace ReportPackageUsage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    /// <summary>
    ///     Console logger with optional file logging
    /// </summary>
    /// <seealso cref="ILog" />
    public class Clogrr : ILog
    {
        /// <summary>
        ///     The log file path
        /// </summary>
        private readonly string _logFilePath;

        private readonly ConcurrentQueue<string> _fileBuffer;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Clogrr" /> class.
        /// </summary>
        public Clogrr()
        {
            _logFilePath = null;
        }

        public Clogrr(string logFileName, string logFileFolder, CancellationToken cancellationToken)
        {
             _ = logFileName ?? throw new ArgumentNullException(nameof(logFileName));
            _logFilePath = Path.Combine(logFileFolder, logFileName);
            Console.WriteLine($"Using log file path of '{_logFilePath}'");
            _fileBuffer = new ConcurrentQueue<string>();
            _cancellationToken = cancellationToken;
            var flushThread = new Thread(FileWriter);
            flushThread.IsBackground = false;
            flushThread.Name = "Clogrr Flush Worker";
            flushThread.Start();
        }

       
        /// <inheritdoc />
        public void Info(string message, string callingMethod = null)
        {
            var msg = $"{DateTime.Now:hh:mm:ss.fff} - {nameof(Info)} - {callingMethod} - {message}";
            Console.WriteLine(msg); //to the console
            Trace.TraceInformation(msg); // to the VS output debug window 
            if (string.IsNullOrWhiteSpace(_logFilePath) == false)
            {
               // Debug.WriteLine($"CLOGRR - enqueuing message '{msg}'");
                _fileBuffer.Enqueue(msg);
            }
        }

        /// <inheritdoc />
        public void Err(string message, Exception ex = null, string callingMethod = null)
        {
            var msg = $"{DateTime.Now:hh:mm:ss.fff} - {nameof(Err)} - {callingMethod} - {message} - {ex}";
            Console.Error.WriteLine(msg); //to the console
            Trace.TraceError(msg); // to the VS output debug window 
            if (string.IsNullOrWhiteSpace(_logFilePath) == false)
            {
                Debug.WriteLine($"CLOGRR - enqueuing message '{msg}'");
                _fileBuffer.Enqueue(msg);
            }
        }

        private void FileWriter()
        {
            while (_cancellationToken.IsCancellationRequested == false)
            {
                if (_fileBuffer.IsEmpty == false && _fileBuffer.TryDequeue(out var msg))
                {
                    File.AppendAllLines(_logFilePath, new List<string>(1) { msg });
                }

                Thread.Sleep(20);
            }

            Debug.WriteLine("Collecting any leftover messages");
            var restOfBuffer = new List<string>();
            while (_fileBuffer.IsEmpty == false)
            {
                if (_fileBuffer.TryDequeue(out var msg))
                {
                    restOfBuffer.Add(msg);
                }
            }

            if (restOfBuffer.Count > 0)
            {
                Debug.WriteLine($"Writing {restOfBuffer.Count} final messages");
                File.AppendAllLines(_logFilePath, restOfBuffer);
            }

            Thread.Sleep(500);
        }
    }
}