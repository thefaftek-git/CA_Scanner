# Conditional Access Policy: Block legacy authentication
# Generated from JSON export on 2025-05-30 18:20:52 UTC

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

