

using System;
using System.Threading;

namespace ConditionalAccessExporter.Utils
{
    /// <summary>
    /// Simple spinner for CLI progress indication
    /// </summary>
    public class SimpleSpinner : IDisposable
    {
        private readonly string _message;
        private bool _disposed = false;
        private Thread? _spinnerThread;

        // Spinner characters
        private static readonly char[] _spinnerChars = { '-', '\\', '|', '/' };

        /// <summary>
        /// Create a new spinner with the specified message
        /// </summary>
        /// <param name="message">Message to display</param>
        public SimpleSpinner(string message)
        {
            _message = message;

            // Start spinner on a separate thread
            _spinnerThread = new Thread(SpinnerThread);
            _spinnerThread.IsBackground = true;
            _spinnerThread.Start();
        }

        /// <summary>
        /// Spinner thread implementation
        /// </summary>
        private void SpinnerThread()
        {
            int counter = 0;

            while (true)
            {
                if (_disposed)
                {
                    break;
                }

                // Update spinner character and print message
                char spinnerChar = _spinnerChars[counter % _spinnerChars.Length];
                Console.Write($"\r{_message} {spinnerChar}");
                counter++;

                // Sleep for a short period before next update
                Thread.Sleep(100);
            }

            // Clear the line when done
            if (!_disposed)
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Dispose of the spinner and stop the thread
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
            if (_spinnerThread != null && _spinnerThread.IsAlive)
            {
                // Wait for thread to finish
                _spinnerThread.Join();
            }
        }
    }
}

