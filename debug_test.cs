using System;
using System.Threading.Tasks;
using ConditionalAccessExporter.Tests;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Testing console output capture...");
        
        string[] testArgs = { "json-to-terraform", "--input", "policies.json" };
        
        try
        {
            var capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
            {
                await ProgramTestHelper.InvokeMainAsync(testArgs);
            });
            
            Console.WriteLine($"Captured output is null: {capturedOutput == null}");
            Console.WriteLine($"Captured output length: {capturedOutput?.Length ?? 0}");
            Console.WriteLine($"Captured output content: '{capturedOutput}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
