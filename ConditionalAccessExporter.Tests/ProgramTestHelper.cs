using System.Reflection;
using System.Text;
using Microsoft.Graph.Models;

namespace ConditionalAccessExporter.Tests
{
    /// <summary>
    /// Helper class for testing Program.cs private methods
    /// </summary>
    internal static class ProgramTestHelper
    {
        /// <summary>
        /// Retrieves a private static method from the Program class using reflection.
        /// </summary>
        private static MethodInfo GetPrivateMethod(string methodName)
        {
            var method = typeof(Program).GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException($"Could not find {methodName} method in Program class");
            return method;
        }
        
        /// <summary>
        /// Invokes the private static Main method in Program class
        /// </summary>
        public static async Task<int> InvokeMainAsync(string[] args)
        {
            var mainMethod = GetPrivateMethod("Main");
            
            var result = await (Task<int>)mainMethod.Invoke(null, new object[] { args });
            return result;
        }

        /// <summary>
        /// Invokes the private static ExportPoliciesAsync method in Program class
        /// </summary>
        public static async Task<int> InvokeExportPoliciesAsync(string outputPath)
        {
            var method = GetPrivateMethod("ExportPoliciesAsync");
            
            var result = await (Task<int>)method.Invoke(null, new object[] { outputPath });
            return result;
        }

        /// <summary>
        /// Invokes the private static ConvertTerraformAsync method in Program class
        /// </summary>
        public static async Task<int> InvokeConvertTerraformAsync(
            string inputPath, 
            string outputPath,
            bool validate = true,
            bool verbose = false)
        {
            var method = GetPrivateMethod("ConvertTerraformAsync");
            
            var result = await (Task<int>)method.Invoke(null, new object[] { inputPath, outputPath, validate, verbose });
            return result;
        }

        /// <summary>
        /// Invokes the private static ConvertJsonToTerraformAsync method in Program class
        /// </summary>
        public static async Task<int> InvokeConvertJsonToTerraformAsync(
            string jsonInputPath,
            string terraformOutputDir,
            bool generateVariables,
            bool generateProvider,
            bool separateFiles,
            bool generateModule,
            bool includeComments,
            string providerVersion)
        {
            var method = GetPrivateMethod("ConvertJsonToTerraformAsync");
            
            var result = await (Task<int>)method.Invoke(
                null, 
                new object[] { 
                    jsonInputPath, 
                    terraformOutputDir,
                    generateVariables,
                    generateProvider,
                    separateFiles,
                    generateModule,
                    includeComments,
                    providerVersion
                });
            return result;
        }

        /// <summary>
        /// Invokes the private static ComparePoliciesAsync method in Program class
        /// </summary>
        public static async Task<int> InvokeComparePoliciesAsync(
            string referenceDirectory,
            string entraFile,
            string outputDirectory,
            string[] reportFormats,
            Models.MatchingStrategy matchingStrategy,
            bool caseSensitive,
            bool explainValues = false)
        {
            var method = GetPrivateMethod("ComparePoliciesAsync");
            
            var result = await (Task<int>)method.Invoke(
                null, 
                new object[] { 
                    referenceDirectory,
                    entraFile,
                    outputDirectory,
                    reportFormats,
                    matchingStrategy,
                    caseSensitive,
                    explainValues,
                    false, // exitOnDifferences
                    null,  // maxDifferences
                    new string[0], // failOn
                    new string[0], // ignore
                    false  // quiet
                });
            return result;
        }

        /// <summary>
        /// Invokes the private static CrossFormatComparePoliciesAsync method in Program class
        /// </summary>
        public static async Task<int> InvokeCrossFormatComparePoliciesAsync(
            string sourceDirectory,
            string referenceDirectory,
            string outputDirectory,
            string[] reportFormats,
            string matchingStrategy,
            bool caseSensitive,
            bool enableSemantic,
            double similarityThreshold)
        {
            var method = GetPrivateMethod("CrossFormatComparePoliciesAsync");
            
            var result = await (Task<int>)method.Invoke(
                null, 
                new object[] { 
                    sourceDirectory,
                    referenceDirectory,
                    outputDirectory,
                    reportFormats,
                    matchingStrategy,
                    caseSensitive,
                    enableSemantic,
                    similarityThreshold
                });
            return result;
        }

        /// <summary>
        /// Invokes the private static FetchEntraPoliciesAsync method in Program class
        /// </summary>
        public static async Task<ConditionalAccessPolicyCollectionResponse> InvokeFetchEntraPoliciesAsync()
        {
            var method = GetPrivateMethod("FetchEntraPoliciesAsync");
            
            var result = await (Task<ConditionalAccessPolicyCollectionResponse>)method.Invoke(null, null);
            return result;
        }

        /// <summary>
        /// Invokes the private static HandleExceptionAsync method in Program class
        /// </summary>
        public static async Task InvokeHandleExceptionAsync(Exception exception)
        {
            var method = GetPrivateMethod("HandleExceptionAsync");
            
            await (Task)method.Invoke(null, new object[] { exception });
        }

        /// <summary>
        /// Capture console output to a string
        /// </summary>
        public static string CaptureConsoleOutput(Action action)
        {
            // Synchronously wrapping the asynchronous CaptureConsoleOutputInternal method
            // to provide a synchronous interface for capturing console output.
            return CaptureConsoleOutputInternal(() => { action(); return Task.CompletedTask; }).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Capture console output to a string (async version)
        /// </summary>
        public static async Task<string> CaptureConsoleOutputAsync(Func<Task> action)
        {
            return await CaptureConsoleOutputInternal(action);
        }

        /// <summary>
        /// Internal helper method for console output capture with proper exception handling
        /// </summary>
        private static async Task<string> CaptureConsoleOutputInternal(Func<Task> action)
        {
            var originalOutput = Console.Out;
            var originalError = Console.Error;
            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);
            Console.SetError(stringWriter); // Also capture stderr
            
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging purposes
                stringWriter.WriteLine($"Exception occurred: {ex.Message}");
                stringWriter.WriteLine(ex.StackTrace);
                // Even if an exception occurs, we still want to return any captured output
                // The tests are checking for console output, not success/failure
            }
            finally
            {
                Console.SetOut(originalOutput);
                Console.SetError(originalError); // Restore stderr
            }
            
            return stringWriter.ToString();
        }
    }
}