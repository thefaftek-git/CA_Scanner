
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ConditionalAccessExporter.Utils
{
    public static class FileHelper
    {
        /// <summary>
        /// Sanitizes a filename by replacing invalid characters with underscores
        /// and ensuring the result is a valid filename.
        /// </summary>
        /// <param name="fileName">The filename to sanitize</param>
        /// <returns>A sanitized filename safe for use on the filesystem</returns>
        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "unnamed";
                
            // Get invalid characters for file names
            var invalidChars = Path.GetInvalidFileNameChars();
            
            // Replace invalid characters with underscores
            var sanitized = new StringBuilder();
            foreach (char c in fileName)
            {
                if (invalidChars.Contains(c))
                    sanitized.Append('_');
                else
                    sanitized.Append(c);
            }
            
            // Replace multiple consecutive underscores with single underscore
            var collapsed = new StringBuilder();
            bool lastWasUnderscore = false;
            foreach (char c in sanitized.ToString())
            {
                if (c == '_')
                {
                    if (!lastWasUnderscore)
                    {
                        collapsed.Append(c);
                    }
                    lastWasUnderscore = true;
                }
                else
                {
                    collapsed.Append(c);
                    lastWasUnderscore = false;
                }
            }
            var result = collapsed.ToString();
                
            // Trim underscores from start and end
            result = result.Trim('_');
            
            // Ensure we have a valid filename
            if (string.IsNullOrWhiteSpace(result))
                return "unnamed";
                
            return result;
        }
    }
}

