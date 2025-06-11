using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace ConditionalAccessExporter.Utils
{
    /// <summary>
    /// Provides streaming JSON processing capabilities for handling large files efficiently
    /// without loading entire contents into memory.
    /// </summary>
    public static class StreamingJsonProcessor
    {
        /// <summary>
        /// Reads and processes a large JSON file using streaming to minimize memory usage.
        /// </summary>
        /// <param name="filePath">Path to the JSON file</param>
        /// <param name="processor">Function to process each JSON token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task that completes when processing is done</returns>
        public static async Task ProcessJsonStreamAsync(
            string filePath, 
            Func<JToken, Task<bool>> processor, 
            CancellationToken cancellationToken = default)
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536, useAsync: true);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, bufferSize: 65536);
            using var jsonReader = new JsonTextReader(streamReader);

            var serializer = new JsonSerializer();
            
            while (await jsonReader.ReadAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (jsonReader.TokenType == JsonToken.StartObject)
                {
                    var jObject = await JToken.ReadFromAsync(jsonReader, cancellationToken);
                    
                    // Process the object and check if we should continue
                    var shouldContinue = await processor(jObject);
                    if (!shouldContinue)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Reads a large JSON array and processes each element individually to minimize memory usage.
        /// </summary>
        /// <typeparam name="T">Type of objects in the array</typeparam>
        /// <param name="filePath">Path to the JSON file containing an array</param>
        /// <param name="processor">Function to process each array element</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task that completes when processing is done</returns>
        public static async Task ProcessJsonArrayStreamAsync<T>(
            string filePath, 
            Func<T, Task<bool>> processor, 
            CancellationToken cancellationToken = default)
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536, useAsync: true);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, bufferSize: 65536);
            using var jsonReader = new JsonTextReader(streamReader);

            var serializer = new JsonSerializer();

            // Read until we find the start of the array
            while (await jsonReader.ReadAsync(cancellationToken))
            {
                if (jsonReader.TokenType == JsonToken.StartArray)
                {
                    // Process each array element
                    while (await jsonReader.ReadAsync(cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        if (jsonReader.TokenType == JsonToken.EndArray)
                        {
                            break;
                        }

                        if (jsonReader.TokenType == JsonToken.StartObject)
                        {
                            var item = serializer.Deserialize<T>(jsonReader);
                            if (item != null)
                            {
                                var shouldContinue = await processor(item);
                                if (!shouldContinue)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Writes JSON data to a file using streaming to handle large datasets efficiently.
        /// </summary>
        /// <param name="filePath">Output file path</param>
        /// <param name="dataProvider">Function that provides data to write</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task that completes when writing is done</returns>
        public static async Task WriteJsonStreamAsync<T>(
            string filePath, 
            Func<CancellationToken, IAsyncEnumerable<T>> dataProvider, 
            CancellationToken cancellationToken = default)
        {
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 65536, useAsync: true);
            using var streamWriter = new StreamWriter(fileStream, Encoding.UTF8, bufferSize: 65536);
            using var jsonWriter = new JsonTextWriter(streamWriter) { Formatting = Formatting.Indented };

            var serializer = new JsonSerializer();
            
            await jsonWriter.WriteStartArrayAsync(cancellationToken);

            await foreach (var item in dataProvider(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                serializer.Serialize(jsonWriter, item);
                await streamWriter.FlushAsync();
            }

            await jsonWriter.WriteEndArrayAsync(cancellationToken);
        }

        /// <summary>
        /// Estimates the memory usage that would be required to load a JSON file entirely into memory.
        /// </summary>
        /// <param name="filePath">Path to the JSON file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Estimated memory usage in bytes</returns>
        public static Task<long> EstimateMemoryUsageAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                return Task.FromResult(0L);
            }

            // JSON in memory typically uses 2-4x more memory than file size due to:
            // - UTF-16 encoding in .NET strings (2x for ASCII content)
            // - Object overhead and property names
            // - Parsing structures
            var estimatedMemoryUsage = fileInfo.Length * 3;
            
            return Task.FromResult(estimatedMemoryUsage);
        }

        /// <summary>
        /// Determines if a file should be processed using streaming based on its size.
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="threshold">Size threshold in bytes (default: 10MB)</param>
        /// <returns>True if streaming should be used, false otherwise</returns>
        public static bool ShouldUseStreaming(string filePath, long threshold = 10 * 1024 * 1024)
        {
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Exists && fileInfo.Length > threshold;
        }
    }
}
