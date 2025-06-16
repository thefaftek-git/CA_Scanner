





using ConditionalAccessExporter.Services.ValidationRules.SecurityRules;
using ConditionalAccessExporter.Services.ValidationRules.GovernanceRules;
using System.Reflection;

namespace ConditionalAccessExporter.Services.ValidationRules
{
    /// <summary>
    /// Registry for managing validation rules
    /// </summary>
    public class ValidationRuleRegistry
    {
        private readonly Dictionary<string, IValidationRule> _rules = new();
        private readonly Dictionary<ValidationRuleCategory, List<IValidationRule>> _rulesByCategory = new();

        public ValidationRuleRegistry()
        {
            RegisterBuiltInRules();
        }

        /// <summary>
        /// Registers all built-in validation rules
        /// </summary>
        private void RegisterBuiltInRules()
        {
            // Security rules
            RegisterRule(new MfaRequirementRule());
            RegisterRule(new DeviceComplianceRule());
            RegisterRule(new LegacyAuthenticationRule());
            
            // Governance rules
            RegisterRule(new PolicyNamingConventionRule());
        }

        /// <summary>
        /// Registers a validation rule
        /// </summary>
        public void RegisterRule(IValidationRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            _rules[rule.RuleId] = rule;

            if (!_rulesByCategory.ContainsKey(rule.Category))
                _rulesByCategory[rule.Category] = new List<IValidationRule>();

            _rulesByCategory[rule.Category].Add(rule);
        }

        /// <summary>
        /// Gets all registered rules
        /// </summary>
        public IEnumerable<IValidationRule> GetAllRules()
        {
            return _rules.Values;
        }

        /// <summary>
        /// Gets rules by category
        /// </summary>
        public IEnumerable<IValidationRule> GetRulesByCategory(ValidationRuleCategory category)
        {
            return _rulesByCategory.TryGetValue(category, out var rules) ? rules : Enumerable.Empty<IValidationRule>();
        }

        /// <summary>
        /// Gets a specific rule by ID
        /// </summary>
        public IValidationRule? GetRuleById(string ruleId)
        {
            return _rules.TryGetValue(ruleId, out var rule) ? rule : null;
        }

        /// <summary>
        /// Gets enabled rules based on options
        /// </summary>
        public IEnumerable<IValidationRule> GetEnabledRules(ValidationOptions options)
        {
            return _rules.Values.Where(rule => 
                (rule.IsEnabledByDefault || options.Configuration.ContainsKey(rule.RuleId)) &&
                !options.DisabledRules.Contains(rule.RuleId));
        }

        /// <summary>
        /// Loads custom rules from an assembly
        /// </summary>
        public void LoadCustomRules(Assembly assembly)
        {
            var ruleTypes = assembly.GetTypes()
                .Where(t => typeof(IValidationRule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var ruleType in ruleTypes)
            {
                try
                {
                    if (Activator.CreateInstance(ruleType) is IValidationRule rule)
                    {
                        RegisterRule(rule);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue loading other rules
                    Console.WriteLine($"Warning: Failed to load custom rule {ruleType.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Gets rule statistics
        /// </summary>
        public ValidationRuleStatistics GetStatistics()
        {
            var stats = new ValidationRuleStatistics
            {
                TotalRules = _rules.Count,
                RulesByCategory = _rulesByCategory.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => kvp.Value.Count),
                EnabledByDefault = _rules.Values.Count(r => r.IsEnabledByDefault)
            };

            return stats;
        }
    }

    /// <summary>
    /// Statistics about validation rules
    /// </summary>
    public class ValidationRuleStatistics
    {
        public int TotalRules { get; set; }
        public Dictionary<string, int> RulesByCategory { get; set; } = new();
        public int EnabledByDefault { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}





