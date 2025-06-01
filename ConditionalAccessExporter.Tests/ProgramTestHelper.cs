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
        /// Invokes the private static Main method in Program class
        /// </summary>
        public static async Task<int> InvokeMainAsync(string[] args)
        {
            var mainMethod = typeof(Program).GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static);
            if (mainMethod == null)
                throw new InvalidOperationException("Could not find Main method in Program class");
            
            var result = await (Task<int>)mainMethod.Invoke(null, new object[] { args });
            return result;
        }

        /// <summary>
        /// Invokes the private static ExportPoliciesAsync method in Program class
        /// </summary>
        public static async Task<int> InvokeExportPoliciesAsync(string outputPath)
        {
            var method = typeof(Program).GetMethod("ExportPoliciesAsync", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException("Could not find ExportPoliciesAsync method in Program class");
            
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
            var method = typeof(Program).GetMethod("ConvertTerraformAsync", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException("Could not find ConvertTerraformAsync method in Program class");
            
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
            var method = typeof(Program).GetMethod("ConvertJsonToTerraformAsync", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException("Could not find ConvertJsonToTerraformAsync method in Program class");
            
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
            bool caseSensitive)
        {
            var method = typeof(Program).GetMethod("ComparePoliciesAsync", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException("Could not find ComparePoliciesAsync method in Program class");
            
            var result = await (Task<int>)method.Invoke(
                null, 
                new object[] { 
                    referenceDirectory,
                    entraFile,
                    outputDirectory,
                    reportFormats,
                    matchingStrategy,
                    caseSensitive
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
            var method = typeof(Program).GetMethod("CrossFormatComparePoliciesAsync", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException("Could not find CrossFormatComparePoliciesAsync method in Program class");
            
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
            var method = typeof(Program).GetMethod("FetchEntraPoliciesAsync", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException("Could not find FetchEntraPoliciesAsync method in Program class");
            
            var result = await (Task<ConditionalAccessPolicyCollectionResponse>)method.Invoke(null, null);
            return result;
        }

        /// <summary>
        /// Invokes the private static HandleExceptionAsync method in Program class
        /// </summary>
        public static async Task InvokeHandleExceptionAsync(Exception exception)
        {
            var method = typeof(Program).GetMethod("HandleExceptionAsync", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException("Could not find HandleExceptionAsync method in Program class");
            
            await (Task)method.Invoke(null, new object[] { exception });
        }

        /// <summary>
        /// Capture console output to a string
        /// </summary>
        public static string CaptureConsoleOutput(Action action)
        {
            var originalOutput = Console.Out;
            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);
            
            try
            {
                action();
                return stringWriter.ToString();
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }
        
        /// <summary>
        /// Capture console output to a string (async version)
        /// </summary>
        public static async Task<string> CaptureConsoleOutputAsync(Func<Task> action)
        {
            var originalOutput = Console.Out;
            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);
            
            try
            {
                await action();
                return stringWriter.ToString();
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }
    }
}