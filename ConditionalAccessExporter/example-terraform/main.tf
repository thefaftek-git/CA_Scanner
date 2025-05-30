# Example Terraform configuration for Conditional Access Policies

variable "tenant_id" {
  description = "The Azure AD tenant ID"
  type        = string
  default     = "example-tenant-id"
}

variable "admin_group_id" {
  description = "Object ID of the admin group"
  type        = string
  default     = "admin-group-object-id"
}

variable "all_users" {
  description = "All users identifier"
  type        = string
  default     = "All"
}

locals {
  common_applications = [
    "All",
    "Office365"
  ]
  
  risk_levels = [
    "medium",
    "high"
  ]
}

# Conditional Access Policy - Require MFA for all users
resource "azuread_conditional_access_policy" "require_mfa_all_users" {
  display_name = "Require MFA for All Users"
  state        = "enabled"

  conditions {
    applications {
      include_applications = local.common_applications
      exclude_applications = []
    }

    users {
      include_users  = [var.all_users]
      exclude_users  = []
      include_groups = []
      exclude_groups = [var.admin_group_id]
    }

    client_app_types = [
      "browser",
      "mobileAppsAndDesktopClients"
    ]

    locations {
      include_locations = ["All"]
      exclude_locations = ["AllTrusted"]
    }

    sign_in_risk_levels = local.risk_levels
  }

  grant_controls {
    operator          = "OR"
    built_in_controls = ["mfa"]
  }

  session_controls {
    sign_in_frequency {
      is_enabled = true
      type       = "hours"
      value      = 24
    }
  }
}

# Conditional Access Policy - Block access from untrusted locations
resource "azuread_conditional_access_policy" "block_untrusted_locations" {
  display_name = "Block Access from Untrusted Locations"
  state        = "enabled"

  conditions {
    applications {
      include_applications = ["All"]
    }

    users {
      include_users = ["All"]
      exclude_groups = [var.admin_group_id]
    }

    locations {
      include_locations = ["All"]
      exclude_locations = ["AllTrusted"]
    }

    client_app_types = [
      "browser",
      "mobileAppsAndDesktopClients"
    ]
  }

  grant_controls {
    operator          = "OR"
    built_in_controls = ["block"]
  }
}

# Conditional Access Policy - Require compliant device for mobile
resource "azuread_conditional_access_policy" "require_compliant_device_mobile" {
  display_name = "Require Compliant Device for Mobile"
  state        = "enabledForReportingButNotEnforced"

  conditions {
    applications {
      include_applications = ["All"]
    }

    users {
      include_users = ["All"]
    }

    platforms {
      include_platforms = ["android", "iOS"]
    }

    client_app_types = ["mobileAppsAndDesktopClients"]
  }

  grant_controls {
    operator          = "OR"
    built_in_controls = ["compliantDevice", "domainJoinedDevice"]
  }

  session_controls {
    application_enforced_restrictions {
      is_enabled = true
    }

    cloud_app_security {
      is_enabled             = true
      cloud_app_security_type = "mcasConfigured"
    }
  }
}