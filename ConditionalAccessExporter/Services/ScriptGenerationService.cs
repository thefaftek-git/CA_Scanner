using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConditionalAccessExporter.Models;
using ConditionalAccessExporter.Utils;

namespace ConditionalAccessExporter.Services
{
    public class ScriptGenerationService
    {
        public async Task<ScriptGenerationResult> GenerateScriptAsync(PolicyRemediation remediation, RemediationFormat format, RemediationOptions options)
        {
            var result = new ScriptGenerationResult
            {
                Format = format,
                FilePath = GetScriptPath(remediation, format, options.OutputDirectory)
            };

            result.Script = format switch
            {
                RemediationFormat.PowerShell => GeneratePowerShellScript(remediation),
                RemediationFormat.AzureCLI => GenerateAzureCliScript(remediation),
                RemediationFormat.Terraform => GenerateTerraformScript(remediation),
                RemediationFormat.RestAPI => GenerateRestApiScript(remediation),
                RemediationFormat.ManualInstructions => GenerateManualInstructions(remediation),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };

            return result;
        }

        public string GeneratePowerShellScript(PolicyRemediation remediation)
        {
            var script = new StringBuilder();
            script.AppendLine("# PowerShell script for Conditional Access Policy remediation");
            script.AppendLine($"# Policy: {remediation.PolicyName}");
            script.AppendLine($"# Risk Level: {remediation.RiskLevel}");
            script.AppendLine();
            
            script.AppendLine("# Connect to Microsoft Graph");
            script.AppendLine("Connect-MgGraph -Scopes \"Policy.ReadWrite.ConditionalAccess\"");
            script.AppendLine();
            
            foreach (var step in remediation.Steps)
            {
                script.AppendLine($"# {step.Description}");
                script.AppendLine(GeneratePowerShellForStep(step, remediation));
                script.AppendLine();
            }
            
            return script.ToString();
        }
        
        public string GenerateAzureCliScript(PolicyRemediation remediation)
        {
            var script = new StringBuilder();
            script.AppendLine("#!/bin/bash");
            script.AppendLine("# Azure CLI script for Conditional Access Policy remediation");
            script.AppendLine($"# Policy: {remediation.PolicyName}");
            script.AppendLine($"# Risk Level: {remediation.RiskLevel}");
            script.AppendLine();
            
            script.AppendLine("# Login to Azure");
            script.AppendLine("az login");
            script.AppendLine();
            
            foreach (var step in remediation.Steps)
            {
                script.AppendLine($"# {step.Description}");
                script.AppendLine(GenerateAzureCliForStep(step, remediation));
                script.AppendLine();
            }
            
            return script.ToString();
        }
        
        public string GenerateTerraformScript(PolicyRemediation remediation)
        {
            var script = new StringBuilder();
            script.AppendLine("# Terraform configuration for Conditional Access Policy remediation");
            script.AppendLine($"# Policy: {remediation.PolicyName}");
            script.AppendLine($"# Risk Level: {remediation.RiskLevel}");
            script.AppendLine();
            
            script.AppendLine("terraform {");
            script.AppendLine("  required_providers {");
            script.AppendLine("    azuread = {");
            script.AppendLine("      source  = \"hashicorp/azuread\"");
            script.AppendLine("      version = \"~> 2.0\"");
            script.AppendLine("    }");
            script.AppendLine("  }");
            script.AppendLine("}");
            script.AppendLine();
            
            foreach (var step in remediation.Steps)
            {
                script.AppendLine($"# {step.Description}");
                script.AppendLine(GenerateTerraformForStep(step, remediation));
                script.AppendLine();
            }
            
            return script.ToString();
        }
        
        private string GeneratePowerShellForStep(RemediationStep step, PolicyRemediation remediation)
        {
            return remediation.Action switch
            {
                RemediationAction.Create => $"New-MgIdentityConditionalAccessPolicy -DisplayName \"{remediation.PolicyName}\" # {step.Description}",
                RemediationAction.Update => $"Update-MgIdentityConditionalAccessPolicy -ConditionalAccessPolicyId \"{remediation.PolicyId}\" # {step.Description}",
                RemediationAction.Delete => $"Remove-MgIdentityConditionalAccessPolicy -ConditionalAccessPolicyId \"{remediation.PolicyId}\" # {step.Description}",
                RemediationAction.NoAction => $"# No action required: {step.Description}",
                _ => $"# TODO: Implement action type {remediation.Action}"
            };
        }
        
