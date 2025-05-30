# Conditional Access Policies
# Generated from JSON export on 2025-05-30 18:20:34 UTC
# Total policies: 2

# Policy: Require MFA for all users
# State: Enabled
# Created: 2025-01-15
# Modified: 2025-05-20
resource "azuread_conditional_access_policy" "require_mfa_for_all_users" {
  display_name = "Require MFA for all users"
  state        = "enabled"

  conditions {
    applications {
      included_applications = ["All"]
    }

    users {
      included_users = ["All"]
    }

    client_app_types = ["All"]

  }

  grant_controls {
    operator          = "OR"
    built_in_controls = ["mfa"]
  }

}


# Policy: Block legacy authentication
# State: Enabled
# Created: 2025-01-10
# Modified: 2025-03-05
resource "azuread_conditional_access_policy" "block_legacy_authentication" {
  display_name = "Block legacy authentication"
  state        = "enabled"

  conditions {
    applications {
      included_applications = ["All"]
    }

    users {
      included_users = ["All"]
    }

    client_app_types = ["exchangeActiveSync", "other"]

  }

  grant_controls {
    operator          = "OR"
    built_in_controls = ["block"]
  }

}


