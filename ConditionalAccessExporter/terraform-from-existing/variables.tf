# Terraform Variables
# Define reusable variables for Conditional Access policies

variable "tenant_id" {
  description = "Azure AD Tenant ID"
  type        = string
  sensitive   = true
}