        private string GenerateAzureCliForStep(RemediationStep step, PolicyRemediation remediation)
        {
            return remediation.Action switch
            {
                RemediationAction.Create => $"az ad policy conditional-access create --display-name \"{remediation.PolicyName}\" # {step.Description}",
                RemediationAction.Update => $"az ad policy conditional-access update --id \"{remediation.PolicyId}\" # {step.Description}",
                RemediationAction.Delete => $"az ad policy conditional-access delete --id \"{remediation.PolicyId}\" # {step.Description}",
                RemediationAction.NoAction => $"# No action required: {step.Description}",
                _ => $"# TODO: Implement action type {remediation.Action}"
            };
        }
        
        private string GenerateTerraformForStep(RemediationStep step, PolicyRemediation remediation)
        {
            return remediation.Action switch
            {
                RemediationAction.Create => GenerateTerraformCreatePolicy(remediation.PolicyId, remediation.PolicyName),
                RemediationAction.Update => GenerateTerraformUpdatePolicy(remediation.PolicyId, remediation.PolicyName),
                RemediationAction.Delete => $"# Remove resource: azuread_conditional_access_policy.policy_{remediation.PolicyId.Replace("-", "_")}",
                RemediationAction.NoAction => $"# No action required: {step.Description}",
                _ => $"# TODO: Implement action type {remediation.Action}"
            };
        }
        
        private string GenerateTerraformCreatePolicy(string policyId, string policyName)
        {
            return GenerateTerraformPolicy(policyId, policyName);
        }
        
        private string GenerateTerraformUpdatePolicy(string policyId, string policyName)
        {
            return GenerateTerraformPolicy(policyId, policyName);
        }
        
        private string GenerateTerraformPolicy(string policyId, string policyName)
        {
            return $@"resource ""azuread_conditional_access_policy"" ""policy_{policyId.Replace("-", "_")}"" {{
  display_name = ""{policyName}""
  state        = ""enabled""
}}";
        }
        
        public string GenerateRestApiScript(PolicyRemediation remediation)
        {
            var script = new StringBuilder();
            script.AppendLine("# REST API calls for Conditional Access Policy remediation");
            script.AppendLine($"# Policy: {remediation.PolicyName}");
            script.AppendLine($"# Risk Level: {remediation.RiskLevel}");
            script.AppendLine();
            
            foreach (var step in remediation.Steps)
            {
                script.AppendLine($"# {step.Description}");
                script.AppendLine(GenerateRestApiForStep(step, remediation));
                script.AppendLine();
            }
            
            return script.ToString();
        }
        
        public string GenerateManualInstructions(PolicyRemediation remediation)
        {
            var instructions = new StringBuilder();
            instructions.AppendLine("# Manual Instructions for Conditional Access Policy remediation");
            instructions.AppendLine($"Policy: {remediation.PolicyName}");
            instructions.AppendLine($"Risk Level: {remediation.RiskLevel}");
            instructions.AppendLine();
            
            foreach (var step in remediation.Steps)
            {
                instructions.AppendLine($"{step.Order}. {step.Description}");
                if (!string.IsNullOrEmpty(step.Action))
                {
                    instructions.AppendLine($"   Action: {step.Action}");
                }
                instructions.AppendLine();
            }
            
            return instructions.ToString();
        }
        
        private string GenerateRestApiForStep(RemediationStep step, PolicyRemediation remediation)
        {
            return remediation.Action switch
            {
                RemediationAction.Create => $"POST https://graph.microsoft.com/v1.0/identity/conditionalAccess/policies # {step.Description}",
                RemediationAction.Update => $"PATCH https://graph.microsoft.com/v1.0/identity/conditionalAccess/policies/{remediation.PolicyId} # {step.Description}",
                RemediationAction.Delete => $"DELETE https://graph.microsoft.com/v1.0/identity/conditionalAccess/policies/{remediation.PolicyId} # {step.Description}",
                RemediationAction.NoAction => $"# No API call required: {step.Description}",
                _ => $"# TODO: Implement REST API for {remediation.Action}"
            };
        }
        
        private string GetScriptPath(PolicyRemediation remediation, RemediationFormat format, string outputDirectory)
        {
            var extension = format switch
            {
                RemediationFormat.PowerShell => ".ps1",
                RemediationFormat.AzureCLI => ".sh",
                RemediationFormat.Terraform => ".tf",
                RemediationFormat.RestAPI => ".http",
                RemediationFormat.ManualInstructions => ".md",
                _ => ".txt"
            };
            
            var sanitizedPolicyName = FileHelper.SanitizeFileName(remediation.PolicyName);
            var fileName = $"{sanitizedPolicyName}_{format}{extension}";
            return Path.Combine(outputDirectory, fileName);
        }

    }
}
