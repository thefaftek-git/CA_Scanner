using Xunit;

namespace ConditionalAccessExporter.Tests
{
    /// <summary>
    /// Collection definition to ensure tests that manipulate Console.Out run sequentially
    /// to avoid race conditions in console output redirection.
    /// </summary>
    [CollectionDefinition("Console Output Tests", DisableParallelization = true)]
    public class ConsoleOutputTestCollection
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
