

        /// <summary>
        /// Get an example value for a given option
        /// </summary>
        /// <param name="option">The option to get an example value for</param>
        /// <returns>An example value for the option</returns>
        private static string GetExampleValue(Option option)
        {
            // For file paths, use a placeholder
            if (option.Name().EndsWith("Path", StringComparison.OrdinalIgnoreCase) ||
                option.Name().Contains("file", StringComparison.OrdinalIgnoreCase) ||
                option.Name().Contains("dir", StringComparison.OrdinalIgnoreCase) ||
                option.Name().Contains("output", StringComparison.OrdinalIgnoreCase))
            {
                return "example-path.json";
            }

            // For boolean options, show both true and false
            if (option.ArgumentAcceptsAnyValue())
            {
                return option.GetDefaultValue()?.ToString() ?? "example-value";
            }

            return "example-value";
        }

        /// <summary>
        /// Prompt the user to confirm an action
        /// </summary>
        /// <param name="message">The confirmation message</param>
        /// <returns>True if the user confirms, false otherwise</returns>
        public static bool ConfirmAction(string message)
        {
            while (true)
            {
                Console.Write(ConsoleColorHelper.FormatProgress($"{message} (y/n): "));
                var input = Console.ReadLine()?.Trim().ToLower();

                if (input == "y" || input == "yes")
                {
                    return true;
                }
                else if (input == "n" || input == "no")
                {
                    return false;
                }

                Console.WriteLine(ConsoleColorHelper.FormatError("Please enter 'y' or 'n'."));
            }
        }

        /// <summary>
        /// Prompt the user to select an option from a list
        /// </summary>
        /// <typeparam name="T">The type of items in the list</typeparam>
        /// <param name="items">The list of items to choose from</param>
        /// <param name="prompt">The prompt message</param>
        /// <returns>The selected item or default if cancelled</returns>
        public static T? SelectFromList<T>(List<T> items, string prompt)
        {
            Console.WriteLine(ConsoleColorHelper.FormatSubheader(prompt));
            for (int i = 0; i < items.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {items[i]}");
            }
            Console.WriteLine();

            while (true)
            {
                Console.Write(ConsoleColorHelper.FormatProgress("Enter number: "));
                var input = Console.ReadLine()?.Trim();

                if (int.TryParse(input, out int selection) && selection > 0 && selection <= items.Count)
                {
                    return items[selection - 1];
                }

                Console.WriteLine(ConsoleColorHelper.FormatError("Invalid selection. Please enter a number from the list."));
            }
        }

        /// <summary>
        /// Display an interactive tutorial for using CA_Scanner
        /// </summary>
        public static void ShowTutorial()
        {
            Console.Clear();
            Console.WriteLine(ConsoleColorHelper.FormatHeader("CA_Scanner Interactive Tutorial"));
            Console.WriteLine();

            var steps = new[]
            {
                "Welcome to the CA_Scanner tutorial! This guide will help you get started with managing Azure Conditional Access policies.",
                "Step 1: Authentication",
                "Before using CA_Scanner, ensure you have set up your Azure app registration and environment variables (AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET).",
                "You can find setup instructions in the documentation at docs/GITHUB_SECRETS_SETUP.md.",
                "",
                "Step 2: Basic Export",
                "The most basic operation is exporting your current Conditional Access policies using:",
                ConsoleColorHelper.FormatInfo("dotnet run export"),
                "This will create a JSON file with all your policies.",
                "",
                "Step 3: Baseline Generation",
                "To create reference policy files for comparison, use the baseline command:",
                ConsoleColorHelper.FormatInfo("dotnet run baseline --output-dir ./policy-baselines"),
                "These baselines can be used to detect changes in your environment.",
                "",
                "Step 4: Policy Comparison",
                "Compare live policies against your baselines with:",
                ConsoleColorHelper.FormatInfo("dotnet run compare --reference-dir ./policy-baselines"),
                "This will show you any differences between your current policies and the baselines.",
                "",
                "Step 5: Terraform Integration",
                "You can convert JSON policies to Terraform format for infrastructure as code workflows:",
                ConsoleColorHelper.FormatInfo("dotnet run json-to-terraform --input exported-policies.json --output-dir ./terraform"),
                "",
                "Step 6: Advanced Features",
                "Explore other commands like cross-format comparison, validation, and remediation in the command help.",
                "",
                "Need Help?",
                "For detailed information on any command, use the --help flag:",
                ConsoleColorHelper.FormatInfo("dotnet run export --help"),
                "",
                "Ready to get started? Try exporting your policies now!"
            };

            foreach (var step in steps)
            {
                Console.WriteLine(step);
                if (!string.IsNullOrEmpty(step.Trim()) && !step.StartsWith("Step") && !step.Contains("--help"))
                {
                    Console.WriteLine();
                    Console.Write(ConsoleColorHelper.FormatProgress("Press any key to continue..."));
                    Console.ReadKey(true);
                    Console.WriteLine();
                }
            }

            if (ConfirmAction("Would you like to see the command help for the export command"))
            {
                var rootCommand = new RootCommand();
                var exportCommand = new Command("export", "Export Conditional Access policies from Entra ID");
                var outputOption = new Option<string>(
                    name: "--output",
                    description: "Output file path",
                    getDefaultValue: () => $"ConditionalAccessPolicies_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json"
                );
                exportCommand.AddOption(outputOption);

                ShowCommandHelp(exportCommand);
            }
        }

        /// <summary>
        /// Result of a command prompt interaction
        /// </summary>
        public class CommandPromptResult
        {
            /// <summary>
            /// The name of the selected command
            /// </summary>
            public string? CommandName { get; set; }

            /// <summary>
            /// Indicates if the user cancelled the operation
            /// </summary>
            public bool Cancelled { get; set; }
        }
    }
}

