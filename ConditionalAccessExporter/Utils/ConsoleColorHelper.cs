
using System;

namespace ConditionalAccessExporter.Utils
{
    /// <summary>
    /// Helper class for colored console output with ANSI escape codes
    /// </summary>
    public static class ConsoleColorHelper
    {
        /// <summary>
        /// Reset all formatting to default
        /// </summary>
        public const string Reset = "\x1b[0m";

        /// <summary>
        /// Black text
        /// </summary>
        public const string Black = "\x1b[30m";

        /// <summary>
        /// Red text
        /// </summary>
        public const string Red = "\x1b[31m";

        /// <summary>
        /// Green text
        /// </summary>
        public const string Green = "\x1b[32m";

        /// <summary>
        /// Yellow text
        /// </summary>
        public const string Yellow = "\x1b[33m";

        /// <summary>
        /// Blue text
        /// </summary>
        public const string Blue = "\x1b[34m";

        /// <summary>
        /// Magenta text
        /// </summary>
        public const string Magenta = "\x1b[35m";

        /// <summary>
        /// Cyan text
        /// </summary>
        public const string Cyan = "\x1b[36m";

        /// <summary>
        /// White text
        /// </summary>
        public const string White = "\x1b[37m";

        /// <summary>
        /// Bold text style
        /// </summary>
        public const string Bold = "\x1b[1m";

        /// <summary>
        /// Underline text style
        /// </summary>
        public const string Underline = "\x1b[4m";

        /// <summary>
        /// Inverse/negative text style (swap foreground and background)
        /// </summary>
        public const string Inverse = "\x1b[7m";

        /// <summary>
        /// Check if the console supports ANSI escape codes
        /// </summary>
        public static bool IsConsoleColorSupported => Environment.GetEnvironmentVariable("TERM") != "dumb" &&
                                                      !OperatingSystem.IsWindows() ||  // Windows 10+ supports ANSI
                                                      RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                                                      (Environment.Version.Major > 10 ||
                                                       (Environment.Version.Major == 10 && Environment.Version.Minor >= 0));

        /// <summary>
        /// Apply color formatting to a string
        /// </summary>
        /// <param name="text">The text to format</param>
        /// <param name="color">The color to apply</param>
        /// <param name="styles">Additional styles (bold, underline, etc.)</param>
        /// <returns>Formatted string with ANSI escape codes</returns>
        public static string Colorize(string text, string color, params string[] styles)
        {
            if (!IsConsoleColorSupported) return text;
            if (string.IsNullOrEmpty(text)) return text;
            if (string.IsNullOrEmpty(color)) return text;

            var formatted = $"{color}{styles.Aggregate(string.Empty, (current, style) => current + style)}{text}{Reset}";
            return formatted;
        }

        /// <summary>
        /// Write colored text to the console
        /// </summary>
        /// <param name="text">The text to write</param>
        /// <param name="color">The color to apply</param>
        /// <param name="styles">Additional styles (bold, underline, etc.)</param>
        public static void WriteColored(string text, string color, params string[] styles)
        {
            if (!IsConsoleColorSupported)
            {
                Console.Write(text);
                return;
            }

            Console.Write(Colorize(text, color, styles));
        }

        /// <summary>
        /// WriteLine colored text to the console
        /// </summary>
        /// <param name="text">The text to write</param>
        /// <param name="color">The color to apply</param>
        /// <param name="styles">Additional styles (bold, underline, etc.)</param>
        public static void WriteLineColored(string text, string color, params string[] styles)
        {
            if (!IsConsoleColorSupported)
            {
                Console.WriteLine(text);
                return;
            }

            Console.WriteLine(Colorize(text, color, styles));
        }

        /// <summary>
        /// Get a formatted string with section header styling
        /// </summary>
        /// <param name="text">The header text</param>
        /// <returns>Formatted header string</returns>
        public static string FormatHeader(string text)
        {
            return Colorize(text, Blue, Bold);
        }

        /// <summary>
        /// Get a formatted string with section subheader styling
        /// </summary>
        /// <param name="text">The subheader text</param>
        /// <returns>Formatted subheader string</returns>
        public static string FormatSubheader(string text)
        {
            return Colorize(text, Cyan, Bold);
        }

        /// <summary>
        /// Get a formatted string with success message styling
        /// </summary>
        /// <param name="text">The success text</param>
        /// <returns>Formatted success string</returns>
        public static string FormatSuccess(string text)
        {
            return Colorize(text, Green);
        }

        /// <summary>
        /// Get a formatted string with warning message styling
        /// </summary>
        /// <param name="text">The warning text</param>
        /// <returns>Formatted warning string</returns>
        public static string FormatWarning(string text)
        {
            return Colorize(text, Yellow);
        }

        /// <summary>
        /// Get a formatted string with error message styling
        /// </summary>
        /// <param name="text">The error text</param>
        /// <returns>Formatted error string</returns>
        public static string FormatError(string text)
        {
            return Colorize(text, Red);
        }

        /// <summary>
        /// Get a formatted string with important note styling
        /// </summary>
        /// <param name="text">The note text</param>
        /// <returns>Formatted note string</returns>
        public static string FormatNote(string text)
        {
            return Colorize(text, Magenta);
        }

        /// <summary>
        /// Get a formatted string with info message styling
        /// </summary>
        /// <param name="text">The info text</param>
        /// <returns>Formatted info string</returns>
        public static string FormatInfo(string text)
        {
            return Colorize(text, White);
        }

        /// <summary>
        /// Get a formatted string with verbose message styling
        /// </summary>
        /// <param name="text">The verbose text</param>
        /// <returns>Formatted verbose string</returns>
        public static string FormatVerbose(string text)
        {
            return Colorize(text, Cyan);
        }

        /// <summary>
        /// Get a formatted string with progress message styling
        /// </summary>
        /// <param name="text">The progress text</param>
        /// <returns>Formatted progress string</returns>
        public static string FormatProgress(string text)
        {
            return Colorize(text, Blue);
        }
    }
}
