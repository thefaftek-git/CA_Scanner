using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConditionalAccessExporter.Models;

namespace ConditionalAccessExporter.Services
{
    public class ScriptGenerationService
    {
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
            
            foreach (var action in remediation.Actions)
            {
                script.AppendLine($"# {action.Description}");
                script.AppendLine(GeneratePowerShellForAction(action, remediation.PolicyId));
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
            
            foreach (var action in remediation.Actions)
            {
                script.AppendLine($"# {action.Description}");
                script.AppendLine(GenerateAzureCliForAction(action, remediation.PolicyId));
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
            
            foreach (var action in remediation.Actions)
            {
                script.AppendLine($"# {action.Description}");
                script.AppendLine(GenerateTerraformForAction(action, remediation.PolicyId));
                script.AppendLine();
            }
            
            return script.ToString();
        }
        
        private string GeneratePowerShellForAction(RemediationAction action, string policyId)
        {
            return action.ActionType switch
            {
                RemediationActionType.EnablePolicy => $"Update-MgIdentityConditionalAccessPolicy -ConditionalAccessPolicyId \"{policyId}\" -State enabled",
                RemediationActionType.DisablePolicy => $"Update-MgIdentityConditionalAccessPolicy -ConditionalAccessPolicyId \"{policyId}\" -State disabled",
                RemediationActionType.UpdateConfiguration => GeneratePowerShellConfigUpdate(action, policyId),
                RemediationActionType.AddCondition => GeneratePowerShellAddCondition(action, policyId),
                RemediationActionType.RemoveCondition => GeneratePowerShellRemoveCondition(action, policyId),
                _ => $"# TODO: Implement action type {action.ActionType}"
            };
        }
        
        private string GenerateAzureCliForAction(RemediationAction action, string policyId)
        {
            return action.ActionType switch
            {
                RemediationActionType.EnablePolicy => $"az ad policy conditional-access update --id \"{policyId}\" --state enabled",
                RemediationActionType.DisablePolicy => $"az ad policy conditional-access update --id \"{policyId}\" --state disabled",
                RemediationActionType.UpdateConfiguration => GenerateAzureCliConfigUpdate(action, policyId),
                RemediationActionType.AddCondition => GenerateAzureCliAddCondition(action, policyId),
                RemediationActionType.RemoveCondition => GenerateAzureCliRemoveCondition(action, policyId),
                _ => $"# TODO: Implement action type {action.ActionType}"
            };
        }
        
        private string GenerateTerraformForAction(RemediationAction action, string policyId)
        {
            return action.ActionType switch
            {
                RemediationActionType.EnablePolicy => GenerateTerraformEnablePolicy(policyId),
                RemediationActionType.DisablePolicy => GenerateTerraformDisablePolicy(policyId),
                RemediationActionType.UpdateConfiguration => GenerateTerraformConfigUpdate(action, policyId),
                RemediationActionType.AddCondition => GenerateTerraformAddCondition(action, policyId),
                RemediationActionType.RemoveCondition => GenerateTerraformRemoveCondition(action, policyId),
                _ => $"# TODO: Implement action type {action.ActionType}"
            };
        }
        
        private string GeneratePowerShellConfigUpdate(RemediationAction action, string policyId)
        {
            var updates = action.Parameters.Select(p => $"-{p.Key} {FormatPowerShellValue(p.Value)}");
            return $"Update-MgIdentityConditionalAccessPolicy -ConditionalAccessPolicyId \"{policyId}\" {string.Join(" ", updates)}";
        }
        
        private string GenerateAzureCliConfigUpdate(RemediationAction action, string policyId)
        {
            var updates = action.Parameters.Select(p => $"--{p.Key.ToLower()} \"{p.Value}\"");
            return $"az ad policy conditional-access update --id \"{policyId}\" {string.Join(" ", updates)}";
        }
        
        private string GenerateTerraformEnablePolicy(string policyId)
        {
            return $@"resource ""azuread_conditional_access_policy"" ""policy_{policyId.Replace("-", "_")}"" {{
  display_name = ""Updated Policy""
  state        = ""enabled""
}}";
        }
        
        private string GenerateTerraformDisablePolicy(string policyId)
        {
            return $@"resource ""azuread_conditional_access_policy"" ""policy_{policyId.Replace("-", "_")}"" {{
  display_name = ""Updated Policy""
  state        = ""disabled""
}}";
        }
        
        private string GenerateTerraformConfigUpdate(RemediationAction action, string policyId)
        {
            var config = new StringBuilder();
            config.AppendLine($@"resource ""azuread_conditional_access_policy"" ""policy_{policyId.Replace("-", "_")}"" {{");
            
            foreach (var param in action.Parameters)
            {
                config.AppendLine($"  {param.Key.ToLower()} = {FormatTerraformValue(param.Value)}");
            }
            
            config.AppendLine("}");
            return config.ToString();
        }
        
        private string GeneratePowerShellAddCondition(RemediationAction action, string policyId)
        {
            return $"# Add condition: {string.Join(", ", action.Parameters.Select(p => $"{p.Key}={p.Value}"))}";
        }
        
        private string GenerateAzureCliAddCondition(RemediationAction action, string policyId)
        {
            return $"# Add condition: {string.Join(", ", action.Parameters.Select(p => $"{p.Key}={p.Value}"))}";
        }
        
        private string GenerateTerraformAddCondition(RemediationAction action, string policyId)
        {
            return $"# Add condition: {string.Join(", ", action.Parameters.Select(p => $"{p.Key}={p.Value}"))}";
        }
        
        private string GeneratePowerShellRemoveCondition(RemediationAction action, string policyId)
        {
            return $"# Remove condition: {string.Join(", ", action.Parameters.Select(p => $"{p.Key}={p.Value}"))}";
        }
        
        private string GenerateAzureCliRemoveCondition(RemediationAction action, string policyId)
        {
            return $"# Remove condition: {string.Join(", ", action.Parameters.Select(p => $"{p.Key}={p.Value}"))}";
        }
        
        private string GenerateTerraformRemoveCondition(RemediationAction action, string policyId)
        {
            return $"# Remove condition: {string.Join(", ", action.Parameters.Select(p => $"{p.Key}={p.Value}"))}";
        }
        
        private string FormatPowerShellValue(string value)
        {
            if (value.Contains(" ") || value.Contains("\""))
                return $"\"{value.Replace("\"", "\\\"")}\"";
            return value;
        }
        
        private string FormatTerraformValue(string value)
        {
            if (bool.TryParse(value, out bool boolValue))
                return boolValue.ToString().ToLower();
            if (int.TryParse(value, out _))
                return value;
            return $"\"{value}\"";
        }
    }
}
