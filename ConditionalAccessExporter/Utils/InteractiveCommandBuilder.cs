

using System.CommandLine;
using System.CommandLine.Invocation;

namespace ConditionalAccessExporter.Utils
{
    /// <summary>
    /// Provides an interactive command builder for the CLI
    /// </summary>
    public static class InteractiveCommandBuilder
    {
        /// <summary>
        /// Display a welcome message and prompt the user to choose a command
        /// </summary>
        /// <param name="rootCommand">The root command with all subcommands</param>
        /// <returns>The command selected by the user</returns>
        public static CommandPromptResult ShowMainMenu(RootCommand rootCommand)
        {
            Console.Clear();
            Console.WriteLine(ConsoleColorHelper.FormatHeader("Welcome to CA_Scanner - Azure Conditional Access Policy Management"));
            Console.WriteLine();

            var commands = rootCommand.Commands.Select(c => c.Name).ToList();
            if (commands.Count == 0)
            {
                Console.WriteLine(ConsoleColorHelper.FormatWarning("No commands available."));
                return new CommandPromptResult { CommandName = null, Cancelled = true };
            }

            Console.WriteLine(ConsoleColorHelper.FormatSubheader("Available Commands:"));
            for (int i = 0; i < commands.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {commands[i]} - {rootCommand.FindCommand(commands[i])?.Description}");
            }
            Console.WriteLine();
            Console.WriteLine($"{commands.Count + 1}. Exit");

            while (true)
            {
                Console.Write(ConsoleColorHelper.FormatProgress("Enter command number: "));
                var input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals((commands.Count + 1).ToString()))
                {
                    return new CommandPromptResult { CommandName = null, Cancelled = true };
                }

                if (int.TryParse(input, out int selection) && selection > 0 && selection <= commands.Count)
                {
                    return new CommandPromptResult
                    {
                        CommandName = commands[selection - 1],
                        Cancelled = false
                    };
                }

                Console.WriteLine(ConsoleColorHelper.FormatError("Invalid selection. Please enter a number from the list."));
            }
        }

        /// <summary>
        /// Display help for a specific command with interactive options
        /// </summary>
        /// <param name="command">The command to show help for</param>
        public static void ShowCommandHelp(Command command)
        {
            Console.Clear();
            Console.WriteLine(ConsoleColorHelper.FormatHeader($"CA_Scanner - {command.Name} Command Help"));
            Console.WriteLine();

            Console.WriteLine(command.Description);
            Console.WriteLine();

            if (command.Options.Count > 0)
            {
                Console.WriteLine(ConsoleColorHelper.FormatSubheader("Options:"));
                foreach (var option in command.Options)
                {
                    Console.WriteLine($"{option.Name():<25} {option.Description}");
                    if (option.GetDefaultValue() != null)
                    {
                        Console.WriteLine($"   Default: {option.GetDefaultValue()}");
                    }
                    Console.WriteLine();
                }
            }

            Console.WriteLine(ConsoleColorHelper.FormatSubheader("Examples:"));
            foreach (var example in GetCommandExamples(command))
            {
                Console.WriteLine(ConsoleColorHelper.FormatInfo(example));
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }

        /// <summary>
        /// Generate example commands for a given command
        /// </summary>
        /// <param name="command">The command to generate examples for</param>
        /// <returns>A list of example command strings</returns>
        private static List<string> GetCommandExamples(Command command)
        {
            var examples = new List<string>();

            // Basic command usage
            examples.Add($"ca-scanner {command.Name}");

            // Add options with defaults
            foreach (var option in command.Options.Where(o => !o.IsRequired))
            {
                if (option.GetDefaultValue() != null && option.GetDefaultValue().ToString() != string.Empty)
                {
                    examples.Add($"ca-scanner {command.Name} --{option.Name()} {GetExampleValue(option)}");
                }
            }

            // Add required options with example values
            foreach (var option in command.Options.Where(o => o.IsRequired))
            {
                examples.Add($"ca-scanner {command.Name} --{option.Name()} {GetExampleValue(option)}");
            }

            return examples;
        }

        /// <summary>
        /// Get an example value for a given option
        /// </summary>
        /// <param name="option">The option to get an example value for